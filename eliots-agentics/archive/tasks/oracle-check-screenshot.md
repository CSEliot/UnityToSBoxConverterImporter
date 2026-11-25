## Status: STARTED? REJECTED! CLARIFIED? NO! HISTORY? NO! REVIEWED? YES! DONE? NO! WORKER: MASTER OTHER: clean-output perceptual gate run; symptoms still present

## Task
Make the screenshot taken after launching play to the MergeV2 scene in S&Box Editor, look like the screenshot from the Unity equivalent.

### Description
Unity screenshot to compare: /Testing/Unity-Screenshot-Outside.png.
Unity project we're importing: C:\cseliot\1-Development\4-Unity\2-Projects\old_school_model

When comparing screenshots and considering what needs to happen next:
 - You are allowed to re-open closed tasks (i.e. set DONE? NO! and REVIEWED? NO!) if you believe the problem in the comparison lies within that task.
 - You are allowed to make new tasks (copying .template-task.md) if you believe the problem in the comparison requires a new task.
 - BIG NOTE: Unity HDRP projects are known problematic, and the old-school-project is an HDRP project. So, goal is "BEST EFFORT", not "PERFECT OR DIE."

### Work Review

2026-11-24 22:23 - Verdict: changes required
Reviewer role: Review Critic (bounded adversarial re-review); judged the four uncommitted Editor/Core files plus the closing evidence, not a missing worker trail.

1. [High] eliots-agentics/tasks/oracle-check-screenshot.md:4 - the task goal ("look like the screenshot from the Unity equivalent") is not met. I looked at both frames: composition does match (same elevated courtyard, L-shaped building, plaza, tree row, long shadows), but OUTDOOR-main-640.jpg is bright daylight with a clear blue sky and dark olive-green trees, while Testing/Unity-Screenshot-Outside.png is hazy navy dusk with amber windows and burgundy trees; mean brightness 100.16 vs 48.54. The BIG NOTE asks for BEST EFFORT on HDRP problems, not blanket acceptance of a visibly different render, and no Director acceptance of these three gaps is on disk. Fix: present the frame pair and the three gaps to the Director; keep DONE? NO!.
2. [Medium] COMPARISON-2026-11-24.md:75-79 - the sky gap is excused as HDRP-side, but the Director explicitly said the skybox is one of the tasks definitely NOT HDRP related. That part of the closure rationale is unsupported. Fix: strike the HDRP excuse for gap 2 or obtain a Director ruling on it.
3. [Low] COMPARISON-2026-11-24.md:81-101 - the parent's "tree data carried faithfully" claim survived my falsification attempt (vertex colours exist in Tree_01 FBX but are RGB 0 with alpha ~1; Tree_Brick materials and both albedo TGAs are identical source/generated), so no importer data defect was found. Fix: keep the gap disclosed as Unity-side/open, not claimed resolved.

Tree (four reviewed source files at HEAD ae9781a):
- Editor/Core/UnityHdrpLighting.cs — `git hash-object --path="Editor/Core/UnityHdrpLighting.cs" "Editor/Core/UnityHdrpLighting.cs"` => 3de49ef03d589016281b43d044910382893f34ac
- Editor/Core/UnityPrefabExport.cs — `git hash-object --path="Editor/Core/UnityPrefabExport.cs" "Editor/Core/UnityPrefabExport.cs"` => 7c80ad48c817952ae79aa2935775e32616249049
- Editor/Core/UnitySceneExport.cs — `git hash-object --path="Editor/Core/UnitySceneExport.cs" "Editor/Core/UnitySceneExport.cs"` => 6bc6a265c16a18cc84b1e7677fbc45f14f1637ae
- Editor/Core/UnityVolume.cs — `git hash-object --path="Editor/Core/UnityVolume.cs" "Editor/Core/UnityVolume.cs"` => 420f4633aac52e30cb9547819171efe06f428786

## Notes

MASTER CLEAN-OUTPUT PERCEPTUAL GATE 2026-11-24: regenerated Assets/imported from
HEAD ae9781a, opened the Dev editor, launched play on mergev2 and captured a
frame from the main camera. The screenshot does NOT look like the Unity
reference: Unity mean 46.09 / 730872 coloured px / 391051 warm px; clean s&box
mean 55.46 / 152052 coloured px / 91 warm px. The grey fog wall is gone but the
frame is dim, cool, desaturated and has no golden sunlight. This task stays
DONE? NO!. Full numbers, paths, the run falsifier and remaining mechanisms:
<scratch>/taskmaster-nov22/visual/CLEAN-PERCEPTUAL-GATE.md.


MASTER OUTDOOR COMPARISON GATE 2026-11-24 22:09 (new oracle, camera updated by
Director): captured two play-mode frames from the main camera of the freshly
re-imported mergev2.scene and compared to Testing/Unity-Screenshot-Outside.png.
Composition now MATCHES (same elevated courtyard, L-shaped building, plaza, tree
row, edging, long shadows) - the camera update reached the generated main camera.
Remaining gaps: brightness 100.16 vs 48.54 (s&box bright daylight vs Unity dusk),
sky (clear blue vs hazy navy twilight), and TREE COLOUR (s&box dark olive-green
vs Unity burgundy/maroon). Full numbers, vision reads, paths and the three gaps:
<scratch>/taskmaster-nov22/outdoor-compare/COMPARISON-2026-11-24.md.
Task 18's symptom is now judgeable and appears PRESENT; it is being diagnosed
before any header close. Gaps 1-2 are treated as HDRP-side / BEST EFFORT per the
BIG NOTE. The stale indoor note above is retained as history, not as the current
oracle.


DIRECTOR DECISION 2026-11-24 (recorded by Task Master): "Fix trees and skybox
100% (i'm confident those are fixable) and then fix lighting to "best ability"."
Work order: trees -> skybox -> lighting. BEST EFFORT now applies to LIGHTING ONLY,
not trees or skybox. The outdoor comparison at 22:09 is the current gate:
composition matches, three gaps (brightness 100.16 vs 48.54, clear blue sky vs
hazy navy dusk, olive-green trees vs burgundy). This real Director turn is the
fresh decision that unblocks re-tuning directional intensity. Any code change
resets REVIEWED? to NO! and needs a fresh critic "no further changes" before
DONE? YES!. Evidence:
<scratch>/taskmaster-nov22/outdoor-compare/COMPARISON-2026-11-24.md.
Research child sa-0-1b8f9add is pinning the source-authored mechanism; no code
changed yet.

## Clarification Q&A


## Relevant History 


## Stylistic Task Notes:
References to variables like X and Y automatically imply that these are to be exposed properties for developer tweaking.
When programming, portability and DRY are a priority.

## Post-Task Follow Ups 
