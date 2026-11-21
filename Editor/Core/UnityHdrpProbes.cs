using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using static ImportUnityPackage.UnityPrefabExport;

namespace ImportUnityPackage;

/// <summary>
/// The HDRP scene components s&amp;box has no counterpart for, and the report lines that say so. A Unity
/// ReflectionProbe does convert (to Sandbox.EnvmapProbe, built by <see cref="UnitySceneExport.EnvmapProbe"/>);
/// planar probes, decal projectors and light-layer masks do not, and are named once per scene with their
/// counts rather than turned into a component that would render something the source does not.
/// </summary>
internal static class UnityHdrpProbes
{
	/// <summary>The HDRP component kind of a MonoBehaviour block, or null for any other component.</summary>
	static HdrpComponentKind? Kind( Block block ) =>
		block.Type == "114" ? UnityHdrp.Component( GuidOf( Field( block.Body, "m_Script" ) ) )?.Kind : null;

	/// <summary>The GameObject name of a component body, or an empty string when its owner is not in the file.</summary>
	static string Owner( IReadOnlyDictionary<long, Block> byId, string body ) =>
		byId.GetValueOrDefault( IdOf( Field( body, "m_GameObject" ) ) ) is { Type: "1" } go ? Scalar( go.Body, "m_Name" ) : "";

	/// <summary>A sample of names for a grouped line: up to five, distinct, in file order.</summary>
	static string Names( IEnumerable<string> names )
	{
		var distinct = names.Where( n => n.Length > 0 ).Distinct().ToArray();
		return distinct.Length == 0 ? "" : $" ({string.Join( ", ", distinct.Take( 5 ) )}{(distinct.Length > 5 ? ", …" : "")})";
	}

	/// <summary>
	/// Reports the HDRP components this task does not convert: planar reflection probes, decal projectors and
	/// light-layer masks. A scene with no HDRP component at all writes only the converted-probe line, so a
	/// non-HDRP project's report does not grow a line saying it has no decals.
	/// </summary>
	internal static void Report( IReadOnlyList<Block> blocks, IReadOnlyDictionary<string, UnityAsset> byGuid, List<string> information )
	{
		var byId = blocks.GroupBy( b => b.Id ).ToDictionary( g => g.Key, g => g.First() );

		var probes = blocks.Where( b => !b.Stripped && b.Type == "215" ).ToArray();
		if ( probes.Length > 0 )
			information.Add( $"{probes.Length} reflection probe(s) converted to Sandbox.EnvmapProbe with Unity's box bounds, resolution, importance, blend distance and clip planes{Names( probes.Select( p => Owner( byId, p.Body ) ) )}; Unity's baked cubemap data cannot be imported, so they capture in real time." );

		var hdrp = blocks.Any( b => !b.Stripped && Kind( b ) != null );
		if ( !hdrp ) return;

		var planar = blocks.Where( b => !b.Stripped && Kind( b ) == HdrpComponentKind.PlanarReflectionProbe ).ToArray();
		if ( planar.Length > 0 )
			information.Add( $"{planar.Length} PlanarReflectionProbe component(s) were not converted{Names( planar.Select( p => Owner( byId, p.Body ) ) )}: s&box exposes no planar reflection component, and an envmap probe or a water plane would reflect something the source does not." );

		var decals = blocks.Where( b => !b.Stripped && Kind( b ) == HdrpComponentKind.DecalProjector ).ToArray();
		if ( decals.Length == 0 )
			information.Add( "0 DecalProjector component(s) in the scene; decal projectors are reported, not converted, in this step." );
		else
			information.Add( $"{decals.Length} DecalProjector component(s) were not converted{Names( decals.Select( d => Decal( byId, d ) ) )}: s&box decals need a decal material and projection box of their own, which this step does not build." );

		// HDRP masks which light layers a light renders with (m_LightlayersMask) and which ones a probe captures
		// (m_LightLayerMask). s&box has no per-light mask, so the value is reported and never interpreted.
		var lights = blocks.Where( b => !b.Stripped && Kind( b ) == HdrpComponentKind.Light ).ToArray();
		var hdProbes = blocks.Where( b => !b.Stripped && Kind( b ) == HdrpComponentKind.ReflectionProbe ).ToArray();
		var probeMasks = hdProbes.SelectMany( b => Regex.Matches( b.Body, @"(?m)^\s*m_LightLayerMask:\s*([0-9]+)" ).Select( m => m.Groups[1].Value ) ).ToArray();
		var restrictedLights = lights.Count( b => Scalar( b.Body, "m_LightlayersMask" ) is { Length: > 0 } mask && mask != "1" );
		var restricted = restrictedLights + probeMasks.Count( m => m != "1" );
		if ( lights.Length + hdProbes.Length > 0 )
			information.Add( restricted > 0
				? $"{restricted} HDRP light layer mask(s) restrict which layers a light renders or a probe captures; s&box lights have no per-light layer mask, so that restriction was not converted."
				: $"All {lights.Length} HD light(s) and {hdProbes.Length} probe(s) use HDRP's default light layer; s&box lights have no per-light layer mask, so none was lost." );
	}

	/// <summary>A decal projector named for the report: its object, its material and its projection size, all of
	/// which a reader needs to rebuild it by hand because this step does not.</summary>
	static string Decal( IReadOnlyDictionary<long, Block> byId, Block block )
	{
		var owner = Owner( byId, block.Body );
		var material = GuidOf( Field( block.Body, "m_Material" ) );
		var size = Vector( block.Body, "m_Size", "x", "y", "z" );
		return $"{(owner.Length > 0 ? owner : "DecalProjector")} [material {material}, size {Join( size )}]";
	}
}