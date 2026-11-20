using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Editor;
using Sandbox;

namespace ImportUnityPackage;

public record ImportPreparationResult( string[] Warnings, bool Cancelled, int Rebuilt )
{
	public string[] Errors { get; init; } = Array.Empty<string>();
}

/// <summary>Runs on the editor context after the imported folder has been committed.</summary>
public static class ImportAssetPreparation
{
	static Task repairTask;

	/// <summary>Repair already compiled resources in an existing import without reimporting source files.</summary>
	[ConCmd( "unity_import_repair_resources" )]
	public static void Repair( string directory )
	{
		if ( repairTask is { IsCompleted: false } ) return;
		repairTask = RepairAsync( directory );
	}

	static async Task RepairAsync( string directory )
	{
		try
		{
			var assets = Project.Current.GetAssetsPath();
			var root = UnityImport.ImportedDirectory( assets ) + Path.DirectorySeparatorChar;
			var target = Path.GetFullPath( Path.IsPathRooted( directory ) ? directory : Path.Combine( assets, directory ) );
			if ( !(target.Equals( root.TrimEnd( Path.DirectorySeparatorChar ), StringComparison.OrdinalIgnoreCase ) || target.StartsWith( root, StringComparison.OrdinalIgnoreCase )) || !Directory.Exists( target ) )
				throw new ArgumentException( "Choose an existing package folder under this project's Assets/imported." );
			var stale = Directory.EnumerateFiles( target, "*", SearchOption.AllDirectories )
				.Where( f => ResourceOrder( f ) > 0 )
				.Where( f => AssetSystem.FindByPath( f ) is { IsCompiled: true } asset && !asset.IsCompiledAndUpToDate ).ToArray();
			Log.Info( $"UNITY_RESOURCE_REPAIR: preparing {stale.Length} out-of-date resources in {target}" );
			var result = await Run( stale, null, CancellationToken.None );
			foreach ( var warning in result.Warnings ) Log.Warning( warning );
			foreach ( var error in result.Errors ) Log.Error( error );
			Log.Info( $"UNITY_RESOURCE_REPAIR complete: {stale.Length} checked, {result.Rebuilt} full rebuilds, {result.Warnings.Length} warnings, {result.Errors.Length} errors." );
		}
		catch ( Exception ex ) { Log.Error( $"UNITY_RESOURCE_REPAIR failed: {ex}" ); }
	}

	public static async Task<ImportPreparationResult> RunImport( ImportResult result, IProgress<ImportProgress> progress, CancellationToken cancel )
	{
		var files = result.Files.ToHashSet( StringComparer.OrdinalIgnoreCase );
		var warnings = new List<string>();
		// Discovering dependants registers every committed file and can take minutes on large packages.
		var changed = result.ChangedFiles.ToArray();
		var stopwatch = System.Diagnostics.Stopwatch.StartNew();
		for ( var i = 0; i < changed.Length; i++ )
		{
			if ( cancel.IsCancellationRequested ) break;
			if ( i % 8 == 0 || i == changed.Length - 1 )
				progress?.Report( new( 0.1 * (i + 1) / Math.Max( 1, changed.Length ), $"Discovering affected resources {i + 1}/{changed.Length}: {Path.GetFileName( changed[i] )} · {stopwatch.Elapsed.TotalSeconds:F0}s elapsed" ) );
			try
			{
				var asset = AssetSystem.RegisterFile( changed[i] );
				if ( asset == null ) continue;
				foreach ( var dependant in asset.GetDependants( true ) )
					if ( ResourceOrder( dependant.Path ) > 0 && dependant.HasSourceFile ) files.Add( dependant.GetSourceFile( true ) );
			}
			catch ( Exception ex ) { warnings.Add( $"{changed[i]}: could not discover affected resources: {ex.Message}" ); }
		}
		// Run reports 0..1; slice it into the remaining 90% so the whole stage stays monotonic.
		var prepared = await Run( files, progress is null ? null : new StagedProgress( progress, 0.1, 0.9 ), cancel );
		return prepared with { Warnings = warnings.Concat( prepared.Warnings ).ToArray() };
	}

	public static async Task<ImportPreparationResult> Run( IEnumerable<string> files, IProgress<ImportProgress> progress, CancellationToken cancel )
	{
		var warnings = new List<string>();
		var errors = new List<string>();
		var resources = new List<Asset>();
		var rebuilt = 0;
		var stopwatch = System.Diagnostics.Stopwatch.StartNew();
		try
		{
			// Register all sources before compiling textures, then materials, then models.
			var sources = files.Distinct( StringComparer.OrdinalIgnoreCase ).OrderBy( ResourceOrder ).ToArray();
			for ( var i = 0; i < sources.Length; i++ )
			{
				var file = sources[i];
				cancel.ThrowIfCancellationRequested();
				if ( i % 8 == 0 || i == sources.Length - 1 )
					progress?.Report( new( 0.25 * (i + 1) / Math.Max( 1, sources.Length ), $"Registering {i + 1}/{sources.Length}: {Path.GetFileName( file )} · {errors.Count} failed so far · {stopwatch.Elapsed.TotalSeconds:F0}s elapsed" ) );
				if ( Path.GetExtension( file ).ToLowerInvariant() is ".mat" or ".json" or ".mtl" ) continue;
				try
				{
					var asset = AssetSystem.RegisterFile( file );
					if ( ResourceOrder( file ) > 0 )
					{
						if ( asset == null ) throw new InvalidOperationException( "Asset registration returned no resource" );
						resources.Add( asset );
					}
				}
				catch ( Exception ex ) { errors.Add( $"{file}: registration failed: {ex.Message}" ); }
			}
			stopwatch.Restart();
			var total = resources.Count;
			for ( var i = 0; i < total; i++ )
			{
				cancel.ThrowIfCancellationRequested();
				var asset = resources[i];
				progress?.Report( new( 0.25 + 0.75 * i / Math.Max( 1, total ), $"Compiling {i + 1}/{total}: {asset.Name} · {errors.Count} failed so far · {stopwatch.Elapsed.TotalSeconds:F0}s compiling" ) );
				try
				{
					if ( !asset.IsCompiledAndUpToDate ) await asset.CompileIfNeededAsync().AsTask().WaitAsync( cancel );
					cancel.ThrowIfCancellationRequested();
					// Compilation and publishing generated children to the registry are separate steps.
					// Let the editor process completion notifications before checking dependency validity.
					await WaitForReady( asset, TimeSpan.FromMilliseconds( 250 ), cancel );
					// Incremental compilation can leave generated children absent from the asset registry,
					// even when their _c files exist. One full rebuild restores those dependencies.
					if ( !asset.IsCompileFailed && !asset.IsCompiledAndUpToDate )
					{
						var accepted = asset.Compile( true );
						rebuilt++;
						if ( accepted ) await WaitForReady( asset, TimeSpan.FromSeconds( 30 ), cancel );
					}
					if ( asset.IsCompileFailed || !asset.IsCompiledAndUpToDate )
						errors.Add( $"{asset.Path}: " + (asset.IsCompileFailed
							? "the resource compiler reported a failure; see its diagnostics in the editor console."
							: !asset.IsCompiled ? "no compiled resource became available after compilation."
							: "compiled resource dependencies did not become ready within 30 seconds. Reimport or run unity_import_repair_resources for this folder.") );
				}
				catch ( OperationCanceledException ) { throw; }
				catch ( Exception ex ) { errors.Add( $"{asset.Path}: resource preparation failed: {ex.Message}" ); }
				await Task.Delay( 1, cancel );
			}
		}
		catch ( OperationCanceledException ) when ( cancel.IsCancellationRequested )
		{
			return new( warnings.ToArray(), true, rebuilt ) { Errors = errors.ToArray() };
		}
		progress?.Report( new( 1, "Import complete" ) );
		return new( warnings.ToArray(), false, rebuilt ) { Errors = errors.ToArray() };
	}

	/// <summary>Maps an inner 0..1 progress into a slice of the caller's range.</summary>
	sealed class StagedProgress( IProgress<ImportProgress> inner, double start, double scale ) : IProgress<ImportProgress>
	{
		public void Report( ImportProgress value ) => inner.Report( new( start + scale * value.Fraction, value.Message, value.Stage ) );
	}

	static async Task WaitForReady( Asset asset, TimeSpan timeout, CancellationToken cancel )
	{
		var started = System.Diagnostics.Stopwatch.StartNew();
		while ( !asset.IsCompiledAndUpToDate && started.Elapsed < timeout )
			await Task.Delay( 50, cancel );
	}

	static int ResourceOrder( string file ) => Path.GetExtension( file ).ToLowerInvariant() switch
	{
		".vtex" => 1,
		".vmat" or ".tmat" => 2,
		".vmdl" => 3,
		_ => 0
	};
}
