## Task
Terrain oracle education: Director prerequisites (step 1 of 2).

### Description
Master note 2026-11-26 08:47: the text below is the Director's verbatim brief. The final two paragraphs are a stale 08:28 snapshot: current HEAD is `eb48c7f`, task 22 is DONE, the Review Critic has finished, and the next hourly archive is due around 09:15. They are preserved for fidelity, not as live state.

Short version: your 07:14-07:59 edits added assets but did not produce a controlled 1-to-1 oracle. Every section 7.2 hold-constant axis is PARTIAL or MISSING, every vary axis MISSING except a few PARTIAL, and all 14 section-10 UNVERIFIED items remain open. That is why "the process was very similar but the output is much less similar".

What is left for you, in priority order:

P0 - Three decisions, cost one note each, unblocks everything else (section 9):
(a) whether to extend the oracles with new ablation axes at all;
(b) add a terrain implementation task or amend report-only task 26;
(c) declare the canonical build. The Unity oracle is now 6000.7.0b10 / URP 17.7.0 with 1 terrainlayer, while the session fixture SyntyForest is 6000.3.29f1 / URP 17.3.0. Right now the comparison spans two different Unity/URP versions.

P1 - Turn both notes_on_scene_creation.txt files into a comparison manifest. They currently record only brush strokes: s&box "size 3937, opacity 1, 10 clicks" vs Unity "brush size 200, opacity 100, 10 clicks" - different brush settings, so the shapes differ by construction. Record the section 7.2 constants numerically: extents/units, height resolution and samples, layer inventory, splat assignment, hole mask, camera poses, lighting/fog/tonemapping state, build. Cheapest action, closes the most items.

P2 - Neutralise the screenshot trap and capture matched pairs. Verified: SampleScene.unity:53 still has m_EnableBakedLightmaps: 1; the s&box scene has FogMode "Enabled", 2 cameras and post on both; Unity has m_Fog: 0 and 2 cameras with post off/on. Set baked to 0, make both sides realtime with fog/tonemapping matched, then capture four annotated s&box camera poses into Documentation/. There are currently zero s&box counterpart captures anywhere under C:/cseliot. The four existing unity-camera-*.png appear to be SyntyForest rather than the small oracle (child's finding; I did not visually verify), so label which scene each came from.

P3 - One-axis variants, only if P0a says extend. Clone the scenes and vary exactly one axis each with a README: tiling scale, the existing .tmat pair, resource.terrain vs embedded Storage, layer count / dominating texel, holes, amplitude/resolution, non-square size, pixel-error/clipmap LOD, trees/details. Note: resource.terrain exists, but minimal.scene:209-210 still embeds Storage ("$compiler": "embed") with no {Id,Path} reference to it, so answering Q6 needs a scene that actually references the standalone .terrain - that is your authoring action, not an agent check.

P4 - Clutter pair if trees/details are in scope. Documentation/ai-note-sbox-vs-unity-terrain-and-clutter.md concludes s&box has no detail-layer system at all; grass belongs to Clutter. Author a ClutterDefinition + TerrainMaterialScatterer vs Unity detail/tree prototypes comparison.

P5 - Fold build provenance into P1/P2.

Not yours (importer code): binary TerrainData reader, TerrainStorage writer, splat/hole/terrainlayer encoding, ToTmat keys, collider emission, tree/detail mapping. UNVERIFIED items 1, 6 and 8 are code-blocked and no Director action closes them; 2, 5, 9 and 12 are agent-checkable.

One housekeeping note: HEAD moved to 7ee25b0 at 08:17 from a parallel session - Documentation files only (the ai-note, four PNGs, graphics-warnings md), no Core or task files, so task 22's review tree is intact. I recorded this investigation as an 08:28 note in session-latest.md.

Review Critic sa-0-1c651de3 still running on task 22; next hourly archive due around 09:15.

### Required work
This task is step 1 of 2 and is half working. It CANNOT be started or closed while "requirements by Director not done" applies. No worker may be dispatched and no DONE? YES! may be set until the P0-P5 requirements above are complete and verified on disk. Once they are, step 2 (the terrain implementation task: binary TerrainData reader, TerrainStorage writer and verified mapping) can be specified against the section 7.2 round-trip.

### Acceptance (one explicit pass/fail check per line)
1. P0a, P0b and P0c are recorded as decisions in `eliots-agentics/session-latest.md` `## Director Notes`.
2. Both `notes_on_scene_creation.txt` files contain the numeric section 7.2 comparison manifest (extents/units, height resolution and samples, layer inventory, splat assignment, hole mask, camera poses, lighting/fog/tonemapping state, build), not just brush strokes.
3. `SampleScene.unity` has `m_EnableBakedLightmaps: 0`; both sides are realtime with fog/tonemapping matched; four annotated s&box camera captures exist in `Documentation/` and each Unity PNG is labelled with its source scene.
4. If P0a is "extend": cloned one-axis variant scenes plus a README exist, holding the manifest constants and varying exactly one axis each.
5. If P0b puts trees/details in scope: a `ClutterDefinition` / `TerrainMaterialScatterer` vs Unity detail/tree prototype comparison exists.
6. Build provenance (Unity/URP versions, per-scene state) is recorded in the manifest and captures.
7. If the Q6 ablation is in scope: a scene references `resource.terrain` via an `{Id, Path}` reference rather than embedded `Storage`.

#### Master disposition (2026-11-26 23:59, from Director chat statement)
Director (chat, verbatim): "C:\cseliot\1-Development\4-Unity\2-Projects\unity_samples_oracle and C:\cseliot\1-Development\1-SBox\2-Projects\sbox_samples_oracle are up to parity in terms of assiting w terrain importing. No further work by Director will be done on them. And no screenshots exist, I do not consider them helpful for comparing the 2 oracles."
CONSEQUENCES (Master interpretation, change if wrong):
- The Director's remaining oracle-side prerequisites are WITHDRAWN: P1 (numeric comparison manifest), P2 (matched captures / baked-lightmap neutralisation), P3 (one-axis variants), P5 (build provenance) and acceptance #2/#3/#4/#6/#7 no longer bind. P0c (canonical build) is already settled: SyntyForest and the UNITY oracle sit on 6000.7.0b10 / URP 17.7.0 (parent-verified); `sbox_samples_oracle` is an s&box project and has no Unity version. No screenshots will be produced or compared for the oracles.
- P0a/P0b collapse into ONE remaining decision that gates step 2: extend the oracles further (no, per "no further work"), and add a terrain implementation task vs keep terrain report-only in task 26. Step 2 (binary TerrainData reader, TerrainStorage writer, verified mapping) stays UNSPECIFIED until that decision.
- This task stays STARTED? NO! DONE? NO! (half working step 1/2): the only open item is the Director's terrain-implementation decision, asked in chat 23:59. Clutter / terrain detail painting / terrain trees remain OUT OF SCOPE per the Director Update in Post-Task Follow Ups.

### Work Review
2026-11-27 00:48 - Verdict: changes required
1. [High] task.md:35-42,65-66 — DONE? YES! is not legitimate as written. Acceptance #1 requires P0a/P0b/P0c recorded in `eliots-agentics/session-latest.md` `## Director Notes`; that section (lines 3-17) records the branch-rename decision only, while the 00:46 Q1 mapping is under `## Task Master Notes` (lines 21-26). Neither verbatim Director statement (23:59 "no further work ... no screenshots"; 00:46 "1A, 2A") withdraws #1. Fix: put the three decisions in `## Director Notes` or get an explicit Director withdrawal of #1 before closing; also reconcile status.md:1 (DONE? NO!) with the closure note's "closes DONE? YES!".
2. [High] task.md:57 — attribution overreach. The only verbatim Director text is "1A, 2A"; the long option-1A sentence after the colon is the Master's full-text expansion (session-latest.md:22-23 labels it "Mapping recorded as both letter and full text" under Task Master Notes). As written, the paragraph reads as Director words. Fix: relabel it "Master's full-text expansion of option 1A (not Director verbatim)" and reserve quotation marks for "1A, 2A".
3. [Low] task.md:47 — "both oracles and SyntyForest sit on 6000.7.0b10 / URP 17.7.0 (parent-verified)" is false for `sbox_samples_oracle`: it is an s&box project (ProjectSettings/Collision.config, no ProjectVersion.txt), not a Unity/URP build. Fix: restrict the claim to the Unity oracle and SyntyForest; call the s&box oracle the s&box-side sample.
4. [Info] task.md:35-42,44-49,66 — acceptance #2/#3/#4/#6/#7 are covered by the recorded 23:59 withdrawal or are vacuous (#5 trees/details out of scope, #7 Q6 ablation not in scope); `git diff HEAD` for this folder is doc-only, so "it delivered no code" is not contradicted. Findings 1-3 are the blockers.

Tree: eliots-agentics/tasks/28 Terrain Oracle Education (Director Prerequisites)/status.md = 4bf8e5962e7cab0871f60a355ea8be6280c7fd3b (pre-edit snapshot) — `git hash-object --path="eliots-agentics/tasks/28 Terrain Oracle Education (Director Prerequisites)/status.md" "C:/cseliot/1-Development/1-SBox/2-Projects/unityimporterplus_branch_2/eliots-agentics/tasks/28 Terrain Oracle Education (Director Prerequisites)/status.md"`
Tree: eliots-agentics/tasks/28 Terrain Oracle Education (Director Prerequisites)/task.md = 3cfc4a2684623d4813be886e48eb03aae390d5b6 (pre-edit snapshot) — `git hash-object --path="eliots-agentics/tasks/28 Terrain Oracle Education (Director Prerequisites)/task.md" "C:/cseliot/1-Development/1-SBox/2-Projects/unityimporterplus_branch_2/eliots-agentics/tasks/28 Terrain Oracle Education (Director Prerequisites)/task.md"`

#### Re-check 2026-11-27 00:54
2026-11-27 00:54 - Verdict: changes required
- Original [High] #1 (acceptance #1): CLOSED. Director 01:03 answered "Yes - add the three decisions to ## Director Notes (Master patches only that block)"; `eliots-agentics/session-latest.md:19-22` now records P0a/P0b/P0c inside `## Director Notes`. Lines 1-17 are byte-identical to the 00:48 read snapshot; only `:19-22` was added and `## Task Master Notes` moved from `:19` to `:24`. [Info] `git diff HEAD -- eliots-agentics/session-latest.md` is NOT a clean one-block patch (165 additions, 52 deletions) because HEAD's copy predates the Signal-ban notice; the Director-Notes portion has zero deletions, but the brief's "removed-lines side is empty" is false for the file as a whole.
- Original [High] #2 (attribution): CLOSED. Q&A `:64` now says "the Director chose option 1A ... AS POSED BY THE MASTER (not a Director quote)"; task 30 Q&A `:33` has the same pattern. Only "1A, 2A" is labeled verbatim.
- Original [Low] #3: PARTIAL. `:73` and `:65` carry the s&box qualification, but `:47` still says "both oracles and SyntyForest sit on 6000.7.0b10 / URP 17.7.0 (parent-verified)" with no STALE mark. Fix: add the same qualification at `:47` or mark that 23:59 line historical.
- NEW [Low] introduced by the fix: `:74` still says "OPEN ITEM (blocks DONE ...)" and `:65` still says "except acceptance #1's literal placement requirement", although status.md says acceptance #1 is now literally satisfied. Fix: replace `:74` with a dated resolved note (Director 01:03 + session-latest.md:19-22) and delete the "except" clause at `:65`.
Tree: eliots-agentics/tasks/28 Terrain Oracle Education (Director Prerequisites)/status.md = 7348038a91dfbc79c09b069fe54c7596fa139844 (pre-edit snapshot) — `git hash-object --path="eliots-agentics/tasks/28 Terrain Oracle Education (Director Prerequisites)/status.md" "C:/cseliot/1-Development/1-SBox/2-Projects/unityimporterplus_branch_2/eliots-agentics/tasks/28 Terrain Oracle Education (Director Prerequisites)/status.md"`
Tree: eliots-agentics/tasks/28 Terrain Oracle Education (Director Prerequisites)/task.md = afebb9de2d2f73c6cbb9d353a551eb5a2723042a (pre-edit snapshot) — `git hash-object --path="eliots-agentics/tasks/28 Terrain Oracle Education (Director Prerequisites)/task.md" "C:/cseliot/1-Development/1-SBox/2-Projects/unityimporterplus_branch_2/eliots-agentics/tasks/28 Terrain Oracle Education (Director Prerequisites)/task.md"`

#### Re-check 2026-11-27 00:56
- Residual 1 / original [Low] #3: CLOSED at `:47` — now "SyntyForest and the UNITY oracle sit on 6000.7.0b10 / URP 17.7.0 (parent-verified); `sbox_samples_oracle` is an s&box project and has no Unity version." No "both oracles" claim remains.
- Residual 2 / NEW [Low] contradiction: CLOSED at `:85` ("OPEN ITEM (RESOLVED 2026-11-27 01:05) ... Acceptance #1 is therefore SATISFIED.") and `:76` ("...resolved, including acceptance #1's placement requirement (Director approved the ## Director Notes record at 01:03; note appended 01:05)"). The "except acceptance #1" / "blocks DONE" wording is gone.
- Bounded to these two residuals; previously CLOSED findings not reopened.
Tree: eliots-agentics/tasks/28 Terrain Oracle Education (Director Prerequisites)/status.md = 7348038a91dfbc79c09b069fe54c7596fa139844 (pre-edit snapshot) — `git hash-object --path="eliots-agentics/tasks/28 Terrain Oracle Education (Director Prerequisites)/status.md" "C:/cseliot/1-Development/1-SBox/2-Projects/unityimporterplus_branch_2/eliots-agentics/tasks/28 Terrain Oracle Education (Director Prerequisites)/status.md"`
Tree: eliots-agentics/tasks/28 Terrain Oracle Education (Director Prerequisites)/task.md = ce5ea5edb097031e572a6f43b3eb502f9055ed76 (pre-edit snapshot) — `git hash-object --path="eliots-agentics/tasks/28 Terrain Oracle Education (Director Prerequisites)/task.md" "C:/cseliot/1-Development/1-SBox/2-Projects/unityimporterplus_branch_2/eliots-agentics/tasks/28 Terrain Oracle Education (Director Prerequisites)/task.md"`

Verdict: no further changes

## Clarification Q&A
An (Director 2026-11-26 08:47): "add a terrain task that requires a 'half working step 1/2, requirements by direcotr not done' and in the descirption it has what I need to do" followed by the verbatim P0-P5 brief reproduced in the Description above.

An (Director 2026-11-27 00:46, chat, verbatim): "1A, 2A". This task is the Q1 answer: the Director chose option 1A. The text of option 1A AS POSED BY THE MASTER (not a Director quote) was: "add the terrain implementation task (binary TerrainData reader, TerrainStorage writer, verified mapping) as its own numbered task, specified against section 7.2's round-trip. Do not amend report-only task 26 and do not let it block 25/26. Scope guard: read the 41 MB blob only; no editor launch; if the round-trip cannot be shown headlessly, the task ends at its own gate rather than widening."
CONSEQUENCE: P0b is ANSWERED by opening task 31 `Terrain Import Implementation (step 2)`; task 26 stays report-only and unamended. Together with the 23:59 disposition (P0a = do not extend the oracles; P0c settled at one Unity build 6000.7.0b10/URP 17.7.0 for SyntyForest and the Unity oracle; P1/P2/P3/P5 and acceptance #2-#4/#6/#7 withdrawn), every Director prerequisite this task gates on is now resolved, including acceptance #1's placement requirement (Director approved the ## Director Notes record at 01:03; note appended 01:05).

## Relevant History
`eliots-agentics/tasks/terrain-import-research.md` sections 7.1, 7.2, 8, 9, 10 (committed d583109). Read-only investigation reports `04-oracle-current-state.md` and `05-oracle-director-actions.md` under `C:/Users/Eliot/AppData/Local/hermes/profiles/hermes-sbox/cache/scratch/terrain-research/`. `session-latest.md` 08:28 note. Task 27 (research) is DONE; tasks 21 and 22 are DONE; 23-26 remain open and unstarted.

## Notes

#### Master closure note (2026-11-27 00:46; corrected 01:02)
All Director prerequisites this task gates on are resolved: P0a = do not extend the oracles (Director 23:59: parity reached, no further Director work, no screenshots); P0b = ANSWERED 00:46 by opening task 31 `Terrain Import Implementation (step 2)`; P0c = one Unity build, 6000.7.0b10 / URP 17.7.0 (parent-verified for SyntyForest and the Unity oracle; `sbox_samples_oracle` is an s&box project and has no Unity version - corrected 01:02); P1/P2/P3/P5 and acceptance #2/#3/#4/#6/#7 withdrawn by the same 23:59 statement. Clutter / terrain detail painting / terrain trees stay OUT OF SCOPE (Director Update in Post-Task Follow Ups). Step 2's specification is task 31, which carries the Director's scope guard (41 MB blob read-only, no editor launch, ends at its own gate if the round-trip cannot be shown headlessly).
OPEN ITEM (RESOLVED 2026-11-27 01:05): acceptance #1 required P0a/P0b/P0c "recorded as decisions in `eliots-agentics/session-latest.md` `## Director Notes`". They now are: asked at 01:03 whether to record them in that Director-owned section, the Director answered "Yes - add the three decisions to ## Director Notes (Master patches only that block)", and the Master appended a dated block there (pre-existing Director lines byte-identical; `git diff` on that section shows additions only). Acceptance #1 is therefore SATISFIED.

#### Master premise correction (2026-11-26 09:05, binding)
The Standing constraints line below ("1A (6000.3.29f1 / URP 17.3.0 is the session fixture)") and Description P0c ("the comparison spans two different Unity/URP versions") are STALE. Verified on disk at 09:01-09:05: SyntyForest is `6000.7.0b10 (4f3c1c5c4346)` / URP 17.7.0 (dir mtime 09:01) and unity_samples_oracle is ALSO `6000.7.0b10` / 17.7.0; they are distinct directories. Both oracles now sit on the SAME Unity/URP build, so P0c's premise no longer holds and the canonical-build decision is effectively narrowed to "one build, 17.7.0". Director 1A already targets the URP 17.x surface rather than one editor build. This task remains BLOCKED on Director P0-P5 (step 1 of 2): no worker dispatched, no DONE? YES! until all prerequisites are verified on disk. HEAD is 0d31400 (remote main in sync), not eb48c7f. The next hourly archive is due around 09:15. SUPERSEDED (2026-11-26 22:27, Director 1A re-pin; appended 23:17): the verbatim Director brief above (including its `6000.3.29f1` / URP `17.3.0` statements at Description P0c and Standing constraints) is preserved byte-for-byte as history and is now superseded. Canonical Unity 6.7 = `6000.7.0b10 (4f3c1c5c4346)` / URP 17.7.0 with caches `com.unity.render-pipelines.universal@e9044f1f1f57`, `com.unity.render-pipelines.core@7aa87ac5532f`, `com.unity.render-pipelines.universal-config@767fc04a69c2` under FIXTURE/Library/PackageCache/; the 17.3 hashes and 6000.3.29f1 / 96d2fe17529c do not exist under 4-Unity (parent-verified). One SUPERSEDED note per file. BIRP is out of scope (Director 22:55); the remaining four-scene fixture is the baseline. Camera count PINNED by the read-only fact finder (Director 23:17): 7 `!u!20` Camera components over the four scenes (4 enabled, 3 disabled), 6 with a UniversalAdditionalCameraData sibling; the 7th is a plain camera at `PolygonGeneric/Scenes/Overview.unity:4277`.
Standing constraints (Director 03:57): 1A (6000.3.29f1 / URP 17.3.0 is the session fixture), 3A (visual fidelity only, terrain visuals in scope), 4A (three editor conditions plus NTFY before each large round), 5A (no new tooling, no fixture package). Editor rounds follow the 4A conditions: before any editor round, check that no Director-owned editor is already open on that project; never close an editor this work did not launch; NTFY before each large round. No `Editor/Core/*.cs` change belongs to step 1.

## Stylistic Task Notes:
References to variables like X and Y automatically imply that these are to be exposed properties for developer tweaking.
When programming, portability and DRY are a priority.



## Post-Task Follow Ups
Director Update:
Clutter / terrain detail painting / terrain trees are out of scope. SyntyForest doesn't use it, so not importent for getitng into sbox.