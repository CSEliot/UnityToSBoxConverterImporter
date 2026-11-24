## Status: STARTED? NO! CLARIFIED? YES! HISTORY? YES! REVIEWED? NO! DONE? NO! WORKER: NONE OTHER: kickoff 2026-11-25; batch 02 answered; baseline c91e87b

## Task
Imported trees are wrong color. 

### Description
Imported trees are greenish, unity trees are a kind of red-ish. Imported trees should match unity.


While work is being done, the following oracle data is available:
Unity play screenshot to compare: /Testing/Unity-Screenshot-Outside.png.
Unity project we're importing: C:\cseliot\1-Development\4-Unity\2-Projects\old_school_model

### Work Review


## Notes

Master plan (2026-11-25): symptom task - "Imported trees are greenish, unity trees are a kind of red-ish. Imported trees should match unity." Q3 makes this agent-runnable: it must be judged from a rendered frame against Testing/Unity-Screenshot-Outside.png, not from serialized JSON. Phase 1 read-only diagnosis: locate the tree materials/textures in the Unity source and in the generated .vmat/.vmat references, compare every channel (including alpha) against the source decoded by Pillow, and check whether the difference is an asset defect, a material remap, or lighting/sky downstream. Phase 2 one implementer on Editor/Core with a HEAD baseline diff and harness gate. Phase 3 regenerate Assets/imported from fixed HEAD, run the full clean re-import loop, capture two editor play frames, compare them to the Unity screenshot, critic review.
Decisions: baseline c91e87b (Q1=1A). Q2: no standing HDRP abandonment ban; scope comes from this task file and the Director's fresh instruction. Q3: tree colour IS discernible from a screenshot - close DONE? YES! only after a rendered frame comparison; no Open: line substitutes for that. Q4=4B: full clean re-import loop with two editor play frames IS authorized and required.

### Diagnosis census (2026-11-25, READ-ONLY HYPOTHESES - none run)
Full record: C:/Users/Eliot/AppData/Local/hermes/profiles/hermes-sbox/cache/scratch/taskmaster-nov25/diagnosis-census.md, section 4. Line numbers are pinned to HEAD 8bb1eaa; re-grep before relying on them.
- T1 HYPOTHESIS: the importer drops or mis-maps the leaf albedo / material tint (Tree_Leaf_Snaps014.mat:45, :285-287; UnityMaterial.cs:285-290). OBS-T1 headless: decode the source leaf TGA and the generated .vmat TextureColor with Pillow, compare EVERY channel including alpha pixel-for-pixel; a match falsifies T1.
- T2 HYPOTHESIS: a red vertex-colour stream on the tree FBX is dropped (archived 18:77 found three LayerElementColor streams, all RGB (0,0,0)). OBS-T2 headless: probe the current Tree_01_Snaps014.FBX colour streams; no nonzero RGB stream falsifies T2.
- T3 HYPOTHESIS: per-instance m_Materials overrides are dropped (prior census of 78 flora overrides found name/transform only). OBS-T3 headless: enumerate m_Materials.Array overrides on every current Tree_01/Tree_Brick instance and compare with generated slot assignments; no source override and all slots resolving to leaf/trunk .vmat falsifies T3.
- T4 HYPOTHESIS: a 16-bpp TGA normalization or inverted-alpha rewrite corrupts the leaf texture (UnityTgaRewrite.cs; UnityImport.cs:202). SOURCE OBSERVATION ONLY, performed this pass: Tree_Leaf_01_Albedo_Snaps014.tga header is type 2, 1024x2048, 32 bpp, descriptor 8, i.e. not the rewritten variant. OBS-T4 headless: channel-by-channel source-vs-generated comparison including alpha extrema and per-pixel mismatch count; every channel matching falsifies T4.
- T5 HYPOTHESIS: metallic/smoothness remapping darkens or desaturates the foliage (UnityMaterial.cs:298-301). OBS-T5 headless: read g_flMetalness/smoothness from a fresh leaf .vmat against the current source .mat and remap bounds; consistency with the documented remap falsifies T5.
- T6 HYPOTHESIS (shared with skybox S3): the green-to-burgundy gap is a light-rig/exposure/tonemap outcome, not an asset defect; the unityimporterplus-reimport skill says calibrate the LIGHT RIG, never the albedo (leaf albedo imports at about (53,63,33) dark olive). OBS-T6 perceptual: hold leaf texture and .vmat fixed, change only sun/sky/exposure, measure the leaf crop hue; no movement toward burgundy falsifies T6.
- T7 HYPOTHESIS: the reference frame's tree colour comes from baked GI or a light inactive in the serialized scene (MergeV2.unity:53; archived 16:159-167). OBS-T7 headless for files/activation chain, perceptual for render-state proof; no baked contribution with a matching serialized state falsifies T7.
- T8 HYPOTHESIS: a different red-foliage source asset exists and is not in the converted chain (prior git census found pure R100 moves, identical albedo blobs, no burgundy asset). OBS-T8 headless: census current source asset names and references for any red/burgundy foliage; finding one falsifies T8.
- OBS-T1-T5, T8 and the file/activation portion of T7 are headless; OBS-T6 and the Q3 gate are perceptual. Prior contrast (not re-measured): Unity courtyard trees BURGUNDY/MAROON/RED-PURPLE vs s&box dark olive-green (COMPARISON-2026-11-24.md:71-73). No absolute authored red-canopy value exists in the current source chain; inventing a red tint is a NEW mapping, not a recovered source value. None carries a verdict.

### Master-run headless observation (2026-11-25, OBSERVATION not verdict)
Evidence: C:/Users/Eliot/AppData/Local/hermes/profiles/hermes-sbox/cache/scratch/taskmaster-nov25/oracle-state-observation-2026-11-25.md. On the CURRENT MergeV2.unity (mtime 2026-11-25 00:32): m_EnableBakedLightmaps: 1 at line 53, LightingData.asset present, 71 *_comp_light.exr files, and the 20,000-lux Lux directional light sits under the INACTIVE 'Setting' root, so it is OFF in the serialized scene; the other active directional light is only 12 lux. This is the headless portion of OBS-T7. The render-state proof - that the reference screenshot came from this baked state - and the scene identity remain unverified. Partial OBS-T8: no red/burgundy foliage asset by NAME in Assets/School/Art (only Interior_Wal_Red_Snaps014.mat, a wall material); a full name-plus-reference census is still owed. s&box has no lightmap support, so do NOT fake the missing baked sunlight by enabling an inactive light or mapping an unrelated sky/celestial intensity field into Intensity. The Director's dated Q3 instruction governs the acceptance bar; this is reported for his decision, not used to override it.

## Clarification Q&A

Kickoff batch 02 / 03 (Director, 2026-11-25, chat; full record in eliots-agentics/session.md):
An (Q1, verbatim): "Adopt as baseline: commit those 4 files by explicit path in ONE baseline commit so every task diff and critic review is against a clean HEAD. Consequence: fog removal, directional scale and wrapper-root guard become official HEAD." Landed c91e87b.
An (Q2, verbatim): "ignore the git diff"; clarified: "And also, what I meant by 2: Ignore what you learned from the git diff of session.md".
An (Q3, verbatim): "The skybox and tree color you should be able to discern from a screenshot. the others you'll have to marke done yes i guess because that's on me to finalize."
An (Q4, verbatim): "Yes: run the full clean re-import loop (launch editor, open scene, play, capture two play frames against Testing/Unity-Screenshot-Outside.png). Consequence: costs a full editor open/compile/play cycle per verification round."
This task is agent-runnable from a screenshot (Q3), not an Open: line task.

## Relevant History 

Reopens archived task 18 "Trees Wrong Colour" (eliots-agentics/archive/18 Trees Wrong Colour.md; critic "changes required" 2026-11-24 22:23). Its own vision read: s&box dark olive-green rounded crowns vs Unity deep burgundy/red-purple crowns; cause recorded OPEN; all tree-colour mechanisms UNTESTED; assets located; likely downstream of lighting/fog. A prior critic rejected closing a visibly present symptom from serialized JSON alone. The old standing constraints were removed from HEAD (2c17e22 "cleanup") and Q2 says to ignore what was learned from the session.md diff. Prior evidence: C:/Users/Eliot/AppData/Local/hermes/profiles/hermes-sbox/cache/scratch/taskmaster-nov22/outdoor-compare/COMPARISON-2026-11-24.md and the archived probes report.

## Stylistic Task Notes:
References to variables like X and Y automatically imply that these are to be exposed properties for developer tweaking.
When programming, portability and DRY are a priority.

## Post-Task Follow Ups 
