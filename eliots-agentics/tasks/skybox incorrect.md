## Status: STARTED? NO! CLARIFIED? YES! HISTORY? YES! REVIEWED? NO! DONE? NO! WORKER: NONE OTHER: kickoff 2026-11-25; batch 02 answered; baseline c91e87b

## Task
Skybox incorrect

### Description
importer doesn't set correct skybox. Or the skybox in unity isn't working referenced or imported at all. But it appears to be the default.

While work is being done, the following oracle data is available:
Unity play screenshot to compare: /Testing/Unity-Screenshot-Outside.png.
Unity project we're importing: C:\cseliot\1-Development\4-Unity\2-Projects\old_school_model

### Work Review


## Notes

Master plan (2026-11-25): symptom task - "importer doesn't set correct skybox ... it appears to be the default". Q3 makes this agent-runnable: it must be judged from a rendered frame against Testing/Unity-Screenshot-Outside.png, not from serialized JSON. Phase 1 read-only diagnosis: what sky material / SkyColor / sky component the generated scene emits, what the Unity source actually authors (HDRP Volume sky, skybox material), and an engine-authored s&box artifact of the same __type for schema conformance. Phase 2 one implementer on Editor/Core with a HEAD baseline diff and harness gate. Phase 3 regenerate Assets/imported from fixed HEAD, run the full clean re-import loop, capture two editor play frames, compare them to the Unity screenshot, critic review.
Decisions: baseline c91e87b (Q1=1A; the generated scene now omits Sandbox.Fog). Q2: no standing HDRP abandonment ban; scope comes from this task file and the Director's fresh instruction. Q3: skybox IS discernible from a screenshot - close DONE? YES! only after a rendered frame comparison; no Open: line substitutes for that. Q4=4B: full clean re-import loop with two editor play frames IS authorized and required.

### Diagnosis census (2026-11-25, READ-ONLY HYPOTHESES - none run)
Full record: C:/Users/Eliot/AppData/Local/hermes/profiles/hermes-sbox/cache/scratch/taskmaster-nov25/diagnosis-census.md, section 3. Line numbers are pinned to HEAD 8bb1eaa; re-grep before relying on them.
- S1 HYPOTHESIS: SkyBox2D.SkyMaterial is hardcoded to materials/skybox/skybox_day_01.vmat for every scene, so no source sky asset can appear (UnitySceneExport.cs:34, :287-292, :312-317; MergeV2.unity:29 has m_SkyboxMaterial fileID 0; ImportPlan.cs:146-153 excludes it). OBS-S1 headless: a fresh mergev2.scene with any source-derived SkyMaterial falsifies S1.
- S2 HYPOTHESIS: the only authored sky colours are white HDRP PhysicallyBased tints, so the emitted Tint is white and cannot carry the dusk colour (VolumeSettings.asset:33-35 skyType 4; UnityVolume.cs:333-341; UnitySceneExport.cs:291). OBS-S2 headless: a non-white source horizonTint/zenithTint with a generated Tint still 1,1,1,1 falsifies S2.
- S3 HYPOTHESIS (shared with tree-colour T6): the sky-band and whole-frame gap comes from the light rig / exposure, not the sky material, so sky and tree colour would shift together (COMPARISON-2026-11-24.md:35-40, :64-69; UnitySceneExport.cs:258-283, :711; UnityHdrpLighting.cs). OBS-S3 perceptual: hold SkyMaterial and Tint fixed, change only light-rig/exposure, re-measure the matched sky band; no movement toward the reference means S3 false.
- S4 HYPOTHESIS: the reference screenshot was rendered from a different serialized state (baked GI / inactive live light), so a faithful conversion cannot reproduce its dusk sky (MergeV2.unity:53 m_EnableBakedLightmaps: 1; archived 16:145-148, :159-167 records LightingData.asset, *_comp_light.exr and a 20,000-lux sun under an inactive Setting root). OBS-S4 headless for files plus activation chain, perceptual for the render-state proof: enumerate the lighting assets and resolve the sun's activation chain; no baked GI with an active serialized light falsifies S4. This is a hypothesis about the ORACLE, not a licence to re-enable a disabled light or invent intensity.
- S5 HYPOTHESIS: the bare-string SkyMaterial serialization is malformed and fails to load. OBS-S5 headless: compare a fresh generated SkyBox2D block field by field with the engine-authored reward-unbox.scene:3981-3994 block; the same bare-string form and field set falsifies S5.
- OBS-S1, S2, S4 (file/activation portion) and S5 are headless. OBS-S3 and the Q3 gate are perceptual: two play frames and a matched sky-region crop against Testing/Unity-Screenshot-Outside.png. Prior contrast (not re-measured): top-12% sky band Unity meanRGB (56.2,68.1,78.7) vs s&box (125.5,146.8,165.2); whole-frame mean 48.54 vs 100.16, warm 0.0532 vs 0.3737 (COMPARISON-2026-11-24.md:35-38, :68-69, :124). No authored "correct sky" asset exists for MergeV2; a dark-sky choice would be a NEW mapping, not a recovered source value. None carries a verdict.

## Clarification Q&A

Kickoff batch 02 / 03 (Director, 2026-11-25, chat; full record in eliots-agentics/session.md):
An (Q1, verbatim): "Adopt as baseline: commit those 4 files by explicit path in ONE baseline commit so every task diff and critic review is against a clean HEAD. Consequence: fog removal, directional scale and wrapper-root guard become official HEAD." Landed c91e87b.
An (Q2, verbatim): "ignore the git diff"; clarified: "And also, what I meant by 2: Ignore what you learned from the git diff of session.md".
An (Q3, verbatim): "The skybox and tree color you should be able to discern from a screenshot. the others you'll have to marke done yes i guess because that's on me to finalize."
An (Q4, verbatim): "Yes: run the full clean re-import loop (launch editor, open scene, play, capture two play frames against Testing/Unity-Screenshot-Outside.png). Consequence: costs a full editor open/compile/play cycle per verification round."
This task is agent-runnable from a screenshot (Q3), not an Open: line task.

## Relevant History 

Reopens archived tasks 15 "Fog Doesn't Match" and 16 "Skybox Doesn't Match" (eliots-agentics/archive/). Task 16's critic (2026-11-24 22:23) returned "changes required": the sky gap is visibly present (s&box clear pale-blue daylight with thin cloud streaks vs Unity hazy deep navy/blue-grey twilight with a pale horizon band), the "SkyMaterial bare-string" premise was FALSIFIED, and closing on "HDRP abandoned" was rejected as an excuse. The old standing constraints behind that closure were deliberately removed from HEAD (2c17e22 "cleanup"), and the Director's Q2 answer says to ignore what was learned from the session.md diff, so no abandonment ban applies. The generated scene now omits Sandbox.Fog (baseline c91e87b). Prior evidence: C:/Users/Eliot/AppData/Local/hermes/profiles/hermes-sbox/cache/scratch/taskmaster-nov22/outdoor-compare/COMPARISON-2026-11-24.md and the archived differential report.

## Stylistic Task Notes:
References to variables like X and Y automatically imply that these are to be exposed properties for developer tweaking.
When programming, portability and DRY are a priority.

## Post-Task Follow Ups 
