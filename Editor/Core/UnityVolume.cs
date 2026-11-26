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
		// Post-processing overrides, named so an unconverted one reads as itself instead of "override <guid>".
		["f086a068d4c5889438831b3ae9afc11c"] = "Tonemapping",
		["24f077503be6ae942a1e1245dbd53ea9"] = "Bloom",
		["aaa3b8214f75b354e9ba2caadd022259"] = "DepthOfField",
		["9008a067f4d626c4d8bc4bc48f04bb89"] = "ScreenSpaceAmbientOcclusion",
		["4b8bcdf71d7fafa419fca1ed162f5fc9"] = "ColorAdjustments",
		["b51a78e223a2e504bb88a059b55229ea"] = "WhiteBalance",
		["384c4d03a551c44448145f4093304119"] = "ScreenSpaceReflection",
		["31394aa05878563408489d5c1688f3a0"] = "PathTracing",
		["2c1be1b6c95cd2e41b27903b9270817f"] = "Vignette",
		["7e9e4ed5a6f56fb4ebd693e39684f36f"] = "VolumetricClouds",
		["4bff652df78c97c4f963064ad1f34619"] = "ColorCurves",
		["da5ab44aadfb1804db5fd470983ac1b8"] = "LiftGammaGain",
		["6b24b200358312b4fa1004e2431c2f1f"] = "ShadowsMidtonesHighlights",
		["a7649d9368d3a5c4ab8ad01a63e04962"] = "ChannelMixer",
		["0775cbb206825d541bf6c0297ee524ca"] = "SplitToning",
		["5a6b00fcf518bb94a90b408492e07b44"] = "FilmGrain",
		["bcf384b154398e341b6b29969c078198"] = "MotionBlur",
		["9c1bfcd0f0fa7b8468f281d6bbbaf320"] = "LensDistortion",
		["598e2d32e2c7b0c418e030c3236d663a"] = "ChromaticAberration",
		["4b709909182ba0943abef2c49ed59205"] = "PaniniProjection",
		["56b145d2b9ee1ac4f846968484e7485a"] = "ContactShadows",
		["32b6af8f7ad32324cb6941c3290e5895"] = "MicroShadowing",
		["7ddcec8a8eb2d684d833ac8f5d26aebd"] = "HDShadowSettings",
		["29b00527f85bb3346a4d2cb710971587"] = "VolumetricFog",
		["f034cba68ab55e046ae1445a42f18c0e"] = "IndirectLightingController",
		["464ccca2a3ef3ce429c63a5b5cd3be58"] = "SubSurfaceScattering",
	};

	/// <summary>The overrides this step reads. Anything else in the profile is named once in the report.</summary>
	static readonly string[] Consumed = { "VisualEnvironment", "PhysicallyBasedSky", "HDRISky", "GradientSky", "Fog", "ExponentialFog", "Exposure" };

	/// <summary>The overrides that become an s&amp;box post-process effect on the volume's PostProcessVolume.</summary>
	static readonly string[] Mapped = { "Tonemapping", "Bloom", "DepthOfField", "ScreenSpaceAmbientOcclusion" };

	/// <summary>The fields of each mapped override that the s&amp;box effect actually carries; any other overridden
	/// field on that override is reported as dropped rather than silently ignored.</summary>
	static readonly Dictionary<string, string[]> MappedFields = new( StringComparer.OrdinalIgnoreCase )
	{
		["Tonemapping"] = new[] { "mode" },
		["Bloom"] = new[] { "intensity", "threshold", "tint" },
		["DepthOfField"] = new[] { "focusDistance", "nearFocusStart", "farFocusEnd" },
		["ScreenSpaceAmbientOcclusion"] = new[] { "intensity" },
	};

	/// <summary>Whether an override component actually overrides any of its fields; a profile keeps entries it
	/// switched off, and those supply no data to convert or to report.</summary>
	static bool Overridden( string body ) => Regex.IsMatch( body, @"m_OverrideState:\s*1" );

	/// <summary>
	/// One global HDRP volume's post-processing, as the scene export emits it: a Sandbox.PostProcessVolume
	/// carrying the Volume's priority and weight, with one child object per effect the profile overrides.
	/// s&amp;box's effects take different fields from HDRP's, so only the values that mean the same thing are
	/// carried over; anything else is named in <see cref="Unsupported"/> rather than guessed.
	/// </summary>
	internal sealed class VolumePost
	{
		public long ObjectId;
		public string Name;
		public string ProfileName;
		public string ProfilePath;
		public int Priority;
		public double Weight;
		/// <summary>HDRP Tonemapping.mode, or -1 when the profile does not override it.</summary>
		public int TonemapMode = -1;
		public bool Bloom;
		public double BloomStrength = 1;
		public bool BloomThreshold;
		public double BloomThresholdValue = 1;
		public double BloomTintR = 1, BloomTintG = 1, BloomTintB = 1;
		public bool Dof;
		public double FocalDistance, FocusRange;
		public bool Ao;
		public double AoIntensity = 1;
		/// <summary>Overridden options on the mapped effects that s&amp;box has no field for, e.g. Bloom's scatter.</summary>
		public List<string> Dropped { get; } = new();
		public List<string> Unsupported { get; } = new();
	}

	/// <summary>s&amp;box's tonemappers by HDRP's TonemappingMode: None 0, Neutral 1, ACES 2, Custom 3, External 4.
	/// s&amp;box has no untonemapped mode, so None falls back to its default and the report says so.</summary>
	internal static string TonemapMode( int mode ) => mode switch
	{
		1 => "AgX",
		2 or 3 => "ACES",
		_ => "HableFilmic"
	};

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

	/// <summary>The fog a Volume override asks for, in Unity units. Report-only: fog is intentionally omitted
	/// from the emitted scene per the 2026-11-24 Director decision, so these values feed the report alone.</summary>
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
		/// <summary>One entry per global Volume whose profile resolved, in scene order: the scene export writes a
		/// Sandbox.PostProcessVolume for it. Local volumes have no entry and are only reported.</summary>
		public List<VolumePost> PostVolumes { get; } = new();
		/// <summary>URP global Volumes, read only when the scene's pipeline is Urp.</summary>
		public List<UrpPost> UrpPostVolumes { get; } = new();
		public List<string> Information { get; } = new();
	}

	/// <summary>
	/// A URP global Volume's post-processing. URP keeps overrides in a VolumeProfile .asset and applies
	/// them through a shared Core Volume component; this is deliberately separate from
	/// <see cref="VolumePost"/> so the HDRP path is not touched. Every override is enumerated with its
	/// m_OverrideState before any of it is mapped.
	/// </summary>
	internal sealed class UrpPost
	{
		public long ObjectId;
		public string Name;
		public string ProfileName;
		public string ProfilePath;
		public string ProfileGuid;
		public int Priority;
		public double Weight;
		/// <summary>URP 17.7 TonemappingMode: None 0, Neutral 1, ACES 2, AgX 3.</summary>
		public int TonemapMode = -1;
		public bool Bloom, Vignette, Ao, ColorAdjustments;
		/// <summary>Null means the profile did not override the field: no value is invented for it.</summary>
		public double? BloomStrength, BloomThreshold, VignetteIntensity, VignetteSmoothness, VignetteRounded, AoIntensity, ColorContrast, ColorSaturation;
		public double[] BloomTint, VignetteColor, VignetteCenter;
		public List<string> Enumeration { get; } = new();
		public List<string> Dropped { get; } = new();
		public List<string> Unsupported { get; } = new();
		/// <summary>Mapped override components with active: 0; URP does not apply them.</summary>
		public List<string> Inactive { get; } = new();
	}

	/// <summary>
	/// The URP-only additional camera/light MonoBehaviours are positive evidence. The shared Core Volume
	/// guid is deliberately NEUTRAL (it is also in HDRP's table), so a Volume alone never flips a scene;
	/// any other HDRP component wins and keeps that scene on its existing path. New URP rules only run
	/// when this returns Urp.
	/// </summary>
	internal static UnityPipeline ScenePipeline( IReadOnlyList<Block> blocks )
	{
		var urp = false;
		foreach ( var block in blocks )
		{
			if ( block.Stripped || block.Type != "114" ) continue;
			var guid = GuidOf( Field( block.Body, "m_Script" ) );
			// The Core Volume guid is shared with HDRP (UnityHdrp.cs:117): NEUTRAL for classification
			// and for the HDRP-evidence early-return. Only URP camera/light siblings decide Urp.
			if ( guid.Equals( UnityUrp.VolumeScriptGuid, StringComparison.OrdinalIgnoreCase ) ) continue;
			if ( UnityHdrp.Component( guid ) != null ) return UnityPipeline.Unknown;
			if ( UnityUrp.Component( guid ) is not { } component ) continue;
			if ( component.Kind is UrpComponentKind.Camera or UrpComponentKind.Light ) urp = true;
		}
		return urp ? UnityPipeline.Urp : UnityPipeline.Unknown;
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
			var profile = Parse( text, Path.GetFileNameWithoutExtension( asset.Path ) );
			var priority = (int)ScalarNumber( "priority", 0 );
			var weight = ScalarNumber( "weight", 1 );
			applied.Add( (priority, weight, name, profile) );
			// Every global Volume with a readable profile becomes a PostProcessVolume, whatever its priority:
			// HDRP blends them, and s&box needs one volume per profile to do the same.
			if ( weight > 0 )
			{
				// The GameObject id, not the MonoBehaviour's: the scene export looks the placement up in
				// Converter.Transforms, which is keyed by GameObject.
				var post = Post( IdOf( Field( block.Body, "m_GameObject" ) ), name, priority, weight, asset.Path, profile );
				motion.PostVolumes.Add( post );
			}
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
		// CASE (b): the winning Volume can be PhysicallyBased with no texture s&box can display, while a
		// different global profile in the same scene authors an HDRI texture. Using that texture is a NEW
		// LOOK-MATCH APPROXIMATION from a non-active profile, explicitly allowed by the Director's dated
		// "just get the skybox LOOKING like the other" instruction. It is not a recovered active sky value
		// and it fabricates no illumination; atmosphere, clouds and time of day stay unreproduced.
		string authoredHdri = null;
		foreach ( var appliedVolume in applied )
		{
			if ( (int)Number( appliedVolume.Profile.Get( "VisualEnvironment" ), "skyType", SkyNone ) != SkyHdri ) continue;
			var guid = GuidOf( Value( appliedVolume.Profile.Get( "HDRISky" ), "hdriSky" ) ?? "" );
			if ( guid.Length > 0 && byGuid.TryGetValue( guid, out var texture ) ) authoredHdri = texture.Path;
		}
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
				if ( authoredHdri != null )
				{
					motion.Sky = new( "HDRI look-match approximation", new double[] { 1, 1, 1 }, authoredHdri,
						$"NEW LOOK-MATCH MAPPING: the active merged sky is HDRP PhysicallyBased with no s&box counterpart; authored HDRI texture {authoredHdri} from a non-winning global Volume profile is displayed by a generated s&box sky material. This is a new approximation, not the recovered active sky value; atmosphere, clouds, time of day and illumination are not reproduced." );
					break;
				}
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
						: $"HDRP's HDRI sky texture {textureName} is displayed by a generated s&box sky material referencing that texture; the atmosphere, clouds and time of day are not reproduced." );
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
				(fogType == FogVolumetric || Value( source, "enableVolumetricFog" ) == "1" ? " Volumetric fog is not reproduced; only exponential distance fog is emitted." : "") +
				(attenuation > 0 || maximumHeight > 0 ? " HDRP height fog (baseHeight / attenuation / maximumHeight) is not reproduced: s&box's HeightColor alpha is a full height-fog opacity with no authored Unity counterpart." : "") );
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

		// Every override the environment does not read is named per volume by AddPostNotes, including the ones
		// the post-processing step does not map, so no override goes unmentioned and none is listed twice.
		return motion;
	}

	// ---- URP post-processing (only called when ScenePipeline == Urp) ---------------------------------------

	/// <summary>The URP override component names with an s&amp;box effect counterpart.</summary>
	static readonly string[] UrpMapped =
	{
		"Bloom", "Vignette", "Tonemapping", "ScreenSpaceAmbientOcclusionVolumeOverride", "ScreenSpaceAmbientOcclusion", "ColorAdjustments"
	};

	/// <summary>Fields of a mapped override that the matching s&amp;box effect actually carries.</summary>
	static readonly Dictionary<string, string[]> UrpCarried = new( StringComparer.OrdinalIgnoreCase )
	{
		["Bloom"] = new[] { "intensity", "threshold", "tint" },
		["Vignette"] = new[] { "intensity", "color", "center", "smoothness", "rounded" },
		["Tonemapping"] = new[] { "mode" },
		["ScreenSpaceAmbientOcclusionVolumeOverride"] = new[] { "m_Intensity" },
		["ScreenSpaceAmbientOcclusion"] = new[] { "m_Intensity" },
		["ColorAdjustments"] = new[] { "contrast", "saturation" },
	};

	static string UrpName( string type ) => type == "ScreenSpaceAmbientOcclusionVolumeOverride" ? "ScreenSpaceAmbientOcclusion" : type;

	/// <summary>Every candidate override field with its serialized m_OverrideState and m_Value, including
	/// inactive ones: the report must show what was present and what won, not just what was mapped.</summary>
	static void EnumerateUrp( Profile profile, List<string> lines )
	{
		foreach ( var item in profile.Overrides )
		{
			var active = Regex.Match( item.Body, @"(?m)^  active:[ 	]*([^\r\n]*)" );
			if ( active.Success ) lines.Add( $"{UrpName( item.Type )}.active {active.Groups[1].Value.Trim()}" );
			foreach ( Match field in Regex.Matches( item.Body, @"(?m)^  (?<name>[A-Za-z_][A-Za-z0-9_]*):[ 	]*\r?\n(?<body>(?:    [^\r\n]*\r?\n?)+)" ) )
			{
				var state = Regex.Match( field.Groups["body"].Value, @"m_OverrideState:\s*([01])" );
				if ( !state.Success ) continue;
				var value = Regex.Match( field.Groups["body"].Value, @"m_Value:[ 	]*([^\r\n]*)" );
				lines.Add( $"{UrpName( item.Type )}.{field.Groups["name"].Value} m_OverrideState:{state.Groups[1].Value} m_Value:{(value.Success ? value.Groups[1].Value.Trim() : "<none>")}" );
			}
		}
	}

	/// <summary>A URP Vignette center / URP colour vector: {x: 0.5, y: 0.5}.</summary>
	static double[] Vector2( Override source, string field )
	{
		var value = Value( source, field );
		if ( string.IsNullOrEmpty( value ) ) return null;
		return new[] { "x", "y" }.Select( axis =>
		{
			var match = Regex.Match( value, @"\b" + axis + @":\s*([-+0-9.eE]+)" );
			return match.Success && double.TryParse( match.Groups[1].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var number ) ? number : 0;
		} ).ToArray();
	}

	/// <summary>URP applies an override component only when active: 1. Older profiles omit the field;
	/// those are active by default.</summary>
	static bool Active( Override source )
	{
		var match = Regex.Match( source.Body, @"(?m)^  active:[ 	]*([01])" );
		return !match.Success || match.Groups[1].Value == "1";
	}

	/// <summary>Builds one URP PostProcessVolume's data. Only m_OverrideState: 1 values become effects;
	/// everything else is enumerated and either explicitly reported as dropped/unmapped or left absent.</summary>
	static UrpPost UrpVolumePost( long objectId, string name, string path, string guid, int priority, double weight, Profile profile )
	{
		var post = new UrpPost
		{
			ObjectId = objectId, Name = name, ProfileName = profile.Name, ProfilePath = path,
			ProfileGuid = guid, Priority = priority, Weight = weight
		};
		EnumerateUrp( profile, post.Enumeration );
		if ( profile.Get( "Tonemapping" ) is { } tone && Overridden( tone.Body ) )
		{
			if ( Active( tone ) ) post.TonemapMode = (int)Number( tone, "mode", 0 );
			else post.Inactive.Add( "Tonemapping" );
		}
		if ( profile.Get( "Bloom" ) is { } bloom && Overridden( bloom.Body ) )
		{
			if ( !Active( bloom ) ) post.Inactive.Add( "Bloom" );
			else
			{
				post.Bloom = true;
				if ( Value( bloom, "intensity" ) != null ) post.BloomStrength = Number( bloom, "intensity", 0 );
				if ( Value( bloom, "threshold" ) != null ) post.BloomThreshold = Number( bloom, "threshold", 0 );
				if ( Value( bloom, "tint" ) != null ) post.BloomTint = Colour( bloom, "tint", 1, 1, 1 );
			}
		}
		if ( profile.Get( "Vignette" ) is { } vignette && Overridden( vignette.Body ) )
		{
			if ( !Active( vignette ) ) post.Inactive.Add( "Vignette" );
			else
			{
				post.Vignette = true;
				if ( Value( vignette, "intensity" ) != null ) post.VignetteIntensity = Number( vignette, "intensity", 0 );
				if ( Value( vignette, "color" ) != null ) post.VignetteColor = Colour( vignette, "color", 0, 0, 0 );
				if ( Value( vignette, "center" ) != null ) post.VignetteCenter = Vector2( vignette, "center" );
				if ( Value( vignette, "smoothness" ) != null ) post.VignetteSmoothness = Number( vignette, "smoothness", 0 );
				if ( Value( vignette, "rounded" ) != null ) post.VignetteRounded = Number( vignette, "rounded", 0 );
			}
		}
		var ao = profile.Get( "ScreenSpaceAmbientOcclusionVolumeOverride" ) ?? profile.Get( "ScreenSpaceAmbientOcclusion" );
		if ( ao != null && Overridden( ao.Body ) )
		{
			if ( !Active( ao ) ) post.Inactive.Add( "ScreenSpaceAmbientOcclusion" );
			else
			{
				post.Ao = true;
				if ( Value( ao, "m_Intensity" ) != null ) post.AoIntensity = Number( ao, "m_Intensity", 0 );
			}
		}
		if ( profile.Get( "ColorAdjustments" ) is { } color && Overridden( color.Body ) )
		{
			if ( !Active( color ) ) post.Inactive.Add( "ColorAdjustments" );
			else
			{
				post.ColorAdjustments = true;
				if ( Value( color, "contrast" ) != null ) post.ColorContrast = Number( color, "contrast", 0 );
				if ( Value( color, "saturation" ) != null ) post.ColorSaturation = Number( color, "saturation", 0 );
			}
		}
		foreach ( var item in profile.Overrides )
		{
			if ( UrpMapped.Contains( item.Type, StringComparer.OrdinalIgnoreCase ) ) continue;
			if ( !Overridden( item.Body ) ) continue;
			post.Unsupported.Add( UrpName( item.Type ) );
		}
		foreach ( var item in profile.Overrides )
		{
			if ( !UrpCarried.TryGetValue( item.Type, out var carried ) ) continue;
			foreach ( Match field in Regex.Matches( item.Body, @"(?m)^  (?<name>[A-Za-z_][A-Za-z0-9_]*):[ \t]*\r?\n(?<body>(?:    [^\r\n]*\r?\n?)+)" ) )
			{
				var fieldName = field.Groups["name"].Value;
				if ( fieldName == "active" || carried.Contains( fieldName, StringComparer.OrdinalIgnoreCase ) ) continue;
				if ( !Regex.IsMatch( field.Groups["body"].Value, @"m_OverrideState:\s*1" ) ) continue;
				var value = Regex.Match( field.Groups["body"].Value, @"m_Value:[ \t]*([^\r\n]*)" );
				var text = value.Success ? value.Groups[1].Value.Trim() : "";
				if ( text.Length == 0 || text == "{fileID: 0}" ) continue;
				post.Dropped.Add( $"{UrpName( item.Type )}.{fieldName} {text}" );
			}
		}
		return post;
	}

	/// <summary>
	/// URP volumes use the same Core Volume component as HDRP, so the guid alone cannot discriminate.
	/// This reader is called only for a scene whose MonoBehaviours show URP evidence; local Volumes and
	/// profiles that cannot be read stay report-only. URP's project-wide DefaultVolumeProfile is a base
	/// stack rather than a scene Volume: it is named once, never emitted as a second volume.
	/// </summary>
	internal static Motion ReadUrp( IReadOnlyList<Block> blocks, IReadOnlyDictionary<string, UnityAsset> byGuid )
	{
		var motion = new Motion();
		var byId = blocks.GroupBy( b => b.Id ).ToDictionary( g => g.Key, g => g.First() );
		string Owner( string body )
		{
			var objectId = IdOf( Field( body, "m_GameObject" ) );
			return byId.TryGetValue( objectId, out var go ) ? Scalar( go.Body, "m_Name" ) : "";
		}

		var unresolved = new List<string>();
		var locals = 0;
		foreach ( var block in blocks )
		{
			if ( block.Stripped || block.Type != "114" ) continue;
			if ( UnityUrp.Component( GuidOf( Field( block.Body, "m_Script" ) ) ) is not { Kind: UrpComponentKind.GlobalVolume } ) continue;
			var name = Owner( block.Body );
			if ( name.Length == 0 ) name = $"Volume {block.Id}";
			if ( Scalar( block.Body, "m_IsGlobal" ) != "1" ) { locals++; continue; }
			var guid = ProfileGuid( block.Body );
			if ( guid.Length == 0 || !byGuid.TryGetValue( guid, out var asset ) )
			{
				unresolved.Add( guid.Length == 0 ? $"{name} has no profile" : $"{name} names profile {guid}, which is not in the package" );
				continue;
			}
			string text;
			try { text = UnityImport.ReadText( asset.Source ); }
			catch ( InvalidDataException ex ) { unresolved.Add( $"{name}: {asset.Path} could not be read ({ex.Message})" ); continue; }
			double ScalarNumber( string field, double fallback ) =>
				double.TryParse( Scalar( block.Body, field ), NumberStyles.Float, CultureInfo.InvariantCulture, out var value ) ? value : fallback;
			var profile = Parse( text, Path.GetFileNameWithoutExtension( asset.Path ) );
			var post = UrpVolumePost( IdOf( Field( block.Body, "m_GameObject" ) ), name, asset.Path, guid,
				(int)ScalarNumber( "priority", 0 ), ScalarNumber( "weight", 1 ), profile );
			motion.UrpPostVolumes.Add( post );
		}
		if ( motion.UrpPostVolumes.Count > 0 )
			motion.Information.Add( $"Environment read from {motion.UrpPostVolumes.Count} URP global Volume(s): {string.Join( ", ", motion.UrpPostVolumes.Select( v => $"{v.Name} (winning profile {v.ProfileName}, guid {v.ProfileGuid})" ) )}." );
		if ( locals > 0 )
			motion.Information.Add( $"{locals} local URP Volume(s) affect only part of the scene; their overrides are enumerated as report-only and no PostProcessVolume was written for them." );
		if ( unresolved.Count > 0 )
			motion.Information.Add( $"URP volume(s) that could not be read: {string.Join( "; ", unresolved.Take( 4 ) )}{(unresolved.Count > 4 ? ", …" : "")}." );
		if ( byGuid.Values.Any( a => a.Path.EndsWith( "DefaultVolumeProfile.asset", StringComparison.OrdinalIgnoreCase ) ||
			a.Guid.Equals( UnityUrp.DefaultVolumeProfileGuid, StringComparison.OrdinalIgnoreCase ) ) )
			motion.Information.Add( "URP's project-wide DefaultVolumeProfile is the base stack referenced by UniversalRenderPipelineGlobalSettings, not a scene Volume. It is not emitted a second time: the scene global Volume's profile wins; overrides it supplies that the scene profile does not override stay at engine defaults." );
		motion.Information.Add( "Sandbox.ScreenSpaceReflections is obsolete and a no-op in this engine checkout; no URP override is converted into it. Baked/lightmap data is not converted." );
		return motion;
	}

	/// <summary>One report block per emitted URP volume: the winning profile, every candidate override
	/// with its m_OverrideState, the source values actually mapped, and everything left behind.</summary>
	internal static void AddUrpPostNotes( UrpPost post, List<string> information )
	{
		var mapped = new List<string>();
		if ( post.TonemapMode >= 0 ) mapped.Add( $"Tonemapping mode {post.TonemapMode}" );
		if ( post.Bloom ) mapped.Add( $"Bloom Strength {post.BloomStrength?.ToString( "R", CultureInfo.InvariantCulture ) ?? "not overridden"}" );
		if ( post.Vignette ) mapped.Add( $"Vignette Intensity {post.VignetteIntensity?.ToString( "R", CultureInfo.InvariantCulture ) ?? "not overridden"}" );
		if ( post.Ao ) mapped.Add( $"AmbientOcclusion Intensity {post.AoIntensity?.ToString( "R", CultureInfo.InvariantCulture ) ?? "not overridden"}" );
		if ( post.ColorAdjustments ) mapped.Add( $"ColorAdjustments contrast {post.ColorContrast?.ToString( "R", CultureInfo.InvariantCulture ) ?? "not overridden"} / saturation {post.ColorSaturation?.ToString( "R", CultureInfo.InvariantCulture ) ?? "not overridden"}" );
		information.Add( $"{post.Name} ({post.ProfileName}): URP global Volume converted at priority {post.Priority}, blend weight {post.Weight.ToString( "R", CultureInfo.InvariantCulture )}; winning profile {post.ProfileName} ({post.ProfileGuid}). Mapped SOURCE values: {string.Join( "; ", mapped.Count > 0 ? mapped : new[] { "none" } )}." );
		information.Add( $"{post.Name}: URP override enumeration from winning profile {post.ProfileName} ({post.Enumeration.Count} entries): {string.Join( "; ", post.Enumeration )}." );
		foreach ( var dropped in post.Dropped )
			information.Add( $"{post.Name}: URP override {dropped} is overridden in the source but has no same-unit s&box field; reported, not mapped." );
		foreach ( var type in post.Unsupported.Distinct() )
			information.Add( $"{post.Name}: URP override {type} has no s&box effect counterpart; reported, not converted." );
		foreach ( var type in post.Inactive.Distinct() )
			information.Add( $"{post.Name}: URP override {type} has active: 0, so URP does not apply it; reported, not mapped." );
	}

	/// <summary>The post-processing one global Volume's profile supplies. Only the fields whose meaning is the
	/// same in both engines are read; s&amp;box's effects take different units, so a value that would have to be
	/// guessed (an occlusion radius in metres becoming pixels) is left at its default and named in the report.</summary>
	static VolumePost Post( long objectId, string name, int priority, double weight, string path, Profile profile )
	{
		var post = new VolumePost { ObjectId = objectId, Name = name, ProfileName = profile.Name, ProfilePath = path, Priority = priority, Weight = weight };
		if ( profile.Get( "Tonemapping" ) is { } tone && Overridden( tone.Body ) )
			post.TonemapMode = (int)Number( tone, "mode", 0 );
		if ( profile.Get( "Bloom" ) is { } bloom && Overridden( bloom.Body ) )
		{
			post.Bloom = true;
			post.BloomStrength = Number( bloom, "intensity", 1 );
			post.BloomThreshold = Value( bloom, "threshold" ) != null;
			post.BloomThresholdValue = Number( bloom, "threshold", 1 );
			var tint = Colour( bloom, "tint", 1, 1, 1 );
			(post.BloomTintR, post.BloomTintG, post.BloomTintB) = (tint[0], tint[1], tint[2]);
		}
		if ( profile.Get( "DepthOfField" ) is { } dof && Overridden( dof.Body ) )
		{
			post.Dof = true;
			post.FocalDistance = Number( dof, "focusDistance", 0 );
			var near = Number( dof, "nearFocusStart", 0 );
			var far = Number( dof, "farFocusEnd", 0 );
			post.FocusRange = far > near ? far - near : 0;
		}
		if ( profile.Get( "ScreenSpaceAmbientOcclusion" ) is { } ao && Overridden( ao.Body ) )
		{
			post.Ao = true;
			post.AoIntensity = Number( ao, "intensity", 1 );
		}
		foreach ( var @override in profile.Overrides )
		{
			if ( Consumed.Contains( @override.Type, StringComparer.OrdinalIgnoreCase ) ) continue;
			if ( Mapped.Contains( @override.Type, StringComparer.OrdinalIgnoreCase ) ) continue;
			if ( !Overridden( @override.Body ) ) continue;
			post.Unsupported.Add( @override.Type );
		}
		foreach ( var type in Mapped )
		{
			var dropped = Dropped( profile, type ).ToArray();
			if ( dropped.Length == 0 ) continue;
			post.Dropped.Add( dropped.Length <= 3
				? $"{type} {string.Join( ", ", dropped )}"
				: $"{type} {string.Join( ", ", dropped.Take( 3 ) )} and {dropped.Length - 3} more" );
		}
		return post;
	}

	/// <summary>Every overridden field of a mapped override that the s&amp;box effect has no field for, as
	/// "Field value": Bloom's scatter and anamorphic, Tonemapping's curve parameters, DOF's focus mode.</summary>
	static IEnumerable<string> Dropped( Profile profile, string type )
	{
		var @override = profile.Get( type );
		if ( @override == null || !MappedFields.TryGetValue( type, out var carried ) ) yield break;
		foreach ( Match field in Regex.Matches( @override.Body, @"(?m)^  (?<name>[A-Za-z_][A-Za-z0-9_]*):[ 	]*\r?\n(?<body>(?:    [^\r\n]*\r?\n?)+)" ) )
		{
			var name = field.Groups["name"].Value;
			if ( name == "active" || carried.Contains( name, StringComparer.OrdinalIgnoreCase ) ) continue;
			var body = field.Groups["body"].Value;
			if ( !Regex.IsMatch( body, @"m_OverrideState:\s*1" ) ) continue;
			var value = Regex.Match( body, @"m_Value:[ 	]*([^\r\n]*)" );
			var text = value.Success ? value.Groups[1].Value.Trim() : "";
			// An unset object reference (lutTexture {fileID: 0}) carries no value to report.
			if ( text.Length == 0 || text == "{fileID: 0}" ) continue;
			yield return $"{name} {text}";
		}
	}

	/// <summary>The lines one emitted PostProcessVolume owes: what it carries, the options s&amp;box has no field
	/// for, and every override that stayed behind. The scene export calls this per volume it writes, so two Unity
	/// Volumes that share a profile (and therefore one PostProcessVolume) are reported once.</summary>
	internal static void AddPostNotes( VolumePost post, List<string> information )
	{
		var effects = new List<string>();
		if ( post.TonemapMode >= 0 ) effects.Add( $"Tonemapping ({TonemapMode( post.TonemapMode )})" );
		if ( post.Bloom ) effects.Add( "Bloom" );
		if ( post.Dof ) effects.Add( "DepthOfField" );
		if ( post.Ao ) effects.Add( "AmbientOcclusion" );
		var weight = post.Weight.ToString( "0.##", CultureInfo.InvariantCulture );
		information.Add( $"{post.Name} ({post.ProfileName}): post-processing converted to a Sandbox.PostProcessVolume at priority {post.Priority}, blend weight {weight}" +
			(effects.Count > 0 ? $" with {string.Join( ", ", effects )}." : ", with no s&box effect (the profile overrides none of the supported ones).") );
		if ( post.TonemapMode == 0 )
			information.Add( $"{post.Name}: HDRP tonemapping is set to None; s&box has no untonemapped mode, so its HableFilmic default is used." );
		if ( post.Ao )
			information.Add( $"{post.Name}: ambient occlusion intensity is converted; HDRP's radius is in metres and s&box's is in screen pixels, so the radius is left at its default." );
		if ( post.Dropped.Count > 0 )
			information.Add( $"{post.Name}: options on the converted overrides have no s&box field and were not converted: {string.Join( ", ", post.Dropped.Take( 8 ) )}{(post.Dropped.Count > 8 ? ", …" : "")}." );
		if ( post.Unsupported.Count > 0 )
			information.Add( $"{post.Name} ({post.ProfileName}): {post.Unsupported.Count} HDRP override(s) have no s&box counterpart and were not converted: {string.Join( ", ", post.Unsupported.Take( 8 ) )}{(post.Unsupported.Count > 8 ? ", …" : "")}." );
	}
}