using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

namespace ImportUnityPackage;

public sealed record ImportDependency( UnityAsset Asset, string Reason );
public sealed record PlannedAsset( UnityAsset Asset, bool Explicit, ImportDependency[] RequiredBy, bool Vmat, bool Tmat, bool Vmdl )
{
	/// <summary>Unity prefab converted to an s&amp;box prefab referencing the generated models.</summary>
	public bool Prefab { get; init; }
	/// <summary>Unity scene converted to an s&amp;box scene placing the generated prefabs.</summary>
	public bool Scene { get; init; }
	public string OutputLabel => Asset.Kind switch
	{
		UnityAssetKind.Scene => Scene ? "Scene → SCENE" : "Scene source",
		UnityAssetKind.Model => Asset.ModelInfo.CollisionOnly ? "Collision source (FBX)" : Vmdl ? Asset.ModelInfo.Skinned ? "Skinned model → VMDL" : "Model → VMDL" : "Model source",
		UnityAssetKind.Prefab => Prefab ? "Prefab → PREFAB" : "Prefab source",
		UnityAssetKind.Material => Vmat && Tmat ? "Material → VMAT + TMAT" : Vmat ? "Material → VMAT" : Tmat ? "Terrain material → TMAT" : "Material source",
		UnityAssetKind.Texture => UnityImport.NeedsTextureResource( Asset.Path ) ? "Texture → VTEX" : "Texture",
		_ => "Model support"
	};
}

/// <summary>A selection snapshot shared by the window, importer and import report.</summary>
public sealed class ImportPlan
{
	public UnityArchive Archive { get; }
	public ImportOptions Options { get; }
	public IReadOnlyList<PlannedAsset> Assets { get; }
	public IReadOnlyList<ImportIssue> Issues { get; }
	readonly Dictionary<UnityAsset, PlannedAsset> byAsset;
	internal ImportPlan( UnityArchive archive, ImportOptions options, PlannedAsset[] assets, ImportIssue[] issues )
	{
		Archive = archive; Options = options; Assets = Array.AsReadOnly( assets ); Issues = Array.AsReadOnly( issues );
		byAsset = assets.ToDictionary( a => a.Asset );
	}
	public PlannedAsset Find( UnityAsset asset ) => byAsset.GetValueOrDefault( asset );
}

/// <summary>Read dependency evidence once, then resolve selections without disk access.</summary>
public sealed class ImportCatalog
{
	readonly UnityArchive archive;
	readonly Dictionary<UnityAsset, List<ImportDependency>> dependencies = new();
	readonly Dictionary<UnityAsset, List<ImportIssue>> issues = new();
	ImportCatalog( UnityArchive archive ) { this.archive = archive; }

	public static ImportCatalog Read( UnityArchive archive, CancellationToken cancel = default )
	{
		var catalog = new ImportCatalog( archive );
		var byGuid = archive.Assets.ToDictionary( a => a.Guid, StringComparer.OrdinalIgnoreCase );
		var materials = new Dictionary<UnityAsset, UnityMaterial>();
		void Issue( UnityAsset asset, string code, string message )
		{
			if ( !catalog.issues.TryGetValue( asset, out var list ) ) catalog.issues[asset] = list = new();
			list.Add( new( asset.Path, code, message ) );
		}
		void Link( UnityAsset parent, UnityAsset child, string reason )
		{
			if ( !catalog.dependencies.TryGetValue( parent, out var list ) ) catalog.dependencies[parent] = list = new();
			if ( !list.Any( d => d.Asset == child && d.Reason == reason ) ) list.Add( new( child, reason ) );
		}
		foreach ( var asset in archive.Assets.Where( a => a.Kind == UnityAssetKind.Material ) )
		{
			cancel.ThrowIfCancellationRequested();
			try
			{
				var material = UnityMaterial.Parse( UnityImport.ReadText( asset.Source ) );
				if ( !string.IsNullOrEmpty( material.ShaderGuid ) && byGuid.TryGetValue( material.ShaderGuid, out var shader ) && shader.Path.EndsWith( ".shader", StringComparison.OrdinalIgnoreCase ) )
					material.ConfigureShader( UnityImport.ReadText( shader.Source ) );
				materials[asset] = material;
				foreach ( var guid in material.Textures.Values.Distinct( StringComparer.OrdinalIgnoreCase ) )
				{
					if ( byGuid.TryGetValue( guid, out var texture ) && texture.Kind == UnityAssetKind.Texture ) Link( asset, texture, "Texture reference" );
					else Issue( asset, "missing-texture", $"Missing or unsupported texture {guid}." );
				}
			}
			catch ( InvalidDataException ex ) { Issue( asset, "conversion-skipped", $"Conversion skipped. {ex.Message}" ); }
		}
		var byName = archive.Assets.Where( a => a.Kind == UnityAssetKind.Material ).GroupBy( a => Path.GetFileNameWithoutExtension( a.Path ), StringComparer.OrdinalIgnoreCase )
			.ToDictionary( g => g.Key, g => g.ToArray(), StringComparer.OrdinalIgnoreCase );
		var byTexture = materials.SelectMany( m => m.Value.ColorTextureGuids.Where( byGuid.ContainsKey ).Select( g => (Name: Path.GetFileNameWithoutExtension( byGuid[g].Path ), Asset: m.Key) ) )
			.GroupBy( m => m.Name, StringComparer.OrdinalIgnoreCase ).ToDictionary( g => g.Key, g => g.Select( m => m.Asset ).Distinct().ToArray(), StringComparer.OrdinalIgnoreCase );
		foreach ( var asset in archive.Assets.Where( a => a.Kind == UnityAssetKind.Model ) )
		{
			cancel.ThrowIfCancellationRequested();
			if ( asset.ModelInfo.CollisionOnly ) continue;
			foreach ( var warning in asset.ModelInfo.AssignmentWarnings ) Issue( asset, "prefab-material-assignment", warning );
			var referencedMaterials = new HashSet<UnityAsset>();
			void Match( UnityAsset[] candidates, string reason )
			{
				var referenced = candidates.Where( referencedMaterials.Contains ).ToArray();
				if ( referenced.Length > 0 ) candidates = referenced;
				if ( candidates.Length == 1 ) Link( asset, candidates[0], reason + " (inferred)" );
				else if ( candidates.Length > 1 ) Issue( asset, "ambiguous-material", $"{reason} matches multiple materials; no dependency was inferred." );
			}
			var refs = asset.ModelInfo.PrefabMaterials.AsEnumerable();
			if ( asset.Metadata != null ) refs = refs.Concat( UnityMaterial.References( UnityImport.ReadText( asset.Metadata ) ) );
			foreach ( var guid in refs.Distinct( StringComparer.OrdinalIgnoreCase ) )
				if ( byGuid.TryGetValue( guid, out var dependency ) && dependency.Kind is UnityAssetKind.Material or UnityAssetKind.Texture )
				{
					Link( asset, dependency, "Model / prefab reference" );
					if ( dependency.Kind == UnityAssetKind.Material ) referencedMaterials.Add( dependency );
				}
			foreach ( var filename in asset.ModelInfo.MaterialAlbedoFiles.Values ) Match( byTexture.GetValueOrDefault( Path.GetFileNameWithoutExtension( filename ), Array.Empty<UnityAsset>() ), $"Color texture {filename}" );
			foreach ( var slot in asset.ModelInfo.RenderedMaterials.Where( s => !asset.ModelInfo.PrefabSlotMaterials.ContainsKey( s ) && !asset.ModelInfo.UnresolvedPrefabSlots.Contains( s ) ) ) Match( byName.GetValueOrDefault( slot, Array.Empty<UnityAsset>() ), $"Material slot {slot}" );
			Match( byName.GetValueOrDefault( Path.GetFileNameWithoutExtension( asset.Path ), Array.Empty<UnityAsset>() ), "Model filename" );
		}
		// A converted prefab needs the models it renders, the prefabs it nests and the materials its overrides assign.
		foreach ( var asset in archive.Assets.Where( a => a.Kind == UnityAssetKind.Prefab ) )
		{
			cancel.ThrowIfCancellationRequested();
			string text;
			try { text = UnityImport.ReadText( asset.Source ); }
			catch ( InvalidDataException ex ) { Issue( asset, "conversion-skipped", $"Conversion skipped. {ex.Message}" ); continue; }
			foreach ( var guid in UnityMaterial.References( text ) )
				if ( byGuid.TryGetValue( guid, out var dependency ) && dependency != asset )
				{
					if ( dependency.Kind == UnityAssetKind.Model && !dependency.ModelInfo.CollisionOnly ) Link( asset, dependency, "Prefab mesh" );
					if ( dependency.Kind == UnityAssetKind.Prefab ) Link( asset, dependency, "Nested prefab" );
					if ( dependency.Kind == UnityAssetKind.Material ) Link( asset, dependency, "Prefab material" );
				}
		}
		// A converted scene needs the prefabs it places, the meshes its own objects render and the materials its overrides assign.
		foreach ( var asset in archive.Assets.Where( a => a.Kind == UnityAssetKind.Scene ) )
		{
			cancel.ThrowIfCancellationRequested();
			string text;
			try { text = UnityImport.ReadText( asset.Source, UnitySceneExport.TextLimit ); }
			catch ( InvalidDataException ex ) { Issue( asset, "conversion-skipped", $"Conversion skipped. {ex.Message}" ); continue; }
			// A Unity skybox material cannot become an s&box sky, so it is not pulled into the import.
			var skybox = UnitySceneExport.SkyboxMaterialGuid( text );
			foreach ( var guid in UnityMaterial.References( text ) )
				if ( byGuid.TryGetValue( guid, out var dependency ) && dependency != asset )
				{
					if ( dependency.Kind == UnityAssetKind.Model && !dependency.ModelInfo.CollisionOnly ) Link( asset, dependency, "Scene mesh" );
					if ( dependency.Kind == UnityAssetKind.Prefab ) Link( asset, dependency, "Scene prefab instance" );
					if ( dependency.Kind == UnityAssetKind.Material && !guid.Equals( skybox, StringComparison.OrdinalIgnoreCase ) ) Link( asset, dependency, "Scene material override" );
				}
		}
		return catalog;
	}

	public ImportPlan CreatePlan( ImportOptions options )
	{
		var explicitAssets = archive.Assets.Where( a => a.Selected && UnityImport.IsEnabled( a, options ) ).ToHashSet();
		var included = new HashSet<UnityAsset>( explicitAssets );
		var requiredBy = new Dictionary<UnityAsset, List<ImportDependency>>();
		var queue = new Queue<UnityAsset>( included );
		while ( queue.TryDequeue( out var parent ) )
		{
			if ( !dependencies.TryGetValue( parent, out var children ) ) continue;
			foreach ( var child in children )
			{
				// Material and texture dependencies follow the Materials option; a prefab or scene always needs its meshes and prefabs.
				if ( !options.Materials && parent.Kind is not (UnityAssetKind.Prefab or UnityAssetKind.Scene) ) continue;
				if ( !requiredBy.TryGetValue( child.Asset, out var parents ) ) requiredBy[child.Asset] = parents = new();
				parents.Add( new( parent, child.Reason ) );
				if ( included.Add( child.Asset ) ) queue.Enqueue( child.Asset );
			}
		}
		var entries = included.OrderBy( a => a.Kind == UnityAssetKind.Model ? 1 : 0 ).ThenBy( a => a.Path, StringComparer.OrdinalIgnoreCase ).Select( a =>
		{
			var parents = requiredBy.GetValueOrDefault( a )?.ToArray() ?? Array.Empty<ImportDependency>();
			var terrain = a.Path.EndsWith( ".terrainlayer", StringComparison.OrdinalIgnoreCase );
			var modelUse = parents.Any( p => p.Asset.Kind == UnityAssetKind.Model );
			return new PlannedAsset( a, explicitAssets.Contains( a ), parents,
				a.Kind == UnityAssetKind.Material && options.Vmat && (!options.Automatic || !terrain || modelUse),
				a.Kind == UnityAssetKind.Material && (options.Tmat || options.Automatic && terrain),
				a.Kind == UnityAssetKind.Model && options.Vmdl && !a.ModelInfo.CollisionOnly )
			{ Prefab = a.Kind == UnityAssetKind.Prefab && options.Vmdl, Scene = a.Kind == UnityAssetKind.Scene && options.Vmdl && options.Scenes };
		} ).ToArray();
		return new( archive, options, entries, entries.Where( a => a.Vmat || a.Tmat || (a.Vmdl || a.Prefab || a.Scene) && options.Materials )
			.Where( a => issues.ContainsKey( a.Asset ) ).SelectMany( a => issues[a.Asset] ).Distinct().ToArray() );
	}
}
