## Status: STARTED? NO! CLARIFIED? NO! HISTORY? NO! REVIEWED? NO! DONE? NO! WORKER: NONE OTHER: backlog, generated from tasks/sbox-expert-research.md

## Task
	Convert Unity model prefab variants (a .prefab whose root is a PrefabInstance of an FBX) into s&box .prefab assets.

### Description
	The school package's 346 .prefab files are all model prefab variants: the file has no plain GameObject/Transform root, only a PrefabInstance whose m_SourcePrefab points at an FBX in the same package. Editor/Core/UnityPrefabExport.cs:1173 sees no root Transform and throws "Prefab has no root transform (prefab variants are not supported)." 328 of the 346 point straight at an FBX; the other 18 are handled by task 02.

	Evidence: Testing/old_school_project.unitypackage (1.27 GB) produced 0 .prefab outputs and 346 grouped conversion errors in the 2026-11-20 import. Without converted prefabs, UnitySceneExport skips every prefab instance the scenes place (2,861 in MergeV2, 201 in Test_Range), so the generated scenes contain only the plain-object layer. See tasks/sbox-expert-research.md for the full inventory.

	Required work:
	1. Recognise a variant root: a prefab file whose only root blocks are PrefabInstance/1001 (optionally followed by components added by the variant), with no stripped Transform root.
	2. Resolve its m_SourcePrefab GUID through the package. For an FBX base, read the model through UnityModel/MeshName and build the prefab object tree Unity's FBX importer would have produced: root GameObject (id 100000), root Transform (id 400000), one renderer per mesh node with the mesh's generated VMDL and its material slots (ids 2300000, 2300002, ... in the files inspected), plus a ModelCollider when the variant carries stripped MeshCollider blocks for that mesh (218 of the 346 prefabs do). Verify the exact ID mapping against more than one FBX before coding.
	3. Apply the variant's m_Modifications in Unity's order, keyed by (target fileID, target guid, propertyPath): m_Name, m_IsActive, m_LocalPosition/Rotation/Scale, m_StaticEditorFlags, m_Materials.Array.data[i] with its objectReference, and the root order. Unknown property paths must be reported once per path with a count, not per prefab.
	4. Emit stable GUIDs for the generated objects so scene instances can patch them. Scene overrides of a variant target fileIDs that do not appear in the variant YAML (for example the Hose_01 variant is prefab instance 2151425985336930048, while its scene override targets 2151425985336800640 in the variant's own GUID space). Establish how Unity derives a variant's exposed object IDs, or resolve the scene target through the variant chain to the base object ID, before relying on UnityPrefabExport.Converter.Anchor as-is.
	5. Keep the existing mirrored-variant path working: a mirrored prefab variant of a variant must still produce its `_mirror_<axis>` output, and a mirrored model must be used when the placement has a negative scale.
	6. Skip with a per-file issue, not an exception, when the base is outside the package, is a variant of a variant (task 02 territory until it lands), or has a 2018.2 format root. The user must be able to see the reason.
	7. 18 of the 328 direct variants add exactly one MonoBehaviour (class 114) each through m_AddedComponents. Scripts cannot convert: report each file once with the affected object path and the component type, or one grouped line with the count and sample names. No prefab in this package has a non-empty m_AddedGameObjects or m_RemovedGameObjects, and none removes a component.

	Non-goals: prefab-of-prefab chains (task 02), built-in primitive meshes (task 04), scene skip aggregation (task 05), any change to non-variant prefab output.

	Acceptance:
	- Reimport Testing/old_school_project.unitypackage with scene import on. At least the 328 direct FBX-base variants produce .prefab outputs; the remaining 18 may still be skipped until task 02.
	- No "Prefab has no root transform" errors for the 328.
	- The 18 prefabs that add a MonoBehaviour each produce a reported Information line (or one grouped line with the count); none is dropped silently.
	- MergeV2.scene has 2,861 __Prefab stubs and Test_Range.scene has 201 once tasks 01 and 02 both land.
	- Two harness runs into fresh directories are byte-identical; verify_output.py reports 0 problems on the generated Assets tree.
	- Re-running Testing/building.unitypackage and Testing/city--1.unitypackage produces byte-identical output to before the change.

## Notes

	Implementation home: Editor/Core/UnityPrefabExport.cs (Convert at 1166, Converter at 650+, Anchor at ~103 in UnitySceneExport), with model facts from Editor/Core/UnityModel.cs and material-slot resolution from UnityPrefabBindings.

	Useful fixture facts (2026-11-20):
	- Hose_01_Snaps014.prefab is 75 lines and one PrefabInstance block; targets 100000, 400000, 2300000, 2300002, 2300004, 2300006, all in the FBX's GUID space.
	- Of 346 prefabs, 328 resolve straight to an FBX, 12 to another .prefab, 6 two .prefab steps away. 18 of the direct 328 add one MonoBehaviour (class 114) each; the 18 chained prefabs add nothing; no prefab has a non-empty added/removed GameObject list or a removed component.
	- The school package's FBX files are mostly single-mesh, but do not assume it: use the FBX's node list. City--1 (19 MB) is the regression package with skinned models and bodygroups.

	Verification command shape (in-repo harness, no editor):
	dotnet build Documentation/Unity-To-SBox-Automated/harness/harness.csproj -c Release -p:IUP_ROOT=C:/cseliot/1-Development/1-SBox/2-Projects/unityimporterplus
	dotnet <harness.dll> --scenes Testing/old_school_project.unitypackage <fresh scratch>/Assets <scratch>/report.json
	Then verify_output.py <fresh scratch>/Assets <fresh scratch>/Assets/imported, and compare two runs with diff -r.

## Clarification Q&A


## Relevant History 

	Source: tasks/sbox-expert-research.md. The user reported the school package import as "pitiful success"; the research established that the scenes are prefab-heavy and the prefabs are all variants, so this task, not a scene-only change, is the unblocker.

## Stylistic Task Notes:
References to variables like X and Y automatically imply that these are to be exposed properties for developer tweaking.

## Skills To Know: for you and/or subagents:
	unityimporterplus
	unity-to-sbox-conversion
	sbox-prefab-editing
	sbox-scene-prefab-inspection
	sbox-offline-compile-check
	sbox-cli-driving

## Post-Task Follow Ups
	After this lands, task 02 covers the 18 chained prefabs; task 05 makes a partial conversion report one line instead of a silent skip counter.