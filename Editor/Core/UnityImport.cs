using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;

namespace ImportUnityPackage;

public record ImportOptions( bool Materials, bool Models, bool Vmat, bool Tmat, bool Vmdl )
{
	public bool Automatic { get; init; }
	/// <summary>Convert Unity scenes (.unity) to s&amp;box scenes after their prefabs, models and materials.</summary>
	public bool Scenes { get; init; }
	public static ImportOptions Auto( bool additionalTerrainMaterials = false, bool scenes = false ) => new( true, true, true, additionalTerrainMaterials, true ) { Automatic = true, Scenes = scenes };
}
public record ImportIssue( string Asset, string Code, string Message )
{
	// Compatibility fallbacks are warnings; failing to produce a selected resource is an error.
	public string Severity => Code == "conversion-skipped" ? "Error" : "Warning";
}
public record ImportResult( string Directory, int AssetCount, int ConvertedCount, string[] Files, string[] Warnings )
{
	public ImportIssue[] Unresolved { get; init; } = Array.Empty<ImportIssue>();
	public string ReportJson { get; init; }
	public void ExportReport( string path ) => File.WriteAllText( path, ReportJson );
	public string[] ChangedFiles { get; init; } = Array.Empty<string>();
	public string[] Information { get; init; } = Array.Empty<string>();
}
public record TextureChannelRequest( string Source, string Destination, int Channel, double Scale, bool Invert );
public delegate void ExtractTextureChannels( IReadOnlyList<TextureChannelRequest> requests, CancellationToken cancel );
public delegate byte[] ExtractTextureChannel( string source, int channel, double scale, bool invert, CancellationToken cancel );

public static class UnityImport
{
	public static bool NeedsTextureResource( string path ) => Path.GetExtension( path ).ToLowerInvariant() is ".exr" or ".psd" or ".tif" or ".tiff";
	public static bool IsEnabled( UnityAsset asset, ImportOptions options ) => asset.Kind switch
	{
		UnityAssetKind.Material or UnityAssetKind.Texture => options.Materials,
		UnityAssetKind.Model or UnityAssetKind.ModelSupport or UnityAssetKind.Prefab => options.Models,
		UnityAssetKind.Scene => options.Models && options.Scenes,
		_ => false
	};

	public static HashSet<UnityAsset> Selection( UnityArchive archive, ImportOptions options ) =>
		ImportCatalog.Read( archive ).CreatePlan( options ).Assets.Select( a => a.Asset ).ToHashSet();

	internal static string ReadText( string path, long limit = 16 * 1024 * 1024 )
	{
		if ( new FileInfo( path ).Length > limit ) throw new InvalidDataException( $"{(limit > 16 * 1024 * 1024 ? "Scene" : "Material or metadata")} exceeds the {limit / (1024 * 1024)} MiB text limit." );
		return File.ReadAllText( path );
	}

	/// <summary>
	/// Generated resource names are lowercase, like the names s&box tooling writes, so a reimport
	/// cannot produce two spellings of the same asset. The directory keeps the Unity folder's own
	/// case so a generated file lands beside its sources on case-sensitive file systems.
	/// </summary>
	internal static string GeneratedPath( string path )
	{
		var separator = path.LastIndexOf( '/' );
		return separator < 0 ? path.ToLowerInvariant() : path[..(separator + 1)] + path[(separator + 1)..].ToLowerInvariant();
	}

	/// <summary>
	/// s&box resolves asset references case-insensitively and its tools write them lowercase,
	/// so every path embedded inside a generated document is lowercase.
	/// </summary>
	internal static string DocumentPath( string path ) => path.ToLowerInvariant();

	/// <summary>
	/// Names each converted material after its albedo texture, which is how s&box's own
	/// "Create Material" names one, and keeps materials sharing a texture from colliding.
	/// Falls back to the Unity material's own name when there is no unambiguous albedo.
	/// </summary>
	static Dictionary<string, string> MaterialOutputPaths( ImportPlan plan, Dictionary<string, UnityAsset> textures )
	{
		var result = new Dictionary<string, string>( StringComparer.OrdinalIgnoreCase );
		var taken = new HashSet<string>( StringComparer.OrdinalIgnoreCase );
		var materials = plan.Assets.Where( a => a.Vmat || a.Tmat ).Select( a => a.Asset )
			.OrderBy( a => a.Path, StringComparer.OrdinalIgnoreCase ).ToArray();
		foreach ( var material in materials ) taken.Add( GeneratedPath( Path.ChangeExtension( material.Path, null ) ) );
		foreach ( var material in materials )
		{
			var fallback = GeneratedPath( Path.ChangeExtension( material.Path, null ) );
			result[material.Guid] = fallback;
			string albedo = null;
			try
			{
				var parsed = UnityMaterial.Parse( ReadText( material.Source ) );
				if ( parsed.ColorTextureGuid is string guid && textures.TryGetValue( guid, out var texture ) )
					albedo = Path.GetFileNameWithoutExtension( texture.Path );
			}
			catch ( InvalidDataException ) { continue; }
			if ( albedo == null ) continue;
			var directory = Path.GetDirectoryName( material.Path )?.Replace( '\\', '/' );
			var candidate = GeneratedPath( string.IsNullOrEmpty( directory ) ? albedo : directory + "/" + albedo );
			// Several Unity materials can share one albedo; only the first may claim the texture's name.
			if ( candidate != fallback && !taken.Add( candidate ) ) continue;
			result[material.Guid] = candidate;
			taken.Remove( fallback );
		}
		return result;
	}

	public static ImportResult Run( UnityArchive archive, string assetsDirectory, ImportOptions options,
		IProgress<ImportProgress> progress, CancellationToken cancel, ExtractTextureChannel extractChannel = null, ExtractTextureChannels extractChannels = null )
		=> Run( ImportCatalog.Read( archive, cancel ).CreatePlan( options ), assetsDirectory, progress, cancel, extractChannel, extractChannels );

	public static ImportResult Run( ImportPlan plan, string assetsDirectory,
		IProgress<ImportProgress> progress, CancellationToken cancel, ExtractTextureChannel extractChannel = null, ExtractTextureChannels extractChannels = null )
	{
		using var merge = ImportMergePlan.Create( plan, assetsDirectory, cancel );
		merge.Prepare( progress, cancel, extractChannel, extractChannels );
		var result = merge.Commit( progress, cancel );
		merge.Dispose();
		return result with { Warnings = result.Warnings.Concat( merge.CleanupWarnings ).Distinct().ToArray() };
	}

	internal static ImportResult PrepareFiles( ImportPlan plan, string stageDirectory, string destination,
		IProgress<ImportProgress> progress, CancellationToken cancel, ExtractTextureChannel extractChannel, ExtractTextureChannels extractChannels = null )
	{
		cancel.ThrowIfCancellationRequested();
		var archive = plan.Archive;
		var options = plan.Options;
		var selected = plan.Assets.Select( a => a.Asset ).ToArray();
		if ( selected.Length == 0 ) throw new InvalidOperationException( "Select at least one supported asset." );
		var stage = Path.GetFullPath( stageDirectory );
		CheckDirectory( stage );
		const string prefix = "Imported/";
		var warnings = new List<string>( archive.Warnings );
		var information = new List<string>();
		var unresolved = new List<ImportIssue>( plan.Issues );
		warnings.AddRange( plan.Issues.Select( i => $"{i.Asset}: {i.Message}" ) );
		void Issue( string asset, string code, string message )
		{
			unresolved.Add( new( asset, code, message ) );
			warnings.Add( $"{asset}: {message}" );
		}
		var files = new List<string>();
		var outputs = new HashSet<string>( StringComparer.OrdinalIgnoreCase );
		var textures = selected.Where( a => a.Kind == UnityAssetKind.Texture ).ToDictionary( a => a.Guid, a => DocumentPath( prefix + a.Path ), StringComparer.OrdinalIgnoreCase );
		var textureSources = selected.Where( a => a.Kind == UnityAssetKind.Texture ).ToDictionary( a => a.Guid, StringComparer.OrdinalIgnoreCase );
		var generatedFiles = new GeneratedAssetFiles( plan, stage, destination );
		var documents = new List<(string File, string Text)>();
		var generatedChannels = new Dictionary<string, string>();
		var channelRequests = new List<TextureChannelRequest>();
		var channelTimer = new System.Diagnostics.Stopwatch();
		var shaders = archive.Assets.Where( a => a.Path.EndsWith( ".shader", StringComparison.OrdinalIgnoreCase ) ).ToDictionary( a => a.Guid, StringComparer.OrdinalIgnoreCase );
		var shaderText = new Dictionary<string, string>( StringComparer.OrdinalIgnoreCase );
		var convertedMaterials = new Dictionary<string, UnityMaterial>( StringComparer.OrdinalIgnoreCase );
		var materialPaths = MaterialOutputPaths( plan, textureSources );
		var generatedModels = new Dictionary<(string Guid, string Mesh), string>();
		// Each generated model's output path and ModelDoc text, for the mirrored variants instances with a negative scale need.
		var modelOutputs = new Dictionary<string, (string Relative, string Text)>( StringComparer.OrdinalIgnoreCase );
		var conversions = 0;
		// HDRP is detected, never converted: one line for the whole import says what a project is,
		// because an HDRP scene has hundreds of these components and a material line each would drown it.
		var hdrpMaterials = 0;
		var hdrpComponents = new HdrpCounts( 0, 0, 0, 0 );
		// HDRP Lit features the complex shader cannot express. They are grouped into one line at the end
		// of the import rather than reported per material, because a real project has dozens.
		var transmissionMaterials = new HashSet<string>( StringComparer.OrdinalIgnoreCase );
		var subsurfaceMaterials = new HashSet<string>( StringComparer.OrdinalIgnoreCase );
		var operation = "creating the staging folder";
		string currentAsset = null;
		Exception failure = null;
		var preserveStage = false;
		try
		{
			Directory.CreateDirectory( stage );
			string Output( string relative )
			{
				if ( !outputs.Add( relative ) ) throw new InvalidDataException( $"Two assets generate the same output: {relative}" );
				var full = Path.GetFullPath( Path.Combine( stage, relative + ".iup" ) );
				if ( !full.StartsWith( stage + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase ) ) throw new InvalidDataException( "Invalid output path." );
				Directory.CreateDirectory( Path.GetDirectoryName( full ) );
				files.Add( relative );
				return full;
			}
			for ( var i = 0; i < selected.Length; i++ )
			{
				cancel.ThrowIfCancellationRequested();
				var asset = selected[i];
				operation = "writing imported files";
				currentAsset = asset.Path;
				var planned = plan.Find( asset );
				if ( asset.Kind == UnityAssetKind.Model && asset.ModelInfo.CollisionOnly )
					information.Add( $"{asset.Path}: used only by Unity MeshColliders; kept as a collision source without a visible VMDL. Physics setup is not recreated." );
				progress?.Report( new( 0.8 * i / selected.Length, $"Preparing {asset.Path}", "Preparing assets" ) );
				// A Unity prefab is YAML under the name s&box uses for its own JSON prefabs, so only the
				// converted prefab is written; copying the source would leave an unloadable .prefab.
				if ( asset.Kind == UnityAssetKind.Prefab ) continue;
				var copied = Output( asset.Path );
				if ( asset.Path.EndsWith( ".fbx", StringComparison.OrdinalIgnoreCase )
					&& UnityFbxRewrite.Sanitize( File.ReadAllBytes( asset.Source ) ) is byte[] portableFbx )
				{
					// An FBX keeps the original author's absolute texture paths. s&box derives a
					// material reference from each one and cannot make an absolute path content-relative,
					// so reduce them to file names before the resource compiler sees the imported model.
					File.WriteAllBytes( copied, portableFbx );
					information.Add( $"{asset.Path}: absolute texture paths in the FBX were reduced to file names so the resource compiler can resolve its material references." );
				}
				else
				{
					using var source = File.OpenRead( asset.Source );
					using var target = new FileStream( copied, FileMode.CreateNew );
					UnityArchive.Copy( source, target, cancel );
				}
				if ( asset.Kind == UnityAssetKind.Texture && NeedsTextureResource( asset.Path ) )
				{
					// Keep source pixels/HDR data intact and give the editor a loadable texture resource.
					File.WriteAllText( Output( asset.Path + ".vtex" ), JsonSerializer.Serialize( new
					{
						Images = new[] { DocumentPath( prefix + asset.Path ) }, InputColorSpace = "Linear", OutputColorSpace = "Linear",
						OutputFormat = asset.Path.EndsWith( ".exr", StringComparison.OrdinalIgnoreCase ) ? "RGBA16161616F" : "BC7",
						OutputMipAlgorithm = "Box", OutputTypeString = "2D"
					}, new JsonSerializerOptions { WriteIndented = true } ) );
					conversions++;
				}
				if ( planned.Vmat || planned.Tmat )
				{
					try
					{
						var material = UnityMaterial.Parse( ReadText( asset.Source ) );
						if ( material.ShaderGuid != null && shaders.TryGetValue( material.ShaderGuid, out var shader ) )
						{
							if ( !shaderText.TryGetValue( shader.Guid, out var source ) ) shaderText[shader.Guid] = source = ReadText( shader.Source );
							material.ConfigureShader( source );
						}
						if ( material.Pipeline == UnityPipeline.Hdrp ) hdrpMaterials++;
						if ( material.Transmission ) transmissionMaterials.Add( asset.Path );
						if ( material.Subsurface ) subsurfaceMaterials.Add( asset.Path );
						if ( extractChannel != null || extractChannels != null )
						{
							material.PrepareChannels( (guid, channel, scale, invert) =>
							{
								if ( !textureSources.TryGetValue( guid, out var source ) )
								{
									Issue( asset.Path, "missing-texture", $"Missing or unsupported texture {guid}: no Texture asset with that GUID is packed in the source package (source-data gap)." );
									return null;
								}
								var key = $"{guid}_{channel}_{scale.ToString( System.Globalization.CultureInfo.InvariantCulture )}_{invert}";
								if ( !generatedChannels.TryGetValue( key, out var generated ) )
								{
									var purpose = channel switch { 0 => "metallic", 1 => "ao", 2 => "height", _ => invert ? "roughness" : "opacity" };
									var derived = generatedFiles.Add( source.Path, purpose, ".png" );
									generated = derived.Reference;
									var output = derived.File;
									if ( extractChannels != null ) channelRequests.Add( new( source.Source, output, channel, scale, invert ) );
									else File.WriteAllBytes( output, extractChannel( source.Source, channel, scale, invert, cancel ) );
									generatedChannels[key] = generated;
								}
								return generated;
							} );
						}
						string Resolve( string guid )
						{
							if ( textures.TryGetValue( guid, out var texture ) ) return texture;
							Issue( asset.Path, "missing-texture", $"Missing or unsupported texture {guid}: no Texture asset with that GUID is packed in the source package (source-data gap)." );
							return null;
						}
						if ( planned.Vmat ) { documents.Add( (Output( materialPaths[asset.Guid] + ".vmat" ), material.ToVmat( Resolve )) ); conversions++; }
						convertedMaterials[asset.Guid] = material;
						if ( planned.Tmat ) { documents.Add( (Output( materialPaths[asset.Guid] + ".tmat" ), material.ToTmat( Resolve )) ); conversions++; }
						warnings.AddRange( material.Warnings.Select( w => $"{asset.Path}: {w}" ) );
						information.AddRange( material.Information.Select( m => $"{asset.Path}: {m}" ) );
					}
					catch ( InvalidDataException ex ) { Issue( asset.Path, "conversion-skipped", $"Conversion skipped. {ex.Message}" ); }
				}
				if ( planned.Vmdl )
				{
					var meshPath = DocumentPath( prefix + asset.Path );
					if ( asset.Path.EndsWith( ".fbx", StringComparison.OrdinalIgnoreCase ) && UnityFbxCompatibility.Normalize( asset.Source, cancel ) is byte[] normalized )
					{
						var repaired = generatedFiles.Add( asset.Path, "repaired", ".fbx" );
						File.WriteAllBytes( repaired.File, UnityFbxRewrite.Sanitize( normalized ) ?? normalized );
						meshPath = repaired.Reference;
						warnings.Add( $"{asset.Path}: removed an exact duplicate vertex array after a closing brace in a derived FBX; original source preserved." );
					}
					var info = asset.ModelInfo;
					// ModelDoc names render meshes after FBX geometry objects, which Maya leaves unnamed and ModelDoc
					// then merges into one "unnamed" mesh; a bodygroup can only switch meshes that carry their names.
					if ( info.Skinned && asset.Path.EndsWith( ".fbx", StringComparison.OrdinalIgnoreCase ) )
					{
						try
						{
							if ( UnityFbxRewrite.Rewrite( asset.Source, out var renamed, sanitize: true ) is byte[] rewritten )
							{
								var skinnedCopy = generatedFiles.Add( asset.Path, "skinned", ".fbx" );
								File.WriteAllBytes( skinnedCopy.File, rewritten );
								meshPath = skinnedCopy.Reference;
								information.Add( $"{asset.Path}: the model imports a copy of the FBX with {string.Join( " and ", renamed )}; original source preserved." );
							}
						}
						catch ( InvalidDataException ex ) { warnings.Add( $"{asset.Path}: could not rewrite the FBX for ModelDoc ({ex.Message}); its meshes may import unnamed, which bodygroups cannot switch." ); }
					}
					var scale = asset.Path.EndsWith( ".fbx", StringComparison.OrdinalIgnoreCase ) ? info.ImportScale( asset.Metadata == null ? null : ReadText( asset.Metadata ) ) : 1;
					// ModelDoc keys remaps on the FBX's own material slot names, not on material filenames.
					string MaterialFor( UnityAsset material ) => materialPaths.TryGetValue( material.Guid, out var path ) && outputs.Contains( path + ".vmat" ) ? DocumentPath( prefix + path + ".vmat" ) : null;
					var remaps = new Dictionary<string, string>( StringComparer.OrdinalIgnoreCase );
					// Exporters commonly name an FBX slot after the Unity material it was authored against.
					foreach ( var group in selected.Where( a => a.Kind == UnityAssetKind.Material && MaterialFor( a ) != null )
						.GroupBy( a => Path.GetFileNameWithoutExtension( a.Path ), StringComparer.OrdinalIgnoreCase ).Where( g => g.Count() == 1 ) )
						remaps[group.Key.ToLowerInvariant() + ".vmat"] = MaterialFor( group.Single() );
					var prefabMaterials = selected.Where( a => info.PrefabMaterials.Contains( a.Guid, StringComparer.OrdinalIgnoreCase ) && MaterialFor( a ) != null ).ToArray();
					foreach ( var slot in info.Materials )
					{
						var semantic = prefabMaterials.Where( a => UnityModel.Normalize( Path.GetFileNameWithoutExtension( a.Path ) ) == UnityModel.Normalize( slot ) ).ToArray();
						if ( semantic.Length == 1 ) remaps[slot.ToLowerInvariant() + ".vmat"] = MaterialFor( semantic[0] );
					}
					foreach ( var (slot, albedoFile) in info.MaterialAlbedoFiles )
					{
						if ( remaps.ContainsKey( slot.ToLowerInvariant() + ".vmat" ) ) continue;
						var candidates = (prefabMaterials.Length > 0 ? prefabMaterials : selected.Where( a => a.Kind == UnityAssetKind.Material ).ToArray())
							.Where( a => convertedMaterials.TryGetValue( a.Guid, out var material ) && material.ColorTextureGuids.Any( guid =>
								textureSources.TryGetValue( guid, out var texture ) && Path.GetFileNameWithoutExtension( texture.Path ).Equals(
									Path.GetFileNameWithoutExtension( albedoFile ), StringComparison.OrdinalIgnoreCase ) ) ).ToArray();
						// Unity often keeps the old diffuse filename as the material name
						// after repacking its texture. Require a unique prefab-assigned material.
						if ( candidates.Length == 0 ) candidates = prefabMaterials.Where( a =>
							Path.GetFileNameWithoutExtension( a.Path ).Equals( Path.GetFileNameWithoutExtension( albedoFile ), StringComparison.OrdinalIgnoreCase ) ).ToArray();
						if ( candidates.Length == 1 && MaterialFor( candidates[0] ) is string albedoMaterial ) remaps[slot.ToLowerInvariant() + ".vmat"] = albedoMaterial;
					}
					foreach ( var (slot, guid) in info.PrefabSlotMaterials )
					{
						var assigned = selected.FirstOrDefault( a => a.Guid.Equals( guid, StringComparison.OrdinalIgnoreCase ) && MaterialFor( a ) != null );
						if ( assigned != null ) remaps[slot.ToLowerInvariant() + ".vmat"] = MaterialFor( assigned );
					}
					foreach ( var slot in info.UnresolvedPrefabSlots ) remaps.Remove( slot.ToLowerInvariant() + ".vmat" );
					// Some exporters append an LOD suffix to slots that share a resolved base material.
					foreach ( var (slot, material) in remaps.ToArray() )
						for ( var lod = 0; lod <= 8; lod++ ) remaps.TryAdd( $"{Path.GetFileNameWithoutExtension( slot )}_lod{lod}.vmat", material );
					// Only slots this FBX actually declares mean anything to ModelDoc; name-matched
					// guesses for every other material in the package would just be dead entries.
					var declaredSlots = info.Materials.Concat( info.RenderedMaterials )
						.Select( s => s.ToLowerInvariant() + ".vmat" ).ToHashSet( StringComparer.OrdinalIgnoreCase );
					foreach ( var slot in remaps.Keys.Where( s => !declaredSlots.Contains( s ) ).ToArray() ) remaps.Remove( slot );
					var defaultMaterial = prefabMaterials.Length == 1 && info.PrefabSlotMaterials.Count == 0 && info.AssignmentWarnings.Count == 0 ? MaterialFor( prefabMaterials[0] ) : null;
					var missingSlots = info.RenderedMaterials.Where( s => !remaps.ContainsKey( s.ToLowerInvariant() + ".vmat" ) ).ToArray();
					// Exporter "N - Default" slots carry no author material. They are not a missing conversion:
					// the same neutral material used for unnamed FBX slots is their honest target.
					var defaultSlots = info.RenderedMaterials.Where( UnityModel.IsDefaultSlot ).ToArray();
					if ( options.Materials && options.Vmat && defaultMaterial == null &&
						(info.Materials.Count == 0 || defaultSlots.Length > 0) &&
						(info.RendererSlots.Count == 0 || missingSlots.Length > 0) && asset.Path.EndsWith( ".fbx", StringComparison.OrdinalIgnoreCase ) )
					{
						var neutralPath = generatedFiles.Add( asset.Path, "unassigned", ".vmat" );
						var neutral = new UnityMaterial();
						neutral.Colors["_Color"] = new[] { 0.5, 0.5, 0.5, 1.0 };
						File.WriteAllText( neutralPath.File, neutral.ToVmat( _ => null ) );
						conversions++;
						if ( info.RendererSlots.Count == 0 ) defaultMaterial = neutralPath.Reference;
						else foreach ( var slot in missingSlots.Where( s => info.Materials.Count == 0 || defaultSlots.Contains( s, StringComparer.OrdinalIgnoreCase ) ) )
							remaps[slot.ToLowerInvariant() + ".vmat"] = neutralPath.Reference;
						var reason = info.Materials.Count == 0 ? "FBX contains no named material slots"
							: $"FBX default slot(s) {string.Join( ", ", defaultSlots )} carry no author material";
						Issue( asset.Path, "unassigned-material", $"{reason} and no unambiguous prefab assignment was resolved; using a neutral material." );
					}
					// Meshes sharing one FBX slot but assigned different materials cannot share a remap
					// table, so each gets its own model naming only that mesh.
					var meshGroups = ModelSplit( info, materialPaths, selected, MaterialFor );
					if ( meshGroups.Count > 1 )
						information.Add( $"{asset.Path}: meshes use different materials through the same FBX material slot; one model was written per mesh." );
					if ( options.Materials && options.Vmat && defaultMaterial == null && meshGroups.Count <= 1 )
						foreach ( var slot in info.RenderedMaterials.Where( s => !remaps.ContainsKey( s.ToLowerInvariant() + ".vmat" ) ) )
							Issue( asset.Path, "unassigned-material", $"No converted material was resolved for slot '{slot}'." );
					var modelDirectory = Path.GetDirectoryName( asset.Path )?.Replace( '\\', '/' );
					// A skinned FBX's meshes are switched by bodygroups, laid out the way its prefabs use them.
					var bodyGroups = info.Skinned && meshGroups.Count == 1 ? info.BodyGroups : Array.Empty<UnityModel.BodyGroup>();
					if ( info.BodyGroupsOverflow ) Issue( asset.Path, "bodygroups-skipped", "More than 64 skinned meshes; no bodygroups were generated, so every mesh renders." );
					foreach ( var group in meshGroups )
					{
						var meshes = group.Meshes.Length > 0 ? group.Meshes : info.RenderedMeshes;
						var name = meshGroups.Count > 1 ? group.Meshes[0] : Path.GetFileNameWithoutExtension( asset.Path );
						var modelPath = GeneratedPath( (string.IsNullOrEmpty( modelDirectory ) ? "" : modelDirectory + "/") + name + ".vmdl" );
						var collision = meshes.Where( info.CollisionMeshes.Contains ).ToArray();
						var modelText = ModelDocument( meshPath, group.Remaps ?? remaps, group.Remaps != null ? null : defaultMaterial,
							meshes, collision, scale, collision.Length > 0 && collision.All( info.ConvexCollisionMeshes.Contains ), bodyGroups );
						documents.Add( (Output( modelPath ), modelText) );
						modelOutputs[DocumentPath( prefix + modelPath )] = (modelPath, modelText);
						foreach ( var mesh in meshes ) generatedModels[(asset.Guid, mesh)] = DocumentPath( prefix + modelPath );
						conversions++;
					}
					if ( info.Skinned )
					{
						var layout = bodyGroups.Count == 0 ? "" : bodyGroups.Count == 1 && bodyGroups[0].Name == info.Name
							? $" Bodygroup '{bodyGroups[0].Name}' shows one of its {bodyGroups[0].Choices.Length - 1} skinned meshes or none, as the Unity prefabs do."
							: $" Each of its {bodyGroups.Count} skinned meshes has an on/off bodygroup.";
						information.Add( $"{asset.Path}: skinned model; the FBX skeleton ({info.BoneCount} bones) and skin weights import with the meshes.{layout} Check bone orientation (flip_bone_forward) and size in ModelDoc." );
						if ( info.HasAnimation ) information.Add( $"{asset.Path}: the FBX's animation is not converted; add it to the model in ModelDoc." );
						if ( info.HasBlendShapes ) information.Add( $"{asset.Path}: the FBX has blend shapes; check their morphs in ModelDoc." );
					}
					information.Add( $"{asset.Path}: review scale, orientation, material assignments, collision and animations in ModelDoc." );
				}
			}
			// Prefabs go last: they reference the models generated above, and nested sources come first.
			var prefabPaths = plan.Assets.Where( a => a.Prefab ).ToDictionary( a => a.Asset.Guid, a => GeneratedPath( a.Asset.Path ), StringComparer.OrdinalIgnoreCase );
			var byGuid = archive.Assets.ToDictionary( a => a.Guid, StringComparer.OrdinalIgnoreCase );
			var convertedPrefabs = new Dictionary<string, string>( StringComparer.OrdinalIgnoreCase );
			// s&box draws a negatively scaled object inside out, so a mirrored placement uses a positive scale on a
			// variant of the prefab whose models ModelDoc mirrors (its ScaleAndMirror modifier flips the winding too).
			// Variants are made on demand, named after the Unity axis they mirror along.
			var mirroredModels = new Dictionary<string, string>( StringComparer.OrdinalIgnoreCase );
			var mirroredPrefabs = new Dictionary<string, (string Path, string Json)?>( StringComparer.OrdinalIgnoreCase );
			// Generated prefabs of models a scene places directly, by model guid (null: not convertible).
			var modelPrefabs = new Dictionary<string, (string Path, string Json)?>( StringComparer.OrdinalIgnoreCase );
			UnityPrefabExport.Context context = null;
			string MirroredModelFor( UnityAsset model, string mesh, int axis )
			{
				if ( generatedModels.GetValueOrDefault( (model.Guid, mesh) ) is not string document || !modelOutputs.TryGetValue( document, out var output ) ) return null;
				var cacheKey = $"{document}|{axis}";
				if ( mirroredModels.TryGetValue( cacheKey, out var known ) ) return known;
				// Unity x, y, z are s&box -y, z, x (see UnityPrefabExport.Position); the mirror plane follows the axis.
				var sboxAxis = "yzx"[axis];
				var relative = output.Relative[..^".vmdl".Length] + $"_mirror_{"xyz"[axis]}.vmdl";
				documents.Add( (Output( relative ), output.Text.Replace( $"mirror_{sboxAxis} = false", $"mirror_{sboxAxis} = true" )) );
				conversions++;
				return mirroredModels[cacheKey] = DocumentPath( prefix + relative );
			}
			(string Path, string Json)? MirroredPrefabFor( string guid, int axis )
			{
				var cacheKey = $"{guid}|{axis}";
				if ( mirroredPrefabs.TryGetValue( cacheKey, out var known ) ) return known;
				mirroredPrefabs[cacheKey] = null; // a prefab nested in itself stops here
				if ( !prefabPaths.TryGetValue( guid, out var basePath ) || !byGuid.TryGetValue( guid, out var asset ) ) return null;
				var relative = basePath[..^".prefab".Length] + $"_mirror_{"xyz"[axis]}.prefab";
				try
				{
					var variant = UnityPrefabExport.Convert( asset, context, axis );
					documents.Add( (Output( relative ), variant.Json) );
					conversions++;
					warnings.AddRange( variant.Warnings.Select( w => $"{asset.Path} (mirrored {"xyz"[axis]}): {w}" ) );
					information.AddRange( variant.Information.Select( m => $"{asset.Path} (mirrored {"xyz"[axis]}): {m}" ) );
					return mirroredPrefabs[cacheKey] = (DocumentPath( prefix + relative ), variant.Json);
				}
				catch ( InvalidDataException ex )
				{
					Issue( asset.Path, "conversion-skipped", $"Mirrored prefab variant skipped. {ex.Message}" );
					return null;
				}
			}
			(string Path, string Json)? ModelPrefabFor( UnityAsset model )
			{
				if ( modelPrefabs.TryGetValue( model.Guid, out var known ) ) return known;
				modelPrefabs[model.Guid] = null; // a model prefab that instances itself stops here
				try
				{
					// Unity keeps an implicit prefab for every model asset; the wrapper builder writes it, so a
					// scene's direct instance resolves through the same path a converted prefab does. The name
					// keeps the FBX's own extension, which a Unity prefab of the same stem cannot claim.
					var converted = UnityPrefabExport.ConvertModel( model, context );
					var relative = GeneratedPath( model.Path ) + ".prefab";
					documents.Add( (Output( relative ), converted.Json) );
					conversions++;
					warnings.AddRange( converted.Warnings.Select( w => $"{model.Path}: {w}" ) );
					information.AddRange( converted.Information.Select( m => $"{model.Path}: {m}" ) );
					return modelPrefabs[model.Guid] = (DocumentPath( prefix + relative ), converted.Json);
				}
				catch ( InvalidDataException ex )
				{
					Issue( model.Path, "conversion-skipped", $"Model prefab conversion skipped. {ex.Message}" );
					return null;
				}
			}
			// Unity's built-in primitives have no package asset to convert. Each shape Unity ships gets one
			// generated model in this run's output, in s&box units, so every object drawing it can point at it;
			// a quad, a capsule and a cylinder have no ModelDoc render primitive and carry a generated mesh too.
			var primitiveModels = new Dictionary<string, string>( StringComparer.OrdinalIgnoreCase );
			string PrimitiveModelFor( string shapeName )
			{
				if ( primitiveModels.TryGetValue( shapeName, out var known ) ) return known;
				if ( UnityPrimitives.Find( shapeName ) is not UnityPrimitives.Shape shape ) return null;
				string mesh = null;
				if ( UnityPrimitives.NeedsMesh( shape ) )
				{
					var meshPath = GeneratedPath( "_iup_primitives/" + shape.Name + ".obj" );
					documents.Add( (Output( meshPath ), UnityPrimitives.Mesh( shape, UnityPrefabExport.UnitsPerMetre )) );
					mesh = DocumentPath( prefix + meshPath );
					conversions++;
				}
				var relative = GeneratedPath( "_iup_primitives/" + shape.Name + ".vmdl" );
				documents.Add( (Output( relative ), UnityPrimitives.Model( shape, mesh, UnityPrefabExport.UnitsPerMetre )) );
				conversions++;
				return primitiveModels[shapeName] = DocumentPath( prefix + relative );
			}

			context = new UnityPrefabExport.Context( byGuid, ( model, mesh ) => generatedModels.GetValueOrDefault( (model.Guid, mesh) ),
				guid => prefabPaths.TryGetValue( guid, out var path ) ? DocumentPath( prefix + path ) : null, convertedPrefabs,
				guid => materialPaths.TryGetValue( guid, out var path ) && outputs.Contains( path + ".vmat" ) ? DocumentPath( prefix + path + ".vmat" ) : null )
			{ MirroredModelFor = MirroredModelFor, MirroredPrefabFor = MirroredPrefabFor, ModelPrefabFor = ModelPrefabFor, PrimitiveModelFor = PrimitiveModelFor };
			foreach ( var asset in UnityPrefabExport.Order( plan.Assets.Where( a => a.Prefab ).Select( a => a.Asset ) ) )
			{
				cancel.ThrowIfCancellationRequested();
				currentAsset = asset.Path;
				try
				{
					var prefab = UnityPrefabExport.Convert( asset, context );
					convertedPrefabs[asset.Guid] = prefab.Json;
					documents.Add( (Output( prefabPaths[asset.Guid] ), prefab.Json) );
					conversions++;
					warnings.AddRange( prefab.Warnings.Select( w => $"{asset.Path}: {w}" ) );
					information.AddRange( prefab.Information.Select( m => $"{asset.Path}: {m}" ) );
				}
				catch ( InvalidDataException ex ) { Issue( asset.Path, "conversion-skipped", $"Prefab conversion skipped. {ex.Message}" ); }
			}
			// Scenes go after the prefabs: they place the prefabs converted above and the models generated before those.
			var sceneConversions = new List<object>();
			foreach ( var asset in plan.Assets.Where( a => a.Scene ).Select( a => a.Asset ).OrderBy( a => a.Path, StringComparer.OrdinalIgnoreCase ) )
			{
				cancel.ThrowIfCancellationRequested();
				currentAsset = asset.Path;
				try
				{
					var scene = UnitySceneExport.Convert( asset, context );
					documents.Add( (Output( GeneratedPath( Path.ChangeExtension( asset.Path, ".scene" ) ) ), scene.Json) );
					conversions++;
					var s = scene.Statistics;
					sceneConversions.Add( new
					{
						Source = asset.Path, s.Roots, s.Instances, s.Nested, s.AddedComponents, s.RemovedObjects, s.Skipped,
						s.TotalInstances, s.LostInstances,
						// Cause to count, each with up to SkipCollector.MaxSampleNames of the guids that hit it.
						SkipCauses = s.Skips.Causes.ToDictionary( c => c.Cause, c => new { c.Count, Samples = c.Samples } )
					} );
					warnings.AddRange( scene.Warnings.Select( w => $"{asset.Path}: {w}" ) );
					information.AddRange( scene.Information.Select( m => $"{asset.Path}: {m}" ) );
					hdrpComponents += scene.Hdrp;
					// One grouped Error per scene: the losses are already counted by cause, so the report names the
					// scene, how much of it is missing and why, instead of one line per lost instance.
					if ( s.LostInstances > 0 )
						Issue( asset.Path, "conversion-skipped", $"Scene content is missing: {s.LostInstances} of {s.TotalInstances} prefab instance(s) could not be converted. {s.Skips.Summary()}." );
				}
				catch ( InvalidDataException ex ) { Issue( asset.Path, "conversion-skipped", $"Scene conversion skipped. {ex.Message}" ); }
			}
			var hdrp = hdrpComponents + new HdrpCounts( hdrpMaterials, 0, 0, 0 );
			if ( hdrp.Total > 0 ) information.Add( hdrp.Report );
			if ( UnityHdrp.UnsupportedMaterials( transmissionMaterials, subsurfaceMaterials ) is string unsupportedSurface )
				information.Add( unsupportedSurface );
			var variants = mirroredPrefabs.Values.Count( v => v != null );
			if ( variants > 0 ) information.Add( $"{variants} mirrored prefab variant(s) and {mirroredModels.Count} mirrored model(s) were generated for placements with a negative scale (s&box would draw those inside out)." );
			operation = "extracting texture channels";
			currentAsset = null;
			progress?.Report( new( 0.8, $"Preparing {channelRequests.Count} texture channels", "Converting textures" ) );
			channelTimer.Start();
			if ( channelRequests.Count > 0 ) extractChannels( channelRequests, cancel );
			channelTimer.Stop();
			progress?.Report( new( 0.95, "Choosing generated filenames…", "Naming generated files" ) );
			generatedFiles.Complete( Output, cancel );
			foreach ( var document in documents )
			{
				cancel.ThrowIfCancellationRequested();
				File.WriteAllText( document.File, generatedFiles.ResolveDocument( document.Text ) );
			}
			var unsupported = archive.Assets.Count( a => a.Kind == UnityAssetKind.Unsupported );
			if ( unsupported > 0 ) information.Add( $"{unsupported} unsupported files (such as scripts) were excluded." );
			var scenesOff = options.Scenes ? 0 : archive.Assets.Count( a => a.Kind == UnityAssetKind.Scene );
			if ( scenesOff > 0 ) information.Add( $"{scenesOff} Unity scene(s) were not converted; enable scene import under Advanced to write s&box scenes for them." );
			var issues = unresolved.Distinct().ToArray();
			var report = new
			{
				Package = Path.GetFileName( archive.FileName ), Assets = selected.Length, Converted = conversions,
				Files = files.ToArray(), Unresolved = issues,
				TextureProcessing = new { ChannelsGenerated = channelRequests.Count, SourceDecodes = channelRequests.Select( r => r.Source ).Distinct().Count(), ElapsedMilliseconds = channelTimer.ElapsedMilliseconds },
				Selection = plan.Assets.Select( a => new { Source = a.Asset.Path, a.Explicit, a.OutputLabel,
					RequiredBy = a.RequiredBy.Select( d => new { Source = d.Asset.Path, d.Reason } ).ToArray() } ).ToArray(),
				MaterialConversions = selected.Where( a => convertedMaterials.ContainsKey( a.Guid ) ).Select( a => new
				{
					Source = a.Path, convertedMaterials[a.Guid].ShaderName, convertedMaterials[a.Guid].ShaderGuid,
					Conversion = "Common material properties; shader programs are not translated",
					Vmat = plan.Find( a ).Vmat, Tmat = plan.Find( a ).Tmat
				} ).ToArray(),
				SceneConversions = sceneConversions.ToArray(),
				Information = information.ToArray(), Warnings = warnings.Distinct().ToArray()
			};
			operation = "preparing import details";
			currentAsset = null;
			var reportJson = JsonSerializer.Serialize( report, new JsonSerializerOptions { WriteIndented = true } );
			cancel.ThrowIfCancellationRequested();
			var prepared = new ImportResult( stage, selected.Length, conversions, files.Select( f => Path.Combine( stage, f + ".iup" ) ).ToArray(), warnings.Distinct().ToArray() ) { Unresolved = issues, Information = information.ToArray(), ReportJson = reportJson };
			preserveStage = true;

			return prepared;
		}
		catch ( Exception ex )
		{
			failure = ex;
			if ( ex is IOException or UnauthorizedAccessException )
			{
				failure = new IOException( $"Import failed while {operation}" + (currentAsset == null ? "" : $" for '{currentAsset}'") +
					$". Staging folder: '{stage}'. Windows error {ex.HResult & 0xffff}: {ex.Message}", ex );
				throw failure;
			}
			throw;
		}
		finally
		{
			if ( !preserveStage && Directory.Exists( stage ) )
			{
				try { ImportWorkspace.RemoveTree( stage ); }
				catch ( Exception cleanup ) when ( cleanup is IOException or UnauthorizedAccessException )
				{
					// Preserve the original conversion/cancellation error if cleanup also fails.
					if ( failure == null ) throw new IOException( $"Could not remove staging folder '{stage}'. {cleanup.Message}", cleanup );
					failure.Data["StagingCleanupError"] = $"Could not remove '{stage}': {cleanup}";
				}
			}
		}
	}

	/// <summary>
	/// The on-disk location for <paramref name="relative"/> under <paramref name="root"/>.
	/// A folder or file that already exists is reused whatever its case, so earlier imports are
	/// updated in place and never duplicated. Anything new is created lowercase: the native
	/// resource compiler asks for sources by their lowercase path, and on Linux the editor only
	/// finds a differently cased folder that existed when it started.
	/// </summary>
	internal static string DiskPath( string root, string relative )
	{
		var current = root;
		foreach ( var part in relative.Replace( '\\', '/' ).Split( '/', StringSplitOptions.RemoveEmptyEntries ) )
		{
			var lower = part.ToLowerInvariant();
			string name = null;
			if ( Directory.Exists( current ) )
			{
				if ( Exists( Path.Combine( current, lower ) ) ) name = lower;
				else if ( Exists( Path.Combine( current, part ) ) ) name = part;
				else
				{
					try { name = Directory.EnumerateFileSystemEntries( current ).Select( Path.GetFileName ).FirstOrDefault( n => n.Equals( part, StringComparison.OrdinalIgnoreCase ) ); }
					catch ( Exception ex ) when ( ex is IOException or UnauthorizedAccessException ) { }
				}
			}
			current = Path.Combine( current, name ?? lower );
		}
		return current;
		static bool Exists( string path ) => File.Exists( path ) || Directory.Exists( path );
	}

	/// <summary>The project's import folder, Assets/imported (or an existing Assets/Imported).</summary>
	internal static string ImportedDirectory( string assetsDirectory ) => DiskPath( Path.GetFullPath( assetsDirectory ), "Imported" );

	/// <summary>
	/// Refuses a path that passes through a link inside the s&amp;box project, which could send
	/// generated files somewhere outside it. Links at or above the project folder (the nearest
	/// ancestor holding a .sbproj) are where the user keeps the project and are allowed: a
	/// symlinked home or development folder, or Wine/Proton, which reports its drive mappings
	/// (Z:\, and mount points such as Z:\home) as reparse points. If no project folder can be
	/// found, every link is refused.
	/// </summary>
	internal static void CheckDirectory( string path )
	{
		var chain = new List<DirectoryInfo>();
		for ( var current = new DirectoryInfo( Path.GetFullPath( path ) ); current != null; current = current.Parent ) chain.Add( current );
		static bool IsLink( DirectoryInfo d ) => (Directory.Exists( d.FullName ) || File.Exists( d.FullName )) && File.GetAttributes( d.FullName ).HasFlag( FileAttributes.ReparsePoint );
		// Most paths have no link at all; only then is the project folder worth looking for.
		if ( !chain.Any( IsLink ) ) return;
		// The project folder is searched from the destination upwards, so it is found inside the
		// project, whose folders are readable, before any unreadable folder above it is reached.
		var project = chain.FindIndex( d => Directory.Exists( d.FullName ) && IsProjectFolder( d.FullName ) );
		var inside = project < 0 ? chain : chain.Take( project );
		if ( inside.FirstOrDefault( IsLink ) is DirectoryInfo link )
			throw new IOException( $"Import destination cannot pass through a symbolic link inside the project: {link.FullName}" );
	}

	static bool IsProjectFolder( string directory )
	{
		try { return Directory.EnumerateFiles( directory, "*.sbproj" ).Any(); }
		catch ( Exception ex ) when ( ex is IOException or UnauthorizedAccessException ) { return false; }
	}

	sealed record ModelGroup( string[] Meshes, Dictionary<string, string> Remaps );

	/// <summary>
	/// Decides whether one model can represent every mesh in an FBX. ModelDoc keys material
	/// remaps by slot name, so meshes that share a slot but were given different Unity materials
	/// need a model each; otherwise a single model imports the whole file.
	/// </summary>
	static List<ModelGroup> ModelSplit( UnityModel info, Dictionary<string, string> materialPaths, UnityAsset[] selected, Func<UnityAsset, string> materialFor )
	{
		var single = new List<ModelGroup> { new( Array.Empty<string>(), null ) };
		// A skinned FBX stays one model: its meshes share a skeleton, and bodygroups switch between them.
		if ( info.Skinned ) return single;
		var meshes = info.RenderedMeshes;
		if ( meshes.Length < 2 ) return single;
		var assignments = meshes.Select( mesh => (Mesh: mesh, Slots: info.MeshSlotMaterials.GetValueOrDefault( mesh )) ).ToArray();
		if ( assignments.Any( a => a.Slots == null || a.Slots.Count == 0 ) ) return single;
		// A slot mapped to two different materials across meshes is what a single table cannot express.
		var conflicting = assignments.SelectMany( a => a.Slots ).GroupBy( s => s.Key, s => s.Value, StringComparer.OrdinalIgnoreCase )
			.Any( g => g.Distinct( StringComparer.OrdinalIgnoreCase ).Count() > 1 );
		if ( !conflicting ) return single;
		var groups = new List<ModelGroup>();
		foreach ( var (mesh, slots) in assignments )
		{
			var remaps = new Dictionary<string, string>( StringComparer.OrdinalIgnoreCase );
			foreach ( var (slot, guid) in slots )
			{
				var material = selected.FirstOrDefault( a => a.Guid.Equals( guid, StringComparison.OrdinalIgnoreCase ) && materialFor( a ) != null );
				if ( material == null ) return single;
				remaps[slot.ToLowerInvariant() + ".vmat"] = materialFor( material );
			}
			groups.Add( new( new[] { mesh }, remaps ) );
		}
		return groups;
	}

	/// <summary>The BodyGroupList node (a sibling of RenderMeshList, three tabs in) or nothing when the model has no bodygroups.</summary>
	static string BodyGroupList( IReadOnlyList<UnityModel.BodyGroup> groups )
	{
		if ( groups.Count == 0 ) return "";
		var text = new StringBuilder( "\n\t\t\t{\n\t\t\t\t_class = \"BodyGroupList\"\n\t\t\t\tchildren = \n\t\t\t\t[\n" );
		foreach ( var group in groups )
		{
			text.Append( "\t\t\t\t\t{\n\t\t\t\t\t\t_class = \"BodyGroup\"\n\t\t\t\t\t\tname = " ).Append( UnityMaterial.Quote( group.Name ) ).Append( "\n\t\t\t\t\t\tchildren = \n\t\t\t\t\t\t[\n" );
			foreach ( var choice in group.Choices )
			{
				text.Append( "\t\t\t\t\t\t\t{\n\t\t\t\t\t\t\t\t_class = \"BodyGroupChoice\"\n\t\t\t\t\t\t\t\tname = " ).Append( UnityMaterial.Quote( choice.Name ) ).Append( '\n' );
				if ( choice.Meshes.Length == 0 ) text.Append( "\t\t\t\t\t\t\t\tmeshes = [  ]\n" );
				else
				{
					text.Append( "\t\t\t\t\t\t\t\tmeshes = \n\t\t\t\t\t\t\t\t[\n" );
					foreach ( var m in choice.Meshes ) text.Append( "\t\t\t\t\t\t\t\t\t" ).Append( UnityMaterial.Quote( m ) ).Append( ",\n" );
					text.Append( "\t\t\t\t\t\t\t\t]\n" );
				}
				text.Append( "\t\t\t\t\t\t\t},\n" );
			}
			text.Append( "\t\t\t\t\t\t]\n\t\t\t\t\t\thidden_in_tools = false\n\t\t\t\t\t},\n" );
		}
		return text.Append( "\t\t\t\t]\n\t\t\t}," ).ToString();
	}

	/// <summary>One RenderMeshFile node (five tabs in). ModelDoc names the compiled mesh after the node's name.</summary>
	static string RenderMeshFile( string name, string file, string[] filter, bool excludeByDefault, double scale )
	{
		const string t = "\t\t\t\t\t";
		var items = string.Join( "", filter.Select( m => $"\n{t}\t\t\t{UnityMaterial.Quote( m )}," ) );
		return $"{t}{{\n{t}\t_class = \"RenderMeshFile\"\n" + (name == null ? "" : $"{t}\tname = {UnityMaterial.Quote( name )}\n") +
			$"{t}\tfilename = {UnityMaterial.Quote( file )}\n{t}\timport_translation = [ 0.0, 0.0, 0.0 ]\n{t}\timport_rotation = [ 0.0, 0.0, 0.0 ]\n" +
			$"{t}\timport_scale = {scale.ToString( "0.0########", System.Globalization.CultureInfo.InvariantCulture )}\n" +
			$"{t}\talign_origin_x_type = \"None\"\n{t}\talign_origin_y_type = \"None\"\n{t}\talign_origin_z_type = \"None\"\n{t}\tparent_bone = \"\"\n" +
			$"{t}\timport_filter = \n{t}\t{{\n{t}\t\texclude_by_default = {(excludeByDefault ? "true" : "false")}\n{t}\t\texception_list = \n{t}\t\t[{items}\n{t}\t\t]\n{t}\t}}\n{t}}},";
	}

	static string ModelDocument( string mesh, Dictionary<string, string> remaps, string defaultMaterial, string[] meshes, string[] collisionMeshes, double scale, bool convex, IReadOnlyList<UnityModel.BodyGroup> bodyGroups )
	{
		const string indent = "						";
		// Unity's convex MeshCollider is one convex hull of the mesh. A non-convex one collides
		// with the exact triangles, which is s&box's mesh collision (static bodies only).
		var shape = convex ? """
									_class = "PhysicsHullFromRender"
									parent_bone = ""
									surface_prop = "default"
									collision_tags = "solid"
									faceMergeAngle = 20.0
									maxHullVertices = 32
									hull_mode = "HullPerMesh"
			""" : """
									_class = "PhysicsMeshFromRender"
									parent_bone = ""
									surface_prop = "default"
									collision_tags = "solid"
			""";
		var physics = collisionMeshes.Length == 0 ? "" : $$"""

						{
							_class = "PhysicsShapeList"
							children = 
							[
								{
			{{shape}}
								},
							]
						},
			""";
		// A bodygroup choice names RenderMeshFile nodes, so a model with bodygroups imports each mesh through its own
		// named node (the Citizen's layout); one unnamed node importing everything compiles to a single "unnamed_1" mesh.
		var renderMeshes = bodyGroups.Count == 0
			? RenderMeshFile( null, mesh, meshes, meshes.Length > 0, scale )
			: string.Join( "\n", meshes.Select( m => RenderMeshFile( m, mesh, new[] { m }, true, scale ) ) );
		var remapEntries = string.Join( "", remaps.OrderBy( r => r.Key, StringComparer.Ordinal ).Select( r =>
			$"\n{indent}	{{\n{indent}		from = {UnityMaterial.Quote( r.Key )}\n{indent}		to = {UnityMaterial.Quote( r.Value )}\n{indent}	}}," ) );
		return $$"""
		<!-- kv3 encoding:text:version{e21c7f3c-8a33-41c5-9977-a76d3a32aa0d} format:modeldoc30:version{8c2d7a91-9c42-4bf0-883a-5a3b1762d4f1} -->
		{
			rootNode = 
			{
				_class = "RootNode"
				children = 
				[{{physics}}
					{
						_class = "RenderMeshList"
						children = 
						[
		{{renderMeshes}}
						]
					},
					{
						_class = "MaterialGroupList"
						children = 
						[
							{
								_class = "DefaultMaterialGroup"
								remaps = 
								[{{remapEntries}}
								]
								use_global_default = {{(defaultMaterial != null ? "true" : "false")}}
								global_default_material = {{UnityMaterial.Quote( defaultMaterial ?? "" )}}
							},
						]
					},{{BodyGroupList( bodyGroups )}}
					{
						_class = "ModelModifierList"
						children = 
						[
							{
								_class = "ModelModifier_ScaleAndMirror"
								scale = 39.369999
								mirror_x = false
								mirror_y = false
								mirror_z = false
								flip_bone_forward = false
								swap_left_and_right_bones = false
							},
						]
					},
				]
				model_archetype = ""
				primary_associated_entity = ""
				anim_graph_name = ""
				base_model_name = ""
			}
		}
		""";
	}
}
