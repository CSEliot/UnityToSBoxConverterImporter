# Evidence - 21 URP Detection and Fixture

2026-11-26. All commands run from scratch `C:/Users/Eliot/AppData/Local/hermes/profiles/hermes-sbox/cache/scratch/urp21` (logs there). Fixture: `C:/cseliot/1-Development/4-Unity/2-Projects/SyntyForest` (6000.3.29f1, URP 17.3.0, package cache `com.unity.render-pipelines.universal@04369b574bc4`). Harness mode for EVERY run below: no `--scenes`; project runs used `--folders '*'`. No editor/browser/screenshot, no commit.

## H1-H4 falsifiers and the only defect fixed

- **H1 CONFIRMED (real defect).** First post-fix SyntyForest run (`synty-after.log`) exited 2: `error: Import failed while writing imported files for '.../SM_Veh_Steam_Car_01.prefab' ... Windows error 3: Could not find a part of the path 'C:\cseliot\1-Development\4-Unity\2-Projects\SyntyForest\scenes\samplescene.unity'`. Measurement: `UnityArchive.SafePath` strips the `Assets/` prefix from `UnityAsset.Path` (before-report.json sources are `Synty/PNB_Core/Materials/Birds_01.mat`), so `DiskPath(root, a.Path)` produced `root/scenes/samplescene.unity` (File.Exists false), whereas `DiskPath(root, "Assets/" + a.Path)` and the existing `a.Source` both resolve to the real `Assets/Scenes/SampleScene.unity` (true). Fix: scene paths use the existing `a.Source` resolver; graph meta paths use `DiskPath(root, "Assets/" + graph.Path + ".meta")`; `UnityUrp.CountComponents` skips any path that fails `File.Exists`.
- **H2 not falsified by the suggested numeric comparison, but intended.** The line emits `Materials=217` (`allVmatPaths`, all `.mat` in the URP project), not 28 (`urpMaterialPaths`, URP-shader materials). Grounding for 217: 27 URP/Lit + 1 URP/Particles/Unlit + 186 Shader Graph + 2 built-in-resource + 1 unresolved-guid = 217, matching the task's "217 materials would drown the report". Kept deliberately.
- **H3 FALSIFIED (no defect after H1 fix).** 24 `.shadergraph.meta` files resolve to guids; the corrected `Assets/`-prefixed meta path exists. Emitted `Shader Graph material(s)` is 186; direct census of the 14 used graphs below sums to 186.
- **H4 FALSIFIED (no defect).** 5 `.unity` assets are `Kind == Scene` in the fixture; component scan found 5 scenes and emitted 14 URP components.

Only these files changed for the defect fix: `Editor/Core/UnityImport.cs` (two path resolvers) and `Editor/Core/UnityUrp.cs` (File.Exists guard). No `import_scale` / 39.37 changes; all new behaviour remains gated on `Pipeline == Urp` (the URP line is emitted only when `urpMaterialPaths.Count > 0`). Byte check: `UnityHdrp.cs`, `UnityMaterial.cs`, `UnityImport.cs` BOM-less, CRLF preserved as in the checkout; new `UnityUrp.cs` BOM-less, LF-only.

## Gates (real output)

1. **Build gate, HEAD code (before):** `dotnet build .../harness/harness.csproj -c Release -p:IUP_ROOT=<project> --no-incremental` -> `Build succeeded. 0 Warning(s) 0 Error(s)`.
2. **Acceptance 1 - BEFORE fix:** harness on SyntyForest -> `unity project: ...SyntyForest`, `archive assets: 2104`, `assets=1926 converted=1617 files=2862 changed=2862`, exit 1 (pre-existing unresolved); `grep -c 'URP detected:'` -> `0`. FAILING observation recorded as required.
3. **Failed first AFTER run:** exit 2, exact error quoted under H1. Fixed as above.
4. **Build gate, fixed code:** same command -> `Build succeeded. 0 Warning(s) 0 Error(s)`.
5. **Acceptance 2 - AFTER fix:** same SyntyForest command -> exit 1 (pre-existing unresolved), `archive assets: 2104`, `assets=1926 converted=1617 files=2862 changed=2862`, exactly ONE line: `INFO URP detected: 217 URP material(s), 27 URP Lit, 186 Shader Graph material(s), 14 URP component(s).` (`grep -c 'URP detected:'` -> 1). Report shader-name census: `URP/Lit 27`, `URP/Particles/Unlit 1`, `None 217` (graphs + built-in + unresolved guid).
   - Direct `.mat`/`.meta` census: 27 Lit `933532a4...`, 1 Particles/Unlit `0406db5a...`, 2 built-in-resource `000...f000...`, 1 unresolved guid `1d8f5c5f...` = 31 non-graph; 187 materials carry a graph file id, only 186 have a resolvable graph guid.
   - 14 used graphs (counts sum 186): Generic_Decals 58, Foliage 40, Generic_ParticlesUnlit 26, Generic_Basic 24, Generic_Standard 13, Generic_ParticlesLit 8, PolygonShader 5, Clouds 3, Water 3, SkyDome 2, noiseTest 1, Refractive_Transparent 1, Skybox_Generic 1, New Shader Graph 1. 24 graph metas exist; 10 unused graphs have 0 materials.
   - Component breakdown: 7 `UniversalAdditionalCameraData` (`a79441f3...`), 5 `UniversalAdditionalLightData` (`474bcb49...`), 2 Global Volume (`17251560...`, both named GameObject "Global Volume" and `m_IsGlobal: 1`) = 14 grouped components.
6. **Acceptance 3 - differential ablation, no-scenes mode:** HEAD baseline built from the pre-edit harness; fixed-code runs into the same destination-root shape. `building`: `assets=15 converted=10 files=24 changed=24`, 24 generated files, 0 unresolved, 0 warnings, `diff -r base_building diff_building` exit 0 (empty). `city--1`: `assets=729 converted=776 files=1166 changed=1166`, 1166 generated files, 3 unresolved (2 `ambiguous-material`, 1 `prefab-material-assignment`), 3 warnings, `diff -r base_city diff_city` exit 0 (empty).
7. **Acceptance 6 - unresolved codes:** SyntyForest before/after `Unresolved` JSON arrays are byte-for-byte equal (`True`), counts 31 both sides: `unassigned-material 17`, `missing-texture 13`, `prefab-material-assignment 1`. No new code, no growth. Before/after Information diff is exactly one added line (the URP line), nothing removed.
8. **Acceptance 5 - verifier:** `python3 .../verify_output.py <SCRATCH>/after <SCRATCH>/after/imported` -> `checked 1589 files, 0 problems`.

## Remaining

No code work remains in this task. Per the worker contract, verdict/commit belong to the Master after critic review; this turn wrote only the two code fixes above and this evidence note (plus the earlier `01-plan.md`). `status.md`/`task.md` were not edited by this worker.
