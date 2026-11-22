## Status: STARTED? NO! CLARIFIED? NO! HISTORY? NO! REVIEWED? NO! DONE? NO! WORKER: NONE OTHER: fresh backlog from resolve-errors-B (G1); left DONE? NO! per Director Q2=A

## Task
Resolve unresolved FBX material slots so generated models never reference bare .vmat names.

### Description
Created from the 2026-11-22 error census recorded in `resolve-errors-B-research.md` (group G1).

Symptom: `Testing/export-report-264-errors-2-groups.json` has 45 `unassigned-material` and 3
`prefab-material-assignment` Unresolved entries. In `Testing/console-out.txt` these surface as
33 `Trying to load an illegal resource name <name>.vmat!` short-name lines (glass 15, windows 9,
metal 4, lambert2 2, lambert3 1, mesh 1, floor 1), 32 `Missing vmat "<name>.vmat"`, 63
`Could not find material data for "unnamed_1"`, 5 `vmat_c` FILEOPEN lines, and 77 `.vmdl`
dependency-timeout entries in the report. Representative sources:
`a/s/Art/Model/Structure/Doors/Door_01/Door_01_01_Snaps014.fbx` (Glass + Metal),
`a/s/Art/Model/Structure/Windows/Windows_05/Windows_05_01_Snaps014.FBX` (Glass + Windows),
`a/s/Art/Model/Structure/Wall/Classroom_Door_Wall_01_Snaps014.fbx` (lambert2/lambert3/mesh;
its generated `.vmdl` has ZERO material remaps).

Verified mechanism (`research-code.md` in the taskmaster-nov22 scratch dir):
- `Editor/Core/UnityImport.cs:302` `MaterialFor` returns null when no converted `.vmat` output
  exists, so no remap entry is added (`:305-341`).
- The neutral-material fallback at `:346-361` is gated on
  `(info.Materials.Count == 0 || defaultSlots.Length > 0)`, so a NAMED non-default slot such as
  `glass` / `lambert2` never qualifies.
- `:367-369` only emits `No converted material was resolved for slot '<slot>'.`
- `ModelDocument` (`:783-784,800-811`) emits `from = <slot>.vmat`; with no remap entry ModelDoc
  falls back to the raw FBX slot name, a path-less resource name that cannot exist on disk.
- Slot names come from `Editor/Core/UnityModel.cs:226-227,300`.
- `Editor/Core/ImportPlan.cs:97-99` `Match` is silent on zero candidates, so the plan
  under-reports these slots. `UnityPrefabExport.MaterialSlots` (`:1334-1352`) is NOT the emitter.

Distinct from the 32 FULL-PATH illegal-name lines for `poster_01/02 ... .vmat`: those are a
cascade from the 16-bit TGA decode failure and belong to `tga-fix.md` (group G2), not here.

Required work:
1. Make a named-but-unresolved FBX material slot produce a valid, loadable material reference
   in the generated `.vmdl` (e.g. extend the neutral-material fallback to named slots, or emit a
   remap to a real neutral `.vmat`). Do not invent a material that misrepresents the source; keep
   the `unassigned-material` warning so the loss stays visible.
2. Keep the full-path `.vmat` cascade out of scope (that is G2 / `tga-fix.md`).
3. Preserve the existing behaviour for default/no-slot FBXs and for prefab slot overrides.
4. Regression gates: harness build 0 errors / 0 warnings; `building.unitypackage` (24 files,
   no --scenes) and `city--1.unitypackage` (1,166 files, no --scenes) byte-identical to a
   fresh HEAD-code baseline; `verify_output.py <outRoot> <outRoot>/imported` reports 0 problems;
   a fixture that exercises a named unresolved slot; two identical runs byte-identical;
   `Editor/Core/*.cs` BOM-less.
5. Report the before/after counts of short-name illegal resource names and of the 64 Unresolved
   issues. Note that commit c1ec442 already reduced Unresolved 81 -> 64; do not claim that
   reduction as this task's work.

Do not add unrequested tooling. Stage by explicit path. No push until the Master verifies.

### Work Review


## Notes


## Clarification Q&A


## Relevant History 


## Stylistic Task Notes:
References to variables like X and Y automatically imply that these are to be exposed properties for developer tweaking.
When programming, portability and DRY are a priority.

## Post-Task Follow Ups 
