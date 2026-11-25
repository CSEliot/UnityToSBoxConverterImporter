## Status: STARTED? YES! CLARIFIED? YES! HISTORY? YES! REVIEWED? YES! DONE? YES! WORKER: sa-0-509279a9 OTHER: reviewed 19:07 no further changes; HDRP surface semantics committed

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
	- building and city--1, run without --scenes, stay byte-identical: building 24 files / 0 warnings /
	  0 unresolved; city--1 1,166 files / 3 warnings / 3 unresolved (HEAD 9d709a8).
	- Harness builds 0 warnings / 0 errors; verify_output.py 0 problems; two runs byte-identical.

## Notes

	Implementation home: Editor/Core/UnityMaterial.cs. The mask-map and color paths already work; do
	not regress them. HDRP m_ValidKeywords is a YAML list; parse it once in Parse rather than adding
	regexes at each property site.

	Alpha-test opacity extraction currently looks at ColorProperties in order; _BaseColorMap is already
	in that list. The previous warning-removal at line 129 should still remove the packed-mask warning
	on only when a channel image was actually produced.

	--- Task 07 evidence (worker, 2026-11-21 18:50 JST; harness built from this tree) ---

	Implementation (Editor/Core/UnityMaterial.cs, unchanged elsewhere):
	- ReadKeywords(text, keywords) runs once in Parse and fills a HashSet from m_ValidKeywords (the
	  HDRP 17 YAML list, "- _X" per line) and from a legacy space-separated m_ShaderKeywords line.
	  m_InvalidKeywords is never read: it names features that are off.
	- Hdrp/HdrpFeature(keyword, property): AlphaTest <- _ALPHATEST_ON | _AlphaCutoffEnable == 1;
	  Translucent <- _SURFACE_TYPE_TRANSPARENT | _SurfaceType == 1; Backfaces <- _DOUBLESIDED_ON |
	  _DoubleSidedEnable == 1 | _CullMode == 0. All three HDRP sources are gated on
	  Pipeline == Hdrp, so a non-HDRP material's own stale keyword (_ALPHATEST_ON survives on 8
	  Standard-authored HDRP materials, and on ordinary Standard materials) cannot flip a feature.
	- AlphaCutoff uses _AlphaCutoff for HDRP and falls back to _Cutoff; it feeds g_flAlphaTestReference.
	- PrepareChannels tries _BaseColorMap first for HDRP (the _MainTex GI mirror names the same
	  texture), so cutout/transparent materials now emit an opacity PNG.
	- GlassVmat emits F_RENDER_BACKFACES 1 when double-sided: glass.shader takes its CullMode from
	  that feature (game/core/shaders/glass.shader:112), so without it a pane loses its back faces.
	- No emission, remap, normal-scale or PBR-map path was touched (task 08).

	Build: dotnet build harness.csproj --no-incremental -> Build succeeded, 0 Warning(s), 0 Error(s).

	Fixture (fixtures/hdrp) re-baselined to the corpus: the four hand-written task-06 materials are
	replaced by 16 HDRP/Lit materials that mirror the 98-material census - 13 complex alpha-test
	(0.19 x8, 0.04, 0.63, 0.729, 0.5 x2), HDRP_Glass (cutoff 1.0, alpha 0.141, transparent,
	double-sided), HDRP_Curtain_01 (stored 0.615, disabled), HDRP_Lamp_01 (opaque control). 18 assets
	(16 .mat + 1 scene + 1 volume profile), no .shader. Packed with the shared
	fixtures/pack_fixture.py: tar 92,160 bytes md5 b89d06e21a5ae2c26573315d6837daa8, archive 6,029
	bytes md5 7cd981f8cb4571d605ce02f490f36335. Task 06's 6-asset / 10-file figures are superseded.

	The generated vmats were scanned directly for the per-material feature set and cutoff, plus the
	three totals, so a material cannot silently lose a feature behind a stable total. Result:
	"complex F_ALPHA_TEST 13, F_RENDER_BACKFACES 11, glass 1", 0 mismatches.

	Fixture import (--scenes, two runs): 17 selected / 17 converted / 34 files / 0 warnings /
	0 unresolved, exit 0, diff -r between the runs empty. HDRP line: "HDRP detected: 16 HDRP
	material(s), 1 HD light(s), 1 HD reflection probe(s), 1 volume(s)."
	HDRP_Glass vmat: shader "shaders/glass.shader", F_RENDER_BACKFACES 1, TextureTranslucency
	"[0.141000 ...]", no Alpha Test section. HDRP_Tree_Leaf: F_ALPHA_TEST 1, F_RENDER_BACKFACES 1,
	g_flAlphaTestReference "0.190". HDRP_Curtain_01 and HDRP_Lamp_01: zero Alpha Test tokens.
	verify_output.py: 17 files, 0 problems on the imported folder; 1 file, 0 problems on the scene.

	old_school (no --scenes, this build): 1,275 assets, 920 converted, 2,101 files, 97 warnings, 81
	unresolved, 0 errors - identical counts to the no-scenes baseline; the 2,110-file figure in task
	06 is the --scenes mode.
	Vmat census over the 98 materials: complex 97 + glass 1; F_ALPHA_TEST 13, F_RENDER_BACKFACES 11,
	F_TRANSLUCENT 0 (Glass consumes its translucent flag by routing to glass.shader), F_SELF_ILLUM 7,
	F_METALNESS_TEXTURE 95 - every other feature count unchanged.
	g_flAlphaTestReference distribution: 0.190 x8, 0.040, 0.500 x2, 0.630, 0.729. Tree_Leaf 0.190,
	Fan_01 0.729, Clock_01 0.500, Speaker_01 0.630, Shrubs_01 0.040; Lamp_01 has no Alpha Test
	section and Curtain_01 has none either (disabled cutoff, per the Q&A).
	Glass_Snaps014: shader "shaders/glass.shader", F_RENDER_BACKFACES 1, TextureTranslucency
	"[0.141176 0.141176 0.141176 0.000000]" (the census rounds the authored alpha to 0.141).
	Neutrality: hashed both trees (files < 2 MiB compared by md5, larger by size) against
	gates/old1, the --scenes baseline that task 06 proved identical to the pre-task output. Exactly 14
	files changed and all are material vmats (the 13 alpha-test ones plus glass_snaps014); 9 opacity
	PNGs were added; the 16 files only in the baseline are scene-mode outputs (13 _mirror_* prefabs,
	_iuprimitives/plane.vmdl, mergev2/test_range scene files) that a no-scenes run does not write.
	No .vmdl, .prefab, .tmat or texture other than the new opacity PNGs differed.
	verify_output.py: 823 files, 0 problems on the imported folder.
	Two runs of the same package and build are byte-identical: diff -r over both imported trees is empty;
	report.json differs only in Destination and ElapsedMilliseconds (206,371 vs 217,795 ms).

	building / city--1 (no --scenes, this build): building 24 files / 0 warnings / 0 unresolved;
	city--1 1,166 files / 3 warnings / 3 unresolved / 0 errors. diff -r against the populated
	HEAD-mode baselines gates/base-building (24 files) and gates/base-city (1,166 files) is empty for
	both, so this task is output-neutral outside HDRP materials.

	Evidence files: cache/scratch/hdrp/{fx1,fx2,old-new,old-new2} (logs + report.json),
	old-new-vs-old1.txt, gates/{building-new,city-new}.{log.txt,diff}.

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

### Work Review

2026-11-21 19:07 - Verdict: no further changes

	Reviewed: Editor/Core/UnityMaterial.cs (uncommitted, git hash-object 26e66e2c, the hash the Master
	handed over), the hdrp fixture re-baseline (36 files + repacked archive), and the worker evidence in
	cache/scratch/hdrp/. The task07-diff-r1.txt hunk for UnityMaterial.cs is byte-identical to
	`git diff HEAD -- Editor/Core/UnityMaterial.cs`, so the diff under review is the file on disk; the
	code mtime (18:38:23) precedes every evidence tree (fx1 18:39, old-new 18:40-18:45, old-new2 18:50),
	so the evidence is from the final bytes. Every number below was re-derived from the source materials
	and the generated vmats, not read out of the Notes.

	Code, read line by line against the required work:
	- ReadKeywords (UnityMaterial.cs:108-132) runs once in Parse (:65) and fills the Ordinal HashSet from
	  the m_ValidKeywords YAML list ("- _X" per line) and from a legacy space-separated
	  m_ShaderKeywords: line; m_InvalidKeywords is never read. Both fixture forms are covered
	  (HDRP_Fan_01.mat:14 list, HDRP_Lamp_01.mat:14 inline empty).
	- AlphaTest (:36), Translucent (:37) and Backfaces (:40) each gain their HDRP source through
	  HdrpFeature (:35), which is gated on Hdrp - Pipeline == UnityPipeline.Hdrp is set from the shader
	  guid at :63. The Standard/URP paths (_Mode, _AlphaClip, _Surface, _Cull) are untouched, and
	  _CullMode is only consulted for HDRP.
	- AlphaCutoff (:43) prefers _AlphaCutoff and falls back to _Cutoff, and feeds
	  g_flAlphaTestReference at :254.
	- Opacity extraction (:171-182) only reorders the source list for HDRP (_BaseColorMap first, because
	  _MainTex mirrors it for GI); the "remove packed-mask warning only when a channel was produced"
	  guard at :183 is unchanged.
	- GlassVmat (:343) emits F_RENDER_BACKFACES only when double-sided; sbox-public
	  game/core/shaders/glass.shader:112 is `RenderState(CullMode, F_RENDER_BACKFACES ? NONE : DEFAULT)`,
	  so the feature is what makes a pane keep its back faces. The untextured-translucent route at :223
	  fires before any Alpha Test section, so Glass's consumed cutoff cannot leak in.

	Source census (98 HDRP/Lit .mat under old_school_model/Assets): _AlphaCutoffEnable 1 on 14 = the 13
	non-Glass complex materials + Glass; cutoffs 0.19 x8, 0.04, 0.5 x2, 0.63, 0.729, 1.0 (Glass);
	_DoubleSidedEnable 1 on 11 with _CullMode 0 on exactly those 11; _SurfaceType 1 /
	_SURFACE_TYPE_TRANSPARENT on Glass only. Keyword and property never disagree in this corpus and no
	material carries _ALPHATEST_ON with the enable at 0, so the gate ordering is not load-bearing here.

	old_school, no --scenes (cache/scratch/hdrp/old-new, report.json): 1,275 assets / 920 converted /
	2,101 files / 97 warnings / 81 unresolved / 0 errors - the baseline counts. Vmat scan: F_ALPHA_TEST
	13, exactly the 13 enabled non-Glass materials; F_RENDER_BACKFACES 11; F_TRANSLUCENT 0;
	g_flAlphaTestReference 0.190 x8, 0.040, 0.500 x2, 0.630, 0.729 - Tree_Leaf 0.190, Fan_01 0.729,
	Clock_01 0.500, Speaker_01 0.630, Shrubs_01 0.040; Lamp_01 and Curtain_01 carry no Alpha Test
	token. F_SELF_ILLUM 7 and F_METALNESS_TEXTURE 95 equal gates/old1 (which had 0 and 0 for the two new
	features). Neutrality: 14 changed files vs old1, all material vmats (the 13 plus
	tiling/glass_snaps014), 9 added *_iup_opacity.png, the 16 removals all scene-mode-only outputs - no
	.vmdl, .prefab or texture other than the new PNGs moved.
	Glass_Snaps014.vmat: shaders/glass.shader, F_RENDER_BACKFACES 1, TextureTranslucency
	"[0.141176 0.141176 0.141176 0.000000]", no Alpha Test section.

	Fixture: 36 files (16 .mat + .meta, HDRP.unity + .meta, HDRPVolumeProfile.asset + .meta), no .shader;
	repacking the folder with fixtures/pack_fixture.py from this tree reproduces 18 assets, tar 92,160
	bytes md5 b89d06e21a5ae2c26573315d6837daa8 and archive 6,029 bytes md5
	7cd981f8cb4571d605ce02f490f36335. Fixture import fx1: 17 assets / 17 converted / 34 files / 0
	warnings / 0 unresolved, HDRP line "16 HDRP material(s), 1 HD light(s), 1 HD reflection probe(s), 1
	volume(s)"; fx1 vs fx2 `diff -r` empty. Fixture vmats: 13 F_ALPHA_TEST, 11 F_RENDER_BACKFACES (10
	complex + glass), HDRP_Glass shader glass / F_RENDER_BACKFACES 1 / TextureTranslucency 0.141 / no
	Alpha Test, HDRP_Curtain_01 and HDRP_Lamp_01 zero tokens. The materials mirror the corpus one for
	one (cutoffs 0.19 x8, 0.04, 0.5 x2, 0.63, 0.729, Glass 1.0 + _SurfaceType 1, Curtain 0.615 disabled,
	Lamp opaque control), so no fixture material loses a feature.

	Gates re-run on this tree: dotnet build harness.csproj --no-incremental -> 0 Warning(s) / 0
	Error(s); verify_output.py -> 823 files 0 problems (old_school), 17 files 0 problems (fixture
	folder), 1 file 0 problems (scene); old-new vs old-new2 `diff -r` empty; building-new (24 files,
	0/0) vs gates/base-building and city-new (1,166 files, 3 warnings, 3 unresolved) vs gates/base-city
	both diff-empty, so nothing outside HDRP materials moved.

	Scope: Editor/Core/UnityMaterial.cs is the only source file changed (git status: one M under
	Editor/); the fixture re-baseline is task work (4 hand-written materials deleted, 16 added, archive
	repacked). The tasks/01-05 deletions, the .humans-only/archive additions and the task-09 edit are the
	Director/Master moves excluded from this review and were not read. No unrequested tooling rode along:
	the worker's check_hdrp_fixture.py is gone from the tree (a copy survives only outside the repo as
	cache/scratch/hdrp/check_hdrp_fixture.py.removed) and no build output is tracked.

	Findings:
	1. [Low] Editor/Core/UnityMaterial.cs:32-34 - the comment says "the property is the authority when
	   they disagree", but HdrpFeature (:35) ORs keyword with property, so the keyword wins a
	   disagreement (a material with _ALPHATEST_ON and _AlphaCutoffEnable 0 would still alpha-test).
	   Nothing in the corpus or fixture is in that state and the Clarification Q&A only forbids a stored
	   _AlphaCutoff from triggering the feature, so today's output is right; the Notes' parenthetical
	   "(_ALPHATEST_ON survives on 8 Standard-authored HDRP materials, and on ordinary Standard
	   materials)" is not reproducible either - the 8 materials with a stale _MetallicGlossMap carry no
	   _ALPHATEST_ON, and building.unitypackage has no m_ShaderKeywords line at all. Fix: reword the
	   comment to "either one enables the feature", or make the property authoritative
	   (`Hdrp && (Numbers.ContainsKey(property) ? Number(property, 0) == 1 : keywords.Contains(keyword))`)
	   if the Q&A is meant to win. Comment or semantics only; do not re-open the counts.
	2. [Low] Editor/Core/UnityMaterial.cs:175 - `ColorProperties.Prepend("_BaseColorMap")` duplicates the
	   entry ColorProperties already ends with (:29), so an HDRP material whose base-colour texture is
	   missing posts the same missing-texture issue twice (UnityImport.cs:139-140 adds without dedupe).
	   Not triggered here (97 warnings / 81 unresolved equal the baseline). Fix:
	   `Hdrp ? ColorProperties.Where( p => p != "_BaseColorMap" ).Prepend( "_BaseColorMap" ) :
	   ColorProperties`.
	3. [Low] Documentation/Unity-To-SBox-Automated.md:318 - the Glass bullet still lists the translucency
	   triggers as `_Surface: 1` (URP), legacy `_Mode` 2/3 and RenderType=Transparent, and omits HDRP
	   `_SurfaceType 1` / `_SURFACE_TYPE_TRANSPARENT`; 7.3 states no alpha-test or double-sided rule
	   either. Doc work is outside this task's Required work, so this is follow-up, not a gate.
	4. [Low] The r1 diff is `git diff HEAD`, so the 32 new fixture .mat/.meta files never appear in it
	   (only the 4 deletions and the binary archive do). They are present and correct on disk - proved by
	   the repack above - but the commit has to `git add` them or the re-baseline lands as an archive that
	   lost its materials.

	Tree: 03e1ef5 (HEAD) + the uncommitted task-07 edits, reviewed as they stand
	Editor/Core/UnityMaterial.cs                                        26e66e2cf0a6fa75fd4241e7e0dd18df7abc95da
	Documentation/Unity-To-SBox-Automated/fixtures/hdrp.unitypackage   4366c8f83270e281f300ccef4a0ad35bf5012707
	Documentation/Unity-To-SBox-Automated/fixtures/hdrp/ (36 files)    file-list md5 8bca21c93e39aaa43c0a7a34f74cf353; tar md5 b89d06e21a5ae2c26573315d6837daa8
	.../fixtures/hdrp/Assets/Fixture/Materials/HDRP_Glass.mat          80c1965640382944ec2eb7a67046f687598bd71f (spot check; all 36 hashed into the list md5)