## Status: STARTED? NO! CLARIFIED? NO! HISTORY? NO! REVIEWED? NO! DONE? NO! WORKER: NONE OTHER: backlog

## Task
	Report lost scene content as an import error, group scene skips by cause, and name model-wrapper and prefab-variant failures correctly.

### Description
	When an instance cannot be converted, UnitySceneExport.Instance adds a warning and returns null. A null top-level root is counted at UnitySceneExport.cs:68, but that branch never fires for these scenes (Roots 4/3, Skipped 0): every instance is a child of a plain group object and is dropped silently by the child loop at UnityPrefabExport.cs:961, where `if ( Instance( instance, chain, owed ) is JsonObject nested )` omits a null instead of counting it. The `Owner( ... ) is not var (owner, source, g)` check at UnitySceneExport.cs:127 is a working null check on a nullable tuple, not a dead pattern, and its `skipped++` is live; it never runs for these scenes because Nest() is only reached for objects attached under a prefab instance (:144-165), while these instances hang off plain group transforms. The `data == null` check at :134 is a second live guard. The old_school baseline therefore reports SceneConversions MergeV2 Instances 0, Skipped 0, and only 459 warning lines (255 MergeV2 + 204 Test_Range, 455 distinct after deduplication; not all of them are instance skips) for 2861 + 201 lost objects; the import still writes an empty scene. "Pitiful success" is exactly this: the report shows success while the content is gone. The prefab error is also mislabelled: every old_school prefab is a model wrapper, not a prefab variant, yet the message says "prefab variants are not supported".

## Notes
	Implementation:
	- Count every PrefabInstance block whose source cannot be resolved as lost, including instances dropped inside the child loop at UnityPrefabExport.cs:961. Node currently has no scene counter; thread a skip collector or return a per-call drop count from Node/Instance so the scene conversion can total it. Expose the result as TotalInstances/LostInstances beside the existing Roots/Instances/Skipped. Do not count the stripped transforms that belong to those same instances as separate losses.
	- Raise an unresolved Error with code conversion-skipped (the existing severity convention) on the scene asset when content is lost. Aggregate by reason with a count and the first few object names; do not emit thousands of duplicate lines. Keep warnings for cosmetic losses that do not remove an object.
	- In UnityPrefabExport.Convert, distinguish a model-wrapper root from a true variant: report "model prefab instance" when the source is a Model, and keep "prefab variant" only when the source is a Prefab. If the source guid is absent, say the reference is missing.
	- Keep the existing report shape stable for packages with no losses; add fields rather than renaming.
	- SceneConversions schema: this task and tasks-todo-sbox/05 specify the same merged shape: TotalInstances and LostInstances beside Roots/Instances/Skipped, plus SkipCauses (cause to count, with up to MaxSampleNames sample names, a named constant of 10). Work one task, not both. Keep older exported reports readable and add fields rather than renaming.
	- Do not change the guards at UnitySceneExport.cs:127 or :134; both are live. Add the counting at the child loop and the Instance boundary where the null is currently discarded.

	Non-goals: converting prefabs (task 01), fixed-ID material binding (task 02), direct model instances and plain-object materials (task 03), built-in primitives (task 05).

	Acceptance:
	- On the current tree, a harness run of old_school with --scenes reports 3062 lost scene instances as conversion-skipped Errors (2861 MergeV2 + 201 Test_Range) and exits nonzero for that reason, not only for the 346 prefab errors.
	- With a package whose scene instance points at a guid that is not in the package, the report names the scene, the count and the missing guid as an Error.
	- After the prefab and scene fixes, the same run reports 0 lost instances and no error. Packages with complete scenes show no new warnings or errors.
	- Record before/after warning and error counts in the task file; do not assume unrelated model or material warnings disappear.
	- Editor/Core builds with 0 warnings, 0 errors.

## Clarification Q&A


## Relevant History 
	Baseline: SceneConversions reports Skipped 0 while both source scenes contain 2861 and 201 PrefabInstance blocks and both outputs contain 0 instances. The warnings are "Nested prefab ... is not in the package or was not converted; instance skipped", deduplicated per source prefab to 459 lines (455 distinct), not one per lost instance. The severity trap is already known in the importer: any conversion-skipped issue is Error (Editor/Core/UnityImport.cs:22). Evidence is recorded in tasks/unity-expert-research.md.

	Related backlog: tasks-todo-sbox/05 is a reconciled twin of this task and specifies the same merged SceneConversions schema; work one, not both. Do not define both TotalInstances/LostInstances and a parallel cause table in separate changes.

## Stylistic Task Notes:
References to variables like X and Y automatically imply that these are to be exposed properties for developer tweaking.

## Skills To Know: for you and/or subagents:
	unityimporterplus

## Post-Task Follow Ups