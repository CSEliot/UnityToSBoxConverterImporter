using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace ImportUnityPackage;

/// <summary>Repairs a redundant array emitted by some legacy ASCII FBX exporters.</summary>
public static class UnityFbxCompatibility
{
	public static byte[] Normalize( string source, CancellationToken cancel )
	{
		// Ordinary/binary models remain byte-for-byte copies. Bound optional text processing.
		using var stream = File.OpenRead( source );
		if ( stream.Length > 32 * 1024 * 1024 ) return null;
		var header = new byte[32];
		var length = stream.Read( header );
		if ( !Encoding.ASCII.GetString( header, 0, length ).StartsWith( "; FBX ", StringComparison.Ordinal ) ) return null;
		stream.Position = 0;
		using var reader = new StreamReader( stream, new UTF8Encoding( false, true ), false );
		string text;
		try { text = reader.ReadToEnd(); }
		catch ( DecoderFallbackException ) { return null; }
		return NormalizeText( text, cancel ) is string normalized ? Encoding.UTF8.GetBytes( normalized ) : null;
	}

	internal static string NormalizeText( string text, CancellationToken cancel )
	{
		cancel.ThrowIfCancellationRequested();
		const string numbers = @"[-+0-9.eE,\s]+";
		var timeout = TimeSpan.FromSeconds( 2 );
		var tails = Regex.Matches( text, @"(?m)^[ \t]*}(?<tail>,[-+0-9.eE, \t]+)(?=\r?$)", RegexOptions.None, timeout );
		if ( tails.Count == 0 ) return null;
		string Compact( string value ) => string.Concat( value.Where( c => !char.IsWhiteSpace( c ) ) ).Trim( ',' );
		var vertices = Regex.Matches( text, @"(?m)^[ \t]*Vertices:[ \t]*(" + numbers + ")", RegexOptions.None, timeout )
			.Select( m => Compact( m.Groups[1].Value ) ).Where( s => s.Length > 0 ).ToHashSet( StringComparer.Ordinal );
		var output = new StringBuilder( text );
		var changed = false;
		foreach ( Match match in tails.Reverse() )
		{
			cancel.ThrowIfCancellationRequested();
			var tail = match.Groups["tail"];
			// Only discard an exact repeated vertex sequence at an invalid syntax position.
			// Different data is left untouched so an uncertain repair cannot alter geometry.
			if ( !vertices.Contains( Compact( tail.Value ) ) ) continue;
			output.Remove( tail.Index, tail.Length );
			changed = true;
		}
		return changed ? output.ToString() : null;
	}
}
