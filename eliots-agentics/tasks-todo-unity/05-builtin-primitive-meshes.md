## Status: STARTED? NO! CLARIFIED? NO! HISTORY? NO! REVIEWED? NO! DONE? NO! WORKER: NONE OTHER: backlog

## Task
	Render Unity's built-in primitive meshes (Plane, Cube, Sphere, Capsule, Cylinder, Quad) on scene and prefab objects instead of skipping them.

### Description
	A scene made of Unity primitives is the most common prefab-free scene, and every one of those renderers is currently dropped. In the school package, Test_Range.unity's Plane object has a MeshFilter with fileID 10209 and guid 0000000000000000e000000000000000, Unity's default resources. The shared Converter.Node finds no package asset for that guid (Editor/Core/UnityPrefabExport.cs:915-920), adds the warning "mesh ... is not a converted model; renderer skipped", and the generated test_range.scene has no ModelRenderer for it. This is the remaining gap after tasks 01 and 03: those cover package models, not Unity's own primitives.

## Notes
	Implementation:
	- Recognise Unity's built-in resource guids: 0000000000000000e000000000000000 for default resources (where the primitive meshes live) and 0000000000000000f000000000000000 for builtin extra (UnitySceneExport.cs:35 already uses the latter for the default skybox). Map the primitive mesh fileIDs. Plane 10209 is observed in Test_Range; the commonly cited set is Cube 10202, Cylinder 10206, Sphere 10207, Capsule 10208, Quad 10210. Verify every ID against the Unity 6 built-in resources before coding.
	- Choose one mechanism and document it: map each primitive to an engine-provided primitive model, or generate a small .vmdl for the shape. Confirm any engine model path in the installed engine, never guess it. Unity's primitives are a 1 m cube, a 1 m sphere, a 1 x 2 m capsule/cylinder and a 10 x 10 m plane; preserve those dimensions through import_scale and the 39.37 model modifier.
	- Emit the ModelRenderer and, where the object has a MeshCollider on the same primitive, the matching ModelCollider. Unity's built-in colliders reference the same primitive meshes, so both paths share the mapping.
	- Leave non-primitive built-in meshes reported once with a count and a sample name; do not warn per object.
	- Keep prefab and scene output for package meshes byte-identical.

	Non-goals: model-wrapper prefabs (task 01), fixed-ID material binding (task 02), direct model instances and plain-object materials (task 03), loss reporting (task 04).

	Acceptance:
	- Test_Range.unity's Plane has a ModelRenderer and is visible in s&box after reimport; verify in the editor because headless cannot judge scale.
	- A fixture with all six primitives produces one object each with the right dimensions in inches; verify_output.py reports 0 problems.
	- An unsupported built-in mesh produces one grouped Information line, not a warning per object.
	- Two harness runs are byte-identical; building/city--1 output unchanged.
	- Editor/Core builds with 0 warnings, 0 errors.

## Clarification Q&A


## Relevant History 
	Test_Range.unity has two plain MeshFilter objects referencing the built-in Plane (fileID 10209, guid 0000000000000000e000000000000000); the generated scene drops both renderers. This was the sbox expert's built-in-primitives task; it is mirrored here so the unity backlog covers the same prefab-free scene surface. Baseline evidence and the harness command are in tasks/unity-expert-research.md.

	Related backlog: tasks-todo-sbox/04 is the same task from the parallel sbox backlog. Work one, not both.

## Stylistic Task Notes:
References to variables like X and Y automatically imply that these are to be exposed properties for developer tweaking.

## Skills To Know: for you and/or subagents:
	unityimporterplus

## Post-Task Follow Ups
	If a generated primitive VMDL is chosen, record its naming and expected material slot so task 03's plain-object materials attach to it.