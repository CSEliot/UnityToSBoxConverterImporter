## Status: STARTED? NO! CLARIFIED? NO! HISTORY? NO! REVIEWED? NO! DONE? NO! WORKER: NONE OTHER: HDRP backlog, depends on 06

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

## Notes

	Implementation home: Editor/Core/UnityMaterial.cs, primarily PrepareChannels (:99-130) and ToVmat
	(:166-277). The extraction callback already accepts scale and invert; use it where it expresses the
	remap, otherwise post-process the channel image. The material editor's vmat shape must be preserved:
	grouped sections, sorted keys, no trailing newline.

	old_school has 81 pre-existing missing-texture warnings; they are unrelated and must not be
	reclassified by this task.

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