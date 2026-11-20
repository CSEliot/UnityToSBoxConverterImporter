## Status: STARTED? NO! CLARIFIED? NO! HISTORY? NO! REVIEWED? NO! DONE? NO! WORKER: NONE OTHER: backlog; reconciled with tasks/unity-expert-research.md and the unity backlog

## Task
	Convert model-wrapper Unity prefabs (a .prefab whose top-level content is a PrefabInstance of an FBX) into s&box .prefab assets, including nested instances, wrapper components and script reporting.

### Description
	A model-wrapper prefab has no plain GameObject/Transform root. Unity strips the root and stores one top-level PrefabInstance whose m_SourcePrefab points at the FBX, plus overrides and any added components. All 346 .prefab assets in Testing/old_school_project.unitypackage are this shape: 346/346 top-level instances source an FBX, 322 files contain only that instance, 24 also nest .prefab instances, and none has a non-stripped root Transform. There is no top-level prefab-to-prefab chain, so none is a true prefab variant. This corrects the earlier 328-direct / 18-chained split, which came from reading nested m_SourcePrefab blocks as root sources.

	Editor/Core/UnityPrefabExport.cs:706 builds Converter.Roots only from non-stripped Transform blocks; Convert finds none and throws "Prefab has no root transform (prefab variants are not supported)" at line 1173. The 2026-11-20 baseline reports 346 conversion-skipped Errors, writes 0 .prefab files, and leaves every scene that places these wrappers empty (2,861 instances in MergeV2, 201 in Test_Range). Baseline report: 1,277 assets, 540 converted, 1,714 files, 1,131 unresolved issues.

	Required work:
	1. Recognise a wrapper root: Converter.Roots is empty and a top-level PrefabInstance exists (m_TransformParent fileID 0). Resolve its m_SourcePrefab through the package. If the source is a Model, synthesize the prefab root from that model. If the source is a .prefab, or the GUID is missing, keep throwing with the correct message: "prefab variant" only for a Prefab source, "missing prefab reference" for an absent GUID.
	2. Build the root GameObject and transform plus one ModelRenderer per mesh node from Context.ModelFor and the FBX node list. Do not assume one renderer per prefab. Use Unity's fixed model-prefab IDs: root GameObject 100000, root Transform 400000, MeshRenderer 2300000/2300002/2300004..., mesh sub-assets 4300000/4300002/4300004....
	3. Apply the PrefabInstance modifications in Unity's order, keyed by target fileID and propertyPath: m_Name and m_IsActive (100000), m_LocalPosition/Rotation/Scale (400000), m_StaticEditorFlags, and m_Materials.Array.data[i] (2300000+). Material binding for those fixed renderer IDs is task 02's map; call it and pass the resolved binding to Assign so a wrapper override lands on the same slot the model uses. Unknown paths are reported once per path with a count.
	4. Attach non-stripped components whose m_GameObject is the stripped model object: MeshCollider becomes a ModelCollider/HullPerMesh, BoxCollider becomes Sandbox.BoxCollider, Sphere/Capsule if present. Baseline: 240 MeshCollider blocks across 218 prefabs (208 with one, 7 with two, 3 with six); 59 BoxCollider blocks across 58 prefabs (57 with one, 1 with two); no SphereCollider or CapsuleCollider attached to a model object. Report multiple MeshColliders that s&box cannot represent rather than dropping them.
	5. The 24 wrappers that nest .prefab instances (Chair_01 has 2, Stairs_Snaps015 has 70) reuse the existing nested-instance path. Convert nested wrapper sources first, as UnityPrefabExport.Order already does, and keep their overrides. No new variant-chain semantics are needed.
	6. 221 wrappers contain 270 MonoBehaviour (class 114) blocks attached to stripped model objects; 18 files additionally list components in m_AddedComponents. Scripts cannot convert: report each file once, or one grouped line with the count and sample names. No added component is dropped silently.
	7. Keep the mirrored-variant path working, and keep output for packages whose prefabs already convert byte-identical.

	Non-goals: the fixed-ID material binding map (task 02), scene instances and plain objects (task 03), built-in primitives (task 04), loss diagnostics (task 05).

	Acceptance:
	- Harness on Testing/old_school_project.unitypackage with --scenes: 346 .prefab files are written, the report's Converted count includes all 346 prefabs, and there are zero "no root transform" or other prefab conversion-skipped issues.
	- Every generated prefab parses as JSON and has a root object; Doorplate_03_Snaps014 gets one ModelCollider; a multi-mesh model keeps every renderer its FBX has.
	- The 221 script-bearing files and 18 m_AddedComponents files are reported; none is dropped silently.
	- Re-running Testing/building.unitypackage or Testing/city--1.unitypackage is byte-identical to before the change.
	- Editor/Core builds with 0 warnings, 0 errors.

## Notes

	Implementation home: Editor/Core/UnityPrefabExport.cs (Roots at 706, Convert at 1166, throw at 1173, nested-instance child loop at 961), with model facts from Editor/Core/UnityModel.cs, material slots from UnityPrefabBindings, and the scene reuse point in UnitySceneExport.SourceOf.

	Baseline facts (2026-11-20, old_school package):
	- 346 prefabs: 322 single top-level instance, 24 with nested .prefab instances; all 346 top-level sources are FBX; 18 non-empty m_AddedComponents; 221 files / 270 stripped-object MonoBehaviours.
	- Material overrides in wrappers: 1,270 m_Materials.Array.data entries. Task 02 clears the 323 prefab-material-assignment warnings those produce.
	- Scene side after this task: MergeV2 2,861 instances and Test_Range 201 become resolvable, but task 03 owns placing direct model instances and plain objects; expect the stubs only once 03 lands.

	Verification command shape (in-repo harness, no editor):
	dotnet build Documentation/Unity-To-SBox-Automated/harness/harness.csproj -c Release -p:IUP_ROOT=C:/cseliot/1-Development/1-SBox/2-Projects/unityimporterplus
	dotnet <harness.dll> --scenes Testing/old_school_project.unitypackage <fresh scratch>/Assets <scratch>/report.json
	Then verify_output.py <fresh scratch>/Assets <fresh scratch>/Assets/imported, and compare two runs with diff -r. Do not re-export the 1.27 GB Unity project.

## Clarification Q&A


## Relevant History 

	Source: tasks/sbox-expert-research.md, reconciled 2026-11-20 against tasks/unity-expert-research.md and the package YAML. The earlier sbox backlog split this work into "FBX base" (328) and "prefab base and chains" (18); that split was a parse error: the 18 files are wrappers whose top-level source is an FBX while a nested PrefabInstance points at another .prefab. The unity expert's baseline is authoritative: 346 top-level FBX, 322 single / 24 nested, 18 m_AddedComponents, 221 script-bearing.

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
	Task 02 must land with or immediately after this task for wrapper materials; task 03 then resolves scene instances of these prefabs and direct model instances. Task 05 reports any wrapper that still fails.