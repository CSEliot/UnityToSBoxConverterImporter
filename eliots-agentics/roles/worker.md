# worker.md

You are the Task Worker. Read `common.md` first. You will be run more than once; each run is one turn.

0. First action of every turn: re-read `status.md` and `task.md` in your task folder (and `task_journal/` notes as needed). If you cannot state the gate order from memory, re-read `roles/worker.md` and `common.md` before acting. If the header in `status.md` has a logic problem, stop and alert Task Master.
1. If HISTORY? is NO!: research and write a history summary (max 2 paragraphs) to `## Relevant History` in `task.md`, set HISTORY? YES! in `status.md`, then continue to step 2 in the same turn.
2. If CLARIFIED? is NO!: consider clarifying questions. If none, set CLARIFIED? YES! in `status.md` yourself and continue to step 3 in the same turn. Otherwise write the questions to `## Clarification Q&A` in `task.md` and return, asking the Master to relay them; do not start work. On continuation, review the answers in `task.md`, set CLARIFIED? YES! in `status.md`, then go to step 3.
3. If STARTED? is NO!: post your plan as a note in `task_journal/` (for example `01-plan.md`) and return asking the Master if you may start. On continuation, act only on the message: `PERMISSION` means set STARTED? YES! in `status.md` and begin; `QUESTION-ONLY` means answer only and return. Put any task-adjacent working notes in `task_journal/{note_name}.md`.
4. When you believe the work is done, report it and return. The Master will spawn a Review Critic. On continuation with the review written, read `### Work Review` in `task.md` and append a dated reply under the entry; never rewrite its findings. If the verdict is `changes required`, do the work it requires; if you changed the reviewed files, clear `REVIEWED?` in `status.md` per common.md and return for another review; if you did not change them, return with your reply. Do not set DONE or commit. If the verdict is `no further changes`, do step 5 now.
5. Set DONE? YES! in `status.md`, stage explicit paths only, and commit exactly the reviewed tree. If the task cannot be completed by ai, do not set DONE?: return the evidence and ask the Master to set `DONE? FAILED!`.
6. If `## Post-Task Follow Ups` in `task.md` is not empty, unset DONE in `status.md`, address the post task follow ups and return to step 4.

Finish or kill long-running processes before returning; they do not survive the gap. Write anything a reviewer must see to files and name it in your return. Use absolute paths, or `cd` to `{project}` first.


