# Task 22 re-scope - Director decision (2026-11-26 08:10)

Director answer to the open scope question: "keep Shader Graph materials report-only per task 26".

Parent re-measure at 08:10 (unchanged from 05:46): `_ALPHATEST_ON` 105 materials / 0 in URP/Lit; `_SURFACE_TYPE_TRANSPARENT` 33 / 0; `_EMISSION` 83 / 0; `_NORMALMAP` 30 / 27. Three of the four originally targeted keywords live only on Shader Graph materials (`Pipeline == Unknown`); `_NORMALMAP` is on URP/Lit but already converts at the HEAD baseline. `_EMISSION` is valid 0 times and invalid 83 times, so a colour-only emission rule would fabricate illumination.

The uncommitted Core tree still carries the rejected expansion: `UnityUrp.cs:68-91` (24 hardcoded `.shadergraph.meta` guids, `ShaderGraphIndex`, `IsUrpSurface`, five NEW MAPPING knobs) and `UnityMaterial.cs:24,35,40,46,48,52,60,103` (`disabledKeywords`, `HasDisabledKeyword`, `UrpSurface`, `UrpEmission`). Confirmed absent at HEAD. A Shader Graph material with `_ALPHATEST_ON` therefore gains `F_ALPHA_TEST`, which the HEAD baseline did not emit; that is the real failing assertion for the re-scoped task.

Disposition:
- Rejected: the Shader Graph guid table and `IsUrpSurface`, `SurfaceSemanticsForShaderGraphs`, `EmissionFromColorWithoutMask`, `NormalMapProperty`, `NormalMapScaleDefault`.
- Rejected: `UrpEmission`, `HasDisabledKeyword`, `disabledKeywords`; restore `ReadKeywords` to its HEAD shape.
- Kept: `AlphaTestReferenceDefault` as the URP/Lit alpha-reference default; keyword-driven alpha-clip / transparent rules and the existing normal-map path, all gated on `Pipeline == UnityPipeline.Urp`.
- New acceptance focus: Shader Graph `.vmat` output byte-identical to the HEAD baseline, plus the standard harness/compile/verifier gates.

No code was changed by the Master. The worker's uncommitted tree is preserved as-is for the continuation. Next: continue `sa-0-0a97a8d6` on the corrected `task.md`, then dispatch a Review Critic.
