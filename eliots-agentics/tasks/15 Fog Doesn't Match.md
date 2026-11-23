## Status: STARTED? YES! CLARIFIED? YES! HISTORY? YES! REVIEWED? YES! DONE? YES! WORKER: sa-0-13030eee OTHER: reviewed 16:06 no further changes; opaque height-fog wall removed (HeightColor.a=1 swatch alpha), 6 headless gates PASS, rendered before/after shows wall gone (0->177 grey levels); residual distance-fog desaturation is a separate symptom tracked in tasks 16/17/18; perceptual matches-Unity remains Director's observation

## Task
Fog in sbox doesn't match unity fog (is there one in the unity scene?).

### Description
Director's verbatim symptom (2026-11-22): "Fog in sbox doesn't match unity fog (is there one in
the unity scene?)". Acceptance check: the imported MergeV2 / Test_Range scenes must show fog that
matches the Unity source's HDRP fog. The symptom is finished when it is GONE, not when a spec is
satisfied. Do not set DONE? YES! while it is still reported. The parenthetical is part of the
symptom and must be answered with evidence, not assumed.

Split fresh 2026-11-23 from task 15 "Scene Visual Fidelity" (closed b11952e), which was a
verification-only task that compared generated fog to our own converter formulas and concluded
"no headless gap". That is self-consistency, common.md forbids it as a verdict, and the Director
reports the symptom is still present. The old four-issue table is in ## Relevant History for
reference only; do not reuse its conclusion.

Unity source: C:/cseliot/1-Development/4-Unity/2-Projects/old_school_model. Generated scenes:
Assets/imported/assetpackschoolnameverylong/schoolhdrpnameverylong/scenes/{mergev2,test_range}.scene.

Required method:
1. For EACH candidate mechanism, write down the named observation that would FALSIFY it. No
   "investigated" verdict without both a mechanism and its falsifier.
2. ANSWER "is there one in the unity scene?" with source evidence: read `RenderSettings.m_Fog`
   and the HDRP Volume fog overrides in both source scenes and both Volume profiles. Earlier
   research recorded `m_Fog: 0` (no legacy fog) but HDRP Volume fog exists; confirm on disk.
3. Candidate mechanisms (verify, do not assume):
   a. `Editor/Core/UnitySceneExport.cs:284-297` emits `Sandbox.Fog` with `MaximumOpacity = 1`,
      `ColorSource = Constant`, `Color 0.5,0.5,0.5,1`, `Mode Exponential`, `HeightEnabled true`,
      `AttenuationDistance 7874` (mergev2) / `15748` (test_range), `HeightGradientLength 7874` /
      `1968.5`, `HeightOffset 0`. A constant grey at maximum opacity is a full-screen grey wash,
      which matches the flat medium-dark-gray viewport captured 2026-11-23
      (scratch `taskmaster-nov22/mergev2-viewport.png`). Falsifier: with a copy of the scene
      where the Fog MaximumOpacity/ColorSource is changed, capture a before/after screenshot;
      if the wash and hidden geometry do not change, the fog block is not the cause.
   b. MergeV2 has TWO global priority-0 weight-1 Volumes (`Scene Settings`, then
      `Volume Settings`); last-write wins. Wrong profile tie-break could emit fog from the wrong
      profile. Falsifier: read both source profiles' fog overrides and compare which values the
      importer actually emitted.
   c. `Sandbox.Fog` has no volumetric mode; HDRP `enableVolumetricFog 1` / `meanFreePath 400` /
      `maximumHeight 50` cannot be represented. This is an engine ceiling, but note the
      DIFFERENCE between "cannot represent volumetric" and "chose maximum-opacity constant grey
      that hides the scene". The latter is our emitted value, not an engine limit. Falsifier: an
      engine-authored `.scene` Fog block with a lower MaximumOpacity / `SceneSky` ColorSource.
   d. Generated Fog has no `__version`; engine upgrade paths can re-default it on load, so the
      file value may not be what the editor shows. Falsifier: diff the generated Fog key-set and
      `__version` against engine-authored `Sandbox.Fog` blocks (see
      `references/engine-schema-conformance.md`).
4. Differential ablation: `Assets/imported/polygoncity/scenes/demo.scene` (city--1) is a WORKING
   case from the same importer. Enumerate what differs in the fog/atmosphere system between it
   and MergeV2 (presence of Fog, opacity, colour source, SkyBox2D lighting) and identify which
   difference would cause the symptom. A component's values are not the cause when the same
   values render correctly in another scene.
5. Screenshot evidence: no editor is currently up (both installs' `--list` return `[]`); launch
   the editor belonging to the install you use, read `editor_status` BEFORE capture, do not
   save/play/change if `SceneHasUnsavedChanges` is true. Capture the viewport, copy the PNG to
   scratch, downscale to ~960x540 JPEG, and LOOK at it with vision_analyze. Before/after pair
   around any fix. Measure brightness/fog with pixel numbers, not adjectives. State the bridge
   and evidence used.
6. If a mechanism survives, fix the ROOT CAUSE in `Editor/Core/*.cs`, fix the CLASS. If all are
   falsified, say so with observations and name the next candidate; do not close on
   self-consistency. Do not file an objective data defect as an `Open:` editor-visual check.
7. Regression gates IF code changes land: harness build 0/0; `building.unitypackage` (24 files)
   and `city--1.unitypackage` (1,166 files) byte-identical to fresh HEAD baseline (no --scenes,
   BOTH trees populated); `verify_output.py <outRoot> <outRoot>/imported` 0 problems; two
   identical runs byte-identical; BOM-less; no stray artifacts.
8. Put all evidence in scratch files and cite absolute paths in ## Notes.

Stage by explicit path only. No push until the Master verifies. Do not add unrequested tooling.
Final answer in English.

### Work Review

2026-11-23 16:06 - Verdict: no further changes

1. [Medium] Residual far-field grey/desaturation is a genuinely separate symptom, NOT task 15 scope — `Editor/Core/UnitySceneExport.cs:312-321` (unchanged `MaximumOpacity=1`, `ColorSource=Constant`, `Color 0.5,0.5,0.5,1`) and `Editor/Core/UnityVolume.cs:397-401`. I looked at the frames myself: `00_baseline_wall_CAM.jpg` is a flat uniform grey field with no readable geometry, no sky and no colour; `10_fixed_emitted_CAM.jpg` shows a receding ceiling slab, columns, strip lights, floor and horizon — geometry is clearly visible, so the wall is GONE — but the sky is flat grey and the frame near-monochrome; `11_fixed_max03_CAM.jpg` shows a blue-grey sky with a visible gradient. The wall is the objective defect and the concrete manifestation of the reported symptom, and its removal is backed by the live rendered before/after. The remaining desaturation has no oracle: bounding `MaximumOpacity` would be a pinned magic value (forbidden as an oracle) and `ColorSource=SceneSky` discards Unity's authored custom grey (`colorMode 1`). Fix: NONE in task 15 — the Director must re-import/reload and judge in-editor, then resolve within tasks 16/17/18. Non-blocking for this verdict.

2. [Low] Perceptual "matches Unity" is not certified here — `eliots-agentics/tasks/15 Fog Doesn't Match.md` (Notes honesty caveat). No Unity reference render exists, and the before/after frames set Fog values on the stale editor-resaved working-tree scene (mtime 02:09) with extra properties, not a pixel-exact render of the clean generated output. Fix: Director re-import + reload and compare with a Unity reference before any perceptual closure; do not mark DONE? YES! on headless gates alone. Non-blocking.

3. [Low] Scope hygiene — `Editor/Core/UnitySceneExport.cs` / `Editor/Core/UnityVolume.cs`: the only changes in the two reviewed files are the HDRP fog block (`HeightEnabled=false`, six inert `Height*` fields removed, alpha-semantics comment citing Fog.cs:172 vs :184) and the corrected UnityVolume Information note; the legacy RenderSettings fog path and all distance-fog values are untouched. Other modified files in `git status` belong to other tasks and were excluded. Fix: none.

Tree:
`git hash-object --path=Editor/Core/UnitySceneExport.cs Editor/Core/UnitySceneExport.cs` -> aafd7f32686ec91f0aed68681d2817d7d18e556b
`git hash-object --path=Editor/Core/UnityVolume.cs Editor/Core/UnityVolume.cs` -> 6dd6a72887d486ea230236d27f2c379ef88700e5

## Notes

MASTER LIVE-EDITOR DIFFERENTIAL DIAGNOSIS 2026-11-23 (root cause CONFIRMED, mechanism + falsifier):
Launched the locally compiled Dev editor on unityimporterplus.sbproj, captured the loaded
mergev2.scene via the Dev sbox-cli bridge, and ran three in-memory Fog ablation sweeps
(set_component + camera_screenshot, 28 variants total). Full evidence + tables +
looked-at frames: scratch/taskmaster-nov22/visual/LIVE-EDITOR-FOG-DIAGNOSIS.md.

Baseline main-camera frame: mean 114.74, stddev 0.51, 0 coloured pixels, 7 grey levels =
a flat uniform grey WALL. Editor viewport, scene main camera, and the active non-main
"Japanese School Camera" all render BYTE-IDENTICAL flat grey; scene has 4545 objects.
Fog ON = nothing readable; fog OFF = dimly-lit building interior (roof/beams/strip lights,
bright windows to pale sky, reflective floor) — looked at with vision_analyze.

MECHANISM (objective, engine-source oracle): engine Fog.cs:172-175 says HeightColor
"Alpha sets its maximum opacity independently of distance fog"; :184-187 says distance-fog
Color "Alpha does not control opacity"; :111-114 MaximumOpacity is the distance-fog cap.
Unity VolumeSettings.asset:120-152 authors fog color {0.5,0.5,0.5,a:1} — that a:1 is a
COLOUR-SWATCH alpha, NOT an opacity. Importer UnitySceneExport.cs:308-326 / :366-378 copies
that swatch into BOTH Color and HeightColor as "0.5,0.5,0.5,1"; with HeightOffset=0
(full opacity at/below world Z=0) and the camera near Z=0, the height-fog layer is a solid
opaque grey wall over the entire frame. HDRP height fog is an extinction/density modifier,
never a fully-opaque wall, and has no faithful s&box equivalent (importer already notes
volumetric fog "not reproduced"). Distance fog (MaximumOpacity, Color.a, AttenuationDistance,
StartDistance) is NOT the cause.

FALSIFIER (RUN, PASSED): dropping HeightColor.a 1 -> 0 in the live editor made geometry appear
(0 -> 177 grey levels); alpha 0.5/0.3/0.1 interpolate the wash monotonically. StartDistance
=-1e6 and negative HeightGradientLength did NOT clear it, matching the mechanism. All edits
were discarded (close_scene discardChanges + open_scene); final editor state clean, not
playing, SceneHasUnsavedChanges false.

CONSEQUENCE: this wall is the shared root cause of the fog/skybox/lighting/tree "doesn't match"
symptoms — the scene was invisible, so those could not be judged. It also FALSIFIES task 20's
camera premise. Fix target = do not propagate the Unity colour-swatch alpha into HeightColor.a
(bound height-fog opacity to a sensible value, or disable the height layer when it cannot be
faithfully mapped). Oracle caveat: no Unity-side reference render exists, so "matches Unity"
remains the Director's editor observation; what is objective is that the scene is currently
hidden and removing the wall reveals it.

WORKER CODE FIX + HEADLESS GATES 2026-11-23 (option (a); all six gates PASS):
Plan and exact gate numbers: scratch/taskmaster-nov22/visual/fogfix/{PLAN.md,GATES.md}; exact new
Fog block: .../NEW-FOG-BLOCK.txt; fog-field diff: .../diffs/old_school-tree.diff.

Chosen fix (a), justified from engine Fog.cs: :172-175 makes HeightColor.a the height layer's
max opacity independently of distance fog; :184-187 says distance Color.a is ignored; :111-114
MaximumOpacity caps distance fog only. Unity's colour {0.5,0.5,0.5,a:1} carries a COLOUR-SWATCH
alpha, not an authored opacity; its baseHeight/attenuation/maximumHeight are a density ramp with
no s&box opacity counterpart. Option (b) would require a pinned constant (forbidden), so the
HDRP path now emits only the faithful exponential distance fog: HeightEnabled=false and no inert
HeightColor/HeightColorSource/HeightExponent/HeightGradientLength/HeightOffset fields. Corrected
the UnityVolume.cs Information note that still claimed height fog was reproduced. Legacy
RenderSettings fog path and all distance-fog values untouched; code comment cites Fog.cs:172 vs
:184.

Gates (exact numbers in GATES.md): harness build 0 Warning(s)/0 Error(s); building 24/24 and
city--1 1166/1166 byte-identical to a fresh HEAD mirror baseline (diff -r --strip-trailing-cr,
0-byte diffs); old_school --scenes 2150 files, whole-tree diff shows ONLY the two scenes' Fog
fields changed (six height fields -> HeightEnabled=false); verify_output.py checked 874 files,
0 problems; two identical fixed runs 0-byte tree diff; BOM scan 0; no stray untracked artifacts.
Generated mergev2 Fog now has HeightEnabled=false, AttenuationDistance 7874,
Color 0.5,0.5,0.5,1 (Color.a ignored per Fog.cs:184), MaximumOpacity 1, Mode Exponential.

Head moved b06f29c -> b686595 (Master's commit) during the task; it changed only other task
.md files, and baseline Core blobs hash-match current HEAD. No editor render captured by this
Worker: visual confirmation is the Master's follow-up.

MASTER RENDERED VERIFICATION 2026-11-23 (the before/after test the process post-mortem said was
missing for 12h). Full numbers + frames + caveats: visual/RENDERED-VERIFICATION.md; driver
visual/verify_fix.py; frames visual/*_CAM.jpg. Set the live editor Fog to EXACTLY the fixed
harness-emitted block and captured real frames (set_component + camera_screenshot), then
discarded all edits (close_scene discardChanges + open_scene; editor left clean).
  baseline wall           : mean 114.74, std 0.51,  0 coloured px,   7 grey levels
  FIX emitted values      : mean  29.43, std37.51, 37 coloured px, 177 grey levels
  fix + MaximumOpacity 0.3: mean 29.19, colored 123562, 203 levels (blue-grey sky returns)
  fix + ColorSource=SceneSky: colored 128015
vision_analyze of the FIX frame: "3D geometry IS visible" (the grey wall is GONE), but the
render is "essentially MONOCHROME GREY" with a flat grey sky and no chromatic colour.

OBJECTIVE RESULT: the wall is gone; scene renders. This is the criterion the forensics said the
loop lacked ("what the Director sees changed from grey to scene"). Six headless gates + a real
rendered before/after now back task 15's objective defect fix.

RESIDUAL DEFECT (NEW, mechanism + falsifier, NOT in scope for task 15, do NOT close 16/17/18 on
it): distance fog at MaximumOpacity=1 with ColorSource=Constant + Color 0.5 grey still
desaturates the far field/sky. Falsifier RUN: MaximumOpacity 1->0.3 restores 123562 coloured px
and a blue-grey sky; ColorSource=SceneSky (engine DEFAULT, Fog.cs:181) restores 128015. A bounded
MaximumOpacity would be a pinned magic value (Director forbids pinned values as oracle); SceneSky
is principled but ignores Unity's authored custom grey (VolumeSettings.asset:120-125 colorMode 1).
That tradeoff belongs to tasks 16/17/18 and needs the Director's editor observation.

HONESTY CAVEAT: the verification set Fog values on the OLD editor-resaved working-tree scene
(mtime 02:09), which PREDATES all of today's fixes and carries extra editor-added properties
(SkyColor, ShowEndcap, MaxDistance, DistanceRange 0,4096) absent from the clean harness output;
set_component changed only the listed fields. So this is NOT a pixel-exact render of the clean
generated scene. The Director's editor is showing a STALE import; a re-import + reload is
required before any perceptual "matches Unity" judgement. No Unity reference render exists.
Darkness is SEPARATE: with fog FULLY OFF the scene is also dark (mean 17.48), so it is a
lighting/exposure question (task 17), not fog.

## Clarification Q&A

ANSWERED (Director, 2026-11-23 bedtime directive): "1. All those things we originally mentioned
that were accidentally 'only investigated' 'due to headless verification'. Should now all be
renamed according to our discussion earlier (i.e. skybox doesn't match. lighting doesn't match.
etc.) 2. Open back up the tasks related to these that were supposed to get the fixes in, and
rephrase and empty/move Post-Task details if there are any. These are now fresh. 3. If it makes
sense, create new tasks. 4. Be open to creating NEW tasks based on investigations based on
subagents, given all the work of our conversations today." No questions pending; the /goal loop
runs unattended. Do not ask the Director.


## Relevant History 

MASTER READ-ONLY FINDING 2026-11-23 (sharpens the mechanism, do NOT chase a non-bug): the emitted
`Color 0.5,0.5,0.5,1` is NOT a wrong-colour bug. Unity `.../Settings/VolumeSettings.asset:120-125`
has `colorMode: m_Value 1` with an explicit `color: m_Value {r:0.5,g:0.5,b:0.5,a:1}`, and
`Editor/Core/UnityVolume.cs:391-393` only maps to `SceneSky` when colorMode==0 AND no color
override; here both conditions fail, so `ColorSource = Constant` is faithful to the source.
`AttenuationDistance 7874` = authored `fogDistance 200 m` x 39.37 (`VolumeSettings.asset:144-145`),
also faithful. The remaining objective suspects are the two HARDCODED values with no source
counterpart: `MaximumOpacity = 1` (both emission sites in `UnitySceneExport.cs`) and the absence of
any volumetric-fog representation (a documented engine ceiling). A differential child noted that
constant 0.5 linear -> ~188 sRGB is inconsistent with the observed uniform ~115 frame, which
argues AGAINST fog-at-full-opacity being the cause of the flat frame. Task 15 must name a candidate
mechanism with a falsifier and distinguish "faithful value" from "importer choice" before any fix.
The clean combined gate (COMBINED-GATE-RESULT.md) shows task 19 did NOT touch fog colour/opacity.

Previously part of task 15 "Scene Visual Fidelity" (closed b11952e), verification-only. At HEAD
6a65cb1 the worker recorded: both RenderSettings `m_Fog: 0`; HDRP fog in Volumes —
`VolumeSettings.asset` ExponentialFog 200 m grey, `Scene Settings Profile MergeV2.asset` Fog
meanFreePath 400 / maximumHeight 50 / `enableVolumetricFog 1`; importer emits one `Sandbox.Fog`
per scene (`UnitySceneExport.cs:255-300`, `UnityVolume.cs:20-22,29-36`) with values 7874 / 15748;
`verify_visual.py` evidence.txt 155 lines, `CHECKS: all PASS`, `HEADLESS GAP FOUND: NO`;
critic verdict 2026-11-22 22:10 `no further changes`. Archived tasks 09/10/11 recorded no
volumetric fog and default-sky decisions as deliberate engine-capability ceilings.

WHY THIS IS NOT A FIX: the "no gap" verdict compared emitted values to what our own converter
was written to emit. The Director still reports fog doesn't match. The four `Open:` lines from
old task 15 were cleared on reopening and the fog symptom got its own task.

WORKER HISTORY NOTE 2026-11-23: the earlier "MaximumOpacity = 1 is the objective suspect" line
above is SUPERSEDED as a mechanism by the live-editor ablation (see Notes +
LIVE-EDITOR-FOG-DIAGNOSIS.md): MaximumOpacity=0 and StartDistance=-1e6 did not clear the grey
wall; HeightColor.a=1 -> 0 did. The implemented fix therefore removes the fabricated height
layer rather than changing MaximumOpacity, which stays 1 and is distance-only per Fog.cs:111-114.
The old "Height fog maps to Sandbox.Fog's height ramp" note was corrected in UnityVolume.cs.
Full before/after evidence: scratch/taskmaster-nov22/visual/fogfix/GATES.md.

## Stylistic Task Notes:
References to variables like X and Y automatically imply that these are to be exposed properties for developer tweaking.
When programming, portability and DRY are a priority.

## Post-Task Follow Ups 
