## Status: STARTED? NO! CLARIFIED? NO! HISTORY? NO! REVIEWED? NO! DONE? NO! WORKER: NONE OTHER: fresh backlog from resolve-errors-part-c (console group G-A)

## Task
Resolve bare .vmat names in generated LOD/split models.

### Description
Created from the 2026-11-22 console-out-2 census in `resolve-errors-part-c-research.md`
(group G-A). Full code trace: scratch `taskmaster-nov22/part-c-code.md`.

Symptom (Testing/console-out-2.txt lines 1-2, 2 ERROR):
  engine/ResourceSystem Error loading resource file "floor.vmat_c" (Error: ERROR_FILEOPEN:
  File not found) - from imported/assetpackschoolnameverylong/schoolhdrpnameverylong/art/model/
  structure/stair/floor_stairs_bottom_lod01.vmdl
  ... "glass.vmat_c" ... from .../windows/windows_07/windows_07_02_glass_lod01.vmdl

Verified mechanism at HEAD 54ca0a6:
- These are SPLIT models from `ModelSplit` (`Editor/Core/UnityImport.cs:719-745`), named after
  `group.Meshes[0]` when `meshGroups.Count > 1` (`:414`). `floor_stairs_bottom_lod01.vmdl`
  comes from `floor_stairs_bottom_snaps014.fbx` (mesh `Floor_stairs_bottom_LOD01`);
  `windows_07_02_glass_lod01.vmdl` from mesh `Windows_07_02_Glass_LOD01`.
- Their generated `.vmdl` remap tables cover only prefab-assigned slots (skirting_line,
  wall_exterior, wall_interior, wood / windows), with `use_global_default = false` and an empty
  `global_default_material`. The embedded FBX slots `floor` / `glass` have no remap.
- The neutral-material fallback from HEAD `3ae34c7` (`:365-402`) does NOT cover them:
  `GroupSlots` (`:369-372`) returns only `info.MeshSlotMaterials[mesh].Keys` for a split
  model, so `needsNeutral` (`:376-378`) and the fill (`:397-401`) never see the embedded slot.
  `ModelSplit` (`:735-741`) copies only prefab-assigned slots into `group.Remaps`.
  `UnityImport.cs:417` passes `group.Remaps != null ? null : defaultMaterial`, so split models
  get NO global default. LOD remaps `:344-346` synthesize only from the SHARED `remaps`, which
  split models ignore.
- This is a REAL GAP in the just-closed `fix-unresolved-material-slots.md` (3ae34c7), NOT a
  duplicate: that fix covered the shared remap table and default/no-slot FBXs.

Required work:
1. Make every embedded FBX material slot of a split/LOD model end up with a valid, loadable
   material reference (extend `GroupSlots` at `UnityImport.cs:369-372` to include the split
   mesh's embedded FBX material names alongside `MeshSlotMaterials` keys, so detection
   `:376-378` and fill `:397-401` target `group.Remaps`). Reuse the SAME neutral `.vmat` shape
   already used for unnamed/default slots; do not invent a material that misrepresents the
   source.
2. Keep the `unassigned-material` warnings visible (their code and count must not drop).
3. Preserve existing behaviour for non-split models, default/no-slot FBXs, single-prefab
   defaultMaterial, prefab slot overrides, LOD variants and declared-slot pruning.
4. Regression gates: harness build 0 errors / 0 warnings; `building.unitypackage` (24 files,
   no --scenes) and `city--1.unitypackage` (1,166 files, no --scenes) byte-identical to a
   fresh HEAD-code baseline with BOTH trees populated; `verify_output.py <outRoot>
   <outRoot>/imported` 0 problems; real corpus `Testing/s.unitypackage --scenes` before/after
   showing the LOD/split models now carry the slot remaps and no new bare names; a fixture that
   exercises a split FBX with an embedded unassigned slot; two identical runs byte-identical;
   `Editor/Core/*.cs` BOM-less.
5. Report the exact changed-file set and the before/after remap coverage.
   `Testing/console-out-2.txt` is a STATIC editor log; do not claim its lines are gone. The
   verifiable proxy is that regenerated `.vmdl` files no longer reference bare `.vmat` names.

Do not add unrequested tooling. Stage by explicit path. No push until the Master verifies.

### Work Review


## Notes


## Clarification Q&A


## Relevant History 


## Stylistic Task Notes:
References to variables like X and Y automatically imply that these are to be exposed properties for developer tweaking.
When programming, portability and DRY are a priority.

## Post-Task Follow Ups 
