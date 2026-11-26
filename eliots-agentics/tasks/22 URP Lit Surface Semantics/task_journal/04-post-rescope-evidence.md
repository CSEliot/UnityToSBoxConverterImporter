# 04 — Post-rescope evidence (2026-11-26)

Scope: Director 2026-11-26 08:10 kept Shader Graph materials report-only (task 26). The earlier expansion (`ShaderGraphGuids`, `IsUrpSurface`, colour-only `_EMISSION`) was removed before the gates below. No git commands, no commit, no editor round, no fixture/tooling added. Harness: `Documentation/Unity-To-SBox-Automated/harness/harness.csproj`, all runs no-scenes; SyntyForest project runs use `--folders '*'`. Scratch root `C:/Users/Eliot/AppData/Local/hermes/profiles/hermes-sbox/cache/scratch/urp22`; HEAD-code baseline trees from task 21 at `.../scratch/urp21/base_building` and `.../base_city`. Fixture packages read from `C:/cseliot/1-Development/1-SBox/2-Projects/unityimporterplus/Testing/{building,city--1}.unitypackage`.

## Failing assertion written and run FIRST (current uncommitted tree)

Harness rebuilt from the then-current tree: `Build succeeded. / 0 Warning(s) / 0 Error(s)`. SyntyForest run into `urp22/pre-rescope`: `assets=1926 converted=1617 files=2862 changed=2862`; `INFO URP detected: 217 URP material(s), 27 URP Lit, 186 Shader Graph material(s), 14 URP component(s).`

Literal source: `Assets/Synty/PolygonNatureBiomes/PNB_Meadow_Forest/Materials/Plants/WildFlowers_01.mat` has `m_ValidKeywords: - _ALPHATEST_ON`, no `_AlphaClip`, no `_Cutoff`, and a Shader Graph shader guid (`Pipeline == Unknown`). Assertion: its generated `.vmat` must have NO `F_ALPHA_TEST`. Scratch checker (`check_rescope.py`, not a repo deliverable), real output:

```
A1 WildFlowers_01 has NO F_ALPHA_TEST: FAIL | F_ALPHA_TEST_1= True
A2 Circle_Cutout_Mat Shader Graph byte-identity: PASS
A4 URP/Lit Cobblestone byte-identity: PASS
A4 all terrain/ URP/Lit .vmat unchanged: PASS | changed= []
A3 no fabricated self-illum synty/pnb_core/materials/birds_01.vmat: PASS
A3 no fabricated self-illum synty/polygonnaturebiomes/pnb_meadow_forest/terrain/rock_texture_01.vmat: PASS
```

`diff -rq urp22/base/imported urp22/pre-rescope/imported` named exactly one changed file: `wildflowers_01.vmat`. Disclosed limitation: the re-scoped task says the chosen transparent Shader Graph case also fails today; it did not — `Circle_Cutout_Mat.vmat` (and every other generated file except `wildflowers_01.vmat`) was already byte-identical, so A2 is a regression guard rather than a reproduced failure. A3 also could not fail because `UrpEmission` had never been wired into `ToVmat`; the helper was nevertheless removed as instructed.

## Fix

- `Editor/Core/UnityUrp.cs`: removed `ShaderGraphGuids`, `ShaderGraphIndex`, `IsUrpSurface`, `SurfaceSemanticsForShaderGraphs`, `EmissionFromColorWithoutMask`, `NormalMapProperty`, `NormalMapScaleDefault`. Retained only the developer-tweakable `AlphaTestReferenceDefault = 0.5`.
- `Editor/Core/UnityMaterial.cs`: removed `UrpSurface`, `UrpEmission`, `HasDisabledKeyword`, `disabledKeywords`; restored `ReadKeywords( string, HashSet<string> )` to its HEAD shape (valid/legacy keywords only) and its single call site. Keyword rules now gate on `bool Urp => Pipeline == UnityPipeline.Urp`. Existing `_NORMALMAP` path untouched.
- Line-ending/BOM check after edit: `UnityMaterial.cs bom_removed=False crlf_normalized=446 lf_only=True`; `UnityUrp.cs bom_removed=False crlf_normalized=0 lf_only=True`. Rejected-symbol grep over `Editor/Core/*.cs`: `NONE`.

## Gate 1 — harness compile (post-fix, named gate)

`dotnet build Documentation/Unity-To-SBox-Automated/harness/harness.csproj -c Release -p:IUP_ROOT=<project> --no-incremental`
→ `Build succeeded.` / `0 Warning(s)` / `0 Error(s)` / `Time Elapsed 00:00:03.94`.

## Gate 2 — SyntyForest post-fix, same mode

`assets=1926 converted=1617 files=2862 changed=2862`
`INFO URP detected: 217 URP material(s), 27 URP Lit, 186 Shader Graph material(s), 14 URP component(s).`

```
A1 WildFlowers_01 has NO F_ALPHA_TEST: PASS | F_ALPHA_TEST_1= False
A2 Circle_Cutout_Mat Shader Graph byte-identity: PASS
A4 URP/Lit Cobblestone byte-identity: PASS
A4 all terrain/ URP/Lit .vmat unchanged: PASS | changed= []
A3 no fabricated self-illum synty/pnb_core/materials/birds_01.vmat: PASS
A3 no fabricated self-illum synty/polygonnaturebiomes/pnb_meadow_forest/terrain/rock_texture_01.vmat: PASS
```

`diff -rq urp22/base/imported urp22/post-rescope/imported` → empty output: all 2862 generated files byte-identical to the HEAD baseline.

## Gate 3 — building / city--1 (no-scenes, HEAD baseline trees)

| Fixture | files vs baseline | `assets=` | `diff -rq .../imported` | per-code Unresolved |
|---|---|---|---|---|
| building | 24 / 24 | `assets=15 converted=10 files=24 changed=24` | exit `0`, diff file 0 lines | none (0 unresolved) |
| city--1 | 1166 / 1166 | `assets=729 converted=776 files=1166 changed=1166` | exit `0`, diff file 0 lines | `2 ambiguous-material`, `1 prefab-material-assignment` |

## Gate 4 — verify_output.py

`python Documentation/Unity-To-SBox-Automated/verify_output.py <outRoot> <outRoot>/imported`:
- SyntyForest post-rescope → `checked 1589 files, 0 problems`
- building-after → `checked 10 files, 0 problems`
- city-after → `checked 775 files, 0 problems`

## Gate 5 — Unresolved codes

SyntyForest report JSON before vs after: `base-report.json total= 31 {'missing-texture': 13, 'prefab-material-assignment': 1, 'unassigned-material': 17}`; `post-rescope-report.json total= 31 {'missing-texture': 13, 'prefab-material-assignment': 1, 'unassigned-material': 17}`. No new code, no growth in any existing code. building 0, city--1 per-code breakdown unchanged (above). The three codes not exercised by these fixtures have count 0 on both sides.

## Remaining

Implementation and all requested gates complete on disk; Master owns review/commit. `status.md`/`task.md` untouched this turn, no other task folder edited.
