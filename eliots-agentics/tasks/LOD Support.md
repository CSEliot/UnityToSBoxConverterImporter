## Status: STARTED? NO! CLARIFIED? YES! HISTORY? YES! REVIEWED? NO! DONE? NO! WORKER: NONE OTHER: kickoff 2026-11-25; batch 02 answered; baseline c91e87b

## Task
We got z-fighting which I believe is due to no implementation of LODs?

### Description
Examples:
In SBOX Editor: Floor_stairs_middle (1) at:
{
  "Position": "1255.90295,1413.38306,157.479996",
  "Scale": "1,1,1",
  "Rotation": "0,0,-1,0"
}
has all the LODs from unity just thrown together as components. That's not how sbox handles LoDs.

If you need an "oracle" for correctly writing LODs in S&Box say so. But try first.


While work is being done, the following oracle data is available:
Unity play screenshot to compare: /Testing/Unity-Screenshot-Outside.png.
Unity project we're importing: C:\cseliot\1-Development\4-Unity\2-Projects\old_school_model

### Work Review


## Notes

Master plan (2026-11-25): symptom task - "z-fighting ... no implementation of LODs", Floor_stairs_middle (1) has all Unity LODs thrown together as components. Phase 1 read-only diagnosis: how Editor/Core currently emits LOD meshes into generated .prefab/.scene, and what s&box's authored LOD contract is (compare a generated component with an engine-authored artifact of the same __type). Phase 2 one implementer on the LOD emission path with a HEAD baseline diff and the harness gate. Phase 3 regenerate Assets/imported from fixed HEAD, verify_output, editor play frame, critic review. The task says "If you need an 'oracle' for correctly writing LODs in S&Box say so. But try first": no oracle is requested yet; the worker must try first and ask only if genuinely blocked.
Decisions: baseline c91e87b (Q1=1A). Q2: no standing HDRP abandonment ban; scope comes from this task file. Q3: this task may close DONE? YES! with a self-contained Open: line for the Director's own final judgement; objective data defects must still be fixed headlessly. Q4=4B: full clean re-import loop with two editor play frames IS authorized and required. Oracle: Testing/Unity-Screenshot-Outside.png and Unity project C:/cseliot/1-Development/4-Unity/2-Projects/old_school_model.

## Clarification Q&A

Kickoff batch 02 / 03 (Director, 2026-11-25, chat; full record in eliots-agentics/session.md):
An (Q1, verbatim): "Adopt as baseline: commit those 4 files by explicit path in ONE baseline commit so every task diff and critic review is against a clean HEAD. Consequence: fog removal, directional scale and wrapper-root guard become official HEAD." Landed c91e87b.
An (Q2, verbatim): "ignore the git diff"; clarified: "And also, what I meant by 2: Ignore what you learned from the git diff of session.md".
An (Q3, verbatim): "The skybox and tree color you should be able to discern from a screenshot. the others you'll have to marke done yes i guess because that's on me to finalize."
An (Q4, verbatim): "Yes: run the full clean re-import loop (launch editor, open scene, play, capture two play frames against Testing/Unity-Screenshot-Outside.png). Consequence: costs a full editor open/compile/play cycle per verification round."
This task is one of "the others": closes with an Open: line (Q3).

## Relevant History 

Reopens the LOD side of archived task 12 "LOD Split-Model Material Slots" (eliots-agentics/archive/12 LOD Split-Model Material Slots.md; DONE? YES!, verdict "no further changes" 2026-11-22 22:09): split-model embedded material slots were fixed, but s&box LODs were NOT implemented. New symptom (2026-11-25): z-fighting from all Unity LODs emitted together as components on Floor_stairs_middle (1). Converter context in the unityimporterplus skill: fixed model-prefab IDs 2300000+2n / 4300000+2n, per-mesh RenderMeshFile nodes when bodygroups exist, UnityPrefabBindings silently skips prefabs over 16 MiB, and the split-model material-slot fix in archived 12. Prior visual evidence: C:/Users/Eliot/AppData/Local/hermes/profiles/hermes-sbox/cache/scratch/taskmaster-nov22/outdoor-compare/COMPARISON-2026-11-24.md. Baseline c91e87b; HEAD 2c17e22 removed the old standing constraints.

## Stylistic Task Notes:
References to variables like X and Y automatically imply that these are to be exposed properties for developer tweaking.
When programming, portability and DRY are a priority.

## Post-Task Follow Ups 
