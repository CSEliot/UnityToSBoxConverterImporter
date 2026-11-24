# Research record - console-out-2 error census and scene-issue triage (resolve-errors-part-c)

Read-only research, 2026-11-22. Evidence: `Testing/console-out-2.txt` (14 lines, 1,867 B),
the Unity source project `C:/cseliot/1-Development/4-Unity/2-Projects/old_school_model`,
the importer core `Editor/Core/*.cs` at HEAD `54ca0a6`, and the generated
`Assets/imported/assetpackschoolnameverylong/schoolhdrpnameverylong/scenes/mergev2.scene`.
Three read-only research subagents produced scratch reports under the taskmaster-nov22
scratch dir: `part-c-errors.md`, `part-c-unity.md`, and `part-c-code.md` (the last written by
the Master from a capped subagent's verified summary). No importer code was changed here.

## Verified console census (14 lines)

`wc -l` reports 13 because the last line has no trailing newline; `grep -c ''` gives 14.
Classification: 4 ERROR, 4 WARNING, 6 informational.

| # | Lines | Class | Template | Origin |
|---|---|---|---|---|
| G-A | 1-2 | 2 ERROR | `Error loading resource file "<slot>.vmat_c" (Error: ERROR_FILEOPEN: File not found) - from imported/...` | importer-generated |
| G-B | 13-14 | 2 ERROR | same template, but `scenes/reward-unbox.scene` / `sounds/player_use_fail.sound` | engine/stock |
| G-C | 3-6 | 4 WARNING | `Guid <guid> is already taken by GameObject:<effect> - changing` | importer-generated |
| G-D | 7-12 | 6 informational | Bootstrap Init Done / Bootstrap Networking / SteamAPI_Init / 3 RenderSystem Vulkan lines | engine |

Reproduce:
```
grep -F -c 'ERROR_FILEOPEN: File not found) - from imported/' Testing/console-out-2.txt   # 2
grep -E -c 'box_test\.vmdl_c|denyundo\.vsnd_c' Testing/console-out-2.txt                # 2
grep -F -c 'is already taken by GameObject:' Testing/console-out-2.txt                   # 4
grep -E -c 'Bootstrap Init Done|Bootstrap Networking|SteamAPI_Init|RenderSystem Vulkan|VK_EXT_extended' Testing/console-out-2.txt  # 6
```

Four distinct census groups; only TWO are importer root causes (G-A, G-C). G-B and G-D are
engine/stock or informational and are CLASSIFIED / REPORTED here, not faked as importer fixes
(Clarification Q2 parent decision). No path-length error appears in this log despite the much
longer asset-pack folder names.

### G-A - bare .vmat_c from generated LOD/split models (2 ERROR)
`floor.vmat_c` from `.../structure/stair/floor_stairs_bottom_lod01.vmdl` and `glass.vmat_c`
from `.../structure/windows/windows_07/windows_07_02_glass_lod01.vmdl`. These are split
models (`ModelSplit`, `UnityImport.cs:719-745`) whose `group.Remaps` covers only prefab-
assigned slots; the embedded FBX slots `floor` / `glass` have no remap and no global default
(`:417`). The neutral fallback from HEAD `3ae34c7` misses them because `GroupSlots`
(`:369-372`) sees only `MeshSlotMaterials` keys. This is a REAL GAP in the just-closed
`fix-unresolved-material-slots.md`, not a duplicate. Full code trace: `part-c-code.md`.
DISPOSITION: implementation task `12 LOD Split-Model Material Slots.md`.

### G-B - engine/stock asset FILEOPEN (2 ERROR)
`box_test.vmdl_c` from `scenes/reward-unbox.scene`, `sounds/tools/sfm/denyundo.vsnd_c` from
`sounds/player_use_fail.sound`. No `imported/` prefix; these are s&box engine/stock assets the
importer does not produce. NOT importer-fixable. DISPOSITION: reported here only, no task.

### G-C - post-process effect GUID collisions (4 WARNING)
`82a3f100…` Tonemapping, `bc06616b…` Bloom, `8e1c5e0a…` DepthOfField, `34ec698a…`
AmbientOcclusion. `UnitySceneExport.cs:390` gives each effect child GameObject and its own
component the SAME `StableGuid($"{key}:{type}")` (GameObject at `:521`, component at `:562`), so
s&box reassigns the component GUID. Verified: the generated `mergev2.scene` contains
`82a3f100-b4a5-3f93-a94c-c3b9505c8937`. Not hardcoded across volumes: `StableGuid` is MD5
(`UnityPrefabExport.cs:214-220`) and `index`/`type` are in the key. Full trace: `part-c-code.md`.
DISPOSITION: implementation task `13 Post-Process Effect GUID Collisions.md`.

### G-D - informational bootstrap/render chatter (6 lines)
Bootstrap Init Done, Bootstrap Networking, SteamAPI_Init succeeded, Vulkan physical device,
VK_EXT capability negotiation, Vulkan driver version. Not errors; the VK_EXT line is capability
negotiation, not an asset warning. DISPOSITION: reported here only, no task.

## The five scene-specific issues (from the Task Description)

Full Unity-side investigation: `part-c-unity.md`. Summary of verified source facts, current
importer behaviour, and whether a headless-verifiable gap exists:

1. Fog - "is there one in the unity scene?" YES, but not legacy fog: both scenes have
   `RenderSettings.m_Fog: 0`. HDRP fog lives in Volumes: `VolumeSettings.asset` has
   ExponentialFog (grey, 200 m) and `Scene Settings Profile MergeV2.asset` has the newer Fog
   (meanFreePath 400) + VolumetricFog. Importer emits one `Sandbox.Fog` per scene
   (`UnitySceneExport.cs:275-300`, `UnityVolume.cs:20-22,29-36`), values 7874 / 15748 units.
   NO headless gap: mismatch is volumetric->exponential ceiling plus editor-visual.
2. Skybox - no `RenderSettings` skybox (`fileID: 0`); HDRP sky is PhysicallyBased
   (VolumeSettings, wins the MergeV2 merge) and HDRI `spruit_sunrise_8k.exr` (Test_Range).
   Importer always writes `DefaultSky` with a tint (`UnitySceneExport.cs:260-269,307-317`).
   NO headless gap at HEAD: procedural/HDRI parity was deliberately closed as "default sky +
   report" in task 09. Visual match unprovable headlessly.
3. Sun - MergeV2 has a 20,000-lux directional sun (CCT 3849) + 12-lux fill; Test_Range a 5-lux
   warm light. Importer converts units via `UnityHdrpLighting.cs:58-80` (1000 cd/unit, clamp 8)
   and emits `Sandbox.DirectionalLight` at `UnitySceneExport.cs:668-674`. NO headless gap:
   photometric units/clamp are documented approximations from task 10; brightness/angle is an
   editor-visual check.
4. Trees - assets identified (`Tree_01/Leaf .mat`, FBX, prefabs, albedo TGA). Colour drivers:
   base-colour map/tint/alpha-cut (`UnityMaterial.cs:29,202-204,285-290,36-43`), metallic remap
   already fixed by task 08, plus unverified vertex-colour and prefab-override candidates.
   NO confirmed headless gap; the actual green is editor-visual.
5. Placement - the six objects the Director says ARE placed correctly are verified as
   PrefabInstance `m_Name` overrides in the scenes: Bulletin_Board_01_Snaps014, Wall_03,
   Eaves_03, Shrubs_01, Stairs_Middle_01_Snaps014 (1), Desk_01 (55). Conversion paths:
   `UnityPrefabExport.cs:27` (39.37 units/m), `:223-228` (basis change z,-x,y),
   `:1388-1397` (prefab root position zeroed, with rationale), `RootOf :587`. NO confirmed gap
   yet; specific misplacements need a per-object headless source-vs-output comparison.

Editor appearance in the s&box editor is NOT provable headlessly for any of the five. Where no
headless-verifiable conversion gap exists, the honest disposition is an `Open: <editor check>`
line, per Director Q4 = 1A (phone reply 2026-11-22 21:31). Placement gets its own task with a
headless comparison per Director Q5 = 2A (same reply).

Minor source discrepancy noted by research: current Unity source shows Scene Settings
`m_Profile` as baking profile `371c407c` with `sharedProfile` `ca49d0b4`, whereas archived
task 09 recorded `ca49d0b4`; the importer resolves `sharedProfile|m_Profile`, so the merge
outcome is unchanged.

## Task files created from .template-task.md

- `12 LOD Split-Model Material Slots.md` - G-A, importer code fix, headless gates.
- `13 Post-Process Effect GUID Collisions.md` - G-C, importer code fix, headless gates.
- `14 Scene Placement Comparison.md` - issue 5, headless source-vs-output comparison; fix any
  confirmed gap; report editor-visual as `Open:` (Director Q5 = 2A).
- `15 Scene Visual Fidelity.md` - issues 1-4, report-only verification of no headless gap,
  closes with explicit `Open:` editor checks (Director Q4 = 1A).

G-B and G-D are deliberately NOT given task files: they are engine/stock or informational, and
Clarification Q2 says classify and report, not fake an importer fix. Director Q1 parent decision
("You decide") chose one task per ACTIONABLE root-cause group; Q2 = B (Director correction
2026-11-22 18:33) requires each created task driven to DONE? YES! within this goal.
