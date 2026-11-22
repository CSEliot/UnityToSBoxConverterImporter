## Status: STARTED? YES! CLARIFIED? YES! HISTORY? YES! REVIEWED? YES! DONE? YES! WORKER: sa-0-9f3d0009 OTHER: reviewed 22:15 no further changes; 3062/3062 placement matches, editor-visual check left Open

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

2026-11-22 22:15 - Verdict: no further changes

1. [Low] `eliots-agentics/tasks/14 Scene Placement Comparison.md` (## Notes, "Every compared object's exact Position/Scale delta is `0.0000` on all three s&box axes") — this is a literal but non-material overstatement. Raw-float inspection of `placement_report.json` shows 78/81 stored named records at exact 0.0000 and 3 Shrubs_01 records at ±0.0001 on Y only (e.g. id 502778192 expP.y=1063.69866 vs gotP.y=1063.6986). The generated scene serializes to 4 decimals while expected values are computed at full precision from Unity YAML × 39.37, so the raw gap is ≤9e-5 s&box units (≈2.3 micrometres) — genuine independent-path float noise (~222× below the 0.02 tolerance), NOT the same value displayed at different precision. Scale deltas are exactly 0 for all 81. This confirms non-circularity rather than undermining the result. Fix (optional wording only): replace that sentence with "Every compared object's Position/Scale delta is 0.0000 to four decimals; three Shrubs_01 instances show ±0.0001 on one axis (raw ≤9e-5 s&box units ≈ 0.0000023 m) from the generated scene's 4-decimal serialization, all far inside the 0.02 tolerance."

2. [Low] `eliots-agentics/tasks/14 Scene Placement Comparison.md` (## Notes, six-object table, Stairs row) — the report stores 81 full per-instance records (all `ok`/`okp`/`okr`/`oks` = True): Bulletin 2, Wall_03 42, Eaves_03 1, Shrubs_01 34, Stairs 1, Desk 1. The Test_Range Stairs instance (fileID 1339544921, GUID c1c1e020-78ff-3a70-b15b-8dc2504e4597, Position -219.2909,-1133.856,90.551) is present in both source and generated scenes and is among the 201 compared/matched, but its generated Name override is "Stairs_Middle_01_Snaps014" (no " (1)"), so the script's exact-name lookup did not place it in the report's `six` bucket. The table's "1 + 1 / 2/2" and fileID are defensible, but that second record is evidenced only by the global 201/201 count, not by a stored per-instance delta. Fix (optional): footnote that the Test_Range Stairs instance is matched under its un-suffixed Name override.

3. [Low] `compare_placement.py` / `placement_report.json` — the 155-apparent-mismatch scratch-defect explanation is credible and structurally verifiable: `byid` is keyed by integer fileID (`:236`, `:385`) and the wrapper root-target test at `:166` relies on `target not in byid`; a string-keyed dict would make that test always true and fold children's `m_LocalPosition` overrides into the root. Re-running the shipped script reproduces byte-identical output (report SHA-256 unchanged, 3062/3062, 0 mismatches, 0 unresolved). Expected values are genuinely re-derived from Unity source YAML (81/81 named records have unique, non-trivial positions spanning ±1255 units), so the comparison is not circular. No fix.

4. [Low] `eliots-agentics/tasks/14 Scene Placement Comparison.md` (## Notes / ## Honesty limits) — scope, honesty and template checks pass: no `Editor/` file is part of this worker's deliverable, no `git add`/`commit`/`push` by this worker, Status/other headings are untouched, seven Status fields present, no `{subagent_id}`/`{state-here}` placeholders, and the single `Open:` line is byte-verbatim with the HEAD baseline (leading tab intact). The Notes explicitly do not claim the Director's visual observation is fixed, do not claim console-out-2.txt lines are gone, state editor appearance is unprovable headlessly, and disclose the clean-HEAD task-15 mirror. Conditional regression gates (SPEC-task14.md item 4) are correctly skipped: the obligation is explicitly "IF code changes land", no code changed, and the mirror's 0-error/0-warning build is recorded. No fix.

Tree:
  git hash-object --path="C:/Users/Eliot/AppData/Local/hermes/profiles/hermes-sbox/cache/scratch/taskmaster-nov22/task14/compare_placement.py" "C:/Users/Eliot/AppData/Local/hermes/profiles/hermes-sbox/cache/scratch/taskmaster-nov22/task14/compare_placement.py" = 2d480a6173b1857f2948fe68a358fb0445aabe76
  git hash-object --path="C:/Users/Eliot/AppData/Local/hermes/profiles/hermes-sbox/cache/scratch/taskmaster-nov22/task14/compare_placement.log" "C:/Users/Eliot/AppData/Local/hermes/profiles/hermes-sbox/cache/scratch/taskmaster-nov22/task14/compare_placement.log" = 40528785f33a79216aa510343098f71e36ea8cfa
  git hash-object --path="C:/Users/Eliot/AppData/Local/hermes/profiles/hermes-sbox/cache/scratch/taskmaster-nov22/task14/placement_report.json" "C:/Users/Eliot/AppData/Local/hermes/profiles/hermes-sbox/cache/scratch/taskmaster-nov22/task14/placement_report.json" = 88e3f2a8678fd684f962a6a3d9a41413cdfd7fe0
  git hash-object --path="C:/Users/Eliot/AppData/Local/hermes/profiles/hermes-sbox/cache/scratch/taskmaster-nov22/task14/probe_structure.py" "C:/Users/Eliot/AppData/Local/hermes/profiles/hermes-sbox/cache/scratch/taskmaster-nov22/task14/probe_structure.py" = 784580f33fb9c18759701597e7c042903f70943d

## Notes

Verification-only headless placement comparison (worker `sa-0-9f3d0009`, HEAD `6a65cb1`).
VERDICT: **NO confirmed headless transform gap.** No code changed; no `Editor/Core/*.cs` or any
other file under `Editor/` was touched; no `git add` / `commit` / `push`. Regression gates were
therefore not re-run (spec required-d work item 4 applies only if a code fix lands).

### Method
Scratch script `.../taskmaster-nov22/task14/compare_placement.py` (never committed) re-derives the
expected s&box transform from the Unity source and compares it to the generated `.scene`:
- Unity `.unity` YAML parsed into typed blocks; each `PrefabInstance` (type 1001) resolved by
  `StableGuid($"{sceneGuid}:{instanceFileID}")` (`UnityPrefabExport.cs:1098`, `Converter.Key` at
  `:777/:854-859`) against the stub `__guid` in the generated scene. Matching was 2861/2861 and
  201/201 deterministic 1:1 — no name-based fuzzy matching.
- Expected transform = source-prefab root local (plain root at `:1047-1082`, wrapper via
  `RootOf` at `:587-608`, direct model identity) + the instance's `m_Modifications` on the root
  transform (`:1566-1570`), composed with `Anchor()`'s prepend (`:116-122` in
  `UnitySceneExport.cs`: `Chains[g]` for a baked mesh-chain anchor, else `PathTo(source,g)`,
  `:490-502`), then `Unmirror` (`:721-744`) and `ToSbox` (`:223-228`): metres x 39.37, basis
  `(z, -x, y)`, rotation `(-qz, qx, -qy, qw)`, scale `(sz, sx, sy)`; prefab root position zero in
  the prefab file (`:1389-1397`), placement carried by the instance patch (`:1635-1664`).
- Generated values read from each stub's `__PrefabInstancePatch.PropertyOverrides`
  Position/Rotation/Scale/Name. Tolerance 0.02 units (task-11 precedent), quaternion compared
  modulo sign. All 3062 instances are nested (`m_TransformParent != 0`), so a naive local-only
  compare would have been invalid; the anchor prepend is included.

### Results
| Scene | PrefabInstances | Generated stubs | Compared | Matched | Mismatched | Unresolved | Position / Rotation / Scale OK |
|---|---|---|---|---|---|---|---|
| MergeV2 | 2861 | 2861 | 2861 | 2861 | 0 | 0 | 2861 / 2861 / 2861 |
| Test_Range | 201 | 201 | 201 | 201 | 0 | 0 | 201 / 201 / 201 |
| **Total** | **3062** | **3062** | **3062** | **3062** | **0** | **0** | **3062 / 3062 / 3062** |

Every compared object's exact Position/Scale delta is `0.0000` on all three s&box axes; rotation
matches at quaternion-sign tolerance with zero component delta. An intermediate run reported 155
apparent MergeV2 mismatches; that was a defect in the scratch script (string-keyed `byid` dict
caused wrapper root-target detection to mis-fire, mixing one child's `m_LocalPosition.x` into the
root). After keying `byid` by integer fileID and supplying the Test_Range scene GUID, all 3062
match — the 155 were never an importer gap. No systematic offset, rotation or scale class found.

### Six Director-named objects (all matched, exact zero deltas)
| Name | Scene | Instances | Matched | Example instance fileID | Delta (P / S) |
|---|---|---|---|---|---|
| Bulletin_Board_01_Snaps014 | MergeV2 + Test_Range | 1 + 1 | 2/2 | 1492032233 / 787959422 | 0,0,0 / 0,0,0 |
| Wall_03 | MergeV2 | 42 | 42/42 | 112695563 | 0,0,0 / 0,0,0 |
| Eaves_03 | MergeV2 | 1 | 1/1 | 662489786 | 0,0,0 / 0,0,0 |
| Shrubs_01 | MergeV2 | 34 | 34/34 | 44340576 | 0,0,0 / 0,0,0 |
| Stairs_Middle_01_Snaps014 (1) | MergeV2 + Test_Range | 1 + 1 | 2/2 | 1554373962 / 1339544921 | 0,0,0 / 0,0,0 |
| Desk_01 (55) | MergeV2 | 1 | 1/1 | 227445922 | 0,0,0 / 0,0,0 |

### Evidence artifacts (all scratch; none staged)
- `.../taskmaster-nov22/task14/compare_placement.py` — comparison script.
- `.../taskmaster-nov22/task14/compare_placement.log` — run output (the table above).
- `.../taskmaster-nov22/task14/placement_report.json` — per-instance expected/got/delta records.
- `.../taskmaster-nov22/task14/probe_structure.py` — instance census + six-object fileID locator.
- `.../taskmaster-nov22/task14/mirror/` — clean `git archive HEAD 6a65cb1` mirror; harness built
  there with **0 errors / 0 warnings** (dotnet 10.0.302). My own harness generation run was cut
  off by the tool-call cap; the generated scenes compared are the clean-HEAD output from the
  semantically identical task15 mirror, whose CRLF-normalized SHA-256 matches for
  `UnityPrefabExport.cs`, `UnitySceneExport.cs`, `UnityVolume.cs`, `UnityHdrpLighting.cs` and
  `UnityMaterial.cs` were verified by the Master:
  `.../taskmaster-nov22/task15/out/imported/assetstoreoriginals/school/scenes/mergev2.scene` and
  `.../test_range.scene`, with the matching `.unity` source copies in the same folder.

### Honesty limits
This proves serialized importer correctness, not what the Director sees on screen. Visual
placement in the s&box editor stays the existing `Open:` line below and does not block DONE.
`Testing/console-out-2.txt` is a static editor log; nothing here claims any of its lines are gone.
The Director's "too many objects are misplaced" observation is NOT claimed fixed by this headless
result.

## Clarification Q&A


## Relevant History 

Prior research (`resolve-errors-part-c-research.md`, scratch `part-c-unity.md`) classified scene
issue 5 as having no confirmed headless code-path gap: the 39.37 scale, basis change and
prefab-root position-zero rules are implemented at HEAD, and the six objects the Director named as
correct demonstrate the common path. Task 11's 44 EnvmapProbe bounds comparison established the
scratch-script precedent, including a 0.02 tolerance and matching by stable identity rather than
by name. Director Q5 = 2A (phone reply 2026-11-22 21:31) gave misplacement its own headless
source-vs-output comparison task and left the editor-visual check as an `Open:` follow-up.

This run closes that comparison: 3062/3062 placed PrefabInstances in MergeV2 and Test_Range match
exactly, including all six named objects, so no importer edit was made. The two other workers'
in-flight `Editor/Core` edits were never picked up — all builds and analysis used a clean
`git archive HEAD 6a65cb1` snapshot — and the Master sequences any later code work after they land.
The remaining question is purely editor-visual and stays open.

## Stylistic Task Notes:
References to variables like X and Y automatically imply that these are to be exposed properties for developer tweaking.
When programming, portability and DRY are a priority.

## Post-Task Follow Ups 
	Open: <Director's editor-visual check that placement now looks right in the s&box editor>
