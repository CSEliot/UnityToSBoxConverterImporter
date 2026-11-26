# Plan - 21 URP Detection and Fixture

2026-11-26. Permission to start received from the Master in this turn's brief. Per the brief, status.md/task.md are NOT edited by this worker; evidence goes in task_journal/ only.

## Fixture/version facts (pinned, not re-derived)
- SyntyForest: `m_EditorVersion 6000.3.29f1`, `com.unity.render-pipelines.universal 17.3.0`, package cache `com.unity.render-pipelines.universal@04369b574bc4`. "Unity 6.7 URP" is the session label; the code targets the URP 17.x guid/property surface.
- SyntyForest project directory itself is the fixture (Assets/ + ProjectSettings/); no fixture package will be built.

## Order of work
1. Rebuild the harness (`dotnet build ... -c Release -p:IUP_ROOT=<project> --no-incremental`) from unmodified HEAD code, then run it against the SyntyForest DIRECTORY in a fixed harness mode and save the log. Assert NO `URP detected:` line (acceptance 1) and record the real output, exit code and counts.
2. Take a HEAD-code baseline for `building` and `city--1` in the SAME harness mode (no-scenes unless a reason appears), saving per-code Unresolved breakdown (`grep '^UNRESOLVED' ... | sort | uniq -c`) and file lists. Use a scratch mirror of HEAD Editor/Core/*.cs with LF endings and the same harness sources, per the skill's differential-ablation recipe; no new tooling in the repo.
3. Implement in `Editor/Core/*.cs`:
   - `Urp` added to `UnityPipeline`.
   - URP 17.x shader guid table (URP/Lit, URP/Particles/Unlit) and component guid table (UniversalAdditionalCameraData, UniversalAdditionalLightData, Global Volume by guid + `m_IsGlobal: 1`); detection by guid plus properties/keywords, never ShaderLab tags.
   - Shader Graph detection by resolving the material shader guid to a `.shadergraph.meta` (through `UnityImport.DiskPath`), report-only, never translated.
   - ONE grouped `Information` line, deduped by asset path: `URP detected: N URP material(s), N URP Lit, N Shader Graph material(s), N URP component(s)`.
   - Every new rule gated on `Pipeline == Urp`; Standard/HDRP paths untouched. No `import_scale`/39.37 changes. Keep BOM-less LF. Grep all call sites before touching any member.
4. Rebuild the harness from the edited code; run the same SyntyForest mode and assert the grounded counts (27 Lit + 1 Particles/Unlit + 2 built-in-resource + 1 unresolved-guid = 31 non-graph; 186 detected Shader Graph materials from 187 fileID carriers; 7 camera + 5 light components; 14 per-graph counts sum to 186).
5. Re-run `building` and `city--1` in the same mode; assert generated trees and per-code Unresolved breakdown are byte-identical to the HEAD baseline.
6. Run `verify_output.py <outRoot> <outRoot>/imported` and require `checked N files, 0 problems`. Check no new Unresolved codes and no growth in the six existing codes.
7. Write `task_journal/02-evidence.md` with actual commands, exit codes and outputs. No commit, no editor/browser/screenshot, no skill/memory/config edits, no other task folder edits.
