## Status: STARTED? NO! CLARIFIED? NO! HISTORY? NO! REVIEWED? NO! DONE? NO! WORKER: NONE OTHER: fresh backlog from resolve-errors-part-c (scene issue 5); Director Q5 = 2A

## Task
Verify scene object placement against the Unity source and fix any confirmed transform gap.

### Description
Created from the 2026-11-22 console-out-2 triage in `resolve-errors-part-c-research.md`
(scene issue 5). Full Unity-side investigation: scratch `taskmaster-nov22/part-c-unity.md`.
This task exists because the Director answered Q5 = 2A (phone reply 2026-11-22 21:31):
"give misplacement its own task with a headless source-vs-output placement comparison".

The Task Description says "Too many objects are misplaced, actually, instead, I'll list SOME
things that DO seem to be placed correctly". The six verified-as-correct objects (all
PrefabInstance `m_Name` overrides in the Unity scenes) are:
  Bulletin_Board_01_Snaps014, Wall_03, Eaves_03, Shrubs_01,
  Stairs_Middle_01_Snaps014 (1), Desk_01 (55).
The Unity source project is C:/cseliot/1-Development/4-Unity/2-Projects/old_school_model
(scenes MergeV2.unity and Test_Range.unity).

Verified conversion paths at HEAD 54ca0a6:
- `Editor/Core/UnityPrefabExport.cs:27` - 39.37 units/metre import scale.
- `:223-228` - Unity (Y up, Z forward, left-handed) -> s&box (Z up, X forward) basis change
  (z, -x, y); mirrored placements go through `_mirror_<axis>` variants, never negative scale.
- `:1388-1397` - prefab roots keep rotation/scale but write position 0 because s&box adds a
  prefab root's position on placement (explicit rationale in the source).
- `RootOf :587` builds a fresh transform (never mutate the shared `NoTransform`).
- Scene-side placement: `UnitySceneExport.cs`, `UnityPrefabExport.Position/Scale`.
- `UnityPrefabBindings` silently skips prefabs over 16 MiB.

Required work:
1. Build a HEADLESS source-vs-output placement comparison: for each placed PrefabInstance in
   MergeV2.unity and Test_Range.unity, compute the expected s&box position/rotation/scale from
   the Unity transform chain and compare against the generated `.scene` / `.prefab` output.
   Report matched / mismatched counts and the exact per-object deltas. Reuse the approach that
   succeeded for the 44 EnvmapProbe bounds comparison in task 11 (scratch-only script, not a
   committed checker).
2. If a systematic, reproducible transform gap is confirmed (e.g. a class of objects all off by
   the same offset/rotation), fix the root cause in the engine-free importer core. Fix the
   CLASS, not one object. If no headless gap is confirmed, do NOT invent one: report the
   evidence and leave the visual judgement as an `Open:` line below.
3. Preserve the six objects the Director says are placed correctly; prove they still match.
4. Regression gates: harness build 0 errors / 0 warnings; `building.unitypackage` (24 files,
   no --scenes) and `city--1.unitypackage` (1,166 files, no --scenes) byte-identical to a
   fresh HEAD-code baseline with BOTH trees populated; `verify_output.py <outRoot>
   <outRoot>/imported` 0 problems; two identical runs byte-identical; BOM-less; no stray
   artifacts. If a code fix lands, re-run the corpus comparison on the final bytes.
5. Report the comparison table and the before/after mismatch counts.

Honesty: visual placement in the s&box editor is NOT provable headlessly. A headless
source-vs-output comparison is evidence of importer correctness, not of what the Director sees
on screen. Per Director Q4 = 1A (phone reply 21:31), any remaining editor-visual check is
recorded as an `Open:` line in ## Post-Task Follow Ups and does not block DONE? YES!.

Do not add unrequested tooling. Stage by explicit path. No push until the Master verifies.

### Work Review


## Notes


## Clarification Q&A


## Relevant History 


## Stylistic Task Notes:
References to variables like X and Y automatically imply that these are to be exposed properties for developer tweaking.
When programming, portability and DRY are a priority.

## Post-Task Follow Ups 
	Open: <Director's editor-visual check that placement now looks right in the s&box editor>
