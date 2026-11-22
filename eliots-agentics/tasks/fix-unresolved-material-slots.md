## Status: STARTED? YES! CLARIFIED? YES! HISTORY? YES! REVIEWED? YES! DONE? YES! WORKER: sa-0-24978265 OTHER: reviewed 18:32 no further changes; Director confirmed Q2=B; ready to commit

## Task
Resolve unresolved FBX material slots so generated models never reference bare .vmat names.

### Description
Created from the 2026-11-22 error census recorded in `resolve-errors-B-research.md` (group G1).

NOTE (Master, 2026-11-22 18:02): the line numbers below were verified BEFORE commit 656f5b9,
which changed `UnityImport.cs`. The CURRENT verified line numbers are pinned in
`C:/Users/Eliot/AppData/Local/hermes/profiles/hermes-sbox/cache/scratch/taskmaster-nov22/SPEC-material-slots.md`.
Follow the spec's line numbers, not the historical ones in this Description.

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

2026-11-22 18:32 - Verdict: no further changes

1. [High] Goal met (file: `Editor/Core/UnityImport.cs`, ~353-406): the neutral fallback now covers named non-default slots as well as default/no-slot FBXs. Independently re-derived from the corpus reports and both output trees: named-slot remap coverage 0/41 -> 41/41 and all 41 `to` targets exist on disk. Fix: none.
2. [High] Warnings not swallowed (file: `Editor/Core/UnityImport.cs`): Unresolved 64 -> 64 and `unassigned-material` 45 -> 45 (41 per-slot + 4 grouped); `prefab-material-assignment` 3 -> 3 and `missing-texture` 16 -> 16 are untouched. Verified after-message: "No converted material was resolved for slot 'lambert2'; using a neutral material." Fix: none.
3. [Medium] Delta is exactly the intended neutral material (files: generated corpus output): converted 940 -> 961, files 2119 -> 2140, neutral `_iup_unassigned.vmat` 4 -> 25 (+21); independent hash-diff of the two trees found 0 removed, 21 added (all neutral `.vmat`), 21 modified `.vmdl`, everything else byte-identical. Fix: none.
4. [Medium] Regressions clean (files: `Editor/Core/UnityImport.cs`, harness build): building 24/24 and city--1 1166/1166 byte-identical to a fresh HEAD baseline in the same no-`--scenes` mode, both trees populated, 0-byte diffs; harness build 0 errors / 0 warnings for both trees (working tree independently rebuilt during this review). Fix: none.
5. [Medium] Fixture and provenance clean (files: `Documentation/Unity-To-SBox-Automated/fixtures/unresolved-slot/*`, `unresolved-slot.unitypackage`): the shared `pack_fixture.py` reproduced the committed archive byte-for-byte (3 assets, tar 20480 bytes, md5 `77112d18949da8b5837aa559884781d6`, 2064-byte archive). `UnresolvedMesh.fbx` declares `Material::Glass` with no matching `.mat`; the `ResolvedMesh.fbx` control remap is byte-identical to HEAD; `unresolvedmesh.vmdl` has `from = "glass.vmat"` -> `to = "imported/fixture/models/unresolvedmesh_iup_unassigned.vmat"`; `verify_output.py` reports "checked 4 files, 0 problems"; two-run determinism is a 0-byte tree diff with both trees populated (7 files). Fix: none.
6. [Low] Split-mesh-group path (file: `Editor/Core/UnityImport.cs`: `ModelSplit` now runs before the fallback and neutral refs are `TryAdd`-ed to `ModelGroup.Remaps`) is correct by inspection but not directly exercised: the corpus contains 11 split models, none overlapping the 41 named-slot assets, and the fixture has no split mesh. No output evidence is contradicted; no fix required.
7. [Low] Reporting honesty upheld (file: `eliots-agentics/tasks/fix-unresolved-material-slots.md`): `Testing/console-out.txt` is a static editor log with 33 short-name illegal-resource-name lines; the after count cannot be measured headlessly and no editor-visible material appearance is claimed. The verifiable proxy is remap coverage 0 -> 41 plus `verify_output.py` 0 problems. Fix: none.
8. [Low] Scope clean (files: whole worktree): 25/25 `Editor/Core/*.cs` are BOM-less and `UnityImport.cs` is LF-only; no per-fixture packer, unrequested tooling, stray `import-report.json`, `bin/`, `obj/` or `__pycache__` change in the repo. The `.humans-only/.template-task.md` deletion is the Director's archive move and is not staged. Fix: none.

Tree: git hash-object --path="Editor/Core/UnityImport.cs" "Editor/Core/UnityImport.cs" = 38eea9baa50e535e93416e583d1b66c398c99532
Tree: git hash-object --path="Documentation/Unity-To-SBox-Automated/fixtures/unresolved-slot/Assets/Fixture/Materials/ControlMat.mat" "Documentation/Unity-To-SBox-Automated/fixtures/unresolved-slot/Assets/Fixture/Materials/ControlMat.mat" = ca49961795c7f0999a5b3c96fed5ccbe433f5b16
Tree: git hash-object --path="Documentation/Unity-To-SBox-Automated/fixtures/unresolved-slot/Assets/Fixture/Materials/ControlMat.mat.meta" "Documentation/Unity-To-SBox-Automated/fixtures/unresolved-slot/Assets/Fixture/Materials/ControlMat.mat.meta" = aaff18eb6d29c438d71a4d5fa93684c1bcd42888
Tree: git hash-object --path="Documentation/Unity-To-SBox-Automated/fixtures/unresolved-slot/Assets/Fixture/Models/ResolvedMesh.fbx" "Documentation/Unity-To-SBox-Automated/fixtures/unresolved-slot/Assets/Fixture/Models/ResolvedMesh.fbx" = 40b4d9c2cba6228c9bc201ca40c0641b67d4ef6c
Tree: git hash-object --path="Documentation/Unity-To-SBox-Automated/fixtures/unresolved-slot/Assets/Fixture/Models/ResolvedMesh.fbx.meta" "Documentation/Unity-To-SBox-Automated/fixtures/unresolved-slot/Assets/Fixture/Models/ResolvedMesh.fbx.meta" = b0ca491fb333afba89f9161e3f49c849e14cca2c
Tree: git hash-object --path="Documentation/Unity-To-SBox-Automated/fixtures/unresolved-slot/Assets/Fixture/Models/UnresolvedMesh.fbx" "Documentation/Unity-To-SBox-Automated/fixtures/unresolved-slot/Assets/Fixture/Models/UnresolvedMesh.fbx" = 0b9fc5c7fc8567cb351559d077c8f62e4a055fa6
Tree: git hash-object --path="Documentation/Unity-To-SBox-Automated/fixtures/unresolved-slot/Assets/Fixture/Models/UnresolvedMesh.fbx.meta" "Documentation/Unity-To-SBox-Automated/fixtures/unresolved-slot/Assets/Fixture/Models/UnresolvedMesh.fbx.meta" = 14220772074281b448fcaf01babee98f619175a0
Tree: git hash-object --path="Documentation/Unity-To-SBox-Automated/fixtures/unresolved-slot.unitypackage" "Documentation/Unity-To-SBox-Automated/fixtures/unresolved-slot.unitypackage" = f5dc8539270af0d77cc591d29edac8b7ee57c765

## Notes

Implementation (Task Worker, 2026-11-22): widened the neutral-material fallback in `Editor/Core/UnityImport.cs` so every FBX-declared rendered slot without a converted `.vmat` gets the same neutral material already used for unnamed/default slots. `missingSlots` are all remapped when `RendererSlots.Count > 0`; the `RendererSlots.Count == 0` branch still sets `defaultMaterial`. `ModelSplit` now runs before the fallback: a neutral file is created only when an effective model remap table (shared or per-group) actually lacks a slot, and a neutral reference is added to `ModelGroup.Remaps` for split models. Warnings come from the pre-fallback state: all 41 named-slot `unassigned-material` issues are still emitted with the same code and an honest message ending "using a neutral material.", and grouped default/no-slot warnings still use the original gate. No other `Editor/Core` logic, no `Editor/*.cs` GUI change (so no offline editor-compile gate), no report-schema change.

Fixture: added `Documentation/Unity-To-SBox-Automated/fixtures/unresolved-slot/` (UnresolvedMesh.fbx with named `Glass` slot and no matching `.mat`; ResolvedMesh.fbx with `ControlMat.mat` as the unchanged control) and `unresolved-slot.unitypackage`, packed with the shared `pack_fixture.py`: 3 assets, tar 20480 bytes, tar md5 `77112d18949da8b5837aa559884781d6`, archive 2064 bytes (`fixture-pack.log`, re-confirmed by `evidence/repack-check.unitypackage`).

All evidence lives under `C:/Users/Eliot/AppData/Local/hermes/profiles/hermes-sbox/cache/scratch/taskmaster-nov22/slots/`:
1. PASS - harness build 0 errors / 0 warnings for both trees: `gate1-working-build.log`, `gate1-baseline-build.log`. The fresh HEAD baseline mirror `baseline/` was built from `git show HEAD:Editor/Core/*.cs` with the same LF EOL and copied harness sources.
2. PASS - fixture `--scenes`: `fixture-run.log`, `fixture-report.json`; `unresolvedmesh.vmdl` has `from = "glass.vmat"` -> `to = "imported/fixture/models/unresolvedmesh_iup_unassigned.vmat"`, that neutral `.vmat` exists with the standard 0.5-grey shape, the control `resolvedmesh.vmdl` is byte-identical to the HEAD run (`fixture-baseline-run.log`, `fixture-baseline-report.json`), and `verify_output.py <outRoot> <outRoot>/imported` says "checked 4 files, 0 problems" in `evidence/gate2-verify-output.log`. Generated `.vmat` files are s&box KV text, not JSON, so that is the applicable validator.
3. PASS - real corpus `Testing/s.unitypackage --scenes`, baseline vs after: `evidence/corpus-baseline-run.log`, `evidence/corpus-baseline-report.json`, `evidence/corpus-after-run.log`, `evidence/corpus-after-report.json`, per-code analysis `evidence/gate3-analysis.txt`, named-slot census `evidence/named-slots-baseline.json`, changed-file set `evidence/gate3-changed-files.txt`. Named-slot remaps present 0 -> 41/41 (every `to` target exists); Unresolved 64 -> 64 and `unassigned-material` 45 -> 45 (41 per-slot warnings retained with refined wording, 4 grouped warnings unchanged); converted 940 -> 961 and files 2119 -> 2140, i.e. +21 neutral `.vmat` files, one per affected FBX; exactly 21 `.vmdl` files differ and 21 neutral `.vmat` files are added, 0 removed, all other files byte-identical.
4. PASS - no-`--scenes` regressions against that baseline: building 24 files byte-identical (24 -> 24; `evidence/reg-building-baseline-run.log`, `evidence/reg-building-after-run.log`, `evidence/reg-building-baseline-report.json`, `evidence/reg-building-after-report.json`, empty `evidence/gate4-building-diff.txt`) and city--1 1166 files byte-identical (1166 -> 1166; `evidence/reg-city--1-baseline-run.log`, `evidence/reg-city--1-after-run.log`, `evidence/reg-city--1-baseline-report.json`, `evidence/reg-city--1-after-report.json`, empty `evidence/gate4-city--1-diff.txt`); both trees populated per the harness `assets=` lines.
5. PASS - two identical fixture runs produce byte-identical output trees (`evidence/gate5-fixture-run2.log`, `evidence/fixture-report2.json`, empty `evidence/gate5-tree-diff.txt`); reports equal after ignoring `Destination` and `TextureProcessing.ElapsedMilliseconds`.
6. PASS - all 25 `Editor/Core/*.cs` are BOM-less (`75 73 69`), `UnityImport.cs` is LF-only; the tracked `Documentation/Unity-To-SBox-Automated/__pycache__/unity_scene_to_sbox.cpython-314.pyc` is unchanged; no stray `import-report.json` or scratch probe in the repo. Fixture tar md5 re-confirmed on repack.
7. PASS - `git status --short` shows only the intended source, fixture and task-file paths, plus the Director's `.humans-only/.template-task.md` deletion, which is not staged.
8. This record is Gate 8.

Reporting honesty: `Testing/console-out.txt` is a static editor log with 33 short-name illegal-resource-name lines; the after count cannot be measured headlessly and no editor-visible material appearance is claimed. The verifiable proxy is remap coverage 0 -> 41 plus `verify_output.py` 0 problems. No commit and no push: the Review Critic's `no further changes` is still pending, per the parent brief.

## Clarification Q&A

	Q1: This task was created with DONE? NO!. The standing goal says all headers DONE? YES!.
	May the Master drive it?
	An: (Director, in chat, 2026-11-22 18:33) "My mistake, I meant 2B anyway. Good job!"
	The Director's answer to the paired Q2 of resolve-errors-B was Q2=B, NOT Q2=A. Q2=B =
	"Drive the new per-group tasks to DONE? YES! too, implementing those fixes in this same
	loop". Driving this task is therefore a VERIFIED Director decision, not an assumption.
	Historical note: Q2 was initially recorded as Q2=A ("leave the new tasks as fresh backlog,
	close only the three current tasks"). After four re-assertions of the standing goal and four
	unanswered requests for the word, the Master recorded a PARENT DECISION treating those re-sends
	as authorization and flagged it as an assumption. The Director has now confirmed the intent was
	Q2=B, which matches what the Master did. The Q2=A references in resolve-errors-B.md:96,103,
	116-118 and resolve-errors-B-research.md:138 are superseded by this correction.

	Q2: What is the exact root cause and the narrowest fix?
	An: Answered by evidence, not the Director. See SPEC-material-slots.md: the neutral-material
	fallback at UnityImport.cs:357-372 excludes named non-default slots, so ModelDoc falls back to
	the raw FBX slot name. Fix = widen that fallback to all missingSlots, keep the warnings, handle
	split mesh groups. No Director question needed.

## Relevant History 

	This task came out of the 2026-11-22 six-group error census in resolve-errors-B-research.md
	(group G1: unresolved FBX material slots leaking bare .vmat names). It was created by the
	Master under Director Q1=A (count tga-fix.md as the TGA group's task, create ONE new task for
	the other group) and left DONE? NO! under Q2=A (fresh backlog, close only the three current
	tasks). Those three tasks are now closed at HEAD 656f5b9.

	The prior console-log/FBX task (c7f922a) fixed absolute author paths in binary FBX; the
	material-slot regression round (c1ec442) cut Unresolved 81 -> 64; tga-fix (656f5b9)
	normalized 16-bpp TGAs. None of those touched the named-slot neutral fallback that is the
	subject of this task. The standing goal was re-asserted four times after this task became the
	only DONE? NO! header, so the Master initially recorded a parent decision to drive it; the
	Director then confirmed on 2026-11-22 18:33 that the intended answer was Q2=B (drive the
	new per-group tasks to DONE? YES! in this same loop), making that a verified decision.

## Stylistic Task Notes:
References to variables like X and Y automatically imply that these are to be exposed properties for developer tweaking.
When programming, portability and DRY are a priority.

## Post-Task Follow Ups 
