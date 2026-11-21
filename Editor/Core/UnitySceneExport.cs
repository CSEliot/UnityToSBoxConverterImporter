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
/// components Unity added to or removed from them), plain objects with their models, colliders, cameras and
/// lights, and the scene's sky, ambient light and fog. Scripts, terrain, lightmaps, light probes, navigation
/// and post-processing are listed in the report, not converted.
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

	internal sealed record Statistics( int Roots, int Instances, int Nested, int AddedComponents, int RemovedObjects, int Skipped );

	internal sealed class SceneResult
	{
		public string Json;
		public Statistics Statistics;
		public List<string> Information;
		public List<string> Warnings;
	}

	/// <summary>The guid of the scene's skybox material, or an empty string.</summary>
	internal static string SkyboxMaterialGuid( string sceneText ) => GuidOf( Field( sceneText, "m_SkyboxMaterial" ) );

	internal static SceneResult Convert( UnityAsset scene, Context context )
	{
		var result = new Result();
		var byGuid = context.ByGuid;
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
			var node = block.Type == "1001" ? c.Instance( block, null ) : c.Node( block, false );
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
			var data = instance != null ? c.Instance( instance, prepend, pending ) : c.Node( transform, false, prepend, pending );
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
			if ( Owner( strippedObject, $"added {TypeName( component.Type )}" ) is not var (owner, source, g) ) continue;
			var label = $"{c.InstanceName( owner )} / {source.NameOf( g )}";
			if ( component.Type is not ("64" or "65" or "135" or "136") )
			{
				unconvertedAdded[TypeName( component.Type )] = unconvertedAdded.GetValueOrDefault( TypeName( component.Type ) ) + 1;
				continue;
			}
			if ( !NestAddedObjects ) { result.Information.Add( $"{label}: added {TypeName( component.Type )} not converted; the instance was flattened." ); continue; }
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

		// 6. Sky, ambient light and fog from RenderSettings.
		if ( c.Blocks.FirstOrDefault( b => b.Type == "104" ) is Block render )
		{
			var skybox = Field( render.Body, "m_SkyboxMaterial" );
			if ( IdOf( skybox ) != 0 )
			{
				var guid = GuidOf( skybox );
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
			if ( ambientMode is "3" or "1" )
			{
				// Flat ambient maps directly; a trilight gradient has no counterpart, its equator colour is the middle of it.
				var colour = Vector( render.Body, ambientMode == "3" ? "m_AmbientSkyColor" : "m_AmbientEquatorColor", "r", "g", "b" ).Select( v => Linear( v ) * intensity );
				if ( ambientMode == "1" ) result.Information.Add( "Unity's gradient (trilight) ambient has no s&box equivalent; its equator colour is used as a flat ambient light." );
				objects.Add( SceneObject( $"{c.Key}:ambient", "Ambient Light", "", Component( "Sandbox.AmbientLight", StableGuid( $"{c.Key}:ambient:component" ), true, new()
				{
					["Color"] = Colour( colour )
				} ) ) );
			}
			if ( Scalar( render.Body, "m_Fog" ) == "1" )
			{
				var mode = Scalar( render.Body, "m_FogMode" );
				var density = Number( render.Body, "m_FogDensity" );
				// Exponential fog reaches 99 % at e^(-d·k) = 0.01, exp² at e^(-(d·k)²) = 0.01; s&box's fog is a linear fade to that distance.
				var (start, end) = mode == "1" || density <= 0
					? (Number( render.Body, "m_LinearFogStart" ), Number( render.Body, "m_LinearFogEnd" ))
					: (0, (mode == "2" ? 4.6 : 2.15) / density);
				result.Information.Add( "Unity fog converts to a GradientFog object tall enough to act as plain distance fog; check its look in the editor." );
				objects.Add( SceneObject( $"{c.Key}:fog", "Fog", "", Component( "Sandbox.GradientFog", StableGuid( $"{c.Key}:fog:component" ), true, new()
				{
					["Color"] = Colour( Vector( render.Body, "m_FogColor", "r", "g", "b" ).Select( Linear ) ),
					["EndDistance"] = Single( end * UnitsPerMetre ),
					["FalloffExponent"] = 1,
					["Height"] = 100000,
					["StartDistance"] = Single( start * UnitsPerMetre ),
					["VerticalFalloffExponent"] = 1
				} ) ) );
			}
		}

		// 7. What the scene's own objects carry that has no conversion.
		var unconverted = new Dictionary<string, List<string>>();
		foreach ( var block in c.Blocks )
		{
			if ( block.Stripped || block.Type is "1" or "4" or "224" or "1001" or "29" or "104" or "157" or "196" or "1660057539" || Convertible( block.Type ) ) continue;
			if ( c.ById.GetValueOrDefault( IdOf( Field( block.Body, "m_GameObject" ) ) ) is not { Type: "1", Stripped: false } go ) continue;
			if ( !unconverted.TryGetValue( TypeName( block.Type ), out var owners ) ) unconverted[TypeName( block.Type )] = owners = new();
			owners.Add( Scalar( go.Body, "m_Name" ) );
		}
		foreach ( var (type, owners) in unconverted.OrderBy( u => u.Key, StringComparer.Ordinal ) )
		{
			var names = owners.Distinct().ToArray();
			result.Information.Add( $"{owners.Count} {type} component(s) on scene objects not converted ({string.Join( ", ", names.Take( 5 ) )}{(names.Length > 5 ? ", …" : "")})." );
		}
		result.Information.Add( "Lightmaps, light probes, the navigation mesh, occlusion culling data and post-processing volumes are not converted." );
		c.ReportBuiltins();

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
		return new SceneResult
		{
			Json = document.ToJsonString( new JsonSerializerOptions { WriteIndented = true } ),
			Statistics = new( roots, c.Stubs.Count, nested, addedComponents, removed, skipped ),
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

	static JsonObject SceneObject( string key, string name, string tags, JsonObject component )
	{
		var node = GameObject( StableGuid( key ), name, ToSbox( NoTransform ), true );
		node["Tags"] = tags;
		((JsonArray)node["Components"]).Add( component );
		return node;
	}

	static bool Convertible( string type ) => type is "33" or "23" or "64" or "65" or "135" or "136" or "137" or "95" or "20" or "81" or "108";

	static string TypeName( string type ) => type switch
	{
		"114" => "MonoBehaviour (script)", "124" => "Behaviour (flare layer)", "215" => "ReflectionProbe", "82" => "AudioSource",
		"198" => "ParticleSystem", "199" => "ParticleSystemRenderer", "223" => "Canvas", "54" => "Rigidbody", "111" => "Animation",
		"218" => "Terrain", "154" => "TerrainCollider", "212" => "SpriteRenderer", "102" => "TextMesh", "120" => "LineRenderer",
		"96" => "TrailRenderer", "146" => "WheelCollider", "143" => "CharacterController", "258" => "LightProbeGroup", "89" => "Cubemap",
		"205" => "LODGroup", "222" => "CanvasRenderer", "224" => "RectTransform", "1" => "GameObject", "4" => "Transform",
		_ => $"Unity type {type}"
	};

	// ---- components ----------------------------------------------------------------------------------------------

	/// <summary>A component as the editor writes one: header keys, then every property and callback in name order.</summary>
	static JsonObject Component( string type, Guid id, bool enabled, Dictionary<string, JsonNode> properties )
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
	/// convert to inches within the component's ranges; the sky always draws behind, so ClearFlags is All.</summary>
	internal static JsonObject Camera( Guid id, string body, bool enabled, bool main )
	{
		var orthographic = Scalar( body, "orthographic" ) == "1";
		return Component( "Sandbox.CameraComponent", id, enabled, new()
		{
			["BackgroundColor"] = Colour( Vector( body, "m_BackGroundColor", "r", "g", "b" ).Select( Linear ) ),
			["ClearFlags"] = "All",
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
		} );
	}

	/// <summary>Unity AudioListener (class 81).</summary>
	internal static JsonObject AudioListener( Guid id, bool enabled ) => Component( "Sandbox.AudioListener", id, enabled, new() { ["UseCameraDirection"] = true } );

	/// <summary>
	/// Unity Light (class 108): directional, point and spot lights. Unity (linear colour space, as every URP
	/// project) lights with linear(m_Color) × colour temperature × intensity; s&amp;box carries the intensity in
	/// the colour's magnitude. Point and spot brightness is approximate: Unity's intensity is a unit-based
	/// multiplier, s&amp;box attenuates the colour over Radius. Area lights return null.
	/// </summary>
	internal static JsonObject Light( Guid id, string body, bool enabled, out string tag )
	{
		tag = null;
		var type = (int)Number( body, "m_Type" );
		if ( type is not (0 or 1 or 2) ) return null;
		var colour = Vector( body, "m_Color", "r", "g", "b" ).Select( Linear ).ToArray();
		if ( Scalar( body, "m_UseColorTemperature" ) == "1" ) colour = Multiply( colour, Temperature( Number( body, "m_ColorTemperature" ) ) );
		var intensity = Scalar( body, "m_Intensity" ).Length == 0 ? 1 : Number( body, "m_Intensity" );
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
				var inner = Scalar( body, "m_InnerSpotAngle" ).Length == 0 ? outer * 0.75 : Math.Clamp( Number( body, "m_InnerSpotAngle" ) / 2, 0, outer );
				return Component( "Sandbox.SpotLight", id, enabled, new()
				{
					["Attenuation"] = 1, ["ConeInner"] = Single( inner ), ["ConeOuter"] = Single( outer ), ["FogMode"] = "Enabled", ["FogStrength"] = 1,
					["LightColor"] = lightColour, ["Radius"] = radius, ["Shadows"] = shadows
				} );
		}
	}
}
