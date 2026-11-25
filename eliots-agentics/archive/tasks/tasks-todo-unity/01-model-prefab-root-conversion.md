## Status: STARTED? NO! CLARIFIED? NO! HISTORY? NO! REVIEWED? NO! DONE? NO! WORKER: NONE OTHER: backlog

## Task
	Convert model-wrapper Unity prefabs (a prefab whose content is a PrefabInstance of an FBX) instead of rejecting them as unsupported prefab variants.

### Description
	A model-wrapper prefab is a .prefab whose only real content is one top-level PrefabInstance of an FBX. Unity strips the root GameObject and Transform and stores the model instance plus added components and overrides. All 346 prefabs in Testing/old_school_project.unitypackage are built this way: every top-level instance sources an FBX; 322 files contain only that instance, 24 also nest .prefab instances, and none has a non-stripped root Transform. UnityPrefabExport.Converter.Roots only holds non-stripped Transform blocks (Editor/Core/UnityPrefabExport.cs:706), so Convert finds no root and throws "Prefab has no root transform (prefab variants are not supported)" at line 1173. The import reports 346 conversion-skipped Errors, generates zero .prefab files, and the scenes that place those prefabs come out empty.

### Work Review


## Notes
	Implementation:
	- In UnityPrefabExport.Convert, when converter.Roots is empty, look for a top-level PrefabInstance (m_TransformParent fileID 0). If its m_SourcePrefab resolves to UnityAssetKind.Model, synthesize a prefab root from that model. Keep throwing for a source that is a .prefab only or missing; those are true variants or broken references and remain unsupported.
	- Build the root GameObject and ModelRenderer(s) from Context.ModelFor/GeneratedModels, the same VMDL paths Unity renders use. The model is already converted before prefabs, so no extra model conversion is needed.
	- Apply the PrefabInstance modifications: m_Name and m_IsActive on the model root GameObject (fileID 100000), m_LocalPosition/Rotation/Scale on the root Transform (400000), m_StaticEditorFlags, and m_Materials.Array.data[i] on renderers (2300000, 2300002, ...). Follow Unity's fixed model-prefab IDs; do not assume one renderer per prefab. Report an unknown property path once with a count, not per prefab.
	- Attach non-stripped components whose m_GameObject is the stripped model object: MeshCollider becomes a ModelCollider/HullPerMesh; Box/Sphere/Capsule become Sandbox colliders. Baseline: 240 MeshColliders across 218 prefabs (208 with one, 7 with two, 3 with six); 59 BoxColliders across 58 prefabs (57 with one, 1 with two); no SphereCollider or CapsuleCollider attached to a model object. Report multiple MeshColliders that s&box cannot represent rather than dropping them silently.
	- 24 wrappers also reference a nested .prefab. UnityPrefabExport.Order already converts sources first; keep that ordering and reuse the existing nested-instance path.
	- 221 wrappers contain 270 MonoBehaviour (class 114) blocks attached to stripped model objects; separately, 18 files have a non-empty m_AddedComponents list. Scripts cannot convert; report them once per file or as one grouped line with the count and sample names. Do not drop them silently.
	- Do not change output for packages whose prefabs already convert; compare a building.unitypackage or city--1.unitypackage run before and after.
	- Keep the mirrored-variant path working: a wrapper placed with a negative scale still resolves to its `_mirror_<axis>` prefab and mirrored model.

	Non-goals: fixed-ID material binding (task 02), scene instances and plain objects (task 03), built-in primitive meshes (task 05), loss diagnostics (task 04).

	Acceptance:
	- Harness on Testing/old_school_project.unitypackage with --scenes: 346 .prefab files are written, the report's Converted count includes all 346 prefabs, and there are zero "no root transform" or other prefab conversion-skipped issues.
	- Every generated prefab parses as JSON, has a root object, and gives Doorplate_03_Snaps014 one ModelCollider. A multi-mesh model keeps every renderer its FBX has.
	- verify_output.py reports 0 problems on the generated Assets tree; two harness runs into fresh directories are byte-identical.
	- The existing building or city import is byte-identical to the pre-change run.
	- Editor/Core builds with 0 warnings, 0 errors.

## Clarification Q&A


## Relevant History 
	Baseline report (current tree, 2026-11-20): 346/346 prefabs conversion-skipped with the variant message; 0 generated prefabs. The package is Testing/old_school_project.unitypackage, exported from C:/cseliot/1-Development/4-Unity/2-Projects/old_school_model. The report and harness are described in tasks/unity-expert-research.md.

	Verification command shape (in-repo harness, no editor):
	dotnet build Documentation/Unity-To-SBox-Automated/harness/harness.csproj -c Release -p:IUP_ROOT=C:/cseliot/1-Development/1-SBox/2-Projects/unityimporterplus
	dotnet <harness.dll> --scenes Testing/old_school_project.unitypackage <fresh scratch>/Assets <scratch>/report.json
	Then verify_output.py <fresh scratch>/Assets <fresh scratch>/Assets/imported and compare two runs with diff -r. Do not re-export the 1.27 GB Unity project.

	Related backlog: tasks-todo-sbox/01 is now a reconciled twin of this task (its earlier 328-direct / 18-chained split was a parse error and its current file uses the verified 346/322/24 split). Work one set, not both; do not implement the wrapper conversion twice.

## Stylistic Task Notes:
References to variables like X and Y automatically imply that these are to be exposed properties for developer tweaking.

## Skills To Know: for you and/or subagents:
	unityimporterplus

## Post-Task Follow Ups