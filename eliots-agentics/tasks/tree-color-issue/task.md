## Task
Imported trees are wrong color. 

### Description
Imported trees are greenish, unity trees are a kind of red-ish. Imported trees should match unity.


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

Reopens archived task 18 "Trees Wrong Colour" (eliots-agentics/archive/tasks/18 Trees Wrong Colour.md; critic "changes required" 2026-11-24 22:23). Its own vision read: s&box dark olive-green rounded crowns vs Unity deep burgundy/red-purple crowns; cause recorded OPEN; all tree-colour mechanisms UNTESTED; assets located; likely downstream of lighting/fog. A prior critic rejected closing a visibly present symptom from serialized JSON alone. The old standing constraints were removed from HEAD (2c17e22 "cleanup", the HEAD at task open; now bf099d4) and Q2 says to ignore what was learned from the session.md diff. Prior evidence: C:/Users/Eliot/AppData/Local/hermes/profiles/hermes-sbox/cache/scratch/taskmaster-nov22/outdoor-compare/COMPARISON-2026-11-24.md and the archived probes report.

## Stylistic Task Notes:
References to variables like X and Y automatically imply that these are to be exposed properties for developer tweaking.
When programming, portability and DRY are a priority.

## Post-Task Follow Ups 
