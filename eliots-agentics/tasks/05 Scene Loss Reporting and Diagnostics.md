## Status: STARTED? NO! CLARIFIED? NO! HISTORY? NO! REVIEWED? NO! DONE? NO! WORKER: NONE OTHER: backlog; reconciled with the unity backlog; owns the merged SceneConversions schema

## Task
	Report lost scene content as an import error and group scene skips by cause, including the silent child-loop drop.

### Description
	A lost prefab instance does not currently become an error, and often is not even counted. The null-root branch at Editor/Core/UnitySceneExport.cs:68 does `skipped++`, but the old_school scenes report Roots 4 and 3 with Instances 0 and Skipped 0, so it never fires: all 2,861 MergeV2 and 201 Test_Range instances are children of plain group objects. They are dropped silently in UnityPrefabExport.Node's child loop at Editor/Core/UnityPrefabExport.cs:961, where `if ( Instance( instance, chain, owed ) is JsonObject nested ) children.Add( nested );` omits a null without counting it. The `Owner( ... ) is not var (owner, source, g)` check at UnitySceneExport.cs:127 is a working null check on a nullable tuple, not a dead pattern, and its `skipped++` is live; it simply never runs for these scenes because Nest() is only reached for objects attached under a prefab instance (UnitySceneExport.cs:144-165), while these instances hang off plain group transforms. :134's `if ( data == null ) { skipped++; return; }` is a second live guard. Neither is the loss path.

	The baseline therefore shows success while the content is gone: SceneConversions MergeV2 4 roots / 0 instances / Skipped 0, Test_Range 3 / 0 / 0, and 459 warning lines (255 MergeV2 + 204 Test_Range, 455 distinct after deduplication; not all of them are instance skips) for 3,062 lost objects. Most skip warnings are cached per source prefab guid, not per lost instance, so the count understates the loss. Conversion-skipped issues are already Error severity (Editor/Core/UnityImport.cs:22), but these losses never reach that path. The prefab error also mislabels every model wrapper as a prefab variant, so the cause cannot be read from the message.

	Required work:
	1. Count every PrefabInstance whose source cannot be resolved, including instances dropped inside the child loop at UnityPrefabExport.cs:961. Thread a skip collector or return a per-call drop count from Node/Instance so the scene conversion can total it. Do not count the stripped transforms that belong to those same instances as separate losses.
	2. Expose TotalInstances and LostInstances beside Roots/Instances/Skipped in SceneConversions, and add SkipCauses (cause to count, with up to MaxSampleNames sample names, a named constant of 10). This task owns both schema additions: the parallel unity backlog proposed TotalInstances/LostInstances and the sbox backlog proposed per-cause counts, and they are consolidated here rather than defined twice. Keep older exported reports readable and add fields rather than renaming.
	3. Raise one unresolved Error with code conversion-skipped per scene when content is lost, grouped by reason with a count and sample names. Keep warnings for cosmetic losses that remove no object. Do not emit thousands of duplicate lines.
	4. Do not change the guards at UnitySceneExport.cs:127 or :134; both are live checks. Add the counting at the child loop and the Instance boundary where the null is currently discarded.
	5. In UnityPrefabExport.Convert, distinguish a model-wrapper root from a true variant: report "model prefab instance" when the source is a Model, keep "prefab variant" only when the source is a Prefab, and say "missing prefab reference" when the GUID is absent.
	6. Do not change generated scene or prefab JSON for successful conversions.

	Non-goals: converting the prefabs (task 01), material binding (task 02), direct model instances (task 03), built-in primitives (task 04).

	Acceptance:
	- On the current tree, a harness run of old_school with --scenes reports 3,062 lost scene instances as conversion-skipped Errors (2,861 + 201) and exits nonzero for that reason, not only for the 346 prefab errors. The report names the scene, the count and the grouped cause instead of leaving 459 per-source warnings as the only trace.
	- A fixture whose scene instance points at a guid missing from the package reports the scene, the count and the missing guid as an Error.
	- After tasks 01, 02 and 03, the same run reports 0 lost instances and no error; packages with complete scenes show no new warnings or errors.
	- Record the before and after warning and error counts in the task file; do not assume unrelated model or material warnings disappear.
	- Editor/Core builds with 0 warnings, 0 errors.

## Notes

	Implementation home: Editor/Core/UnitySceneExport.cs (SceneResult/Statistics at 37, root loop at 68, ParentWorld guard at 116, Nest guard at 127, live data guard at 134), Editor/Core/UnityPrefabExport.cs (child loop at 961, Convert at 1166-1173), Editor/Core/UnityImport.cs (scene loop 431-447, report object at 482, Error severity at 22).

	Baseline evidence: 459 = 255 + 204 warnings; 455 distinct after `warnings.Distinct()`; 3,062 = 2,861 + 201 lost instances; SceneConversions 0 instances received. These come from the unity expert's saved baseline report; re-run the harness rather than trusting the scratch file.

	The unity set's 04 and this task described the same bug with different field names. This task is now the single owner: TotalInstances, LostInstances and SkipCauses all live in SceneConversions, and the prefab/wrapper message fix belongs with the error path here.

## Clarification Q&A


## Relevant History 

	Source: tasks/sbox-expert-research.md, reconciled 2026-11-20 with tasks/unity-expert-research.md. The earlier sbox version said top-level skips were silent at :68 and that the report had no cause count; that was incomplete. The unity expert established that :68 never fires for these scenes and the drop happens in the child loop at UnityPrefabExport.cs:961; a compiled check of the nullable tuple showed the `is not var` guard at :127 is a live null check that is simply never reached for these instances, and :134 is live. The count belongs at the child-loop drop. This task now owns the merged severity, counting and schema work.

## Stylistic Task Notes:
References to variables like X and Y automatically imply that these are to be exposed properties for developer tweaking.

## Skills To Know: for you and/or subagents:
	unityimporterplus
	unity-to-sbox-conversion
	sbox-scene-prefab-inspection
	sbox-offline-compile-check

## Post-Task Follow Ups
	After the loss count is reliable, consider the same grouped reporting for prefab conversion issues if a package ever produces one issue per prefab again; the editor log already groups them, the window report does not.