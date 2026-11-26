# AI note: s&box vs Unity terrain and clutter

Research status: 2026-11-26. Scope: does s&box terrain have anything like Unity's
"Detail Resolution" and "Detail Resolution Per Patch", and what is the correct
porting/sizing advice when a Unity terrain with detail layers is brought over.

Sources, all read on 2026-11-26:

- s&box engine source at `cefa77dd9` (`{cseliot}/1-Development/1-SBox/1-Engine-Builds/Dev/sbox-public`).
- Shipped `Sandbox.Engine.dll` from both installs: Steam (`D:/SteamLibrary/steamapps/common/sbox`)
  and the Dev build (`.../sbox-public/game/bin/managed`).
- s&box manual clone `3-Documentation/2-Docs-Github-Repo/docs/gameplay/terrain/` and
  `docs/rendering/shaders/terrain.md`.
- Unity 6.0 manual, Terrain Settings reference:
  `https://docs.unity.com/en-us/engine/6000.0/manual/creating-environments/script-terrain/terrain-other-settings.md`

## The one-line answer

s&box terrain has no detail layer system. There is no detail map, no detail resolution,
no detail patch grid, and no per-layer detail density. The terrain has exactly one
resolution, shared by the heightmap, the control map and the baked normal map. Grass and
small props belong to the Clutter system, and their density is a Clutter scatterer
setting, not a terrain asset setting.

## How that conclusion was reached

The question was whether two specific Unity settings have s&box counterparts. The
method was to look for the concept, the data, and the setting, in that order.

### 1. Look for the concept in the engine source

`git ls-files | grep -i terrain` lists every terrain file in the checkout. There is no
`TerrainDetail*` file, and no detail type at all. The full set is the component partials
(`Terrain.cs`, `Terrain.Properties.cs`, `Terrain.Rendering.cs`, `Terrain.Collider.cs`,
`Terrain.Edit.cs`, `Terrain.MaterialQuery.cs`, `Terrain.Trace.cs`, `TerrainClipmap.cs`,
`TerrainClipmapSceneObject.cs`), the asset classes (`TerrainStorage.cs`,
`TerrainMaterial.cs`) and the editor tooling.

A content grep for the word `detail` across all terrain code and all terrain shaders
returns two hits, both prose in doc comments:

- `Terrain.Properties.cs:162` - SubdivisionFactor "adds displacement detail at the cost of more vertices".
- `TerrainClipmapSceneObject.cs:57` - "get a mesh subdivided ... for displacement detail".

Neither is a data layer. There is no `DetailResolution`, `DetailLayer`,
`DetailPrototype`, `DetailPatch` or `detailMap` identifier anywhere.

### 2. Look for it in the shipped surface, not just source

The Dev fork is a source HEAD; the Steam install is what actually runs. Both
`Sandbox.Engine.dll` files were grepped for `TerrainDetail*`, `DetailResolution` and
`DetailLayer`. Zero hits in both. The absence is not a source-only feature that missed
the docs; it is absent from the shipped managed surface as well.

### 3. Look for where the resolution lives

`TerrainStorage.Resolution` (`TerrainStorage.cs:141`) is the only resolution property on
the terrain asset. `SetResolution` (`TerrainStorage.cs:216-226`) allocates both arrays
from that one number:

- `ushort[] HeightMap`, `Resolution * Resolution`.
- `uint[] ControlMap`, `Resolution * Resolution`.

The editor exposes it as a single "Resolution" row with a 256 to 8192 enum
(`game/addons/tools/Code/Scene/Terrain/TerrainComponentWidget.Create.cs:7-15`), default
512, and changing it resamples heightmap and control map together (the confirm dialog
says so at `TerrainComponentWidget.Settings.cs:145`).

The GPU side confirms the same coupling. `CreateTextureMaps` (`Terrain.cs:242-266`)
creates:

- `HeightMap` as `R16`, `Resolution x Resolution`.
- `ControlMap` as `R32_UINT`, `Resolution x Resolution`.

and `RebakeNormalMap` (`Terrain.Rendering.cs:216-240`) creates the baked normal map as
`RG1616` at the same `Resolution`.

So there is no second grid to hold detail coverage, and no independent splatmap grid
either. Unity's `alphamapResolution` and `detailResolution` both have no place to live.

### 4. Look at what the control map can represent

The control map is one 32-bit word per texel, decoded as
`CompactTerrainMaterial` (`TerrainStorage.cs:41-45`):

| Bits | Meaning |
|---|---|
| 0-4 | base material id (0-31) |
| 5-9 | overlay material id (0-31) |
| 10-17 | blend factor base to overlay (0-255) |
| 18 | hole flag |
| 19-31 | reserved |

Two materials blended per texel, out of 32 slots. That is the whole splat model. A
Unity terrain layer stack with more than two layers meeting at one texel cannot be
represented faithfully; it has to be baked down or split across terrains.

### 5. Look at the redraw granularity

The nearest thing in spirit to "Detail Resolution Per Patch" is the clipmap, because it
is the granularity at which terrain is split for culling and drawing. But it is a
renderer LOD construct with no authored data behind it:

- `BlockSize = 4` finest texels per instanced block meshlet, a constant
  (`Terrain.Properties.cs:174`).
- `ClipMapLodLevels` 1 to 8, default 6.
- `ClipMapLodExtentTexels` 16 to 2048, default 256, snapped down to a whole number of
  blocks (`Terrain.Rendering.cs:86-90`).
- `SubdivisionFactor` 1 to 4, default 1.

`TerrainClipmap.BuildLayout` (`TerrainClipmap.cs:72-105`) builds nested square rings
around the camera, each ring twice the cell size of the one inside it, with the inner
quarter hollowed out. Meshlets are placed and camera-snapped in the vertex shader
(`TerrainClipmap.hlsl`). Nothing here is painted, saved, or per-material.

### 6. Look for the system that actually does grass

`ClutterComponent` (`ClutterComponent.cs:15-28`) has two modes, Volume and Infinite
streaming, and points at a `ClutterDefinition`. The definition holds the entries
(prefab or model plus weight), `TileSize` 256 to 4096, `TileRadius` 1 to 10, and a
polymorphic `Scatterer` (`ClutterDefinition.cs:14-53`).

Scatterers generate instances procedurally per tile:

- `SimpleScatterer` - uniform density, points per square metre, default 0.05
  (`SimpleScatterer.cs:14-18`).
- `SlopeScatterer` - picks entries by surface angle.
- `TerrainMaterialScatterer` - picks entries by the painted terrain material at the hit
  (`TerrainScatterer.cs:180-252`). This is the closest analogue to "grass belongs to
  this terrain layer".

`CalculatePointCount` (`Scatterer.cs:256-285`) converts density to a count, caps at
10,000 points per tile, then `JitteredGridPoints` places one jittered point per grid
cell and every point is ground-traced. Implementation detail worth knowing: the code
computes `areaSquareMeters * density / 10f`, so the effective point density is the
inspector value divided by ten, even though the field is labelled points per square
metre. At the time of writing this is a code-read observation from `cefa77dd9`, not an
editor measurement.

## Unity side, for the comparison

From the Unity 6.0 Terrain Settings reference (fetched 2026-11-26):

- Detail Resolution: "The number of pixels in the detail resolution map", a squared grid,
  documented range 0 to 4048.
- Detail Resolution Per Patch: "The size in pixels of each detail patch, which is a part
  of the Detail Resolution map. A high value reduces draw calls, but might increase
  triangle count because detail patches are culled per batch. The recommended value is
  16. ... The range is from 8 to 128, and the value is squared to form a grid."
- Heightmap Resolution: power of two plus one, for example 513.
- Control Texture Resolution: the splatmap resolution, independent of the heightmap.
- Base Texture Resolution: the distant composite texture used beyond the basemap
  distance.

So Unity has four independent grids (height, control, detail, base), and the detail
grid is further partitioned into patches for culling. s&box has one grid and a camera
clipmap.

## Mapping table

| Unity TerrainData | s&box equivalent | Notes |
|---|---|---|
| Terrain Width / Length | `TerrainStorage.TerrainSize` | Uniform, world units (inches) |
| Terrain Height | `TerrainStorage.TerrainHeight` | World units, 16-bit height storage |
| Heightmap Resolution | `TerrainStorage.Resolution` | Power of two, not 2^n+1 |
| Control Texture Resolution | same `Resolution` | Cannot be decoupled |
| Base Texture Resolution | none | Clipmap LOD replaces the basemap idea |
| Detail Resolution | none | No detail grid exists |
| Detail Resolution Per Patch | none in data; clipmap blocks are the render analogue | `BlockSize = 4`, not configurable |
| Terrain Layers | `TerrainMaterial` list, 2 per texel, 32 slots | Base + overlay with one blend byte |
| Detail prototypes (grass texture/mesh) | Clutter entries (model or prefab) | Procedural, not painted coverage |
| Tree instances | Clutter entries | Same system as grass |
| Paint Detail tool | none | Clutter is placed by scatterer rules |
| Terrain holes | control map hole bit | `IsHole`, per texel |

## Why the sizing advice follows

Two separate budgets were being conflated in the original question.

1. Splat and height fidelity is governed only by `Resolution`. There is no second knob.
   Raising it costs memory on three textures, all at the same size:

   | Texture | Format | Bytes per texel |
   |---|---|---|
   | HeightMap | R16 | 2 |
   | ControlMap | R32_UINT | 4 |
   | NormalMap (baked) | RG1616 | 4 |
   | Total after first bake | | 10 |

   None of them request mipmaps: `Texture.Create` defaults `m_nNumMipLevels` to 1 unless
   `WithMips` is called (`TextureBuilder.cs:241-246`, `Texture2D.cs:239`), and the
   terrain never calls it.

   The manual's figure (2048 = 24MB, with the note "combined heightmap and control maps")
   is 6 bytes per texel, so it excludes the normal map. Real resident cost at 2048 is
   about 40 MiB, at 4096 about 160 MiB, at 8192 about 640 MiB. Quote the manual figure
   as the documented one and the 10-byte figure when actually budgeting.

2. Grass density and coverage are Clutter scatterer settings. They are generated per
   tile at runtime from tile size, density and the scatterer rules. No part of that is
   stored in the `.terrain` asset, so changing `Resolution` does nothing for grass
   density, and there is no detail map to paint coverage into.

That is why the advice is: raise `Resolution` for finer height/splat data, tune the
Clutter scatterer for grass density, and do not look for a terrain-side detail setting.

## Porting checklist implications

- Remove "port Detail Resolution" and "port Detail Resolution Per Patch" from any Unity
  terrain migration checklist. Nothing to port, nothing to approximate.
- Rebuild Unity detail prototypes as Clutter entries. Texture-only grass billboards have
  no direct equivalent; use a small model or prefab.
- A Unity terrain with more than two layers meeting at a texel needs a bake-down or
  terrain split before it can look right here.
- Trees and details land in the same Clutter system, so author them as one
  `ClutterDefinition` with weights and a `TerrainMaterialScatterer` for distribution.
- Set `Resolution` before sculpting. The change resamples existing height and control
  data, so it is cheapest at project start.

## Reproduce

```bash
cd {cseliot}/1-Development/1-SBox/1-Engine-Builds/Dev/sbox-public
git ls-files | grep -i terrain
git grep -n -i 'detail' -- game/core/shaders/terrain.shader game/core/shaders/terrain \
  engine/Sandbox.Engine/Scene/Components/Terrain engine/Sandbox.Engine/Resources/Terrain

for d in "D:/SteamLibrary/steamapps/common/sbox/bin/managed" \
         "game/bin/managed"; do
  grep -ao 'TerrainDetail[A-Za-z]*\|DetailResolution\|DetailLayer' "$d/Sandbox.Engine.dll" \
    | sort | uniq -c
done
```

Both DLL greps return nothing. The source grep returns only the two doc-comment lines
named above.
