# Scene Importer

> **Implemented** in `Editor/Core/UnitySceneExport.cs` with the shared `UnityPrefabExport.Converter`. One correction
> to section 5.3, from reading `GameObject.Serialize.cs`: the engine applies `__PrefabInstancePatch` to the prefab's
> own JSON (`Json.ApplyPatch( fullPrefabData, … )`) *before* `RemapPrefabIdsToInstanceIds`, so `AddedObjects.Parent`,
> `RemovedObjects.Id` and `PropertyOverrides.Target` all name the **generated prefab's** GUIDs
> (`StableGuid( "{S.guid}:{g}" )`), not the instance-side ones. Tracked types are `GameObject` (a stub counts as one,
> atomically) and `Component`. Everything else below was implemented as written; the Demo counts in section 6 hold
> (`Scale` overrides are 249 rather than 199 because, as for nested prefabs, an instance also writes `Scale` when the
> source root's own scale is not 1). Section 5.2's "mirrored `-1` scales pass through" turned out wrong in the editor:
> s&box draws a negatively scaled model inside out. Mirrored placements now get a positive scale on a mirrored
> prefab variant (`_mirror_<axis>`), see `UnityPrefabExport.Unmirror` and the CHANGELOG.

Design and implementation guide for an optional **Scene import** toggle in Import Unity Package. When the
toggle is on, a `.unity` scene selected in the package tree is converted to an s&box `.scene` as the last
step of the run, after every model, material and prefab it needs has been generated. Nothing in this
document is guessed: every format fact below was measured on the two test packages in `Assets/` or read
from the s&box engine source (`Facepunch/sbox-public`) and the engine's own shipped scenes and templates.
Where something could not be verified headless it is marked **verify in editor**.

Today the only scene path is `Documentation/Unity-To-SBox-Automated/unity_scene_to_sbox.py`, which edits an
existing `.scene` by hand after an import (root instances, one camera, one sun). This design replaces it with
a converter inside `Editor/Core` that writes a complete scene from the Unity file, so the whole pipeline is
one click. The script stays as a reference for the camera and light maths it already got right.

---

## 1. What a Unity scene contains (measured)

`Assets/city--1.unitypackage` → `Assets/PolygonCity/Scenes/Demo.unity` (guid `35299a6a99d6dfe4bb709527ba95aab0`,
11 MB, 282 139 lines). Saved by a Unity 2022+ editor (it has a `SceneRoots` block), although the package is
from 2017.

| Block | `!u!` type | Count | What it is |
|---|---|---|---|
| `PrefabInstance` | 1001 | 5 326 | placed prefabs (modern grammar: `m_SourcePrefab`, `m_Modification.m_Modifications`) |
| `Transform` | 4 | 1 179 | 3 real transforms + 1 176 **stripped** transforms (an instance's child object used as a parent) |
| `GameObject` | 1 | 4 | `Main Camera`, `Directional Light`, `Cars` (plain objects) + 1 stripped |
| `Camera` | 20 | 1 | on Main Camera |
| `Light` | 108 | 1 | directional, on Directional Light |
| `AudioListener` | 81 | 1 | on Main Camera |
| `Behaviour` | 124 | 1 | FlareLayer on Main Camera (no equivalent) |
| `MonoBehaviour` | 114 | 1 | a script (`474bcb49…`, not in the package) |
| `MeshCollider` | 64 | 2 | added to a stripped object of an instance, pointing at a baked `.asset` mesh |
| `RenderSettings` | 104 | 1 | skybox, ambient, fog |
| `LightmapSettings` | 157 | 1 | references a lighting data asset; no lightmaps in the package |
| `NavMeshSettings` | 196 | 1 | no nav mesh data |
| `OcclusionCullingSettings` | 29 | 1 | nothing to convert |
| `SceneRoots` | 1660057539 | 1 | the ordered list of 4 329 root objects: 4 326 instances + the 3 plain objects |

Prefab instances, in detail:

| Fact | Count | Consequence for the converter |
|---|---|---|
| distinct source prefabs | 247, all present in the package | every instance resolves to a generated prefab |
| overrides per instance | position xyz, rotation xyzw and `m_RootOrder` on all; `m_Name` on 4 763 | the same override grammar `UnityPrefabExport.Instance()` already parses |
| `m_LocalScale.*` overrides | 199 instances; 24 of them mirrored (`-1, 1, 1`) | Scale override with a negative component is valid in s&box |
| `'m_Materials.Array.data[N]'` | 1 929 | material override on the instance's renderer; `Instance()` converts these when the renderer has one slot |
| transform overrides on more than one object of the instance | 10 | child-object overrides; `Instance()` handles them via `childTransforms` |
| `m_IsActive` override | 1 (`SM_Veh_Car_Medium_01`, value 1) | `Enabled` override |
| `m_TransformParent` set | 1 000 | 164 under the plain `Cars` object, **836 under a stripped transform of another instance** |
| `m_AddedGameObjects` non-empty | 227 instances, 838 added objects | 836 are those nested instances (their stripped transform), 2 are the MeshColliders |
| `m_RemovedComponents` / `m_AddedComponents` | 1 / 1 | the MeshCollider pair |
| `m_colliders.Array.*`, `m_colliderAsset`, `Params.maxConvexHulls` | 4 | overrides on a Unity collider script; not convertible |

Nesting is real and common: `SM_Prop_LightPole_Base_01` instances carry 179 child instances (pole tops),
`SM_Env_Ocean_Tile_01` 148, `SM_Bld_FireEscape_01` 97, `SM_Bld_OfficeSquare_Base_01` 73. A stripped
transform looks like this and is the only thing the scene stores for the parent object:

```yaml
--- !u!4 &96486303 stripped
Transform:
  m_CorrespondingSourceObject: {fileID: 480566, guid: 4fa42fc4cca532640a6592f05bb6414a, type: 3}
  m_PrefabInstance: {fileID: 96486302}
  m_PrefabAsset: {fileID: 0}
```

`m_CorrespondingSourceObject` is the transform's fileID **inside the source prefab**, `m_PrefabInstance`
the instance it belongs to. That is enough to find the s&box object the child must hang under (section 5.3).

The plain objects:

```
Main Camera       pos (48.052517, 17.116774, 43.708782)  rot (-0.052281402, 0.9464897, -0.1990719, -0.24858457)  tag MainCamera
Directional Light pos (0, 3, 0)                          rot (0.11729209, -0.8707034, 0.40601596, 0.251534)
Cars              identity, 164 instance children
```

Camera: `field of view: 53.1`, `m_FOVAxisMode: 0` (vertical), near `0.3`, far `1000`, perspective,
`m_BackGroundColor (0.19215687, 0.3019608, 0.4745098)`, `m_ClearFlags: 1` (skybox).
Light: `m_Type: 1` (directional), colour `(1, 0.95686275, 0.8392157)`, intensity `1.2`, `m_Shadows.m_Type: 2`
(soft), `m_UseColorTemperature: 0`, `m_Lightmapping: 4` (realtime).
RenderSettings: `m_Fog: 0`, `m_AmbientMode: 0` (skybox), `m_SkyboxMaterial` = Unity's built-in Default-Skybox
(`guid 0000000000000000f000000000000000`, fileID 10304), `m_Sun: {fileID: 0}`.

The other package, `building.unitypackage` → `Assets/Scenes/SampleScene.unity`, has **no** prefab instances:
3 plain objects (camera with AudioListener, light, a Global Volume with 3 MonoBehaviours). It is the
degenerate case the converter must still handle (camera + light only, nothing placed).

Unity grammar the converter must accept:

- Unity 2018.3+ scenes: `PrefabInstance` blocks with `m_SourcePrefab` and `stripped` placeholder blocks
  (`m_CorrespondingSourceObject`, `m_PrefabInstance`). Both test scenes use this.
- Unity ≤ 2018.2 scenes: `Prefab` blocks (`m_ParentPrefab`, `m_IsPrefabParent: 0`) and every instance object
  serialized in full with `m_PrefabParentObject`/`m_PrefabInternal`. The 2017 **prefabs** in the city package
  use this grammar (`UnityPrefabExport` parses them), so the field names are already handled; a legacy scene
  only differs in that the instance's objects are present as ordinary blocks and must be skipped in favour of
  the instance stub. Not exercised by either test package; keep it as a reported-unsupported case until a
  sample exists.
- Root order: `SceneRoots.m_Roots` (2022+), else `m_RootOrder` on root transforms and `propertyPath:
  m_RootOrder` on instances, else file order.

---

## 2. What an s&box scene is (from engine source)

`engine/Sandbox.Engine/Resources/Scene/SceneFile.cs`: a `.scene` is a `GameResource` JSON with

| Key | Value |
|---|---|
| `__guid` | the scene's Guid (`Id`) |
| `GameObjects` | `JsonObject[]`, each a GameObject exactly as a prefab's `RootObject` children are written |
| `SceneProperties` | `NetworkInterpolation`, `PhysicsMode`, `TimeScale`, `WantsSystemScene`, `Metadata` (`Title`, …), `NavMesh`, optional `GameObjectSystems` |
| `ResourceVersion` | `3` (current; `SceneFile.Version03.cs` adds `Flags`, `Children`, `Components` where missing) |
| `Title`, `Description` | legacy, `null`; the editor reads `SceneProperties.Metadata.Title` |
| `__references` | `[]` |
| `__version` | `3` |

Shipped example (`game/addons/menu/Assets/scenes/menu-main.scene`): `"SceneProperties": {"NetworkInterpolation":
true, "PhysicsMode": "Physics3D", "TimeScale": 1, "WantsSystemScene": false, "Metadata": {"Title":
"menu-main"}, "NavMesh": {...}}`. The project's own `Assets/scenes/manual-imported-example.scene` has the same
shape with `WantsSystemScene: true`. `UnityPrefabExport.SceneProperties()` already writes this object for
prefabs; reuse it and add `Metadata.Title`.

GameObject keys (`GameObject.Serialize.cs`, `JsonKeys`): `__guid`, `__version` (2), `Flags`, `Name`,
`Position`, `Rotation`, `Scale`, `Tags`, `Enabled`, `NetworkMode`, `NetworkFlags`, `NetworkOrphaned`,
`NetworkTransmit`, `OwnerTransfer`, `Components`, `Children`; prefab instances add `__Prefab`,
`__PrefabInstancePatch`, `__PrefabIdToInstanceId`. `UnityPrefabExport.GameObject()` writes exactly this set.

`GameObjectFlags.Static = 32768` (`GameObject.Flags.cs`): "This object never moves during gameplay. Applies
to children too." That is the target for Unity's `m_StaticEditorFlags`.

The prefab instance patch (`engine/Sandbox.Engine/Utility/Json/Json.Diff.cs`, class `Json.Patch`) has four
lists, all already emitted by the importer as empty arrays except `PropertyOverrides`:

```
PropertyOverrides: [{ Target: {Type, IdValue}, Property, Value }]
AddedObjects:      [{ Id: {Type, IdValue}, Parent: {Type, IdValue}, PreviousElement: null | {Type, IdValue},
                      ContainerProperty: "Children" | "Components", IsContainerArray: true, Data: {…} }]
RemovedObjects:    [{ Id: {Type, IdValue} }]
MovedObjects:      [{ Id, NewParent, NewContainerProperty, IsNewContainerArray, NewPreviousElement }]
```

`ApplyPatch` registers every `AddedObject` by `Id`, attaches it to `Parent` (an object of the instance, by its
**instance** GUID), appends it to the parent's `ContainerProperty` array, then `ReorderAddedObjects` places it
after `PreviousElement` (null = anywhere/first). `Data` is the full JSON of the object. This is the mechanism
for Unity's `m_AddedGameObjects` and for instances parented under another instance's object (section 5.3).

Component JSON, as the editor writes it (`game/core/templates/gameobject/*.prefab`, property names and
defaults confirmed in `Scene/Components/Light/*.cs`, `Camera/CameraComponent.cs`, `Render/SkyBox2D.cs`,
`Render/GradientFog.cs`, `Light/AmbientLight.cs`):

```json
{"__type":"Sandbox.DirectionalLight","FogMode":"Enabled","FogStrength":1,"LightColor":"0.91373,0.98039,1,1","Shadows":true,"SkyColor":"0,0,0,0"}
{"__type":"Sandbox.PointLight","Attenuation":1,"FogMode":"Enabled","FogStrength":1,"LightColor":"…","Radius":400,"Shadows":true}
{"__type":"Sandbox.SpotLight","Attenuation":1,"ConeInner":15,"ConeOuter":45,"FogMode":"Enabled","FogStrength":1,"LightColor":"…","Radius":500,"Shadows":true}
{"__type":"Sandbox.AmbientLight","Color":"…"}
{"__type":"Sandbox.CameraComponent","BackgroundColor":"0.33333,0.46275,0.52157,1","ClearFlags":"All","FieldOfView":60,"FovAxis":"Vertical","IsMainCamera":true,"Orthographic":false,"OrthographicHeight":1204,"Priority":1,"RenderExcludeTags":"","RenderTags":"","TargetEye":"None","Viewport":"0,0,1,1","ZFar":10000,"ZNear":10}
{"__type":"Sandbox.AudioListener","UseCameraDirection":true}
{"__type":"Sandbox.SkyBox2D","SkyIndirectLighting":true,"SkyMaterial":"materials/skybox/skybox_day_01.vmat","Tint":"1,1,1,1"}
{"__type":"Sandbox.GradientFog","Color":"…","EndDistance":512,"FalloffExponent":1,"Height":128,"StartDistance":64,"VerticalFalloffExponent":2}
{"__type":"Sandbox.EnvmapProbe","Texture":"textures/cubemaps/default2.vtex","Bounds":{"Mins":"-1000,-1000,-1000","Maxs":"1000,1000,1000"},"Projection":"Box","UpdateStrategy":"OnEnabled",…}
```

Every component also carries `__guid`, `__enabled`, `Flags: 0` and the six `OnComponent*: null` callbacks
when the editor saves a scene (see `menu-main.scene`); write those too, as `UnityPrefabExport` does.
Colours are linear floats `r,g,b,a` (the menu camera's background is `0.00658,0.02919,0.07442,1`).
`CameraComponent.ZNear` is `[Range(1, 1000)]`, `ZFar` `[Range(1, 100000)]`, `FieldOfView` `[Range(1, 179)]`.
`SkyBox2D.SkyMaterial` rejects a material whose shader name does not contain `sky`. `SpotLight.ConeInner/
ConeOuter` are half-angles in degrees `[Range(0, 90)]`. `Light.LightColor` is an HDR colour: intensity is
carried in its magnitude, there is no separate intensity property.

---

## 3. Where the step sits in the import

```
UnityArchive.Read            classify .unity as UnityAssetKind.Scene
ImportCatalog.Read           scene → prefabs it instances, models/materials it references (dependencies)
ImportPlan.CreatePlan        PlannedAsset.Scene = Kind == Scene && options.Vmdl && options.Scenes
UnityImport.PrepareFiles     textures, materials, models … prefabs (existing)
                             ▶ scenes: UnitySceneExport.Convert → <stage>/Imported/<unity path>.scene   (new, last)
ImportMergePlan              Keep/Replace review covers the .scene like any other output
ImportAssetPreparation       .scene needs no compile (ResourceOrder 0); the editor registers it on commit
```

The scene step consumes only the run's own outputs: `generatedModels` (model per FBX mesh), `prefabPaths`
and `convertedPrefabs` (the JSON of every prefab converted earlier in the same run) and `materialPaths`.
That is why it must run after the prefab loop, and why a scene pulls its prefabs into the plan as
dependencies: a scene selected alone still gets everything it places.

---

## 4. Plumbing changes, file by file

1. `Editor/Core/UnityArchive.cs`
   - `UnityAssetKind`: add `Scene`.
   - `Classify`: `".unity" => UnityAssetKind.Scene`. The `.unity` source is copied like other sources (s&box
     ignores the extension); the converted file is written beside it with `.scene`.
2. `Editor/Core/UnityImport.cs`
   - `ImportOptions`: add `bool Scenes`; `Auto( bool additionalTerrainMaterials = false, bool scenes = false )`.
   - `IsEnabled`: `UnityAssetKind.Scene => options.Models && options.Scenes`. With the toggle off a scene is
     not plannable, so **Select all** leaves it out and the tree shows it dimmed with an annotation
     (`UnityAssetTree.OnPaint`: add `" (scene import off)"` for `Kind == Scene` when `plan.Find` is null).
   - `PrepareFiles`: after the prefab loop (`// Prefabs go last …`), add the scene loop:

     ```csharp
     foreach ( var asset in plan.Assets.Where( a => a.Scene ).Select( a => a.Asset ).OrderBy( a => a.Path ) )
     {
         cancel.ThrowIfCancellationRequested();
         currentAsset = asset.Path;
         try
         {
             var scene = UnitySceneExport.Convert( asset, byGuid, ( model, mesh ) => generatedModels.GetValueOrDefault( (model.Guid, mesh) ),
                 guid => prefabPaths.TryGetValue( guid, out var path ) ? DocumentPath( prefix + path ) : null, convertedPrefabs,
                 guid => materialPaths.TryGetValue( guid, out var path ) && outputs.Contains( path + ".vmat" ) ? DocumentPath( prefix + path + ".vmat" ) : null );
             documents.Add( (Output( GeneratedPath( Path.ChangeExtension( asset.Path, ".scene" ) ) ), scene.Json) );
             conversions++;
             warnings.AddRange( scene.Warnings.Select( w => $"{asset.Path}: {w}" ) );
             information.AddRange( scene.Information.Select( m => $"{asset.Path}: {m}" ) );
         }
         catch ( InvalidDataException ex ) { Issue( asset.Path, "conversion-skipped", $"Scene conversion skipped. {ex.Message}" ); }
     }
     ```
   - Report: add a `SceneConversions` array (source, roots, instances, nested, added objects, skipped).
3. `Editor/Core/ImportPlan.cs`
   - `PlannedAsset`: `public bool Scene { get; init; }`; `OutputLabel`: `UnityAssetKind.Scene => Scene ? "Scene → SCENE" : "Scene source"`.
   - `ImportCatalog.Read`: for each `Scene` asset, `UnityMaterial.References( text )` → `Link( scene, prefab,
     "Scene prefab instance" )`, `Link( scene, model, "Scene mesh" )` (only when not `CollisionOnly`),
     `Link( scene, material, "Scene material override" )`. Also link the `RenderSettings.m_SkyboxMaterial`
     guid when it is a package material (section 5.6). The prefab link is what makes "select the scene only"
     import everything it needs, through the existing prefab → model → material chain.
   - `CreatePlan`: `Scene = a.Kind == UnityAssetKind.Scene && options.Vmdl && options.Scenes`; include scene
     issues in the plan's issue list like prefabs (`(a.Vmdl || a.Prefab || a.Scene) && options.Materials`).
4. `Editor/UnityPackageWindow.cs`
   - Field `readonly Checkbox importScenes;` created next to `additionalTerrain` in the **Advanced** panel:
     `"Also import Unity scenes (.unity → .scene)"`, tooltip: "Writes an s&box scene for each selected Unity
     scene after its prefabs, models and materials are generated. Lights, cameras, skybox and fog are
     approximated; scripts, terrain, lightmaps and post-processing are not converted." `StateChanged += _ =>
     RefreshSelection()`. Off by default.
   - `Options => ImportOptions.Auto( additionalTerrain.Value, importScenes.Value )`.
   - `RefreshSelection`: disable it while `Busy` with the other controls; add `· {plan.Assets.Count( a => a.Scene ):N0} SCENE` to the outputs line.
   - The `scope` label currently says "Unity prefab assembly are not reproduced"; prefabs are converted now,
     so reword it: "Custom shader appearance, scripts, terrain and lightmaps are not reproduced."
5. `Editor/UnityAssetTree.cs`: icon for `UnityAssetKind.Scene` (`"public"` or `"landscape"`).
6. `Editor/Core/UnityPrefabExport.cs`: refactor so the scene converter can reuse `Node()` and `Instance()`
   (section 5.1). Keep `Convert( prefab, … )` byte-for-byte compatible: the harness run must show every existing
   prefab unchanged.
7. New `Editor/Core/UnitySceneExport.cs`: the scene converter (section 5).
8. `Documentation/Unity-To-SBox-Automated/verify_output.py` already checks `.scene` files; add the generated
   scene path to the harness's verification step in the guide, and retire `unity_scene_to_sbox.py` from the
   pipeline diagram (keep the file as the reference for the light maths).

---

## 5. The converter

### 5.1 Reuse, not duplication

`UnityPrefabExport.Convert` holds everything a scene needs as local closures over one file's blocks:
`Blocks()`, `byId`, `transforms`, `components`, `BakedChains`, `SkinnedRenderers`, `Node( transform, isRoot,
inherited )` and `Instance( instanceBlock, prepend )`. Extract them into an internal `UnityObjectConverter`
class constructed from `(UnityAsset file, byGuid, modelFor, prefabPathFor, converted, materialFor, Result)`,
with `Node`/`Instance` as methods and `key` (the file guid, lowercased) as a field. `UnityPrefabExport.Convert`
then becomes: construct, find the root transform, `Node( root, isRoot: true )`, wrap in the prefab document.
The scene converter constructs the same class over the `.unity` text and calls `Node`/`Instance` once per
root. Differences the class must accept:

- **No position zeroing.** `Node( …, isRoot: true )` zeroes a prefab root's position because an instance
  replaces it. Scene roots keep Unity's full local transform: pass `isRoot: false`.
- **Many roots**, ordered as in section 1.
- **Stripped transform children that belong to another instance** (section 5.3) do not occur inside a
  prefab (a prefab's stripped children always point at the prefab's own nested instances), so `Node`'s child
  loop needs one more case.
- `BakedChains` runs over the scene's own plain objects only; a plain object with a `MeshFilter` from a
  package FBX converts exactly like a prefab object (`ModelRenderer` on `generatedModels`), colliders included.
  Neither test scene has one, but Unity users place meshes directly in scenes all the time.

### 5.2 Root prefab instances

For every `PrefabInstance` whose `m_TransformParent` is `{fileID: 0}`: `Instance( block, prepend: null )`,
which already produces the `__Prefab` stub with `Position`/`Rotation`/`Scale`/`Name`/`Enabled` overrides,
material overrides, child-object overrides and the full `__PrefabIdToInstanceId` map (every GUID of the
converted source prefab, from `PrefabGuids( convertedPrefabs[sourceGuid] )`). Requirements that already hold:

- the source prefab is in the run (`convertedPrefabs` has it): guaranteed by the catalog link in section 4.3;
  if the user deselected the prefab explicitly, `Instance()` warns "not in the package or was not converted;
  instance skipped" and the scene simply lacks that object.
- `Scale` override is written only when it differs from the source root's scale; mirrored `-1` scales pass
  through (`Join` keeps the sign).
- The 10 Demo instances that override a child object's transform go through `childTransforms` →
  `ChildOverride`, including the baked-chain correction.

Instances parented under a **plain** scene object (the 164 cars under `Cars`) need nothing new: `Node( Cars )`
walks `m_Children`, finds each stripped transform in `strippedInstance`, and emits the stub as a child, with
`prepend` = the parent's baked chain (null for `Cars`).

### 5.3 Instances nested under another instance's object, and added objects

836 of Demo's instances hang under a stripped transform whose `m_PrefabInstance` is a *different* instance.
Unity records the same fact a second time in the parent instance's `m_AddedGameObjects`
(`targetCorrespondingSourceObject` = the source transform, `addedObject` = the child's stripped transform).
s&box's equivalent is an `AddedObject` entry in the parent instance's `__PrefabInstancePatch`:

```json
{
  "Id":     { "Type": "GameObject", "IdValue": "<child instance guid>" },
  "Parent": { "Type": "GameObject", "IdValue": "<instance-side guid of the source object>" },
  "PreviousElement": null,
  "ContainerProperty": "Children",
  "IsContainerArray": true,
  "Data": { …the child's complete GameObject JSON, here a __Prefab stub… }
}
```

How to fill it:

1. Parent instance `P` (block `m_PrefabInstance` of the stripped transform), its source prefab `S`.
2. Source transform fileID `t` = `m_CorrespondingSourceObject.fileID`; its GameObject `g` = `m_GameObject` of
   block `t` in `S`'s YAML (read `S` like `Instance()` does: `sourceBlocks`).
3. Prefab-side GUID of that object = `StableGuid( $"{S.guid}:{g}" )` (how `UnityPrefabExport.Node` names it).
   Instance-side GUID = the value `Instance()` put in `P`'s `__PrefabIdToInstanceId` for that key:
   `StableGuid( $"{sceneKey}:{P.id}:{prefabGuid}" )`. If the key is absent (the source object was dropped,
   e.g. a bone object of a skinned character), fall back to the instance root and warn.
4. `Data` = the child converted as usual (`Instance( child, prepend )` or `Node( … )`), with its transform
   expressed in the parent object's local space. The Unity child transform **is** local to that object, so
   pass it through unchanged, except when the parent object's placement was baked into its model
   (`BakedObjects( sourceBlocks )[g] != null`), in which case `prepend` = that chain, exactly as nested
   instances inside prefabs are handled today.
5. Append the entry to `P`'s patch. `P` itself may be a root stub or an added object of a third instance:
   build stubs first, then attach additions in a second pass keyed by instance fileID, so order of
   appearance does not matter.

Added **components** (`m_AddedComponents`; Demo's two MeshColliders on object `256894277` of a
`SM_Bld_…` instance) use `ContainerProperty: "Components"`, `Id.Type: "Component"`. Those two point at a
baked `.asset` mesh (`b6790cc5…`), which the importer cannot convert, so they are reported and skipped,
the same as today's "MeshCollider without a converted mesh". Added `BoxCollider`/`SphereCollider`/
`CapsuleCollider` components can be emitted with the existing collider builders.

`m_RemovedComponents` → `RemovedObjects` with `Id = {Type:"Component", IdValue: instance-side guid of
StableGuid( $"{S.guid}:{componentId}" )}`; `m_RemovedGameObjects` likewise with `GameObject`. Demo has one
removed component; both lists are cheap to emit.

**Verify in editor.** `AddedObjects` is produced by the editor's own differ and consumed by `ApplyPatch`, but
no file in `sbox-public` ships a non-empty `AddedObjects`, so the exact tracked-object definitions (which keys
identify an object: `__guid` for GameObjects and components) were read from `Json.Diff.cs` rather than from a
sample. Test with one light pole (`SM_Prop_LightPole_Base_01` + its top) before trusting all 836. If the editor
rejects or mis-parents them, the fallback is **flattening**: emit the nested instance at the scene root with its
world transform (compose the parent instance's root transform, the source object's local transform inside
`S`, and the child's local transform; `Compose` already exists). Flattening renders identically and loses only
the parent link; make it a constant in the converter so the switch is one line.

### 5.4 Plain objects

`Node( transform, isRoot: false )` for the 3 Demo roots. Components:

| Unity | s&box | Notes |
|---|---|---|
| `Transform` | `Position`/`Rotation`/`Scale` via `Position()`/`Rotation()`/`Scale()` | metres × 39.37, axis swap, as for prefabs |
| `MeshRenderer`+`MeshFilter`, colliders | as in prefabs | |
| `Camera` (20) | `Sandbox.CameraComponent` | section 5.5 |
| `AudioListener` (81) | `Sandbox.AudioListener { UseCameraDirection: true }` | template default |
| `Light` (108) | `DirectionalLight` / `PointLight` / `SpotLight` | section 5.5; area lights (`m_Type` 3, 4) reported, skipped |
| `ReflectionProbe` (215) | `Sandbox.EnvmapProbe` | optional; `Bounds` from `m_BoxSize`/`m_BoxOffset` × 39.37, `Projection: "Box"`; neither test scene has one |
| `MonoBehaviour` (114), `Behaviour` (124), `FlareLayer` | INFO line, skipped | |
| `m_TagString: MainCamera` | `Tags: "maincamera"` | the editor's camera template uses this tag |
| `m_StaticEditorFlags != 0` | `Flags |= 32768` | see static note below |
| `m_IsActive: 0` | `Enabled: false` | already handled |

Static flags live on the GameObject (`m_StaticEditorFlags`, Demo's plain objects are all 0). For prefab
instances Unity keeps them on the **source prefab's** objects, so the right place to set `Static` is the
generated prefab's root (`UnityPrefabExport.Node`, `isRoot`), where every instance inherits it through
`HasFlagOrParent`. Add that to the prefab converter rather than emitting a `Flags` override per instance;
whether `Flags` is even patchable as a property override is unverified.

### 5.5 Camera and lights

Camera (`CameraComponent`; the python script's rules, already validated against Unity):

| Unity | s&box |
|---|---|
| `field of view`, `m_FOVAxisMode: 0` | `FieldOfView` as is, `FovAxis: "Vertical"` (Unity's default axis; do **not** convert to horizontal) |
| `m_FOVAxisMode: 1` | `FovAxis: "Horizontal"` |
| `near clip plane` | `ZNear = max( 1, near × 39.37 )` → Demo: 11.811 |
| `far clip plane` | `ZFar = far × 39.37` → Demo: 39370 |
| `orthographic: 1`, `orthographic size` | `Orthographic: true`, `OrthographicHeight = size × 2 × 39.37` |
| `m_BackGroundColor` | `BackgroundColor` = sRGB→linear per channel, alpha 1 → Demo: `0.03071,0.07421,0.1912,1` |
| `m_ClearFlags` | `ClearFlags: "All"` always (s&box draws the 2D sky behind everything) |
| first enabled camera (highest `m_Depth` on ties) | `IsMainCamera: true`, `Priority: 1`; others `IsMainCamera: false`, `Priority: 1` |
| `m_TargetEye`, viewport, HDR, MSAA | ignored (`TargetEye: "None"`, `Viewport: "0,0,1,1"`) |

Lights, colour (Unity in linear colour space, which every URP project is; measured in Unity 6000.3, see
guide §7.7):

```
LightColor = srgb_to_linear( m_Color ) × CCT( m_ColorTemperature if m_UseColorTemperature == 1 else none ) × m_Intensity, alpha 1
```

`CCT` is Unity's `Mathf.CorrelatedColorTemperatureToRGB`, **not** linearised; port the sampled table from
`unity_scene_to_sbox.py` verbatim. Demo's sun: `(1, 0.95686275, 0.8392157)` → linear `(1, 0.90466, 0.67249)`
× 1.2 → `LightColor "1.2,1.08559,0.80693,1"`. `Shadows = m_Shadows.m_Type != 0` (Demo: 2 → true).
Rotation converts with the ordinary `Rotation()`: both engines shine along local forward and the axis map
carries forward onto forward. Demo's sun → s&box rotation `(-0.40601596, 0.11729209, 0.8707034, 0.251534)`
and position `0,0,118.11` (the hand-made `manual-imported-example.scene` light sits at `0,0,118.110001`, the
same Unity `(0, 3, 0)` default; a free cross-check).

| Unity `m_Type` | s&box | Extra fields |
|---|---|---|
| 1 directional | `DirectionalLight` | `SkyColor: "0,0,0,0"` (ambient comes from the sky, section 5.6), `FogMode: "Enabled"`, `FogStrength: 1` |
| 2 point | `PointLight` | `Radius = m_Range × 39.37`, `Attenuation: 1` |
| 0 spot | `SpotLight` | `Radius = m_Range × 39.37`, `ConeOuter = m_SpotAngle / 2`, `ConeInner = m_InnerSpotAngle / 2` (Unity ≥ 2019; else `ConeOuter × 0.75`), `Attenuation: 1` |
| 3 rectangle, 4 disc | none | INFO: area lights are not converted |

Caveat to state in the report: Unity point and spot intensity is a brightness multiplier (candela or lumen
in Unity 6, `m_LightUnit`), s&box's is the colour magnitude with quadratic attenuation over `Radius`. The
formula above keeps relative brightness between lights; absolute match needs an eye check. Baked-only lights
(`m_Lightmapping: 2`) are converted anyway, because s&box has no lightmaps and the scene would otherwise be dark.

### 5.6 RenderSettings: sky, ambient, fog

Emit one `SkyBox` object (`Tags: "skybox"`, `SkyBox2D`) when `m_SkyboxMaterial` is set:

- Unity's built-in Default-Skybox (`guid 0000000000000000f000000000000000`, the Demo case) → `SkyMaterial:
  "materials/skybox/skybox_day_01.vmat"` (the editor template's default), `SkyIndirectLighting: true`.
- A package `.mat` with a `Skybox/*` shader: the material converter targets `shaders/complex.shader`, which
  `SkyBox2D.SkyMaterial` rejects (its setter requires a shader name containing `sky`). Use the default sky and
  report "Unity skybox material X is not convertible to an s&box sky; default sky used". Do not generate a
  `.vmat` for it.

Ambient (`m_AmbientMode`): `0` skybox → nothing extra, `SkyIndirectLighting` supplies it. `3` flat →
`AmbientLight { Color = linear( m_AmbientSkyColor ) × m_AmbientIntensity }`. `1` trilight → `AmbientLight` with
the equator colour (the average of the three is also defensible; pick one and say so in the INFO line).

Fog (`m_Fog: 1`) → one `GradientFog` object: `Color = linear( m_FogColor )`, `Height: 100000`,
`VerticalFalloffExponent: 1` (so height plays no part), `FalloffExponent: 1`, and distances in inches:

| `m_FogMode` | `StartDistance` | `EndDistance` |
|---|---|---|
| 1 linear | `m_LinearFogStart × 39.37` | `m_LinearFogEnd × 39.37` |
| 2 exponential, `f = e^(-d·k)` | 0 | `4.6 / k × 39.37` (99 % fog) |
| 3 exp², `f = e^(-(d·k)²)` | 0 | `2.15 / k × 39.37` |

**Verify in editor**: `GradientFog` is a distance-and-height gradient; the `Height` value that makes it behave
as plain distance fog is an assumption. Demo has `m_Fog: 0`, so this path is untested on the test data.

`LightmapSettings`, `NavMeshSettings`, `OcclusionCullingSettings`, `m_Sun`, halo/flare settings: INFO once,
nothing emitted. s&box's own `NavMesh` block in `SceneProperties` is written with the defaults from
`SceneProperties()`.

### 5.7 Identity, naming, determinism

- Scene `__guid` = `StableGuid( $"scene:{sceneGuid}" )`; plain object GUIDs `StableGuid( $"{sceneKey}:{fileId}" )`;
  instance GUIDs `StableGuid( $"{sceneKey}:{instanceId}" )` and nested maps `StableGuid( $"{sceneKey}:{instanceId}:{prefabGuid}" )`,
  all through `UnityPrefabExport.StableGuid` so a reimport rewrites the scene byte-for-byte and the Keep/Replace
  review shows "Identical" when nothing changed. (The python script used `uuid5`; the hand-made
  `manual-imported-example.scene` is not touched by any of this.)
- Output path: `Imported/<Unity folder>/<name>.scene`, folder case kept, file lowercase (`GeneratedPath`):
  `PolygonCity/Scenes/Demo.unity` → `imported/polygoncity/scenes/demo.scene`.
- `SceneProperties.Metadata.Title` = the Unity scene's name (`Demo`).
- Root order: `SceneRoots.m_Roots`; otherwise sort roots by `m_RootOrder` (plain transforms carry it as a
  field, instances as a `propertyPath: m_RootOrder` override); otherwise file order. Children keep Unity's
  `m_Children` order, as prefabs do.
- No `.scene_d` sidecar is written. The editor creates one (48 bytes for the manual scene) when it saves
  blob data; a scene loads without it.

---

## 6. Verification plan

Headless, with the harness (`Documentation/Unity-To-SBox-Automated.md` §5; the SDK comes from nix in this
environment: `nix build nixpkgs#dotnet-sdk_10`):

1. Build and run twice on `city--1.unitypackage` with scenes enabled (`ImportOptions.Auto( scenes: true )`;
   give the harness a `--scenes` flag). Diff the two runs: byte-identical. Diff against a run with scenes off:
   only `imported/polygoncity/scenes/demo.unity`, `demo.scene` and the report differ. Every prefab and model
   must be unchanged by the `UnityPrefabExport` refactor.
2. `verify_output.py runs/x/Assets runs/x/Assets/imported/polygoncity/scenes/demo.scene`: JSON parses, every
   `__Prefab` path and model path resolves.
3. Counts to assert from the Demo numbers in section 1: 4 329 roots (4 326 stubs + Cars + camera + light), 164
   stubs under Cars, 836 `AddedObjects` across 227 parent instances (or 836 extra roots when flattening),
   1 929 material overrides attempted, 199 Scale overrides, 1 Enabled override, 2 skipped added
   MeshColliders, 1 skipped MonoBehaviour, 1 FlareLayer, camera at `1720.8147,-1891.8276,673.8874` with
   `FieldOfView 53.1`, `ZNear 11.811`, `ZFar 39370`, sun `LightColor 1.2,1.08559,0.80693,1`.
4. `building.unitypackage` / `SampleScene.unity`: a scene with no instances converts to camera + listener +
   sun + default sky, no errors; the three MonoBehaviours become one INFO line.
5. Scene file size: 5 326 stubs at roughly 1.2 KB each is about 7 MB of JSON. Acceptable for the format
   (`menu-main.scene` is JSON too), but note it in the report and keep the writer streaming-friendly
   (`JsonObject` per root is fine; do not build one giant string by concatenation).

In the editor, in this order, each one isolating a single unknown:

1. Open `demo.scene`. The hierarchy shows the roots; the camera looks at the city from Unity's position.
2. One `SM_Prop_LightPole_Base_01` with its top: the top sits on the pole (AddedObjects accepted and
   parented correctly). If not, flip the flatten constant and reopen.
3. A mirrored instance (`Scale -1,1,1`) renders mirrored, not inside out.
4. The sun direction and brightness match a Unity screenshot; the sky is the default day sky.
5. Static flags: pick a building instance and check `IsStatic` is set through its prefab root.

---

## 7. Known limits, stated up front

- Scripts, UI canvases, particle systems, audio sources, animation, timelines, terrain, lightmaps, light
  probes, occlusion data, post-processing volumes: listed in the report, not converted. Audio sources could
  later map to `SoundPointComponent` and reflection probes to `EnvmapProbe` (template JSON in section 2).
- Legacy (≤ 2018.2) scene instance grammar: reported as unsupported until a sample package exists.
- Unity skybox materials are not convertible; the default s&box sky is used.
- Point/spot intensity is approximate; area lights are skipped.
- `AddedObjects` parenting and `GradientFog` behaviour are the two design points that could only be derived
  from engine source, not from a shipped example; both have a documented fallback.
- A scene selected while its prefabs were explicitly deselected imports with those instances missing, with
  one warning per instance. The catalog link makes this a deliberate act, not an accident.
