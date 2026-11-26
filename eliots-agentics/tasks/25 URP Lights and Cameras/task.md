## Task
Read URP UniversalAdditionalLightData / UniversalAdditionalCameraData sibling bodies and decide, with engine-source evidence, which fields map to s&box light and camera components and which are report-only.

### Description
Grounding: Editor/Core/UnitySceneExport.cs:780-813 emits Sandbox.CameraComponent from generic Unity Camera fields; :829-877 emits Sandbox.DirectionalLight / PointLight / SpotLight from generic Unity Light fields. The HDRP path finds HDAdditional*Data bodies by script guid at Editor/Core/UnityPrefabExport.cs:858-885 and hands them to the converters; there is no URP equivalent. Generated components go through Component(...) at :718-728. Engine oracles: engine/Sandbox.Engine/Scene/Components/Camera/CameraComponent.cs:23-110, Light/Light.cs:140-223, game/core/templates/gameobject/Camera.prefab:10-66, game/templates/game.minimal/Assets/scenes/minimal.scene:12-20.

FALSIFIED IN PLACE (2026-11-26 23:17) - the former premise "SyntyForest: 5 Lights ..., 8 Cameras ... 5 at `{r: 0,...}` and 3 at `{r: 0.192...}`, 0 ReflectionProbes. URP MonoBehaviours: UniversalAdditionalCameraData ... (7), UniversalAdditionalLightData ... (5)" was measured over a FIVE-scene fixture that included `Demo_02_BIRP.unity`. Primary source, Director (chat 2026-11-26 22:55, verbatim): 'I deleted "Demo_02_BIRP.unity" because it stands for "BUILT IN RENDER PIPELINE" and we're not supporing that.' Director 23:17: 'BIRP is to be ignored'. BIRP is DROPPED FROM SCOPE; do not merge its counts into the baseline. CURRENT FOUR-SCENE BASELINE (`SampleScene`, `PolygonGeneric/Overview`, `PNB_Meadow_Forest/Scene/Overview`, `PNB_Meadow_Forest/Scene/Demo`): parent-measured Light 4, Terrain 1, ReflectionProbe 0, 28 `.terrainlayer`. CAMERA COUNT PINNED by the read-only fact finder (Director 23:17 verbatim: 'Lean on the fact finder for the camera count in that instance.'): 7 `!u!20` Camera components across the four current scenes (4 enabled, 3 disabled); 6 carry a UniversalAdditionalCameraData sibling, and the 7th is a plain camera at `PolygonGeneric/Scenes/Overview.unity:4277`. Use 7 cameras / 6 URP camera bodies; do not use 6 or 8 as the camera total. The old 8-camera count was falsified with the BIRP scene. Both URP sibling guids (`a79441f348de89743a2939f4d699eac1` camera, `474bcb49853aa07438625e644c072ee6` light) still resolve in the 17.7.0 cache; re-resolve against its `Runtime/*.cs.meta:2`. The old background-colour distribution (5/3) is falsified with the BIRP scene and must be re-measured. Example serialized fields below were read against the current 17.7.0 cache; re-derive their line citations before use.

Work:
1. Enumerate EVERY field in the two sibling bodies, apply the same filter the engine uses (component class, script guid, override state, priority/weight), and identify which component wins. A wrong selector looks exactly like missing source data; prove absence before approximating.
2. Map only fields with a grounded engine counterpart (clear flags, background colour, main-camera flag, FOV/axis, near/far, orthographic; light colour, range, spot angle, shadows, temperature). Report every unmapped URP field by name with its value; do not emit an approximation or a fabricated default.
3. Compare each generated component to an engine-authored artifact of the same __type (engine schema conformance), including __version, and fix any key the engine does not deserialise.
4. Gate on `Pipeline == Urp`; Standard and HDRP output unchanged.

### Acceptance (one explicit pass/fail check per line)
1. A generated camera component carries the source clear flags, background colour, FOV and near/far values, each asserted against the source scene lines; print both sides.
2. Each generated light component carries the source colour, intensity and range; the m_Type: 1 interpretation is grounded in primary source (UnityCsReference GraphicsEnums.cs:69-73, Directional = 1), not assumed.
3. Every URP-only field without an engine counterpart appears in a report line naming the source field and its value; no approximation is stacked on a value that was actually present but missed by the selector.
4. RE-SCOPED 2026-11-26 09:05: diff each generated `__type` against an engine-authored artifact of that type (key set AND `__version`), FIX any key the engine does not deserialise, and LIST every remaining delta with a justification. The original wording "same key set and __version ... list any delta" was self-contradictory: engine-authored Camera/DirectionalLight/PointLight/SpotLight artifacts carry no `__version`, templates omit `Flags`/six null `OnComponent*` callbacks, and generated lights add `Intensity`/`__version` while omitting authored `Contribution`/`Cookie`/`ShadowBias`/`ShadowHardness`/`Visualizer`. A byte-identical key set is NOT the bar; an un-deserialised key is.
5. building and city--1 output byte-identical to a HEAD baseline in the same mode; per-code Unresolved breakdown unchanged.
6. Harness compile gate 0 errors; verify_output.py `checked N files, 0 problems`.

#### Master premise re-ground (2026-11-26 09:05, binding)
The 6000.3.29f1 / URP 17.3.0 / cache `@04369b574bc4` pins are SUPERSEDED. On disk SyntyForest is `6000.7.0b10 (4f3c1c5c4346)` / URP 17.7.0 with cache `com.unity.render-pipelines.universal@e9044f1f1f57`; unity_samples_oracle is the same version. Director 1A already targets the URP 17.x surface, not one editor build, so no clarification was raised. Both URP sibling script guids still resolve in the on-disk cache; re-resolve them there and re-derive EVERY line citation on disk. PASS BOTH the URP additional body and the Unity Light/Camera class body to any converter. `Assets/imported/` is an untracked pre-existing artifact (mtime 01:29) - regenerate from fixed HEAD before any acceptance claim.

### Work Review


## Clarification Q&A
An (Director 2026-11-26 03:57, 1A/3A/5A): "Unity 6.7 URP" is the session label for the on-disk 6000.3.29f1 / URP 17.3.0 fixture; target the URP 17.x surface, not one editor build. Visual fidelity only. No new tooling or fixture package.

SUPERSEDED (2026-11-26 22:27, Director 1A re-pin; appended 23:17): the verbatim Q&A above is kept byte-for-byte. Canonical target: Unity 6000.7.0b10 (4f3c1c5c4346) / URP 17.7.0, caches `com.unity.render-pipelines.universal@e9044f1f1f57`, `com.unity.render-pipelines.core@7aa87ac5532f`, `com.unity.render-pipelines.universal-config@767fc04a69c2` under FIXTURE/Library/PackageCache/; old 17.3.0 / 6000.3.29f1 / 96d2fe17529c pins do not exist under 4-Unity (parent-verified). One note per file; the binding re-ground is at `#### Master premise re-ground` above.

## Relevant History
See eliots-agentics/tasks/urp-support-research.md sections 3-4 for the MonoBehaviour census and engine component citations. HEAD bfc189b; session-start harness compile gate green.

## Notes
Standing constraints: gate every URP rule on `Pipeline == Urp`; keep Editor/Core/*.cs BOM-less and LF; route new paths through UnityImport.DiskPath; pass BOTH the URP additional body and the Unity Light/Camera class body to any converter (HDRP light data is split across two components and silently loses range/shadows if only one is passed). Editor rounds follow the 4A conditions: before any editor round, check that no Director-owned editor is already open on that project; never close an editor this work did not launch; NTFY before each large round.

## Stylistic Task Notes:
References to variables like X and Y automatically imply that these are to be exposed properties for developer tweaking.
When programming, portability and DRY are a priority.



## Post-Task Follow Ups
