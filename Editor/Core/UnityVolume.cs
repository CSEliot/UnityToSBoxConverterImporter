using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using static ImportUnityPackage.UnityPrefabExport;

namespace ImportUnityPackage;

/// <summary>
/// HDRP keeps a scene's sky, ambient and fog in Volume overrides, not in RenderSettings. The overrides
/// live in VolumeProfile .asset files that Unity exports as text YAML next to the scene, so the importer
/// reads them directly: this class parses a profile, resolves which global Volume a scene applies, and
/// decides what the environment becomes. Rendering a procedural sky is not possible here; the default
/// sky carries the approximation and the report says so.
/// </summary>
internal static class UnityVolume
{
	/// <summary>HDRP's VisualEnvironment.skyType and fogType values, verified against HDRP 17's source.</summary>
	const int SkyNone = 0, SkyHdri = 1, SkyProcedural = 2, SkyGradient = 3, SkyPhysicallyBased = 4;
	const int FogNone = 0, FogExponential = 2, FogVolumetric = 3;

	/// <summary>
	/// Override component names by script guid. A migrated profile (VolumeSettings.asset) serialises an
	/// empty m_Name and m_EditorClassIdentifier on every override, so the guid is the only reliable name;
	/// the name and class-identifier columns are used when a hand-authored profile carries them.
	/// </summary>
	static readonly Dictionary<string, string> OverrideNames = new( StringComparer.OrdinalIgnoreCase )
	{
		["0d7593b3a9277ac4696b20006c21dde2"] = "VisualEnvironment",
		["953beb541740ddc499d005ee80c9ff29"] = "Fog",
		["2150ee00179b2f4418ea8b21a7e98eea"] = "ExponentialFog",
		["d877ec3e844f2ca46830012e8e79319b"] = "PhysicallyBasedSky",
		["59b6606ef2548734bb6d11b9d160bc7e"] = "HDRISky",
		["a81bcacc415a1f743bfdf703afc52027"] = "GradientSky",
		["2d08ce26990eb1a4a9177b860541e702"] = "Exposure",
	};

	/// <summary>The overrides this step reads. Anything else in the profile is named once in the report.</summary>
	static readonly string[] Consumed = { "VisualEnvironment", "PhysicallyBasedSky", "HDRISky", "GradientSky", "Fog", "ExponentialFog", "Exposure" };

	/// <summary>One override component of a profile: its name and its serialised body.</summary>
	internal sealed record Override( string Type, string Body );

	/// <summary>A VolumeProfile asset: the overrides it lists and the name for the report.</summary>
	internal sealed class Profile
	{
		public string Name;
		public List<Override> Overrides { get; } = new();
		public Override Get( string type ) => Overrides.FirstOrDefault( o => o.Type.Equals( type, StringComparison.OrdinalIgnoreCase ) );
	}

	/// <summary>What HDRP supplies as a sky: the kind, the tint s&amp;box's default sky takes, and the report line.</summary>
	internal sealed record Sky( string Kind, double[] Tint, string Texture, string Note );

	/// <summary>The fog a Volume override asks for, in Unity units; the scene export converts to engine units.</summary>
	internal sealed record Fog( double[] Colour, bool SkyColour, double Distance, double BaseHeight, double HeightAttenuation, double MaximumHeight, bool Legacy, string Note );

	/// <summary>HDRP's Exposure override, in EV100. s&amp;box's CameraComponent.AutoExposure is a multiplier
	/// tonemap (its Min/MaxExposure are 1..3, not stops), so the range is reported and never guessed into it.</summary>
	internal sealed record Exposure( bool Automatic, double FixedEv, double Compensation, double LimitMin, double LimitMax, string Note );

	/// <summary>Everything the environment step found: null members mean the source supplies nothing of that kind.</summary>
	internal sealed class Motion
	{
		public Sky Sky;
		public double[] Ambient;
		public Fog Fog;
		public Exposure Exposure;
		public List<string> Information { get; } = new();
	}

	/// <summary>Reads a profile's overrides. A profile with no override list reads as empty, never as an error.</summary>
	internal static Profile Parse( string text, string name )
	{
		var profile = new Profile { Name = name };
		var blocks = Blocks( text );
		if ( blocks.FirstOrDefault( b => b.Type == "114" && Regex.IsMatch( b.Body, @"(?m)^\s*components:" ) ) is not { } root ) return profile;
		var byId = blocks.GroupBy( b => b.Id ).ToDictionary( g => g.Key, g => g.First() );
		foreach ( Match reference in Regex.Matches( Section( root.Body, "components" ), @"fileID:\s*(-?[0-9]+)" ) )
		{
			if ( !long.TryParse( reference.Groups[1].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var id ) ) continue;
			if ( !byId.TryGetValue( id, out var block ) ) continue;
			profile.Overrides.Add( new( OverrideName( block ), block.Body ) );
		}
		return profile;
	}

	/// <summary>The component name of one override block: guid table, then class identifier, then m_Name.</summary>
	static string OverrideName( Block block )
	{
		var guid = GuidOf( Field( block.Body, "m_Script" ) );
		if ( OverrideNames.TryGetValue( guid, out var known ) ) return known;
		var identifier = Scalar( block.Body, "m_EditorClassIdentifier" );
		var shortName = identifier.Length == 0 ? "" : identifier[(identifier.LastIndexOf( '.' ) + 1)..];
		if ( shortName.Length > 0 ) return shortName;
		var name = Scalar( block.Body, "m_Name" );
		return name.Length > 0 ? name : $"override {guid}";
	}

	/// <summary>
	/// A parameter's m_Value, or null when the profile leaves it unoverridden. HDRP wraps every field as
	/// "field:\n  m_OverrideState: 1\n  m_Value: X"; a plain scalar such as active is returned as its text.
	/// </summary>
	internal static string Value( Override source, string field )
	{
		if ( source == null ) return null;
		var block = Regex.Match( source.Body, @"(?m)^  " + Regex.Escape( field ) + @":[ \t]*\r?\n((?:    [^\r\n]*\r?\n?)+)" );
		if ( !block.Success )
		{
			var scalar = Regex.Match( source.Body, @"(?m)^  " + Regex.Escape( field ) + @":[ \t]*([^\r\n]*?)[ \t]*\r?$" );
			return scalar.Success && scalar.Groups[1].Value.Length > 0 ? scalar.Groups[1].Value : null;
		}
		var body = block.Groups[1].Value;
		if ( Regex.Match( body, @"m_OverrideState:\s*([01])" ) is { Success: true } state && state.Groups[1].Value == "0" ) return null;
		var value = Regex.Match( body, @"m_Value:[ \t]*([^\r\n]*)" );
		return value.Success && value.Groups[1].Value.Trim().Length > 0 ? value.Groups[1].Value.Trim() : null;
	}

	static double Number( Override source, string field, double fallback ) =>
		double.TryParse( Value( source, field ), NumberStyles.Float, CultureInfo.InvariantCulture, out var value ) ? value : fallback;

	/// <summary>A colour parameter's r,g,b; Unity serialises it as "{r: 0.5, g: 0.5, b: 0.5, a: 1}".</summary>
	static double[] Colour( Override source, string field, params double[] fallback )
	{
		var value = Value( source, field );
		if ( string.IsNullOrEmpty( value ) ) return fallback ?? new double[] { 1, 1, 1 };
		var body = value.Trim( '{', '}', ' ' );
		return new[] { "r", "g", "b" }.Select( axis =>
		{
			var match = Regex.Match( body, @"\b" + axis + @":\s*([-+0-9.eE]+)" );
			return match.Success && double.TryParse( match.Groups[1].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var channel ) ? channel : 1;
		} ).ToArray();
	}

	static double[] Average( double[] a, double[] b ) => a.Zip( b, ( x, y ) => (x + y) / 2 ).ToArray();

	/// <summary>The guid of a Volume's profile: HDRP 17 writes sharedProfile, older scenes m_Profile.</summary>
	static string ProfileGuid( string body )
	{
		var match = Regex.Match( body, @"(?m)^\s*(?:sharedProfile|m_Profile):[ \t]*(?<first>[^\r\n]*)(?:\r?\n[ \t]+(?<rest>[^\r\n]*))?" );
		return match.Success ? GuidOf( match.Groups["first"].Value + " " + match.Groups["rest"].Value ) : "";
	}

	/// <summary>
	/// The environment a scene's Volumes supply. HDRP blends every global Volume that shares the highest
	/// priority by weight; the importer merges the overrides of those Volumes in scene order instead of
	/// blending values, applies only the highest priority, and reports local Volumes and odd weights
	/// rather than pretending to interpolate them.
	/// </summary>
	internal static Motion Read( IReadOnlyList<Block> blocks, IReadOnlyDictionary<string, UnityAsset> byGuid )
	{
		var motion = new Motion();
		var byId = blocks.GroupBy( b => b.Id ).ToDictionary( g => g.Key, g => g.First() );
		string Owner( string body )
		{
			var objectId = IdOf( Field( body, "m_GameObject" ) );
			return byId.TryGetValue( objectId, out var go ) ? Scalar( go.Body, "m_Name" ) : "";
		}

		var applied = new List<(int Priority, double Weight, string Name, Profile Profile)>();
		var locals = new List<string>();
		var unresolved = new List<string>();
		foreach ( var block in blocks )
		{
			if ( block.Stripped || block.Type != "114" ) continue;
			if ( UnityHdrp.Component( GuidOf( Field( block.Body, "m_Script" ) ) )?.Kind != HdrpComponentKind.Volume ) continue;
			var name = Owner( block.Body );
			if ( name.Length == 0 ) name = $"Volume {block.Id}";
			if ( Scalar( block.Body, "m_IsGlobal" ) != "1" ) { locals.Add( name ); continue; }
			var guid = ProfileGuid( block.Body );
			if ( guid.Length == 0 || !byGuid.TryGetValue( guid, out var asset ) )
			{
				unresolved.Add( guid.Length == 0 ? $"{name} has no profile" : $"{name} names profile {guid}, which is not in the package" );
				continue;
			}
			string text;
			try { text = UnityImport.ReadText( asset.Source ); }
			catch ( InvalidDataException ex ) { unresolved.Add( $"{name}: {asset.Path} could not be read ({ex.Message})" ); continue; }
			// priority and weight are plain scalars, not m_OverrideState parameters.
			double ScalarNumber( string field, double fallback ) =>
				double.TryParse( Scalar( block.Body, field ), NumberStyles.Float, CultureInfo.InvariantCulture, out var value ) ? value : fallback;
			applied.Add( ( (int)ScalarNumber( "priority", 0 ), ScalarNumber( "weight", 1 ), name, Parse( text, Path.GetFileNameWithoutExtension( asset.Path ) ) ) );
		}

		if ( unresolved.Count > 0 )
			motion.Information.Add( $"HDRP volume(s) that could not be read: {string.Join( "; ", unresolved.Take( 4 ) )}{(unresolved.Count > 4 ? ", …" : "")}." );

		var merged = new Dictionary<string, Override>( StringComparer.OrdinalIgnoreCase );
		if ( applied.Count > 0 )
		{
			var priority = applied.Max( a => a.Priority );
			var active = applied.Where( a => a.Priority == priority && a.Weight > 0 ).ToList();
			var tied = new List<string>();
			foreach ( var volume in active )
				foreach ( var @override in volume.Profile.Overrides )
				{
					// HDRP blends global Volumes in scene order; for a non-interpolated value such as
					// skyType the last write wins, so a later Volume's override replaces an earlier one.
					if ( merged.ContainsKey( @override.Type ) && Consumed.Contains( @override.Type, StringComparer.OrdinalIgnoreCase ) && !tied.Contains( @override.Type ) ) tied.Add( @override.Type );
					merged[@override.Type] = @override;
				}
			if ( active.Count > 0 )
			{
				var weights = active.Where( a => Math.Abs( a.Weight - 1 ) > 1e-4 ).Select( a => $"{a.Name} weight {a.Weight.ToString( "R", CultureInfo.InvariantCulture )}" ).ToArray();
				motion.Information.Add( $"Environment read from {active.Count} global HDRP volume(s) at priority {priority}: {string.Join( ", ", active.Select( a => $"{a.Name} ({a.Profile.Name})" ) )}." +
					(weights.Length > 0 ? $" {string.Join( ", ", weights )} not blended at partial weight." : "") );
			}
			if ( tied.Count > 0 )
				motion.Information.Add( $"Volumes at the same priority override the same HDRP setting(s); the last in scene order was used: {string.Join( ", ", tied )}." );
			var skipped = applied.Where( a => a.Priority != priority ).Select( a => a.Name ).ToArray();
			if ( skipped.Length > 0 )
				motion.Information.Add( $"{string.Join( ", ", skipped )} at a lower priority than {priority} was not applied." );
		}
		else if ( locals.Count > 0 )
			motion.Information.Add( "No global HDRP volume supplies the environment; only local volumes exist and their overrides are not converted." );
		if ( locals.Count > 0 )
			motion.Information.Add( $"{locals.Count} local HDRP volume(s) affect only part of the scene and were not blended: {string.Join( ", ", locals.Take( 5 ) )}{(locals.Count > 5 ? ", …" : "")}." );

		var staticSky = blocks.Count( b => !b.Stripped && b.Type == "114" && UnityHdrp.Component( GuidOf( Field( b.Body, "m_Script" ) ) )?.Kind == HdrpComponentKind.StaticLightingSky );
		if ( staticSky > 0 )
			motion.Information.Add( $"{staticSky} StaticLightingSky component(s) select a bake-time sky profile; they are reported, not converted." );

		merged.TryGetValue( "VisualEnvironment", out var visual );
		var skyType = (int)Number( visual, "skyType", SkyNone );
		var skyOverride = skyType switch
		{
			SkyPhysicallyBased => merged.GetValueOrDefault( "PhysicallyBasedSky" ),
			SkyGradient => merged.GetValueOrDefault( "GradientSky" ),
			SkyHdri => merged.GetValueOrDefault( "HDRISky" ),
			_ => null
		};
		switch ( skyType )
		{
			case SkyPhysicallyBased:
			{
				// The atmosphere itself cannot be reproduced; its horizon and zenith filters are the
				// only authored colours that survive as a tint on the default sky.
				var tint = Average( Colour( skyOverride, "horizonTint", 1, 1, 1 ), Colour( skyOverride, "zenithTint", 1, 1, 1 ) );
				motion.Sky = new( "physically based", tint, null,
					"HDRP's PhysicallyBased sky is approximated by the default s&box sky with its horizon/zenith tint; the atmosphere, sun, clouds and time of day are not reproduced." );
				break;
			}
			case SkyGradient:
			{
				// A three-band gradient has no s&box counterpart; the middle (horizon) band is the
				// colour most of the visible sky carries, so it becomes the tint and the ambient.
				var middle = Colour( skyOverride, "middle", 0.3, 0.7, 1 );
				motion.Sky = new( "gradient", middle, null,
					"HDRP's gradient sky is approximated by the default s&box sky tinted with its middle (horizon) band; the three-band gradient is not reproduced." );
				motion.Ambient = middle;
				break;
			}
			case SkyHdri:
			{
				var texture = GuidOf( Value( skyOverride, "hdriSky" ) ?? "" );
				var textureName = texture.Length > 0 && byGuid.TryGetValue( texture, out var asset ) ? asset.Path : null;
				motion.Sky = new( "HDRI", new double[] { 1, 1, 1 }, textureName,
					textureName == null
						? "HDRP's HDRI sky has no resolved sky texture; the default s&box sky is used and the HDRI is not reproduced."
						: $"HDRP's HDRI sky texture {textureName} is not converted (no verified s&box sky texture parameter); the default s&box sky is used instead." );
				break;
			}
			case SkyProcedural:
				motion.Sky = new( "procedural", new double[] { 1, 1, 1 }, null,
					"HDRP's deprecated procedural sky is not converted; the default s&box sky is used." );
				break;
		}
		if ( motion.Sky != null && motion.Ambient == null )
			motion.Information.Add( "Ambient light is left to the sky (HDRP derives it from the sky), so no Sandbox.AmbientLight was written." );

		var fogType = (int)Number( visual, "fogType", FogNone );
		var exponential = merged.GetValueOrDefault( "ExponentialFog" );
		var fog = merged.GetValueOrDefault( "Fog" );
		var fogEnabled = fog == null ? (bool?)null : Value( fog, "enabled" ) != "0";
		// fogType is deprecated in HDRP 17: the profile's Fog override renders whether the stale field
		// says Exponential or Volumetric, and it only selects the legacy ExponentialFog record.
		if ( fogEnabled == false )
			motion.Information.Add( "The HDRP Fog override is disabled; no Sandbox.Fog was written." );
		else if ( exponential != null || fog != null )
		{
			// fogType 2 is HDRP's selector for the legacy ExponentialFog override; a modern profile that
			// leaves the deprecated field alone carries the same data in the newer Fog override instead.
			var useLegacy = fogType == FogExponential && exponential != null;
			var source = useLegacy ? exponential : fog ?? exponential;
			var legacy = ReferenceEquals( source, exponential );
			if ( exponential != null && fog != null )
				motion.Information.Add( useLegacy
					? "The profile also carries the newer Fog override; HDRP's fogType selects ExponentialFog, so that one was used."
					: "The profile also carries a deprecated ExponentialFog override; the newer Fog override was used." );
			var distance = Number( source, legacy ? "fogDistance" : "meanFreePath", 200 );
			var colour = Colour( source, "color", 0.5, 0.5, 0.5 );
			// colorMode 0 is SkyColor, 1 is CustomColor; an authored colour only exists for the latter.
			var skyColour = !legacy && (int)Number( source, "colorMode", 0 ) == 0 && Value( source, "color" ) == null;
			var baseHeight = Number( source, legacy ? "fogBaseHeight" : "baseHeight", 0 );
			var maximumHeight = legacy ? 0 : Number( source, "maximumHeight", 50 );
			var attenuation = legacy ? Number( source, "fogHeightAttenuation", 0.2 ) : 0;
			motion.Fog = new( colour, skyColour, distance, baseHeight, attenuation, maximumHeight, legacy,
				"HDRP's exponential fog becomes Sandbox.Fog's exponential mode: AttenuationDistance is its fog distance in engine units and the colour is read as authored (linear)." +
				(legacy ? " ExponentialFog is deprecated in HDRP 17 but is the only fog record in the profile." : "") +
				(fogType == FogVolumetric || Value( source, "enableVolumetricFog" ) == "1" ? " Volumetric fog is not reproduced; only distance and height fog are." : "") +
				(attenuation > 0 || maximumHeight > 0 ? " Height fog maps to Sandbox.Fog's height ramp, a different falloff curve." : "") );
		}

		// Exposure lives in the Volume, not in HDAdditionalCameraData, which has no exposure field at all.
		// s&box's CameraComponent.AutoExposure is a multiplier tonemap (Min/MaxExposure are 1..3, not EV100
		// stops), so the authored range is reported instead of being mapped onto different units.
		if ( merged.GetValueOrDefault( "Exposure" ) is { } exposureOverride )
		{
			var mode = (int)Number( exposureOverride, "mode", 0 );
			var automatic = mode != 0;
			var fixedEv = Number( exposureOverride, "fixedExposure", 0 );
			var compensation = Number( exposureOverride, "compensation", 0 );
			var minimum = Number( exposureOverride, "limitMin", -1 );
			var maximum = Number( exposureOverride, "limitMax", 14 );
			static string Ev( double value ) => value.ToString( "0.##", CultureInfo.InvariantCulture );
			motion.Exposure = new( automatic, fixedEv, compensation, minimum, maximum, automatic
				? $"HDRP auto exposure (mode {mode}, EV100 {Ev( minimum )} to {Ev( maximum )}, compensation {Ev( compensation )}) is reported, not mapped: s&box's CameraComponent.AutoExposure is a multiplier tonemap, not an EV100 range."
				: $"HDRP fixed exposure EV100 {Ev( fixedEv )} is reported, not mapped: s&box's CameraComponent.AutoExposure has no fixed-EV mode." );
		}

		var unsupported = merged.Keys.Where( k => !Consumed.Contains( k, StringComparer.OrdinalIgnoreCase ) ).OrderBy( k => k, StringComparer.Ordinal ).ToArray();
		if ( unsupported.Length > 0 )
			motion.Information.Add( $"{unsupported.Length} HDRP volume override(s) are not converted by the environment step: {string.Join( ", ", unsupported.Take( 8 ) )}{(unsupported.Length > 8 ? ", …" : "")}." );
		return motion;
	}
}