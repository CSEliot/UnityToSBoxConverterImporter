## Status: STARTED? NO! CLARIFIED? NO! HISTORY? NO! REVIEWED? NO! DONE? NO! WORKER: NONE OTHER: backlog

## Task
	Place scene prefab instances whose source is an FBX model, so scenes that do not use .prefab assets import with their objects, overrides and parents.

### Description
	UnitySceneExport resolves every PrefabInstance through UnityPrefabExport.Converter.SourceOf, which looks up Context.PrefabPathFor(guid) and Context.Converted[guid]. Those dictionaries only contain .prefab assets. A scene that places an FBX model directly stores a PrefabInstance whose m_SourcePrefab guid is the FBX, and the converter skips it with "Nested prefab ... is not in the package or was not converted; instance skipped" even though the model exists and was converted. The old_school scenes point at model-wrapper prefabs instead, so they are gated by the prefab task, but they lose 2861 instances in MergeV2 and 201 in Test_Range for the same reason: the source prefab was rejected. Both cases need the same on-demand model-prefab conversion.

## Notes
	Implementation:
	- Reuse the model-prefab builder from the prefab-root task; do not write a second model-to-prefab converter. Expose it so Converter.SourceOf can synthesize a source for a Model guid and cache it like a converted prefab.
	- In Instance(), keep the existing override path, but resolve modifications aimed at fixed model-prefab IDs: 100000 name, 400000 transform, 2300000/2300002... renderers, m_Materials.Array.data[i]. Scene instances in MergeV2 carry 405 material overrides, 3654 static-flag overrides, and 2861 transform/name blocks.
	- Preserve the current parent handling: all 2861 MergeV2 instances are parented to plain non-prefab Transform objects in the Setting/Structure/Props/JapaneseSchoolSettings hierarchy; root order comes from m_RootOrder/SceneRoots.
	- Keep single- and multi-mesh models intact; one scene instance may expand to several renderers on the generated prefab.
	- Verify ImportPlan already pulls the models and materials a direct-model scene references; it links Model and Material guids from scene text (Editor/Core/ImportPlan.cs:127-143), so a subset import should include them.

	Acceptance:
	- Harness on Testing/old_school_project.unitypackage with --scenes after the prefab tasks: SceneConversions MergeV2 Instances 2861, Test_Range Instances 201, Skipped 0 for both; generated mergev2.scene contains the prefab stubs under Setting/Structure/Props instead of four empty groups.
	- A small handcrafted .unitypackage fixture (one FBX, its .meta, one .unity scene whose PrefabInstance targets the FBX guid, and the scene .meta) imports with that model, its material and its transform. Store it under Documentation/Unity-To-SBox-Automated/fixtures so it can be tracked, or beside Testing/ locally if it must stay untracked. Do not re-export the 1.27 GB old_school project to make it.
	- No "instance skipped" warnings for sources that are present; no change to scenes whose prefabs already convert.
	- Two consecutive runs produce byte-identical scenes and prefabs.

## Clarification Q&A


## Relevant History 
	Baseline: MergeV2 has 2861 PrefabInstance blocks and Test_Range 201, all sourcing .prefab assets that currently fail conversion; both generated scenes have 0 instances and keep only empty groups. Source scenes and counts are in tasks/unity-expert-research.md.

	Related backlog: tasks-todo-sbox/03 covers genuinely prefab-free scenes made of plain objects and their renderer materials; this task owns scene instances of models and model-wrapper prefabs. They are complementary, not duplicates. tasks-todo-sbox/01 acceptance also expects the 2861/201 stubs, so both sets must agree on the final counts.

## Stylistic Task Notes:
References to variables like X and Y automatically imply that these are to be exposed properties for developer tweaking.

## Skills To Know: for you and/or subagents:
	unityimporterplus

## Post-Task Follow Ups