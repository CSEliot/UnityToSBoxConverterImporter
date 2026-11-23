## Status: STARTED? YES! CLARIFIED? YES! HISTORY? YES! REVIEWED? NO! DONE? NO! WORKER: sa-0-bb00b315 OTHER: implementer continued after iteration cap to land the code fix; oracle evidence + falsifications folded into Description

## Task
Generated .scene component blocks do not conform to the engine's authored schema.

### Description
NEW task created 2026-11-23 from an investigation run today (per Director bedtime item 4: "Be open
to creating NEW tasks based on investigations based on subagents, given all the work of our
conversations today"). A parallel-session investigation produced the skill reference
`references/engine-schema-conformance.md`, which identified OBJECTIVE, headless-checkable data
defects in generated `Sandbox.*` component blocks. These are not "editor-visual" limitations and
must not be filed as `Open:` checks.

This is a CLASS-level root-cause task, distinct from the five symptom tasks (14-18): those are
finished when the reported symptom is gone; this one is finished when every generated scene
component conforms to the engine's authored shape, verified against an external oracle.

The importer's own output is NOT an oracle. Use the installed s&box engine:
- Engine-authored `.scene` files under `<engine>/addons/`, `templates/`, `samples/`,
  `download/`. Written BY THE EDITOR, so their component key sets and value ranges are the
  authoritative shape.
- `<engine>/bin/managed/Sandbox.Engine.xml` and sibling `*.xml` for property semantics,
  defaults, `ComponentVersion` and `Upgrade*Tint` / `UpgradeLegacy*` upgrade behaviour.
Resolve `<engine>` from the running install; on Steam it is `D:/SteamLibrary/steamapps/common/sbox`,
on the Dev build `.../1-Engine-Builds/Dev/sbox-public/game`.

Known defects already identified (verify, do not assume these are the only ones):
1. `Editor/Core/UnitySceneExport.cs:673` hardcodes `["SkyColor"] = "0,0,0,0"` for EVERY
   generated `Sandbox.DirectionalLight`.
2. Generated directional lights emit NO `Intensity` key; intensity is folded into `LightColor`.
   Engine source `.../Dev/sbox-public/engine/Sandbox.Engine/Scene/Components/Light/Light.cs:18,144-172,204-223`
   proves `LightColor` and `Intensity` are SEPARATE serialized properties and rendered colour =
   `LightColor * Intensity * normalized Temperature`; folding happens at `UnitySceneExport.cs:658-693`.
   This is the real, headless-checkable schema misuse.
3. Generated lights have NO `__version`. `Fog.cs:34-59` shows `Sandbox.Fog` is version 1 with an
   upgrader that injects `DistanceRange` when absent, so a generated Fog with no `__version` and
   no `DistanceRange` is MUTATED on load — falsifier: a generated Fog with `__version 1` and
   `DistanceRange` surviving load unchanged. HOWEVER the "unversioned LIGHT re-defaults to black
   tint" mechanism is FALSIFIED 2026-11-23: `Light.cs:35-45` upgraders only set `LightColor` when
   MISSING (#E9FAFF), and generated lights already have it. Add `__version` for schema conformance
   but do NOT claim it fixes a black-tint upgrade.
4. Hardcoded sentinels elsewhere: `Sandbox.Fog` `ColorSource = Constant`,
   `MaximumOpacity = 1`, `Color 0.5,0.5,0.5,1`.
   FALSIFIED 2026-11-23 (drop): `SkyBox2D.SkyMaterial` as a bare `.vmat` string is NOT a defect —
   engine-authored scenes use the same bare string; only editor-resaved scenes use the object form.
   FALSIFIED 2026-11-23 (drop): authored `LightColor` is NOT limited to 0..2 (directional reaches
   3.96, point ~36.89); do not use that range as a gate.
5. No authored `Sandbox.Fog` instance exists in either install's scenes/prefabs, so Fog key-set
   comparison must rely on engine source (`Fog.cs`) plus XML docs, not an authored `.scene` sample.

Required method:
1. Enumerate EVERY `Sandbox.*` component type the exporter emits (grep the `Editor/Core/*.cs`
   for `["__type"]` / component constructors and read the generated `.scene` blocks). Do not
   locate or count types with `grep -o` on serialized references; use a recursive `python3` +
   `json.load` walk over the whole scene selecting nodes whose `__type` matches.
2. For each type, collect authored key sets and value ranges from engine-authored `.scene` files,
   and diff against the generated block in BOTH directions. Missing keys are defects, not
   defaults (an engine default can be black/zero/1 and silently wrong for the intent). Extra keys
   may be ignored or misspelled.
3. Read the XML docs for every property involved. Check `__version` on EVERY generated component
   and emit the current authored version.
4. Treat hardcoded sentinels, colours with intensity already multiplied in, and values with no
   separate multiplier field as suspects; verify each against an authored value range and the XML
   docs.
5. Fix the ROOT CAUSE in the engine-free importer core (`Editor/Core/*.cs`) so generated
   components carry the authored key set, `__version`, and correctly separated
   `LightColor`/`Intensity`/`SkyColor` (and equivalent fixes for other component types found).
   Fix the CLASS across all emitted scenes, not one value. Preserve source intent: do not simply
   re-default every scene to a constant; carry the source-derived value into the correct field.
6. Gate on a diff of the emitted component BLOCK against the authored shape, plus XML-doc
   semantics. `verify_output.py` / "0 problems" is necessary but not sufficient: it validates
   structure and references only.
7. Regression gates: harness build 0 errors / 0 warnings; `building.unitypackage` (24 files,
   no --scenes) and `city--1.unitypackage` (1,166 files, no --scenes) byte-identical to a fresh
   HEAD-code baseline with BOTH trees populated; `verify_output.py <outRoot>
   <outRoot>/imported` 0 problems; two identical runs byte-identical; BOM-less; no stray
   artifacts. Re-run the old_school `--scenes` corpus comparison on the final bytes.
8. Because this changes rendered lighting/fog/sky, capture a before/after screenshot pair from
   the editor (no editor is currently up; launch the correct install, read `editor_status`
   before capture, do not save/play/change if `SceneHasUnsavedChanges` is true) and LOOK at it.
   Put all evidence (component diffs, XML excerpts, screenshots, gate logs) in scratch files and
   cite absolute paths in ## Notes.

Do not overpromise: engine-capability ceilings (no volumetric fog, no procedural/HDRI sky
parity, no photometric light units) remain out of scope; this task is about emitting the correct
SCHEMA for the values we do emit. Coordinate with the five symptom tasks (14-18): they may touch
the same `Editor/Core/UnitySceneExport.cs` regions, so the Master sequences the code work.

Stage by explicit path only. No push until the Master verifies. Do not add unrequested tooling.
Final answer in English.

### Work Review


## Notes


## Clarification Q&A

ANSWERED (Director, 2026-11-23 bedtime directive): "1. All those things we originally mentioned
that were accidentally 'only investigated' 'due to headless verification'. Should now all be
renamed according to our discussion earlier (i.e. skybox doesn't match. lighting doesn't match.
etc.) 2. Open back up the tasks related to these that were supposed to get the fixes in, and
rephrase and empty/move Post-Task details if there are any. These are now fresh. 3. If it makes
sense, create new tasks. 4. Be open to creating NEW tasks based on investigations based on
subagents, given all the work of our conversations today." No questions pending; the /goal loop
runs unattended. Do not ask the Director.


## Relevant History 

Created 2026-11-23 as a NEW task. It grows out of today's discussion about oracles: the Director
asked "What is an oracle?", noted no perfect "what is correct" oracle exists yet, offered a
modified-over-time imported project as a "normal ranges" reference, then supplied the city--1
counterexample (same black sun tint / intensity 1 renders fine because the 2D Skybox lights
materials) and said to continue WITHOUT an oracle using "lighting doesn't match" phrasing.

From that, a parallel-session investigation produced the skill reference
`references/engine-schema-conformance.md` ("Engine schema conformance for generated scene
components"): the installed engine's OWN authored `.scene` files and `Sandbox.Engine.xml` are a
usable external oracle for component SHAPE (key sets, `__version`, value ranges, property
semantics) even when no oracle exists for perceptual correctness. Its worked
`Sandbox.DirectionalLight` example names the hardcoded `SkyColor "0,0,0,0"`, absent `Intensity`
and absent `__version` as objective headless-checkable defects. This task generalises that
finding across every emitted component type.


## Stylistic Task Notes:
References to variables like X and Y automatically imply that these are to be exposed properties for developer tweaking.
When programming, portability and DRY are a priority.

## Post-Task Follow Ups 
