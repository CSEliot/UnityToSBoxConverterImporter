using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;

namespace ImportUnityPackage;

public sealed class ImportFileChoice
{
	public string Path { get; init; }
	public string Status { get; init; }
	public string Detail { get; init; }
	public bool Conflict { get; init; }
	public bool Replace { get; set; }
	internal string ExistingHash { get; init; }
	public string Action => ExistingHash == null ? "Add" : !Conflict ? "Reuse" : Replace ? "Replace" : "Keep existing";
}

/// <summary>One-off content comparison. No history, backups, automatic reports or rollback.</summary>
public sealed class ImportMergePlan : IDisposable
{
	readonly ImportPlan original;
	readonly ImportWorkspace workspace;
	ImportResult prepared;
	bool committed;
	public string Destination { get; }
	public List<ImportFileChoice> Outputs { get; } = new();
	public string[] CleanupWarnings => workspace.Warnings.ToArray();

	ImportMergePlan( ImportPlan plan, string assets )
	{
		original = plan;
		Destination = UnityImport.ImportedDirectory( assets );
		UnityImport.CheckDirectory( Destination );
		foreach ( var item in plan.Assets )
			if ( item.Asset.Path.Split( '/' )[0].Equals( ".iup-temp", StringComparison.OrdinalIgnoreCase ) )
				throw new InvalidDataException( $"Reserved importer output path: {item.Asset.Path}" );
		workspace = new ImportWorkspace( assets );
	}

	public static ImportMergePlan Create( ImportPlan plan, string assets, CancellationToken cancel = default )
	{
		cancel.ThrowIfCancellationRequested();
		return new( plan, assets );
	}

	public void Prepare( IProgress<ImportProgress> progress, CancellationToken cancel, ExtractTextureChannel extract = null, ExtractTextureChannels extractChannels = null )
	{
		if ( prepared != null ) throw new InvalidOperationException( "This import is already prepared." );
		prepared = UnityImport.PrepareFiles( original, Path.Combine( workspace.DirectoryPath, "output" ), Destination, progress, cancel, extract, extractChannels );
		progress?.Report( new( 1, "Comparing destination files…", "Checking conflicts" ) );
		foreach ( var file in prepared.Files )
		{
			cancel.ThrowIfCancellationRequested();
			var path = Path.GetRelativePath( prepared.Directory, file ).Replace( '\\', '/' );
			if ( !path.EndsWith( ".iup", StringComparison.Ordinal ) ) throw new InvalidDataException( "Invalid staging file" );
			path = path[..^4];
			var existing = HashFile( Target( path ), cancel );
			var incoming = HashFile( file, cancel );
			var conflict = existing != null && existing != incoming;
			Outputs.Add( new() { Path = path, ExistingHash = existing, Conflict = conflict,
				Status = existing == null ? "New" : conflict ? "Different" : "Identical",
				Detail = conflict ? "Keeping this file retains its current content; it may differ from the rest of the incoming package." : "" } );
		}
	}

	public ImportResult Commit( IProgress<ImportProgress> progress, CancellationToken cancel )
	{
		if ( prepared == null || committed ) throw new InvalidOperationException( "Prepare a new import before writing." );
		committed = true;
		// Recheck all reviewed files before the first write. Individual writes check again below.
		foreach ( var choice in Outputs )
			if ( HashFile( Target( choice.Path ), cancel ) != choice.ExistingHash ) throw new IOException( $"Destination changed since review: {choice.Path}. Review the import again." );
		var writes = Outputs.Where( c => c.ExistingHash == null || c.Conflict && c.Replace ).ToArray();
		var createdDirectories = new List<string>();
		try
		{
			for ( var i = 0; i < writes.Length; i++ )
			{
				cancel.ThrowIfCancellationRequested();
				var choice = writes[i];
				progress?.Report( new( (double)i / Math.Max( 1, writes.Length ), $"Writing {choice.Path}" ) );
				cancel.ThrowIfCancellationRequested();
				var target = Target( choice.Path );
				CreateOutputDirectories( Path.GetDirectoryName( target ), createdDirectories );
				// Staging is on the same filesystem. Rename a complete file; never leave a partial copy.
				ImportStorage.Retry( () =>
				{
					if ( HashFile( target, cancel ) != choice.ExistingHash ) throw new IOException( $"Destination changed while writing: {choice.Path}" );
					File.Move( Path.Combine( prepared.Directory, choice.Path + ".iup" ), target, choice.ExistingHash != null );
				}, cancel );
			}
		}
		finally { RemoveEmptyOutputDirectories( createdDirectories ); }
		var report = JsonNode.Parse( prepared.ReportJson );
		report["Destination"] = Destination;
		report["Files"] = JsonSerializer.SerializeToNode( Outputs.Select( c => c.Path ).ToArray() );
		report["Conflicts"] = JsonSerializer.SerializeToNode( Outputs.Where( c => c.Conflict ).Select( c => new { c.Path, c.Action } ).ToArray() );
		progress?.Report( new( 1, "Files imported" ) );
		return prepared with { Directory = Destination, Files = Outputs.Select( c => Target( c.Path ) ).ToArray(),
			ReportJson = report.ToJsonString( new JsonSerializerOptions { WriteIndented = true } ), ChangedFiles = writes.Select( c => Target( c.Path ) ).ToArray() };
	}

	void CreateOutputDirectories( string directory, List<string> created )
	{
		var missing = new Stack<string>();
		while ( !Directory.Exists( directory ) && directory.StartsWith( Destination + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase ) )
		{
			missing.Push( directory );
			directory = Path.GetDirectoryName( directory );
		}
		foreach ( var path in missing )
		{
			UnityImport.CheckDirectory( path );
			if ( Directory.Exists( path ) ) continue;
			Directory.CreateDirectory( path );
			created.Add( path );
		}
	}

	void RemoveEmptyOutputDirectories( List<string> created )
	{
		// Only this run's new directories, deepest first. Never delete recursively or undo completed writes.
		foreach ( var path in created.AsEnumerable().Reverse() )
		{
			try
			{
				UnityImport.CheckDirectory( path );
				if ( Directory.Exists( path ) && !Directory.EnumerateFileSystemEntries( path ).Any() )
					ImportStorage.Retry( () => Directory.Delete( path, false ), CancellationToken.None );
			}
			catch ( Exception ex ) when ( ex is IOException or UnauthorizedAccessException )
			{
				workspace.Warnings.Add( $"Empty output folder could not be removed: {path}. {ex.Message}" );
			}
		}
	}

	string Target( string path )
	{
		var safe = UnityArchive.SafePath( "Assets/" + path );
		var target = Path.GetFullPath( UnityImport.DiskPath( Destination, safe ) );
		UnityImport.CheckDirectory( target );
		if ( Directory.Exists( target ) ) throw new IOException( $"A directory occupies the file destination: {path}" );
		return target;
	}

	public static string HashFile( string path, CancellationToken cancel = default )
	{
		if ( !File.Exists( path ) ) return null;
		using var input = File.OpenRead( path );
		using var hash = IncrementalHash.CreateHash( HashAlgorithmName.SHA256 );
		var buffer = new byte[128 * 1024];
		int count;
		while ( (count = input.Read( buffer, 0, buffer.Length )) > 0 ) { cancel.ThrowIfCancellationRequested(); hash.AppendData( buffer, 0, count ); }
		return Convert.ToHexString( hash.GetHashAndReset() ).ToLowerInvariant();
	}

	public void Dispose() => workspace.Dispose();
}
