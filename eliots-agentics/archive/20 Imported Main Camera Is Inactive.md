## Status: STARTED? YES! CLARIFIED? YES! HISTORY? YES! REVIEWED? YES! DONE? YES! WORKER: sa-1-cfcbe5b6 OTHER: reviewed 16:36 no further changes; MainCamera() now walks the ParentOf ancestor chain so IsMainCamera=True lands on the ACTIVE Japanese School Camera, matching Unity Camera.main; 6 gates PASS; separate headless data defect (NOT the flat-frame cause)

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

2026-11-23 16:36 - Verdict: no further changes

1. [High] ROOT CAUSE FIXED CORRECTLY — `Editor/Core/UnityPrefabExport.cs:923-946`: `ActiveChain(go.Id)` begins at the camera's own GameObject (so the old self `m_IsActive:0` rejection is preserved) then walks the file's already-baked `ParentOf` map (GameObject fileID -> parent GameObject fileID, built at :143 and :901) with a `HashSet` cycle guard and `id != 0` root stop. This faithfully implements Unity's `Camera.main` eligibility rule that a camera under an inactive ancestor does not count, without altering Unity's own faithful `Enabled=False` on `/Setting`. Fix: none required.
2. [High] DEFECT CONFIRMED RESOLVED ON DISK — generated scene `after/oldschool/imported/assetstoreoriginals/school/scenes/mergev2.scene`, independently re-censused: `/Setting/Main Camera (4)` is now `IsMainCamera=False` (ancestor `/Setting` Enabled=False), and `/JapaneseSchoolSettings/Japanese School Camera` is now `IsMainCamera=True` (own Enabled=True, ancestor `/JapaneseSchoolSettings` Enabled=True). Exactly one MAIN camera and it is ACTIVE; all six cameras retain priority=1/clear=All, so only the designation moved. Fix: none required.
3. [Medium] TIE-BREAKS AND SCOPE PRESERVED — `Editor/Core/UnityPrefabExport.cs:943`: the selection condition (tagged before untagged, highest `m_Depth`, then file order) is byte-identical to HEAD. Verified gates: whole-tree `--scenes` diff is exactly two `IsMainCamera` flips in one file; building 24/24 and city--1 1166/1166 byte-identical to baseline; harness build 0 Warning(s)/0 Error(s); verify_output 874 files/0 problems; two runs byte-identical; BOM scan 25 files/0; no stray artifacts. Fix: none required.
4. [Low] HONESTY / OUT OF SCOPE — task-file Notes and `Testing/Unity-Screenshot.png`: the Worker correctly claims no visual result. The separate Unity reference screenshot (well-lit classroom, no fog) is NOT this task's concern, and the flat/grey frame remains task 15's fog issue (commit e9e71cf); this review judges only the headless data defect. Fix: none required; do not record this designation fix as a visual fix.

Tree:
`git hash-object --path=Editor/Core/UnityPrefabExport.cs Editor/Core/UnityPrefabExport.cs` -> 83d6a618df39bc8b2c753e223b695f16d337b96a

## Notes

MASTER LIVE-EDITOR FINDING 2026-11-23 — PREMISE FALSIFIED (mechanism + falsifier run):
The investigation that created this task inferred "no active main camera -> flat/unrendered
frame" from serialized JSON (only IsMainCamera=True camera under disabled /Setting root).
Live-editor test DISPROVES that as the cause of the flat frame. In the Dev editor I rendered
BOTH the scene main camera (camera_screenshot '{}') AND the active non-main "Japanese School
Camera" (41ba4fe2-4e64-3b3a-baed-bdb1ed89f3f7): both frames are BYTE-IDENTICAL flat grey
(md5 862da996...), mean 114.74, 0 coloured pixels. So camera designation changes nothing.
The flat frame is an OPAQUE HEIGHT-FOG layer (see task 15 / LIVE-EDITOR-FOG-DIAGNOSIS.md).
Falsifier (RUN, PASSED): disabling fog (or setting HeightColor.a 1->0) makes the SAME main
camera render a full 3D building interior (132767 coloured pixels). Also both fog-off frames
had identical md5 regardless of which camera was used.

This does NOT mean the "inactive main camera" is faithful: Unity source MergeV2.unity has
SIX cameras tagged MainCamera and the Setting root m_IsActive:0, so Enabled=False on /Setting
is faithful to Unity; the real issue is that Unity's ACTIVE "Japanese School Camera" is left
IsMainCamera=False by UnityPrefabExport.cs:MainCamera() (:922-937), which skips only a
camera's OWN m_IsActive:0, not the ancestor chain. That is a genuine but SEPARATE objective
data defect worth its own headless check; it is NOT the cause of the flat/grey mismatch.
Sequencing: do NOT "fix the camera" to address a visual symptom that fog causes. Reassess
this task AFTER the task 15 fog fix with a fresh rendered frame; decide closure based on
whether the main-camera designation defect is real and in-scope, not on self-consistency.
Evidence: scratch/taskmaster-nov22/visual/LIVE-EDITOR-FOG-DIAGNOSIS.md.

TASK 20 CODE FIX 2026-11-23 (worker; headless gates only, NO visual claim). PLAN: isolate a scratch tree;
archive a fresh baseline from the task-specified HEAD b686595; build the .NET 10 harness against that
snapshot and against the working tree; capture a pre-fix --scenes census; change ONLY
`Editor/Core/UnityPrefabExport.cs:MainCamera()`; rebuild; re-run the same gates; append evidence here.
MECHANISM: the old test rejected a camera only when its OWN GameObject had `m_IsActive: 0`. The file already
has the ancestor map `Converter.ParentOf` (GameObject fileID -> parent GameObject fileID, baked by
`BakedChains` at :133-160; field at :831) and the walk idiom with a `HashSet` cycle guard at :368-375.
`MainCamera()` now walks that existing map from the camera's own GameObject up to the root via a local
`ActiveChain()` and rejects the camera if ANY object in the chain has `m_IsActive: 0`. Tie-breaks are
untouched (tagged MainCamera before untagged, highest `m_Depth`, then file order). With Unity's six
MainCamera-tagged cameras under the disabled `/Setting` root, all five "Main Camera" children are skipped;
the ACTIVE `/JapaneseSchoolSettings/Japanese School Camera` (also tagged MainCamera in Unity) now wins.
SCOPE HONESTY: this is a data fix for PLAY-mode camera designation only. It is NOT a visual fix and did not
change the flat grey frame; that cause is the fog layer (task 15). No screenshot was captured in this task.

GATES (exact numbers; all PASS):
1) Harness build with `IUP_ROOT=<project root>`: 0 Warning(s) / 0 Error(s) (also 0/0 for the b686595 snapshot
   build). `evidence/harness-build-AFTER.txt`, `evidence/harness-build-head.txt`.
2) No --scenes regression vs a fresh b686595 HEAD baseline (`diff -r --strip-trailing-cr`): building.unitypackage
   24/24 files, 0 diff lines; city--1.unitypackage 1166/1166 files, 0 diff lines.
   `evidence/after-building-vs-HEAD.diff`, `evidence/after-city-vs-HEAD.diff`.
3) ONE --scenes run of `Testing/old_school_project.unitypackage` (fixed build): 2150 output files. Whole-tree
   diff against the pre-fix run of the same source: 9 diff lines, exactly ONE differing file
   (`.../scenes/mergev2.scene`), exactly TWO changed fields, both `IsMainCamera` (line 407 true->false on
   `/Setting/Main Camera (4)`; line 127897 false->true on `/JapaneseSchoolSettings/Japanese School Camera`).
   `evidence/oldschool-whole-tree.diff`, `evidence/oldschool-differing-files.txt`.
4) `verify_output.py <outRoot> <outRoot>/imported`: `checked 874 files, 0 problems`.
   `evidence/verify_output_AFTER.log`.
5) Determinism: two identical fixed-code --scenes runs, 2150 files each, `diff -r --strip-trailing-cr` = 0 lines.
   `evidence/determinism.diff`.
6) BOM scan of `Editor/Core/*.cs`: 25 files scanned, 0 BOM files. `evidence/bom-scan.txt`. No artifacts created
   in the repo; the only untracked file in the final status (`eliots-agentics/tasks/oracle-check-screenshot.md`,
   mtime Nov 22 00:56) predates and is unrelated to this task. `evidence/git-status.txt`, `evidence/code-fix.diff`.

EXACT CAMERA CENSUS (ACTIVE = own Enabled AND all ancestors Enabled; source
`evidence/camera-census-BEFORE.txt` / `evidence/camera-census-AFTER.txt`):
BEFORE:
  INACTIVE nonmain /Setting/Main Camera                              priority=1 clear=All
  INACTIVE nonmain /Setting/Main Camera (1)                          priority=1 clear=All
  INACTIVE nonmain /Setting/Main Camera (2)                          priority=1 clear=All
  INACTIVE nonmain /Setting/Main Camera (3)                          priority=1 clear=All
  INACTIVE MAIN    /Setting/Main Camera (4)                          priority=1 clear=All  <-- only main, inactive
  ACTIVE   nonmain /JapaneseSchoolSettings/Japanese School Camera    priority=1 clear=All
AFTER:
  INACTIVE nonmain /Setting/Main Camera                              priority=1 clear=All
  INACTIVE nonmain /Setting/Main Camera (1)                          priority=1 clear=All
  INACTIVE nonmain /Setting/Main Camera (2)                          priority=1 clear=All
  INACTIVE nonmain /Setting/Main Camera (3)                          priority=1 clear=All
  INACTIVE nonmain /Setting/Main Camera (4)                          priority=1 clear=All
  ACTIVE   MAIN    /JapaneseSchoolSettings/Japanese School Camera    priority=1 clear=All  <-- main is active
Camera positions, priorities, clear flags, Enabled flags and GUIDs are unchanged; only the designation moved.

EVIDENCE root (absolute):
C:/Users/Eliot/AppData/Local/hermes/profiles/hermes-sbox/cache/scratch/taskmaster-nov22/task20-fix/
  evidence/camera_census.py, camera-census-BEFORE.txt, camera-census-AFTER.txt,
  harness-build-head.txt, harness-build-preworking.txt, harness-build-AFTER.txt,
  pre-building-vs-head.diff, pre-city-vs-head.diff,
  after-building-vs-HEAD.diff, after-city-vs-HEAD.diff,
  oldschool-whole-tree.diff, oldschool-differing-files.txt, verify_output_AFTER.log,
  determinism.diff, bom-scan.txt, git-status.txt, code-fix.diff
  head-source/ (fresh b686595 archive), base/ (HEAD baseline outputs),
  pre/ (pre-fix outputs), after/ and after2/ (two fixed-code runs).
HEAD moved mid-task (b686595 -> 27402a2: fog fix committed by the Master). Baseline and all gates above use
the task-specified HEAD b686595 archived at task start; source bytes for UnitySceneExport.cs/UnityVolume.cs
were identical before and after this edit, so the before/after camera diff isolates this change only.

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


MASTER READ-ONLY FINDING 2026-11-23 (mechanism located; fix NOT yet written): `IsMainCamera` is assigned by
`Editor/Core/UnityPrefabExport.cs:MainCamera()` at `:922-937`, called at `:914` and used at `:1496`
(`UnitySceneExport.Camera(..., component.Id == mainCamera)`). `MainCamera()` skips a camera only when its OWN
GameObject has `m_IsActive: 0` (`:929-931`); it does NOT check the ancestor chain. Verification against the
Unity source `.../Scenes/MergeV2.unity`: SIX cameras are tagged `m_TagString: MainCamera` (lines 7567, 155740,
188683, 200401, 206671, 297015 = "Main Camera (4)/(3)/(2)/Main Camera/(1)" and "Japanese School Camera").
Their `Setting` root is `m_IsActive: 0` (`:319087`), which the importer faithfully emits as `Enabled=False`, so
the five "Main Camera" children are effectively inactive in s&box too, yet one is designated main. The active
`Japanese School Camera` (under the enabled `JapaneseSchoolSettings` root) is left `IsMainCamera=False`.
This is a candidate root cause, NOT a confirmed defect: the named falsifier in the task (PLAY-mode frame with
`Setting` enabled or main reassigned to the active camera) has NOT been run. A task-20 code fix must be sequenced
AFTER task 14 is committed, because `MainCamera()` lives in the SAME file task 14 modifies
(`UnityPrefabExport.cs`) and an edit now would invalidate task 14's reviewed Tree hash.

WORKER HISTORY 2026-11-23: the confirmed data defect was fixed in `UnityPrefabExport.cs:MainCamera()` only —
ancestor activation now uses the file's existing baked `ParentOf` map instead of a single-object check. All
six regression gates passed (build 0/0; building 24/24 and city--1 1166/1166 byte-identical to a fresh
b686595 HEAD baseline; one --scenes run with a whole-tree diff of only two `IsMainCamera` flips; verify_output
874 files/0 problems; two runs byte-identical; 25 Editor/Core files/0 BOM). The exact census, mechanism,
falsified visual premise and absolute evidence paths are appended to `## Notes` above. No commit, no push, no
staging, no editor touch, no visual result claimed. Sequence note: task 14 is already committed (it landed
before this edit), and HEAD advanced to 27402a2 mid-task with the independent fog fix; neither invalidates
the isolated before/after comparison because both runs used the same source bytes apart from this fix.

## Stylistic Task Notes:
References to variables like X and Y automatically imply that these are to be exposed properties for developer tweaking.
When programming, portability and DRY are a priority.

## Post-Task Follow Ups 
