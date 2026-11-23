# Unity 6.7 (URP 17.7) to s&box: version-specific porting notes

Research status: 2026-11-23. Unity 6000.7.0b10 is installed at
`D:/Applications/Unity/6000.7.0b10`. URP 17.7.0 source is available at
`D:/Applications/Unity/6000.7.0b10/Editor/Data/Resources/PackageManager/BuiltInPackages/com.unity.render-pipelines.universal/`.
The current URP template package is `com.unity.template.urp-blank@17.3.2` (package.json:
`"unity": "6000.7"`, URP 17.7.0).

Scope: what changed in Unity 6.7 that affects a Unity-to-s&box port, and how each 6.7 feature maps
to s&box. General parity gaps are in [unity_vs_sbox_parity.md](unity_vs_sbox_parity.md).

## 1. What 6.7 adds that matters for porting

| 6.7 feature | Source evidence | s&box equivalent | Porting impact |
|---|---|---|---|
| AgX tonemapping mode | `Runtime/Overrides/Tonemapping.cs:32` adds `AgX`; `:213` adds `agxContrast` (default 1.193, range 0.5-3); `:219` adds `agxMidGrey` (default 0.18, range 0.10-0.30). | `Tonemapping` has `AgX` (`engine/Sandbox.Engine/Scene/Components/PostProcessing/Effects/Tonemapping.cs:41`), implemented as a Punchy AgX look (pow 1.35, saturation 1.4, pow 2.2 in `game/core/shaders/tonemapping/tonemapping.shader`). | APPROX, not identical. Use s&box AgX and A/B the two. URP exposes contrast and mid-grey; s&box exposes no AgX controls. |
| Fixed exposure volume override | `Runtime/Overrides/ExposureVolume.cs`: `Mode.Fixed` only (`:17-30`), `fixedExposure` in EV (`:40`), default 0, clamp -126..126 (`:32-44`). `Runtime/Passes/ExposurePass.cs:49` computes `2^-fixedExposure`. | No standalone Exposure component. `Tonemapping` auto-exposure group (`AutoExposureEnabled`, `MinimumExposure`, `MaximumExposure`, `ExposureCompensation`, `Rate`) or `CameraComponent.AutoExposure`. `ExposureCompensation` stores `2^value` (`SceneCamera.Tonemap.cs:95-101`). | APPROX. URP positive EV darkens (`2^-EV`); s&box positive compensation brightens (`2^+EV`). Invert the sign: `sbox_compensation = -unity_fixedExposure`. Still no auto exposure in URP. |
| GTAO and SSR in URP | 6.7 release notes and forum announcements; s&box has GTAO-style `AmbientOcclusion` and core SSR already. | `AmbientOcclusion` (GTAO) and renderer-level SSR. | Low. Retune radius/intensity; no new feature work. |
| On-tile post-processing | URP 6.7 on-tile post path; s&box has its own post stages. | N/A for desktop. | Not a porting blocker for PC. |
| Surface Cache GI | Announced for 6.7 beta; s&box has Indirect Light Volumes (Feedback Preview, editor-bake-only). | `IndirectLightVolume` / DDGI. | Both are immature; budget manual GI fix-up. |
| DLSS 4.5 Super Resolution | 6.7 release notes; s&box exposes DLSS/FSR3 upscalers. | `UpscalerMode` (FSR1/FSR3/DLSS). | Settings mapping only. |
| Default template still uses Neutral | `com.unity.template.urp-blank@17.3.2` ships `SampleSceneProfile.asset` with Tonemapping mode 1 (Neutral), Bloom 0.25, Vignette 0.2; scene camera has `m_RenderPostProcessing: 1`. The project-level `DefaultVolumeProfile.asset` has Tonemapping mode 0 (None). | `Tonemapping` defaults to `HableFilmic`; Bloom/Vignette default on when the components exist. | Port the template look as Neutral -> HableFilmic + 1.58 EV, then add Bloom 0.25 and Vignette 0.2. |

Notes:

- URP 6.7 still has no native automatic exposure. The `ExposureVolume` enum contains only `Fixed`.
  HDRP still has the automatic/histogram/curve/physical-camera modes; URP does not.
- URP 6.7's `Tonemapping` default mode remains `None` (`Tonemapping.cs:192`). The template scene
  overrides it with `Neutral` through its Global Volume.
- The template's `SampleSceneProfile` is the default look, not the engine default. If the scene
  volume is removed, URP falls back to the project `DefaultVolumeProfile` (None).

## 2. Exact 6.7 to s&box mapping

| Unity 6.7 setting | Value / behavior | s&box target | Mapping |
|---|---|---|---|
| Tonemapping Mode = None | No curve; `saturate(input)` in URP. | `Tonemapping` Mode = Linear, Auto Exposure Enabled = false. | Closest match. s&box has no None; Linear multiplies by 1 when auto exposure is off. |
| Tonemapping Mode = Neutral | Hable/Hejl/BT.2390 range reduction; default in the template. | `HableFilmic`, `ExposureMethod = RGB`. | Measured: +1.58 EV (2.99x) matches Unity Neutral with sRGB RMSE 0.005. |
| Tonemapping Mode = ACES | ACES approximation. | `ACES`. | Name match, different implementation; visual A/B required. |
| Tonemapping Mode = AgX | AgX with `agxContrast` and `agxMidGrey`. | `AgX`. | Same family, different look controls. Start with defaults, then tune; s&box is Punchy AgX. |
| Exposure Mode = Fixed, `fixedExposure` = EV | Multiplier = `2^-EV`. | `Tonemapping` auto exposure pinned, `ExposureCompensation = -EV`. | Sign inversion. Pin `MinimumExposure = MaximumExposure` so the adaptive meter cannot move; verify the native clamp order visually. |
| Exposure Mode = Automatic / AutomaticHistogram (HDRP only) | Metered exposure. | `AutoExposureEnabled = true`, `MinimumExposure` / `MaximumExposure` / `Rate`. | APPROX; s&box has a simpler auto exposure. |
| Exposure Mode = CurveMapping / UsePhysicalCamera (HDRP only) | Curve or physical camera. | No direct equivalent. | Bake a fixed EV or use auto exposure; document the loss. |
| Bloom: threshold, intensity, scatter, tint, clamp, dirt | Threshold default 0.9 gamma-space; intensity default 0; scatter 0.7. | `Bloom`: `Threshold` (0..2), `Strength` (0..10), `Tint`. | APPROX. No scatter, clamp, dirt, filter or downscale. Threshold ranges differ; s&box has a v1 upgrader mapping old values to `old/2 + 1`. |
| Vignette: intensity, smoothness, rounded, color, center | Intensity default 0; smoothness 0.2; rounded false. | `Vignette`: `Intensity` (default 1), `Smoothness` (default 1), `Roundness` (0..1), `Color`, `Center`. | APPROX. Zero the s&box defaults first; Roundness is continuous. |
| Film Grain: type, intensity, response, texture | Intensity default 0; response 0.8; 11 presets plus custom texture. | `FilmGrain`: `Intensity` (default 0.1), `Response` (default 0.5). | APPROX. No presets or custom texture. |
| Chromatic Aberration: intensity | Default 0; applied before tonemapping. | `ChromaticAberration`: `Scale` (default 0.33), `Offset` vector. | APPROX. s&box runs after tonemapping; opposite side of the tonemap. |
| Color Adjustments: postExposure, contrast, colorFilter, hueShift, saturation | postExposure EV default 0; contrast/saturation neutral 0; colorFilter white. | `ColorAdjustments`: `Brightness` (default 1), `Contrast` (default 1), `Saturation` (default 1), `HueRotate` (default 0). | APPROX. No colorFilter; neutral values differ; s&box runs after tonemapping. |
| Color Curves | Master/R/G/B plus hue/sat curves. | `ColorGrading` curves. | APPROX. s&box caps at 4 frames; no master curve. |
| Color Lookup | Strict 2D strip LUT. | `ColorGrading` `GradingMethod = LUT`, `LookupTexture`, `BlendFactor`. | APPROX. LUT layout/format unverified. |
| White Balance: temperature, tint | Neutral 0/0. | `ColorGrading` `GradingMethod = TemperatureControl`, `ColorTempK`. | PARTIAL. Temperature only; no tint. |
| Lift/Gamma/Gain, Split Toning, Channel Mixer, Shadows/Midtones/Highlights | Volume overrides. | MISSING. | Port as new `BasePostProcess` components or approximate with ColorGrading curves. |
| Lens Distortion, Panini Projection | Volume overrides. | MISSING. | Port the UV math as new components. |
| Screen Space Lens Flare | Volume override. | MISSING. | Port as a bloom-mip-driven pass. |
| Depth of Field | Gaussian/Bokeh; focusDistance, aperture, focalLength, blades. | `DepthOfField`: FocalDistance, FocusRange, BlurSize, FrontBlur/BackBlur, DebugView. | APPROX. Different model; retune. |
| Motion Blur | CameraOnly/CameraAndObjects; intensity, quality, clamp. | `MotionBlur`: Scale; quality convar. | APPROX. Camera-velocity only; no clamp. |
| SSAO | Standard/GTAO with many controls. | `AmbientOcclusion`: GTAO, Intensity, Radius (world units), FalloffRange, ThinCompensation. | APPROX. Different units and parameter set. |
| SSR | 6.7 renderer feature. | Renderer-level SSR. | MAPPED at feature level; no tunable volume override in s&box. |
| Fog | URP uses RenderSettings fog, not a volume override. | `Fog`: distance/height/sun. | APPROX. HDRP volumetric fog has no direct equivalent. |
| Decals | Decal Projector + renderer feature. | `Decal` component + `DecalDefinition`. | MAPPED. Asset conversion only. |

## 3. What 6.7 still does not give s&box

These are gaps that remain after accounting for 6.7. They are not fixed by the 6.7 additions:

- HDR display output. URP 6.7 supports HDR output; s&box appears SDR-only.
- HDRP-style automatic exposure history/histogram/curve modes. URP 6.7 is fixed-only; s&box has a
  simpler auto exposure.
- VFX Graph GPU particles. s&box particles are CPU-simulated.
- Cinemachine virtual cameras, dolly, ClearShot.
- UI Toolkit / UXML / USS / UI Builder.
- Addressables / remote content catalogs.
- Timeline / Playable API.
- Native TAA mode and public motion-vector pass. s&box uses CMAA2 and FSR3/DLSS temporal AA.
- HDRP area lights, volumetric fog/clouds, ray tracing/SSGI, planar reflection probe components.
- HDRP Lit surface features: transmission, subsurface scattering, clearcoat, anisotropy,
  iridescence, tessellation, detail maps.
- Platform reach: s&box standalone is Windows-only, preview, Valve/Facepunch-approved, Steam-first.

## 4. Porting checklist for a Unity 6.7 project

1. Read the source Volume profile. Record the Tonemapping mode, Exposure mode and EV, Bloom,
   Vignette, Color Adjustments and any HDRP-only overrides.
2. Decide the target look:
   - None -> s&box Linear + auto exposure off.
   - Neutral -> HableFilmic + 1.58 EV.
   - ACES -> ACES.
   - AgX -> AgX.
3. Set s&box post-process defaults to off before matching: Tonemapping auto exposure off or pinned,
   Bloom Strength 0, Vignette Intensity 0, FilmGrain Intensity 0, CA Scale 0, AO Intensity 0.
4. Map exposure:
   - Fixed EV -> `ExposureCompensation = -EV` with min/max pinned, or `ColorAdjustments.Brightness`
     if the value is within 2x and post-tonemap brightening is acceptable.
   - Automatic -> s&box auto exposure; tune Rate and min/max.
5. Map Bloom/Vignette by visual fit, not field copy. Record scatter/clamp/dirt as not ported.
6. Check order-sensitive effects: Chromatic Aberration moves from before tonemapping in URP to
   after in s&box; Color Adjustments moves from before/after to after. Re-tune, do not copy values.
7. Replace missing effects with new `BasePostProcess<T>` components. Start with LensDistortion and
   PaniniProjection, then the LUT-style grading effects.
8. Capture A/B references for AgX, ACES, Bloom and exposure. The 6.7 AgX and s&box AgX are not
   proven identical.
9. Verify HDR output assumptions. If the Unity project targets HDR displays, s&box has no matching
   output path; plan SDR only.
10. Re-check the importer's HDRP path. Direct HDRP -> s&box is still shorter than HDRP -> URP ->
    s&box, and the importer already reads HDRP materials, lights and Volumes.

## 5. Open verification items

- Numeric or visual A/B of URP 17.7 AgX against s&box AgX.
- Rendered A/B of URP `fixedExposure` against s&box `ExposureCompensation`, including the sign and
  the native min/max clamp order.
- Whether s&box has any HDR display output path in the current editor. No managed API was found.
- Whether the importer's HDRP light conversion should switch from baking into `LightColor` to the
  current `Light.Intensity` / `Temperature` properties.
- LUT layout compatibility between Unity ColorLookup and s&box ColorGrading.

## 6. Sources

Unity 6.7 / URP 17.7:

- Tonemapping: `D:/Applications/Unity/6000.7.0b10/Editor/Data/Resources/PackageManager/BuiltInPackages/com.unity.render-pipelines.universal/Runtime/Overrides/Tonemapping.cs`
- Exposure volume: `.../Runtime/Overrides/ExposureVolume.cs`
- Exposure pass: `.../Runtime/Passes/ExposurePass.cs`
- Exposure texture: `.../ShaderLibrary/DeclareExposureTexture.hlsl`
- URP 6.7 tonemapping docs: https://docs.unity3d.com/6000.7/Documentation/Manual/urp/post-processing-tonemapping.html
- HDR output docs: https://docs.unity3d.com/6000.3/Documentation/Manual/urp/post-processing/hdr-output.html
- 6.7 release notes (AgX, fixed exposure): https://unity.com/releases/editor/beta/6000.7.0b9

Template:

- `com.unity.template.urp-blank@17.3.2`, package.json: `"unity": "6000.7"`, URP 17.7.0.
- `ProjectData~/Assets/Settings/SampleSceneProfile.asset`: Tonemapping mode 1, Bloom 0.25,
  Vignette 0.2.
- `ProjectData~/Assets/Settings/DefaultVolumeProfile.asset`: Tonemapping mode 0.
- `ProjectData~/Assets/Scenes/SampleScene.unity`: camera `m_RenderPostProcessing: 1`.

s&box:

- Tonemapping: `engine/Sandbox.Engine/Scene/Components/PostProcessing/Effects/Tonemapping.cs`
- Tonemap shader: `game/core/shaders/tonemapping/tonemapping.shader`
- Exposure compensation: `engine/Sandbox.Engine/Systems/SceneSystem/SceneCamera.Tonemap.cs`
- Stage order: `engine/Sandbox.Engine/Systems/Render/Stage.cs`
- Light: `engine/Sandbox.Engine/Scene/Components/Light/Light.cs`

Repository paths are relative to the s&box Dev checkout:
`C:/cseliot/1-Development/1-SBox/1-Engine-Builds/Dev/sbox-public/`.
