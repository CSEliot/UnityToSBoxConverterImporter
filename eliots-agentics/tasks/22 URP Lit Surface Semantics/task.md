## Task
Convert the URP Lit surface state (alpha clipping, transparency, normal map, emission enable) that the Standard path currently drops.

### Description
Grounding: keywords are parsed at Editor/Core/UnityMaterial.cs:128-152 but their only consumers (:35 HdrpFeature, :61 Transmission, :63 Subsurface) require the Hdrp local (:31). With Pipeline == Unknown for URP, `_NORMALMAP`, `_ALPHATEST_ON`, `_SURFACE_TYPE_TRANSPARENT`, `_EMISSION` and `_SurfaceType` are silently dropped. `_Surface` is read only as a legacy Standard scalar (`_Surface == 1` -> F_TRANSLUCENT at :37), and `_EmissionColor` is read only inside `if ( emission != null )` at :319, so an emission colour with no emission texture is dropped along with the whole Self Illum section (:266, :324, :344).

Observed in SyntyForest (217 .mat): m_ValidKeywords `_ALPHATEST_ON` 105, `_SURFACE_TYPE_TRANSPARENT` 33, `_NORMALMAP` 27; m_InvalidKeywords `_EMISSION` 83; `_EmissionColor` 196, `_EmissionMap` 90, `_Surface` 204, `_AlphaClip` 205. URP Lit declares these features at Shaders/Lit.shader:121,125,126,128. Whether an m_InvalidKeywords entry is active at runtime is UNVERIFIED: ground it in URP source before treating `_EMISSION` as enabled.

Work, all gated on `Pipeline == Urp`:
1. `_ALPHATEST_ON` / `_AlphaClip` -> F_ALPHA_TEST + g_flAlphaTestReference; `_SURFACE_TYPE_TRANSPARENT` and `_Surface` 0/1 -> F_TRANSLUCENT and the glass path selection at UnityMaterial.cs:249-250.
2. `_NORMALMAP` -> TextureNormal + g_flNormalMapScaleFactor from _BumpScale.
3. `_EMISSION` + `_EmissionColor` WITHOUT an emission texture -> F_SELF_ILLUM + g_vSelfIllumTint. Do not fabricate an emission texture the source lacks; label any new mapping as NEW MAPPING in the report.
4. Gate every rule on Urp so building and city--1 output is unchanged.

Write each failing assertion FIRST from the literal observed values above, run it on current output and confirm it fails before fixing. A task whose acceptance is "confirm our formula emitted what our formula says" cannot fail.

### Acceptance (one explicit pass/fail check per line)
1. For a chosen SyntyForest material with `_ALPHATEST_ON` and no `_AlphaClip` scalar, the generated .vmat has F_ALPHA_TEST 1 and a finite g_flAlphaTestReference. FAILS on current output; record the failing run.
2. For a chosen material with `_SURFACE_TYPE_TRANSPARENT` / `_Surface: 1`, the generated .vmat is translucent or glass. FAILS today.
3. For a chosen material with `_NORMALMAP` and `_BumpMap`, the generated .vmat has TextureNormal and a non-default g_flNormalMapScaleFactor.
4. For a chosen material with `_EmissionColor` and no `_EmissionMap`, the generated .vmat has F_SELF_ILLUM and g_vSelfIllumTint. FAILS today (colour dropped).
5. building and city--1 output byte-identical to a HEAD baseline in the same mode; per-code Unresolved breakdown unchanged.
6. Harness compile gate 0 errors; verify_output.py `checked N files, 0 problems`.

### Work Review


## Clarification Q&A
An (Director 2026-11-26 03:57, 1A/3A/5A): "Unity 6.7 URP" is the session label for the on-disk 6000.3.29f1 / URP 17.3.0 fixture; target the URP 17.x surface, not one editor build. Visual fidelity only. No new tooling or fixture package.

## Relevant History
See eliots-agentics/tasks/urp-support-research.md sections 2-3 for the verified census, exact commands and file:line evidence. HEAD bfc189b; session-start harness compile gate green.

## Notes
Standing constraints: gate every URP rule on `Pipeline == Urp`; keep Editor/Core/*.cs BOM-less and LF; route new paths through UnityImport.DiskPath; expose any NEW MAPPING as a developer-tweakable property. Editor rounds follow the 4A conditions: before any editor round, check that no Director-owned editor is already open on that project; never close an editor this work did not launch; NTFY before each large round.

## Stylistic Task Notes:
References to variables like X and Y automatically imply that these are to be exposed properties for developer tweaking.
When programming, portability and DRY are a priority.

## Post-Task Follow Ups
