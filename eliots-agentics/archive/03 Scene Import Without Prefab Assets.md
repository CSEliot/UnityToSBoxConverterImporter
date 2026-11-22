## Status: STARTED? YES! CLARIFIED? YES! HISTORY? YES! REVIEWED? YES! DONE? YES! WORKER: sa-0-34ca0be8 OTHER: reviewed 02:35 no further changes; direct FBX instances and plain renderer materials committed

## Task
	Import scenes that place no .prefab assets: plain GameObjects and direct FBX instances, with their renderer materials.

### Description
	A scene that does not use .prefab assets has two shapes, and neither is fully covered today.

	Plain objects already convert: hierarchy, transforms, enabled state, static flags, cameras, lights, audio listeners, box/sphere/capsule colliders and convex MeshColliders survive (verified in the school package's generated scenes). Gap: ModelRenderer is built with "Materials" and "MaterialOverride" null (Editor/Core/UnityPrefabExport.cs:262), so a plain MeshRenderer's assigned material is lost. There is also no fixture that exercises a scene whose only model reference is a plain MeshFilter.

	Direct model instances do not convert: a scene may store a PrefabInstance whose m_SourcePrefab guid is the FBX itself, with no .prefab asset in between. UnitySceneExport resolves every instance through Context.PrefabPathFor(guid) and Context.Converted[guid], which only contain .prefab assets (Editor/Core/UnityPrefabExport.cs:809-812). Such an instance is skipped with "Nested prefab ... is not in the package or was not converted" even though the model exists and was converted. The old_school scenes point at model-wrapper prefabs instead, so they are gated by task 01, but the same SourceOf path loses their 2,861 and 201 instances while the source prefab is rejected. Both the wrapper and the direct-model case need the same on-demand model-prefab source.

	Required work:
	1. Reuse the model-wrapper builder from task 01; expose it so Converter.SourceOf can synthesize a source for a Model guid and cache it like a converted prefab. Do not write a second model-to-prefab converter.
	2. In Instance(), keep the existing override path but resolve modifications aimed at fixed model-prefab IDs: 100000 name, 400000 transform, 2300000/2300002... renderers, m_Materials.Array.data[i] through task 02's map. MergeV2 carries 405 material overrides, 3,654 static-flag overrides and 2,861 transform/name blocks.
	3. In the shared Converter.Node (Editor/Core/UnityPrefabExport.cs:882), when a MeshRenderer has material references, resolve each with Context.MaterialFor and emit them on the ModelRenderer using the same indexed override shape the prefab path uses (same file:1041-1042). Support multi-slot renderers; unresolved slots stay null and are reported once per object.
	4. Confirm ImportPlan links Model and Material assets referenced only by plain scene objects or direct instances. It already scans scene text (Editor/Core/ImportPlan.cs:127-143); fix the link reason or scan if the fixture shows a miss.
	5. Add a minimal fixture: one FBX and its .meta, one .unity scene whose PrefabInstance targets the FBX guid plus its .meta, and plain objects covering parent/child, the same FBX rendered through a MeshFilter with a non-default material, a convex MeshCollider, a camera, a directional light, an audio listener, a disabled object and a static object. Store it under Documentation/Unity-To-SBox-Automated/fixtures so it can be tracked, or beside Testing/ locally if it must stay untracked. Do not re-export the 1.27 GB project.
	6. Do not treat zero prefab instances as an error or a warning; it is a normal scene and must convert.
	7. Keep output unchanged for scenes whose prefabs already convert.

	Non-goals: converting the .prefab assets themselves (task 01), the fixed-ID material map (task 02), built-in primitive meshes (task 04), loss reporting (task 05).

	Acceptance:
	- The fixture converts with --scenes to a .scene containing the direct FBX instance and every plain object/component above; the FBX renderer names the converted material.
	- verify_output.py reports 0 problems on the fixture output.
	- Harness on Testing/old_school_project.unitypackage after tasks 01 and 02: SceneConversions MergeV2 Instances 2,861 / Skipped 0 and Test_Range 201 / 0; the generated mergev2.scene contains prefab stubs under Setting/Structure/Props instead of four empty groups.
	- Two consecutive runs are byte-identical; building/city--1 output unchanged.

## Notes

	Implementation home: Editor/Core/UnitySceneExport.cs (Instance/SourceOf path, Anchor at ~103), Editor/Core/UnityPrefabExport.cs (Converter.SourceOf at 809-812, Node at 882, ModelRenderer at 262, prefab override shape at 1041), Editor/Core/ImportPlan.cs:127-143. Material resolution is Context.MaterialFor, wired in Editor/Core/UnityImport.cs:278.

	The model-wrapper builder from task 01 is the shared dependency; land 01 first. Task 02's fixed-ID map is needed for the direct instance's material arrays; if 02 is not in yet, land its map with 01 rather than duplicating it here. Tasks 01 (0d1cd94) and 02 (fa149ef) are now landed: use the existing builder and map, do not write a second converter or material map, and locate current line numbers rather than trusting the refs above.

	Fixture placement: Testing/* is gitignored, so use Documentation/Unity-To-SBox-Automated/fixtures for a tracked fixture or keep a local one beside Testing. Do not import the fixture into the project's Assets/imported as a committed artifact.

	Plan (this turn): (1) split WrapperRoot into a reusable model-prefab builder (Document + ModelRoot) so the same code builds a wrapper prefab and the implicit prefab of an FBX placed directly in a scene; (2) add Context.ModelPrefabFor, implemented in UnityImport, which synthesizes and writes `<fbx>.fbx.prefab` on demand and caches it, so Converter.SourceOf resolves a Model guid like a converted prefab (root 100000/400000, renderers 2300000+2n, overrides resolved by the existing wrapper branch of Instance()); (3) emit a MeshRenderer's own material slots on the ModelRenderer from Converter.Node, through UnityPrefabBindings.WrapperMaterialSlot, gated by a scene-only flag because a prefab's renderers already feed the model's slot remaps and prefab output must not move; (4) tracked fixture under Documentation/Unity-To-SBox-Automated/fixtures (hand-written ASCII FBX + meta, two materials, one scene + meta, a deterministic packer) because Testing/* is untracked; (5) gates: fixture run + verify_output.py, old_school --scenes counts, building/city--1 (no --scenes) byte-identical, two consecutive runs identical, and a scene-only diff against the pre-change baseline.

	Baseline recorded at HEAD fa149ef into the scratch dir (old_school --scenes): Converted 935, Files 2109, Unresolved 81, SceneConversions MergeV2 Roots 4 / Instances 2,861 / Skipped 0 and Test_Range Roots 3 / Instances 201 / Skipped 0; mergev2.scene already carries 2,861 `__Prefab` stubs under Setting/Structure/Props. building (no scenes): 24 files, 0 unresolved; city--1: 1,166 files, 3 unresolved.

	Work report (2026-11-21). Code, all in Editor/Core: UnityPrefabExport gained `Document(root)`, `ModelRoot(...)` returning `ModelParts` (the wrapper builder, now callable without a wrapper file), `ConvertModel(model, context)` for an FBX's implicit prefab, `Context.ModelPrefabFor`, a model branch in `SourceOf`, an `m_Enabled` fixed-id branch in `Instance`, `Converter.ConvertsRendererMaterials` and `MaterialSlots(...)`; UnitySceneExport sets the flag; UnityImport writes and caches the `<fbx>.fbx.prefab` document on demand. `.fbx.prefab` keeps the model's own extension so a Unity prefab of the same stem cannot collide with it.

	Evidence, all from real harness runs in the scratch dir. Fixture (`fixtures/prefab-free-scene.unitypackage`, 4 assets) with --scenes: Converted 5, Files 9, Unresolved 0, Warnings 0, SceneConversions Roots 4 / Instances 1 / Skipped 0; the stub's `__Prefab` is `imported/fixture/models/fixturemesh.fbx.prefab`, its patch carries Position/Rotation/Name and a `Materials` component override `{"indexed":{"0":"imported/fixture/materials/redoverride.vmat"}}`; the plain MeshFilter object's ModelRenderer names the same material; the convex MeshCollider survives as a ModelCollider; the child stays disabled, the static object gets Flags 32768, camera/AudioListener/directional light convert; `verify_output.py` reports 0 problems; two consecutive runs are byte-identical. old_school --scenes after the change: Assets 1,277, Converted 935, Files 2,109, Unresolved 81 (same codes: 16 missing-texture, 20 prefab-material-assignment, 1 ambiguous-material, 44 unassigned-material), Warnings 101, SceneConversions MergeV2 2,861 / 0 and Test_Range 201 / 0, and the whole 2,109-file output is byte-identical to the pre-change run (0 added, 0 removed, 0 changed) — requirement 7 holds at the byte level, not just structurally. building, city--1 and building --scenes are byte-identical to their baselines. The zero-prefab-instance case is `building --scenes` (Instances 0, Skipped 0, no error or warning).

	Deliberate limits, all reported rather than guessed: material emission is gated to scenes because a prefab's own renderers already feed the model's slot remaps and prefab output had to stay put; a static-flag override on an instance whose source has no such flag is still listed as not converted (s&box writes no Flags override for a prefab instance); a mirrored instance of a bare FBX has no mirrored model prefab yet and is reported; ImportPlan needed no change — the fixture report shows the model linked as "Scene mesh" and the non-default material as "Scene material override", so the scene scan already covers direct instances and plain objects.

	Files touched: Editor/Core/UnityPrefabExport.cs, Editor/Core/UnityImport.cs, Editor/Core/UnitySceneExport.cs, Documentation/Unity-To-SBox-Automated.md (Step 5 records the direct-FBX path and the fixture), and the new tracked fixture Documentation/Unity-To-SBox-Automated/fixtures/{pack_fixture.py, prefab-free-scene.unitypackage, prefab-free-scene/Assets/Fixture/{Models/FixtureMesh.fbx(+.meta), Materials/FixtureMat.mat(+.meta), Materials/RedOverride.mat(+.meta), Scenes/PrefabFree.unity(+.meta)}}. Nothing outside these paths changed; nothing is committed yet (worker.md step 5 waits for the review verdict).

## Clarification Q&A


## Relevant History 

	Source: tasks/sbox-expert-research.md, reconciled 2026-11-20 with tasks/unity-expert-research.md. The unity expert separated direct model instances from model-wrapper prefabs: the former have an FBX guid in m_SourcePrefab and never touch a .prefab asset. This task now covers both prefab-free shapes (plain objects and direct FBX instances); task 01 covers .prefab assets. The earlier version of this task covered only plain objects, which would have left the direct-instance path untested.

## Stylistic Task Notes:
References to variables like X and Y automatically imply that these are to be exposed properties for developer tweaking.

## Skills To Know: for you and/or subagents:
	unityimporterplus
	unity-to-sbox-conversion
	sbox-scripting-patterns
	sbox-model-material-resources
	sbox-scene-prefab-inspection
	sbox-offline-compile-check

## Post-Task Follow Ups
	When the direct-instance fixture passes, add a second scene that mixes plain objects, direct FBX instances and wrapper prefabs to prove the three paths coexist without duplicate objects.

### Work Review
2026-11-21 02:18 - Verdict: changes required

Tree: HEAD fa149ef + working tree (4 code/doc files, task file, untracked fixture)
  UnityPrefabExport.cs   -> 5f6766a53fb0f43972257c9c8453ea95b01951e5
  UnityImport.cs         -> c0d74ad9256c62e6a2e0bdcfe84da631ef10e0eb
  UnitySceneExport.cs    -> 1acab6f985ce4f7c4590da664e029d23fdc1228d
  Documentation/Unity-To-SBox-Automated.md -> 23fa576d246db08d7cc578f29cafaec5ef173be0

1. [Medium] Editor/Core/UnityPrefabExport.cs:1211: when every slot fails to resolve, MaterialSlots returns null
   before the `unresolved > 0` Information line at :1212, so a plain MeshRenderer whose materials are all
   unconverted is silently given the model's material. Task requirement 3 and the new
   Documentation/Unity-To-SBox-Automated.md sentence promise "reported once per object". Evidence: fixture
   variant with the plain renderer's material guid replaced by a guid absent from the package (crit03/badfix)
   produced "Materials": null in prefabfree.scene:65 with empty Unresolved, empty Warnings and no INFO. Fix:
   emit the Information before the `indexed.Count == 0` return.
2. [Low] Documentation/Unity-To-SBox-Automated/fixtures/prefab-free-scene.unitypackage: repacking the tracked
   folder with the tracked pack_fixture.py gives 4,462 bytes / md5 e7488ccd4eee1d668804da9e3cc15ef0, while the
   tracked file is 4,472 / f097500b488b394d05190930082c3831; the decompressed tar is identical, so only
   deflate bytes differ. The script's "a diff means a real change to the fixture" promise does not hold. Fix:
   regenerate and commit from the tracked script, or pin the compressor setting in the docstring.
3. [Low] Editor/Core/UnityPrefabExport.cs:1402-1404: the new m_Enabled branch tests plain.IsWrapper rather than
   direct-model sources, and falls back to plain.RendererIds[0] for any non-model-renderer target, so an
   enabled override aimed at another component can be applied to a single-renderer wrapper. No package
   exercises it (old_school/building/city output byte-identical). Fix: require IsModelRenderer(target), or
   keep the fallback and say why in the comment.
4. [Low] Editor/Core/UnitySceneExport.cs:116-117 dereferences source.Root.Body through InstanceRootTransform,
   but the new SourceOf model branch leaves Source.Root null. Unreachable while NestAddedObjects is a
   compile-time true; make InstanceRootTransform fall back to source.RootTransform so a later flip of that
   flag cannot throw.

Verified on disk: harness build 0 warnings / 0 errors; fixture --scenes Converted 5 / Files 9 / Unresolved 0 /
Warnings 0 / Roots 4 / Instances 1 / Skipped 0, stub imported/fixture/models/fixturemesh.fbx.prefab with
Position/Rotation/Name and Materials.indexed.0 = redoverride.vmat, plain MeshRenderer names the same vmat,
convex MeshCollider -> ModelCollider, child disabled, static Flags 32768, camera/AudioListener/light convert,
verify_output.py "checked 5 files, 0 problems", two runs byte-identical; old_school --scenes 935 / 2,109 / 81 /
101, MergeV2 2,861 / 0, Test_Range 201 / 0, and diff -r vs a HEAD fa149ef baseline built with the same LF
source convention: 0 files differ; building 24 files / 0 unresolved and city--1 1,166 files / 3 unresolved
byte-identical. WrapperRoot and ConvertModel share one ModelRoot builder (no second converter, task 02's slot
map reused); ConvertsRendererMaterials is set only in UnitySceneExport.cs:55; ImportPlan needed no change
(report links "Scene mesh" and "Scene material override"). Baselines must be built from LF sources: the kv3
template is a raw string literal in UnityImport.cs, so a CRLF checkout makes 377 .vmdl differ in line endings
only.

2026-11-21 02:48 - Worker reply. All four items fixed; UnityPrefabExport.cs and the fixture files changed, so REVIEWED? is NO! again.

1. [Medium] Fixed: `MaterialSlots` now emits the Information line before the `indexed.Count == 0` return, so a renderer whose slots all fail still reports. Evidence: a variant package (`badfix` in the scratch dir) whose plain MeshRenderer points at `ffffffffffffffffffffffffffffffff` now yields `Static Object: 1 of 1 material slot(s) have no converted material; the model's own material is used there.` with `Materials: null` in prefabfree.scene; Converted 5, Unresolved 0, Warnings 0 unchanged.
2. [Low] Fixed both ways you offered: the tracked `.unitypackage` was regenerated from the tracked `pack_fixture.py` (same bytes as before: 4,472, md5 f097500b488b394d05190930082c3831), and the docstring no longer promises byte-identical archives — it states that the tar is the reproducible part (printed as `tar 30720 bytes md5 7167015c6da703617d01fa7c6e44c884`) while the gzip wrapper's bytes come from the local zlib, and tells the reader to compare the tar, not the archive.
3. [Low] Fixed by tightening, not by documenting: the `m_Enabled` branch now requires `IsModelRenderer( target )`, so an enabled override aimed at another component can no longer land on a single-renderer wrapper's only renderer; the material branch keeps its single-renderer fallback (it has no other way to name the renderer).
4. [Low] Fixed: `InstanceRootTransform` returns a copy of `source.RootTransform` when `source.Root` is null (a direct model source), so enabling the `NestAddedObjects` flattening path cannot dereference a null root.

Gates re-run after the fixes, same scratch harness (build 0 warnings): fixture --scenes Converted 5 / Files 9 / Unresolved 0 / Warnings 0 / Roots 4 / Instances 1 / Skipped 0, verify_output.py "checked 5 files, 0 problems", two runs byte-identical; old_school --scenes 935 / 2,109 / 81 / 101 with MergeV2 2,861 / 0 and Test_Range 201 / 0, and its 2,109-file output is still byte-identical to the fa149ef baseline (0 added, 0 removed, 0 changed); building, city--1 and building --scenes byte-identical to their baselines.

### Work Review
2026-11-21 02:35 - Verdict: no further changes

Tree: HEAD fa149ef + working tree (3 code files, 1 doc, untracked fixture)
  Editor/Core/UnityPrefabExport.cs -> 1aa7f2a19e48637521b183ff7e792cfaea3b75b3
  Editor/Core/UnityImport.cs       -> c0d74ad9256c62e6a2e0bdcfe84da631ef10e0eb
  Editor/Core/UnitySceneExport.cs  -> 1acab6f985ce4f7c4590da664e029d23fdc1228d
  Documentation/Unity-To-SBox-Automated.md -> 23fa576d246db08d7cc578f29cafaec5ef173be0

No findings. All four items from 02:18 are fixed and re-verified on this tree.

1. MaterialSlots reports before the empty-map return: UnityPrefabExport.cs:1211 emits the Information line
   before the `indexed.Count == 0` return at :1212. Evidence: badfix-src (plain renderer's material guid
   ffffffff...) repacked with the tracked packer gives "Static Object: 1 of 1 material slot(s) have no
   converted material..." with ModelRenderer "Materials": null, Converted 5 / Files 9 / Unresolved 0 /
   Warnings 0 / Roots 4 / Instances 1 / Skipped 0.
2. pack_fixture.py:6-9,72: the promise now matches the tar. Repacking the tracked folder (python3 3.14.9 and
   py -3 3.13.14) yields tar 30,720 bytes md5 7167015c6da703617d01fa7c6e44c884 and a 4,462-byte archive;
   the tracked 4,472-byte archive (md5 f097500b488b394d05190930082c3831) decompresses to that same tar. The
   tar is stable and the gzip variance is now stated instead of denied.
3. UnityPrefabExport.cs:1404 now requires IsModelRenderer( target ); the material branch keeps its documented
   single-renderer fallback at :1406.
4. UnityPrefabExport.cs:1534-1539 returns a cloned copy of source.RootTransform when source.Root is null, so
   UnitySceneExport.cs:117 (ParentWorld) cannot dereference a null root.

Verified: harness build 0 warnings / 0 errors; fixture --scenes Converted 5 / Files 9 / Unresolved 0 /
Warnings 0 / Roots 4 / Instances 1 / Skipped 0, stub imported/fixture/models/fixturemesh.fbx.prefab with
Position/Rotation/Name and Materials.indexed.0 = redoverride.vmat, plain MeshRenderer names the same vmat,
convex MeshCollider -> ModelCollider, child disabled, static Flags 32768, camera/AudioListener/light convert,
verify_output.py "checked 5 files, 0 problems", two runs diff -r identical; old_school --scenes 935 / 2,109 /
81 / 101 with MergeV2 2,861 / 0 and Test_Range 201 / 0, 2,109 files diff -r byte-identical to the fa149ef
baseline; building 24 files / 0 unresolved and city--1 1,166 / 3 unresolved byte-identical to their baselines;
only the run log differs, in the console's "..." truncation glyph, not in any output file.

Note, not a finding: the work report says the tracked archive was regenerated with identical bytes (4,472);
the tracked script produces 4,462 here. The tar is identical, so the fixture content is the same and the
docstring now describes exactly this difference.