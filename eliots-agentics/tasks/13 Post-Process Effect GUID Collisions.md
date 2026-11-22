## Status: STARTED? YES! CLARIFIED? YES! HISTORY? YES! REVIEWED? YES! DONE? YES! WORKER: sa-1-80e58d05 OTHER: reviewed 22:23 no further changes (replacement critic); effect component GUIDs decoupled from host GameObjects

## Task
Give post-process effect components GUIDs distinct from their host GameObjects.

### Description
Created from the 2026-11-22 console-out-2 census in `resolve-errors-part-c-research.md`
(group G-C). Full code trace: scratch `taskmaster-nov22/part-c-code.md`.

Symptom (Testing/console-out-2.txt lines 3-6, 4 WARNING):
  Generic Sandbox.Tonemapping: Guid 82a3f100-b4a5-3f93-a94c-c3b9505c8937 is already taken by
  GameObject:Tonemapping - changing
  Generic Sandbox.Bloom: Guid bc06616b-aa92-3251-93c6-6f19279b649a is already taken by
  GameObject:Bloom - changing
  Generic Sandbox.DepthOfField: Guid 8e1c5e0a-5556-319e-8ddd-360694e17d75 is already taken by
  GameObject:Depth of Field - changing
  Generic Sandbox.AmbientOcclusion: Guid 34ec698a-355e-3c1a-8d78-897b8b0a19a1 is already taken
  by GameObject:Ambient Occlusion - changing

Verified mechanism at HEAD 54ca0a6:
- `Editor/Core/UnitySceneExport.cs:389-390`, the `Effect` closure inside the post-processing
  loop (`:364-407`):
    children.Add( SceneObject( $"{key}:{type}", name, "",
        Component( type, StableGuid( $"{key}:{type}" ), true, properties ) ) );
- `key = $"{c.Key}:postprocessing:{index}"` (`:366`); `type` is Sandbox.Tonemapping / Bloom /
  DepthOfField / AmbientOcclusion (`:391,396,398,403`).
- `SceneObject` builds the GameObject with `GameObject( StableGuid( key ), ... )`
  (`UnitySceneExport.cs:519-525`, `:521`); `Component` writes `["__guid"] = id.ToString()`
  (`:560-565`, `:562`). So the effect child's GameObject `__guid` and its own component
  `__guid` are the SAME `StableGuid($"{key}:{type}")`, and s&box reassigns one.
- `StableGuid` is MD5-based over `"unityimporterplus:" + key` (`UnityPrefabExport.cs:214-220`),
  RFC-4122 v3. `index` and `type` are in the key, so different volumes/effects already get
  distinct GUIDs - this is NOT a hardcoded GUID shared across volumes.
- Verified empirically: generated
  `Assets/imported/assetpackschoolnameverylong/schoolhdrpnameverylong/scenes/mergev2.scene`
  contains `82a3f100-b4a5-3f93-a94c-c3b9505c8937` (Tonemapping child). The Unity HDRP
  override script GUIDs at `UnityVolume.cs:39-42` are bare 32-hex asset GUIDs and do NOT
  match the dashed v3 console UUIDs.
- The same pattern is already done correctly for skybox / ambient / fog / PostProcessVolume
  components at `UnitySceneExport.cs:263,271,284,341,367` (`:component` suffix).

Required work:
1. At `UnitySceneExport.cs:390`, give the component a distinct key, e.g.
   `StableGuid( $"{key}:{type}:component" )`. Deterministic, engine-free, minimal. Do not
   change the GameObject's own GUID - prefab/scene links may depend on it.
2. Confirm the four effect components in generated scenes now have GUIDs different from
   their host GameObjects, and that all other components keep their existing GUIDs.
3. Regression gates: harness build 0 errors / 0 warnings; `building.unitypackage` (24 files,
   no --scenes) and `city--1.unitypackage` (1,166 files, no --scenes) byte-identical to a
   fresh HEAD-code baseline with BOTH trees populated (note: these packages may contain no
   post-process volumes, so also run a scene-bearing fixture); `verify_output.py <outRoot>
   <outRoot>/imported` 0 problems; real corpus `Testing/s.unitypackage --scenes` before/after
   with a per-effect GUID uniqueness check; two identical runs byte-identical; BOM-less.
4. Report the exact changed-file set and the before/after GUID relationship for all four
   effect types. `Testing/console-out-2.txt` is a STATIC editor log; do not claim its lines
   are gone. The verifiable proxy is that generated effect components no longer collide.

Do not add unrequested tooling. Stage by explicit path. No push until the Master verifies.

### Work Review

2026-11-22 22:23 - Verdict: no further changes
1. [Low] `Editor/Core/UnitySceneExport.cs:390` — the only reviewed change: the `Effect` closure's component key is now `StableGuid( $"{key}:{type}:component" )`; the `SceneObject( $"{key}:{type}" ... )` GameObject key and its `GameObject( StableGuid( key ) )` GUID are untouched. On-disk evidence (gate2 fixture, gate3 real corpus) shows 4+4 baseline GameObject/component duplicate GUID pairs drop to 0, all effect component GUIDs distinct, and all host GameObject GUIDs unchanged. Mechanism sound and minimal. Fix: none.
2. [Low] Scope — `git diff` confirms exactly one changed line in the single reviewed file; no other component GUID, GUID scheme, scene structure, report schema or GUI code changed, and no new fixture or tooling was added to the repo. The `UnityImport.cs`, `.humans-only/` and other workers' changes visible in `git status` are out of scope and untouched. Fix: none.
3. [Low] Regression gates — harness build 0 Warning(s)/0 Error(s) on both mirrors; scene-bearing fixture and real corpus (`Testing/s.unitypackage --scenes`) confirm collision removal with only 4 `__guid` lines per scene changed; no-scenes packages byte-identical (24/24, 1166/1166); two identical runs byte-identical; `verify_output.py` checked 864 files, 0 problems; 25 `Editor/Core/*.cs` BOM-less; unresolved 64/64. Fix: none.
4. [Low] Honesty & process — the Notes do not claim the static `Testing/console-out-2.txt` warnings disappeared; they use the verifiable proxy (generated effect components no longer share a GUID with their host GameObjects). No `git add` / `git commit` / `git push` was performed by the worker. Fix: none.

Tree:
- `git hash-object --path="Editor/Core/UnitySceneExport.cs" "Editor/Core/UnitySceneExport.cs" = 6330bb7fb75e87b708f6aae630ced8f19f1c6216`

## Notes

Implemented at HEAD 6a65cb1 (spec pinned at 54ca0a6; delta since pin is task docs only, importer code identical). One-line change at `Editor/Core/UnitySceneExport.cs:390`: the `Effect` closure now passes `StableGuid( $"{key}:{type}:component" )` to `Component(...)`; the `SceneObject( $"{key}:{type}", ... )` GameObject key and its `GameObject( StableGuid( key ) )` GUID are untouched. No other component GUID, GUID helper, scene structure, report schema or GUI code changed; no `Editor/*.cs` change, so the offline editor-compile gate does not apply. No new fixture: the existing `Documentation/Unity-To-SBox-Automated/fixtures/hdrp.unitypackage` already emits all four effect children (archive 9670 bytes, tar 143360 bytes, tar md5 1ff2815532db7b1a484e9a7f542caede).

All harness work ran in scratch `C:/Users/Eliot/AppData/Local/hermes/profiles/hermes-sbox/cache/scratch/taskmaster-nov22/task13/`. Baseline and after mirrors were built from `git show HEAD:Editor/Core/*.cs` with identical LF sources; the after mirror differs from baseline by exactly the one line above, which excludes the concurrent worker's uncommitted `UnityImport.cs` edit from the comparison.

Gates (all PASS; artifact paths relative to that scratch dir):

1. `logs/build-baseline.log`, `logs/build-after.log`: harness `dotnet build -c Release`, 0 errors / 0 warnings on both mirrors.
2. HDRP fixture `--scenes`: `logs/fixture-baseline.log`, `logs/fixture-after.log`, `runs/fixture-baseline/{out,report.json}`, `runs/fixture-after/{out,report.json}`; `logs/gate2-fixture-guid-evidence.txt`: baseline has 4 duplicate GameObject/component GUID pairs, after has 0, all four component GUIDs distinct, all four GameObject GUIDs unchanged from HEAD; `logs/gate2-verify-after.log`: `checked 21 files, 0 problems`.
3. Real corpus `Testing/s.unitypackage --scenes`: `logs/s-baseline.log`, `logs/s-after.log`, `runs/s-{baseline,after}/{out,report.json}` (both 2140 files, no `.iup-temp`, reports complete); `logs/gate3-changed-files.txt`: only `imported/a/s/scenes/mergev2.scene` and `imported/a/s/scenes/test_range.scene` differ; `logs/gate3-mergev2-line-diff.txt` and `logs/gate3-test_range-line-diff.txt`: exactly four `__guid` lines per scene, zero non-GUID lines changed; `logs/gate3-s-guid-evidence.txt`: 8 effect components, 8 baseline host collisions -> 0 after, all component GUIDs distinct; unresolved 64/64, warnings 0/0 with empty `logs/gate3-s-unresolved-diff.txt` and byte-identical console logs (`logs/gate3-console-diff.txt`); reports equal after normalizing only `Destination` / `TextureProcessing.ElapsedMilliseconds`; `logs/gate3-s-verify.log`: `checked 864 files, 0 problems`.
4. No-scenes regressions: `runs/building-{baseline,after}/out` 24 files each, `runs/city--1-{baseline,after}/out` 1166 files each, both trees populated; `logs/gate4-building-diff.txt` and `logs/gate4-city--1-diff.txt` empty under `diff -r --strip-trailing-cr`; unresolved 0/0 and 3/3 with empty breakdown diffs (`logs/gate4-*-unresolved-diff.txt`).
5. Two identical fixture runs: `runs/fixture-after/out` vs `runs/fixture-after2/out` byte-identical (`logs/gate5-tree-diff.txt` empty, run log `logs/fixture-after2.log`); reports equal after normalizing only `Destination` / `TextureProcessing.ElapsedMilliseconds`.
6. BOM/stray artifacts: all 25 `Editor/Core/*.cs` BOM-less; no repo `import-report.json`, probe, new `__pycache__`, or new `bin`/`obj` (the existing harness `bin`/`obj` and tracked `.pyc` predate this task) - `logs/gate6-bom-evidence.txt`.
7. `git status --short` captured in `logs/gate7-git-status.txt`: this worker changed only `Editor/Core/UnitySceneExport.cs` and this task file; the concurrent workers' `UnityImport.cs`, fixtures, Director archive/deletions and other tasks' files are untouched and unstaged. Per the dispatch override, no `git add` / `git commit` / `git push` was run; the Master commits sequentially.

Honesty: `Testing/console-out-2.txt` is a static editor log; no claim is made that its four warnings disappeared. The verifiable proxy - generated effect components no longer share a GUID with their host GameObjects - is proven above. No new fixture or checker/tooling was added to the repo.

## Clarification Q&A


## Relevant History 

Task 11 established scene post-processing volumes and already used the `:component` suffix for the PostProcessVolume, skybox, ambient and fog components; the HDRP fixture from task 06 carries the four effect overrides exercised here, so no new fixture was needed. The 2026-11-22 part-C census (group G-C) traced the four GUID warnings to the shared `StableGuid( $"{key}:{type}" )` at `UnitySceneExport.cs:390`; the pinned spec chose the narrowest seam, appending `:component` to the component key only.

## Stylistic Task Notes:
References to variables like X and Y automatically imply that these are to be exposed properties for developer tweaking.
When programming, portability and DRY are a priority.

## Post-Task Follow Ups 
