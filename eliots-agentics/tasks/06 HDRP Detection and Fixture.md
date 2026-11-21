## Status: STARTED? NO! CLARIFIED? NO! HISTORY? NO! REVIEWED? NO! DONE? NO! WORKER: NONE OTHER: HDRP backlog, foundation for 07-11

## Task

	Detect HDRP packages, name their shaders and components in the import report, and add a small
	hand-written HDRP fixture that later HDRP tasks extend. No HDRP behaviour changes beyond detection
	and reporting.

### Description

	The importer currently has no notion of a render pipeline. HDRP materials parse as ordinary
	materials because their properties are present, but the shader itself is unreachable: HDRP Lit
	lives in Packages/ and the .unitypackage contains 0 .shader entries, so ConfigureShader never runs
	and no "approximated" warning appears. HDRP scene components are reported as anonymous
	MonoBehaviour (script) or ReflectionProbe lines, and nothing says "this is an HDRP project".

	Current tree (commit 1657ed6, tasks 01-05 landed) on
	Testing/old_school_project.unitypackage (HDRP 17.3.0):
	- 1,277 assets, 936 converted, 2,110 files, 97 warnings, 81 unresolved (all missing textures), 0 errors.
	- MergeV2 roots 4, instances 2,861, skipped 0; Test_Range roots 3, instances 201, skipped 0.
	- 98 materials all use HDRP/Lit guid 6e4ae4064600d784cac1e41a9e6f2e59.
	- MergeV2 has 119 HDAdditionalLightData, 44 HDAdditionalReflectionData, 6 HDAdditionalCameraData,
	  2 Volume, 2 StaticLightingSky, 1 PlanarReflectionProbe; one unresolved script is
	  SimpleCameraController (e8a636f62116c0a40bbfefdf876d4608), not HDRP.

	Research: eliots-agentics/tasks/hdrp-research.md and the scratch reports it names.

### Required work

	1. Add a known-pipeline table in a new Editor/Core file (for example UnityHdrp.cs), keyed by guid:
	   HDRP/Lit 6e4ae4064600d784cac1e41a9e6f2e59, plus the HDRP/Unlit, LitTessellation, Decal and
	   TerrainLit guids found in the source project's HDRP package. Each entry carries a display name
	   and the property names the pipeline is known to declare. Do not require .shader source.
	2. Mark parsed materials with Pipeline = Hdrp when m_Shader matches a known HDRP guid. Leave the
	   existing property parsing untouched; later tasks consume the flag.
	3. Add a script-guid name table for HDAdditionalLightData (7a68c43f...), HDAdditionalReflectionData
	   (d0ef8dc2...), HDAdditionalCameraData (23c1ce4f...), Volume (17251560...), StaticLightingSky
	   (441482e8...), PlanarReflectionProbe (a4ee7c3a...), and DecalProjector if found. Use it in
	   UnitySceneExport's type-name/census reporting instead of "MonoBehaviour (script)".
	4. Emit one Information line per import when any HDRP material or script is detected:
	   "HDRP detected: N Lit material(s), N HD light(s), N reflection probe(s), N volume(s)."
	   Do not emit 98 per-material lines in this task.
	5. Add Documentation/Unity-To-SBox-Automated/fixtures/hdrp/ (an Assets/ tree) with hand-written YAML
	   for four HDRP materials (opaque, alpha-cutout 0.3, double-sided, transparent/emissive), one scene
	   with a HDAdditionalLightData light, one global Volume with VisualEnvironment and Fog, one
	   ReflectionProbe with HDAdditionalReflectionData. Pack it with the shared
	   Documentation/Unity-To-SBox-Automated/fixtures/pack_fixture.py <folder> <out> and write
	   fixtures/hdrp.unitypackage at the fixtures root. The fixture must contain no .shader source,
	   matching a real export.
	6. Import the fixture and record: converted/files/warnings/errors, the HDRP-detected line, and the
	   component names in the report. Two runs must be byte-identical.

### Acceptance

	- The fixture imports; its report contains the HDRP-detected line and names the HD light, Volume
	  and reflection probe without "MonoBehaviour (script)".
	- old_school still reports 936 converted / 2,110 files / 97 warnings / 81 unresolved / 0 errors;
	  MergeV2 2,861 instances with 0 skipped; Test_Range 201 with 0 skipped; output byte-identical.
	- building 26 files / 0 warnings / 0 unresolved and city--1 1,192 files / 4 warnings / 3 unresolved
	  stay byte-identical.
	- Harness builds 0 warnings / 0 errors; two runs byte-identical; verify_output.py 0 problems.

## Notes

	Implementation home: new Editor/Core/UnityHdrp.cs; hooks in UnityMaterial.Parse /
	UnityImport material reporting, UnitySceneExport.TypeName and the step-7 census, and the import
	summary in UnityImport. Existing shader resolution is ImportPlan.cs:72 and UnityImport.cs:151/207;
	do not try to resolve HDRP shaders through it, because the package cannot contain them.

	Baseline counts above are from the fresh run in cache/scratch/hdrp/report.json. The HDRP fixture
	will be extended by tasks 07-11; keep its packer deterministic and record its current md5/tar hash
	after every repack.

	The fixture must not depend on Unity's HDRP package being present on the machine. All guids and
	YAML are literal.

## Clarification Q&A

	Q: Should detection try to scan a Unity project folder for shaders?
	A: No. The importer only reads .unitypackage archives; a project-folder scan is out of scope.

## Relevant History

	Tasks 01-05 fixed prefab wrappers, material-slot mapping, prefab-free scenes, primitives and scene
	loss reporting. HDRP is the next support boundary; this task is detection/fixture only so later
	tasks have a fast gate.

## Stylistic Task Notes:

	References to variables like N and M automatically imply that these are to be exposed properties
	for developer tweaking, if they become code knobs.

## Skills To Know: for you and/or subagents:
	sbox-offline-compile-check
	unityimporterplus
	unity-to-sbox-conversion
	sbox-model-material-resources

## Post-Task Follow Ups