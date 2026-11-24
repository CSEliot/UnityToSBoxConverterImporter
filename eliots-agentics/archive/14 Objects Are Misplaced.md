## Status: STARTED? REJECTED! CLARIFIED? YES! HISTORY? YES! REVIEWED? YES! DONE? NO! WORKER: NONE OTHER: reopened by Director 2026-11-24; Restroom_Wall_01 (3) still misplaced/rotated

## Task
Too many objects are misplaced.

### Description
Director's verbatim symptom (2026-11-24): "Some objects are still not in their correct location and/or rotation (idk, mayube both? only one?), such as Restroom_Wall_01 (3). So find the task that said this was fixed from archive and bring it back."

Acceptance: in the imported MergeV2 / Test_Range scenes, objects sit where the Unity source puts them AND carry the source rotation. The symptom is finished when it is GONE, not when a spec is satisfied. Do not set DONE? YES! while it is reported.

Reopened from eliots-agentics/archive/14 Objects Are Misplaced.md (closed at b06f29c). That round fixed ONE class: 17 nested PrefabInstance root POSITION overrides were dropped because WrapperRootTransform() and the root test in Editor/Core/UnityPrefabExport.cs:1067,1566-1570 did not recognise a stripped Transform whose m_CorrespondingSourceObject equals the model root (400004/400008). Independent YAML census: 137 OK / 17 MISMATCH -> 154 OK / 0 MISMATCH. Six Director-named objects preserved. Critic 2026-11-23 14:32 "no further changes". The archive file holds the full census, gates and review.

What that round did NOT test (archive task 14 Description candidates b/d and Work Review finding 3): rotation/scale basis change (z,-x,y) at UnityPrefabExport.cs:223-228 applied to positions but possibly not to rotations/scales for some class; _mirror_<axis> variants flipping a position/rotation; and nested root overrides for the ROTATION/SCALE channels (only position was proven). No viewport screenshot was ever captured. These are UNTESTED, not falsified. The Director still reports a symptom, so the old close is invalid under common.md.

Current state (2026-11-24): HEAD ae9781a. Assets/imported is clean generated output from HEAD (2154 files); generated scenes at Assets/imported/school/scenes/mergev2.scene and test_range.scene. Unity source: C:/cseliot/1-Development/4-Unity/2-Projects/old_school_model/Assets/School/Scenes/MergeV2.unity. The live Dev editor holds the Director's UNSAVED in-memory scene work (SceneHasUnsavedChanges: true): do not save, play, reload or change that scene. Work headlessly.

No HDRP-specific work: on 2026-11-24 the Director abandoned HDRP support for this project. Do not chase HDRP sky/sun/light-unit/baked-GI semantics.

Required method:
1. Ground the named object: locate "Restroom_Wall_01 (3)" in the Unity source and in the generated scene. Derive its expected world POSITION and ROTATION independently from Unity YAML (basis change (z,-x,y), metres x 39.37), never from the importer's own formulas. Report both channels with numbers.
2. For EACH candidate mechanism write down the named observation that would FALSIFY it. Candidates: (b) rotation/scale basis not applied to some class; (d) mirror variant flipping a position or rotation; (a-recheck) nested root override still dropped for rotation/scale channels. No "investigated" verdict without a mechanism and its falsifier.
3. Differential ablation: find a working case from the same importer and enumerate what differs in the placement path.
4. If a mechanism survives, fix the ROOT CAUSE in the engine-free importer core (Editor/Core/*.cs), fix the CLASS. Do not special-case one object name. If all are falsified, say so with observations and name the next candidate.
5. Regression gates IF code changes land: harness build 0 errors / 0 warnings; building.unitypackage (24 files, no --scenes) and city--1.unitypackage (1166 files, no --scenes) byte-identical to a fresh HEAD-code baseline with BOTH trees populated and the same mode; verify_output.py <outRoot> <outRoot>/imported 0 problems; two identical runs byte-identical; BOM-less; no stray artifacts.
6. Put all evidence (comparison tables, scene excerpts, gate logs) in scratch files and cite absolute paths in ## Notes. Do not claim the visual symptom is fixed from serialized JSON alone.

Stage by explicit path only. No push until the Master verifies. Do not add unrequested tooling. Final answer in English.

### Work Review

2026-11-24 22:23 - Verdict: no further changes
Reviewer role: Review Critic (bounded adversarial re-review); judged the four uncommitted Editor/Core files plus the closing evidence, not a missing worker trail.

1. [Low] Editor/Core/UnityPrefabExport.cs:1592-1623 - re-read of the diff confirms the F4 broad regex is replaced by the strict ModificationPattern plus wrapper-root id and real m_Local* axis checks, and the sole main-loop edit is the authorised `!hasWrapperRootOverride &&`. The 67/67 independent placement audit and the six gates stand. The named object remains a perceptual gap: OUTDOOR-main-640.jpg shows the courtyard, but Restroom_Wall_01 (3) is not identifiable in it, so no fresh frame re-confirms the Director's symptom. Fix: no code change justified by this review; keep DONE? NO! until the Director observes that object. My own vision read of both outdoor frames does confirm the parent's composition claim (same elevated courtyard, building wings, plaza, tree row and long shadows), so the camera update reached the generated camera.

Tree (four reviewed source files at HEAD ae9781a):
- Editor/Core/UnityHdrpLighting.cs — `git hash-object --path="Editor/Core/UnityHdrpLighting.cs" "Editor/Core/UnityHdrpLighting.cs"` => 3de49ef03d589016281b43d044910382893f34ac
- Editor/Core/UnityPrefabExport.cs — `git hash-object --path="Editor/Core/UnityPrefabExport.cs" "Editor/Core/UnityPrefabExport.cs"` => 7c80ad48c817952ae79aa2935775e32616249049
- Editor/Core/UnitySceneExport.cs — `git hash-object --path="Editor/Core/UnitySceneExport.cs" "Editor/Core/UnitySceneExport.cs"` => 6bc6a265c16a18cc84b1e7677fbc45f14f1637ae
- Editor/Core/UnityVolume.cs — `git hash-object --path="Editor/Core/UnityVolume.cs" "Editor/Core/UnityVolume.cs"` => 420f4633aac52e30cb9547819171efe06f428786

## Notes

MASTER COMBINED GATES 2026-11-24 (headless, parent-run, fog + placement fixes):
harness build 0/0; old_school --scenes 2154 files, verify_output.py 875 files /
0 problems; grep 'Sandbox.Fog' across generated *.scene = 0 files; building
24/24 and city--1 1166/1166 byte-identical to the fresh HEAD baseline
(0 diff lines); two identical building runs 0-byte diff; BOM 0 files; no stray
repo artifacts. Diff between fog-only and fog+placement output = mergev2.scene
ONLY, but it contains ~45 changed values: (3) now Position -314.96,-787.4,
3.937015 / Rotation 0,0,-1,0 (exact independent expectation), (4) unchanged,
plus ~40 other Rotation/Position changes. Those class-wide effects are NOT yet
verified as correct: a read-only research child is deriving every changed
stub's expected transform independently from Unity YAML. Do NOT set DONE? YES!
or commit until that verdict lands and a fresh rendered frame is looked at.
Logs: scratch/taskmaster-nov22/combined-gate/out/.

PLACEMENT AUDIT VERDICT 2026-11-24 (read-only child, parent-verified):
the class-wide fix is CORRECT, no regressed objects. `diff --strip-trailing-cr`
fog-only vs fog+placement = 67 changed values (6 Position, 61 Rotation) across
65 stubs, all inside mergev2.scene; recursive tree diff = exactly one differing
file. Independent Unity-YAML derivation per stub gives 67 -> NEW, 0 -> OLD,
0 -> NEITHER (scratch placement-audit-table.txt, 67 verdict rows; parent
re-counted them). Spot-checks: Door_Frame root MergeV2.unity:159044-159112
p=(-1.9984,-7.3185e-06,-0.084811), q=(0,-1,0,0) with scale.z=-1 -> expected
(-3.339009,78.677008,-0.000288)/(0,0,1,0), matches combined :44283;
Chair_01 (1) root :291982-292012 p=(6.0368996,0.10200024,-3.3140001),
q=(0,0.7071068,0,0.7071068) -> (-130.47218,-237.67274,4.015749)/
(0,0,-0.7071068,0.7071068), matches combined :82457. Named falsifier: any
changed stub whose fog-only value equals the root-derived value while the new
value differs; none. All 154 nested prefabs are byte-identical between the two
outputs, so the archive's 154 OK / 0 MISMATCH census is preserved. The child hit
its 40-call cap before writing a final report, but its artifacts are on disk and
the tally was re-counted by the parent. Task still needs a fresh rendered frame
before DONE? YES!.

REVIEW DISPOSITIONS 2026-11-24 (adversarial review deleg_a58a8db3):
F4 ACCEPTED - the hasWrapperRootOverride regex at UnityPrefabExport.cs:1592-1593
matches ANY `- target:` line, not just m_Local* transform overrides, so a
wrapper-root m_Name/m_IsActive override can suppress a legitimate absent-id root
transform override. Tightening fix dispatched (deleg_ac3c8ee8). The review does
NOT contradict the 67/67 placement audit. Full per-finding answers:
scratch/taskmaster-nov22/REVIEW-DISPOSITIONS-AND-FIX-SPEC.md. No commit or
header change until the fix lands and the six gates are re-run.

## Clarification Q&A

ANSWERED (Director, 2026-11-24): "1. I AM ABANDONING HDRP SUPPORT FOR THIS PROJECT. So if it's HDRP specific, leave it alone. That being said ... 3. Some objects are still not in their correct location and/or rotation (idk, mayube both? only one?), such as Restroom_Wall_01 (3). So find the task that said this was fixed from archive and bring it back." No questions pending; the /goal loop runs unattended. Do not ask the Director.

## Relevant History 

See eliots-agentics/archive/14 Objects Are Misplaced.md for the full 2026-11-23 round (b06f29c), its 17->0 census, gates and critic review. That round's "no further changes" verdict is NOT acceptance: the Director still reports the symptom.

## Stylistic Task Notes:
References to variables like X and Y automatically imply that these are to be exposed properties for developer tweaking.
When programming, portability and DRY are a priority.

## Post-Task Follow Ups 
