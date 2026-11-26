# Task 25 - verification evidence (final bytes, HEAD 56192a0)

All artifacts are on disk under
`C:/Users/Eliot/AppData/Local/hermes/profiles/hermes-sbox/cache/scratch/urp25/`
unless a task-24 path is named. The final harness rebuild happened AFTER the last code edit:
`build-final.log` = `Build succeeded. 0 Warning(s) 0 Error(s)`; `dll-before.sha` and
`dll-after.sha` are identical, so all cited artifacts came from the final DLL (no SyntyForest rerun needed).

## Gate 1 - harness build: PASS
`dotnet build Documentation/Unity-To-SBox-Automated/harness/harness.csproj -c Release -p:IUP_ROOT=... --no-incremental`
-> `0 Error(s), 0 Warning(s)` (`build.log`, `build-final.log`).

## Gate 2 - fresh SyntyForest `--scenes`: PASS (exit 1 with unresolved = data)
Command: `harness --scenes --folders '*' <SyntyForest> .../urp25/after/Assets .../urp25/after-report.json`.
`after.log` ends with `URP detected: 217 URP material(s), 28 URP Lit, 186 Shader Graph material(s), 12 URP component(s).`
`verify_output.py .../urp25/after/Assets .../urp25/after/Assets/imported` -> `checked 1627 files, 0 problems` (`verify.log`).

## Acceptance 1 - camera, both sides (`probe.out`, SampleScene)
| Field | Source line(s) | Generated |
|---|---|---|
| ClearFlags | URP sibling `m_ClearDepth: 1` | `ClearFlags: All` |
| BackgroundColor | Camera `m_BackGroundColor: {r:0.19215687,g:0.3019608,b:0.4745098}` | `0.03071,0.07421,0.1912,1` (sRGB->linear) |
| FieldOfView | `field of view: 60` | `60` |
| ZNear | `near clip plane: 0.3` m x 39.37 | `11.811` |
| ZFar | `far clip plane: 1000` m x 39.37 | `39370` |
| EnablePostProcessing | URP sibling `m_RenderPostProcessing: 1` | `True` |
| DisableAntiAliasing | URP sibling `m_Antialiasing: 0` | `True` |

`ASSERT camera colour=True`, `ASSERT camera FOV=True near=True far=True`.
The muted Demo/Overview cameras show the other half from source: `m_RenderPostProcessing: 0` ->
`EnablePostProcessing: false` (6 mapped lines in the report; `probe.out` lists each).

## Acceptance 2 - light, both sides (`probe.out`)
Source SampleScene `Light:` `m_Type: 1` (UnityCsReference `GraphicsEnums.cs:72` Directional = 1; the nested
`m_Shadows.m_Type: 2` is the shadow type, not the light type), `m_Color {1,1,1,1}`,
`m_Intensity: 2`, `m_Range: 10`, `m_UseColorTemperature: 1`, `m_ColorTemperature: 5000`.
Generated: `Sandbox.DirectionalLight`, `LightColor: 1,0.77997,0.61952,1` (source white x the grounded
5000 K tint), `Intensity: 2`, `Shadows: true`. `ASSERT light colour incl. CCT=True intensity=True`.
All four fixture light blocks are `m_Type: 1, m_Range: 10`; `Sandbox.DirectionalLight` has no `Radius`
field, so the punctual `m_Range -> Radius` half is NOT exercised by this fixture and is reported as
unproven, not fabricated.

## Acceptance 3 - every URP-only field reported (`probe.out`, `probe2.out`)
The pre-review `after-report.json` carried **172** URP-only lines plus 6 mapped lines, but its six
`m_VolumeLayerMask` values were truncated (`{serializedVersion: 2}`) and only SampleScene's six TAA
fields were reported. AFTER the review fixes, `after2-report.json` carries **202** URP-only lines
(172 + 30 newly reported TAA fields) plus the same **6** mapped lines. `probe2.out` proves 6 TAA lines
for each of the 6 camera bodies (36 total: SampleScene's lowercase aliases and the `m_*` form in the
5 other bodies); all 6 `m_VolumeLayerMask` lines and both Demo light layer-mask lines now include
`m_Bits: 1`. Representative values: `m_UsePipelineSettings=1`,
`m_AdditionalLightsShadowResolutionTier=2`, `m_SoftShadowQuality=1`, `m_LightCookieSize={x: 1, y: 1}`,
`m_VolumeLayerMask={serializedVersion: 2 m_Bits: 1}`, `m_RendererIndex=-1`,
`m_TaaSettings.m_Quality=3` (SampleScene keeps the authored `m_TaaSettings.quality=3` alias).
No shadow tier -> cascade count, soft quality -> hardness, or cookie size -> Cookie substitution was
made. The `m_ClearDepth: 0` half has no fixture body.
**CORRECTION (2026-11-27 01:15):** the earlier claim that the mask values were already complete in the
`after` report was wrong; the truncated forms are superseded by the `after2` lines above.

## Acceptance 4 - engine-schema conformance (`probe.out`, `=== Acceptance 4 ===`)
Diffed generated components against P3 oracles under
`C:/cseliot/1-Development/1-SBox/1-Engine-Builds/Dev/sbox-public`:
`game/core/templates/gameobject/Camera.prefab`, `game/templates/game.minimal/Assets/scenes/minimal.scene`,
`game/templates/game.playercontroller/Assets/scenes/basic_example.scene`,
`game/download/assets/scenes/lesson_1_day_1.c66ba5b5e4e5364c.scene`,
`game/core/templates/gameobject/Light - Point.prefab`, `.../Light - Spot.prefab`,
`game/addons/menu/Assets/scenes/reward-unbox.scene`.
No generated key lacked an engine property, so **no schema fix was needed** (byte-identical key sets are
not the bar). All remaining deltas and their justification:
- generated `Sandbox.CameraComponent`: adds `Flags` + six null `OnComponent*` callbacks (authored in
  `basic_example.scene:21`) and `DisableAntiAliasing` (engine property `CameraComponent.cs:143`; emitted
  only on HDRP/Urp-gated cameras); omits authored `PostProcessAnchor` / `RenderTexture` (object
  references with no grounded Unity source value). No `__version`, as authored.
- generated `Sandbox.DirectionalLight`: adds `Flags` + callbacks (`basic_example.scene:69`), `Intensity`
  and `__version: 1` (both authored in `lesson_1_day_1.scene:386`); omits `Contribution`, `Cookie`,
  `ShadowBias`, `ShadowHardness`, `Visualizer`, cascade/animation/contact-shadow fields (no grounded
  Unity Light value; engine defaults preserved).
- no generated PointLight/SpotLight exists in the four-scene fixture, so those P3 oracles are recorded
  key sets only (`Attenuation/ConeInner/ConeOuter/FogMode/FogStrength/LightColor/Radius/Shadows`, plus
  `Intensity/__version` in `lesson_1`) - this half is unproven against generated output.
- enum values are engine-authored: `ClearFlags: "All"/"Color"` (`CameraComponent.cs:27`),
  `FovAxis: "Horizontal"/"Vertical"` (`Axis` enum at `CameraComponent.cs:968-980`).

## Task-24 regression gate - PASS
`diff -r --strip-trailing-cr .../urp24/after3/Assets/imported .../urp25/after/Assets/imported`
(`scene-diff.txt`, 27 lines, exit 1). Every changed line is `DisableAntiAliasing` or
`EnablePostProcessing` on `Sandbox.CameraComponent` in `SampleScene`, `Demo`, and PNB `Overview`;
PolygonGeneric `Overview` is unchanged. `probe.out` reports the same classification structurally:
changed leafs 1 / 0 / 7 / 2, all `[Sandbox.CameraComponent]`, zero light-component deltas.
PPV subtrees are IDENTICAL in all four scenes (122/58/270/58 keys, zero diffs). `ppv-values.txt`
confirms the authored values still present: Bloom Strength 0.25 (SampleScene) and 2.19 (Demo),
Vignette Intensity 0.2, AO Intensity 0.4, Tonemapping HableFilmic with ExposureCompensation 1.58.

## Gate 5 - building / city--1 without `--scenes`: PASS
`diff -r --strip-trailing-cr .../urp23/base-building .../urp25/base-building` -> exit 0, 0 lines
(`building-diff.txt`); `diff -r --strip-trailing-cr .../urp23/base-city .../urp25/base-city` -> exit 0,
0 lines (`city-diff.txt`). `building.json`: Unresolved 0, Warnings 0, Information 13, exit 0.
`city.json`: Unresolved 3 (`ambiguous-material` 2 + `prefab-material-assignment` 1), Warnings 3,
Information 749, exit 1 (expected unresolved data).

## Gate 6 - Import Error Outputs: PASS, one flagged growth
`after-report.json` exact counts: **Unresolved 31, Warnings 122, Information 7216**; per-code Unresolved
`missing-texture 13 + prefab-material-assignment 1 + unassigned-material 17` - identical to
`urp24/after3-report.json` (31/122/7038) for Unresolved and Warnings. The harness emitted no separate
`issues:` stdout line in either log; counts are read from the report JSON (`grep '^issues'` returned none).
**FLAGGED:** Information grew by **178 = 172 URP-only lines + 6 mapped-field lines**, exactly the new
report output; no new issue category. Master to classify.

## 2026-11-27 01:15 - post-review gates on final bytes (findings 1-5)

Fresh build: `build-r2.log` = `Build succeeded. 0 Warning(s) 0 Error(s)`.
Fresh SyntyForest `--scenes`: `after2.log`, `after2-report.json`, exit 1 with unresolved (data).
`verify_output.py .../after2/Assets .../after2/Assets/imported` -> `checked 1627 files, 0 problems`
(`verify-r2.log`). `after2-report.json`: Unresolved **31** (13 missing-texture + 1
prefab-material-assignment + 17 unassigned-material), Warnings **122**, Information **7246**,
URP-only **202**, mapped **6**. Versus the pre-review 7216: **+30 net** = 30 newly reported TAA
fields in the 5 bodies that used `m_*`; SampleScene's 6 lowercase names are unchanged, and 8 mask
lines changed text in place (8 gained + 8 lost, net 0) because `m_Bits` was recovered. The 6 camera
TAA counts are 6/6/6/6/6/6 (`probe2.out`: SampleScene Main Camera, Demo Main Camera (1), Camera (2),
(3), (4), PNB Overview Main Camera). All 6 `m_VolumeLayerMask` lines and both Demo light mask lines
contain `m_Bits: 1`.
Generated scene trees: `diff -r --strip-trailing-cr after/Assets/imported after2/Assets/imported`
-> **exit 0, 0 lines** (`scene-diff-r2.txt`); the report fixes are output-neutral, and no URP body in
this fixture omits `m_RenderPostProcessing`, so the source-default fix changes no emitted file.
building/city--1 re-run after the final edit (no `--scenes`): exit 0 / 1; `diff -r --strip-trailing-cr`
against `scratch/urp23/base-building` and `base-city` -> exit 0 / 0 lines for both
(`building-diff-r2.txt`, `city-diff-r2.txt`); per-code Unresolved 0 and 3 (2 ambiguous-material + 1
prefab-material-assignment). All earlier acceptance numbers still hold; the flag above now reads
+30 Information, fully attributed to the two High fixes.

## Evidence index
`build-r2.log`, `after2.log`, `after2-report.json`, `verify-r2.log`, `probe2.py`, `probe2.out`,
`scene-diff-r2.txt`, `building-r2.json`, `city-r2.json`, `building-diff-r2.txt`, `city-diff-r2.txt`
are the final-round artifacts; the first-round files named above remain for the before/after claim
(all under scratch/urp25/). Scratch probes are not in the repo.
