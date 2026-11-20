using System;
using System.Buffers.Binary;
using System.Text;

namespace ImportUnityPackage;

/// <summary>Unity's modern model subasset IDs use seed-zero XXH64 of a type and hierarchy path.</summary>
internal static class UnityFileId
{
	// XXH64 algorithm: https://github.com/Cyan4973/xxHash/blob/dev/doc/xxhash_spec.md
	// Unity path format: https://discussions.unity.com/t/fbx-submesh-fileids/803882
	internal static long Renderer( string path ) => Hash( "Type:MeshRenderer->" + path + "/MeshRenderer0" );
	internal static long Hash( string text )
	{
		unchecked
		{
			const ulong p1 = 0x9E3779B185EBCA87, p2 = 0xC2B2AE3D27D4EB4F, p3 = 0x165667B19E3779F9,
				p4 = 0x85EBCA77C2B2AE63, p5 = 0x27D4EB2F165667C5;
			static ulong Rotate( ulong v, int n ) => (v << n) | (v >> (64 - n));
			static ulong Round( ulong v, ulong lane ) => unchecked( Rotate( v + lane * p2, 31 ) * p1 );
			static ulong Merge( ulong h, ulong v ) => unchecked( (h ^ Round( 0, v )) * p1 + p4 );
			ReadOnlySpan<byte> data = Encoding.UTF8.GetBytes( text );
			int offset = 0;
			ulong hash = p5;
			if ( data.Length >= 32 )
			{
				ulong a = p1 + p2, b = p2, c = 0, d = 0UL - p1;
				while ( offset <= data.Length - 32 )
				{
					a = Round( a, BinaryPrimitives.ReadUInt64LittleEndian( data[offset..] ) );
					b = Round( b, BinaryPrimitives.ReadUInt64LittleEndian( data[(offset + 8)..] ) );
					c = Round( c, BinaryPrimitives.ReadUInt64LittleEndian( data[(offset + 16)..] ) );
					d = Round( d, BinaryPrimitives.ReadUInt64LittleEndian( data[(offset + 24)..] ) );
					offset += 32;
				}
				hash = Rotate( a, 1 ) + Rotate( b, 7 ) + Rotate( c, 12 ) + Rotate( d, 18 );
				hash = Merge( Merge( Merge( Merge( hash, a ), b ), c ), d );
			}
			hash += (ulong)data.Length;
			while ( offset <= data.Length - 8 )
			{
				hash = Rotate( hash ^ Round( 0, BinaryPrimitives.ReadUInt64LittleEndian( data[offset..] ) ), 27 ) * p1 + p4;
				offset += 8;
			}
			if ( offset <= data.Length - 4 )
			{
				hash = Rotate( hash ^ BinaryPrimitives.ReadUInt32LittleEndian( data[offset..] ) * p1, 23 ) * p2 + p3;
				offset += 4;
			}
			while ( offset < data.Length ) hash = Rotate( hash ^ data[offset++] * p5, 11 ) * p1;
			hash ^= hash >> 33; hash *= p2; hash ^= hash >> 29; hash *= p3; hash ^= hash >> 32;
			return (long)hash;
		}
	}
}
