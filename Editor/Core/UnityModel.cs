using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace ImportUnityPackage;

public sealed class UnityModel
{
	public sealed record BodyGroupChoice( string Name, string[] Meshes );
	public sealed record BodyGroup( string Name, BodyGroupChoice[] Choices );
	/// <summary>Meshes the FBX binds to its skeleton with a skin deformer; they render through a SkinnedModelRenderer.</summary>
	public HashSet<string> SkinnedMeshes { get; } = new( StringComparer.OrdinalIgnoreCase );
	public bool Skinned => SkinnedMeshes.Count > 0;
	/// <summary>Skeleton bones (FBX LimbNodes).</summary>
	public int BoneCount { get; internal set; }
	public bool HasBlendShapes { get; internal set; }
	/// <summary>An animation curve in the FBX changes value over time. A take of constant curves, which exporters write by default, is not one.</summary>
	public bool HasAnimation { get; internal set; }
	/// <summary>The FBX file's name without extension; names the bodygroup that switches between its skinned meshes.</summary>
	public string Name { get; private set; } = "";
	/// <summary>Per Unity prefab rendering this FBX's skinned meshes, the ones it shows. Decides the bodygroup layout.</summary>
	public List<string[]> PrefabVisibleSkinnedMeshes { get; } = new();
	IReadOnlyList<BodyGroup> bodyGroups;
	/// <summary>
	/// How the generated model switches its skinned meshes. Unity shows or hides each skinned mesh object on
	/// its own. When every prefab shows at most one of them (an outfit switch), one group named after the FBX
	/// offers each mesh and "none", the way the Citizen picks a head; otherwise each mesh gets an on/off group
	/// so any combination stays expressible. Without prefab evidence that per-mesh layout defaults to everything
	/// on, which is what Unity's own preview of the FBX shows. Empty for a static FBX or a single skinned mesh.
	/// </summary>
	public IReadOnlyList<BodyGroup> BodyGroups => bodyGroups ??= BuildBodyGroups();
	/// <summary>A mesh-group mask has 64 bits; a model needing more keeps every mesh visible instead.</summary>
	public bool BodyGroupsOverflow { get; private set; }

	List<BodyGroup> BuildBodyGroups()
	{
		var meshes = RenderedMeshes.Where( SkinnedMeshes.Contains ).ToArray();
		if ( meshes.Length < 2 ) return new();
		var exclusive = PrefabVisibleSkinnedMeshes.Count > 0 && PrefabVisibleSkinnedMeshes.All( shown => shown.Count( m => meshes.Contains( m, StringComparer.OrdinalIgnoreCase ) ) <= 1 );
		var groups = exclusive
			? new List<BodyGroup> { new( Name, meshes.Select( m => new BodyGroupChoice( m, new[] { m } ) ).Append( new( "none", Array.Empty<string>() ) ).ToArray() ) }
			: meshes.Select( m => new BodyGroup( m, new[] { new BodyGroupChoice( "on", new[] { m } ), new BodyGroupChoice( "off", Array.Empty<string>() ) } ) ).ToList();
		if ( groups.Sum( g => g.Choices.Length ) <= 64 ) return groups;
		BodyGroupsOverflow = true;
		return new();
	}

	/// <summary>
	/// The renderer mask showing exactly <paramref name="visible"/> among the skinned meshes: one bit per choice,
	/// groups laid out in order, which is how the compiled model numbers its mesh groups (the Citizen's five
	/// two-choice groups with every first choice picked is 0b101010101 = 341 in the engine's own scenes).
	/// Null when the model has no bodygroups.
	/// </summary>
	public ulong? BodyGroupMask( IEnumerable<string> visible, out string warning )
	{
		warning = null;
		var groups = BodyGroups;
		if ( groups.Count == 0 ) return null;
		var shown = visible.ToHashSet( StringComparer.OrdinalIgnoreCase );
		ulong mask = 0;
		var bit = 0;
		foreach ( var group in groups )
		{
			var wanted = group.Choices.SelectMany( c => c.Meshes ).Where( shown.Contains ).Distinct( StringComparer.OrdinalIgnoreCase ).ToArray();
			var index = Array.FindIndex( group.Choices, c => c.Meshes.Length == wanted.Length && wanted.All( m => c.Meshes.Contains( m, StringComparer.OrdinalIgnoreCase ) ) );
			if ( index < 0 )
			{
				// An exclusive group cannot show two meshes at once; the first one wins.
				index = Array.FindIndex( group.Choices, c => c.Meshes.Any( m => wanted.Contains( m, StringComparer.OrdinalIgnoreCase ) ) );
				warning = $"bodygroup '{group.Name}' cannot show {string.Join( ", ", wanted )} together; showing {group.Choices[index].Name}.";
			}
			mask |= 1UL << (bit + index);
			bit += group.Choices.Length;
		}
		return mask;
	}

	public sealed record RendererBinding( string Mesh, string[] Slots )
	{
		// Without material objects the model compiler names the implicit slot after the mesh.
		public IEnumerable<string> EffectiveSlots => Slots.Length == 0 ? new[] { Mesh } : Slots;
	}
	public Dictionary<long, RendererBinding> RendererSlots { get; } = new();
	public Dictionary<string, string> PrefabSlotMaterials { get; } = new( StringComparer.OrdinalIgnoreCase );
	public HashSet<string> UnresolvedPrefabSlots { get; } = new( StringComparer.OrdinalIgnoreCase );
	/// <summary>Per-mesh slot assignments, so meshes sharing one FBX material can keep different materials.</summary>
	public Dictionary<string, Dictionary<string, string>> MeshSlotMaterials { get; } = new( StringComparer.OrdinalIgnoreCase );
	/// <summary>Meshes the Unity prefab gave a MeshCollider; only these get generated physics shapes.</summary>
	public HashSet<string> CollisionMeshes { get; } = new( StringComparer.OrdinalIgnoreCase );
	/// <summary>Collision meshes whose Unity MeshCollider is convex: one hull for the whole mesh.</summary>
	public HashSet<string> ConvexCollisionMeshes { get; } = new( StringComparer.OrdinalIgnoreCase );
	/// <summary>Unity's legacy fileIDToRecycleName map, which identifies sub-assets in pre-2018 packages.</summary>
	public Dictionary<long, string> RecycleNames { get; } = new();
	public List<string> AssignmentWarnings { get; } = new();
	public bool CollisionOnly { get; internal set; }
	public List<string> Materials { get; } = new();
	public List<string> Meshes { get; } = new();
	public List<string> PrefabMaterials { get; } = new();
	public Dictionary<string, string> MaterialAlbedoFiles { get; } = new( StringComparer.OrdinalIgnoreCase );
	public double? UnitScaleCentimeters { get; private set; }
	public double ImportScale( string metadata )
	{
		double Setting( string key, double fallback )
		{
			var match = Regex.Match( metadata ?? "", @"(?m)^\s*" + key + @":\s*([-+0-9.eE]+)" );
			return match.Success && double.TryParse( match.Groups[1].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var value ) && double.IsFinite( value ) ? value : fallback;
		}
		// Unity's globalScale is relative to the file's own units. The result is Unity units (metres)
		// per FBX unit; the generated model's ScaleAndMirror modifier then converts metres to inches.
		var globalScale = Setting( "globalScale", 1 );
		var meters = Setting( "useFileScale", 1 ) == 0 ? 1 : (UnitScaleCentimeters ?? 2.54) / 100;
		var scale = globalScale * meters;
		if ( !double.IsFinite( scale ) || scale <= 0 ) throw new InvalidDataException( "Model import scale must be finite and positive." );
		return scale;
	}
	public string[] HighestDetailMeshes => Meshes.Where( n => Regex.IsMatch( n, @"(?i)(?:^|[_ .-])LOD0(?:$|[_ .-])" ) ).ToArray();
	/// <summary>The meshes a generated model should import: LOD0 only when the FBX has LODs.</summary>
	public string[] RenderedMeshes => HighestDetailMeshes is { Length: > 0 } lod0 ? lod0 : Meshes.ToArray();
	public string[] RenderedMaterials
	{
		get
		{
			var meshes = HighestDetailMeshes;
			var bindings = RendererSlots.Values.Where( b => meshes.Length == 0 || meshes.Contains( b.Mesh ) ).ToArray();
			return bindings.Length > 0 ? bindings.SelectMany( b => b.EffectiveSlots ).Distinct( StringComparer.OrdinalIgnoreCase ).ToArray() : Materials.ToArray();
		}
	}
	/// <summary>
	/// Unity's fixed model-prefab renderer ids (2300000+2n) and the FBX mesh each draws, read from the
	/// importer's own fileIDToRecycleName map. Renderer ids count objects that have a renderer and mesh ids
	/// count meshes, so the two id families are not parallel counters: an object without geometry leaves a
	/// gap, and Unity names the mesh it builds for such a root or group object after that object (Brush_01,
	/// counter01_2) instead of after a mesh node in the file. Reading the map keeps every override on the
	/// mesh Unity shows rather than on a mesh named by arithmetic.
	/// </summary>
	public Dictionary<long, string> FixedRendererMeshes { get; } = new();
	/// <summary>Fixed renderer ids of model root objects that carry no mesh, so an override of one draws nothing.</summary>
	public HashSet<long> RendererlessObjects { get; } = new();
	/// <summary>Fixed renderer ids whose mesh the file no longer carries (stale sub-assets, merged root meshes).</summary>
	public HashSet<long> StaleRenderers { get; } = new();
	public static string Normalize( string name ) => Regex.Replace( name.ToLowerInvariant(), "[^a-z]", "" );

	/// <summary>
	/// Pre-2018 Unity identifies FBX sub-assets by recycle ID (4300000, 4300002, …) listed in the
	/// importer metadata instead of the modern hashed hierarchy path. Without this map, prefab
	/// MeshFilter references in older packages resolve to no renderer and their assignments are lost.
	/// </summary>
	void ReadRecycleNames( UnityAsset asset )
	{
		if ( asset.Metadata == null ) return;
		var metadata = UnityImport.ReadText( asset.Metadata );
		const string indent = @"[^\S\r\n]";
		var section = Regex.Match( metadata, $@"(?m)^{indent}*fileIDToRecycleName:{indent}*\r?\n(?<body>(?:{indent}+\S.*\r?\n?)+)" );
		if ( !section.Success ) return;
		foreach ( Match entry in Regex.Matches( section.Groups["body"].Value, @"(?m)^\s*(-?[0-9]+):\s*(.+?)\s*$" ) )
			if ( long.TryParse( entry.Groups[1].Value, out var id ) ) RecycleNames[id] = entry.Groups[2].Value;
	}

	/// <summary>
	/// Pairs the recycle map's renderer ids with the mesh each one draws. The map lists the object's name for a
	/// renderer and a mesh's name for a mesh, so the name is the pairing: the mesh sub-asset named after the
	/// object. A mesh Unity built for a root or group object is that model's one rendered mesh when it has
	/// only one; over several rendered meshes it belongs to none of them and stays unmapped rather than guessed.
	/// </summary>
	void ReadFixedRenderers()
	{
		var meshes = RecycleNames.Where( r => r.Key is >= 4300000 and < 4400000 ).OrderBy( r => r.Key ).Select( r => r.Value ).ToArray();
		if ( meshes.Length == 0 ) return;
		var renderers = RecycleNames.Where( r => r.Key is >= 2300000 and < 2400000 ).OrderBy( r => r.Key ).ToArray();
		// The map names the model root object "//RootNode"; the mesh it draws is the one no renderer claims.
		var claimed = renderers.Select( r => r.Value ).ToHashSet( StringComparer.OrdinalIgnoreCase );
		var rootMesh = meshes.FirstOrDefault( m => !claimed.Contains( m ) );
		// A mesh Unity built for a root or group object is not a node in this file, so nothing pairs it with an
		// override: placing one by position would land it on a child whose slot order is the child's, not the
		// built mesh's. Every such renderer is recorded as stale rather than mapped.
		foreach ( var (id, objectName) in renderers )
		{
			var owner = objectName == "//RootNode" ? rootMesh : objectName;
			// A root object with no mesh asset of its own has a renderer that draws nothing, so an override
			// of it has no slot to land on and is identified rather than guessed at.
			if ( string.IsNullOrEmpty( owner ) ) { RendererlessObjects.Add( id ); continue; }
			// Unity's duplicate objects ("Fan_02 1") share the mesh of the object they copy.
			var shared = Regex.Match( owner, @"^(?<name>.+) \d+$" );
			if ( shared.Success && !meshes.Contains( owner, StringComparer.OrdinalIgnoreCase ) ) owner = shared.Groups["name"].Value;
			if ( Meshes.Contains( owner, StringComparer.OrdinalIgnoreCase ) )
			{
				FixedRendererMeshes[id] = Meshes.First( m => m.Equals( owner, StringComparison.OrdinalIgnoreCase ) );
				continue;
			}
			// An identified renderer whose mesh this file does not carry: a stale sub-asset entry, or a mesh
			// Unity merged for a root or group object. Not a renderer nobody can name, so it is not a warning.
			StaleRenderers.Add( id );
		}
	}

	public static UnityModel Read( UnityAsset asset )
	{
		var result = new UnityModel { Name = Path.GetFileNameWithoutExtension( asset.Path ) };
		if ( !asset.Path.EndsWith( ".fbx", StringComparison.OrdinalIgnoreCase ) ) return result;
		result.ReadRecycleNames( asset );
		using var stream = File.OpenRead( asset.Source );
		using var reader = new BinaryReader( stream, Encoding.UTF8 );
		var header = Encoding.ASCII.GetString( reader.ReadBytes( 23 ) );
		if ( !header.StartsWith( "Kaydara FBX Binary", StringComparison.Ordinal ) )
		{
			stream.Position = 0;
			using var textReader = new StreamReader( stream );
			var links = new UnityAsciiFbxLinks();
			var asciiBindings = new UnityFbxBindings();
			string line;
			while ( (line = textReader.ReadLine()) != null )
			{
				links.Observe( line );
				asciiBindings.ObserveAscii( line );
				var unit = Regex.Match( line, "^\\s*(?:P|Property):\\s*\"UnitScaleFactor\".*?,\\s*([-+0-9.eE]+)\\s*$" );
				if ( unit.Success && double.TryParse( unit.Groups[1].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var centimeters ) && centimeters > 0 && double.IsFinite( centimeters ) ) result.UnitScaleCentimeters = centimeters;
				var material = Regex.Match( line, "^\\s*Material:\\s*(?:[-0-9]+,\\s*)?\"Material::([^\"]+)\"" );
				if ( material.Success && !result.Materials.Contains( material.Groups[1].Value ) ) result.Materials.Add( material.Groups[1].Value );
				var mesh = Regex.Match( line, "^\\s*Model:\\s*(?:[-0-9]+,\\s*)?\"Model::([^\"]+)\",\\s*\"Mesh\"" );
				if ( mesh.Success && !result.Meshes.Contains( mesh.Groups[1].Value ) ) result.Meshes.Add( mesh.Groups[1].Value );
			}
			foreach ( var link in links.Resolve() ) result.MaterialAlbedoFiles[link.Key] = link.Value;
			asciiBindings.Apply( result, asset.Metadata != null && Regex.IsMatch( UnityImport.ReadText( asset.Metadata ), @"(?m)^\s*preserveHierarchy:\s*1\s*$" ) );
		result.ReadFixedRenderers();
			return result;
		}
		var wide = reader.ReadUInt32() >= 7500;
		var materialNames = new Dictionary<long, string>();
		var textureFiles = new Dictionary<long, string>();
		var diffuseLinks = new List<(long Texture, long Material)>();
		var bindings = new UnityFbxBindings();
		int nodes = 0;
		void ReadNodes( long limit, bool objects = false, bool connections = false, long textureId = 0, bool settings = false, bool curve = false )
		{
			while ( stream.Position + (wide ? 25 : 13) <= limit )
			{
				if ( ++nodes > 200000 ) throw new InvalidDataException( "Too many FBX nodes." );
				var end = wide ? (long)reader.ReadUInt64() : reader.ReadUInt32();
				var properties = wide ? (long)reader.ReadUInt64() : reader.ReadUInt32();
				var propertyBytes = wide ? (long)reader.ReadUInt64() : reader.ReadUInt32();
				var nameBytes = reader.ReadByte();
				if ( end == 0 ) return;
				if ( end <= stream.Position || end > stream.Length ) throw new InvalidDataException( "Invalid FBX node offset." );
				var name = Encoding.UTF8.GetString( reader.ReadBytes( nameBytes ) );
				var children = stream.Position + propertyBytes;
				if ( children > end ) throw new InvalidDataException( "Invalid FBX property size." );
				object Property()
				{
						var type = (char)reader.ReadByte();
						if ( type == 'L' ) return reader.ReadInt64();
						if ( type == 'I' ) return reader.ReadInt32();
						if ( type == 'D' ) return reader.ReadDouble();
						if ( type == 'F' ) return reader.ReadSingle();
						if ( type == 'S' )
						{
							var length = reader.ReadInt32();
							if ( length < 0 || length > 1024 * 1024 || stream.Position + length > children ) throw new InvalidDataException( "Invalid FBX string." );
							return Encoding.UTF8.GetString( reader.ReadBytes( length ) );
						}
						throw new InvalidDataException( "Unsupported FBX object property." );
				}
				// A float/double array property (animation keys); null for any other array type.
				double[] ReadArray()
				{
					var type = (char)reader.ReadByte();
					var length = reader.ReadUInt32();
					var encoding = reader.ReadUInt32();
					var compressed = reader.ReadUInt32();
					if ( length > 4_000_000 || stream.Position + compressed > children ) throw new InvalidDataException( "Invalid FBX array." );
					var raw = reader.ReadBytes( (int)compressed );
					if ( type is not ('f' or 'd') ) return null;
					if ( encoding == 1 )
					{
						using var zlib = new ZLibStream( new MemoryStream( raw ), CompressionMode.Decompress );
						using var buffer = new MemoryStream();
						zlib.CopyTo( buffer );
						raw = buffer.ToArray();
					}
					var size = type == 'f' ? 4 : 8;
					if ( raw.Length < length * size ) throw new InvalidDataException( "Invalid FBX array." );
					var values = new double[length];
					for ( var i = 0; i < length; i++ ) values[i] = type == 'f' ? BitConverter.ToSingle( raw, i * 4 ) : BitConverter.ToDouble( raw, i * 8 );
					return values;
				}
				if ( objects && name is "Material" or "Model" or "Texture" or "Geometry" or "Deformer" && properties >= 3 )
				{
					var id = Convert.ToInt64( Property() );
					var objectName = (Property() as string ?? "").Split( '\0' )[0];
					var typeName = Property() as string;
					bindings.Observe( name, id, objectName, typeName );
					if ( name == "Material" ) { result.Materials.Add( objectName ); materialNames[id] = objectName; }
					if ( name == "Model" && typeName == "Mesh" ) result.Meshes.Add( objectName );
					if ( name == "Texture" ) { stream.Position = children; ReadNodes( end, textureId: id ); }
				}
				// One moving curve is enough to know the take is a real animation; constant curves are export noise.
				else if ( objects && name == "AnimationCurve" && !result.HasAnimation ) { stream.Position = children; ReadNodes( end, curve: true ); }
				else if ( curve && name == "KeyValueFloat" && properties == 1 && !result.HasAnimation )
				{
					try
					{
						if ( ReadArray() is double[] keys && keys.Length > 1 && keys.Any( k => Math.Abs( k - keys[0] ) > 1e-5 ) ) result.HasAnimation = true;
					}
					catch ( InvalidDataException ) { }
				}
				else if ( textureId != 0 && name is "FileName" or "RelativeFilename" && properties > 0 ) textureFiles[textureId] = Property() as string;
				else if ( settings && name is "P" or "Property" && properties >= 4 && properties <= 8 )
				{
					if ( Property() as string == "UnitScaleFactor" )
					{
						object value = null;
						for ( var index = 1; index < properties; index++ ) value = Property();
						var centimeters = Convert.ToDouble( value, CultureInfo.InvariantCulture );
						if ( centimeters > 0 && double.IsFinite( centimeters ) ) result.UnitScaleCentimeters = centimeters;
					}
				}
				else if ( connections && name == "C" && properties >= 3 )
				{
					var kind = Property() as string;
					var child = Convert.ToInt64( Property() );
					var parent = Convert.ToInt64( Property() );
					var channel = properties >= 4 ? Property() as string ?? "" : "";
					if ( kind == "OO" ) bindings.Connections.Add( (child, parent) );
					if ( kind == "OP" && (channel == "DiffuseColor" || channel.EndsWith( "|base_color_map", StringComparison.Ordinal )) ) diffuseLinks.Add( (child, parent) );
				}
				if ( name == "Objects" ) { stream.Position = children; ReadNodes( end, true ); }
				if ( name == "Connections" ) { stream.Position = children; ReadNodes( end, connections: true ); }
				if ( name == "GlobalSettings" || settings && name is "Properties70" or "Properties60" ) { stream.Position = children; ReadNodes( end, settings: true ); }
				stream.Position = end;
			}
		}
		ReadNodes( stream.Length, false );
		bindings.Apply( result, asset.Metadata != null && Regex.IsMatch( UnityImport.ReadText( asset.Metadata ), @"(?m)^\s*preserveHierarchy:\s*1\s*$" ) );
		foreach ( var link in diffuseLinks )
			if ( materialNames.TryGetValue( link.Material, out var material ) && textureFiles.TryGetValue( link.Texture, out var file ) && !string.IsNullOrEmpty( file ) )
				result.MaterialAlbedoFiles[material] = file.Replace( '\\', '/' ).Split( '/' ).Last();
	result.ReadFixedRenderers();
		return result;
	}

	public static void AssignPrefabMaterials( UnityArchive archive ) => UnityPrefabBindings.Apply( archive );
}
