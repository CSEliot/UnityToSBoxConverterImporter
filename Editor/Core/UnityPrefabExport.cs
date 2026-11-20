using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace ImportUnityPackage;

/// <summary>
/// Converts a Unity prefab's hierarchy into an s&amp;box prefab: GameObjects with converted transforms,
/// ModelRenderers on the generated models, ModelColliders where Unity had MeshColliders, Box/Sphere/Capsule
/// colliders where Unity had those, cameras and lights, and nested prefab instances with their transform/name
/// overrides. A mesh child whose parent carries a mesh from the same FBX gets an identity transform: its
/// per-node .vmdl already imports the FBX placement, so the prefab must not apply it again; only components
/// that do not follow the model (primitive colliders) keep the removed transform. Scripts and other components
/// are not converted. The object and instance conversions live in <see cref="Converter"/>, which
/// <see cref="UnitySceneExport"/> reuses for scenes.
/// </summary>
internal static class UnityPrefabExport
{
	/// <summary>Unity metres to s&amp;box inches; the same factor the generated models are scaled by.</summary>
	internal const double UnitsPerMetre = 39.37;

	/// <summary>GameObjectFlags.Static: the object (and its children) never moves during gameplay.</summary>
	internal const int StaticFlag = 32768;

	internal sealed class Block
	{
		public string Type;
		public long Id;
		public bool Stripped;
		public string Body;
	}

	internal sealed record UnityTransform( double[] Position, double[] Rotation, double[] Scale );

	/// <summary>Unity identity transform; also the s&amp;box identity, since both spaces share the same identity.</summary>
	internal static readonly UnityTransform NoTransform = new( new double[3], new double[] { 0, 0, 0, 1 }, new double[] { 1, 1, 1 } );

	/// <summary>Unity's TRS convention: scale, then rotate, then translate; parent-first composition nests transforms.</summary>
	internal static UnityTransform Compose( UnityTransform parent, UnityTransform child ) => new(
		Add( parent.Position, Rotate( parent.Rotation, Multiply( parent.Scale, child.Position ) ) ),
		MultiplyQuaternions( parent.Rotation, child.Rotation ),
		Multiply( parent.Scale, child.Scale ) );

	internal static double[] Multiply( double[] a, double[] b ) => a.Zip( b, ( x, y ) => x * y ).ToArray();
	static double[] Add( double[] a, double[] b ) => a.Zip( b, ( x, y ) => x + y ).ToArray();

	static double[] MultiplyQuaternions( double[] a, double[] b ) => new[]
	{
		a[3] * b[0] + a[0] * b[3] + a[1] * b[2] - a[2] * b[1],
		a[3] * b[1] - a[0] * b[2] + a[1] * b[3] + a[2] * b[0],
		a[3] * b[2] + a[0] * b[1] - a[1] * b[0] + a[2] * b[3],
		a[3] * b[3] - a[0] * b[0] - a[1] * b[1] - a[2] * b[2],
	};

	/// <summary>Rotate a Unity point by a quaternion (x, y, z, w): v + w*t + cross(q.xyz, t), t = 2*cross(q.xyz, v).</summary>
	static double[] Rotate( double[] q, double[] v )
	{
		var t = new[] { 2 * (q[1] * v[2] - q[2] * v[1]), 2 * (q[2] * v[0] - q[0] * v[2]), 2 * (q[0] * v[1] - q[1] * v[0]) };
		return new[]
		{
			v[0] + q[3] * t[0] + (q[1] * t[2] - q[2] * t[1]),
			v[1] + q[3] * t[1] + (q[2] * t[0] - q[0] * t[2]),
			v[2] + q[3] * t[2] + (q[0] * t[1] - q[1] * t[0]),
		};
	}

	/// <summary>Where a Unity local point ends up under a removed chain (itself when there is none).</summary>
	static double[] Apply( UnityTransform chain, double[] point ) => chain == null ? point : Add( chain.Position, Rotate( chain.Rotation, Multiply( chain.Scale, point ) ) );

	static double ChainScale( UnityTransform chain ) => chain == null ? 1 : Math.Max( Math.Abs( chain.Scale[0] ), Math.Max( Math.Abs( chain.Scale[1] ), Math.Abs( chain.Scale[2] ) ) );

	/// <summary>For an exact 90-degree-multiple rotation, the Unity local axis each Unity world axis takes its
	/// extent from (so an axis-aligned box stays axis-aligned and only its size axes permute); null otherwise.</summary>
	static int[] AxisPermutation( double[] q )
	{
		// Rotation matrix rows: world axis a = R[a][b] * local axis b.
		var r = new[]
		{
			new[] { 1 - 2 * (q[1] * q[1] + q[2] * q[2]), 2 * (q[0] * q[1] - q[2] * q[3]), 2 * (q[0] * q[2] + q[1] * q[3]) },
			new[] { 2 * (q[0] * q[1] + q[2] * q[3]), 1 - 2 * (q[0] * q[0] + q[2] * q[2]), 2 * (q[1] * q[2] - q[0] * q[3]) },
			new[] { 2 * (q[0] * q[2] - q[1] * q[3]), 2 * (q[1] * q[2] + q[0] * q[3]), 1 - 2 * (q[0] * q[0] + q[1] * q[1]) },
		};
		var from = new int[3];
		var used = new bool[3];
		for ( var world = 0; world < 3; world++ )
		{
			from[world] = -1;
			for ( var local = 0; local < 3; local++ )
			{
				if ( Math.Abs( r[world][local] ) < 1e-6 ) continue;
				if ( Math.Abs( Math.Abs( r[world][local] ) - 1 ) > 1e-6 || used[local] ) return null;
				used[local] = true;
				from[world] = local;
			}
			if ( from[world] < 0 ) return null;
		}
		return from;
	}

	/// <summary>Unity's local transforms of one GameObject (its transform block body), shared with s&amp;box space only in the identity.
	/// A field missing from the YAML means Unity's default (no rotation, scale 1), not zeros.</summary>
	internal static UnityTransform Local( string body ) => new( Vector( body, "m_LocalPosition", "x", "y", "z" ),
		Field( body, "m_LocalRotation" ).Length == 0 ? new double[] { 0, 0, 0, 1 } : Vector( body, "m_LocalRotation", "x", "y", "z", "w" ),
		Field( body, "m_LocalScale" ).Length == 0 ? new double[] { 1, 1, 1 } : Vector( body, "m_LocalScale", "x", "y", "z" ) );

	/// <summary>The FBX guid a GameObject's MeshFilter points at, or null when it has none or the mesh is not a package model.</summary>
	static string MeshFbx( Dictionary<long, Block[]> components, long objectId, IReadOnlyDictionary<string, UnityAsset> byGuid )
	{
		var filter = components.GetValueOrDefault( objectId )?.FirstOrDefault( b => b.Type == "33" );
		return filter != null && byGuid.TryGetValue( GuidOf( Field( filter.Body, "m_Mesh" ) ), out var model ) && model.Kind == UnityAssetKind.Model ? model.Guid.ToLowerInvariant() : null;
	}

	/// <summary>
	/// The removed Unity placement, per GameObject fileID, of every object whose mesh placement its own generated
	/// model already carries: an object with a mesh from FBX F whose parent has a mesh from the same F. Its vmdl is
	/// the whole FBX filtered to that node, so the prefab must not apply the node transform again; the object gets
	/// an identity transform. The top of a contiguous run of same-F meshes (the anchor, whose parent has no F mesh)
	/// keeps its transform. A baked object's chain is the composition of the Unity locals from just below the
	/// anchor down to and including the object; anything that does not follow the model — its primitive colliders,
	/// its non-baked children and nested instances — must apply the chain instead.
	/// </summary>
	/// <param name="byId">Every block of the prefab by fileID.</param>
	/// <param name="transformBlocks">The prefab's non-stripped Transform blocks.</param>
	/// <param name="meshOf">The FBX guid of a GameObject's mesh, or null.</param>
	/// <param name="parents">Receives each GameObject's parent GameObject fileID (0 for the root).</param>
	static Dictionary<long, UnityTransform> BakedChains( IReadOnlyDictionary<long, Block> byId, IEnumerable<Block> transformBlocks, Func<long, string> meshOf, out Dictionary<long, long> parents )
	{
		var bodyOf = new Dictionary<long, string>();
		var fatherOf = new Dictionary<long, long>();
		parents = fatherOf;
		foreach ( var transform in transformBlocks )
		{
			var objectId = IdOf( Field( transform.Body, "m_GameObject" ) );
			if ( bodyOf.ContainsKey( objectId ) ) continue;
			bodyOf[objectId] = transform.Body;
			fatherOf[objectId] = byId.GetValueOrDefault( IdOf( Field( transform.Body, "m_Father" ) ) ) is Block father ? IdOf( Field( father.Body, "m_GameObject" ) ) : 0;
		}
		var chains = new Dictionary<long, UnityTransform>();
		UnityTransform Chain( long objectId )
		{
			if ( chains.TryGetValue( objectId, out var known ) ) return known;
			chains[objectId] = null; // malformed parent cycles stop here instead of recursing forever
			var fbx = meshOf( objectId );
			var fatherId = fatherOf.GetValueOrDefault( objectId );
			var chain = fbx != null && bodyOf.ContainsKey( fatherId ) && meshOf( fatherId ) == fbx
				? Chain( fatherId ) is UnityTransform fatherChain ? Compose( fatherChain, Local( bodyOf[objectId] ) ) : Local( bodyOf[objectId] )
				: null;
			chains[objectId] = chain;
			return chain;
		}
		foreach ( var objectId in bodyOf.Keys.ToArray() ) Chain( objectId );
		return chains;
	}

	/// <summary>The baked chains of a prefab's objects (see <see cref="BakedChains"/>) and each object's parent, for
	/// converting nested-instance overrides the same way the prefab itself was converted.</summary>
	static Dictionary<long, UnityTransform> BakedObjects( List<Block> blocks, IReadOnlyDictionary<string, UnityAsset> byGuid, out Dictionary<long, long> parents )
	{
		var byId = blocks.GroupBy( b => b.Id ).ToDictionary( g => g.Key, g => g.First() );
		var transforms = blocks.Where( b => b.Type is "4" or "224" && !b.Stripped ).GroupBy( b => IdOf( Field( b.Body, "m_GameObject" ) ) ).ToDictionary( g => g.Key, g => g.First() );
		var components = blocks.Where( b => !b.Stripped && b.Type is "33" or "23" or "64" or "65" or "135" or "136" )
			.GroupBy( b => IdOf( Field( b.Body, "m_GameObject" ) ) ).ToDictionary( g => g.Key, g => g.ToArray() );
		return BakedChains( byId, transforms.Values, objectId => MeshFbx( components, objectId, byGuid ), out parents );
	}

	/// <summary>Finds the model resource generated for one mesh of an FBX, or null when none was.</summary>
	internal delegate string ModelResolver( UnityAsset model, string mesh );

	static readonly Regex BlockPattern = new( @"(?ms)^--- !u!(?<type>[0-9]+) &(?<id>-?[0-9]+)(?<stripped>[^\r\n]*)\r?\n(?<body>.*?)(?=^--- !u!|\z)" );

	internal static string GuidOf( string reference ) => Regex.Match( reference ?? "", @"\bguid:\s*([a-fA-F0-9]{32})" ).Groups[1].Value;
	internal static long IdOf( string reference ) => long.TryParse( Regex.Match( reference ?? "", @"\bfileID:\s*(-?[0-9]+)" ).Groups[1].Value, out var id ) ? id : 0;
	internal static string Field( string text, string name ) => Regex.Match( text, @"(?m)^\s*" + Regex.Escape( name ) + @":\s*\{([^}\r\n]*)\}" ).Groups[1].Value;
	internal static string Scalar( string text, string name ) => Unquote( Regex.Match( text, @"(?m)^\s*" + Regex.Escape( name ) + @":[ \t]*([^\r\n]*?)[ \t]*\r?$" ).Groups[1].Value );

	/// <summary>The text of a YAML list under <paramref name="name"/>, up to the next field at any indent.</summary>
	internal static string Section( string text, string name ) => Regex.Match( text, @"(?ms)^\s*" + Regex.Escape( name ) + @":(.*?)(?=^\s*m_\w+:|\z)" ).Groups[1].Value;

	/// <summary>YAML scalars with special characters are single- or double-quoted; the quotes are not part of the value.</summary>
	internal static string Unquote( string value )
	{
		if ( value.Length >= 2 && value[0] == '\'' && value[^1] == '\'' ) return value[1..^1].Replace( "''", "'" );
		if ( value.Length >= 2 && value[0] == '"' && value[^1] == '"' ) return Regex.Unescape( value[1..^1] );
		return value;
	}

	internal static double[] Vector( string text, string name, params string[] axes )
	{
		var body = Field( text, name );
		return axes.Select( a =>
		{
			var m = Regex.Match( body, @"\b" + a + @":\s*([-+0-9.eE]+)" );
			return m.Success && double.TryParse( m.Groups[1].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var v ) ? v : 0;
		} ).ToArray();
	}

	internal static List<Block> Blocks( string text ) => BlockPattern.Matches( text ).Select( m => new Block
	{
		Type = m.Groups["type"].Value,
		Id = long.Parse( m.Groups["id"].Value, CultureInfo.InvariantCulture ),
		Stripped = m.Groups["stripped"].Value.Contains( "stripped" ),
		Body = m.Groups["body"].Value
	} ).ToList();

	/// <summary>Stable GUID so a re-import writes byte-identical prefabs and keeps instance links valid.</summary>
	internal static Guid StableGuid( string key )
	{
		var hash = MD5.HashData( Encoding.UTF8.GetBytes( "unityimporterplus:" + key ) );
		// Guid(byte[]) stores its third field little-endian, so the RFC 4122 version nibble is byte 7's high nibble.
		hash[7] = (byte)((hash[7] & 0x0F) | 0x30);
		hash[8] = (byte)((hash[8] & 0x3F) | 0x80);
		return new Guid( hash );
	}

	/// <summary>Unity (left-handed, Y up, Z forward) to s&amp;box (right-handed, Z up, X forward, Y left).</summary>
	internal static double[] Position( double[] u ) => new[] { u[2] * UnitsPerMetre, -u[0] * UnitsPerMetre, u[1] * UnitsPerMetre };
	/// <summary>The basis change is a reflection, so the rotation axis maps with an extra sign flip.</summary>
	internal static double[] Rotation( double[] q ) => new[] { -q[2], q[0], -q[1], q[3] };
	internal static double[] Scale( double[] s ) => new[] { s[2], s[0], s[1] };
	/// <summary>A whole Unity local transform in s&amp;box space.</summary>
	internal static UnityTransform ToSbox( UnityTransform t ) => new( Position( t.Position ), Rotation( t.Rotation ), Scale( t.Scale ) );

	static string Number( double value )
	{
		var f = (float)value;
		if ( Math.Abs( f ) < 1e-6f ) f = 0;
		return f.ToString( "R", CultureInfo.InvariantCulture );
	}
	internal static double Number( string text, string name ) => double.TryParse( Scalar( text, name ), NumberStyles.Float, CultureInfo.InvariantCulture, out var v ) ? v : 0;
	internal static string Join( double[] values ) => string.Join( ",", values.Select( Number ) );

	internal static JsonObject GameObject( Guid id, string name, UnityTransform transform, bool enabled )
	{
		return new JsonObject
		{
			["__guid"] = id.ToString(),
			["__version"] = 2,
			["Flags"] = 0,
			["Name"] = name,
			["Position"] = Join( transform.Position ),
			["Rotation"] = Join( transform.Rotation ),
			["Scale"] = Join( transform.Scale ),
			["Tags"] = "",
			["Enabled"] = enabled,
			["NetworkMode"] = 2,
			["NetworkFlags"] = 0,
			["NetworkOrphaned"] = 0,
			["NetworkTransmit"] = true,
			["OwnerTransfer"] = 1,
			["Components"] = new JsonArray(),
			["Children"] = new JsonArray()
		};
	}

	static JsonObject ModelRenderer( Guid id, string model, bool enabled ) => new()
	{
		["__type"] = "Sandbox.ModelRenderer",
		["__guid"] = id.ToString(),
		["__enabled"] = enabled,
		["Flags"] = 0,
		["BodyGroups"] = 18446744073709551615UL,
		["CreateAttachments"] = false,
		["LodOverride"] = null,
		["MaterialGroup"] = null,
		["MaterialOverride"] = null,
		["Materials"] = null,
		["Model"] = model,
		["OnComponentDestroy"] = null,
		["OnComponentDisabled"] = null,
		["OnComponentEnabled"] = null,
		["OnComponentFixedUpdate"] = null,
		["OnComponentStart"] = null,
		["OnComponentUpdate"] = null,
		["RenderOptions"] = new JsonObject { ["GameLayer"] = true, ["OverlayLayer"] = false, ["BloomLayer"] = false, ["AfterUILayer"] = false },
		["RenderType"] = "On",
		["Tint"] = "1,1,1,1"
	};

	/// <summary>Property set and order as the editor writes them (see the engine's avatar scene). <paramref name="bodyGroups"/> null keeps every mesh.</summary>
	static JsonObject SkinnedModelRenderer( Guid id, string model, ulong? bodyGroups, bool enabled ) => new()
	{
		["__type"] = "Sandbox.SkinnedModelRenderer",
		["__guid"] = id.ToString(),
		["__enabled"] = enabled,
		["Flags"] = 0,
		["AnimationGraph"] = null,
		["BodyGroups"] = bodyGroups ?? ulong.MaxValue,
		["BoneMergeTarget"] = null,
		["CreateAttachments"] = false,
		["CreateBoneObjects"] = false,
		["LodOverride"] = null,
		["MaterialGroup"] = null,
		["MaterialOverride"] = null,
		["Materials"] = null,
		["Model"] = model,
		["Morphs"] = new JsonObject(),
		["OnComponentDestroy"] = null,
		["OnComponentDisabled"] = null,
		["OnComponentEnabled"] = null,
		["OnComponentFixedUpdate"] = null,
		["OnComponentStart"] = null,
		["OnComponentUpdate"] = null,
		["Parameters"] = new JsonObject { ["bools"] = new JsonObject(), ["ints"] = new JsonObject(), ["floats"] = new JsonObject(), ["vectors"] = new JsonObject(), ["rotations"] = new JsonObject() },
		["PlaybackRate"] = 1,
		["RenderOptions"] = new JsonObject { ["GameLayer"] = true, ["OverlayLayer"] = false, ["BloomLayer"] = false, ["AfterUILayer"] = false },
		["RenderType"] = "On",
		["Sequence"] = new JsonObject { ["Name"] = null, ["Looping"] = true, ["Blending"] = false },
		["Tint"] = "1,1,1,1",
		["UseAnimGraph"] = true
	};

	sealed class SkinnedConversion
	{
		/// <summary>SkinnedModelRenderer components to add, by the GameObject that gets them, with the model and mesh they show.</summary>
		public readonly Dictionary<long, List<(JsonObject Renderer, UnityAsset Model, string Mesh)>> Components = new();
		/// <summary>GameObjects the conversion replaces: pure bone objects, and objects that only held a folded skinned mesh.</summary>
		public readonly HashSet<long> DroppedObjects = new();
		/// <summary>SkinnedMeshRenderer blocks folded into a SkinnedModelRenderer; they emit nothing themselves.</summary>
		public readonly HashSet<long> FoldedRenderers = new();
	}

	sealed record SkinnedRenderer( Block Block, long Object, UnityAsset Model, string Mesh, long Host, long[] Bones );

	/// <summary>
	/// Plans the skinned part of a prefab. Unity gives every skinned mesh its own object and renders it through
	/// the bone objects, so an object's own transform never moves its skinned mesh, while the root bone's parent
	/// is where the FBX root sits. s&amp;box renders one SkinnedModelRenderer per skeleton: it goes on that parent
	/// (the host), its bodygroups show the meshes Unity had active, and the objects that only existed to hold a
	/// mesh or a bone are dropped. A bone object carrying other components or children stays as a plain object.
	/// </summary>
	static SkinnedConversion SkinnedRenderers( List<Block> blocks, IReadOnlyDictionary<long, Block> byId, Dictionary<long, Block> transforms,
		Dictionary<long, Block[]> components, IReadOnlyDictionary<string, UnityAsset> byGuid, ModelResolver modelFor, string key, Result result )
	{
		var conversion = new SkinnedConversion();
		var objectOf = transforms.Values.ToDictionary( t => t.Id, t => IdOf( Field( t.Body, "m_GameObject" ) ) );
		long ParentOf( long objectId ) => transforms.TryGetValue( objectId, out var t ) ? objectOf.GetValueOrDefault( IdOf( Field( t.Body, "m_Father" ) ) ) : 0;
		string NameOf( long objectId ) => byId.GetValueOrDefault( objectId ) is { Type: "1" } go ? Scalar( go.Body, "m_Name" ) : objectId.ToString( CultureInfo.InvariantCulture );
		IEnumerable<long> Children( long objectId )
		{
			if ( !transforms.TryGetValue( objectId, out var t ) ) yield break;
			foreach ( Match child in Regex.Matches( Regex.Match( t.Body, @"(?ms)^\s*m_Children:(.*?)(?=^\s*m_Father:)" ).Groups[1].Value, @"fileID:\s*(-?[0-9]+)" ) )
				// A stripped child is a nested prefab instance, which no dropped object may carry.
				yield return objectOf.TryGetValue( long.Parse( child.Groups[1].Value, CultureInfo.InvariantCulture ), out var childObject ) ? childObject : -1;
		}
		var inactive = blocks.Where( b => b.Type == "1" && !b.Stripped && Scalar( b.Body, "m_IsActive" ) == "0" ).Select( b => b.Id ).ToHashSet();
		var renderers = new List<SkinnedRenderer>();
		foreach ( var block in blocks.Where( b => b.Type == "137" && !b.Stripped ) )
		{
			var objectId = IdOf( Field( block.Body, "m_GameObject" ) );
			var mesh = Field( block.Body, "m_Mesh" );
			if ( !byGuid.TryGetValue( GuidOf( mesh ), out var model ) || model.Kind != UnityAssetKind.Model ) continue;
			var meshName = MeshName( model, IdOf( mesh ) );
			if ( meshName == null || !model.ModelInfo.SkinnedMeshes.Contains( meshName ) ) continue;
			var rootBone = objectOf.GetValueOrDefault( IdOf( Field( block.Body, "m_RootBone" ) ) );
			var bones = Regex.Matches( Regex.Match( block.Body, @"(?ms)^\s*m_Bones:(.*?)(?=^\s*m_\w+:)" ).Groups[1].Value, @"fileID:\s*(-?[0-9]+)" )
				.Select( m => objectOf.GetValueOrDefault( long.Parse( m.Groups[1].Value, CultureInfo.InvariantCulture ) ) ).Append( rootBone ).Where( id => id != 0 ).Distinct().ToArray();
			// The model's origin is the FBX root, the parent of the skeleton's root bone. Without bones the renderer's own object is the best guess.
			var host = rootBone == 0 ? objectId : ParentOf( rootBone ) is var parent && parent != 0 ? parent : rootBone;
			renderers.Add( new( block, objectId, model, meshName, host, bones ) );
		}
		bool Shown( SkinnedRenderer renderer )
		{
			if ( Scalar( renderer.Block.Body, "m_Enabled" ) == "0" ) return false;
			// Active up to, not including, the host, whose own Enabled flag the converted object keeps.
			for ( var (id, seen) = (renderer.Object, new HashSet<long>()); id != 0 && id != renderer.Host && seen.Add( id ); id = ParentOf( id ) )
				if ( inactive.Contains( id ) ) return false;
			return true;
		}
		foreach ( var group in renderers.GroupBy( r => (r.Host, Guid: r.Model.Guid.ToLowerInvariant()) ).OrderBy( g => g.Key.Host ).ThenBy( g => g.Key.Guid, StringComparer.Ordinal ) )
		{
			var hostName = NameOf( group.Key.Host );
			var model = group.First().Model;
			var path = modelFor( model, group.First().Mesh );
			if ( path == null ) { result.Warnings.Add( $"{hostName}: no generated model for {model.Path}; its skinned mesh renderers were skipped." ); continue; }
			var shown = group.Where( Shown ).Select( r => r.Mesh ).Distinct( StringComparer.OrdinalIgnoreCase ).ToArray();
			var mask = model.ModelInfo.BodyGroupMask( shown, out var warning );
			if ( warning != null ) result.Warnings.Add( $"{hostName}: {warning}" );
			// Without bodygroups (a single skinned mesh) the renderer itself is switched, as Unity switched its object.
			if ( !conversion.Components.TryGetValue( group.Key.Host, out var list ) ) conversion.Components[group.Key.Host] = list = new();
			list.Add( (SkinnedModelRenderer( StableGuid( $"{key}:skinned:{group.Key.Host}:{group.Key.Guid}" ), path, mask, mask != null || shown.Length > 0 ), model, group.First().Mesh) );
			var kept = new List<string>();
			foreach ( var renderer in group )
			{
				conversion.FoldedRenderers.Add( renderer.Block.Id );
				if ( renderer.Object == group.Key.Host ) continue;
				var only = components.GetValueOrDefault( renderer.Object )?.All( c => c.Id == renderer.Block.Id ) == true;
				if ( only && !Children( renderer.Object ).Any() ) conversion.DroppedObjects.Add( renderer.Object );
				else kept.Add( NameOf( renderer.Object ) );
			}
			// s&box poses bones from the model; an object per bone would only sit still where the FBX put it.
			var bones = group.SelectMany( r => r.Bones ).Where( b => b != group.Key.Host ).ToHashSet();
			var droppable = new Dictionary<long, bool>();
			bool Droppable( long objectId )
			{
				if ( droppable.TryGetValue( objectId, out var known ) ) return known;
				droppable[objectId] = false; // a parent cycle stops here
				return droppable[objectId] = bones.Contains( objectId ) && !components.ContainsKey( objectId ) && Children( objectId ).All( Droppable );
			}
			var droppedBones = bones.Where( Droppable ).ToArray();
			foreach ( var bone in droppedBones ) conversion.DroppedObjects.Add( bone );
			result.Information.Add( $"{hostName}: a SkinnedModelRenderer for {Path.GetFileName( path )} replaces {group.Count()} skinned mesh object(s) and {droppedBones.Length} bone object(s); it shows {(shown.Length == 0 ? "no mesh" : string.Join( ", ", shown ))}." );
			if ( kept.Count > 0 ) result.Information.Add( $"{hostName}: {string.Join( ", ", kept )} kept as plain object(s) for their other components or children; their skinned mesh renders from {hostName}." );
			if ( bones.Count > droppedBones.Length ) result.Warnings.Add( $"{hostName}: {bones.Count - droppedBones.Length} bone object(s) with other components or children were kept as plain objects; they will not follow the skeleton." );
		}
		return conversion;
	}

	internal static JsonObject ModelCollider( Guid id, string model, bool enabled ) => new()
	{
		["__type"] = "Sandbox.ModelCollider",
		["__guid"] = id.ToString(),
		["__enabled"] = enabled,
		["Flags"] = 0,
		["ColliderFlags"] = 0,
		["Elasticity"] = null,
		["Friction"] = null,
		["IsTrigger"] = false,
		["Model"] = model,
		["OnComponentDestroy"] = null,
		["OnComponentDisabled"] = null,
		["OnComponentEnabled"] = null,
		["OnComponentFixedUpdate"] = null,
		["OnComponentStart"] = null,
		["OnComponentUpdate"] = null,
		["OnObjectTriggerEnter"] = null,
		["OnObjectTriggerExit"] = null,
		["OnTriggerEnter"] = null,
		["OnTriggerExit"] = null,
		["RollingResistance"] = null,
		["Static"] = true,
		["Surface"] = null,
		["SurfaceVelocity"] = "0,0,0"
	};

	/// <summary>The Collider base properties every shape collider shares, in the order editor-written prefabs use.</summary>
	static void ColliderBase( JsonObject collider, Guid id, bool enabled, bool trigger )
	{
		collider["__guid"] = id.ToString();
		collider["__enabled"] = enabled;
		collider["Flags"] = 0;
		collider["ColliderFlags"] = 0;
		collider["Elasticity"] = null;
		collider["Friction"] = null;
		collider["IsTrigger"] = trigger;
		collider["OnComponentDestroy"] = null;
		collider["OnComponentDisabled"] = null;
		collider["OnComponentEnabled"] = null;
		collider["OnComponentFixedUpdate"] = null;
		collider["OnComponentStart"] = null;
		collider["OnComponentUpdate"] = null;
		collider["OnObjectTriggerEnter"] = null;
		collider["OnObjectTriggerExit"] = null;
		collider["OnTriggerEnter"] = null;
		collider["OnTriggerExit"] = null;
		collider["RollingResistance"] = null;
		// s&box mesh/shape collision on placed props is static; a keyframed prop would need dynamic bodies.
		collider["Static"] = true;
		collider["Surface"] = null;
		collider["SurfaceVelocity"] = "0,0,0";
	}

	/// <summary>size/centre/segment values may carry a removed chain (see <see cref="BakedChains"/>): the Unity placement
	/// a baked child lost to its model, which a primitive collider must keep so the shape stays in place.
	/// centre is in Unity local space; Unity sizes are corner-to-corner in metres, and the axis swap must not
	/// turn a size into a signed offset.</summary>
	static JsonObject BoxCollider( Guid id, double[] size, double[] center, bool trigger, bool enabled, UnityTransform chain = null, int[] mirror = null )
	{
		var collider = new JsonObject { ["__type"] = "Sandbox.BoxCollider" };
		ColliderBase( collider, id, enabled, trigger );
		collider["Center"] = Join( Position( Mirrored( mirror ?? NoMirror, Apply( chain, center ) ) ) );
		var scaled = chain == null ? size : Multiply( size, chain.Scale );
		// An axis-aligned box can follow a rotation only as its bounds: extent along each parent axis is
		// sum |R[axis][local]| * size[local]. For 90-degree turns that is exactly the permuted size.
		if ( chain != null )
		{
			var local = scaled;
			scaled = Enumerable.Range( 0, 3 ).Select( axis =>
			{
				var unit = new double[3];
				unit[axis] = 1;
				// Row `axis` of R is R^T applied to the unit axis, i.e. the rotation's inverse on that axis.
				var row = Rotate( new[] { -chain.Rotation[0], -chain.Rotation[1], -chain.Rotation[2], chain.Rotation[3] }, unit );
				return Enumerable.Range( 0, 3 ).Sum( j => Math.Abs( row[j] * local[j] ) );
			} ).ToArray();
		}
		collider["Scale"] = Join( Scale( scaled ).Select( s => Math.Abs( s ) * UnitsPerMetre ).ToArray() );
		return collider;
	}

	static JsonObject SphereCollider( Guid id, double radius, double[] center, bool trigger, bool enabled, UnityTransform chain = null, int[] mirror = null )
	{
		var collider = new JsonObject { ["__type"] = "Sandbox.SphereCollider" };
		ColliderBase( collider, id, enabled, trigger );
		collider["Center"] = Join( Position( Mirrored( mirror ?? NoMirror, Apply( chain, center ) ) ) );
		collider["Radius"] = (float)(Math.Abs( radius ) * ChainScale( chain ) * UnitsPerMetre);
		return collider;
	}

	/// <summary>Unity's capsule axis (0=X 1=Y 2=Z) with the segment ends at centre ± (height/2 - radius) along it.
	/// Exact under uniform scale. Under non-uniform parent scale s&amp;box scales the ends per axis but the radius
	/// uniformly, while Unity uses the largest axis for both, so the shapes differ there.</summary>
	static JsonObject CapsuleCollider( Guid id, double radius, double height, int direction, double[] center, bool trigger, bool enabled, UnityTransform chain = null, int[] mirror = null )
	{
		var collider = new JsonObject { ["__type"] = "Sandbox.CapsuleCollider" };
		ColliderBase( collider, id, enabled, trigger );
		var half = Math.Max( height * 0.5 - Math.Abs( radius ), 0 );
		double[] Axis( double offset )
		{
			var unity = (double[])center.Clone();
			unity[direction] += offset;
			return Position( Mirrored( mirror ?? NoMirror, Apply( chain, unity ) ) );
		}
		collider["Start"] = Join( Axis( -half ) );
		collider["End"] = Join( Axis( half ) );
		collider["Radius"] = (float)(Math.Abs( radius ) * ChainScale( chain ) * UnitsPerMetre);
		return collider;
	}

	internal static JsonObject SceneProperties() => new()
	{
		["NetworkInterpolation"] = true,
		["PhysicsMode"] = "Physics3D",
		["TimeScale"] = 1,
		["WantsSystemScene"] = true,
		["Metadata"] = new JsonObject(),
		["NavMesh"] = new JsonObject
		{
			["Enabled"] = false, ["IncludeStaticBodies"] = true, ["IncludeKeyframedBodies"] = true, ["EditorAutoUpdate"] = false,
			["AgentHeight"] = 64, ["AgentRadius"] = 16, ["AgentStepSize"] = 18, ["AgentMaxSlope"] = 40,
			["ExcludedBodies"] = "", ["IncludedBodies"] = "", ["DeferGeneration"] = false, ["CustomBounds"] = false
		}
	};

	/// <summary>Every GUID an instance of a generated prefab needs a counterpart for, with its node kind.</summary>
	static IEnumerable<Guid> Guids( JsonNode node )
	{
		if ( node is not JsonObject obj ) yield break;
		if ( obj["__guid"]?.GetValue<string>() is string id && Guid.TryParse( id, out var guid ) ) yield return guid;
		foreach ( var key in new[] { "Components", "Children" } )
			if ( obj[key] is JsonArray array )
				foreach ( var child in array )
					foreach ( var nested in Guids( child ) ) yield return nested;
	}

	/// <summary>Mesh name for a MeshFilter reference, by modern hashed ID or legacy recycle ID.</summary>
	internal static string MeshName( UnityAsset asset, long meshId )
	{
		var model = asset.ModelInfo;
		var hashed = model.Meshes.Where( m => UnityFileId.Hash( "Type:Mesh->" + m + "0" ) == meshId ).ToArray();
		if ( hashed.Length == 1 ) return hashed[0];
		if ( model.RecycleNames.TryGetValue( meshId, out var name ) && model.Meshes.Contains( name ) ) return name;
		return model.Meshes.Count == 1 ? model.Meshes[0] : null;
	}

	/// <summary>Unity's fixed object ids inside a model prefab: the FBX root object and its transform, and the
	/// MeshRenderer/mesh sub-asset pairs 2300000+2n / 4300000+2n in the model's own node order.</summary>
	internal const long ModelRootObject = 100000, ModelRootTransform = 400000;
	internal static bool IsModelRenderer( long id ) => id >= 2300000 && id < 2400000 && (id - 2300000) % 2 == 0;
	internal static long ModelRendererId( int index ) => 2300000 + 2 * index;

	/// <summary>One m_Modifications entry: the source object it targets, the property, its value and the object
	/// reference when the entry carries one (older packages omit the line entirely).</summary>
	static readonly Regex ModificationPattern = new( @"(?ms)^\s*- target:\s*\{(?<target>[^}]+)\}\s*\r?\n\s*propertyPath:\s*(?<path>[^\r\n]+?)\s*\r?\n\s*value:[ 	]*(?<value>[^\r\n]*?)[ 	]*\r?$(?:\r?\n\s*objectReference:\s*\{(?<reference>[^}]*)\})?" );

	/// <summary>
	/// The fileID of a reference field. Unlike <see cref="Field"/> this tolerates a reference Unity folds onto
	/// two lines, which it does whenever the guid makes the line long ("{fileID: 100000, guid: …, type: 3}").
	/// </summary>
	internal static long Reference( string text, string name ) =>
		long.TryParse( Regex.Match( text ?? "", @"(?m)^\s*" + Regex.Escape( name ) + @":\s*\{fileID:\s*(-?[0-9]+)" ).Groups[1].Value, out var id ) ? id : 0;

	/// <summary>A model-prefab instance's root: what its m_Modifications say about the FBX root object and transform.</summary>
	internal sealed record ModelRoot( UnityTransform Transform, string Name, bool Active, double StaticFlags );

	/// <summary>
	/// Reads the root of a model-prefab instance: the transform overrides on the model's root Transform (400000),
	/// and the name, active state and static flags on its root object (100000). A model-wrapper prefab is exactly
	/// one such instance, so its own root and a nested instance's root are read the same way.
	/// </summary>
	internal static ModelRoot RootOf( Block instance, string fallbackName )
	{
		var name = fallbackName ?? "";
		var active = true;
		var flags = 0.0;
		// A fresh identity: NoTransform is shared, and the override writes below go into these arrays.
		var unity = new UnityTransform( new double[3], new[] { 0d, 0d, 0d, 1d }, new[] { 1d, 1d, 1d } );
		foreach ( Match modification in ModificationPattern.Matches( instance.Body ) )
		{
			var target = IdOf( modification.Groups["target"].Value );
			var path = modification.Groups["path"].Value.Trim( '\'' );
			var value = Unquote( modification.Groups["value"].Value );
			if ( target == ModelRootObject )
			{
				if ( path == "m_Name" ) name = value;
				else if ( path == "m_IsActive" ) active = value != "0";
				else if ( path == "m_StaticEditorFlags" && double.TryParse( value, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed ) ) flags = parsed;
				continue;
			}
			if ( target != ModelRootTransform ) continue;
			var axis = path.Length > 2 && path[^2] == '.' ? "xyzw".IndexOf( path[^1] ) : -1;
			if ( axis < 0 || !double.TryParse( value, NumberStyles.Float, CultureInfo.InvariantCulture, out var component ) ) continue;
			if ( path.StartsWith( "m_LocalRotation." ) ) unity.Rotation[axis] = component;
			else if ( path.StartsWith( "m_LocalScale." ) ) unity.Scale[axis] = component;
			else if ( path.StartsWith( "m_LocalPosition." ) && axis < 3 ) unity.Position[axis] = component;
		}
		return new( unity, name, active, flags );
	}

	internal sealed class Result
	{
		public string Json;
		public List<string> Information { get; } = new();
		public List<string> Warnings { get; } = new();
	}

	/// <summary>The GUIDs of a converted prefab, so instances can map every node (see __PrefabIdToInstanceId).</summary>
	internal static Guid[] PrefabGuids( string json ) => Guids( JsonNode.Parse( json )?["RootObject"] ).ToArray();

	/// <summary>Everything a conversion resolves outside its own file: the package, the run's generated models and
	/// prefabs, and the mirrored variants of both that instances with a negative scale need.</summary>
	internal sealed record Context( IReadOnlyDictionary<string, UnityAsset> ByGuid, ModelResolver ModelFor, Func<string, string> PrefabPathFor,
		IReadOnlyDictionary<string, string> Converted, Func<string, string> MaterialFor )
	{
		/// <summary>The model of a mesh mirrored along a Unity axis (0 x, 1 y, 2 z), generated on demand; null when the mesh has no model.</summary>
		public Func<UnityAsset, string, int, string> MirroredModelFor { get; init; }
		/// <summary>A prefab's variant mirrored along a Unity axis, converted on demand: its resource path and JSON, or null.</summary>
		public Func<string, int, (string Path, string Json)?> MirroredPrefabFor { get; init; }
		/// <summary>The generated prefab of a model asset a scene instances directly, built on demand: its resource
		/// path and JSON, or null. Unity keeps such a prefab implicitly for every FBX, so a scene's direct model
		/// instance resolves through the same path a wrapper prefab does.</summary>
		public Func<UnityAsset, (string Path, string Json)?> ModelPrefabFor { get; init; }
	}

	/// <summary>+1/-1 per Unity axis: the mirror a subtree still owes. Identity when no component is negative.</summary>
	static readonly int[] NoMirror = { 1, 1, 1 };
	static bool IsMirror( int[] d ) => d[0] < 0 || d[1] < 0 || d[2] < 0;
	/// <summary>The one negative axis of a single-axis mirror, or -1.</summary>
	internal static int MirrorAxis( int[] d ) => Array.IndexOf( d, -1 );
	static string MirrorName( int axis ) => "xyz"[axis].ToString();

	/// <summary>
	/// s&amp;box renders a negatively scaled object inside out (Unity flips the face winding, s&amp;box does not), so
	/// no emitted transform carries a negative scale. The sign is split off: a local R·S under a pending mirror D
	/// becomes position D·p, rotation D·R·D and scale |S|, and D·sign(S) is what the object's own model and its
	/// children still owe. An even number of negative axes is a 180° turn, folded into the rotation; an odd number
	/// leaves one axis to mirror, which the model takes through ModelDoc's mirror modifier and the children through
	/// the same split. A baked object emits the identity and passes the pending mirror on unchanged: its own sign
	/// travels in the chain its children compose with.
	/// </summary>
	internal static (UnityTransform Emitted, int[] Pending) Unmirror( UnityTransform local, int[] pending, bool baked )
	{
		if ( baked ) return (local, pending);
		var position = new[] { pending[0] * local.Position[0], pending[1] * local.Position[1], pending[2] * local.Position[2] };
		var rotation = Conjugate( pending, local.Rotation );
		var total = new[] { pending[0] * Math.Sign( local.Scale[0] ), pending[1] * Math.Sign( local.Scale[1] ), pending[2] * Math.Sign( local.Scale[2] ) };
		for ( var i = 0; i < 3; i++ ) if ( total[i] == 0 ) total[i] = 1;
		var scale = new[] { Math.Abs( local.Scale[0] ), Math.Abs( local.Scale[1] ), Math.Abs( local.Scale[2] ) };
		var negatives = total.Count( s => s < 0 );
		var owed = NoMirror;
		if ( negatives == 2 )
		{
			// diag with two -1 is a half turn about the remaining axis.
			rotation = MultiplyQuaternions( rotation, HalfTurn( Array.IndexOf( total, 1 ) ) );
		}
		else if ( negatives == 1 ) owed = total;
		else if ( negatives == 3 )
		{
			// -I = a half turn about x (flips y and z) followed by a mirror of x.
			rotation = MultiplyQuaternions( rotation, HalfTurn( 0 ) );
			owed = new[] { -1, 1, 1 };
		}
		return (new UnityTransform( position, rotation, scale ), owed);
	}

	/// <summary>Quaternion of a 180° rotation about a Unity axis.</summary>
	static double[] HalfTurn( int axis )
	{
		var q = new double[4];
		q[axis] = 1;
		return q;
	}

	/// <summary>D·R·D for a single-axis mirror D: the rotation seen through the mirror negates the quaternion's
	/// components on the other two axes. The identity leaves it alone.</summary>
	static double[] Conjugate( int[] mirror, double[] q )
	{
		var axis = MirrorAxis( mirror );
		if ( axis < 0 ) return q;
		var r = (double[])q.Clone();
		for ( var i = 0; i < 3; i++ ) if ( i != axis ) r[i] = -r[i];
		return r;
	}

	/// <summary>A Unity-space point seen through a pending mirror.</summary>
	static double[] Mirrored( int[] mirror, double[] point ) => new[] { mirror[0] * point[0], mirror[1] * point[1], mirror[2] * point[2] };

	/// <summary>
	/// One Unity YAML file (a prefab or a scene) and the conversions a prefab and a scene share: a plain
	/// GameObject with its components and children (<see cref="Node"/>) and a prefab instance with its
	/// overrides (<see cref="Instance"/>). Source prefabs of instances are read once per file. A mirrored
	/// variant (<see cref="MirrorAxis"/> 0..2) converts the same file seen through that mirror, under its own key.
	/// </summary>
	internal sealed class Converter
	{
		/// <summary>The file's guid, lowercased (plus the mirror axis of a variant); the prefix of every GUID the conversion derives.</summary>
		public readonly string Key;
		/// <summary>The Unity axis this conversion mirrors the file along, or -1.</summary>
		public readonly int MirrorAxis;
		public readonly Result Result;
		public readonly Context Context;
		public readonly List<Block> Blocks;
		public readonly Dictionary<long, Block> ById;
		/// <summary>Non-stripped Transform/RectTransform blocks by their GameObject fileID, in file order.</summary>
		public readonly Dictionary<long, Block> Transforms;
		/// <summary>Convertible component blocks by GameObject fileID.</summary>
		public readonly Dictionary<long, Block[]> Components;
		/// <summary>PrefabInstance blocks by fileID.</summary>
		public readonly Dictionary<long, Block> Instances;
		/// <summary>Stripped transform fileID → the PrefabInstance it stands in for an object of.</summary>
		public readonly Dictionary<long, long> StrippedInstance;
		/// <summary>Transforms without a parent, in file order; a prefab has exactly one.</summary>
		public readonly List<Block> Roots;
		/// <summary>The one top-level PrefabInstance of a model wrapper, or null. A model wrapper is a prefab whose
		/// whole content is a PrefabInstance of an FBX model prefab, so it has no Transform block of its own.</summary>
		public readonly Block Wrapper;
		/// <summary>The model a wrapper's top-level instance sources, or null when it is absent or not a model.</summary>
		public readonly UnityAsset WrapperModel;
		/// <summary>Baked placements by GameObject fileID (see <see cref="BakedChains"/>).</summary>
		public readonly Dictionary<long, UnityTransform> Chains;
		/// <summary>Each GameObject's parent GameObject fileID (0 at the top).</summary>
		public readonly Dictionary<long, long> ParentOf;
		/// <summary>Every instance stub <see cref="Instance"/> produced, by PrefabInstance fileID.</summary>
		public readonly Dictionary<long, JsonObject> Stubs = new();
		/// <summary>The mirror axis of the source variant each stub points at (-1 for the prefab itself).</summary>
		public readonly Dictionary<long, int> StubMirror = new();
		/// <summary>Set by a caller that converts an instance's added and removed objects itself (a scene does);
		/// otherwise <see cref="Instance"/> reports them as not converted.</summary>
		public bool ConvertsAdditions { get; set; }
		/// <summary>Set by a caller that converts a scene, whose plain objects are not prefab evidence: a
		/// MeshRenderer's own material assignment is then emitted on its ModelRenderer. A prefab's renderers
		/// already feed the model's slot remaps (<see cref="UnityPrefabBindings"/>), so prefab output does not move.</summary>
		public bool ConvertsRendererMaterials { get; set; }
		readonly SkinnedConversion skinned;
		readonly Dictionary<string, Source> sources = new( StringComparer.OrdinalIgnoreCase );
		readonly long mainCamera;
		IReadOnlyDictionary<string, UnityAsset> byGuid => Context.ByGuid;

		public Converter( UnityAsset file, string text, Context context, Result result, int mirrorAxis = -1 )
		{
			Context = context;
			Result = result;
			MirrorAxis = mirrorAxis;
			Key = mirrorAxis < 0 ? file.Guid.ToLowerInvariant() : $"{file.Guid.ToLowerInvariant()}:mirror{MirrorName( mirrorAxis )}";
			Blocks = UnityPrefabExport.Blocks( text );
			ById = Blocks.GroupBy( b => b.Id ).ToDictionary( g => g.Key, g => g.First() );
			Transforms = Blocks.Where( b => b.Type is "4" or "224" && !b.Stripped ).GroupBy( b => IdOf( Field( b.Body, "m_GameObject" ) ) ).ToDictionary( g => g.Key, g => g.First() );
			Components = Blocks.Where( b => !b.Stripped && b.Type is "33" or "23" or "64" or "65" or "135" or "136" or "137" or "95" or "20" or "81" or "108" )
				.GroupBy( b => IdOf( Field( b.Body, "m_GameObject" ) ) ).ToDictionary( g => g.Key, g => g.ToArray() );
			var unconverted = Blocks.Where( b => !b.Stripped ).Select( b => b.Type switch { "154" => "TerrainCollider", "146" => "WheelCollider", "143" => "CharacterController", _ => null } )
				.Where( n => n != null ).Distinct().ToArray();
			if ( unconverted.Length > 0 ) result.Information.Add( $"{string.Join( ", ", unconverted )} not converted." );
			Instances = Blocks.Where( b => b.Type == "1001" ).ToDictionary( b => b.Id );
			// A nested instance appears in its parent's m_Children as a stripped transform pointing at the PrefabInstance.
			StrippedInstance = Blocks.Where( b => b.Stripped && b.Type is "4" or "224" )
				.ToDictionary( b => b.Id, b => IdOf( Field( b.Body, "m_PrefabInstance" ) ) );
			Roots = Transforms.Values.Where( t => IdOf( Field( t.Body, "m_Father" ) ) == 0 ).ToList();
			// A model wrapper is a prefab whose content is one PrefabInstance of an FBX model: Unity strips the
			// model's own root objects, so the file has no Transform block and nothing for Roots to hold.
			Wrapper = Roots.Count == 0
				? Instances.Values.Where( i => IdOf( Field( i.Body, "m_TransformParent" ) ) == 0 ).OrderBy( i => i.Id ).FirstOrDefault()
				: null;
			WrapperModel = Wrapper != null && byGuid.TryGetValue( GuidOf( Field( Wrapper.Body, "m_SourcePrefab" ) ), out var wrapperSource ) && wrapperSource.Kind == UnityAssetKind.Model
				? wrapperSource : null;
			Chains = BakedChains( ById, Transforms.Values, objectId => MeshFbx( Components, objectId, byGuid ), out ParentOf );
			// Baking assumes the top mesh of a same-FBX run is the FBX's root node, placed at the model origin.
			// Below a root that is not certain, and a node placement there would be applied twice.
			var rootObjects = Roots.Select( r => IdOf( Field( r.Body, "m_GameObject" ) ) ).ToHashSet();
			foreach ( var anchor in Chains.Where( c => c.Value != null ).Select( c => ParentOf.GetValueOrDefault( c.Key ) )
				.Where( a => !rootObjects.Contains( a ) && Chains.GetValueOrDefault( a ) == null ).Distinct().OrderBy( a => a ) )
			{
				var anchorName = ById.GetValueOrDefault( anchor ) is Block anchorGo ? Scalar( anchorGo.Body, "m_Name" ) : anchor.ToString( CultureInfo.InvariantCulture );
				result.Information.Add( $"{anchorName}: its FBX sub-parts are laid out relative to this object, not the prefab root; check in s&box that it is not offset twice." );
			}
			// Unity renders a skinned mesh through its bone objects, one object per mesh; s&box renders the whole
			// skeleton from one SkinnedModelRenderer whose bodygroups pick the visible meshes.
			skinned = SkinnedRenderers( Blocks, ById, Transforms, Components, byGuid, context.ModelFor, Key, result );
			mainCamera = MainCamera();
		}

		/// <summary>The mirror a file's root starts under: the variant's axis, or none.</summary>
		int[] RootMirror => MirrorAxis < 0 ? NoMirror : Enumerable.Range( 0, 3 ).Select( i => i == MirrorAxis ? -1 : 1 ).ToArray();

		/// <summary>The camera that becomes s&amp;box's main camera: enabled on an active object, tagged MainCamera
		/// before untagged, the highest Unity depth before lower ones, then file order.</summary>
		long MainCamera()
		{
			Block best = null;
			var bestTagged = false;
			double bestDepth = 0;
			foreach ( var camera in Blocks.Where( b => b.Type == "20" && !b.Stripped ) )
			{
				if ( Scalar( camera.Body, "m_Enabled" ) == "0" ) continue;
				var go = ById.GetValueOrDefault( IdOf( Field( camera.Body, "m_GameObject" ) ) );
				if ( go == null || Scalar( go.Body, "m_IsActive" ) == "0" ) continue;
				var tagged = Scalar( go.Body, "m_TagString" ) == "MainCamera";
				var depth = Number( camera.Body, "m_Depth" );
				if ( best == null || tagged && !bestTagged || tagged == bestTagged && depth > bestDepth ) { best = camera; bestTagged = tagged; bestDepth = depth; }
			}
			return best?.Id ?? 0;
		}

		/// <summary>A model path seen through a pending mirror: the mesh's mirrored variant, or the model itself.</summary>
		string ModelThrough( int[] mirror, UnityAsset model, string mesh, string plain )
		{
			if ( plain == null || !IsMirror( mirror ) ) return plain;
			return Context.MirroredModelFor?.Invoke( model, mesh, UnityPrefabExport.MirrorAxis( mirror ) ) ?? plain;
		}

		/// <summary>A prefab that instances in this file are made from (or its mirrored variant), read once.</summary>
		internal sealed class Source
		{
			public UnityAsset Asset;
			/// <summary>The generated prefab's resource path, as an instance refers to it.</summary>
			public string SboxPath;
			/// <summary>The key the generated prefab derived its GUIDs from.</summary>
			public string Key;
			/// <summary>The Unity axis the variant mirrors along, or -1.</summary>
			public int MirrorAxis;
			public List<Block> Blocks;
			public Dictionary<long, Block> ById;
			/// <summary>Non-stripped transforms by GameObject fileID.</summary>
			public Dictionary<long, Block> Transforms;
			public Block Root;
			public long RootObject;
			public Block RootObjectBlock;
			/// <summary>True when the source prefab is a model wrapper, whose root is an FBX model's root object.</summary>
			public bool IsWrapper;
			/// <summary>The root's Unity transform id: 400000 inside a model wrapper, the root Transform's fileID otherwise.</summary>
			public long RootTransformId;
			/// <summary>The root's Unity local transform, with the source prefab's own root overrides applied.</summary>
			public UnityTransform RootTransform = NoTransform;
			/// <summary>The root object's name and active state as the source prefab sets them.</summary>
			public string RootName;
			public bool RootActive = true;
			/// <summary>The root object's Unity static flags; an override carrying the same value changes nothing.</summary>
			public double RootFlags;
			/// <summary>A wrapper's fixed renderer ids (2300000+2n), one per mesh node in the order the models were written.</summary>
			public long[] RendererIds = Array.Empty<long>();
			public Dictionary<long, UnityTransform> Chains;
			public Dictionary<long, long> Parents;
			/// <summary>Every GUID of the converted prefab, root first.</summary>
			public Guid[] Ids;
			/// <summary>Why instances of this prefab cannot be converted, or null.</summary>
			public string Problem;
			readonly Dictionary<long, int[]> pendingOut = new();
			public string NameOf( long objectId ) => ById.GetValueOrDefault( objectId ) is { Type: "1" } go ? Scalar( go.Body, "m_Name" ) : objectId.ToString( CultureInfo.InvariantCulture );

			/// <summary>The mirror an object of the converted prefab starts under: what <see cref="Node"/> computed for its parent.</summary>
			public int[] PendingIn( long objectId ) => objectId == RootObject || !Parents.TryGetValue( objectId, out var parent ) || parent == 0 ? RootPending : PendingOut( parent );

			/// <summary>The mirror an object of the converted prefab passes to its children and model.</summary>
			public int[] PendingOut( long objectId )
			{
				if ( pendingOut.TryGetValue( objectId, out var known ) ) return known;
				pendingOut[objectId] = NoMirror; // a parent cycle stops here
				var pending = PendingIn( objectId );
				var chain = Chains.GetValueOrDefault( objectId );
				int[] result;
				if ( chain != null || !Transforms.TryGetValue( objectId, out var transform ) ) result = pending;
				else
				{
					var local = Local( transform.Body );
					var inherited = objectId == RootObject ? null : Chains.GetValueOrDefault( Parents.GetValueOrDefault( objectId ) );
					result = Unmirror( inherited == null ? local : Compose( inherited, local ), pending, false ).Pending;
				}
				return pendingOut[objectId] = result;
			}

			int[] RootPending => MirrorAxis < 0 ? NoMirror : Enumerable.Range( 0, 3 ).Select( i => i == MirrorAxis ? -1 : 1 ).ToArray();
		}

		public Source SourceOf( string guid, int mirrorAxis = -1 )
		{
			var cacheKey = mirrorAxis < 0 ? guid : $"{guid}:{mirrorAxis}";
			if ( sources.TryGetValue( cacheKey, out var known ) ) return known;
			var source = new Source { MirrorAxis = mirrorAxis };
			string path = null, json = null;
			var found = byGuid.TryGetValue( guid, out var asset );
			// A scene may instance an FBX directly, with no .prefab asset in between: the model's own generated
			// prefab stands in for it, built by the wrapper builder and cached like a converted prefab.
			var isModel = found && asset.Kind == UnityAssetKind.Model;
			if ( isModel && mirrorAxis < 0 )
			{
				if ( Context.ModelPrefabFor?.Invoke( asset ) is { } synthesized ) (path, json) = synthesized;
			}
			else if ( found && mirrorAxis < 0 ) { path = Context.PrefabPathFor( guid ); Context.Converted.TryGetValue( guid, out json ); }
			else if ( found && !isModel && Context.MirroredPrefabFor?.Invoke( guid, mirrorAxis ) is var (variantPath, variantJson) ) (path, json) = (variantPath, variantJson);
			if ( !found || path == null || json == null )
				source.Problem = mirrorAxis < 0 ? $"Nested prefab {guid} is not in the package or was not converted; instance skipped."
					: $"Nested prefab {(found ? asset.Path : guid)} has no variant mirrored along {MirrorName( mirrorAxis )}; mirrored instance skipped.";
			else if ( isModel )
			{
				// An FBX carries no prefab YAML: its prefab is the model root at the identity, named after the
				// file, and every object an instance overrides is one of Unity's fixed model-prefab ids.
				source.Asset = asset;
				source.SboxPath = path;
				source.Key = asset.Guid.ToLowerInvariant();
				source.IsWrapper = true;
				source.RootTransformId = ModelRootTransform;
				source.RootObject = ModelRootObject;
				source.RootName = Path.GetFileNameWithoutExtension( asset.Path );
				source.RendererIds = Enumerable.Range( 0, asset.ModelInfo.RenderedMeshes.Length ).Select( ModelRendererId ).ToArray();
				// No objects of its own below the root: everything an instance addresses lives in the model.
				source.ById = new();
				source.Transforms = new();
				source.Chains = new();
				source.Parents = new();
				source.Ids = PrefabGuids( json );
			}
			else
			{
				source.Asset = asset;
				source.SboxPath = path;
				source.Key = mirrorAxis < 0 ? asset.Guid.ToLowerInvariant() : $"{asset.Guid.ToLowerInvariant()}:mirror{MirrorName( mirrorAxis )}";
				source.Blocks = UnityPrefabExport.Blocks( UnityImport.ReadText( asset.Source ) );
				source.Root = source.Blocks.FirstOrDefault( b => b.Type is "4" or "224" && !b.Stripped && IdOf( Field( b.Body, "m_Father" ) ) == 0 );
				// A model wrapper's root is the FBX model's root object, which the file names only through its
				// top-level PrefabInstance: that instance's overrides are what place, name and switch it.
				var wrapper = source.Root == null
					? source.Blocks.FirstOrDefault( b => b.Type == "1001" && IdOf( Field( b.Body, "m_TransformParent" ) ) == 0 ) : null;
				var wrapperModel = wrapper == null ? null : byGuid.GetValueOrDefault( GuidOf( Field( wrapper.Body, "m_SourcePrefab" ) ) );
				if ( source.Root == null && wrapperModel is not { Kind: UnityAssetKind.Model } )
					source.Problem = $"Nested prefab {asset.Path} has no root transform; instance skipped.";
				else
				{
					source.ById = source.Blocks.GroupBy( b => b.Id ).ToDictionary( g => g.Key, g => g.First() );
					if ( wrapperModel is { Kind: UnityAssetKind.Model } )
					{
						source.IsWrapper = true;
						source.RootTransformId = ModelRootTransform;
						var root = RootOf( wrapper, Path.GetFileNameWithoutExtension( wrapperModel.Path ) );
						source.RootTransform = root.Transform;
						source.RootName = root.Name;
						source.RootActive = root.Active;
						source.RootFlags = root.StaticFlags;
						source.RootObject = ModelRootObject;
						source.RootObjectBlock = source.Blocks.FirstOrDefault( b => b.Type == "1" && b.Stripped && Reference( b.Body, "m_CorrespondingSourceObject" ) == ModelRootObject );
						source.RendererIds = Enumerable.Range( 0, wrapperModel.ModelInfo.RenderedMeshes.Length ).Select( ModelRendererId ).ToArray();
					}
					else
					{
						source.RootObject = IdOf( Field( source.Root.Body, "m_GameObject" ) );
						source.RootObjectBlock = source.Blocks.FirstOrDefault( b => b.Id == source.RootObject );
						source.RootTransformId = source.Root.Id;
						source.RootTransform = Local( source.Root.Body );
						source.RootName = source.RootObjectBlock == null ? Path.GetFileNameWithoutExtension( asset.Path ) : Scalar( source.RootObjectBlock.Body, "m_Name" );
						source.RootActive = source.RootObjectBlock == null || Scalar( source.RootObjectBlock.Body, "m_IsActive" ) != "0";
						source.RootFlags = source.RootObjectBlock == null ? 0 : Number( source.RootObjectBlock.Body, "m_StaticEditorFlags" );
					}
					source.Transforms = source.Blocks.Where( b => b.Type is "4" or "224" && !b.Stripped ).GroupBy( b => IdOf( Field( b.Body, "m_GameObject" ) ) ).ToDictionary( g => g.Key, g => g.First() );
					// Objects of the source prefab whose placement its converted form moved into the model: an instance
					// override of their transform would fight the zeroing, so those overrides are dropped (see BakedChains).
					source.Chains = BakedObjects( source.Blocks, byGuid, out source.Parents );
					source.Ids = PrefabGuids( json );
				}
			}
			return sources[cacheKey] = source;
		}

		/// <summary>The GUID of an instance's root object in this file.</summary>
		public Guid InstanceGuid( long instanceId ) => StableGuid( $"{Key}:{instanceId}" );

		/// <summary>Path counts as one line, so a property over many objects is reported once with its count.</summary>
		static string Summary( Dictionary<string, int> counts ) =>
			string.Join( ", ", counts.OrderBy( c => c.Key, StringComparer.Ordinal ).Select( c => $"{c.Key} ({c.Value})" ) );

		/// <summary>What <see cref="ModelRoot"/> built: the root object, the models its renderers draw, those
		/// renderer components in the same order, which renderer a fixed id (2300000+2n) points at, and whether
		/// one model shows every mesh of the FBX.</summary>
		internal sealed record ModelParts( JsonObject Node, List<(string Path, string Mesh)> Renderers, JsonObject[] RendererNodes, int[] RendererOf, bool SharedModel );

		/// <summary>
		/// The root object of an FBX's generated prefab: one ModelRenderer per generated model of the meshes Unity
		/// renders. Unity keeps such a prefab implicitly for every model asset, so this builds both a model
		/// wrapper's root and the prefab a scene's direct model instance resolves through.
		/// <paramref name="extra"/> holds a wrapper file's own blocks, whose MeshFilters narrow the meshes it
		/// shows; it is null for a model with no prefab file. Unity numbers those objects 100000 (root) and
		/// 2300000+2n (renderers), which is what an instance's overrides address.
		/// </summary>
		public ModelParts ModelRoot( UnityAsset model, UnityTransform rootTransform, string name, bool active, double staticFlags, IReadOnlyList<Block> extra )
		{
			var meshes = model.ModelInfo.RenderedMeshes;
			var explicitMeshes = (extra ?? Array.Empty<Block>()).Where( b => !b.Stripped && b.Type == "23" )
				.Select( b => MeshName( model, IdOf( Field( b.Body, "m_Mesh" ) ) ) ).Where( m => m != null ).ToArray();
			if ( explicitMeshes.Length > 0 ) meshes = meshes.Where( m => explicitMeshes.Contains( m, StringComparer.OrdinalIgnoreCase ) ).ToArray();
			// One renderer per generated model. A model written per mesh (meshes whose Unity materials differ
			// inside one FBX slot) brings its own renderer each; otherwise one model shows every mesh it holds.
			var models = meshes.Select( m => Context.ModelFor( model, m ) ).ToArray();
			var renderers = new List<(string Path, string Mesh)>();
			var rendererOf = new int[meshes.Length];
			for ( var i = 0; i < meshes.Length; i++ )
			{
				rendererOf[i] = -1;
				if ( models[i] == null )
				{
					Result.Warnings.Add( $"{name}: no generated model for mesh '{meshes[i]}' of {model.Path}; its renderer was skipped." );
					continue;
				}
				var known = renderers.FindIndex( r => r.Path.Equals( models[i], StringComparison.OrdinalIgnoreCase ) );
				if ( known < 0 ) renderers.Add( (models[i], meshes[i]) );
				rendererOf[i] = known < 0 ? renderers.Count - 1 : known;
			}
			// Unity keeps a model prefab's root rotation and scale but places nothing: an instance's own
			// override supplies the position, exactly as it does for a wrapper prefab.
			var node = GameObject( StableGuid( $"{Key}:{ModelRootObject}" ), name,
				ToSbox( new UnityTransform( new double[3], rootTransform.Rotation, rootTransform.Scale ) ), active );
			if ( staticFlags != 0 ) node["Flags"] = StaticFlag;
			var components = (JsonArray)node["Components"];
			var nodes = new JsonObject[renderers.Count];
			for ( var i = 0; i < renderers.Count; i++ )
			{
				nodes[i] = ModelRenderer( StableGuid( $"{Key}:{ModelRendererId( i )}" ), renderers[i].Path, true );
				components.Add( nodes[i] );
			}
			return new( node, renderers, nodes, rendererOf, renderers.Count == 1 );
		}

		/// <summary>
		/// Converts a model wrapper: a prefab whose whole content is one top-level PrefabInstance of an FBX model,
		/// with Unity's model prefab stripped out of the file. The root is rebuilt from that instance, which places,
		/// names and switches the model's root object; it gets one ModelRenderer per generated model of the FBX's
		/// meshes, the colliders the wrapper adds to model objects, and the prefab instances nested under it.
		/// Unity numbers the objects of a model prefab 100000 (root), 400000 (its transform), then 2300000+2n and
		/// 4300000+2n for each node's renderer and mesh, and the overrides address exactly those ids.
		/// </summary>
		public JsonObject WrapperRoot()
		{
			var sourceGuid = GuidOf( Field( Wrapper.Body, "m_SourcePrefab" ) );
			if ( sourceGuid.Length == 0 ) throw new InvalidDataException( "Prefab has a top-level prefab instance with a missing prefab reference." );
			if ( !byGuid.TryGetValue( sourceGuid, out var model ) ) throw new InvalidDataException( $"Prefab has a top-level prefab instance with a missing prefab reference ({sourceGuid})." );
			// A top-level instance that sources another prefab is a prefab variant: the base would have to be
			// merged first, which this converter does not do.
			if ( model.Kind == UnityAssetKind.Prefab ) throw new InvalidDataException( "Prefab has no root transform (prefab variants are not supported)." );
			if ( model.Kind != UnityAssetKind.Model ) throw new InvalidDataException( $"Prefab's top-level prefab instance instances {model.Path}, which is not a model; the wrapper cannot be converted." );
			var root = RootOf( Wrapper, Path.GetFileNameWithoutExtension( model.Path ) );
			var parts = ModelRoot( model, root.Transform, root.Name, root.Active, root.StaticFlags, Blocks );
			var node = parts.Node;
			var renderers = parts.Renderers;
			var rendererOf = parts.RendererOf;
			var sharedModel = parts.SharedModel;
			var components = (JsonArray)node["Components"];
			var materials = new Dictionary<int, Dictionary<int, string>>();
			var subObject = new Dictionary<string, int>();
			var unknown = new Dictionary<string, int>();
			var unresolvedMaterials = 0;
			static string PathKey( string path ) => Regex.Replace( path, @"Array\.data\[[0-9]+\]", "Array.data[i]" );
			static string ComponentName( string type ) => type switch { "23" => "MeshRenderer", "33" => "MeshFilter", "95" => "Animator", "20" => "Camera", "108" => "Light", _ => $"Unity type {type}" };
			foreach ( Match modification in ModificationPattern.Matches( Wrapper.Body ) )
			{
				var target = IdOf( modification.Groups["target"].Value );
				var path = modification.Groups["path"].Value.Trim( '\'' );
				var slot = Regex.Match( path, @"^m_Materials\.Array\.data\[([0-9]+)\]$" );
				// The root object's namesake paths are already applied (see RootOf); hints have no counterpart.
				if ( target == ModelRootObject || target == ModelRootTransform || path.StartsWith( "m_LocalEulerAnglesHint" ) || path == "m_RootOrder" ) continue;
				var meshIndex = IsModelRenderer( target ) ? (int)((target - 2300000) / 2) : -1;
				if ( meshIndex >= 0 && slot.Success )
				{
					var unitySlot = int.Parse( slot.Groups[1].Value, CultureInfo.InvariantCulture );
					var assigned = GuidOf( modification.Groups["reference"].Value );
					var renderer = meshIndex < rendererOf.Length ? rendererOf[meshIndex] : -1;
					var binding = renderer < 0 ? null : UnityPrefabBindings.WrapperRenderer( model, target, out _ );
					// One model: the slot is its own material-list position. A model per mesh: the mesh's own slot.
					var materialSlot = binding == null ? -1 : (sharedModel ? UnityPrefabBindings.WrapperMaterialSlot( model, binding, unitySlot ) ?? -1 : unitySlot);
					var material = materialSlot < 0 ? null : Context.MaterialFor?.Invoke( assigned );
					if ( material == null ) { unresolvedMaterials++; continue; }
					if ( !materials.TryGetValue( renderer, out var map ) ) materials[renderer] = map = new();
					map[materialSlot] = material;
					continue;
				}
				// A model object other than the root: its placement and components are the model's own.
				if ( meshIndex >= 0 || target >= 100000 && target < 300000 || target >= 400000 && target < 500000 ) subObject[PathKey( path )] = subObject.GetValueOrDefault( PathKey( path ) ) + 1;
				else unknown[PathKey( path )] = unknown.GetValueOrDefault( PathKey( path ) ) + 1;
			}
			for ( var i = 0; i < renderers.Count; i++ )
			{
				if ( !materials.TryGetValue( i, out var map ) || map.Count == 0 ) continue;
				var indexed = new JsonObject();
				foreach ( var (slot, material) in map.OrderBy( m => m.Key ) ) indexed[slot.ToString( CultureInfo.InvariantCulture )] = material;
				parts.RendererNodes[i]["Materials"] = new JsonObject { ["indexed"] = indexed };
			}

			// The wrapper's own components sit on model objects. A collider on a sub-object still covers the whole
			// model (its shape list is generated from the collided mesh), so it is added to the root.
			var meshColliders = 0;
			var subObjectColliders = 0;
			var colliderFallbacks = 0;
			var reportedRotation = false;
			foreach ( var component in Blocks.Where( b => !b.Stripped && b.Type is "64" or "65" or "135" or "136" ) )
			{
				var id = StableGuid( $"{Key}:{component.Id}" );
				var enabled = Scalar( component.Body, "m_Enabled" ) != "0";
				var owner = ById.GetValueOrDefault( IdOf( Field( component.Body, "m_GameObject" ) ) );
				if ( owner != null && Reference( owner.Body, "m_CorrespondingSourceObject" ) != ModelRootObject ) subObjectColliders++;
				if ( component.Type == "64" )
				{
					meshColliders++;
					// Several Unity colliders (e.g. one convex piece each) map to the model's one hull.
					if ( meshColliders > 1 ) continue;
					var meshName = MeshName( model, IdOf( Field( component.Body, "m_Mesh" ) ) );
					var collider = meshName == null ? null : Context.ModelFor( model, meshName );
					if ( collider == null && renderers.Count > 0 ) { collider = renderers[0].Path; colliderFallbacks++; }
					if ( collider == null ) { Result.Warnings.Add( $"{root.Name}: a MeshCollider's mesh is not a converted model; the collider was skipped." ); continue; }
					components.Add( ModelCollider( id, collider, enabled ) );
				}
				else if ( PrimitiveCollider( component, id, enabled, null, root.Name, ref reportedRotation ) is JsonObject collider ) components.Add( collider );
			}

			// Prefabs nested under the model. Unity hangs them off the model's objects, whose placement the model
			// carries, so they attach to the root; their own overrides place them.
			var children = (JsonArray)node["Children"];
			foreach ( var nested in Instances.Values.Where( i => i.Id != Wrapper.Id ).OrderBy( i => i.Id ) )
			{
				var parentTransform = ById.GetValueOrDefault( IdOf( Field( nested.Body, "m_TransformParent" ) ) );
				var parentObject = parentTransform == null ? 0 : Reference( parentTransform.Body, "m_CorrespondingSourceObject" );
				if ( parentObject != 0 && parentObject != ModelRootObject )
					Result.Information.Add( $"{root.Name}: a nested prefab instance hangs under model object {parentObject}; that placement lives in the model, so the instance is attached to the wrapper root." );
				if ( Instance( nested, null ) is JsonObject child ) children.Add( child );
			}

			if ( subObject.Count > 0 ) Result.Information.Add( $"{root.Name}: overrides of model sub-objects are part of the model and were not applied again: {Summary( subObject )}." );
			if ( unknown.Count > 0 ) Result.Information.Add( $"{root.Name}: prefab overrides not converted: {Summary( unknown )}." );
			if ( unresolvedMaterials > 0 ) Result.Information.Add( $"{root.Name}: {unresolvedMaterials} material override(s) could not be mapped to a generated model slot and were not converted." );
			if ( meshColliders > 1 ) Result.Information.Add( $"{root.Name}: {meshColliders} MeshColliders on the model are represented by one s&box ModelCollider, which collides with the whole model." );
			if ( subObjectColliders > 0 ) Result.Information.Add( $"{root.Name}: {subObjectColliders} collider(s) sit on model sub-objects; the generated collider covers the whole model, so their local offset is not applied again." );
			if ( colliderFallbacks > 0 ) Result.Information.Add( $"{root.Name}: {colliderFallbacks} MeshCollider(s) name a mesh with no generated model; their collider uses the model's own shapes instead." );
			// Anything else the wrapper adds to a model object has to be named too, or it is dropped silently.
			var other = Blocks.Where( b => !b.Stripped && b.Type is not ("1" or "4" or "224" or "1001" or "64" or "65" or "135" or "136" or "114") )
				.GroupBy( b => ComponentName( b.Type ) ).OrderBy( g => g.Key, StringComparer.Ordinal ).Select( g => $"{g.Count()} {g.Key}" ).ToArray();
			if ( other.Length > 0 ) Result.Information.Add( $"{root.Name}: components on the model that s&box has no counterpart for were not converted: {string.Join( ", ", other )}." );
			// Scripts cannot be converted, and nothing on the wrapper may be dropped without a line in the report.
			var scripts = Blocks.Count( b => !b.Stripped && b.Type == "114" );
			if ( scripts > 0 ) Result.Information.Add( $"{root.Name}: {scripts} MonoBehaviour (script) component(s) attached to the model are not converted." );
			var addedComponents = Regex.Matches( Section( Wrapper.Body, "m_AddedComponents" ), @"(?m)^\s*-\s" ).Count;
			if ( addedComponents > 0 ) Result.Information.Add( $"{root.Name}: m_AddedComponents lists {addedComponents} component(s) added to the model; they are not converted." );
			return node;
		}

		/// <summary>An instance's name as Unity shows it: its m_Name override, else the source prefab's root name or file name.</summary>
		public string InstanceName( Block instance )
		{
			var source = SourceOf( GuidOf( Field( instance.Body, "m_SourcePrefab" ) ) );
			if ( source.Problem != null ) return byGuid.TryGetValue( GuidOf( Field( instance.Body, "m_SourcePrefab" ) ), out var asset ) ? Path.GetFileNameWithoutExtension( asset.Path ) : instance.Id.ToString( CultureInfo.InvariantCulture );
			var name = string.IsNullOrEmpty( source.RootName ) ? Path.GetFileNameWithoutExtension( source.Asset.Path ) : source.RootName;
			var renamed = Regex.Match( instance.Body, @"(?ms)^\s*- target:\s*\{fileID:\s*" + source.RootObject + @"\b[^}]*\}\s*\r?\n\s*propertyPath:\s*m_Name\s*\r?\n\s*value:[ \t]*([^\r\n]*?)[ \t]*\r?$" );
			return renamed.Success ? Unquote( renamed.Groups[1].Value ) : name;
		}

		/// <summary>A Box/Sphere/Capsule collider component, or null for any other block. <paramref name="chain"/> is the
		/// Unity placement the owning object lost to its model, which the shape must keep; <paramref name="mirror"/>
		/// the mirror the object's own frame still owes (see <see cref="Unmirror"/>).</summary>
		public JsonObject PrimitiveCollider( Block component, Guid id, bool enabled, UnityTransform chain, string owner, ref bool reportedRotation, int[] mirror = null )
		{
			if ( component.Type is not ("65" or "135" or "136") ) return null;
			mirror ??= NoMirror;
			// Primitive colliders: centre and size are in the object's local space, like s&box's — unless the
			// object's placement moved into its model, then the collider keeps it via the chain instead.
			var trigger = Scalar( component.Body, "m_IsTrigger" ) == "1";
			var center = Vector( component.Body, "m_Center", "x", "y", "z" );
			if ( component.Type == "65" )
			{
				if ( chain != null && AxisPermutation( chain.Rotation ) == null && !reportedRotation )
				{
					reportedRotation = true;
					Result.Information.Add( $"{owner}: box collider on a baked child is rotated; s&box BoxCollider has no rotation, so its size is the rotated box's bounds." );
				}
				return BoxCollider( id, Vector( component.Body, "m_Size", "x", "y", "z" ), center, trigger, enabled, chain, mirror );
			}
			if ( component.Type == "135" ) return SphereCollider( id, Number( component.Body, "m_Radius" ), center, trigger, enabled, chain, mirror );
			var direction = (int)Number( component.Body, "m_Direction" );
			return CapsuleCollider( id, Number( component.Body, "m_Radius" ), Number( component.Body, "m_Height" ), direction is >= 0 and < 3 ? direction : 1, center, trigger, enabled, chain, mirror );
		}

		/// <summary>
		/// A MeshRenderer's own material assignments as the indexed overrides a ModelRenderer takes: Unity's slot i
		/// is the mesh's i-th submesh material, which the generated model carries at the position of the FBX slot
		/// it names (see <see cref="UnityPrefabBindings.WrapperMaterialSlot"/>). A slot with no converted material
		/// stays unset, so the model's own assignment shows there, and is reported once for the object.
		/// </summary>
		JsonObject MaterialSlots( Block renderer, UnityAsset meshAsset, string meshNode, string owner )
		{
			var references = Regex.Matches( Section( renderer.Body, "m_Materials" ), @"\{([^}]*)\}" ).Select( m => GuidOf( m.Value ) ).ToArray();
			if ( references.Length == 0 ) return null;
			var binding = meshAsset == null || meshNode == null ? null
				: meshAsset.ModelInfo.RendererSlots.Values.FirstOrDefault( b => b.Mesh.Equals( meshNode, StringComparison.OrdinalIgnoreCase ) );
			var indexed = new JsonObject();
			var unresolved = 0;
			for ( var slot = 0; slot < references.Length; slot++ )
			{
				var material = references[slot].Length == 0 ? null : Context.MaterialFor?.Invoke( references[slot] );
				var position = binding == null ? null : UnityPrefabBindings.WrapperMaterialSlot( meshAsset, binding, slot );
				if ( material == null || position == null ) { unresolved++; continue; }
				indexed[position.Value.ToString( CultureInfo.InvariantCulture )] = material;
			}
			if ( unresolved > 0 ) Result.Information.Add( $"{owner}: {unresolved} of {references.Length} material slot(s) have no converted material; the model's own material is used there." );
			if ( indexed.Count == 0 ) return null;
			return new JsonObject { ["indexed"] = indexed };
		}

		/// <summary>
		/// One GameObject with its components and children. <paramref name="isRoot"/> is a prefab root, whose position
		/// is zeroed because an instance replaces it; a scene root keeps its full transform. <paramref name="inherited"/>:
		/// a baked parent emitted an identity, so its non-baked children fold that placement into their own local
		/// transform to stay where Unity has them. <paramref name="pending"/>: the mirror the parent still owes this
		/// object (see <see cref="Unmirror"/>); null for none.
		/// </summary>
		public JsonObject Node( Block transform, bool isRoot, UnityTransform inherited = null, int[] pending = null )
		{
			pending ??= isRoot ? RootMirror : NoMirror;
			var objectId = IdOf( Field( transform.Body, "m_GameObject" ) );
			var go = ById.GetValueOrDefault( objectId );
			var name = go == null ? "GameObject" : Scalar( go.Body, "m_Name" );
			var active = go == null || Scalar( go.Body, "m_IsActive" ) != "0";
			var unityLocal = Local( transform.Body );
			// A baked object loses its transform to its model (see BakedChains); its chain is what the object
			// keeps carrying through its primitive colliders, children, and nested instances.
			var chain = Chains.GetValueOrDefault( objectId );
			// The prefab root keeps Unity's rotation and scale, which Unity also keeps when an instance is placed.
			// Its position stays zero: s&box adds a prefab root's position to wherever the prefab is placed,
			// whereas Unity replaces it, so baking it in would offset every placement.
			UnityTransform local;
			if ( isRoot ) local = new UnityTransform( new double[3], unityLocal.Rotation, unityLocal.Scale );
			else if ( chain != null ) local = NoTransform;
			else local = inherited == null ? unityLocal : Compose( inherited, unityLocal );
			var (emitted, owed) = Unmirror( local, pending, chain != null );
			var node = GameObject( StableGuid( $"{Key}:{objectId}" ), name, ToSbox( emitted ), active );
			// Any of Unity's static flags (lightmap, occluder, batching, navigation …) says the object never moves;
			// s&box has the one flag for that, inherited by children.
			if ( go != null && Number( go.Body, "m_StaticEditorFlags" ) != 0 ) node["Flags"] = StaticFlag;
			var tags = new List<string>();
			if ( go != null && Scalar( go.Body, "m_TagString" ) == "MainCamera" ) tags.Add( "maincamera" );
			var list = (JsonArray)node["Components"];
			string model = null;
			// The FBX and mesh the object's renderer draws, for the material slots it assigns.
			UnityAsset meshSource = null;
			string meshNode = null;
			foreach ( var component in Components.GetValueOrDefault( objectId ) ?? Array.Empty<Block>() )
			{
				// A SkinnedMeshRenderer on an unskinned mesh (blend shapes only) is a plain renderer here.
				if ( component.Type == "33" || component.Type == "137" && !skinned.FoldedRenderers.Contains( component.Id ) )
				{
					var mesh = Field( component.Body, "m_Mesh" );
					if ( !byGuid.TryGetValue( GuidOf( mesh ), out var source ) || source.Kind != UnityAssetKind.Model )
					{
						Result.Warnings.Add( $"{name}: mesh {GuidOf( mesh )} is not a converted model; renderer skipped." );
						continue;
					}
					var meshName = MeshName( source, IdOf( mesh ) );
					model = meshName == null ? null : ModelThrough( owed, source, meshName, Context.ModelFor( source, meshName ) );
					if ( model == null ) Result.Warnings.Add( $"{name}: no generated model for mesh '{meshName ?? IdOf( mesh ).ToString( CultureInfo.InvariantCulture )}' of {source.Path}; renderer skipped." );
					else { meshSource = source; meshNode = meshName; }
				}
			}
			var reportedRotation = false;
			foreach ( var component in Components.GetValueOrDefault( objectId ) ?? Array.Empty<Block>() )
			{
				var enabled = Scalar( component.Body, "m_Enabled" ) != "0";
				var id = StableGuid( $"{Key}:{component.Id}" );
				if ( component.Type is "23" or "137" && model != null && !skinned.FoldedRenderers.Contains( component.Id ) )
				{
					var renderer = ModelRenderer( id, model, enabled );
					if ( ConvertsRendererMaterials && MaterialSlots( component, meshSource, meshNode, name ) is JsonObject assigned ) renderer["Materials"] = assigned;
					list.Add( renderer );
				}
				if ( component.Type == "95" ) Result.Information.Add( $"{name}: Animator not converted; s&box animates a SkinnedModelRenderer through an animation graph or sequences." );
				// Unity bakes MeshCollider shapes into separate assets; the generated model carries the
				// equivalent hull, so the collider points at the same model. Several Unity colliders
				// (e.g. one per baked convex piece) all become that one hull, so only the first is kept.
				if ( component.Type == "64" && model != null && !list.Any( c => c?["__type"]?.GetValue<string>() == "Sandbox.ModelCollider" ) )
					list.Add( ModelCollider( id, model, enabled ) );
				if ( component.Type == "64" && model == null ) Result.Warnings.Add( $"{name}: MeshCollider without a converted mesh on the same object was skipped." );
				if ( PrimitiveCollider( component, id, enabled, chain, name, ref reportedRotation, owed ) is JsonObject collider ) list.Add( collider );
				if ( component.Type == "20" ) list.Add( UnitySceneExport.Camera( id, component.Body, enabled, component.Id == mainCamera ) );
				if ( component.Type == "81" ) list.Add( UnitySceneExport.AudioListener( id, enabled ) );
				if ( component.Type == "108" )
				{
					var light = UnitySceneExport.Light( id, component.Body, enabled, out var tag );
					if ( light == null ) Result.Information.Add( $"{name}: area light not converted; s&box has no rectangle or disc lights." );
					else { list.Add( light ); tags.Add( tag ); }
				}
			}
			if ( tags.Count > 0 ) node["Tags"] = string.Join( ",", tags );
			foreach ( var (renderer, skinnedModel, skinnedMesh) in skinned.Components.GetValueOrDefault( objectId ) ?? new List<(JsonObject, UnityAsset, string)>() )
			{
				var copy = (JsonObject)renderer.DeepClone();
				copy["Model"] = ModelThrough( owed, skinnedModel, skinnedMesh, renderer["Model"]?.GetValue<string>() );
				list.Add( copy );
			}
			var children = (JsonArray)node["Children"];
			foreach ( Match child in Regex.Matches( Regex.Match( transform.Body, @"(?ms)^\s*m_Children:(.*?)(?=^\s*m_Father:)" ).Groups[1].Value, @"fileID:\s*(-?[0-9]+)" ) )
			{
				var childId = long.Parse( child.Groups[1].Value, CultureInfo.InvariantCulture );
				if ( StrippedInstance.TryGetValue( childId, out var instanceId ) && Instances.TryGetValue( instanceId, out var instance ) )
				{
					if ( Instance( instance, chain, owed ) is JsonObject nested ) children.Add( nested );
				}
				else if ( ById.TryGetValue( childId, out var childTransform ) && !childTransform.Stripped && !skinned.DroppedObjects.Contains( IdOf( Field( childTransform.Body, "m_GameObject" ) ) ) )
					children.Add( Node( childTransform, false, chain, owed ) );
			}
			return node;
		}

		/// <summary>
		/// A prefab instance as an s&amp;box stub: the source prefab's path, Position/Rotation/Scale/Name/Enabled
		/// overrides, material and child-object overrides, and the id map every node of the source needs.
		/// <paramref name="prepend"/>: the baked parent this instance hangs under emitted an identity, so the
		/// instance root must fold that placement into its own transform override to stay where Unity has it.
		/// <paramref name="pending"/>: the mirror the parent still owes the instance. An instance whose transform
		/// mirrors (a negative scale) points at the source prefab's mirrored variant with a positive scale.
		/// Returns null (with a warning) when the source prefab was not converted.
		/// </summary>
		public JsonObject Instance( Block instance, UnityTransform prepend, int[] pending = null )
		{
			pending ??= NoMirror;
			var sourceGuid = GuidOf( Field( instance.Body, "m_SourcePrefab" ) );
			var plain = SourceOf( sourceGuid );
			if ( plain.Problem != null )
			{
				Result.Warnings.Add( plain.Problem );
				return null;
			}
			var sourceRootObject = plain.RootObject;
			// The root's own values, which the instance's overrides replace; copied so editing them cannot reach
			// the cached source prefab.
			var unity = new UnityTransform( (double[])plain.RootTransform.Position.Clone(), (double[])plain.RootTransform.Rotation.Clone(), (double[])plain.RootTransform.Scale.Clone() );
			string name = plain.RootName;
			var active = plain.RootActive;
			var skipped = new List<string>();
			var sourceById = plain.ById;
			var sourceChains = plain.Chains;
			var sourceParents = plain.Parents;
			var componentOverrides = new List<(long Target, string Property, JsonNode Value)>();
			// Overrides on objects below the nested root, keyed by the source GameObject's fileID.
			var childTransforms = new Dictionary<long, UnityTransform>();
			var childNames = new Dictionary<long, string>();
			var childActive = new Dictionary<long, bool>();
			foreach ( Match modification in Regex.Matches( instance.Body, @"(?ms)^\s*- target:\s*\{(?<target>[^}]+)\}\s*\r?\n\s*propertyPath:\s*(?<path>[^\r\n]+?)\s*\r?\n\s*value:[ \t]*(?<value>[^\r\n]*?)[ \t]*\r?$(?:\r?\n\s*objectReference:\s*\{(?<reference>[^}]*)\})?" ) )
			{
				var target = IdOf( modification.Groups["target"].Value );
				var path = modification.Groups["path"].Value.Trim( '\'' );
				var value = Unquote( modification.Groups["value"].Value );
				double Parse() => double.TryParse( value, NumberStyles.Float, CultureInfo.InvariantCulture, out var v ) ? v : 0;
				var axis = path.Length > 2 && path[^2] == '.' ? "xyzw".IndexOf( path[^1] ) : -1;
				var component = sourceById.GetValueOrDefault( target );
				var slot = Regex.Match( path, @"^m_Materials\.Array\.data\[([0-9]+)\]$" );
				// A wrapper's own objects have no block in its file: Unity addresses them by an id derived from
				// the instance, which the file never contains. Its one model root takes those transform
				// overrides, and an object override names a stripped object, whose source is the model's root.
				var targetObject = component is { Type: "1", Stripped: true } ? Reference( component.Body, "m_CorrespondingSourceObject" )
					: plain.IsWrapper && component == null ? sourceRootObject : target;
				var rootTransform = target == plain.RootTransformId
					|| plain.IsWrapper && !plain.ById.ContainsKey( target ) && path.StartsWith( "m_Local" ) && !path.StartsWith( "m_LocalEulerAnglesHint" );
				if ( rootTransform && path.StartsWith( "m_LocalPosition." ) && axis is >= 0 and < 3 ) unity.Position[axis] = Parse();
				else if ( rootTransform && path.StartsWith( "m_LocalRotation." ) && axis >= 0 ) unity.Rotation[axis] = Parse();
				else if ( rootTransform && path.StartsWith( "m_LocalScale." ) && axis is >= 0 and < 3 ) unity.Scale[axis] = Parse();
				// Editor-only hints: euler display angles and sibling order (children keep Unity's order).
				else if ( path.StartsWith( "m_LocalEulerAnglesHint" ) || path == "m_RootOrder" ) { }
				else if ( targetObject == sourceRootObject && path == "m_Name" ) name = value;
				else if ( targetObject == sourceRootObject && path == "m_IsActive" ) active = value != "0";
				else if ( component is { Type: "4" or "224", Stripped: false } && axis >= 0
					&& (path.StartsWith( "m_LocalPosition." ) && axis < 3 || path.StartsWith( "m_LocalRotation." ) || path.StartsWith( "m_LocalScale." ) && axis < 3) )
				{
					var objectId = IdOf( Field( component.Body, "m_GameObject" ) );
					if ( !childTransforms.TryGetValue( objectId, out var local ) ) childTransforms[objectId] = local = Local( component.Body );
					(path.StartsWith( "m_LocalPosition." ) ? local.Position : path.StartsWith( "m_LocalRotation." ) ? local.Rotation : local.Scale)[axis] = Parse();
				}
				else if ( component is { Type: "1", Stripped: false } && path == "m_Name" ) childNames[target] = value;
				else if ( component is { Type: "1", Stripped: false } && path == "m_IsActive" ) childActive[target] = value != "0";
				else if ( component is { Type: "23" or "64" or "65" or "135" or "136" } && path == "m_Enabled" )
				{
					// An override equal to the source value changes nothing.
					if ( (value != "0") != (Scalar( component.Body, "m_Enabled" ) != "0") ) componentOverrides.Add( (target, "__enabled", value != "0") );
				}
				else if ( component is { Type: "23" } && slot.Success )
				{
					var index = int.Parse( slot.Groups[1].Value, CultureInfo.InvariantCulture );
					var assigned = GuidOf( modification.Groups["reference"].Value );
					var current = Regex.Matches( component.Body.Split( "m_Materials:" ).ElementAtOrDefault( 1 )?.Split( "m_StaticBatchInfo" )[0] ?? "", @"\{[^}]*\}" )
						.Select( m => GuidOf( m.Value ) ).ToArray();
					if ( index < current.Length && current[index].Equals( assigned, StringComparison.OrdinalIgnoreCase ) ) continue;
					// A Unity renderer slot is a submesh; it is the model's material index only when the mesh has one material.
					if ( current.Length != 1 || Context.MaterialFor?.Invoke( assigned ) is not string material ) { skipped.Add( path ); continue; }
					componentOverrides.Add( (target, "Materials", new JsonObject { ["indexed"] = new JsonObject { [index.ToString( CultureInfo.InvariantCulture )] = material } }) );
				}
				// A renderer's enabled flag is the one other property an instance switches by fixed model id. The
				// target must be a renderer id: the single-renderer fallback the material branch uses would put an
				// override meant for another component onto the wrapper's only renderer.
				else if ( plain.IsWrapper && path == "m_Enabled" && IsModelRenderer( target ) )
					componentOverrides.Add( (target, "__enabled", value != "0") );
				else if ( plain.IsWrapper && slot.Success && (IsModelRenderer( target ) || plain.RendererIds.Length == 1) )
				{
					// A wrapper's renderers keep Unity's model-prefab ids, so the override names the generated
					// component directly; a file that only addresses it indirectly falls back to the one renderer.
					var rendererId = IsModelRenderer( target ) ? target : plain.RendererIds[0];
					var index = int.Parse( slot.Groups[1].Value, CultureInfo.InvariantCulture );
					var assigned = GuidOf( modification.Groups["reference"].Value );
					var binding = UnityPrefabBindings.WrapperRenderer( plain.Asset, rendererId, out _ );
					var slotName = binding?.EffectiveSlots.ElementAtOrDefault( index );
					// An override equal to the source value changes nothing.
					var assignedByModel = slotName == null ? null
						: plain.Asset.ModelInfo.MeshSlotMaterials.TryGetValue( binding.Mesh, out var meshSlots ) && meshSlots.TryGetValue( slotName, out var perMesh ) ? perMesh
						: plain.Asset.ModelInfo.PrefabSlotMaterials.GetValueOrDefault( slotName );
					if ( assignedByModel != null && assignedByModel.Equals( assigned, StringComparison.OrdinalIgnoreCase ) ) { }
					else
					{
						var materialSlot = binding == null ? (int?)null : UnityPrefabBindings.WrapperMaterialSlot( plain.Asset, binding, index );
						var material = materialSlot == null ? null : Context.MaterialFor?.Invoke( assigned );
						if ( material == null ) skipped.Add( path );
						else componentOverrides.Add( (rendererId, "Materials", new JsonObject { ["indexed"] = new JsonObject { [materialSlot.Value.ToString( CultureInfo.InvariantCulture )] = material } }) );
					}
				}
				// s&box keeps one static flag per object, and an instance root carries the source prefab's.
				else if ( targetObject == sourceRootObject && path == "m_StaticEditorFlags" && Parse() == plain.RootFlags ) { }
				else skipped.Add( path );
			}
			if ( skipped.Count > 0 ) Result.Information.Add( $"{name}: nested prefab overrides not converted: {string.Join( ", ", skipped.Distinct() )}." );
			if ( !ConvertsAdditions && (Regex.IsMatch( instance.Body, @"(?m)^\s*m_RemovedComponents:\s*\r?\n\s*-" ) || Regex.IsMatch( instance.Body, @"(?m)^\s*m_AddedGameObjects:\s*\r?\n\s*-" )) )
				Result.Information.Add( $"{name}: added or removed objects/components on the nested prefab instance are not converted." );

			// A mirrored placement keeps a positive scale and points at the prefab seen through that mirror.
			var (emitted, owed) = Unmirror( prepend == null ? unity : Compose( prepend, unity ), pending, false );
			var source = IsMirror( owed ) ? SourceOf( sourceGuid, UnityPrefabExport.MirrorAxis( owed ) ) : plain;
			if ( source.Problem != null )
			{
				Result.Warnings.Add( $"{name}: {source.Problem}" );
				return null;
			}
			var sourceKey = source.Key;
			var instanceGuid = InstanceGuid( instance.Id );
			var sourceIds = source.Ids;
			var map = new JsonObject();
			foreach ( var id in sourceIds )
				map[id.ToString()] = (id == sourceIds[0] ? instanceGuid : StableGuid( $"{Key}:{instance.Id}:{id}" )).ToString();
			var transform = ToSbox( emitted );
			var overrides = new JsonArray();
			void Override( string property, JsonNode value ) => overrides.Add( new JsonObject
			{
				["Target"] = new JsonObject { ["Type"] = "GameObject", ["IdValue"] = sourceIds[0].ToString() },
				["Property"] = property,
				["Value"] = value
			} );
			Override( "Position", Join( transform.Position ) );
			Override( "Rotation", Join( transform.Rotation ) );
			// Overrides replace the source prefab's root values; the source root keeps Unity's scale (made positive, see
			// Unmirror), so an instance needs a Scale override whenever its scale differs from that, including back to 1.
			var sourceScale = Scale( plain.RootTransform.Scale.Select( Math.Abs ).ToArray() );
			if ( transform.Scale.Zip( sourceScale ).Any( s => Math.Abs( s.First - s.Second ) > 1e-6 ) ) Override( "Scale", Join( transform.Scale ) );
			Override( "Name", name );
			if ( !active ) Override( "Enabled", false );
			void ChildOverride( long objectId, string property, JsonNode value )
			{
				var id = StableGuid( $"{sourceKey}:{objectId}" );
				if ( !sourceIds.Contains( id ) ) { Result.Information.Add( $"{name}: override of {property} targets an object the source prefab did not convert." ); return; }
				overrides.Add( new JsonObject
				{
					["Target"] = new JsonObject { ["Type"] = "GameObject", ["IdValue"] = id.ToString() },
					["Property"] = property,
					["Value"] = value
				} );
			}
			foreach ( var (objectId, local) in childTransforms )
			{
				// The converted source prefab zeroed this object's transform because its model carries the
				// placement; a Position/Rotation/Scale override would re-apply it on top of the model.
				if ( sourceChains.GetValueOrDefault( objectId ) != null )
				{
					var bakedName = sourceById.GetValueOrDefault( objectId ) is { Type: "1" } bakedGo ? Scalar( bakedGo.Body, "m_Name" ) : objectId.ToString( CultureInfo.InvariantCulture );
					Result.Information.Add( $"{name}: transform override of {bakedName} not converted; the source prefab bakes that object's placement into its model." );
					continue;
				}
				// Under a baked parent the source prefab folded the parent's chain into this object's local
				// transform; an override replaces that value, so it must carry the chain too. The object is
				// emitted through the mirror its ancestors in the source prefab left it (see Unmirror).
				var parentChain = sourceChains.GetValueOrDefault( sourceParents.GetValueOrDefault( objectId ) );
				var (childEmitted, childOwed) = Unmirror( parentChain == null ? local : Compose( parentChain, local ), source.PendingIn( objectId ), false );
				if ( !childOwed.SequenceEqual( source.PendingOut( objectId ) ) )
					Result.Information.Add( $"{name}: the scale override of {source.NameOf( objectId )} mirrors it; its children keep the source prefab's orientation." );
				var childLocal = ToSbox( childEmitted );
				ChildOverride( objectId, "Position", Join( childLocal.Position ) );
				ChildOverride( objectId, "Rotation", Join( childLocal.Rotation ) );
				ChildOverride( objectId, "Scale", Join( childLocal.Scale ) );
			}
			foreach ( var (objectId, childName) in childNames ) ChildOverride( objectId, "Name", childName );
			foreach ( var (objectId, childEnabled) in childActive ) ChildOverride( objectId, "Enabled", childEnabled );
			foreach ( var (target, property, value) in componentOverrides )
			{
				// Components are identified by the GUID the source prefab's conversion gave them.
				var id = StableGuid( $"{sourceKey}:{target}" );
				if ( !sourceIds.Contains( id ) ) { Result.Information.Add( $"{name}: override of {property} targets a component the source prefab did not convert." ); continue; }
				overrides.Add( new JsonObject
				{
					["Target"] = new JsonObject { ["Type"] = "Component", ["IdValue"] = id.ToString() },
					["Property"] = property,
					["Value"] = value
				} );
			}
			var stub = new JsonObject
			{
				["__guid"] = instanceGuid.ToString(),
				["__version"] = 2,
				["__Prefab"] = source.SboxPath,
				["__PrefabInstancePatch"] = new JsonObject
				{
					["AddedObjects"] = new JsonArray(),
					["RemovedObjects"] = new JsonArray(),
					["PropertyOverrides"] = overrides,
					["MovedObjects"] = new JsonArray()
				},
				["__PrefabIdToInstanceId"] = map
			};
			Stubs[instance.Id] = stub;
			StubMirror[instance.Id] = source.MirrorAxis;
			return stub;
		}

		/// <summary>An instance root's Unity local transform: the source root's, with the instance's own overrides.
		/// What <see cref="Instance"/> writes as the Position/Rotation/Scale overrides, before the axis change.</summary>
		public UnityTransform InstanceRootTransform( Block instance, Source source )
		{
			// A direct model instance has no prefab file to read a root from: its root is the model's own
			// transform, which Instance() overrides. Copied, because the source's transform is shared state.
			if ( source.Root == null )
				return new UnityTransform( (double[])source.RootTransform.Position.Clone(), (double[])source.RootTransform.Rotation.Clone(), (double[])source.RootTransform.Scale.Clone() );
			var unity = Local( source.Root.Body );
			foreach ( Match modification in Regex.Matches( instance.Body, @"(?ms)^\s*- target:\s*\{fileID:\s*" + source.Root.Id + @"\b[^}]*\}\s*\r?\n\s*propertyPath:\s*m_Local(?<field>Position|Rotation|Scale)\.(?<axis>[xyzw])\s*\r?\n\s*value:[ \t]*(?<value>[^\r\n]*?)[ \t]*\r?$" ) )
			{
				var axis = "xyzw".IndexOf( modification.Groups["axis"].Value[0] );
				var values = modification.Groups["field"].Value switch { "Position" => unity.Position, "Rotation" => unity.Rotation, _ => unity.Scale };
				if ( axis < values.Length && double.TryParse( modification.Groups["value"].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var v ) ) values[axis] = v;
			}
			return unity;
		}
	}

	/// <summary>
	/// Converts one prefab, or (<paramref name="mirrorAxis"/> 0..2) its variant mirrored along a Unity axis, for
	/// instances placed with a negative scale. Nested prefab instances need the converted JSON of their source
	/// prefab (<see cref="Context.Converted"/>); callers convert sources first (see <see cref="Order"/>).
	/// </summary>
	internal static Result Convert( UnityAsset prefab, Context context, int mirrorAxis = -1 )
	{
		var result = new Result();
		var text = UnityImport.ReadText( prefab.Source );
		var converter = new Converter( prefab, text, context, result, mirrorAxis );
		var root = converter.Roots.FirstOrDefault();
		// A model wrapper has no Transform block at all: its whole content is one PrefabInstance of an FBX
		// model, which WrapperRoot rebuilds. Anything else without a root is an unsupported prefab variant.
		JsonObject rootNode;
		if ( root != null ) rootNode = converter.Node( root, true );
		else if ( converter.Wrapper != null ) rootNode = converter.WrapperRoot();
		else throw new InvalidDataException( "Prefab has no root transform (prefab variants are not supported)." );
		result.Json = Document( rootNode );
		return result;
	}

	/// <summary>
	/// The generated prefab of an FBX that a scene instances directly, with no .prefab asset in between.
	/// Unity keeps an implicit prefab for every model asset; this rebuilds it with the same builder a model
	/// wrapper uses (<see cref="Converter.ModelRoot"/>), so a scene's instance resolves through the same
	/// <see cref="Converter.SourceOf"/> path and its overrides address the same fixed model-prefab ids.
	/// </summary>
	internal static Result ConvertModel( UnityAsset model, Context context )
	{
		var result = new Result();
		// An FBX has no YAML of its own: the converter reads the model's fixed ids and nothing else.
		var converter = new Converter( model, "", context, result );
		result.Json = Document( converter.ModelRoot( model, NoTransform, Path.GetFileNameWithoutExtension( model.Path ), true, 0, null ).Node );
		return result;
	}

	/// <summary>Wraps a converted root object in the prefab document the editor writes, with its properties.</summary>
	internal static string Document( JsonObject rootNode )
	{
		rootNode["__properties"] = SceneProperties();
		rootNode["__variables"] = new JsonArray();
		var document = new JsonObject
		{
			["RootObject"] = rootNode,
			["ResourceVersion"] = 2,
			["ShowInMenu"] = false,
			["MenuPath"] = null,
			["MenuIcon"] = null,
			["DontBreakAsTemplate"] = false,
			["__references"] = new JsonArray(),
			["__version"] = 2
		};
		return document.ToJsonString( new JsonSerializerOptions { WriteIndented = true } );
	}

	/// <summary>Source prefabs before the prefabs that nest them. Cycles (invalid in Unity) are dropped.</summary>
	internal static List<UnityAsset> Order( IEnumerable<UnityAsset> prefabs )
	{
		var all = prefabs.ToDictionary( p => p.Guid, StringComparer.OrdinalIgnoreCase );
		var ordered = new List<UnityAsset>();
		var state = new Dictionary<string, int>( StringComparer.OrdinalIgnoreCase );
		void Visit( UnityAsset prefab )
		{
			if ( state.GetValueOrDefault( prefab.Guid ) != 0 ) return;
			state[prefab.Guid] = 1;
			// An unreadable prefab still gets a slot; its own conversion reports the problem.
			IEnumerable<Block> nested;
			try { nested = Blocks( UnityImport.ReadText( prefab.Source ) ).Where( b => b.Type == "1001" ).ToArray(); }
			catch ( InvalidDataException ) { nested = Array.Empty<Block>(); }
			foreach ( var block in nested )
				if ( all.TryGetValue( GuidOf( Field( block.Body, "m_SourcePrefab" ) ), out var source ) && state.GetValueOrDefault( source.Guid ) == 0 ) Visit( source );
			state[prefab.Guid] = 2;
			ordered.Add( prefab );
		}
		foreach ( var prefab in all.Values.OrderBy( p => p.Path, StringComparer.OrdinalIgnoreCase ) ) Visit( prefab );
		return ordered;
	}
}
