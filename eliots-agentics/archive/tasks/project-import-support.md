## Status: STARTED? YES! CLARIFIED? YES! HISTORY? YES! REVIEWED? YES! DONE? YES! WORKER: sa-0-2fd743fc OTHER: reviewed 14:15 no further changes; Unity project directory import committed and pushed

## Task
Build support for importing directly from a unity project directory.

### Description
Reusing as much code as possible, give the option (in both the terminal and in-editor GUI) to do the import directly by pointing at a unity folder.
Only other instructions are:
Have 'IsValidUnityFolder' detection.
Ask which folders from the /Assets/ that the user wishes to import.

### Work Review

2026-11-22 14:15 - Verdict: no further changes

No defects found in any of the six reviewed files. Verdict is `no further changes`. Every gate below was re-run independently by the critic (fresh probe sources and freshly built harnesses), not taken from the worker's logs.

1. [Low] Gate - harness build: `dotnet build Documentation/Unity-To-SBox-Automated/harness/harness.csproj -c Release -p:IUP_ROOT=<project>` = `Build succeeded. 0 Warning(s) 0 Error(s)`. Fix: none.
2. [Low] Gate - offline editor compile: full rebuild of `.../taskmaster-nov22/editor-check/editor-check.csproj -c Release -t:Rebuild` = `28 Warning(s), 0 Error(s)` (incremental run: 8/0); warnings are pre-existing CS1570/CS1573/CS0618 engine/addon warnings, none in the five changed sources. Fix: none.
3. [Low] Detection/enumeration - `Editor/Core/UnityProjectFolders.cs:15-16,22-27,30-35,38-44`: public static engine-free class; criteria are exposed constants (`AssetsFolderName`, `ProjectSettingsFolderName`, `IncludeAssetsRootFiles` at :19); `IsValidUnityFolder` requires the dir plus `Assets/` plus `ProjectSettings/`. Critic-built probe (fresh csproj compiling actual Editor/Core): fixture=True, Assets-only non-unity=False, missing=False, null=False; `ListAssetFolders(fixture)=Materials|Textures` (immediate, OrdinalIgnoreCase-sorted). Called by BOTH CLI (`Program.cs:186` via `UnityArchive.ReadDirectory`:130) and GUI (`UnityPackageWindow.cs:163,178`). Fix: none.
4. [Low] `Editor/Core/UnityArchive.cs:124-166`: `ReadDirectory` builds the same `UnityAsset` shape as `Read` - guid from the shared `MetaGuidPattern` regex on `X.meta` (:36,151-153), `Path = SafePath("Assets/"+relative)` (:146), `Source`/`Metadata` are real files on disk (:143,148), `Kind = Classify` (:156), `Selected` (:158), sorted (:162), `UnityModel.AssignPrefabMaterials` (:163). Missing/invalid/duplicate guid and duplicate path are warned and skipped (:149-155). `SourceDirectory`/`FileName` set and `HasExtractedFiles=true` (:134), so the reimport path cannot re-open a directory as gzip. `Dispose()` stays workspace-null-safe (:208-212). Fix: none.
5. [Low] Pipeline untouched: `ImportCatalog`/`UnityImport.Run` are not in the reviewed change set; the HEAD mirror of `Editor/Core/UnityImport.cs` matches `git show HEAD:...` byte-for-byte. CLI calls the unchanged `UnityImport.Run(..., ImportOptions.Auto(...))` at `Program.cs:194`. Fix: none.
6. [Low] CLI - `Documentation/Unity-To-SBox-Automated/harness/Program.cs:116-117,118-122,129-158,82-87,184-191`: a DIRECTORY first positional is a project, a FILE stays `.unitypackage`; `--folders A,B` and `--folders=A,B` validate against `ListAssetFolders` and reject unknown names with a clean `error:` + exit 2; TTY numbered prompt, piped-stdin all-folders fallback, `--*` unknown-option rejection, and preserved `-h`/`--human`/`--help` all confirmed by fresh runs. Fix: none.
7. [Low] Folder-selection gate: a fresh CLI run with `--folders Materials` on the hand-built fixture produced exactly `Materials/MaterialTexture.png`, `Materials/TestMat.mat`, `RootMat.mat` and an output tree of `imported/materials/*` + `imported/rootmat.*` only - `Textures/Pixel.png` is absent. The root-level `RootMat.mat` inclusion is a documented, tweakable design (`UnityProjectFolders.cs:19,38-41`, GUI label `UnityPackageWindow.cs:182`), not another subfolder leaking in. Bad `--folders=Nope` and a non-Unity dir both exited 2 with a one-line error, no stack trace. Fix: none.
8. [Low] Output validation - `verify_output.py <parent-of-imported> <.../imported>` = `checked 2 files, 0 problems`, exit 0. Two identical `--folders Materials` runs gave byte-identical `imported/` trees (`diff -r --strip-trailing-cr` = 0 bytes). Generated resources contain no raw absolute paths; the report's `Destination` absolute path is pre-existing, unchanged `UnityImport.Run` behaviour and the only difference between the two run reports (different output roots). Fix: none.
9. [Low] Byte-identity gate: rebuilt a HEAD-code harness from the scratch mirror (verified `UnityArchive.cs`, `UnityImport.cs`, `Program.cs` match `git show HEAD:...`). Current vs HEAD, no `--scenes`: building `24` files / `0` unresolved / `0` warnings and city--1 `1,166` files / `3` unresolved / `3` warnings on both sides; `diff -r --strip-trailing-cr` of each `imported/` tree = 0 bytes. Report JSON differs only in the non-deterministic `TextureProcessing.ElapsedMilliseconds` timing field. Fix: none.
10. [Low] GUI - `Editor/UnityPackageWindow.cs:75-76,153-173,176-200,203-243,270-281` and `Editor/UnityPackageMenu.cs:15-21`: new `Browse folder…` button calls `SetFindDirectory()` (:157), validates with the shared detector (:163), awaits a checkbox dialog of immediate subfolders defaulting to all selected (:188), and routes through the shared `LoadArchive` refactor (:210) extracted from `LoadPackage` (:206); the reimport guard `!archive.HasExtractedFiles && archive.SourceDirectory == null` (:271) prevents gzip re-read of a directory archive; new menu item present. Compile-proven offline; no GUI click claimed. Fix: none.

Disposition of the three judged items:
11. [Low] `Documentation/Unity-To-SBox-Automated/make_editor_check.py` Windows `re.sub` bad-escape bug (`out = os.path.abspath(...)` at HEAD line 11 interpolated into a replacement string at line 21): `git diff HEAD -- <file>` is empty, so the defect is pre-existing and outside the six reviewed files; the worker generated the equivalent csproj in scratch to prove the compile gate instead of editing repo tooling. Acceptable Open item, NOT a defect of this change. Fix: none here (leave for a separate task if desired).
12. [Low] GUI visual/interaction check recorded as compile-proven only: honestly stated - task file Notes lines 42, 58, 94, 100 explicitly say "compile-proven only, not clicked", "No GUI click is claimed as tested" and "explicit Open follow-up", matching SPEC hard rule 5. Fix: none.
13. [Low] Empty `## Post-Task Follow Ups` with Open items under `## Notes`: acceptable. The section also exists empty in `git show HEAD:<task file>` (pre-existing template heading), and the SPEC Deliverable directs evidence/gates into `## Notes` / `## Relevant History`; the worker placed a clearly labelled "Open items (not done here)" list at Notes lines 57-60. Not a defect; at most a cosmetic doc nit, no fix required.

Context only (not a finding): the Director advanced HEAD from c1ec442 to 64a98ff (commits fc304e6, 64a98ff) during the review; c1ec442 is an ancestor. The five source files and the task file remain modified/untracked and nothing is staged, so the reviewed bytes are still what the worker will commit.

Tree:
git hash-object --path="Editor/Core/UnityProjectFolders.cs" "Editor/Core/UnityProjectFolders.cs" = 625a411ebbed9d1fb03cb922a3f92d4d2510c809
git hash-object --path="Editor/Core/UnityArchive.cs" "Editor/Core/UnityArchive.cs" = cfa658c034ebc376e82d5677b16c7254730e444b
git hash-object --path="Documentation/Unity-To-SBox-Automated/harness/Program.cs" "Documentation/Unity-To-SBox-Automated/harness/Program.cs" = 7636ac803eaf11d6f83c8a4e53a9728b7d325086
git hash-object --path="Editor/UnityPackageWindow.cs" "Editor/UnityPackageWindow.cs" = f647f03437f62ccf2a8310c148fb031e650b1cd3
git hash-object --path="Editor/UnityPackageMenu.cs" "Editor/UnityPackageMenu.cs" = 98dd36222999c306764709dc114665b33da9194c

## Notes

Implementation (base HEAD c1ec442)
- `Editor/Core/UnityProjectFolders.cs` (new, engine-free): public static `IsValidUnityFolder( folder )` requires the directory itself plus `Assets/` and `ProjectSettings/` (names are exposed constants). `ListAssetFolders` returns the immediate Assets/ subfolders sorted OrdinalIgnoreCase. `IsPathSelected` + `IncludeAssetsRootFiles` define selection; files directly in Assets/ are always included because they belong to no subfolder.
- `Editor/Core/UnityArchive.cs`: `public static ReadDirectory( projectFolder, progress, cancel, assetsDirectory, folders = null )` builds the same `UnityAsset` list as `Read`: guid from `(?m)^\s*guid:\s*([0-9a-fA-F]{32})` in each `X.meta`, `Path = SafePath( "Assets/" + relative )`, `Source` = the real file on disk, `Metadata` = its sidecar, `Kind = Classify`, sorted by path, `UnityModel.AssignPrefabMaterials`. Missing/duplicate guid or path is warned and skipped. No workspace; `SourceDirectory` marks this archive so reimport never treats it as a gzip tar. `ImportCatalog`/`UnityImport.Run` are untouched.
- CLI `harness/Program.cs`: first positional DIRECTORY => Unity project (FILE stays `.unitypackage`); `--folders A,B` / `--folders=A,B`, interactive numbered prompt only when stdin is a terminal, all-folders fallback for piped stdin, clean exit 2 with `error: …` for bad folder/detection, updated usage/help.
- GUI `UnityPackageWindow.cs`: new `Browse folder…` button using `FileDialog.SetFindDirectory()`; `BrowseFolder()` checks `IsValidUnityFolder`, awaits a checkbox dialog (immediate subfolders, all `Value = true` by default) and calls the shared `LoadArchive(...)` extracted from `LoadPackage`; reimport guard is `!archive.HasExtractedFiles && archive.SourceDirectory == null`. `UnityPackageMenu.OpenFolder()` adds `Editor > Import Unity Package > Import Unity project folder...`.

IsValidUnityFolder / enumeration evidence
Scratch probe (`C:/Users/Eliot/AppData/Local/hermes/profiles/hermes-sbox/cache/scratch/iup-probe`) compiled against the actual `Editor/Core/*.cs`:
```
IsValidUnityFolder(fixture)=True
IsValidUnityFolder(nonunity)=False          # Assets/ only, no ProjectSettings/
IsValidUnityFolder(missing)=False
ListAssetFolders(fixture)=Materials|Textures
ListAssetFolders(nonunity).Length=0
all-folders assets=Materials/TestMat.mat|RootMat.mat|Textures/Pixel.png
  c29d9dff2056b1543bbb70ec32427a62 Materials/TestMat.mat SourceExists=True MetadataExists=True Kind=Material
  22222222222222222222222222222222 RootMat.mat SourceExists=True MetadataExists=True Kind=Material
  11111111111111111111111111111111 Textures/Pixel.png SourceExists=True MetadataExists=True Kind=Texture
```
Fixture: `.../scratch/iup-gates/fixture` with `Assets/{Materials,Textures}/` (mat+png and sidecars with 32-hex guid), `Assets/RootMat.mat` at the Assets root, `ProjectSettings/ProjectVersion.txt`.

Folder-selection evidence
- CLI (`--folders Materials`): `archive assets: 3` listing `Materials/MaterialTexture.png`, `Materials/TestMat.mat`, `RootMat.mat` — `Textures/Pixel.png` is absent. Output tree: `imported/materials/{materialtexture.png,testmat.mat,testmat.vmat}` + `imported/rootmat.{mat,vmat}` only; report `Unresolved=0 Warnings=0 Information=2 Files=5`, exit 0.
- Real TTY prompt (background PTY): printed `1. Materials`, `2. Textures`, `Enter numbers...`; submitting `1` selected `Materials` and produced the same 3 assets, exit 0. Piped stdin printed `Assets/ subfolders: Materials, Textures (all selected)` and imported 4 assets including `Textures/Pixel.png`, no hang.
- Bad `--folders Nope`: `error: Unknown Assets/ subfolder(s): Nope. Available: Materials, Textures`, exit 2 (no stack trace). Non-Unity directory: `error: Not a Unity project folder (needs Assets/ and ProjectSettings/): ...`, exit 2.
- GUI code path (compile-proven only, not clicked): `BrowseFolder` → `SetFindDirectory()` → `IsValidUnityFolder` → `ChooseFolders` checkbox list default all selected → shared `LoadArchive` → same `ImportCatalog`/`ImportPlan` tree; new menu entry; directory archives cannot be re-read as gzip (`SourceDirectory` guard).

Before/after generated output
- Before the missing-texture sidecar was added to the fixture, selecting `Materials` produced `archive assets: 2`, 2 `UNRESOLVED Warning missing-texture` lines, exit 1. After adding `Assets/Materials/MaterialTexture.png` (guid `b993ae890161b184188e417d77e0e1a3` referenced by the material), the same run produced 3 assets, 0 unresolved/0 warnings, exit 0; generated files above. This is fixture data, not a code change.
- Legacy package output is unchanged: see baseline table below.

Gate results (all real runs)
- Harness build (final bytes, after the clean-error patch): `Build succeeded. 0 Warning(s) 0 Error(s)`.
- Offline editor compile (done by parent): csproj `C:/Users/Eliot/AppData/Local/hermes/profiles/hermes-sbox/cache/scratch/taskmaster-nov22/editor-check/editor-check.csproj`, `dotnet build ... -c Release` = 21 Warning(s), 0 Error(s); warnings pre-existing (CS1570 XML `s&box` entity, CS0618 obsolete `GameTransform.Position`, CS1573 engine addons, MSB3539).
- Two identical `--folders Materials` imports into `final1`/`final2`: both exit 0, `assets=3 converted=2 files=5 changed=5`, `diff -r --strip-trailing-cr final1/imported final2/imported` = 0 lines / exit 0.
- `verify_output.py <out-root> <out-root>/imported` (Assets root = parent of `imported/`): `checked 2 files, 0 problems`, exit 0. An earlier run against `out/imported` as its first argument was mis-rooted and reported false MISSINGs; corrected invocation is the one recorded here.
- `--help` shows the directory positional, `--folders A,B`, `--scenes`, `-h/--human`; `-h` run prints the compact digest (`issues: unresolved=0 warnings=0 info=2`) and omits the per-asset list; verbose mode still lists every asset.
- Legacy `.unitypackage`: `Testing/building.unitypackage` with no `--scenes` gives `assets=15 converted=10 files=24 changed=24`, 24 files on disk, 0 warnings, 0 unresolved, exit 0.
- HEAD baseline mirror (`scratch/iup-head`, `git archive HEAD Editor/Core` + HEAD `Program.cs`, LF sources, same harness csproj) vs current, both no-scenes, `diff -r --strip-trailing-cr`: building 24 files / 0 unresolved / 0 warnings, diff 0 lines; city--1 1,166 files / 3 unresolved / 3 warnings, diff 0 lines. Both sides have matching counts and non-empty trees.

Open items (not done here)
- GUI visual/interaction check in a live s&box editor: not possible headlessly; verify the folder button, checkbox dialog and reimport by hand. No GUI click is claimed as tested.
- `Documentation/Unity-To-SBox-Automated/make_editor_check.py` has a pre-existing Windows bug: `re.sub()` replacement built from `os.path.abspath()` raises `re.PatternError: bad escape \U`. Parent generated the equivalent csproj in scratch only; the repo file was deliberately NOT changed (out of scope).
- No commit and no push yet (Q4 provisional policy: push after critic approval). Nothing staged.

Commit path list (explicit paths only; no `git add -A`)
```
Editor/Core/UnityProjectFolders.cs
Editor/Core/UnityArchive.cs
Editor/UnityPackageWindow.cs
Editor/UnityPackageMenu.cs
Documentation/Unity-To-SBox-Automated/harness/Program.cs
eliots-agentics/tasks/project-import-support.md
```
All Director edits, archived task files, `.humans-only/*`, `Documentation/sbox-console-logs.txt` and the offline-check csproj stay unstaged. Source files are BOM-less LF; no repo checker/tooling added.


## Clarification Q&A

	Q1: When importing from a Unity project folder, which /Assets/ folders should be offered for selection?
	An: Immediate subfolders of Assets/ (Recommended): short checkbox list, e.g. AssetStoreOriginals, Scenes, Settings.

	Q2: How should the terminal ask which folders to import?
	An: Interactive numbered prompt when stdin is a terminal, plus a --folders flag for scripts/LLMs.

	Q3: What should IsValidUnityFolder require to accept a directory?
	An: An Assets/ directory plus a ProjectSettings/ directory.

	Q4: common.md now says 'Always git push'. origin is your own fork and main is ahead 20 commits. Confirm the push policy for this task?
	An: No answer (timed out). PROVISIONAL parent decision: push after the commit, as common.md now requires; origin is CSEliot/UnityToSBoxConverterImporter.git (your own fork) and main is ahead 20. Say the word and I will keep it local-only instead.

	Parent decisions (change if wrong):
	- First positional arg stays load-bearing: a DIRECTORY means a Unity project, a FILE means a .unitypackage. No new mode switch; the legacy positional contract is preserved.
	- GUI gets a new "Browse folder..." button plus a new menu item; the existing .unitypackage flow is untouched.
	- Folder selection is a checkbox dialog of the immediate Assets/ subfolders, default all selected; the existing package-contents tree still governs per-file selection after load.
	- IsValidUnityFolder is a public static engine-free method in Editor/Core, called by BOTH the CLI and GUI (DRY). Its criteria are exposed as tweakable constants.
	- Output paths, report JSON, ImportOptions and all conversion logic are unchanged; only the archive source changes. building 24 and city--1 1,166 no-scenes outputs must stay byte-identical.
	- The s&box editor is not available headlessly; GUI code is proven to COMPILE offline, and the visual/interaction check is an explicit Open follow-up.

## Relevant History 

UnityImporterPlus converts `.unitypackage` GUID-tar archives through `Editor/Core` into VMDL/VMAT/TMAT/prefab/scene output under `Assets/imported/<unity folder>/`; the headless harness in `Documentation/Unity-To-SBox-Automated/harness` compiles the same core files and `UnityImport.Run` with `ImportOptions.Auto`, while the editor window offers per-file selection through `ImportCatalog`/`ImportPlan`. Before this task the only archive source was `UnityArchive.Read( file, … )`, which extracts into a scratch `ImportWorkspace`; the window re-reads that archive on reimport when staging is gone.

Task project-import-support adds a second archive source (a live Unity project directory) without touching the converter: a shared engine-free detector/enumerator in `Editor/Core`, a `UnityArchive.ReadDirectory` factory producing the same `UnityAsset` list, a directory-aware CLI (`--folders` plus a TTY prompt) and GUI folder button/dialog. Verification used a hand-built scratch fixture, a PTY/stdin prompt run, `verify_output.py`, and a HEAD-code baseline mirror for byte-identity; the editor UI itself is compile-proven only and remains a manual follow-up.
## Stylistic Task Notes:
References to variables like X and Y automatically imply that these are to be exposed properties for developer tweaking.
When programming, portability and DRY are a priority.

## Post-Task Follow Ups 
