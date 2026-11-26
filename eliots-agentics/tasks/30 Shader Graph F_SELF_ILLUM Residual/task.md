## Task
Decide whether the pre-existing `F_SELF_ILLUM` on two report-only Shader Graph fire FX materials is a real defect, and fix it only if the evidence and the Director's scope allow it.

### Description
Opened by Director 2026-11-26 (Option A turn): "And yes open a separate task for the pre-existing Shader Graph F_SELF_ILLUM residual." This is a SYMPTOM task. It begins from a headless, objective observation, not from a spec to confirm.

PARENT-VERIFIED ON DISK (2026-11-26 23:5x, sequence order; re-verify before acting):
- Two generated `.vmat` contain `F_SELF_ILLUM`: `.../urp23/after/imported/synty/polygongeneric/materials/fx/fire/gen_fireframes_grey_01.vmat` and `gen_fireframes_large_grey_01.vmat`.
- Their Unity sources: `C:/cseliot/1-Development/4-Unity/2-Projects/SyntyForest/Assets/Synty/PolygonGeneric/Materials/FX/Fire/Gen_FireFrames_01.mat` (`:11` shader guid `0736e099ec10c9e46b9551b2337d0cc7`, `:14` `m_ValidKeywords`, `:18` `m_InvalidKeywords`, `:19` `- _EMISSION`, `:20` `m_LightmapFlags: 10`, `:54` `_EmissionMap`, `:160` `_EmissionColor` r:2 g:0.9522445 b:0) and `Gen_FireFrames_Large_01.mat` (`:37` guid `dfec08fb273e4674bb5398df25a5932c`, `:40` valid, `:45` invalid, `:46` `- _EMISSION`, `:47` lightmapFlags 10, `:81` `_EmissionMap`, `:200` `_EmissionColor` r:1.6837997 g:0.73922914 b:0). So `_EMISSION` is explicitly OFF (`m_InvalidKeywords`), not ON, and there is no `m_ShaderKeywords` line.
- PRE-EXISTING at HEAD: `git show HEAD:Editor/Core/UnityMaterial.cs:280` is `if ( emission != null ) Feature( "F_SELF_ILLUM 1" );` with NO keyword check. So this is NOT a task 23 regression and NOT caused by the 6.7 upgrade.
- Current tree: `Editor/Core/UnityMaterial.cs:349` gates emission with `if ( Urp && emission != null && !HasValidKeyword( "_EMISSION" ) ) emission = null;` and `:354` emits `F_SELF_ILLUM`. Both fire FX shader guids are in NEITHER pipeline table (`Editor/Core/UnityUrp.cs:57-61`, `Editor/Core/UnityHdrp.cs:142-143`), so `PipelineOf` returns Unknown, `Urp` is false, and the URP-only gate never applies. Hence the residual survives.
- Standing constraints: gate EVERY new rule on `Pipeline == Urp`; keep `Editor/Core/*.cs` BOM-less and LF; route new paths through `UnityImport.DiskPath`; expose every NEW MAPPING as a developer-tweakable property; Standard and HDRP output unchanged; "keep Shader Graph materials report-only per task 26" (Director 08:10). BIRP is OUT OF SCOPE unless a BIRP-related thing is found NECESSARY for SyntyForest to import properly (Director 23:17) - if you find such a thing, REPORT it, do not silently drop it. Canonical target Unity 6.7 = 6000.7.0b10 (4f3c1c5c4346) / URP 17.7.0, cache `com.unity.render-pipelines.universal@e9044f1f1f57`. Current fixture has FOUR `.unity` scenes and 7 cameras. No new fixture package, no new tooling, no editor/sbox launch, no git add/commit/push.

Candidate mechanisms (each needs a named falsifier that must actually be RUN and recorded; a mechanism without a run falsifier is a guess, not a hypothesis):
1. M1 - The emission branch is pipeline-agnostic by design and `F_SELF_ILLUM` is intentionally emitted for ANY material with an emission texture, including report-only Shader Graph ones; the `_EMISSION` keyword is irrelevant to that path. Falsifier: show from engine/URP 17.7.0 primary source that `F_SELF_ILLUM` requires a valid `_EMISSION` keyword for Shader Graph-derived materials, OR show a generated report-only `.vmat` whose source has `_EMISSION` OFF and NO `F_SELF_ILLUM`.
2. M2 - The residual is a real symptom mask: emitting `F_SELF_ILLUM` from a nonzero `_EmissionColor` without a valid keyword fabricates illumination. Falsifier: show that the two fire FX materials' generated `.vmat` are never observed by the Director (report-only, no rendered frame), OR show that `F_SELF_ILLUM` is inert without the keyword in the engine's complex shader.
3. M3 - The keyword gate should not be `Pipeline == Urp` but "any material whose shader guid resolves to a known pipeline", i.e. the gate is too narrow. Falsifier: show that broadening it changes Standard/HDRP output byte-for-byte on `building`/`city--1` (forbidden by the standing constraint), or show a Standard/HDRP material with `_EMISSION` OFF that legitimately needs `F_SELF_ILLUM`.
4. M4 - It is correct as-is because Shader Graph materials are report-only and their serialized keywords are not authoritative (the graph itself defines emission). Falsifier: show that Unity 6.7 URP 17.7.0 `BaseShaderGUI`/`Emission.hlsl` treats `m_InvalidKeywords: - _EMISSION` as authoritative even for Shader Graph materials.

### Acceptance (one explicit pass/fail check per line)
1. BEFORE observation recorded in `task_journal/01-creation.md` with the exact two generated `.vmat` paths, the two source `.mat` keyword lines, and the HEAD line proving pre-existence. This is the FAILING assertion: 2 generated `.vmat` contain `F_SELF_ILLUM` while `_EMISSION` is OFF.
2. Each of M1-M4 has its named falsifier RUN and the produced observation recorded on disk with the command or primary-source line. No "investigated"/"rules out" conclusion without the produced observation.
3. A single explicit verdict with the evidence chain: real defect / correct-as-is / cannot determine, naming the mechanism that survives and the caveat that could falsify it.
4. If real defect AND the Director's scope permits: fix it with `Pipeline == Urp` (or a Director-approved broader gate), keep Shader Graph materials report-only, keep `Editor/Core/*.cs` BOM-less and LF, add NO fabricated value; prove Standard/HDRP output unchanged byte-for-byte on `building`/`city--1`, harness compile 0 errors, `verify_output.py <outRoot> <outRoot>/imported` -> 0 problems, per-code Unresolved 13/1/17 unchanged.
5. If correct-as-is: close with `DONE? YES!` and record that the residual is pre-existing at HEAD and out of task 23's URP-only scope; no code change.
6. If the verdict needs a Director scope decision (e.g. broadening the gate, or touching report-only Shader Graph materials), record it as a clarifying question and HOLD; do not decide alone.

### Work Review

## Clarification Q&A
An (Director 2026-11-26 22:11 chat / 23:59 sequence order, verbatim): "Option A. And yes open a separate task for the pre-existing Shader Graph F_SELF_ILLUM residual." Full text of the constraint carried in: "keep Shader Graph materials report-only per task 26".

An (Director 2026-11-27 00:46, chat, verbatim): "1A, 2A". This task is the Q2 answer: the Director chose option 2A. The text of option 2A AS POSED BY THE MASTER (not a Director quote) was: "approve the gate widening for the two report-only Shader Graph fire FX materials, scoped to suppressing a fabricated F_SELF_ILLUM where no valid emission keyword exists. This is not a conversion to URP/Lit; the report-only stance stays. Prove building/city byte-identical and the per-code Unresolved counts unchanged."
CONSEQUENCE: the code phase is APPROVED under Option A's conditions but stays QUEUED behind task 25, because task 25's worker owns the shared harness build/gate this round (one gate owner at a time; concurrent builds corrupt the output dir). Scope: suppress F_SELF_ILLUM only where no valid emission keyword exists; no Shader Graph material becomes a URP/Lit conversion; report-only stance unchanged.

## Relevant History
Task 23 `### Work Review` finding 5 (2026-11-26 21:13, deleg_02a4d048) - F_SELF_ILLUM residual correctly out of scope for task 23; parent corrections at session-latest.md 23:55 and 23:59. `eliots-agentics/tasks/23 URP Lit PBR and Emission Fidelity/task_journal/04-implementation-and-schema.md` holds the corrected keyword-state record. `eliots-agentics/tasks/urp-support-research.md` and `eliots-agentics/tasks/terrain-import-research.md` hold the version census. HEAD de1f01e.

## Notes
Parent decision (2026-11-26): this task is research/scope FIRST, code LAST. The 4A serial sequence (worker 23 on 6.7, then 24/25/26 serially) means no worker is dispatched for this task yet; it is queued. Do not fabricate a fixture, a rendered frame, or a keyword value. A visual symptom needs a rendered frame someone looked at; if none exists, say so rather than inferring from serialized JSON.

## Stylistic Task Notes:
References to variables like X and Y automatically imply that these are to be exposed properties for developer tweaking.
When programming, portability and DRY are a priority.

## Post-Task Follow Ups
If verdict is real defect, note whether the same pipeline-agnostic emission path affects other report-only Shader Graph materials beyond these two fire FX assets (census, not patch).
ADDRESSED 2026-11-27 00:09 (research phase, sa-1-bce5b58c): census run over 217 source .mat and the whole urp23/after/imported tree (task_journal/04-census-raw.txt): 186 Shader Graph sources, 4 with an assigned _EmissionMap (the 2 fire FX + the 2 already-gated URP/Lit rocks), 2 generated .vmat with F_SELF_ILLUM total, 0 additional report-only Shader Graph materials affected. Verdict and Director question: task_journal/03-verdict.md.
