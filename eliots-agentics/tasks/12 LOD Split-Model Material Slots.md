## Status: STARTED? YES! CLARIFIED? YES! HISTORY? YES! REVIEWED? YES! DONE? YES! WORKER: sa-0-c554d46f OTHER: reviewed 22:09 no further changes; split-model embedded slots fixed

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

2026-11-22 22:09 - Verdict: no further changes

1. [Low] Documentation/Unity-To-SBox-Automated/fixtures/split-model-slot/Assets/Fixture/Prefabs/SplitModel.prefab:1-2 — the generated header is literally `%%YAML 1.1` / `%%TAG !u! tag:unity3d.com,2011:` (the scratch generator's `prefab = """%%YAML ...` string is concatenated, not `%`-formatted, unlike its `.mat` writer). Harmless here: UnityPrefabBindings.cs:138 parses `^--- !u!` blocks and ignores the header, the fixture still produced two split models with the new `embedded.vmat` remap, and sibling fixtures use `%YAML 1.1`. Fix (optional, non-blocking): change the two leading `%%` to `%` in the prefab and its scratch generator so the committed fixture is conventional Unity YAML.
2. [Low] Editor/Core/UnityImport.cs:375-378 — Master's empty-`Slots` question is NOT a reachable gap; no fix. `info.RendererSlots` only receives empty-`Slots` bindings from UnityFbxBindings.cs:81 (an FBX mesh node with zero material connections); the `new RendererBinding(mesh, Array.Empty<string>())` fallbacks at UnityPrefabBindings.cs:57 and :61 are local returns to `Assign` and never inserted into `RendererSlots`. For a split model ModelSplit:733 requires every mesh to have nonempty `MeshSlotMaterials`, and Assign:128 maps an empty-`Slots` binding's implicit slot to `binding.Mesh`, so that mesh-named key is already in `MeshSlotMaterials[mesh]` and is covered by the first half of the union at :375; with no prefab assignment no split occurs. `b.Slots` is correct; `b.EffectiveSlots` would add nothing here.
3. [Low] Editor/Core/UnityImport.cs:382-407 — case-sensitivity confirmed consistent; no fix. `GroupSlots` normalises via `.Distinct(StringComparer.OrdinalIgnoreCase)`, and both `needsNeutral` (:384) and the fill (:407) key on `slot.ToLowerInvariant() + ".vmat"`, matching every other remap key (:318/:323/:336/:341/:346/:746) and ModelDocument's `from = "<lower>.vmat"` output (:826-827). Corpus evidence confirms `floor.vmat` / `glass.vmat` remaps were generated and their target `_iup_unassigned.vmat` files exist on disk.
4. [Low] Editor/Core/UnityImport.cs — scope and evidence re-run independently; no fix. The edit is only the GroupSlots union; the non-split path (`group.Remaps == null` -> `missingSlots`) is untouched. Re-verified: harness build 0 warnings/0 errors; corpus Unresolved 64 with unassigned-material 45 both before and after (counted from report JSON); fixture packed by the shared pack_fixture.py (tar md5 af7f1a1b624836297633e6145ab5b1f9, my repack identical) and verify_output.py 6 files/0 problems; building 24/24 and city--1 1166/1166 byte-identical with both trees populated (`diff -r --strip-trailing-cr` exit 0 in my own run); two-run determinism exit 0 and REPORTS_NORMALIZED_EQUAL True; 25 Core .cs BOM-less; no staging/commit/push, no task-added stray artifacts, no report-schema/GUI/harness/channels.py change (UnitySceneExport.cs is task 13's disjoint edit).

Tree: git hash-object --path="Editor/Core/UnityImport.cs" "Editor/Core/UnityImport.cs" = 91be234550ee300464f6ab30a15b996f05faa016; git hash-object --path="Documentation/Unity-To-SBox-Automated/fixtures/split-model-slot/Assets/Fixture/Models/SplitModel.fbx" "Documentation/Unity-To-SBox-Automated/fixtures/split-model-slot/Assets/Fixture/Models/SplitModel.fbx" = 1d59cff1aebe6e5307337e3c47014cfef43ba680; git hash-object --path="Documentation/Unity-To-SBox-Automated/fixtures/split-model-slot/Assets/Fixture/Models/SplitModel.fbx.meta" "Documentation/Unity-To-SBox-Automated/fixtures/split-model-slot/Assets/Fixture/Models/SplitModel.fbx.meta" = 3d660025d1036e5d91f1174c29cc6d894d44b86e; git hash-object --path="Documentation/Unity-To-SBox-Automated/fixtures/split-model-slot/Assets/Fixture/Materials/ControlA.mat" "Documentation/Unity-To-SBox-Automated/fixtures/split-model-slot/Assets/Fixture/Materials/ControlA.mat" = 988e5bddf45c0af99c18a47c81613a25c16e672c; git hash-object --path="Documentation/Unity-To-SBox-Automated/fixtures/split-model-slot/Assets/Fixture/Materials/ControlA.mat.meta" "Documentation/Unity-To-SBox-Automated/fixtures/split-model-slot/Assets/Fixture/Materials/ControlA.mat.meta" = 983115c87b044e6acd78a8475ebcf8523a4c22a9; git hash-object --path="Documentation/Unity-To-SBox-Automated/fixtures/split-model-slot/Assets/Fixture/Materials/ControlB.mat" "Documentation/Unity-To-SBox-Automated/fixtures/split-model-slot/Assets/Fixture/Materials/ControlB.mat" = da56edb23ca7e7be0754de3802109cf49f519d9f; git hash-object --path="Documentation/Unity-To-SBox-Automated/fixtures/split-model-slot/Assets/Fixture/Materials/ControlB.mat.meta" "Documentation/Unity-To-SBox-Automated/fixtures/split-model-slot/Assets/Fixture/Materials/ControlB.mat.meta" = 40108ce5cd4202043ff821166e885ea9cf810d81; git hash-object --path="Documentation/Unity-To-SBox-Automated/fixtures/split-model-slot/Assets/Fixture/Prefabs/SplitModel.prefab" "Documentation/Unity-To-SBox-Automated/fixtures/split-model-slot/Assets/Fixture/Prefabs/SplitModel.prefab" = 8d8581469ba9222a7f6400bd4952f4c33b20a7c8; git hash-object --path="Documentation/Unity-To-SBox-Automated/fixtures/split-model-slot/Assets/Fixture/Prefabs/SplitModel.prefab.meta" "Documentation/Unity-To-SBox-Automated/fixtures/split-model-slot/Assets/Fixture/Prefabs/SplitModel.prefab.meta" = 873eae6995f9a38f8d19df2df13de9f737325ce2; git hash-object --path="Documentation/Unity-To-SBox-Automated/fixtures/split-model-slot.unitypackage" "Documentation/Unity-To-SBox-Automated/fixtures/split-model-slot.unitypackage" = c7bceac290f4970b6536ed04624edb9b70d99eca

## Notes

Implementation (Editor/Core/UnityImport.cs, GroupSlots at former :369-372): for a split model the helper now unions each mesh's prefab-assigned slot names with that mesh's embedded FBX material names from `info.RendererSlots` (`RendererBinding.Slots`). Detection (`needsNeutral`) and the fill into `group.Remaps` therefore see embedded slots such as `floor`/`glass`; the fill reuses the existing neutral `UnityMaterial` (`_Color` 0.5,0.5,0.5,1 via `ToVmat`). No other Core file changed, no report/GUI/harness change, no repo tooling added.

Gates (all evidence under C:/Users/Eliot/AppData/Local/hermes/profiles/hermes-sbox/cache/scratch/taskmaster-nov22/task12/):
1. Harness build: `evidence/gate1-after-build.log` = Build succeeded, 0 Warning(s), 0 Error(s); HEAD mirror baseline `evidence/gate1-baseline-build.log` also 0/0.
2. Fixture: `fixtures/split-model-slot/` (8 files: FBX + meta, 2 control .mat + meta, prefab + meta) and `fixtures/split-model-slot.unitypackage` packed with the shared packer; `evidence/gate2-fixture-pack.log`: 4 assets, tar 20480 bytes md5 af7f1a1b624836297633e6145ab5b1f9, archive 2330 bytes (repack reproduced identically). Before-run `evidence/gate2-fixture-before-remaps.txt` has only `shared.vmat` on both split models; after-run `evidence/gate2-fixture-after-remaps.txt` adds `from = "embedded.vmat"` -> `to = "imported/fixture/models/splitmodel_iup_unassigned.vmat"` on both, control slot unchanged. `evidence/gate2-fixture-after1-verify.log` / `...after2-verify.log`: checked 6 files, 0 problems (exit 0). Run logs/reports: `evidence/fixture-before.log`, `fixture-after1.log`, `fixture-after2.log` and matching `*-report.json`.
3. Real corpus `Testing/s.unitypackage --scenes`, fresh HEAD mirror (LF, BOM-less) vs patched mirror: `evidence/corpus-before.log` / `corpus-after.log`, reports `corpus-before-report.json` / `corpus-after-report.json`. `evidence/gate3-targets-before.txt` vs `...after.txt`: `floor_stairs_bottom_lod01.vmdl` now remaps `floor.vmat` and `windows_07_02_glass_lod01.vmdl` now remaps `glass.vmat`, both to generated `*_iup_unassigned.vmat`; control remaps unchanged. `evidence/gate3-changed-files.diff`: exactly 16 changed `.vmdl` + 4 new neutral `.vmat` (counter01, floor_stairs_bottom, windows_07_01, windows_07_02); no prefab/scene changed. `evidence/gate3-issue-counts.log` and `gate3-warning-codes.log`: Unresolved 64 before and 64 after (missing-texture 16, prefab-material-assignment 3, unassigned-material 45); `unassigned-material` stays 45. `evidence/gate3-bare-and-issues.log` records the earlier overly broad bare-name census; its `from =` keys are remap sources, not unresolved refs, so the authoritative proxy is the target-model remaps above. Harness exit 1 on the corpus is the pre-existing unresolved set, unchanged by the fix. Testing/console-out-2.txt is a static editor log; no claim is made that its lines disappeared.
4. Regressions without `--scenes`: `evidence/reg-building-before.log`, `reg-building-after.log`, `reg-city--1-before.log`, `reg-city--1-after.log` and reports. Both trees populated: building 24/24 files, city--1 1,166/1,166 files. `evidence/gate4-building-byte-identical.diff` and `gate4-city--1-byte-identical.diff`: `diff -r --strip-trailing-cr` exit 0, 0 lines.
5. Determinism: `evidence/gate5-fixture-two-run.diff` exit 0 (tree identical); `evidence/gate5-report-normalized.log` = REPORTS_NORMALIZED_EQUAL True after normalizing Destination and TextureProcessing.ElapsedMilliseconds.
6. BOM/hygiene: `evidence/gate6-bom.log`: 25 Core .cs files, 0 BOMs. No `import-report.json`, `bin`, `obj` or probe added to the repo by this task; the generator script lives only in scratch.
7. `evidence/gate7-git-status.txt`: intended changes are `Editor/Core/UnityImport.cs`, the new fixture folder + .unitypackage and this task file. The Director's `.humans-only/` deletions, `eliots-agentics/archive/` additions, other tasks and Testing/Assets files were left untouched. No `git add`/`commit`/`push` was run, per the dispatch override; the Master commits sequentially.
8. No `Editor/*.cs` GUI file changed, so the offline editor-compile gate is not applicable.


## Clarification Q&A


## Relevant History

Task 12 was carved out of the 2026-11-22 `resolve-errors-part-c` census (group G-A) after the HEAD `3ae34c7` fix for shared remap tables and default/no-slot FBXs left split/LOD models uncovered: `GroupSlots` saw only prefab-assigned slots, while `ModelSplit` gives each split model its own remap table and `ModelDocument` passes no global default for it. The earlier fix's neutral material shape is reused here, not re-invented.

The Director had already recorded STARTED/CLARIFIED/HISTORY and the plan before this worker turn; all work was done against the pinned `SPEC-task12.md` and a scratch HEAD mirror at 6a65cb1 (not the spec's dispatch-time 54ca0a6). Four workers edit parallel tasks, so no git index operation was attempted; the Master commits sequentially after review.


## Stylistic Task Notes:
References to variables like X and Y automatically imply that these are to be exposed properties for developer tweaking.
When programming, portability and DRY are a priority.

## Post-Task Follow Ups 
