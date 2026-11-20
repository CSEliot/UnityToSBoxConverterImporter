## Status: STARTED? NO! CLARIFIED? NO! HISTORY? NO! REVIEWED? NO! DONE? NO! WORKER: NONE OTHER: backlog, generated from tasks/sbox-expert-research.md

## Task
	Make a scene with no prefab instances import completely, and carry plain scene objects' renderer materials onto their ModelRenderers.

### Description
	A genuinely prefab-free Unity scene is the case the task title asks for, and there is no fixture that proves it. The current converter already emits plain objects: hierarchy, transforms, enabled state, static flags, cameras, lights, audio listeners, box/sphere/capsule colliders and convex MeshColliders all survive (verified in the school package's generated scenes). Two gaps remain visible:

	1. Materials on plain renderers are dropped. UnityPrefabExport.ModelRenderer (Editor/Core/UnityPrefabExport.cs:262) writes "Materials" and "MaterialOverride" as null. For a prefab instance, per-instance materials are emitted as an override (same file:1041-1042, shape { indexed: { slot: vmat } }); for a plain scene object there is no equivalent, so a MeshRenderer that points at a hand-assigned material renders with the model's default.
	2. The dependency scan must pull the models and materials of plain scene objects. Editor/Core/ImportPlan.cs:127-143 does scan all GUIDs in the scene, but no fixture exercises a scene whose only model reference is a plain MeshFilter, so a regression here would only show as missing geometry.

	Required work:
	1. Add a minimal prefab-free fixture: a package (or a scene added to the Unity source project and exported) with plain GameObjects only: a parent with a child, one package FBX object with a non-default material, a convex MeshCollider, a camera, a directional light, an audio listener, a disabled object and an object with Unity static flags. Keep it small; a handcrafted .unitypackage with one FBX and one .mat is enough.
	2. In the shared Converter.Node (Editor/Core/UnityPrefabExport.cs:882), when a MeshRenderer has material references, resolve each with Context.MaterialFor and emit them on the ModelRenderer using the same indexed override shape the prefab path uses. Support multi-slot renderers; unresolved slots stay null and are reported once per object, not silently.
	3. Confirm ImportPlan links Model and Material assets referenced only by plain scene objects. Fix the link reason/scan if the fixture shows a miss.
	4. Do not treat zero prefab instances as an error or a warning; it is a normal scene and must convert.
	5. Keep prefab-instance output unchanged.

	Acceptance:
	- The fixture converts with --scenes to a .scene containing every object and component listed above; the FBX object's ModelRenderer names the converted material.
	- verify_output.py reports 0 problems on the fixture output.
	- Two runs byte-identical.
	- Reimporting the school package leaves the plain-object layer at 250 and 11 objects with the same components, no new warnings, and Test_Range's plain objects still convert.
	- No change to building/city--1 output.

## Notes

	Implementation home: Editor/Core/UnitySceneExport.cs (scene loop and component assembly) and Editor/Core/UnityPrefabExport.cs (Converter.Node at 882, ModelRenderer call at 922, ModelRenderer definition at 262, prefab override shape at 1041). Material resolution is Context.MaterialFor, wired in Editor/Core/UnityImport.cs:278.

	Fixture placement: Testing/* is gitignored, so either keep the fixture beside the existing Testing packages (local only) or add it under Documentation/Unity-To-SBox-Automated/fixtures if it needs to be tracked. Do not import the fixture into the project's Assets/imported as a committed artifact.

	The school package's Test_Range scene has only two MeshFilters and both use Unity's built-in Plane mesh (guid 0000000000000000e000000000000000), so it cannot prove this task; that mesh is task 04.

## Clarification Q&A


## Relevant History 

	Source: tasks/sbox-expert-research.md. The research showed the plain-object path already runs and that the reported school failure is prefab-variant driven; this task keeps the prefab-free case from being silently incomplete after that is fixed.

## Stylistic Task Notes:
References to variables like X and Y automatically imply that these are to be exposed properties for developer tweaking.

## Skills To Know: for you and/or subagents:
	unityimporterplus
	unity-to-sbox-conversion
	sbox-scripting-patterns
	sbox-model-material-resources
	sbox-offline-compile-check

## Post-Task Follow Ups
	If multi-slot overrides turn out not to be honoured by ModelRenderer, record the limitation in README and report the dropped slots per object instead of emitting a misleading override.