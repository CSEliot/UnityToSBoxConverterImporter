## Status: STARTED? YES! CLARIFIED? YES! HISTORY? YES! REVIEWED? NO! DONE? NO! WORKER: NONE OTHER: fresh symptom task; reopened 2026-11-23 per Director bedtime instructions; read-only placement research (sa-2-32793b5e) in flight

## Task
Too many objects are misplaced.

### Description
Director's verbatim symptom (2026-11-22): "Too many objects are misplaced". This is the one
acceptance check for this task: in the imported MergeV2 / Test_Range scenes, objects must sit
where the Unity source puts them. The symptom is finished when it is GONE, not when a spec is
satisfied. Do not set DONE? YES! while it is still reported.

Reopened fresh 2026-11-23. The previous round (task 14 "Scene Placement Comparison", closed at
756fcb4) compared generated transforms to our OWN converter formulas and got 3062/3062
"matches". That is self-consistency, not correctness: common.md forbids treating a HEAD-baseline
or own-formula comparison as a verdict, and forbids "no headless gap" as a closing statement.
The old notes are preserved in ## Relevant History for reference only; do NOT reuse their
"no confirmed gap" conclusion as this task's answer.

Unity source project: C:/cseliot/1-Development/4-Unity/2-Projects/old_school_model
(scenes .../AssetPackSchoolNameVeryLong/SchoolHDRPNameVeryLong/Scenes/MergeV2.unity and
Test_Range.unity). Generated output: Assets/imported/assetpackschoolnameverylong/
schoolhdrpnameverylong/scenes/mergev2.scene and test_range.scene.

Required method:
1. For EACH candidate mechanism below, write down the named observation that would FALSIFY it.
   No "investigated" verdict without both a mechanism and its falsifier.
2. Candidate mechanisms (verify, do not assume):
   a. CONFIRMED ROOT CAUSE 2026-11-23 (Master verified against scratch
      `research-placement-trees/probe_nested_census.log` and `Editor/Core/UnityPrefabExport.cs:1067`,
      `:1566-1570`): 17 named nested PrefabInstances are emitted at their parent's origin
      (generated `PropertyOverrides.Position = (0,0,0)`) with NON-ZERO Unity local positions.
      Worst case `Windows_01_Frame_Snaps014` |delta| 454.839 s&box units. Affected classes:
      Windows_01/02/03/04/05/06/07_Frame_Snaps014, Door_05_Frame_Snaps014,
      Door_Frame_04_Snaps014 across 13 wall/stair prefabs. Mechanism: `:1067` hard-codes
      `RootTransformId = ModelRootTransform (400000)` for model wrappers; the rootTransform test
      at `:1566-1567` (`target == plain.RootTransformId || plain.IsWrapper &&
      !plain.ById.ContainsKey(target) && path.StartsWith("m_Local")`) fails when the override
      targets an EXISTING stripped Transform id in the file (e.g. 1073696690906165838), so the
      `m_LocalPosition.*` override is silently dropped. Census: 154 nested stubs compared,
      137 OK / 17 MISMATCH. Falsifier (already applied): the probe re-derived expected positions
      independently from Unity YAML via the basis change, not from the importer's own formulas,
      and the 17 generated values are exactly zero while the 137 others match. This is the FIX
      target — fix the CLASS, not one object.
   b. Basis change (z,-x,y) at `UnityPrefabExport.cs:223-228` applied to positions but not to
      rotations / scales for some class. Falsifier: a class of non-identity-rotation objects
      whose generated rotation differs from an independent Unity-chain computation. UNTESTED.
   c. FALSIFIED 2026-11-23: `UnityPrefabBindings` silently skips prefabs over 16 MiB — 0 of 346
      source prefabs exceed 16 MiB (largest 188,066 bytes), so this cannot explain the gap.
   d. `_mirror_<axis>` variants replacing negative scale may flip a position. Falsifier: a
      mirrored instance's left/right position compared to source. UNTESTED.
3. Differential ablation (no absolute oracle): `Assets/imported/polygoncity/scenes/demo.scene`
   (city--1) is the WORKING case from the same importer. Enumerate what differs in the placement
   path between it and MergeV2 (nested prefab depth, parent chains, mirrors, >16 MiB prefabs)
   and identify which difference would cause the symptom.
4. Screenshot evidence. No editor is currently up (both installs' `--list` return `[]`), so
   launch the editor belonging to the install you will use, read `editor_status` BEFORE capture,
   and do not save / play / change the scene if `SceneHasUnsavedChanges` is true. Capture the
   viewport, copy the PNG into scratch immediately, downscale to ~960x540 JPEG, and LOOK at it
   with vision_analyze. Capture a before/after pair around any fix. Corroborate any specific
   object-position claim with a crop or a pixel measurement; vision models invent detail on flat
   frames. State which bridge and which evidence you used.
5. If a mechanism survives, fix the ROOT CAUSE in the engine-free importer core (`Editor/Core/
   *.cs`), fix the CLASS, not one object. If all are falsified, say so with the observations and
   name the next candidate; do not close on self-consistency.
6. Regression gates IF code changes land: harness build 0 errors / 0 warnings;
   `building.unitypackage` (24 files, no --scenes) and `city--1.unitypackage` (1,166 files,
   no --scenes) byte-identical to a fresh HEAD-code baseline with BOTH trees populated;
   `verify_output.py <outRoot> <outRoot>/imported` 0 problems; two identical runs
   byte-identical; BOM-less; no stray artifacts. Take the baseline in the same mode.
7. Put all evidence (comparison tables, scene excerpts, screenshots, pixel measurements, gate
   logs) in scratch files and cite their absolute paths in ## Notes. Do not claim the visual
   symptom is fixed from serialized JSON alone.

The Director's six objects named as placed correctly (preserve them):
Bulletin_Board_01_Snaps014, Wall_03, Eaves_03, Shrubs_01,
Stairs_Middle_01_Snaps014 (1), Desk_01 (55).

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

Previously task 14 "Scene Placement Comparison" (closed 756fcb4). A verification-only worker
`sa-0-9f3d0009` built a scratch `compare_placement.py` that re-derived expected s&box transforms
from Unity YAML and compared to generated scene stubs: 3062/3062 PrefabInstances matched
(MergeV2 2861, Test_Range 201), zero mismatches, and all six Director-named objects at exact
zero deltas. Critic verdict 2026-11-22 22:15 `no further changes`. Regression gates were skipped
because no code changed. Evidence: scratch `taskmaster-nov22/task14/compare_placement.py`,
`compare_placement.log`, `placement_report.json`, `probe_structure.py`, `mirror/`.

WHY THIS IS NOT A FIX: the expected values were computed with the SAME conversion formulas the
importer uses, so the comparison proved self-consistency only. The Director still reports the
symptom. Per common.md ("Notes on task work (reported symptoms)") a symptom task is finished
when the symptom is gone; a "no headless gap" verdict is not a verdict. The four `Open:` lines
from the old task were cleared on reopening.


## Stylistic Task Notes:
References to variables like X and Y automatically imply that these are to be exposed properties for developer tweaking.
When programming, portability and DRY are a priority.

## Post-Task Follow Ups 
