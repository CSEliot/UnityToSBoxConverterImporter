## Status: STARTED? NO! CLARIFIED? NO! HISTORY? NO! REVIEWED? NO! DONE? NO! WORKER: NONE OTHER: fresh backlog from resolve-errors-part-c (console group G-C)

## Task
Give post-process effect components GUIDs distinct from their host GameObjects.

### Description
Created from the 2026-11-22 console-out-2 census in `resolve-errors-part-c-research.md`
(group G-C). Full code trace: scratch `taskmaster-nov22/part-c-code.md`.

Symptom (Testing/console-out-2.txt lines 3-6, 4 WARNING):
  Generic Sandbox.Tonemapping: Guid 82a3f100-b4a5-3f93-a94c-c3b9505c8937 is already taken by
  GameObject:Tonemapping - changing
  Generic Sandbox.Bloom: Guid bc06616b-aa92-3251-93c6-6f19279b649a is already taken by
  GameObject:Bloom - changing
  Generic Sandbox.DepthOfField: Guid 8e1c5e0a-5556-319e-8ddd-360694e17d75 is already taken by
  GameObject:Depth of Field - changing
  Generic Sandbox.AmbientOcclusion: Guid 34ec698a-355e-3c1a-8d78-897b8b0a19a1 is already taken
  by GameObject:Ambient Occlusion - changing

Verified mechanism at HEAD 54ca0a6:
- `Editor/Core/UnitySceneExport.cs:389-390`, the `Effect` closure inside the post-processing
  loop (`:364-407`):
    children.Add( SceneObject( $"{key}:{type}", name, "",
        Component( type, StableGuid( $"{key}:{type}" ), true, properties ) ) );
- `key = $"{c.Key}:postprocessing:{index}"` (`:366`); `type` is Sandbox.Tonemapping / Bloom /
  DepthOfField / AmbientOcclusion (`:391,396,398,403`).
- `SceneObject` builds the GameObject with `GameObject( StableGuid( key ), ... )`
  (`UnitySceneExport.cs:519-525`, `:521`); `Component` writes `["__guid"] = id.ToString()`
  (`:560-565`, `:562`). So the effect child's GameObject `__guid` and its own component
  `__guid` are the SAME `StableGuid($"{key}:{type}")`, and s&box reassigns one.
- `StableGuid` is MD5-based over `"unityimporterplus:" + key` (`UnityPrefabExport.cs:214-220`),
  RFC-4122 v3. `index` and `type` are in the key, so different volumes/effects already get
  distinct GUIDs - this is NOT a hardcoded GUID shared across volumes.
- Verified empirically: generated
  `Assets/imported/assetpackschoolnameverylong/schoolhdrpnameverylong/scenes/mergev2.scene`
  contains `82a3f100-b4a5-3f93-a94c-c3b9505c8937` (Tonemapping child). The Unity HDRP
  override script GUIDs at `UnityVolume.cs:39-42` are bare 32-hex asset GUIDs and do NOT
  match the dashed v3 console UUIDs.
- The same pattern is already done correctly for skybox / ambient / fog / PostProcessVolume
  components at `UnitySceneExport.cs:263,271,284,341,367` (`:component` suffix).

Required work:
1. At `UnitySceneExport.cs:390`, give the component a distinct key, e.g.
   `StableGuid( $"{key}:{type}:component" )`. Deterministic, engine-free, minimal. Do not
   change the GameObject's own GUID - prefab/scene links may depend on it.
2. Confirm the four effect components in generated scenes now have GUIDs different from
   their host GameObjects, and that all other components keep their existing GUIDs.
3. Regression gates: harness build 0 errors / 0 warnings; `building.unitypackage` (24 files,
   no --scenes) and `city--1.unitypackage` (1,166 files, no --scenes) byte-identical to a
   fresh HEAD-code baseline with BOTH trees populated (note: these packages may contain no
   post-process volumes, so also run a scene-bearing fixture); `verify_output.py <outRoot>
   <outRoot>/imported` 0 problems; real corpus `Testing/s.unitypackage --scenes` before/after
   with a per-effect GUID uniqueness check; two identical runs byte-identical; BOM-less.
4. Report the exact changed-file set and the before/after GUID relationship for all four
   effect types. `Testing/console-out-2.txt` is a STATIC editor log; do not claim its lines
   are gone. The verifiable proxy is that generated effect components no longer collide.

Do not add unrequested tooling. Stage by explicit path. No push until the Master verifies.

### Work Review


## Notes


## Clarification Q&A


## Relevant History 


## Stylistic Task Notes:
References to variables like X and Y automatically imply that these are to be exposed properties for developer tweaking.
When programming, portability and DRY are a priority.

## Post-Task Follow Ups 
