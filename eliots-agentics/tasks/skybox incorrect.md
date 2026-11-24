## Status: STARTED? NO! CLARIFIED? YES! HISTORY? YES! REVIEWED? NO! DONE? NO! WORKER: NONE OTHER: kickoff 2026-11-25; batch 02 answered; baseline c91e87b

## Task
Skybox incorrect

### Description
importer doesn't set correct skybox. Or the skybox in unity isn't working referenced or imported at all. But it appears to be the default.

While work is being done, the following oracle data is available:
Unity play screenshot to compare: /Testing/Unity-Screenshot-Outside.png.
Unity project we're importing: C:\cseliot\1-Development\4-Unity\2-Projects\old_school_model

### Work Review


## Notes

Master plan (2026-11-25): symptom task - "importer doesn't set correct skybox ... it appears to be the default". Q3 makes this agent-runnable: it must be judged from a rendered frame against Testing/Unity-Screenshot-Outside.png, not from serialized JSON. Phase 1 read-only diagnosis: what sky material / SkyColor / sky component the generated scene emits, what the Unity source actually authors (HDRP Volume sky, skybox material), and an engine-authored s&box artifact of the same __type for schema conformance. Phase 2 one implementer on Editor/Core with a HEAD baseline diff and harness gate. Phase 3 regenerate Assets/imported from fixed HEAD, run the full clean re-import loop, capture two editor play frames, compare them to the Unity screenshot, critic review.
Decisions: baseline c91e87b (Q1=1A; the generated scene now omits Sandbox.Fog). Q2: no standing HDRP abandonment ban; scope comes from this task file and the Director's fresh instruction. Q3: skybox IS discernible from a screenshot - close DONE? YES! only after a rendered frame comparison; no Open: line substitutes for that. Q4=4B: full clean re-import loop with two editor play frames IS authorized and required.

## Clarification Q&A

Kickoff batch 02 / 03 (Director, 2026-11-25, chat; full record in eliots-agentics/session.md):
An (Q1, verbatim): "Adopt as baseline: commit those 4 files by explicit path in ONE baseline commit so every task diff and critic review is against a clean HEAD. Consequence: fog removal, directional scale and wrapper-root guard become official HEAD." Landed c91e87b.
An (Q2, verbatim): "ignore the git diff"; clarified: "And also, what I meant by 2: Ignore what you learned from the git diff of session.md".
An (Q3, verbatim): "The skybox and tree color you should be able to discern from a screenshot. the others you'll have to marke done yes i guess because that's on me to finalize."
An (Q4, verbatim): "Yes: run the full clean re-import loop (launch editor, open scene, play, capture two play frames against Testing/Unity-Screenshot-Outside.png). Consequence: costs a full editor open/compile/play cycle per verification round."
This task is agent-runnable from a screenshot (Q3), not an Open: line task.

## Relevant History 

Reopens archived tasks 15 "Fog Doesn't Match" and 16 "Skybox Doesn't Match" (eliots-agentics/archive/). Task 16's critic (2026-11-24 22:23) returned "changes required": the sky gap is visibly present (s&box clear pale-blue daylight with thin cloud streaks vs Unity hazy deep navy/blue-grey twilight with a pale horizon band), the "SkyMaterial bare-string" premise was FALSIFIED, and closing on "HDRP abandoned" was rejected as an excuse. The old standing constraints behind that closure were deliberately removed from HEAD (2c17e22 "cleanup"), and the Director's Q2 answer says to ignore what was learned from the session.md diff, so no abandonment ban applies. The generated scene now omits Sandbox.Fog (baseline c91e87b). Prior evidence: C:/Users/Eliot/AppData/Local/hermes/profiles/hermes-sbox/cache/scratch/taskmaster-nov22/outdoor-compare/COMPARISON-2026-11-24.md and the archived differential report.

## Stylistic Task Notes:
References to variables like X and Y automatically imply that these are to be exposed properties for developer tweaking.
When programming, portability and DRY are a priority.

## Post-Task Follow Ups 
