using System;
using System.Collections.Generic;
using System.Formats.Tar;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace ImportUnityPackage;

public enum UnityAssetKind { Unsupported, Material, Texture, Model, ModelSupport, Prefab, Scene }

public sealed class UnityAsset
{
	public string Guid { get; init; }
	public string Path { get; init; }
	public string Source { get; init; }
	public string Metadata { get; init; }
	public UnityAssetKind Kind { get; init; }
	public bool Selected { get; set; }
	UnityModel modelInfo;
	public UnityModel ModelInfo => modelInfo ??= UnityModel.Read( this );
}

public record ImportProgress( double Fraction, string Message, string Stage = null );

/// <summary>Reads Unity's GUID/asset, GUID/pathname, GUID/asset.meta tar layout.</summary>
public sealed class UnityArchive : IDisposable
{
	const long MaxEntryBytes = 2L * 1024 * 1024 * 1024;
	const long MaxTotalBytes = 32L * 1024 * 1024 * 1024;
	const string MetaExtension = ".meta";
	const long MaxMetaBytes = 16L * 1024 * 1024;
	static readonly Regex MetaGuidPattern = new( @"(?m)^\s*guid:\s*([0-9a-fA-F]{32})" );
	ImportWorkspace workspace;
	string scratch => workspace?.DirectoryPath;
	public bool HasExtractedFiles { get; private set; }
	public string[] CleanupWarnings => workspace?.Warnings.ToArray() ?? Array.Empty<string>();
	public string FileName { get; private set; }
	/// <summary>Non-null when the archive reads a live Unity project instead of a .unitypackage.</summary>
	public string SourceDirectory { get; private set; }
	public List<UnityAsset> Assets { get; } = new();
	public List<string> Warnings { get; } = new();

	public static UnityArchive Read( string file, IProgress<ImportProgress> progress, CancellationToken cancel, string assetsDirectory )
	{
		ArgumentException.ThrowIfNullOrWhiteSpace( assetsDirectory );
		if ( !System.IO.Path.IsPathFullyQualified( assetsDirectory ) ) throw new ArgumentException( "An absolute project Assets path is required.", nameof( assetsDirectory ) );
		var package = new UnityArchive { FileName = file };
		try
		{
			package.workspace = new ImportWorkspace( assetsDirectory );
			package.HasExtractedFiles = true;
			using var input = File.OpenRead( file );
			using var gzip = new GZipStream( input, CompressionMode.Decompress );
			using var tar = new TarReader( gzip );
			var entries = new Dictionary<string, Dictionary<string, string>>( StringComparer.OrdinalIgnoreCase );
			long total = 0;
			int count = 0;
			TarEntry entry;
			while ( (entry = tar.GetNextEntry( false )) != null )
			{
				cancel.ThrowIfCancellationRequested();
				if ( ++count > 200000 ) throw new InvalidDataException( "Package has too many archive entries." );
				if ( entry.EntryType == TarEntryType.Directory ) continue;
				if ( entry.EntryType != TarEntryType.RegularFile && entry.EntryType != TarEntryType.V7RegularFile )
					throw new InvalidDataException( "Package contains unsupported links or special archive entries." );
				if ( entry.Length > MaxEntryBytes || (total += entry.Length) > MaxTotalBytes )
					throw new InvalidDataException( "Package exceeds the 2 GiB per file / 32 GiB extracted size limit." );
				var name = entry.Name.Replace( '\\', '/' );
				if ( name.StartsWith( "./", StringComparison.Ordinal ) ) name = name[2..];
				// Asset Store downloads include a package thumbnail outside the GUID records.
				if ( name == ".icon.png" ) continue;
				var parts = name.Split( '/' );
				if ( parts.Length == 2 && parts[0] == "packagemanagermanifest" && parts[1] is "asset" or "pathname" or "asset.meta" ) continue;
				if ( parts.Length != 2 || !Regex.IsMatch( parts[0], "^[0-9a-fA-F]{32}$" ) )
					throw new InvalidDataException( $"Invalid Unity archive entry: {entry.Name}" );
				if ( parts[1] is not ("asset" or "asset.meta" or "pathname" or "preview.png") ) continue;
				if ( parts[1] == "preview.png" ) continue;
				if ( parts[1] != "asset" && entry.Length > 16 * 1024 * 1024 )
					throw new InvalidDataException( "Package metadata is too large." );
				if ( !entries.TryGetValue( parts[0], out var record ) ) entries[parts[0]] = record = new();
				if ( record.ContainsKey( parts[1] ) ) throw new InvalidDataException( $"Duplicate archive entry: {name}" );
				var target = System.IO.Path.Combine( package.scratch, parts[0] + "-" + parts[1] + ".iup" );
				using ( var output = new FileStream( target, FileMode.CreateNew ) )
					Copy( entry.DataStream, output, cancel );
				record[parts[1]] = target;
				progress?.Report( new( (double)input.Position / Math.Max( 1, input.Length ), $"Reading package ({entries.Count:N0} entries)…" ) );
			}
			var paths = new HashSet<string>( StringComparer.OrdinalIgnoreCase );
			foreach ( var (guid, record) in entries )
			{
				cancel.ThrowIfCancellationRequested();
				if ( !record.TryGetValue( "asset", out var source ) ) continue; // Unity folder metadata.
				if ( !record.TryGetValue( "pathname", out var pathname ) ) throw new InvalidDataException( $"Asset {guid} has no pathname." );
				// Unity writes the pathname on the first line, sometimes followed by a "00" trailer.
				var path = SafePath( (File.ReadLines( pathname, Encoding.UTF8 ).FirstOrDefault() ?? "").TrimEnd( '\0' ) );
				if ( !paths.Add( path ) ) throw new InvalidDataException( $"Duplicate asset path: {path}" );
				var kind = Classify( path );
				package.Assets.Add( new UnityAsset { Guid = guid, Path = path, Source = source,
					Metadata = record.GetValueOrDefault( "asset.meta" ), Kind = kind,
					Selected = kind != UnityAssetKind.Unsupported } );
			}
			package.Assets.Sort( (a, b) => StringComparer.OrdinalIgnoreCase.Compare( a.Path, b.Path ) );
			UnityModel.AssignPrefabMaterials( package );
			if ( package.Assets.Count == 0 ) throw new InvalidDataException( "This package contains no file assets." );
			return package;
		}
		catch ( Exception ex )
		{
			package.Dispose();
			if ( package.CleanupWarnings.Length > 0 ) ex.Data["StagingCleanupError"] = string.Join( "\n", package.CleanupWarnings );
			throw;
		}
	}

	/// <summary>
	/// Build the same UnityAsset list straight from a Unity project folder. Files live on disk (no
	/// workspace), so the existing ImportCatalog/UnityImport pipeline runs unchanged.
	/// A null folders collection means every immediate Assets/ subfolder.
	/// </summary>
	public static UnityArchive ReadDirectory( string projectFolder, IProgress<ImportProgress> progress,
		CancellationToken cancel, string assetsDirectory, IReadOnlyCollection<string> folders = null )
	{
		ArgumentException.ThrowIfNullOrWhiteSpace( projectFolder );
		ArgumentException.ThrowIfNullOrWhiteSpace( assetsDirectory );
		if ( !System.IO.Path.IsPathFullyQualified( assetsDirectory ) ) throw new ArgumentException( "An absolute project Assets path is required.", nameof( assetsDirectory ) );
		if ( !UnityProjectFolders.IsValidUnityFolder( projectFolder ) )
			throw new InvalidDataException( $"Not a Unity project folder (needs Assets/ and ProjectSettings/): {projectFolder}" );

		var selectedFolders = folders ?? UnityProjectFolders.ListAssetFolders( projectFolder );
		var package = new UnityArchive { FileName = projectFolder, SourceDirectory = projectFolder, HasExtractedFiles = true };
		var assetsRoot = System.IO.Path.Combine( projectFolder, UnityProjectFolders.AssetsFolderName );
		var guids = new HashSet<string>( StringComparer.OrdinalIgnoreCase );
		var paths = new HashSet<string>( StringComparer.OrdinalIgnoreCase );
		int count = 0;
		var files = Directory.EnumerateFiles( assetsRoot, "*", SearchOption.AllDirectories ).ToArray();
		for ( int i = 0; i < files.Length; i++ )
		{
			cancel.ThrowIfCancellationRequested();
			var source = files[i];
			if ( source.EndsWith( MetaExtension, StringComparison.OrdinalIgnoreCase ) ) continue; // sidecars, including folder metas
			var relative = Path.GetRelativePath( assetsRoot, source ).Replace( '\\', '/' );
			var path = SafePath( $"{UnityProjectFolders.AssetsFolderName}/{relative}" );
			if ( !UnityProjectFolders.IsPathSelected( path, selectedFolders ) ) continue;
			var metadata = source + MetaExtension;
			if ( !File.Exists( metadata ) ) { package.Warnings.Add( $"Skipped {relative}: no .meta sidecar." ); continue; }
			if ( new FileInfo( metadata ).Length > MaxMetaBytes ) { package.Warnings.Add( $"Skipped {relative}: .meta sidecar is too large." ); continue; }
			var match = MetaGuidPattern.Match( File.ReadAllText( metadata ) );
			if ( !match.Success ) { package.Warnings.Add( $"Skipped {relative}: .meta has no guid." ); continue; }
			var guid = match.Groups[1].Value.ToLowerInvariant();
			if ( !guids.Add( guid ) ) { package.Warnings.Add( $"Skipped {relative}: duplicate guid {guid}." ); continue; }
			if ( !paths.Add( path ) ) { package.Warnings.Add( $"Skipped {relative}: duplicate asset path." ); continue; }
			var kind = Classify( path );
			package.Assets.Add( new UnityAsset { Guid = guid, Path = path, Source = source, Metadata = metadata,
				Kind = kind, Selected = kind != UnityAssetKind.Unsupported } );
			count++;
			progress?.Report( new( (double)(i + 1) / files.Length, $"Reading project ({count:N0} assets)…" ) );
		}
		package.Assets.Sort( (a, b) => StringComparer.OrdinalIgnoreCase.Compare( a.Path, b.Path ) );
		UnityModel.AssignPrefabMaterials( package );
		if ( package.Assets.Count == 0 ) throw new InvalidDataException( "This Unity project contains no file assets in the selected folders." );
		return package;
	}

	public static string SafePath( string path )
	{
		path = path.Replace( '\\', '/' );
		if ( !path.StartsWith( "Assets/", StringComparison.OrdinalIgnoreCase ) )
			throw new InvalidDataException( $"Asset path must start with Assets/: {path}" );
		path = path[7..];
		foreach ( var part in path.Split( '/' ) )
		{
			var stem = part.Split( '.' )[0];
			if ( string.IsNullOrWhiteSpace( part ) || part is "." or ".." || part.EndsWith( '.' ) || part.EndsWith( ' ' ) ||
				part.Any( c => c < 32 || "<>:\"|?*".Contains( c ) ) ||
				Regex.IsMatch( stem, "^(CON|PRN|AUX|NUL|COM[0-9]|LPT[0-9])$", RegexOptions.IgnoreCase ) )
				throw new InvalidDataException( $"Unsafe asset path: {path}" );
		}
		return path;
	}

	public static UnityAssetKind Classify( string path ) => System.IO.Path.GetExtension( path ).ToLowerInvariant() switch
	{
		".mat" or ".terrainlayer" => UnityAssetKind.Material,
		".png" or ".jpg" or ".jpeg" or ".tga" or ".tif" or ".tiff" or ".exr" or ".psd" => UnityAssetKind.Texture,
		".fbx" or ".obj" or ".smd" or ".dmx" or ".vox" => UnityAssetKind.Model,
		".mtl" => UnityAssetKind.ModelSupport,
		".prefab" => UnityAssetKind.Prefab,
		".unity" => UnityAssetKind.Scene,
		_ => UnityAssetKind.Unsupported
	};

	internal static void Copy( Stream source, Stream target, CancellationToken cancel )
	{
		if ( source == null ) return;
		var buffer = new byte[128 * 1024];
		int read;
		while ( (read = source.Read( buffer, 0, buffer.Length )) > 0 )
		{
			cancel.ThrowIfCancellationRequested();
			target.Write( buffer, 0, read );
		}
	}

	public void Dispose()
	{
		workspace?.Dispose();
		HasExtractedFiles = false;
	}
}
