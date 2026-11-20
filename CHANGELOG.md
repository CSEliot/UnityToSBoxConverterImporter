# Changelog

## Unreleased — 2026-10-01 (mirrored placements)

- A placement with a negative scale (a Unity mirror; 32 instances in the city Demo scene, placed on 6 prefabs) rendered inside out in s&box, which does not flip the face winding the way Unity does. No generated transform carries a negative scale any more: an even number of negative axes becomes a half turn folded into the rotation, an odd number becomes a positive scale on a **mirrored variant** of the prefab (`<prefab>_mirror_<x|y|z>.prefab`, the Unity axis it mirrors along) whose models are mirrored variants too (`<model>_mirror_<axis>.vmdl`, ModelDoc's `ScaleAndMirror` modifier with the matching `mirror_*` flag, which also flips the winding). Inside a variant every object is seen through the mirror: positions, rotations, collider centres and nested instances, which pick up their own variants. Objects with a negative scale inside prefabs and scenes get the same treatment for their subtree. Variants are generated on demand and listed in the report; outputs without mirrored placements are byte-identical to before.

## Unreleased — 2026-10-01 (scenes)

- **Advanced → Also import Unity scenes (.unity → .scene)** converts each selected Unity scene into an s&box scene after its prefabs, models and materials are generated (`imported/<unity folder>/<name>.scene`, beside the copied `.unity` source). Selecting a scene pulls in the prefabs it places, the meshes its own objects render and the materials its overrides assign. The toggle is off by default; with it off, scenes show as **(scene import off)** in the tree and are left out of **Select all**.
- Root objects keep Unity's order (`SceneRoots`, else `m_RootOrder`, else file order). Prefab instances become `__Prefab` stubs with the same Position/Rotation/Scale/Name/Enabled, child-object and material overrides the prefab converter writes for nested instances. Instances Unity parents under an object of another instance, and plain objects added there, are written as `AddedObjects` of that instance's patch, parented to the generated prefab's own object GUID (the engine applies a patch before mapping prefab GUIDs to instance GUIDs); added Box/Sphere/Capsule colliders and MeshColliders on package meshes become added components, removed objects and components become `RemovedObjects`. A constant in `UnitySceneExport` flattens nested instances to the scene root instead, should the editor reject the patch.
- Cameras, audio listeners and directional, point and spot lights convert on scene objects and inside prefabs (`CameraComponent` with Unity's vertical field of view, near/far planes in inches and the first enabled camera as the main camera; light colour = linear colour × colour temperature × intensity, shadows on/off, point/spot radius and cone angles). Area lights are reported. The scene gets the default s&box sky when Unity had a skybox, a flat `AmbientLight` for flat or trilight ambient, and a tall `GradientFog` for Unity fog. Objects with any Unity static flag get `GameObjectFlags.Static`; `MainCamera`-tagged objects get the `maincamera` tag.
- Scripts, terrain, lightmaps, light probes, navigation and occlusion data, reflection probes and post-processing volumes are listed in the report, not converted. Unity 2018.2-and-earlier scene files (instances serialized in full) are reported as unsupported. The report gains a `SceneConversions` table (roots, instances, nested, added components, removed objects, skipped).
- The headless harness takes `--scenes`. Every existing prefab and model is byte-identical with the toggle off and on; the prefab converter's object and instance conversions moved into a shared `UnityPrefabExport.Converter` that reads each source prefab once per file.

## Unreleased — 2026-10-01

- Models with bodygroups now import each mesh through its own `RenderMeshFile` node named after the mesh and filtered to that FBX node, the layout the Citizen uses. A bodygroup choice refers to the `RenderMeshFile` name, so the first import's single unnamed node compiled all nine outfits into one `unnamed_1` mesh with ten empty choices and nothing rendering in the prefabs. Static models keep their single node and are byte-identical to before.
- Skinned FBX models import through a derived copy (`*_iup_skinned.fbx`, beside the untouched source) whose Geometry objects carry their mesh-node names and whose duplicate bone names get their side appended (`Finger_01_L` / `Finger_01_R`), so ModelDoc lists meshes and bones by name instead of `unnamed` and `_duplicate`. The rewriter copies every other byte unchanged and recomputes record offsets and the footer; writing a file back without renames reproduces all 332 binary FBX files in the test packages byte for byte.

## Unreleased — 2026-09-30

- Skinned FBX models are detected (skin deformers on the meshes, in binary and ASCII FBX) and labelled **Skinned model → VMDL**. Their generated `.vmdl` keeps the FBX skeleton and skin weights and adds a `BodyGroupList`: when the package's prefabs show at most one skinned mesh at a time, one bodygroup named after the FBX offers each mesh plus `none`; otherwise every skinned mesh gets an on/off group. A skinned FBX is never split into per-mesh models.
- Unity `SkinnedMeshRenderer`s convert to one `Sandbox.SkinnedModelRenderer` per skeleton, placed on the parent of the skeleton's root bone (the FBX root) with `BodyGroups` set to the meshes Unity had active (one bit per choice, groups in order). The objects that only held a skinned mesh and the pure bone objects are dropped; bone objects with other components or children stay as plain objects with a warning. Animators are listed as not converted.
- FBX animation is inspected: a take whose curves never change (the exporter default) is ignored; real animation and blend shapes are reported for follow-up in ModelDoc.
- PolygonCity characters: the 18 character prefabs now render one outfit each through a `SkinnedModelRenderer` instead of nine stacked meshes with no renderer and 49 dead bone objects. `FixedScaleCharacters/Character_Male_Jacket` shows `none`, as it does in Unity.

## Unreleased — 2026-09-29

- Convert Unity prefabs to s&box `.prefab` assets (`UnityPrefabExport`): hierarchy with transforms, names and enabled state, ModelRenderers on generated models, ModelColliders from MeshColliders, nested prefab instances, and per-instance material/enabled overrides. Prefab variants are reported as unsupported and skipped.
- Generated models now scale metres to inches via a `ModelModifier_ScaleAndMirror` (39.37); `import_scale` stays Unity-unit.
- Unity convex MeshColliders become convex hull physics shapes (`HullPerMesh`) on the generated model. Baked convex-collider source `.asset` meshes are still not convertible.
- Untextured transparent Unity materials generate glass materials; normal maps carry their strength setting.
- Output folders keep their Unity casing while generated file names stay lowercase, fixing split same-name-different-case folders on Linux.
- On Linux, temp-folder cleanup no longer mistakes a busy lock file for an error.
- Fixed: instance overrides without an `objectReference: {fileID, guid}` line were silently dropped; oversized or broken prefabs could crash package loading or the whole import (now a reported per-asset issue; `conversion-skipped` is Error severity, and prefabs over 16 MiB are still skipped silently during binding resolution); a duplicate transform could crash the converter; YAML quotes leaked into object names.

## Unreleased — 2026-09-16

- Resolve prefab material overrides to individual FBX renderer slots using source GUIDs, hierarchy-derived renderer IDs and ordered material connections. Preserve LOD0 assignments and report ambiguous or unsupported bindings.
- Resolve prefab-only slot-0 assignments for FBX meshes without material definitions, including their material/texture dependencies. Preserve different materials per mesh and warn on conflicting or unsupported assignments.
- Keep models referenced only by prefab MeshColliders as labelled collision sources without generating visible VMDLs. Shared visual/collider sources retain normal model output. Existing outputs are not deleted and physics setup is not recreated.
- Existing imports need regenerated VMDLs with Replace to pick up the corrected assignments.

## 0.1.0-beta.1 — release candidate, 2026-09-15

- Import selected Unity package models, materials and textures through a checkable dependency tree.
- Review differing destination files by content with Keep/Replace; skip identical files without rewriting.
- Prepare generated resources in dependency order and allow time for s&box to publish compiled dependency records.
- Preserve prefab-derived material assignments when preparing a merge.
- Decode packed textures once per source, process up to two independent images concurrently, and skip writing byte-identical outputs.
- Show bulk overwrite controls only for actual conflicts. On completion, show Reimport on the left and primary Done on the right.
- Keep informational model review advice out of warning totals; distinguish compatibility warnings from conversion and compilation errors.
- Use `Assets/Imported/.iup-temp/<unique-run-id>/` for temporary work and clean it after completion, cancellation or failure; sweep abandoned runs without touching active imports.
- Keep no persistent import records, automatic reports, backups, rollback data or conversion cache. Reports are optional exports.
- Place derived textures, repaired meshes and fallback materials beside their sources, with simple numbered suffixes only for collisions and content reuse on reimport. Keep legacy `_unity_generated` dependencies intact.
- Display the current operation in a green stage label above the progress bar, including preparation, confirmation and cleanup.
- Report cleanup failures when closing or switching packages, clear previous report data, and remove only newly created empty output folders after interrupted writes.
- Require an explicit absolute Assets path for archive extraction; remove unused archive-view and GUID-alias code.
- Keep developer validation commands in the host project. MIT licensed.

### Scope

Material conversion maps common properties. Arbitrary Unity shader programs, wind and vertex-layer effects, Unity terrain scenes and animation controllers are not reproduced. (Updated 2026-09-29: Unity prefab hierarchies with nested instances and material/enabled overrides, and convex MeshCollider-to-`HullPerMesh` physics, are now converted; Unity scene layout remains out of scope.) Shader warnings can accompany a successful import. Existing imports in package/ID folders are not automatically relocated.
