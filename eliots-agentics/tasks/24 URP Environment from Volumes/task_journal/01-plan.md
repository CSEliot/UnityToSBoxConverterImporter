# Task 24 - URP Environment from Volumes - Plan (round 1, QUESTION-ONLY)

Status: Premise 1 ANSWERED. Premises 2-4 being resolved on disk; this file is rewritten after each answered premise so a turn cap leaves finished answers on disk.

## Scope held this turn
- QUESTION-ONLY. No Editor/Core/*.cs edits, no git add/commit/push, no editor/browser/sbox process, no new tooling, no .humans-only access, no role/skill/memory/config edits.
- Serialization: round 1 Core owner is task 23. Task 24 is question-only; ownership/hooks/verification pinned below only, not exercised.

## Ownership / hooks / verification (to be pinned as premises resolve)
- (a) exact Editor/Core file ownership: PENDING
- (b) required hooks and call sites: PENDING
- (c) headless-vs-rendered verification split: PENDING

## Premise answers
1. ANSWERED. Global Volume script guid `172515602e62fb746b5d573b38a5fe58` resolves to
   `UnityEngine.Rendering.Volume` (the shared Core `Volume : MonoBehaviour`), NOT a URP-specific
   class and NOT a "GlobalVolume" type.
   - `Library/PackageCache/com.unity.render-pipelines.core@7aa87ac5532f/Runtime/Volume/Volume.cs.meta:2`
     -> `guid: 172515602e62fb746b5d573b38a5fe58`.
   - `.../Runtime/Volume/Volume.cs:5` `namespace UnityEngine.Rendering`; `:15`
     `public class Volume : MonoBehaviour`.
   - Discriminator fields grounded in the same file: `:17-18` `m_IsGlobal` (formerly `isGlobal`),
     `:38` `priority`, `:45` `blendDistance`, `:51` `weight`, `:63` `sharedProfile`.
   - Consequence: detection keys on this guid + `m_IsGlobal: 1` + GameObject name
     `Global Volume`; "Global Volume" is editor default naming of a generic Core Volume. The
     same guid is shared with HDRP Volumes, so the `Pipeline == Urp` gate + scene script-guid
     census (not the guid alone) must discriminate.
2. ANSWERED (with a fixture-drift report). Both scene Volumes reference exactly one profile each,
   chosen by `sharedProfile` guid (NOT by any root `settings:` list):
   - `SampleScene.unity:410` `sharedProfile: {fileID: 11400000, guid: 10fc4df2da32a41aaa32d77bc913491c, type: 2}`
     -> `Assets/Settings/SampleSceneProfile.asset` (`.meta:2` confirms guid).
   - `Demo.unity:371232` `sharedProfile: {fileID: 11400000, guid: 7679a2a2468dc7742a7ece2dea61ab3b, type: 2}`
     -> `.../PNB_Meadow_Forest/Scene/Demo/Global Volume Profile.asset` (`.meta:2` confirms guid).
   Every `m_OverrideState` in those two profiles (owner/field/line/state/value), full enumeration:
   - SampleSceneProfile (53 overrides): Bloom `:17 skipIterations=1:0, :20 threshold=1:1, :23 intensity=1:0.25,
     :26 scatter=1:0.5, :29 clamp=0:65472, :32 tint=0, :35 highQualityFiltering=1:1, :38 filter=0:0,
     :41 downscale=0:0, :44 maxIterations=0:6, :47 dirtTexture=0, :51 dirtIntensity=0:0`;
     Vignette `:67 color=0, :70 center=0, :73 intensity=1:0.2, :76 smoothness=0:0.2, :79 rounded=0:0`;
     Tonemapping `:113 mode=1:1 (Neutral, NOT AgX - PLAN DEFECT corrected 23:17; see P4 below), :116 neutralHDRRangeReductionMode=0:2, :119 acesPreset=0:3,
     :122 agxContrast=0:1.193, :125 agxMidGrey=0:0.18, :128 hueShiftAmount=0:0, :131 detectPaperWhite=1:0,
     :134 paperWhite=1:234, :137 detectBrightnessLimits=1:1, :140 minNits=1:0.005, :143 maxNits=1:647`;
     ScreenSpaceAmbientOcclusionVolumeOverride (`:155-156` class `UnityEngine.Rendering.Universal.ScreenSpaceAmbientOcclusionVolumeOverride`)
     `:159 m_Mode=1:0, :162 m_Quality=1:3, :165 m_Intensity=1:0.4, :168 m_Radius=1:0.3,
     :171 m_DirectLightingStrength=1:0.25, :174 m_FalloffDistance=1:100, :177 m_SampleCount=1:1,
     :180 m_Downsample=1:0, :183 m_AfterOpaque=1:0, :186 m_BlurQuality=1:2, :189 m_Method=1:0,
     :192 m_DepthSource=1:1, :195 m_NormalQuality=1:1, :198 m_MinimumRadiusInPixels=1:40,
     :201 m_UseComputeShader=1:0, :204 m_SpatialFilter=1:0, :207 m_DepthSampling=1:0,
     :210 m_TemporalFilter=1:0, :213 m_GhostingMitigation=1:0.5, :216 m_HistoryLength=1:0.9,
     :219 m_DirectionCount=1:2, :222 m_StepCount=1:4`;
     MotionBlur (component `active: 0` at `:236`) `:238 mode=0:0, :241 quality=1:2, :244 intensity=1:0.6, :247 clamp=0:0.05`.
   - Global Volume Profile (Demo, 28 overrides): LiftGammaGain `:17 lift=1, :20 gamma=1, :23 gain=1`;
     Bloom `:39 skipIterations=0:1, :42 threshold=1:0.81, :45 intensity=1:2.19, :48 scatter=0:0.7,
     :51 clamp=0:65472, :54 tint=0, :57 highQualityFiltering=0:0, :60 downscale=0:0, :63 maxIterations=0:6,
     :66 dirtTexture=1 (guid c1031a60a1e38c74b8dc72da43828163), :70 dirtIntensity=1:8.07`;
     WhiteBalance `:86 temperature=1:7.4, :89 tint=0:0`;
     ColorAdjustments `:123 postExposure=0:0, :126 contrast=1:6.2, :129 colorFilter=0, :132 hueShift=0:0,
     :135 saturation=1:7`;
     ShadowsMidtonesHighlights `:151 shadows=1, :154 midtones=1, :157 highlights=1, :160 shadowsStart=0:0,
     :163 shadowsEnd=0:0.3, :166 highlightsStart=0:0.55, :169 highlightsEnd=0:1`.
   - Default profile: YES, active as the URP *global default* stack, not as a scene Volume.
     `Assets/Settings/DefaultVolumeProfile.asset.meta:2` guid `ab09877e2e707104187f6f83e2f62510`;
     LINE CITATION RE-DERIVED 2026-11-26 23:17 against the on-disk 17.7.0 fixture:
     `Assets/Settings/UniversalRenderPipelineGlobalSettings.asset:191` verbatim
     `      type: {class: URPDefaultVolumeProfileSettings, ns: UnityEngine.Rendering.Universal, asm: Unity.RenderPipelines.Universal.Runtime}`
     and `:194` verbatim
     `        m_VolumeProfile: {fileID: 11400000, guid: ab09877e2e707104187f6f83e2f62510, type: 2}`.
     The old `:190-194` citation is superseded; `:191`/`:194` is the current primary source.
     A `:212`/`:215` citation reflects an EARLIER file state - the asset was rewritten at 22:59:25
     (verified mtime) after the parent's 22:49 check, so line numbers drifted -21. Both were
     correct for their respective states; this is drift, not a wrong measurement (parent 23:37).
     `ProjectSettings/GraphicsSettings.asset:73` registers that GlobalSettings guid `18dc0cd2c080841dea60987a38ce93fa`
     for `UniversalRenderPipeline`; URP source `UniversalRenderPipeline.cs:444-445` passes
     `defaultVolumeProfileSettings.volumeProfile` into `VolumeManager.instance.Initialize(...)`.
     It has 209 `m_OverrideState: 1` and 0 `: 0` (parent-verified, re-counted 23:17), so it is not inert. No scene MonoBehaviour references it;
     the two root-`settings:` profiles `PPP_Base_01.asset` / `Meadows_Post_Processing_01.asset` are unreferenced
     by any Volume and stay report-only.
   - REPORTED PREMISE FAILURE (fixture drift, not re-asked): the on-disk fixture is
     `6000.7.0b10 (4f3c1c5c4346)` / URP `17.7.0` (`ProjectVersion.txt:1-2`, `Packages/manifest.json:12`),
     cache `com.unity.render-pipelines.universal@e9044f1f1f57` / `com.unity.render-pipelines.core@7aa87ac5532f`.
     The pinned `6000.3.29f1 (96d2fe17529c)` / URP `17.3.0` / cache `@04369b574bc4` is NOT on disk.
     URP 17.x target decision is unchanged, but every path/guid/citation must be re-grounded against the
     current cache; research-doc sub-asset counts (5/6/29) are stale vs current on-disk 6/6/33
     (`grep -c '^--- !u!114 &'` over DefaultVolumeProfile / SampleSceneProfile / Global Volume Profile,
     re-counted 23:17).
3. ANSWERED. A NON-ORIGIN Global Volume exists, so acceptance #1 can be exercised without a new
   fixture package:
   - Demo (`Assets/Synty/PolygonNatureBiomes/PNB_Meadow_Forest/Scene/Demo.unity`): GameObject
     `&785097868` named `Global Volume` (`:371200-371215`), Volume MonoBehaviour `&785097869`
     (`:371216-371232`, `m_GameObject: {fileID: 785097868}`), Transform `&785097870`
     (`:371233-371247`, `m_GameObject: {fileID: 785097868}`). Exact
     `m_LocalPosition: {x: 61.38825, y: -10.131328, z: 41.147015}` at `:371242`;
     `m_LocalRotation: {x: 0, y: 0, z: 0, w: 1}`, `m_LocalScale: {x: 1, y: 1, z: 1}`,
     `m_Father: {fileID: 0}`.
   - SampleScene (`Assets/Scenes/SampleScene.unity`): GameObject `&832575517` (`:379-393`),
     Volume MonoBehaviour `&832575518` (`:394-410`, `m_GameObject: {fileID: 832575517}`),
     Transform `&832575519` (`:411-425`), `m_LocalPosition: {x: 0, y: 0, z: 0}` at `:420` —
     AT ORIGIN. Both cases therefore exist; assert BOTH, using the Demo numbers for the non-origin case.
   - Line-number drift: task.md cites `Demo.unity:371362`/`:371369`; current on-disk guid is
     `:371225` and `sharedProfile` `:371232` (uniform -137 shift). Re-derive citations, never copy them.
4. Transform source / Type Infinite / nested effects / local report-only / override->engine-class map: PENDING

### MASTER TRANSCRIPTION (2026-11-26 09:05) - SUPERSEDES the PENDING markers above
The worker hit its 40-call cap with P4 and the three pins only in its final answer. The Master transcribed them verbatim below. This file is a PLAN / HYPOTHESIS, not a gate.

P4: Transform source is the Volume MonoBehaviour `m_GameObject` fileID (Demo :371222 -> 785097868; SampleScene :400 -> 832575517), NOT the MonoBehaviour id; `c.Transforms` is keyed by GameObject fileID (UnityVolume.cs:279-281, UnitySceneExport.cs:465-474). Type Infinite: `SceneVolume.VolumeTypes.Infinite = 1000`, `VolumeComponent.SceneVolume`, `PostProcessVolume : VolumeComponent`; an emitted `SceneVolume Type Infinite` already exists at UnitySceneExport.cs:460. Effects on nested GameObjects via node `Children` + Effect helper (:477-495); local volumes report-only (UnityVolume.cs:258, :315-318); SSR, baked data and unmapped overrides report-only (UnitySceneExport.cs:70-79, :648). Override -> engine class: Bloom -> Sandbox.Bloom (Strength from intensity 0.25 / 2.19, Threshold from 1 / 0.81, Tint; scatter / highQualityFiltering / dirtTexture reported dropped), Vignette -> Sandbox.Vignette (Intensity 0.2), Tonemapping -> Sandbox.Tonemapping (PLAN DEFECT corrected in place 2026-11-26 23:17, NOT citation drift: mode 1 -> Neutral -> s&box HableFilmic +1.58 EV (`Documentation/unity_6_7_to_sbox.md:38`); AgX is mode 3 -> s&box AgX (`unity_6_7_to_sbox.md:40`). URP 17.7.0 `Library/PackageCache/com.unity.render-pipelines.universal@e9044f1f1f57/Runtime/Overrides/Tonemapping.cs:13/19/26/32` defines None=0, Neutral=1, ACES=2, AgX=3; `Assets/Settings/SampleSceneProfile.asset:112-114` reads `mode:`, `m_OverrideState: 1`, `m_Value: 1`, i.e. Neutral. The earlier "mode 1 -> AgX" mapping was wrong on every version and must not be implemented), MotionBlur -> Sandbox.MotionBlur ONLY after a units check (source intensity 0.6 has no same-unit Scale field, so report), SSAO VolumeOverride -> Sandbox.AmbientOcclusion only for m_Intensity 0.4 as a developer-tweakable NEW MAPPING (m_Mode / m_Quality / m_Radius are not same-unit and must be reported), ColorAdjustments -> Sandbox.ColorAdjustments (contrast 6.2, saturation 7; engine ranges differ, so a developer-tweakable property). LiftGammaGain, WhiteBalance and ShadowsMidtonesHighlights have NO engine effect class and stay report-only. All mapped values come from source, never camera-template defaults.

(a) Core file ownership when task 24 is granted a Core round: Editor/Core/UnityVolume.cs primary (URP override guid table, profile parse, m_OverrideState enumeration, VolumePost, AddPostNotes report lines), Editor/Core/UnitySceneExport.cs secondary (call site :335 and post block :446-504, CameraPostEffects :638-667 and :811), Editor/Core/UnityHdrp.cs only for the guid/pipeline gate (:112-124, :142-147), Editor/Core/UnityUrp.cs shared with tasks 21/25 for URP component guid constants. Do NOT change the HDRP Volume path, the single 39.37 ModelModifier, import_scale, or any Shader Graph conversion. Keep all files BOM-less LF; route new paths through UnityImport.DiskPath; expose every NEW MAPPING as a developer-tweakable property.

(b) Hooks: H1 UnitySceneExport.cs:335 `var environment = UnityVolume.Read(c.Blocks, byGuid)` - add a URP-only branch; the HDRP call and :387-390 HDRP fog report stay byte-identical. H2 UnitySceneExport.cs:446-504 post block: :456-461 PostProcessVolume component with SceneVolume Type Infinite already at :460, :465 Find by StableGuid(c.Key:post.ObjectId), :473 transform from c.Transforms.GetValueOrDefault(post.ObjectId), :477-479 nested effect children, :491-495 CameraPostEffects must NOT feed URP volumes, :501-503 source-vs-template report line and UnityVolume.AddPostNotes. H3 UnityVolume.cs:29-65 OverrideNames, :68 Consumed, :71 Mapped, :75-81 MappedFields, :85 Overridden, :93-115 VolumePost, :127-135 Profile/Override, :162-175 Parse, :178-187 OverrideName, :193-206 Value, :227-231 ProfileGuid, :239-443 Read, :448-491 Post, :495-511 Dropped, :516-534 AddPostNotes. H4 UnityHdrp.cs:112-124 ComponentTable (guid 1725... at :117), :142-143 PipelineOf, :146-147 Component, :158-170 Count; UnityImport.cs:50 ReadText, :744 DiskPath, :262-267 existing Urp material gate. H5 UnitySceneExport.cs:638-667 CameraPostEffects returns a fresh unparented List of JsonObject and is also called at :811 by Camera; grep every caller before changing any helper return shape; a JsonNode has exactly one parent.

(c) Verification split - HEADLESS (this task, no editor): rebuild harness from fixed sources `dotnet build Documentation/Unity-To-SBox-Automated/harness/harness.csproj -c Release -p:IUP_ROOT=C:/cseliot/1-Development/1-SBox/2-Projects/unityimporterplus_branch_2 --no-incremental` (0 errors); `verify_output.py` expecting `checked N files, 0 problems`; generated .scene JSON asserts Demo transform 61.38825 / -10.131328 / 41.147015 and source Bloom Strength 0.25 vs camera-template default 1; full 53 + 28 override enumeration and report lines for SSR / baked / unmapped; `building` and `city--1` byte-identical to a HEAD baseline in the SAME harness mode with per-code Unresolved breakdown unchanged. RENDERED (Director only, not this worker): perceptual look / size sign-off and screenshots under 4A - check no Director-owned editor is open on that project first, never close an editor this work did not launch, NTFY before each large round. No editor round in this plan phase.

VERSION RE-GROUND: the 6000.3.29f1 / 17.3.0 pin quoted in this plan is SUPERSEDED; see task.md `## Notes` (binding) and the Master dispatch contract AMENDMENT 09:05. Re-derive EVERY line citation on disk (uniform -137 drift in Demo.unity). HEAD is 0d31400.
