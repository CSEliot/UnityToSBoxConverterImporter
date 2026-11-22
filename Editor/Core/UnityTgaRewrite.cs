using System;

namespace ImportUnityPackage;

/// <summary>
/// Expands the one TGA variant that s&amp;box's loader rejects: an uncompressed (type 2) 16-bpp
/// image. Bitmap.Loading.Tga.cs accepts type 2 only at 24 or 32 bits per pixel, so a 5:5:5:1
/// image is left verbatim by the importer and fails with "Unknown Format!" in the editor.
/// Every other TGA variant is returned unchanged; this keeps the rewrite narrow and avoids
/// pretending to repair a file whose payload this code cannot interpret.
/// </summary>
internal static class UnityTgaRewrite
{
	const int HeaderLength = 18;
	const int FooterLength = 26;
	const int MaxSize = 8192;
	const string FooterSignature = "TRUEVISION-XFILE.";

	/// <summary>
	/// Returns normalized bytes for a type-2 16-bpp TGA, otherwise null (leave the file verbatim).
	/// reason is empty when the file needs no attention (already supported, or not a TGA) and
	/// names the variant when a recognized TGA cannot be normalized, so the caller can report it.
	/// </summary>
	public static byte[] Normalize( byte[] data, out string reason )
	{
		reason = "";
		if ( data is null || data.Length < HeaderLength ) return null;

		int idLength = data[0];
		int colorMapType = data[1];
		int imageType = data[2];
		int colorMapLength = data[3] | ( data[4] << 8 );
		int width = data[12] | ( data[13] << 8 );
		int height = data[14] | ( data[15] << 8 );
		int pixelSize = data[16];
		int descriptor = data[17];

		// IsTga() only recognizes types 2, 3 and 10 with positive dimensions.
		if ( imageType is not ( 2 or 3 or 10 ) || width <= 0 || height <= 0 ) return null;

		if ( imageType == 3 )
		{
			if ( pixelSize != 8 ) reason = $"type-3 grayscale TGA at {pixelSize} bits per pixel";
			return null;
		}
		if ( imageType == 10 )
		{
			if ( pixelSize is not ( 24 or 32 ) ) reason = $"RLE type-10 TGA at {pixelSize} bits per pixel";
			return null;
		}
		if ( pixelSize != 16 )
		{
			// Types 2 at 24/32 are already loadable. A colormap or an oversized image is not,
			// but this rewrite deliberately does not guess at those payloads.
			if ( pixelSize is not ( 24 or 32 ) ) reason = $"type-2 TGA at {pixelSize} bits per pixel";
			else if ( colorMapType != 0 || colorMapLength != 0 ) reason = "color-mapped type-2 TGA";
			else if ( width > MaxSize || height > MaxSize || (long)width * height > (long)MaxSize * MaxSize ) reason = "type-2 TGA over the 8192x8192 limit";
			return null;
		}

		// Only uncompressed 5:5:5:1 truecolor with no colormap and a complete payload is safe to expand.
		if ( colorMapType != 0 || colorMapLength != 0 ) { reason = "color-mapped 16-bit TGA"; return null; }
		if ( width > MaxSize || height > MaxSize || (long)width * height > (long)MaxSize * MaxSize ) { reason = "16-bit TGA over the 8192x8192 limit"; return null; }
		long required = HeaderLength + idLength + (long)width * height * 2;
		if ( data.Length < required ) { reason = "truncated 16-bit TGA payload"; return null; }

		bool hasFooter = data.Length == required + FooterLength
			&& HasFooter( data, data.Length - FooterLength );
		int pixels = width * height;
		var result = new byte[HeaderLength + idLength + pixels * 4 + ( hasFooter ? FooterLength : 0 )];

		result[0] = data[0];   // keep the image ID field, if any
		result[1] = 0;         // no color map after the rewrite
		result[2] = 2;         // uncompressed truecolor
		result[8] = data[8];   // origin X
		result[9] = data[9];
		result[12] = data[12]; // width
		result[13] = data[13];
		result[14] = data[14]; // height
		result[15] = data[15];
		result[16] = 32;
		// Keep the vertical-origin bit and declare 8 attribute bits: the rewritten payload
		// carries a full alpha byte where the 16-bpp source had one attribute bit.
		result[17] = (byte)( ( descriptor & 0x30 ) | 0x08 );
		if ( idLength > 0 ) Array.Copy( data, HeaderLength, result, HeaderLength, idLength );

		// A 16-bpp TGA pixel is little-endian ARRRRRGGGGGBBBBB: bits 10-14 red, 5-9 green,
		// 0-4 blue and bit 15 the single attribute bit. Expand each 5-bit channel to 8 bits
		// by replicating its top three bits into the low three. Alpha is INVERTED: Pillow
		// decodes 16-bpp TGA with rawmode BGRA;15Z (PIL/TgaImagePlugin.py:41,161-174), where
		// bit 15 CLEAR means OPAQUE and SET means TRANSPARENT, so clear -> 255, set -> 0.
		// The output channel order is BGRA, which is what the 32-bpp TGA payload expects.
		int source = HeaderLength + idLength;
		int target = source;
		for ( int i = 0; i < pixels; i++, source += 2, target += 4 )
		{
			int value = data[source] | ( data[source + 1] << 8 );
			result[target] = Expand( value & 0x1f );
			result[target + 1] = Expand( ( value >> 5 ) & 0x1f );
			result[target + 2] = Expand( ( value >> 10 ) & 0x1f );
			result[target + 3] = ( value & 0x8000 ) != 0 ? (byte)0 : (byte)255;
		}

		if ( hasFooter )
		{
			// The extension and developer-directory offsets are zero: those blocks are not
			// copied, and a stale offset would point past the rewritten payload.
			int footer = result.Length - FooterLength;
			for ( int i = 0; i < FooterSignature.Length; i++ ) result[footer + 8 + i] = (byte)FooterSignature[i];
			result[result.Length - 1] = 0;
		}
		return result;
	}

	static bool HasFooter( byte[] data, int offset )
	{
		for ( int i = 0; i < FooterSignature.Length; i++ )
			if ( data[offset + 8 + i] != (byte)FooterSignature[i] ) return false;
		return true;
	}

	static byte Expand( int fiveBits ) => (byte)( ( fiveBits << 3 ) | ( fiveBits >> 2 ) );
}
