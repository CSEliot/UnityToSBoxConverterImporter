## Task
Make clear this project is about supporting URP Unity 6.3 (6000.3), not "Unity 6.7".

### Description
Director correction, 2026-11-26: the original session context said "This session is now about building Unity 6.7 URP support", and that label propagated into research docs, task files, session notes and the branch name `urp_6_7_support`. That was a mistake. The real target is Unity 6.3 = 6000.3.x, verified on disk as `m_EditorVersion 6000.3.29f1` / `m_EditorVersionWithRevision 6000.3.29f1 (96d2fe17529c)` in `C:/cseliot/1-Development/4-Unity/2-Projects/SyntyForest/ProjectSettings/ProjectVersion.txt`, with `"com.unity.render-pipelines.universal": "17.3.0"` in `SyntyForest/Packages/manifest.json`. Work stays on URP 17.x rather than one literal editor build.

Scope of this task: audit and correct every forward-looking statement that says 6.7 or implies 6.7 is the target, and state the target as URP Unity 6.3 / 6000.3. Historical verbatim quotes of the Director's original wording (for example `first urp research/task.md` Q1/Q3, archived session notes, and this task's own Clarification Q&A) may stay verbatim but must carry an explicit "superseded" annotation rather than being silently rewritten or left to read as current scope.

Do not change importer code, do not change URP detection logic, and do not rename the git branch without a recorded Director decision (a rename affects the remote and CI).

### Acceptance (one explicit pass/fail check per line)
1. `rg -n -i '6\.7|six\.?dot\.?seven|urp_6_7'` over `eliots-agentics/tasks/`, `eliots-agentics/session-latest.md`, `Documentation/*.md`, `README*`, and `Editor/**/*.cs` returns zero hits outside this task's own folder and outside `eliots-agentics/archive/` (archive is immutable history and excluded).
2. Every hit that is intentionally retained as a verbatim historical quote carries a "superseded" annotation on the same line or in an immediately adjacent dated note.
3. `eliots-agentics/tasks/urp-support-research.md` and `eliots-agentics/tasks/terrain-import-research.md` state the target as Unity 6.3 (6000.3) / URP 17.3.0 with the on-disk version facts.
4. `eliots-agentics/session-latest.md` has a dated Director note recording the correction, marked as superseding the "Unity 6.7" label.
5. `git diff --name-only` for this task shows no `Editor/Core/*.cs` file.
6. The branch-rename decision is recorded in `## Director Notes` before any branch rename is attempted; if no rename is wanted, that is recorded too.

### Work Review


## Clarification Q&A
An (Director 2026-11-26): "Oshit, I fucked up. I REALLY fucked up. This is all about UNITY SIX DOT 3 (6000.3) SUPPORT! NOT 6.7! Create a task to 'make clear this project is about supporting URP Unity 6.3'!"
An (Director 2026-11-26, follow-up): "(don't DO the task, just MAKE the task)".

## Relevant History
Original context line: "This session is now about building Unity 6.7 URP support." Prior Master decision, now superseded: treat "Unity 6.7 URP" as a session label for on-disk 6000.3.29f1 + URP 17.3.0. `first urp research/task.md` Q1/Q3 and `urp-support-research.md` carry that label. Branch `urp_6_7_support`. Tasks 21, 22 and 27 are DONE; 23-28 are open; this task is 29.

## Notes
Standing constraints (Director 03:57, unchanged): 1A/1C nuance (6000.3.29f1 / URP 17.3.0 is the session fixture), 3A (visual fidelity only, terrain visuals in scope), 4A (three editor conditions plus NTFY before each large round), 5A (no new tooling, no fixture package). Editor rounds follow the 4A conditions: before any editor round, check that no Director-owned editor is already open on that project; never close an editor this work did not launch; NTFY before each large round. This task is documentation-only; no harness run, editor round or compile gate is required.

## Stylistic Task Notes:
References to variables like X and Y automatically imply that these are to be exposed properties for developer tweaking.
When programming, portability and DRY are a priority.

## Post-Task Follow Ups
