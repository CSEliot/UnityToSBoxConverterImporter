# Plan — 22 URP Lit Surface Semantics (2026-11-26)

Status: PERMISSION received in this turn's brief. Header flags in `status.md` are Master-owned for this round; this worker writes only `task_journal/` notes and `Editor/Core/*.cs`. No git commands, no status/task edits, no critic, no editor/browser launch.

## Pinned facts and scope

- Fixture: SyntyForest on Unity `6000.3.29f1` / URP `17.3.0` (`Library/PackageCache/com.unity.render-pipelines.universal@04369b574bc4`). "Unity 6.7 URP" is the session label, not an installed editor. Target the URP 17.x Lit surface; do not tie code to one build.
- HEAD `69056f0` closes task 21: `UnityPipeline` has `Unknown/Hdrp/Urp`, `Pipeline == Urp` is already available, keywords are parsed, `UnityUrp.cs` holds the guid tables/counts. This task consumes those hooks; it must not re-derive the census or change detection.
- Visual fidelity only. All new behaviour gated on `Pipeline == Urp`; Standard/HDRP output must stay byte-identical.

## Order of work

1. **Failing assertions FIRST.** Before any source edit, build the harness from current HEAD bytes and run SyntyForest in no-scenes `--folders '*'` mode into a scratch output root. Pick four concrete SyntyForest materials from the literal observed values: (a) `_ALPHATEST_ON` with no `_AlphaClip` scalar, (b) `_SURFACE_TYPE_TRANSPARENT` / `_Surface: 1`, (c) `_NORMALMAP` + `_BumpMap`, (d) `_EmissionColor` with no `_EmissionMap`. Write a check script *in scratch* (not in the repo) that asserts, from the generated `.vmat`: `F_ALPHA_TEST 1` + finite `g_flAlphaTestReference`; a translucent/glass material; `TextureNormal` + non-default `g_flNormalMapScaleFactor`; `F_SELF_ILLUM` + `g_vSelfIllumTint`. Run it against current output and record the four FAIL lines verbatim in `02-failing-assertions.md`. No assertion may be written from our own conversion formula.
2. **Ground `_EMISSION`.** Inspect URP 17 `Shaders/Lit.shader` / `ShaderLibrary` source in the fixture package cache to determine whether `m_InvalidKeywords` `_EMISSION` can be active. Only treat it as an enable signal where the source supports it; otherwise implement the colour-driven rule (`_EmissionColor` present, no emission texture) and record the keyword caveat explicitly. Never fabricate an emission texture.
3. **Implement** in `Editor/Core/UnityMaterial.cs` (and only other `Editor/Core/*.cs` if unavoidable), all under `Pipeline == Urp`:
   - `_ALPHATEST_ON` / `_AlphaClip` -> `F_ALPHA_TEST` + `g_flAlphaTestReference` (finite default when the scalar is absent).
   - `_SURFACE_TYPE_TRANSPARENT` / `_Surface` 0/1 -> `F_TRANSLUCENT` and glass path selection at current `:249-250`, without changing Standard's legacy `_Surface == 1` behaviour.
   - `_NORMALMAP` -> `TextureNormal` + `g_flNormalMapScaleFactor` from `_BumpScale`.
   - `_EMISSION` + `_EmissionColor` with no emission texture -> `F_SELF_ILLUM` + `g_vSelfIllumTint`; a NEW MAPPING (no recovered texture), labelled as such in the report.
   - Expose every NEW MAPPING default/knob as a developer-tweakable property on the existing converter settings type; reuse existing properties where they already serve the same value. Route any new path through `UnityImport.DiskPath`. Keep files BOM-less, LF, tree compiling. Grep call sites before removing or renaming any member.
4. **Gates, in order** (each with real output recorded in `task_journal/evidence.md`):
   1. harness compile `dotnet build Documentation/Unity-To-SBox-Automated/harness/harness.csproj -c Release -p:IUP_ROOT=<project> --no-incremental` -> 0 errors (name this gate explicitly).
   2. Rebuild from fixed code; SyntyForest `--folders '*'` no-scenes run; assert the report line is unchanged (`URP detected: 217 URP material(s), 27 URP Lit, 186 Shader Graph material(s), 14 URP component(s).`) and that all four chosen `.vmat` cases now hold.
   3. building and city--1 no-scenes runs on fixed code; compare against the HEAD-baseline artifacts in the same mode (24 files / 0 unresolved; 1166 files / 3 unresolved) with per-code `^UNRESOLVED` breakdown and a byte diff of generated trees. Baseline is built from HEAD sources in a scratch mirror with identical LF EOL if no existing baseline artifacts are present.
   4. `verify_output.py <outRoot> <outRoot>/imported` -> `checked N files, 0 problems` for each output root.
   5. Confirm no new Unresolved code and no growth in the six existing codes.
5. Write the evidence note with pass/fail per acceptance line, NEW MAPPING labels, limitations, and files changed. Do not commit; do not edit any other task folder.

## Risks / rules

- Harness exits 1 on unresolved issues: treat as data.
- Report JSON's `Destination`/timings vary; compare generated trees and per-code breakdown, not those fields.
- Output root must be the parent of `imported/` for `verify_output.py`.
- If the cap approaches, stop cleanly with a compiling tree rather than ship a half-edited file.
