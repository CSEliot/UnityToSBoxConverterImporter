## Status: STARTED? YES! CLARIFIED? YES! HISTORY? YES! REVIEWED? YES! DONE? YES! WORKER: sa-0-bc4bfc28 OTHER: reviewed 18:31 no further changes; HDRP foundation for 07-11

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
	   "HDRP detected: N HDRP material(s), N HD light(s), N HD reflection probe(s), N volume(s)."
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
	- building and city--1, run without --scenes, stay byte-identical: building 24 files / 0 warnings /
	  0 unresolved; city--1 1,166 files / 3 warnings / 3 unresolved (HEAD 9d709a8). If a worker uses
	  --scenes for them, compare against a HEAD baseline taken in that same mode instead.
	- Harness builds 0 warnings / 0 errors; two runs byte-identical; verify_output.py 0 problems.

### Work Review

2026-11-21 18:11 - Verdict: changes required

Reviewer re-executed every gate from the current tree and from HEAD 9d709a8 (git archive, harness built 0/0, line endings normalized after the archive applied core.autocrlf). Counts, guids, scoping and output-neutrality all hold: 98/120/45/3 match a raw package scan; all 5 shader and 7 component guids match the Unity Graphics .meta files; the fixture repacks to tar md5 2488314ea3af6e5a0effad044089b144; fixture import 5 converted / 10 files / 0 / 0, two runs identical, verify_output 0 problems; old_school 936 / 2,110 / 97 / 81 with 2,861 / 201 instances and empty old.diff and old-vs-baseline.diff against a populated 2,110-file baseline; HEAD no-scenes building 24 files and city--1 1,166 files are byte-identical to building1/city1, and HEAD --scenes gives 26 and 1,192, proving the re-baseline.

1. [Medium] gates/gates_run.sh:27 imports fixtures/building.unitypackage, which does not exist (the package is Testing/building.unitypackage), so gates_run.log:7-8 records exit=127 with an empty assets line, and building.diff is a 0-byte diff of two empty trees while building1/2 were repopulated at 17:51 after the 17:48 diff. Fix: correct the path, re-run both building imports, regenerate building.diff.
2. [Low] Task-file evidence ("the 6 lines still reading MonoBehaviour (script)") is wrong: gates/old1/log.txt:3898 and :3924 are two lines covering seven components (MergeV2 6 + Test_Range 1), all SimpleCameraController; fix the sentence.
3. [Low] UnityHdrp.cs:48 says "N Lit material(s)" for all five table shaders, so HDRP/Unlit or Decal would be mislabelled, and "N reflection probe(s)" omits plain 215 probes seen in the census. The numbers are exact for old_school, so the wording is a caveat, not a wrong count here.
4. [Low] UnitySceneExport.cs:321-324 claims the summary and census "can never disagree", but Count also includes type-114 blocks the census loop drops when Owner()/GameObject attribution fails at :187; count the same filtered set or soften the comment.
5. [Low] UnityHdrp.cs:62 states the property list is "as Lit.shader lists them"; the arrays have no consumer yet, so cross-check every name before task 07 reads them.

Tree: uncommitted working tree at HEAD 9d709a8
  2cbc0345920b3c8b33fbfaa59ca2ac3dad59fa08  A Editor/Core/UnityHdrp.cs
  fb5c3a8957bea7fdb5099613658e3371c1f3f740  M Editor/Core/UnityMaterial.cs
  a14dd22118e151ac206a4a9dd0b81799a3257823  M Editor/Core/UnitySceneExport.cs
  f294b6237291fcfdcdc483795fbfde3ce9dd3cae  M Editor/Core/UnityImport.cs
  (fixture) Documentation/Unity-To-SBox-Automated/fixtures/hdrp/ 12 files; hdrp.unitypackage md5 575237274cc16b091d6c8c6bc4b8292f, tar md5 2488314ea3af6e5a0effad044089b144

2026-11-21 18:21 - Verdict: no further changes

Re-checked only the five fixes, from a full `dotnet build harness.csproj --no-incremental` (0 Warning(s), 0 Error(s)) and my own fresh runs.

1. [Medium] closed. gates_run.sh:27 now imports $TEST/building.unitypackage, and building.diff / city.diff were regenerated at 18:12 over populated trees and are 0 bytes; I re-diffed them myself: base-building vs building1 0 bytes over 24 files, base-city vs city1 0 bytes over 1,166 files, building1 vs building2 and city1 vs city2 also 0.
2. [Low] closed. Task evidence :121-123 now reads "the two lines ... cover seven SimpleCameraController components (MergeV2 6 + Test_Range 1)", which matches gates/old1/log.txt:3898 and :3924.
3. [Low] closed. UnityHdrp.cs:48 now emits "HDRP detected: N HDRP material(s), N HD light(s), N HD reflection probe(s), N volume(s)."; observed live as 4/1/1/1 on the fixture and 98/120/45/3 on old_school, and required-work item 4 (:41) and the review point (:135-138) were updated to the same string, so the overclaim is gone rather than reworded around.
4. [Low] closed. UnitySceneExport.cs:321-323 now states the census can be lower than the total on a lossy scene, which is what the two code paths do, instead of claiming they cannot disagree.
5. [Low] closed. UnityHdrp.cs:62-63 now calls the array a curated subset and names the two _MATERIAL_FEATURE_* entries as keywords. I also checked every name against Unity's HDRP shader source: all present (Lit 58, Unlit 19, Decal 25, TerrainLit 11), so the table is real, just not exhaustive.

Independent re-verification with the fixed build: fixture (--scenes, twice) exit 0, 5 selected / 5 converted / 10 files / 0 warnings / 0 unresolved, two runs byte-identical, the HDRP line above, no "MonoBehaviour (script)" line, verify_output.py "checked 5 files, 0 problems" on the imported folder and 1 file / 0 problems on the generated .scene. old_school (--scenes) 936 converted / 2,110 files / 97 warnings / 81 unresolved / 0 errors, MergeV2 4 roots / 2,861 instances / 0 skipped / 0 lost, Test_Range 3 / 201 / 0 / 0, output byte-identical to gates/old1 (diff -r empty), and old1 is byte-identical to the pre-task baseline. UnityMaterial.cs and UnityImport.cs are unchanged at their reviewed hashes; UnityHdrp.cs and UnitySceneExport.cs differ from review 1 only by the report string and the two corrected comments, everything else in their hunks is what I reviewed at 18:11.

Tree: uncommitted working tree at HEAD 9d709a8
  73f5800ffbb0738c91637a43186dcc9cb26d6bcc  A Editor/Core/UnityHdrp.cs
  fb5c3a8957bea7fdb5099613658e3371c1f3f740  M Editor/Core/UnityMaterial.cs
  0249d0a8449faf8e4aa9d164d4dc680a49457165  M Editor/Core/UnitySceneExport.cs
  f294b6237291fcfdcdc483795fbfde3ce9dd3cae  M Editor/Core/UnityImport.cs
  (fixture, unchanged this round) Documentation/Unity-To-SBox-Automated/fixtures/hdrp/ 12 files; hdrp.unitypackage md5 575237274cc16b091d6c8c6bc4b8292f, tar md5 2488314ea3af6e5a0effad044089b144

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

	--- Task 06 evidence (worker, uncommitted tree; harness rebuilt from the current Editor/Core) ---

	Build: dotnet build harness.csproj -> Build succeeded, 0 Warning(s), 0 Error(s).

	Fixture: fixtures/hdrp holds 6 assets (4 .mat, 1 .unity, 1 VolumeProfile .asset) and no .shader,
	packed with the shared fixtures/pack_fixture.py: 6 assets, tar 40,960 bytes md5
	2488314ea3af6e5a0effad044089b144, archive 4,831 bytes. Tasks 07-11 must re-run that packer and
	rewrite this line after every repack.

	Fixture import (harness --scenes, two runs): 6 archive assets, 5 selected (the .asset profile is
	Unsupported and excluded), 5 converted, 10 files, 0 warnings, 0 unresolved, exit 0. Two runs
	byte-identical (diff -r empty; report.json differs only in the run's Destination path).
	Report: "HDRP detected: 4 HDRP material(s), 1 HD light(s), 1 HD reflection probe(s), 1 volume(s).";
	census lines name 1 HDAdditionalLightData (HD Light), 1 HDAdditionalReflectionData (Reflection
	Probe), 1 Volume (Global Volume); no "MonoBehaviour (script)" line. verify_output.py: 5 files,
	0 problems on the imported folder and 1 file, 0 problems on the generated .scene.

	old_school (--scenes, two runs): 1,277 assets, 936 converted, 2,110 files, 97 warnings, 81
	unresolved, 0 errors; MergeV2 roots 4 / instances 2,861 / skipped 0 / lost 0; Test_Range roots 3 /
	instances 201 / skipped 0 / lost 0. Two runs byte-identical AND byte-identical to the pre-task
	baseline in cache/scratch/hdrp/out (diff -r empty).
	Report: "HDRP detected: 98 HDRP material(s), 120 HD light(s), 45 HD reflection probe(s), 3 volume(s)."
	(98 = every converted material, all HDRP/Lit; 120/45/3 = both scenes: 119+1 lights, 44+1 probes,
	2+1 volumes). Census now names HDAdditionalLightData 119+1, HDAdditionalReflectionData 44+1,
	HDAdditionalCameraData 6+1, Volume 2+1, StaticLightingSky 2+1 and PlanarReflectionProbe 1; the two
	lines still reading "MonoBehaviour (script)" cover seven SimpleCameraController components
	(MergeV2 6 + Test_Range 1), not HDRP.
	MaterialConversions reports ShaderName "HDRP/Lit" for all 98.

	building / city--1 (two runs each): building 24 files, 0 warnings, 0 unresolved; city--1 1,166
	files, 3 warnings, 3 unresolved, 0 errors. Both byte-identical run-to-run AND byte-identical to a
	harness built from HEAD 9d709a8 in a throwaway worktree, so this task is output-neutral.

	Master re-baselined Acceptance to these measured no-scenes counts: the earlier 26 / 1,192 figures
	were the --scenes mode, not a change caused by this task. The HEAD-baseline diff above is the proof
	that neither output moved.

	Review points on the implementation:
	- "N HDRP material(s)" counts every converted material whose m_Shader guid is in the HDRP table
	  (Lit, Unlit, LitTessellation, Decal, TerrainLit); the source project only exercises HDRP/Lit.
	- "N HD reflection probe(s)" counts HDAdditionalReflectionData only. Plain Unity 215 probes and
	  PlanarReflectionProbe have no slot in the fixed one-line wording and are named in the census.
	- Counts are per package (scene totals are summed), which is why old_school reads 120/45/3 where
	  the Description quotes MergeV2-only 119/44/2.
	- UnityHdrp.cs owns the tables; detection is three call sites: UnityMaterial.Parse (Pipeline +
	  ShaderName), UnitySceneExport.TypeName/Count (census naming + per-scene counts) and the summary
	  line in UnityImport.PrepareFiles. Property parsing and every generated asset are untouched.

	--- Master resolution after review 1 (18:11) ---
	The report wording is now "N HDRP material(s), N HD light(s), N HD reflection probe(s), N
	volume(s)", matching all five shader table entries and the HD-only probe count. Final re-runs from
	the current tree: fixture 4 HDRP / 5 converted / 10 files / 0 warnings / 0 unresolved, verify_output
	"checked 5 files, 0 problems"; old_school (old3) 936 / 2,110 / 97 warnings / 81 unresolved / 0
	errors, MergeV2 2,861 / 0, Test_Range 201 / 0, and its imported tree is identical to old1 (the only
	difference was the re-run's report.json at the destination root). building.diff and city.diff were
	regenerated from populated HEAD/candidate trees: base-building vs building1 (24 files) and
	base-city vs city1 (1,166 files) are empty, as are the two-run comparisons. The gate script's
	building package path is corrected to Testing/building.unitypackage.

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