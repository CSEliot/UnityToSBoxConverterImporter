## Status: STARTED? YES! CLARIFIED? YES! HISTORY? YES! REVIEWED? YES! DONE? YES! WORKER: sa-0-1ff9c4d0 OTHER: reviewed 20:14 no further changes; HDRP PBR and emission committed

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

## Clarification Q&A

	Q: Should transmission/SSS be approximated with translucency?
	A: Not in this task. Report them; a later task may add a documented approximation if the editor
	result justifies it.

	Q: Is _NormalScale worth wiring when no material uses it?
	A: Yes; it is the HDRP standard property and costs one lookup.

## Relevant History

	Task 07 owns alpha/double-sided/transparency; this task owns the remaining HDRP Lit PBR semantics.
	Task 06 supplies detection and the fixture.

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