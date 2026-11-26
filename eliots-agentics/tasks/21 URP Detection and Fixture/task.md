## Task
Add URP 17.x pipeline detection, guid tables and a grouped report line, gated so Standard and HDRP output stays byte-identical.

### Description
Grounding: Editor/Core/UnityHdrp.cs:9-13 declares `public enum UnityPipeline { Unknown, Hdrp }`; `PipelineOf` at :140 maps a material's shader guid to Hdrp only when it appears in the five-entry ShaderTable at :86-109. `UnityMaterial.Pipeline` is assigned once at UnityMaterial.cs:83 from the m_Shader guid read at :80. Every URP shader guid therefore resolves to Unknown and falls through the Standard path (UnityMaterial.cs:12). No URP shader or component guid table exists. Exactly two literal `Pipeline == UnityPipeline.Hdrp` gates exist (UnityImport.cs:246, UnityMaterial.cs:31).

Work:
1. Add `Urp` to UnityPipeline. Add a URP 17.x shader guid table for the package shaders used by the fixture: URP/Lit 933532a4fcc9baf4fa0491de14d08ed7 (Library/PackageCache/.../Shaders/Lit.shader.meta:2) and URP/Particles/Unlit 0406db5a14f94604a8c57ccfbc9f3b46 (Shaders/Particles/ParticlesUnlit.shader.meta:2). Detect by shader guid PLUS properties/keywords: a .unitypackage carries no .shader source for pipeline shaders, so ShaderLab tags are unavailable.
2. Add a URP component guid table: UniversalAdditionalCameraData a79441f348de89743a2939f4d699eac1, UniversalAdditionalLightData 474bcb49853aa07438625e644c072ee6, and the Global Volume script guid 172515602e62fb746b5d573b38a5fe58 (C# type UNVERIFIED; identify by the "Global Volume" GameObject and m_IsGlobal: 1).
3. Detect Shader Graph materials by resolving the shader guid to a .shadergraph.meta. They are detected and reported, never translated (UnityMaterial.cs:167 already warns shaders are approximated with complex.shader).
4. Emit ONE grouped Information line, deduped by asset path: `URP detected: N URP material(s), N URP Lit, N Shader Graph material(s), N URP component(s)`. No per-material line; 217 materials would drown the report.
5. Gate EVERY new rule on `Pipeline == Urp`. Standard and HDRP packages must remain byte-identical to a HEAD baseline in the same harness mode.

Fixture: SyntyForest contains Assets/ and ProjectSettings/ and is a valid harness DIRECTORY first argument (UnityProjectFolders.IsValidUnityFolder). Use it directly; build no new fixture package (Director 5A). Target the URP 17.x surface, not a single editor build (Director 1A nuance).

### Acceptance (one explicit pass/fail check per line)
1. BEFORE the fix, a project-folder harness run against SyntyForest reports NO URP line. Record that failing observation in task_journal/.
2. AFTER the fix, the same run reports the URP line with grounded counts: 27 URP/Lit, 1 URP/Particles/Unlit, 186 Shader Graph materials, 2 built-in-resource, 1 unresolved-guid material (187 materials carry the Shader Graph fileID); 7 UniversalAdditionalCameraData and 5 UniversalAdditionalLightData. The 14 per-graph counts sum to 186.
3. building and city--1 harness output is byte-identical to a HEAD-code baseline taken in the SAME mode (name --scenes or no-scenes). Compare the per-code Unresolved breakdown, not just totals.
4. `dotnet build Documentation/Unity-To-SBox-Automated/harness/harness.csproj -c Release -p:IUP_ROOT=<project> --no-incremental` -> 0 errors. Name which gate was run.
5. `verify_output.py <outRoot> <outRoot>/imported` -> `checked N files, 0 problems`.
6. No new Unresolved code and no growth in the six existing codes' counts.

### Work Review


## Clarification Q&A
An (Director 2026-11-26 03:57, 1A): SyntyForest on 6000.3.29f1 / URP 17.3.0 is the fixture; record the version fact and treat "Unity 6.7 URP" as this session's label, but keep the work on the URP 17.x surface rather than tying it to one editor build.
An (Director 2026-11-26 03:57, 5A): No new tooling; 5C only if research shows a real gap; do not pre-build a fixture package.

## Relevant History
See eliots-agentics/tasks/urp-support-research.md sections 1-3 for the verified census, exact commands and file:line evidence. HEAD bfc189b; session-start harness compile gate green (0 warnings, 0 errors).

## Notes
Standing constraints: visual fidelity only (Director 3A); no fixture package; every new rule gated on Pipeline == Urp; keep Editor/Core/*.cs BOM-less and LF; route new paths through UnityImport.DiskPath. Editor rounds, if any, follow the 4A conditions: check that no Director-owned editor is already open on that project, never close an editor this work did not launch, NTFY before each large round.

## Stylistic Task Notes:
References to variables like X and Y automatically imply that these are to be exposed properties for developer tweaking.
When programming, portability and DRY are a priority.

## Post-Task Follow Ups
