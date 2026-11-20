using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace ImportUnityPackage;

/// <summary>Legacy ASCII FBX attaches texture/material objects to the mesh by name.</summary>
internal sealed class UnityAsciiFbxLinks
{
	readonly Dictionary<string, string> textures = new( StringComparer.Ordinal );
	readonly List<(string Child, string Parent)> connections = new();
	int depth;
	int objectsDepth = -1;
	int textureDepth = -1;
	string texture;

	public void Observe( string line )
	{
		if ( Regex.IsMatch( line, @"^\s*Objects:\s*\{" ) ) objectsDepth = depth + 1;
		if ( objectsDepth > 0 && depth == objectsDepth )
		{
			var start = Regex.Match( line, "^\\s*Texture:\\s*\"(Texture::[^\"]+)\"" );
			if ( start.Success ) { texture = start.Groups[1].Value; textureDepth = depth + 1; }
		}
		if ( texture != null && depth >= textureDepth )
		{
			var file = Regex.Match( line, "^\\s*(?:FileName|Filename|RelativeFilename):\\s*\"([^\"]+)\"" );
			if ( file.Success ) textures[texture] = file.Groups[1].Value.Replace( '\\', '/' ).Split( '/' ).Last();
		}
		var connection = Regex.Match( line, "^\\s*Connect:\\s*\"OO\",\\s*\"([^\"]+)\",\\s*\"([^\"]+)\"" );
		if ( connection.Success ) connections.Add( (connection.Groups[1].Value, connection.Groups[2].Value) );
		var structural = line.Contains( '"' ) ? Regex.Replace( line, "\"[^\"]*\"", "" ) : line;
		structural = structural.Split( ';' )[0];
		depth += structural.Count( c => c == '{' ) - structural.Count( c => c == '}' );
		if ( depth < textureDepth ) { texture = null; textureDepth = -1; }
		if ( depth < objectsDepth ) objectsDepth = -1;
	}

	public IEnumerable<KeyValuePair<string, string>> Resolve()
	{
		var candidates = new List<(string Material, string File)>();
		foreach ( var mesh in connections.Where( c => c.Parent.StartsWith( "Model::", StringComparison.Ordinal ) ).GroupBy( c => c.Parent ) )
		{
			var materials = mesh.Select( c => c.Child ).Where( c => c.StartsWith( "Material::", StringComparison.Ordinal ) ).Distinct().ToArray();
			var maps = mesh.Select( c => c.Child ).Where( textures.ContainsKey ).Distinct().ToArray();
			// Without polygon-slot information, multiple materials/textures are ambiguous.
			if ( materials.Length == 1 && maps.Length == 1 ) candidates.Add( (materials[0]["Material::".Length..], textures[maps[0]]) );
		}
		foreach ( var material in candidates.GroupBy( c => c.Material ) )
		{
			var files = material.Select( c => c.File ).Distinct( StringComparer.OrdinalIgnoreCase ).ToArray();
			if ( files.Length == 1 ) yield return new( material.Key, files[0] );
		}
	}
}
