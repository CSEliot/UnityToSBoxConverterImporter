using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace ImportUnityPackage;

/// <summary>FBX object hierarchy, ordered material-to-model connections and skinning links; no vertex decoding needed.</summary>
internal sealed class UnityFbxBindings
{
	internal readonly Dictionary<long, (string Name, bool Mesh)> Nodes = new();
	internal readonly Dictionary<long, string> Materials = new();
	internal readonly List<(long Child, long Parent)> Connections = new();
	/// <summary>Mesh geometry objects; FBX 7 hangs a mesh's skin deformer off its geometry, FBX 6 off the mesh node itself.</summary>
	internal readonly HashSet<long> Geometries = new();
	/// <summary>Skin deformers: what binds a mesh to bones.</summary>
	internal readonly HashSet<long> Skins = new();
	/// <summary>Skeleton bones (LimbNode models).</summary>
	internal readonly HashSet<long> Bones = new();
	internal bool HasBlendShapes;
	readonly Dictionary<string, long> legacyIds = new( StringComparer.Ordinal );
	long Id( string value )
	{
		if ( long.TryParse( value, out var id ) ) return id;
		if ( value == "Model::Scene" ) return 0;
		if ( !legacyIds.TryGetValue( value, out id ) ) legacyIds[value] = id = -legacyIds.Count - 1;
		return id;
	}
	internal void ObserveAscii( string line )
	{
		var item = Regex.Match( line, "^\\s*(Model|Material|Geometry|Deformer):\\s*(?:([-0-9]+),\\s*)?\"(?:Model|Material|Geometry|Deformer)::([^\"]*)\",\\s*\"([^\"]*)\"" );
		if ( item.Success )
		{
			var kind = item.Groups[1].Value;
			Observe( kind, Id( item.Groups[2].Success ? item.Groups[2].Value : kind + "::" + item.Groups[3].Value ), item.Groups[3].Value, item.Groups[4].Value );
		}
		var link = Regex.Match( line, "^\\s*(?:C|Connect):\\s*\"OO\",\\s*(?:\"([^\"]+)\"|([-0-9]+)),\\s*(?:\"([^\"]+)\"|([-0-9]+))" );
		if ( link.Success ) Connections.Add( (Id( link.Groups[1].Success ? link.Groups[1].Value : link.Groups[2].Value ), Id( link.Groups[3].Success ? link.Groups[3].Value : link.Groups[4].Value )) );
	}

	/// <summary>Records one FBX object by kind, id, name and subtype; the binary and ASCII readers both feed this.</summary>
	internal void Observe( string kind, long id, string name, string type )
	{
		switch ( kind )
		{
			case "Material": Materials[id] = name; break;
			case "Model":
				Nodes[id] = (name, type == "Mesh");
				if ( type == "LimbNode" ) Bones.Add( id );
				break;
			case "Geometry": if ( type == "Mesh" ) Geometries.Add( id ); break;
			case "Deformer":
				if ( type == "Skin" ) Skins.Add( id );
				if ( type == "BlendShape" ) HasBlendShapes = true;
				break;
		}
	}

	internal void Apply( UnityModel model, bool preserveHierarchy )
	{
		var parents = Connections.Where( c => Nodes.ContainsKey( c.Child ) && (c.Parent == 0 || Nodes.ContainsKey( c.Parent )) )
			.GroupBy( c => c.Child ).Where( g => g.Select( c => c.Parent ).Distinct().Count() == 1 ).ToDictionary( g => g.Key, g => g.First().Parent );
		var roots = Nodes.Keys.Where( id => !parents.TryGetValue( id, out var parent ) || parent == 0 ).ToArray();
		var ambiguous = new HashSet<long>();
		foreach ( var (id, node) in Nodes.Where( p => p.Value.Mesh ) )
		{
			var names = new List<string>();
			var seen = new HashSet<long>();
			var current = id;
			while ( Nodes.TryGetValue( current, out var ancestor ) && seen.Add( current ) )
			{
				names.Add( !preserveHierarchy && roots.Length == 1 && roots[0] == current ? "root" : ancestor.Name );
				current = parents.GetValueOrDefault( current );
			}
			if ( current != 0 ) continue; // Cyclic/malformed hierarchy cannot identify a renderer.
			names.Reverse();
			if ( preserveHierarchy || roots.Length != 1 ) names.Insert( 0, "root" );
			var path = "//RootNode/" + string.Join( "/", names );
			var slots = Connections.Where( c => c.Parent == id && Materials.ContainsKey( c.Child ) ).Select( c => Materials[c.Child] ).ToArray();
			var renderer = UnityFileId.Renderer( path );
			if ( !model.RendererSlots.TryAdd( renderer, new( node.Name, slots ) ) ) ambiguous.Add( renderer );
		}
		foreach ( var id in ambiguous ) model.RendererSlots.Remove( id );
		model.BoneCount = Bones.Count;
		model.HasBlendShapes = HasBlendShapes;
		// A skinned mesh is one whose geometry (FBX 7) or mesh node (FBX 6) has a skin deformer attached.
		var parentsOf = Connections.ToLookup( c => c.Child, c => c.Parent );
		foreach ( var skin in Skins )
			foreach ( var parent in parentsOf[skin] )
			{
				IEnumerable<long> owners = Geometries.Contains( parent ) ? parentsOf[parent] : new[] { parent };
				foreach ( var owner in owners )
					if ( Nodes.TryGetValue( owner, out var node ) && node.Mesh ) model.SkinnedMeshes.Add( node.Name );
			}
	}
}
