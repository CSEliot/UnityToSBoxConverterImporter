## Task
Report every URP feature that has no s&box counterpart, classify the 186 Shader Graph materials, and keep terrain/sky/fog activation faithful, with no silent fallback to primary_white.vmat.

### Description
Grounding: Editor/Core/UnityMaterial.cs:167 warns that any shader is "approximated using s&box's complex shader; shader code and graph behavior are not translated". Editor/Core/UnityPrimitives.cs:24 defines `PlaceholderMaterial = "materials/dev/primary_white.vmat"`. UnitySceneExport.cs:515,526-529 report planar probes, decal projectors, light-layer masks, baked lightmaps/light probes/occlusion as not converted. Effects/ScreenSpaceReflections.cs:3-12 marks Sandbox.ScreenSpaceReflections obsolete and non-functional; the importer emits it only for camera-template parity (UnitySceneExport.cs:70-76,648). engine/Sandbox.Engine/Scene/Components/Terrain/Terrain.cs:5-10 declares a heightmap Terrain component, but the importer emits no Sandbox.Terrain; whether Unity heightmap/control-map data can be serialised into it is UNVERIFIED.

SyntyForest: 186 of 217 materials (86%) are Shader Graph across 14 graphs (the 187th Shader-Graph-fileID material carries the unresolved guid `1d8f5c5fc8a9f6d428ac325779c841cd`) - Generic_Decals 58, Foliage 40, Generic_ParticlesUnlit 26, Generic_Basic 24, Generic_Standard 13, Generic_ParticlesLit 8, PolygonShader 5, Clouds 3, Water 3, SkyDome 2, noiseTest 1, Refractive_Transparent 1, Skybox_Generic 1, New Shader Graph 1. All 5 scenes have m_Fog: 0 at line 17 and a built-in default skybox material at line 29; m_AmbientMode is 0 or 1. 2 Terrain components, 28 .terrainlayer files, 0 ReflectionProbes. Shader Graph literal `Shader "..."` declarations are UNVERIFIED in the .shadergraph text, and whether all 24 .shadergraph / 12 .shadersubgraph files target URP is UNVERIFIED.

Work:
1. Emit one explicit report line per graph (name, material count, classification: plausible complex .vmat mapping vs report-only), deduped by asset path. No graph material may silently fall back to primary_white.vmat; assert the generated .vmat path is a real generated asset.
2. Emit report-only lines for SSR overrides, baked lightmaps/light probes, planar probes, decal projectors, light-layer masks, and URP additional-data fields with no engine counterpart. Name the source field and value.
3. Terrain: verify the activation chain in BOTH the Unity source and generated output before treating an absent component as a bug. With heightmap mapping UNVERIFIED, terrain stays report-only; do not fabricate a Sandbox.Terrain. Emit the 28 terrain layers as a report line.
4. Sky/fog/ambient: with m_Fog: 0 in all 5 scenes, no Sandbox.Fog is faithful; do not "fix" it by emitting an active light or fog. Copy m_IsActive/m_EnableFog faithfully and report the built-in default skybox mapping. Do not map an unrelated intensity field to fabricate illumination.
5. Gate on `Pipeline == Urp`; Standard and HDRP output unchanged.

### Acceptance (one explicit pass/fail check per line)
1. The report contains exactly one line per graph with the 14 counts summing to 186, and no graph material's generated .vmat is primary_white.vmat; assert the exact generated path exists on disk.
2. Report-only lines name light-layer masks (5 `m_LightLayerMask`), rendering layers (6 `m_RenderingLayers`) and additional-light settings (5 `m_UsePipelineSettings`, 5 `m_CustomShadowLayers`), all present in the source scenes; no Sandbox.ScreenSpaceReflections is emitted. SSR has 0 references in this fixture (`rg -l -i 'screenspacereflection' Assets --glob '*.asset' --glob '*.unity' | wc -l` -> 0), so its assertion is code-path inspection only: the converter must not emit an SSR component from a URP Volume override. A report-only assertion that names only an absent source field cannot fail.
3. With m_Fog: 0, no Sandbox.Fog is emitted; the activation chain is resolved in both source and generated output and recorded.
4. No Sandbox.Terrain is emitted; the 28 terrain layers are reported.
5. building and city--1 output byte-identical to a HEAD baseline in the same mode; per-code Unresolved breakdown unchanged.
6. Harness compile gate 0 errors; verify_output.py `checked N files, 0 problems`.

### Work Review


## Clarification Q&A
An (Director 2026-11-26 03:57, 1A/3A/5A): "Unity 6.7 URP" is the session label for the on-disk 6000.3.29f1 / URP 17.3.0 fixture; target the URP 17.x surface, not one editor build. Visual fidelity only. No new tooling or fixture package.

## Relevant History
See eliots-agentics/tasks/urp-support-research.md sections 3-5 for the Shader Graph census, activation facts and engine citations. HEAD bfc189b; session-start harness compile gate green.

## Notes
Standing constraints: gate every URP rule on `Pipeline == Urp`; keep Editor/Core/*.cs BOM-less and LF; route new paths through UnityImport.DiskPath; compare a generated component to an engine-authored artifact of the same __type for any objective schema defect. Editor rounds follow the 4A conditions: before any editor round, check that no Director-owned editor is already open on that project; never close an editor this work did not launch; NTFY before each large round. Genuinely perceptual judgement stays with the Director.

## Stylistic Task Notes:
References to variables like X and Y automatically imply that these are to be exposed properties for developer tweaking.
When programming, portability and DRY are a priority.

## Post-Task Follow Ups
