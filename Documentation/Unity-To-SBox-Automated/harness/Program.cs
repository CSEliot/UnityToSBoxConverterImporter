using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using ImportUnityPackage;

// Headless driver for UnityImport.Run: the same Core code the editor window calls, with the
// same automatic output choices (ImportOptions.Auto). Writes into <assetsDirectory>/Imported/.
// usage: harness [--scenes] <package.unitypackage> <assetsDirectory> [report.json]
// --scenes also converts the package's Unity scenes to s&box scenes (the window's "Also import Unity scenes" toggle).
var scenes = args.Contains( "--scenes" );
args = args.Where( a => a != "--scenes" ).ToArray();
if ( args.Length is < 2 or > 3 ) { Console.Error.WriteLine( "usage: harness [--scenes] <package.unitypackage> <assetsDirectory> [report.json]" ); return 2; }
var package = Path.GetFullPath( args[0] );
var assets = Path.GetFullPath( args[1] );
Directory.CreateDirectory( assets );
var channelsScript = Path.Combine( AppContext.BaseDirectory, "channels.py" );
var python = OperatingSystem.IsWindows() ? "py" : "python3";

// Stand-in for Editor/TextureChannels.cs, which needs the engine's Bitmap: one Pillow call per request.
void Extract( System.Collections.Generic.IReadOnlyList<TextureChannelRequest> requests, CancellationToken cancel )
{
	foreach ( var r in requests )
	{
		var psi = new ProcessStartInfo( python ) { RedirectStandardError = true };
		if ( OperatingSystem.IsWindows() ) psi.ArgumentList.Add( "-3" );
		foreach ( var a in new[] { channelsScript, r.Source, r.Destination, r.Channel.ToString(), r.Scale.ToString( System.Globalization.CultureInfo.InvariantCulture ), r.Invert ? "1" : "0" } ) psi.ArgumentList.Add( a );
		using var p = Process.Start( psi );
		var err = p.StandardError.ReadToEnd();
		p.WaitForExit();
		if ( p.ExitCode != 0 ) throw new InvalidDataException( $"channel extraction failed for {r.Source}: {err}" );
	}
}

var progress = new Progress<ImportProgress>( _ => { } );
using var archive = UnityArchive.Read( package, progress, CancellationToken.None, assets );
Console.WriteLine( $"archive assets: {archive.Assets.Count}" );
foreach ( var a in archive.Assets.OrderBy( a => a.Path ) ) Console.WriteLine( $"  {a.Kind,-12} {a.Path}" );
var result = UnityImport.Run( archive, assets, ImportOptions.Auto( scenes: scenes ), progress, CancellationToken.None, extractChannels: Extract );
Console.WriteLine( $"assets={result.AssetCount} converted={result.ConvertedCount} files={result.Files.Length} changed={result.ChangedFiles.Length}" );
foreach ( var i in result.Unresolved ) Console.WriteLine( $"UNRESOLVED {i.Severity} {i.Code} {i.Asset}: {i.Message}" );
foreach ( var w in result.Warnings ) Console.WriteLine( $"WARN {w}" );
foreach ( var i in result.Information ) Console.WriteLine( $"INFO {i}" );
// The report goes where the caller says (default: current directory), never beside the target Assets folder.
File.WriteAllText( args.Length == 3 ? args[2] : "import-report.json", result.ReportJson );
return result.Unresolved.Length == 0 ? 0 : 1;
