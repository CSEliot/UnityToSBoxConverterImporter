# HDRP support: research and task plan

Scope: `{cseliot}/1-Development/1-SBox/2-Projects/unityimporterplus` against the HDRP project
`C:/cseliot/1-Development/4-Unity/2-Projects/old_school_model` (HDRP 17.3.0, Unity 6) and its
export `Testing/old_school_project.unitypackage` (1.2 GB, 1,277 assets).

Research artifacts (scratch, not committed):
- `C:/Users/Eliot/AppData/Local/hermes/profiles/hermes-sbox/cache/scratch/hdrp/research-materials.md`
  (full 98-material census, citations)
- `.../cache/scratch/hdrp/research-scenes.md` (scene census and current-output diagnosis)
- `.../cache/scratch/hdrp/research-sbox.md` (HDRP to s&box capability mapping)
- `.../cache/scratch/hdrp/report.json` and `.../hdrp/out/` (fresh current-HEAD harness run)
- `.../cache/scratch/hdrp/source_census.json` (scene component census)

## 1. What is actually in this HDRP package

- 98 `.mat`, all 98 using HDRP Lit, shader guid `6e4ae4064600d784cac1e41a9e6f2e59`
  (`Library/PackageCache/com.unity.render-pipelines.high-definition@2d37db618c5e/Runtime/Material/Lit/Lit.shader`).
- The `.unitypackage` contains **0 `.shader` entries**. HDRP shaders live in `Packages/`, which Unity
  does not export, so the importer can never resolve the shader source from the archive.
- Materials carry HDRP properties and `m_ValidKeywords`: 94 base-colour maps, 95 mask maps, 95 normal
  maps, 7 emissive maps, 0 detail maps; 14 alpha-test, 11 double-sided, 1 transparent, 10 transmission,
  9 subsurface, 4 non-black emissive colours.
- MergeV2: 250 GameObjects, 2,861 prefab instances, 119 lights (2 directional, 63 spot, 9 point,
  45 rectangle/area), 6 cameras, 44 reflection probes, 2 global Volumes, 2 StaticLightingSky,
  2 light-probe groups, 166 prefabs. Test_Range: 11 GameObjects, 201 instances, 1 light, 1 camera,
  1 probe, 1 Volume.
- HDRP moves scene-wide lighting out of `RenderSettings` into Volume profile assets:
  `m_SkyboxMaterial: {fileID: 0}`, `m_AmbientMode: 0`, `m_Fog: 0`; `VolumeSettings.asset` selects a
  PhysicallyBased sky and exponential fog; `Scene Settings Profile MergeV2.asset` holds Exposure and
  related overrides; light physical units live in `HDAdditionalLightData`, not the Unity Light.

## 2. Current importer, measured on the current tree

Fresh run (commit `1657ed6` plus the Director's archive moves; harness `--scenes`, exit 1 because 81
missing-texture warnings remain):

| package | assets | converted | files | warnings | unresolved | errors |
|---|---|---|---|---|---|---|
| old_school | 1,277 | 936 | 2,110 | 97 | 81 | 0 |

Scene conversions: MergeV2 roots 4, instances 2,861, skipped 0, lost 0; Test_Range roots 3,
instances 201, skipped 0, lost 0. Generated MergeV2 has 3,111 nodes (250 plain GameObjects + 2,861
`__Prefab` stubs). Components converted: 63 SpotLight, 9 PointLight, 2 DirectionalLight, 6
CameraComponent, 6 AudioListener, 4 BoxCollider. Test_Range: 2 ModelRenderer, 2 ModelCollider, 1
DirectionalLight, 1 Camera, 1 AudioListener.

That is already far better than the pre-task-01 output (4 empty roots, no prefabs), but the HDRP
result is still structurally wrong:

- **No sky, ambient or fog.** Source RenderSettings have no skybox material, no ambient mode and fog
  off; HDRP puts all three in Volumes. `UnitySceneExport.cs:255/271/281` therefore skips all three
  silently. The generated scene has no `SkyBox2D`, `AmbientLight` or fog object.
- **Light units are wrong.** The converter reads Unity `m_Intensity` and ignores
  `HDAdditionalLightData`. A 20,000-lux sun becomes `LightColor 19573,11926,6488`; 90 lumen lights are
  converted at 200/4π with no unit table; 45 area lights are dropped with one warning.
- **Probes, Volumes and StaticLightingSky are not converted.** 44 reflection probes become generic
  "ReflectionProbe not converted" lines; 1 planar probe, 2 Volumes and 2 StaticLightingSky are lost;
  no `EnvmapProbe` or `PostProcessVolume` is emitted.
- **Cameras.** All 6 MergeV2 cameras have `IsMainCamera: false` because the main camera is a camera
  component inside a prefab instance; `MainCamera()` only scans non-stripped scene blocks
  (`UnityPrefabExport.cs:879-894`). HDRP background/clear mode comes from `HDAdditionalCameraData`;
  exposure lives in the Volume and is not read.
- **Materials are partly converted, silently partially wrong.** The generated vmats have 0
  `F_ALPHA_TEST`, 0 `F_RENDER_BACKFACES` and 0 `F_TRANSLUCENT`; 14 alpha-test, 11 double-sided and
  1 transparent material render solid/one-sided/opaque. Metallic and smoothness remaps are ignored in
  10 and 3 materials; HDR emissive colours are flattened to white in 3. Mask-map channel splitting,
  base colour, normal maps and emissive masks already work.
- `SceneConversions` and the warning stream report the prefab/missing-texture state but not HDRP
  semantics; there is no "HDRP detected" statement and no per-material explanation.

## 3. Gap inventory

| Area | Source count | Current behaviour | Needed |
|---|---|---|---|
| HDRP detection | 98 Lit materials, 7 script guids | none; HDRP scripts named "MonoBehaviour (script)" | built-in shader/script tables, one report line |
| Alpha test | 14 | 0 converted | `_AlphaCutoffEnable` / `_ALPHATEST_ON`, `_AlphaCutoff` |
| Double-sided | 11 | 0 converted | `_DoubleSidedEnable` / `_DOUBLESIDED_ON` / `_CullMode 0` |
| Transparency | 1 (Glass) | opaque | `_SurfaceType 1` / `_SURFACE_TYPE_TRANSPARENT`, opacity channel, glass route |
| Metallic remap | 10 (8 visible; Plane/Glass no bound mask) | 8 export raw mask R; Plane/Glass already 0 | `_MetallicRemapMin/Max` |
| Smoothness remap | 3 (float-noise values) | raw mask A; effect below 8-bit | `_SmoothnessRemapMin/Max` |
| Emission HDR/tint | 3 of 7 maps | white tint, brightness 0 | `_EmissiveColor`, `_EmissiveIntensity` |
| Transmission / SSS | 10 / 9 | silently flattened | report-only unless a complex approximation is chosen |
| Sky | 2 volumes, PhysicallyBased | none | default/tinted sky + explicit approximation report; HDRI sky only if its texture key is verified |
| Fog | exponential in volume | none; `GradientFog` is obsolete | `Sandbox.Fog` from Volume Fog/ExponentialFog |
| Ambient | volume/sky | none | `AmbientLight` or report |
| Lights | 119 (45 area) | legacy intensity; area dropped | unit table, CCT, dimmer, area report |
| Cameras | 6, main camera in prefab | all false, no exposure | main-camera resolution, HDRP background, exposure/report |
| Reflection probes | 44 | reported, not converted | `Sandbox.EnvmapProbe` |
| Planar probe | 1 | reported | report-only |
| Volumes / post-processing | 2 global | reported as script | `PostProcessVolume` + supported effects, report the rest |
| StaticLightingSky | 2 | reported | report or use its profile for sky |
| Decal projectors | 0 in scenes; 98 decal-capable mats | not handled | report; `Sandbox.Decal` if a projector appears |
| Light layers | HD data | not read | report-only |

s&box constraints verified against the Dev engine source (`1-Engine-Builds/Dev/sbox-public` @
`fa2af44b9`) and the shipped `Sandbox.Engine.dll`:
- `Sandbox.Light.Intensity` is a 0-10-ish brightness multiplier on the tint, not a physical unit
  (`Light.cs:143-200`); there is no lumen/lux/candela/EV system. Any HDRP unit conversion is an
  approximation and must be documented.
- `Sandbox.EnvmapProbe` exists with bounds, resolution, priority, tint, feathering, near/far,
  max distance and multi-bounce; it can stand in for a Unity reflection probe.
- `Sandbox.PostProcessVolume` exists, with effect components (`Bloom`, `Tonemapping`,
  `ColorGrading`, `AmbientOcclusion`, `DepthOfField`) parented under it. HDRP volume override graphs
  do not map one-to-one; unsupported overrides must be reported.
- `Sandbox.SkyBox2D` exists; its `SkyMaterial` is a vmat. HDRI sky texture mapping is unconfirmed
  (`g_tSkyTexture` is read by `SkyBox2D.SkyTexture` but absent from the shipped shader strings); a
  PhysicallyBased sky has no texture at all. Do not promise procedural sky parity.
- `Sandbox.Fog` replaces the obsolete `GradientFog`; the current exporter still writes
  `Sandbox.GradientFog` (`UnitySceneExport.cs:290`).
- No rectangular area light; no public planar reflection component (only `WaterBody`); light layers
  have no light-side mask. These are report-only.

## 4. Task backlog

Execution order is 06 → 07 → 08 → 09 → 10 → 11; 07/08 are independent material edits, 09/10/11 are
scene edits and can be reordered after 06.

- **06 HDRP Detection and Fixture** — built-in shader/script tables, honest "HDRP detected" report,
  and a small hand-written HDRP fixture that later tasks extend. No conversion behaviour yet.
- **07 HDRP Lit Surface Semantics** — alpha test, double-sided, transparency; the 14/11/1 material
  groups.
- **08 HDRP Lit PBR and Emission Fidelity** — metallic/smoothness remaps, normal scale, HDR emissive
  tint/brightness; report transmission/SSS/decals.
- **09 HDRP Environment from Volumes** — sky, ambient and fog from Volume profiles; replace the
  obsolete GradientFog; explicit approximation report.
- **10 HDRP Lights and Cameras** — physical-unit table, CCT, dimmer, area-light reporting, main-camera
  resolution, HDRP background and exposure reporting.
- **11 HDRP Probes, Decals and Volume Post-Processing** — reflection probes to `EnvmapProbe`,
  global Volumes to `PostProcessVolume` with the effects that map, planar/static-sky/decal/light-layer
  reporting.

Every task keeps `building` and `city--1` output byte-identical and uses the HDRP fixture for fast
gates; the full old_school run is the real-world gate. Editor appearance cannot be judged headless and
stays an explicit follow-up.

## 5. Risks and non-goals

- Physical light units cannot be reproduced; the task must choose a documented, testable approach and
  report the approximation, not claim parity.
- PhysicallyBased/Gradient sky has no s&box equivalent. Default/tinted sky plus a warning is the
  honest ceiling; HDRI sky is a stretch item pending an editor round-trip of the sky texture key.
- HDRP transmission, subsurface, light layers, planar reflections, area lights, ray tracing and most
  Volume overrides have no direct counterpart. They are reported, not faked.
- The importer cannot read `Packages/` shader source. Detection must be property/keyword and
  known-guid driven.
- old_school has 81 pre-existing missing-texture warnings and exits 1; HDRP tasks must not hide or
  reclassify them.