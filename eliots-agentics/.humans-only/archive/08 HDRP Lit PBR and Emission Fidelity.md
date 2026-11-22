## Status: STARTED? YES! CLARIFIED? YES! HISTORY? YES! REVIEWED? YES! DONE? YES! WORKER: sa-0-f8024b88 OTHER: reviewed 03:32 no further changes; material-slot regression round committed

## Task

	Convert HDRP Lit PBR and emission fidelity: metallic/smoothness remaps, normal scale, and HDR
	emissive tint/brightness; report transmission and subsurface instead of silently flattening them.

### Description

	HDRP stores its PBR values differently from Standard/URP, and the importer currently loses or
	misreads several of them. On the 98 HDRP Lit materials in old_school:

	- _MetallicRemapMin/Max are non-default in 10 materials; _MetallicRemapMax is 0 in all 10, so they
	  are authored fully non-metallic. 8 of the 10 currently export the raw mask R channel as metalness
	  and render fully metallic: Shrubs, Tree_Leaf, Shoes 01/02 and Umbrella 01-04. Plane and Glass have
	  no bound _MaskMap and already output g_flMetalness 0.
	- _SmoothnessRemapMin/Max are non-default in 3 materials (Fan_01, Corridor_Floor_01, Plastic_01), but
	  the values are 0.99999976/0.9999999, float serialization noise below 8-bit output. Treat them as a
	  no-op regression case, not a visible bug.
	- _NormalScale differs from 1 in 0 materials; the code reads _BumpScale only (latent gap).
	- 7 materials have an emissive map; 4 have a non-black _EmissiveColor and 3 carry HDR/coloured
	  values: Lamp_01 and Lamp_02 are 2.996 white; Exit_Sign is (0, 2.996, 0.753). The importer reads
	  _EmissionColor (which HDRP keeps at 1,1,1 in all 98) and hard-codes g_flSelfIllumBrightness to 0,
	  so all emissive tints become white and the HDR energy is lost.
	- 10 materials enable _MATERIAL_FEATURE_TRANSMISSION and 9 enable
	  _MATERIAL_FEATURE_SUBSURFACE_SCATTERING. s&box's complex shader has no equivalent; they must be
	  reported, not silently rounded to opaque PBR.

	The mask-map split itself is already HDRP-shaped (R metal, G AO, B detail, A smoothness) at
	UnityMaterial.cs:111-118; keep it and add the remaps.

### Required work

	1. Apply _MetallicRemapMin/Max to the extracted metalness channel (or to a scalar metalness when
	   remap max is 0). _MetallicRemapMax 0 must produce no raw metalness texture and g_flMetalness 0.
	2. Apply _SmoothnessRemapMin/Max to the roughness channel (roughness = 1 - remapped smoothness).
	   Leave default and epsilon (0.99999976/0.9999999) remaps byte-identical.
	3. Read _NormalScale as the normal strength, falling back to _BumpScale and then 1. No corpus
	   material changes, but the property must be wired.
	4. Emission: prefer [HDR] _EmissiveColor over _EmissionColor; split a value above 1 into a
	   normalised tint and g_flSelfIllumBrightness (or an equivalent complex-shader parameter), and
	   honour _UseEmissiveIntensity/_EmissiveIntensity when set. Keep the existing black-to-white guard.
	5. Add one grouped report line per import naming the materials whose transmission or subsurface
	   cannot be reproduced, with counts and up to five sample names. Do not emit per-material spam and
	   do not claim the feature is converted.
	6. Extend the HDRP fixture with a metal-remapped material, a smoothness-remapped material and an
	   HDR emissive material; keep the existing opaque/alpha/double/transparent cases unchanged.

### Acceptance

	- The 8 materials that currently export raw mask-R metalness (Shrubs, Tree_Leaf, Shoes 01/02,
	  Umbrella 01-04) no longer emit a metalness texture and carry g_flMetalness 0.000. Plane and Glass
	  already output 0 and stay unchanged.
	- Fan_01, Corridor_Floor_01 and Plastic_01 keep a byte-identical Roughness section relative to the
	  task-07 output; a default-remap material (Lamp_01) is byte-identical.
	- Lamp_01/Lamp_02 and Exit_Sign vmats encode non-unit brightness or a non-white tint; Projector_01
	  (white 1.0 emission) and Burglar_Alarm (map-only) remain visually equivalent.
	- The import report groups 10 transmission and 9 subsurface materials with sample names; no
	  material is mislabelled as converted.
	- The HDRP fixture asserts the same features; building and city--1 stay byte-identical; harness
	  builds 0 warnings / 0 errors; two runs byte-identical.

### Work Review

2026-11-21 20:00 - Verdict: changes required

Tree (HEAD 24e5e3e + the uncommitted task-08 changes):
  cde464125b8a4404f04da6bc889d5e8e8357c35f  M Editor/Core/UnityMaterial.cs
  940dc8a6ede741172ec80377eee7cbaa79be3465  M Editor/Core/UnityHdrp.cs
  93348614f4d3f20125e38240e3cec7c0b23c19d9  M Editor/Core/UnityImport.cs
  Documentation/Unity-To-SBox-Automated/fixtures/hdrp/ (48 files); hdrp.unitypackage archive md5 279e59661cd3a712548f9679a162938d, tar md5 4fd0bd0cdb72f2f83b9737872ffe1e06

Verified: build 0/0; fixture 23/20/46/0/0, two runs identical, HDRP 19 and one transmission; old_school 1,275/920/2,097/97/81 (four metal PNGs removed, 11 vmats changed), two runs identical; building 24/0/0 and city--1 1,166/3/3, diffs empty; verify_output 20/0 and 823/0. Metal, epsilon, emission and transmission/SSS counts all hold. The `_TransmissionEnable` false positive is correctly keyword-gated.

1. [Medium] UnityHdrp.cs:182 says the flagged materials "were converted as opaque PBR", but the only material named in the fixture, HDRP_Glass, is emitted with shaders/glass.shader. Change to "were converted without that feature" (or "as opaque or glass PBR").
2. [Low] UnityImport.cs:221-222 dedupes transmission/SSS names on Path.GetFileNameWithoutExtension, so same-named materials in different folders undercount. Key the set on asset.Path and display the file name.
3. [Low] UnityMaterial.cs:325-329: the _UseEmissiveIntensity == 1 branch is unexercised; the fixture's HDRP_Exit_Sign has it 0 and no corpus material sets it. Set it to 1 in one fixture material.
4. [Low] The task's Post-Task Follow Ups is empty while the plan defers the unit-emission (brightness 0 reads dark) gap to "a follow-up below". Record it there.
5. [Low] The task evidence says "two runs byte-identical / diff -r empty" but report.json differs by run; say diff -r -x report.json.

## Notes

	Implementation home: Editor/Core/UnityMaterial.cs, primarily PrepareChannels (:99-130) and ToVmat
	(:166-277). The extraction callback already accepts scale and invert; use it where it expresses the
	remap, otherwise post-process the channel image. The material editor's vmat shape must be preserved:
	grouped sections, sorted keys, no trailing newline.

	old_school has 81 pre-existing missing-texture warnings; they are unrelated and must not be
	reclassified by this task.

### Plan (Task Worker)

	All conversion changes are in Editor/Core; no new tooling, no checker script.
	1. UnityMaterial gains HDRP-gated MetalRemapMin/Max and SmoothRemapMin/Max (defaults 0/1), a
	   RemapEpsilon of 1e-6 that treats 0.9999999 as the 0..1 default (it is below one 8-bit step),
	   and Remap/RemapSpan helpers for min + (max - min) * value.
	2. PrepareChannels: _MetallicRemapMax <= eps skips the metal channel entirely, so no metalness
	   PNG is generated, F_METALNESS_TEXTURE is absent and g_flMetalness stays 0.000. Otherwise the
	   channel is extracted with scale (max - min) only when the remap is not the default. Roughness
	   keeps its extract-then-invert order with the same scale, which is exactly 1 - (max - min) * A
	   while the remap starts at 0, so a default or epsilon remap is byte-identical.
	   A non-zero remap minimum cannot fit in an 8-bit channel; it emits one warning instead of being
	   silently dropped (no corpus material has one).
	3. ToVmat: scalar metalness and roughness go through Remap; g_flNormalMapScaleFactor reads
	   _NormalScale, then _BumpScale, then 1, under HDRP only.
	4. Emission prefers _EmissiveColor on HDRP, multiplies by _EmissiveIntensity when
	   _UseEmissiveIntensity is 1, keeps the black-to-white guard, and splits a peak above 1 into a
	   normalised g_vSelfIllumTint plus g_flSelfIllumBrightness = peak.
	5. UnityHdrp.UnsupportedMaterials builds the single grouped transmission/subsurface line;
	   UnityImport collects deduped material names and adds it once beside the HDRP census line.
	6. Fixture: three materials and three 8x8 PNGs added; the 16 existing materials are untouched.

	Decision for review: g_flSelfIllumBrightness stays 0.000 when the emission peak is <= 1.
	Projector_01 (white 1.0) and Burglar_Alarm (map-only) therefore stay byte-identical, as do
	building/city--1, which carry PolygonCity emissive materials; only an HDR peak becomes a
	brightness value. Hand-authored vmats in this workspace use non-zero brightness (3.380, 10.000),
	so unit-emission materials still read as dark in s&box; that pre-existing gap is a follow-up
	below, not a reason to change every emissive material in this task.

### Evidence (final tree, master-verified after the worker turn)

	Code: UnityMaterial.cs cde464125b8a4404f04da6bc889d5e8e8357c35f, UnityHdrp.cs
	b37d680df81739bddb0b6bb8b2df78d4e1972b03, UnityImport.cs ff0671f2e7152b51bcc3238963b2c0c9ab4a1ab5.
	Harness build: 0 warnings / 0 errors.

	Fixture: 24 assets (19 HDRP/Lit materials, 3 textures, 1 scene, 1 volume profile), 48 files with
	.meta, no .shader. hdrp.unitypackage archive md5 8d767c68a383123c472151141892d8bf, uncompressed tar
	md5 37d156fdf8e7d75151682cb295481f75. Import --scenes: 23 assets / 20 converted / 46 files / 0
	warnings / 0 unresolved, two runs byte-identical modulo report.json (diff -r --exclude=report.json
	empty). HDRP line: "19 HDRP material(s),
	1 HD light(s), 1 HD reflection probe(s), 1 volume(s)"; the grouped unsupported line names
	HDRP_Glass for transmission. verify_output.py: 20 files, 0 problems.
	HDRP_Metal_Remap_01: no TextureMetalness, g_flMetalness 0.000, roughness texture retained.
	HDRP_Exit_Sign: F_SELF_ILLUM 1, g_flSelfIllumBrightness 2.996, tint [0 1 0.251335].
	HDRP_Plastic_01: roughness scale 1.000 with texture.

	old_school (no --scenes): 1,275 assets / 920 converted / 2,097 files / 97 warnings / 81 unresolved
	/ 0 errors; two runs byte-identical (diff -r --exclude=report.json empty). The final run is
	old-final3; its imported tree is identical to old-final. 2,097 = task 07's 2,101 minus the four
	unique metalness PNGs shared by the eight remapped materials; no other file was removed. The eight remapped materials
	(Tree_Leaf, Shrubs_01, Shoes_01/02, Umbrella_01-04) have no TextureMetalness and g_flMetalness
	0.000. Fan_01, Floor_01_Albedo (Corridor_Floor_01; the report lists that material as Vmat true) and
	Plastic_01 are byte-identical to task 07, so the three epsilon smoothness remaps are no-ops.
	F_ALPHA_TEST 13, F_RENDER_BACKFACES 11 and glass 1
	are unchanged. Lamp_01/Lamp_02 carry brightness 2.996; Exit_Sign carries brightness 2.996 and tint
	[0 1 0.251309]; Projector_01 and Burglar_Alarm stay at brightness 0.000. The report has one grouped
	line: 10 materials use transmission, 9 use subsurface.

	building: 24 files / 0 warnings / 0 unresolved; city--1: 1,166 files / 3 warnings / 3 unresolved;
	both diffs against the populated HEAD baselines are empty. verify_output.py on old_school: 823
	files, 0 problems. No checker or other tooling was added.

	### Reopened round 2026-11-22 — root-cause evidence (no commit)

	Source-data gap (proposal 5) verified before any code change: streamed all 1.27 GB of
	Testing/old_school_project.unitypackage. GUIDs 6012db391d795e54f8166f209f6ce80c and
	8a1b6f6da87cf3c4dbeeeb50c35ed3c5 occur as `m_Texture: {fileID: 2800000, guid: ...}` in exactly
	eight material assets each (the eight structure materials in unity-expert-review-nov-22.md) and in
	ZERO asset.meta files. No Texture asset with either GUID is packed. This is a missing source
	asset, not an unsupported decodable format; no decoder was invented. The three emission sites
	(ImportPlan.cs:78, UnityImport.cs:242, UnityImport.cs:262) now say so explicitly.

	Root causes and fixes (current-tree line numbers):
	1. UnityPrefabBindings.cs:274-294 (proposal 1): the old exemption compared the union of ALL
	   mesh-slot values with one slot's values, so it warned or stayed silent for the wrong reason.
	   It now compares only THIS slot's values across meshes (:282-289). Values fully accounted for
	   by distinct per-mesh assignments are expressed by UnityImport.ModelSplit (:675-701) and need
	   no warning; a genuine single-mesh conflict keeps one warning naming the prefab asset that
	   supplied each competing material (:290-293). Deviation: the task-02 decision required
	   source-real residuals to be NAMED, never blanket-suppressed. Only conflicts genuinely
	   resolved by the per-mesh models lose their warning; every residual that survives is named.
	2. UnityPrefabBindings.cs:104-120 (proposal 2): before rejecting an index, WrapperMaterialSlot
	   (:69-76) maps it through EffectiveSlots/RenderedMaterials and accepts a declared slot.
	   LOD01+ overrides whose LOD0 counterpart resolves are skipped via UnityModel.LodBase
	   (UnityModel.cs:150) because RenderedMaterials (UnityModel.cs:124-131) imports LOD0 only.
	   Genuinely out-of-range indices remain, named by model/mesh/material. The three survivors are
	   Door_06_LOD00 (Wood_05), Outdoor_Wall_01 (School_Wall), Windows_02_Frame_LOD00 (Trim_02).
	3. UnityImport.cs:344-361 + UnityModel.cs:148 (proposal 3a/3c): "N - Default" slots now take the
	   existing generated neutral vmat. The neutral-generation condition was widened from
	   `info.Materials.Count == 0` to also cover models declaring a default slot, while the
	   `missingSlots` loop was narrowed so genuinely missing semantic slots (Metal, Glass,
	   lambert2, ...) are NOT silently filled. The _lodN alias expansion at UnityImport.cs:334-335
	   was checked and covers the LOD01 names in the report. Proposal 3b's blanket semantic hint was
	   NOT applied: those slots are source-real residuals and stay named, per the task-02 decision.
	4. ImportPlan.cs:93-99, 109-121 (proposal 4): a declared PrefabSlotMaterials/MeshSlotMaterials
	   assignment resolves an albedo matching several materials when it selects exactly one
	   candidate; otherwise the ambiguous warning lists the candidate paths.

	Before/after counts (fresh harness --scenes runs, same package and options; the HEAD-code
	baseline was built from `git show HEAD:Editor/Core/*.cs` in a scratch mirror with LF sources):
	  before (HEAD code) 1,277 assets / 936 converted / 2,115 files / 97 warnings / 81 unresolved
	    = 44 unassigned-material, 20 prefab-material-assignment, 16 missing-texture, 1 ambiguous-material
	  after (this round) 1,277 assets / 940 converted / 2,119 files / 80 warnings / 64 unresolved
	    = 45 unassigned-material, 16 missing-texture, 3 prefab-material-assignment, 0 ambiguous-material
	  The code delta is exactly +4 converted / +4 files: four new neutral .vmat files
	  (Partition_01, Floor_Drain_01, Metal_Edge_01 and Windows_03_Frame) plus four vmdl remap lines.
	  Windows_03_Frame was previously unreported and is now named, which is the 44 -> 45 increase.
	  The pinned editor report (1,277 / 1,034 converted / 2,213 files, terrain materials on) is a
	  DIFFERENT run; it is retained only as secondary context, not as this comparison's baseline.
	  0 unidentified / 0 misclassified: 16 missing-texture warnings name the source-data gap, 3 prefab
	  warnings name model/mesh/material, and 45 unassigned warnings name model and slot (3 default
	  slots name the neutral material they use).

	Gates measured on the final bytes (logs under scratch/t08):
	  harness build 0 warnings / 0 errors; fixture 24 assets / 21 converted / 48 files / 0 / 0 with
	  verify_output.py 21 files / 0 problems; old_school --scenes 64 unresolved with verify_output.py
	  843 files / 0 problems; two old_school runs' imported trees byte-identical (diff -r
	  --strip-trailing-cr empty) and report.json identical except TextureProcessing timing
	  (165021 vs 166666 ms) and the run's Destination path, as in the previous round.
	  HDRP features unchanged: F_ALPHA_TEST 13, F_RENDER_BACKFACES 11, glass 1.
	  building 24 files / 0 warnings / 0 unresolved and city--1 1,166 files / 3 warnings / 3
	  unresolved, both byte-identical to a HEAD-code baseline built from the same LF sources
	  (diff -r --strip-trailing-cr empty for both trees). All four edited Editor/Core files are
	  BOM-less. No checker, script or tooling was added; Documentation/sbox-console-logs.txt, the
	  harness CLI and the c7f922a FBX sanitize fix were not touched. Headless only: no editor or
	  visual result is claimed.

	Commit-path list (explicit paths only; NO COMMIT yet, awaiting critic):
	  Editor/Core/ImportPlan.cs
	  Editor/Core/UnityImport.cs
	  Editor/Core/UnityModel.cs
	  Editor/Core/UnityPrefabBindings.cs
	  eliots-agentics/tasks/08 HDRP Lit PBR and Emission Fidelity.md
	  The Director's archive moves (deleted tasks/01-05, untracked .humans-only/) are NOT staged.

## Clarification Q&A

	Q: Should transmission/SSS be approximated with translucency?
	A: Not in this task. Report them; a later task may add a documented approximation if the editor
	result justifies it.

	Q: Is _NormalScale worth wiring when no material uses it?
	A: Yes; it is the HDRP standard property and costs one lookup.

## Relevant History

	Task 07 owns alpha/double-sided/transparency; this task owns the remaining HDRP Lit PBR semantics.
	Task 06 supplies detection and the fixture.

	Reopened 2026-11-22 on the review feedback in unity-expert-review-nov-22.md: the five proposals
	were re-checked against the current tree, and the source-GUID gap was proved from the packed
	package before editing. The implementation changes material-slot classification and per-mesh
	remaps in Editor/Core (ImportPlan, UnityImport, UnityModel, UnityPrefabBindings), not HDRP
	surface conversion; the HDRP feature counts and both no-scenes baselines are unchanged. The
	task-02 decision that source-real residuals are named (never silently zeroed) was preserved:
	the only warnings removed are those genuinely resolved by ModelSplit, LOD0-only import, a
	declared prefab slot or a neutral default-slot material. Full counts and file:line evidence are
	in this file's ## Notes under "Reopened round 2026-11-22". REVIEWED? is NO! because
	Editor/Core/UnityImport.cs changed after the prior review; a critic must re-review before commit.

## Stylistic Task Notes:

	References to variables like N and M automatically imply that these are to be exposed properties
	for developer tweaking, if they become code knobs.

## Skills To Know: for you and/or subagents:
	sbox-offline-compile-check
	unityimporterplus
	unity-to-sbox-conversion
	sbox-model-material-resources

## Post-Task Follow Ups
	Unit-emission HDRP materials (peak <= 1) keep g_flSelfIllumBrightness 0, so they read dark in s&box;
	task 08 deliberately preserves byte-identity. Revisit if the editor shows the emissive props too dark.

### Review feedback appended 2026-11-22 (from task unity-expert-review-nov-22)

	Attribution limit: this feedback is attached to task 08 because its shipped diff touched Editor/Core/UnityImport.cs, which owns slot-keyed material remaps — the closest live DONE task to the reported failures. The failures themselves are model material-slot resolution, the subject of "02 Model-Prefab Material Slots", which is archived under .humans-only/archive/ and off-limits to agents. No new task file was created. Every code change below is a PROPOSAL — NOT APPLIED. This review edited no project source file.

	Scope reviewed: the 81 objects in the Unresolved array of Testing/old_school_project+import-report+post-hdrp-update+post-no-prefabs-update.json (report lines 2220-2707), all Severity "Warning": 44 unassigned-material, 20 prefab-material-assignment, 16 missing-texture, 1 ambiguous-material. Per-object evidence (Asset, Code, Message, report line) is in eliots-agentics/tasks/unity-expert-review-nov-22.md under "Review evidence (worker, 2026-11-22)".

	What went wrong:
	- unassigned-material (44): "No converted material was resolved for slot 'X'" (UnityImport.cs:362) for FBX default slots ('15 - Default', '16 - Default'), generic names ('lambert2', 'mesh') and repeated semantic names ('Metal', 'Glass', 'Windows') across the Windows_02/05/06 and Door_01 models. ImportPlan.cs:109 can infer a material from a slot filename, but it skips slots recorded as unresolved, so nothing fills them.
	- prefab-material-assignment (20): 12 "Prefab assignment for slot 'X' is missing or conflicting (A vs B); no material was guessed" from UnityPrefabBindings.cs:274, and 8 "A prefab material override puts 'M' in slot N of mesh ... which the FBX gives no material slot for" from the index guard in UnityPrefabBindings.Assign (:97-121). Three of the latter name LOD01 meshes (Door_06, Windows_02_Frame, Windows_04), which UnityModel.RenderedMaterials (:124-131) never selects.
	- missing-texture (16): the same two texture GUIDs (6012db391d795e54f8166f209f6ce80c, 8a1b6f6da87cf3c4dbeeeb50c35ed3c5) repeated across eight structure materials, two each, from ImportPlan.cs:78.
	- ambiguous-material (1): Floor_stairs_bottom_Snaps014.fbx, color texture Wall_02_Albedo_Snaps014.tga matches more than one material at ImportPlan.cs:98.

	Proposals (NOT APPLIED; names and line numbers verified against the current tree):
	1. Conflicting prefab slots (12 warnings): UnityPrefabBindings.cs:262-275 reduces each slot's values across all candidate prefabs; a single value goes to PrefabSlotMaterials (:265), otherwise the slot is added to UnresolvedPrefabSlots (:268) and an AssignmentWarning is appended (:274), which ImportPlan.cs:91 republishes. MeshSlotMaterials (UnityModel.cs:92, populated at UnityPrefabBindings.cs:251-260) already records per-mesh evidence, and UnityImport.ModelSplit (:675-700) can emit one model per mesh when a slot maps to two materials. PROPOSAL: widen the existing per-mesh exemption at UnityPrefabBindings.cs:270-272 to compare values for that specific slot instead of the union of all slots on the mesh, so a cross-mesh conflict stays silent and ModelSplit expresses it. For a genuine single-mesh conflict, keep one warning but name the prefab asset that supplied each competing material.
	2. Override slot index rejected (8 warnings): UnityPrefabBindings.Assign (:97-121) rejects index >= Math.Max(1, binding.Slots.Length) at :104-110. PROPOSAL: before rejecting, map the Unity MeshRenderer material index through WrapperMaterialSlot (:69) and UnityModel.EffectiveSlots (:86); accept it when it lands on a declared slot. Suppress warnings for LOD01-only meshes whose LOD0 counterpart resolves, because UnityModel.RenderedMaterials (:124-131) imports LOD0 only; still report genuinely out-of-range indices once per model with the material name.
	3. Unassigned slots (44 warnings): UnityImport.PrepareFiles applies PrefabSlotMaterials remaps (:327-331), removes UnresolvedPrefabSlots (:332) and prunes undeclared slots (:338-340) before emitting "No converted material" at :360-362. PROPOSAL: (a) route "N - Default" slots with no Unity material to the neutral .vmat already generated at :346-352 instead of leaving them unresolved; (b) let ImportPlan.Match (:93-99) consult a deterministic PrefabSlotMaterials/MeshSlotMaterials hint before declaring no dependency for semantic names, and emit one grouped line naming all candidates when several remain, instead of one line per slot; (c) verify the _lodN alias expansion at UnityImport.cs:334-335 covers the LOD01 slot names appearing in this report.
	4. Ambiguous texture (1 warning): ImportPlan.cs:85-86 builds byTexture from albedo filenames, and Match narrows by already-referenced materials (:95-96) before reporting at :98. PROPOSAL: before :98, consult asset.ModelInfo.MeshSlotMaterials / PrefabSlotMaterials (UnityModel.cs:92, :89) for a slot whose albedo is Wall_02_Albedo_Snaps014.tga; if exactly one candidate GUID is assigned there, link it; otherwise keep the warning but list the candidate material paths.
	5. Missing textures (16 warnings): ImportPlan.cs:75-79 posts these for texture GUIDs not present as Texture assets in the archive. PROPOSAL: resolve the two repeated GUIDs in the source package first — if they name a texture in an unsupported format, either add a decoder path or emit an explicit unsupported-format line with the format. This may be a source-data gap rather than an importer defect; verify before changing code.

	Re-check criterion for any later implementation: re-run this package and confirm the 81-object extract falls in the targeted codes while the HDRP material feature counts (F_ALPHA_TEST 13, F_RENDER_BACKFACES 11, glass 1) and the building / city--1 baselines stay unchanged.

### Work Review

2026-11-21 20:11 - Verdict: changes required

Tree (HEAD 24e5e3e + the uncommitted task-08 changes):
  b37d680df81739bddb0b6bb8b2df78d4e1972b03  M Editor/Core/UnityHdrp.cs
  ff0671f2e7152b51bcc3238963b2c0c9ab4a1ab5  M Editor/Core/UnityImport.cs
  cde464125b8a4404f04da6bc889d5e8e8357c35f  M Editor/Core/UnityMaterial.cs (unchanged since round 1)
  Documentation/Unity-To-SBox-Automated/fixtures/hdrp/ (48 files, no .shader); hdrp.unitypackage archive md5 8d767c68a383123c472151141892d8bf, uncompressed tar md5 37d156fdf8e7d75151682cb295481f75

Round-1 findings all fixed: 1 (UnityHdrp.cs:186 now says "were converted without that feature"), 2 (UnityHdrp.cs:176-179 counts paths and shows distinct short names; UnityImport.cs:221-222 add asset.Path), 3 (HDRP_Exit_Sign.mat:49 _UseEmissiveIntensity 1, output unchanged at brightness 2.996), 4 (Post-Task Follow Ups records the unit-emission gap), 5 (evidence says diff -r --exclude=report.json).

Verified on disk: clean Release rebuild 0 warnings / 0 errors; fixture 23 assets / 20 converted / 46 files / 0 warnings / 0 unresolved, 19 HDRP materials, one transmission sample, fx-final3 vs fx-final4 identical modulo report.json, verify_output.py 20 files 0 problems; old_school 1,275/920/2,097/97/81, old-final3 imported tree identical to old-final, grouped line "10 use transmission …; 9 use subsurface scattering", verify_output.py 823 files 0 problems; building 24 files/0/0 and city--1 1,166 files/3/3 with empty diffs against the HEAD baselines; only the three Editor/Core files plus the fixture are modified and no checker was added. Metal remaps stay HDRP-gated and epsilon-safe, the eight remapped materials carry g_flMetalness 0.000, and Fan_01/Plastic_01 are byte-identical.

1. [Low] The evidence paragraph for old_school says "Corridor_Floor_01 has no converted vmat in this package", but report.json lists Corridor_Floor_01_Snaps014.mat as Vmat: true, and it converts to imported/assetstoreoriginals/school/art/material/structure/floor/floor_01_albedo_snaps014.vmat (named after Floor_01_Albedo_Snaps014.tga, which the same report names as that material's texture). That vmat is absent from diff-oldschool-nocr.txt, so it is byte-identical and acceptance item 2 does hold for Corridor_Floor_01. Restore the earlier "Floor_01_Albedo (Corridor_Floor_01) is byte-identical" wording rather than reporting the case as unverifiable.

### Work Review

2026-11-21 20:14 - Verdict: no further changes

Tree (HEAD 24e5e3e + the uncommitted task-08 changes, unchanged since review 2):
  b37d680df81739bddb0b6bb8b2df78d4e1972b03  M Editor/Core/UnityHdrp.cs
  ff0671f2e7152b51bcc3238963b2c0c9ab4a1ab5  M Editor/Core/UnityImport.cs
  cde464125b8a4404f04da6bc889d5e8e8357c35f  M Editor/Core/UnityMaterial.cs
  Documentation/Unity-To-SBox-Automated/fixtures/hdrp/ (48 files, no .shader); hdrp.unitypackage archive md5 8d767c68a383123c472151141892d8bf, uncompressed tar md5 37d156fdf8e7d75151682cb295481f75

Round-2 finding fixed: acceptance again names Fan_01, Corridor_Floor_01 and Plastic_01, and the evidence says Floor_01_Albedo is that material's converted vmat (report.json lists it Vmat true) and is byte-identical. The stale "no converted vmat" claim survives only inside the review-2 finding it answers.

Re-checked after the wording edit: the three source hashes and the fixture archive md5 are unchanged, so every gate recorded for review 2 stands unaltered — Release rebuild 0 warnings / 0 errors; fixture 23 assets / 20 converted / 46 files / 0 warnings / 0 unresolved with 19 HDRP materials and one transmission sample; old_school 1,275/920/2,097/97/81 with the single 10/9 grouped line and its four metalness PNGs removed; building 24/0/0 and city--1 1,166/3/3 unchanged against the HEAD baselines; verify_output.py 20 files 0 problems and 823 files 0 problems. Metal and smoothness remaps stay HDRP-gated and epsilon-safe, the eight remapped materials carry g_flMetalness 0.000, emission splits HDR peaks into brightness plus normalised tint, transmission and subsurface are reported once instead of being silently flattened, and no checker or unrelated edit was added.

Commit as is.

### Work Review

2026-11-22 03:32 - Verdict: no further changes

Tree (HEAD c7f922a + the four staged Editor/Core files):
  git hash-object --path=Editor/Core/ImportPlan.cs "Editor/Core/ImportPlan.cs" -> 802619652605eeeae648a802d2d37fc46d767b6b
  git hash-object --path=Editor/Core/UnityImport.cs "Editor/Core/UnityImport.cs" -> 71f2f719e1e11e59c357f1e893017a2a045b71c4
  git hash-object --path=Editor/Core/UnityModel.cs "Editor/Core/UnityModel.cs" -> 2c6af9b23c1cb57b926152e886c48ef81d23a494
  git hash-object --path=Editor/Core/UnityPrefabBindings.cs "Editor/Core/UnityPrefabBindings.cs" -> 43806719c1d1f3754643f9a767b78e75bf69aeb7

Highest-priority gate passed. A fresh HEAD-code mirror (git show HEAD:Editor/Core/*.cs, LF, hashes 7d0a643/147705a/da3ae01/72db226) built 0 warnings / 0 errors and, with --scenes on the same package/options as the worker's after-run, gave 1,277 assets / 936 converted / 2,115 files / 97 warnings / 81 unresolved = 44 unassigned-material, 20 prefab-material-assignment, 16 missing-texture, 1 ambiguous-material. This matches the parent's independent baseline, so the 81 -> 64 reduction is attributable to the code, not the terrain-option difference. The staged code built 0/0 and gave 1,277 / 940 / 2,119 / 80 warnings / 64 unresolved = 45 unassigned, 16 missing-texture, 3 prefab, 0 ambiguous. The only tree delta HEAD -> work is four _iup_unassigned.vmat files (Partition_01, Floor_Drain_01, Metal_Edge_01, Windows_03_Frame) and four vmdl remap lines.

The 44 -> 45 unassigned count is one added asset, Windows_03_Frame_Snaps014.FBX, previously unreported; no existing warning was removed. The 17 removed prefab warnings and one ambiguous warning are false positives: per-mesh LOD splits such as partition_02_lod00/01.vmdl carry different per-slot materials and are byte-identical to HEAD. The three surviving prefab warnings name model/mesh/material (Door_06/Wood_05/Door_06_LOD00, Outdoor_Wall_01/School_Wall, Windows_02_Frame/Trim_02/Windows_02_Frame_LOD00). Proposal 3b's blanket semantic hint was not applied: the ImportPlan.cs:93-121 hint only narrows albedo matches and only when hinted.Length == 1, so semantic slots stay named; the deviation is recorded in Notes. UnityPrefabBindings.cs:285-289 compares THIS slot's per-mesh values via TryGetValue(slot,…); :69-76 and :104-128 map the index through EffectiveSlots/RenderedMaterials without an out-of-range write, and :109-118 skips LOD01 overrides whose LOD0 counterpart resolves. UnityModel.cs:148/150 IsDefaultSlot and LodBase are correct. Streaming the package confirmed the two texture GUIDs appear as m_Texture references in eight material payloads each and in zero of 1,462 asset.meta files, so the 16 warnings correctly report a source-data gap; all 16 name it.

Gates: harness builds 0/0; F_ALPHA_TEST 13, F_RENDER_BACKFACES 11 and glass 1 are identical to HEAD; building 24 files / 0 warnings / 0 unresolved and city--1 1,166 / 3 / 3 are byte-identical to a HEAD-code baseline (diff -r --strip-trailing-cr empty); verify_output.py with the correct Assets root (parent of imported/) reports 843 files, 0 problems; two old_school runs give byte-identical imported trees, with report.json differing only in TextureProcessing elapsed ms and Destination. All four edited sources are BOM-less; no checker or tooling was added; Editor/Core/UnityFbxRewrite.cs (c7f922a) and the harness CLI (b4292f0) are byte-identical to HEAD. Headless only; no editor or visual result is claimed.

1. [Medium] Task file Notes lines 189-198: the heading says "fresh harness --scenes runs, same package and options" but labels "before (pinned report)" as 1,277 / 1,034 converted / 2,213 files, which is the EDITOR run with terrain materials on, and explains a 94-file delta via 98 .tmat files. The pinned numbers are quoted accurately but are not a same-options baseline. Fix: report the apples-to-apples HEAD harness baseline, 1,277 assets / 936 converted / 2,115 files / 81 unresolved = 44/20/16/1, and state that after is 1,277 / 940 / 2,119 / 64, so the code delta is exactly +4 converted / +4 files (the four neutral vmats). Keep the pinned editor report only as a secondary note. Documentation hygiene in the task file only; all code gates pass and no code change is required.

2026-11-22 03:45 - Master resolution of finding 1: applied. The ## Notes before/after block was rewritten to the apples-to-apples HEAD harness baseline (1,277 / 936 / 2,115 / 81 = 44/20/16/1) and after (1,277 / 940 / 2,119 / 64), with the pinned editor report demoted to secondary context and the code delta stated as +4 converted / +4 files. Only this task file changed; re-checked the four reviewed Editor/Core hashes above and they are byte-identical to the Tree manifest, so the `no further changes` verdict still binds the code. No re-review required: the task file is the review record, not a reviewed deliverable.