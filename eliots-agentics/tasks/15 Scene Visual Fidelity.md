## Status: STARTED? NO! CLARIFIED? NO! HISTORY? NO! REVIEWED? NO! DONE? NO! WORKER: NONE OTHER: fresh backlog from resolve-errors-part-c (scene issues 1-4); Director Q4 = 1A

## Task
Verify the four scene-visual conversion issues and report any headless-verifiable gap.

### Description
Created from the 2026-11-22 console-out-2 triage in `resolve-errors-part-c-research.md`
(scene issues 1-4). Full Unity-side investigation: scratch `taskmaster-nov22/part-c-unity.md`.
The Unity source project is C:/cseliot/1-Development/4-Unity/2-Projects/old_school_model.

The Director's four issues: (1) "Fog in sbox doesn't match unity fog (is there one in the unity
scene?)"; (2) "Skybox doesn't match."; (3) "Sun (directional light) doesn't match.";
(4) "The trees aren't the correct color."

Verified source facts and current importer behaviour at HEAD 54ca0a6:
1. Fog - both scenes have `RenderSettings.m_Fog: 0` (no legacy fog), but HDRP fog exists in
   Volumes: `VolumeSettings.asset` ExponentialFog (grey, 200 m) and `Scene Settings Profile
   MergeV2.asset` newer Fog (meanFreePath 400) + VolumetricFog. Importer emits one
   `Sandbox.Fog` per scene (`UnitySceneExport.cs:275-300`, `UnityVolume.cs:20-22,29-36`),
   values 7874 / 15748 units. ANSWER to "is there one in the unity scene?": YES, HDRP Volume
   fog, not RenderSettings fog.
2. Skybox - no `RenderSettings` skybox (`fileID: 0`); HDRP sky is PhysicallyBased
   (VolumeSettings, wins the MergeV2 merge) and HDRI `spruit_sunrise_8k.exr` (Test_Range).
   Importer always writes `DefaultSky` with a tint (`UnitySceneExport.cs:260-269,307-317`).
   Procedural/HDRI parity was deliberately closed as "default sky + report" in task 09.
3. Sun - MergeV2 has a 20,000-lux directional sun (CCT 3849) + 12-lux fill; Test_Range a
   5-lux warm light. Importer converts units via `UnityHdrpLighting.cs:58-80` (1000 cd/unit,
   clamp 8) and emits `Sandbox.DirectionalLight` at `UnitySceneExport.cs:668-674`.
   Photometric units/clamp are documented approximations from task 10.
4. Trees - assets `Tree_01/Leaf .mat`, FBX, prefabs, albedo TGA. Colour drivers:
   base-colour map/tint/alpha-cut (`UnityMaterial.cs:29,202-204,285-290,36-43`), metallic
   remap already fixed by task 08, plus unverified vertex-colour and prefab-override candidates.

Research verdict: NO confirmed headless-verifiable conversion gap at HEAD for any of the four.
Editor appearance in the s&box editor is NOT provable headlessly.

Required work:
1. Re-verify each of the four claims against the Unity source and the current importer output
   with evidence ON DISK (source asset excerpts, generated `.scene` / `.vmat` excerpts, and a
   scratch comparison script). Confirm or refute the "no headless gap" verdict.
2. If a real headless-verifiable gap IS found (e.g. a wrong value, a dropped Volume override,
   a material that loses its albedo), fix it in the engine-free importer core and re-run the
   gates. Fix the root cause, not the symptom. If none is found, do NOT invent one.
3. Check the archived HDRP tasks 09/10/11 in `eliots-agentics/archive/` for already-recorded
   decisions about procedural sky, HDRI texture keys, physical light units and auto exposure;
   do not overpromise a fix that those tasks deliberately reported as unsupported.
4. Regression gates IF code changes land: harness build 0 errors / 0 warnings;
   `building.unitypackage` (24 files, no --scenes) and `city--1.unitypackage` (1,166 files,
   no --scenes) byte-identical to a fresh HEAD-code baseline with BOTH trees populated;
   `verify_output.py <outRoot> <outRoot>/imported` 0 problems; two identical runs
   byte-identical; BOM-less. If NO code changes land, state that plainly and record the
   verification evidence instead.
5. Report per-issue: source fact, importer behaviour (file:line), headless verdict, and the
   editor-visual check that remains open.

Per Director Q4 = 1A (phone reply 2026-11-22 21:31), this task closes DONE? YES! with explicit
`Open: <editor check>` lines in ## Post-Task Follow Ups for the four visual comparisons, since
no agent-runnable check remains once the headless verification is complete. Do NOT claim the
visual mismatches are fixed; that is the Director's call in the editor.

Do not add unrequested tooling. Stage by explicit path. No push until the Master verifies.

### Work Review


## Notes


## Clarification Q&A


## Relevant History 


## Stylistic Task Notes:
References to variables like X and Y automatically imply that these are to be exposed properties for developer tweaking.
When programming, portability and DRY are a priority.

## Post-Task Follow Ups 
	Open: <Director's editor-visual check that fog matches the Unity HDRP fog>
	Open: <Director's editor-visual check that the skybox matches the Unity PhysicallyBased/HDRI sky>
	Open: <Director's editor-visual check that the sun direction/brightness matches Unity>
	Open: <Director's editor-visual check that tree colours match Unity>
