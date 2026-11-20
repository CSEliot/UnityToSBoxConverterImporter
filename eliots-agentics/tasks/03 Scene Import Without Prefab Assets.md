## Status: STARTED? NO! CLARIFIED? NO! HISTORY? NO! REVIEWED? NO! DONE? NO! WORKER: NONE OTHER: backlog; depends on tasks 01 and 02; reconciled with the unity backlog

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

	The model-wrapper builder from task 01 is the shared dependency; land 01 first. Task 02's fixed-ID map is needed for the direct instance's material arrays; if 02 is not in yet, land its map with 01 rather than duplicating it here.

	Fixture placement: Testing/* is gitignored, so use Documentation/Unity-To-SBox-Automated/fixtures for a tracked fixture or keep a local one beside Testing. Do not import the fixture into the project's Assets/imported as a committed artifact.

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