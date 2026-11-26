using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ImportUnityPackage;

/// <summary>The render pipeline a package's assets were authored for, as far as the archive can say.</summary>
public enum UnityPipeline
{
	Unknown,
	Hdrp,
	Urp,
}

/// <summary>
/// What the importer knows HDRP's scene components are. HDRP keeps a light's physical unit, a probe's
/// settings and a Volume's overrides in these MonoBehaviours, so naming them is the difference between
/// "this scene uses HDRP" and an anonymous script line.
/// </summary>
public enum HdrpComponentKind
{
	Light,
	ReflectionProbe,
	Camera,
	Volume,
	StaticLightingSky,
	PlanarReflectionProbe,
	DecalProjector,
}

/// <summary>
/// A pipeline shader recognised by guid. Its source lives in Packages/, which Unity never exports, so
/// the archive contains no .shader entry and the importer can only know it from this table: the display
/// name and the properties the shader is known to declare.
/// </summary>
public sealed record UnityPipelineShader( string Guid, string Name, string[] Properties );

/// <summary>A pipeline component recognised by the guid of the script an archive's MonoBehaviour names.</summary>
public sealed record HdrpComponent( string Guid, string Name, HdrpComponentKind Kind );

/// <summary>
/// What one import saw of HDRP. <see cref="Report"/> is the single line the import writes when any of
/// these is non-zero; there is no per-material line, because an HDRP project has hundreds.
/// </summary>
public sealed record HdrpCounts( int Materials, int Lights, int ReflectionProbes, int Volumes )
{
	public int Total => Materials + Lights + ReflectionProbes + Volumes;

	public string Report => $"HDRP detected: {Materials} HDRP material(s), {Lights} HD light(s), {ReflectionProbes} HD reflection probe(s), {Volumes} volume(s).";

	public static HdrpCounts operator +( HdrpCounts a, HdrpCounts b ) =>
		new( a.Materials + b.Materials, a.Lights + b.Lights, a.ReflectionProbes + b.ReflectionProbes, a.Volumes + b.Volumes );
}

/// <summary>
/// The render-pipeline facts the importer cannot read from a .unitypackage: HDRP's shader guids (their
/// source is in Packages/) and its component script guids (the scripts are compiled, not shipped).
/// All guids are the ones HDRP 17 (Unity 6) assigns, verified against the package cache of a real
/// project; they are stable across projects because Unity derives them from the asset, not the project.
/// </summary>
public static class UnityHdrp
{
	// HDRP/Lit properties and feature keywords the converter cares about. This is a curated subset, not
	// the shader's full 150-property declaration list; the two _MATERIAL_FEATURE_* names are keywords.
	static readonly string[] LitProperties =
	{
		"_BaseColor", "_BaseColorMap", "_Metallic", "_Smoothness", "_MaskMap", "_MetallicRemapMin", "_MetallicRemapMax",
		"_SmoothnessRemapMin", "_SmoothnessRemapMax", "_AlphaRemapMin", "_AlphaRemapMax", "_AORemapMin", "_AORemapMax",
		"_NormalMap", "_NormalMapOS", "_NormalScale", "_BentNormalMap", "_HeightMap", "_DetailMap", "_DetailAlbedoScale",
		"_DetailNormalScale", "_DetailSmoothnessScale", "_SpecularColor", "_SpecularColorMap", "_SpecularOcclusionMode",
		"_EmissiveColor", "_EmissiveColorMap", "_AlbedoAffectEmissive", "_EmissiveIntensityUnit", "_UseEmissiveIntensity",
		"_EmissiveIntensity", "_EmissiveExposureWeight", "_AlphaCutoffEnable", "_AlphaCutoff", "_TransparentDepthPrepassEnable",
		"_TransparentBackfaceEnable", "_TransparentDepthPostpassEnable", "_TransparentSortPriority", "_RefractionModel", "_Ior",
		"_TransmittanceColor", "_TransmittanceColorMap", "_ATDistance", "_SurfaceType", "_BlendMode", "_ZWrite", "_CullMode",
		"_DoubleSidedEnable", "_DoubleSidedNormalMode", "_DoubleSidedConstants", "_DoubleSidedGIMode", "_UVBase", "_MaterialID",
		"_TransmissionEnable", "_MATERIAL_FEATURE_TRANSMISSION", "_MATERIAL_FEATURE_SUBSURFACE_SCATTERING",
		"_EnableFogOnTransparent", "_EnableBlendModePreserveSpecularLighting"
	};

	static readonly string[] TessellationProperties = LitProperties.Concat( new[]
	{
		"_HeightAmplitude", "_HeightCenter", "_HeightMapParametrization", "_HeightOffset", "_HeightMin", "_HeightMax",
		"_HeightTessAmplitude", "_HeightTessCenter", "_HeightPoMAmplitude"
	} ).ToArray();

	static readonly UnityPipelineShader[] ShaderTable =
	{
		new( "6e4ae4064600d784cac1e41a9e6f2e59", "HDRP/Lit", LitProperties ),
		new( "c4edd00ff2db5b24391a4fcb1762e459", "HDRP/Unlit", new[]
		{
			"_UnlitColor", "_UnlitColorMap", "_AlphaRemapMin", "_AlphaRemapMax", "_EmissiveColor", "_EmissiveColorMap",
			"_AlbedoAffectEmissive", "_UseEmissiveIntensity", "_EmissiveIntensity", "_EmissiveExposureWeight",
			"_AlphaCutoffEnable", "_AlphaCutoff", "_SurfaceType", "_BlendMode", "_ZWrite", "_CullMode", "_DoubleSidedEnable",
			"_TransparentSortPriority", "_EnableFogOnTransparent"
		} ),
		new( "756bac9090102564582875f4c7e30202", "HDRP/LitTessellation", TessellationProperties ),
		new( "1d64af84bdc970c4fae0c1e06dd95b73", "HDRP/Decal", new[]
		{
			"_BaseColor", "_BaseColorMap", "_NormalMap", "_MaskMap", "_DecalBlend", "_NormalBlendSrc", "_MaskBlendSrc",
			"_DrawOrder", "_EmissiveColor", "_EmissiveColorMap", "_UseEmissiveIntensity", "_EmissiveIntensity",
			"_MetallicRemapMin", "_MetallicRemapMax", "_SmoothnessRemapMin", "_SmoothnessRemapMax", "_Smoothness", "_Metallic",
			"_AO", "_AffectAlbedo", "_AffectNormal", "_AffectAO", "_AffectMetal", "_AffectSmoothness", "_AffectEmission"
		} ),
		new( "eb607f5e8fc16e140b732f33d26c8c31", "HDRP/TerrainLit", new[]
		{
			"_EnableHeightBlend", "_HeightTransition", "_SpecularOcclusionMode", "_CullMode", "_ZWrite", "_TerrainHolesTexture",
			"_EmissionColor", "_MainTex", "_Color", "_SupportDecals", "_ReceivesSSR"
		} ),
	};

	static readonly HdrpComponent[] ComponentTable =
	{
		new( "7a68c43fe1f2a47cfa234b5eeaa98012", "HDAdditionalLightData", HdrpComponentKind.Light ),
		new( "d0ef8dc2c2eabfa4e8cb77be57a837c0", "HDAdditionalReflectionData", HdrpComponentKind.ReflectionProbe ),
		new( "23c1ce4fb46143f46bc5cb5224c934f6", "HDAdditionalCameraData", HdrpComponentKind.Camera ),
		new( "172515602e62fb746b5d573b38a5fe58", "Volume", HdrpComponentKind.Volume ),
		new( "441482e8936e35048a1dffac814e3ef8", "StaticLightingSky", HdrpComponentKind.StaticLightingSky ),
		new( "a4ee7c3a3b205a14a94094d01ff91d6b", "PlanarReflectionProbe", HdrpComponentKind.PlanarReflectionProbe ),
		new( "f19d9143a39eb3b46bc4563e9889cfbd", "DecalProjector", HdrpComponentKind.DecalProjector ),
	};

	static readonly Dictionary<string, UnityPipelineShader> Shaders = Index( ShaderTable, s => s.Guid );
	static readonly Dictionary<string, HdrpComponent> Components = Index( ComponentTable, c => c.Guid );

	static Dictionary<string, T> Index<T>( T[] entries, Func<T, string> guid ) where T : class
	{
		var index = new Dictionary<string, T>( StringComparer.OrdinalIgnoreCase );
		foreach ( var entry in entries ) index[guid( entry )] = entry;
		return index;
	}

	/// <summary>The known pipeline shader <paramref name="guid"/> names, or null for any other shader.</summary>
	public static UnityPipelineShader Shader( string guid ) =>
		!string.IsNullOrEmpty( guid ) && Shaders.TryGetValue( guid, out var shader ) ? shader : null;

	/// <summary>The display name of a known pipeline shader; null when the shader is not in the table.</summary>
	public static string ShaderName( string guid ) => Shader( guid )?.Name;

	/// <summary>The pipeline a material's shader belongs to. Only the shader guid is consulted; the
	/// URP table is checked first, so a URP material never falls through the Standard path.</summary>
	public static UnityPipeline PipelineOf( string shaderGuid ) =>
		UnityUrp.Shader( shaderGuid ) != null ? UnityPipeline.Urp : Shader( shaderGuid ) == null ? UnityPipeline.Unknown : UnityPipeline.Hdrp;

	/// <summary>The known HDRP component <paramref name="scriptGuid"/> names, or null for any other script.</summary>
	public static HdrpComponent Component( string scriptGuid ) =>
		!string.IsNullOrEmpty( scriptGuid ) && Components.TryGetValue( scriptGuid, out var component ) ? component : null;

	/// <summary>The display name of a known HDRP component; null when the script is not in the table.</summary>
	public static string ComponentName( string scriptGuid ) => Component( scriptGuid )?.Name;

	/// <summary>
	/// The HDRP components a scene's script guids name, counted for the one-line summary. A light is
	/// counted once per HDAdditionalLightData (HDRP keeps exactly one per light), a reflection probe per
	/// HDAdditionalReflectionData (a planar probe is its own component and is named in the census, not
	/// counted here), and a volume per Volume.
	/// </summary>
	public static HdrpCounts Count( IEnumerable<string> scriptGuids, int materials = 0 )
	{
		int lights = 0, probes = 0, volumes = 0;
		foreach ( var guid in scriptGuids )
		{
			switch ( Component( guid )?.Kind )
			{
				case HdrpComponentKind.Light: lights++; break;
				case HdrpComponentKind.ReflectionProbe: probes++; break;
				case HdrpComponentKind.Volume: volumes++; break;
			}
		}
		return new( materials, lights, probes, volumes );
	}

	/// <summary>
	/// One line for the HDRP Lit features s&box's complex shader has no model for. Transmission and
	/// subsurface scattering change what a surface is, so they are neither approximated nor silently
	/// dropped: the materials are named so a reviewer can decide per surface. Long lists are sampled,
	/// because a project has more of these than the report can carry.
	/// </summary>
	public static string UnsupportedMaterials( IEnumerable<string> transmission, IEnumerable<string> subsurface )
	{
		static string Group( string feature, IEnumerable<string> materials )
		{
			// Key by path so same-named materials in different folders are counted; show the short name.
			var paths = materials.Distinct( StringComparer.OrdinalIgnoreCase ).OrderBy( n => n, StringComparer.Ordinal ).ToArray();
			var names = paths.Select( Path.GetFileNameWithoutExtension ).Distinct( StringComparer.OrdinalIgnoreCase ).ToArray();
			return $"{paths.Length} use {feature} ({string.Join( ", ", names.Take( 5 ) )}{(names.Length > 5 ? ", …" : "")})";
		}
		var groups = new List<string>();
		if ( transmission.Any() ) groups.Add( Group( "transmission", transmission ) );
		if ( subsurface.Any() ) groups.Add( Group( "subsurface scattering", subsurface ) );
		if ( groups.Count == 0 ) return null;
		return $"HDRP surfaces with no s&box equivalent were converted without that feature, so they do not render as authored: {string.Join( "; ", groups )}.";
	}
}