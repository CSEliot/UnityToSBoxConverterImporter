# Findings disposition - task 27 Review Critic (2026-11-26 06:12)

Critic: sa-0-51f248d3 (deleg_8d070762). Verdict: changes required, 8 findings. The critic set `REVIEWED? YES!`; the Task Master set it back to `REVIEWED? NO!` before fixing reviewed files, per protocol (the flag describes current bytes, not the last review).

All 8 findings ACCEPTED and re-verified against primary source before fixing:

1. [High] Pre-existing oracle projects: VERIFIED. Both directories exist; `minimal.scene:183-253` is a saved `Sandbox.Terrain` with full embedded `Storage`; `minimal.scene_d` has `heightmap` at byte offset 92 and `splatmap` at 17929; sbox scene mtime 2026-11-25 09:38; `unity_samples_oracle` has a 1,960,540-byte TerrainData, `SampleScene.unity:287` `!u!218`, 13 `.terrainlayer`. ROOT CAUSE: all three research children scoped their searches to `1-Engine-Builds/Dev/sbox-public` and none looked at `2-Projects/`. Fixed: CORRECTION banner, new section 1.1, revised section 1.2, all zero-instance claims scoped to `sbox-public`.
2. [High] Oracle-only questions already answerable read-only: ACCEPTED. Fixed: section 7.1 "answered read-only" and section 7.2 restricted to controlled ablations.
3. [Medium] `Storage` shape UNVERIFIED: ACCEPTED. Fixed: section 4 VERIFIED-from-sample block; UNVERIFIED item 2 narrowed to standalone `.terrain` round-trip.
4. [Medium] Uncited binary field enumeration: ACCEPTED. Fixed: section 3 VERIFIED field names with `grep -abo -m 1` byte offsets 8503 through 9394; decoded numeric values stay CHILD-REPORTED / UNVERIFIED.
5. [Medium] URP shader internals "not read": ACCEPTED. Fixed: section 3 cites `TerrainLitInput.hlsl:26-30,41-42,76,86-91,112,150-153` and `package.json:4`; UNVERIFIED item 9 narrowed to layer-count limits / basemap details not inspected.
6. [Medium] Tree/detail REPORT-ONLY inference: ACCEPTED. Fixed: section 5 re-checked engine-wide, cites `TerrainScatterer.cs:35-39,206-210` (`SlopeScatterer`, `TerrainMaterialScatterer`), states the classification as a scope decision with residual UNVERIFIED item 13.
7. [Low] 19-key miscount: ACCEPTED. Parent re-counted all 28 SyntyForest `.terrainlayer` files: 18 keys each. Fixed in deliverable section 3 and scratch `01-unity-terraindata.md` (global 19 to 18 keys replace).
8. [Low] Existing small Unity oracle ignored: ACCEPTED. Fixed: sections 1.1, 7.1, 7.2 and 9 cite `unity_samples_oracle`; "smallest useful oracle" now says the two existing projects already are it.

Checks that passed unchanged: no new numbered task folder beyond 27; no task 26 amendment; no oracle directory created by task 27; uncommitted `Editor/Core/*.cs` diffs belong to task 22, not this deliverable; scratch reports correctly labelled outside the repo.

Additional parent findings recorded in the corrected doc: the real `.tmat` key set (`sample_terrain_material.tmat:1-16`) has seven keys the importer's `ToTmat` omits; `notes_on_scene_creation.txt:1` confirms a hand-made hill; `unity_samples_oracle` is on `6000.7.0b10` while SyntyForest is `6000.3.29f1` / URP `17.3.0` (UNVERIFIED item 14); both Unity scenes set `m_EnableBakedLightmaps: 1` at line 53.

Original pre-correction deliverable preserved (moved aside, NOT deleted) at `C:/Users/Eliot/AppData/Local/hermes/profiles/hermes-sbox/cache/scratch/terrain-research/terrain-import-research-pre-correction.md`.

Next: continue the SAME critic `sa-0-51f248d3` for a scoped re-check of only the 8 fixes, with fresh `Tree:` hashes.
