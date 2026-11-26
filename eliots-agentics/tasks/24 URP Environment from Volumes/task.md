## Task
Read URP Global Volume profiles and map their override values to Sandbox.PostProcessVolume and engine effect components, instead of reporting source values and copying camera-template defaults.

### Description
Grounding: Editor/Core/UnityVolume.cs reads only Volume blocks identified by an HDRP script guid (UnityHdrp.cs:111-120); UnitySceneExport.cs:501-503 reports source post values rather than applying them, and :387-390 deliberately omits HDRP Volume fog. CameraPostEffects at :636-667 emits Bloom/SSR/AmbientOcclusion/Tonemapping with values transcribed from the engine camera template (:70-79), not from the source. A PostProcessVolume must use the Volume's `m_GameObject` fileID for its transform (`c.Transforms` is keyed by GameObject fileID), set `Type: Infinite`, put effects on nested GameObjects, and keep local volumes report-only.

SyntyForest: 2 Global Volume MonoBehaviours with script guid 172515602e62fb746b5d573b38a5fe58 (SampleScene.unity:403, Demo.unity:371362), fields m_IsGlobal: 1, priority: 0, blendDistance: 0, weight: 1. Profiles and MonoBehaviour sub-asset counts (`rg -c '^--- !u!114 &' <file>`): Assets/Settings/DefaultVolumeProfile.asset 29, Assets/Settings/SampleSceneProfile.asset 5, .../Scene/Demo/Global Volume Profile.asset 6, Assets/Synty/PNB_Core/PPP_Base_01.asset 5, .../Meadows_Post_Processing_01.asset 5. The two profiles actually referenced are `SampleScene.unity:410` -> guid `10fc4df2da32a41aaa32d77bc913491c` (`Assets/Settings/SampleSceneProfile.asset`) and `Demo.unity:371369` -> guid `7679a2a2468dc7742a7ece2dea61ab3b` (`.../Demo/Global Volume Profile.asset`); their `components:` lists (`SampleSceneProfile.asset:90`, `Global Volume Profile.asset:103`) name Bloom, Vignette, Tonemapping, MotionBlur and LiftGammaGain, Bloom, WhiteBalance, ColorAdjustments, ShadowsMidtonesHighlights, with `m_OverrideState: 1` on intensity overrides (`SampleSceneProfile.asset:22-23,69-70,154-155`; `Global Volume Profile.asset:44-45`). Two further profiles (`PPP_Base_01.asset`, `Meadows_Post_Processing_01.asset`) have a root `settings:` list, but the `sharedProfile` guid, not that list, decides which profile a Volume uses. The Global Volume C# type/path and whether the default profile is active are UNVERIFIED: resolve them before implementing.

Engine effect classes exist for Bloom, Tonemapping, ColorAdjustments, Vignette, FilmGrain, DepthOfField, MotionBlur, ChromaticAberration, ColorGrading, Sharpen, AmbientOcclusion (12 named class declarations matched by `grep -rlE 'public (sealed )?(partial )?class (Bloom|Tonemapping|ColorAdjustments|Vignette|FilmGrain|DepthOfField|MotionBlur|ChromaticAberration|ColorGrading|Sharpen|AmbientOcclusion)' engine/Sandbox.Engine/Scene/Components/PostProcessing/Effects/*.cs | wc -l` -> 12; 18 `.cs` files total by `find ... -name '*.cs' | wc -l`). `Sandbox.ScreenSpaceReflections` is [Obsolete]/[Hide] and does nothing (Effects/ScreenSpaceReflections.cs:3-12): report, do not convert. No authored Sandbox.PostProcessVolume or Sandbox.Fog instance exists in the 11 engine .scene files; the class definitions are the oracle.

Work:
1. Detect URP Volume blocks by the guid above, read the referenced VolumeProfile sub-assets, and enumerate EVERY override with the same filter the engine uses (component class, m_OverrideState, priority/weight). Prove a value is genuinely absent before approximating one.
2. Map only overrides with an engine effect class, using SOURCE values, into Sandbox.PostProcessVolume with `SceneVolume = {"Type": "Infinite"}` and effects on nested GameObjects. Use the Volume's m_GameObject fileID for the transform. Keep local volumes and unmapped overrides report-only.
3. Gate on `Pipeline == Urp`; do not change the HDRP path.

### Acceptance (one explicit pass/fail check per line)
1. A generated .scene for a scene containing a Global Volume has a Sandbox.PostProcessVolume with Type Infinite whose transform matches the Volume GameObject's transform (assert the exact position values; a non-origin Volume case must be exercised).
2. At least one mapped effect (e.g. Bloom strength or Tonemapping exposure) carries the source override value, NOT the camera-template default; print both numbers.
3. Every candidate override is enumerated with its m_OverrideState and the winning profile stated; no approximation is introduced for a value that was actually present.
4. SSR, baked data and unmapped overrides produce report lines, not fabricated components.
5. building and city--1 output byte-identical to a HEAD baseline in the same mode; per-code Unresolved breakdown unchanged.
6. Harness compile gate 0 errors; verify_output.py `checked N files, 0 problems`.

### Work Review


## Clarification Q&A
An (Director 2026-11-26 03:57, 1A/3A/5A): "Unity 6.7 URP" is the session label for the on-disk 6000.3.29f1 / URP 17.3.0 fixture; target the URP 17.x surface, not one editor build. Visual fidelity only. No new tooling or fixture package.

SUPERSEDED (2026-11-26 22:27, Director 1A re-pin; appended 23:17): the verbatim Q&A above stays byte-for-byte as history. Canonical target is Unity 6000.7.0b10 (4f3c1c5c4346) / URP 17.7.0 with caches `com.unity.render-pipelines.universal@e9044f1f1f57`, `com.unity.render-pipelines.core@7aa87ac5532f`, `com.unity.render-pipelines.universal-config@767fc04a69c2` under FIXTURE/Library/PackageCache/. The 6000.3.29f1 / 17.3.0 / @04369b574bc4 pins do not exist anywhere under 4-Unity (parent-verified). This note is the only SUPERSEDED note in this file; the binding re-ground is in `## Notes` below.

## Relevant History
See eliots-agentics/tasks/urp-support-research.md sections 3-4 for the Volume census and engine effect-class citations. HEAD bfc189b; session-start harness compile gate green.

## Notes

#### Master premise re-ground (2026-11-26 09:05, binding)
The 6000.3.29f1 / URP 17.3.0 / cache `@04369b574bc4` pins in this task and in urp-support-research.md are SUPERSEDED. Verified on disk: SyntyForest is `6000.7.0b10 (4f3c1c5c4346)` / URP 17.7.0 with caches `com.unity.render-pipelines.universal@e9044f1f1f57` and `com.unity.render-pipelines.core@7aa87ac5532f`; unity_samples_oracle is the same version. Director 1A already targets the URP 17.x surface, not one editor build, so no clarification was raised. Re-resolve every guid/keyword against the CURRENT caches and re-derive EVERY line citation on disk: this task's Description cites `Demo.unity:371362`/`:371369`, but the on-disk guid is `:371225` and sharedProfile `:371232` (uniform -137 shift). Research sub-asset counts 5/6/29 are stale vs on-disk 6/6/33 (re-counted 23:17 with `grep -c '^--- !u!114 &'`). The Global Volume guid resolves to `UnityEngine.Rendering.Volume` (shared Core type), NOT a URP-only class: detect with `m_IsGlobal: 1` plus scene URP evidence and gate on `Pipeline == Urp`. A non-origin Global Volume DOES exist (Demo.unity GameObject &785097868, m_LocalPosition 61.38825 / -10.131328 / 41.147015), so acceptance #1 can be exercised with no new fixture; assert both the Demo and the origin SampleScene case. The DefaultVolumeProfile is active as the URP global default stack, not as a scene Volume. `Assets/imported/` is an untracked pre-existing artifact (mtime 01:29) - regenerate from fixed HEAD before any acceptance claim.
Standing constraints: gate every URP rule on `Pipeline == Urp`; keep Editor/Core/*.cs BOM-less and LF; route new paths through UnityImport.DiskPath; any helper generating several components must return an unattached List<JsonObject> (a JsonNode cannot be added to two JsonArrays). Editor rounds follow the 4A conditions: before any editor round, check that no Director-owned editor is already open on that project; never close an editor this work did not launch; NTFY before each large round.

## Stylistic Task Notes:
References to variables like X and Y automatically imply that these are to be exposed properties for developer tweaking.
When programming, portability and DRY are a priority.



## Post-Task Follow Ups
