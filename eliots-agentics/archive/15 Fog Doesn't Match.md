## Status: STARTED? REJECTED! CLARIFIED? YES! HISTORY? YES! REVIEWED? YES! DONE? NO! WORKER: NONE OTHER: reopened 2026-11-24; Unity has no visible fog; HDRP support abandoned

## Task
Fog doesn't match.

### Description
Director's verbatim symptom (2026-11-24): "There is no fog in the unity scene. Or if there is, it only renders at a distance of 1000 ft and is BARELY noticeable. So that should be the same here." And: "I removed the fog myself and i got much closer to unity-sbox parity by increasing directional light intensity. So once you finally solve the fog situation, use this knowledge to help yourself get lighting parity."

Acceptance: the imported MergeV2 / Test_Range scenes show no visible fog, matching the Unity source. The symptom is finished when it is GONE, not when a spec is satisfied. Do not set DONE? YES! while it is reported.

Reopened from eliots-agentics/archive/15 Fog Doesn't Match.md. The 2026-11-23 fix (e9e71cf) removed the opaque height-fog layer (HeightColor alpha 1 -> HeightEnabled:false) and is a real objective fix; it stays. This reopen is about the residual DISTANCE fog. That closure was already flagged invalid because it covered only objective wall removal, not perceptual acceptance.

Current state (2026-11-24): HEAD ae9781a. Editor/Core/UnitySceneExport.cs:299-323 emits Sandbox.Fog from UnityVolume.Read's HDRP Volume fog:
  AttenuationDistance 7874 (200 m), Color "0.5,0.5,0.5,1", ColorSource "Constant",
  DistanceEnabled true, DistanceRange "0,23622", FogEnabled true,
  HeightEnabled false, MaximumOpacity 1, Mode "Exponential", StartDistance 0.
Unity RenderSettings has m_Fog: 0 (MergeV2.unity) and the Director reports no visible fog. The generated fog produces a grey desaturating wash that is not in Unity; the Director removed the Fog component in the editor and got much closer to parity. The clean-output perceptual gate (scratch/.../visual/CLEAN-PERCEPTUAL-GATE.md) measured mean 55.46 / 152052 coloured px / 91 warm px against the Unity oracle's 46.09 / 730872 / 391051, with a flat grey exterior attributed to this fog.

No HDRP-specific work: the Director abandoned HDRP support on 2026-11-24. Do NOT investigate HDRP Volume semantics, PhysicallyBased sky, sunIntensity, baked GI or HDRP light units. Ground the fix in the visible parity requirement: Unity shows no fog, so the imported output must not emit a fog component.

Required work:
1. Change Editor/Core/UnitySceneExport.cs so the HDRP Volume fog path (the `if ( environment.Fog is UnityVolume.Fog volumeFog )` block at lines 299-323) does NOT emit a Sandbox.Fog SceneObject. Keep the legacy Unity RenderSettings fog path (lines 352-376) intact for non-HDRP scenes.
2. Update the report text so it no longer claims the fog was converted: replace `result.Information.Add( volumeFog.Note )` with a line stating that HDRP Volume fog is not emitted because the Unity source shows no visible fog at scene scale. Do not hand-edit generated scenes; the change is in the importer core only.
3. Do not change the height-fog fix, the skybox emission, or any lighting code in this task. Lighting parity is a separate step the Director ordered AFTER the fog is solved.
4. Headless acceptance: grep the generated mergev2.scene, test_range.scene and outdoorsscene.scene for "Sandbox.Fog" -> 0 occurrences. building (24 files, no --scenes) and city--1 (1166 files, no --scenes) byte-identical to a fresh HEAD-code baseline in the same mode with BOTH trees populated; verify_output.py <outRoot> <outRoot>/imported 0 problems; harness build 0 errors / 0 warnings; two identical runs byte-identical; BOM-less; no stray artifacts.
5. Evidence in scratch files with absolute paths cited in ## Notes. No DONE? YES! from serialized JSON alone; a fresh rendered frame must be looked at before the symptom closes.
6. Stage by explicit path only. No push until the Master verifies. Do not add unrequested tooling. Final answer in English.

### Work Review

2026-11-24 22:23 - Verdict: no further changes
Reviewer role: Review Critic (bounded adversarial re-review); judged the four uncommitted Editor/Core files plus the closing evidence, not a missing worker trail.

1. [Low] Editor/Core/UnitySceneExport.cs:299-302 - HDRP Volume fog emission is gone and the Information line now cites the Director's observation; the legacy RenderSettings path is untouched and the parser in UnityVolume.cs is deliberately report-only. I looked at OUTDOOR-main-640.jpg myself: no grey wash, no flat grey wall, defined building edges. The reported fog symptom is GONE; no code change required.
2. [Low] Testing/Unity-Screenshot-Outside.png - the oracle does show a pale haze band at the horizon and distant hills, whereas the s&box frame has clear blue sky and abrupt platform edges. That residual distance/atmosphere difference is not the Director's "no visible fog" order and is not a reason to re-emit Sandbox.Fog. Fix: disclose it to the Director alongside the other outdoor gaps; keep DONE? NO!.

Tree (four reviewed source files at HEAD ae9781a):
- Editor/Core/UnityHdrpLighting.cs — `git hash-object --path="Editor/Core/UnityHdrpLighting.cs" "Editor/Core/UnityHdrpLighting.cs"` => 3de49ef03d589016281b43d044910382893f34ac
- Editor/Core/UnityPrefabExport.cs — `git hash-object --path="Editor/Core/UnityPrefabExport.cs" "Editor/Core/UnityPrefabExport.cs"` => 7c80ad48c817952ae79aa2935775e32616249049
- Editor/Core/UnitySceneExport.cs — `git hash-object --path="Editor/Core/UnitySceneExport.cs" "Editor/Core/UnitySceneExport.cs"` => 6bc6a265c16a18cc84b1e7677fbc45f14f1637ae
- Editor/Core/UnityVolume.cs — `git hash-object --path="Editor/Core/UnityVolume.cs" "Editor/Core/UnityVolume.cs"` => 420f4633aac52e30cb9547819171efe06f428786

## Notes

MASTER FOG GATES 2026-11-24 (headless, parent-run, fog fix only):
HEAD baseline built in an isolated `git archive HEAD` mirror at
scratch/taskmaster-nov22/fog-gate-head/out (harness 0/0, building 24 files,
city--1 1166 files / 3 unresolved). Fixed tree rebuilt harness 0/0.
building 24/24 and city--1 1166/1166, `diff -r --brief --strip-trailing-cr`
vs baseline = 0 lines for both. Two identical building runs = 0-byte diff.
verify_output.py: building 10 files / 0 problems, city--1 775 files / 0
problems. BOM scan of Editor/Core/*.cs = 0 BOM files.
old_school --scenes --folders '*' scratch import: assets=1280 converted=972
files=2154, unresolved=64 (source-data gaps, documented), verify_output.py
875 files / 0 problems; grep 'Sandbox.Fog' across generated *.scene = 0 files
(outdoorsscene.scene, mergev2.scene, test_range.scene). Diff on disk:
Editor/Core/UnitySceneExport.cs 1 insertion / 22 deletions; legacy
RenderSettings fog path (lines 331+) intact. Logs:
scratch/taskmaster-nov22/fog-gate-fix/out/. The symptom still needs a fresh
rendered frame before DONE? YES!, so this header stays NO!.

MASTER RENDERED-FRAME GATE 2026-11-24 (fog + placement fixes):
opened a SEPARATE Dev editor on a scratch project (local/fogcheck, PID 46236)
containing the 2154-file generated output, so the Director's unsaved in-memory
work in the live editor was never touched. Pinned camera Japanese School Camera
(41ba4fe2-...), play mode, camera_screenshot 1280x720:
  Unity oracle:  mean 46.09, stddev 32.68, coloured 730872 (61.0%), warm 391051 (32.6%)
  fog+placement: mean 40.92, stddev 15.39, coloured 610701 (66.3%), warm 290005 (31.5%)
  clean pre-fix: mean 55.46, stddev 20.31, coloured 152052 (16.5%), warm 91 (0.01%)
vision_analyze of the frame reports NO fog, NO flat grey wall, defined edges and
dark/blank window panes; it is still dim with a red/magenta cast and no long
golden shadows. The FOG symptom (grey wash) is gone; full lighting parity is a
separate task. Frames: visual/RENDER-fogfix-pinned-960.jpg. A critic review and
commit are still required before DONE? YES!.

REVIEW DISPOSITIONS 2026-11-24 (adversarial review deleg_a58a8db3):
F3 PARTIAL ACCEPT - the "RenderSettings m_Fog: 0" reason at UnitySceneExport.cs:301
is false for HDRP (fog lives in Volumes). The Information line and the stale
UnityVolume.cs:140 comment will cite the Director's visual finding instead.
REJECT deleting the parser or per-scene gating: scope creep, and unconditional
"no fog" is the Director's order. Full answers:
scratch/taskmaster-nov22/REVIEW-DISPOSITIONS-AND-FIX-SPEC.md.
REGENERATED FRAME (actual compiled file, not in-memory): visual/
RENDER-FINAL-regenerated.png, mean 47.21 / stddev 26.0; asset_compile succeeded,
open_scene loaded mergev2, active light reads Intensity 8 from the file.

REGENERATED COMPILED FRAME 2026-11-24: confirms NO fog, NO haze, NO flat grey
wall; edges are crisp. NO shadow claim is made (two vision reads disagree).
Rendered from the actual compiled `imported/school/scenes/mergev2.scene` with
`asset_compile full:true` and the active light reading Intensity 8 from the file.
Full verdict: visual/REGENERATED-FRAME-VERDICT.md. The FOG symptom is gone.

## Clarification Q&A

ANSWERED (Director, 2026-11-24): verbatim symptom above, plus "1. I AM ABANDONING HDRP SUPPORT FOR THIS PROJECT. So if it's HDRP specific, leave it alone. ... 4. I can tell you now that some tasks are definitely NOT HDRP related. Like the skybox. 5. I removed the fog myself and i got much closer to unity-sbox parity by increasing directional light intensity. So once you finally solve the fog situation, use this knowledge to help yourself get lighting parity." No questions pending; the /goal loop runs unattended. Do not ask the Director.

## Relevant History 

See eliots-agentics/archive/15 Fog Doesn't Match.md and scratch/taskmaster-nov22/visual/CLEAN-PERCEPTUAL-GATE.md. The grey height-fog wall (HeightColor alpha 1 = full opacity per engine Fog.cs:172-175) was removed at e9e71cf; the residual constant-grey distance fog is this task's scope.

## Stylistic Task Notes:
References to variables like X and Y automatically imply that these are to be exposed properties for developer tweaking.
When programming, portability and DRY are a priority.

## Post-Task Follow Ups 
