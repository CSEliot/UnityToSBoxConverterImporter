## Status: STARTED? NO! CLARIFIED? NO! HISTORY? NO! REVIEWED? NO! DONE? NO! WORKER: NONE OTHER: backlog

## Task
	Import scenes that place no .prefab assets: direct FBX instances and plain GameObjects, with their renderer materials.

### Description
	A scene that does not use .prefab assets has two shapes today, and neither is complete.

	Plain objects already convert their hierarchy, transforms, enabled state, static flags, cameras, lights, audio listeners, primitive colliders and convex MeshColliders. The gap is materials: ModelRenderer is built with `Materials` and `MaterialOverride` null (Editor/Core/UnityPrefabExport.cs:262), so a plain MeshRenderer's assigned material is dropped. No fixture exercises a scene whose only model reference is a plain MeshFilter.

	Direct model instances do not convert: a scene may store a PrefabInstance whose m_SourcePrefab guid is the FBX itself, with no .prefab asset in between. UnitySceneExport resolves every instance through Context.PrefabPathFor(guid) and Context.Converted[guid], which only contain .prefab assets (Editor/Core/UnityPrefabExport.cs:809-812). Such an instance is skipped with "Nested prefab ... is not in the package or was not converted" even though the model exists and was converted. The old_school scenes point at model-wrapper prefabs instead, so they are gated by task 01, but the same SourceOf path loses their 2861 and 201 instances while the source prefab is rejected. Both cases need the same on-demand model-prefab source.

## Notes
	Implementation:
	- Reuse the model-prefab builder from task 01; expose it so Converter.SourceOf can synthesize a source for a Model guid and cache it like a converted prefab. Do not write a second model-to-prefab converter.
	- In Instance(), keep the existing override path but resolve modifications aimed at fixed model-prefab IDs: 100000 name, 400000 transform, 2300000/2300002... renderers, m_Materials.Array.data[i] through task 02's map. MergeV2 carries 405 material overrides, 3654 static-flag overrides and 2861 transform/name blocks.
	- In shared Converter.Node (Editor/Core/UnityPrefabExport.cs:882), when a MeshRenderer has material references, resolve each with Context.MaterialFor and emit them on the ModelRenderer using the same indexed override shape the prefab path uses (same file:1041-1042). Support multi-slot renderers; unresolved slots stay null and are reported once per object.
	- Confirm ImportPlan links Model and Material assets referenced only by plain scene objects or direct instances. It scans all scene text (Editor/Core/ImportPlan.cs:127-143); fix the link reason or scan if the fixture shows a miss.
	- Add a minimal fixture: one FBX and its .meta, one .unity scene whose PrefabInstance targets the FBX guid plus its .meta, and plain objects covering parent/child, the same FBX rendered through a MeshFilter with a non-default material, a convex MeshCollider, a camera, a directional light, an audio listener, a disabled object and a static object. Store it under Documentation/Unity-To-SBox-Automated/fixtures so it can be tracked, or beside Testing/ locally if it must stay untracked. Do not re-export the 1.27 GB project.
	- Do not treat zero prefab instances as an error or warning; it is a normal scene and must convert.
	- Keep output unchanged for scenes whose prefabs already convert.

	Non-goals: converting the .prefab assets themselves (task 01), the fixed-ID material map (task 02), built-in primitive meshes (task 05), loss reporting (task 04).

	Acceptance:
	- The fixture converts with --scenes to a .scene containing the direct FBX instance and every plain object/component above; the FBX renderer names the converted material.
	- verify_output.py reports 0 problems on the fixture output.
	- Harness on Testing/old_school_project.unitypackage after tasks 01 and 02: SceneConversions MergeV2 Instances 2861 / Skipped 0 and Test_Range 201 / 0; generated mergev2.scene contains prefab stubs under Setting/Structure/Props instead of four empty groups.
	- No "instance skipped" warnings for sources that are present; two consecutive runs are byte-identical; building/city--1 output unchanged.
	- Editor/Core builds with 0 warnings, 0 errors.

## Clarification Q&A


## Relevant History 
	Baseline: MergeV2 has 2861 PrefabInstance blocks and Test_Range 201, all sourcing .prefab assets that currently fail conversion; both generated scenes have 0 instances and keep only empty groups. Test_Range's two plain MeshFilters use Unity's built-in Plane mesh, so they cannot prove the plain-object material path; task 05 covers built-in primitives. Source scenes and counts are in tasks/unity-expert-research.md.

	Related backlog: tasks-todo-sbox/03 is a reconciled twin of this task (both prefab-free shapes, fixture, counts). Work one set, not both. tasks-todo-sbox/01 Notes says to expect the 2861/201 stubs only once task 03 lands, so both sets must agree on the final counts.

## Stylistic Task Notes:
References to variables like X and Y automatically imply that these are to be exposed properties for developer tweaking.

## Skills To Know: for you and/or subagents:
	unityimporterplus

## Post-Task Follow Ups