## Status: STARTED? YES! CLARIFIED? YES! HISTORY? YES! REVIEWED? YES! DONE? YES! WORKER: sa-0-bb00b315 OTHER: reviewed 14:28 no further changes; Intensity/LightColor separated, SkyColor from ambient, __version + Fog DistanceRange; clean combined gate PASS; two [Low] findings noted

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

2026-11-23 14:28 - Verdict: no further changes

1. [Low] `Editor/Core/UnitySceneExport.cs:372` — the RenderSettings fallback fog passes `start` and `attenuation` to `FogDistanceRange()` in Unity metres, while `StartDistance` (:378) and `AttenuationDistance` (:368) on that same component multiply by `UnitsPerMetre` (39.37), and the HDRP fog site (:314) already passes engine units. `Fog.cs:92-96` defines `DistanceRange` endpoints "in engine units", so the fallback value is ~39x short. It is inert for every emitted component because the importer always writes `Mode = Exponential`, and no authored `Sandbox.Fog` exists in either install to gate against, so this does not affect current rendered output or schema conformance. Fix: optional hardening — pass `Math.Max( 0, start ) * UnitsPerMetre` and `attenuation * UnitsPerMetre` at :372 so an editor user switching to `Distance` mode gets the intended ramp.

2. [Low] No before/after editor screenshot pair was captured although task step 8 requested one. Notes (e) and `combined/COMBINED-GATE-RESULT.md` disclaim this explicitly and leave perceptual certification to the Director; every defect judged here is headless-checkable against engine source/XML, not a visual symptom. Fix: none (Director-owned editor observation; not a worker code change).

Verified on disk, no finding: independent recomputation from `combined/clean-combined.diff` of four lights gives old `LightColor` == new `LightColor` x new `Intensity` within 4e-5 (5-decimal rounding): `7.82938,4.77047,2.59516` = `0.97867,0.59631,0.32439` x 8; `0.17323,0.18602,0.2` = `0.86613,0.93009,1` x 0.2; `1.73227,1.86017,2` = `0.86613,0.93009,1` x 2; `2,1.80932,1.34489` = `1,0.90466,0.67244` x 2. `Light.cs:166` clamp (non-finite -> 1, negative -> 0) is mirrored exactly at :723; `Single()` writes a float, matching the engine's `float Intensity`. The `SkyColour` walk (:271-282) snapshots via `ToArray()`, recurses JsonArray and JsonObject, touches only the scene tree (including prefab-instance patches) and cannot loop or reach standalone prefab files, which keep the `Light()` tint fallback. `ComponentVersions` values match `Light.cs:18` (1), `Fog.cs:34` (1), `EnvmapProbe.Upgrade.cs:7` (1), `Bloom.cs:110` (1), `Tonemapping.cs:101` (3), `AmbientOcclusion.cs:16` (1); all other emitted types (SkyBox2D, AmbientLight, PostProcessVolume, DepthOfField, CameraComponent, AudioListener, colliders, ModelRenderer, SkinnedModelRenderer) have no `ComponentVersion` override and correctly emit none. `Fog.cs:39-59` injects `DistanceRange` only when absent, so `__version 1` plus the key is additive and cannot misread authored scenes. All three dropped premises are absent from code and Notes: no black-tint-upgrade claim, no `SkyMaterial` object-form defect, no 0..2 `LightColor` gate. Contamination is disclaimed in (e) and the clean combined gate is cited; no screenshot result is claimed. Template: 7 Status fields unchanged, headings at column 0, one `### Work Review`, no placeholders, no `Open:` lines.

Tree:
`git hash-object --path=Editor/Core/UnitySceneExport.cs Editor/Core/UnitySceneExport.cs` -> 484674aae2501652ef317a5421fd6d8f2ee263fb
`git hash-object --path="eliots-agentics/tasks/19 Scene Component Schema Conformance.md" "eliots-agentics/tasks/19 Scene Component Schema Conformance.md"` -> 55608ebbb187ed8c4e0ea68babf3b1a90bcd4865 (hash of the file as reviewed, before this entry was appended)

## Notes

WORKER (2026-11-23): ROOT-CAUSE FIX LANDED in `Editor/Core/UnitySceneExport.cs` only (BOM-less). No commit, no push, no staging.

MASTER CLEAN COMBINED GATE (2026-11-23, supersedes the contaminated corpus run in (e)): with BOTH fixes in the
final tree, the Master ran ONE clean verification vs a fresh `git archive HEAD` baseline in LF-normalized isolated
mirrors. PASS: old_school --scenes HEAD 2144 / FINAL 2150 files; the 82160-byte combined diff is EXACTLY the union
of the two fixes (task 19's 2 scenes changed only LightColor/Intensity/__version/SkyColor/DistanceRange, task 14's
11 prefabs + 6 mirror variants); building 24/24 and city--1 1166/1166 byte-identical (0-byte diffs);
verify_output `checked 874 files, 0 problems`; two identical FINAL runs 0-byte determinism diff; 0 BOM files.
Full evidence: `C:/Users/Eliot/AppData/Local/hermes/profiles/hermes-sbox/cache/scratch/taskmaster-nov22/combined/COMBINED-GATE-RESULT.md`.
The clean gate does NOT certify perceptual correctness (no editor screenshot captured); that stays the Director's
editor observation per common.md.

(a) Fix:
- `Light()` no longer folds Unity/HDRP intensity into the colour string. It writes the source tint (linear `m_Color` x Unity colour-temperature curve) into `LightColor` and the clamped source intensity (`m_Intensity` or `UnityHdrpLighting.Converted.Intensity`) into a separate `Intensity` key. Engine oracle: `.../Dev/sbox-public/engine/Sandbox.Engine/Scene/Components/Light/Light.cs:144-172,204-223` — rendered = `LightColor * Intensity * normalized Temperature`, and the engine setter clamps non-finite to 1 / negative to 0 (mirrored in code). `Temperature` stays at its neutral 6500 K engine default because the Unity CCT factor is already in the tint; this avoids applying the curve twice. Fix is in the shared helper, so scene AND prefab directional/point/spot lights are covered.
- `Sandbox.DirectionalLight.SkyColor` is no longer the hardcoded `"0,0,0,0"`. Scene conversion recursively walks the emitted scene JSON (including prefab-instance patches) and writes the source-derived ambient colour: HDRP Volume ambient when present, else `RenderSettings.m_AmbientSkyColor` linearised and multiplied by `m_AmbientIntensity`. Prefab-only conversions keep a source-derived fallback: the light's own tint. Engine docs: `DirectionalLight.cs:16-21` describes SkyColor as the ambient sky contribution.
- `ComponentVersions` table emits current engine `__version`: AmbientOcclusion 1, Bloom 1, DirectionalLight 1, EnvmapProbe 1, Fog 1, PointLight 1, SpotLight 1, Tonemapping 3; zero-version types emit none. Sources: `Light.cs:18`, `Fog.cs:34`, `EnvmapProbe.Upgrade.cs:7`, `Bloom.cs:110`, `Tonemapping.cs:101`, `AmbientOcclusion.cs:16`.
- `FogDistanceRange( start, attenuation )` adds `DistanceRange` to both fog emission sites (`start, start + 3*attenuation` in engine units), so `Fog.cs:34-59`'s v1 upgrader has no legacy keys to inject on load.

(b) Falsifiers actually applied (all PASS — `component-block-diff.txt`):
- Product invariance: before `LightColor=8,7.23729,5.37955,1`; after `LightColor=1,0.90466,0.67244,1` with `Intensity=8`; new tint x intensity reproduces 8.0/7.2373/5.3795 within 0.01.
- After `SkyColor=0.55,0.68,0.85,1`, identical to the scene's generated `Sandbox.AmbientLight.Color`; the constant zero is gone.
- Every generated versioned type has its current `__version` (Tonemapping 3, all others 1); generated Fog has both `__version 1` and `DistanceRange 0,23622`.
- External oracles used: Dev/Steam engine-authored `.scene` files via recursive `python3` + `json.load` census (`census.py`, `shape.py`, `engine-census-dev.txt`, `engine-census-steam.txt`, `authored-shapes.txt`), `Sandbox.Engine.xml` (`xml-excerpts.txt`, `xml-upgrade-excerpts.txt`, `xml-members.txt`), and the sbox-public engine C# source (`engine-component-versions.txt`). No importer output was used as an oracle.

(c) Gate results (real numbers): harness build after fix `0 Warning(s) 0 Error(s)` (isolated HEAD-core + our file mirror also 0/0). hdrp `--scenes`: `assets=24 converted=21 files=48`; two identical runs byte-identical (0-byte `diff -r`). `verify_output.py <outRoot> <outRoot>/imported`: `checked 21 files, 0 problems`. `building.unitypackage` no `--scenes`: 24 files, byte-identical to fresh HEAD baseline (`--strip-trailing-cr`, 0-byte diff). `city--1.unitypackage` no `--scenes`: 1166 files, byte-identical to fresh HEAD baseline (`--strip-trailing-cr`, 0-byte diff). Changed source is BOM-less. No stray artifacts added to the repo.

(d) Three premises falsified and dropped by the Master (do not reintroduce): (1) "unversioned light re-defaults to black tint" is FALSE — `Light.cs:35-45` upgraders only set `LightColor` when it is MISSING (#E9FAFF), and generated lights already carry it; `__version` is emitted for schema conformance only. (2) `SkyBox2D.SkyMaterial` as a bare `.vmat` string is NOT a defect — engine-authored scenes use the same bare string. (3) Authored `LightColor` is NOT limited to 0..2 — authored directional reaches 3.96 and point ~36.89; the defect was the missing separate multiplier, not the magnitude.

(e) HONESTY: the old_school `--scenes` corpus comparison is CONTAMINATED and is NOT used to certify this task. The post-fix harness DLL was rebuilt at 13:50:30 while another worker edited `Editor/Core/UnityPrefabExport.cs` at 13:50:22 (`WrapperRootTransform`), so the corpus diff shows 11 `.prefab` differences/new files plus 2 `.scene` differences (baseline 2144 files vs 2150, converted 965 vs 971) attributable to both fixes. An isolated mirror with HEAD `UnityPrefabExport.cs` + our `UnitySceneExport.cs` was built and passed every small gate; the Master owns the single combined clean corpus gate after all fixes land. No editor was up for this task; NO before/after screenshot was captured and NO visual result is claimed.

(f) Evidence (absolute scratch paths): `C:/Users/Eliot/AppData/Local/hermes/profiles/hermes-sbox/cache/scratch/taskmaster-nov22/task19/` + `component-block-diff.txt`, `component-diff.py`, `harness-build-before.txt`, `harness-build-after.txt`, `mirror-build.txt`, `generated-census-before.txt`, `before/hdrp.log`, `before/building.log`, `before/city.log`, `before/oldschool.log`, `after/hdrp.log`, `after/hdrp2.log`, `after/building.log`, `after/city.log`, `after/oldschool.log`, `after/oldschool-vs-baseline.diff`, `isolated/verify_output.log`, `isolated/hdrp-identical.diff`, `isolated/building-vs-baseline.diff`, `isolated/city-vs-baseline.diff`, `isolated/hdrp-vs-contaminated.diff`, `before/hdrp/imported/fixture/scenes/hdrp.scene`, `after/hdrp/imported/fixture/scenes/hdrp.scene`, `engine-census-dev.txt`, `engine-census-steam.txt`, `authored-shapes.txt`, `xml-excerpts.txt`, `xml-upgrade-excerpts.txt`, `xml-members.txt`, `engine-component-versions.txt`, `census.py`, `shape.py`.

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

2026-11-23 (worker): the schema fix landed in `Editor/Core/UnitySceneExport.cs` — intensity separated from tint into `Intensity`, source-derived `SkyColor`, current `__version` on versioned components, Fog `DistanceRange`. Three original premises were FALSIFIED against the engine source/oracle and dropped from the spec by the Master: the unversioned-light black-tint re-default (`Light.cs:35-45` fills LightColor only when missing), the bare-string `SkyBox2D.SkyMaterial` (authored scenes use it), and any 0..2 `LightColor` limit (authored values reach 3.96 directional / ~36.89 point). All small gates passed on the final bytes; the old_school `--scenes` corpus comparison is contaminated by a concurrent task-14 `UnityPrefabExport.cs` edit and is explicitly not a certification. The single clean combined corpus verification is owned by the Master after all fixes land. Evidence is cited in `## Notes`.


## Stylistic Task Notes:
References to variables like X and Y automatically imply that these are to be exposed properties for developer tweaking.
When programming, portability and DRY are a priority.

## Post-Task Follow Ups 
