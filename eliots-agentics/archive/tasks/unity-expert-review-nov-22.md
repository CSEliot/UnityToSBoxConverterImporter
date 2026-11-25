## Status: STARTED? YES! CLARIFIED? YES! HISTORY? YES! REVIEWED? YES! DONE? YES! WORKER: sa-0-16430c08 OTHER: review and task-08 feedback written; critic review recorded below

## Task
Analyze the latest unity importer log report and build feedback


### Description
After the hdrp update and the non-prefab-support update, I ran another import test of old_school_project and the export log of that is at:
C:\cseliot\1-Development\1-SBox\2-Projects\unityimporterplus\Testing

Review the errors, and find the related task in the unityimporterplus/tasks folder, unmark it done and leave notes in the ##Post-Task Follow Ups section of that task w ideas on what went wrong and how to fix.

### Work Review

2026-11-22 02:49 - Verdict: no further changes

	Tree: HEAD 124edad; working tree re-checked at review time. Task-scoped files are only this untracked task file (read directly) and the tracked task 08. The M entries for Editor/Core/UnityImport.cs, Editor/Core/UnityFbxRewrite.cs and Documentation/Unity-To-SBox-Automated/harness/Program.cs, plus the other untracked runtime .md files, belong to other sessions and were not judged. No project source file was edited by this task.
	git hash-object --path="eliots-agentics/tasks/08 HDRP Lit PBR and Emission Fidelity.md" "eliots-agentics/tasks/08 HDRP Lit PBR and Emission Fidelity.md" -> 06e59fe7317d1d5b16ec7fe4003c1b81f897ea40

	Confirmed real on disk, no change required:
	- Scope: the report's Unresolved array holds 81 objects, all Severity Warning, codes 44 unassigned-material / 20 prefab-material-assignment / 16 missing-texture / 1 ambiguous-material. A script re-parsed the report and matched all 81 extract lines of this file's "Review evidence (worker, 2026-11-22)" section to the objects at their cited Asset lines: 0 mismatches. The 428 "compiled resource dependencies did not become ready" strings are named only as out of scope (this file's Plan item 1, Review evidence section, Q&A answer Q1, Parent decisions Q1 bullet and Relevant History) and never analysed; the report actually has 434 Completion.Errors, the other 6 being the "no compiled resource became available" variant, so the deliverable's "428 ... did not become ready strings" wording is exact, not an error.
	- task 08 diff: exactly two hunks — line 1 DONE? YES! -> DONE? NO! with the other six header fields byte-identical, and a 21-line feedback block inserted at :182 after the unit-emission note (:179-180) and before the existing ### Work Review. The unit-emission note and all three pre-existing ### Work Review entries (:63, :203, :219) are byte-intact.
	- Attribution limit and "Every code change below is a PROPOSAL — NOT APPLIED. This review edited no project source file" both at task 08 :184; all five proposals are individually marked PROPOSAL. No scratch path cited in either task file (the sole "scratch" token is this file's Plan item 5 saying the extract was written "without scratch files").
	- Citations re-checked against the CURRENT tree: UnityImport.cs :122 (PrepareFiles), :327-331, :332, :334-335, :338-340, :346-352, :360-362, :675-700 (ModelSplit); ImportPlan.cs :75-79, :78, :85-86, :91, :93-99, :95-96, :98, :109; UnityPrefabBindings.cs :69 (WrapperMaterialSlot), :97-121 (Assign), :104 (index guard), :251-260, :262-275, :265, :268, :270-272, :274; UnityModel.cs :86 (EffectiveSlots), :89, :92, :124-131 (RenderedMaterials). Every cited line still contains the described code; the parallel UnityImport.cs/UnityFbxRewrite.cs edits caused no drift.

	Findings (both Low and optional):
	1. [Low] task 08 :186 — the appended feedback does not quote task 08's header before/after or list the exact task-08 lines changed, which the worker's own Plan item 5 in this file promised. Fix: add one clause at task 08 :186 quoting "DONE? YES! -> DONE? NO!" and "lines 1, 182-201". Not a Master requirement; the goal is already met without it.
	2. [Low] task 08 :190 and :196 — three cited ranges are slightly short: UnityPrefabBindings.Assign spans :97-124 (not :97-121), its index guard runs :104-114 (not :104-110), and UnityImport.ModelSplit ends at :701 (not :700). Fix: widen those ranges if precision matters. No code drifted; correctness is unaffected.

	False alarms checked and dismissed: the 434 vs 428 Completion.Errors total (428 is the exact count of the quoted timeout subset), and the three short line ranges above (the described code is present inside each cited span). No [High] or [Medium] findings. Commit as is.

## Notes

	Plan (Task Worker, gate 3 — NOT started; awaiting PERMISSION). Decisions applied from ## Clarification Q&A: Q1 = B, Q2 = Task Master decision, Q3 = B.

	1. Scope: review ONLY the 81 Unresolved issue objects in Testing/old_school_project+import-report+post-hdrp-update+post-no-prefabs-update.json (:2220-2707). Every one carries Severity "Warning" (no Error-severity entries); measured counts are 44 unassigned-material, 20 prefab-material-assignment, 16 missing-texture and 1 ambiguous-material. I will group by Code and Asset, read every Message, and keep the report line numbers with each example. The 428 Completion.Errors engine-side "compiled resource dependencies did not become ready" strings (:24063-24497) are explicitly OUT OF SCOPE for this pass and will be named once in the notes for that reason only — no analysis of them.
	2. Related task (settled by Q2): '08 HDRP Lit PBR and Emission Fidelity.md'. Evidence for the choice is the Master's record in Q&A: task 08's shipped diff touched Editor/Core/UnityImport.cs, which owns slot-keyed material remaps, making it the closest live fit among the 8 DONE files. The notes will state the attribution limit explicitly: the true content match, '02 Model-Prefab Material Slots', is archived under .humans-only/archive/ and is off-limits; no new task file is created. '07 HDRP Lit Surface Semantics.md' was the earlier alternative and is now excluded.
	3. Files I will edit only after PERMISSION: (a) '08 HDRP Lit PBR and Emission Fidelity.md' — change ONLY the DONE? value on its line 1 (DONE? YES! -> DONE? NO!), keep its other six header fields byte-identical, and APPEND feedback to its existing ## Post-Task Follow Ups after the unit-emission note currently at lines 179-180; that note and all ### Work Review entries stay untouched; (b) this main task file, for state and evidence. No project source file, no other task file, nothing under .humans-only/, no new file. Task 08's mtime will be re-checked immediately before writing; the user runs parallel sessions on this tree.
	4. Feedback to append (Q3 = B, every code change marked PROPOSAL — NOT APPLIED): what went wrong per Code group, with representative Asset + Message + report line; then concrete files/functions and the exact slot/remap logic proposed as fixes — the slot-keyed remap table built in UnityImport.PrepareFiles, renderer/slot ID resolution in UnityPrefabBindings (fixed Unity fileIDs versus hashed IDs), per-mesh slot naming in UnityModel, and the texture-to-material dependency inference behind the ambiguous-material case. I will verify each function name and path against the on-disk source during the implementation turn before citing it, and give re-check counts alongside each proposal. No source file will be edited by this task.
	5. Evidence on disk: all cited report line numbers, the per-code counts, and a compact issue extract (Asset, Code, Message, report line) will be written into this task file's notes so a reviewer sees them without scratch files; the appended notes in task 08 will name the exact lines changed and quote its header line before/after. No git command that changes state; no commit in this turn.

### Review evidence (worker, 2026-11-22)

	Source: Testing/old_school_project+import-report+post-hdrp-update+post-no-prefabs-update.json, Unresolved array (:2220-2707). Parsed all objects with a script and cross-checked against a full JSON parse: 81 objects, every Severity "Warning" (no Error-severity entries). Per-code counts: unassigned-material 44, prefab-material-assignment 20, missing-texture 16, ambiguous-material 1. The 428 engine-side Completion.Errors timeouts are explicitly OUT OF SCOPE for this pass and are not analysed here.
	Compact issue extract — each line is: report Asset line | Code | Asset | Message (verbatim):
	L2222 | missing-texture | AssetStoreOriginals/School/Art/Material/Structure/Floor/Earth_Floor_Snaps014.mat | Missing or unsupported texture 6012db391d795e54f8166f209f6ce80c.
	L2228 | missing-texture | AssetStoreOriginals/School/Art/Material/Structure/Floor/Earth_Floor_Snaps014.mat | Missing or unsupported texture 8a1b6f6da87cf3c4dbeeeb50c35ed3c5.
	L2234 | missing-texture | AssetStoreOriginals/School/Art/Material/Structure/Floor/Wood_Floor_01_Snaps014.mat | Missing or unsupported texture 6012db391d795e54f8166f209f6ce80c.
	L2240 | missing-texture | AssetStoreOriginals/School/Art/Material/Structure/Floor/Wood_Floor_01_Snaps014.mat | Missing or unsupported texture 8a1b6f6da87cf3c4dbeeeb50c35ed3c5.
	L2246 | missing-texture | AssetStoreOriginals/School/Art/Material/Structure/Wall/Exterior_Wall_01_Snaps014.mat | Missing or unsupported texture 6012db391d795e54f8166f209f6ce80c.
	L2252 | missing-texture | AssetStoreOriginals/School/Art/Material/Structure/Wall/Exterior_Wall_01_Snaps014.mat | Missing or unsupported texture 8a1b6f6da87cf3c4dbeeeb50c35ed3c5.
	L2258 | missing-texture | AssetStoreOriginals/School/Art/Material/Structure/Wall/Exterior_Wall_02_Snaps014.mat | Missing or unsupported texture 6012db391d795e54f8166f209f6ce80c.
	L2264 | missing-texture | AssetStoreOriginals/School/Art/Material/Structure/Wall/Exterior_Wall_02_Snaps014.mat | Missing or unsupported texture 8a1b6f6da87cf3c4dbeeeb50c35ed3c5.
	L2270 | missing-texture | AssetStoreOriginals/School/Art/Material/Structure/Wall/Interior_Skirting_line_Snaps015.mat | Missing or unsupported texture 6012db391d795e54f8166f209f6ce80c.
	L2276 | missing-texture | AssetStoreOriginals/School/Art/Material/Structure/Wall/Interior_Skirting_line_Snaps015.mat | Missing or unsupported texture 8a1b6f6da87cf3c4dbeeeb50c35ed3c5.
	L2282 | missing-texture | AssetStoreOriginals/School/Art/Material/Structure/Wall/Interior_Wall_01_Snaps014.mat | Missing or unsupported texture 6012db391d795e54f8166f209f6ce80c.
	L2288 | missing-texture | AssetStoreOriginals/School/Art/Material/Structure/Wall/Interior_Wall_01_Snaps014.mat | Missing or unsupported texture 8a1b6f6da87cf3c4dbeeeb50c35ed3c5.
	L2294 | missing-texture | AssetStoreOriginals/School/Art/Material/Structure/Wall/Interior_Wall_Blue_Snaps014.mat | Missing or unsupported texture 6012db391d795e54f8166f209f6ce80c.
	L2300 | missing-texture | AssetStoreOriginals/School/Art/Material/Structure/Wall/Interior_Wall_Blue_Snaps014.mat | Missing or unsupported texture 8a1b6f6da87cf3c4dbeeeb50c35ed3c5.
	L2306 | missing-texture | AssetStoreOriginals/School/Art/Material/Structure/Wall/Interior_Wal_Red_Snaps014.mat | Missing or unsupported texture 6012db391d795e54f8166f209f6ce80c.
	L2312 | missing-texture | AssetStoreOriginals/School/Art/Material/Structure/Wall/Interior_Wal_Red_Snaps014.mat | Missing or unsupported texture 8a1b6f6da87cf3c4dbeeeb50c35ed3c5.
	L2318 | prefab-material-assignment | AssetStoreOriginals/School/Art/Model/Props/Bathroom/Partition_02_Snaps014.FBX | Prefab assignment for slot 'Plastic' is missing or conflicting (Metallic_01_Snaps014 vs Plastic_01_Snaps014); no material was guessed.
	L2324 | prefab-material-assignment | AssetStoreOriginals/School/Art/Model/Props/Bathroom/Partition_02_Snaps014.FBX | Prefab assignment for slot 'partition01_3' is missing or conflicting (Plastic_01_Snaps014 vs Plastic_02_Snaps014); no material was guessed.
	L2330 | prefab-material-assignment | AssetStoreOriginals/School/Art/Model/Props/Bathroom/Partition_02_Snaps014.FBX | Prefab assignment for slot 'lambert4' is missing or conflicting (Metallic_01_Snaps014 vs Plastic_02_Snaps014); no material was guessed.
	L2336 | prefab-material-assignment | AssetStoreOriginals/School/Art/Model/Props/Bathroom/Partition_03_Snaps014.FBX | Prefab assignment for slot 'partition01_5' is missing or conflicting (Plastic_01_Snaps014 vs Plastic_02_Snaps014); no material was guessed.
	L2342 | prefab-material-assignment | AssetStoreOriginals/School/Art/Model/Props/Bathroom/Partition_03_Snaps014.FBX | Prefab assignment for slot 'plastic' is missing or conflicting (Plastic_01_Snaps014 vs Plastic_02_Snaps014); no material was guessed.
	L2348 | prefab-material-assignment | AssetStoreOriginals/School/Art/Model/Props/Classroom/Counter01_Snaps014.fbx | Prefab assignment for slot 'metal' is missing or conflicting (Glass_Snaps014 vs Metallic_01_Snaps014); no material was guessed.
	L2354 | prefab-material-assignment | AssetStoreOriginals/School/Art/Model/Structure/Doors/Door_01/Door_Frame_01_Snaps014.fbx | A prefab material override puts 'Trim_02_Snaps014' in slot 3 of mesh 'Door_Frame_01_LOD00', which the FBX gives no material slot for.
	L2360 | prefab-material-assignment | AssetStoreOriginals/School/Art/Model/Structure/Doors/Door_06_Snaps014.fbx | A prefab material override puts 'Wood_05_Snaps014' in slot 2 of mesh 'Door_06_LOD00', which the FBX gives no material slot for.
	L2366 | prefab-material-assignment | AssetStoreOriginals/School/Art/Model/Structure/Doors/Door_06_Snaps014.fbx | A prefab material override puts 'Wood_05_Snaps014' in slot 2 of mesh 'Door_06_LOD01', which the FBX gives no material slot for.
	L2372 | prefab-material-assignment | AssetStoreOriginals/School/Art/Model/Structure/Stair/Floor_stairs_bottom_Snaps014.fbx | Prefab assignment for slot 'Wall_exterior' is missing or conflicting (Exterior_Wall_01_Snaps014 vs Exterior_Wall_02_Snaps014); no material was guessed.
	L2378 | ambiguous-material | AssetStoreOriginals/School/Art/Model/Structure/Stair/Floor_stairs_bottom_Snaps014.fbx | Color texture Wall_02_Albedo_Snaps014.tga matches multiple materials; no dependency was inferred.
	L2384 | prefab-material-assignment | AssetStoreOriginals/School/Art/Model/Structure/Stair/Floor_stairs_middle_Snaps014.fbx | Prefab assignment for slot 'Wall_exterior1' is missing or conflicting (Exterior_Wall_01_Snaps014 vs Exterior_Wall_02_Snaps014); no material was guessed.
	L2390 | prefab-material-assignment | AssetStoreOriginals/School/Art/Model/Structure/Stair/Floor_stairs_top_Snaps014.fbx | Prefab assignment for slot 'Wall_exterior8' is missing or conflicting (Exterior_Wall_01_Snaps014 vs Exterior_Wall_02_Snaps014); no material was guessed.
	L2396 | prefab-material-assignment | AssetStoreOriginals/School/Art/Model/Structure/Stair/Stairs_top_Snaps014.fbx | Prefab assignment for slot 'lambert10' is missing or conflicting (Metallic_01_Snaps014 vs Wood_06_Snaps014); no material was guessed.
	L2402 | prefab-material-assignment | AssetStoreOriginals/School/Art/Model/Structure/Stair/Stairs_top_Snaps014.fbx | Prefab assignment for slot 'lambert11' is missing or conflicting (Interior_Wall_01_Snaps014 vs Wood_06_Snaps014); no material was guessed.
	L2408 | prefab-material-assignment | AssetStoreOriginals/School/Art/Model/Structure/Stair/Stairs_top_Snaps014.fbx | Prefab assignment for slot 'pasted__lambert19' is missing or conflicting (Interior_Skirting_line_Snaps015 vs Trim_02_Snaps014); no material was guessed.
	L2414 | prefab-material-assignment | AssetStoreOriginals/School/Art/Model/Structure/Wall/Classroom_Windows_Wall_01_Snaps014.fbx | A prefab material override puts 'Exterior_Wall_01_Snaps014' in slot 3 of mesh 'Classroom_Windows_Wall_01_LOD00', which the FBX gives no material slot for.
	L2420 | prefab-material-assignment | AssetStoreOriginals/School/Art/Model/Structure/Wall/Outdoor_Wall_01_Snaps014.fbx | A prefab material override puts 'School_Wall_Snaps014' in slot 1 of mesh 'Outdoor_Wall_01', which the FBX gives no material slot for.
	L2426 | prefab-material-assignment | AssetStoreOriginals/School/Art/Model/Structure/Windows/Windows_02/Windows_02_Frame_Snaps014.fbx | A prefab material override puts 'Trim_02_Snaps014' in slot 2 of mesh 'Windows_02_Frame_LOD00', which the FBX gives no material slot for.
	L2432 | prefab-material-assignment | AssetStoreOriginals/School/Art/Model/Structure/Windows/Windows_02/Windows_02_Frame_Snaps014.fbx | A prefab material override puts 'Glass_Snaps014' in slot 1 of mesh 'Windows_02_Frame_LOD01', which the FBX gives no material slot for.
	L2438 | prefab-material-assignment | AssetStoreOriginals/School/Art/Model/Structure/Windows/Windows_04/Windows_04_Frame_Snaps014.fbx | A prefab material override puts 'Metallic_02_Snaps014' in slot 1 of mesh 'Windows_04_Frame_LOD01', which the FBX gives no material slot for.
	L2444 | unassigned-material | AssetStoreOriginals/School/Art/Model/Props/Bathroom/Partition_01_Snaps014.FBX | No converted material was resolved for slot '15 - Default'.
	L2450 | unassigned-material | AssetStoreOriginals/School/Art/Model/Props/Bathroom/Pipe/Floor_Drain_01_Snaps014.FBX | No converted material was resolved for slot '16 - Default'.
	L2456 | unassigned-material | AssetStoreOriginals/School/Art/Model/Props/Bathroom/Pipe/Metal_Edge_01_Snaps014.FBX | No converted material was resolved for slot '16 - Default'.
	L2462 | unassigned-material | AssetStoreOriginals/School/Art/Model/Structure/Ceiling/Restroom_Ceiling_01_Snaps014.fbx | No converted material was resolved for slot 'lambert2'.
	L2468 | unassigned-material | AssetStoreOriginals/School/Art/Model/Structure/Doors/Door_01/Door_01_01_Snaps014.fbx | No converted material was resolved for slot 'Metal'.
	L2474 | unassigned-material | AssetStoreOriginals/School/Art/Model/Structure/Doors/Door_01/Door_01_01_Snaps014.fbx | No converted material was resolved for slot 'Glass'.
	L2480 | unassigned-material | AssetStoreOriginals/School/Art/Model/Structure/Doors/Door_01/Door_01_02_Snaps014.fbx | No converted material was resolved for slot 'Metal'.
	L2486 | unassigned-material | AssetStoreOriginals/School/Art/Model/Structure/Doors/Door_01/Door_01_02_Snaps014.fbx | No converted material was resolved for slot 'Glass'.
	L2492 | unassigned-material | AssetStoreOriginals/School/Art/Model/Structure/Doors/Door_01/Door_01_03_Snaps014.fbx | No converted material was resolved for slot 'Metal'.
	L2498 | unassigned-material | AssetStoreOriginals/School/Art/Model/Structure/Doors/Door_01/Door_01_03_Snaps014.fbx | No converted material was resolved for slot 'Glass'.
	L2504 | unassigned-material | AssetStoreOriginals/School/Art/Model/Structure/Doors/Door_01/Door_01_04_Snaps014.fbx | No converted material was resolved for slot 'Metal'.
	L2510 | unassigned-material | AssetStoreOriginals/School/Art/Model/Structure/Doors/Door_01/Door_01_04_Snaps014.fbx | No converted material was resolved for slot 'Glass'.
	L2516 | unassigned-material | AssetStoreOriginals/School/Art/Model/Structure/Floor/Restroom_Floor_04_Snaps014.fbx | No converted material was resolved for slot 'lambert2'.
	L2522 | unassigned-material | AssetStoreOriginals/School/Art/Model/Structure/Wall/Classroom_Door_Wall_01_Snaps014.fbx | No converted material was resolved for slot 'mesh'.
	L2528 | unassigned-material | AssetStoreOriginals/School/Art/Model/Structure/Wall/Classroom_Door_Wall_01_Snaps014.fbx | No converted material was resolved for slot 'lambert3'.
	L2534 | unassigned-material | AssetStoreOriginals/School/Art/Model/Structure/Wall/Classroom_Door_Wall_01_Snaps014.fbx | No converted material was resolved for slot 'lambert2'.
	L2540 | unassigned-material | AssetStoreOriginals/School/Art/Model/Structure/Windows/Windows_02/Windows_02_01_Snaps014.fbx | No converted material was resolved for slot 'Windows'.
	L2546 | unassigned-material | AssetStoreOriginals/School/Art/Model/Structure/Windows/Windows_02/Windows_02_01_Snaps014.fbx | No converted material was resolved for slot 'Glass'.
	L2552 | unassigned-material | AssetStoreOriginals/School/Art/Model/Structure/Windows/Windows_02/Windows_02_02_Snaps014.fbx | No converted material was resolved for slot 'Windows'.
	L2558 | unassigned-material | AssetStoreOriginals/School/Art/Model/Structure/Windows/Windows_02/Windows_02_02_Snaps014.fbx | No converted material was resolved for slot 'Glass'.
	L2564 | unassigned-material | AssetStoreOriginals/School/Art/Model/Structure/Windows/Windows_02/Windows_02_03_Snaps014.fbx | No converted material was resolved for slot 'Windows'.
	L2570 | unassigned-material | AssetStoreOriginals/School/Art/Model/Structure/Windows/Windows_02/Windows_02_03_Snaps014.fbx | No converted material was resolved for slot 'Glass'.
	L2576 | unassigned-material | AssetStoreOriginals/School/Art/Model/Structure/Windows/Windows_02/Windows_02_04_Snaps014.fbx | No converted material was resolved for slot 'Windows'.
	L2582 | unassigned-material | AssetStoreOriginals/School/Art/Model/Structure/Windows/Windows_02/Windows_02_04_Snaps014.fbx | No converted material was resolved for slot 'Glass'.
	L2588 | unassigned-material | AssetStoreOriginals/School/Art/Model/Structure/Windows/Windows_05/Windows_05_01_Snaps014.FBX | No converted material was resolved for slot 'Windows'.
	L2594 | unassigned-material | AssetStoreOriginals/School/Art/Model/Structure/Windows/Windows_05/Windows_05_01_Snaps014.FBX | No converted material was resolved for slot 'Glass'.
	L2600 | unassigned-material | AssetStoreOriginals/School/Art/Model/Structure/Windows/Windows_05/Windows_05_02_Snaps014.FBX | No converted material was resolved for slot 'Windows'.
	L2606 | unassigned-material | AssetStoreOriginals/School/Art/Model/Structure/Windows/Windows_05/Windows_05_02_Snaps014.FBX | No converted material was resolved for slot 'Glass'.
	L2612 | unassigned-material | AssetStoreOriginals/School/Art/Model/Structure/Windows/Windows_05/Windows_05_03_Snaps014.FBX | No converted material was resolved for slot 'Windows'.
	L2618 | unassigned-material | AssetStoreOriginals/School/Art/Model/Structure/Windows/Windows_05/Windows_05_03_Snaps014.FBX | No converted material was resolved for slot 'Glass'.
	L2624 | unassigned-material | AssetStoreOriginals/School/Art/Model/Structure/Windows/Windows_05/Windows_05_04_Snaps014.FBX | No converted material was resolved for slot 'Windows'.
	L2630 | unassigned-material | AssetStoreOriginals/School/Art/Model/Structure/Windows/Windows_05/Windows_05_04_Snaps014.FBX | No converted material was resolved for slot 'Glass'.
	L2636 | unassigned-material | AssetStoreOriginals/School/Art/Model/Structure/Windows/Windows_05/Windows_05_05_Snaps014.FBX | No converted material was resolved for slot 'Windows'.
	L2642 | unassigned-material | AssetStoreOriginals/School/Art/Model/Structure/Windows/Windows_05/Windows_05_05_Snaps014.FBX | No converted material was resolved for slot 'Glass'.
	L2648 | unassigned-material | AssetStoreOriginals/School/Art/Model/Structure/Windows/Windows_05/Windows_05_06_Snaps014.FBX | No converted material was resolved for slot 'Windows'.
	L2654 | unassigned-material | AssetStoreOriginals/School/Art/Model/Structure/Windows/Windows_05/Windows_05_06_Snaps014.FBX | No converted material was resolved for slot 'Glass'.
	L2660 | unassigned-material | AssetStoreOriginals/School/Art/Model/Structure/Windows/Windows_06/Windows_06_01_Snaps014.FBX | No converted material was resolved for slot 'Metal'.
	L2666 | unassigned-material | AssetStoreOriginals/School/Art/Model/Structure/Windows/Windows_06/Windows_06_01_Snaps014.FBX | No converted material was resolved for slot 'Glass'.
	L2672 | unassigned-material | AssetStoreOriginals/School/Art/Model/Structure/Windows/Windows_06/Windows_06_02_Snaps014.FBX | No converted material was resolved for slot 'Metal'.
	L2678 | unassigned-material | AssetStoreOriginals/School/Art/Model/Structure/Windows/Windows_06/Windows_06_02_Snaps014.FBX | No converted material was resolved for slot 'Glass'.
	L2684 | unassigned-material | AssetStoreOriginals/School/Art/Model/Structure/Windows/Windows_06/Windows_06_03_Snaps014.FBX | No converted material was resolved for slot 'Metal'.
	L2690 | unassigned-material | AssetStoreOriginals/School/Art/Model/Structure/Windows/Windows_06/Windows_06_03_Snaps014.FBX | No converted material was resolved for slot 'Glass'.
	L2696 | unassigned-material | AssetStoreOriginals/School/Art/Model/Structure/Windows/Windows_06/Windows_06_04_Snaps014.FBX | No converted material was resolved for slot 'Metal'.
	L2702 | unassigned-material | AssetStoreOriginals/School/Art/Model/Structure/Windows/Windows_06/Windows_06_04_Snaps014.FBX | No converted material was resolved for slot 'Glass'.

	The 81-line extract above is the durable record; the report itself is the authority for line numbers and the per-code counts.

## Clarification Q&A

	Q1: What counts as "the errors" to review? The report has two different failure streams: 81 Unresolved issue objects that are ALL Severity "Warning" (:2220-2707; 44 unassigned-material, 20 prefab-material-assignment, 16 missing-texture, 1 ambiguous-material), and 428 engine-side strings under Completion.Errors (:24063-24497) reading "compiled resource dependencies did not become ready within 30 seconds". Should the review target the 81, the 428, or both?

	Q2: Which of the 8 live DONE task files is "the related task" to unmark? The 81 issues are model material-slot resolution (e.g. :2320 "Prefab assignment for slot 'Plastic' is missing or conflicting", :23918 "No converted material was resolved for slot 'Metal'"), which matches the earlier Model-Prefab Material Slots work, but tasks 01-05 are archived and off limits; the 428 Completion.Errors have no counterpart in any live task. Current candidates: '08 HDRP Lit PBR and Emission Fidelity.md' (:1,:3-6, closest to material conversion) or '07 HDRP Lit Surface Semantics.md' (:1,:3-5). Should I unmark one of these, or open a new live task file instead?

	Q3: Should the notes in that task's ## Post-Task Follow Ups only say what went wrong with a high-level fix, or should they also propose concrete code changes (files/functions and the exact slot/remap logic)? The brief says "ideas on what went wrong and how to fix", which reads as high-level; I will not edit source either way.

	Answered (2026-11-22, Director, via chat; the clarify card had timed out, questions were reposted in chat):
	An: Q1 B - only the 81 Unresolved issues; prioritize those for now. The 428 Completion.Errors engine-side timeouts are out of scope for this pass.
	An: Q2 "You decide." - delegated to the Task Master; see Parent decisions below.
	An: Q3 B - also name concrete files/functions and the exact slot/remap logic, marked as proposals NOT applied. No project source files are edited.

	Parent decisions (change if wrong):
	- Q2: unmark '08 HDRP Lit PBR and Emission Fidelity.md' (DONE? YES! -> DONE? NO!) and leave the notes in its ## Post-Task Follow Ups. Reason: it is the live DONE task in eliots-agentics/tasks/ whose shipped diff touched Editor/Core/UnityImport.cs, the file that owns slot-keyed material remaps; closest live fit to material-slot resolution. The true content match '02 Model-Prefab Material Slots' is archived under .humans-only/archive/ and off-limits to agents, and the 428 timeouts match no live task. No new task file is created, so the loop stays scoped to the one concerning task. The notes must state this attribution limit explicitly so the record is not misread.
	- Q1: scope = the 81 Unresolved issues only. Name the 428 Completion.Errors solely as explicitly out of scope; do not analyze them.
	- Q3: concrete proposals are written as NOT applied; no project source files are edited by this task.
	- Task 08's ## Post-Task Follow Ups already holds a unit-emission note (lines 179-180). APPEND after it; do not overwrite it. Change ONLY the DONE? value on task 08's line 1; keep its other six header fields byte-identical. Do not touch its ### Work Review entries or any code file.

## Relevant History

	The HDRP conversion chain in eliots-agentics/tasks/ is closed out: '06 HDRP Detection and Fixture.md':1,3-7 shipped shader/script-guid detection, an HDRP census Information line and a hand-written fixture; '07 HDRP Lit Surface Semantics.md':1,3-5 shipped alpha-test, double-sided and transparent handling; '08 HDRP Lit PBR and Emission Fidelity.md':1,3-6 shipped metallic/smoothness remaps, normal scale, HDR emission and report-only transmission/SSS; '09 HDRP Environment from Volumes.md':1,3-5 added the VolumeProfile reader with SkyBox2D/Fog/AmbientLight; '10 HDRP Lights and Cameras.md':1,3-6 added the HD light-unit table and camera data; '11 HDRP Probes, Decals and Volume Post-Processing.md':1,3-7 added EnvmapProbes, PostProcessVolumes and report-only planar/decal/light-layer handling. Each of those six headers reads STARTED? YES! ... DONE? YES!, and 'hdrp-research.md':15-31 records the pre-task baseline they were written against.

	The new export Testing/old_school_project+import-report+post-hdrp-update+post-no-prefabs-update.json records 1,277 assets / 1,034 converted (:2-4), healthy scenes (MergeV2 4 roots / 2,861 instances / 0 lost / 0 skipped; Test_Range 3 / 201 / 0 / 0, :21397-21421), and 81 Unresolved issue objects that are all Severity Warning (:2220-2226, :2317-2322, :2377-2381): 44 unassigned-material, 20 prefab-material-assignment, 16 missing-texture, 1 ambiguous-material, concentrated in model material-slot resolution (:23913-23956). Completion.Title still reads "Import completed with errors" (:23961) and Completion.Errors holds 428 engine-side "compiled resource dependencies did not become ready within 30 seconds" entries (:24063-24064). LATEST.md:3-4 describes an older, still-open goal (make generated output match Assets/Manual/), :44-58 lists earlier material-remap/legacy-fileID/scale root causes and :183-190 records the whole-package/prefab phase; it predates tasks 06-11, so it is context, not current task state.

## Stylistic Task Notes:
References to variables like X and Y automatically imply that these are to be exposed properties for developer tweaking.
When programming, portability and DRY are a priority.

## Post-Task Follow Ups 
