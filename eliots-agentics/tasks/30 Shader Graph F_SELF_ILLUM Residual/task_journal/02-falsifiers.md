# 02-falsifiers.md - M1-M4 falsifier runs (research/scope task 30)

Research/scope turn only: no importer code edits, no git, no editor launch, no new import. All artifacts read from the task-23 AFTER tree. All observations below were produced this turn; raw census output is in `04-census-raw.txt`.

## M1 - emission branch pipeline-agnostic by design; `_EMISSION` irrelevant on that path

**Falsifier (task.md:15), both branches:** (a) show from engine/URP 17.7.0 primary source that `F_SELF_ILLUM` requires a valid `_EMISSION` keyword for Shader Graph-derived materials; OR (b) show a generated report-only `.vmat` whose source has `_EMISSION` OFF and NO `F_SELF_ILLUM`.

**Runs (commands / primary-source lines):**

1. `search_files pattern="F_SELF_ILLUM", path=C:/cseliot/1-Development/1-SBox/1-Engine-Builds/Dev/sbox-public` -> only 2 hits:
   - `game/addons/tools/Code/Editor/AssetBrowser/Material/MaterialMenu.cs:145` `texSelfIllum = $"\n\tF_SELF_ILLUM 1\n\tTextureSelfIllumMask \"{texSelfIllum}\"";`
   - `game/core/stringtokendatabase.txt:114` `%$SR@PF_SELF_ILLUM`
   No shader-source hit for `F_SELF_ILLUM` exists in this checkout.
2. `read_file engine/Tests/Sandbox.Test.Integration/Engine/SharedShaderStorage.cs:29-32` -> `material.SetFeature( feature, value );` / `Assert.AreEqual( value, material.GetFeature( feature ) );` (features are direct material flags; no keyword step).
3. `search_files pattern="_EMISSION", file_glob="*.hlsl", path=URP@e9044f1f1f57` -> `Shaders/Utils/Emission.hlsl:26-49`:
   `bool UseEmission() { #if defined(_EMISSION_KEYWORD_DECLARED) return _EMISSION; #elif DEFINED_NONZERO(_EMISSION) return true; #else return false; #endif }` and `SampleEmission(uv, useEmission)` returns `half3(0,0,0)` when false. This gates URP's own shaders, not s&box's `F_SELF_ILLUM`.
4. `python3 .../scratch/task30_census.py` (output saved `04-census-raw.txt`) -> `TOTAL .mat scanned: 217 ; ShaderGraph-sourced: 186 ; emission texture assigned: 4`; `ALL generated .vmat with F_SELF_ILLUM ... COUNT = 2`; of the 4 emission-textured sources, exactly 2 are ShaderGraph (both fire, both with `F_SELF_ILLUM=True`), the other 2 are URP Lit (`Mat_Rock_01`, `Mat_Rock_Moss_01`, both `F_SELF_ILLUM=False`).

**Produced observation:** Branch (a) - no primary source makes s&box `F_SELF_ILLUM` depend on `_EMISSION`; the engine sets `F_` flags directly (`SharedShaderStorage.cs:29`) and its own material tool writes `F_SELF_ILLUM 1` from a texture alone (`MaterialMenu.cs:142-145`). URP's keyword gate is a different shader system. Branch (b) - no report-only ShaderGraph counterexample exists in this fixture: every other ShaderGraph source lacks an assigned `_EmissionMap`, so there is no second case to compare. The two non-ShaderGraph emission-textured sources ARE keyword-gated (URP), but they are not report-only Shader Graph, so they do not satisfy the named falsifier.

**Verdict on M1:** NOT FALSIFIED - survives as the *mechanism* (Unknown-pipeline emission texture -> `F_SELF_ILLUM` with no keyword check; `UnityMaterial.cs:345,349,354`). Its "by design" wording is only weakly supported: the design intent shown in engine source is texture-presence-driven for ALL materials, not a Shader Graph-specific decision.

## M2 - real symptom mask: nonzero `_EmissionColor` + no keyword fabricates illumination

**Falsifier (task.md:16), both branches:** show the two generated `.vmat` are never observed by the Director (report-only, no rendered frame), OR show `F_SELF_ILLUM` is inert without the keyword in the engine's complex shader.

**Runs / primary-source lines:**
1. `read_file .../urp23/after/.../gen_fireframes_grey_01.vmat:11,43-50` -> `F_SELF_ILLUM 1`, `g_flSelfIllumBrightness "2.000"`, `g_vSelfIllumTint "[1.000000 0.476122 0.000000 1.000000]"`, `TextureSelfIllumMask "imported/.../gen_fireframes_grey_01.png"`. Secondary `.vmat` read confirms the same structure.
2. `search_files pattern="(?i)self.?illum", file_glob="*.shader", path=sbox-public/game/core/shaders` -> 0 hits; complex shader source is not in this checkout. Engine-side evidence is the material feature API above (`SharedShaderStorage.cs:29-32`) and `MaterialMenu.cs:142-145` (texture -> `F_SELF_ILLUM 1` + `TextureSelfIllumMask`). No keyword gate exists anywhere in the s&box material path.
3. Source-side primary lines (`search_files pattern="_Enable_Emission|_Emission_Map|_Emission_Color|m_LightmapFlags", file_glob="Gen_FireFrames*.mat"`):
   - `Gen_FireFrames_Large_01.mat:47 m_LightmapFlags: 10`, `:85 - _Emission_Map:` with `:86 m_Texture: {fileID: 0}`, `:160 - _Enable_Emission: 0`, `:201 - _Emission_Color: {r: 0, g: 0, b: 0, a: 0}`.
   - `Gen_FireFrames_01.mat:20 m_LightmapFlags: 10` (ParticlesUnlit graph, no emission node).
   - `search_files pattern="(?i)emission|emissive|_EMISSION", path=Generic_ParticlesUnlit.shadergraph` -> 0 hits; `m_Keywords: []` at `:46`. ParticlesLit graph declares `Enable Emission` -> `_Enable_Emission` at `:4374-4377` and `Emission Map` -> `_Emission_Map` at `:4403-4406`; the source sets both OFF/empty.
4. `read_file task_journal/01-creation.md` + `task.md ## Notes` -> the only recorded symptom evidence is headless `.vmat` text; no rendered frame exists in this task's evidence and the notes explicitly forbid inventing one.

**Produced observation:** Branch 1 - "never observed" cannot be proved from disk; report-only status is a scope statement (Director 08:10 / task 26), not proof of zero observation. No rendered frame was produced or reviewed this turn. Branch 2 - FALSIFIER FAILS: `F_SELF_ILLUM` is an active material feature toggled by `SetFeature`, and the generated `.vmat` supplies a non-empty mask, brightness 2.0 and a nonzero tint. It is not inert. Relative to the Unity source (graph emission disabled, URP `_EMISSION` invalid, `UseEmission()` false -> `SampleEmission` black), the s&box output has illumination the source does not have.

**Verdict on M2:** NOT FALSIFIED - the pathway fabricates active self-illumination, an objective, headless-checkable data defect. Caveat carried forward: no rendered frame exists, so the *visual* symptom in s&box is unconfirmed; the defect claim rests on engine feature semantics + source comparison, not on a screenshot.

## M3 - keyword gate should be "known pipeline" rather than `Pipeline == Urp`

**Falsifier (task.md:17):** show broadening changes Standard/HDRP output byte-for-byte on `building`/`city--1` (forbidden), OR show a Standard/HDRP material with `_EMISSION` OFF that legitimately needs `F_SELF_ILLUM`.

**Runs / primary-source lines:**
1. `python3 .../scratch/task30_census.py` -> `URP table guids: 5; HDRP table guids: 12; ShaderGraph guids in Assets: 24`; `fire shader guids in URP table? False ; in HDRP table? False ; in ShaderGraph set? True`. `UnityHdrp.cs:142-143`: `PipelineOf(shaderGuid) => UnityUrp.Shader(guid) != null ? Urp : Shader(guid) == null ? Unknown : Hdrp`.
2. Same census -> `All sources with emission texture AND _EMISSION not valid ... Urp: 2` (both `F_SELF_ILLUM=False`), `Hdrp: 0`, `Unknown: 2` (the two fire ShaderGraph sources, both `F_SELF_ILLUM=True`). Whole AFTER tree `COUNT = 2` for `F_SELF_ILLUM`.
3. The named byte-for-byte `building`/`city--1` diff was NOT RUN this turn: it needs a code change and a fresh import, both banned in this research phase and owned by a sibling worker's harness. Recorded as a code-phase gate, not as an observation.

**Produced observation:** Zero HDRP materials and zero Standard materials in the fixture have an emission texture with `_EMISSION` not valid; no material legitimately needing HDRP/Standard `F_SELF_ILLUM` was found. The two fire sources are Unknown-pipeline ShaderGraph, so NEITHER the current `Pipeline == Urp` gate nor the proposed "known pipeline" gate can reach them.

**Verdict on M3:** NOT FALSIFIED as a general rule (the byte-for-byte check was not run), but IRRELEVANT to this symptom: no broadening of the gate from Urp to known-pipeline can remove the residual on the two Unknown fire materials. Code-phase acceptance still requires the byte-for-byte proof if any gate change is made.

## M4 - correct as-is: Shader Graph serialized keywords are not authoritative

**Falsifier (task.md:18):** show that Unity 6.7 / URP 17.7.0 `BaseShaderGUI`/`Emission.hlsl` treats `m_InvalidKeywords: - _EMISSION` as authoritative even for Shader Graph materials.

**Runs / primary-source lines:**
1. `read_file URP@e9044f1f1f57/Editor/ShaderGUI/BaseShaderGUI.cs:1458-1469` -> `if (material.HasProperty(Property.EmissionColor)) MaterialEditor.FixupEmissiveFlag(material);`, `:1462 bool shouldEmissionBeEnabled = (material.globalIlluminationFlags & MaterialGlobalIlluminationFlags.RealtimeDirectEmission) != 0;`, `:1469 CoreUtils.SetKeyword(material, ShaderKeywordStrings._EMISSION, shouldEmissionBeEnabled);` - the keyword is RECOMPUTED from GI flags, not read from the serialized valid/invalid keyword lists. `UpdateMaterialSurfaceOptions` begins at `:1236`, so `:1462` is inside the shared path.
2. `read_file .../Editor/ShaderGUI/ShaderGraphLitGUI.cs:9-10,34-41,88` -> `class ShaderGraphLitGUI : BaseShaderGUI` and `BaseShaderGUI.UpdateMaterialSurfaceOptions(material, automaticRenderQueue);` at `:41`; `:88 // ignore emission color for shadergraphs, because shadergraphs don't have a hard-coded emission property, it's up to the user`. Primary source that Shader Graph emission is graph-defined, not a serialized built-in property.
3. `read_file URP@e9044f1f1f57/Shaders/Utils/Emission.hlsl:26-49` -> `UseEmission()` false without `_EMISSION`, `SampleEmission` returns black - the shader consumes the keyword the GUI computes, not the YAML keyword list.
4. Source graph checks in M2 run 3: `Generic_ParticlesUnlit.shadergraph` has `m_Keywords: []` and zero emission references; `Generic_ParticlesLit.shadergraph` declares `_Enable_Emission` / `_Emission_Map`, and the fire-large `.mat` sets `_Enable_Emission: 0` and `_Emission_Map` fileID 0.

**Produced observation:** The falsifier FAILS. URP does not treat `m_InvalidKeywords` as authoritative: `BaseShaderGUI` overwrites `_EMISSION` from GI flags and ShaderGraph's own GUI documents that emission is user/graph-defined. The serialized `_EmissionMap`/`_EmissionColor` the importer reads at `UnityMaterial.cs:345` are NOT the graph's emission inputs (the graph uses `_Emission_Map`/`_Enable_Emission`, both OFF/empty in the source).

**Verdict on M4:** NOT FALSIFIED on the narrow claim "serialized keywords/properties are not authoritative for Shader Graph", but that claim does NOT make the generated `.vmat` correct: the importer builds `F_SELF_ILLUM` from a non-graph property the graph ignores, so the narrow claim explains the residual without excusing it.

## Cross-mechanism summary (recorded for 03-verdict.md)

Surviving mechanism = M1 (pipeline-agnostic texture->`F_SELF_ILLUM` path on Unknown pipeline) + M2 (active fabricated illumination) + M4's explanation of why the source keyword is not authoritative. M3 cannot reach the two materials. No rendered frame exists; no Director scope decision has been made this turn.
