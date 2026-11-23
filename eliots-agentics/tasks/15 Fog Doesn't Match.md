## Status: STARTED? YES! CLARIFIED? YES! HISTORY? YES! REVIEWED? NO! DONE? NO! WORKER: NONE OTHER: fresh symptom task; split from investigated-only task 15; differential report on disk; fog wash (MaximumOpacity 1 / Constant grey 0.5) is strongest candidate; baseline frame is 3 grey levels, not empty

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


## Notes


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


## Stylistic Task Notes:
References to variables like X and Y automatically imply that these are to be exposed properties for developer tweaking.
When programming, portability and DRY are a priority.

## Post-Task Follow Ups 
