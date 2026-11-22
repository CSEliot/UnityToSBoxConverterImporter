using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using ImportUnityPackage;

// Headless command-line front end for the Unity Importer Plus pipeline. This is the SAME driver
// the harness has always used (Editor/Core via UnityImport.Run + ImportOptions.Auto); it now has
// real argument parsing and two console-output modes. Verbose is the default and is byte-for-byte
// the legacy output, so every existing gate/baseline keeps working.
//   harness [--scenes] [-h|--human] <package.unitypackage> <assetsDirectory> [report.json]
//   harness --help
// NOTE: -h / --human means "Human-Readable" (reduced verbosity), NOT help; --help is the help switch.
internal static class Program
{
	// Exposed, tweakable knobs (task Stylistic Notes: named properties, never inline magic).
	internal static class CliSettings
	{
		public const string ScenesFlag = "--scenes";
		public static readonly string[] HumanFlags = { "-h", "--human" };
		public static readonly string[] HelpFlags = { "--help" };
		public const string DefaultReportFile = "import-report.json";
		public const string Usage = "usage: harness [--scenes] [-h|--human] <package.unitypackage> <assetsDirectory> [report.json]";

		// Per-section output caps. int.MaxValue lists everything (the legacy/default behaviour);
		// the human values are small so the summary stays compact. Change any of these to tune output.
		public static int VerboseAssetLimit = int.MaxValue;
		public static int HumanAssetLimit = 0;          // human summary omits the per-asset listing
		public static int VerboseIssueLimit = int.MaxValue;
		public static int HumanIssueLimit = 3;          // human digest shows at most 3 detailed issues per section

		// Progress reporting is opt-in: the legacy harness used a silent Progress<T>, so both default
		// to false. Flip one to true to add progress lines without touching conversion code.
		public static bool VerboseProgress = false;
		public static bool HumanProgress = false;

		public static int Limit( Verbosity mode, bool asset ) => mode == Verbosity.Human
			? ( asset ? HumanAssetLimit : HumanIssueLimit )
			: ( asset ? VerboseAssetLimit : VerboseIssueLimit );

		public static bool Progress( Verbosity mode ) => mode == Verbosity.Human ? HumanProgress : VerboseProgress;
	}

	internal enum Verbosity { Verbose, Human }

	private sealed class Invocation
	{
		public bool Scenes;
		public Verbosity Verbosity = Verbosity.Verbose;
		public string Package;
		public string Assets;
		public string Report;
	}

	private static int Main( string[] args )
	{
		if ( args.Any( a => CliSettings.HelpFlags.Contains( a, StringComparer.Ordinal ) ) )
		{
			PrintHelp();
			return 0;
		}
		if ( !TryParse( args, out var cmd, out var error ) )
		{
			Console.Error.WriteLine( error );
			Console.Error.WriteLine( CliSettings.Usage );
			return 2;
		}
		return Run( cmd );
	}

	// Flags may appear in any order; the positional contract is unchanged.
	private static bool TryParse( string[] args, out Invocation cmd, out string error )
	{
		cmd = new Invocation();
		error = null;
		var positional = new List<string>();
		foreach ( var a in args )
		{
			if ( a == CliSettings.ScenesFlag ) cmd.Scenes = true;
			else if ( CliSettings.HumanFlags.Contains( a, StringComparer.Ordinal ) ) cmd.Verbosity = Verbosity.Human;
			else if ( a.Length > 1 && a[0] == '-' ) { error = $"unknown option: {a}"; return false; }
			else positional.Add( a );
		}
		if ( positional.Count is < 2 or > 3 )
		{
			error = "expected <package.unitypackage> <assetsDirectory> [report.json]";
			return false;
		}
		cmd.Package = Path.GetFullPath( positional[0] );
		cmd.Assets = Path.GetFullPath( positional[1] );
		cmd.Report = positional.Count == 3 ? positional[2] : CliSettings.DefaultReportFile;
		return true;
	}

	private static int Run( Invocation cmd )
	{
		Directory.CreateDirectory( cmd.Assets );
		var channelsScript = Path.Combine( AppContext.BaseDirectory, "channels.py" );
		// Portable launcher: py -3 on Windows, python3 elsewhere (unchanged from the harness).
		var python = OperatingSystem.IsWindows() ? "py" : "python3";

		// Stand-in for Editor/TextureChannels.cs, which needs the engine's Bitmap: one Pillow call per request.
		void Extract( IReadOnlyList<TextureChannelRequest> requests, CancellationToken cancel )
		{
			foreach ( var r in requests )
			{
				var psi = new ProcessStartInfo( python ) { RedirectStandardError = true };
				if ( OperatingSystem.IsWindows() ) psi.ArgumentList.Add( "-3" );
				foreach ( var a in new[] { channelsScript, r.Source, r.Destination, r.Channel.ToString(), r.Scale.ToString( CultureInfo.InvariantCulture ), r.Invert ? "1" : "0" } ) psi.ArgumentList.Add( a );
				using var p = Process.Start( psi );
				var err = p.StandardError.ReadToEnd();
				p.WaitForExit();
				if ( p.ExitCode != 0 ) throw new InvalidDataException( $"channel extraction failed for {r.Source}: {err}" );
			}
		}

		var progress = new Progress<ImportProgress>( _ => { if ( CliSettings.Progress( cmd.Verbosity ) ) Console.WriteLine( "  importing..." ); } );
		using var archive = UnityArchive.Read( cmd.Package, progress, CancellationToken.None, cmd.Assets );
		var result = UnityImport.Run( archive, cmd.Assets, ImportOptions.Auto( scenes: cmd.Scenes ), progress, CancellationToken.None, extractChannels: Extract );

		PrintReport( archive, result, cmd.Verbosity, cmd.Report );

		// The report path and bytes are identical in both modes; only the console text differs.
		File.WriteAllText( cmd.Report, result.ReportJson );
		return result.Unresolved.Length == 0 ? 0 : 1;
	}

	// One formatting routine for both modes: the mode only selects the exposed caps above (DRY).
	private static void PrintReport( UnityArchive archive, ImportResult result, Verbosity mode, string reportPath )
	{
		Console.WriteLine( $"archive assets: {archive.Assets.Count}" );
		foreach ( var a in archive.Assets.OrderBy( a => a.Path ).Take( CliSettings.Limit( mode, asset: true ) ) )
			Console.WriteLine( $"  {a.Kind,-12} {a.Path}" );

		Console.WriteLine( $"assets={result.AssetCount} converted={result.ConvertedCount} files={result.Files.Length} changed={result.ChangedFiles.Length}" );

		if ( mode == Verbosity.Human )
		{
			// Compact digest: counts per category, then unresolved issues grouped by code.
			Console.WriteLine( $"issues: unresolved={result.Unresolved.Length} warnings={result.Warnings.Length} info={result.Information.Length}" );
			foreach ( var g in result.Unresolved.GroupBy( i => i.Code ).OrderByDescending( g => g.Count() ).ThenBy( g => g.Key ) )
				Console.WriteLine( $"  {g.Key}: {g.Count()}" );
		}

		var issueLimit = CliSettings.Limit( mode, asset: false );
		foreach ( var i in result.Unresolved.Take( issueLimit ) ) Console.WriteLine( $"UNRESOLVED {i.Severity} {i.Code} {i.Asset}: {i.Message}" );
		foreach ( var w in result.Warnings.Take( issueLimit ) ) Console.WriteLine( $"WARN {w}" );
		foreach ( var i in result.Information.Take( issueLimit ) ) Console.WriteLine( $"INFO {i}" );
		if ( mode == Verbosity.Human && ( result.Unresolved.Length > issueLimit || result.Warnings.Length > issueLimit || result.Information.Length > issueLimit ) )
			Console.WriteLine( $"  (details truncated; full list in {reportPath})" );
	}

	private static void PrintHelp()
	{
		Console.WriteLine( "Unity Importer Plus - headless command-line importer" );
		Console.WriteLine();
		Console.WriteLine( CliSettings.Usage );
		Console.WriteLine();
		Console.WriteLine( "Arguments:" );
		Console.WriteLine( "  <package.unitypackage>  Unity package to import (required)" );
		Console.WriteLine( "  <assetsDirectory>       Output assets root (required; created if missing)" );
		Console.WriteLine( "  [report.json]           Report path (default: " + CliSettings.DefaultReportFile + " in the current directory)" );
		Console.WriteLine( "Options:" );
		Console.WriteLine( "  --scenes                 Also convert Unity scenes" );
		Console.WriteLine( "  -h, --human              Human-Readable output: reduced verbosity (NOT help)" );
		Console.WriteLine( "  --help                   Show this help" );
		Console.WriteLine();
		Console.WriteLine( "Output is verbose by default; -h/--human prints counts plus a compact issue digest." );
		Console.WriteLine( "Exit code: 0 on success, 1 when the report has unresolved issues, 2 on bad arguments." );
	}
}
