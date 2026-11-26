# 03-verdict.md - task 30 verdict (research/scope phase, 2026-11-26)

## VERDICT: REAL DEFECT (objective data defect in generated `.vmat`) - remediation needs a Director scope decision, so HOLD.

This is a single explicit verdict on the generated data. It is NOT a permission to edit code: fixing it requires widening the emission keyword gate beyond the Director-approved `Pipeline == Urp` condition and touching report-only Shader Graph output, which needs Director approval (task.md acceptance #6, Master's instruction this turn). No code, git, editor or new import was performed.

## Evidence chain

1. **Symptom (pre-existing, not a task-23 regression).** `task_journal/01-creation.md` records exactly two generated `.vmat` with `F_SELF_ILLUM`: `urp23/after/imported/synty/polygongeneric/materials/fx/fire/gen_fireframes_grey_01.vmat` and `.../gen_fireframes_large_grey_01.vmat`. Both Unity sources carry `- _EMISSION` in `m_ValidKeywords`-adjacent `m_InvalidKeywords` (OFF), no `m_ShaderKeywords`. Re-run this turn: `search_files pattern="F_SELF_ILLUM", file_glob="*.vmat", path=.../urp23/after/imported` -> `total_count: 2`, the same two paths; `04-census-raw.txt` reproduces the count.
2. **Why it survives.** `Editor/Core/UnityMaterial.cs:345` reads `_EmissionMap` with no shader-graph awareness; `:349` applies the keyword gate only when `Urp`; `:354` emits `F_SELF_ILLUM` from texture presence alone. `Editor/Core/UnityHdrp.cs:142-143` maps both fire shader guids to `Unknown` (URP table check `UnityUrp.cs:53-69`, HDRP table `UnityHdrp.cs`); census confirms both guids are ShaderGraph `Generic_ParticlesUnlit`/`Generic_ParticlesLit` and in neither pipeline table. So the URP-only gate cannot reach them.
3. **The Unity source does not have emission.** `URP@e9044f1f1f57/Shaders/Utils/Emission.hlsl:26-49`: `UseEmission()` is false without `_EMISSION` and `SampleEmission()` returns `half3(0,0,0)`. `Gen_FireFrames_Large_01.mat:160 - _Enable_Emission: 0`, `:201 - _Emission_Color: {r: 0, g: 0, b: 0, a: 0}`, `:85-86 _Emission_Map` fileID 0. `Generic_ParticlesUnlit.shadergraph:46 m_Keywords: []` and zero emission references; `Generic_ParticlesLit.shadergraph:4374-4377` declares graph property `_Enable_Emission`, `:4403-4406` `_Emission_Map`. `ShaderGraphLitGUI.cs:88`: "ignore emission color for shadergraphs, because shadergraphs don't have a hard-coded emission property, it's up to the user". The `_EmissionMap` the importer reads is a built-in leftover also used as the colour map (`:58-60` of `Gen_FireFrames_01.mat`), not the graph's emission input.
4. **The s&box output does have emission.** `gen_fireframes_grey_01.vmat:11 F_SELF_ILLUM 1`, `:45 g_flSelfIllumBrightness "2.000"`, `:49 g_vSelfIllumTint "[1.000000 0.476122 0.000000 1.000000]"`, `:50 TextureSelfIllumMask "<fire texture>"`. Engine primary sources: `SharedShaderStorage.cs:29-32 material.SetFeature(feature, value)` (F_ flags are direct material features, no keyword step) and `MaterialMenu.cs:142-145` (emission-like texture -> `F_SELF_ILLUM 1` + `TextureSelfIllumMask`). `F_SELF_ILLUM` is therefore ACTIVE in the generated material, not inert.
5. **Mechanism selection.** M1 survives as the mechanism (pipeline-agnostic texture -> feature on Unknown pipeline), M2 survives as the consequence (fabricated active illumination), M4 explains why the serialized `_EMISSION` state does not drive the graph; M3 cannot reach these Unknown-pipeline materials. Every named falsifier was RUN and recorded in `02-falsifiers.md` with its command/primary-source line and produced observation.
6. **Caveat that could falsify the verdict.** (a) No rendered frame exists in this task's evidence; the visual symptom in s&box is unconfirmed and none was fabricated. The defect claim rests on engine feature semantics plus source comparison, which is headless-checkable; if the Director defines report-only Shader Graph output as explicitly exempt from source fidelity, the classification collapses to correct-as-is. (b) `complex.shader` source is not present in the `sbox-public` checkout, so `F_SELF_ILLUM`'s activity is inferred from the engine's feature API and the generated values, not from the shader body itself. (c) M3's byte-for-byte `building`/`city--1` check was not run (code/import banned this turn); it remains a code-phase gate.
7. **Standing constraints observed.** Shader Graph materials remain report-only; no new mapping exposed, no new tooling, no BIRP requirement found during the census (nothing BIRP was found NECESSARY for SyntyForest), no git writes, no editor launch, no Signal/ntfy, status.md header untouched (parent-owned). The two URP materials that ARE keyword-gated (`Mat_Rock_01`, `Mat_Rock_Moss_01`) correctly have no `F_SELF_ILLUM`; Standard/HDRP output was not modified.

## Clarifying question for the Director (HOLD - do not decide alone)

**Q:** The residual is real in the generated data, but removing it requires suppressing `F_SELF_ILLUM` for Unknown-pipeline, report-only Shader Graph materials. Does the Director approve widening the emission gate beyond `Pipeline == Urp` for this case?

- **Option A - Approve:** in the code phase, gate emission on the keyword/graph state for Unknown-pipeline Shader Graph sources too (e.g. `_EMISSION` invalid OR graph `_Enable_Emission: 0`), keeping Standard/HDRP output byte-identical and proving it on `building`/`city--1` with compile 0 errors, `verify_output.py <outRoot> <outRoot>/imported` 0 problems, and Unresolved 13/1/17 unchanged.
- **Option B - Decline:** keep all report-only Shader Graph materials untouched; reclassify the residual as correct-as-is within scope and close with no code change (task.md acceptance #5).
- **Option C - Report only:** no gate change; document the residual as a known report-only fidelity gap and close.

## Post-Task Follow Up census (requested if verdict is real defect; census only, no patch)

Ran `python3 C:/Users/Eliot/AppData/Local/hermes/profiles/hermes-sbox/cache/scratch/task30_census.py` over 217 source `.mat` files and the whole `urp23/after/imported` tree (raw output: `04-census-raw.txt`):

- Shader Graph-sourced source materials: **186**.
- Source materials with an assigned `_EmissionMap`: **4** - the two fire FX (Unknown pipeline, ShaderGraph, `_EMISSION` invalid, both with `F_SELF_ILLUM`) and two URP Lit terrain materials (`Mat_Rock_01`, `Mat_Rock_Moss_01`, both gated, no `F_SELF_ILLUM`).
- Generated `.vmat` with `F_SELF_ILLUM` in the entire AFTER tree: **2** (the two fire FX).
- **Additional report-only Shader Graph materials affected beyond the two fire FX: 0** in this fixture. The same pipeline-agnostic path is present in code but has no other material to act on because the other 184 Shader Graph sources have no assigned `_EmissionMap`.

## Files written this turn

- `task_journal/02-falsifiers.md` - four falsifier runs with commands/primary-source lines, produced observations, per-mechanism verdicts.
- `task_journal/03-verdict.md` - this file.
- `task_journal/04-census-raw.txt` - raw census output (evidence backing M1/M3 and the follow-up census).
