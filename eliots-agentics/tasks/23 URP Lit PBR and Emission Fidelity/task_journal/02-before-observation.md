# 02 - BEFORE observation (failing assertion) - task 23

2026-11-26 (fresh run against the CURRENT Unity 6.7 / URP 17.7.0 SyntyForest fixture).
This file REPLACES the earlier observation, which ran before the Director deleted
`Demo_02_BIRP.unity`. Harness rebuilt from the current fixed sources FIRST. Recorded BEFORE
implementing the grouped emission report line. Full console:
`C:/Users/Eliot/AppData/Local/hermes/profiles/hermes-sbox/cache/scratch/urp23/before-console.txt`.

## Exact commands

```
cd C:/cseliot/1-Development/1-SBox/2-Projects/unityimporterplus_branch_2
dotnet build Documentation/Unity-To-SBox-Automated/harness/harness.csproj -c Release -p:IUP_ROOT=C:/cseliot/1-Development/1-SBox/2-Projects/unityimporterplus_branch_2 --no-incremental 2>&1 | tee C:/Users/Eliot/AppData/Local/hermes/profiles/hermes-sbox/cache/scratch/urp23/rebuild-before-build.txt
BUILD_EXIT=0

S=C:/Users/Eliot/AppData/Local/hermes/profiles/hermes-sbox/cache/scratch/urp23
H=C:/cseliot/1-Development/1-SBox/2-Projects/unityimporterplus_branch_2/Documentation/Unity-To-SBox-Automated/harness/bin/Release/net10.0/harness.dll
rm -rf "$S/before" "$S/before-report.json" "$S/before-console.txt"; mkdir -p "$S/before"
dotnet "$H" --folders Synty C:/cseliot/1-Development/4-Unity/2-Projects/SyntyForest "$S/before" "$S/before-report.json" > "$S/before-console.txt" 2>&1
HARNESS_EXIT=1
```

Harness exit 1 is expected: `Program.cs:200` returns 1 when `Unresolved` is non-empty. It is DATA,
not a run failure. No editor/browser/sbox process was launched.

## Exact build output

```
Determining projects to restore...
  All projects are up-to-date for restore.
  harness -> C:\cseliot\1-Development\1-SBox\2-Projects\unityimporterplus_branch_2\Documentation\Unity-To-SBox-Automated\harness\bin\Release\net10.0\harness.dll

Build succeeded.
    0 Warning(s)
    0 Error(s)

Time Elapsed 00:00:02.22
BUILD_EXIT=0
```

## Exact relevant harness output (verbatim lines from before-console.txt)

```
1:unity project: C:\cseliot\1-Development\4-Unity\2-Projects\SyntyForest
2:selected folders: Synty
3:archive assets: 2090
2094:assets=1925 converted=1617 files=2861 changed=2861
4305:INFO URP detected: 217 URP material(s), 28 URP Lit, 186 Shader Graph material(s), 9 URP component(s).
4306:INFO 162 unsupported files (such as scripts) were excluded.
4307:INFO 3 Unity scene(s) were not converted; enable scene import under Advanced to write s&box scenes for them.
```

`wc -l "$S/before-console.txt"` -> `4307`.
`find "$S/before/imported" -type f | wc -l` -> `2861` (the generated tree is populated; the
comparison below is not hollow).
Report JSON counters: `Assets 1925`, `Converted 1617`, `Files 2861`, `Unresolved 31`,
`Information 2066`, `Warnings 116`.
Fixture scene census (four `.unity` files, BIRP scene absent): `Assets/Scenes/SampleScene.unity`,
`Assets/Synty/PolygonGeneric/Scenes/Overview.unity`,
`Assets/Synty/PolygonNatureBiomes/PNB_Meadow_Forest/Scene/Demo.unity`,
`Assets/Synty/PolygonNatureBiomes/PNB_Meadow_Forest/Scene/Overview.unity`.

## Failing assertion (acceptance #1, BEFORE half)

Exact commands and their exact output:

```
grep -ic 'emission-disabled\|_EMISSION is OFF\|reported, not converted' "$S/before-console.txt"  -> 0
grep -in 'emission' "$S/before-console.txt" | head -20                                          -> (no output)
grep -ic 'emission-disabled\|_EMISSION is OFF\|reported, not converted' "$S/before-report.json" -> 0
grep -in 'emission' "$S/before-report.json" | head -20                                          -> (no output)
```

**FAIL:** ZERO report lines name an emission-disabled URP/Lit material. Neither `Mat_Rock_01` nor
`Mat_Rock_Moss_01` appears in an emission report line; the source colour `0.3679245` and the phrase
"reported, not converted" appear nowhere in the console or report JSON. This is the required
failing BEFORE observation. The AFTER run must produce exactly ONE grouped, deduped-by-asset-path
line with both names, `0.3679245`, `_EMISSION is OFF` and "reported, not converted".
