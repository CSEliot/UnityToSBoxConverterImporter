## Status: STARTED? NO! CLARIFIED? YES! HISTORY? YES! REVIEWED? NO! DONE? NO! WORKER: NONE OTHER: NEW task from 2026-11-23 subagent differential-ablation investigation, per Director bedtime item 4

## Task
The imported scene has no active main camera.

### Description
NEW task created 2026-11-23 from a subagent investigation (Director bedtime item 4: "Be open to
creating NEW tasks based on investigations based on subagents, given all the work of our
conversations today"). This is an OBJECTIVE, headless-checkable structural defect, not an
editor-visual judgement. It is distinct from the five symptom tasks (14-18).

Verified on disk by the Master against the clean-HEAD harness output
`scratch/taskmaster-nov22/task15/out/imported/assetstoreoriginals/school/scenes/mergev2.scene`:

  INACTIVE nonmain pri 1 clear All | /Setting/Main Camera
  INACTIVE nonmain pri 1 clear All | /Setting/Main Camera (1)
  INACTIVE nonmain pri 1 clear All | /Setting/Main Camera (2)
  INACTIVE nonmain pri 1 clear All | /Setting/Main Camera (3)
  INACTIVE MAIN     pri 1 clear All | /Setting/Main Camera (4)   <-- only IsMainCamera=True
  ACTIVE   nonmain pri 1 clear All | /JapaneseSchoolSettings/Japanese School Camera

Root GO `Setting` is `Enabled=False`; `Sandbox.GameObject.Active` requires ALL ancestors enabled,
so the ONLY main camera is inactive. Six cameras all share `Priority=1` and `ClearFlags=All`.
The WORKING case `Assets/imported/polygoncity/scenes/demo.scene` (city--1) has exactly ONE
camera, root-enabled, `IsMainCamera=True`.

IMPORTANT honesty constraint, verified by the Master: Unity source
`.../SchoolHDRPNameVeryLong/Scenes/MergeV2.unity:319082-319087` has `m_Name: Setting` with
`m_IsActive: 0`. So the importer's `Enabled=False` on that root is FAITHFUL to Unity, not a
conversion error by itself. The defect, if any, is that the importer designates an inactive
camera as `IsMainCamera` while leaving the only active camera non-main, and emits six
same-priority `ClearFlags=All` cameras. Do NOT claim "Unity keeps Setting active" without
evidence.

Also verified by the Master, contradicting part of the child's report: the 2026-11-23 baseline
screenshot is NOT an empty/flat clear frame. Deterministic pixel map of
`scratch/taskmaster-nov22/mergev2-viewport.png` (1280x720): exactly three grey levels
{113: 47819 px, 114: 196575 px, 115: 677206 px}, mean 114.68, stddev 0.57; top 180 rows are
pure 115, while 113 pixels occupy rows 213-297 x 257-1279 and 114 pixels rows 284-719
x 262-1279. That is structured content with essentially ZERO tonal range. Two vision reads
disagree on what it depicts (one said a faint text-like overlay, a contrast-stretched read said
a building with railing/windows/tree canopies). Do NOT claim either semantic content without a
crop or pixel measurement; record the pixel facts as the durable evidence. Because that frame was
captured with `editor_camera_screenshot` while `IsPlaying=false`, it uses the EDITOR viewport
camera, so scene-camera configuration does NOT explain it. The camera defect is a PLAY-mode
concern and must be tested that way.

Required method (no "verify and report no gap"):
1. State the candidate mechanism AND the named observation that would FALSIFY it. Mechanism
   candidate: in play mode, no active `IsMainCamera` -> no game camera output, and six
   `Priority=1`/`ClearFlags=All` cameras clear over each other -> flat frame. Falsifier: a
   PLAY-mode frame captured with `Setting` enabled (or with `IsMainCamera` moved to the active
   `Japanese School Camera`) that shows lit geometry, versus a default/empty shot that stays
   flat. If both look the same, the camera configuration is NOT the cause.
2. Determine from the Unity source which camera Unity actually renders in game view (the
   `JapaneseSchoolSettings/Japanese School Camera` is the likely candidate) and read its
   `m_TagString`/MainCamera tag. Compare to the tag the importer used to set `IsMainCamera`.
   This is an objective, headless-checkable data question.
3. Only if a reproducible defect is confirmed, fix the ROOT CAUSE in the engine-free importer
   core (`Editor/Core/UnitySceneExport.cs` camera emission): keep Unity's active/inactive state
   faithful, but ensure the emitted main camera is the one that can actually render, and do not
   emit six identical-priority clearing cameras from a disabled settings group. Fix the CLASS.
   Do NOT simply force every camera enabled (that would break fidelity).
4. If a code fix lands, capture a before/after PLAY-mode screenshot pair from the editor, read
   `editor_status` BEFORE capture, do not save/play/change the scene if
   `SceneHasUnsavedChanges` is true, downscale to ~960x540 JPEG and LOOK at it with
   vision_analyze. Measure brightness with pixel numbers, not adjectives. State the bridge and
   evidence used. If no editor is up, say so plainly rather than fabricating a frame.
5. Regression gates IF code changes land: harness build 0/0; `building.unitypackage` (24 files)
   and `city--1.unitypackage` (1,166 files) byte-identical to a fresh HEAD baseline (no
   --scenes, BOTH trees populated); `verify_output.py <outRoot> <outRoot>/imported` 0 problems;
   two identical runs byte-identical; BOM-less; no stray artifacts.
6. Put all evidence in scratch files and cite absolute paths in ## Notes.

Do not set DONE? YES! on self-consistency, "no headless gap", or an `Open:` line. Stage by
explicit path only. No push until the Master verifies. Do not add unrequested tooling.
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

Created 2026-11-23. Origin: a read-only differential-ablation subagent (`sa-1-fbca556e`, batch
`deleg_06d56c39`) compared the FAILING MergeV2/Test_Range scenes to the WORKING city--1
`demo.scene` from the same importer, using a recursive json.load census (not grep -o). Its
strongest structural differential was the camera hierarchy above. Its census table is at
`scratch/taskmaster-nov22/research-diff/diffsummary.md` and the GO hierarchy at
`research-diff/hierarchy-cameras.txt`. Its single deliverable report `differential-report.md`
was NOT written before it hit the 40-call cap; the Master verified the key claims directly
against the clean-HEAD harness scene and the Unity source instead.

The same batch's other two children: `sa-0-bb00b315` (task 19 schema conformance) produced
oracle evidence and falsified several premises (folded into tasks 16, 17, 19); `sa-2-32793b5e`
(task 14/18 research) found a confirmed placement root cause (folded into task 14). All three
were truncated at the iteration cap.


## Stylistic Task Notes:
References to variables like X and Y automatically imply that these are to be exposed properties for developer tweaking.
When programming, portability and DRY are a priority.

## Post-Task Follow Ups 
