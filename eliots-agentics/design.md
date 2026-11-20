# design.md

**Not runtime input. Agents do not read this file.**

Why the role docs are split this way.

## One turn, then continuation

Hermes subagents run one turn and end. They cannot idle or wait. The Master continues a returned worker with `delegate_task(action='continue', ...)`, which reloads the saved transcript but gives a fresh terminal with no live processes. That is why every role re-reads the task file and writes evidence to disk.

## Split by audience, not topic

The spawn prompt delivers files. A topic split (gates, commits, evidence) would force every role to read most files. Each role reads its own file plus the shared contract; the critic reads only its own.

## One common contract

The task-file header is shared state with several writers. If the contract lived in three role files, a change in one would silently break the state machine. `common.md` is the single home; the critic carries only the two field semantics it acts on because it does not read `common.md`.

## The critic is low-context on purpose

The critic never reads `master.md` or this file. Leaking the plan would bias the review. Its value is a cold read of the diff.

## roles.md stays

The project skill and the human kickoff point at `roles.md`. It is a router, not a manual, so those references keep working.

## The task file is not split

It is per-task state, not role knowledge. Splitting it would multiply reads and create a second source of truth.

## Why the Master keeps the branching

The Master is the only agent that survives the whole task. If a worker learned the Master's branching, it could skip a gate. The harness already refuses a child's `delegate_task` at `max_spawn_depth=1`; no prompt rule is needed.

## Runtime vs reasons

Runtime files hold imperatives only. A rule agents must obey stays in `common.md`, `master.md`, `worker.md`, or `critic.md`, with "(why: design.md)" if useful. This file holds reasons and is capped so it cannot become a second runtime manual.
