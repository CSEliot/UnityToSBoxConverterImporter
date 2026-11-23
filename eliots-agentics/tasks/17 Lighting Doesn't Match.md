## Status: STARTED? YES! CLARIFIED? YES! HISTORY? YES! REVIEWED? NO! DONE? NO! WORKER: NONE OTHER: fresh symptom task; split from investigated-only task 15 per Director bedtime instructions; read-only differential ablation (sa-1-fbca556e) in flight

## Task
Lighting doesn't match.

### Description
Director's symptom (2026-11-22, deliberately restated 2026-11-23): originally "Sun (directional
light) doesn't match", now "lighting doesn't match". The Director explicitly withdrew the pinned
values ("Sun sky color is black and 1 intensity") after the city--1 counterexample: that imported
scene has the same black sun tint / intensity 1 and renders fine because the 2D Skybox lights the
materials. Do NOT diagnose from those pinned values alone. Acceptance check: the imported
MergeV2 / Test_Range scenes must be lit like the Unity source. The symptom is finished when it is
GONE, not when a spec is satisfied. Do not set DONE? YES! while it is still reported.

Split fresh 2026-11-23 from task 15 "Scene Visual Fidelity" (closed b11952e), a verification-only
task that declared "no headless gap" against its own converter. common.md forbids that as a
verdict. The old table is in ## Relevant History for reference only.

Unity source: C:/cseliot/1-Development/4-Unity/2-Projects/old_school_model. Generated scenes:
Assets/imported/assetpackschoolnameverylong/schoolhdrpnameverylong/scenes/{mergev2,test_range}.scene.
Working case: Assets/imported/polygoncity/scenes/demo.scene (city--1). A 2026-11-23 Dev-editor
capture of MergeV2 (scratch `taskmaster-nov22/mergev2-viewport.png`) showed a flat medium-dark
gray, under-lit viewport: no sun, no sky detail, no shadows, no shading gradients, faint geometry
silhouettes.

Required method:
1. For EACH candidate mechanism, write down the named observation that would FALSIFY it. No
   "investigated" verdict without both a mechanism and its falsifier.
2. Use the engine as an EXTERNAL ORACLE (the importer's own output is not one): engine-authored
   `.scene` files under `<engine>/addons/`, `templates/`, `samples/`, `download/`, and the managed
   API XML docs `<engine>/bin/managed/Sandbox.Engine.xml`. Procedure and worked
   `Sandbox.DirectionalLight` shape are in the skill reference
   `references/engine-schema-conformance.md`. Diff key sets in BOTH directions, check
   `__version` on EVERY generated light, read XML docs for `Light.Intensity`,
   `Light.LightColor`, `Light.ComponentVersion`, `UpgradeDirectionalLightTint`,
   `UpgradeLegacyTint`, `DirectionalLight.SkyColor`, `AmbientLight`.
3. Candidate mechanisms (verify, do not assume):
   a. OBJECTIVE DATA DEFECT (verified against engine source): `Editor/Core/UnitySceneExport.cs:673`
      hardcodes `["SkyColor"] = "0,0,0,0"` for EVERY generated directional light, emits no
      `Intensity` key, and folds intensity INTO `LightColor` (mergev2 sun
      `7.82938,4.77047,2.59516,1`). Engine source
      `C:/cseliot/1-Development/1-SBox/1-Engine-Builds/Dev/sbox-public/engine/Sandbox.Engine/Scene/Components/Light/Light.cs:18,144-172,204-223`
      proves `LightColor` and `Intensity` are SEPARATE serialized properties and rendered colour =
      `LightColor * Intensity * normalized Temperature`; the folding occurs at
      `UnitySceneExport.cs:658-693`. Falsifier: if generated `LightColor` equalled the source tint
      with a sibling `Intensity` equal to source intensity and the product preserved, the defect
      is gone. Two premises from this task's first draft were FALSIFIED 2026-11-23 and must NOT be
      used: (i) authored `LightColor` is NOT limited to 0..2 — authored directional lights reach
      3.96 and point lights ~36.89; (ii) `Light.ComponentVersion=1` upgraders
      (`Light.cs:35-45`) only set `LightColor` when it is MISSING (#E9FAFF), and generated lights
      already have `LightColor`, so the "unversioned light re-defaults to black tint on load"
      mechanism is FALSE in this engine build. The separation of `Intensity` remains the real,
      headless-checkable schema misuse.
   b. The engine upgrade path (`Light.ComponentVersion` "Versions light defaults so existing
      scenes retain their original tint") re-defaults unversioned lights. Falsifier: an
      unversioned generated light in an engine-authored scene that still displays its authored
      tint/intensity.
   c. Missing `Sandbox.AmbientLight`: the conformance reference notes the recommended ambient
      contribution is a separate `AmbientLight` component, while we hardcode `SkyColor` black and
      rely on `SkyBox2D`. Falsifier: a working engine-authored scene whose directional light
      also has black `SkyColor` and no `AmbientLight` yet is well lit (city--1 may be this case) —
      then the cause is the sky/ambient system, not the light block.
   d. Differential ablation: compare the WORKING city--1 `demo.scene` lighting system to the
      FAILING MergeV2 `mergev2.scene` component by component (DirectionalLight blocks,
      SkyBox2D, Fog, EnvmapProbe count, AmbientOcclusion, tonemapping/exposure). Identify which
      difference would cause the symptom. A component's values are not the cause when the same
      values render correctly in another scene.
   e. Photometric conversion: `Editor/Core/UnityHdrpLighting.cs:58-80` uses 1000 cd/unit with a
      clamp of 8; MergeV2's 20,000-lux sun clamps to 8. Check whether the clamp discards the
      source intent. Falsifier: source lux values vs emitted `LightColor`/`Intensity` for a case
      where the clamp materially changes the rendered brightness.
4. Screenshot evidence: no editor is currently up (both installs' `--list` return `[]`); launch
   the editor belonging to the install you use, read `editor_status` BEFORE capture, do not
   save/play/change if `SceneHasUnsavedChanges` is true. Capture the viewport, copy the PNG to
   scratch, downscale to ~960x540 JPEG, and LOOK at it with vision_analyze. Before/after pair
   around any fix. Measure brightness with pixel numbers, not adjectives. State the bridge and
   evidence used.
5. If a mechanism survives, fix the ROOT CAUSE in the engine-free importer core, fix the CLASS.
   If all are falsified, say so with observations and name the next candidate; do not close on
   self-consistency. Do not file an objective data defect as an `Open:` editor-visual check.
6. Regression gates IF code changes land: harness build 0/0; `building.unitypackage` (24 files)
   and `city--1.unitypackage` (1,166 files) byte-identical to fresh HEAD baseline (no --scenes,
   BOTH trees populated); `verify_output.py <outRoot> <outRoot>/imported` 0 problems; two
   identical runs byte-identical; BOM-less; no stray artifacts.
7. Put all evidence (component diffs, XML doc excerpts, scene excerpts, screenshots, pixel
   measurements, gate logs) in scratch files and cite absolute paths in ## Notes.

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
subagents, given all the work of our conversations today."

ANSWERED (Director, 2026-11-23, oracle counterexample): city--1 has the same black sun tint /
intensity 1 yet renders fine because the 2D Skybox lights materials; "we need to continue without
an oracle for now ... Instead of stating specifically: 'Sun sky color is black and 1 instensity'
I'll just say 'lighting doesn't match' and leave it to you to investigate correctly." No
questions pending; the /goal loop runs unattended. Do not ask the Director.


## Relevant History 

Previously part of task 15 "Scene Visual Fidelity" (closed b11952e), verification-only. At HEAD
6a65cb1 the worker recorded: MergeV2 20,000-lux sun (CCT 3849) + 12-lux fill, Test_Range 5-lux
warm light; `UnityHdrpLighting.cs:31,34,58-80,132-135` (1000 cd/unit, clamp 8);
`UnitySceneExport.cs:652-695` emits `Sandbox.DirectionalLight`; output sun
`7.82938,4.77047,2.59516,1` (20 -> clamp 8), fill `0.012,0.00683,0.00332,1`; verdict "no gap";
critic 2026-11-22 22:10 `no further changes`. Task 10 (archived) recorded the unit/clamp as a
documented approximation.

WHY THIS IS NOT A FIX: "no gap" compared emitted values to our own converter. The Director still
reports lighting doesn't match, and a Dev-editor capture confirmed a flat under-lit viewport.
The four `Open:` lines from old task 15 were cleared on reopening; the sun/lighting symptom got
its own task under the Director's new name. A parallel-session investigation on 2026-11-23
produced `references/engine-schema-conformance.md`, which identifies the hardcoded
`SkyColor "0,0,0,0"`, missing `Intensity` and missing `__version` as objective headless-checkable
defects with the engine's own authored scenes as oracle.


## Stylistic Task Notes:
References to variables like X and Y automatically imply that these are to be exposed properties for developer tweaking.
When programming, portability and DRY are a priority.

## Post-Task Follow Ups 
