## Status: STARTED? NO! CLARIFIED? NO! HISTORY? NO! REVIEWED? NO! DONE? NO! WORKER: NONE OTHER: HDRP backlog, depends on 06

## Task

	Convert HDRP Lit surface semantics: alpha test, double-sided rendering and transparency.

### Description

	HDRP Lit writes its surface state in properties and keywords, not in the ShaderLab tags the importer
	currently infers. On the 98 HDRP Lit materials in Testing/old_school_project.unitypackage:

	- 14 materials have _ALPHATEST_ON and _AlphaCutoffEnable = 1. 12 use a non-default _AlphaCutoff:
	  0.19 (8 materials), 0.04, 0.63, 0.729 and 1.0; two use the 0.5 default (Clock_01, Decal).
	  _Cutoff mirrors _AlphaCutoff but is not the source of truth. One material (Curtain_01) has a
	  stored but disabled cutoff.
	- 11 materials have _DOUBLESIDED_ON and _DoubleSidedEnable = 1, and _CullMode = 0. 87 use _CullMode 2.
	- 1 material (Tiling/Glass_Snaps014) has _SURFACE_TYPE_TRANSPARENT, _SurfaceType = 1, alpha 0.141,
	  _SrcBlend 1, _DstBlend 10, _ZWrite 0.

	The generated vmats currently have F_ALPHA_TEST 0/98, F_RENDER_BACKFACES 0/98, F_TRANSLUCENT 0/98
	and no glass shader. Leaves and shrubs render as opaque quads, trees and umbrella panels disappear
	from behind, and Glass renders as an opaque white pane.

	UnityMaterial.cs:30 AlphaTest checks _Mode and _AlphaClip (neither exists on HDRP Lit);
	:31 Translucent checks _Mode and _Surface (URP), not _SurfaceType; :188 checks _Cull and
	shaderBackfaces, not _DoubleSidedEnable/_CullMode; :120-128 opacity extraction is gated on those
	wrong flags; :200 reads _Cutoff rather than _AlphaCutoff. ConfigureShader cannot help: HDRP shaders
	are not in the package, and its RenderType checks would not match "HDLitShader" anyway.

	Research: hdrp-research.md; cache/scratch/hdrp/research-materials.md sections 2-6.

### Required work

	1. Read HDRP feature keywords from m_ValidKeywords (HDRP 17 materials have no m_ShaderKeywords line)
	   and use the property values as the fallback/authority: _ALPHATEST_ON, _AlphaCutoffEnable,
	   _SURFACE_TYPE_TRANSPARENT, _SurfaceType, _DOUBLESIDED_ON, _DoubleSidedEnable, _CullMode.
	2. Extend AlphaTest with _AlphaCutoffEnable == 1 / _ALPHATEST_ON. Prefer _AlphaCutoff over _Cutoff
	   for g_flAlphaTestReference, with _Cutoff as the fallback for non-HDRP materials.
	3. Extend Translucent with _SurfaceType == 1 / _SURFACE_TYPE_TRANSPARENT. Let the opacity channel
	   extraction use the base-colour alpha for these materials. Route an untextured transparent HDRP
	   material through the existing GlassVmat path at UnityMaterial.cs:169; Glass_Snaps014 is the
	   test case.
	4. Extend backface detection with _DoubleSidedEnable == 1 / _DOUBLESIDED_ON / _CullMode == 0.
	5. Do not change opaque, single-sided or non-HDRP materials. Do not touch emission, remaps or
	   normal scale (task 08).

### Acceptance

	- Re-import old_school and count vmats: F_ALPHA_TEST = 13 on complex-shader materials, Glass emits
	  shaders/glass.shader with TextureTranslucency 0.141 (its alpha-test flag is consumed by the glass
	  route), and F_RENDER_BACKFACES = 11.
	- g_flAlphaTestReference carries 0.19 for Tree_Leaf, 0.729 for Fan_01 and 0.5 for Clock_01;
	  Lamp_01 has no Alpha Test section. Do not assert an alpha reference on Glass.
	- The HDRP fixture from task 06 carries matching materials and asserts the same counts; no fixture
	  material silently loses a feature.
	- building 26 files / 0 warnings / 0 unresolved and city--1 1,192 files / 4 warnings / 3 unresolved
	  remain byte-identical; old_school's non-material output is byte-identical.
	- Harness builds 0 warnings / 0 errors; verify_output.py 0 problems; two runs byte-identical.

## Notes

	Implementation home: Editor/Core/UnityMaterial.cs. The mask-map and color paths already work; do
	not regress them. HDRP m_ValidKeywords is a YAML list; parse it once in Parse rather than adding
	regexes at each property site.

	Alpha-test opacity extraction currently looks at ColorProperties in order; _BaseColorMap is already
	in that list. The previous warning-removal at line 129 should still remove the packed-mask warning
	only when a channel image was actually produced.

## Clarification Q&A

	Q: Should HDRP transmission/SSS make the material translucent here?
	A: No. Those are reported in task 08; only _SurfaceType 1 is transparency in this task.

	Q: Should the disabled Curtain_01 cutoff be applied?
	A: No. _AlphaCutoffEnable 0 means no alpha test; the stored _AlphaCutoff alone must not trigger it.

## Relevant History

	Tasks 01-05 established the material output, fixed renderer IDs and scene loss reporting. The
	HDRP census found 21 of 98 materials with a lost or wrong semantic; this task closes the 26
	alpha/double/transparent features (some materials carry more than one).

## Stylistic Task Notes:

	References to variables like N and M automatically imply that these are to be exposed properties
	for developer tweaking, if they become code knobs.

## Skills To Know: for you and/or subagents:
	sbox-offline-compile-check
	unityimporterplus
	unity-to-sbox-conversion
	sbox-model-material-resources

## Post-Task Follow Ups