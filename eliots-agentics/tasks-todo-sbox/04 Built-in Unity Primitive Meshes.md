## Status: STARTED? NO! CLARIFIED? NO! HISTORY? NO! REVIEWED? NO! DONE? NO! WORKER: NONE OTHER: backlog, generated from tasks/sbox-expert-research.md

## Task
	Render Unity's built-in primitive meshes (Plane, Cube, Sphere, Capsule, Cylinder, Quad) on scene and prefab objects instead of skipping them.

### Description
	A scene made of Unity primitives is the most common prefab-free scene, and every one of those renderers is currently dropped. In the school package, Test_Range.unity's "Plane" object has a MeshFilter with fileID 10209 and guid 0000000000000000e000000000000000, Unity's default resources. The shared Converter.Node sees no package asset for that GUID (Editor/Core/UnityPrefabExport.cs:915-920), adds a warning "mesh ... is not a converted model; renderer skipped", and the generated test_range.scene has no ModelRenderer for it.

	Required work:
	1. Recognise Unity's built-in resource GUIDs (0000000000000000e000000000000000 for default resources, where the primitive meshes live, and 0000000000000000f000000000000000 for builtin extra, which UnitySceneExport.cs:35 uses for the default skybox) and the primitive mesh fileIDs. Verify the IDs against the Unity 6 built-in resources before coding; the ones seen so far are Plane 10209, and the commonly cited set is Cube 10202, Cylinder 10206, Sphere 10207, Capsule 10208, Plane 10209, Quad 10210.
	2. Choose one mechanism and document it: map each primitive to an engine-provided primitive model, or generate a small .vmdl for the primitive shape. Engine model paths must be confirmed in the installed engine (D:/SteamLibrary/steamapps/common/sbox), not guessed. Unity's primitives are 1 unit (metre) cubes/spheres, a 10 x 10 unit plane, and a 1 x 2 unit capsule/cylinder; preserve those dimensions through import_scale and the 39.37 model modifier.
	3. Emit the ModelRenderer and, where the object has a MeshCollider on the same primitive, the matching ModelCollider. Unity's built-in colliders reference the same primitive meshes, so both paths can share the mapping.
	4. Leave non-primitive built-in meshes (for example the built-in quad used by sprites or the skybox) reported once with a count; do not warn per object.
	5. Keep prefab and scene output for package meshes byte-identical.

	Acceptance:
	- Test_Range.unity's Plane has a ModelRenderer and is visible in s&box after reimport; verify in the editor (headless cannot judge scale).
	- A fixture with all six primitives produces one object each with the right dimensions in inches; verify_output.py reports 0 problems.
	- An unsupported built-in mesh produces one grouped Information line, not a warning per object.
	- Two harness runs byte-identical; building/city--1 output unchanged.

## Notes

	Implementation home: Converter.Node mesh resolution in Editor/Core/UnityPrefabExport.cs (around 910-925), PrimitiveCollider in the same file for the collider half, and UnityModel/GeneratedAssetFiles if a primitive VMDL is generated.

	Open question to settle first: whether s&box's engine models cover the shapes and dimensions, or whether a generated VMDL is cheaper. Do not ship a mapping that silently rescales a Unity Plane (10 x 10 m) to an engine 1 x 1 m plane.

## Clarification Q&A


## Relevant History 

	Source: tasks/sbox-expert-research.md. The research found the Plane skipped with a per-object warning and no ModelRenderer; this task covers the primitive half of prefab-free scene support while tasks 01/02 cover the prefab half.

## Stylistic Task Notes:
References to variables like X and Y automatically imply that these are to be exposed properties for developer tweaking.

## Skills To Know: for you and/or subagents:
	unityimporterplus
	unity-to-sbox-conversion
	sbox-model-material-resources
	sbox-prefab-editing
	sbox-offline-compile-check
	sbox-agent-playtest

## Post-Task Follow Ups
	If a generated primitive VMDL is chosen, record its naming and the material slot it expects so scene materials from task 03 attach to it.