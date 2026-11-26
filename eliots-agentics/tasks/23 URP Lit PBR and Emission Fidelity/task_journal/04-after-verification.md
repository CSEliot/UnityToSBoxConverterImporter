# 04 - AFTER verification - task 23

2026-11-26. Harness rebuilt from the CURRENT FIXED sources before any generated-output claim.
Full console: `$S/after-console.txt`; full report: `$S/after-report.json`, where
`S=C:/Users/Eliot/AppData/Local/hermes/profiles/hermes-sbox/cache/scratch/urp23`.

## Commands

```
cd C:/cseliot/1-Development/1-SBox/2-Projects/unityimporterplus_branch_2
dotnet build Documentation/Unity-To-SBox-Automated/harness/harness.csproj -c Release -p:IUP_ROOT=C:/cseliot/1-Development/1-SBox/2-Projects/unityimporterplus_branch_2 --no-incremental 2>&1 | tee "$S/rebuild-after-build.txt"
BUILD_EXIT=0

S=C:/Users/Eliot/AppData/Local/hermes/profiles/hermes-sbox/cache/scratch/urp23
H=C:/cseliot/1-Development/1-SBox/2-Projects/unityimporterplus_branch_2/Documentation/Unity-To-SBox-Automated/harness/bin/Release/net10.0/harness.dll
rm -rf "$S/after" "$S/after-report.json" "$S/after-console.txt"; mkdir -p "$S/after"
dotnet "$H" --folders Synty C:/cseliot/1-Development/4-Unity/2-Projects/SyntyForest "$S/after" "$S/after-report.json" > "$S/after-console.txt" 2>&1
HARNESS_EXIT=1

python3 Documentation/Unity-To-SBox-Automated/verify_output.py "$S/after" "$S/after/imported"
```

Harness exit 1 is expected (`Program.cs:200`: non-empty `Unresolved` is data). No editor/sbox launch.

## Exact build output

```
  Determining projects to restore...
  All projects are up-to-date for restore.
  harness -> C:\cseliot\1-Development\1-SBox\2-Projects\unityimporterplus_branch_2\Documentation\Unity-To-SBox-Automated\harness\bin\Release\net10.0\harness.dll

Build succeeded.
    0 Warning(s)
    0 Error(s)

Time Elapsed 00:00:03.38
BUILD_EXIT=0
```

## Exact relevant harness output

```
2094:assets=1925 converted=1617 files=2861 changed=2861
4305:INFO 2 emission-disabled URP/Lit material(s) reported, not converted: Mat_Rock_01, Mat_Rock_Moss_01; source _EmissionColor 0.3679245/0.3679245/0.3679245; _EMISSION is OFF in each source material.
4306:INFO URP detected: 217 URP material(s), 28 URP Lit, 186 Shader Graph material(s), 9 URP component(s).
```

Report JSON line 29905 contains the same grouped Information line verbatim.

Acceptance #1 assertions (exact commands/output):

```
grep -c 'emission-disabled' "$S/after-console.txt"                 -> 1
grep -c 'emission-disabled' "$S/after-report.json"                 -> 1
grep -c 'Mat_Rock_01, Mat_Rock_Moss_01' "$S/after-console.txt"     -> 1
grep -c '0.3679245' "$S/after-console.txt"                         -> 1
grep -c 'reported, not converted' "$S/after-console.txt"           -> 1
grep -c '_EMISSION is OFF' "$S/after-console.txt"                  -> 1
```

Exactly ONE grouped line, deduplicated by asset path in `UnityImport.cs:671`, not two per-material
lines. Source names come from `asset.Path`; `m_Name` is not used.

## Acceptance #2 — F_SELF_ILLUM

Generated paths derived from each material's `_BaseMap` texture GUID (`8b97a5d5...` ->
`Rock_Texture_01.png`, `3c95c9e0...` -> `Rock_Moss_Texture_01.png`):

```
.../after/imported/synty/polygonnaturebiomes/pnb_meadow_forest/terrain/rock_texture_01.vmat              0
.../after/imported/synty/polygonnaturebiomes/pnb_meadow_forest/terrain/rock_moss_texture_01.vmat         0
```

(counts are `grep -c 'F_SELF_ILLUM'` on each exact generated file). Two OTHER files still contain
`F_SELF_ILLUM`: `gen_fireframes_grey_01.vmat` and `gen_fireframes_large_grey_01.vmat`. Their source
materials are Shader Graph-style FX materials with a VALID `_EMISSION` keyword and HDR colours;
they legitimately glow. They are not the two URP/Lit materials under test. See
`04-implementation-and-schema.md`.

## Acceptance #5/#7 — output validator and Unresolved breakdown

```
checked 1589 files, 0 problems
```

Report counters and per-code breakdown (BEFORE vs AFTER, identical):

```
{'Assets': 1925, 'Converted': 1617, 'Files': 2861, 'Unresolved': 31, 'Information': 2067, 'Warnings': 116}
before {'missing-texture': 13, 'prefab-material-assignment': 1, 'unassigned-material': 17}
after  {'missing-texture': 13, 'prefab-material-assignment': 1, 'unassigned-material': 17}
identical True
```

`Information` grew by exactly 1 (the grouped line); `Unresolved` and all codes are unchanged, with no
new missing-texture growth. Compile gate: 0 warnings, 0 errors (above).
