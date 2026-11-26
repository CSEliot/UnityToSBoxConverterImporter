# Task 24 - verification evidence (worker, 2026-11-26)

## 1. Compile gate (rerun after the ScenePipeline fix)
Command: `dotnet build C:/cseliot/1-Development/1-SBox/2-Projects/unityimporterplus_branch_2/Documentation/Unity-To-SBox-Automated/harness/harness.csproj -c Release -p:IUP_ROOT=C:/cseliot/1-Development/1-SBox/2-Projects/unityimporterplus_branch_2 --no-incremental`
Result: **Build succeeded. 0 Warning(s), 0 Error(s).**

## 2. Fresh SyntyForest run (with scenes)
Command (all native C:/ paths, explicit scratch report):
`dotnet .../harness/bin/Release/net10.0/harness.dll --scenes --folders '*' C:/cseliot/1-Development/4-Unity/2-Projects/SyntyForest C:/Users/Eliot/AppData/Local/hermes/profiles/hermes-sbox/cache/scratch/urp24/after2/Assets C:/Users/Eliot/AppData/Local/hermes/profiles/hermes-sbox/cache/scratch/urp24/after2-report.json`
`selected folders: Scenes, Scripts, Settings, Synty, TutorialInfo`. Exit 1 (unresolved source-data issues) is expected DATA.
Outputs: `scratch/urp24/after2/Assets/imported/...`, `after2-report.json`, `after2-console.txt`, `analysis2.txt`.

### Acceptance assertions (generated .scene JSON, both sides of each number)
| Check | Result |
|---|---|
| SampleScene `Sandbox.PostProcessVolume` | exactly **1**; `SceneVolume {"Type":"Infinite"}`, Priority 0, BlendWeight 1, parent `Global Volume` Position `0,0,0` |
| Demo `Sandbox.PostProcessVolume` | exactly **1** (no template duplicate); `SceneVolume {"Type":"Infinite"}`, parent `Global Volume` Position `1619.958,-2416.8555,-398.8704` from Unity `61.38825/-10.131328/41.147015` via importer basis `(z,-x,y) * 39.37`; `README`-verified by Master |
| Bloom Strength SOURCE vs template | SampleScene generated **0.25**, Demo generated **2.19**, report prints both against engine Camera.prefab template default **1** |
| Tonemapping | SampleScene source mode **1 = Neutral** -> generated `Mode HableFilmic`, `ExposureCompensation 1.58`, `AutoExposureEnabled false`, `ExposureMethod RGB`; report prints template `AgX / 0 EV`. NOT AgX (mode 3 is AgX in URP 17.7) |
| Other mapped effects | SampleScene Vignette Intensity **0.2**, AmbientOcclusion Intensity **0.4**; Demo Bloom Threshold **0.81**, ColorAdjustments Contrast **1.062** / Saturation **1.07** from source **6.2 / 7** (NEW MAPPING range remap) |
| Enumeration | report line states winning profile and enumerated every candidate: SampleSceneProfile **59 entries**, Global Volume Profile **33 entries**, each with `m_OverrideState` and `m_Value`; inactive values are listed and not mapped |
| Report-only | explicit lines for DefaultVolumeProfile (global base stack, not emitted twice), SSR obsolete/no-op, baked data, unmapped overrides (LiftGammaGain, WhiteBalance, ShadowsMidtonesHighlights, MotionBlur) and dropped fields (bloom scatter/dirt, SSAO radius/quality, etc.) |
| HDRP path isolation | comparing `after-report.json` vs `after2-report.json` Information: **0 added HDRP lines**; 10 removed lines were the old HDRP-environment interpretation of the same URP volumes. No new HDRP environment lines on URP scenes |

## 3. verify_output.py
Command: `python .../Documentation/Unity-To-SBox-Automated/verify_output.py .../urp24/after2/Assets .../urp24/after2/Assets/imported`
Result: **`checked 1627 files, 0 problems`**. `engine?` informational lines are not problems.

## 4. building / city--1 byte identity (no --scenes, same mode as task-23 baselines)
Commands:
`dotnet .../harness.dll C:/cseliot/1-Development/1-SBox/2-Projects/unityimporterplus/Testing/building.unitypackage .../urp24/after-building/Assets .../urp24/after-building.json > .../after-building.log`
`dotnet .../harness.dll .../city--1.unitypackage .../urp24/after-city/Assets .../urp24/after-city.json > .../after-city.log`
Baseline provenance: task-23 HEAD b828409 baselines at `scratch/urp23/base-building` / `base-city` (their logs `base-building.log` / `base-city.log` and HEAD mirror `urp23/head-src` were checked; parent-verified). Non-URP code paths were not edited; `git status --short -- Editor/Core` shows only the three owned files modified.
Results:
- `diff -r --strip-trailing-cr .../urp23/base-building .../urp24/after-building/Assets` -> **exit 0, 0 lines** (byte-identical).
- `diff -r --strip-trailing-cr .../urp23/base-city .../urp24/after-city/Assets` -> **exit 0, 0 lines** (byte-identical).
- building console `grep '^UNRESOLVED' ... | sed ... | sort | uniq -c` current and base both: no UNRESOLVED lines (**0**, per `after-building.json`); city current and base both: `2 ambiguous-material`, `1 prefab-material-assignment` (3, per `after-city.json`). Logs: `urp24/diff-building.txt`, `diff-city.txt`, `after-building.log`, `after-city.log`.

## 5. Import Error Outputs gate
Exact stdout issue line (first of 31 `UNRESOLVED` lines, all `Warning` severity):
`UNRESOLVED Warning missing-texture Synty/PNB_Core/Shaders/Synty_Fabric.mat: Missing or unsupported texture 57754c55e28afad49bfd4e4a81e04452: no Texture asset with that GUID is packed in the source package (source-data gap).`
Last line:
`UNRESOLVED Warning unassigned-material Synty/PolygonNatureBiomes/PNB_Meadow_Forest/Models/SM_Prop_StoneWall_02.fbx: No converted material was resolved for slot 'Nature26'; using a neutral material.`

| Report | Unresolved | Warnings | Information | per-code Unresolved |
|---|---|---|---|---|
| task-23 `before-report.json` | 31 | 116 | 2066 | 13 / 1 / 17 |
| task-23 `after-report.json` | 31 | 116 | 2067 | 13 / 1 / 17 |
| task-24 `after-report.json` (before fix, --scenes) | 31 | 122 | 7001 | 13 / 1 / 17 |
| task-24 `after2-report.json` (after fix, --scenes) | 31 | 122 | 7038 | 13 / 1 / 17 |

Per-code breakdown every run: `missing-texture 13`, `prefab-material-assignment 1`, `unassigned-material 17`. **No growth, no new category.** The +6 Warnings / +4934 Information vs the task-23 reports are the `--scenes` mode delta (Master classification); the +37 Information from after->after2 are the new URP volume report lines and the removal of the incorrect HDRP lines.

## 6. Fix recorded
`UnityVolume.ScenePipeline` no longer treats the shared Core Volume guid `172515602e62fb746b5d573b38a5fe58` as HDRP evidence; any other HDRP-table guid still returns `Unknown`. `UnitySceneExport` filters those shared-guid MonoBehaviours out of the HDRP `UnityVolume.Read` call only when `pipeline == Urp`, eliminating the duplicate template-effect volume. HDRP/Unknown scenes keep the exact original call.

## Fix round (2026-11-27, after Work Review: 6 accepted Low findings)
- Finding 1 (`UnityVolume.cs:204-207`, `:194-213`): the shared Core Volume guid is now NEUTRAL for both classification and the HDRP-evidence early-return; only URP camera/light sibling guids set `urp = true`. All four SyntyForest scenes still classify Urp (verified by the generated effects below). Known limitation (recorded in `02-implementation.md`): a URP scene with only a local Volume and no URP camera/light sibling classifies `Unknown` and gets no local-volume report; no such scene in the fixture, out of scope.
- Finding 2 (`UnityVolume.cs:182,:592-593`, `UnitySceneExport.cs:758-759`): `VignetteSmoothness`/`VignetteRounded` read from source and emitted as engine `Smoothness`/`Roundness` when `m_OverrideState: 1`. Both inactive on this fixture, so no scene delta.
- Finding 3 (this file, section 4): building/city per-code clause swapped to the authoritative JSON values (building 0; city 3 = 2 ambiguous-material + 1 prefab-material-assignment).
- Finding 4 (`UnitySceneExport.cs:521-525`): the SOURCE report's Tonemapping clause is conditional; Demo now reads `Tonemapping not overridden` and emits no Tonemapping component.
- Finding 5 (`UnitySceneExport.cs:88-100`): `UrpBloomStrengthMax = 10`, `UrpBloomThresholdMax = 2`, `UrpNeutralExposureClampEv = 5` exposed and used at `:746-747` and `:788`, documented in the NEW MAPPINGS block. No current source value hits a bound.
- Finding 6 (`UnityVolume.cs:551-557`, `:568-610`, `:709-710`): mapped components require `active: 1` (missing `active` defaults to active); inactive mapped components are not emitted and get a dedicated `active: 0` report line. On this fixture no mapped component is inactive (only MotionBlur is `active: 0`, unmapped), so no output delta.

### Fix-round gates
1. Compile: `--no-incremental` rebuild -> **Build succeeded, 0 Warning(s), 0 Error(s)** (first attempt hit a transient host SDK GC/fork resource error `0x8007000E`, retried cleanly; no code change between).
2. Fresh SyntyForest `--scenes --folders '*'` into `scratch/urp24/after3/Assets`, report `after3-report.json`, console `after3-console.txt`, analysis `analysis3.txt`: exit 1 expected; **Unresolved 31** (`missing-texture 13`, `prefab-material-assignment 1`, `unassigned-material 17`), **Warnings 122**, **Information 7038** (identical counts to after2).
   - SampleScene: one PPV, Type Infinite, Position `0,0,0`, Bloom Strength **0.25** vs template **1**, Threshold 1, Vignette 0.2, AO 0.4, Tonemapping **HableFilmic + 1.58 EV** from source mode 1.
   - Demo: one PPV, Type Infinite, Position `1619.958,-2416.8555,-398.8704`, Bloom Strength **2.19** vs template **1**, Threshold **0.81**, ColorAdjustments **1.062 / 1.07** from source 6.2 / 7, **no Tonemapping effect**, report line `Tonemapping not overridden`. Two Overview scenes: 0 PPVs.
3. `diff -r --strip-trailing-cr .../urp24/after2/Assets/imported .../urp24/after3/Assets/imported` -> **exit 0, 0 lines** (scene trees byte-identical; the Demo report-text change lives only in the report/console, not scene JSON). Evidence: `diff-after2-after3.txt`.
4. `verify_output.py .../urp24/after3/Assets .../urp24/after3/Assets/imported` -> **`checked 1627 files, 0 problems`**.
5. building/city--1 rerun without `--scenes` into `urp24/fix-building/Assets` and `urp24/fix-city/Assets`; `diff -r --strip-trailing-cr` against `urp23/base-building` / `base-city` -> **exit 0, 0 lines** each (`diff-fix-building.txt`, `diff-fix-city.txt`). Per-code Unresolved from the authoritative JSON reports: building **0**, city **3** (`ambiguous-material 2`, `prefab-material-assignment 1`), unchanged from both baselines.
