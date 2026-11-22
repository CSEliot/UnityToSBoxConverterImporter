## Status: STARTED? YES! CLARIFIED? YES! HISTORY? YES! REVIEWED? YES! DONE? YES! WORKER: sa-0-2df4d484 OTHER: backlog; reconciled with the unity backlog; owns the merged SceneConversions schema

## Task
	Report lost scene content as an import error and group scene skips by cause, including the silent child-loop drop.

### Description
	A lost prefab instance does not currently become an error, and often is not even counted. The null-root branch at Editor/Core/UnitySceneExport.cs:68 does `skipped++`, but the old_school scenes report Roots 4 and 3 with Instances 0 and Skipped 0, so it never fires: all 2,861 MergeV2 and 201 Test_Range instances are children of plain group objects. They are dropped silently in UnityPrefabExport.Node's child loop at Editor/Core/UnityPrefabExport.cs:961, where `if ( Instance( instance, chain, owed ) is JsonObject nested ) children.Add( nested );` omits a null without counting it. The `Owner( ... ) is not var (owner, source, g)` check at UnitySceneExport.cs:127 is a working null check on a nullable tuple, not a dead pattern, and its `skipped++` is live; it simply never runs for these scenes because Nest() is only reached for objects attached under a prefab instance (UnitySceneExport.cs:144-165), while these instances hang off plain group transforms. :134's `if ( data == null ) { skipped++; return; }` is a second live guard. Neither is the loss path.

	The baseline therefore shows success while the content is gone: SceneConversions MergeV2 4 roots / 0 instances / Skipped 0, Test_Range 3 / 0 / 0, and 459 warning lines (255 MergeV2 + 204 Test_Range, 455 distinct after deduplication; not all of them are instance skips) for 3,062 lost objects. Most skip warnings are cached per source prefab guid, not per lost instance, so the count understates the loss. Conversion-skipped issues are already Error severity (Editor/Core/UnityImport.cs:22), but these losses never reach that path. The prefab error also mislabels every model wrapper as a prefab variant, so the cause cannot be read from the message.

	Task state (2026-11-21): tasks 01-04 are landed (0d1cd94, fa149ef, 24a5509, 1cecb21). The old_school scenes now convert: MergeV2 2,861 / 0 skipped, Test_Range 201 / 0, and the 2,110-file output is byte-identical to HEAD 1cecb21 (the pre-task-01 baseline was 2,109; task 04 adds the Test_Range Plane's generated primitive model). The 3,062 lost instances and 459 warnings above are the pre-01 historical baseline; do not recreate them. The remaining work is to count and report genuinely unresolvable sources, including the child-loop drop, proven with a constructed fixture.

	Required work:
	1. Count every PrefabInstance whose source cannot be resolved, including instances dropped inside the child loop at UnityPrefabExport.cs:961. Thread a skip collector or return a per-call drop count from Node/Instance so the scene conversion can total it. Do not count the stripped transforms that belong to those same instances as separate losses.
	2. Expose TotalInstances and LostInstances beside Roots/Instances/Skipped in SceneConversions, and add SkipCauses (cause to count, with up to MaxSampleNames sample names, a named constant of 10). This task owns both schema additions: the parallel unity backlog proposed TotalInstances/LostInstances and the sbox backlog proposed per-cause counts, and they are consolidated here rather than defined twice. Keep older exported reports readable and add fields rather than renaming.
	3. Raise one unresolved Error with code conversion-skipped per scene when content is lost, grouped by reason with a count and sample names. Keep warnings for cosmetic losses that remove no object. Do not emit thousands of duplicate lines.
	4. Do not change the guards at UnitySceneExport.cs:127 or :134; both are live checks. Add the counting at the child loop and the Instance boundary where the null is currently discarded.
	5. In UnityPrefabExport.Convert, distinguish a model-wrapper root from a true variant: report "model prefab instance" when the source is a Model, keep "prefab variant" only when the source is a Prefab, and say "missing prefab reference" when the GUID is absent.
	6. Do not change generated scene or prefab JSON for successful conversions.

	Non-goals: converting the prefabs (task 01), material binding (task 02), direct model instances (task 03), built-in primitives (task 04).

	Acceptance:
	- On the current tree (tasks 01-04 landed), old_school --scenes reports MergeV2 2,861 / 0 and Test_Range 201 / 0 lost, its 2,110-file output (2,109 before task 04's Test_Range Plane) stays byte-identical to HEAD, and no new error appears. The pre-01 3,062-loss / 459-warning baseline is historical and must stay fixed.
	- A constructed fixture with a scene PrefabInstance pointing at a missing guid, plus a nested instance whose source is missing, reports each scene's total and lost counts, groups the causes with sample guids, and raises one conversion-skipped Error per scene. Use at least three missing instances so the count is more than a boolean.
	- Packages with complete scenes show no new warnings or errors, and older exported reports without the new fields stay readable.
	- Record the before and after warning and error counts in the task file; do not assume unrelated model or material warnings disappear.
	- Editor/Core builds with 0 warnings, 0 errors.

## Notes

	Implementation home: Editor/Core/UnitySceneExport.cs (SceneResult/Statistics at 37, root loop at 68, ParentWorld guard at 116, Nest guard at 127, live data guard at 134), Editor/Core/UnityPrefabExport.cs (child loop at 961, Convert at 1166-1173), Editor/Core/UnityImport.cs (scene loop 431-447, report object at 482, Error severity at 22). Tasks 01-04 landed and shifted these lines; locate the current child loop and scene report before editing. The current old_school baseline is 0 lost instances; prove the loss path with a fixture.

	Baseline evidence (historical, pre-task-01): 459 = 255 + 204 warnings; 455 distinct after `warnings.Distinct()`; 3,062 = 2,861 + 201 lost instances; SceneConversions 0 instances received. These come from the unity expert's saved baseline report and no longer reproduce on the current tree; use them only to understand the original bug.

	The unity set's 04 and this task described the same bug with different field names. This task is now the single owner: TotalInstances, LostInstances and SkipCauses all live in SceneConversions, and the prefab/wrapper message fix belongs with the error path here.

	Report (2026-11-21, harness-verified; nothing here is editor-verified):
	- Editor/Core/UnityPrefabExport.cs: SkipCollector (nested, after Result) holds cause -> count with up to
	  MaxSampleNames = 10 sample names and dedupes by PrefabInstance fileID, so the child loop and Instance can both
	  see a drop without doubling it. SkipCause names the six reasons: missing source, source not converted, source
	  without a root, missing mirrored variant, instance missing from the file, instance not reached. Source gained
	  ProblemCause beside Problem, set at all four SourceOf failure sites (message text unchanged). Node and Instance
	  take an optional SkipCollector: with one, a dropped instance is counted with a "Name [guid]" sample; without one
	  (prefab conversion) the old per-instance warning stays. Node's child loop now also counts a stripped object that
	  names a PrefabInstance the file does not hold. InstanceName falls back to the instance's own m_Name override, so
	  a lost instance is named in the report instead of by fileID. New Converter.WrapperProblem replaces the single
	  "prefab variants are not supported" text: "missing prefab reference" (guid absent or not in the package),
	  "prefab variant (path)" only when the source is a Prefab, and "model prefab instance"/"<kind> instance" wording
	  otherwise; Convert throws it when WrapperModel is null and WrapperRoot uses it as its own guard.
	- Editor/Core/UnitySceneExport.cs: Statistics gained TotalInstances, LostInstances and Skips; one collector per
	  scene threaded through the root loop, Nest and both Node/Instance call sites. Step 5b counts every PrefabInstance
	  without a stub that no earlier site recorded (an instance whose parent was lost is never walked to). Stripped
	  transforms are not losses of their own. TotalInstances is the PrefabInstance blocks plus any stripped object
	  naming an instance the file lost, so Total and Lost partition the placements (city--1: 5,326 / 0).
	- Editor/Core/UnityImport.cs: SceneConversions entries add TotalInstances, LostInstances and SkipCauses
	  (cause -> { Count, Samples }); the older fields are unchanged, so an older report still reads. One
	  conversion-skipped Error per scene when LostInstances > 0, grouped by cause, e.g. "Scene content is missing:
	  5 of 6 prefab instance(s) could not be converted. prefab source is not in the package (4: Missing A
	  [1111...], ...); prefab source was not converted (1: Variant Instance [bbbb...])." No per-instance lines.
	- Counts, HEAD 1cecb21 worktree vs this tree, same packages, full harness runs:
	  old_school 1,277 assets / 936 converted / 2,110 files and 97 warnings + 81 unresolved warnings (0 Errors) both;
	  MergeV2 Roots 4, Instances 2,861, TotalInstances 2,861, Lost 0, SkipCauses {}; Test_Range 3 / 201 / 201 / 0 / {}.
	  building 16 assets / 26 files, 0 warnings, 0 unresolved, Total 0 / Lost 0.
	  city--1 730 assets / 1,192 files, 4 warnings (3 distinct unresolved warnings) both, Demo.unity Roots 4,329,
	  Instances 5,326, Total 5,326, Lost 0. All three output trees are byte-identical to the HEAD run
	  (`diff -r --strip-trailing-cr`, 2,110 / 26 / 1,192 files). The task's "2,109 files" is one short: HEAD itself
	  writes 2,110.
	- Fixture Documentation/Unity-To-SBox-Automated/fixtures/scene-loss + scene-loss.unitypackage (5 assets, tar md5
	  70aaf993c94992bdbadef810742f090d): Lossy.unity 6 instances / 5 lost, two cause groups with guid samples
	  (missing source 4, source not converted 1); Nested.unity 2 / 2 (not reached 1 - a valid prefab under a lost
	  instance - and missing source 1); Complete.unity 2 / 0 with SkipCauses {} and no error. One conversion-skipped
	  Error per lossy scene and none for Complete; 7 output files, verify_output.py "checked 4 files, 0 problems",
	  two runs byte-identical. The nested missing instance (Missing Nested under the kept instance) is counted at the
	  Instance boundary through Nest, and Variant.prefab reports the split wording ("prefab variant (path)").
	- Editor/Core builds with 0 warnings, 0 errors (harness csproj). Not claimed: how the lossy scenes look in the
	  s&box editor; headless cannot judge that.

## Clarification Q&A


## Relevant History 

	Source: tasks/sbox-expert-research.md, reconciled 2026-11-20 with tasks/unity-expert-research.md. The earlier sbox version said top-level skips were silent at :68 and that the report had no cause count; that was incomplete. The unity expert established that :68 never fires for these scenes and the drop happens in the child loop at UnityPrefabExport.cs:961; a compiled check of the nullable tuple showed the `is not var` guard at :127 is a live null check that is simply never reached for these instances, and :134 is live. The count belongs at the child-loop drop. This task now owns the merged severity, counting and schema work.

## Stylistic Task Notes:
References to variables like X and Y automatically imply that these are to be exposed properties for developer tweaking.

## Skills To Know: for you and/or subagents:
	unityimporterplus
	unity-to-sbox-conversion
	sbox-scene-prefab-inspection
	sbox-offline-compile-check

## Post-Task Follow Ups
	After the loss count is reliable, consider the same grouped reporting for prefab conversion issues if a package ever produces one issue per prefab again; the editor log already groups them, the window report does not.
	From the review's low findings (2026-11-21, none blocking, not changed in this commit): (1) SkipCollector.Loss with
	instanceId 0 neither dedupes nor records into the lost set, so LostInstances could read 0 while SkipCauses counts -
	unreachable from Instance (every caller passes a real PrefabInstance id) but the invariant deserves a comment or a
	sentinel count; (2) this file's Report says "all four SourceOf failure sites" where there are three Problem
	assignments; (3) Documentation/Unity-To-SBox-Automated.md still documents only the old SceneConversions columns and
	lists only the prefab-free-scene fixture, so TotalInstances/LostInstances/SkipCauses and fixtures/scene-loss need a
	sentence there.

### Work Review
2026-11-21 04:03 - Verdict: no further changes

Tree:
f5f504160c4907ad502bc9ca2913148c3aebb47f  Editor/Core/UnityPrefabExport.cs
856c683b71ec9c20c83c6cfd304ecb83a614c2e6  Editor/Core/UnitySceneExport.cs
eed9d8bb496a95ca86c16b06b5121495302f895e  Editor/Core/UnityImport.cs

Verified by real execution (harness rebuilt from this tree, 0 warnings / 0 errors):
- Fixture re-run: 5 assets / 4 converted / 7 files / 3 Errors. Complete 2 total / 0 lost / {}; Lossy
  6 / 5 (missing source 4, not converted 1); Nested 2 / 2 (not reached 1, missing source 1). Output
  byte-identical to the worker's run; verify_output.py "checked 4 files, 0 problems"; tar md5 70aaf993...
  matches; guid samples aaaa/bbbb = Prop/Variant, cccc-eeee = the scenes.
- old_school/building/city--1 after vs HEAD json: old SceneConversions fields, Unresolved, Warnings and
  Information identical; old_school 2,110 files / 97 warnings / 81 unresolved / 0 Errors, MergeV2 2,861 / 0,
  Test_Range 201 / 0; building 26 / 0 / 0; city--1 1,192 files, Demo 4,329 roots / 5,326 inst / 5,326 total
  / 0 lost. diff -r --strip-trailing-cr on all four trees identical.
- Two-run determinism: fixture outputs and SceneConversions identical; only Destination differs.
- No post-pass false positives: real packages report Lost 0 (no Unreached), the fixture's genuinely
  unreached instance is counted.
- Code: dedupe uses a lost HashSet with Count => lost.Count (UnityPrefabExport.cs:651-657); Instance returns
  null only at :1498 and :1605 and both record a loss; the child loop adds MissingInstance only when the
  PrefabInstance block is absent (:1464-1473); step 5b guards on Stubs + skips.Has (UnitySceneExport.cs:244-249);
  Stubs is written only on success (:1692); Total = Instances U StrippedInstance values, Lost = skips.Count;
  one grouped Error per lossy scene (UnityImport.cs:501-502), severity Error at :22, no per-instance lines;
  older fields untouched (add-only).
- Classifier: WrapperProblem (UnityPrefabExport.cs:1249-1258) gives missing reference / prefab variant (path)
  only for Kind == Prefab / <kind> instance; ProblemCause set at the three SourceOf failure sites
  (:987, :993, :1033), mirror branch falls back to MissingVariant (:1605).
- The 2,110-vs-2,109 note is correct: HEAD itself writes 2,110; 2,109 is the pre-task-04 figure. The editor
  appearance of the lossy scenes is explicitly unclaimed, not fabricated.

Low findings, none blocking:
1. UnityPrefabExport.cs:651-653: a loss with instanceId == 0 is neither deduped nor added to lost, so
   LostInstances could stay 0 while SkipCauses shows counts. Unreachable in valid scenes; state the invariant
   or count it under a sentinel.
2. Task Notes say "all four SourceOf failure sites"; there are three Problem assignments (:988, :994, :1034).
3. Documentation/Unity-To-SBox-Automated.md:170-174 still describes only the old SceneConversions columns and
   names only the prefab-free-scene fixture; one sentence for the new fields and the scene-loss fixture would
   make them discoverable.