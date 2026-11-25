## Task
Skybox incorrect

### Description
importer doesn't set correct skybox. Or the skybox in unity isn't working referenced or imported at all. But it appears to be the default.

While work is being done, the following oracle data is available:
Unity play screenshots to compare:
GameObject Name: Japanese School Camera (1), Screenshot Location: /Testing/Unity-Screenshot-Outside-Facing-School-Camera-1.png.
GameObject Name: Japanese School Camera (2), Screenshot Location: /Testing/Unity-Screenshot-Outside-Facing-Sun-Camera-2.png.
GameObject Name: Japanese School Camera (3), Screenshot Location: /Testing/Unity-Screenshot-Inside-Facing-Hallway-Camera-3.png.
GameObject Name: Japanese School Camera (4), Screenshot Location: /Testing/Unity-Screenshot-Outside-Facing-Windows-Camera-4.png.
These screenshots were taken from 4 cameras in the scene, enable one each after importing into sbox to get validation from in-game sbox screenshots.
Unity project we're importing: C:\cseliot\1-Development\4-Unity\2-Projects\old_school_model

### Work Review


## Clarification Q&A

Kickoff batch 02 / 03 (Director, 2026-11-25, chat; full record in eliots-agentics/archive/sessions/):
An (Q1, verbatim): "Adopt as baseline: commit those 4 files by explicit path in ONE baseline commit so every task diff and critic review is against a clean HEAD. Consequence: fog removal, directional scale and wrapper-root guard become official HEAD." Landed c91e87b.
An (Q2, verbatim): "ignore the git diff"; clarified: "And also, what I meant by 2: Ignore what you learned from the git diff of session.md".
An (Q3, verbatim): "The skybox and tree color you should be able to discern from a screenshot. the others you'll have to marke done yes i guess because that's on me to finalize."
An (Q4, verbatim): "Yes: run the full clean re-import loop (launch editor, open scene, play, capture two play frames against Testing/Unity-Screenshot-Outside.png). Consequence: costs a full editor open/compile/play cycle per verification round."
This task is agent-runnable from a screenshot (Q3), not an Open: line task.

## Relevant History 

Reopens archived tasks 15 "Fog Doesn't Match" and 16 "Skybox Doesn't Match" (eliots-agentics/archive/tasks/). Task 16's critic (2026-11-24 22:23) returned "changes required": the sky gap is visibly present (s&box clear pale-blue daylight with thin cloud streaks vs Unity hazy deep navy/blue-grey twilight with a pale horizon band), the "SkyMaterial bare-string" premise was FALSIFIED, and closing on "HDRP abandoned" was rejected as an excuse. The old standing constraints behind that closure were deliberately removed from HEAD (2c17e22 "cleanup"), and the Director's Q2 answer says to ignore what was learned from the session.md diff, so no abandonment ban applies. The generated scene now omits Sandbox.Fog (baseline c91e87b). Prior evidence: C:/Users/Eliot/AppData/Local/hermes/profiles/hermes-sbox/cache/scratch/taskmaster-nov22/outdoor-compare/COMPARISON-2026-11-24.md and the archived differential report.

## Stylistic Task Notes:
References to variables like X and Y automatically imply that these are to be exposed properties for developer tweaking.
When programming, portability and DRY are a priority.

## Post-Task Follow Ups 
