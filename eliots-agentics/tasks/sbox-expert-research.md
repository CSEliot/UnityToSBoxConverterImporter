## Status: STARTED? YES! CLARIFIED? YES! HISTORY? YES! REVIEWED? YES! DONE? YES! WORKER: MASTER OTHER: research complete; five backlog tasks committed

## Task
	Generate the tasks to: Update project UnityImporterPlus to support importing scenes that don't use prefabs.
		
### Description
	As the sbox expert, you the Task Manager, shall do this task, as if you had decided it was small enough for you to do alone.

	The project: C:\cseliot\1-Development\1-SBox\2-Projects\unityimporterplus\
	Research and build tasks in to put in {project}\eliots-agentics\tasks-todo-sbox correlating to your research. 

	Recently, an attempt was made to import "C:\cseliot\1-Development\1-SBox\2-Projects\unityimporterplus\Testing\old_school_project.unitypackage", with pitiful success.
	The old_school_project.unitypackage was made from project C:\cseliot\1-Development\4-Unity\2-Projects\old_school_model.

### Work Review

2026-11-20 22:04 - Verdict: changes required

Tree: eliots-agentics/tasks-todo-sbox/{01 Prefab Variants - FBX Model Base.md, 02 Prefab Variants - Prefab Base and Chains.md, 03 Scene Import Without Prefab Instances.md, 04 Built-in Unity Primitive Meshes.md, 05 Scene Skip Diagnostics and Report.md}
git hash-object --path=...
  01 9f70907f44c0905aaa6d330f9b7f4136cc23eb12
  02 9fa2b31fededf2f987ff25ce2c6b7487c583cd12
  03 177d9763d55fa989f987a5ae78e5b23ceb1dfa45
  04 2c9ee91e07af7a0dd04e04239eb65d2938986ee4
  05 e2d74e2f1e5d76cfbeedf4c552483db58f4cd17f

1. **[High]** `tasks-todo-sbox/02 ...md` Description + Required work 4, and `tasks/sbox-expert-research.md` "What the research found" bullet 2. The 18 chained prefabs and the 18 prefabs with added components are **disjoint sets** (intersection 0). All 18 chain prefabs (depth 2-3) contain no additions at all; the 18 with `m_AddedComponents` are all depth-1 FBX-base variants (inside task 01's 328), each adding exactly one MonoBehaviour (class 114). There are **0** non-empty `m_AddedGameObjects`, **0** `m_RemovedGameObjects`, **0** `m_RemovedComponents` in all 346 files, so "Added colliders convert" describes a case this package does not have. Fix: in research, state the two 18s separately; in task 02 delete "18 files contain m_AddedGameObjects, m_AddedComponents and m_RemovedGameObjects" and the added-collider sentence, keep additions/removals only as chain-merge semantics; in task 01 add a required-work item that `m_AddedComponents` is reported per file with a count (MonoBehaviours cannot convert) plus an acceptance line that no prefab's added components are dropped silently.

2. **[Medium]** `tasks/sbox-expert-research.md` Relevant History bullet 2: "Base GUIDs resolve to 346 distinct FBX files and 63 prefab references". Actual: 340 distinct base GUIDs = 324 FBX + 16 prefabs; 328 FBX-base references and 18 prefab-base references (500 `PrefabInstance` blocks total across the 346 files - 24 files nest prefab instances). "No external base" is correct. Fix the numbers.

3. **[Medium]** `.../03 ...md` Notes: "Editor/Core/UnitySceneExport.cs (Node builds components; ModelRenderer call at line ~922)". `UnitySceneExport.cs` is 507 lines and has no `Node`; `Converter.Node` is `UnityPrefabExport.cs:882` and the `ModelRenderer` call is `UnityPrefabExport.cs:922`. Fix the file attribution.

4. **[Low]** `.../05 ...md` item 4 example "2,861 of 3,111 prefab instances skipped": 3,111 = 2,861 instances + 250 plain objects; instances across both scenes are 3,062. Reword to "2,861 of 2,861".

5. **[Low]** `.../05 ...md` Description premise "says so one warning per instance": `UnityPrefabExport.cs:812/985` emits one line *per distinct source prefab* and `UnityImport` dedupes with `warnings.Distinct()`; top-level skips are counted silently (`UnitySceneExport.cs:68`) and the log contains 0 per-instance lines. Reword premise and acceptance ("one grouped warning naming the 255 distinct source prefabs / 2,861 skipped instances").

6. **[Low]** `.../04 ...md` item 1: the GUID labels are swapped - `0000000000000000e000000000000000` is *unity default resources* (where the primitive meshes live), `0000000000000000f000000000000000` is *unity_builtin_extra* (matches `UnitySceneExport.cs:36` `BuiltInGuid` for the default skybox). Swap the parenthetical names, keep both values.

7. **[Low]** `tasks/sbox-expert-research.md` Relevant History bullet 1: "1,462 assets:" then a list summing to 1,299 (163 folder pathnames, 9 `.preset`, 2 `.hdr`, 1 `.lighting` unlisted). Say "1,462 pathnames (1,299 files + 163 folders)" or complete the list.

8. **[Low]** `.../01 ...md` item 2 "plus a ModelCollider when the base prefab carries a MeshCollider": the base is an FBX model; colliders arrive as stripped MeshCollider blocks in the variant (218 prefab files carry `MeshCollider` blocks). Reword to "when the variant's base model was imported with colliders".

2026-11-20 22:13 - Verdict: no further changes

Tree: eliots-agentics/tasks-todo-sbox/{01 Prefab Variants - FBX Model Base.md, 02 Prefab Variants - Prefab Base and Chains.md, 03 Scene Import Without Prefab Instances.md, 04 Built-in Unity Primitive Meshes.md, 05 Scene Skip Diagnostics and Report.md}
git hash-object --path=...
  01 cff0c0cd6bd5b209a238417107e38d75d529cb38
  02 7ba1acf84a7d760e3629f490b7335943b820f440
  03 62c9e93c141aac4cd1f24e63f6fbbd113c788960
  04 143dcba4c5c028deb7cdfc2f4f1f11ae2df72480
  05 b0238e0087893cbbf86579b0f8d1b95e4242f9e3

1. **[Low]** (resolved) The 22:04 review's only open item was `.../04 ...md` item 1 citing `UnitySceneExport.cs:36` for the built-in-extra GUID. Confirmed on disk: `Editor/Core/UnitySceneExport.cs:34` is the summary comment and `:35` is `const string BuiltInGuid = "0000000000000000f000000000000000";` (line 36 is blank). The task now reads "`0000000000000000f000000000000000` for builtin extra, which `UnitySceneExport.cs:35` uses for the default skybox", with the two GUID labels correctly oriented.

2. **[Low]** (confirmation, no action) Spot-checked that the citation was the only change: the eight 22:04 fixes are all still on disk verbatim in substance - `05` item 4 "2,861 of 2,861", `05` premise "one warning per distinct source prefab", `03` `UnityPrefabExport.cs:882` for `Converter.Node`, `01` item 7 `m_AddedComponents` reporting plus the 0 `m_AddedGameObjects`/`m_RemovedGameObjects`/`m_RemovedComponents` statement, `01` item 2 "stripped MeshCollider blocks ... (218 of the 346 prefabs do)", research Relevant History "1,462 pathnames (1,299 files + 163 folders)". File line counts unchanged (63/50/52/47/46). No requirement text beyond the single citation moved.

3. **[Low]** (scope) `git status --short` is `?? eliots-agentics/` and `git diff HEAD` is empty, so the five backlog files are untracked-new and nothing tracked outside `eliots-agentics/` was touched; no file under `.humans-only/` was read or written. The five hashes above are the values the worker will commit.

## Notes

Plan (Master doing this alone, per master.md step 1):

1. Inventory the source package and both scenes: plain GameObject vs PrefabInstance counts, prefab asset shapes, which references resolve in the package.
2. Read the generated output and the editor log of the 17:03 import to separate the reported causes from the visible result.
3. Trace the code paths: ImportPlan scene dependency scan, UnitySceneExport plain-object conversion, UnityPrefabExport.Convert's variant guard, ModelRenderer construction.
4. Write one backlog task per independently shippable change in tasks-todo-sbox, each with evidence, scope, acceptance and a headless verification step.
5. No importer code changes in this task.

What the research found:

- The two scenes are not prefab-free. MergeV2.unity has 250 plain GameObjects and 2,861 PrefabInstance blocks; Test_Range.unity has 11 and 201. Neither has a prefab-free object layer large enough to explain the result.
- The actual blocker is prefab variants. All 346 .prefab assets in the package are model prefab variants: their root is a PrefabInstance whose m_SourcePrefab points at an in-package FBX (328 direct, 12 one prefab step from the FBX, 6 two steps). Separately, 18 direct FBX-base variants add exactly one MonoBehaviour (class 114) each; no prefab has a non-empty m_AddedGameObjects, m_RemovedGameObjects or m_RemovedComponents. Every source GUID resolves inside the package, so variant support is self-contained.
- UnityPrefabExport.cs:1173 throws "Prefab has no root transform (prefab variants are not supported)" for each of them. The editor log shows one grouped error: "346 resources: Prefab conversion skipped. Prefab has no root transform (prefab variants are not supported)."
- No prefab was generated (0 .prefab in Assets/imported/assetstoreoriginals/school), so every scene prefab instance was skipped and no __Prefab stub exists in either generated scene. mergev2.scene has 250 objects and 0 stubs; test_range.scene has 11 and 0. The level is empty except for the plain-object layer.
- The plain-object scene path already works for hierarchy, transforms, enabled state, static flags, cameras, lights, audio listeners, box/sphere/capsule colliders and convex MeshColliders. It has two visible gaps: ModelRenderer is built with Materials = null (UnityPrefabExport.cs:262) so a plain object's MeshRenderer material slots are lost, and Unity built-in primitive meshes are skipped (Test_Range's Plane uses built-in mesh fileID 10209, guid 0000000000000000e000000000000000, and produces no renderer).
- The import finished with 1,277 assets, 1,260 warnings and 479 errors in 3 groups. The other two groups are resource-readiness issues (127 lightmap/dependency resources not ready in 30 s, 6 resources never compiled), not scene conversion. Adjacent findings outside this task: two source TGAs are 16-bit and fail the engine image header, and several FBX files carry the asset author's absolute XRef paths, which the engine logs as "could not be made content-relative". Neither is generated by the importer.
- Task split: 01 model-prefab variants (FBX base), 02 prefab-base variants and chains, 03 a no-prefab-scene fixture plus plain-renderer materials, 04 built-in primitives, 05 scene skip diagnostics. 01 and 02 make the school package work; 03 and 04 make genuinely prefab-free scenes complete; 05 makes the next failure explain itself.

## Clarification Q&A


## Relevant History 

The task names the failure but not its cause, so the research tested the premise. Evidence is on disk from the 2026-11-20 17:03 import (generated files 17:03:47, editor log 17:12:17):

- Source package Testing/old_school_project.unitypackage, 1,275,644,739 bytes, 6,959 tar entries, 1,462 pathnames (1,299 files + 163 folders): 2 scenes, 346 prefabs, 344 FBX, 98 .mat, 248 .tga, 142 .png, 97 .exr, 6 .asset, 4 .cs, plus 9 .preset, 2 .hdr and 1 .lighting.
- Every .prefab file starts with a PrefabInstance block (class 1001) or a collider/component block followed by one. No file has a plain Transform root. Root bases are 340 distinct GUIDs: 324 FBX (328 prefab-to-FBX references) and 16 prefabs (18 prefab-to-prefab references). 500 PrefabInstance source references across the 346 files; 24 files nest another prefab. No external base.
- Generated output: 4,108 files under Assets/imported/assetstoreoriginals/school, including 2 .scene and 0 .prefab. Scene JSON counts: mergev2.scene 250 GameObjects / 0 stubs, test_range.scene 11 / 0. Plain components survive (63 spot, 9 point, 2 directional lights, 6 cameras, 6 audio listeners, box colliders).
- Editor log D:/SteamLibrary/steamapps/common/sbox/logs/sbox-dev-2026-11-20.13.log line 3013: 346 prefab conversions skipped for the variant guard; line 3016: 1,277 assets, 1,260 warnings, 479 errors (3 groups).
- Code: UnityPrefabExport.Convert (Editor/Core/UnityPrefabExport.cs:1166-1175), ModelRenderer (same file:262), scene dependency scan (Editor/Core/ImportPlan.cs:127-143), scene conversion loop (Editor/Core/UnityImport.cs:431-447), SceneConversions report fields (same file:482).
- Fixtures available: Testing/old_school_project.unitypackage, Testing/city--1.unitypackage, Testing/building.unitypackage; the Unity source project is C:/cseliot/1-Development/4-Unity/2-Projects/old_school_model. Testing/*, Assets/manual and Assets/imported are gitignored.
- The repo has no Tests/ project; the working headless surface is Documentation/Unity-To-SBox-Automated/harness plus verify_output.py.

## Stylistic Task Notes:
References to variables like X and Y automatically imply that these are to be exposed properties for developer tweaking.

## Skills To Know: for you and/or subagents:
	sbox-agent-playtest
	sbox-live-play-agent-loop
	sbox-cli-driving

## Post-Task Follow Ups 
