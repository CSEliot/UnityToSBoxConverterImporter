# common.md

During conflicting instructions: Director's dated instruction > project role/task files > skills > global defaults

A subagent runs exactly one turn. Nothing survives it except files on disk and its transcript.

The task file is the only shared memory. Read it at the start of every turn. The Director's answers and the critic's review arrive there between turns. Uncommitted prose in it is a hypothesis, not a gate: commit it, label it, or delete it before it steers a worker.

role instruction behaviour files are NOT to be modified. NO EDIT: common.md, critic.md, master.md, worker.md, roles.md.

## Task file

The header is shared state. Any role may update it when the state changes. Rewrite it as one complete line; do not drop fields.

- `STARTED?` work has begun.
- `CLARIFIED?` the Director's answers are recorded.
- `HISTORY?` the context summary is written.
- `REVIEWED?` a critic reviewed the current tree. A change to the reviewed files after the review sets `REVIEWED? NO!`. The critic's entry and flag, and the worker's reply and `DONE?` write, are not changes to the reviewed files.
- `DONE?` YES/FAILED/REJECTED. YES = Task complete according to all rules. FAILED = Task could not be complete by ai. REJECTED = Bad and impossible task given by Director, as determined by AI.
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

A task that starts from a reported symptom ("lighting doesn't match", "objects are misplaced") is finished when the symptom is gone, not when a spec is satisfied. Do not set `DONE? YES!` while the Director still reports it, and do not turn a symptom into a "confirm there is no gap" task. Judge the work by what the Director sees, not by whether our own converter emitted what we intended: comparing generated output to our own baseline or to structural validity proves self-consistency, not correctness.

Diagnose by changing one thing and looking again. Trust engine source and a before/after rendered frame over our own formula; a component's values are not the cause when two scenes with those values render differently. A visual symptom needs a rendered frame that someone looked at, and an objective data defect (a missing field, a hardcoded zero, a value the engine does not write) is headless-checkable and belongs on disk, never deferred to the Director. An "investigated" verdict needs a candidate mechanism that would produce the symptom and a named observation that would falsify it. A screenshot is evidence, not a verdict on correctness.

CLI, bridge selection, capture mechanics and the clean re-import loop live in the `sbox-cli-driving` and `unityimporterplus-reimport` skills; load them when needed rather than restating procedure here.

