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


