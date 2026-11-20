## Status: STARTED? NO! CLARIFIED? NO! HISTORY? NO! REVIEWED? NO! DONE? NO! WORKER: NONE OTHER: backlog, generated from tasks/sbox-expert-research.md

## Task
	Collapse scene skip warnings into per-cause summaries and report them in SceneConversions.

### Description
	A scene conversion can skip thousands of instances without saying why. Top-level skips are silent: Editor/Core/UnitySceneExport.cs:68 increments the Skipped counter and continues. Nested-instance failures produce one warning per distinct source prefab (Editor/Core/UnityPrefabExport.cs:812/985), deduped by UnityImport, and the editor log contains no per-instance lines. The 2026-11-20 school import finished with 1,260 warnings; the cause of the empty MergeV2 scene was only visible as the prefab-conversion error group, and the report's SceneConversions table (Editor/Core/UnityImport.cs:441-444, written at 482) records only Roots, Instances, Nested, AddedComponents, RemovedObjects and Skipped; it cannot say why.

	Required work:
	1. In UnitySceneExport, collect skips by cause and by source prefab: unconverted source prefab (with the prefab's path), source prefab missing from the package, nested object belonging to an instance that was not converted, object parented to a nested prefab, missing parent object, unsupported 2018.2 format, unsupported added component. Keep the existing per-object detail available for debugging behind the report, not in the warning list.
	2. Emit one warning per cause with a count and up to `MaxSampleNames` sample instance names (a named constant, 10 is reasonable). One cause with 2,861 instances must read as one line, not 2,861.
	3. When a scene has prefab instances but produced zero stubs, add one Error issue (conversion-skipped) naming the dominant cause; a partial conversion stays a warning. Do not raise an error for a scene that legitimately has no prefab instances.
	4. Extend SceneConversions with per-scene skip counts by cause (a small object keyed by cause) and keep the existing fields for compatibility. The report must let a reader tell "2,861 of 2,861 prefab instances skipped because 255 source prefabs were variants" without opening the editor log.
	5. Do not change generated scene JSON or prefab JSON for successful conversions.

	Acceptance:
	- Reimporting Testing/old_school_project.unitypackage with tasks 01/02 absent produces one grouped warning for the 2,861 MergeV2 skips and one for the 201 Test_Range skips, naming the prefab-variant cause and the top sample names.
	- The grouped lines are the only scene-skip warnings the import emits; record the before and after warning counts in the task file, and do not assume unrelated model or material warnings disappear. The new Error is present once per affected scene.
	- The report JSON carries the per-cause counts; verify with jq or python, not by eye.
	- With tasks 01/02 present, the same package reports zero skipped prefab instances and the grouped warnings disappear.
	- Output scene/prefab bytes are identical to before the change for a successful fixture.

## Notes

	Implementation home: Editor/Core/UnitySceneExport.cs (SceneResult record, Statistics at 37, warning sites at 78, 85, 93, 149, 194, 206; the skipped counter), Editor/Core/UnityImport.cs (scene loop 431-447 and report object 482).

	Keep the report schema additive: older exported reports must still be readable, and the UI's warning totals should use the grouped list. The 320-character-style truncation the editor already applies to grouped issues (see the log's "346 resources: ..." line) is a good target shape.

## Clarification Q&A


## Relevant History 

	Source: tasks/sbox-expert-research.md. The research found 1,260 warnings alongside 479 errors in three groups; scene skips are silent at the top level and only counted, with no cause count in the report. This task makes the next failure explain itself.

## Stylistic Task Notes:
References to variables like X and Y automatically imply that these are to be exposed properties for developer tweaking.

## Skills To Know: for you and/or subagents:
	unityimporterplus
	unity-to-sbox-conversion
	sbox-scene-prefab-inspection
	sbox-offline-compile-check

## Post-Task Follow Ups
	Consider the same grouping for prefab conversion issues if a package ever produces one issue per prefab again; the editor log already groups them, the window report does not.