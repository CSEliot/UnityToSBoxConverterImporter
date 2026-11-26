# 04 - HEAD baseline, byte identity and pipeline gating - task 23

2026-11-26. `git archive HEAD` was used READ-ONLY to build the baseline; no add/commit/push/stash/
checkout/restore/reset was performed. `S=C:/Users/Eliot/AppData/Local/hermes/profiles/hermes-sbox/cache/scratch/urp23`.

## Exact commands

```
cd C:/cseliot/1-Development/1-SBox/2-Projects/unityimporterplus_branch_2
rm -rf "$S/head-src"; mkdir -p "$S/head-src"
git archive HEAD | tar -x -C "$S/head-src"          # ARCHIVE_EXIT=0

# git archive writes CRLF blobs; normalise to the same LF EOL as the edited working tree BEFORE building.
python3 -c 'import pathlib; r=pathlib.Path(r"<S>/head-src/Editor/Core"); [p.write_bytes(p.read_bytes().replace(b"\r\n",b"\n")) for p in r.glob("*.cs")]'
# verified: [('GeneratedAssetFiles.cs', False, 0, 105), ('ImportCompletion.cs', False, 0, 60), ('ImportMerge.cs', False, 0, 164)]

dotnet build "$S/head-src/Documentation/Unity-To-SBox-Automated/harness/harness.csproj" -c Release -p:IUP_ROOT="$S/head-src" --no-incremental 2>&1 | tee "$S/head-build-lf.txt"
HEAD_BUILD_EXIT=0

HB="$S/head-src/Documentation/Unity-To-SBox-Automated/harness/bin/Release/net10.0/harness.dll"
H=C:/cseliot/1-Development/1-SBox/2-Projects/unityimporterplus_branch_2/Documentation/Unity-To-SBox-Automated/harness/bin/Release/net10.0/harness.dll
T=C:/cseliot/1-Development/1-SBox/2-Projects/unityimporterplus/Testing
for n in building city--1; do
  dotnet "$HB" "$T/$n.unitypackage" "$S/head-$n" "$S/head-$n.json" > "$S/head-$n.log" 2>&1
  dotnet "$H"  "$T/$n.unitypackage" "$S/after-$n" "$S/after-$n.json" > "$S/after-$n.log" 2>&1
done
```

## Exact build output (HEAD sources, LF-normalised)

```
  Determining projects to restore...
  All projects are up-to-date for restore.
  harness -> C:\Users\Eliot\AppData\Local\hermes\profiles\hermes-sbox\cache\scratch\urp23\head-src\Documentation\Unity-To-SBox-Automated\harness\bin\Release\net10.0\harness.dll

Build succeeded.
    0 Warning(s)
    0 Error(s)

Time Elapsed 00:00:01.98
HEAD_BUILD_EXIT=0
```

## Run exits and telemetry (same harness mode for HEAD and AFTER)

```
HEAD_building_EXIT=0
AFTER_building_EXIT=0
HEAD_city--1_EXIT=1
AFTER_city--1_EXIT=1
```

(city--1 exit 1 is the expected non-empty `Unresolved` data condition.)

## Byte identity and per-code Unresolved

```
### building
DIFF_LINES=0
### city--1
DIFF_LINES=0

building head {} after {} same True
city--1 head {'ambiguous-material': 2, 'prefab-material-assignment': 1} after {'ambiguous-material': 2, 'prefab-material-assignment': 1} same True
```

`DIFF_LINES` is the line count of `diff -r -q "$S/head-$n" "$S/after-$n"`; zero means every
generated file is byte-identical. The only report-JSON differences are `Destination` (different
scratch folders) and `TextureProcessing.ElapsedMilliseconds` (wall-clock), plus no new
`Information` line for these packages. Unresolved per-code breakdowns are identical.

## Pipeline gating / HDRP

Every new conversion rule is gated on `Pipeline == Urp` (`UnityMaterial.cs:77-99,241-255,346-349`),
and the collector only sees `EmissionReportedNotConverted`, which itself requires `Urp` and
`ShaderName == UrpCounts.LitShaderName`. Standard/HDRP conversion paths are therefore unchanged by
construction; the byte-identical `building`/`city--1` runs confirm the non-URP output path on real
packages.

An HDRP package was attempted for an extra output-level check but is BLOCKED by a pre-existing,
unrelated archive guard, identically on HEAD and AFTER (no generated files on either side, hence
no difference):

```
head_HDRP_EXIT=2
after_HDRP_EXIT=2
error: Asset path must start with Assets/: ProjectSettings/InputManager.asset
HDRP tree diff DIFF_LINES=0
```

No HDRP output claim is made from that run; the HDRP assertion rests on the URP gate plus the
byte-identical package runs above.
