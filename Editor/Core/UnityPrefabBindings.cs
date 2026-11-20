using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace ImportUnityPackage;

/// <summary>Resolves renderer/slot assignments from prefab data, independently of publisher or material filenames.</summary>
internal static class UnityPrefabBindings
{
	sealed class Candidate( string path )
	{
		internal readonly string Path = path;
		internal readonly Dictionary<string, HashSet<string>> Slots = new( StringComparer.OrdinalIgnoreCase );
		internal readonly Dictionary<string, Dictionary<string, HashSet<string>>> MeshSlots = new( StringComparer.OrdinalIgnoreCase );
		internal readonly HashSet<string> Materials = new( StringComparer.OrdinalIgnoreCase );
		internal readonly HashSet<string> CollisionMeshes = new( StringComparer.OrdinalIgnoreCase );
		internal readonly HashSet<string> ConvexCollisionMeshes = new( StringComparer.OrdinalIgnoreCase );
		internal readonly List<string> Warnings = new();
		internal bool HasOverrides;
		/// <summary>The skinned meshes this prefab shows (active object, enabled renderer).</summary>
		internal readonly HashSet<string> VisibleSkinnedMeshes = new( StringComparer.OrdinalIgnoreCase );
		internal bool HasSkinnedRenderers;
	}
	static string GuidOf( string reference ) => Regex.Match( reference, @"\bguid:\s*([a-fA-F0-9]{32})" ).Groups[1].Value;
	static long IdOf( string reference ) => long.TryParse( Regex.Match( reference, @"\bfileID:\s*(-?[0-9]+)" ).Groups[1].Value, out var id ) ? id : 0;
	static string Field( string text, string name ) => Regex.Match( text, @"(?m)^\s*" + Regex.Escape( name ) + @":\s*\{([^}\r\n]*)\}" ).Groups[1].Value;

	/// <summary>
	/// Identifies which FBX mesh a prefab's mesh reference points at. Modern Unity hashes the
	/// mesh's hierarchy path; pre-2018 Unity uses recycle IDs listed in the model's own metadata.
	/// </summary>
	static UnityModel.RendererBinding Binding( UnityAsset asset, long meshId )
	{
		var model = asset.ModelInfo;
		var hashed = model.RendererSlots.Values.Where( b => UnityFileId.Hash( "Type:Mesh->" + b.Mesh + "0" ) == meshId ).ToArray();
		if ( hashed.Length == 1 ) return hashed[0];
		if ( hashed.Length > 1 || !model.RecycleNames.TryGetValue( meshId, out var name ) ) return null;
		var recycled = model.RendererSlots.Values.Where( b => b.Mesh.Equals( name, StringComparison.Ordinal ) ).ToArray();
		return recycled.Length == 1 ? recycled[0] : null;
	}

	internal static void Apply( UnityArchive archive )
	{
		var models = archive.Assets.Where( a => a.Kind == UnityAssetKind.Model ).ToDictionary( a => a.Guid, StringComparer.OrdinalIgnoreCase );
		var materialIds = archive.Assets.Where( a => a.Kind == UnityAssetKind.Material ).Select( a => a.Guid ).ToHashSet( StringComparer.OrdinalIgnoreCase );
		var candidates = new Dictionary<string, List<Candidate>>( StringComparer.OrdinalIgnoreCase );
		var visible = new HashSet<string>( StringComparer.OrdinalIgnoreCase );
		var colliders = new HashSet<string>( StringComparer.OrdinalIgnoreCase );
		foreach ( var prefab in archive.Assets.Where( a => a.Path.EndsWith( ".prefab", StringComparison.OrdinalIgnoreCase ) ) )
		{
			if ( new FileInfo( prefab.Source ).Length > 16 * 1024 * 1024 ) continue;
			var text = File.ReadAllText( prefab.Source );
			var local = new Dictionary<string, Candidate>( StringComparer.OrdinalIgnoreCase );
			Candidate For( string guid )
			{
				if ( !local.TryGetValue( guid, out var candidate ) ) local[guid] = candidate = new( prefab.Path );
				return candidate;
			}
			void Assign( string guid, UnityModel.RendererBinding binding, int index, string material )
			{
				var candidate = For( guid ); candidate.HasOverrides = true;
				var model = models[guid].ModelInfo;
				if ( binding == null ) { candidate.Warnings.Add( "A prefab material override targets an unsupported or unidentified renderer; no slot was guessed." ); return; }
				// ModelDoc currently imports only LOD0 when present. Other LOD overrides must not change its slots.
				if ( model.HighestDetailMeshes.Length > 0 && !model.HighestDetailMeshes.Contains( binding.Mesh ) ) return;
				if ( index < 0 || index >= Math.Max( 1, binding.Slots.Length ) )
				{
					candidate.Warnings.Add( "A prefab material override references a missing FBX material slot." );
					// Extra Unity material passes cannot be represented by this mesh's implicit slot.
					if ( binding.Slots.Length == 0 )
					{
						if ( !candidate.Slots.TryGetValue( binding.Mesh, out var invalid ) ) candidate.Slots[binding.Mesh] = invalid = new();
						invalid.Add( "" );
					}
					return;
				}
				var slot = binding.Slots.Length == 0 ? binding.Mesh : binding.Slots[index];
				if ( !candidate.Slots.TryGetValue( slot, out var values ) ) candidate.Slots[slot] = values = new( StringComparer.OrdinalIgnoreCase );
				values.Add( material );
				// Record the same assignment per mesh: several meshes can share one FBX material
				// but take different Unity materials, which a single ModelDoc remap table cannot express.
				if ( !candidate.MeshSlots.TryGetValue( binding.Mesh, out var meshSlots ) ) candidate.MeshSlots[binding.Mesh] = meshSlots = new( StringComparer.OrdinalIgnoreCase );
				if ( !meshSlots.TryGetValue( slot, out var meshValues ) ) meshSlots[slot] = meshValues = new( StringComparer.OrdinalIgnoreCase );
				meshValues.Add( material );
				if ( materialIds.Contains( material ) ) candidate.Materials.Add( material );
			}
			var blocks = Regex.Matches( text, @"(?ms)^--- !u!(?<type>[0-9]+) &(?<id>-?[0-9]+)[^\r\n]*\r?\n(?<body>.*?)(?=^--- !u!|\z)" ).Cast<Match>()
				.Select( m => (Type: m.Groups["type"].Value, Id: long.TryParse( m.Groups["id"].Value, out var id ) ? id : 0, Body: m.Groups["body"].Value) ).ToArray();
			// A skinned mesh shows in Unity when its object and every parent are active; the parent chain runs through the transforms.
			var inactive = blocks.Where( b => b.Type == "1" && Regex.IsMatch( b.Body, @"(?m)^\s*m_IsActive:\s*0\s*$" ) ).Select( b => b.Id ).ToHashSet();
			var transformObject = blocks.Where( b => b.Type is "4" or "224" ).GroupBy( b => b.Id ).ToDictionary( g => g.Key, g => IdOf( Field( g.First().Body, "m_GameObject" ) ) );
			var parentObject = blocks.Where( b => b.Type is "4" or "224" ).GroupBy( b => IdOf( Field( b.Body, "m_GameObject" ) ) )
				.ToDictionary( g => g.Key, g => transformObject.GetValueOrDefault( IdOf( Field( g.First().Body, "m_Father" ) ) ) );
			bool ActiveInHierarchy( long objectId )
			{
				for ( var seen = new HashSet<long>(); objectId != 0 && seen.Add( objectId ); objectId = parentObject.GetValueOrDefault( objectId ) )
					if ( inactive.Contains( objectId ) ) return false;
				return true;
			}
			var filters = new Dictionary<long, List<string>>();
			var colliderObjects = new HashSet<long>();
			var convexObjects = new HashSet<long>();
			foreach ( var block in blocks )
			{
				var mesh = Field( block.Body, "m_Mesh" );
				var guid = GuidOf( mesh );
				if ( block.Type == "64" )
				{
					if ( models.ContainsKey( guid ) ) colliders.Add( guid );
					// Unity usually points a MeshCollider at a baked convex .asset, so the mesh it gets
					// collision for is identified by the MeshFilter sharing its GameObject, not by this reference.
					var owner = IdOf( Field( block.Body, "m_GameObject" ) );
					if ( owner != 0 ) colliderObjects.Add( owner );
					if ( owner != 0 && Regex.IsMatch( block.Body, @"(?m)^\s*m_Convex:\s*1\s*$" ) ) convexObjects.Add( owner );
				}
				if ( block.Type is "33" or "137" && models.ContainsKey( guid ) )
				{
					visible.Add( guid );
					if ( block.Type == "33" )
					{
						var gameObject = IdOf( Field( block.Body, "m_GameObject" ) );
						if ( gameObject == 0 ) continue;
						if ( !filters.TryGetValue( gameObject, out var list ) ) filters[gameObject] = list = new();
						list.Add( mesh );
					}
					// Which skinned meshes each prefab shows decides how the generated model lays out its bodygroups.
					else if ( Binding( models[guid], IdOf( mesh ) ) is { } skinned && models[guid].ModelInfo.SkinnedMeshes.Contains( skinned.Mesh ) )
					{
						var candidate = For( guid );
						candidate.HasSkinnedRenderers = true;
						if ( ActiveInHierarchy( IdOf( Field( block.Body, "m_GameObject" ) ) ) && !Regex.IsMatch( block.Body, @"(?m)^\s*m_Enabled:\s*0\s*$" ) )
							candidate.VisibleSkinnedMeshes.Add( skinned.Mesh );
					}
				}
				if ( block.Type == "1001" )
				{
					var source = GuidOf( Field( block.Body, "m_SourcePrefab" ) );
					if ( models.ContainsKey( source ) ) visible.Add( source );
				}
			}
			foreach ( Match modification in Regex.Matches( text, @"(?ms)^\s*- target:\s*\{(?<target>[^}]+)\}\s*\r?\n(?<body>.*?)(?=^\s*- target:|^--- !u!|\z)" ) )
			{
				var target = modification.Groups["target"].Value;
				var guid = GuidOf( target );
				var property = Regex.Match( modification.Groups["body"].Value, @"propertyPath:\s*m_Materials\.Array\.data\[([0-9]+)\]" );
				if ( !property.Success || !models.TryGetValue( guid, out var asset ) ) continue;
				visible.Add( guid );
				if ( !int.TryParse( property.Groups[1].Value, out var index ) ) continue;
				Assign( guid, asset.ModelInfo.RendererSlots.GetValueOrDefault( IdOf( target ) ), index,
					GuidOf( Field( modification.Groups["body"].Value, "objectReference" ) ) );
			}
			// Explicit MeshRenderer + MeshFilter (or SkinnedMeshRenderer) blocks in non-variant prefabs.
			foreach ( var block in blocks.Where( b => b.Type is "23" or "137" ) )
			{
				var refs = block.Type == "137" ? new[] { Field( block.Body, "m_Mesh" ) } :
					filters.GetValueOrDefault( IdOf( Field( block.Body, "m_GameObject" ) ) )?.ToArray() ?? Array.Empty<string>();
				if ( refs.Length != 1 || !models.TryGetValue( GuidOf( refs[0] ), out var asset ) ) continue;
				var slots = Regex.Match( block.Body, @"(?m)^\s*m_Materials:[ \t]*\r?\n(?<items>(?:[ \t]*- \{[^}\r\n]*\}[ \t]*\r?\n)+)" );
				if ( !slots.Success ) continue;
				var binding = Binding( asset, IdOf( refs[0] ) );
				var index = 0;
				foreach ( Match reference in Regex.Matches( slots.Groups["items"].Value, @"\{([^}]+)\}" ) ) Assign( asset.Guid, binding, index++, GuidOf( reference.Value ) );
			}
			// A MeshCollider beside a MeshFilter is the only per-mesh collision evidence a prefab carries.
			foreach ( var (owner, meshes) in filters.Where( f => colliderObjects.Contains( f.Key ) ) )
			{
				if ( meshes.Count != 1 || !models.TryGetValue( GuidOf( meshes[0] ), out var asset ) ) continue;
				if ( Binding( asset, IdOf( meshes[0] ) ) is UnityModel.RendererBinding collided )
				{
					For( asset.Guid ).CollisionMeshes.Add( collided.Mesh );
					if ( convexObjects.Contains( owner ) ) For( asset.Guid ).ConvexCollisionMeshes.Add( collided.Mesh );
				}
			}
			// Keep the prior single-model fallback for legacy prefabs without renderer-slot evidence.
			var references = UnityMaterial.References( text ).Distinct( StringComparer.OrdinalIgnoreCase ).ToArray();
			var modelIds = references.Where( g => models.ContainsKey( g ) && (!colliders.Contains( g ) || visible.Contains( g )) ).ToArray();
			if ( modelIds.Length == 1 && !For( modelIds[0] ).HasOverrides )
				foreach ( var material in references.Where( materialIds.Contains ) ) For( modelIds[0] ).Materials.Add( material );
			foreach ( var (guid, candidate) in local )
			{
				if ( !candidates.TryGetValue( guid, out var list ) ) candidates[guid] = list = new();
				list.Add( candidate );
			}
		}
		foreach ( var (guid, list) in candidates )
		{
			var asset = models[guid]; var model = asset.ModelInfo;
			var named = list.Where( p => Path.GetFileNameWithoutExtension( p.Path ).Equals( Path.GetFileNameWithoutExtension( asset.Path ), StringComparison.OrdinalIgnoreCase ) ).ToArray();
			var chosen = named.Length > 0 ? named : list.ToArray();
			// Resolve per mesh first: two meshes sharing one FBX material may take different Unity
			// materials, which looks like a slot conflict but is expressible as one model per mesh.
			foreach ( var mesh in chosen.SelectMany( c => c.MeshSlots.Keys ).Distinct( StringComparer.OrdinalIgnoreCase ) )
			{
				var perMesh = new Dictionary<string, string>( StringComparer.OrdinalIgnoreCase );
				foreach ( var slot in chosen.SelectMany( c => c.MeshSlots.GetValueOrDefault( mesh )?.Keys ?? Enumerable.Empty<string>() ).Distinct( StringComparer.OrdinalIgnoreCase ) )
				{
					var values = chosen.SelectMany( c => c.MeshSlots.GetValueOrDefault( mesh )?.GetValueOrDefault( slot ) ?? new HashSet<string>() )
						.Distinct( StringComparer.OrdinalIgnoreCase ).ToArray();
					if ( values.Length == 1 && materialIds.Contains( values[0] ) ) perMesh[slot] = values[0];
				}
				if ( perMesh.Count > 0 ) model.MeshSlotMaterials[mesh] = perMesh;
			}
			foreach ( var slot in chosen.SelectMany( c => c.Slots.Keys ).Distinct( StringComparer.OrdinalIgnoreCase ) )
			{
				var values = chosen.SelectMany( c => c.Slots.GetValueOrDefault( slot ) ?? new HashSet<string>() ).Distinct( StringComparer.OrdinalIgnoreCase ).ToArray();
				if ( values.Length == 1 && materialIds.Contains( values[0] ) ) model.PrefabSlotMaterials[slot] = values[0];
				else
				{
					model.UnresolvedPrefabSlots.Add( slot );
					// Every value is accounted for per mesh, so the importer can still produce one model per mesh.
					if ( values.All( v => materialIds.Contains( v ) ) && model.MeshSlotMaterials.SelectMany( m => m.Value.Values ).Distinct( StringComparer.OrdinalIgnoreCase ).OrderBy( v => v, StringComparer.OrdinalIgnoreCase )
							.SequenceEqual( values.OrderBy( v => v, StringComparer.OrdinalIgnoreCase ), StringComparer.OrdinalIgnoreCase ) )
						continue;
					model.AssignmentWarnings.Add( $"Prefab assignment for slot '{slot}' is missing or conflicting; no material was guessed." );
				}
			}
			model.PrefabMaterials.AddRange( chosen.SelectMany( c => c.Materials ).Distinct( StringComparer.OrdinalIgnoreCase ) );
			foreach ( var mesh in chosen.SelectMany( c => c.CollisionMeshes ).Distinct( StringComparer.OrdinalIgnoreCase ) ) model.CollisionMeshes.Add( mesh );
			// A mesh is convex only if every prefab colliding with it says so.
			foreach ( var mesh in model.CollisionMeshes.Where( m => chosen.Where( c => c.CollisionMeshes.Contains( m ) ).All( c => c.ConvexCollisionMeshes.Contains( m ) ) ) ) model.ConvexCollisionMeshes.Add( mesh );
			model.AssignmentWarnings.AddRange( chosen.SelectMany( c => c.Warnings ).Distinct() );
			// Every prefab counts here, not just the ones named after the model: an outfit switch is only one if nobody shows two.
			foreach ( var candidate in list.Where( c => c.HasSkinnedRenderers ) )
				model.PrefabVisibleSkinnedMeshes.Add( candidate.VisibleSkinnedMeshes.OrderBy( m => m, StringComparer.OrdinalIgnoreCase ).ToArray() );
		}
		foreach ( var guid in colliders.Where( g => !visible.Contains( g ) ) ) models[guid].ModelInfo.CollisionOnly = true;
	}
}
