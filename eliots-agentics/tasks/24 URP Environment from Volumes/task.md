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

## Relevant History
See eliots-agentics/tasks/urp-support-research.md sections 3-4 for the Volume census and engine effect-class citations. HEAD bfc189b; session-start harness compile gate green.

## Notes
Standing constraints: gate every URP rule on `Pipeline == Urp`; keep Editor/Core/*.cs BOM-less and LF; route new paths through UnityImport.DiskPath; any helper generating several components must return an unattached List<JsonObject> (a JsonNode cannot be added to two JsonArrays). Editor rounds follow the 4A conditions: before any editor round, check that no Director-owned editor is already open on that project; never close an editor this work did not launch; NTFY before each large round.

## Stylistic Task Notes:
References to variables like X and Y automatically imply that these are to be exposed properties for developer tweaking.
When programming, portability and DRY are a priority.

## Post-Task Follow Ups
