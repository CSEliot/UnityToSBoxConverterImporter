## Status: STARTED? YES! CLARIFIED? YES! HISTORY? YES! REVIEWED? YES! DONE? YES! WORKER: sa-0-e185acdc OTHER: reviewed 14:32 no further changes; WrapperRootTransform recovers stripped nested-prefab root position overrides, 17 MISMATCH->0 with 137 OK unchanged, independently Unity-YAML-derived; clean combined gate PASS; building/city--1 byte-identical

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

2026-11-23 14:32 - Verdict: no further changes

1. [High] `Editor/Core/UnityPrefabExport.cs` — fix logic verified, no defect. `WrapperRootTransform()` walks
   `ModificationPattern.Matches` in YAML order, skips non-`m_Local*` and `m_LocalEulerAnglesHint`, requires the
   modification target's guid to equal the wrapper instance's own `m_SourcePrefab` guid, and returns that target's
   `fileID`; with no such override it falls back to `ModelRootTransform` (400000). On disk in
   `Windows_01_Snaps014.prefab` the model instance's `m_Local*` target is fileID 400004 (fbx guid
   20078532efa84644c9e7d0c27dde24ce) and the stripped `&3402674832240474118` Transform block resolves via
   `m_CorrespondingSourceObject` to that same 400004 — so `ById.ContainsKey` is true and only the new disjunct
   recovers the override. That disjunct additionally demands `Type: "4" or "224"` + `Stripped: true` + corr id ==
   `WrapperRootTransformId`; a child transform carries a different corr id (e.g. 400002), and the
   `childTransforms` branch requires `Stripped: false`, so no child override is captured or stolen. `localPath`
   preserves the euler-hint exclusion. Fix: none.

2. [High] Symptom observed independently, not self-consistency: before the fix the 17 nested stubs carried
   `Position = (0,0,0)` while their Unity YAML `m_LocalPosition` values are non-zero. I re-derived two from the
   Unity source prefabs myself, not from importer output: `Windows_01_Frame_Snaps014` in
   `Restroom_Wall_Window_01_Snaps014.prefab` src `[-11.5, 1.1, 0.1]` m -> expected `[3.937, 452.755, 43.307]`;
   `Windows_04_Frame_Snaps014` in `Floor_Stairs_Middle_01_Snaps014.prefab` src `[-1.5, 3.19, -8.2555]` ->
   `[-325.019035, 59.055, 125.5903]` (39.37 x (z,-x,y)). Both match `proof_17_before_after.txt`. Census
   137 OK / 17 MISMATCH -> 154 OK / 0 MISMATCH, all 17 |delta| 0.000; `check_six.log` CHANGED 0 / MISSING 0 /
   ADDED 0; `verify_output_after2.log` 874 files / 0 problems; building and city--1 0-byte diffs; Master clean
   combined gate PASS. Fix: none.

3. [Medium] No viewport screenshot was captured, so no perceptual claim is made — the task's method 4 asked for a
   before/after capture and none exists on disk (only imported texture PNGs). The Notes disclose this honestly;
   the verdict rests on the objective dropped-override / hardcoded-zero data defect, which is headless-checkable,
   not on serialized-output self-consistency. Mechanisms b (rotation/scale basis) and d (mirror position) remain
   explicitly UNTESTED in the Notes. Fix: none — Director retains the final visual check; do not set DONE? YES!
   from this review.

4. [Low] Mirror variants verified as legitimate downstream, not collateral: the real generated trees have 15 -> 21
   `_mirror_*` prefabs (6 new, all valid JSON), exactly those 6 appearing as `Only in ...` in
   `clean-combined.diff`. A property-by-property comparison of every non-mirror prefab stub HEAD vs FINAL shows
   only 17 Position, 4 `__Prefab`, 2 Rotation and 1 Scale changes, all inside the 17 repaired stubs; the 137
   previously-correct stubs are unchanged in Position/Rotation/Scale/Name/Enabled. The Rotation/Scale changes are
   the pre-existing `Unmirror` path now receiving the restored negative/composed scale. Fix: none.

5. [Low] Latent edge, no current impact: `WrapperRootTransform` returns the FIRST `m_Local*` target id; 7 wrappers
   in the source corpus address several model transform ids (e.g. 400000 + 400002 + 400004). All 7 address 400000
   first and none is the source of any of the 154 nested stubs, so no placement was mis-attributed here. The
   `wrapperRoot` disjunct is class-shaped: no list of the 17 names appears anywhere in the diff. Fix: none.

6. [Low] Evidence-provenance caveat: scratch `probe_nested_census.py` documents its basis as "independently fitted
   ... (see basis_fit.py)", but `basis_fit.log` shows that script crashed (`LinAlgError`, 0 six-object pairs).
   The reviewed task Notes do not repeat that claim — they state the (z,-x,y) x 39.37 basis, which is also given
   in this task's Description — so no false claim entered the task file. Fix: none (optional scratch-doc fix).

7. [Low] Template/state check: 7 Status fields, headings at column 0, no placeholders, one `### Work Review`, no
   `Open:` lines under Post-Task Follow Ups. I did not edit the Status header (REVIEWED? / DONE? remain NO!, as
   the Master owns it), did not change code, and did not commit. Fix: none.

Tree: Editor/Core/UnityPrefabExport.cs
  git hash-object --path=Editor/Core/UnityPrefabExport.cs Editor/Core/UnityPrefabExport.cs
  = 7d8f9c5bc03b460732a75bcfa7466ea705a294d2
Tree: eliots-agentics/tasks/14 Objects Are Misplaced.md
  git hash-object --path="eliots-agentics/tasks/14 Objects Are Misplaced.md" "eliots-agentics/tasks/14 Objects Are Misplaced.md"
  = c0b8098afdaf9c47be597f007609952cc48cd578 (worker state before this entry; post-append hash is in the Critic JSON)

## Notes

PLAN (worker, 2026-11-23): fix the confirmed class defect in `Editor/Core/UnityPrefabExport.cs`, prove it with an
independent Unity-YAML-derived nested-position census, and run the regression gates. Not committed; Master owns
Status and the commit.

MASTER CLEAN COMBINED GATE (2026-11-23, supersedes the contaminated per-worker corpus runs): because task 14 and
task 19 landed concurrently, the Master ran ONE clean verification of the FINAL tree (both fixes) vs a fresh
`git archive HEAD` baseline in LF-normalized isolated mirrors. Result PASS: old_school --scenes HEAD 2144 files /
FINAL 2150 files; the 82160-byte combined diff is EXACTLY the union of the two fixes (11 changed prefabs + 6 new
mirror variants for task 14; 2 scenes with only LightColor/Intensity/__version/SkyColor/DistanceRange for task 19);
building 24/24 and city--1 1166/1166 byte-identical (0-byte diffs); verify_output `checked 874 files, 0 problems`;
two identical FINAL runs 0-byte determinism diff; 0 BOM files. Full evidence and the diagnosis of the transient
`channel extraction failed` retry: `C:/Users/Eliot/AppData/Local/hermes/profiles/hermes-sbox/cache/scratch/taskmaster-nov22/combined/COMBINED-GATE-RESULT.md`.

MECHANISM (candidate): `:1064-1067` hard-codes a model wrapper's root transform to synthetic 400000. The wrapper
fallback in `Instance()` only accepted a root override whose target id was ABSENT from `plain.ById`. Unity instead
addresses the wrapper's stripped root Transform (a real block in the source prefab), so `ById.ContainsKey` is true
and the `m_LocalPosition.*` parse was never reached. FALSIFIER (named before the fix, then applied): re-derive
every nested stub's expected position from the Unity prefab YAML alone (basis change metres x 39.37, `(z,-x,y)`),
key `ById` from the nested instance's own `m_SourcePrefab` (NOT the containing prefab), and check whether exactly
the mismatches are stripped type-4 Transforms resolving to the wrapper's model root while the others target an
absent id. Observation: 17/17 mismatches were `(inPlainById=True, type=4, stripped=True,
m_CorrespondingSourceObject == 400004 or 400008)`, 137/137 OK were `inPlainById=False`. The model root is not
always 400000 — multi-root FBXes use 400004/400008 — so hard-coding 400000 cannot work. This is a non-circular,
class-shaped signal and it survived the falsifier.

FIX (on disk, unchanged since; no special-casing of the 17 names): added `WrapperRootTransform( Block, string )`
next to `RootOf`, which reads the model-root transform fileID from the wrapper instance's own `m_Local*` override
target (falls back to 400000); stored it as `Source.WrapperRootTransformId`; and extended the root test to accept a
stripped `4`/`224` block whose `m_CorrespondingSourceObject` equals that id, while keeping the original absent-id
fallback and the `m_LocalEulerAnglesHint` exclusion intact. Positions, rotations and scales now reach the single
generated root through the existing parse and `Unmirror` path.

17 -> 0 PROOF (independent expected values from Unity YAML, never from importer output):
BEFORE `nested stubs: 154, Counter({'OK': 137, 'MISMATCH': 17})`; AFTER `nested stubs: 154, Counter({'OK': 154})`.
Cross-tab BEFORE `{('OK',(False,None,None,False)):137, ('MISMATCH',(True,'4',True,True)):17}`; AFTER
`{('OK',(False,None,None,False)):137, ('OK',(True,'4',True,True)):17}`. All 17 previously-zero positions now match
the independently derived expectation at |delta| 0.000 (worst before: Windows_01_Frame_Snaps014 454.839, exp
`[3.937, 452.755, 43.307]`, got `[0,0,0]` -> got `[3.937, 452.755, 43.307]`; full 17-row table with exact src
local, expected, before/after deltas: `.../task14-fix/proof_17_before_after.txt`). Logs:
`.../task14-fix/classify_targets2.log` (before), `.../task14-fix/census_after.log` (after),
`.../task14-fix/classified_targets_before.json` and `_after.json`.

SIX DIRECTOR-NAMED OBJECTS PRESERVED: `check_six.py` matched all 81 scene stubs carrying those six names by
`(scene, __guid, name)` between the known-good task15 output and the post-fix output; `CHANGED: 0`, `MISSING: 0`,
`ADDED: 0`. Log: `.../task14-fix/check_six.log`. This is a preservation identity check, not a correctness oracle.

GATES (all real, logs in `.../task14-fix/`): harness build on the fixed tree 0 Warning(s)/0 Error(s)
(`harness-build-after.txt`); fresh HEAD-code baseline tree built from LF sources also 0/0
(`harness-build-baseline.txt`); `building.unitypackage` 24/24 files and `city--1.unitypackage` 1166/1166 files
byte-identical to that baseline with both trees populated, `diff -r --strip-trailing-cr` 0 bytes for each
(`runs/building.diff`, `runs/city--1.diff`); `verify_output.py <outRoot> <outRoot>/imported` -> `checked 874 files,
0 problems`, exit 0 (`runs/verify_output_after2.log`); BOM scan of `Editor/Core/*.cs` -> 0 BOM files; repo status
shows only `M Editor/Core/UnityPrefabExport.cs` and the concurrent task-19 worker's `M
Editor/Core/UnitySceneExport.cs` (untouched, unstaged), no stray untracked artifacts. Post-fix old_school
`--scenes` run: `archive assets: 1299, assets=1277 converted=971 files=2150` (`runs/after2.log`); exit 1 is the
importer's pre-existing unresolved-issues convention, not a harness failure.

HONESTY / LIMITS: no editor screenshot was captured and no visual or perceptual conclusion is claimed here — the
verdict rests on the independent YAML-derived position evidence above. Actual HEAD is `930f9e4d` (the brief said
`f93d1ee`); the concurrent `UnitySceneExport.cs` edit predates this worker and was not modified, staged, or built
into the baseline. DONE? is deliberately left to the Master after review.

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

WORKER 2026-11-23 (this round, recorded in ## Notes above): the reopened symptom's confirmed class defect is
now fixed in `Editor/Core/UnityPrefabExport.cs` — a model wrapper's stripped root Transform (resolving to the FBX
root id 400004/400008, not always 400000) was not recognised as a root override target, so 17 nested position
overrides were silently dropped. Independent Unity-YAML census moved 137 OK / 17 MISMATCH to 154 OK / 0 MISMATCH,
all 17 |delta| now 0.000; all six Director-named objects unchanged. Regression gates passed; details and absolute
evidence paths are in `## Notes`. No screenshot was captured, so no visual verdict is claimed.


## Stylistic Task Notes:
References to variables like X and Y automatically imply that these are to be exposed properties for developer tweaking.
When programming, portability and DRY are a priority.

## Post-Task Follow Ups 
