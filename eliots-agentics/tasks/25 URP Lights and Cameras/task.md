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

2026-11-27 00:59 - Verdict: changes required
PARENT TRANSCRIPTION NOTE: the critic (sa-0-35abd44a) hit its 40-call cap after completing all attack areas and returning the full findings in its completion summary, before it could write them into this file. The entry below is transcribed verbatim from that summary by the Master; the Tree: hashes were re-derived by the Master with `git hash-object --path=` and match the on-disk files at transcription. The critic made no repo edits.

Attack areas A-F complete. Two silent-drop defects break acceptance #3 (every unmapped URP field by name + value); all other reviewed gates passed.
1. [High] UnityUrp.cs:216-219,240-246 - `UnmappedTaaFields` lists only legacy lowercase names; canonical serialisation is `m_Quality`/`m_FrameInfluence`/`m_JitterScale`/`m_MipBias`/`m_VarianceClampScale`/`m_ContrastAdaptiveSharpening` (URP 17.7.0 `Runtime/TemporalAA.cs:76-91`, `FormerlySerializedAs`). 5 of 6 camera bodies use the `m_` form; the report contains TAA lines for SampleScene only - 30 field/value pairs silently unreported. Fix: report both name variants and make `Section()` indentation-aware so `m_*` children are not treated as field terminators.
2. [High] UnityUrp.cs:194,221-227 - `Section`'s `^\s*m_\w+:` lookahead stops at nested `m_Bits:`, so all 6 `m_VolumeLayerMask` and 2 Demo light mask lines report `{serializedVersion: 2}` / `{serializedVersion: 0}`, dropping `m_Bits: 1`. 03-verification.md:49 claims the opposite. Fix: read the full indented/braced block; correct the verification claim.
3. [Low] UnitySceneExport.cs:953-954 - absent `m_RenderPostProcessing` falls back to `true`, but URP source `UniversalAdditionalCameraData.cs:501` declares `= false`. Fix: map only when authored; otherwise use the source default, never an invented `true`.
4. [Low] UnitySceneExport.cs:980 - mapped line derives `DisableAntiAliasing` from emitted properties; with `MapAntialiasing=false` the key is absent and the line falsely prints `False`. Fix: compute from the source rule or print "not mapped".
5. [Low] UnityUrp.cs:88-96 / UnityPrefabExport.cs:16-19 - `OrderBy(b=>b.Id).First()` tie-break is undocumented (only the worker journal says "orders by block id"); class check via `.Split('.').Last()` handles the three authored `Assembly::Namespace.Class` bodies but would skip a `Namespace.Class, Assembly` form. Fix: document the tie-break and tolerate the `, Assembly` suffix; no duplicate candidates exist in the fixture.
6. [Info] Fixture gaps: no `m_ClearDepth: 0` body (Color half unproven; `&&`-before-`||` precedence and hd path are correct); all 4 lights directional (`Range->Radius` unproven); no URP guids outside top-level `!u!114` blocks, so prefab patch path unproven; 3 volumetric light entries are documented absent (harmless).
7. [Info] Duplicate scan: 0 duplicate `(file, component, field, object)` triples in 172 URP-only lines; 6 mapped lines, one per camera. Counts 31/122/7216 (+178); building/city 0-byte diffs; scene diff camera-only, PPV subtrees identical. Source-explicit `m_RenderPostProcessing: 0` on Demo Cameras 2/3/4 and PNB Overview (no fallback involved); Demo Main Camera (1) has `m_Antialiasing: 2`. Post off on 3 Demo cameras + 1 overview and AA on Demo Main Camera (1) are visible changes - Director must judge perceptually in the editor; not an agent call.
8. [Info] Hygiene passed: engine properties `EnablePostProcessing` (PostProcess.cs:35) and `DisableAntiAliasing` (CameraComponent.cs:143) exist; generated keys deserialise; no BOM/markers; both call sites updated (UnityPrefabExport.cs:1529,1540); HDRP/Unknown pass null.

PARENT VERIFICATION of the two High findings: TemporalAA.cs:76-91 does carry `[FormerlySerializedAs("quality")] m_Quality` ... for all six fields; Demo.unity has 4 `m_TaaSettings:` blocks with `m_Quality: 3` at :525941, :664087, :707115, :1008541 and PNB Overview 1 at :4939, while SampleScene:215-258 uses the lowercase form - so the lowercase-only list does miss 5 bodies. after-report.json confirms `m_VolumeLayerMask={serializedVersion: 2}` (m_Bits dropped) on Main Camera. `UniversalAdditionalCameraData.cs:501` is `[SerializeField] bool m_RenderPostProcessing = false;`.

Tree:
- Editor/Core/UnityUrp.cs: `git hash-object --path="Editor/Core/UnityUrp.cs" "Editor/Core/UnityUrp.cs"` -> 4ddb0b3776281fa8e7f0047672c8714781f456e1
- Editor/Core/UnitySceneExport.cs: `git hash-object --path="Editor/Core/UnitySceneExport.cs" "Editor/Core/UnitySceneExport.cs"` -> ecfdc558477b49c47e53bf4b232bb6f1dc5ace87
- Editor/Core/UnityPrefabExport.cs: `git hash-object --path="Editor/Core/UnityPrefabExport.cs" "Editor/Core/UnityPrefabExport.cs"` -> e864c1a8a9349b86aa63af8e977d6cf669652646

Verdict: changes required

#### Re-check 2026-11-27 01:11

Scoped to findings 1-5 and new regressions; Info 6-8 and previously passed bars not re-opened. No imports re-run; parent gates re-derived where cheap.

1. [High] FIXED - UnityUrp.cs:220-230 holds canonical `m_*` names first with lowercase `[FormerlySerializedAs]` aliases; :251-259 emits one authored variant then `break`. `after2-report.json` re-derived: 36 TAA lines, exactly 6 per camera body (SampleScene lowercase x6; the 5 `m_*` bodies x6 each), names 1 canonical-set + 5 canonical form, 0 duplicate `(file, component, field, object)` triples across 202 URP-only lines. Information 7216 -> 7246 = +30. No double emission.
2. [High] FIXED - UnityPrefabExport.cs:183-209 is indentation-aware: nested `m_Bits` stays in the block, same-indent `-` YAML items stay, only same-or-shallower non-item lines terminate. All 8 mask lines now carry `m_Bits: 1` (6 camera `{serializedVersion: 2 m_Bits: 1}`, 2 Demo light `{serializedVersion: 0 m_Bits: 1}`). Caller audit: the only `Section` callers are `m_AddedComponents` (:1351), `m_Materials` (:1418,:1443), `FieldValue` and the TAA block (UnityUrp.cs:237,:251). Fixture `m_Materials:` / `m_AddedComponents:` sequence items are written at the field's indent (`  - ...`), so the explicit dash exception preserves them; no caller relied on the old any-indent `m_*` termination. 03-verification.md:43-57 corrected with the 2026-11-27 01:15 note.
3. [Low] FIXED - UnitySceneExport.cs:953-955 uses the authored value when present and source-default `false` when absent; `urp == null` remains `true` for HDRP/Standard. All six fixture bodies author `m_RenderPostProcessing` (1,0,0,0,1,0), so the default branch is unreachable here; after/after2 whole-tree diff is 0 lines, confirming zero change to authored bodies.
4. [Low] FIXED - UnitySceneExport.cs:981-984 derives `DisableAntiAliasing` from `Number(urp,"m_Antialiasing") == 0` when mapped and prints `DisableAntiAliasing not mapped (MapAntialiasing off)` otherwise. All 6 mapped lines remain single and correct (AA on only for Demo Main Camera (1), `m_Antialiasing: 2`).
5. [Low] FIXED - UnityUrp.cs:95-99 strips a `, Assembly` suffix before taking the last dot segment; `Assembly::Namespace.Class` bodies still pass (no comma). Unity doesn't serialize generic MonoBehaviours with a comma-bearing class name, and the check is reached only after the URP script guid matches, so no legitimate identifier is truncated here. UnityPrefabExport.cs:846-852 documents lowest-block-id as the tie-break.

No new silent drop or fabricated value found. Parent gates on final bytes: build 0/0, verify 1627/0, counts 31/122/7246, URP-only 202 + mapped 6, imported-tree diff 0 lines, building/city 0 bytes against urp23 baselines.

Tree:
- Editor/Core/UnityUrp.cs: `git hash-object --path="Editor/Core/UnityUrp.cs" "Editor/Core/UnityUrp.cs"` -> d755b96e61df19a7ee8a4e8077ea6924e0735cf0
- Editor/Core/UnitySceneExport.cs: `git hash-object --path="Editor/Core/UnitySceneExport.cs" "Editor/Core/UnitySceneExport.cs"` -> a8f0bbb09f72853f4eec9f0f9e0788d61faefe96
- Editor/Core/UnityPrefabExport.cs: `git hash-object --path="Editor/Core/UnityPrefabExport.cs" "Editor/Core/UnityPrefabExport.cs"` -> de982affbc10ef780222125ef43c89ca264aa989

Verdict: no further changes

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
