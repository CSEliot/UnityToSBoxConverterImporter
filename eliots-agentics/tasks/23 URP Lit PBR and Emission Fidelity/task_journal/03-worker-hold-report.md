# 03 - Worker 23 final report (HOLD) - 2026-11-26 09:32

Source: completion notice for deleg_e8102e95 / sa-0-b83c7df4 (dispatched 09:11, landed 09:22), plus the parent's on-disk verification at 09:23-09:26. The worker was STEERED to HOLD after the Director corrected the session target to Unity 6.3 (6000.3.29f1 / URP 17.3.0); its PERMISSION brief had carried the now-RETRACTED 17.7.0 re-ground.

## What the worker reports
- Held: no commits, no staging, no further harness or generated-output gates, no DONE? YES!, no Work Review, no reverts.
- Edited (unstaged, left in place): `Editor/Core/UnityMaterial.cs` ONLY - four changes:
  1. Split keyword state into three separate sets (`m_ValidKeywords` ON, `m_InvalidKeywords` OFF, `m_ShaderKeywords` legacy ON); `ReadKeywords` no longer merges the OFF set into the ON set; added `HasValidKeyword` / `HasInvalidKeyword`; updated the call site.
  2. Added developer-tweakable NEW MAPPING properties for URP packed channels (`UrpMetallicChannel`, `UrpGlossChannel`, `UrpGlossInvert`, `UrpGlossScale`, `UrpOcclusionChannel`, `UrpParallaxChannel`, `UrpParallaxScale`, `UrpParallaxInvert`) plus `EmissionReportedNotConverted` / `EmissionSourceColor`, all gated on `Pipeline == Urp`.
  3. `PrepareChannels`: URP-gated packed-channel branch for `_MetallicGlossMap`, `_OcclusionMap`, `_ParallaxMap`; non-URP paths guarded with `!Urp`.
  4. `ToVmat`: suppress `_EmissionMap`-driven emission when URP has no valid `_EMISSION` keyword, preventing fabricated `F_SELF_ILLUM`.
- Did NOT edit `Editor/Core/UnityImport.cs` or any other `Editor/Core` file.
- Created `task_journal/02-before-observation.md` (BEFORE observation: 0 emission-disabled report lines).
- Verification: only PRE-EDIT gates ran (compile, BEFORE import, HEAD baselines for building/city--1). After the edits: no build, no harness run, no `verify_output.py`, no byte diff. The edited tree is UNVERIFIED and INCOMPLETE.

## Parent on-disk verification (09:23-09:26)
- `git diff --numstat -- Editor/Core/` = 88 additions / 14 deletions in `UnityMaterial.cs`; nothing else under `Editor/Core` modified. Matches the worker's file-ownership claim.
- Harness compile gate on the current tree is RED: `BUILD_EXIT=1`, `error CS0117: 'UnityUrp' does not contain a definition for 'LitShaderName'` at `Editor/Core/UnityMaterial.cs(93,76)`, 0 warnings / 1 error. The constant is `UrpCounts.LitShaderName` at `Editor/Core/UnityUrp.cs:27`, NOT a member of `UnityUrp`. This is a REAL defect in the partial edit; the parent did NOT patch it (no silent patching of a real defect).
- `02-before-observation.md` exists, but its harness run used the 17.7.0 fixture BEFORE the Director restored 6.3, so it must be re-run against 6.3 output before it can be used as evidence.

## Premise finding to re-scope when task 23 resumes
The worker reports the 16-bpp TGA staging-seam normalisation is ALREADY implemented at HEAD via `UnityTgaRewrite.Normalize`, called from `UnityImport.PrepareFiles`; it only inspected it and did not edit `UnityImport.cs`. If verified, task.md Work item 3 and acceptance #5 are a NO-OP and must be re-scoped to an assertion that the existing seam is correct and gated, rather than asking a worker to add it. Parent is verifying against `git show HEAD:Editor/Core/UnityImport.cs` on disk.

## State
All edits UNSTAGED. No commit, no push, no critic, no DONE? YES!. HELD pending the Director's decision (drive task 29 / revert or continue worker 23) and re-verification on the restored 6.3 fixture. The 17.7.0 pins in this worker's brief are RETRACTED; see task.md `## Director Correction (2026-11-26 09:14, BINDING)` and its `UPDATE 09:21`.
