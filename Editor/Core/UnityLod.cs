using System;
using System.Globalization;
using System.Text.RegularExpressions;

namespace ImportUnityPackage;

/// <summary>
/// Reads Unity's numeric LOD suffixes and writes s&amp;box ModelDoc LOD switch distances.
/// The source corpus has no Unity LODGroup thresholds: every distance here is a NEW mapping,
/// not a value recovered from Unity. Both knobs are exposed so a developer can tune them.
/// </summary>
public static class UnityLod
{
	static readonly Regex LevelPattern = new( @"(?i)(?:^|[_ .-])LOD(?<level>\d+)(?:$|[_ .-])", RegexOptions.Compiled );
	static readonly Regex BasePattern = new( @"(?i)_lod\d+$", RegexOptions.Compiled );

	/// <summary>NEW MAPPING: switch distance for LOD level 0 (the engine writes 0.0 for level 0).</summary>
	public static double FirstSwitchDistance { get; set; } = 0.0;

	/// <summary>
	/// NEW MAPPING: metres added per LOD level. 50 is the engine's own default in
	/// CModelDoc::AddRenderMeshFile (engine/Definitions/modeldoc/ModelDocApp.def, SetSwitchThreshold( nLod * 50.0f )).
	/// </summary>
	public static double SwitchDistanceStep { get; set; } = 50.0;

	/// <summary>The numeric LOD level in a mesh name, or null when the name has no LOD suffix.</summary>
	public static int? Level( string mesh )
	{
		var match = LevelPattern.Match( mesh ?? "" );
		return match.Success && int.TryParse( match.Groups["level"].Value, NumberStyles.None, CultureInfo.InvariantCulture, out var level )
			? level : null;
	}

	/// <summary>The mesh stem before a numeric LOD suffix, shared by every level of one logical object.</summary>
	public static string Base( string mesh ) => BasePattern.Replace( mesh ?? "", "" );

	/// <summary>The switch_threshold serialized into the LODGroup for a level.</summary>
	public static double SwitchDistance( int level ) => FirstSwitchDistance + level * SwitchDistanceStep;
}
