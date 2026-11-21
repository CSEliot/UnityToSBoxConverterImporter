using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using static ImportUnityPackage.UnityPrefabExport;

namespace ImportUnityPackage;

/// <summary>
/// Converts a Unity scene (.unity) into an s&amp;box scene (.scene): every root object in Unity's order, prefab
/// instances as stubs of the prefabs generated in the same run (with their overrides, and the objects and
/// components Unity added to or removed from them), plain objects with their models, colliders, cameras,
/// lights and reflection probes, the scene's sky, ambient light and fog, and one post-process volume per
/// global Volume profile. Scripts, terrain, lightmaps, light probes, navigation, planar probes, decal
/// projectors and light-layer masks are listed in the report, not converted.
/// </summary>
internal static class UnitySceneExport
{
	/// <summary>A scene is read whole; city-sized scenes run to tens of megabytes of YAML.</summary>
	internal const long TextLimit = 256L * 1024 * 1024;

	/// <summary>
	/// Objects Unity parents under an object of a prefab instance (nested instances, added objects and
	/// components) are written as AddedObjects of that instance's patch, the mechanism the editor itself uses.
	/// False flattens them to the scene root with their world transform instead, which renders the same and
	/// only loses the parent link (added components cannot be flattened and are reported).
	/// </summary>
	static readonly bool NestAddedObjects = true;

	const string DefaultSky = "materials/skybox/skybox_day_01.vmat";
	/// <summary>Unity's built-in resources (Default-Skybox among them) share this guid.</summary>
	const string BuiltInGuid = "0000000000000000f000000000000000";

	/// <summary>
	/// What a scene conversion placed and what it could not resolve. <see cref="Instances"/> is the number of
	/// prefab stubs written; <see cref="TotalInstances"/> every prefab instance placement the scene refers to
	/// (the file's PrefabInstance blocks plus any stripped object naming one the file has lost), and
	/// <see cref="LostInstances"/> the ones that could not be placed, grouped by <see cref="Skips"/>' causes.
	/// </summary>
	internal sealed record Statistics( int Roots, int Instances, int Nested, int AddedComponents, int RemovedObjects, int Skipped,
		int TotalInstances, int LostInstances, SkipCollector Skips );

	internal sealed class SceneResult
	{
		public string Json;
		public Statistics Statistics;
		/// <summary>The HDRP components this scene declares, for the import's one-line summary.</summary>
		public HdrpCounts Hdrp;
		public List<string> Information;
		public List<string> Warnings;
	}

	/// <summary>The guid of the scene's skybox material, or an empty string.</summary>
	internal static string SkyboxMaterialGuid( string sceneText ) => GuidOf( Field( sceneText, "m_SkyboxMaterial" ) );

	internal static SceneResult Convert( UnityAsset scene, Context context )
	{
		var result = new Result();
		var byGuid = context.ByGuid;
		// Every prefab instance this scene cannot place, by cause: threaded through the root loop and both
		// layers of nesting, because Node and Instance otherwise discard the null where an instance is dropped.
		var skips = new SkipCollector();
		var text = UnityImport.ReadText( scene.Source, TextLimit );
		var c = new Converter( scene, text, context, result ) { ConvertsAdditions = true, ConvertsRendererMaterials = true };
		// Before Unity 2018.3 a scene serialized every object of an instance in full next to a Prefab block.
		if ( c.Instances.Values.Any( i => Scalar( i.Body, "m_IsPrefabParent" ) == "0" || Field( i.Body, "m_SourcePrefab" ).Length == 0 && Field( i.Body, "m_ParentPrefab" ).Length > 0 ) )
			throw new InvalidDataException( "The scene stores prefab instances in the Unity 2018.2 (and earlier) format, which is not supported yet." );
		var objects = new JsonArray();
		var additions = new List<(long Owner, JsonObject Entry, string Name)>();
		int roots = 0, nested = 0, addedComponents = 0, removed = 0, skipped = 0;

		// 1. Roots, in Unity's order. Instances under plain objects come out of Node through their stripped transforms.
		foreach ( var id in RootOrder( c ) )
		{
			var block = c.ById[id];
			var node = block.Type == "1001" ? c.Instance( block, null, null, skips ) : c.Node( block, false, null, null, skips );
			if ( node == null ) { skipped++; continue; }
			objects.Add( node );
			roots++;
		}

		// Where an object of a prefab instance lives: the instance, its converted source prefab and the source object.
		(Block Instance, Converter.Source Source, long Object)? Owner( Block stripped, string childName, bool quiet = false )
		{
			if ( !c.Instances.TryGetValue( IdOf( Field( stripped.Body, "m_PrefabInstance" ) ), out var instance ) )
			{
				if ( !quiet ) result.Warnings.Add( $"{childName}: belongs to a prefab instance the scene does not contain; skipped." );
				return null;
			}
			// The stub decides which variant of the source prefab the object lives in; without a stub yet, the plain one.
			var source = c.SourceOf( GuidOf( Field( instance.Body, "m_SourcePrefab" ) ), c.StubMirror.GetValueOrDefault( instance.Id, -1 ) );
			if ( source.Problem != null )
			{
				if ( !quiet ) result.Warnings.Add( $"{childName}: its prefab instance {c.InstanceName( instance )} was not converted; skipped." );
				return null;
			}
			var correspondingId = IdOf( Field( stripped.Body, "m_CorrespondingSourceObject" ) );
			if ( stripped.Type == "1" ) return (instance, source, correspondingId);
			// A stripped transform names the source prefab's transform; the object is that transform's GameObject.
			if ( source.ById.GetValueOrDefault( correspondingId ) is not { Type: "4" or "224", Stripped: false } sourceTransform )
			{
				if ( !quiet ) result.Warnings.Add( $"{childName}: parented to an object of a prefab nested inside {source.Asset.Path}; skipped." );
				return null;
			}
			return (instance, source, IdOf( Field( sourceTransform.Body, "m_GameObject" ) ));
		}

		// The patch target for object g of a source prefab (the GUID the generated prefab gave it; the engine applies the
		// patch before mapping prefab GUIDs to instance GUIDs), the Unity placement a child of g must fold in (the
		// chain g lost to its model, or, when the converted prefab dropped g, the path from the root to g) and the
		// mirror g's children are owed (see UnityPrefabExport.Unmirror).
		(Guid Parent, UnityTransform Prepend, int[] Pending) Anchor( Converter.Source source, long g, string childName )
		{
			var prefabGuid = StableGuid( $"{source.Key}:{g}" );
			if ( source.Ids.Contains( prefabGuid ) ) return (prefabGuid, source.Chains.GetValueOrDefault( g ), source.PendingOut( g ));
			result.Information.Add( $"{childName}: object {source.NameOf( g )} of {Path.GetFileName( source.Asset.Path )} is not in the converted prefab; attached to the instance root instead." );
			return (source.Ids[0], PathTo( source, g ), source.PendingIn( source.RootObject ));
		}

		// Unity world transforms, for flattening only.
		UnityTransform ParentWorld( long transformId )
		{
			if ( transformId == 0 || c.ById.GetValueOrDefault( transformId ) is not Block parent ) return NoTransform;
			if ( !parent.Stripped ) return Compose( ParentWorld( IdOf( Field( parent.Body, "m_Father" ) ) ), Local( parent.Body ) );
			if ( Owner( parent, null, quiet: true ) is not var (instance, source, g) ) return NoTransform;
			return Compose( Compose( ParentWorld( IdOf( Field( instance.Body, "m_TransformParent" ) ) ), c.InstanceRootTransform( instance, source ) ), PathTo( source, g ) );
		}

		// 2. Objects Unity hangs under an object of a prefab instance: the instance's own m_AddedGameObjects, seen from the
		// child's side. A child needs its owner's stub first (the owner may itself be nested), so owners go before children.
		void Nest( Block instance, Block transform )
		{
			var child = instance ?? transform;
			var childName = instance != null ? c.InstanceName( instance ) : NameOf( c, transform );
			var strippedId = IdOf( Field( child.Body, instance != null ? "m_TransformParent" : "m_Father" ) );
			if ( Owner( c.ById[strippedId], childName ) is not var (owner, source, g) ) { skipped++; return; }
			var parent = Guid.Empty;
			UnityTransform prepend;
			int[] pending = null;
			if ( NestAddedObjects ) (parent, prepend, pending) = Anchor( source, g, childName );
			else prepend = Compose( ParentWorld( strippedId ), NoTransform );
			var data = instance != null ? c.Instance( instance, prepend, pending, skips ) : c.Node( transform, false, prepend, pending, skips );
			if ( data == null ) { skipped++; return; }
			if ( NestAddedObjects ) additions.Add( (owner.Id, AddedObject( "GameObject", Guid.Parse( data["__guid"].GetValue<string>() ), parent, "Children", data ), childName) );
			else { objects.Add( data ); nested++; }
		}
		var waiting = new List<(Block Instance, Block Transform, long Owner)>();
		foreach ( var block in c.Blocks )
		{
			if ( block.Type == "1001" )
			{
				var parentId = IdOf( Field( block.Body, "m_TransformParent" ) );
				if ( parentId != 0 && c.ById.GetValueOrDefault( parentId ) is { Stripped: true, Type: "4" or "224" } ) waiting.Add( (block, null, c.StrippedInstance[parentId]) );
			}
			else if ( block.Type is "4" or "224" && !block.Stripped )
			{
				var father = IdOf( Field( block.Body, "m_Father" ) );
				if ( father == 0 || c.ById.GetValueOrDefault( father ) is not { Type: "4" or "224" } parent ) { if ( father != 0 ) { result.Warnings.Add( $"{NameOf( c, block )}: its parent object is missing from the scene; skipped." ); skipped++; } }
				else if ( parent.Stripped ) waiting.Add( (null, block, c.StrippedInstance[father]) );
			}
		}
		for ( var progress = true; progress && waiting.Count > 0; )
		{
			progress = false;
			foreach ( var entry in waiting.ToArray() )
			{
				if ( !c.Stubs.ContainsKey( entry.Owner ) ) continue;
				waiting.Remove( entry );
				progress = true;
				Nest( entry.Instance, entry.Transform );
			}
		}
		// Whatever is left hangs under an instance that was never converted; Nest reports each one.
		foreach ( var entry in waiting ) Nest( entry.Instance, entry.Transform );

		// 3. Components Unity added to an object of an instance (m_AddedComponents): colliders convert, the rest is reported.
		var reportedRotation = false;
		var unconvertedAdded = new Dictionary<string, int>();
		foreach ( var component in c.Blocks )
		{
			if ( component.Stripped || component.Type is "1" or "4" or "224" or "1001" ) continue;
			var objectId = IdOf( Field( component.Body, "m_GameObject" ) );
			if ( objectId == 0 || c.ById.GetValueOrDefault( objectId ) is not { Type: "1", Stripped: true } strippedObject ) continue;
			if ( Owner( strippedObject, $"added {TypeName( component.Type, component.Body )}" ) is not var (owner, source, g) ) continue;
			var label = $"{c.InstanceName( owner )} / {source.NameOf( g )}";
			if ( component.Type is not ("64" or "65" or "135" or "136") )
			{
				var name = TypeName( component.Type, component.Body );
				unconvertedAdded[name] = unconvertedAdded.GetValueOrDefault( name ) + 1;
				continue;
			}
			if ( !NestAddedObjects ) { result.Information.Add( $"{label}: added {TypeName( component.Type, component.Body )} not converted; the instance was flattened." ); continue; }
			var id = StableGuid( $"{c.Key}:{component.Id}" );
			var enabled = Scalar( component.Body, "m_Enabled" ) != "0";
			var (parent, chain, pending) = Anchor( source, g, label );
			JsonObject data;
			if ( component.Type == "64" )
			{
				// Unity bakes the collider's mesh into an asset of its own; only a package FBX mesh has a model to point at.
				var mesh = Field( component.Body, "m_Mesh" );
				var meshName = byGuid.TryGetValue( GuidOf( mesh ), out var asset ) && asset.Kind == UnityAssetKind.Model ? MeshName( asset, IdOf( mesh ) ) : null;
				// A built-in primitive is not a package model either, but it has a generated model of its own.
				var model = UnityPrimitives.Find( GuidOf( mesh ), IdOf( mesh ) ) is UnityPrimitives.Shape primitiveShape
					? context.PrimitiveModelFor?.Invoke( primitiveShape.Name )
					: meshName == null ? null : context.ModelFor( asset, meshName );
				if ( asset != null && model != null && MirrorAxis( pending ) is int axis && axis >= 0 ) model = context.MirroredModelFor?.Invoke( asset, meshName, axis ) ?? model;
				if ( model == null ) { result.Warnings.Add( $"{label}: added MeshCollider uses a mesh that is not a converted model; skipped." ); continue; }
				data = ModelCollider( id, model, enabled );
			}
			else data = c.PrimitiveCollider( component, id, enabled, chain, label, ref reportedRotation, pending );
			additions.Add( (owner.Id, AddedObject( "Component", id, parent, "Components", data ), label) );
		}
		foreach ( var (type, count) in unconvertedAdded.OrderBy( u => u.Key, StringComparer.Ordinal ) )
			result.Information.Add( $"{count} {type} component(s) added to prefab instances not converted." );

		// 4. Attach the additions. Every instance stub exists by now, whichever order the scene listed them in.
		foreach ( var (owner, entry, name) in additions )
		{
			if ( !c.Stubs.TryGetValue( owner, out var stub ) ) { result.Warnings.Add( $"{name}: its prefab instance is not in the scene; skipped." ); skipped++; continue; }
			((JsonArray)stub["__PrefabInstancePatch"]["AddedObjects"]).Add( entry );
			if ( entry["ContainerProperty"].GetValue<string>() == "Children" ) nested++; else addedComponents++;
		}

		// 5. Components and objects Unity removed from an instance (m_RemovedComponents / m_RemovedGameObjects).
		foreach ( var instance in c.Instances.Values )
		{
			if ( !c.Stubs.TryGetValue( instance.Id, out var stub ) ) continue;
			var source = c.SourceOf( GuidOf( Field( instance.Body, "m_SourcePrefab" ) ), c.StubMirror.GetValueOrDefault( instance.Id, -1 ) );
			var list = (JsonArray)stub["__PrefabInstancePatch"]["RemovedObjects"];
			foreach ( var (field, type) in new[] { ("m_RemovedComponents", "Component"), ("m_RemovedGameObjects", "GameObject") } )
				foreach ( Match reference in Regex.Matches( Section( instance.Body, field ), @"\{[^}]*\}" ) )
				{
					var prefabGuid = StableGuid( $"{source.Key}:{IdOf( reference.Value )}" );
					if ( !GuidOf( reference.Value ).Equals( source.Asset.Guid, StringComparison.OrdinalIgnoreCase ) || !source.Ids.Contains( prefabGuid ) )
					{
						result.Information.Add( $"{c.InstanceName( instance )}: a removed {(type == "Component" ? "component" : "object")} is not part of the converted prefab; nothing to remove." );
						continue;
					}
					list.Add( new JsonObject { ["Id"] = new JsonObject { ["Type"] = type, ["IdValue"] = prefabGuid.ToString() } } );
					removed++;
				}
		}

		// 5b. Instances conversion never reached. Every path above stubs the instances it walks to, so one that
		// has no stub hangs under content that was lost and no root or child loop ever visited it. Counting it
		// here makes TotalInstances and LostInstances partition the scene's placements instead of understating it.
		foreach ( var instance in c.Instances.Values )
			if ( !c.Stubs.ContainsKey( instance.Id ) && !skips.Has( instance.Id ) )
				skips.Loss( instance.Id, SkipCause.Unreached, c.InstanceName( instance ) );

		// 6. Sky, ambient light and fog: HDRP's Volume overrides first, then Unity's RenderSettings.
		var environment = UnityVolume.Read( c.Blocks, byGuid );
		result.Information.AddRange( environment.Information );
		// Exposure is a Volume override, so its EV range belongs to the scene's report, not to a camera.
		if ( environment.Exposure != null ) result.Information.Add( environment.Exposure.Note );
		if ( environment.Sky is UnityVolume.Sky volumeSky )
		{
			result.Information.Add( volumeSky.Note );
			objects.Add( SceneObject( $"{c.Key}:skybox", "2D Skybox", "skybox", Component( "Sandbox.SkyBox2D", StableGuid( $"{c.Key}:skybox:component" ), true, new()
			{
				["SkyIndirectLighting"] = true,
				["SkyMaterial"] = DefaultSky,
				["Tint"] = Colour( volumeSky.Tint )
			} ) ) );
		}
		if ( environment.Ambient != null )
			objects.Add( SceneObject( $"{c.Key}:ambient", "Ambient Light", "", Component( "Sandbox.AmbientLight", StableGuid( $"{c.Key}:ambient:component" ), true, new()
			{
				["Color"] = Colour( environment.Ambient )
			} ) ) );
		if ( environment.Fog is UnityVolume.Fog volumeFog )
		{
			// HDRP's exponential fog is an extinction with a 63 % attenuating distance, which is exactly
			// Sandbox.Fog's exponential mode; the height ramp is HDRP's fogBaseHeight/attenuation pair.
			var height = volumeFog.BaseHeight != 0 || volumeFog.HeightAttenuation > 0 || volumeFog.MaximumHeight > 0;
			var heightLength = volumeFog.MaximumHeight > volumeFog.BaseHeight
				? volumeFog.MaximumHeight - volumeFog.BaseHeight
				: volumeFog.HeightAttenuation > 0 ? 1 / volumeFog.HeightAttenuation : 50;
			result.Information.Add( volumeFog.Note );
			objects.Add( SceneObject( $"{c.Key}:fog", "Fog", "", Component( "Sandbox.Fog", StableGuid( $"{c.Key}:fog:component" ), true, new()
			{
				["AttenuationDistance"] = Single( volumeFog.Distance * UnitsPerMetre ),
				["Color"] = Colour( volumeFog.Colour ),
				["ColorSource"] = volumeFog.SkyColour ? "SceneSky" : "Constant",
				["DistanceEnabled"] = true,
				["FalloffExponent"] = 1,
				["FogEnabled"] = true,
				["HeightColor"] = Colour( volumeFog.Colour ),
				["HeightColorSource"] = "Constant",
				["HeightEnabled"] = height,
				["HeightExponent"] = 1,
				["HeightGradientLength"] = Single( heightLength * UnitsPerMetre ),
				["HeightOffset"] = Single( volumeFog.BaseHeight * UnitsPerMetre ),
				["MaximumOpacity"] = 1,
				["Mode"] = "Exponential",
				["StartDistance"] = 0
			} ) ) );
		}
		if ( c.Blocks.FirstOrDefault( b => b.Type == "104" ) is Block render )
		{
			if ( environment.Sky == null && IdOf( Field( render.Body, "m_SkyboxMaterial" ) ) != 0 )
			{
				var guid = GuidOf( Field( render.Body, "m_SkyboxMaterial" ) );
				if ( !guid.Equals( BuiltInGuid, StringComparison.OrdinalIgnoreCase ) )
					result.Information.Add( byGuid.TryGetValue( guid, out var material )
						? $"Unity skybox material {material.Path} is not convertible to an s&box sky (its shader is not a sky shader); the default sky is used."
						: "The scene's skybox material is not in the package; the default sky is used." );
				objects.Add( SceneObject( $"{c.Key}:skybox", "2D Skybox", "skybox", Component( "Sandbox.SkyBox2D", StableGuid( $"{c.Key}:skybox:component" ), true, new()
				{
					["SkyIndirectLighting"] = true,
					["SkyMaterial"] = DefaultSky,
					["Tint"] = "1,1,1,1"
				} ) ) );
			}
			var intensity = Scalar( render.Body, "m_AmbientIntensity" ).Length == 0 ? 1 : Number( render.Body, "m_AmbientIntensity" );
			var ambientMode = Scalar( render.Body, "m_AmbientMode" );
			if ( environment.Ambient == null && ambientMode is "3" or "1" )
			{
				// Flat ambient maps directly; a trilight gradient has no counterpart, its equator colour is the middle of it.
				var colour = Vector( render.Body, ambientMode == "3" ? "m_AmbientSkyColor" : "m_AmbientEquatorColor", "r", "g", "b" ).Select( v => Linear( v ) * intensity );
				if ( ambientMode == "1" ) result.Information.Add( "Unity's gradient (trilight) ambient has no s&box equivalent; its equator colour is used as a flat ambient light." );
				objects.Add( SceneObject( $"{c.Key}:ambient", "Ambient Light", "", Component( "Sandbox.AmbientLight", StableGuid( $"{c.Key}:ambient:component" ), true, new()
				{
					["Color"] = Colour( colour )
				} ) ) );
			}
			if ( environment.Fog == null && Scalar( render.Body, "m_Fog" ) == "1" )
			{
				var mode = Scalar( render.Body, "m_FogMode" );
				var density = Number( render.Body, "m_FogDensity" );
				var start = Number( render.Body, "m_LinearFogStart" );
				// Unity's density is an extinction per metre; s&box's fog takes the distance that removes 63 %.
				var attenuation = mode == "1" ? (Number( render.Body, "m_LinearFogEnd" ) - start) / 2.15 : density <= 0 ? 0 : 1 / density;
				result.Information.Add( mode == "1"
					? "Unity's linear fog converts to Sandbox.Fog's exponential mode at the same fade distance; check its look in the editor."
					: "Unity fog converts to Sandbox.Fog's exponential mode at the same density; check its look in the editor." );
				objects.Add( SceneObject( $"{c.Key}:fog", "Fog", "", Component( "Sandbox.Fog", StableGuid( $"{c.Key}:fog:component" ), true, new()
				{
					["AttenuationDistance"] = Single( attenuation * UnitsPerMetre ),
					["Color"] = Colour( Vector( render.Body, "m_FogColor", "r", "g", "b" ).Select( Linear ) ),
					["ColorSource"] = "Constant",
					["DistanceEnabled"] = true,
					["FalloffExponent"] = 1,
					["FogEnabled"] = true,
					["HeightEnabled"] = false,
					["MaximumOpacity"] = 1,
					["Mode"] = "Exponential",
					["StartDistance"] = Single( Math.Max( 0, start ) * UnitsPerMetre )
				} ) ) );
			}
		}

		// 6b. Post-processing: one PostProcessVolume per distinct global profile, with the mapped effects on child
		// objects. PostProcessSystem collects effects from the volume object and its descendants, so the volume
		// itself carries only Priority, BlendWeight and an infinite (global) SceneVolume.
		var posts = new List<UnityVolume.VolumePost>();
		foreach ( var post in environment.PostVolumes )
			if ( !posts.Any( p => p.ProfilePath.Equals( post.ProfilePath, StringComparison.OrdinalIgnoreCase ) ) ) posts.Add( post );
		for ( var index = 0; index < posts.Count; index++ )
		{
			var post = posts[index];
			var key = $"{c.Key}:postprocessing:{index}";
			var volume = Component( "Sandbox.PostProcessVolume", StableGuid( $"{key}:component" ), true, new()
			{
				["BlendWeight"] = Single( Math.Clamp( post.Weight, 0, 1 ) ),
				["Priority"] = post.Priority,
				["SceneVolume"] = new JsonObject { ["Type"] = "Infinite" }
			} );
			// The Unity Volume's own object is already in the output (its script was not converted), so the volume
			// component attaches to it: one object per volume, at the transform Unity gave it. A Volume inside a
			// prefab instance has no plain node to attach to and keeps a root of its own.
			var node = Find( objects, StableGuid( $"{c.Key}:{post.ObjectId}" ).ToString() );
			if ( node is { } existing && existing["__PrefabInstancePatch"] == null && existing["Components"] is JsonArray existingComponents )
			{
				existingComponents.Add( volume );
				if ( string.IsNullOrEmpty( existing["Tags"]?.GetValue<string>() ) ) existing["Tags"] = "postprocess";
			}
			else
			{
				var place = c.Transforms.GetValueOrDefault( post.ObjectId ) is Block volumeTransform ? ToSbox( Local( volumeTransform.Body ) ) : ToSbox( NoTransform );
				node = SceneObject( key, post.Name, "postprocess", volume, place );
				objects.Add( node );
			}
			var children = (JsonArray)node["Children"];
			void Effect( string name, string type, Dictionary<string, JsonNode> properties ) =>
				children.Add( SceneObject( $"{key}:{type}", name, "", Component( type, StableGuid( $"{key}:{type}" ), true, properties ) ) );
			if ( post.TonemapMode >= 0 ) Effect( "Tonemapping", "Sandbox.Tonemapping", new() { ["Mode"] = UnityVolume.TonemapMode( post.TonemapMode ) } );
			if ( post.Bloom )
			{
				var bloom = new Dictionary<string, JsonNode> { ["Strength"] = Single( post.BloomStrength ), ["Tint"] = Colour( new[] { post.BloomTintR, post.BloomTintG, post.BloomTintB } ) };
				if ( post.BloomThreshold ) bloom["Threshold"] = Single( post.BloomThresholdValue );
				Effect( "Bloom", "Sandbox.Bloom", bloom );
			}
			if ( post.Dof ) Effect( "Depth of Field", "Sandbox.DepthOfField", new()
			{
				["FocalDistance"] = Single( post.FocalDistance * UnitsPerMetre ),
				["FocusRange"] = Single( post.FocusRange * UnitsPerMetre )
			} );
			if ( post.Ao ) Effect( "Ambient Occlusion", "Sandbox.AmbientOcclusion", new() { ["Intensity"] = Single( post.AoIntensity ) } );
			// Reported per emitted volume, so two Unity Volumes sharing a profile (one PostProcessVolume) are one line.
			UnityVolume.AddPostNotes( post, result.Information );
		}

		// 7. What the scene's own objects carry that has no conversion.
		var unconverted = new Dictionary<string, List<string>>();
		foreach ( var block in c.Blocks )
		{
			if ( block.Stripped || block.Type is "1" or "4" or "224" or "1001" or "29" or "104" or "157" or "196" or "1660057539" || Convertible( block.Type ) ) continue;
			// HDRP light and camera components are read by the light and camera conversion, Volume and
			// StaticLightingSky by the environment step, and planar probes and decal projectors by the HDRP
			// report, which name them in their own lines; counting them here would report them twice as
			// anonymous scripts.
			if ( block.Type == "114" && UnityHdrp.Component( GuidOf( Field( block.Body, "m_Script" ) ) )?.Kind is HdrpComponentKind.Volume or HdrpComponentKind.StaticLightingSky or HdrpComponentKind.Light or HdrpComponentKind.Camera or HdrpComponentKind.PlanarReflectionProbe or HdrpComponentKind.DecalProjector or HdrpComponentKind.ReflectionProbe ) continue;
			if ( c.ById.GetValueOrDefault( IdOf( Field( block.Body, "m_GameObject" ) ) ) is not { Type: "1", Stripped: false } go ) continue;
			var name = TypeName( block.Type, block.Body );
			if ( !unconverted.TryGetValue( name, out var owners ) ) unconverted[name] = owners = new();
			owners.Add( Scalar( go.Body, "m_Name" ) );
		}
		foreach ( var (type, owners) in unconverted.OrderBy( u => u.Key, StringComparer.Ordinal ) )
		{
			var names = owners.Distinct().ToArray();
			result.Information.Add( $"{owners.Count} {type} component(s) on scene objects not converted ({string.Join( ", ", names.Take( 5 ) )}{(names.Length > 5 ? ", …" : "")})." );
		}
		result.Information.Add( "Lightmaps, light probes, the navigation mesh and occlusion culling data are not converted." );
		// Reflection probes do convert; planar probes, decal projectors and light-layer masks do not, and are
		// named once per scene with their counts rather than turned into a component s&box does not have.
		UnityHdrpProbes.Report( c.Blocks, byGuid, result.Information );
		c.ReportBuiltins();
		c.ReportHdrp();
		// Counted here from every non-stripped type-114 block. The census names only blocks it can attribute
		// to a GameObject, so on a lossy scene its component lines can be lower than this total; HDRP keeps
		// light units, probe settings and Volume overrides in these MonoBehaviours, not in the Unity components.
		var hdrp = UnityHdrp.Count( c.Blocks.Where( b => !b.Stripped && b.Type == "114" ).Select( b => GuidOf( Field( b.Body, "m_Script" ) ) ) );

		var properties = SceneProperties();
		((JsonObject)properties["Metadata"])["Title"] = Path.GetFileNameWithoutExtension( scene.Path );
		var document = new JsonObject
		{
			["__guid"] = StableGuid( $"scene:{c.Key}" ).ToString(),
			["GameObjects"] = objects,
			["SceneProperties"] = properties,
			["ResourceVersion"] = 3,
			["Title"] = null,
			["Description"] = null,
			["__references"] = new JsonArray(),
			["__version"] = 3
		};
		// Every placement the scene refers to: the PrefabInstance blocks it holds, plus a stripped object naming an
		// instance the file lost (that placement is still in the scene, and conversion reports it as lost).
		var placements = c.Instances.Keys.ToHashSet();
		placements.UnionWith( c.StrippedInstance.Values.Where( id => id != 0 && !placements.Contains( id ) ) );
		return new SceneResult
		{
			Json = document.ToJsonString( new JsonSerializerOptions { WriteIndented = true } ),
			Statistics = new( roots, c.Stubs.Count, nested, addedComponents, removed, skipped, placements.Count, skips.Count, skips ),
			Hdrp = hdrp,
			Information = result.Information,
			Warnings = result.Warnings
		};
	}

	/// <summary>The scene's root objects (PrefabInstance and Transform fileIDs) in Unity's order: SceneRoots (2022+),
	/// else m_RootOrder (as a transform field or an instance override), else file order. Anything the list misses follows.</summary>
	static List<long> RootOrder( Converter c )
	{
		var candidates = c.Instances.Values.Where( i => IdOf( Field( i.Body, "m_TransformParent" ) ) == 0 ).Select( i => i.Id ).Concat( c.Roots.Select( t => t.Id ) ).ToHashSet();
		var fileOrder = c.Blocks.Where( b => candidates.Contains( b.Id ) ).Select( b => b.Id ).Distinct().ToList();
		List<long> order;
		if ( c.Blocks.FirstOrDefault( b => b.Type == "1660057539" ) is Block sceneRoots )
			order = Regex.Matches( Section( sceneRoots.Body, "m_Roots" ), @"fileID:\s*(-?[0-9]+)" ).Select( m => long.Parse( m.Groups[1].Value, CultureInfo.InvariantCulture ) ).Where( candidates.Contains ).Distinct().ToList();
		else
		{
			double RootOrderOf( long id )
			{
				var block = c.ById[id];
				if ( block.Type != "1001" ) return Number( block.Body, "m_RootOrder" );
				var order = Regex.Match( block.Body, @"(?m)^\s*propertyPath:\s*m_RootOrder\s*\r?\n\s*value:[ \t]*([-0-9.eE+]+)" );
				return order.Success && double.TryParse( order.Groups[1].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var v ) ? v : 0;
			}
			order = fileOrder.Select( ( id, index ) => (id, index) ).OrderBy( x => RootOrderOf( x.id ) ).ThenBy( x => x.index ).Select( x => x.id ).ToList();
		}
		var listed = order.ToHashSet();
		order.AddRange( fileOrder.Where( id => !listed.Contains( id ) ) );
		return order;
	}

	/// <summary>The Unity placement of a source prefab's object relative to the prefab root: the composition of the
	/// locals from the root's child down to the object.</summary>
	static UnityTransform PathTo( Converter.Source source, long objectId )
	{
		var locals = new List<UnityTransform>();
		var seen = new HashSet<long>();
		for ( var id = objectId; id != 0 && id != source.RootObject && seen.Add( id ); id = source.Parents.GetValueOrDefault( id ) )
			if ( source.Transforms.TryGetValue( id, out var transform ) ) locals.Add( Local( transform.Body ) );
		var path = NoTransform;
		for ( var i = locals.Count - 1; i >= 0; i-- ) path = Compose( path, locals[i] );
		return path;
	}

	static string NameOf( Converter c, Block transform ) => c.ById.GetValueOrDefault( IdOf( Field( transform.Body, "m_GameObject" ) ) ) is { Type: "1" } go ? Scalar( go.Body, "m_Name" ) : transform.Id.ToString( CultureInfo.InvariantCulture );

	static JsonObject AddedObject( string type, Guid id, Guid parent, string container, JsonObject data ) => new()
	{
		["Id"] = new JsonObject { ["Type"] = type, ["IdValue"] = id.ToString() },
		["Parent"] = new JsonObject { ["Type"] = "GameObject", ["IdValue"] = parent.ToString() },
		["PreviousElement"] = null,
		["ContainerProperty"] = container,
		["IsContainerArray"] = true,
		["Data"] = data
	};

	static JsonObject SceneObject( string key, string name, string tags, JsonObject component ) =>
		SceneObject( key, name, tags, component, ToSbox( NoTransform ) );

	static JsonObject SceneObject( string key, string name, string tags, JsonObject component, UnityTransform transform )
	{
		var node = GameObject( StableGuid( key ), name, transform, true );
		node["Tags"] = tags;
		((JsonArray)node["Components"]).Add( component );
		return node;
	}

	static bool Convertible( string type ) => type is "33" or "23" or "64" or "65" or "135" or "136" or "137" or "95" or "20" or "81" or "108" or "215";

	/// <summary>The node with this GUID anywhere in a scene's tree, or null. A prefab instance stub has no
	/// Components array of its own, so a caller attaching a component must still check for one.</summary>
	static JsonObject Find( JsonArray nodes, string guid )
	{
		foreach ( var entry in nodes )
		{
			if ( entry is not JsonObject node ) continue;
			if ( node["__guid"]?.GetValue<string>() == guid ) return node;
			if ( node["Children"] is JsonArray children && Find( children, guid ) is { } found ) return found;
		}
		return null;
	}

	/// <summary>
	/// A Unity class id as the report names it. A MonoBehaviour only means something through the script
	/// it names, so <paramref name="body"/> is consulted for the known pipeline components; without it
	/// the line stays the generic one.
	/// </summary>
	static string TypeName( string type, string body = null ) => type switch
	{
		"114" => (body == null ? null : UnityHdrp.ComponentName( GuidOf( Field( body, "m_Script" ) ) )) ?? "MonoBehaviour (script)", "124" => "Behaviour (flare layer)", "215" => "ReflectionProbe", "82" => "AudioSource",
		"198" => "ParticleSystem", "199" => "ParticleSystemRenderer", "223" => "Canvas", "54" => "Rigidbody", "111" => "Animation",
		"218" => "Terrain", "154" => "TerrainCollider", "212" => "SpriteRenderer", "102" => "TextMesh", "120" => "LineRenderer",
		"96" => "TrailRenderer", "146" => "WheelCollider", "143" => "CharacterController", "258" => "LightProbeGroup", "89" => "Cubemap",
		"205" => "LODGroup", "222" => "CanvasRenderer", "224" => "RectTransform", "1" => "GameObject", "4" => "Transform",
		_ => $"Unity type {type}"
	};

	// ---- components ----------------------------------------------------------------------------------------------

	/// <summary>A component as the editor writes one: header keys, then every property and callback in name order.</summary>
	internal static JsonObject Component( string type, Guid id, bool enabled, Dictionary<string, JsonNode> properties )
	{
		var component = new JsonObject { ["__type"] = type, ["__guid"] = id.ToString(), ["__enabled"] = enabled, ["Flags"] = 0 };
		foreach ( var callback in new[] { "OnComponentDestroy", "OnComponentDisabled", "OnComponentEnabled", "OnComponentFixedUpdate", "OnComponentStart", "OnComponentUpdate" } ) properties[callback] = null;
		foreach ( var (key, value) in properties.OrderBy( p => p.Key, StringComparer.Ordinal ) ) component[key] = value;
		return component;
	}

	static JsonNode Single( double value ) => JsonValue.Create( (float)value );

	/// <summary>sRGB to linear per channel; s&amp;box colours are linear floats.</summary>
	static double Linear( double c ) => c <= 0.04045 ? c / 12.92 : Math.Pow( (c + 0.055) / 1.055, 2.4 );

	/// <summary>r,g,b,a with five decimals, enough for a colour and stable across platforms.</summary>
	static string Colour( IEnumerable<double> rgb ) => Join( rgb.Select( v => Math.Round( v, 5 ) ).Append( 1 ).ToArray() );

	/// <summary>Unity 6000.3 Mathf.CorrelatedColorTemperatureToRGB sampled every 500 K (fewer steps above 10000 K):
	/// kelvin, r, g, b. Linear interpolation stays within 0.009 of Unity. Not linearised: Unity multiplies the
	/// light's linear colour by this factor as it is.</summary>
	static readonly double[][] Temperatures =
	{
		new[] { 1000, 1, 0.0416116, 0.00333669 }, new[] { 1500, 1, 0.146938, 0 }, new[] { 2000, 1, 0.253856, 0.0133016 }, new[] { 2500, 1, 0.358414, 0.066929 },
		new[] { 3000, 1, 0.457798, 0.147839 }, new[] { 3500, 1, 0.550245, 0.24998 }, new[] { 4000, 1, 0.634856, 0.366719 }, new[] { 4500, 1, 0.711371, 0.491717 },
		new[] { 5000, 1, 0.779967, 0.619521 }, new[] { 5500, 1, 0.841093, 0.745848 }, new[] { 6000, 1, 0.89534, 0.86761 }, new[] { 6500, 1, 0.943362, 0.98279 },
		new[] { 7000, 0.910237, 0.90006, 1 }, new[] { 7500, 0.826545, 0.85172, 1 }, new[] { 8000, 0.76165, 0.812576, 1 }, new[] { 8500, 0.709879, 0.780241, 1 },
		new[] { 9000, 0.667633, 0.753088, 1 }, new[] { 9500, 0.632521, 0.72997, 1 }, new[] { 10000, 0.602889, 0.710056, 1 }, new[] { 11000, 0.555661, 0.677519, 1 },
		new[] { 12000, 0.519747, 0.652082, 1 }, new[] { 13000, 0.491563, 0.631673, 1 }, new[] { 14000, 0.468894, 0.614956, 1 }, new[] { 15000, 0.450297, 0.601027, 1 },
		new[] { 17500, 0.415885, 0.574693, 1 }, new[] { 20000, 0.392443, 0.556273, 1 }, new[] { 25000, 0.36314, 0.532518, 1 }, new[] { 30000, 0.34621, 0.518215, 1 },
		new[] { 40000, 0.329113, 0.502752, 1 },
	};

	static double[] Temperature( double kelvin )
	{
		kelvin = Math.Clamp( kelvin, Temperatures[0][0], Temperatures[^1][0] );
		for ( var i = 0; i < Temperatures.Length - 1; i++ )
		{
			var (a, b) = (Temperatures[i], Temperatures[i + 1]);
			if ( kelvin < a[0] || kelvin > b[0] ) continue;
			var t = (kelvin - a[0]) / (b[0] - a[0]);
			return new[] { a[1] + (b[1] - a[1]) * t, a[2] + (b[2] - a[2]) * t, a[3] + (b[3] - a[3]) * t };
		}
		return new double[] { 1, 1, 1 };
	}

	/// <summary>Unity Camera (class 20). Unity's field of view is vertical by default and stays so; near/far planes
	/// convert to inches within the component's ranges; the sky always draws behind, so ClearFlags is All.
	/// An HDRP camera carries HDAdditionalCameraData (<paramref name="hd"/>, null for a non-HDRP camera): its
	/// clear-colour mode picks between the sky and backgroundColorHDR, clearDepth drops the depth clear, and
	/// the antialiasing mode maps to s&amp;box's single on/off flag. The volume mask and physical camera
	/// settings have no CameraComponent counterpart and are reported by <see cref="UnityHdrpLighting.Report"/>.</summary>
	internal static JsonObject Camera( Guid id, string body, string hd, bool enabled, bool main )
	{
		var orthographic = Scalar( body, "orthographic" ) == "1";
		// ClearColorMode: 0 Sky, 1 Color, 2 None. Only Color carries an authored background; HDRP stores it linear.
		var solid = hd != null && (int)Number( hd, "clearColorMode" ) == 1;
		var background = solid
			? Vector( hd, "backgroundColorHDR", "r", "g", "b" )
			: Vector( body, "m_BackGroundColor", "r", "g", "b" ).Select( Linear );
		var properties = new Dictionary<string, JsonNode>
		{
			["BackgroundColor"] = Colour( background ),
			// HDRP's clearDepth 0 leaves the depth buffer alone; s&amp;box clears the color buffer only then.
			["ClearFlags"] = hd != null && Scalar( hd, "clearDepth" ) == "0" ? "Color" : "All",
			["FieldOfView"] = Single( Math.Clamp( Scalar( body, "field of view" ).Length == 0 ? 60 : Number( body, "field of view" ), 1, 179 ) ),
			["FovAxis"] = Scalar( body, "m_FOVAxisMode" ) == "1" ? "Horizontal" : "Vertical",
			["IsMainCamera"] = main,
			["Orthographic"] = orthographic,
			["OrthographicHeight"] = Single( orthographic ? Number( body, "orthographic size" ) * 2 * UnitsPerMetre : 1204 ),
			["Priority"] = 1,
			["RenderExcludeTags"] = "",
			["RenderTags"] = "",
			["TargetEye"] = "None",
			["Viewport"] = "0,0,1,1",
			["ZFar"] = Single( Math.Clamp( Number( body, "far clip plane" ) * UnitsPerMetre, 1, 100000 ) ),
			["ZNear"] = Single( Math.Clamp( Number( body, "near clip plane" ) * UnitsPerMetre, 1, 1000 ) )
		};
		// AntialiasingMode: 0 None, 1 FXAA, 2 TAA, 3 SMAA. s&amp;box has one flag, so any non-zero mode enables it.
		// Only an HDRP camera writes the key; a Unity camera keeps the editor's own property set byte for byte.
		if ( hd != null ) properties["DisableAntiAliasing"] = Number( hd, "antialiasing" ) == 0;
		return Component( "Sandbox.CameraComponent", id, enabled, properties );
	}

	/// <summary>Unity AudioListener (class 81).</summary>
	internal static JsonObject AudioListener( Guid id, bool enabled ) => Component( "Sandbox.AudioListener", id, enabled, new() { ["UseCameraDirection"] = true } );

	/// <summary>Unity Light (class 108): directional, point and spot lights. Unity (linear colour space, as every URP
	/// project) lights with linear(m_Color) × colour temperature × intensity; s&amp;box carries the intensity in
	/// the colour's magnitude. Point and spot brightness is approximate: Unity's intensity is a unit-based
	/// multiplier, s&amp;box attenuates the colour over Radius. An HDRP light takes its intensity from
	/// HDAdditionalLightData through <see cref="UnityHdrpLighting"/>; <paramref name="hd"/> is that component's
	/// body, or null for a non-HDRP light. Area lights return null.
	/// </summary>
	internal static JsonObject Light( Guid id, string body, string hd, bool enabled, out string tag, out UnityHdrpLighting.Converted converted )
	{
		tag = null;
		var type = (int)Number( body, "m_Type" );
		converted = UnityHdrpLighting.Read( hd, type );
		if ( type is not (0 or 1 or 2) ) return null;
		var colour = Vector( body, "m_Color", "r", "g", "b" ).Select( Linear ).ToArray();
		// Colour temperature is on the Unity Light and is applied exactly once, here; the HD component has none.
		if ( Scalar( body, "m_UseColorTemperature" ) == "1" ) colour = Multiply( colour, Temperature( Number( body, "m_ColorTemperature" ) ) );
		var intensity = converted.Hdrp && !converted.ReportOnly
			? converted.Intensity
			: Scalar( body, "m_Intensity" ).Length == 0 ? 1 : Number( body, "m_Intensity" );
		var lightColour = Colour( colour.Select( v => v * intensity ) );
		var shadows = Regex.Match( body, @"(?ms)^\s*m_Shadows:\s*\r?\n\s*m_Type:\s*([0-9]+)" ) is { Success: true } shadow && shadow.Groups[1].Value != "0";
		var radius = Single( Number( body, "m_Range" ) * UnitsPerMetre );
		switch ( type )
		{
			case 1:
				tag = "light_directional,light";
				return Component( "Sandbox.DirectionalLight", id, enabled, new()
				{
					["FogMode"] = "Enabled", ["FogStrength"] = 1, ["LightColor"] = lightColour, ["Shadows"] = shadows, ["SkyColor"] = "0,0,0,0"
				} );
			case 2:
				tag = "light_point,light";
				return Component( "Sandbox.PointLight", id, enabled, new()
				{
					["Attenuation"] = 1, ["FogMode"] = "Enabled", ["FogStrength"] = 1, ["LightColor"] = lightColour, ["Radius"] = radius, ["Shadows"] = shadows
				} );
			default:
				tag = "light_spot,light";
				// Unity angles are full cone angles; s&box's are half-angles. Before 2019 there was no inner angle.
				var outer = Math.Clamp( Number( body, "m_SpotAngle" ) / 2, 0, 90 );
				// HDRP states the inner cone as a percentage of the outer one (m_InnerSpotPercent, -1 when unset).
				var hdInner = UnityHdrpLighting.InnerSpotAngle( hd, outer );
				var inner = !double.IsNaN( hdInner ) ? hdInner
					: Scalar( body, "m_InnerSpotAngle" ).Length == 0 ? outer * 0.75 : Math.Clamp( Number( body, "m_InnerSpotAngle" ) / 2, 0, outer );
				return Component( "Sandbox.SpotLight", id, enabled, new()
				{
					["Attenuation"] = 1, ["ConeInner"] = Single( inner ), ["ConeOuter"] = Single( outer ), ["FogMode"] = "Enabled", ["FogStrength"] = 1,
					["LightColor"] = lightColour, ["Radius"] = radius, ["Shadows"] = shadows
				} );
		}
	}

	/// <summary>
	/// The indented YAML block under <paramref name="name"/>, e.g. the influence section of an
	/// HDAdditionalReflectionData: its lines are more indented than the key, and the block ends at the first
	/// line that is not. Used instead of <see cref="Section"/>, which stops at any m_ field.
	/// </summary>
	static string Nested( string text, string name )
	{
		if ( string.IsNullOrEmpty( text ) ) return "";
		var match = Regex.Match( text, @"(?m)^(?<indent>[ 	]*)" + Regex.Escape( name ) + @":[ 	]*\r?$" );
		if ( !match.Success ) return "";
		var indent = match.Groups["indent"].Value.Length;
		var lines = new List<string>();
		foreach ( var line in text[(match.Index + match.Length)..].Split( '\n' ) )
		{
			var trimmed = line.TrimEnd( '\r' );
			if ( trimmed.Trim().Length == 0 ) { lines.Add( trimmed ); continue; }
			if ( trimmed.Length - trimmed.TrimStart( ' ', '	' ).Length <= indent ) break;
			lines.Add( trimmed );
		}
		return string.Join( "\n", lines );
	}

	/// <summary>
	/// Unity ReflectionProbe (class 215), with its HDAdditionalReflectionData (<paramref name="hd"/>, null when
	/// the probe has none). HDRP keeps the influence shape, blend distances, intensity multiplier and fade
	/// distance in that component; Unity keeps the box, resolution, importance and clip planes. s&amp;box's
	/// EnvmapProbe takes both, so the bounds are remapped to its basis and scaled to engine units and the
	/// transform stays on the object. Unity's baked cubemap is not in the package, so the probe captures in
	/// real time: the report says so once per scene.
	/// </summary>
	internal static JsonObject EnvmapProbe( Guid id, string body, string hd, bool enabled )
	{
		var size = Vector( body, "m_BoxSize", "x", "y", "z" );
		var offset = Vector( body, "m_BoxOffset", "x", "y", "z" );
		var projection = Scalar( body, "m_BoxProjection" ) == "1" ? "Box" : "Sphere";
		var resolution = (int)Number( body, "m_Resolution" );
		var near = Number( body, "m_NearClip" );
		var far = Number( body, "m_FarClip" );
		var blend = Number( body, "m_BlendDistance" );
		var priority = (int)Number( body, "m_Importance" );
		var multiplier = 1.0;
		if ( hd != null )
		{
			var settings = Nested( hd, "m_ProbeSettings" );
			var influence = Nested( settings, "influence" );
			var lighting = Nested( settings, "lighting" );
			if ( influence.Length > 0 )
			{
				// HDRP's InfluenceVolume.shape: 0 Box, 1 Sphere.
				var shape = (int)Number( influence, "m_Shape" );
				projection = shape == 1 ? "Sphere" : "Box";
				var box = Vector( influence, "m_BoxSize", "x", "y", "z" );
				if ( box.Any( v => v > 0 ) ) size = box;
				var radius = Number( influence, "m_SphereRadius" );
				if ( shape == 1 && radius > 0 ) size = new[] { radius * 2, radius * 2, radius * 2 };
				var positive = Vector( influence, "m_BoxBlendDistancePositive", "x", "y", "z" );
				if ( positive.Any( v => v > 0 ) ) blend = positive.Max();
			}
			var hdResolution = (int)Number( Nested( settings, "frustum" ), "m_Resolution" );
			if ( hdResolution > 0 ) resolution = hdResolution;
			var hdMultiplier = Number( lighting, "m_Multiplier" );
			if ( hdMultiplier > 0 ) multiplier = hdMultiplier;
			// HDRP's lighting.m_FadeDistance is where a reflection fades out; s&box's MaxDistance instead
			// gates how close the camera must be for a realtime update, so the two are not the same value
			// and the engine's default (512) is kept.
		}
		if ( size.All( v => v <= 0 ) ) size = new double[] { 10, 10, 10 };
		var half = size.Select( v => Math.Abs( v ) / 2 ).ToArray();
		var min = new[] { offset[0] - half[0], offset[1] - half[1], offset[2] - half[2] };
		var max = new[] { offset[0] + half[0], offset[1] + half[1], offset[2] + half[2] };
		// Unity (Y up) to s&box (Z up): (x, y, z) becomes (z, -x, y), so the low corner takes the low Z, the
		// negated high X and the low Y. A local box is scaled, never rotated, by that basis change.
		var mins = new[] { min[2], -max[0], min[1] }.Select( v => v * UnitsPerMetre ).ToArray();
		var maxs = new[] { max[2], -min[0], max[1] }.Select( v => v * UnitsPerMetre ).ToArray();
		return Component( "Sandbox.EnvmapProbe", id, enabled, new()
		{
			["Bounds"] = new JsonObject { ["Mins"] = Join( mins ), ["Maxs"] = Join( maxs ) },
			["Feathering"] = Single( Math.Max( 0, blend ) * UnitsPerMetre ),
			["Mode"] = "Realtime",
			["MultiBounce"] = false,
			["Priority"] = Math.Clamp( priority, 0, 100 ),
			["Projection"] = projection,
			["Resolution"] = resolution switch { <= 128 => "Small", <= 256 => "Medium", <= 512 => "Large", _ => "Huge" },
			["TintColor"] = Colour( new[] { multiplier, multiplier, multiplier } ),
			["UpdateStrategy"] = "OnEnabled",
			["ZFar"] = Single( Math.Max( 1, far * UnitsPerMetre ) ),
			["ZNear"] = Single( Math.Max( 1, near * UnitsPerMetre ) )
		} );
	}
}
