## Status: STARTED? YES! CLARIFIED? YES! HISTORY? YES! REVIEWED? YES! DONE? YES! WORKER: sa-0-0354b674 OTHER: backlog; reconciled with tasks/unity-expert-research.md and the unity backlog

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

	None needed: the task pins the wrapper shape, the fixed model-prefab ids, the required error messages and the
	acceptance checks, and the package was measured before starting (346/346 wrappers, 0 with a root Transform),
	so no question would change the work.

## Work Report 2026-11-21 (worker sa-0-0354b674)

	Plan (posted before implementing; STARTED? was already YES!):
	1. Converter recognises a wrapper when Roots is empty and one top-level PrefabInstance exists; resolve its
	   m_SourcePrefab: Model -> rebuild Unity's model prefab, Prefab -> keep "prefab variants are not supported",
	   absent GUID -> "missing prefab reference".
	2. Root GameObject 100000 / Transform 400000 from the instance's own overrides (RootOf); position written 0 like
	   every prefab root, rotation and scale kept.
	3. One ModelRenderer per distinct Context.ModelFor of the FBX's RenderedMeshes, GUID StableGuid(prefab:2300000+2n)
	   so a nested override names the generated component directly.
	4. m_Modifications applied keyed by (target, propertyPath); unknown paths aggregated once per path with a count.
	5. Colliders from the non-stripped blocks whose m_GameObject is a stripped model object.
	6. Nested instances under the root through the existing Instance() path; Source/Instance gained wrapper roots.
	7. UnityPrefabBindings resolves the fixed renderer ids so wrapper material overrides reach the model.
	8. Verify: harness build, two school runs (diff), building/city byte-identity, verify_output.py.

	Changed files:
	- Editor/Core/UnityPrefabExport.cs: ModelRootObject/ModelRootTransform/IsModelRenderer/ModelRendererId,
	  ModificationPattern, Reference() (Unity folds a long reference onto two lines, which Field() cannot read),
	  ModelRoot + RootOf(), Converter.Wrapper/WrapperModel, Converter.WrapperRoot(), Source.IsWrapper/
	  RootTransformId/RootTransform/RootName/RootActive/RootFlags/RendererIds, wrapper roots in SourceOf and
	  Instance(), the Convert() branch.
	- Editor/Core/UnityPrefabBindings.cs: WrapperRenderer() (2300000+2n -> FBX mesh through the recycle map) and
	  WrapperMaterialSlot() (the seam task 02 owns), wrapper MeshCollider m_Mesh as collision evidence, wrapper
	  material overrides resolved before Assign.

	Verified (harness, no editor; scratch runs under
	C:/Users/Eliot/AppData/Local/hermes/profiles/hermes-sbox/cache/scratch/iup-task01/):
	- Harness build of Editor/Core (harness.csproj compiles Editor/Core/*.cs): 0 warnings, 0 errors. The
	  engine-bound Editor/*.editor.csproj still reports 11 warnings, 3 of them pre-existing CS1570 doc comments
	  in UnityImport.cs, which this task does not touch.
	- old_school_project.unitypackage --scenes: 346/346 prefabs written (361 .prefab files = 346 + 15 mirrored
	  variants), Converted 540 -> 984, Files 1714 -> 2158, conversion-skipped 346 -> 0, Unresolved 1131 -> 354.
	- Every generated prefab parses as JSON, has a RootObject, and has at least one ModelRenderer (0 without).
	- Doorplate_03_Snaps014: one ModelRenderer + one ModelCollider, model carries the collided mesh's shapes.
	- Multi-mesh models keep every renderer: 28 prefabs have more than one (Counter01 9, Door_02_01 4); the 5
	  wrappers that reference several mesh sub-assets all render each of them.
	- Stairs_Snaps015 keeps its 69 nested instances as children of the root; 110 prefabs carry the static flag.
	- Reported, not dropped: 233 lines for the 221 script-bearing prefabs (form-filling 12 that hold two
	  MonoBehaviour blocks, plus mirrored variants), 19 lines for the 18 m_AddedComponents files (+1 mirrored),
	  one line per prefab for reduced MeshColliders (11), sub-object colliders, unmappable material slots and
	  unconverted override paths (per path with a count).
	- Determinism: two full runs into fresh folders, diff -r identical. verify_output.py: 887 files, 0 problems.
	- building.unitypackage and city--1.unitypackage: diff -r identical to the pre-change baselines.
	- Material warnings fall as a side effect of the fixed-id map: prefab-material-assignment 323 -> 242,
	  unassigned-material 445 -> 95. The rest is task 02's ModelRenderer slot map.

	Facts found while measuring (they matter for the tasks after this one):
	- All 154 nested instances in the 24 nesting wrappers hang off ONE stripped transform per file whose
	  corresponding source object is 400000, i.e. the model root, so they are root children, not sub-node children.
	- A nested instance's override ids (and a scene instance's) are Unity-derived ids that appear NOWHERE in the
	  source prefab's YAML (checked Staircase_Wall_03: both the object and the transform target have no block).
	  They are therefore matched by property class, not by id: m_Name/m_IsActive/m_StaticEditorFlags belong to the
	  model root object, m_Local* to its transform, m_Materials.Array.data[i] to renderer 2300000+2n. A scene
	  instance of a wrapper still loses its placement overrides: that id space is task 03's problem, and the report
	  says so per instance instead of dropping them.
	- Wrappers override model sub-node transforms (400002+): 400004 and 400008 dominate. Those placements live in
	  the generated model, so they are reported as "overrides of model sub-objects" rather than applied twice.
	- s&box cannot express a per-instance static flag; scene instances that set m_StaticEditorFlags on a wrapper
	  whose prefab does not are reported (1012 lines) rather than silently ignored.

	Left for the reviewer: no commit was made (worker.md: report, then the critic reviews, then DONE and commit).


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

### Work Review
2026-11-21 00:25 - Verdict: changes required

Tree: HEAD b2df09f + working tree (2 code files, task file)
  git hash-object --path="Editor/Core/UnityPrefabExport.cs" "Editor/Core/UnityPrefabExport.cs"  -> 5dbc8316b1b4c4e2e01066a026012c89d2a8470c
  git hash-object --path="Editor/Core/UnityPrefabBindings.cs" "Editor/Core/UnityPrefabBindings.cs" -> f312ed4506111d076562c17b104e334d1d83a4b0

1. [Medium] Editor/Core/UnityPrefabExport.cs:592 (`var unity = NoTransform;`) writes through the shared
   static identity record at :608-:610 (`unity.Rotation[axis]`, `.Scale[axis]`, `.Position[axis]`).
   UnityTransform is a record over mutable arrays (:40) and NoTransform is `static readonly` (:45), so a
   wrapper root override mutates the global identity for the rest of the run (later wrapper roots, the
   `NoTransform` fallbacks at :1147 and UnitySceneExport.cs:132/372). It is invisible in this package only
   because every wrapper root override is identity: all 361 generated roots are `0,0,0,1 / 1,1,1 / 0,0,0`
   and prefab_facts.json has 0/346 non-identity rotations, 0 non-zero positions, 0 non-unit scales. A
   package with one rotated/scaled wrapper silently misplaces everything converted after it. Fix: build a
   fresh UnityTransform at :592 (`new UnityTransform(new double[3], new[]{0d,0d,0d,1d}, new[]{1d,1d,1d})`)
   or clone the arrays before writing.
2. [Low] Editor/Core/UnityPrefabExport.cs:1044 handles only collider blocks (64/65/135/136). Other
   non-stripped components on model objects are dropped with no report line, against item 6's "No added
   component is dropped silently". Live case: Brand_01_Snaps014 carries a non-stripped MeshRenderer
   (`m_GameObject: 6979160220768290091`, empty `m_Materials`, no m_Mesh) and gets no report line. Fix:
   report every non-stripped component block the loop does not convert.
3. [Low] Editor/Core/UnityPrefabExport.cs:1057 falls back to `renderers[0].Path` when a MeshCollider's mesh
   does not resolve, so the collider silently collides with the first model; the neighbouring branch at
   :1059 reports the failure. Fix: report the fallback (count per prefab).
4. [Low] Work Report "Editor/Core build: 0 warnings" is true only for the harness project (it compiles
   Editor/Core/*.cs): 0 warnings, 0 errors. `dotnet build Editor/unityimporterplus.editor.csproj` gives 0
   errors and 11 warnings, 3 of them pre-existing CS1570 in unmodified UnityImport.cs:57/68/74. Acceptance
   is met; reword the claim to "harness build of Editor/Core".

Verified (evidence on disk, scratch/iup-task01):
- Diff is exactly the reviewed tree; saved diff == live `git diff HEAD`; only the two Core files + task file.
- 346/346 wrappers convert: 361 .prefab files (346 + 15 mirrored), Converted 984, Files 2158, Unresolved 354,
  0 Errors/Conflicts; "no root transform" 346 (base-school) -> 0.
- All 361 parse as JSON objects with RootObject and >=1 ModelRenderer (28 multi-renderer, max 9 Counter01,
  Door_02_01 4); Doorplate_03_Snaps014 has 1 ModelRenderer + 1 ModelCollider and its vmdl carries
  PhysicsMeshFromRender.
- Colliders: 240 MeshCollider blocks / 218 prefabs -> 232 generated ModelColliders; 59 BoxCollider blocks /
  58 prefabs -> 57x1 + 1x2; multiple MeshColliders reported (11 lines).
- Nesting: Stairs_Snaps015 69 stubs, Chair_01 1 stub (task's 70/2 count the top-level instance too).
- Reporting: 233 prefab script lines (221 + 12 mirrored; +2 scene lines) and 19 m_AddedComponents lines
  (18 + 1 mirrored); nothing dropped silently for those two classes.
- Determinism: diff -r runA/runB == 0; verify_output.py 887 files, 0 problems.
- Byte-identical gates: building and city--1 base vs current both diff -r == 0.
- Error strings: WrapperRoot :960-965 gives "missing prefab reference" for an absent GUID, "prefab variants
   are not supported" only for a Prefab source; prefab-variant path is unexercised by this package.

#### Worker reply 2026-11-21 (sa-0-0354b674)

	All four findings accepted; none contested. The reviewed files changed, so REVIEWED? stays NO! and this
	returns for another review.

	1. [Medium] Accepted, real bug. RootOf now builds a fresh `new UnityTransform( new double[3], {0,0,0,1},
	   {1,1,1} )` instead of writing through the `static readonly NoTransform` record. No other writer of a
	   `NoTransform`-derived array was introduced by this task (WrapperRoot copies the arrays it emits via
	   ToSbox, and Instance() already cloned plain roots at :1089-1095).
	2. [Low] Accepted. WrapperRoot now names every non-stripped component block it does not convert:
	   "components on the model that s&box has no counterpart for were not converted: N <Type>". Verified live
	   on the reviewer's case: exactly one line, `Brand_01_Snaps014: ... 1 MeshRenderer.` A first cut of this
	   filter also counted the wrapper's own PrefabInstance block (type 1001), which made all 361 files report
	   "1 Unity type 1001"; types 1/4/224/1001 are now excluded, and the corrected run reports that single line.
	3. [Low] Accepted. The `renderers[0].Path` fallback increments `colliderFallbacks` and reports
	   "N MeshCollider(s) name a mesh with no generated model; their collider uses the model's own shapes
	   instead." (5 lines in the school package, alongside the 11 reduced-MeshCollider lines).
	4. [Low] Accepted. The Work Report line now says "harness build of Editor/Core (harness.csproj compiles
	   Editor/Core/*.cs): 0 warnings, 0 errors" and states the 11 warnings / 3 pre-existing CS1570 of the
	   engine-bound editor csproj.

	Re-verification after the fixes (scratch/iup-task01, runs runC and runD):
	- Harness build of Editor/Core: 0 warnings, 0 errors.
	- old_school_project --scenes: assets 1277, Converted 984, Files 2158, Unresolved 354, conversion-skipped
	  0; 361 .prefab files; `diff -r runA/Assets/Imported runD/Assets/Imported` reports no difference, so the
	  fixes changed reporting only and no generated asset.
	- Reporting after the fix: 1 no-counterpart line (Brand_01's MeshRenderer), 5 collider-fallback lines,
	  233 script lines, 19 m_AddedComponents lines.
	- Byte-identity re-run with the NoTransform fix in place: building.unitypackage and city--1.unitypackage
	  both `diff -r` clean against their pre-change baselines.
	- Determinism and verify_output.py were re-run before the review (two identical runs, 887 files, 0
	  problems); the fixes touch Information strings and one local variable, so those results stand.

### Work Review
2026-11-21 00:41 - Verdict: no further changes

Tree: HEAD b2df09f + working tree (2 code files, task file)
  UnityPrefabExport.cs  -> 94636c8dd15e5cc57d02a185e741281d99a0951d
  UnityPrefabBindings.cs -> f312ed4506111d076562c17b104e334d1d83a4b0

All four prior findings are fixed in this tree:
1. [Medium] NoTransform aliasing: UnityPrefabExport.cs:593 now builds a fresh
   `new UnityTransform(new double[3], {0,0,0,1}, {1,1,1})`. No other writer of a NoTransform-derived
   array remains: Compose/ToSbox/Unmirror/Multiply/Add return fresh arrays; :1155 is a read-only local;
   :1248 clones the source root arrays.
2. [Low] Unlisted components: :1086-1088 excludes only types 1/4/224/1001/64/65/135/136/114 and reports
   the rest grouped by name and count. Live case: exactly one line for Brand_01_Snaps014 (1 MeshRenderer).
3. [Low] Collider fallback: :1060 counts it and :1084 reports it; 5 lines in the school package
   (Pencil_Case_01, Staircase_Wall_02, Stairs_01, Stairs_Floor_03, Eaves_02).
4. [Low] Build wording: the Work Report now says "harness build of Editor/Core (harness.csproj compiles
   Editor/Core/*.cs): 0 warnings, 0 errors" and records the engine-bound csproj's 11 warnings / 3
   pre-existing CS1570.

Acceptance re-verified independently on the fixed tree (fresh run, byte-identical to the worker's runD):
- 346/346 prefabs; 361 .prefab files (346 + 15 mirror variants); 0 unresolved prefab issues; 0 "root
  transform" matches.
- All 361 parse as JSON with RootObject and at least one ModelRenderer; 28 multi-renderer (max 9,
  Counter01); 232 ModelColliders from 240 blocks; Doorplate_03_Snaps014 has 1 ModelRenderer + 1
  ModelCollider with PhysicsMeshFromRender in the vmdl.
- Reporting: 233 prefab script lines plus 2 scene lines (235 total), 19 m_AddedComponents lines, 11
  reduced-MeshCollider lines, 50 sub-object-collider lines, 154 nested-under-model-object lines; 24
  prefabs with children (Stairs_Snaps015 69, Chair_01 1).
- Determinism: independent run's Assets diff -r clean against runD; verify_output.py 887 files, 0 problems.
- building.unitypackage and city--1.unitypackage with --scenes: diff -r clean against their pre-change
  baselines.
- Scope: only the two Editor/Core files and the task file are modified; no untracked files; hashes match.