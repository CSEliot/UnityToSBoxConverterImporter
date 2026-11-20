using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace ImportUnityPackage;

/// <summary>
/// Rewrites a binary FBX so ModelDoc can tell its meshes and bones apart. ModelDoc names a render mesh
/// after the FBX Geometry object, which Maya leaves unnamed, and then folds every unnamed mesh into one
/// "unnamed_1" that no bodygroup can name. The copy names each geometry after the mesh node it belongs to.
/// Bones sharing a name (Finger_01 on both hands) get the side of their nearest _L/_R ancestor appended,
/// where ModelDoc would otherwise rename one of them "_duplicate". Only those strings change: every other
/// byte, compressed arrays included, is copied, and the record offsets and footer are recomputed.
/// </summary>
internal static class UnityFbxRewrite
{
	sealed class Node
	{
		public string Name;
		public readonly List<byte[]> Properties = new(); // each: type byte followed by its payload, as stored
		public readonly List<Node> Children = new();
		public bool Terminated; // the file closed this node's child list with a null record
	}

	static readonly byte[] FooterMagic = { 0xf8, 0x5a, 0x8c, 0x6a, 0xde, 0xf5, 0xd9, 0x7e, 0xec, 0xe9, 0x0c, 0xe3, 0x75, 0x8f, 0x29, 0x0b };
	const string Magic = "Kaydara FBX Binary";

	/// <summary>
	/// The rewritten file, or null when the file is not binary FBX or nothing needed renaming. With
	/// <paramref name="rename"/> false the file is only parsed and written back, which must reproduce it byte for byte.
	/// </summary>
	internal static byte[] Rewrite( string source, out List<string> changes, bool rename = true )
	{
		changes = new List<string>();
		var data = File.ReadAllBytes( source );
		if ( data.Length < 27 + 13 || Encoding.ASCII.GetString( data, 0, Magic.Length ) != Magic ) return null;
		var version = BitConverter.ToUInt32( data, 23 );
		var wide = version >= 7500;
		var pos = 27;
		var roots = new List<Node>();
		while ( ReadNode( data, ref pos, wide ) is Node node ) roots.Add( node );
		if ( pos + 16 > data.Length ) throw new InvalidDataException( "FBX footer is missing." );
		var footerId = data[pos..(pos + 16)];
		if ( rename ) Rename( roots, changes );
		if ( rename && changes.Count == 0 ) return null;
		return Serialize( data[..27], roots, wide, version, footerId );
	}

	static Node ReadNode( byte[] data, ref int pos, bool wide )
	{
		var record = wide ? 25 : 13;
		if ( pos + record > data.Length ) throw new InvalidDataException( "Truncated FBX node." );
		long end, count, length;
		if ( wide )
		{
			end = (long)BitConverter.ToUInt64( data, pos ); count = (long)BitConverter.ToUInt64( data, pos + 8 ); length = (long)BitConverter.ToUInt64( data, pos + 16 );
			pos += 24;
		}
		else
		{
			end = BitConverter.ToUInt32( data, pos ); count = BitConverter.ToUInt32( data, pos + 4 ); length = BitConverter.ToUInt32( data, pos + 8 );
			pos += 12;
		}
		var nameLength = data[pos++];
		if ( end == 0 ) return null; // null record: closes a child list
		if ( end <= pos || end > data.Length || pos + nameLength > end || count > 1_000_000 ) throw new InvalidDataException( "Invalid FBX node offset." );
		var node = new Node { Name = Encoding.UTF8.GetString( data, pos, nameLength ) };
		pos += nameLength;
		if ( length < 0 || pos + length > end ) throw new InvalidDataException( "Invalid FBX property size." );
		var propertiesEnd = pos + (int)length;
		for ( var i = 0; i < count; i++ ) node.Properties.Add( ReadProperty( data, ref pos, propertiesEnd ) );
		if ( pos != propertiesEnd ) throw new InvalidDataException( "FBX property list size mismatch." );
		while ( pos < end )
		{
			var child = ReadNode( data, ref pos, wide );
			if ( child == null ) { node.Terminated = true; break; }
			node.Children.Add( child );
		}
		if ( pos != (int)end ) throw new InvalidDataException( "FBX node size mismatch." );
		return node;
	}

	static byte[] ReadProperty( byte[] data, ref int pos, int limit )
	{
		if ( pos >= limit ) throw new InvalidDataException( "FBX property list overruns its node." );
		var start = pos;
		var type = (char)data[pos++];
		int size;
		switch ( type )
		{
			case 'Y': size = 2; break;
			case 'C': size = 1; break;
			case 'I': case 'F': size = 4; break;
			case 'D': case 'L': size = 8; break;
			case 'S': case 'R':
				if ( pos + 4 > limit ) throw new InvalidDataException( "Truncated FBX string." );
				size = 4 + BitConverter.ToInt32( data, pos );
				break;
			case 'f': case 'd': case 'l': case 'i': case 'b': case 'c':
				if ( pos + 12 > limit ) throw new InvalidDataException( "Truncated FBX array." );
				size = 12 + BitConverter.ToInt32( data, pos + 8 );
				break;
			default: throw new InvalidDataException( $"Unknown FBX property type '{type}'." );
		}
		if ( size < 0 || pos + size > limit ) throw new InvalidDataException( "FBX property overruns its node." );
		pos += size;
		return data[start..pos];
	}

	static string Text( byte[] property ) => property.Length >= 5 && property[0] == (byte)'S' ? Encoding.UTF8.GetString( property, 5, property.Length - 5 ) : null;
	static long Integer( byte[] property ) => property.Length == 9 && property[0] == (byte)'L' ? BitConverter.ToInt64( property, 1 ) : 0;
	static byte[] StringProperty( string text )
	{
		var bytes = Encoding.UTF8.GetBytes( text );
		var property = new byte[5 + bytes.Length];
		property[0] = (byte)'S';
		BitConverter.GetBytes( bytes.Length ).CopyTo( property, 1 );
		bytes.CopyTo( property, 5 );
		return property;
	}
	/// <summary>FBX object names are "Display\0\u0001Class".</summary>
	static (string Display, string Class) ObjectName( string text )
	{
		var separator = text.IndexOf( "\0\u0001", StringComparison.Ordinal );
		return separator < 0 ? (text, "") : (text[..separator], text[(separator + 2)..]);
	}

	static void Rename( List<Node> roots, List<string> changes )
	{
		var objects = roots.FirstOrDefault( n => n.Name == "Objects" );
		var connections = roots.FirstOrDefault( n => n.Name == "Connections" );
		if ( objects == null || connections == null ) return;
		static bool IsObject( Node n ) => n.Properties.Count >= 3 && n.Properties[0][0] == (byte)'L' && Text( n.Properties[1] ) != null && Text( n.Properties[2] ) != null;
		var models = objects.Children.Where( n => n.Name == "Model" && IsObject( n ) ).GroupBy( n => Integer( n.Properties[0] ) ).ToDictionary( g => g.Key, g => g.First() );
		var geometries = objects.Children.Where( n => n.Name == "Geometry" && IsObject( n ) && Text( n.Properties[2] ) == "Mesh" ).GroupBy( n => Integer( n.Properties[0] ) ).ToDictionary( g => g.Key, g => g.First() );
		var links = connections.Children.Where( c => c.Name == "C" && c.Properties.Count >= 3 && Text( c.Properties[0] ) == "OO" )
			.Select( c => (Child: Integer( c.Properties[1] ), Parent: Integer( c.Properties[2] )) ).ToList();
		var parentsOf = links.ToLookup( l => l.Child, l => l.Parent );
		string Display( Node model ) => ObjectName( Text( model.Properties[1] ) ).Display;
		var taken = models.Values.Select( Display ).ToHashSet( StringComparer.Ordinal );

		// Geometry takes the name of the one mesh node it belongs to, when that name is unambiguous.
		var meshes = models.Where( m => Text( m.Value.Properties[2] ) == "Mesh" ).ToDictionary( m => m.Key, m => Display( m.Value ) );
		var uniqueMeshNames = meshes.Values.GroupBy( n => n, StringComparer.Ordinal ).Where( g => g.Count() == 1 ).Select( g => g.Key ).ToHashSet( StringComparer.Ordinal );
		var namedGeometries = 0;
		foreach ( var (id, geometry) in geometries )
		{
			var owners = parentsOf[id].Where( meshes.ContainsKey ).Distinct().ToArray();
			if ( owners.Length != 1 || !uniqueMeshNames.Contains( meshes[owners[0]] ) ) continue;
			var (display, type) = ObjectName( Text( geometry.Properties[1] ) );
			if ( display == meshes[owners[0]] ) continue;
			geometry.Properties[1] = StringProperty( meshes[owners[0]] + "\0\u0001" + (type.Length > 0 ? type : "Geometry") );
			namedGeometries++;
		}
		if ( namedGeometries > 0 ) changes.Add( $"{namedGeometries} geometr{(namedGeometries == 1 ? "y" : "ies")} named after {(namedGeometries == 1 ? "its" : "their")} mesh node{(namedGeometries == 1 ? "" : "s")}" );

		// Bones that share a name get their side from the nearest _L/_R ancestor, else a number.
		var bones = models.Where( m => Text( m.Value.Properties[2] ) == "LimbNode" ).ToDictionary( m => m.Key, m => m.Value );
		string Side( long id )
		{
			var seen = new HashSet<long>();
			for ( var current = parentsOf[id].FirstOrDefault( models.ContainsKey ); current != 0 && seen.Add( current ); current = parentsOf[current].FirstOrDefault( models.ContainsKey ) )
			{
				var match = Regex.Match( Display( models[current] ), @"[_. ]([LRlr])$" );
				if ( match.Success ) return match.Groups[1].Value.ToUpperInvariant();
			}
			return null;
		}
		var renamedBones = new List<string>();
		foreach ( var group in bones.GroupBy( b => Display( b.Value ), StringComparer.Ordinal ).Where( g => g.Count() > 1 ).OrderBy( g => g.Key, StringComparer.Ordinal ) )
		{
			var number = 2;
			foreach ( var (id, bone) in group )
			{
				var candidate = Side( id ) is string side ? $"{group.Key}_{side}" : null;
				if ( candidate == null || taken.Contains( candidate ) )
				{
					while ( taken.Contains( $"{group.Key}_{number}" ) ) number++;
					candidate = $"{group.Key}_{number}";
				}
				taken.Add( candidate );
				bone.Properties[1] = StringProperty( candidate + "\0\u0001" + ObjectName( Text( bone.Properties[1] ) ).Class );
				renamedBones.Add( candidate );
			}
		}
		if ( renamedBones.Count > 0 ) changes.Add( $"{renamedBones.Count} bones sharing a name made distinct ({string.Join( ", ", renamedBones )})" );
	}

	static int Size( Node node, bool wide )
	{
		var record = wide ? 25 : 13;
		var size = record + Encoding.UTF8.GetByteCount( node.Name ) + node.Properties.Sum( p => p.Length ) + node.Children.Sum( c => Size( c, wide ) );
		return node.Terminated ? size + record : size;
	}

	static void WriteNull( Stream output, bool wide ) => output.Write( new byte[wide ? 25 : 13] );

	static void Write( Stream output, Node node, bool wide )
	{
		var end = output.Position + Size( node, wide );
		var name = Encoding.UTF8.GetBytes( node.Name );
		if ( name.Length > 255 ) throw new InvalidDataException( "FBX node name too long." );
		var length = node.Properties.Sum( p => p.Length );
		if ( wide )
		{
			output.Write( BitConverter.GetBytes( (ulong)end ) );
			output.Write( BitConverter.GetBytes( (ulong)node.Properties.Count ) );
			output.Write( BitConverter.GetBytes( (ulong)length ) );
		}
		else
		{
			if ( end > uint.MaxValue ) throw new InvalidDataException( "FBX file too large for its 32-bit offsets." );
			output.Write( BitConverter.GetBytes( (uint)end ) );
			output.Write( BitConverter.GetBytes( (uint)node.Properties.Count ) );
			output.Write( BitConverter.GetBytes( (uint)length ) );
		}
		output.WriteByte( (byte)name.Length );
		output.Write( name );
		foreach ( var property in node.Properties ) output.Write( property );
		foreach ( var child in node.Children ) Write( output, child, wide );
		if ( node.Terminated ) WriteNull( output, wide );
	}

	static byte[] Serialize( byte[] header, List<Node> roots, bool wide, uint version, byte[] footerId )
	{
		using var output = new MemoryStream();
		output.Write( header );
		foreach ( var node in roots ) Write( output, node, wide );
		WriteNull( output, wide );
		// Footer, as the SDK writes it: file id, padding to a 16-byte boundary, four zero bytes, version, 120 zero bytes, magic.
		output.Write( footerId );
		output.Write( new byte[16 - (int)(output.Position % 16) + 4] ); // 1..16 bytes of padding: a full 16 when already aligned
		output.Write( BitConverter.GetBytes( version ) );
		output.Write( new byte[120] );
		output.Write( FooterMagic );
		return output.ToArray();
	}
}
