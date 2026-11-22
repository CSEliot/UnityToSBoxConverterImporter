# common.md

A subagent runs exactly one turn. Nothing survives it except files on disk and its transcript.

The task file is the only shared memory. Read it at the start of every turn. The Director's answers and the critic's review arrive there between turns.

## Task file

The header is shared state. Any role may update it when the state changes. Rewrite it as one complete line; do not drop fields.

- `STARTED?` work has begun.
- `CLARIFIED?` the Director's answers are recorded.
- `HISTORY?` the context summary is written.
- `REVIEWED?` a critic reviewed the current tree. A change to the reviewed files after the review sets `REVIEWED? NO!`. The critic's entry and flag, and the worker's reply and `DONE?` write, are not changes to the reviewed files.
- `DONE?` the work is finished and the commit was attempted. A failed commit or a change to the reviewed files after the commit sets `DONE? NO!` and `REVIEWED? NO!` until re-reviewed and re-committed.
- `WORKER:` the current subagent id, or `MASTER` when the Master did the work.
- `OTHER:` free-form; use it sparingly.

`{...}` is a value you substitute; never leave one in a file you write. Replace `{subagent_id}` and `{state-here}` in the copied header.

The canonical field order is line 1 of `.template-task.md`. A header missing any field is malformed: restore all seven fields from the template line, then re-derive from the file: a non-empty `## Relevant History` means HISTORY? YES!, a line carrying the Director's answer (`Answered`, `ANSWER`, or `An:`) in `## Clarification Q&A` means CLARIFIED? YES! (questions alone leave it NO!), work has begun means STARTED? YES!, a non-empty `### Work Review` means REVIEWED? YES!, a commit for this task exists and `git status --short -- {task_paths}` reports no changes for those paths (quote a spaced path and confirm git reported the paths you passed; empty output from a bad pathspec is not clean) means DONE? YES!. Keep `WORKER:` and `OTHER:` from the latest state. If you cannot tell, ask the Master; do not re-run a gate the file shows is complete. Copy the template; never edit it.

## Project

`{project}` is the directory holding `eliots-agentics/`.
Never read, write, list, or stage `{project}/eliots-agentics/.humans-only/`.
Historical tasks (for research) available at `{project}/eliots-agentics/archive/`.


## Work

Evidence must be on disk. Logs, screenshots, and command output a reviewer must see go into files, not into your return. The commit is the deliverable.

Stage explicit paths only: the files you changed and the task file. Never `git add -A` or `git add .`, and stage nothing under `.humans-only/` or `.obsidian/`. Check `git status --short` before committing; only intended files are staged. Always git push, though a project may not have a remote.

The protocol messages:
- `PERMISSION` = "you may start"
- `QUESTION-ONLY` = "do not start yet; answer only"
- `ANSWERS` = "answers are in ## Clarification Q&A"

Send the message text, not the label.

## Notes on task work (reported symptoms)

A task that starts from a reported symptom ("lighting doesn't match", "objects are misplaced") is finished when the symptom is gone, not when a spec is satisfied. Do not set `DONE? YES!` while the Director still reports the symptom.

Do not turn a symptom into a confirmation task. "Verify and report any gap" with an expected answer of "no gap" closes by construction. Carry the Director's words into the task as one acceptance check per symptom line.

No "investigated" verdict without both:
- a candidate mechanism that would produce the reported symptom, and
- a named observation that would falsify that mechanism.

"No headless gap" is not a verdict. It means only "no bug found in our own formula". Comparing generated output to our own HEAD baseline, to values our own converter computed, or to structural validity (JSON parses, references resolve) proves self-consistency, not correctness. Name the external observation a claim was checked against, or state that none exists.

When there is no absolute reference of correct output, work by differential ablation: take a case that works and a case that fails from the same importer, enumerate what differs in the system that produces the symptom, and identify which difference would cause it. A component's values are not the cause when the same values render correctly in another scene.

An objective data defect (a missing field, a hardcoded zero, a value outside the range the engine writes, a field the engine does not deserialize) is headless-checkable. Only genuinely perceptual judgement is "editor-visual". Do not file a data defect under an `Open:` line in order to close a task.

When nothing else exists, the Director's editor observation is the closing oracle. State the mechanism and one discriminating test, ask the Director to look, and report back. Do not close on self-consistency instead.

## Visual evidence (screenshots)

The Master and vision-capable workers can SEE a rendered frame. "Headless" describes the import pipeline, not our ability to look at the result. A task whose symptom is visual must produce a screenshot and look at it; do not certify a rendered result from serialized JSON alone.

Which bridge: the running editor may be the Dev checkout, not the Steam install. Check before capturing. `D:/SteamLibrary/steamapps/common/sbox/bin/managed/sbox-cli.exe --list` returned `[]` while the Dev editor was live. Use the CLI that belongs to the running editor:

  DEV="C:/cseliot/1-Development/1-SBox/1-Engine-Builds/Dev/sbox-public/game/bin/managed/sbox-cli.exe"
  "$DEV" --list                                  # editor pid + project
  "$DEV" --call editor_status '{}'               # ActiveScene, IsPlaying, SceneHasUnsavedChanges
  "$DEV" --call editor_camera_screenshot '{}'    # prints the absolute PNG path

Before any capture, read `editor_status`. Do not save, play, or change the scene if `SceneHasUnsavedChanges` is true: that is the Director's live work. Position the view with `set_editor_camera` first. For a play-mode frame use `console_command '{"command":"screenshot_highres 1280 720"}'` and take the newest file under the engine's `screenshots/` directory; confirm freshness by timestamp, not `ls -t` alone. On Linux/Vulkan the plain `screenshot` command swaps red and blue; use `screenshot_highres` whenever colour matters.

Copy the PNG into the scratch directory immediately (the CLI's temp path is per-connection). Keep it out of the repo and cite its path in the task file's evidence. Large PNGs can time out in `vision_analyze`: downscale to a ~960x540 JPEG (about 10 KB) and load that with `vision_analyze`. Pass screenshots to workers and critics through `delegate_task(images=...)` so the pixels arrive with the turn; a child with `vision_analyze` can also load a path itself. State which you used.

Use screenshots differentially, since there is no absolute reference of correct output: capture the same view before and after a change, or capture a working case and a failing case from the same importer, and compare what moved. A component's values are not the cause when two scenes with those values render differently. Report what you actually saw (brightness, visible geometry, fog, colour) and the pixel or file evidence behind it; vision reads glow and bloom as overlap, so measure a number when a claim is about colour, position or size. Vision models also invent detail on low-contrast frames: one read of a flat gray viewport reported a UI panel with an "Add" button that a crop proved absent. Corroborate any specific object or text claim with a crop or a pixel measurement, and treat agreement between two models on the overall symptom as stronger than any one model's specific details.

A screenshot is evidence, not an oracle. It lets us confirm the symptom and confirm that a change altered the rendered result. It does not by itself say the result is correct. The Director's editor observation still closes genuinely perceptual judgement.


