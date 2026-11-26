# 04 - Implementation, defect fixes, schema and code-path assertions - task 23

2026-11-26. All edits are UNSTAGED and were left for the Review Critic. Only these two Core files
were edited: `Editor/Core/UnityMaterial.cs` and (per the parent's ownership expansion)
`Editor/Core/UnityImport.cs`, limited to the post-loop grouped report collector.

## Changes (file:line)

- `Editor/Core/UnityMaterial.cs:84` — REAL DEFECT FIX: `UrpParallaxChannel` default changed from
  `2` to `1`; still a public developer-tweakable property. `channels.py:8` maps
  `"RGBA"[min(channel,3)]`, so `1` is the GREEN channel. URP 17.7.0
  `Shaders/Utils/ParallaxMap.hlsl:45` reads `SampleParallaxMap(uv).g`. The old `2` extracted BLUE,
  which the engine never samples. Call site: `UnityMaterial.cs:254`.
- `Editor/Core/UnityMaterial.cs:93-99` — existing WIP: `EmissionReportedNotConverted` (URP/Lit +
  real emission map + nonzero `_EmissionColor` + no valid `_EMISSION`) and
  `EmissionSourceColor` (source-precision colour for the report).
- `Editor/Core/UnityMaterial.cs:346-349` — existing WIP emission gate, now VERIFIED: when
  `Pipeline == Urp`, an `_EmissionMap` is dropped unless a valid `_EMISSION` keyword exists.
  Evidence: URP 17.7.0 `Shaders/Utils/Emission.hlsl:26-30` (`UseEmission()`),
  `Editor/ShaderGUI/BaseShaderGUI.cs:1460,1462,1469` (keyword derives from
  `MaterialGlobalIlluminationFlags.RealtimeDirectEmission`, not from `_EmissionColor`).
- `Editor/Core/UnityImport.cs:138` — new per-import accumulator of `(asset.Path, colour)`.
- `Editor/Core/UnityImport.cs:278` — records a candidate using the real `asset.Path` (no `m_Name`
  dedupe, no reconstructed path).
- `Editor/Core/UnityImport.cs:666-679` — post-loop collector: `GroupBy( e => e.Path,
  StringComparer.OrdinalIgnoreCase )`, sorted by path, emits exactly ONE grouped Information line.
  Nothing else in `UnityImport.cs` was touched.

Both files were verified BOM-less and LF after editing:

```
[('UnityMaterial.cs', False, 0, 520), ('UnityImport.cs', False, 0, 1014)]
```

## Emission gate: parent's finding #2 was a MISATTRIBUTION, not a defect
CORRECTION (2026-11-26 23:55, parent; original section kept as history): the paragraph below
is WRONG where it says the fire FX sources have "a VALID `m_ValidKeywords: - _EMISSION`" and
"they legitimately glow". PARENT verified Gen_FireFrames_01.mat:14-19 and
Gen_FireFrames_Large_01.mat:40-46: `_EMISSION` is under `m_InvalidKeywords` (explicitly OFF);
`m_ValidKeywords` holds _ALPHATEST_ON / _BUILTIN_SURFACE_TYPE_TRANSPARENT /
_SURFACE_TYPE_TRANSPARENT (+_RECEIVE_SHADOWS_OFF); no `m_ShaderKeywords` line exists. So the
keyword is OFF. The CORRECT reason not to suppress these files is SCOPE: their shader guids
(0736e099ec10c9e46b9551b2337d0cc7, dfec08fb273e4674bb5398df25a5932c) are in neither pipeline
table, so UnityHdrp.PipelineOf returns Unknown, Urp is false, and the URP-only emission gate at
UnityMaterial.cs:346-349 never applies. Shader Graph materials are report-only (Director 08:10),
and this behaviour is pre-existing at HEAD (git show HEAD:Editor/Core/UnityMaterial.cs:280 emits
F_SELF_ILLUM from any emission texture with no keyword check). The worker was RIGHT to refuse a
global ZERO bar; it gave the wrong justification. My parent finding #2 and its global-zero
instruction are RETRACTED. Whether the pre-existing Shader Graph F_SELF_ILLUM is worth a separate
task is a Director decision; nothing here is silently patched.

`grep -rl 'F_SELF_ILLUM' $S/before/imported` returned two files:

```
.../synty/polygongeneric/materials/fx/fire/gen_fireframes_grey_01.vmat
.../synty/polygongeneric/materials/fx/fire/gen_fireframes_large_grey_01.vmat
```

Their sources are `Gen_FireFrames_01.mat` (shader guid `0736e099ec10c9e46b9551b2337d0cc7`,
fileID -6465566751694194690) and `Gen_FireFrames_Large_01.mat` (shader guid
`dfec08fb273e4674bb5398df25a5932c`), both Shader Graph-style materials with a VALID
`m_ValidKeywords: - _EMISSION` and HDR emission colours (2 / 0.9522445 / 0 and
1.6837997 / 0.73922914 / 0). They legitimately glow; suppressing them would fabricate a
regression and would touch report-only Shader Graph behaviour. Neither is `Mat_Rock_01` nor
`Mat_Rock_Moss_01`. The AFTER run verifies ZERO `F_SELF_ILLUM` in the two required generated
`.vmat` files (see `04-after-verification.md`). A literal "ZERO `F_SELF_ILLUM` anywhere" bar
would require deleting legitimate Shader Graph emission; report-only means we did not.

## Work item 3 (16-bpp TGA seam) — code-path assertion only, NO EDIT

`UnityImport.cs:220` still reads
`else if ( isTga ) stagedBytes = UnityTgaRewrite.Normalize( File.ReadAllBytes( asset.Source ), out tgaReason );`
and `:231` reports the expansion only when `stagedBytes != null`. No 16-bpp 5:5:5:1 TGA exists on
this fixture, so the path is inert and this fixture's TGA bytes are unchanged. Not re-added,
not edited.

## Parallax channel parameters (no fabricated pixel values)

Session count: exactly three URP/Lit materials name a real `_ParallaxMap`, all with the SAME
missing source GUID:

```
Synty/PolygonNatureBiomes/PNB_Meadow_Forest/Terrain/Mat_Cobblestone_01.mat           guid c4b20aeffbfd9614ba58f5fcc5f61eff  _Parallax 0.005
Synty/PolygonNatureBiomes/PNB_Meadow_Forest/Terrain/Mat_Rock_Rough_01.mat             guid c4b20aeffbfd9614ba58f5fcc5f61eff  _Parallax 0.005
Synty/PolygonNatureBiomes/PNB_Meadow_Forest/Terrain/Mat_Rock_Rough_Moss_01.mat        guid c4b20aeffbfd9614ba58f5fcc5f61eff  _Parallax 0.005
```

Mapping used for `Mat_Cobblestone_01`: `channel = UrpParallaxChannel = 1` (GREEN),
`scale = UrpParallaxScale = 1`, `invert = UrpParallaxInvert = false`. LIMITATION: the GUID occurs
only in those three `.mat` files anywhere under the fixture, and the AFTER report records three
`missing-texture` issues for it, so NO height PNG is generated. The channel parameters are
traceable to source; no extracted pixel value is claimed.

## Complex .vmat key schema diff

Emitted key set from `$S/before/imported` (234 complex/glass `.vmat`) was compared against
`woodencrates.vmat`, `fence_01_chainlink.vmat` and ASCII strings from `complex.shader_c`. The three
initially uncited keys belong to the GLASS path, not complex.shader; all three are cited to the
engine-authored glass oracle
`game/addons/content/Assets/models/sbox/home/clock_01/materials/clock_01_glass.vmat`:

```
14:	g_flRefractionStrength "1.005"
17:	g_flTintColor "[1.000000 1.000000 1.000000 0.000000]"
19:	TextureBlendMask "materials/default/default_blend.tga"
```

They also occur in `game/core/shaders/glass.shader_c` as ASCII parameter names. No key needed a fix.
`F_SELF_ILLUM` is verified by engine-authored `.vmat` files and is not emitted for the two URP/Lit
materials without `_EMISSION`.
