## Status: STARTED? YES! CLARIFIED? YES! HISTORY? YES! REVIEWED? YES! DONE? YES! WORKER: MASTER OTHER: research

## Task
	Generate the tasks to: Update project UnityImporterPlus to support importing scenes that don't use prefabs.
		
### Description
	As the unity expert, you the Task Manager, shall do this task, as if you had decided it was small enough for you to do alone.

	The project: C:\cseliot\1-Development\1-SBox\2-Projects\unityimporterplus\
	Research and build tasks in to put in C:\cseliot\1-Development\1-SBox\2-Projects\unityimporterplus\eliots-agentics\tasks-todo-unity correlating to your research. 

	Recently, an attempt was made to import "C:\cseliot\1-Development\1-SBox\2-Projects\unityimporterplus\Testing\old_school_project.unitypackage", with pitiful success.
	The old_school_project.unitypackage was made from project C:\cseliot\1-Development\4-Unity\2-Projects\old_school_model.

### Work Review
	2026-11-20 22:11 - Verdict: changes required

	Round-1 fixes re-verified on disk, all applied: the 323-warning path is the direct RendererSlots lookup at Editor/Core/UnityPrefabBindings.cs:151 (message text matches report.json's prefab-material-assignment group, 323); no trivially-true prefab acceptance remains (task 01 now names Doorplate_03_Snaps014 and a multi-mesh case); the scene warning count reads 459 lines / 455 distinct, and 455 is the distinct count in the saved report's Warnings; 405 scene material overrides (339+56+8+2) is internally consistent; the throw is at UnityPrefabExport.cs:1173; the 1.27 GB re-export is replaced by a handcrafted fixture (task 03 acceptance). Baseline numbers I re-checked against the saved report agree: 1277 assets / 540 converted / 1714 files / 1131 unresolved (346 conversion-skipped + 323 prefab-material-assignment + 445 unassigned-material + 16 missing-texture + 1 ambiguous), 346 .prefab and 344 .fbx assets, both scene files, Roots 4/3 with Instances 0 and Skipped 0. Nothing under .humans-only/ was read; only the task file and the four backlog files are the review target.

	1. [Medium] tasks-todo-unity/04-scene-loss-reporting.md:7 states the loss mechanism wrongly and would send the implementer to a line that already works. "the scene root loop does not count it" is false: Editor/Core/UnitySceneExport.cs:68 counts a null root (`if ( node == null ) { skipped++; continue; }`), and MergeV2's Roots 4 / Skipped 0 proves no root was ever an instance, so that branch never fired. The 2861+201 instances are children of plain group objects and are dropped in UnityPrefabExport.Node's child loop at Editor/Core/UnityPrefabExport.cs:961, where a failed nested instance is silently omitted (`if ( Instance( instance, chain, owed ) is JsonObject nested ) children.Add( nested );`). Compounding it, the scene-side counters at UnitySceneExport.cs:127 and :134 sit behind `Owner(...) is not var (owner, source, g)`, a pattern that can never match, so those `skipped++` branches are dead. Fix: replace the sentence with the child-loop mechanism, cite UnityPrefabExport.cs:961 and the dead guards at UnitySceneExport.cs:113/127/134, and keep the acceptance as written (3062 lost, nonzero exit, 0 after the fixes). Also note tasks-todo-sbox/05 line 7 already describes :68 correctly, so the two sets currently disagree about the same line.

	2. [Low] tasks-todo-unity/02-model-prefab-material-slots.md:7 says Binding() "is used only for MeshFilter/MeshCollider references (:162, :170)". There is a third call: UnityPrefabBindings.cs:129, the SkinnedMeshRenderer (type 137) branch. Fix: ":129 skinned mesh, :162 MeshFilter/MeshRenderer, :170 MeshCollider". The load-bearing claim — that the modification loop bypasses Binding() at :151 — is correct and verified.

	3. [Low] tasks-todo-unity/01-model-prefab-root-conversion.md:17 orders Box/Sphere/Capsule wrapper colliders converted to s&box colliders, but the baseline only counted MeshColliders (0/1/2/6 across 128/208/7/3 prefabs). Fix: state whether any wrapper carries a non-MeshCollider collider, or scope the line to "if present" so a worker does not build converters nothing needs.

	4. [Low] Baseline evidence is not durable. tasks/unity-expert-research.md records numbers but no report path and no runnable harness command, and the supporting artefact (report.json plus the analysis scripts) lives only in the Hermes scratch cache, which prunes after 24h. Four tasks' acceptance criteria rest on those numbers. Fix: record the exact harness invocation and copy report.json (and the analysis scripts) under Documentation/Unity-To-SBox-Automated/ so a later worker can re-check instead of re-running the 1.27 GB import.

	5. [Low] The rejected-supersede decision is right on the merits and needs one more sentence. tasks-todo-sbox/ holds genuinely non-overlapping work (03 plain-object material assignment, 04 built-in primitive meshes, 05 grouped skip diagnostics), and the unity set owns model-wrapper conversion, fixed-ID binding, scene model instances and loss severity; declaring supersede would have dropped coverage. What is still unresolved is ownership of the SceneConversions schema: unity/04 and sbox/05 both add fields there and each only says "reconcile". Fix: in unity/04's Notes, name the fields it will add (TotalInstances/LostInstances beside Roots/Instances/Skipped) and state that sbox/05 must extend that shape rather than define a parallel one.

	Tree: main @ 3a42518, eliots-agentics/ untracked (no tracked file touched; no commit made)
	a22cafb79c21633141ae45365609265dd90e3399  eliots-agentics/tasks-todo-unity/01-model-prefab-root-conversion.md
	f1ce44518b556a178b2e0c819e03591234215d8c  eliots-agentics/tasks-todo-unity/02-model-prefab-material-slots.md
	c1941e5a5b2e57921b72f2fc2903e1b4236bab34  eliots-agentics/tasks-todo-unity/03-scene-model-instance-placement.md
	4de3dda37bf51d7bd816d301ab2906dfce8d68c1  eliots-agentics/tasks-todo-unity/04-scene-loss-reporting.md

	2026-11-20 22:15 - Verdict: changes required

	Round-2 fixes re-verified against source, four of five applied correctly: 04:7 now names the silent child-loop drop at UnityPrefabExport.cs:961 (`if ( Instance( instance, chain, owed ) is JsonObject nested ) children.Add( nested );`, verified) and drops the false claim about the root loop, with :68 correctly described as counting a null root (`if ( node == null ) { skipped++; continue; }`, verified line 68); 02:7's Binding() sites are correct (:34 declaration, :129 skinned, :162 MeshFilter/MeshRenderer, :170 MeshCollider, verified); 01:17's wrapper evidence is now scoped (240 MeshCollider / 59 BoxCollider, no Sphere/Capsule); 04:15 names TotalInstances/LostInstances as this set's fields and makes sbox/05 extend them. The durable baseline in the task file (harness usage `[--scenes] <package> <assets> [report.json]`, `-p:IUP_ROOT` build) matches Documentation/Unity-To-SBox-Automated/harness/{Program.cs:10-14,harness.csproj:13}; not committing the report is accepted. Line citations re-checked and correct: UnityPrefabExport.cs:706 Roots, :1173 throw, UnityPrefabBindings.cs:151 RendererSlots lookup, UnityFbxBindings.cs:80-81 hashed slots, UnityImport.cs:22 Error severity, ImportPlan.cs:127-143 scene dependency links, SourceOf's PrefabPathFor/Converted failure at UnityPrefabExport.cs:809-812. Baseline arithmetic still agrees (3062 = 2861+201; 459 = 255+204; 1131 = 346+323+445+16+1; 405 = 339+56+8+2; 1270 prefab material entries). Nothing under .humans-only/ read; only the task file and the four backlog files reviewed; no harness run and no package extraction.

	1. [Low] tasks-todo-unity/04-scene-loss-reporting.md:7 and :16 cite the dead guards as "UnitySceneExport.cs:113/127/134" and say "their `skipped++` branches are dead". Two of the three are wrong, and one instruction would delete working code. Actual source: :113 is the opening brace of `UnityTransform ParentWorld( long transformId )`; the never-matching `Owner( ... ) is not var (instance, source, g)` guards are at :116 (ParentWorld, which returns NoTransform and has no `skipped++`) and :127 (Nest's Owner guard — the one the sentence means); :134 is `if ( data == null ) { skipped++; return; }`, a live guard that :135 depends on, not an `is not var` pattern. For these scenes :127 never runs because Nest() is only reached for objects attached under a prefab instance (UnitySceneExport.cs:144-165), while the 2861+201 instances hang off plain group objects. Fix: cite :127 (optionally :116) as the unreachable `is not var` guard, and reword :16 so :134 is not described as dead — change "Fix or remove the dead `is not var (...)` guards at UnitySceneExport.cs:113/127/134" to "Fix the `is not var` guard at UnitySceneExport.cs:127 (and :116) so `skipped++` runs when Owner returns null; leave the `data == null` check at :134 intact". Mirror the same correction in the task file's Relevant History line about the dead guards, which repeats 113/127/134. Mechanism, counts and acceptance of 04 are otherwise correct.

	2026-11-20 22:17 - Verdict: no further changes

	Round-3 citation fix verified against source, in both places it was owed. tasks-todo-unity/04-scene-loss-reporting.md:7 now cites the unreachable `Owner( ... ) is not var (owner, source, g)` guard at UnitySceneExport.cs:127 inside Nest, which holds `{ skipped++; return; }`, and the same pattern at :116 in ParentWorld (which returns NoTransform and has no `skipped++`), and separates :134 as a different, live guard; :16 gives the matching instruction ("fix the `is not var` guard at :127 (and :116) ... leave the `data == null` check at :134 intact"). Source re-read at UnitySceneExport.cs:112-137 confirms every citation: :113 is ParentWorld's opening brace and is no longer cited anywhere, :116 is its `is not var` line, :127 is Nest's guard, :134 is `if ( data == null ) { skipped++; return; }`. The task file's Relevant History ('Scene loss mechanism', current line 78) states the same thing correctly: :127 (and :116) unreachable, `skipped++` at :127 dead, :134 live and not part of the bug.

	No regression elsewhere. 01/02/03 are byte-identical to the hashes recorded at round 1 (a22caf…, f1ce445…, c1941e5…), so the round-2 fixes to 01:17, 02:7 and 03 stand untouched. 04 moved from 4de3dda to 85ca50e5, and re-reading it shows the round-2-verified content intact apart from the rewritten citations: mechanism (null root counted at :68, silent drop in the child loop at UnityPrefabExport.cs:961, Nest only reached for objects attached under an instance at :144-165), baseline (3062 = 2861+201 lost, 459 warning lines = 255+204, 455 distinct, Instances 0 / Skipped 0), TotalInstances/LostInstances ownership with sbox/05 extending the same object, and the four acceptance criteria. Carried-forward citations re-checked on this pass: UnityPrefabExport.cs:961, :706, :1173. No harness run, no package extraction, nothing under .humans-only/ read; only the task file and the four backlog files are the review target.

	Observation, no action required: the round-2 log sentence in ## Notes still repeats "dead `is not var` guards at UnitySceneExport.cs:113/127/134" as a record of what round 2 corrected, and the round-3 entry sits under it without its own dated `Verdict:` header. Round 3's own sentence immediately states the corrected lines, and no backlog file cites 113/134 as dead any more, so this is log tidy-up only.

	Tree: main @ 3a42518, eliots-agentics/ untracked (no tracked file touched; no commit made)
	a22cafb79c21633141ae45365609265dd90e3399  eliots-agentics/tasks-todo-unity/01-model-prefab-root-conversion.md
	f1ce44518b556a178b2e0c819e03591234215d8c  eliots-agentics/tasks-todo-unity/02-model-prefab-material-slots.md
	c1941e5a5b2e57921b72f2fc2903e1b4236bab34  eliots-agentics/tasks-todo-unity/03-scene-model-instance-placement.md
	85ca50e5d9639b2014f23068dcf2352cd3be9f13  eliots-agentics/tasks-todo-unity/04-scene-loss-reporting.md

	2026-11-20 23:34 - Verdict: changes required

	Cross-review round: the adopted sbox-derived additions checked against source and against the sbox files themselves, not the round-5 summary. All four adopted details are accurate. 01: ModelRenderer at Editor/Core/UnityPrefabExport.cs:262 really does set `["MaterialOverride"] = null` and `["Materials"] = null`; static flags are applied at :904 (`if ( go != null && Number( go.Body, "m_StaticEditorFlags" ) != 0 ) node["Flags"] = StaticFlag;`); the mirrored-variant sentence matches the `_mirror_{axis}` naming at UnityPrefabExport.cs:396 for prefabs and UnityImport.cs:385 for VMDLs. 02: the fixed-ID usage counts (410/313/267/98) and the 323/445/1270/405 baseline are repeated verbatim in tasks-todo-sbox/02, so the "near-twin" claim holds for this pair. 03: `Node( Block transform, bool isRoot, ... )` is at :882 and the indexed override shape is exactly `["Materials"] = { ["indexed"] = { [i] = material } }` at :1041-1042; UnitsPerMetre = 39.37 at :27 backs 05's "39.37 model modifier". 04: :68 null-root count, the dead `is not var` guards at :116 and :127, the live `data == null` guard at :134, the silent child-loop drop at :961 and the throw at :1173 all match on re-read; sbox/05 now states the identical merged schema (TotalInstances/LostInstances beside Roots/Instances/Skipped, SkipCauses, MaxSampleNames as a named constant of 10) and claims sole ownership of it, which unity/04 honours by saying "work one task, not both" rather than defining a parallel shape. 05: no invented engine path survived the port — sbox/04's `D:/SteamLibrary/...` hint is gone and replaced by "confirm any engine model path in the installed engine, never guess it"; the primitive fileIDs are hedged with "verify every ID against the Unity 6 built-in resources before coding", the only one asserted as observed (Plane 10209, guid 0000000000000000e000000000000000) is confirmed in the source scene YAML at test_range.unity:7493, and the e000/f000 split matches UnitySceneExport.cs:35 using f000 for the built-in skybox. No contradictions introduced between 01/02/03/04/05: each names non-goals that match the actual split (01 prefab root, 02 fixed-ID materials, 03 scene instances/plain objects, 04 loss reporting, 05 primitives).

	1. [Low] tasks-todo-unity/03-scene-model-instance-placement.md:38 misstates the sbox twin. "tasks-todo-sbox/01 acceptance also expects the 2861/201 stubs" is false: sbox/01's Acceptance bullets (:22-27) contain no stub expectation at all, and the stub claim sits in its Notes (:36) with the opposite ordering — "expect the stubs only once 03 lands" — reinforced by its Follow Ups ("Task 03 then resolves scene instances"). The substantive point of the sentence (both sets must agree on the final 2861/201 counts) is correct and unity/03's own acceptance carries those numbers. Fix: replace that clause with "tasks-todo-sbox/01 notes the 2861/201 stubs as expected once its task 03 lands; both sets must agree on the final counts."

	Everything else is clean: no finding against 01, 02, 04 or 05, and the round-3 guard citations remain correct. Scope respected: only the task file and the five unity backlog files were reviewed; no file outside eliots-agentics/ was touched; nothing under .humans-only/ read; no harness run and no package re-extraction (the extracted source scene already on hand was read for the Plane reference).

	Tree: main @ 35dde86, five unity backlog files tracked and modified vs HEAD (round-5 edits), tasks-todo-unity/05 still untracked (no commit made)
	ca79b64e84339fdfef090435f92ad370ed3bcfbd  eliots-agentics/tasks-todo-unity/01-model-prefab-root-conversion.md
	5a656ae43c167c1d47b4e7539d7581ba44e069e3  eliots-agentics/tasks-todo-unity/02-model-prefab-material-slots.md
	ed2de7856d3ab8674bfdea11f81f32f368b3bcb4  eliots-agentics/tasks-todo-unity/03-scene-model-instance-placement.md
	4f011bf1ae0dd9bb7569be41aef026382ba4f8eb  eliots-agentics/tasks-todo-unity/04-scene-loss-reporting.md
	a261077a460d11bf2aa3261895358ae09d830a45  eliots-agentics/tasks-todo-unity/05-builtin-primitive-meshes.md

	2026-11-20 23:35 - Verdict: no further changes

	Cross-review fix verified on disk; the corrected cross-reference is accurate. tasks-todo-unity/03:38 now reads "tasks-todo-sbox/01 Notes says to expect the 2861/201 stubs only once task 03 lands, so both sets must agree on the final counts." sbox/01's Acceptance (:22-27) carries no stub expectation — it covers 346 .prefab outputs, Doorplate_03's single ModelCollider, the script-bearing files, byte-identical re-runs and a clean build — while the stub claim sits in its Notes (:36): "Scene side after this task: MergeV2 2,861 instances and Test_Range 201 become resolvable, but task 03 owns placing direct model instances and plain objects; expect the stubs only once 03 lands," reinforced by its Follow Ups ("task 03 then resolves scene instances of these prefabs and direct model instances"). The clause now names Notes, keeps the correct ordering, and preserves the original point that both sets must agree on the final counts, which unity/03's own acceptance (:28) states as Instances 2861 / Skipped 0 and 201 / 0. The same line's other half also holds: sbox/03 is a reconciled twin of this task (:27 repeats the 2,861/201 / 0 acceptance, :18 the same fixture, :36 the same fixture-placement guidance).

	No unexpected change. 01/02/04/05 are byte-identical to the round-5 hashes (ca79b64…, 5a656ae…, 4f011bf…, a261077…) with mtimes still at 23:30-23:32; only 03 moved (ed2de785… → 80f989a…) at 23:35:14, and its one edited sentence is the clause the finding named, with the rest of the file matching what round 5 verified. The five unity backlog files remain the only task work: tasks-todo-sbox was rewritten by the sbox expert earlier (23:25) and is untouched by this round, nothing outside eliots-agentics/ changed, and nothing under .humans-only/ was read, listed or staged. No harness run and no package extraction on this pass. No commit made.

	Tree: main @ 35dde86, five unity backlog files tracked and modified vs HEAD (round-5 edits plus this fix); tasks-todo-unity/05 still untracked (no commit made)
	ca79b64e84339fdfef090435f92ad370ed3bcfbd  eliots-agentics/tasks-todo-unity/01-model-prefab-root-conversion.md
	5a656ae43c167c1d47b4e7539d7581ba44e069e3  eliots-agentics/tasks-todo-unity/02-model-prefab-material-slots.md
	80f989ae8173e99eda3ba2a22b1e47d7266990ed  eliots-agentics/tasks-todo-unity/03-scene-model-instance-placement.md
	4f011bf1ae0dd9bb7569be41aef026382ba4f8eb  eliots-agentics/tasks-todo-unity/04-scene-loss-reporting.md
	a261077a460d11bf2aa3261895358ae09d830a45  eliots-agentics/tasks-todo-unity/05-builtin-primitive-meshes.md

## Notes
	Plan:
	1. Reproduce the old_school import headlessly with Documentation/Unity-To-SBox-Automated/harness.
	2. Read the report and the Unity YAML to name the exact failure, not the symptom.
	3. Write ordered backlog tasks under tasks-todo-unity, each with baseline evidence and acceptance criteria.
	4. Send the tree to a Review Critic, fix findings, commit.

	Result: five tasks, ordered by dependency:
	 01-model-prefab-root-conversion.md
	 02-model-prefab-material-slots.md
	 03-scene-model-instance-placement.md
	 04-scene-loss-reporting.md
	 05-builtin-primitive-meshes.md

	The task premise is inaccurate for this package: both scenes use prefab instances heavily. The real failure is that every prefab in the package is a model-wrapper prefab (a PrefabInstance of an FBX), which UnityPrefabExport rejects as an unsupported prefab variant, leaving the scenes empty.

	Review round 1 (changes required, applied): corrected the material-warning code path to the direct RendererSlots lookup at UnityPrefabBindings.cs:151, removed the trivially-true prefab label acceptance, corrected the scene warning count to 459 lines / 455 distinct, corrected 405 scene material overrides, corrected the throw to line 1173, and replaced the re-export-the-1.27-GB-project acceptance with a small handcrafted fixture. Rejected the request to declare this set supersedes tasks-todo-sbox: the sets overlap but are complementary, so reconciliation notes were added instead.

	Review round 2 (changes required, applied): corrected the scene-loss mechanism to the silent child-loop drop at UnityPrefabExport.cs:961 and the dead `is not var` guards at UnitySceneExport.cs:113/127/134; added the third Binding() call site (:129); corrected wrapper component evidence (240 MeshColliders/218 files, 59 BoxColliders/58 files, 270 MonoBehaviours/221 files, no Sphere/Capsule); recorded the durable harness command and baseline metrics instead of committing generated reports; named tasks-todo-sbox/05 as the owner of the grouped SkipCauses extension on the SceneConversions fields this set adds.

	Review round 3 (changes required, applied): corrected the guard citations. The unreachable `is not var` guard is UnitySceneExport.cs:127 (and :116); :134 is a live `data == null` check and is left intact. Task 04 and this file's Relevant History now say so.

	Review round 5 (user-requested cross-review of tasks-todo-sbox, applied): the sbox expert rewrote their backlog at 23:23-23:25 and reconciled it against this research. Their 01/02/03/05 now mirror unity 01/02/03/04 and add useful detail; their 04 (built-in Unity primitive meshes) was a gap here. Adopted into the unity set: m_StaticEditorFlags and grouped unknown-path reporting plus mirrored-variant preservation (01); fixed-ID usage counts and non-goals (02); plain-scene-object renderer materials and a richer fixture (03); SkipCauses/MaxSampleNames schema detail (04); new unity 05 for built-in primitive meshes. The sbox set's 01/02/03/05 are near-twins of ours; work one set, not both. Its earlier 328-direct/18-chained counts were a parse error and its own files now use the verified 346/322/24 split.

	Cross-review round (changes required, applied): corrected the unity/03 cross-reference. sbox/01 has no stub acceptance; its Notes say to expect the 2861/201 stubs only once task 03 lands.

## Clarification Q&A


## Relevant History 
	Baseline run (2026-11-20, current tree):
	- Harness: `dotnet <harness.dll> --scenes Testing/old_school_project.unitypackage <scratch>/assets <scratch>/report.json` (build with `dotnet build Documentation/Unity-To-SBox-Automated/harness/harness.csproj -c Release -p:IUP_ROOT=<project>`). The saved baseline report used for these numbers is C:/Users/Eliot/AppData/Local/hermes/profiles/hermes-unity/cache/scratch/iup-old-out/report.json; the scratch cache prunes, so re-run the harness and expect the metrics below rather than relying on the file. Do not commit generated reports or ad-hoc analysis scripts.
	- 1277 assets, 540 converted, 1714 files, 1131 unresolved issues, exit 1.
	- 346/346 .prefab assets fail with "Prefab conversion skipped. Prefab has no root transform (prefab variants are not supported)." Zero .prefab outputs are generated.
	- SceneConversions: MergeV2 4 roots / 0 instances; Test_Range 3 roots / 0 instances.
	- Source scenes contain 2861 and 201 PrefabInstance blocks (MergeV2: 255 distinct prefab guids, all present in the package). The generated scenes keep only the empty group objects (Setting, Structure, Props, JapaneseSchoolSettings), so the import produces empty scenes.
	- Prefab shape: all 346 have a top-level PrefabInstance whose m_SourcePrefab is an FBX. 322 files contain only that instance; 24 also nest one or more .prefab instances (Chair_01 has 2, Stairs_Snaps015 has 70). 18 files list components in m_AddedComponents; separately, 221 files contain 270 MonoBehaviour (class 114) blocks attached to stripped model objects. None has a top-level .prefab source, so none is a true prefab variant.
	- Wrapper-added components: 240 MeshColliders across 218 prefabs (208 with 1, 7 with 2, 3 with 6); 59 BoxColliders across 58 prefabs (57 with 1, 1 with 2); no SphereCollider or CapsuleCollider attached to a model object. Material overrides: 1270 m_Materials.Array.data entries.
	- Material baseline: 323 prefab-material-assignment warnings ("unsupported or unidentified renderer"), 445 unassigned-material issues, only 16 missing-texture, 1 ambiguous. The 323 come from the direct RendererSlots lookup at Editor/Core/UnityPrefabBindings.cs:151, not from Binding().
	- Scene overrides: MergeV2 has 405 material-array overrides (339 data[0], 56 data[1], 8 data[2], 2 data[3]), 3654 static-flag overrides, 2861 transform/name instances; Test_Range has 201 instances, 1 material override, 2 MeshCollider blocks. All instances are parented to plain non-prefab Transform objects.
	- Scene skip warnings: 459 lines (255 MergeV2 + 204 Test_Range, 455 distinct) because SourceOf caches per prefab guid; they do not count instances and are not unresolved errors.
	- Scene loss mechanism: a null top-level root is counted at UnitySceneExport.cs:68, but the 2861+201 instances are children dropped silently in UnityPrefabExport.Node's child loop at UnityPrefabExport.cs:961. The `Owner(...) is not var (...)` guard at UnitySceneExport.cs:127 (and :116) can never match, so the `skipped++` at :127 is dead; :134 is a live `data == null` check, not part of this bug.
	- Plain-object gap: ModelRenderer is built with Materials and MaterialOverride null (Editor/Core/UnityPrefabExport.cs:262), so a plain scene MeshRenderer's assigned material is dropped. No fixture covers a scene whose only model reference is a plain MeshFilter.
	- Built-in primitives: Test_Range.unity's two plain MeshFilters reference Unity's built-in Plane (fileID 10209, guid 0000000000000000e000000000000000). Converter.Node finds no package asset and warns "mesh ... is not a converted model; renderer skipped" (Editor/Core/UnityPrefabExport.cs:915-920); the scene has no ModelRenderer.
	- UnityPrefabExport.Convert: Roots is built from non-stripped Transform blocks (Editor/Core/UnityPrefabExport.cs:706) and throws when empty (line 1173). UnityPrefabBindings maps renderer IDs through UnityModel.RendererSlots hashes and legacy recycle IDs only; Unity's fixed model-prefab IDs (100000, 400000, 2300000, 4300000/4300002) are not mapped.
	- Direct FBX instances in a scene fail the same way: UnitySceneExport resolves instances only through prefabPaths/Converted and warns "Nested prefab ... is not in the package or was not converted; instance skipped" when the source is an FBX.
	- tasks-todo-sbox holds the parallel sbox-expert backlog. As of the 2026-11-20 23:23 rewrite it is reconciled with this research: sbox 01/02/03/05 are near-twins of unity 01/02/03/04, and sbox 04 (built-in primitive meshes) is mirrored here as unity 05. Its earlier 328-direct / 18-chained split was a parse error and its own files now use the verified 346/322/24 split. Work one set, not both.

## Stylistic Task Notes:
References to variables like X and Y automatically imply that these are to be exposed properties for developer tweaking.

## Post-Task Follow Ups 
