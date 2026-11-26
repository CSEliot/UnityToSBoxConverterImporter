# Task 26 plan - URP Report-Only Features and Shader Graph Census

Round 1: QUESTION-ONLY / plan + premise resolution. Protocol roles/worker.md step 3 (STARTED? NO!). No Editor/Core edits, no git add/commit/push, no editor process, no new tooling, no .humans-only access, no status.md change this turn. Final answer = JSON only.

## Scope pins (SPEC.md / task.md / Director 08:10)
- Fixture SyntyForest 6000.7.0b10 (4f3c1c5c4346) / URP 17.7.0; cache
  `Library/PackageCache/com.unity.render-pipelines.universal@e9044f1f1f57` (`package.json:4` =
  `"version": "17.7.0"`), plus `com.unity.render-pipelines.core@7aa87ac5532f` and
  `com.unity.render-pipelines.universal-config@767fc04a69c2`. "Unity 6.7 URP" is the Director's
  22:27 label for that build, not a different editor; target the URP 17.x surface. The former
  6000.3.29f1 / 17.3.0 / `@04369b574bc4` pin is SUPERSEDED and does not exist under 4-Unity.
- BIRP scope: four `.unity` scenes only; `Demo_02_BIRP.unity` was deliberately deleted (Director
  22:55 verbatim: 'I deleted "Demo_02_BIRP.unity" because it stands for "BUILT IN RENDER PIPELINE"
  and we're not supporing that.'); BIRP is DROPPED FROM SCOPE. Baseline: parent-measured Light 4,
  Terrain 1, ReflectionProbe 0, 28 `.terrainlayer`; CAMERA COUNT PINNED by the read-only fact finder
  (Director 23:17: 'Lean on the fact finder for the camera count in that instance.'): 7 `!u!20`
  Camera components over the four scenes (4 enabled, 3 disabled), 6 with a
  UniversalAdditionalCameraData sibling; the 7th is a plain camera at
  `PolygonGeneric/Scenes/Overview.unity:4277`. Use 7 cameras / 6 URP bodies, not 6 or 8.
- Shader Graph materials: DETECT + REPORT only, never translated; never hardcode fixture graph guids as pipeline guids; no silent fallback to materials/dev/primary_white.vmat. Emit one line per graph, deduped by asset path.
- Gate every new rule on Pipeline == Urp (gate is `urpMaterialPaths.Count > 0` + `material.Pipeline == UnityPipeline.Urp`); Standard/HDRP byte-identical to HEAD baseline in the SAME harness mode; per-code Unresolved breakdown unchanged.
- Editor/Core/*.cs BOM-less LF; every new path via UnityImport.DiskPath; no import_scale / 39.37 compensation; every NEW MAPPING a developer-tweakable property.
- SSR [Obsolete]/[Hide] no-op: report only, never convert from a URP Volume override; acceptance by code-path inspection. A report assertion must name fields actually present (m_LightLayerMask, m_RenderingLayers, m_UsePipelineSettings, m_CustomShadowLayers).
- Activation chain in BOTH source and generated output. FALSIFIED IN PLACE (23:17): the old "m_Fog: 0 in 5 scenes" premise included the deleted BIRP scene - Director 22:55 verbatim 'I deleted "Demo_02_BIRP.unity" because it stands for "BUILT IN RENDER PIPELINE" and we're not supporing that.'; BIRP DROPPED FROM SCOPE. m_Fog: 0 in the four remaining scenes -> no Sandbox.Fog is faithful; no fabricated active light / intensity mapping. Terrain report-only, no Sandbox.Terrain; report 28 terrain layers.
- Evidence on disk in task_journal/. No editor round this turn; 4A conditions for any later round. Perceptual sign-off stays with Director. Never cite the scratch SPEC path in a task file.

## Ownership (pinned; NOT edited this round)
- Round-1 Core owner is task 23. Task 26 edits ZERO files under Editor/Core/ this turn.
- Task 26 WRITE set when granted a Core round: `Editor/Core/UnityUrp.cs` (per-graph census records + report-only lines + developer-tweakable properties) and `Editor/Core/UnityImport.cs` (census collection + information.Add emission). No edits to UnitySceneExport.cs / UnityMaterial.cs / UnityPrimitives.cs / UnityHdrp*.cs: SSR and activation checks there are code-path inspection only (read-only), avoiding collision with tasks 24/25.

## Hooks / call sites (read-only this round)
- UnityImport.cs:159-164 shaderGraphGuids from .shadergraph.meta - reuse, no new guid hardcode.
- UnityImport.cs:256-272 material parse loop (materialShaderGuids at :271) - collect graph material paths, dedupe by asset.Path.
- UnityImport.cs:648-663 end-of-import information.Add - emit one line per graph + report-only lines; gate on urpMaterialPaths.Count > 0.
- UnityUrp.cs:25-30 UrpCounts.Report - extend to per-graph lines; new count fields as properties.
- Read-only evidence sites: UnitySceneExport.cs:34,375-403 (DefaultSky + built-in default skybox mapping), :387-390,419-437 (fog gated on m_Fog == 1), :506-531 (not-converted report lines; UnityHdrpProbes.Report at :529), :482-501,648 (SSR camera-template parity + effect map - do NOT add SSR from URP Volume), :693 (Terrain in TypeName table). UnityPrimitives.cs:24 PlaceholderMaterial - assert no graph material resolves to it. UnityImport.DiskPath for all new paths.

## Premise answers (all resolved on disk 2026-11-26, round 1)
P1 VERIFIED. 24 .shadergraph.meta; 14 graphs have material refs: 58+40+26+24+13+8+5+3+3+2+1+1+1+1 = 186; 10 graphs have 0 material refs. Generated path (exists, 870 bytes): `C:/cseliot/1-Development/1-SBox/2-Projects/unityimporterplus_branch_2/Assets/imported/synty/pnb_core/shaders/shader graphs_new shader graph.vmat` from source `Assets/Synty/PNB_Core/Shaders/Shader Graphs_New Shader Graph.mat`; also `.../imported/synty/polygongeneric/materials/decals/generic_decals_tyre_01.vmat` from a Generic_Decals graph material. 0 generated .vmat files reference materials/dev/primary_white.vmat; only _iup_primitives/plane.vmdl(_c) reference it (placeholder primitive). CAVEAT: Assets/imported/ is an untracked pre-existing artifact (git ls-files = 0; files dated 01:27-01:33) - regenerate from fixed sources before any acceptance claim.
P2 VERIFIED with one nuance, then FALSIFIED IN PLACE (2026-11-26 23:17): the old "5" counts
included `Demo_02_BIRP:892773/892774/892771/892775`. Primary source, Director 22:55 verbatim:
'I deleted "Demo_02_BIRP.unity" because it stands for "BUILT IN RENDER PIPELINE" and we're not
supporing that.' BIRP is DROPPED FROM SCOPE, not merged. Re-derived on the four remaining scenes
(primary-source grep, 23:17): `m_LightLayerMask` 4 = SampleScene:369, PG/Overview:23440,
Demo:893194, PNB/Overview:4800. Exact `m_RenderingLayers:` 4 = 370, 23441, 893196, 4801;
Demo:893187 also has the DISTINCT field `m_RenderingLayersMask:`. `m_UsePipelineSettings` 4 =
367, 23438, 893181, 4798. `m_CustomShadowLayers` 4 = 371, 23442, 893183, 4802. Report must name
both m_RenderingLayers and m_RenderingLayersMask so the assertion can fail.
P3 RESOLVED. All 24/24 .shadergraph contain `UnityEditor.Rendering.Universal.ShaderGraph.UniversalTarget`; 22 also list a BuiltIn target (multi-target, URP not exclusive). 0/12 .shadersubgraph contain UniversalTarget; each has `m_ActiveTargets: []` - target-agnostic, cannot be classified URP from target data. Literal `Shader "` declarations: 0 across all 36 files. Source activation (FALSIFIED IN PLACE 23:17 for the old "all 5 scenes" / "2 u!218 Terrain" premise; Director 22:55 verbatim 'I deleted "Demo_02_BIRP.unity" because it stands for "BUILT IN RENDER PIPELINE" and we're not supporing that.' - BIRP DROPPED FROM SCOPE): m_Fog:0 (line 17), built-in default skybox m_SkyboxMaterial {fileID 10304, guid 000...f000...} (line 29) in the four remaining scenes; parent-measured Terrain 1 (Demo.unity:132657), ReflectionProbe 0, Light 4, CAMERA COUNT PINNED by the fact finder: 7 `!u!20` cameras (4 enabled, 3 disabled), 6 URP bodies, 7th plain at `PolygonGeneric/Scenes/Overview.unity:4277`, 28 .terrainlayer, m_AmbientMode 0/1. Generated output: no Sandbox.Fog; SkyBox2D SkyMaterial `materials/skybox/skybox_day_01.vmat` (samplescene.scene:374); no Sandbox.Terrain; 28 .tmat; SSR 2 at samplescene.scene:78 (camera-template parity) and :255 (standalone "Screen Space Reflections" GameObject), 0 in source - report as no-op parity, assert by code-path inspection; a literal "no SSR emitted" line would FAIL against this stale artifact, report to Master. Baked-data fields (m_LightmapData/m_LightProbes/m_ReflectionProbes) 0 in source - falsifiable lines must name the present fields above.
P4 PINNED. 28 terrain layers -> one report line "28 .terrainlayer -> .tmat; Unity Terrain heightmap/control map not converted to Sandbox.Terrain (serialization UNVERIFIED)", no fabricated component. Built-in default skybox -> report line naming m_SkyboxMaterial guid 0000000000000000f000000000000000 / fileID 10304 and generated materials/skybox/skybox_day_01.vmat. Report-only lines: SSR (code-path inspection: no SSR from URP Volume override; name present m_LightLayerMask/m_RenderingLayers/m_UsePipelineSettings/m_CustomShadowLayers counts), baked lightmaps/light probes/occlusion (existing UnitySceneExport.cs:526 line), planar probes/decal projectors (UnityHdrpProbes.Report/:515), light-layer masks (4 present over the four remaining scenes; the old "5 present" included the deleted BIRP scene and is FALSIFIED/DROPPED FROM SCOPE). No Sandbox.Fog, no Sandbox.Terrain, no fabricated illumination.

## Verification split
HEADLESS (objective, this task): counts above; generated .vmat path existence + no primary_white fallback; URP target/literal-Shader census; activation chain source vs generated output; SSR code-path inspection (no new SSR from URP Volume); harness rebuild from fixed sources (0 warnings/0 errors); verify_output.py `checked N files, 0 problems`; building/city--1 byte-identical to HEAD baseline in same mode + per-code Unresolved unchanged.
RENDERED (Director only): perceptual sky/fog/ambient look; no editor round this round. Later editor rounds follow 4A: check no Director-owned editor on the project first (sbox-dev.exe 70260/79524, sbox-launcher.exe 38088), never close an editor not launched by this work, NTFY before each large round.

### MASTER DISPOSITION (2026-11-26 09:05)
This file is a PLAN / HYPOTHESIS, not a gate. The worker completed without truncation; all P1-P4 pins are accepted as evidence. The Master re-verified the load-bearing counts on disk and RE-SCOPED task.md acceptance #2 (the "6 m_RenderingLayers" substring miscount -> 5 exact `m_RenderingLayers` + 1 distinct `m_RenderingLayersMask`; the literal "no SSR emitted anywhere" assertion -> code-path-only "no SSR from a URP Volume override", because the stale generated samplescene.scene already carries 2 no-op parity SSR components). The 6000.3.29f1 / 17.3.0 pin in this plan is SUPERSEDED by `6000.7.0b10 (4f3c1c5c4346)` / URP 17.7.0 with caches `com.unity.render-pipelines.universal@e9044f1f1f57` and `com.unity.render-pipelines.core@7aa87ac5532f`; see task.md `## Notes` (binding). Worker 26 stays QUESTION-ONLY until it is granted a Core round. HEAD is 0d31400.
