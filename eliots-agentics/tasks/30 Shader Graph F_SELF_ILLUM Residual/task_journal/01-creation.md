# 01-creation.md - BEFORE observation (FAILING assertion)

Created 2026-11-26 by the Task Master at the Director's request (Option A turn, verbatim: "And yes open a separate task for the pre-existing Shader Graph F_SELF_ILLUM residual."). Parent-authored; evidence re-verified on disk in the parent's own shell, not taken from a child's prose.

## The symptom
Two generated `.vmat` files contain `F_SELF_ILLUM` even though their Unity source materials have `_EMISSION` explicitly OFF. This is the FAILING assertion for this task.

Generated paths (from `grep -rln "F_SELF_ILLUM" .../urp23/after/imported --include='*.vmat'`, run 2026-11-26):
- `C:/Users/Eliot/AppData/Local/hermes/profiles/hermes-sbox/cache/scratch/urp23/after/imported/synty/polygongeneric/materials/fx/fire/gen_fireframes_grey_01.vmat`
- `C:/Users/Eliot/AppData/Local/hermes/profiles/hermes-sbox/cache/scratch/urp23/after/imported/synty/polygongeneric/materials/fx/fire/gen_fireframes_large_grey_01.vmat`

COUNT: exactly 2 generated `.vmat` with `F_SELF_ILLUM` in the AFTER tree.

## Source keyword state (parent-verified verbatim)
`C:/cseliot/1-Development/4-Unity/2-Projects/SyntyForest/Assets/Synty/PolygonGeneric/Materials/FX/Fire/Gen_FireFrames_01.mat`
- `:11` `m_Shader: {fileID: -6465566751694194690, guid: 0736e099ec10c9e46b9551b2337d0cc7, type: 3}`
- `:14` `m_ValidKeywords:` (contains `_ALPHATEST_ON`, `_BUILTIN_SURFACE_TYPE_TRANSPARENT`, `_SURFACE_TYPE_TRANSPARENT`; NO `_EMISSION`)
- `:18` `m_InvalidKeywords:` / `:19` `- _EMISSION`  <- explicitly OFF
- `:20` `m_LightmapFlags: 10`
- `:54` `- _EmissionMap:` / `:160` `- _EmissionColor: {r: 2, g: 0.9522445, b: 0, a: 1}`

`Gen_FireFrames_Large_01.mat`
- `:37` `m_Shader: {fileID: -6465566751694194690, guid: dfec08fb273e4674bb5398df25a5932c, type: 3}`
- `:40` `m_ValidKeywords:` / `:45` `m_InvalidKeywords:` / `:46` `- _EMISSION`  <- explicitly OFF
- `:47` `m_LightmapFlags: 10`
- `:81` `- _EmissionMap:` / `:200` `- _EmissionColor: {r: 1.6837997, g: 0.73922914, b: 0, a: 1}`

There is NO `m_ShaderKeywords` line in either file. So `_EMISSION` is OFF in every serialized form the importer reads.

## Pre-existence at HEAD (NOT a regression)
`git show HEAD:Editor/Core/UnityMaterial.cs | grep -n "F_SELF_ILLUM"` -> `:280` `if ( emission != null ) Feature( "F_SELF_ILLUM 1" );` with NO keyword check. HEAD de1f01e. Therefore the residual is pre-existing, not introduced by task 23 or by the Unity 6.7 / URP 17.7.0 upgrade.

## Why the current URP-only gate does not suppress it
`Editor/Core/UnityMaterial.cs:349` (current tree): `if ( Urp && emission != null && !HasValidKeyword( "_EMISSION" ) ) emission = null;`
`Editor/Core/UnityMaterial.cs:354`: `if ( emission != null ) Feature( "F_SELF_ILLUM 1" );`

Both fire FX shader guids (`0736e099ec10c9e46b9551b2337d0cc7`, `dfec08fb273e4674bb5398df25a5932c`) appear in NEITHER pipeline table (`Editor/Core/UnityUrp.cs:57-61`, `Editor/Core/UnityHdrp.cs:142-143`). `PipelineOf` therefore returns Unknown, `Urp` is false, and the `Pipeline == Urp` gate never applies. That is why these two `.vmat` still carry `F_SELF_ILLUM`.

## Corrected record (two errors, kept as history)
- Worker 23 claimed these were "Shader Graph fire FX materials with a VALID `_EMISSION` keyword" and "legitimately glow". FALSE - the keyword is in `m_InvalidKeywords`, i.e. OFF.
- The parent's 23:37 note called this an "emission gate INCOMPLETE" defect and demanded a global ZERO assertion. FALSE - that was a misattribution; a global-zero bar would require breaking report-only Shader Graph fidelity (Director 08:10 / task 26). The worker was right to refuse it, for the wrong reason. The correct reason is SCOPE: Unknown pipeline, report-only Shader Graph, pre-existing at HEAD.

## Status
FAILING assertion recorded. No worker dispatched yet (queued behind the 4A serial sequence: task 23, then 24/25/26). No code edits, no git writes, no editor/sbox launch, no Signal (banned), no ntfy.
