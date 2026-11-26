# Task 25 - URP Lights and Cameras - Plan (QUESTION-ONLY round 1)

Status: skeleton. Round 1 is PLAN + PREMISE RESOLUTION only. No Editor/Core edits,
no git add/commit/push, no editor process launch/close, no new tooling, no .humans-only.

## Scope
Decide, with engine-source and primary-source evidence, which fields of
UniversalAdditionalLightData / UniversalAdditionalCameraData map to s&box light and
camera components and which are report-only. Gate every new rule on Pipeline == Urp.

## Premise answers

### P1 - m_Type: 1 == Directional: ANSWERED (grounded)
Unity PRIMARY SOURCE: official UnityCsReference
`Runtime/Export/Graphics/GraphicsEnums.cs:69-73` declares
`public enum LightType { Spot = 0, Directional = 1, Point = 2, ... }`
(`Directional = 1` at :72); fetched verbatim from
`https://raw.githubusercontent.com/Unity-Technologies/UnityCsReference/master/Runtime/Export/Graphics/GraphicsEnums.cs`.
Local URP 17.x corroboration (same numbering):
`SyntyForest/Library/PackageCache/com.unity.render-pipelines.universal@e9044f1f1f57/ShaderLibrary/Shadows.hlsl:262`
comment: `z: light type (Spot = 0, Directional = 1, Point = 2, Area/Rectangle = 3, Disc = 4, ...)`.
Scene evidence: `SampleScene.unity:278-289` `Light:` block has `m_Type: 1`,
`m_Color {1,1,1,1}`, `m_Intensity: 2`, `m_Range: 10`, and GameObject name
`Directional Light` at :271. The nested `m_Type: 2` at :294 is `m_Shadows.m_Type`,
NOT the Light type.
PREMISE FAILURE (report, do not fabricate): on-disk fixture is now
`ProjectSettings/ProjectVersion.txt:1-2` = `6000.7.0b10 (4f3c1c5c4346)` and
`Packages/manifest.json:12` = `com.unity.render-pipelines.universal 17.7.0`,
cache `com.unity.render-pipelines.universal@e9044f1f1f57`. This differs from the
FALSIFIED dispatch pin (6000.3.29f1 / URP 17.3.0 / @04369b574bc4); the two guids under test
still resolve in the on-disk cache. Re-resolve guids against this cache.
SUPERSEDED (2026-11-26 22:27, Director 1A re-pin; appended 23:17): canonical Unity 6.7 is
`6000.7.0b10 (4f3c1c5c4346)` / URP 17.7.0 with caches `...@e9044f1f1f57`, `...@7aa87ac5532f`
and `...@767fc04a69c2` under FIXTURE/Library/PackageCache/. The old 17.3 hashes and
6000.3.29f1 / 96d2fe17529c do not exist anywhere under 4-Unity (parent-verified). The
dispatch pin above is historical, not binding.

### P2 - Every serialized field of the two URP sibling bodies: ANSWERED
FALSIFIED IN PLACE (2026-11-26 23:17): the old "5 light, 7 camera" census came from the
five-scene fixture. Primary source, Director (chat 22:55, verbatim): 'I deleted "Demo_02_BIRP.unity"
because it stands for "BUILT IN RENDER PIPELINE" and we're not supporing that.' BIRP is DROPPED
FROM SCOPE. Current four-scene fixture: parent-measured Light 4, Terrain 1, ReflectionProbe 0,
28 `.terrainlayer`; CAMERA COUNT PINNED by the read-only fact finder (Director 23:17 verbatim:
'Lean on the fact finder for the camera count in that instance.'): 7 `!u!20` Camera components over
the four scenes (4 enabled, 3 disabled), 6 with a UniversalAdditionalCameraData sibling; the 7th is
a plain camera at `PolygonGeneric/Scenes/Overview.unity:4277`. Use 7 cameras / 6 URP bodies, not 6
or 8. Source field
declarations are in the verified 17.x cache `@e9044f1f1f57`; engine counterparts cited from
`engine/Sandbox.Engine/Scene/Components/...`.

**UniversalAdditionalLightData** (guid `474bcb49853aa07438625e644c072ee6`), FALSIFIED 5-body census -> four remaining bodies:
`SampleScene.unity:354-376`, `PolygonGeneric/Scenes/Overview.unity:23425-23447`,
`PNB_Meadow_Forest/Scene/Overview.unity:4785-4807`, `Demo.unity:893169-893197`.
`Demo_02_BIRP.unity:892758-892780` is dropped from scope; parent-measured Light 4 matches the
four remaining bodies. Field values re-counted over those four:
- `m_Version` = 3 (3 bodies) / 4 (Demo) - serialization metadata, no engine counterpart.
- `m_UsePipelineSettings` = 1 (4/4; `.cs:37`) - NO counterpart (shadow-bias source selector).
- `m_AdditionalLightsShadowResolutionTier` = 2 (3/4; `.cs:99`; absent in Demo v4 body) - NO counterpart. Engine `ShadowCascadeCount` (`DirectionalLight.cs:36`) is directional cascade count, NOT this tier: do not substitute.
- `m_CustomShadowLayers` = 0 (4/4; `.cs:117`) - NO counterpart.
- `m_LightCookieSize` = {x:1,y:1} (4/4; `.cs:148`) - NO counterpart (engine `Cookie` texture at `Light.cs:106`/`SpotLight.cs:88` has no size property).
- `m_LightCookieOffset` = {x:0,y:0} (4/4; `.cs:159`) - NO counterpart.
- `m_SoftShadowQuality` = 1 (SampleScene) / 0 (other 3) (`.cs:170`) - NO counterpart; `ShadowHardness` (`Light.cs:336`) is a scalar, not this quality enum.
- `m_RenderingLayersMask` = {m_Bits:1} (Demo only, :893187-189; `.cs:204`) - NO counterpart.
- `m_ShadowRenderingLayersMask` = {m_Bits:1} (Demo only, :893190-192; `.cs:220`) - NO counterpart.
- `m_LightLayerMask` = 1 (4/4; deprecated `.deprecated.cs:45`) - NO counterpart.
- `m_RenderingLayers` = 1 (4/4; `:74`) - NO counterpart.
- `m_ShadowLayerMask` = 1 (4/4; `:60`) - NO counterpart.
- `m_ShadowRenderingLayers` = 1 (4/4; `:78`) - NO counterpart.
- `m_AffectsVolumetricFog`, `m_VolumetricMultiplier`, `m_VolumetricShadowDimmer` (`.cs:181,191,201`, `#if VOLUMETRIC_FOG`) - absent in all four bodies; NO counterpart (engine Fog is scene-wide).
Generic Unity `Light` body fields DO have grounded counterparts and must still be passed to the converter: `m_Color` -> `LightColor` (`Light.cs:144`), `m_Intensity` -> `Intensity` (`:161`), `m_ColorTemperature`/`m_UseColorTemperature` -> `Temperature` (`:183`), `m_Range` -> `Radius` (`PointLight.cs:50`, `SpotLight.cs:20`), `m_SpotAngle`/`m_InnerSpotAngle` -> `ConeOuter`/`ConeInner` (`SpotLight.cs:37,54`, full->half angle), `m_Shadows.m_Type` -> `Shadows` (`Light.cs:302`), `m_Type` -> component type (P1).

**UniversalAdditionalCameraData** (guid `a79441f348de89743a2939f4d699eac1`), FALSIFIED five-scene census -> FACT-FINDER PIN: 7 cameras / 6 URP bodies:
`SampleScene.unity:215-258`; `Demo.unity:525904-525947, 664050-664093, 707078-707121, 1008504-1008547`; `PNB_Meadow_Forest/Scene/Overview.unity:4901-4944`.
`Demo_02_BIRP.unity:243532-243575` is FALSIFIED and dropped from scope (Director 22:55 verbatim above);
the old "7 bodies" and "8 Cameras" counts came from the deleted BIRP scene and must not be carried
forward. Director 23:17 verbatim: 'Lean on the fact finder for the camera count in that instance.'
The fact finder pinned 7 `!u!20` Camera components over the four scenes (4 enabled, 3 disabled),
6 with a UniversalAdditionalCameraData sibling and the 7th a plain camera at
`PolygonGeneric/Scenes/Overview.unity:4277`; that pin supersedes both the old and any interim count.
- MAP (NEW MAPPING, Urp-gated, developer-tweakable): `m_RenderPostProcessing` = 1 (SampleScene, Demo:707078) / 0 (other enumerated bodies; re-census pending) -> `EnablePostProcessing` (`CameraComponent.PostProcess.cs:35`); `m_Antialiasing` = 0/1/2 (`.cs:502`) -> `DisableAntiAliasing` (`CameraComponent.cs:143`, any non-zero = false, same rule as HDRP antialiasing); `m_ClearDepth` = 1 (6 URP bodies out of 7 cameras; fact-finder pin; `.cs:506`) -> `ClearFlags` (`CameraComponent.cs:27`, value 0 would mean Color-only; no body exercises 0, so this mapping cannot be acceptance-proven from the fixture - report that).
- NO counterpart (report by name+value): `m_RenderShadows`=1 (`.cs:485`), `m_RequiresDepthTextureOption`=2 (`:488`), `m_RequiresOpaqueTextureOption`=2 (`:491`), `m_CameraType`=0 (`:493`), `m_Cameras`=[] (`:494`), `m_RendererIndex`=-1 (`:495`), `m_VolumeLayerMask`={m_Bits:1} (`:497`), `m_VolumeTrigger`={fileID:0} (`:498`), `m_VolumeFrameworkUpdateModeOption`=2 (`:499`), `m_AntialiasingQuality`=2 (`:503`), `m_StopNaN`=0 (`:504`), `m_Dithering`=0 (`:505`), `m_AllowXRRendering`=1 (`:507`), `m_AllowHDROutput`=1 (`:508`), `m_UseScreenCoordOverride`=0 (`:510`), `m_ScreenSizeOverride`={0,0,0,0} (`:511`), `m_ScreenCoordScaleBias`={0,0,0,0} (`:512`), `m_RequiresDepthTexture`=0 (`:517`, deprecated), `m_RequiresColorTexture`=0 (`:520`, deprecated), `m_Version`=2 (`:1060`), `m_TaaSettings.m_Quality`(or `m_Quality`)=3 / `m_FrameInfluence`=0.1 / `m_JitterScale`=1 / `m_MipBias`=0 / `m_VarianceClampScale`=0.9 / `m_ContrastAdaptiveSharpening`=0 (`TemporalAA.cs:74-96`).
- Generic Unity `Camera` fields keep their existing grounded mapping (`m_BackGroundColor` -> BackgroundColor, clear flags -> ClearFlags, FOV -> FieldOfView, near/far -> ZNear/ZFar, orthographic -> Orthographic/OrthographicHeight, `m_FOVAxisMode` -> FovAxis, MainCamera tag -> IsMainCamera).

## Pinned ownership
(pending)

## Hooks and call sites
(pending)

## Verification split (headless vs rendered)
(pending)

### MASTER TRANSCRIPTION (2026-11-26 09:05) - SUPERSEDES the (pending) markers above
The worker hit its 40-call cap with P3, P4 and the three pins only in its final answer. The Master transcribed them verbatim below. This file is a PLAN / HYPOTHESIS, not a gate.

P3 - engine-authored oracles (exact key set / __version): CameraComponent: `game/core/templates/gameobject/Camera.prefab:10-28` keys __type,__guid,__enabled,BackgroundColor,ClearFlags,EnablePostProcessing,FieldOfView,FovAxis,IsMainCamera,Orthographic,OrthographicHeight,Priority,RenderExcludeTags,RenderTags,TargetEye,Viewport,ZFar,ZNear, NO __version; `game/templates/game.minimal/Assets/scenes/minimal.scene:212-227` same minus __enabled and EnablePostProcessing/FovAxis, NO __version; `game/templates/game.playercontroller/Assets/scenes/basic_example.scene:21` adds Flags, six null OnComponent* callbacks, PostProcessAnchor, RenderTexture, NO __version. DirectionalLight: `minimal.scene:13-20` keys __type,__guid,FogMode,FogStrength,LightColor,Shadows,SkyColor, NO __version; `basic_example.scene:69` adds Flags, six callbacks, ShadowBias, ShadowCascadeCount, ShadowCascadeSplitRatio, ShadowHardness, Visualizer, __enabled, NO __version; `game/download/assets/scenes/lesson_1_day_1.c66ba5b5e4e5364c.scene:386` has __version=1, Intensity=1, Temperature=6500. PointLight: `gameobject/Light - Point.prefab:10` keys __type,__guid,Attenuation,FogMode,FogStrength,LightColor,Radius,Shadows, NO __version; `addons/menu/Assets/scenes/reward-unbox.scene:37172-37191` adds __enabled, Flags, Contribution, six callbacks, ShadowBias, ShadowHardness, NO __version; NO authored PointLight with __version was found. SpotLight: `gameobject/Light - Spot.prefab:10` adds Attenuation,ConeInner,ConeOuter,..., NO __version; `reward-unbox.scene:4055-4077` adds __enabled, Flags, Contribution, Cookie, six callbacks, ShadowBias, ShadowHardness, NO __version; `lesson_1_day_1.scene:957` has __version=1, Intensity=10, Temperature=4096.6387. Importer `Component()` (UnitySceneExport.cs:710-716) emits __version=1 for the three light types and NONE for CameraComponent. Deltas to list at implementation: generated lights add Intensity/__version and omit authored Contribution/Cookie/ShadowBias/ShadowHardness/Visualizer; generated cameras omit PostProcessAnchor/RenderTexture but add Flags + six null callbacks. Acceptance #4 in task.md is RE-SCOPED to a diff + fix-un-deserialised-keys + justified delta list, not byte-identical key sets.

P4 - URP-only fields with NO engine counterpart are REPORTED, never approximated: light m_UsePipelineSettings=1, m_AdditionalLightsShadowResolutionTier=2, m_CustomShadowLayers=0, m_LightCookieSize={1,1}, m_LightCookieOffset={0,0}, m_SoftShadowQuality=0/1, m_RenderingLayersMask/m_ShadowRenderingLayersMask m_Bits=1, m_LightLayerMask=1, m_RenderingLayers=1, m_ShadowLayerMask=1, m_ShadowRenderingLayers=1; camera m_RenderShadows=1, m_RequiresDepthTextureOption=2, m_RequiresOpaqueTextureOption=2, m_CameraType=0, m_Cameras=[], m_RendererIndex=-1, m_VolumeLayerMask m_Bits=1, m_VolumeTrigger fileID 0, m_VolumeFrameworkUpdateModeOption=2, m_AntialiasingQuality=2, m_StopNaN=0, m_Dithering=0, m_AllowXRRendering=1, m_AllowHDROutput=1, m_UseScreenCoordOverride=0, m_ScreenSizeOverride/ScreenCoordScaleBias zeros, m_RequiresDepthTexture=0, m_RequiresColorTexture=0, all m_TaaSettings values (quality=3, frameInfluence=0.1, jitterScale=1, mipBias=0, varianceClampScale=0.9, contrastAdaptiveSharpening=0). Report line format: `URP-only <field>=<value> on <GameObject> (<component>) has no Sandbox counterpart; reported, not converted.` No fabricated default and no substitution: shadow tier must NOT become ShadowCascadeCount, softShadowQuality must NOT become ShadowHardness, cookie size must NOT become a Cookie texture. MAP as Urp-gated developer-tweakable NEW MAPPINGs only: m_RenderPostProcessing=1/0 -> EnablePostProcessing (CameraComponent.PostProcess.cs:35), m_Antialiasing=0/1/2 -> DisableAntiAliasing (CameraComponent.cs:143), m_ClearDepth=1 -> ClearFlags (CameraComponent.cs:27; no fixture body has 0, so that half cannot be acceptance-proven). PASS BOTH the URP additional body and the generic Unity Light/Camera class body to any converter.

(a) Core ownership when task 25 is granted a Core round: Editor/Core/UnityUrp.cs (URP sibling-component selector by script guid + class + m_Enabled + priority/weight; field report helper; developer-tweakable properties for every NEW MAPPING); Editor/Core/UnitySceneExport.cs (Camera :780-813 and Light :829-877 converters accept the URP additional body alongside the Unity class body and HDRP body; Component :719-728 schema conformance); Editor/Core/UnityPrefabExport.cs (build URP sibling maps like HdComponents :858-885, pass bodies at :1505-1507 and :1513-1519; file-level Pipeline==Urp gate).

(b) Hooks: UnityPrefabExport.cs:1505-1507 camera hook - add urpCamera = Urp(UrpCameras, objectId) and pass BOTH bodies to UnitySceneExport.Camera(...). :1513-1519 light hook - pass Urp(UrpLights, objectId) together with the Unity Light class body and Hd(HdLights, objectId); enumerate EVERY MonoBehaviour candidate and filter by component class, script guid, m_Enabled, priority/weight; never take First() blindly. :858-885 HdComponents pattern - add a URP sibling map keyed by GameObject fileID via UnityUrp.Component(GuidOf(Field(body,'m_Script'))).Kind. UnitySceneExport.cs:719-728 Component(...) writes __type,__guid,__enabled,Flags, six null OnComponent* callbacks and __version from ComponentVersions (:706-716); diff generated keys against the P3 oracles. :691 report class-name site - extend to UnityUrp.Component for URP report lines; unmapped fields go through Result.Information via a UnityUrp report helper. Gate every new URP rule on Pipeline==Urp derived from the FILE-LEVEL URP script-guid census (scene objects have no UnityMaterial.Pipeline; UnityMaterial.cs:76 is per-material only). Direct-model Source has no Root block: InstanceRootTransform must fall back to a cloned source.RootTransform. Keep Editor/Core/*.cs BOM-less and LF, route new paths through UnityImport.DiskPath, never change import_scale or the single 39.37 ModelModifier. No git add/commit/push by any worker; no editor launch/close; no new tooling, checker, harness or fixture package; no .humans-only access; no edits to role files, skills, memory or config.

(c) Verification split - HEADLESS (worker, on disk): rebuild harness `dotnet build Documentation/Unity-To-SBox-Automated/harness/harness.csproj -c Release -p:IUP_ROOT=C:/cseliot/1-Development/1-SBox/2-Projects/unityimporterplus_branch_2 --no-incremental` (0 errors); `verify_output.py <outRoot> <outRoot>/imported` (`checked N files, 0 problems`); source-vs-generated value assertions printing both sides; component key-set / __version diff against the P3 oracles; per-code Unresolved differential against a HEAD baseline in the SAME harness mode; building and city--1 byte-identical; Standard/HDRP byte-identical. RENDERED (Director only): perceptual look / size / lighting judgment under 4A conditions (check no Director-owned editor already open, never close an editor this work did not launch, NTFY before each large round; live sbox-dev pids 70260/79524 and launcher 38088 are untouched). No editor round in this plan phase.

VERSION RE-GROUND: the 6000.3.29f1 / 17.3.0 pin quoted in this plan is SUPERSEDED; see task.md (binding) and the Master dispatch contract AMENDMENT 09:05. Both URP sibling script guids still resolve in the on-disk 17.7.0 cache; re-resolve them there and re-derive EVERY line citation. HEAD is 0d31400.
