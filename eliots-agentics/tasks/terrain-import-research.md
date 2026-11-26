# Terrain import research: Unity 6.7 URP to s&box

Date: 2026-11-26. Author: Task Master (Master work, Director-authorized task 27).
HEAD: `69056f05a224ab53af64c61c88a23c475addf2fd`.

CORRECTION (2026-11-26, after Review Critic verdict "changes required", deleg_8d070762): an earlier answer in this session said the two oracle projects were still to be built and that no authored `Sandbox.Terrain` instance existed. That was wrong. Both projects already exist on disk and predate task 27: the s&box oracle scene was saved 2026-11-25 09:38, and the Unity oracle's first git commit is 2026-11-23 22:59:27 ("Initial check-in"), with its TerrainData last written 2026-11-25 22:52. The earlier "zero authored instance" result was TRUE ONLY of `1-Engine-Builds/Dev/sbox-public`, because all three research children scoped their searches there and none looked at the workspace's `2-Projects/` tree. The Task Master re-verified every corrected claim below against primary source. This document supersedes the earlier answer.

Authorization: Director 2026-11-26 05:46, "Yes add that terrain research task." "Unity 6.7 URP" is the session label for the on-disk fixture `6000.3.29f1` / URP `17.3.0` (Director 1A); note the pre-existing Unity oracle is on `6000.7.0b10` (`unity_samples_oracle/ProjectSettings/ProjectVersion.txt:1-2`), a different build whose relation to the session label is UNVERIFIED.
Scope: read-only research. No oracle projects built, no implementation task files created, no `Editor/Core/*.cs` change, no new tooling, no fixture package, no editor round, no commit of generated output.
Sources: three read-only research children in `deleg_5a07ef74` (`01-unity-terraindata.md`, `02-sbox-terrain-schema.md`, `03-mapping-and-oracle.md`, under `C:/Users/Eliot/AppData/Local/hermes/profiles/hermes-sbox/cache/scratch/terrain-research/`), plus the two pre-existing oracle projects (section 1.1), `urp-support-research.md` (committed `77eca25`), task 26 `task.md`, and the Review Critic's 8 findings.
Evidence labels: VERIFIED = parent re-checked against primary source; CHILD-REPORTED = research child's finding, not independently re-derived; UNVERIFIED = explicitly unproven.

## 1. Direct answer: are the two oracle projects still useful?
Yes, but not as things to build. They already exist, and they already answer several of the questions this research originally filed as "oracle-only".

### 1.1 Pre-existing oracle projects (VERIFIED, 2026-11-26)
`C:/cseliot/1-Development/1-SBox/INDEX.md:24` describes `2-Projects/` as the "Working area of project clones and scratch copies (gitignored)". The two projects sit there and under the Unity tree; neither is part of `sbox-public`, which is why a repo-wide search of `sbox-public` found no authored `Sandbox.Terrain`.

- `C:/cseliot/1-Development/1-SBox/2-Projects/sbox_samples_oracle/` (scene mtime 2026-11-25 09:38, not a nested git clone). `Assets/scenes/minimal.scene:183-253` is a saved `Sandbox.Terrain` component: `__type: "Sandbox.Terrain"` (`:183`), `__guid` (`:184`), `__enabled`, `Flags`, `__version: 1` (`:187`), all serialized `[Property]` keys (`:188-252`), and a fully embedded `Storage` (`:209-247`) with `"$compiler": "embed"`, `data.Maps.$blob` (`:213-215`), `Resolution: 512`, `TerrainSize: 7874`, `TerrainHeight: 7874` (`:216-218`), two `.tmat` references in `{Id, Path}` form (`:219-228`), and `MaterialSettings` (`:229-242`). The blob sidecar `Assets/scenes/minimal.scene_d` (20,947 bytes) contains the literal names `heightmap` at byte offset 92 and `splatmap` at 17929. `Assets/sample_terrain_material.tmat:1-16` is a real authored terrain material. `Assets/scenes/notes_on_scene_creation.txt:1` records the hill was made by hand with the raise/lower tool (size 3937, opacity 1, 10 clicks), matching the Director's "as similar as humanly possible" description.
- `C:/cseliot/1-Development/4-Unity/2-Projects/unity_samples_oracle/` (nested git clone; TerrainData mtime 2026-11-25 22:52). `Assets/sample_terrain_asset.asset` is 1,960,540 bytes; `Assets/Scenes/SampleScene.unity:287` is `--- !u!218 &219513031`, with `m_TerrainData` guid `53d7110d3beef294b99d01ae037f9602` (`:285`, `:296`), `m_TreeDistance: 5000` (`:297`), `m_HeightmapPixelError: 5` (`:303`); 13 `.terrainlayer` files; `ProjectSettings/ProjectVersion.txt:1-2` = `6000.7.0b10`. `SampleScene.unity:53` also sets `m_EnableBakedLightmaps: 1` (section 8).

What these samples already answer, read-only: the exact `__type` literal, `__version: 1`, the full serialized `[Property]` key set, the exact embedded-`Storage` JSON shape, the `{Id, Path}` resource-reference form, the `Maps.$blob` container plus its named `heightmap`/`splatmap` maps, the real `.tmat` key set, and the existence of a small hand-authored Unity terrain. See section 7.1.

### 1.2 Revised conclusion
The `sbox-samples-oracle` is NOT "essential because it must be built". It is already built and is the authoritative sample for the s&box serialized schema. The `unity-samples-oracle` is already built and is a small controlled Unity terrain (1.96 MB TerrainData, 13 layers) that is far better suited to schema work than SyntyForest's 41 MB / 28-layer fixture. The zero-authored-instance result must be stated as scoped to `sbox-public`, not as a workspace-wide fact.

What remains genuinely open is the mapping from Unity's binary TerrainData to that `.terrain` blob, plus controlled ablations that the existing single-pair samples cannot isolate (section 7.2). Perceptual side-by-side is the last step, not the method (section 8). The "decide the oracle size from evidence" part of the Director's request is settled: use the existing small samples; extend them only if an ablation needs a new axis. Task 27 builds neither project.

## 2. What the importer does today (VERIFIED, HEAD 69056f0)
- `UnityArchive.cs:187` classifies `.mat` or `.terrainlayer` as `UnityAssetKind.Material`. The switch has no `.asset` case, so a Unity `.asset` falls through to `UnityAssetKind.Unsupported` at `:193`.
- `ImportPlan.cs:180` detects the `.terrainlayer` suffix; `:183-184` select `.tmat` (`Vmat` false for non-model terrain layers under `Automatic`, `Tmat` true under `Automatic && terrain`).
- `UnityMaterial.cs:78` routes `TerrainLayer:` text to `ParseTerrainLayer` at `:216-232`, which reads only `m_DiffuseTexture` / `m_NormalMapTexture` / `m_MaskMapTexture` into `_MainTex` / `_BumpMap` / `_MaskMap` (`:219-223`) and `m_Metallic` / `m_Smoothness` (`:224-228`), then adds two warnings (`:229-230`). It ignores `m_TileSize`, `m_TileOffset`, `m_NormalScale`, `m_DiffuseRemap*` and `m_MaskMapRemap*`.
- `ToTmat` at `UnityMaterial.cs:415-438` writes `__version`, `AlbedoImage`, `NormalImage`, `RoughnessImage`, `AOImage`, `HeightImage` (defaults `:420-426`, aliases `:432-435`). `:417` states verbatim: `// TerrainMaterial image fields are source image paths, not .vmat references.`
- `UnityImport.cs:306` emits `material.ToTmat( Resolve )` when `planned.Tmat`.
- `UnitySceneExport.cs:18-19` lists terrain among things "listed in the report, not converted"; `:693` uses `"218" => "Terrain"`, `"154" => "TerrainCollider"` only as labels. `UnityPrefabExport.cs:886-888` reports class 154 unconverted.
- VERIFIED (`git grep 'Sandbox.Terrain' HEAD -- Editor/Core/`): no `Sandbox.Terrain` emission anywhere. Terrain support today is `.terrainlayer -> .tmat` plus report lines.
- `urp-support-research.md:67,100` already mark heightmap serialization UNVERIFIED and task 26 report-only.

## 3. Unity side: what a terrain actually is
VERIFIED (parent):
- `ProjectSettings/EditorSettings.asset:7` `m_SerializationMode: 2` (Force Binary).
- `Assets/Synty/PolygonNatureBiomes/PNB_Meadow_Forest/Terrain/_Meadow_Terrain_01.asset` is 41,358,500 bytes; first 24 bytes `00 00 28 00 02 77 14 a4 00 00 00 15 00 00 28 20 00 00 00 00 32 30 31 39` (embedded `2019...` version string).
- 28 `.terrainlayer` files, each with exactly 18 `m_*` keys (parent re-count: all 28 returned 18). Representative `Terrain_Meadow_Grass_01.terrainlayer:5-22` (the `:4` `TerrainLayer:` header is not a key), with `m_DiffuseTexture` `:10`, `m_NormalMapTexture` `:11`, `m_MaskMapTexture` `:12` (`fileID: 0`), `m_TileSize {x: 4, y: 4}` `:13`, `m_TileOffset` `:14`, `m_Specular` `:15`, `m_Metallic 0` `:16`, `m_Smoothness 0` `:17`, `m_NormalScale 1` `:18`, and the four remap keys `:19-22`.
- Exactly 2 class-218 Terrain blocks in SyntyForest: `Demo.unity:132657` and `Demo_02_BIRP.unity:132659`, both `&276425552`, both referencing TerrainData guid `a1eddcd73a70e8f48a947b8240c684f4` (`Demo.unity:132655` TerrainCollider, `:132666` Terrain). `Demo.unity` values: `m_TreeDistance 5000` `:132667`, `m_DetailObjectDistance 80` `:132671`, `m_DetailObjectDensity 1` `:132672`, `m_HeightmapPixelError 5` `:132673`, `m_ShadowCastingMode 2` `:132677`, `m_DrawHeightmap 1` `:132678`. `m_EnableBakedLightmaps: 1` at `:53`. The other three `.unity` scenes have no `!u!218` block.

VERIFIED field enumeration (binary type-tree strings, `grep -abo -m 1` byte offsets in the 41 MB asset): `m_AlphaTextures` 8503, `m_AlphamapResolution` 8519, `m_BaseMapResolution` 8540, `m_DetailDatabase` 8575, `m_Patches` 8592, `m_DetailPrototypes` 8674, `m_TreeInstances` 9076, `m_TreePrototypes` 9177, `m_Heights` 9263, `m_Holes` 9273, `m_HolesLOD` 9281, `m_Resolution` 9364, `m_Levels` 9377, `m_Scale` 9386, `m_PreloadShaders` 9394. Field NAMES are VERIFIED; decoded numeric VALUES remain CHILD-REPORTED or UNVERIFIED (section 10).

VERIFIED URP 17.3.0 terrain inputs: `Library/PackageCache/com.unity.render-pipelines.universal@04369b574bc4/package.json:4` = `"version": "17.3.0"`; `Shaders/Terrain/TerrainLitInput.hlsl:26-30` declares per-layer `_Metallic0..3`, `_Smoothness0..3`, `_MaskMapRemapOffset0..3`, `_MaskMapRemapScale0..3`; `:41-42` `_TerrainHeightmapRecipSize` / `_TerrainHeightmapScale`; `:76` `_TerrainHeightmapTexture`; `:86-91` `_TerrainHolesTexture` and its sampler; `:112` `SampleLayerMasks(i)`; `:150-153` height unpack and vertex displacement. So the per-layer texture-set expectation (four layers, each with metallic/smoothness/mask-remap inputs) IS readable on disk; the earlier "contents not read" claim is withdrawn. Layer-count limits beyond four and basemap-generation details were not inspected (UNVERIFIED).

CHILD-REPORTED (binary internals; no parser built, per 5A):
- Embedded type tree with classID 28 `Texture2D` (66 nodes) and classID 156 `TerrainData` (171 nodes); 8 objects: 7 embedded Texture2D (5,592,572 bytes each) plus one TerrainData `pathID 15600000` (2,200,196 bytes); 29 external refs (28 GUIDs plus `unity_builtin_extra`).
- `m_Heights` is a `vector<SInt16>` of raw 16-bit signed samples; `m_Holes` / `m_HolesLOD` are UInt8 vectors; alphamaps are embedded Texture2D objects, not separate image files; layers are external GUID refs.
- Decoded values `m_AlphamapResolution: 1024`, `m_BaseMapResolution: 1024`, `m_Patches` 1024 entries, `m_TerrainLayers` 28 refs with `m_PathID 8574412962073106934` are CHILD-REPORTED and remain UNVERIFIED.

Consequence: any real terrain import must parse this binary serialized file. Nothing in the current importer does.

## 4. s&box side: what Sandbox.Terrain actually is
VERIFIED (parent):
- `engine/Sandbox.Engine/Resources/Terrain/TerrainStorage.cs:121-122` declares `[AssetType( Name = "Terrain", Extension = "terrain", Category = "World" )] TerrainStorage : GameResource`.
- `:41-43` packed control word bitfields `BaseTexture` (shift 0, 5 bits), `OverlayTexture` (shift 5, 5), `Blend` (shift 10, 8); `IsHole` at `:96`.
- `:273-274` and `:291-292` write deflate-compressed named maps `"heightmap"` (`sizeof( ushort )`) and `"splatmap"` (`sizeof( uint )`); read back at `:316-317`, `:336-339`; `DeflateStream` at `:352`, `:360-363`.
- `Terrain.Properties.cs` serialized keys: `Storage` `:10`, `MaterialOverride` `:31`, `TerrainSize` `:94`, `TerrainHeight` `:114`, `ClipMapLodLevels` `:147`, `ClipMapLodExtentTexels` `:154`, `SubdivisionFactor` `:165`, `RenderType` `:184`; `Terrain.Collider.cs:10` `EnableCollision`.
- `Component.Serialize.cs:397,399,400` constants `__guid`, `__version`, `__type`.
- `game/core/materials/core/terrain.vmat`: `:3` `Layer0`, `:5` `shader "shaders/terrain.shader"`, `:8` `F_VERTEX_DISPLACEMENT 1`, `:11` `g_bFogEnabled "1"`. No texture parameters.
- Zero authored `Sandbox.Terrain` instances inside `sbox-public` (parent: 0 in `game/` `.scene`/`.prefab`; child 2: 0 repo-wide including bin/obj, no `.terrain` or `.tmat` assets). This is a SCOPED result, not a workspace-wide one (section 1.1).

VERIFIED from the pre-existing sample (not an inference):
- `__type: "Sandbox.Terrain"` and `__version: 1` at `minimal.scene:183,187`.
- The exact embedded `Storage` JSON shape at `:209-247`, the `{Id, Path}` reference form at `:219-228`, and the named `heightmap` / `splatmap` maps in `minimal.scene_d` at byte offsets 92 / 17929.
- The real `.tmat` key set at `Assets/sample_terrain_material.tmat:1-16`: `AlbedoImage`, `RoughnessImage`, `NormalImage`, `HeightImage`, `AOImage`, `UVScale`, `Metalness`, `NormalStrength`, `HeightBlendStrength`, `DisplacementScale`, `NoTiling`, `Surface`, `__references`, `__version`. The importer's `ToTmat` emits the five image-path keys with matching names but omits `UVScale`, `Metalness`, `NormalStrength`, `HeightBlendStrength`, `DisplacementScale`, `NoTiling`, `Surface` and `__references`.

CHILD-REPORTED (schema detail from engine source):
- Height/control data are runtime `R16` / `R32_UINT` textures (`Terrain.cs:242-266`), not authored `.vtex`. Clipmap geometry is generated (`TerrainClipmap.cs:32-105`); there is no clipmap asset.
- Authoring: `GameObjectNode.cs:850-862` creates a Terrain; `TerrainDropObject.cs:19-42` assigns a `TerrainStorage`; `ImportHeightmap.cs:105-143` reads raw 8/16-bit heightmaps; `TerrainComponentWidget.Settings.cs:218-313` packs RGBA splat into a `uint`; `Terrain.Edit.cs:70-105` syncs GPU textures back to CPU.
- Disk: `TerrainStorage.cs` JSON scalars `Resolution`, `TerrainSize`, `TerrainHeight`, `Materials`, `MaterialSettings` (`:140-199`) plus `Maps` `"$blob"` (`:124`) holding the two deflate arrays; `TEST/TerrainTests.cs:160-200` proves a bit-exact round-trip. Legacy base64 upgrade at `TerrainStorage.Upgrade.cs:110-141`.
- `terrain.shader` inputs are bindless structured buffers, not `.vmat` texture parameters (`TerrainCommon.hlsl:24-76,109-111,192-270`; `TerrainSplatFormat.hlsl:97-152`).

The generic `Storage` reference shape is VERIFIED. What remains UNVERIFIED is whether a standalone `.terrain` resource, rather than embedded `Storage`, survives a controlled round-trip (section 10 item 2).

## 5. Concept classification
- Heightmap: NEW MAPPING. Target `R16` `ushort` grid exists (`Terrain.cs:252-256`; `TerrainStorage.cs:126-131,291`), but the source is a binary blob with no importer reader; decode, transform and round-trip are UNVERIFIED.
- Control / splat maps (alphamaps): NEW MAPPING. Unity stores multi-channel alpha layers; the target stores one packed `uint` with base/overlay/blend/hole (`TerrainStorage.cs:41-45,96`). Capacity for N Unity layers is UNVERIFIED.
- TerrainLayers: NEW MAPPING, partial today. `.terrainlayer -> .tmat` ships; binding those textures into `TerrainStorage.Materials` and shader layers does not. Splat-to-layer binding is UNVERIFIED.
- LOD / pixel error: NEW MAPPING, depends on UNVERIFIED. Target `ClipMapLodLevels` / `ClipMapLodExtentTexels` / `SubdivisionFactor` exist (`Terrain.Properties.cs:147-165`); no verified equivalence to Unity `m_HeightmapPixelError: 5`.
- Holes: NEW MAPPING, depends on UNVERIFIED. Target `IsHole` bit exists; decoding Unity's hole array from the binary TerrainData is UNVERIFIED.
- TerrainCollider: DIRECT-MAP, not implemented. `Terrain : Collider` is the direct analogue (`Terrain.cs:10,192-220`); the importer reports class 154 unconverted (`UnityPrefabExport.cs:886-888`).
- Tree instances/prototypes and detail prototypes/density: REPORT-ONLY, and this is a scope decision, not an impossibility claim. VERIFIED: no direct tree-instance or detail-instance target exists in the engine's Terrain component set. Engine-wide re-check found scatterer targets outside that set, `engine/Sandbox.Engine/Scene/Components/Clutter/TerrainScatterer.cs:35-39` (`SlopeScatterer`) and `:206-210` (`TerrainMaterialScatterer`), which place vegetation by slope or by terrain material. Whether those could map Unity tree/detail instances is UNVERIFIED (section 10 item 13), so the classification is held for this session rather than asserted as a proven impossibility.

## 6. The hard blocker
Two different binary containers, neither of which the importer reads:
- Unity: a 41 MB Force-Binary serialized asset with an embedded type tree, `SInt16` height samples, 7 embedded alpha Texture2D objects and 28 external layer GUIDs (or, in the small oracle, a 1,960,540-byte asset).
- s&box: a `.terrain` `GameResource` whose `Maps` `"$blob"` holds deflate-compressed `ushort` heightmap + `uint` splatmap, referenced by the serialized Terrain component's `Storage`.

The s&box side now has a concrete authored example (`minimal.scene`), so the schema is no longer unknown. What is still unbuilt is (a) a Unity binary-serialization reader, (b) an s&box `TerrainStorage` writer that emits that shape, and (c) a verified mapping between them. The existing samples let us pin (b) and test (c) before writing (a).

## 7. Oracle design (existing samples plus controlled ablations)

### 7.1 Already answered read-only, no new oracle needed
- Q1 (exact serialized heightmap key / `Storage` shape): ANSWERED by `minimal.scene:209-247` and `minimal.scene_d` byte offsets 92 / 17929. The data is carried by `data.Maps.$blob`, whose sidecar holds named `heightmap` and `splatmap` maps, inside an embedded `Storage` with `"$compiler": "embed"`.
- Q8 (does a hand-authored scene + resource pair exist?): the premise "no authored `Sandbox.Terrain` scene to copy" is false. `minimal.scene` is that sample. A bit-exact save/reload round-trip is still a controlled test (section 7.2), but the existence/schema precondition is met.
- Q4 (material path): PARTIALLY ANSWERED read-only. `sample_terrain_material.tmat:1-16` shows the real key set; `minimal.scene:219-228` shows how `.tmat` resources are referenced from `Storage.Materials`. The importer's `UnityMaterial.cs:415-438` emits the five image-path keys with matching names but omits eight other keys. Whether those omissions affect terrain shader sampling is a controlled test, not an oracle-only unknown.
- Also VERIFIED read-only: the `__type` literal, `__version: 1`, the full `[Property]` key set, the `{Id, Path}` reference form, and the small hand-authored Unity terrain (`unity_samples_oracle`, 1.96 MB TerrainData, 13 layers, class-218 at `SampleScene.unity:287`).

### 7.2 Controlled ablations (the only questions that still need an oracle)
Hold constant (one logical terrain, one ablation axis at a time): scene identity (one Terrain GameObject, one class-218 `Terrain`, one class-154 `TerrainCollider`, same names, transforms and active state); extents/units (Unity TerrainData size vs `TerrainSize`/`TerrainHeight`, `Terrain.Properties.cs:94-124`), with the unit/basis convention fixed on both sides; height resolution and numeric 16-bit samples (`Terrain.cs:252-256`); layer inventory (same N, same diffuse/normal/mask texture bytes, same tiling); one canonical per-texel splat assignment; realtime lighting with one fixed camera, fog off, identical tonemapping/exposure and both sides free of baked data (`m_EnableBakedLightmaps: 1` at `Demo.unity:53` AND at `unity_samples_oracle/Assets/Scenes/SampleScene.unity:53` must be neutralised); identical hole mask.

Vary one axis at a time: height amplitude; terrain resolution; layer count (1 / 2 / 4 / >4); which layer dominates a texel; hole present vs absent; non-square size; pixel-error / clipmap LOD settings; trees/details present vs absent; texture tiling scale.

Questions only a controlled ablation can answer:
1. Clipmap fidelity: can `R16` meshlet displacement (`Terrain.cs:252-256`; `terrain.shader:79-81,87-139`) reproduce a known Unity height field at the same resolution and amplitude, and what is lost as `ClipMapLodLevels` / `SubdivisionFactor` vary?
2. Splat to layer mapping with N > 2: how do Unity alphamap channels map onto the 5-bit base / 5-bit overlay / 8-bit blend word (`TerrainStorage.cs:41-45`), where do the N layer textures live (`:153`), and where does the 5-bit / 32-ID cap bind?
3. Hole parity: does `IsHole` (`TerrainStorage.cs:96`) reproduce a known Unity hole mask and does the shader hole path render the same cutout?
4. Collider parity: does `Terrain : Collider` (`Terrain.cs:10,192-220`) reproduce the Unity TerrainCollider heightfield?
5. LOD parity: does `m_HeightmapPixelError: 5` / `m_HeightmapMaximumLOD: 0` map to any clipmap property, or is it strictly report-only?
6. Standalone-resource round-trip: does a `.terrain` GameResource referenced by path, rather than embedded `Storage`, survive save/reload unchanged?
7. Importer-emitted `.tmat`: do the eight omitted keys from `UnityMaterial.cs:415-438` change terrain shader sampling versus the authored `sample_terrain_material.tmat`?
8. Binary decode fidelity: does a Unity TerrainData reader reproduce the same height and splat fields as the Unity editor for the same asset?

Smallest useful oracle: the two existing projects already are it. `unity_samples_oracle` has a 1.96 MB TerrainData and 13 layers; `sbox_samples_oracle` has a hand-made hill recorded at `notes_on_scene_creation.txt:1`. Extend them only if an ablation needs a new axis. Task 27 builds neither.

## 8. Screenshot-oracle trap
VERIFIED hazard: both `Demo.unity:53` (SyntyForest) and `unity_samples_oracle/Assets/Scenes/SampleScene.unity:53` set `m_EnableBakedLightmaps: 1`. The importer reports lightmaps as not converted (`UnitySceneExport.cs:18-19`; `urp-support-research.md:63,69`). A screenshot from a baked render reflects a different serialized state than the one the importer consumed, so copying serialized values cannot reproduce it. No `LightingData.asset` or `*_comp_light.exr` was found under the SyntyForest fixture `Assets/` (child 3, negative result; UNVERIFIED for any particular screenshot). Both sides must be realtime and have their state recorded. Schema work settles headlessly first; perceptual side-by-side is the last step and stays with the Director (`urp-support-research.md`, verification strategy section).

## 9. Backlog consequence
- Director 3A includes "terrain visuals" in scope, but backlog 21-26 has no implementation task for terrain. Task 26 is explicitly report-only: "No Sandbox.Terrain is emitted; the 28 terrain layers are reported."
- This research does NOT close that gap. It establishes that a terrain implementation task would be substantial (binary reader + resource writer + mapping) and that its acceptance should start from the sample round-trip in section 7.2.
- Because the two oracle projects already exist, the Director's decisions are now: (a) whether to extend them for the ablations in section 7.2, (b) whether to add a terrain implementation task or amend task 26 in this session's backlog, and (c) whether to reconcile the Unity oracle's `6000.7.0b10` build with the SyntyForest `6000.3.29f1` / URP `17.3.0` fixture (section 10 item 14). Task 27's acceptance forbids creating implementation task files or building either project.
- No `Editor/Core/*.cs` change, no new tooling, no fixture package, no editor round and no commit of generated output in this task.

## 10. UNVERIFIED register
1. Whether `Sandbox.Terrain` can serialize Unity heightmap/control-map data at all.
2. Whether a standalone `.terrain` resource, rather than embedded `Storage`, survives a controlled round-trip. The generic `Storage` JSON reference shape is VERIFIED (section 4).
3. Whether clipmaps faithfully represent a Unity heightmap, including resolution/amplitude loss under LOD.
4. Whether the 32-bit packed control word can represent N Unity alphamap layers and how >2 layers bind to `TerrainStorage.Materials`.
5. Whether emitted `.tmat` image paths, with the eight omitted keys noted in section 7.1, are consumed by terrain shader sampling as-is.
6. Whether Unity hole arrays can be decoded from binary TerrainData and repacked into `IsHole`.
7. Whether `m_HeightmapPixelError` / `m_HeightmapMaximumLOD` have any clipmap equivalent.
8. Internal binary TerrainData VALUES: heightmap `m_Resolution`, `m_Scale`, `m_Levels`, prototype counts, tree/detail instance counts, 1024 patch AABB interpretation, 28 externals to 28 layers 1:1 GUID mapping, 7 alpha textures to `SplatAlpha 0..6` names. The field NAMES are VERIFIED with byte offsets in section 3.
9. URP 17.3.0 terrain shader layer-count limits beyond the four declared layers and basemap-generation details not inspected in task 27. The on-disk path is `Library/PackageCache/com.unity.render-pipelines.universal@04369b574bc4/Shaders/Terrain/`; the per-layer metallic/smoothness/mask-remap, heightmap and holes inputs are VERIFIED in section 3.
10. Whether any particular Unity screenshot came from a baked-vs-realtime state.
11. Whether ignored `.terrainlayer` fields (`m_TileSize`, `m_TileOffset`, `m_NormalScale`, remaps) need a terrain-specific encoding beyond `.tmat`.
12. Whether more than one terrain is supported at runtime; exact editor-save behaviour for inherited `Collider`/`Component` keys; `DebugView` / `PreviewLayer` semantics.
13. Whether `SlopeScatterer` / `TerrainMaterialScatterer` (`TerrainScatterer.cs:35-39,206-210`) can serve as a mapping target for Unity tree/detail instances.
14. Whether the Unity oracle's `6000.7.0b10` build is what the "Unity 6.7 URP" session label refers to, and whether its terrain serialization differs from SyntyForest's `6000.3.29f1` / URP `17.3.0`.

## 11. Sources
- `01-unity-terraindata.md` - Master transcription of child `sa-0-78028b93`'s completion summary (child hit its 40-call cap before writing the file), corrected for the 18-key count and byte-offset citations.
- `02-sbox-terrain-schema.md` - child `sa-1-62c2ef1e`, 18,532 bytes.
- `03-mapping-and-oracle.md` - child `sa-2-b37616c4`, 14,694 bytes.
- The three scratch reports above are under `C:/Users/Eliot/AppData/Local/hermes/profiles/hermes-sbox/cache/scratch/terrain-research/`, OUTSIDE the repo.
- Pre-existing oracle projects (on disk, not created by this task): `C:/cseliot/1-Development/1-SBox/2-Projects/sbox_samples_oracle/` and `C:/cseliot/1-Development/4-Unity/2-Projects/unity_samples_oracle/`.
- `eliots-agentics/tasks/urp-support-research.md` (committed `77eca25`), task 26 `task.md`, `session-latest.md` 05:46 and 06:03 notes, and the Review Critic's `### Work Review` entry in task 27 `task.md` (2026-11-26 06:12).

## 12. Parent verification log (2026-11-26)
First round: HEAD `69056f0`; TerrainData 41,358,500 bytes and exact first 24 bytes; `EditorSettings.asset:7` `m_SerializationMode: 2`; 28 `.terrainlayer` files and representative key values at `:10-18`; exactly 2 `!u!218` blocks (`Demo.unity:132657`, `Demo_02_BIRP.unity:132659`); `Demo.unity:132666` guid, `:132667` `m_TreeDistance 5000`, `:132673` `m_HeightmapPixelError 5`, `:53` `m_EnableBakedLightmaps: 1`; `git show HEAD:Editor/Core/UnityArchive.cs:193` `Unsupported` with no `.asset` case; `git grep 'Sandbox.Terrain' HEAD -- Editor/Core/` -> none; 0 authored instances in `sbox-public/game/` `.scene`/`.prefab`; `TerrainStorage.cs:41-43,96,121-122,273-274,291-292,316-317,336-339,352,360-363`; `Terrain.Properties.cs:10,31,94,114,147,154,165,184`; `Terrain.Collider.cs:10`; `Component.Serialize.cs:397,399,400`; `terrain.vmat:3,5,8,11`; `UnityMaterial.cs:78,216,415,417,421,432`; `UnitySceneExport.cs:18-19,693`; `UnityArchive.cs:187`.

Second round (after the Review Critic verdict): located both oracle directories under `2-Projects`; read `minimal.scene:180-253` directly; confirmed `minimal.scene_d` `heightmap` at byte 92 and `splatmap` at 17929; read `sample_terrain_material.tmat:1-16` and `notes_on_scene_creation.txt:1`; confirmed `unity_samples_oracle/Assets/sample_terrain_asset.asset` 1,960,540 bytes, `SampleScene.unity:287` `!u!218`, `:285`/`:296` TerrainData guid, `:297` `m_TreeDistance 5000`, `:303` `m_HeightmapPixelError 5`, `:53` `m_EnableBakedLightmaps: 1`, 13 `.terrainlayer` files, `ProjectVersion.txt:1-2` `6000.7.0b10`; re-counted all 28 SyntyForest `.terrainlayer` files as 18 keys each; `grep -abo -m 1` byte offsets for all 15 TerrainData field names; `TerrainScatterer.cs:35-39,206-210` scatterer classes; `TerrainLitInput.hlsl:26-30,41-42,76,86-91,112,150-153` and `package.json:4` version `17.3.0`; `INDEX.md:24` `2-Projects/` description.

NOT independently re-derived: decoded numeric values from the binary TerrainData (no parser built, per 5A no-new-tooling), URP basemap-generation details, and the engine `Terrain.cs` / `TerrainCommon.hlsl` line numbers quoted from child 2's report. These carry the CHILD-REPORTED or UNVERIFIED label above.
