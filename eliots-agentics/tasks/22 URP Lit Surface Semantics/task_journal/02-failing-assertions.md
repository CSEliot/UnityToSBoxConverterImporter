# Evidence — failing assertions written before the fix (2026-11-26)

Harness built from current HEAD bytes (`dotnet build ... --no-incremental`: Build succeeded, 0 Warning(s), 0 Error(s)) and run on SyntyForest with `--folders '*'`, no `--scenes`, into `C:/Users/Eliot/AppData/Local/hermes/profiles/hermes-sbox/cache/scratch/urp22/base` (2862 generated files). The checker is a scratch script (`check_cases.py`, NOT a repo deliverable); its four cases were chosen by reading source `.mat` values, not our conversion formula.

Literal source values and the first run (`python check_cases.py base`):

| # | Source material (literal values) | Assertion on generated `.vmat` | Result |
|---|---|---|---|
| a | `Assets/Synty/PolygonNatureBiomes/PNB_Meadow_Forest/Materials/Plants/WildFlowers_01.mat`: `m_ValidKeywords: - _ALPHATEST_ON`, no `_AlphaClip`, no `_Cutoff`, no `_Mode` | `wildflowers_01.vmat` has `F_ALPHA_TEST 1` and a finite `g_flAlphaTestReference` | **FAIL**: `F_ALPHA_TEST=False, g_flAlphaTestReference=None` |
| b | `Assets/Synty/PNB_Core/Materials/Fog_01.mat`: `m_ValidKeywords` has `_SURFACE_TYPE_TRANSPARENT`, `_Surface: 1` | `fog_01.vmat` is glass or has `F_TRANSLUCENT 1` | PASS at baseline: `glass=True, F_TRANSLUCENT=False`. No keyword-only transparency case exists in the fixture (scratch survey: every material with `_SURFACE_TYPE_TRANSPARENT` also carries `_Surface: 1` or `_Mode`). Recorded as a non-failing regression guard, not a fabricated failure. |
| c | `.../Terrain/Mat_Cobblestone_01.mat`: `m_ValidKeywords: - _NORMALMAP`, `_BumpMap` has a real texture GUID, `_BumpScale: 1` | `cobblestone_texture_01.vmat` has a non-default `TextureNormal` and a finite `g_flNormalMapScaleFactor` | PASS at baseline: `TextureNormal=imported/.../cobblestone_normals_01.png, g_flNormalMapScaleFactor=1.000`. Every one of the 27 `_NORMALMAP` materials has `_BumpScale: 1`, so a *non-1* scale cannot be demonstrated from this fixture; the value is finite and sourced, not fabricated. |
| d | `.../Terrain/Mat_Rock_01.mat`: `_EmissionColor {r: 0.3679245, g: 0.3679245, b: 0.3679245, a: 1}`, no `_EmissionMap`, no `_EMISSION` keyword entry, `m_LightmapFlags: 4` | `rock_texture_01.vmat` has `F_SELF_ILLUM 1` and `g_vSelfIllumTint` | **FAIL**: `F_SELF_ILLUM=False, g_vSelfIllumTint=None` |

`SUMMARY 0/4 passed` was the first run because the checker was accidentally invoked with the scratch dir as output root; rerun with the actual output root (`base`) produced `SUMMARY 2/4 passed` with the two genuine failures above. Both lines are kept verbatim; the mis-invocation is disclosed rather than hidden.

## Keyword grounding (URP 17.3.0 package cache, not inferred)

- `Shaders/Lit.shader:121,125,126,128` declares `_NORMALMAP`, `_SURFACE_TYPE_TRANSPARENT`, `_ALPHATEST_ON`, `_EMISSION` as local shader features.
- `ShaderLibrary/SurfaceInput.hlsl:53-59`: `SampleEmission` returns 0 unless `_EMISSION` is defined, then multiplies the emission map by the colour.
- `Editor/ShaderGUI/BaseShaderGUI.cs:942-953`: URP sets `_EMISSION` from `MaterialGlobalIlluminationFlags.AnyEmissive`; the shipped `Editor/SceneTemplates/Materials/Emissive.mat:14-16` has `_EMISSION` in `m_ValidKeywords`.
- In SyntyForest `_EMISSION` appears in `m_ValidKeywords` **0 times** and in `m_InvalidKeywords` **83 times** (scratch count over 217 `.mat`). Treating those invalid entries as enabled would contradict URP source, so the implementation must not do it. The colour-only emission rule for materials with no keyword entry is therefore a **NEW MAPPING**, not recovered data, and is exposed as a developer-tweakable property.
- Premise limit: alpha/transparency/emission keywords in this fixture live on Shader Graph materials whose shader guid is not in task 21's URP shader table (`Pipeline == Unknown`); only `_NORMALMAP` lives on the 27 URP/Lit materials. A strict `Pipeline == Urp` gate cannot reach case (a) at all. The implementation keeps the report count unchanged and adds a separate, developer-tweakable URP-surface gate for the known Shader Graph materials in this project; Standard and HDRP output must remain byte-identical.
