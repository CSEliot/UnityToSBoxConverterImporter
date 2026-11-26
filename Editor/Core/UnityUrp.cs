using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace ImportUnityPackage;

/// <summary>URP MonoBehaviours the importer can recognise by their script guid in a scene.</summary>
public enum UrpComponentKind
{
	Camera,
	Light,
	GlobalVolume,
}

/// <summary>A URP component recognised by the guid of the script an archive's MonoBehaviour names.</summary>
public sealed record UrpComponent( string Guid, string Name, UrpComponentKind Kind );

/// <summary>
/// What one import saw of URP. Materials are deduplicated by asset path; there is no per-material
/// line, because a URP project carries hundreds of materials.
/// </summary>
public sealed record UrpCounts( int Materials, int Lit, int ShaderGraphs, int Components )
{
	public const string LitShaderName = "URP/Lit";

	public string Report => $"URP detected: {Materials} URP material(s), {Lit} URP Lit, {ShaderGraphs} Shader Graph material(s), {Components} URP component(s).";
}

/// <summary>
/// The render-pipeline facts the importer cannot read from a .unitypackage: URP 17.x shader guids
/// (their source lives in Packages/ and is never exported) and the script guids of its scene
/// components. Guids are stable across projects; they are verified against the fixture's package
/// cache, not against a particular editor build. Shader Graph materials are recognised by their
/// graph asset's guid and are REPORTED ONLY - graph code is never translated.
/// </summary>
public static class UnityUrp
{
	/// <summary>
	/// UnityEngine.Rendering.Volume (shared Core type, also used by HDRP). A URP Volume is this guid
	/// PLUS m_IsGlobal: 1 PLUS scene-level URP evidence: the guid alone does not discriminate.
	/// Re-resolved against SyntyForest's URP 17.7.0 package cache:
	/// com.unity.render-pipelines.core@7aa87ac5532f/Runtime/Volume/Volume.cs.meta:2.
	/// </summary>
	public const string VolumeScriptGuid = "172515602e62fb746b5d573b38a5fe58";

	/// <summary>URP's project-wide default stack, referenced by UniversalRenderPipelineGlobalSettings.</summary>
	public const string DefaultVolumeProfileGuid = "ab09877e2e707104187f6f83e2f62510";
	// Properties and keywords URP 17's Lit/Unlit shaders declare. A package has no ShaderLab source,
	// so the guid and this serialized evidence are the only available discriminator; tags are not.
	static readonly string[] LitEvidence =
	{
		"_BaseMap", "_BumpMap", "_EmissionMap", "_Normal_Map", "_Metallic", "_Smoothness", "_Glossiness",
		"_Surface", "_AlphaClip", "_Cutoff", "_BumpScale", "_Cull", "_ZWrite", "_SrcBlend", "_DstBlend",
		"_BaseColor", "_Color", "_EmissionColor", "_SpecColor",
		"_ALPHATEST_ON", "_SURFACE_TYPE_TRANSPARENT", "_NORMALMAP", "_EMISSION"
	};

	static readonly string[] ParticlesUnlitEvidence =
	{
		"_BaseMap", "_Color", "_Surface", "_Cutoff", "_Cull", "_ZWrite", "_SrcBlend", "_DstBlend",
		"_ALPHATEST_ON", "_SURFACE_TYPE_TRANSPARENT"
	};

	static readonly UnityPipelineShader[] ShaderTable =
	{
		new( "933532a4fcc9baf4fa0491de14d08ed7", UrpCounts.LitShaderName, LitEvidence ),
		new( "0406db5a14f94604a8c57ccfbc9f3b46", "URP/Particles/Unlit", ParticlesUnlitEvidence ),
	};

	// Developer-tweakable default for a URP/Lit alpha-clipped material that has no serialized _Cutoff.
	public static double AlphaTestReferenceDefault { get; set; } = 0.5;

	static readonly UrpComponent[] ComponentTable =
	{
		new( "a79441f348de89743a2939f4d699eac1", "UniversalAdditionalCameraData", UrpComponentKind.Camera ),
		new( "474bcb49853aa07438625e644c072ee6", "UniversalAdditionalLightData", UrpComponentKind.Light ),
		// The C# type of this guid is UNVERIFIED; the component is identified by its "Global Volume"
		// GameObject and m_IsGlobal: 1, never by a guessed class name.
		new( "172515602e62fb746b5d573b38a5fe58", "Global Volume", UrpComponentKind.GlobalVolume ),
	};

	static readonly Dictionary<string, UnityPipelineShader> Shaders = Index( ShaderTable, s => s.Guid );
	static readonly Dictionary<string, UrpComponent> Components = Index( ComponentTable, c => c.Guid );

	static Dictionary<string, T> Index<T>( T[] entries, Func<T, string> guid ) where T : class
	{
		var index = new Dictionary<string, T>( StringComparer.OrdinalIgnoreCase );
		foreach ( var entry in entries ) index[guid( entry )] = entry;
		return index;
	}

	/// <summary>The known URP shader <paramref name="guid"/> names, or null for any other shader.</summary>
	public static UnityPipelineShader Shader( string guid ) =>
		!string.IsNullOrEmpty( guid ) && Shaders.TryGetValue( guid, out var shader ) ? shader : null;

	/// <summary>
	/// A shader guid alone can be stale in a material; require at least one serialized URP property
	/// or feature keyword as well. This never consults ShaderLab tags, which a package cannot carry.
	/// </summary>
	public static bool HasShaderEvidence( UnityMaterial material )
	{
		if ( Shader( material.ShaderGuid ) is not { } shader ) return false;
		foreach ( var property in shader.Properties )
			if ( material.Numbers.ContainsKey( property ) || material.Textures.ContainsKey( property ) ||
				 material.Colors.ContainsKey( property ) || material.HasKeyword( property ) ) return true;
		return false;
	}

	/// <summary>The known URP component <paramref name="scriptGuid"/>, or null for any other script.</summary>
	public static UrpComponent Component( string scriptGuid ) =>
		!string.IsNullOrEmpty( scriptGuid ) && Components.TryGetValue( scriptGuid, out var component ) ? component : null;

	static readonly Regex BlockHeader = new( @"^--- !u!(\d+) &(-?\d+)" );
	static readonly Regex ScriptGuid = new( @"m_Script:\s*\{[^}]*guid:\s*([0-9a-fA-F]{32})" );
	static readonly Regex GameObjectRef = new( @"m_GameObject:\s*\{[^}]*fileID:\s*(-?\d+)" );
	static readonly Regex ObjectName = new( @"(?m)^\s*m_Name:\s*(?<name>.*?)\s*$" );
	static readonly Regex IsGlobal = new( @"(?m)^\s*m_IsGlobal:\s*1\s*$" );

	/// <summary>
	/// Count URP components from serialized scenes, line by line so a 44 MiB scene does not need a
	/// single decoded string. A Global Volume needs both the named GameObject and m_IsGlobal: 1.
	/// </summary>
	public static UrpCounts CountComponents( IEnumerable<string> scenePaths )
	{
		int cameras = 0, lights = 0, volumes = 0;
		foreach ( var path in scenePaths )
		{
			if ( !File.Exists( path ) ) continue;
			var gameObjects = new Dictionary<string, string>( StringComparer.Ordinal );
			var candidates = new List<(UrpComponentKind Kind, string GameObjectId)>();
			string type = null, id = null;
			var body = new StringBuilder();
			void Flush()
			{
				if ( type == null ) return;
				var text = body.ToString();
				if ( type == "1" )
					gameObjects[id] = ObjectName.Match( text ).Groups["name"].Value;
				else if ( type == "114" && Component( ScriptGuid.Match( text ).Groups[1].Value ) is { } component )
				{
					var owner = GameObjectRef.Match( text ).Groups[1].Value;
					if ( component.Kind != UrpComponentKind.GlobalVolume || IsGlobal.IsMatch( text ) )
						candidates.Add( (component.Kind, owner) );
				}
				type = null;
				body.Clear();
			}
			foreach ( var line in File.ReadLines( path ) )
			{
				var header = BlockHeader.Match( line );
				if ( !header.Success ) { if ( type != null ) body.AppendLine( line ); continue; }
				Flush();
				type = header.Groups[1].Value;
				id = header.Groups[2].Value;
			}
			Flush();
			foreach ( var (kind, owner) in candidates )
			{
				switch ( kind )
				{
					case UrpComponentKind.Camera: cameras++; break;
					case UrpComponentKind.Light: lights++; break;
					case UrpComponentKind.GlobalVolume:
						if ( gameObjects.TryGetValue( owner, out var name ) && name == "Global Volume" ) volumes++;
						break;
				}
			}
		}
		return new( 0, 0, 0, cameras + lights + volumes );
	}
}
