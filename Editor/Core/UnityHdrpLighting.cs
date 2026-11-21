using System;
using System.Collections.Generic;
using static ImportUnityPackage.UnityPrefabExport;

namespace ImportUnityPackage;

/// <summary>
/// HDRP keeps a light's physical intensity, unit and dimmer in HDAdditionalLightData, not in the Unity
/// Light component: every light in the source project is authored in lumen or lux, so reading Unity's
/// m_Intensity as a multiplier lands a 150-lumen downlight and a 20,000-lux sun two orders of magnitude
/// apart in the wrong direction. s&box has no photometric unit at all - a light's brightness is the
/// magnitude of its colour, a plain multiplier - so one documented scale maps candela onto that
/// multiplier and the whole set keeps its relative brightness.
///
/// The table (one scale, no per-light special cases):
///   Lumen    → candela = lm / 4π          (an isotropic emitter's mean intensity over the full sphere)
///   Lux      → candela = lx × d²          (punctual: d = m_LuxAtDistance; directional keeps the lux value)
///   Candela  → itself
///   EV100    → not converted: reported, never guessed
/// then s&box intensity = candela × m_LightDimmer / <see cref="CandelaPerUnit"/>, clamped at
/// <see cref="MaxIntensity"/> so a single outlier (the 20,000-lux sun) cannot write a 19,573-magnitude
/// LightColor. Every other light stays under the clamp, so their ordering matches the source exactly.
/// </summary>
internal static class UnityHdrpLighting
{
	/// <summary>HDAdditionalLightData.m_LightUnit, in HDRP's declaration order (LightUnit: Lumen, Candela, Lux, Ev100).</summary>
	internal enum Unit { Lumen, Candela, Lux, Ev }

	/// <summary>1 s&box intensity unit = 1000 candela. Half the corpus' interior lights land at 0.01-2
	/// and the sun is the only value above the clamp; relative brightness is what survives, not absolute.</summary>
	internal const int CandelaPerUnit = 1000;

	/// <summary>The brightest light multiplier the importer writes; s&box lights above it bloom to white.</summary>
	internal const int MaxIntensity = 8;

	/// <summary>What one HDRP light becomes: its s&box multiplier and how it was read.</summary>
	internal readonly struct Converted
	{
		/// <summary>True when an HDAdditionalLightData supplied the value; false keeps Unity's m_Intensity.</summary>
		public bool Hdrp { get; init; }
		public Unit Unit { get; init; }
		/// <summary>A rectangle, disc or tube light: s&box has no shape for it, so only the report carries it.</summary>
		public bool Area { get; init; }
		/// <summary>EV100 is reported, not converted; the Unity intensity is kept so the light is not lost.</summary>
		public bool ReportOnly { get; init; }
		/// <summary>The converted value before the clamp, so the report can name how far the sun went over.</summary>
		public double Unclamped { get; init; }
		/// <summary>The multiplier the light colour is scaled by, dimmer applied and clamped.</summary>
		public double Intensity { get; init; }
		public bool Clamped { get; init; }
	}

	/// <summary>
	/// Reads one light's HDAdditionalLightData. <paramref name="type"/> is the Unity Light's m_Type: only a
	/// directional light (1) reads lux as illuminance; a punctual one converts lux at m_LuxAtDistance.
	/// A light without an HD component reads as no conversion, which keeps every non-HDRP package byte-identical.
	/// </summary>
	internal static Converted Read( string hd, int type )
	{
		if ( hd == null ) return default;
		var unit = (Unit)(int)Number( hd, "m_LightUnit" );
		if ( type == 3 ) return new Converted { Hdrp = true, Unit = unit, Area = true };
		var value = Scalar( hd, "m_Intensity" ).Length == 0 ? 1 : Number( hd, "m_Intensity" );
		var dimmer = Scalar( hd, "m_LightDimmer" ).Length == 0 ? 1 : Number( hd, "m_LightDimmer" );
		double candela;
		switch ( unit )
		{
			case Unit.Lumen: candela = value / (4 * Math.PI); break;
			case Unit.Candela: candela = value; break;
			case Unit.Lux:
				// HDRP's lux unit is illuminance at m_LuxAtDistance for a punctual light (E = I / d²).
				var distance = Scalar( hd, "m_LuxAtDistance" ).Length == 0 ? 1 : Number( hd, "m_LuxAtDistance" );
				candela = type == 1 ? value : value * distance * distance;
				break;
			default: return new Converted { Hdrp = true, Unit = unit, ReportOnly = true, Intensity = 1 };
		}
		var intensity = candela * dimmer / CandelaPerUnit;
		var clamped = intensity > MaxIntensity;
		return new Converted { Hdrp = true, Unit = unit, Unclamped = intensity, Intensity = clamped ? MaxIntensity : intensity, Clamped = clamped };
	}

	/// <summary>The spot cone: HDRP states the inner cone as a percentage of the outer one, -1 when unset.</summary>
	internal static double InnerSpotAngle( string hd, double outer )
	{
		if ( hd == null ) return double.NaN;
		var percent = Scalar( hd, "m_InnerSpotPercent" ).Length == 0 ? -1 : Number( hd, "m_InnerSpotPercent" );
		return percent < 0 ? double.NaN : outer * Math.Clamp( percent, 0, 100 ) / 100;
	}

	/// <summary>
	/// The grouped lines a converted file owes for its HDRP lights and cameras: one for the area lights,
	/// one for the unit scale and one for the camera data, instead of a line per light that buries the report.
	/// </summary>
	internal sealed class Report
	{
		readonly List<string> areaNames = new();
		int areaLights, lumen, lux, candela, ev, clamped, cameras, solid, antiAliased;

		public void Add( string name, Converted converted )
		{
			if ( !converted.Hdrp ) return;
			// An area light is counted by unit as well: the unit line has to cover all 90 lumen and 29 lux
			// lights, not only the ones s&box can draw.
			switch ( converted.Unit )
			{
				case Unit.Lumen: lumen++; break;
				case Unit.Lux: lux++; break;
				case Unit.Candela: candela++; break;
				default: ev++; break;
			}
			if ( converted.Clamped ) clamped++;
			if ( !converted.Area ) return;
			areaLights++;
			if ( areaNames.Count < 5 && !areaNames.Contains( name ) ) areaNames.Add( name );
		}

		/// <summary>Counts one camera that carried HDAdditionalCameraData; <paramref name="hd"/> may be null.</summary>
		public void AddCamera( string hd )
		{
			if ( hd == null ) return;
			cameras++;
			if ( (int)Number( hd, "clearColorMode" ) == 1 ) solid++;
			if ( Number( hd, "antialiasing" ) != 0 ) antiAliased++;
		}

		public void Flush( List<string> information )
		{
			if ( areaLights > 0 )
				information.Add( $"{areaLights} area light(s) not converted; s&box has no rectangle, disc or tube light: {string.Join( ", ", areaNames )}{(areaLights > areaNames.Count ? ", …" : "")}." );
			if ( lumen + lux + candela + ev > 0 )
			{
				var line = $"HDRP light units converted at 1 s&box intensity unit = {CandelaPerUnit} cd: {lumen} lumen (value / 4 pi), {lux} lux (x m_LuxAtDistance^2, directional lux as-is), {candela} candela; {clamped} clamped at {MaxIntensity}.";
				if ( areaLights > 0 ) line += $" The {areaLights} area lights are counted in these totals but are not converted.";
				if ( ev > 0 ) line += $" {ev} EV100 light(s) reported only; their Unity intensity is kept unconverted.";
				information.Add( line + " Per-light shadow resolution, shadow dimmer and volumetric settings have no s&box counterpart." );
			}
			if ( cameras > 0 )
				information.Add( $"{cameras} HDRP camera(s) read HDAdditionalCameraData: {solid} clear to a solid colour instead of the sky, {antiAliased} request antialiasing. The volume mask, physical camera settings and per-camera frame settings are not converted." );
		}
	}
}