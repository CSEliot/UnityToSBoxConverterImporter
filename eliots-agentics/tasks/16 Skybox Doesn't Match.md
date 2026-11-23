## Status: STARTED? NO! CLARIFIED? YES! HISTORY? YES! REVIEWED? NO! DONE? NO! WORKER: NONE OTHER: fresh symptom task; split from investigated-only task 15 per Director bedtime instructions

## Task
Skybox doesn't match.

### Description
Director's verbatim symptom (2026-11-22): "Skybox doesn't match." Acceptance check: the imported
MergeV2 / Test_Range scenes must show a sky that matches the Unity source's sky. The symptom is
finished when it is GONE, not when a spec is satisfied. Do not set DONE? YES! while it is still
reported.

Split fresh 2026-11-23 from task 15 "Scene Visual Fidelity" (closed b11952e), a verification-only
task that declared "no headless gap under the task-09 contract". That compared our output to our
own documented approximation and is self-consistency, not correctness. common.md forbids it as a
verdict. The old table is in ## Relevant History for reference; do not reuse its conclusion.

Unity source: C:/cseliot/1-Development/4-Unity/2-Projects/old_school_model. Generated scenes:
Assets/imported/assetpackschoolnameverylong/schoolhdrpnameverylong/scenes/{mergev2,test_range}.scene.

Required method:
1. For EACH candidate mechanism, write down the named observation that would FALSIFY it. No
   "investigated" verdict without both a mechanism and its falsifier.
2. Candidate mechanisms (verify, do not assume):
   a. `Editor/Core/UnitySceneExport.cs:34` hardcodes
      `const string DefaultSky = "materials/skybox/skybox_day_01.vmat"` and writes it as
      `SkyBox2D.SkyMaterial` for EVERY scene (`:266`, `:315`) regardless of the Unity source sky.
      Source has no `RenderSettings` skybox (`m_SkyboxMaterial: {fileID: 0}`); HDRP sky is
      skyType 4 PhysicallyBased (VolumeSettings, wins the MergeV2 merge) and skyType 1 HDRI
      `spruit_sunrise_8k.exr` (Test_Range). Emitting a default day skybox for a sunrise HDRI
      scene would produce a sky that does not match. Falsifier: read both source scenes' sky
      settings; if the importer emits a source-derived sky material/path instead of the constant,
      this mechanism is false.
   b. `SkyMaterial` is written as a bare string path. Engine-authored `SkyBox2D` blocks store a
      resource object (`{"Id":..., "Path":...}`) — see `references/engine-schema-conformance.md`
      and the sbox-scene-prefab-inspection skill. A shape the engine does not deserialize is an
      objective data defect, headless-checkable. Falsifier: diff the generated `SkyMaterial`
      key/value shape against engine-authored `.scene` `SkyBox2D` blocks; if they match, false.
   c. `SkyIndirectLighting: true` and `Tint 1,1,1,1` are emitted for both scenes. In city--1 the
      2D Skybox lights materials and the scene renders fine; in MergeV2 it does not. Falsifier:
      compare the working city--1 `demo.scene` `SkyBox2D` block to MergeV2's; if the blocks are
      equivalent, the skybox component is not the differentiator and the cause lies elsewhere
      (see tasks 15/16). Do not pin a component value as the cause when the same value renders
      correctly in another scene.
   d. Task 09 deliberately closed procedural sky / HDRI parity as "default sky + report" with an
      unverified HDRI texture key. Re-examine that decision: the symptom is still reported, so
      "we decided not to fix it" is not acceptance. Falsifier: an engine-authored scene that
      uses an HDRI sky material with a verified texture key, proving the key exists and can be
      emitted.
3. Differential ablation (no absolute oracle): `Assets/imported/polygoncity/scenes/demo.scene`
   is a WORKING case from the same importer. Enumerate what differs in the sky/ambient system
   between it and MergeV2 and identify which difference would cause the symptom.
4. Screenshot evidence: no editor is currently up (both installs' `--list` return `[]`); launch
   the editor belonging to the install you use, read `editor_status` BEFORE capture, do not
   save/play/change if `SceneHasUnsavedChanges` is true. Capture the viewport, copy the PNG to
   scratch, downscale to ~960x540 JPEG, and LOOK at it with vision_analyze. Before/after pair
   around any fix. Corroborate specific sky claims with a crop or pixel measurement. State the
   bridge and evidence used.
5. If a mechanism survives, fix the ROOT CAUSE in the engine-free importer core, fix the CLASS.
   Do not overpromise procedural-sky or HDRI parity that the engine cannot deliver; but a
   hardcoded default material where a source-derived one is available IS a fixable defect. If
   all are falsified, say so with observations and name the next candidate; do not close on
   self-consistency. Do not file an objective data defect as an `Open:` editor-visual check.
6. Regression gates IF code changes land: harness build 0/0; `building.unitypackage` (24 files)
   and `city--1.unitypackage` (1,166 files) byte-identical to fresh HEAD baseline (no --scenes,
   BOTH trees populated); `verify_output.py <outRoot> <outRoot>/imported` 0 problems; two
   identical runs byte-identical; BOM-less; no stray artifacts.
7. Put all evidence in scratch files and cite absolute paths in ## Notes.

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

Previously part of task 15 "Scene Visual Fidelity" (closed b11952e), verification-only. At HEAD
6a65cb1 the worker recorded: `m_SkyboxMaterial: {fileID: 0}` both scenes; skyType 4
PhysicallyBased (`VolumeSettings.asset:33-35`), skyType 1 HDRI (`Scene Settings Profile
MergeV2.asset:1872/1874`, `spruit_sunrise_8k.exr`); importer writes `DefaultSky` with a tint
(`UnitySceneExport.cs:34,260-269,305-317`); both scenes emit 1 `SkyBox2D`,
`SkyMaterial=materials/skybox/skybox_day_01.vmat`, `Tint 1,1,1,1`; verdict "no gap under task-09
contract"; critic 2026-11-22 22:10 `no further changes`. Task 09 (archived) deliberately closed
procedural sky as "default sky + report" with the HDRI texture key left unverified.

WHY THIS IS NOT A FIX: "no gap under our own contract" is self-consistency. The Director still
reports the skybox doesn't match. The four `Open:` lines from old task 15 were cleared on
reopening and the skybox symptom got its own task.


## Stylistic Task Notes:
References to variables like X and Y automatically imply that these are to be exposed properties for developer tweaking.
When programming, portability and DRY are a priority.

## Post-Task Follow Ups 
