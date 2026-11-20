using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace ImportUnityPackage;

/// <summary>Best-effort conversion of text-serialized Standard/URP material properties.</summary>
public sealed class UnityMaterial
{
	public Dictionary<string, string> Textures { get; } = new();
	public Dictionary<string, double> Numbers { get; } = new();
	public Dictionary<string, double[]> Colors { get; } = new();
	public List<string> Warnings { get; } = new();
	public List<string> Information { get; } = new();
	readonly Dictionary<string, string> channelImages = new();
	readonly Dictionary<string, double[]> textureScale = new();
	readonly Dictionary<string, double[]> textureOffset = new();
	bool shaderAlphaTest;
	bool shaderBackfaces;
	bool shaderTranslucent;
	bool terrainLayer;
	HashSet<string> declaredProperties;
	static readonly string[] ColorProperties = { "_BaseMap", "_Diffuse", "_MainTex", "_Albedo", "_BaseColorMap" };
	public IEnumerable<string> ColorTextureGuids => ColorProperties.Where( Textures.ContainsKey ).Select( p => Textures[p] );
	bool AlphaTest => shaderAlphaTest || Number( "_Mode", 0 ) == 1 || Number( "_AlphaClip", 0 ) == 1;
	bool Translucent => shaderTranslucent || Number( "_Mode", 0 ) >= 2 || Number( "_Surface", 0 ) == 1;
	public string ShaderName { get; private set; }
	public string ShaderGuid { get; private set; }
	static readonly Regex GuidPattern = new( @"guid:\s*([a-fA-F0-9]{32})" );

	public static IEnumerable<string> References( string text ) => GuidPattern.Matches( text )
		.Select( m => m.Groups[1].Value ).Where( g => g.Any( c => c != '0' ) ).Distinct( StringComparer.OrdinalIgnoreCase );

	public static UnityMaterial Parse( string text )
	{
		if ( text.Contains( "TerrainLayer:" ) && !text.Contains( '\0' ) ) return ParseTerrainLayer( text );
		if ( !text.Contains( "Material:" ) || text.Contains( '\0' ) )
			throw new InvalidDataException( "Material is not Unity text YAML. Re-export it with Asset Serialization set to Force Text in Unity." );
		var result = new UnityMaterial();
		result.ShaderGuid = Regex.Match( text, @"m_Shader:[^\r\n]*guid:\s*([a-fA-F0-9]{32})" ).Groups[1].Value;
		string property = null;
		foreach ( var line in text.Split( '\n' ) )
		{
			var item = Regex.Match( line, @"^\s*-\s*(_[A-Za-z0-9_]+):\s*(.*)$" );
			if ( item.Success )
			{
				property = item.Groups[1].Value;
				var value = item.Groups[2].Value.Trim();
				if ( double.TryParse( value, NumberStyles.Float, CultureInfo.InvariantCulture, out var number ) && double.IsFinite( number ) )
					result.Numbers[property] = number;
				if ( value.StartsWith( "{" ) )
				{
					var channels = Regex.Matches( value, @"[rgba]:\s*([-+0-9.eE]+)" );
					if ( channels.Count == 4 ) result.Colors[property] = channels.Select( m => double.Parse( m.Groups[1].Value, CultureInfo.InvariantCulture ) ).ToArray();
				}
			}
			if ( property != null && line.Contains( "m_Texture:" ) )
			{
				var guid = GuidPattern.Match( line );
				if ( guid.Success && guid.Groups[1].Value.Any( c => c != '0' ) ) result.Textures[property] = guid.Groups[1].Value;
			}
			if ( property != null && (line.Contains( "m_Scale:" ) || line.Contains( "m_Offset:" )) )
			{
				var xy = Regex.Match( line, @"x:\s*([-+0-9.eE]+),\s*y:\s*([-+0-9.eE]+)" );
				if ( xy.Success ) (line.Contains( "m_Scale:" ) ? result.textureScale : result.textureOffset)[property] =
					new[] { double.Parse( xy.Groups[1].Value, CultureInfo.InvariantCulture ), double.Parse( xy.Groups[2].Value, CultureInfo.InvariantCulture ) };
			}
			if ( line.Contains( "m_Scale:" ) && !Regex.IsMatch( line, @"x:\s*1(?:\.0+)?\s*,\s*y:\s*1(?:\.0+)?\s*}" ) ||
				line.Contains( "m_Offset:" ) && !Regex.IsMatch( line, @"x:\s*0(?:\.0+)?\s*,\s*y:\s*0(?:\.0+)?\s*}" ) )
				result.Warnings.Add( "Texture tiling/offset needs manual adjustment." );
		}
		if ( result.Textures.ContainsKey( "_MetallicGlossMap" ) || result.Textures.ContainsKey( "_MaskMap" ) )
			result.Warnings.Add( "Packed metallic/smoothness or HDRP mask maps need channel separation; scalar metallic/roughness values were used." );
		result.Information.Add( "Review the converted material: custom shaders, normal-map conventions and advanced Unity settings are not reproduced exactly." );
		return result;
	}

	public void ConfigureShader( string source )
	{
		ShaderName = Regex.Match( source, "Shader\\s+\"([^\"]+)\"" ).Groups[1].Value;
		declaredProperties = Regex.Matches( source, "(?m)^\\s*(?:\\[[^\\]\\r\\n]*\\]\\s*)*(_[A-Za-z0-9_]+)\\s*\\(\\s*\"[^\"]*\"\\s*," )
			.Select( m => m.Groups[1].Value ).ToHashSet();
		if ( declaredProperties.Count > 0 )
		{
			// Unity retains old shader properties in materials. Only use properties declared by the active shader.
			foreach ( var stale in Textures.Keys.Where( k => !declaredProperties.Contains( k ) ).ToArray() ) Textures.Remove( stale );
		}
		shaderAlphaTest = Regex.IsMatch( source, "\"RenderType\"\\s*=\\s*\"TransparentCutout\"" );
		shaderBackfaces = Regex.IsMatch( source, @"(?m)^\s*Cull\s+Off\s*$", RegexOptions.IgnoreCase );
		shaderTranslucent = Regex.IsMatch( source, "\"RenderType\"\\s*=\\s*\"Transparent\"" );
		if ( !string.IsNullOrEmpty( ShaderName ) ) Warnings.Add( $"Shader '{ShaderName}' is approximated using s&box's complex shader; shader code and graph behavior are not translated." );
	}

	public void PrepareChannels( Func<string, int, double, bool, string> extract )
	{
		void Channel( string target, string guid, int channel, double scale = 1, bool invert = false )
		{
			var path = extract( guid, channel, scale, invert );
			if ( path != null ) channelImages[target] = path;
		}
		if ( Textures.TryGetValue( "_MetallicGlossMap", out var packed ) )
		{
			Channel( "metal", packed, 0 );
			Channel( "rough", packed, 3, Number( "_GlossMapScale", Number( "_Smoothness", 1 ) ), true );
		}
		if ( Textures.TryGetValue( "_MaskMap", out var mask ) )
		{
			Channel( "metal", mask, 0 );
			Channel( "ao", mask, 1 );
			// TerrainLit stores height in B; HDRP Lit stores a detail mask instead.
			if ( terrainLayer ) Channel( "height", mask, 2 );
			Channel( "rough", mask, 3, 1, true );
		}
		if ( Textures.TryGetValue( "_OcclusionMap", out var ao ) ) Channel( "ao", ao, 1 );
		if ( AlphaTest || Translucent )
		{
			foreach ( var property in ColorProperties )
			{
				if ( !Textures.TryGetValue( property, out var guid ) ) continue;
				Channel( "opacity", guid, 3 );
				if ( channelImages.ContainsKey( "opacity" ) ) break;
			}
		}
		if ( channelImages.Count > 0 ) Warnings.RemoveAll( w => w.StartsWith( "Packed metallic/" ) || w.StartsWith( "Unity terrain mask channels" ) );
	}

	static UnityMaterial ParseTerrainLayer( string text )
	{
		var result = new UnityMaterial { terrainLayer = true };
		foreach ( var (source, target) in new[] { ("m_DiffuseTexture", "_MainTex"), ("m_NormalMapTexture", "_BumpMap"), ("m_MaskMapTexture", "_MaskMap") } )
		{
			var line = text.Split( '\n' ).FirstOrDefault( l => l.TrimStart().StartsWith( source + ":", StringComparison.Ordinal ) );
			if ( line != null && References( line ).FirstOrDefault() is string guid ) result.Textures[target] = guid;
		}
		foreach ( var (source, target) in new[] { ("m_Metallic", "_Metallic"), ("m_Smoothness", "_Smoothness") } )
		{
			var match = Regex.Match( text, @"(?m)^\s*" + source + @":\s*([-+0-9.eE]+)" );
			if ( match.Success && double.TryParse( match.Groups[1].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var value ) ) result.Numbers[target] = value;
		}
		result.Warnings.Add( "Terrain layer converted; review world tiling, normal strength and remapping in the terrain material editor." );
		if ( result.Textures.ContainsKey( "_MaskMap" ) ) result.Warnings.Add( "Unity terrain mask channels need separation; packed mask was preserved as a source texture." );
		return result;
	}

	string Texture( Func<string, string> resolve, params string[] names )
	{
		foreach ( var name in names )
			if ( Textures.TryGetValue( name, out var guid ) && resolve( guid ) is string path ) return path;
		return null;
	}
	public double Number( string name, double fallback ) => Numbers.GetValueOrDefault( name, fallback );
	// The material editor writes fixed-precision scalars and six-decimal colour channels.
	static string F( double value ) => (double.IsFinite( value ) ? value : 0).ToString( "0.000", CultureInfo.InvariantCulture );
	static string C( double value ) => (double.IsFinite( value ) ? value : 0).ToString( "0.000000", CultureInfo.InvariantCulture );
	internal static string Quote( string value ) => JsonSerializer.Serialize( value );

	/// <summary>The albedo texture a generated material should be named after, if it has an unambiguous one.</summary>
	public string ColorTextureGuid => ColorProperties.Where( Textures.ContainsKey ).Select( p => Textures[p] )
		.Distinct( StringComparer.OrdinalIgnoreCase ).Take( 2 ).ToArray() is { Length: 1 } single ? single[0] : null;

	public string ToVmat( Func<string, string> resolve )
	{
		// An untextured transparent Unity material is window glass; s&box draws that with its glass shader.
		if ( Translucent && ColorProperties.All( p => !Textures.ContainsKey( p ) ) ) return GlassVmat( resolve );

		// Grouped, sorted sections with all shader defaults present: the on-disk shape s&box's
		// material editor writes, so a generated material round-trips through it without churn.
		var sections = new List<(string Name, List<(string Key, string Value)> Values)>();
		List<(string, string)> Section( string name )
		{
			var values = new List<(string, string)>();
			sections.Add( (name, values) );
			return values;
		}
		var features = new List<string>();
		void Feature( string name ) => features.Add( name );

		var colorProperty = ColorProperties.FirstOrDefault( p => Textures.TryGetValue( p, out var guid ) && resolve( guid ) != null );
		var metal = channelImages.GetValueOrDefault( "metal" );
		var emission = Texture( resolve, "_EmissionMap", "_EmissiveColorMap" );
		if ( metal != null ) Feature( "F_METALNESS_TEXTURE 1" );
		if ( AlphaTest ) Feature( "F_ALPHA_TEST 1" );
		if ( shaderBackfaces || Number( "_Cull", 2 ) == 0 ) Feature( "F_RENDER_BACKFACES 1" );
		if ( Translucent ) Feature( "F_TRANSLUCENT 1" );
		if ( emission != null ) Feature( "F_SELF_ILLUM 1" );

		var ao = Section( "Ambient Occlusion" );
		ao.Add( ("g_flAmbientOcclusionDirectDiffuse", Quote( F( 0 ) )) );
		ao.Add( ("g_flAmbientOcclusionDirectSpecular", Quote( F( 0 ) )) );
		ao.Add( ("TextureAmbientOcclusion", Quote( channelImages.GetValueOrDefault( "ao" ) ?? Texture( resolve, "_OcclusionMap", "_Occlusion" ) ?? "materials/default/default_ao.tga" )) );

		if ( AlphaTest )
		{
			var alpha = Section( "Alpha Test" );
			alpha.Add( ("g_flAlphaTestReference", Quote( F( Number( "_Cutoff", 0.5 ) ) )) );
			alpha.Add( ("g_flAntiAliasedEdgeStrength", Quote( F( 1 ) )) );
		}

		var color = Section( "Color" );
		color.Add( ("g_flModelTintAmount", Quote( F( 1 ) )) );
		var tint = new[] { 1.0, 1.0, 1.0, 1.0 };
		if ( Colors.TryGetValue( "_BaseColor", out var baseColor ) || colorProperty == "_Diffuse" && Colors.TryGetValue( "_MainColor", out baseColor ) || Colors.TryGetValue( "_Color", out baseColor ) ) tint = baseColor;
		color.Add( ("g_vColorTint", Quote( $"[{C( tint[0] )} {C( tint[1] )} {C( tint[2] )} {C( tint[3] )}]" )) );
		color.Add( ("TextureColor", Quote( Texture( resolve, ColorProperties ) ?? "materials/default/default_color.tga" )) );

		var fade = Section( "Fade" );
		fade.Add( ("g_flFadeExponent", Quote( F( 1 ) )) );

		var fog = Section( "Fog" );
		fog.Add( ("g_bFogEnabled", Quote( "1" )) );

		var metalness = Section( "Metalness" );
		if ( metal != null ) metalness.Add( ("TextureMetalness", Quote( metal )) );
		else metalness.Add( ("g_flMetalness", Quote( F( Math.Clamp( Number( "_Metallic", 0 ), 0, 1 ) ) )) );

		var normal = Section( "Normal" );
		var normalMap = Texture( resolve, "_BumpMap", "_Normal", "_NormalMap" );
		if ( normalMap != null )
		{
			normal.Add( ("g_flLightRangeForSelfShadowNormals", Quote( F( 0.707 ) )) );
			normal.Add( ("g_flNormalMapScaleFactor", Quote( F( Number( "_BumpScale", 1 ) ) )) );
		}
		normal.Add( ("TextureNormal", Quote( normalMap ?? "materials/default/default_normal.tga" )) );

		var roughness = Section( "Roughness" );
		roughness.Add( ("g_flRoughnessScaleFactor", Quote( F( channelImages.ContainsKey( "rough" ) ? 1 : 1 - Math.Clamp( Number( "_Smoothness", Number( "_Glossiness", 0.5 ) ), 0, 1 ) ) )) );
		roughness.Add( ("TextureRoughness", Quote( channelImages.GetValueOrDefault( "rough" ) ?? "materials/default/default_rough.tga" )) );

		if ( emission != null )
		{
			var illum = Section( "Self Illum" );
			illum.Add( ("g_flSelfIllumAlbedoFactor", Quote( F( 1 ) )) );
			illum.Add( ("g_flSelfIllumBrightness", Quote( F( 0 ) )) );
			illum.Add( ("g_flSelfIllumScale", Quote( F( 1 ) )) );
			illum.Add( ("g_vSelfIllumOffset", Quote( $"[{F( 0 )} {F( 0 )}]" )) );
			illum.Add( ("g_vSelfIllumScrollSpeed", Quote( $"[{F( 0 )} {F( 0 )}]" )) );
			var glow = Colors.GetValueOrDefault( "_EmissionColor", new[] { 1.0, 1.0, 1.0, 1.0 } );
			// Unity stores an unused emission colour as black; the mask texture is the real signal,
			// and a black tint here would silently multiply the self-illum away.
			if ( glow.Take( 3 ).All( c => c <= 0 ) ) glow = new[] { 1.0, 1.0, 1.0, glow[3] };
			illum.Add( ("g_vSelfIllumTint", Quote( $"[{C( glow[0] )} {C( glow[1] )} {C( glow[2] )} {C( glow[3] )}]" )) );
			illum.Add( ("TextureSelfIllumMask", Quote( emission )) );
		}

		if ( channelImages.TryGetValue( "opacity", out var opacity ) )
		{
			var translucent = Section( "Translucent" );
			translucent.Add( ("TextureTranslucency", Quote( opacity )) );
		}

		var coordinates = Section( "Texture Coordinates" );
		var scale = colorProperty != null ? textureScale.GetValueOrDefault( colorProperty ) : null;
		var offset = colorProperty != null ? textureOffset.GetValueOrDefault( colorProperty ) : null;
		coordinates.Add( ("g_vTexCoordOffset", Quote( $"[{F( offset?[0] ?? 0 )} {F( offset?[1] ?? 0 )}]" )) );
		coordinates.Add( ("g_vTexCoordScale", Quote( $"[{F( scale?[0] ?? 1 )} {F( scale?[1] ?? 1 )}]" )) );
		coordinates.Add( ("g_vTexCoordScrollSpeed", Quote( $"[{F( 0 )} {F( 0 )}]" )) );

		var text = new StringBuilder( "// THIS FILE IS AUTO-GENERATED\n\nLayer0\n{\n	shader \"shaders/complex.shader\"\n" );
		if ( features.Count > 0 )
		{
			text.Append( "\n	//---- PBR ----\n" );
			foreach ( var feature in features ) text.Append( "	" ).Append( feature ).Append( '\n' );
		}
		foreach ( var (name, values) in sections.OrderBy( s => s.Name, StringComparer.Ordinal ) )
		{
			if ( values.Count == 0 ) continue;
			text.Append( $"\n	//---- {name} ----\n" );
			foreach ( var (key, value) in values ) text.Append( '	' ).Append( key ).Append( ' ' ).Append( value ).Append( '\n' );
		}
		// The material editor writes no trailing newline after the closing brace.
		return text.Append( '}' ).ToString();
	}

	/// <summary>
	/// shaders/glass.shader in the material editor's on-disk shape. Glass takes constant colours
	/// in place of texture paths, so Unity's base colour and alpha become flat texture values.
	/// </summary>
	string GlassVmat( Func<string, string> resolve )
	{
		string Flat( double r, double g, double b, double a ) => Quote( $"[{C( r )} {C( g )} {C( b )} {C( a )}]" );
		var color = Colors.GetValueOrDefault( "_BaseColor" ) ?? Colors.GetValueOrDefault( "_Color" ) ?? new[] { 1.0, 1.0, 1.0, 1.0 };
		var metal = Math.Clamp( Number( "_Metallic", 0 ), 0, 1 );
		var rough = 1 - Math.Clamp( Number( "_Smoothness", Number( "_Glossiness", 0.5 ) ), 0, 1 );
		var normal = Texture( resolve, "_BumpMap", "_Normal", "_NormalMap" ) ?? "materials/default/default_normal.tga";
		var text = new StringBuilder( "// THIS FILE IS AUTO-GENERATED\n\nLayer0\n{\n	shader \"shaders/glass.shader\"\n" );
		text.Append( "\n	//---- Blending ----\n	F_ADDITIVE_BLEND 1\n" );
		text.Append( "\n	//---- Glass ----\n	F_GLASS_QUALITY 1 // Simple Glass ( Faster To Render )\n" );
		text.Append( "\n	//---- Fog ----\n	g_bFogEnabled \"1\"\n" );
		text.Append( "\n	//---- Glass ----\n	g_flRefractionStrength \"1.005\"\n" );
		text.Append( "\n	//---- Material ----\n" );
		foreach ( var (key, value) in new[]
		{
			("g_flTintColor", Flat( 1, 1, 1, 0 )),
			("TextureAmbientOcclusion", Flat( 1, 1, 1, 0 )),
			("TextureBlendMask", Flat( 1, 1, 1, 0 )),
			("TextureColor", Flat( color[0], color[1], color[2], color[3] )),
			("TextureMetalness", Flat( metal, metal, metal, 0 )),
			("TextureNormal", Quote( normal )),
			("TextureRoughness", Flat( rough, rough, rough, 0 )),
			("TextureTintMask", Flat( 1, 1, 1, 0 )),
			("TextureTranslucency", Flat( color[3], color[3], color[3], 0 )),
		} ) text.Append( '	' ).Append( key ).Append( ' ' ).Append( value ).Append( '\n' );
		return text.Append( '}' ).ToString();
	}

	public string ToTmat( Func<string, string> resolve )
	{
		// TerrainMaterial image fields are source image paths, not .vmat references.
		var values = new Dictionary<string, object>
		{
			["__version"] = 1,
			["AlbedoImage"] = "materials/default/default_color.tga",
			["NormalImage"] = "materials/default/default_normal.tga",
			["RoughnessImage"] = "materials/default/default_rough.tga",
			["AOImage"] = "materials/default/default_ao.tga",
			["HeightImage"] = "materials/default/default_height.tga"
		};
		void Set( string key, params string[] names )
		{
			var value = Texture( resolve, names );
			if ( value != null ) values[key] = value;
		}
		Set( "AlbedoImage", "_BaseMap", "_Diffuse", "_MainTex", "_Albedo", "_BaseColorMap" );
		Set( "NormalImage", "_BumpMap", "_Normal", "_NormalMap" );
		Set( "AOImage", "_OcclusionMap", "_Occlusion" );
		Set( "HeightImage", "_ParallaxMap", "_HeightMap" );
		foreach ( var (channel, field) in new[] { ("rough", "RoughnessImage"), ("ao", "AOImage"), ("height", "HeightImage") } )
			if ( channelImages.TryGetValue( channel, out var image ) ) values[field] = image;
		return JsonSerializer.Serialize( values, new JsonSerializerOptions { WriteIndented = true } );
	}
}
