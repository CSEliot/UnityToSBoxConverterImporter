## Status: STARTED? YES! CLARIFIED? YES! HISTORY? YES! REVIEWED? YES! DONE? YES! WORKER: sa-0-95851fd2 OTHER: reviewed 02:55 no further changes; CLI committed

## Task
Build terminal access for this tool so LLMs can utilize it and test it.

### Description
Is that called a cli? We just need cmd/terminal i/o. Nothing fancy, but should be verbose by default and -h option for "Human-Readable" where verbosity is reduced.
Make sure to reuse existing code here where possible, and refactor code rather than make new code when possible.

### Work Review

2026-11-22 02:55 - Verdict: no further changes
Tree:
  git hash-object --path="Documentation/Unity-To-SBox-Automated/harness/Program.cs" "Documentation/Unity-To-SBox-Automated/harness/Program.cs" = f59c7288331c5346575801ae0c02af3e7b54c804
Verification (independent, on reviewed bytes; HEAD 124edad; worker sa-0-95851fd2):
- Build: `IUP_ROOT=... dotnet build harness/harness.csproj -c Release` -> Build succeeded, 0 Warning(s), 0 Error(s), exit 0. HEAD source rebuilt in scratch: same 0/0.
- `Program.cs:20-45` exposes every knob (`ScenesFlag`, `HumanFlags`, `HelpFlags`, `DefaultReportFile`, `Usage`, asset/issue limits for both modes, `VerboseProgress`/`HumanProgress`, `Limit()`, `Progress()`); `Program.cs:47,52` default to `Verbosity.Verbose`; `Program.cs:135,148-151` consume `Limit(...)`; `Program.cs:120` genuinely consumes `CliSettings.Progress(cmd.Verbosity)` in the `Progress<T>` callback. Progress booleans are exposed AND used.
- `Program.cs:58-96`: `--help` handled before parse (60-64), flags accepted in any order (80-86), positional contract `[--scenes] <package> <assetsDir> [report.json]` intact (87-94), unknown dash-args rejected (84).
- `Program.cs:132-154`: one shared formatter; human-only counts/digest at 140-146; verbose caps are `int.MaxValue` so every asset/issue prints; progress defaults false (37-38) so no extra console lines.
- Byte-identity: HEAD's legacy `Program.cs` compiled in scratch and run on `fixtures/scene-loss.unitypackage` with the same destination string; stdout `cmp` exit 0, both 808 bytes / 10 lines. New default vs `-h` on that fixture/destination: 10 vs 7 lines, the 5 per-asset lines replaced by a 2-line count/digest; issue detail lines identical; both exit 1 (1 unresolved). `--human` matches `-h`.
- Report invariance: destination held constant -> `cmp` exit 0, both sha256 `61fc6ee5d5792b0a0607d887d6d7d50d0fba9f4373197b1a0abc2c15a0e536fa` (re-checked on final bytes; worker's cmp-0 claim confirmed). Different destinations -> diff is `Destination` (caller-supplied) plus `TextureProcessing.ElapsedMilliseconds` (run-to-run timing; 0 on this fixture, 870 vs 901 in the parent's texture-bearing run). Neither is a mode effect; mode invariance stands.
- Legacy regression: `Testing/building.unitypackage`, no flags -> `archive assets: 31`, `assets=15 converted=10 files=24 changed=24`, 0 UNRESOLVED / 0 WARN, exit 0. (Notes' "9 INFO" reflects the worker's earlier log; current working-tree Core gives 12 INFO -- a sibling task's Core edit, explicitly out of scope.)
- Args: `--bogus` -> exit 2 "unknown option"; `-report.json` as 3rd positional -> exit 2 "unknown option", no file created (not silently swallowed); `--help` -> exit 0 and documents `-h, --human` as "NOT help".
- Scope: `git status --short -- Documentation/Unity-To-SBox-Automated/harness/` shows only `M Program.cs`; no second CLI, no Editor/Core change, no new tooling from this task.

Findings:
1. [Low] `eliots-agentics/tasks/build-terminal-access.md:71` (was :55 before this review block was inserted) - the control note claims that with different destination dirs "the only JSON difference was the caller-supplied `Destination` field". That is an overstatement: `TextureProcessing.ElapsedMilliseconds` is also run-dependent and can differ (parent's smoke run: 870 vs 901; 0 on the small fixture, which is why the worker's pair did not show it). Mode invariance and the constant-destination byte-identity both still hold. Fix (documentation only, non-blocking, no code change): add a one-line qualifier, e.g. "the only caller-dependent difference was `Destination`; `TextureProcessing.ElapsedMilliseconds` is run-to-run timing noise, and neither field varies with mode."

## Notes

### Plan (carried out this turn)
Refactor the existing harness in place — no second CLI project, no Editor/Core changes. Status header left to the Master per the dispatch instruction; all evidence written here.

### Design / root cause
- Before: `harness/Program.cs` was 47 lines of top-level statements — it filtered `--scenes`, required 2-3 positionals, printed every archive asset and every issue, wrote `import-report.json`, exit 1 on unresolved. No argument parser, no help, no reduced-output mode. The missing "terminal access" was a missing argument/output surface on this driver, not missing conversion code.
- After: same file, now a `Program` class that still calls the identical `UnityArchive.Read` / `UnityImport.Run(..., ImportOptions.Auto(...), extractChannels: Extract)` / `File.WriteAllText(..., result.ReportJson)` sequence (Program.cs:120-128). Conversion semantics, report bytes and exit codes are unchanged.
- `CliSettings` (Program.cs:20-45) exposes every knob as a named, tweakable member: `ScenesFlag`, `HumanFlags` (`-h`, `--human`), `HelpFlags` (`--help`), `DefaultReportFile`, `Usage`, `VerboseAssetLimit` / `HumanAssetLimit`, `VerboseIssueLimit` / `HumanIssueLimit`, `VerboseProgress` / `HumanProgress`, plus `Limit(...)` / `Progress(...)` selectors. Defaults: verbose = `int.MaxValue` (lists everything = byte-identical legacy output); human = 0 assets / 3 issues per section plus a grouped-by-code digest.
- `Main` (58-72): `--help` prints help and returns 0; otherwise `TryParse`; bad args return 2. `TryParse` (75-96) accepts flags in any order and keeps the exact positional contract `[--scenes] <package> <assetsDir> [report.json]` (default report path still CWD-relative `import-report.json`). Unknown dash-args are rejected rather than silently mis-parsed.
- One shared `PrintReport` (132-154) renders both modes; the mode only selects the exposed caps — no duplicated formatting logic. Human-only additions are the counts line and grouped digest (143-145). Progress stays silent by default (120) so verbose console bytes match the legacy harness; the two progress booleans are the tweak points.
- Portability/DRY: `py` + `-3` on Windows / `python3` elsewhere kept verbatim (102-112); `Path.Combine`, no hardcoded separators or drive letters. `channels.py`, `harness.csproj` and all `Editor/Core/*.cs` untouched. No unrequested scripts or tooling added.

### Verification evidence (from the final reviewed bytes)
Build gate — `cd Documentation/Unity-To-SBox-Automated && IUP_ROOT=C:/cseliot/1-Development/1-SBox/2-Projects/unityimporterplus dotnet build harness/harness.csproj -c Release`: **Build succeeded, 0 Warning(s), 0 Error(s)** (exit 0). `Program.cs` first bytes `75 73 69 6e 67` (no UTF-8 BOM), 0 CR bytes (LF).

Default verbose run — fixture `fixtures/scene-loss.unitypackage` (1,987 B), destination `.../iup-cli-evidence/out-pair`, report `report-pair-verbose.json`, exit 1 (data: 1 unresolved). Captured console, 10 lines:
```
archive assets: 5
  Prefab       Fixture/Prefabs/Prop.prefab
  Prefab       Fixture/Prefabs/Variant.prefab
  Scene        Fixture/Scenes/Complete.unity
  Scene        Fixture/Scenes/Lossy.unity
  Scene        Fixture/Scenes/Nested.unity
assets=2 converted=1 files=1 changed=1
UNRESOLVED Error conversion-skipped Fixture/Prefabs/Variant.prefab: Prefab conversion skipped. Prefab has a top-level prefab instance with a prefab variant (Fixture/Prefabs/Prop.prefab); prefab variants are not supported.
WARN Fixture/Prefabs/Variant.prefab: Prefab conversion skipped. Prefab has a top-level prefab instance with a prefab variant (Fixture/Prefabs/Prop.prefab); prefab variants are not supported.
INFO 3 Unity scene(s) were not converted; enable scene import under Advanced to write s&box scenes for them.
```

`-h` run — same fixture and same destination string (dir wiped between runs), report `report-pair-human.json`, exit 1. Captured console, 7 lines:
```
archive assets: 5
assets=2 converted=1 files=1 changed=1
issues: unresolved=1 warnings=1 info=1
  conversion-skipped: 1
UNRESOLVED Error conversion-skipped Fixture/Prefabs/Variant.prefab: Prefab conversion skipped. Prefab has a top-level prefab instance with a prefab variant (Fixture/Prefabs/Prop.prefab); prefab variants are not supported.
WARN Fixture/Prefabs/Variant.prefab: Prefab conversion skipped. Prefab has a top-level prefab instance with a prefab variant (Fixture/Prefabs/Prop.prefab); prefab variants are not supported.
INFO 3 Unity scene(s) were not converted; enable scene import under Advanced to write s&box scenes for them.
```
Verbosity difference: verbose prints 5 per-asset lines; `-h` replaces them with a 2-line count/digest. Counts and the three issue detail lines are identical (shared formatter). `--human` long form produced the same 7-line output.

Report byte-identity — `cmp report-pair-verbose.json report-pair-human.json` exit 0; both sha256 `5f82379ae861315d6e81f75950cf8b0056aaa0a5344906bf2229f8a2227994d8`. Control note: with *different* destination dirs the only JSON difference was the caller-supplied `Destination` field (verified by diff), unrelated to mode; holding the destination constant gives byte-identical JSON.

Legacy regression — `harness Testing/building.unitypackage .../legacy-out .../report-legacy.json` with no new flags, exit 0: `archive assets: 31`, `assets=15 converted=10 files=24 changed=24`, 9 INFO, 0 WARN, 0 UNRESOLVED — matches the documented no-`--scenes` baseline (24 files / 0 warnings / 0 unresolved). Legacy positional contract intact.

`--help` — exit 0; prints usage/arguments/options, explicitly documents `-h, --human` as "Human-Readable output: reduced verbosity (NOT help)" and `--help` as the help switch.

Logs/reports on disk: `C:/Users/Eliot/AppData/Local/hermes/profiles/hermes-sbox/cache/scratch/iup-cli-evidence/` (`pair-verbose.log`, `pair-human.log`, `human-long.log`, `legacy.log`, `help.log`, `report-*.json`). Headless only; no editor/visual claim.

### Commit path list (explicit paths only; no commit yet)
- `Documentation/Unity-To-SBox-Automated/harness/Program.cs` — the refactor
- `eliots-agentics/tasks/build-terminal-access.md` — this evidence
No Editor/Core, `channels.py` or `harness.csproj` changes. The Director's archive moves (deleted tasks/01-05 files, untracked `.humans-only/` additions) were deliberately NOT staged. Awaiting Review Critic verdict; no commit, no remote push.

## Clarification Q&A


## Relevant History 

Before this task the only headless surface was the harness at `Documentation/Unity-To-SBox-Automated/harness/` — `Program.cs` (47 lines of top-level statements), `harness.csproj` (net10 console compiling `Editor/Core/*.cs`, gated by `IUP_ROOT`) and `channels.py` (Pillow stand-in). It filtered `--scenes`, accepted 2-3 positionals, printed one line per archive asset and per issue, wrote `import-report.json` to CWD, and exited 1 when unresolved. README.md mentions console commands that do not exist in this checkout. There was no argument parser, no help and no reduced-output mode; the work was to add a CLI surface to this driver, not to write a second one.

The Director pinned `-h` as Human-Readable (reduced verbosity), not help, with `--help` as the help switch; verbose is the default and the legacy positional contract is load-bearing for gates/baselines, so legacy console bytes had to stay unchanged. Stylistic Notes require named, developer-tweakable properties instead of inline magic, plus portability and DRY. Acceptance fixtures: `Testing/building.unitypackage` (24 files, 0 warnings, 0 unresolved without `--scenes`) for the legacy regression and `fixtures/scene-loss.unitypackage` (tiny, one `conversion-skipped` issue) for the mode comparison.

## Stylistic Task Notes:
References to variables like X and Y automatically imply that these are to be exposed properties for developer tweaking.
When programming, portability and DRY are a priority.

## Post-Task Follow Ups 
