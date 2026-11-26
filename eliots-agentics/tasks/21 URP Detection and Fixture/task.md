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

2026-11-26 05:12 - Verdict: no further changes

1. [High] A - Materials oracle, `Editor/Core/UnityImport.cs:661` and `Editor/Core/UnityUrp.cs:25,29`. `Materials = allVmatPaths.Count` emits 217 while `Lit = 27`; only 28 materials carry a URP 17.x shader guid. Checked against task.md:11 (prescribes the exact label "N URP material(s)" and states "217 materials would drown the report") and task.md:18 (27 URP/Lit + 1 Particles/Unlit + 186 Shader Graph + 2 built-in-resource + 1 unresolved-guid = 217). The worker's "kept deliberately" rationale (02-evidence.md:8) survives the task-text check: 217 is the grounded project-material total the task asks the grouped line to carry, and the label is the task's own wording, not a worker invention. No defect. Fix: none; retain `allVmatPaths.Count` and the label. Re-counting or relabelling requires an explicit change to task.md:11 first.

2. [High] B - Acceptance evidence reproduced on disk. `logs/synty-before.log`: 0 `URP detected:` lines. `logs/synty-after2.log:4320` equivalent and `parent-verify/run.log:4320`: exactly 1 `INFO URP detected: 217 URP material(s), 27 URP Lit, 186 Shader Graph material(s), 14 URP component(s).`; `synty-after.log` is the documented exit-2 failed run, not the final run. `logs/before-report.json` vs `logs/after-report.json`: Unresolved arrays byte-equal, 31 entries each (17 unassigned-material, 13 missing-texture, 1 prefab-material-assignment); Information diff is exactly the one added URP line, 0 removed. `diff -r base_building diff_building` and `diff -r base_city diff_city` both exit 0 with empty output. Re-ran the verifier: `verify_output.py after after/imported` -> `checked 1589 files, 0 problems`, exit 0. Direct fixture census: 14 used graphs sum to 186 (58+40+26+24+13+8+5+3+3+2+1+1+1+1); 24 graph metas exist; 27 materials on `933532a4...`. Build logs before/after: `0 Warning(s) 0 Error(s)`. Fix: none.

3. [Medium] C - Guid tables and Standard/HDRP preservation, `Editor/Core/UnityHdrp.cs:87-110,142-143`, `Editor/Core/UnityMaterial.cs:87`. The URP table is checked first in `PipelineOf`; no shader guid appears in both tables (URP `933532a4...`, `0406db5a...`; HDRP `6e4ae406...`, `c4edd00f...`, `756bac90...`, `1d64af84...`, `eb607f5e...`). Non-URP guids still resolve Unknown/Hdrp as before; `?? UnityUrp.Shader` leaves `ShaderName` null for Standard and unchanged for HDRP. building/city output byte-identical (finding 2). Fix: none.

4. [Medium] D - Component count, `Editor/Core/UnityUrp.cs:113-159`, `Editor/Core/UnityImport.cs:657-660`. `File.Exists` guard at UnityUrp.cs:118; scenes come from `a.Source`, not reconstructed from `a.Path` (which loses the `Assets/` segment that caused the H1 exit-2 failure). Global Volume requires both `m_IsGlobal: 1` (:132) and a GameObject named "Global Volume" (:154). Direct scene census: cameras 1+0+4+1+1=7, lights 5, volumes 2 (both named and global) = 14. All three component guids verified against package-cache .meta files. The Volume guid `17251560...` is also in the HDRP table (same core `Volume.cs`), but the URP scan sits inside the `urpMaterialPaths.Count > 0` gate, so no HDRP double-count; no unguarded `File.ReadLines` remains. Fix: none.

5. [Low] E - Source hygiene. All four code files BOM-less; tracked files CRLF in the working tree exactly as checked out (`core.autocrlf=true`, clean filter normalizes to LF blobs, hence `--path` hashes below), new `UnityUrp.cs` LF-only. No Dummy/TODO/STUB/_Removed markers in new code; no `import_scale`/39.37 change (the hit at UnityImport.cs:875 is pre-existing); no editor/browser/CLI launch, new fixture package, new tooling or commit in this task's diff. The `.shadergraph.meta` scan at UnityImport.cs:155-163 runs ahead of the URP gate, so a non-URP import pays extra reads, but no output or Unresolved result changes (finding 2). Fix: none; if strict gate placement is later demanded, move that scan inside the `urpMaterialPaths.Count > 0` block.

Tree hashes computed with `git hash-object --path=<repo-relative> <absolute>`; `core.autocrlf=true` makes the clean filter significant, so plain `git hash-object` would be wrong. `eliots-agentics/tasks/21 URP Detection and Fixture/status.md` and `.../task.md` are SELF-REFERENTIAL SNAPSHOTS: this entry and the REVIEWED? flag rewrite both files, so their hashes below are review-time values taken during this review (task.md includes its skeleton entry) and will not match the post-write files.
Tree: Editor/Core/UnityHdrp.cs = 3c9c5d20a72f1dd31e3ef33ddc7f56930f1e44b2
Tree: Editor/Core/UnityImport.cs = b3cb153ff58392ddad196b081176993b9f8f38f8
Tree: Editor/Core/UnityMaterial.cs = 860794d8a029aa7ca480730ecc240b75a69012e2
Tree: Editor/Core/UnityUrp.cs = 56f53c2992a756c984a06c7bc21a68d805ca4d85
Tree: eliots-agentics/tasks/21 URP Detection and Fixture/status.md = 2ec6a30e1a0c423210e33ab8b17bedd39dd30854
Tree: eliots-agentics/tasks/21 URP Detection and Fixture/task.md = 8423021bad12562ab758b3a00837388efdc2f23b
Tree: eliots-agentics/tasks/21 URP Detection and Fixture/task_journal/01-plan.md = d9194d9362afc0b0dd0f3fe8726cd890b75cf8af
Tree: eliots-agentics/tasks/21 URP Detection and Fixture/task_journal/02-evidence.md = 6da6b804956ca362fa3e9e29d189a6ab576c0965

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
