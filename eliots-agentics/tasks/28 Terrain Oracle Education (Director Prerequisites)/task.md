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
- The Director's remaining oracle-side prerequisites are WITHDRAWN: P1 (numeric comparison manifest), P2 (matched captures / baked-lightmap neutralisation), P3 (one-axis variants), P5 (build provenance) and acceptance #2/#3/#4/#6/#7 no longer bind. P0c (canonical build) is already settled: both oracles and SyntyForest sit on 6000.7.0b10 / URP 17.7.0 (parent-verified). No screenshots will be produced or compared for the oracles.
- P0a/P0b collapse into ONE remaining decision that gates step 2: extend the oracles further (no, per "no further work"), and add a terrain implementation task vs keep terrain report-only in task 26. Step 2 (binary TerrainData reader, TerrainStorage writer, verified mapping) stays UNSPECIFIED until that decision.
- This task stays STARTED? NO! DONE? NO! (half working step 1/2): the only open item is the Director's terrain-implementation decision, asked in chat 23:59. Clutter / terrain detail painting / terrain trees remain OUT OF SCOPE per the Director Update in Post-Task Follow Ups.

### Work Review


## Clarification Q&A
An (Director 2026-11-26 08:47): "add a terrain task that requires a 'half working step 1/2, requirements by direcotr not done' and in the descirption it has what I need to do" followed by the verbatim P0-P5 brief reproduced in the Description above.

## Relevant History
`eliots-agentics/tasks/terrain-import-research.md` sections 7.1, 7.2, 8, 9, 10 (committed d583109). Read-only investigation reports `04-oracle-current-state.md` and `05-oracle-director-actions.md` under `C:/Users/Eliot/AppData/Local/hermes/profiles/hermes-sbox/cache/scratch/terrain-research/`. `session-latest.md` 08:28 note. Task 27 (research) is DONE; tasks 21 and 22 are DONE; 23-26 remain open and unstarted.

## Notes

#### Master premise correction (2026-11-26 09:05, binding)
The Standing constraints line below ("1A (6000.3.29f1 / URP 17.3.0 is the session fixture)") and Description P0c ("the comparison spans two different Unity/URP versions") are STALE. Verified on disk at 09:01-09:05: SyntyForest is `6000.7.0b10 (4f3c1c5c4346)` / URP 17.7.0 (dir mtime 09:01) and unity_samples_oracle is ALSO `6000.7.0b10` / 17.7.0; they are distinct directories. Both oracles now sit on the SAME Unity/URP build, so P0c's premise no longer holds and the canonical-build decision is effectively narrowed to "one build, 17.7.0". Director 1A already targets the URP 17.x surface rather than one editor build. This task remains BLOCKED on Director P0-P5 (step 1 of 2): no worker dispatched, no DONE? YES! until all prerequisites are verified on disk. HEAD is 0d31400 (remote main in sync), not eb48c7f. The next hourly archive is due around 09:15. SUPERSEDED (2026-11-26 22:27, Director 1A re-pin; appended 23:17): the verbatim Director brief above (including its `6000.3.29f1` / URP `17.3.0` statements at Description P0c and Standing constraints) is preserved byte-for-byte as history and is now superseded. Canonical Unity 6.7 = `6000.7.0b10 (4f3c1c5c4346)` / URP 17.7.0 with caches `com.unity.render-pipelines.universal@e9044f1f1f57`, `com.unity.render-pipelines.core@7aa87ac5532f`, `com.unity.render-pipelines.universal-config@767fc04a69c2` under FIXTURE/Library/PackageCache/; the 17.3 hashes and 6000.3.29f1 / 96d2fe17529c do not exist under 4-Unity (parent-verified). One SUPERSEDED note per file. BIRP is out of scope (Director 22:55); the remaining four-scene fixture is the baseline. Camera count PINNED by the read-only fact finder (Director 23:17): 7 `!u!20` Camera components over the four scenes (4 enabled, 3 disabled), 6 with a UniversalAdditionalCameraData sibling; the 7th is a plain camera at `PolygonGeneric/Scenes/Overview.unity:4277`.
Standing constraints (Director 03:57): 1A (6000.3.29f1 / URP 17.3.0 is the session fixture), 3A (visual fidelity only, terrain visuals in scope), 4A (three editor conditions plus NTFY before each large round), 5A (no new tooling, no fixture package). Editor rounds follow the 4A conditions: before any editor round, check that no Director-owned editor is already open on that project; never close an editor this work did not launch; NTFY before each large round. No `Editor/Core/*.cs` change belongs to step 1.

## Stylistic Task Notes:
References to variables like X and Y automatically imply that these are to be exposed properties for developer tweaking.
When programming, portability and DRY are a priority.



## Post-Task Follow Ups
Director Update:
Clutter / terrain detail painting / terrain trees are out of scope. SyntyForest doesn't use it, so not importent for getitng into sbox.