# Unity to s&box, automated

This guide covers converting a Unity scene into an s&box scene with UnityImporterPlus. Everything is produced by tools; nothing is hand-authored. It records what the process is, what each step checks, the conversion rules the tools apply, and every failure seen while building it, with the fix for each.

It was written while converting `Assets/building.unitypackage` (the SampleScene round office building from the Unity project `4-Unity/deleteme`) into `Assets/scenes/manual-imported-example.scene`. The numbers quoted under "Reference result" come from that run. Use them to check a repeat run.

The tools this guide uses live next to it, in `Documentation/Unity-To-SBox-Automated/`:

| File | What it does |
|---|---|
| `harness/` (`harness.csproj`, `Program.cs`, `channels.py`) | Headless driver for the importer. Compiles `Editor/Core/*.cs` unmodified and calls the same `UnityImport.Run` the editor window calls, with the same automatic output choices. |
| `verify_output.py` | Checks generated `.prefab`/`.scene` JSON parses, `.vmdl`/`.vmat` braces balance, and every resource path inside them resolves to a real file (case-insensitively, like s&box). |
| `unity_scene_to_sbox.py` | Retired from the pipeline: the importer now writes whole scenes itself (`Editor/Core/UnitySceneExport.cs`, harness `--scenes`). Kept as the reference for the camera and light maths, and for patching an existing hand-made scene. |
| `make_editor_check.py` | Makes an offline compile check for the importer's editor (UI) code, which needs the engine DLLs. |

## 1. What is automated and what is not

The importer (`Editor/Core`) converts a `.unitypackage` into s&box assets:

- textures (copied, with channel-split metallic/roughness/opacity maps generated where Unity packs them)
- materials (`.mat` to `.vmat`, including glass and normal maps)
- models (`.fbx` copied, `.vmdl` ModelDoc generated, with Unity's MeshColliders turned into hull or mesh physics)
- prefabs (`.prefab` Unity YAML to s&box `.prefab` JSON, with nested prefab instances, transforms, names, enabled state and per-instance material overrides)
- scenes (`.unity` to s&box `.scene`, with the `--scenes` flag or the window's **Also import Unity scenes** toggle): every root object in Unity's order, prefab instances as stubs of the generated prefabs with their overrides, objects and components added under or removed from an instance, plain objects with models and colliders, cameras, directional/point/spot lights, the default sky, ambient light and fog

The importer does not convert scripts, Shader Graphs, terrain data, FBX animation takes or animation controllers. It lists them and excludes them. Skinned FBX models (characters) are converted: skeleton and skin weights come in with the mesh, bodygroups switch between the skinned meshes, and prefabs get a `SkinnedModelRenderer`; they stand in their bind pose until an animation graph or sequence drives them.

`unity_scene_to_sbox.py` predates the scene converter. It patches an existing s&box scene with root prefab instances, one camera and one directional light, and is still the way to update a hand-made scene (such as `manual-imported-example.scene`) without replacing it.

Not converted by anything, by design or for lack of an s&box equivalent:

- URP Volume post-processing (bloom, vignette, motion blur, colour grading)
- Area lights, reflection probes, light probes and baked lighting (point and spot lights convert, with approximate brightness)
- Prefab variants (a Unity prefab derived from another prefab). These are reported as unsupported.
- Scripts, particle systems, audio sources, UI canvases and terrain on scene objects (listed per type in the report)
- Unity skybox materials (the default s&box sky is used) and the camera background colour, which has no visible effect behind a skybox anyway

## 2. Paths used in this guide

Shell variables, set once per terminal:

```bash
export IUP={cseliot}/1-Development/1-SBox/2-Projects/unityimporterplus    # the importer project
export UNITY={cseliot}/1-Development/4-Unity/deleteme                      # the Unity source project
export ENGINE={cseliot}/1-Development/1-SBox/1-Engine-Builds/Dev/sbox-public/game   # an s&box game folder with bin/managed/Sandbox.Engine.dll
export TOOLS=$IUP/Documentation/Unity-To-SBox-Automated
export WORK=$HOME/iup-work                                                     # any scratch folder OUTSIDE the project
```

Keep `WORK` outside the s&box project. The editor watches everything under `Assets/` and will try to compile anything written there, including trial runs.

On Windows, use `py -3` for `python3`, `set VAR=...` for `export`, and backslash paths. The harness itself picks `py -3` on Windows automatically for its texture helper.

## 3. Prerequisites

| Need | Version seen working | Check |
|---|---|---|
| .NET SDK | 10.0.112 (`/usr/bin/dotnet`); 10.0.400 (`~/.dotnet`) also present | `dotnet --version` |
| Python 3 | 3.12+ | `python3 --version` |
| Pillow | 12.1.1 | `python3 -c "import PIL; print(PIL.__version__)"`; install with `pip install pillow` |
| An s&box game folder | Dev build `1-Engine-Builds/Dev/sbox-public/game` | only needed for `make_editor_check.py` and the editor check in step 9 |
| Unity | 6000.3.23f1 | only needed to re-export the package or answer a Unity-behaviour question (section 12.4) |

Pillow is only used by `harness/channels.py`. That script stands in for `Editor/TextureChannels.cs`, which needs the engine's `Bitmap` class and so cannot run headless. It extracts one channel of a texture, scales it, optionally inverts it, and writes the result to all three colour channels with opaque alpha, the same as the engine code.

## 4. The pipeline at a glance

```
Unity project --export--> .unitypackage
                              |
                  (1) harness build  (once per importer change)
                              |
                  (2) dry run into $WORK, run it twice, diff        <- determinism
                              |
                  (3) verify_output.py on the dry run               <- references resolve
                              |
                  (4) harness run --scenes into $IUP/Assets         <- the real import, scenes included
                              |
                  (5) (optional) unity_scene_to_sbox.py to patch a hand-made .scene
                              |
                  (6) verify_output.py on project output + generated scenes
                              |
      (7) if Editor/*.cs changed: make_editor_check.py + dotnet build
                              |
                  (8) open the scene in the s&box editor            <- final visual check
```

Steps 2 to 7 run without the s&box editor, so they also work on a machine where the editor cannot run.

## 5. Step by step

### Step 0. Export the package from Unity

In Unity, select the scene and the prefabs it places, then use Assets > Export Package with "Include dependencies" ticked. Save it into `$IUP/Assets/` (this guide uses `$IUP/Assets/building.unitypackage`).

Two things to check before moving on:

1. The package must contain every prefab the scene places, plus their models, materials and textures. "Include dependencies" does this. If you skip it, step 2 reports unresolved references.
2. The scene inside the package is only as new as the export. If you edit the scene in Unity afterwards, either re-export or pass the `.unity` file on disk to step 5 (that is what this guide does). In the reference run, the packaged `SampleScene.unity` was an older copy with no prefab instances and the camera at Unity's default `(0, 1, -10)`. The scene on disk had the building and the camera at the origin. The scene tool detects the "no prefab instances" case and refuses to run on it (exit 1).

A package is a gzip tarball with one folder per asset GUID, each holding `pathname`, `asset` and `asset.meta`. `tar -tzf building.unitypackage | head` shows this. It is useful when checking what a package really contains.

### Step 1. Build the harness

```bash
mkdir -p $WORK && cp -r $TOOLS/harness $WORK/
cd $WORK
dotnet build harness/harness.csproj -c Release -p:IUP_ROOT=$IUP
```

Expect `Build succeeded.` The csproj compiles `$IUP/Editor/Core/*.cs` directly, so every importer change is picked up by rebuilding. It stops with a clear error if `IUP_ROOT` does not point at the project. Build the harness from a copy in `$WORK`, not in place under `Documentation/`: building in place leaves `bin/` and `obj/` folders in the project.

`Editor/Core` has no engine dependency (only `System.*` usings), which is why it builds standalone. Keep it that way. Anything that needs `Sandbox.*` or `Editor.*` belongs in `Editor/` outside `Core/`.

### Step 2. Dry run into scratch, twice

```bash
cd $WORK
mkdir -p runs/a/Assets runs/b/Assets
dotnet harness/bin/Release/net10.0/harness.dll --scenes $IUP/Assets/building.unitypackage runs/a/Assets runs/a.report.json
dotnet harness/bin/Release/net10.0/harness.dll --scenes $IUP/Assets/building.unitypackage runs/b/Assets runs/b.report.json
diff -r runs/a/Assets runs/b/Assets && echo DETERMINISTIC
```

`--scenes` converts the package's Unity scenes as well (the window's **Advanced → Also import Unity scenes** toggle); leave it off to get the pre-scene output. A run with and a run without it differ only in the `.unity` copy, the `.scene` beside it and the report.

What to read in the output:

- `archive assets: N` followed by every package entry with its kind (`Texture`, `Material`, `Model`, `Prefab`, `Script`, `Scene`, ...). Check that everything you expect to convert has the right kind.
- `assets=… converted=… files=… changed=…`: counts for this run.
- `UNRESOLVED` lines: missing dependencies or conversion failures. The harness exits 1 if there are any. Do not import into the project until this is 0.
- `WARN` lines: skipped pieces (for example a MeshCollider whose mesh was not converted, or a nested prefab missing from the package). Each one is something that will be missing in s&box.
- `INFO` lines: review notes that are always printed (material review, model review, unsupported file count). They are expected.
- The report JSON: the full machine-readable result, including every output path.

The two runs must be byte-identical. The importer derives every generated GUID from a stable hash (section 7.5), so any difference means something non-deterministic crept in. A likely cause is dictionary order, a timestamp or a random GUID. Fix that before going further: a non-deterministic importer rewrites every prefab on each import and breaks every scene reference to a nested object.

### Step 3. Verify the dry-run output

```bash
python3 $TOOLS/verify_output.py runs/a/Assets runs/a/Assets/Imported
```

Expect `checked N files, 0 problems` and exit 0. `MISSING` lines are references to files that do not exist under `Assets` at all. The check ignores case, like s&box, so a casing difference alone never shows up as `MISSING`. The usual cause is a dependency that was not in the package. `engine?` lines are references to engine content that is not in the project (`materials/default/…`, `materials/skybox/…`). Those are normal: they are s&box's built-in fallbacks.

### Step 4. Import into the project

Once the dry run is clean, run the same command against the project:

```bash
cd $WORK
dotnet harness/bin/Release/net10.0/harness.dll $IUP/Assets/building.unitypackage $IUP/Assets $WORK/project.report.json
```

This writes into `$IUP/Assets/Imported/` exactly what the dry run wrote into `runs/a/Assets/Imported/`. Check with:

```bash
cd $WORK/runs/a/Assets && for f in $(find Imported -type f); do cmp -s $f $IUP/Assets/$f || echo "DIFF $f"; done
```

(no output means identical). Generated files the importer owns are replaced. Files that exist and differ are handled by the importer's merge rules (see `README.md`). Pass the report path as the third argument: without it, the harness writes `import-report.json` into the current directory. Never let it land inside `Assets/`.

You can also import from inside the s&box editor (Editor menu, UnityImporterPlus window). It runs the same Core code, but:

- Under the Steam editor on Proton, the window used to fail to open with `Import destination cannot pass through a symbolic link: Z:\home`. Wine reports the `Z:\` and `Z:\home` drive mappings as reparse points. `UnityImport.CheckDirectory` now rejects a link only when it is inside the s&box project (at or below the folder holding the `.sbproj`), so this works; a regression brings the error back.
- In the native Linux editor, see section 10, "Assets written while the editor is running do not compile".

### Step 5. Scenes

With `--scenes`, step 4 already wrote `Assets/imported/scenes/samplescene.scene` (and `imported/polygoncity/scenes/demo.scene` for the city package): a complete scene with the camera, the sun, the default sky and every prefab instance, in Unity's root order. Open it in the editor as it is. The report's `SceneConversions` table gives the counts (roots, instances, nested instances written as `AddedObjects`, added components, removed objects, skipped). For the city's `Demo.unity` expect 4 329 roots, 5 326 instances, 836 nested, 0 skipped; the two added MeshColliders point at a baked Unity mesh asset and are reported, not converted.

A scene that places no `.prefab` assets converts the same way. Unity keeps an implicit prefab for every model asset, so an instance whose `m_SourcePrefab` is the FBX itself is rebuilt by the same wrapper builder (`UnityPrefabExport.ModelRoot`) and written beside the model as `<name>.fbx.prefab`; its overrides address Unity's fixed model-prefab ids (100000 name, 400000 transform, 2300000+2n renderers) and its `m_Materials` entries resolve through the same slot map a wrapper prefab uses. A plain `MeshRenderer` in a scene keeps its own assignment: each slot becomes an indexed `Materials` override on the generated `ModelRenderer`, and a slot with no converted material is left to the model and reported once per object. Zero prefab instances is a normal scene, not a warning.

`Documentation/Unity-To-SBox-Automated/fixtures/prefab-free-scene/` is the tiny tracked fixture for that path (one ASCII FBX and its meta, two materials, one scene with plain objects and a direct FBX instance, all hand-written). `fixtures/pack_fixture.py <folder> <out.unitypackage>` packs it reproducibly into `fixtures/prefab-free-scene.unitypackage`, which runs through the harness like any other package.

The rest of this step is only for patching a scene you made by hand (the reference `manual-imported-example.scene`) instead of using the generated one. Make a backup, then run the scene tool on the target s&box scene:

```bash
cp $IUP/Assets/scenes/manual-imported-example.scene $WORK/manual-imported-example.scene.bak
python3 $TOOLS/unity_scene_to_sbox.py \
    $IUP/Assets/building.unitypackage \
    $UNITY/Assets/Scenes/SampleScene.unity \
    $IUP/Assets \
    $IUP/Assets/scenes/manual-imported-example.scene \
    --replace SM_Bld_OfficeRound_Base_01=10d80ce6-de58-4b8d-9edb-5d13b496631f
```

Arguments, in order:

1. The package. Prefab GUIDs in the Unity scene resolve through it.
2. The Unity scene: either its path inside the package (`Assets/Scenes/SampleScene.unity`) or a `.unity` file on disk. A file on disk wins when both could apply. Use it whenever the Unity project is newer than the export.
3. The s&box `Assets` folder the importer wrote into. The tool reads the generated prefabs there to get their root GUIDs, and exits 1 if a prefab has not been generated yet.
4. The target `.scene`, edited in place.

`--replace "<Unity instance name>=<scene object __guid>"` swaps an existing scene object for the generated prefab and keeps its `__guid`. Use it the first time, to replace a hand-made instance: the reference run replaced the Manual prefab instance with it. Without `--replace`, an instance is matched by its `Name` override (which the tool itself writes), so later runs update in place. Any Unity instance with no match is appended as a new object (`added instance …`). So a first run without `--replace` leaves the old hand-made object in place next to the new one. `--allow-partial` writes the file even when some objects could not be converted. Without it, any `PROBLEM` line makes the tool exit 1 and leave the target file untouched.

What it prints, from the reference run:

```
scene read from disk: {cseliot}/1-Development/4-Unity/deleteme/Assets/Scenes/SampleScene.unity
replaced instance SM_Bld_OfficeRound_Base_01 -> imported/polygoncity/prefabs/sm_bld_officeround_base_01.prefab
camera Main Camera: fov 60 Vertical, near 11.811, far 39370
light Directional Light: LightColor 2,1.55993,1.23904,1, shadows True, rotation -0.109382,0.408218,0.23457,0.875426
```

The tool keeps the target file's line endings (the editor saves scenes CRLF) and writes 2-space-indented JSON like the editor does. Running it twice gives the same file: re-running after an importer change is safe.

What it converts:

- Root prefab instances: position, rotation, scale, name and active state, via `__PrefabInstancePatch` overrides on the generated prefab's root. Scene-level overrides of anything else (materials, child transforms, components) are listed as problems, not applied.
- The first Unity camera, onto the first object with a `CameraComponent`: transform, field of view and axis, near/far planes, orthographic size.
- The first Unity directional light, onto the first object with a `DirectionalLight`: transform, colour (including colour temperature and intensity), shadows on/off.

It does not create the camera or light objects. The target scene must already have them. An empty s&box scene from the editor's "New Scene" does.

### Step 6. Verify the project output and the scenes

```bash
python3 $TOOLS/verify_output.py $IUP/Assets $IUP/Assets/Imported $IUP/Assets/scenes/manual-imported-example.scene
```

`Assets/Imported` includes the generated `.scene` files, so every `__Prefab` path and model path inside them is checked too. Expect `0 problems`. The one expected `engine?` line per scene is its skybox material.

### Step 7. If you changed the importer's C#: compile checks

Core code is covered by step 1. Code in `Editor/` outside `Core/` (the window, the asset tree, `TextureChannels.cs`) needs the engine. The project's own `Editor/unityimporterplus.editor.csproj` does not build on its own, because the editor writes it with that machine's install paths (for example `S:/steamapps/common/sbox` under Proton). Generate a buildable copy:

```bash
python3 $TOOLS/make_editor_check.py $IUP $ENGINE $WORK/edcheck
dotnet build $WORK/edcheck/editor-check.csproj -c Release -p:RunAnalyzers=false -p:TreatWarningsAsErrors=false
```

Expect `0 Error(s)` and `Build succeeded.` (the reference run had 3 warnings, all pre-existing). Point `ENGINE` at the engine the editor you test in actually runs. The Dev build and the Steam build can expose different APIs.

A green compile says nothing about what the importer writes. For any change to generated text (`.vmat`, `.vmdl`, `.prefab`), also rerun steps 2 and 3 and diff against the previous output.

### Step 8. Check in the s&box editor

Open `scenes/manual-imported-example.scene` and check:

- The building sits where it did in Unity (seen from the camera, same framing as Unity's Game view).
- All ten pieces render with textures, the glass is see-through, and the two roof pieces use the second colour variant (Mat_02_A).
- Walking into the building collides.
- The console and `logs/sbox-dev.log` have no `ERROR recompiling` or `doesn't exist!` lines for `imported/…` paths.

Log locations: native Linux build `<engine>/game/logs/sbox-dev.log`; Steam install `~/.local/share/Steam/steamapps/common/sbox/logs/sbox-dev.log`.

## 6. Reference result (building.unitypackage)

Use these numbers to confirm a repeat run is behaving the same way.

| Check | Value |
|---|---|
| Package entries | 31 |
| Harness summary | `assets=15 converted=10 files=24 changed=24`, 0 unresolved, 0 warnings |
| Output | 3 models (`.fbx` + 4 `.vmdl`), 3 source `.mat` + 3 `.vmat`, 7 textures (2 generated by channel split), 3 `.prefab`, 1 URP icon |
| Two runs | byte-identical |
| `verify_output.py` | 0 problems |
| Scene objects | 10 building pieces, position and rotation error against Unity 0.000 (independent check, world transforms composed from both sides) |
| Materials | every piece's material matches Unity, including the two override targets (Roof_03 and Roof_04 use Mat_02_A) |
| Camera | FOV 60 vertical, near 11.811, far 39370, at the origin |
| Light | `LightColor 2,1.55993,1.23904,1`, shadows on, position `0,0,118.11` |
| Generated scene (`--scenes`) | `imported/scenes/samplescene.scene`: camera + listener, sun with the same `LightColor`, 2D Skybox; `SceneConversions` 3 roots, 0 instances; the packaged scene predates the building placement, so it holds no instances (the Unity project's scene on disk does) |

Generated files:

```
Imported/PolygonCity/Materials/  PolygonCity_Glass_Mat.mat  polygoncity_glass_mat.vmat
                                 PolygonCity_Mat_01_A.mat   polygoncity_texture_01_a.vmat
                                 PolygonCity_Mat_02_A.mat   polygoncity_texture_02_a.vmat
Imported/PolygonCity/Models/     SM_Bld_OfficeRound_02.fbx       sm_bld_officeround_02.vmdl
                                 SM_Bld_OfficeRound_Base_01.fbx  sm_bld_officeround_base_01.vmdl
                                                                 sm_bld_officeround_base_01_glass.vmdl
                                 SM_Bld_OfficeRound_Roof_01.fbx  sm_bld_officeround_roof_01.vmdl
Imported/PolygonCity/Prefabs/    sm_bld_officeround_base_01.prefab
Imported/PolygonCity/Prefabs/Buildings/  sm_bld_officeround_02.prefab  sm_bld_officeround_roof_01.prefab
Imported/PolygonCity/Textures/   (7 textures)
Imported/TutorialInfo/Icons/URP.png
```

`sm_bld_officeround_base_01.vmdl` and `…_glass.vmdl` come from one FBX. Its meshes use different Unity materials through the same FBX material slot, and ModelDoc can only remap a slot once per model, so the importer writes one model per mesh.

## 7. Conversion rules

These are the rules the tools apply. When something looks wrong in s&box, check it against this section first.

### 7.1 Units and axes

Unity is Y-up, left-handed, 1 unit = 1 metre. s&box is Z-up, 1 unit = 1 inch (39.37 per metre), forward = +X, left = +Y.

| Quantity | Unity `(x, y, z)` or `(x, y, z, w)` | s&box |
|---|---|---|
| Position | `(x, y, z)` | `(z, -x, y) * 39.37` |
| Rotation (quaternion) | `(x, y, z, w)` | `(-z, x, -y, w)` |
| Scale | `(x, y, z)` | `(z, x, y)` (no unit change) |
| Distance (clip planes, ortho size) | `d` | `d * 39.37` |

These are implemented once in `UnityPrefabExport.cs` (`Position`, `Rotation`, `Scale`) and once in `unity_scene_to_sbox.py` (`pos`, `rot`, `scl`). Keep the two in step. Unity forward `+Z` maps to s&box forward `+X`, and Unity up `+Y` to s&box up `+Z`. The rotation mapping follows from the same axis permutation plus the handedness flip.

Scene example: the Unity light at `(0, 3, 0)` lands at `0,0,118.11`.

### 7.2 Models

- The `.fbx` is copied unchanged. A `.vmdl` ModelDoc is generated next to it with lowercase file names. One exception: if the FBX contains an exact duplicate vertex array after a closing brace (a known exporter defect), the importer also writes a repaired `*_iup_repaired.fbx`, points the `.vmdl` at that copy instead, and prints a warning. The original is still copied unchanged.
- `import_scale` stays in Unity units, derived from the FBX `UnitScaleFactor` and Unity's `globalScale`/`useFileScale` import settings. The metre-to-inch conversion is a separate `ModelModifier_ScaleAndMirror` with `scale = 39.369999`. Do not also fold 39.37 into `import_scale`, or everything comes out 39.37 times too big.
- Material remaps map each FBX slot name to the generated `.vmat`, using Unity's per-slot assignments (source GUID, renderer identity and slot index).
- Collision from a MeshCollider on the mesh: when every prefab collider using the mesh is convex (`m_Convex: 1`), the model gets `PhysicsHullFromRender` with `hull_mode = "HullPerMesh"` and `maxHullVertices = 32`, one convex hull like Unity's. If any collider using it is non-convex, the model gets `PhysicsMeshFromRender`, the exact triangles, which is what Unity collides with. s&box mesh collision only works on static bodies; imported props are static, so that matches. An earlier version wrapped non-convex meshes in per-piece hulls, which produced visibly wrong collision (a road patch was the reported case).
- Known gap: Unity MeshColliders that use a separate baked collision mesh (a `.asset` mesh, not the rendered FBX mesh) cannot be converted. Those become a hull of the rendered mesh. The reference building's base had two such colliders, which became one hull per mesh.
- Skinned FBX (meshes with skin deformers, detected in binary and ASCII FBX): one `.vmdl` for the whole file, never split per mesh. `RenderMeshFile` imports the skeleton and skin weights with the meshes; a `BodyGroupList` follows the `MaterialGroupList`. Each mesh gets its own `RenderMeshFile` named after it and filtered to that node, the Citizen's layout: a bodygroup choice refers to the `RenderMeshFile` name, and one unnamed node importing every mesh compiles to a single `unnamed_1` mesh. The nodes point at a derived copy, `*_iup_skinned.fbx`, in which each Geometry object is named after its mesh node and bones sharing a name get their side appended (`Finger_01_L`/`_R`); the rewrite changes only those strings and recomputes the binary record offsets and footer (`UnityFbxRewrite`), and the original FBX is still copied. When every prefab in the package shows at most one of the file's skinned meshes, one bodygroup named after the FBX offers each mesh plus `none` (the Citizen's on/off idiom applied as an outfit switch); otherwise each skinned mesh gets its own on/off group. A take whose curves never change is not an animation; real animation and blend shapes are reported as `INFO` lines for ModelDoc follow-up. The PolygonCity `Characters.fbx` (header in metres, 0.01 baked onto the Root bone and the mesh nodes) gets `import_scale` 1.0 and `FixedScaleCharacters/Characters.fbx` (header in centimetres) gets 0.01; whether ModelDoc honours the Root bone's 0.01 for the first is the check left for the editor.

### 7.3 Materials

The importer maps Unity Standard/URP/HDRP properties onto `shaders/complex.shader` by default. Rules that were added or confirmed for this conversion:

- Base colour texture to `TextureColor`, tint from `_BaseColor`/`_Color`. The alpha comes from Unity's real value: 1.0 for opaque. The material editor's default of `0.000000` would make a translucent material invisible.
- Normal map to `TextureNormal`, plus `g_flNormalMapScaleFactor` from Unity `_BumpScale` (default 1) and `g_flLightRangeForSelfShadowNormals "0.707"`, both matching what the material editor writes. Without a normal map, the default `materials/default/default_normal.tga` is written.
- Packed metallic/smoothness: channels are split into separate generated textures (`*_iup_metallic.png`, `*_iup_roughness.png`). Roughness is the inverted smoothness channel.
- Glass: a translucent Unity material with no colour texture becomes `shaders/glass.shader`. Translucent means `_Surface: 1` (URP), legacy `_Mode` 2 or 3 (Fade or Transparent), or a Unity shader that declares `"RenderType"="Transparent"` (`UnityMaterial.cs`, `Translucent`). It gets `F_GLASS_QUALITY 1`, `F_ADDITIVE_BLEND 1`, `g_flRefractionStrength "1.005"`, `TextureColor` from `_BaseColor` (alpha included), `TextureTranslucency` from the `_BaseColor` alpha, roughness `1 - _Smoothness`, and metalness from `_Metallic`. This matches the hand-made reference `glass.vmat`. It applies to every package: any transparent, untextured Unity material is treated as glass.
- A generated `.vmat` has no trailing newline, like the material editor's own output.

### 7.4 Prefabs

Each Unity `.prefab` becomes an s&box `.prefab` at the same relative path, with the file name lowercased. The Unity YAML is never copied into the project.

- Hierarchy: each Unity GameObject becomes an s&box GameObject with the converted local transform, name and enabled state (`m_IsActive`).
- FBX sub-parts: every mesh node of an FBX gets its own `.vmdl`, which imports the whole FBX filtered to that node, so the compiled model already contains the node's placement. A Unity child object whose mesh comes from the same FBX as its parent's (car doors, wheels, glass, steering wheels) therefore gets position 0, rotation identity and scale 1; otherwise s&box applies the placement twice. The removed Unity placement (the chain of locals from below the same-FBX parent down to the object) moves into everything that does not follow the model: primitive collider centres, sizes and radii, non-mesh children, and nested prefab instances under it. A collider centre is the object's placement applied to the collider's own `m_Center`, not the object position alone. A box on a rotated sub-part keeps its exact shape only for 90-degree turns. At any other angle it becomes the rotated box's bounds, with an `INFO` line, because s&box's BoxCollider has no rotation. If the top mesh of a same-FBX group is not the prefab root, the import cannot tell whether that object is offset twice, so it logs an `INFO` line naming the object.
- `MeshRenderer` + `MeshFilter` become a `ModelRenderer` on the generated `.vmdl` for that mesh. A `MeshCollider` becomes a `ModelCollider` on the same model. `m_Enabled` carries over to each component.
- `SkinnedMeshRenderer`s of one skeleton become a single `Sandbox.SkinnedModelRenderer` on the parent of the skeleton's root bone (where the FBX root sits), pointing at the skinned `.vmdl`, with `BodyGroups` set to the meshes Unity had active: one bit per bodygroup choice, groups in order, the way the compiled model numbers its mesh groups (the Citizen's five two-choice groups with their first choices picked is 341 in the engine's `avatar.scene`). Unity renders a skinned mesh through its bone objects, so the skinned object's own transform is not applied. The objects that only held a skinned mesh and the pure bone objects are dropped; a bone object with other components or children is kept as a plain object and reported, since it will not follow the skeleton. `Animator` components are listed as `INFO` lines.
- `BoxCollider`, `SphereCollider` and `CapsuleCollider` become `Sandbox.BoxCollider`, `SphereCollider` and `CapsuleCollider` on the same object. Centre maps like a position, box size like a scale times 39.37, radius times 39.37; a capsule's axis (`m_Direction`) and height become s&box `Start`/`End` points `height/2 - radius` either side of the centre. `m_IsTrigger` becomes `IsTrigger`; colliders are `Static`. `TerrainCollider`, `WheelCollider` and `CharacterController` are not converted and are listed as `INFO` lines.
- Nested prefab instances become s&box prefab instances (`__Prefab`, `__PrefabInstancePatch`, `__PrefabIdToInstanceId`), pointing at the generated nested prefab. The instance's root transform, name and enabled state become property overrides.
- Instance overrides carried over: material overrides (`m_Materials.Array.data[N]` on a renderer becomes a `Materials` override on the matching `ModelRenderer`), `m_Enabled` on components, and position/rotation/scale/name/active on objects below the nested root. The reference package only exercised the first two.
- Prefab variants (a prefab with no root transform of its own) are skipped with "prefab variants are not supported".
- A nested prefab that is missing from the package, unreadable or a variant becomes a warning, and that instance is skipped. The rest of the prefab still converts.

The file's own root keeps Unity's root rotation and scale (a Unity prefab can bake a scale into its root, for example `SM_Env_Road_Patch_01` at `2.198, 0.290, 3.477`). Its position is written as zero: s&box adds a prefab root's position to wherever the prefab is placed (`GameObject.Clone`), while Unity replaces it, so baking it in would shift every placement. An instance, in a scene or a parent prefab, always writes its own Position and Rotation, and writes Scale whenever it differs from the prefab root's scale. Instance overrides replace the prefab's values; they do not multiply.

A placement with a negative scale (Unity's way of mirroring a prop) never reaches a file as such: s&box draws it inside out. Two negative axes are a half turn, folded into the rotation; one (or three) becomes a positive scale on a variant of the prefab mirrored along that Unity axis, `<prefab>_mirror_<x|y|z>.prefab`, whose models are `<model>_mirror_<axis>.vmdl` (ModelDoc's `ScaleAndMirror` modifier with `mirror_y` for Unity x, `mirror_z` for Unity y, `mirror_x` for Unity z, following the axis mapping in 7.1). Inside the variant every position, rotation, collider centre and nested instance is seen through the mirror (`UnityPrefabExport.Unmirror`). The Demo scene places 32 such instances on 6 prefabs; the report counts the variants generated.

### 7.5 Stable GUIDs

Every GameObject and component GUID in a generated prefab is derived, not random:

```
MD5("unityimporterplus:" + key), with the RFC 4122 version 3 nibble and variant bits set
key = <prefab GUID>:<Unity fileID>   (instances and their children extend it)
```

This keeps GUIDs identical across re-imports, so scene overrides that target a prefab's inner objects keep pointing at the right thing. One trap when changing this code: `new Guid(byte[])` stores its third field little-endian, so the version nibble is the high nibble of byte 7, not byte 6. Getting that wrong still produces valid-looking GUIDs whose version digit is wrong.

`unity_scene_to_sbox.py` uses the same idea (`uuid5` over scene and fileID) for instances it appends.

### 7.6 Camera

| Unity | s&box `CameraComponent` |
|---|---|
| `field of view` with `m_FOVAxisMode: 0` | `FieldOfView` unchanged, `FovAxis: Vertical` |
| `m_FOVAxisMode: 1` | `FovAxis: Horizontal` |
| `near clip plane` / `far clip plane` | `ZNear` / `ZFar` times 39.37 (near clamped to at least 1) |
| `orthographic: 1`, `orthographic size` | `Orthographic: true`, `OrthographicHeight = size * 2 * 39.37` |

Unity's field of view is vertical by default. Do not convert it to horizontal and keep `FovAxis` vertical; that makes the view noticeably wider than in Unity.

### 7.7 Directional light

s&box lights shine along their forward axis. So do Unity's (+Z), and the axis mapping in 7.1 carries forward onto forward, so the rotation converts with no extra flip.

Colour, for a Unity project in linear colour space (every URP project):

```
LightColor = srgb_to_linear(m_Color) * CCT(m_ColorTemperature) * m_Intensity     (CCT only if m_UseColorTemperature: 1)
```

The colour-temperature factor is Unity's `Mathf.CorrelatedColorTemperatureToRGB`, used as-is, not linearised. This was measured in Unity 6000.3 with `LightmapperUtils.Extract` on a light with known inputs: white at 5000 K gives `(1, 0.77997, 0.61952)`, identical to the raw function. The script stores that function sampled every 500 K (fewer steps above 10000 K) and interpolates. The table is within 0.009 of Unity everywhere from 1000 K to 40000 K.

A widely copied approximation (the Tanner Helland curve fit) is not what Unity uses. With the colour linearised as well, it gave `1.89547,1.44731,1.12895` for the reference light instead of the correct `2,1.55993,1.23904`, about 7% too dark and slightly too warm. Anyone porting this rule should check against Unity itself (section 12.4), not an internet formula.

### 7.8 File and folder names

- Generated file names are lowercase. Paths written inside generated documents are lowercase too, because s&box resource paths are compared lowercase.
- New folders are created lowercase (`imported/polygoncity/prefabs/buildings/`). The native resource compiler asks for sources by their lowercase path. On Linux, the editor resolves that against a cached listing of each folder and does not see differently cased folders created after it started, so Unity-cased folders made every newly imported resource fail to compile until an editor restart. A folder or file that already exists is reused whatever its case (`UnityImport.DiskPath`), so older imports under `Imported/PolygonCity/` keep updating in place and nothing is duplicated.
- The importer refuses to write through a symbolic link inside the s&box project. Links above the project folder (a symlinked home, or Wine's drive mappings) are allowed.

## 8. Changing the importer: rules that prevent the failures seen so far

1. Change code in `Editor/Core` only when it can stay engine-free. Rebuild the harness (step 1) after every change and keep it building.
2. After every change to generated output, rerun steps 2 and 3 and diff the output against the previous run's output. Review every changed line, not just the new files.
3. Keep output deterministic. No `Guid.NewGuid()`, no timestamps, no reliance on `Dictionary` or file-system order for anything that reaches a file.
4. Parse Unity YAML defensively. Real prefabs contain duplicate `m_GameObject` fileIDs, instance modifications without an `objectReference:` line, quoted scalars (`'SM_Bld: A'`), `stripped` blocks and very large files. Each of these crashed or silently dropped data in an earlier version. Unreadable input must become a reported issue, never an exception that aborts the whole package.
5. Watch regex escapes when editing through tools. One edit turned `\t` inside `[ \t]` character classes into literal tab characters. It still compiled, and matched differently. `grep -nP '\[[^]]*\t' Editor/Core/*.cs` finds any that sneak in.
6. Keep each file's existing line endings. They are mixed in this repo: most of `Editor/Core/*.cs` is LF, `ImportPlan.cs` and several `Editor/*.cs` files are CRLF (one, `ImportAssetPreparation.cs`, is mixed), and `core.autocrlf=input` normalises commits to LF. An edit that changes a file's line endings produces a whole-file diff. Check with `git ls-files --eol Editor/` (the `w/` column) before and after editing.
7. Unity is the reference, not `Assets/Manual/`. The hand-made Manual assets were a starting target, and they are wrong in places. The Manual base prefab used Mat_01_A on pieces where Unity renders Mat_02_A, and the uppercase `Manual/PolygonCity/Prefabs/*.prefab` files are 2-byte `{}` placeholders (from commit `49d16c1`). When the importer and Manual disagree, check Unity's YAML.
8. After any change, have someone other than the author review it, and check each claim of theirs against the source before acting on it.

## 9. Verifying against Unity without the s&box editor

Useful when the editor is unavailable or to double-check it:

- Transforms: compose world transforms on both sides and compare. Unity: walk `m_Father` from each Transform, applying `m_LocalPosition`/`m_LocalRotation`/`m_LocalScale`, including `PrefabInstance` modifications. s&box: walk the generated prefab tree plus the scene instance's overrides, then map the Unity result through 7.1. The reference run matched to 0.000 on all ten pieces.
- Materials: for each renderer, resolve Unity's `m_Materials` (after instance overrides) to material GUIDs, and compare with the `ModelRenderer` model's remaps plus any `Materials` override.
- Anything about how Unity itself behaves: ask Unity (next section).

### 9.1 Asking Unity headlessly

Unity can run a static editor method with no window:

```bash
U={cseliot}/Unity/Hub/Editor/6000.3.23f1/Editor/Unity
$U -batchmode -nographics -createProject $WORK/unityprobe -quit -logFile $WORK/create.log     # once, about a minute
# put a static class with a public static void Run() into $WORK/unityprobe/Assets/Editor/Probe.cs, then:
$U -batchmode -nographics -projectPath $WORK/unityprobe -executeMethod Probe.Run -quit -logFile $WORK/probe.log
grep 'error CS' $WORK/probe.log     # compile errors land here, not on stdout
```

Pass absolute paths to `-projectPath` and `-logFile`. Relative ones such as `../probe.log` fail with ".. is not a valid directory name". `LightType` is ambiguous once `UnityEngine.Experimental.GlobalIllumination` is imported, so write `UnityEngine.LightType`. This is how the light-colour rule in 7.7 was measured.

## 10. Troubleshooting

| Symptom | Cause | Fix |
|---|---|---|
| Harness: `Set IUP_ROOT to the unityimporterplus project folder` | `-p:IUP_ROOT=` missing or wrong | pass the project folder that contains `Editor/Core/UnityImport.cs` |
| Harness exits 1 with `UNRESOLVED` | a dependency is not in the package, or a file failed to convert | re-export with "Include dependencies"; read the issue message; never import with unresolved issues |
| `channel extraction failed` | Pillow missing, or `python3`/`py` not on PATH | `pip install pillow` |
| Two dry runs differ | non-deterministic code in the importer | find it and fix it before importing (rule 8.3) |
| `verify_output.py` reports `MISSING` | a path written into a document names a file that does not exist under `Assets` (compared case-insensitively) | usually a dependency that was not in the package: re-export with "Include dependencies" |
| Scene tool: `the Unity scene has no prefab instances` | the package holds an older copy of the scene | pass the `.unity` file on disk as argument 2, or re-export |
| Scene tool: `generated prefab … missing; run the importer first` | step 4 not done, or done into a different `Assets` folder | run step 4 against the same `Assets` folder |
| Scene tool: `scene-level overrides not applied: …` | the Unity scene changes something inside a prefab instance beyond its root transform, name or active state | apply those in s&box by hand or make them part of the prefab in Unity; `--allow-partial` writes the rest |
| Scene tool: `target scene has no CameraComponent/DirectionalLight object` | the target scene lacks them | start from a scene that has them (the editor's default new scene does) |
| Light too dark or too orange compared with Unity | light colour computed with a generic colour-temperature formula, or the CCT factor linearised | use the rule in 7.7 |
| View wider than in Unity | Unity's vertical FOV was treated as horizontal | 7.6 |
| Models 39.37 times too big, or tiny | inch conversion applied twice, or not at all | 7.2: `import_scale` in Unity units, 39.37 only in the ScaleAndMirror modifier |
| Folders `PolygonCity/` and `polygoncity/` side by side | path-casing regression: something built an output path without `UnityImport.DiskPath` | 7.8 |
| `Temporary import folder … being used by another process` at the end of a Linux run | Linux reports a file lock held by the same process as `EAGAIN` (HResult 11), not the Windows sharing-violation codes | `ImportWorkspace.Sweep` must skip HResult 32, 33 and 11; fixed, a regression if it reappears |
| Editor window fails to open under Proton: `Import destination cannot pass through a symbolic link: Z:\home` | Wine exposes drive mappings as reparse points | fixed (7.8); the check must only reject links inside the project |
| Native Linux editor: after an import, `logs/sbox-dev.log` fills with `Failed to save …meta (Could not find a part of the path …)` then `ResourceCompilerSystem File "…/assets/imported/…" doesn't exist!` and `ERROR recompiling` | the compiler is handed the lowercase path; the engine's case-resolving cache (`CaseInsensitivePhysicalFileSystem`) does not re-read a folder's listing when a directory lookup misses, so Unity-cased folders created after editor start are not found | fixed in the importer by creating new folders lowercase (7.8). Folders from an older import, created after the editor started, still need one editor restart |
| Import report lists hundreds of `the resource compiler reported a failure` errors | usually the row above | the completion now groups identical errors into one line; the report JSON keeps the full list |
| Road/terrain pieces collide as bumpy hulls instead of their surface | non-convex MeshCollider converted to hulls (old behaviour) | 7.2: now `PhysicsMeshFromRender` |
| Prefab has no collision though Unity had a BoxCollider/SphereCollider | primitive colliders were not converted (old behaviour) | 7.4 |
| Prefab the wrong size compared with Unity | the Unity prefab root has a scale, and it was dropped (old behaviour) | 7.4 |
| Car doors, wheels, glass or steering wheel sit twice as far out as they should, or tilted twice | the sub-part's FBX placement was applied by both its model and its prefab object (old behaviour) | 7.4, FBX sub-parts |
| `ERROR recompiling: 'manual/polygoncity/prefabs/buildings/…'` | those Manual files are `{}` placeholders (8.7) | not an importer problem; ignore or delete them |
| `sbox-cli` does not answer | `sbox-cli --list` shows no instance, or `sbox-cli --connect /abs/path/Project.sbproj --call editor_status` exits 1 (could not connect); the old editor HTTP MCP on `127.0.0.1:7269` that it replaced is gone | none of the steps in this guide need the editor bridge |
| Running the engine's `resourcecompiler` by hand on a generated file writes stray `*.generated.vtex_c` files and cannot resolve project paths | it has no project mount context outside the editor | do not use it to verify; delete what it wrote |

## 11. Files touched by a full run

- `Assets/Imported/**`: all generated, safe to delete and regenerate with step 4 (the `_c` compiled files and `.meta` files are the editor's and are regenerated too).
- Generated `.scene` files under `Assets/Imported/**`, regenerated like everything else there.
- A hand-made target `.scene`: edited in place only by the optional scene tool in step 5. Keep the backup until the editor check passes.
- Nothing else in the project. The harness, reports and trial runs stay in `$WORK`.

`*_c` and `*.generated.*` are ignored by the repo's `.gitignore`. The generated source assets under `Assets/Imported/` are not, so decide before committing whether they belong in the repository.
