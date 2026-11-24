# Session

## Notes

Standing constraints (Director 2026-11-25, recorded by Task Master; copy into every child brief):
- Baseline HEAD is c91e87b (the 2026-11-24 importer WIP adopted by ONE explicit-path baseline commit, Q1=1A). Pushed to origin/main.
- Parent decisions (change if wrong): the 2026-11-24 standing constraints that used to live here (HDRP abandonment, "no fog", work order trees -> skybox -> lighting, BEST EFFORT, Restroom_Wall_01 note) were deliberately removed by the Director (HEAD 2c17e22 "cleanup") and are NOT in force. Q2 answer "ignore the git diff" / "Ignore what you learned from the git diff of session.md" means no ban is invented from them. Scope is set by the four live task files and the Director's fresh instruction only.
- Q4=4B: the full clean re-import loop is AUTHORIZED and REQUIRED (launch editor, open scene, play, capture two play frames against Testing/Unity-Screenshot-Outside.png). Not out-of-scope extra testing.
- Q3: skybox and tree colour are agent-runnable from a rendered frame compared to the Unity screenshot; LOD/z-fighting and objects-misplaced may close DONE? YES! with a self-contained `Open:` line for the Director's own final judgement. Objective data defects and unchanged reported symptoms never close with an Open: line.
- Oracle: Testing/Unity-Screenshot-Outside.png; Unity source C:/cseliot/1-Development/4-Unity/2-Projects/old_school_model (HDRP GraphicsSettings).
- Session-start harness gate on the adopted baseline: 0 warnings / 0 errors, log C:/Users/Eliot/AppData/Local/hermes/profiles/hermes-sbox/cache/scratch/taskmaster-nov25/session-start-harness-build.log.

## Clarification Q&A

### Kickoff batch 01 / 02 / 03 (Director, 2026-11-25, chat)
ASK_SINCE=1795560225 (batch 02). Batch 01 text: C:/Users/Eliot/AppData/Local/hermes/profiles/hermes-sbox/cache/scratch/taskmaster-nov25/batch-01.txt; its chat clarify timed out with all four unanswered at 01:25, once-only Signal + ntfy relay fired 01:28:44 / 01:28:50. Batch 02 text: .../taskmaster-nov25/batch-02.txt; partly answered (Q1/Q2 answered, Q3/Q4 timed out), once-only Signal + ntfy relay fired 2026-11-25 (SEND_EXIT=0, ntfy True HTTP 200), relay text .../taskmaster-nov25/relay-batch-02.txt. Q3/Q4 re-asked in chat as batch 03 and answered (outcome submitted).

Q1 (tree baseline): four Editor/Core files modified 2026-11-24 05:24-05:47, absent from HEAD: UnitySceneExport.cs (omits Sandbox.Fog), UnityHdrpLighting.cs (directional lights bypass /1000 candela scale, clamp 8), UnityPrefabExport.cs (wrapper-root override guard), UnityVolume.cs (fog doc comment).
An (Q1, verbatim, chat): "Adopt as baseline: commit those 4 files by explicit path in ONE baseline commit so every task diff and critic review is against a clean HEAD. Consequence: fog removal, directional scale and wrapper-root guard become official HEAD." (= 1A). Landed c91e87b; origin/main == c91e87b.

Q2 (HDRP scope): the 2026-11-24 standing constraints learned from the git diff of session.md.
An (Q2, verbatim, chat): "ignore the git diff". Clarified in a later real user turn: "And also, what I meant by 2: Ignore what you learned from the git diff of session.md".
Parent decision: those diff-derived constraints are not carried into this loop; HEAD 2c17e22 committed their deletion, so none of them are on disk. No HDRP abandonment ban is invented.

Q3 (closing rule, batch 03):
An (Q3, verbatim, chat): "The skybox and tree color you should be able to discern from a screenshot. the others you'll have to marke done yes i guess because that's on me to finalize."

Q4 (extras, batch 03):
An (Q4, verbatim, chat): "Yes: run the full clean re-import loop (launch editor, open scene, play, capture two play frames against Testing/Unity-Screenshot-Outside.png). Consequence: costs a full editor open/compile/play cycle per verification round." (= 4B)

