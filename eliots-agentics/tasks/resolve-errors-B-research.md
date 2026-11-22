# Research record - grouping the 264 import errors (resolve-errors-B)

Read-only research, 2026-11-22. Three research subagents + a Master byte-level re-read.
No importer code was changed while producing this record.

## Evidence files

- `Testing/export-report-264-errors-2-groups.json` (1,330,304 B) - importer report from a
  fresh `s.unitypackage` import.
- `Testing/console-out.txt` (172,714 B, exactly 1,000 lines, 02:25:34-03:07:54) - a sample
  of the s&box editor / resource-compiler session. Console counts are exact WITHIN this file,
  not claims about the full editor log. The two `Generic ... resources:` summary lines at
  02:29:53 name all 264 report paths.
- Scratch reports (full detail, reproducible commands):
  `C:/Users/Eliot/AppData/Local/hermes/profiles/hermes-sbox/cache/scratch/taskmaster-nov22/research-errors.md`,
  `research-tga.md`, `research-code.md`.

## Verified baseline

| Fact | Value |
|---|---|
| Assets / Converted / Files | 1277 / 940 / 2119 |
| Unresolved | 64 = 45 `unassigned-material` + 16 `missing-texture` + 3 `prefab-material-assignment` |
| Warnings | 80 = 64 + 16 tiling/offset (tiling is NOT in Unresolved) |
| Information | 2771 |
| Completion.Errors | 264 strings, all unique = 261 dependency-timeout + 3 no-compiled |

`Completion.Errors` by template x path type (sums to 264):
`timeout x lightmap-vtex 96`, `timeout x vmat 88`, `timeout x vmdl 77`,
`no-compiled x vmat 2`, `no-compiled x hdr-vtex 1`.

## Are the 261 timeouts one cause?

One downstream SYMPTOM, several upstream CAUSES. All 261 share the exact template
`compiled resource dependencies did not become ready within 30 seconds. Reimport or run
unity_import_repair_resources for this folder.` The affected resources split cleanly:
96 scene lightmap `.exr.vtex` (G3), 77 `.vmdl` (G1), 88 `.vmat` mostly queue contention
behind G1/G2/G3. The separate 3-error group is also mixed: 2 poster `.vmat` (G2) +
1 HDRI `.vtex` (G3). So the report's own `264 (2 groups)` is a MESSAGE-TEMPLATE grouping,
not a root-cause grouping. Grouping below is by root cause.

## Six root-cause groups

### G1 - Unresolved FBX material slots leak bare .vmat names into models
48 importer entries (45 `unassigned-material` + 3 `prefab-material-assignment`),
133 console lines (33 illegal short names, 32 `Missing vmat`, 63 `Could not find material
data`, 5 `vmat_c` FILEOPEN), 77 `.vmdl` timeouts.
Bare names: glass 15, windows 9, metal 4, lambert2 2, lambert3 1, mesh 1, floor 1.
Representative: `a/s/Art/Model/Structure/Doors/Door_01/Door_01_01_Snaps014.fbx` (Glass+Metal),
`.../Classroom_Door_Wall_01_Snaps014.fbx` (lambert2/lambert3/mesh; generated `.vmdl` has
ZERO remaps).
Mechanism (code census, `research-code.md`): `Editor/Core/UnityImport.cs:302` `MaterialFor`
returns null when no converted `.vmat` output exists -> no remap entry (`:305-341`);
neutral fallback (`:346-361`) is gated on default/no-slot FBXs, so a named non-default slot
does not qualify; `:367-369` only emits an `unassigned-material` warning; `ModelDocument`
(`:783-811`) emits `from = <slot>.vmat`, so ModelDoc falls back to the raw FBX slot name,
a path-less resource name that cannot exist on disk. Slot names originate at
`Editor/Core/UnityModel.cs:226-227,300`. `ImportPlan.Match` (`:97-99`) is silent on zero
candidates, which is why the plan under-reports these slots.
DISPOSITION: NEW TASK `fix-unresolved-material-slots.md` (per Director Q1=A).

### G2 - 16-bit poster TGAs rejected by s&box's TGA loader
208 console lines in the poster decode/compile chain, fully decomposed with zero overlaps:
FIVE decode messages x 32 = 160 (`TGALoader::GetInfo`, `CTextureObject::ReadFromFile`,
`Error processing texture g_tColor`, `ReadInputTextures`, `Error calling CompileImmediately`),
+ 10 `Error loading image file header` + 32 poster full-path `illegal resource name`
+ 6 `describe failed` = 208. Verified by union of the four buckets (160/10/32/6, all disjoint).
The ~22 further poster lines carrying `Generic Not Compiled` / `Not compiling` /
`ERROR recompiling` / `CompileIfNeededAsync` are the cross-cutting content-build cascade listed
below, NOT part of this group's 208; they are not double-counted. 2 `.vmat` no-compiled errors.
Root cause verified byte-level by the Master: both albedos are TGA type 2 at **16 bpp**
(5:5:5:1), 1024x1024 and 2048x2048, descriptor 0x01. Engine
`Bitmap.Loading.Tga.cs:70` rejects every non-type-3 image whose depth is not 24/32;
`TgaValidationTests.cs:36` test-locks 16-bpp rejection. Of 248 imported TGAs exactly these
2 are 16-bpp - exactly the 2 in the console errors. The importer copies them verbatim
(`UnityArchive.cs:86-89,196-206`; `UnityImport.cs:206-211`) and does NOT corrupt them
(md5 matches the Unity source project and the `.unitypackage` tar payload).
DISPOSITION: covered by existing `eliots-agentics/tasks/tga-fix.md` (per Director Q1=A).

CORRECTION: one research child reported the poster_01 header as "4x4, 24-bit". That is a
little-endian misread of the width bytes. Master re-read the raw 18 bytes directly:
`00 00 02 00 00 00 00 00 00 00 00 00 00 04 00 04 10 01` = width 0x0400 (1024),
height 1024, depth 0x10 (16), descriptor 0x01. Do not carry the 4x4/24-bit claim forward.

### G3 - EXR lightmap / HDRI compile stalls (large valid sources)
97 report errors = 96 scene lightmap `.exr.vtex` timeouts + 1 HDRI
`spruit_sunrise_8k.exr.vtex` no-compiled (console: a 231.4 s compile). All 96 sources and
wrappers exist on disk (152 MB total; HDRI 201 MB) while `Information` says lightmaps/HDRI
"are not converted" - wrappers are emitted for sources the importer itself declares
unconverted. No `Unresolved`/`Warnings` entry corresponds; this is a resource-compiler
throughput/validity issue, not a material-resolution warning.
DISPOSITION: reported here, no task file (Director Q1=A: one new task only).

### G4 - Absolute author paths embedded in copied FBX binaries
5 console `ERROR: Specified path ... could not be made content-relative`; 0 report
Unresolved/Warnings/Completion.Errors; 19 copied FBX binaries still contain author absolute
paths (8 `huangzhilin`, 4 `JP_School corridor`) despite 337 `Information` lines claiming
reduction. Generated `.vmdl` text is already content-relative; the escaping path is in the
binary FBX. Partly addressed by commit c7f922a (`UnityFbxRewrite.Sanitize`); residual.
DISPOSITION: reported here, no task file (Q1=A).

### G5 - Missing source texture GUIDs (source-data gaps)
16 importer warnings = 16 of the 64 `Unresolved` (`missing-texture`), 2 GUIDs x 8 materials.
0 console lines, 0 Completion.Errors; neither GUID appears in `console-out.txt`.
DISPOSITION: source-data gap, no engine error follows; no task file (Q1=A).

### G6 - Manual material tuning: tiling/offset and unsatisfiable prefab overrides
19 importer warnings = 16 tiling/offset (in `Warnings`, not `Unresolved`) + 3
`prefab-material-assignment`. 0 console lines, 0 Completion.Errors.
DISPOSITION: by-design manual tuning; no task file (Q1=A).

### Cross-cutting, NOT a seventh group
Engine content-build cascade: 191 `Not compiling ... a content build is running`, 191
`ERROR recompiling`, 56 `Generic Not Compiled`, 73 `[FAIL]`, 4 `Generic Content build:`
summaries, 2 `Clamped index` FBX lines. These are downstream symptoms of G1-G3 / a minor
standalone geometry note, not independent root causes.

## Reproduce (all commands run from the project root)

```
python3 -c "import json,collections;d=json.load(open('Testing/export-report-264-errors-2-groups.json'));print(collections.Counter(u['Code'] for u in d['Unresolved']))"
python3 -c "import json,collections;d=json.load(open('Testing/export-report-264-errors-2-groups.json'));E=d['Completion']['Errors'];print(collections.Counter('timeout' if 'did not become ready' in s else 'no-compiled' for s in E))"
grep -F -c 'Trying to load an illegal resource name' Testing/console-out.txt        # 65
grep -F -c 'Missing vmat' Testing/console-out.txt                                   # 32
grep -F -c 'Could not find material data for' Testing/console-out.txt               # 63
grep -F -c 'ERROR_FILEOPEN' Testing/console-out.txt                                 # 5
grep -F -c 'could not be made content-relative' Testing/console-out.txt              # 5
python3 -c "import json;d=json.load(open('Testing/export-report-264-errors-2-groups.json'));print(sum('/scenes/mergev2/' in s for s in d['Completion']['Errors']))"  # 96
```

## Why only one new task file

Director Q1=A: count `tga-fix.md` as the TGA group's task; create ONE new task for the
other group. Research found six groups, not the two the question was premised on; the
Master recorded that as a parent decision in `resolve-errors-B.md` `## Clarification Q&A`.
G1 is the group with an actionable importer-code cause and 77 report errors, so it gets
the single new task. G2 is `tga-fix.md`. G3-G6 are documented above with their dispositions.
Director Q2=A: the new per-group task stays `DONE? NO!` as fresh backlog; only the three
current tasks are closed. This explicitly overrides the standing "all headers DONE? YES!"
goal for the new backlog task.
SUPERSEDED (Director, in chat, 2026-11-22 18:33): "My mistake, I meant 2B anyway." The
intended answer was Q2=B = drive the new per-group tasks to `DONE? YES!` in this same loop.
The three lines above are retained as the historical record but are no longer the governing
decision. `fix-unresolved-material-slots.md` was driven to `DONE? YES!` at commit 3ae34c7.
