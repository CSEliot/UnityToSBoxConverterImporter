# Import Unity Package

**Release candidate: 0.1.0-beta.1 · MIT license**

An s&box **library** with an editor-only Unity asset importer. Add this library to a project, then choose **Import Unity Package → Import .unitypackage…** from the editor menu.

## Import flow

1. Browse to a `.unitypackage` file. The browser starts in `%APPDATA%\Unity` when that folder exists; otherwise it uses the file dialog's normal starting location.
2. Check individual files or folders in the tree. **Select all** and **Clear** change the selection directly. **Expand all** and **Collapse all** control folder visibility without changing selections. The **+** and **−** icon buttons expand the next closed folder layer or collapse the deepest open layer. Folder checkboxes show partial selections. Space toggles the focused row; folder arrows expand it. Unsupported files are disabled.
3. Review output labels and dependency badges. A link icon means a required dependency, included even without an explicit selection. Select its row to see **Required by** paths and whether a binding was inferred. Remove the selected parent assets to release a dependency; explicitly selected assets remain included independently. The summary counts explicit selections and additional dependencies separately.
4. Conversion is automatic: visible models generate VMDLs, ordinary Unity materials generate VMATs, and `.terrainlayer` sources generate TMATs. Skinned FBX models are labelled **Skinned model → VMDL** and generate one model with bodygroups for their skinned meshes. Models referenced only by prefab MeshColliders are labelled **Collision source (FBX)** and retain their source without a visible VMDL. **Advanced → Also generate terrain materials** adds TMAT output without removing model VMATs. Terrain materials referenced by models also retain VMAT output. Original source assets are kept.
5. Click **Review import**. The importer converts the selection, then compares all resulting files with the destination. Identical content is reused without rewriting. Differing files default to **Keep existing**; choose **Replace** individually or use the bulk controls when conflicts exist. There is no package ownership, GUID history or local-edit classification.
6. Continue to write the chosen files and prepare resources. Keeping older files can mix existing content with the incoming package. Replacements are permanent; the importer keeps no backups and performs no rollback.
7. A short completion alert shows separate warning/error counts. The window retains **Details**, optional **Export report**, **Reimport** on the left and blue **Done** on the right. Details stay in memory; a report is saved only when you explicitly export it.

Paths preserve the original Unity hierarchy beneath `Assets/Imported`. For example, `Assets/BK/Common/Textures/Grass.png` becomes `Assets/Imported/BK/Common/Textures/Grass.png`. Converted model/material resources sit beside their original sources. Derived texture channels sit beside their source texture (for example, `Mask_iup_roughness.png`); repaired meshes and fallback materials sit beside their model (`Rock_iup_repaired.fbx`, `Rock_iup_unassigned.vmat`). No package-name or random-ID destination folder is added.

Generated names use `_1`, `_2`, `_3` only when the plain name is occupied by different content or a package source. Identical generated content reuses its existing name, including on subset reimports. Different variants keep separate files so other materials can continue using the older version. Updated VMAT/VMDL references participate in the usual Keep/Replace review. Existing `_unity_generated` folders are left intact for older imports; new derived files do not use that folder.

A green stage label above the progress bar identifies scanning, dependency lookup, asset/texture preparation, filename selection, conflict checking, confirmation, writing, resource preparation and cleanup. Preparation before confirmation only writes temporary files.

## Temporary work and cleanup

Extraction and conversion use `Assets/Imported/.iup-temp/<unique-run-id>/`. Temporary payloads have the `.iup` extension so they are not registered as ordinary texture/model/material resources. Complete files are renamed into their final destinations after review.

Temporary work is removed after success, cancellation or failure, including extracted package data while the completed window remains open. **Reimport** rereads the package. Windows file locks are retried; a cleanup warning identifies anything still locked. Opening the importer or starting another run removes abandoned run folders, while exclusive leases protect active imports. The empty `.iup-temp` parent is removed too. A crash or persistent file lock can delay cleanup until a later run.

No `.importunitypackage` folder, persistent cache, automatic reports, indexes, backups or recovery journals are created. Generated channel images, repaired meshes and fallback materials are required assets and remain in the project.

**Cancel** stops further writes. Files already written remain; the importer does not undo replacements. A failed write can similarly leave a partial import. Empty output folders created by the current import are removed; pre-existing folders and completed assets are kept. The next import compares current content again.

Resources are prepared in texture/material/model order. The importer waits for dependency records to settle after compilation and can perform one full rebuild when incremental compilation leaves dependencies out of date. Existing resources can be repaired with `unity_import_repair_resources "Imported"` or a specific imported subfolder.

## Supported content

- Materials: text-serialized Unity `.mat` and `.terrainlayer` files.
- Texture sources: PNG, JPEG, TGA, TIFF, EXR and PSD (engine decoder support still applies). TIFF, EXR and PSD also get `.vtex` wrappers so the editor can load them as texture resources. EXR wrappers preserve HDR values; baked reflection probes remain source images rather than reconstructed Unity reflection-probe objects.
- Model sources: FBX, OBJ, SMD, DMX and VOX; OBJ MTL sidecars can also be selected.
- Skinned FBX models (meshes bound to a skeleton) are labelled **Skinned model → VMDL**. Their skeleton and skin weights import with the meshes; a bodygroup switches between the skinned meshes. Animation clips inside an FBX and Unity Animator controllers are not converted, so skinned models stand in their bind pose until an s&box animation graph or sequence drives them.
- **Advanced → Also import Unity scenes (.unity → .scene)** converts selected Unity scenes into s&box scenes placing the generated prefabs, with their cameras, lights, sky, ambient light and fog approximated. It is off by default; scenes then show as *(scene import off)*.
- Unsupported files such as scripts, Shader Graphs, Unity terrain data and animation controllers are listed but excluded.

Material texture GUID references, model metadata/prefab references and unambiguous material matches are resolved within the package. Filename-based matches are identified as inferred; ambiguous matches are reported instead of choosing arbitrarily. Dependency relationships are collected in memory during package loading. Checkbox changes calculate a plan from those relationships; confirmation executes that same selection/output snapshot. Missing textures and unreadable material sources appear before importing, while additional material-slot and conversion issues are recorded during conversion. OBJ MTL sidecars can be selected explicitly; arbitrary external model dependencies and missing assets outside the package are not reconstructed.

## Conversion scope

The converter maps common Standard/URP/HDRP base-color and normal texture properties, scalar metallic/smoothness values, color tint and common alpha/emission settings into the complex shader. ShaderLab render tags supply alpha-test, transparency and culling settings. When shader source is included, only its declared texture properties are used, so stale texture properties left by a previous Unity shader do not override the active shader's textures. Common metallic/smoothness and mask maps are split into channel images automatically. Terrain output maps source images to the terrain material fields and supplies default images for missing channels.

Shader conversion is independent of asset publisher. The importer converts material data; it does not translate arbitrary Unity HLSL, ShaderLab programs or Shader Graphs into s&box shaders. A transparent Unity material with no colour texture is routed to `shaders/glass.shader` instead of the complex shader approximation. Common properties can produce usable approximations, but custom wind, vertex blending, procedural shading, water and advanced render behavior require an equivalent s&box shader for visual parity. These approximations are named in the import report. Texture scale and offset are transferred for the selected base-color property. Terrain tint, scalar roughness/metallicity and remap settings are not transferred to `.tmat`. Binary Unity materials are retained with a conversion warning; re-export using Unity's **Force Text** serialization to convert them.

Generated ModelDoc files reference the imported mesh and remap unambiguous material filenames. FBX scale uses its `UnitScaleFactor` and Unity's `globalScale`/`useFileScale` settings to produce the Unity-unit scale; generated models then apply a `ModelModifier_ScaleAndMirror` of 39.37 to convert metres to s&box inches. Disabling Unity file scale treats the source coordinates as meters before applying global scale. Sources without unit information use `import_scale` 0.0254, which together with the 39.37 modifier nets out to the previous inch size. Note the `ScaleAndMirror` modifier is unconditional in the shared template and predates this working-tree change: non-FBX models (`import_scale` 1) are scaled ×39.37 like FBX ones, so their coordinates are treated as metres, not inches. Unity convex MeshColliders become convex hull physics shapes (`HullPerMesh`) on the generated model; several baked colliders on one object collapse to that one hull, and only the first exports a `ModelCollider`. The importer reads ASCII/binary FBX material names and material references from single-model Unity prefabs to restore assignments. Binary FBX diffuse-texture links also resolve unnamed slots when they identify one material; absolute exporter paths are used only for filename matching. FBX meshes with no named material slots and no unambiguous prefab assignment receive a neutral material and an unresolved issue. Exporter LOD suffixes are recognized; where explicit LOD0 meshes exist, those highest-detail meshes are imported without overlaying lower-detail meshes or impostor billboards. Animation controllers are not reconstructed; Unity scene layouts are, when scene import is enabled (see CHANGELOG for what a scene conversion covers). Prefab variants (prefabs derived from another prefab) are reported as unsupported and skipped. Nonstandard model conventions may still require additional support.

Legacy ASCII FBX names without numeric object IDs are recognized. For ASCII sources up to 32 MiB, an exact duplicate vertex array incorrectly appended after a closing brace is removed in a neighbouring `*_iup_repaired.fbx`; the VMDL uses that copy and the original is preserved. The repair is reported and applies only when the misplaced data exactly repeats an existing vertex sequence. Other malformed FBX files are not silently rewritten. This uses the library's C# code and requires no external converter.

Legacy ASCII mesh-to-material and mesh-to-texture connections also resolve exported slot names through the texture filename when a mesh has exactly one material and texture. A unique Unity material using that color texture supplies the binding and dependencies. Ambiguous connections remain unresolved. This legacy link path does not reconstruct a Unity prefab's mesh-coordinate transforms; matching material bindings alone does not establish correct orientation (prefab-hierarchy transforms are converted by the prefab exporter, not here).

Prefab material overrides are matched by source model GUID, renderer identity and material-slot index. ASCII/binary FBX hierarchy and ordered material connections supply the renderer slots; modern Unity model renderer IDs are reproduced from the hierarchy. Explicit renderer/filter blocks with identifiable mesh IDs are also supported. Overrides for excluded LODs cannot replace LOD0 assignments. A prefab sharing the model's filename is preferred; conflicting slot assignments or unsupported renderer IDs produce warnings instead of guessed mappings. Pre-2018 sub-asset recycle IDs (`fileIDToRecycleName`) are resolved alongside modern hashed IDs.

Unity prefabs are converted to s&box `.prefab` assets: the GameObject hierarchy with transforms, names and enabled state, `ModelRenderer`s on the generated models, `ModelCollider`s where Unity had `MeshColliders`, nested prefab instances, and per-instance material/enabled overrides. Overrides on objects below a nested prefab's root (position, rotation, scale, name, enabled) are implemented but not yet exercised by a real package. Scripts and other components are dropped. Prefab variants are skipped and reported as unsupported.

Skinned meshes follow s&box's model: Unity gives every skinned mesh its own object and renders it through a hierarchy of bone objects, whereas s&box renders a skeleton from one `SkinnedModelRenderer`. A skinned FBX therefore generates one `.vmdl` whose `RenderMeshFile` brings in the skeleton and weights, plus a `BodyGroupList`. Each mesh gets its own named `RenderMeshFile` node filtered to that FBX node, as the Citizen's render mesh list does: a bodygroup choice refers to the `RenderMeshFile` name, and one unnamed node importing every mesh compiles to a single `unnamed_1` mesh that no bodygroup can switch. The nodes point at a derived copy of the FBX (`*_iup_skinned.fbx`, beside the untouched source) in which every Geometry object carries its mesh node's name and bones that share a name carry their side (`Finger_01_L`, `Finger_01_R`), so ModelDoc lists meshes and bones by name instead of `unnamed` and `_duplicate`. When the package's prefabs show at most one of the FBX's skinned meshes at a time (an outfit switch), one bodygroup named after the FBX offers each mesh and `none`; otherwise every skinned mesh gets its own on/off group so any combination stays expressible. In prefabs, all `SkinnedMeshRenderer`s of one skeleton become a single `SkinnedModelRenderer` on the parent of the skeleton's root bone (where the FBX root sits), with `BodyGroups` set to the meshes Unity had active (one bit per choice, groups in order, as the compiled model numbers its mesh groups). The objects that only held a skinned mesh and the pure bone objects are dropped; a bone object carrying other components or children is kept as a plain object and reported, since it will not follow the skeleton. Animators are listed as not converted; an FBX take whose curves never change is ignored, while real FBX animation is reported as not converted.

Meshes without FBX material definitions support prefab slot-0 assignments through the model compiler's implicit slots, named after each mesh. Different meshes can retain different materials; unresolved meshes do not replace resolved assignments with a model-wide fallback. Conflicting assignments and extra material passes are reported. Existing neutral fallback files are not deleted automatically; reimport with **Replace** updates the VMDLs to use the resolved materials.

Collision-only classification uses MeshCollider references and absence of visible model references in the package's prefabs, not names containing `collision`. Sources used both visibly and for collision still generate VMDLs, and where Unity had convex MeshColliders the generated VMDL carries the equivalent `HullPerMesh` physics hull. Collider-only sources remain available for manual physics setup. Existing collision VMDLs from older imports are not deleted automatically. To repair existing material bindings, update the library and reimport with **Replace** for the affected VMDLs; resource recompilation alone cannot rewrite their material remaps.

## Reading the import report

- `Unresolved`: missing textures, skipped conversions and unresolved material assignments. A neutral fallback remains an unresolved assignment even if the engine can render the mesh.
- `Selection`: each imported source's explicit/dependency status, planned output type and the parent assets and evidence requiring it.
- `MaterialConversions`: each converted material's source path, shader name/GUID when available, selected output types, and conversion scope. This records common-property conversion, not shader-program translation or verified visual parity.
- `Warnings`: actionable compatibility notes, including custom shader approximations.
- `Information`: routine model/material review advice and counts of excluded unsupported files; these do not inflate warning counts.
- `TextureProcessing`: generated channel count, decoded source count and channel processing time.

The tool has no publisher-specific import branches. Package names are recorded for provenance; asset GUIDs, mesh slots, texture links and material properties drive conversion. Adding support for a custom shader should use its shader identity or property layout and a corresponding s&box implementation. Choosing a publisher or guessing from an asset's appearance is not a reliable shader mapping.

## Implementation

All functionality lives in `Editor/`; the library adds no gameplay components. Archive extraction runs off the UI thread into a leased project temporary directory. Imports stage inside `Assets/Imported/.iup-temp` before per-file atomic replacements. Archive links, traversal, duplicate paths and unsafe Windows filenames are rejected. Limits are 200,000 archive entries, 2 GiB per file and 32 GiB expanded data. Free disk space must accommodate extracted sources plus the selected import.

## Texture processing

Packed-channel requests are grouped by source. Each image is decoded once per import, and up to two independent images are processed concurrently within a 1 GiB estimated working-set admission budget. Very large or unrecognized image formats run alone. Bitmaps and pixel arrays are owned by one worker; this is not a hard cap on native decoder allocations.

There is no persistent conversion cache. Required channels are regenerated for each import, then compared with destination files so identical outputs are not rewritten. Archive extraction and resource compilation still contribute to total import time.

Thread isolation follows [Skia's bitmap threading requirements](https://api.skia.org/classSkBitmap.html).

## Project layout

The library lives in `Libraries/ImportUnityPackage` and uses the `ImportUnityPackage` C# namespace. Its package identity is `grubs.importunitypackage`. The development host uses the separate `importunitypackageproject` identity; open its generated `importunitypackageproject.slnx` solution.

## Development checks

From the host repository:

```powershell
# Focused checkpoint after changing issue reporting.
dotnet run --project Tests/Importer.CoreTests -- --checkpoint reporting
dotnet run --project Tests/Importer.CoreTests -- --checkpoint fbx
dotnet run --project Tests/Importer.CoreTests -- --checkpoint scale
dotnet run --project Tests/Importer.CoreTests -- --checkpoint ascii-links
dotnet run --project Tests/Importer.CoreTests -- --checkpoint import-plan
dotnet run --project Tests/Importer.CoreTests -- --checkpoint storage
dotnet run --project Tests/Importer.CoreTests -- --checkpoint merge
dotnet run --project Tests/Importer.CoreTests -- --checkpoint generated
dotnet run --project Tests/Importer.CoreTests -- --checkpoint prefab
dotnet run --project Tests/Importer.CoreTests -- --checkpoint implicit-slots

# Broader core suite: use after archive/dependency/conversion changes that affect it.
dotnet run --project Tests/Importer.CoreTests

# Read and import every cached package into a temporary test destination,
# verify generated references, retain reports, and remove test imports.
dotnet run --project Tests/Importer.CoreTests -- --real "PATH/TO/PACKAGES" .verification/real
```

The core checks run without the editor or external test packages. For an engine integration run, use the editor console:

```text
unity_import_validate "PATH/TO/PACKAGE.unitypackage"
unity_import_validate "PATH/TO/PACKAGE.unitypackage" true
unity_import_validate_subset "PATH/TO/PACKAGE.unitypackage" "MODEL_FILENAME.fbx"
unity_import_validate_cancel
unity_import_check_channels
unity_import_recheck "PATH/TO/EXPORTED_REPORT.json"
```

The validator uses automatic output choices like the window; the optional `true` argument adds terrain materials. The subset command selects models whose source paths contain the filter and includes their dependencies; `@PATH/TO/selection.json` accepts an array of exact model source paths. It loads source and generated textures, compiles resources, places every generated model into an editor preview scene, checks material assignments, and saves rendered previews. Reports are written under `.verification/engine/`; imported assets remain in `Assets/imported/`. A directory argument validates its packages in sequence. Neutral fallback materials fail the model-assignment check. Other passing checks establish resource usability; custom shader visual parity still needs review.

Prefer the affected checkpoint and a fresh model subset after a fix. Do not repeat complete package batches after each edit. The host project's `unity_import_check_window "PATH/TO/SMALL_FIXTURE.unitypackage"` command checks window loading, automatic output, collapsed Advanced settings, additional terrain output, empty selection and cancellation cleanup. It does not exercise the native file dialog or physical mouse/keyboard input.

Opacity is extracted from the base-color alpha channel for cutout and transparent materials, because the s&box material input uses a separate opacity image. HDRP mask-map blue is treated as a detail mask; TerrainLit mask-map blue supplies height.

Cancel active validation runs before changing editor code. Changing async command signatures during s&box hot reload can stall the editor. The pixel-level channel check verifies the actual exported RGBA values and smoothness inversion through the engine image decoder.

## License

The importer source is licensed under [MIT](LICENSE). Imported third-party content retains its original license and is not part of this library distribution.

Programmatic callers must pass the absolute project Assets directory to `UnityArchive.Read` and dispose the archive when finished (for example, with `using`). Missing, blank or relative Assets paths are rejected before extraction. The editor window handles this lifetime automatically and releases extraction files after each import attempt. Closing or switching packages also releases extraction files and logs any cleanup warnings.
