using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;

namespace ImportUnityPackage;

/// <summary>Short-lived work under Assets/Imported/.iup-temp. Leases protect active runs during cleanup.</summary>
public sealed class ImportWorkspace : IDisposable
{
	public string DirectoryPath { get; }
	public List<string> Warnings { get; } = new();
	readonly string root;
	FileStream lease;
	public ImportWorkspace( string assetsDirectory )
	{
		root = Path.Combine( UnityImport.ImportedDirectory( assetsDirectory ), ".iup-temp" );
		UnityImport.CheckDirectory( root );
		using var gate = Enter( root );
		Sweep( root, Warnings );
		Directory.CreateDirectory( root );
		DirectoryPath = Path.Combine( root, Guid.NewGuid().ToString( "N" ) );
		Directory.CreateDirectory( DirectoryPath );
		try { lease = new FileStream( Path.Combine( DirectoryPath, ".active" ), FileMode.CreateNew, FileAccess.ReadWrite, FileShare.None ); }
		catch { RemoveTree( DirectoryPath ); RemoveEmptyRoot( root ); throw; }
	}

	public static string[] CleanupAbandoned( string assetsDirectory )
	{
		var root = Path.Combine( UnityImport.ImportedDirectory( assetsDirectory ), ".iup-temp" );
		UnityImport.CheckDirectory( root );
		var warnings = new List<string>();
		using var gate = Enter( root );
		Sweep( root, warnings );
		RemoveEmptyRoot( root );
		return warnings.ToArray();
	}

	static void Sweep( string root, List<string> warnings )
	{
		if ( !Directory.Exists( root ) ) return;
		foreach ( var run in Directory.EnumerateDirectories( root ) )
		{
			if ( !Guid.TryParseExact( Path.GetFileName( run ), "N", out _ ) ) continue;
			try
			{
				UnityImport.CheckDirectory( Path.Combine( run, ".active" ) );
				// The exclusive lease stays open throughout an active run, including other editor processes.
				using ( new FileStream( Path.Combine( run, ".active" ), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None ) ) { }
			}
			catch ( IOException ex ) when ( (ex.HResult & 0xffff) is 32 or 33 or 11 ) { continue; }
			catch ( Exception ex ) when ( ex is IOException or UnauthorizedAccessException )
			{
				warnings.Add( $"Temporary folder could not be checked for cleanup: {run}. {ex.Message}" );
				continue;
			}
			try { RemoveTree( run ); }
			catch ( Exception ex ) when ( ex is IOException or UnauthorizedAccessException ) { warnings.Add( $"Temporary folder could not be removed: {run}. {ex.Message}" ); }
		}
	}

	// All recursive deletion is constrained to importer work and rejects linked directories/files.
	internal static void RemoveTree( string path )
	{
		if ( !Directory.Exists( path ) ) return;
		path = Path.GetFullPath( path );
		var run = new DirectoryInfo( path );
		while ( run != null && run.Parent?.Name != ".iup-temp" ) run = run.Parent;
		if ( run == null || !Guid.TryParseExact( run.Name, "N", out _ ) || !string.Equals( run.Parent.Parent?.Name, "Imported", StringComparison.OrdinalIgnoreCase ) )
			throw new IOException( "Refusing to clean outside Assets/Imported/.iup-temp run folders." );
		UnityImport.CheckDirectory( path );
		void Check( string directory )
		{
			foreach ( var item in Directory.EnumerateFileSystemEntries( directory ) )
			{
				var flags = File.GetAttributes( item );
				if ( flags.HasFlag( FileAttributes.ReparsePoint ) ) throw new IOException( "Refusing to clean linked temporary content: " + item );
				if ( flags.HasFlag( FileAttributes.Directory ) ) Check( item );
			}
		}
		Check( path );
		ImportStorage.Retry( () => Directory.Delete( path, true ), CancellationToken.None );
	}

	static void RemoveEmptyRoot( string root )
	{
		if ( Directory.Exists( root ) && !Directory.EnumerateFileSystemEntries( root ).Any() ) Directory.Delete( root );
	}

	public void Dispose()
	{
		if ( lease == null ) return;
		using var gate = Enter( root );
		lease.Dispose(); lease = null;
		try { RemoveTree( DirectoryPath ); RemoveEmptyRoot( root ); }
		catch ( Exception ex ) when ( ex is IOException or UnauthorizedAccessException )
		{
			Warnings.Add( $"Temporary folder could not be removed: {DirectoryPath}. Cleanup will be retried next time the importer opens. {ex.Message}" );
		}
	}

	static IDisposable Enter( string root ) => new Gate( "iup-temp-" + Convert.ToHexString( SHA256.HashData( Encoding.UTF8.GetBytes( root.ToLowerInvariant() ) ) ) );
	sealed class Gate : IDisposable
	{
		readonly Mutex mutex;
		public Gate( string name ) { mutex = new Mutex( false, name ); try { mutex.WaitOne(); } catch ( AbandonedMutexException ) { } }
		public void Dispose() { mutex.ReleaseMutex(); mutex.Dispose(); }
	}
}
