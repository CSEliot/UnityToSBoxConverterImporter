# SyntyForest - remaining graphics/asset warnings (noted 2026-11-26)

Source: SyntyForest_2026-11-26-04-13-22.projectauditor (Auditor 3.2.0 / rules 3.2.0, Unity 6000.3.29f1).
AssetIssue total 60; 23 in Assets/. Already cleared: PAA1002 (183 -> 0), PAA0003/PAA0000 (33 -> 0).

## Worth considering
- PAA5003 x1, Assets/Synty/PolygonGeneric/Shaders/Generic_Decals.shadergraph
  "no pass URP can render". URP decal passes need the Decal Renderer Feature;
  PC_Renderer has only ScreenSpaceAmbientOcclusion. 0 renderers in Demo.unity use
  Synty/Generic_Decals, so harmless today. Fix only if decals ship: add Decal
  Renderer Feature to PC_Renderer and Mobile_Renderer, verify a decal draws.
- PAS0027 Mipmap Stripping off, PAS0008 Optimize Mesh Data off: build-size only.
  Safe to enable if no runtime material swaps. PAS0020/0021 async upload defaults:
  only if load spikes appear.

## Ignore
- PAA5004 x22: every Synty shader graph has a Built-in RP subshader URP never draws.
  Build/variant cost only. Stripping the Built-In target from 22 graphs would kill
  Demo_02_BIRP.unity (already unusable in URP) for no runtime gain.
- PAA2004 x3: package fallback shaders (core FallbackShader, ShaderGraph
  FallbackError, TMP internal editor shader).
- PAA3000 x2: URP debug Resources prefabs.
- PAA2002 x1: built-in Legacy Shaders/VertexLit from unity_builtin_extra.
- PAA2000 x26: SRP-batcher entries, proven stale/uninitialized (see skill note).

## Console shader warnings (Synty graph output, cosmetic)
- Synty/Clouds: Unity_Branch_float3 implicit truncation at GBuffer vertex.
- Synty/Water, Synty/PolygonShader, Synty/Foliage: pow(f,e) negative-base warning.
  Fixing needs Max(0, base) before each Pow node in the graphs; no visible gain.

Status: noted, no action taken.
