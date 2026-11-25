## Status: STARTED? YES! CLARIFIED? YES! HISTORY? YES! REVIEWED? YES! DONE? YES! WORKER: research subagents OTHER: reviewed 17:37 no further changes; tasks/01-05 archived by the Director

## Task

	Now the Unity Importer Plus project needs to support HDRP unity projects, plan tasks and write them to /tasks/.

### Description

	The project: C:\cseliot\1-Development\1-SBox\2-Projects\unityimporterplus\
	Research and build tasks in to put in {project}\eliots-agentics\tasks\ correlating to your research. 

	Recently, an attempt was made to import "C:\cseliot\1-Development\1-SBox\2-Projects\unityimporterplus\Testing\old_school_project.unitypackage", with pitiful success.
	The old_school_project.unitypackage was made from project C:\cseliot\1-Development\4-Unity\2-Projects\old_school_model.


### Work Review

2026-11-21 17:35 - Verdict: changes required

Tree:
ce2ef259eae6ca735b44ba734240a35788f56d03  eliots-agentics/tasks/hdrp-research.md
bd7b71009c4f73ae4a4886038ec0029a3691a28e  eliots-agentics/tasks/06 HDRP Detection and Fixture.md
861da171a661c3d3796f19165e3850f568512a13  eliots-agentics/tasks/07 HDRP Lit Surface Semantics.md
acd76fe8a010802864ea51f90fdcc829ccd39841  eliots-agentics/tasks/08 HDRP Lit PBR and Emission Fidelity.md
2e3e7a6af4c00b85a7108e37e0e57d0ed56d9ed0  eliots-agentics/tasks/09 HDRP Environment from Volumes.md
6937460f5a862daf690558d5ca110dde432b123b  eliots-agentics/tasks/10 HDRP Lights and Cameras.md
dbe28251349d80b75e9368c0df788cf650041f14  eliots-agentics/tasks/11 HDRP Probes, Decals and Volume Post-Processing.md

Verified independently against the source project and the fresh report: 1,277 assets / 936 converted /
2,110 files / 97 warnings / 81 unresolved / 0 errors; MergeV2 2,861 instances, Test_Range 201, 0
skipped; 98 HDRP Lit materials, 14 alpha-test, 11 double-sided, 1 transparent, 10 metallic remaps,
7 emissive maps, 4 non-black tints (3 above 1), 10 transmission, 9 SSS; 119 lights (63 spot, 45 area,
9 point, 2 directional), 90 Lumen / 29 Lux, 108 shadow-on / 11 off, 88 with CCT enabled; 44 reflection
probes, 6 cameras, 2 Volumes, 2 StaticLightingSky, 1 planar probe. All six script guids, the Lit shader
guid and the cited UnityMaterial/UnitySceneExport lines are correct. EnvmapProbe, PostProcessVolume,
SkyBox2D, Fog-vs-GradientFog and the no-area/no-planar limits match the engine source. No overpromise
was found for physical units, procedural sky, HDRI texture key, area lights or planar probes.

Findings:
1. [High] 07 HDRP Lit Surface Semantics.md:48-50 is self-contradictory. Requirement 3 routes Glass
   through GlassVmat, which emits only shaders/glass.shader and no Alpha Test section, yet acceptance
   demands F_ALPHA_TEST = 14 and an alpha reference for Glass. Glass is one of the 14, so the complex
   count can only be 13. State F_ALPHA_TEST = 13 plus Glass as glass.shader with translucency 0.141,
   and drop the Glass alpha-reference assertion.
2. [Medium] 08 HDRP Lit PBR and Emission Fidelity.md:13-15 and :49-51 overstate the metal failure.
   Plane and Glass have no bound _MaskMap and _Metallic 0, so they already emit no metalness texture
   and g_flMetalness 0.000; 8 of the 10 are currently raw mask-R metallic. Say 8, not 10.
3. [Medium] 08 HDRP Lit PBR and Emission Fidelity.md:16-17 and :51-52 treat the three smoothness
   remaps as wrong. Their values are 0.99999976/0.9999999, float serialization noise; the effect is
   below 8-bit output. Relabel them as a no-op/epsilon regression case and accept byte-identical
   roughness for those three.
4. [Low] 07 HDRP Lit Surface Semantics.md:13 says 13 use a non-default _AlphaCutoff, but the
   enumeration gives 12 non-default (0.19 x8, 0.04, 0.63, 0.729, 1.0) plus 2 at the 0.5 default.
   Fix the count and wording.
5. [Low] 10 HDRP Lights and Cameras.md:12 says 108 soft shadows; m_Shadows.m_Type 1 is Hard, with
   11 off. :13 says 119/119 CCT at 3613 K and 7614 K; the actual values are 3613/3849/6570/7614/15000
   and only 88 have m_UseColorTemperature 1. Fix the counts and wording.
6. [Low] 06 HDRP Detection and Fixture.md:43-47 introduces a new pack_hdrp_fixture.py inside the
   fixture folder. The established convention is the shared
   Documentation/Unity-To-SBox-Automated/fixtures/pack_fixture.py <folder> <out>, with <name>/ and
   <name>.unitypackage at the fixtures root. Reuse it and output fixtures/hdrp.unitypackage.

2026-11-21 17:37 - Verdict: no further changes

	Re-check of the six fixes only, against the same source/report evidence. All six are now correct
	and nothing new contradicts:
	1. [High] resolved - 07:49-53 states F_ALPHA_TEST = 13 on complex-shader materials, Glass as
	   shaders/glass.shader with TextureTranslucency 0.141, and no alpha reference asserted on Glass;
	   the fixture clause still matches.
	2. [Medium] resolved - 08:13-16 and :51-53 name the 8 mask-bound materials and record Plane and
	   Glass as already g_flMetalness 0.
	3. [Medium] resolved - 08:17-19, :36-37 and :54-55 call the 0.99999976/0.9999999 remaps an
	   epsilon no-op regression case and require a byte-identical Roughness section.
	4. [Low] resolved - 07:12-13 says 12 non-default cutoffs plus two at the 0.5 default (Clock_01,
	   Decal); the enumeration still sums to 14 enabled.
	5. [Low] resolved - 10:12-15 says 108 Hard (m_Shadows.m_Type 1), 11 off, five CCT groups
	   (3613, 3849, 6570, 7614, 15000) and 88 with m_UseColorTemperature 1; still 119 lights.
	6. [Low] resolved - 06:43-49 uses the shared fixtures/pack_fixture.py <folder> <out> and writes
	   fixtures/hdrp.unitypackage at the fixtures root.

	Cross-checks on the edited passages: 06/07/08 agree on the four-material fixture and its
	extension; 08's epsilon no-op matches its own acceptance and 07's non-material byte-identity;
	10's unit and temperature counts still partition the 119 lights; no task asserts a feature its
	route cannot produce. No new overpromise was introduced. Planning still changes only
	eliots-agentics/tasks/; .humans-only/ archive moves were not judged.

	Tree: 8fc8d0c88f8e0750acb76c0b8d93d94cba91498f
	d81705bbcae0d4118dc1ce79a4c3f2f6eb4cb14f  eliots-agentics/tasks/hdrp-research.md
	dfa3412525c62901adf2f0e36cfeb0af7ce8ae25  eliots-agentics/tasks/06 HDRP Detection and Fixture.md
	1691472c2aa1f1d2a027f4e55c1708b6b2295d70  eliots-agentics/tasks/07 HDRP Lit Surface Semantics.md
	a34dc6f9f7abc34dff94973211386ab68e490d62  eliots-agentics/tasks/08 HDRP Lit PBR and Emission Fidelity.md
	2e3e7a6af4c00b85a7108e37e0e57d0ed56d9ed0  eliots-agentics/tasks/09 HDRP Environment from Volumes.md
	c861c5519c945947fa68d4ea5777e15a7f5ea901  eliots-agentics/tasks/10 HDRP Lights and Cameras.md
	dbe28251349d80b75e9368c0df788cf650041f14  eliots-agentics/tasks/11 HDRP Probes, Decals and Volume Post-Processing.md

## Notes

	Research and planning completed 2026-11-21; no importer code was changed. Deliverables:
	- eliots-agentics/tasks/hdrp-research.md (evidence, gap table, s&box constraints, task map)
	- 06 HDRP Detection and Fixture.md
	- 07 HDRP Lit Surface Semantics.md
	- 08 HDRP Lit PBR and Emission Fidelity.md
	- 09 HDRP Environment from Volumes.md
	- 10 HDRP Lights and Cameras.md
	- 11 HDRP Probes, Decals and Volume Post-Processing.md

	Current baseline (fresh harness, commit 1657ed6): old_school 1,277 assets / 936 converted /
	2,110 files / 97 warnings / 81 unresolved (missing textures) / 0 errors; MergeV2 2,861 instances,
	Test_Range 201, 0 skipped. building 26 files and city--1 1,192 files are the byte-identity gates.

	The plan splits the work into detection/fixture (06), two independent material edits (07, 08),
	and three scene edits (09, 10, 11). It does not promise physical-unit or procedural-sky parity;
	area lights, planar probes, transmission/SSS, light layers and unsupported Volume overrides are
	reported, not faked.

## Clarification Q&A


## Relevant History 

## Stylistic Task Notes:
References to variables like X and Y automatically imply that these are to be exposed properties for developer tweaking.

## Skills To Know: for you and/or subagents:
	sbox-agent-playtest
	sbox-live-play-agent-loop
	sbox-cli-driving


## Post-Task Follow Ups 
