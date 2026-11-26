# Unity to s&box parity: porting gaps and options

Research status: 2026-11-23. Scope: porting a Unity project (HDRP source, URP considered as an
intermediate) to s&box. This document records what s&box lacks, what the importer already covers,
and which free libraries or shader sources can fill the remaining holes.

Only Unity -> s&box gaps are in scope. Features that exist only in s&box are listed only where they
change the porting decision. "Parity" here means the visible result and the authoring workflow, not
byte-identical assets or settings.

Related documents:

- [Unity-To-SBox-Automated.md](Unity-To-SBox-Automated.md) - the importer pipeline and conversion
  rules.
- [unity_6_7_to_sbox.md](unity_6_7_to_sbox.md) - Unity 6.7 / URP 17.7 specific mapping.

## 1. Executive summary

1. Do not route HDRP -> URP -> s&box as a shipping pipeline. Unity's own migration guide says an
   HDRP-to-URP move requires rewriting most of the project: shaders, materials, VFX Graph nodes,
   lighting and post-processing. URP also drops HDRP-only features (physical light units, automatic
   exposure modes, area lights, volumetric fog/clouds, ray tracing/SSGI, HDRP Lit surface features)
   before the s&box port starts. That is two rewrites instead of one.
2. Use the direct HDRP -> s&box path already present in `unityimporterplus` for assets, materials,
   lights, environment and volume post-processing. Use URP only as a look-development reference
   (tonemapping, post-stack comparison) or as a fallback for features the importer cannot read.
3. There is no turnkey URP-Volume -> s&box post-process bridge. The useful bridge is a parameter
   mapping table plus rendered A/B reference images, with engine-bound components and shaders
   written directly in s&box.
4. The strongest measured bridge is tonemapping: s&box `HableFilmic` matches Unity `Neutral` at
   about +1.58 EV (2.99x linear), with an sRGB RMSE of 0.005 over a 0.01-10 input range. Other
   effects do not map by field name; they need visual fitting.
5. The real shipping gaps are platform reach (s&box standalone is Windows-only, preview, and
   approval-gated), the Unity asset/package ecosystem, Addressables-style live-ops content, UI
   Toolkit, VFX Graph, Cinemachine, and HDR display output. Rendering capability itself is closer
   than expected: s&box already has SSR, GTAO-style SSAO, decals, terrain, Shader Graph, Recast
   navmesh, Source 2 physics, FSR3/DLSS, and dedicated servers.

## 2. Two routes compared

| Route | What happens | Main cost | When to use |
|---|---|---|---|
| HDRP -> s&box direct | `unityimporterplus` reads HDRP materials, lights, Volumes, probes, decals and PPV from the package; unsupported features are reported. | HDRP-only features still need manual replacement (area lights, transmission/subsurface, volumetric fog, ray tracing). | Default route. Shortest path to a playable s&box scene. |
| HDRP -> URP -> s&box | Rebuild the project in URP, then port URP assets and effects to s&box. | Two rewrites. Unity states HDRP-to-URP requires rewriting most of the project. URP loses HDRP features that s&box may have been able to approximate directly. | Only for look-dev or if the URP project is the actual ship target. Not for s&box. |
| HDRP -> URP look-dev -> s&box | Keep the HDRP project as the source of truth; build URP calibration scenes only to compare tonemapping and post-stack behavior against s&box. | Low; no asset migration. | When a specific look must be matched and a URP reference is useful. |

Unity's migration documentation is explicit: "To migrate from HDRP to URP, you must rewrite most of
your project." It also lists manual shader/material conversion, VFX Graph conversion and lighting
conversion as separate tasks. Sources:

- https://docs.unity3d.com/6000.6/Documentation/Manual/urp/migrating-from-hdrp.html
- https://docs.unity3d.com/6000.6/Documentation/Manual/urp/migrating-from-hdrp-workflow.html
- https://docs.unity3d.com/6000.6/Documentation/Manual/urp/convert-hdrp-shaders-to-urp.html

The local HDRP package is 17.3.0:
`C:/cseliot/1-Development/4-Unity/2-Projects/.old_school_model_Library_lfs_broken_2026-11-20/PackageCache/com.unity.render-pipelines.high-definition@2d37db618c5e/`.

SCOPE NOTE (2026-11-26 23:17): this `17.3.0` is the local **HDRP** `old_school_model` fixture, which intentionally remains on `6000.3.29f1`; it is NOT a dangling URP 17.3 cache site. The Director's 22:27 1A re-pin (`6000.7.0b10 (4f3c1c5c4346)` / URP 17.7.0, caches @e9044f1f1f57 / @7aa87ac5532f / @767fc04a69c2) applies to the SyntyForest URP fixture and acceptance bars only. BIRP stays ignored: fact finder found no BIRP asset, shader, material, RP asset or setting necessary for SyntyForest to import; residual evidence retained for audit (not an import input): 22/24 `.shadergraph` carry an active `BuiltInTarget`, 125 `.mat` retain `_BUILTIN_*` properties, and 2 graphs reference BIRP-capable ShaderGUI scripts.

## 3. What the importer already covers

From the verified HDRP census in the importer's HDRP notes (tasks 06-10 committed; task 11 open and
uncommitted):

| Area | Covered | Gaps / reports |
|---|---|---|
| HDRP materials | Detection of Lit, Unlit, LitTessellation, Decal and TerrainLit; base color, mask map, normal, emissive maps; alpha test, double-sided, transparent surface; metallic/smoothness remaps; HDR emissive tint and brightness. | Transmission and subsurface are reported, not converted. Detail maps are ignored. Glass routes to `glass.shader`. |
| HDRP lights | Lumen -> candela, punctual Lux -> candela using `m_LuxAtDistance`, directional Lux as-is, candela direct, EV100 report-only; dimmer, inner spot cone, CCT. | Area lights (rectangle/disc/tube) are not convertible; s&box has no matching light shape. |
| HDRP environment | Volume profiles: sky type (HDRI/procedural/gradient/physical), fog (exponential/volumetric), ambient; one `SkyBox2D`, one `AmbientLight` only for a concrete gradient, one `Sandbox.Fog`. | Local volumes and partial weights are report-only. HDRP volumetric fog/clouds have no true s&box equivalent. |
| Probes / decals | `EnvmapProbe` from Unity ReflectionProbe + `HDAdditionalReflectionData`; decals; planar probes report-only. | Task 11 is not shipped until its status is DONE and committed. |
| Volume post-processing | Tonemapping, Bloom, DepthOfField, AmbientOcclusion mappings. | Other overrides are reported. Local volumes stay report-only. |

Important current-source correction: the Dev checkout's `Light.cs` now has `Intensity` (0..10) and
`Temperature` (Kelvin) properties (`engine/Sandbox.Engine/Scene/Components/Light/Light.cs:161,183`).
The older importer rule that bakes Unity intensity into an HDR `LightColor` is therefore stale for
this checkout. Re-verify the light conversion path before treating the bake-into-LightColor rule as
the only option.

## 4. Post-processing parity matrix

Unity side means URP 17.x/HDRP 17.x volume overrides. s&box side means the current Dev checkout
`engine/Sandbox.Engine/Scene/Components/PostProcessing/Effects/`. Status: MAPPED = direct field
mapping; APPROX = needs visual fitting or has different units/order; MISSING = no component.

| Unity effect | s&box component | Status | Porting note |
|---|---|---|---|
| Tonemapping None | Linear + auto exposure off | APPROX | s&box has no None mode; Linear applies exposure only. |
| Tonemapping Neutral | HableFilmic + about +1.58 EV | MAPPED (measured) | Same Hable/Hejl family; sRGB RMSE 0.005 with exposure fit. |
| Tonemapping ACES | ACES | APPROX | Same name, different implementation; A/B required. |
| Tonemapping AgX (6.7) | AgX | APPROX | s&box uses a Punchy AgX look; URP 6.7 exposes AgX contrast and mid-grey. A/B required. |
| Tonemapping Custom / External (HDRP) | None / ColorGrading LUT | MISSING / APPROX | HDRP custom curve has no URP or s&box equivalent; external LUT can go through ColorGrading LUT if the format matches. |
| Exposure (HDRP modes: Fixed, Automatic, AutomaticHistogram, CurveMapping, PhysicalCamera) | Tonemapping auto-exposure group; CameraComponent.AutoExposure | APPROX | URP 6.7 has fixed only. s&box has auto inside Tonemapping. URP fixedExposure is EV and computes 2^-EV; s&box ExposureCompensation computes 2^+EV, so invert the sign. |
| ColorAdjustments | ColorAdjustments + ColorGrading | APPROX | s&box has no colorFilter; saturation/contrast neutral at 1 vs Unity 0; s&box runs after tonemapping, Unity mostly before. |
| Bloom | Bloom | APPROX | s&box has Strength/Threshold/Tint; no scatter, clamp, lens dirt, filter or downscale controls. Threshold ranges and remap differ. |
| Vignette | Vignette | APPROX | s&box defaults Intensity 1 vs Unity 0; Roundness is a float vs Unity's rounded bool. |
| FilmGrain | FilmGrain | APPROX | No grain presets and no custom texture in s&box. |
| ChromaticAberration | ChromaticAberration | APPROX | s&box runs after tonemapping; Unity runs before. s&box exposes per-channel offset; Unity has one intensity. |
| LensDistortion | MISSING | MISSING | No s&box component. Port the math into a new BasePostProcess. |
| PaniniProjection | MISSING | MISSING | No s&box component. |
| DepthOfField | DepthOfField | APPROX | Different model: s&box FocalDistance/FocusRange/BlurSize vs Unity focusDistance/aperture/focalLength/mode. s&box adds FrontBlur/BackBlur. |
| MotionBlur | MotionBlur | APPROX | s&box is camera-velocity only; no clamp or object mode. |
| SSAO | AmbientOcclusion | APPROX | s&box is GTAO with a world-unit radius; Unity radius is screen/unit-based with a much larger parameter set. |
| SSR | Renderer-level SSR | MAPPED | s&box SSR is core; URP 6.7 adds it as a renderer feature. The `Sandbox.ScreenSpaceReflections` component is obsolete and inert. |
| Fog | Fog | APPROX | s&box Fog supports distance/height/sun; HDRP volumetric fog/clouds have no direct equivalent. |
| ColorCurves | ColorGrading curves | APPROX | s&box caps curves at 4 frames; no master curve; different defaults. |
| ColorLookup | ColorGrading LUT | APPROX | LUT layout/format needs verification; s&box uses a Texture, Unity uses a strict 2D strip. |
| LiftGammaGain | MISSING | MISSING | No s&box component. |
| SplitToning | MISSING | MISSING | No s&box component. |
| WhiteBalance | ColorGrading TemperatureControl | APPROX | Temperature only; no tint. |
| ChannelMixer | MISSING | MISSING | No s&box component. |
| ShadowsMidtonesHighlights | MISSING | MISSING | No s&box component. |
| ScreenSpaceLensFlare | MISSING | MISSING | No s&box component. |
| Decals | Decal component | MAPPED | Both are scene/geometry features, not post-process volumes. |

Pipeline-order facts that change the result:

- URP applies ChromaticAberration and Vignette before tonemapping; s&box applies
  ChromaticAberration after tonemapping and Vignette before.
- URP ColorAdjustments runs before tonemapping for post-exposure and after for grading; s&box
  ColorAdjustments runs after tonemapping (`Stage.AfterPostProcess`, order 3000).
- s&box ColorGrading runs after tonemapping, order 4000.
- URP Bloom, MotionBlur and DoF run before tonemapping in the final post pass; s&box Bloom is
  `Stage.BeforePostProcess`, order 100, MotionBlur order 50, DoF `Stage.AfterTransparent`, order 100.

## 5. Feature gaps that matter for a port

Ranked by practical impact for a small 3D game. This is the "s&box does not have it" list.

| Rank | Gap | s&box status | Porting action |
|---|---|---|---|
| 1 | Platform reach | Windows PC only; standalone export is preview, Valve/Facepunch approved, Steam-first. No mobile, WebGL, macOS/Linux client or console. | If the target is not Windows PC, stop; use Unity. |
| 2 | Asset Store / UPM ecosystem | s&box cloud library and packages are much smaller; Unity assets cannot be used directly. | Budget replacement or conversion for every purchased asset. |
| 3 | Addressables / live-ops content | Cloud assets and mounts exist, but no catalogs, remote content builds or refcounted loading. | Keep the first release self-contained; add cloud content only where s&box supports it. |
| 4 | UI Toolkit / UXML / UI Builder | Razor + Painter + Panel UI. | Re-author UI; do not plan on importing UXML/USS. |
| 5 | VFX Graph GPU particles | CPU-only programmable particles. | Rebuild dense GPU effects or reduce their scale. |
| 6 | Cinemachine | CameraComponent + ICameraModifier + shake/punch/tilt. | Hand-code rigs, dolly and ClearShot behavior. |
| 7 | HDR display output | Appears SDR-only; no HDR10/scRGB path found. | Do not plan HDR output; tone-map for SDR. |
| 8 | GI maturity | Indirect Light Volumes are Feedback Preview, editor-bake-only, documented thin-wall leaking. | Budget manual GI fix-up; use lightmaps/DDGI where they work. |
| 9 | Timeline / Playable API | Movie Maker uses a different asset and API. | Rebuild sequences; runtime `PlayableDirector` code has no direct equivalent. |
| 10 | Render Graph / custom pass API | Internal stage hooks and C# engine hooks, no graph debugger. | Only a problem for custom render passes; most games do not need it. |
| 11 | TAA / native motion vectors | CMAA2 + FSR3/DLSS temporal AA; no standalone TAA mode or public motion-vector pass. | Use an upscaler or accept CMAA2. |
| 12 | Light cookies / IES | Spot cookies only; no point cookies, no IES profiles. | Rebuild gobo effects; use spot lights or projected decals. |
| 13 | Reflection probe blending | EnvmapProbe supports baked/realtime/custom cubemaps, but no Unity-style probe-volume atlas blending. | Re-layout interiors or use HDRI/planar fallbacks. |
| 14 | HDRP-only lighting and volumetrics | Area lights are dropped; no volumetric fog/clouds, ray tracing, SSGI or planar reflection probe component. | Replace manually: spot/point lights, s&box Fog, screen-space GI addon, water/planar features where applicable. |

## 6. What s&box has that Unity does not (only where it matters)

These are not porting blockers. They are listed so effort is not spent adding them to Unity.

- AgX tonemapping. URP 6.3 does not have it; URP 6.7 does. s&box has it today.
- Automatic exposure. URP has none through 6.7 (fixed only); s&box has it inside Tonemapping.
- SSR and GTAO-style SSAO are core in s&box; URP adds them as renderer features in 6.7.
- C#/Razor hotload, built-in netcode + Steam relay, cloud assets, dedicated servers, Razor/Painter
  UI. These are integration and iteration advantages; Unity has equivalents via packages and
  services, usually with more setup.

## 7. Free libraries and shaders that fill s&box gaps

No library bridges URP Volumes to s&box post-process components. The useful sources are individual
effect implementations and portable shader math. Licenses matter: Unity Companion License (UCL)
code cannot be copied into s&box the way MIT code can.

| Source | License | Use |
|---|---|---|
| GarrettGunnell/Post-Processing | MIT | Portable HLSL math: Hable/ACES/Reinhard tonemapping, bloom, vignette, chromatic aberration, grain, fog, sharpen. |
| GarrettGunnell/AcerolaFX | MIT | ReShade HDR pipeline: auto exposure, filmic tonemapping, XeGTAO, bloom, bokeh, dither, CRT, grain, CA, vignette. |
| aki-null/CustomToneMappingURP | MIT | URP 17+ AgX/GT/ACES 2.0/LUT reference; useful for comparing URP AgX against s&box AgX. |
| Lepsima/Auto-Exposure-Unity-6-URP | MIT | URP auto-exposure reference; s&box already has auto exposure, so use for comparison only. |
| jiaozi158/UnitySSReflectionURP | MIT | URP SSR reference; s&box has SSR core. |
| JoshuaLim007/Unity-ScreenSpaceReflections-URP | MIT | Alternative URP SSR reference. |
| CristianQiu/Unity-URP-Volumetric-Light | MIT | URP volumetric fog/light reference; s&box has only distance/height fog. |
| meryuhi/URPFog | MIT | Simple URP fog reference. |
| sbox-ssgi (amir-al-mohamad) | MIT (root Facepunch MIT text) | s&box screen-space GI addon; closest free SSGI option. |
| Postprocessable Razor Panel (jammees) | MIT | s&box compute-shader panel effects; CRT warp is a lens-distortion analogue. |
| Unity Post Processing Stack v2 | UCL | Reference only; do not copy into s&box. |
| IllusionRP | UCL | Reference only; has auto exposure and advanced tonemapping, but UCL restrictions. |
| mseonKim/URP-ScreenSpaceReflection | UCL | Reference only. |

Missing s&box effects that are worth porting as new `BasePostProcess` components, in rough order of
value for a ported Unity look:

1. LensDistortion and PaniniProjection (simple fullscreen UV math).
2. ChannelMixer, SplitToning, LiftGammaGain, ShadowsMidtonesHighlights (LUT-style grading).
3. ScreenSpaceLensFlare (bloom-mip driven).
4. A proper volumetric fog path if the HDRP scene depends on it.

## 8. Recommended porting workflow

1. Inventory the HDRP project: materials, shaders, lights, Volume profiles, decals, probes, VFX,
   UI, Timeline, custom passes.
2. Run `unityimporterplus` on the package or Unity project folder. Keep the report; it names the
   unsupported features.
3. Fix materials in s&box: alpha test, double-sided, transparency, transmission/subsurface, HDR
   emission, metallic/smoothness remaps.
4. Convert lights: lumen/lux to s&box intensity, CCT, inner spot cone. Replace area lights with
   spot/point lights or emissive geometry. Re-verify the current `Light.Intensity` path.
5. Convert environment: Volume sky/fog/ambient to s&box SkyBox2D/AmbientLight/Fog. Local volumes
   stay report-only.
6. Convert post-processing with the matrix above. Start from s&box components disabled, then turn
   on one effect at a time. For tonemapping, use the measured Neutral -> HableFilmic + 1.58 EV
   mapping, or AgX -> AgX if the source uses AgX.
7. Build missing effects as new `BasePostProcess<T>` components with s&box `.shader` files. Port
   the math from MIT sources, not the Unity shader structure.
8. Build a parity harness: a grey ramp and HDR swatches, a PBR sphere grid, one-effect-at-a-time
   scenes, fixed camera and frozen exposure, 1920x1080 screenshots, ROI luminance tables, and a
   mapping table with MAPPED / APPROX / MISSING per row.
9. Verify in the editor last. Objective data errors (missing fields, wrong types, out-of-range
   values) should be fixed headlessly; only the perceptual look is an editor check.

## 9. What will not port cleanly

- HDRP Lit surface features: transmission, subsurface scattering, clearcoat, anisotropy,
  iridescence, tessellation, detail maps. The importer reports them; s&box complex shaders do not
  have equivalents.
- HDRP physical light units and area light shapes. Conversion is approximate and area lights are
  dropped.
- HDRP Exposure automatic/histogram/curve/physical-camera modes. s&box has a simpler auto exposure;
  URP 6.7 has fixed only.
- HDRP custom tonemapping curves and external LUTs. Use Neutral/AgX approximations or s&box
  ColorGrading LUT.
- HDRP volumetric fog/clouds, ray tracing, SSGI, planar reflection probes. Replace manually or
  accept the difference.
- URP Volume YAML, Shader Graph generated HLSL, URP `Core.hlsl`/`Lighting.hlsl` includes, SRP
  Batcher assumptions, and Unity materials. These are not portable source.
- Unity UI Toolkit UXML/USS, VFX Graph assets, Cinemachine rigs, Timeline assets, Addressables
  catalogs, and lightmaps/GI data.

## 10. Open questions

- Does the current `Light.Intensity` path make the importer's HDRP light conversion obsolete? The
  source now has Intensity and Temperature; the HDRP notes still describe baking into LightColor.
- Task 11 (probes/decals/volume post-processing) is open. Do not treat it as shipped until its
  status and commit are verified.
- URP 6.7 AgX vs s&box AgX has not been compared numerically or visually.
- URP 6.7 `ExposureVolume` sign has been read from source (`2^-EV`), but the full rendered A/B
  against s&box compensation has not been captured.
- LUT format compatibility between Unity ColorLookup and s&box ColorGrading is unverified.

## 11. Sources

Unity:

- HDRP to URP migration: https://docs.unity3d.com/6000.6/Documentation/Manual/urp/migrating-from-hdrp.html
- HDRP to URP workflow: https://docs.unity3d.com/6000.6/Documentation/Manual/urp/migrating-from-hdrp-workflow.html
- HDRP shader conversion: https://docs.unity3d.com/6000.6/Documentation/Manual/urp/convert-hdrp-shaders-to-urp.html
- URP 6.3 feature comparison: https://docs.unity3d.com/6000.3/Documentation/Manual/render-pipelines-feature-comparison.html
- URP 6.7 tonemapping: https://docs.unity3d.com/6000.7/Documentation/Manual/urp/post-processing-tonemapping.html
- URP 6.7 HDR output: https://docs.unity3d.com/6000.3/Documentation/Manual/urp/post-processing/hdr-output.html

Local Unity source:

- URP 17.6 (6.6 project): `C:/cseliot/1-Development/4-Unity/2-Projects/deleteme9999/Library/PackageCache/com.unity.render-pipelines.universal@2a6014b851d8/`
- URP 17.7 (6.7 beta editor): `D:/Applications/Unity/6000.7.0b10/Editor/Data/Resources/PackageManager/BuiltInPackages/com.unity.render-pipelines.universal/`
- HDRP 17.3: `C:/cseliot/1-Development/4-Unity/2-Projects/.old_school_model_Library_lfs_broken_2026-11-20/PackageCache/com.unity.render-pipelines.high-definition@2d37db618c5e/`

s&box source:

- Post-process effects: `engine/Sandbox.Engine/Scene/Components/PostProcessing/Effects/`
- Stage order: `engine/Sandbox.Engine/Systems/Render/Stage.cs`
- Tonemapping: `engine/Sandbox.Engine/Scene/Components/PostProcessing/Effects/Tonemapping.cs`
- Light: `engine/Sandbox.Engine/Scene/Components/Light/Light.cs`
- Planar reflections: `engine/Sandbox.Engine/Systems/Render/PlanarReflections.cs`

Repository paths are relative to the s&box Dev checkout:
`C:/cseliot/1-Development/1-SBox/1-Engine-Builds/Dev/sbox-public/`.
