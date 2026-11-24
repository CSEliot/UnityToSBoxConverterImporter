## Status: STARTED? YES! CLARIFIED? YES! HISTORY? YES! REVIEWED? NO! DONE? NO! WORKER: sa-0-44a89c13 OTHER: Phase 1 read-only worker dispatched 2026-11-25; baseline c91e87b

## Task
Imported objects wrong/missing in transform.


### Description
Example:
Flowerbeds_03_LOD00 at:
UnityEditor.TransformWorldPlacementJSON:{"position":{"x":-27.930999755859376,"y":-0.3075700104236603,"z":0.2583700120449066},"rotation":{"x":0.0,"y":0.7071068286895752,"z":0.0,"w":0.7071068286895752},"scale":{"x":1.0,"y":1.0,"z":1.0}}
is missing or i cant find it, in the sbox export.

Floor_03 at: UnityEditor.TransformWorldPlacementJSON:{"position":{"x":-0.0012005999451503158,"y":-0.3000001907348633,"z":2.9491000175476076},"rotation":{"x":0.0,"y":0.0,"z":0.0,"w":1.0},"scale":{"x":-1.0,"y":1.0,"z":1.0}}
is missing or i cant find it, in the sbox export.

Restroom_Floor_02 IN SBOX is in the wrong location, definitely.
At: World Transform: {
  "Position": "-23.3464108,-862.09906,4.52517557",
  "Scale": "1,1,1",
  "Rotation": "0,0,-0.707106829,0.707106829"
}

Outdoor_Floor_01 IN SBOX is in the wrong location, definitely.
At SBOX World Transform: {
  "Position": "535.433594,393.700439,-11.2204437",
  "Scale": "1,1,1",
  "Rotation": "0,0,0,1"
}
NOTE: The collider is much more correct.

### Master note (2026-11-25, worker recon - diagnosis record, NOT a verdict)
Headless derivation in `### Worker Phase 1` (## Notes) shows the generated world transforms for Restroom_Floor_02 and Outdoor_Floor_01 match independently derived Unity world transforms to within 8.0e-7 / 6.1e-6; Flowerbeds_03_LOD00 DOES exist in the generated scene; and no inactive ancestor explains an absence. The literal "wrong/missing in transform" premise is therefore not supported by the current generated data. The reported symptom is still OPEN: common.md says a symptom task is finished when the symptom is GONE, not when a spec is satisfied, and the Director still reports it. Live candidates are the name-target mismatch (from M1), mirror-variant handling (new M7) and the perceptual comparison artefact (M6). This note does not set DONE?.

While work is being done, the following oracle data is available:
Unity play screenshot to compare: /Testing/Unity-Screenshot-Outside.png.
Unity project we're importing: C:\cseliot\1-Development\4-Unity\2-Projects\old_school_model

### Work Review


## Notes

Master plan (2026-11-25): symptom task - Flowerbeds_03_LOD00 and Floor_03 missing from the s&box export; Restroom_Floor_02 and Outdoor_Floor_01 in the wrong location; "the collider is much more correct". Phase 1 read-only diagnosis: resolve each named object in the Unity YAML and in the generated .prefab/.scene, including its activation chain and transform basis, and identify ONE candidate mechanism per symptom with a named falsifying observation. Phase 2 one implementer on Editor/Core with a HEAD baseline diff, the harness gate and an independent placement audit. Phase 3 regenerate Assets/imported from fixed HEAD, verify_output, editor play frame, critic review.
Decisions: baseline c91e87b (Q1=1A; includes the wrapper-root override guard in UnityPrefabExport.cs). Q2: no standing HDRP abandonment ban; scope comes from this task file. Q3: this task may close DONE? YES! with a self-contained Open: line for the Director's own final judgement; objective data defects must still be fixed headlessly. Q4=4B: full clean re-import loop with two editor play frames IS authorized and required. Oracle: Testing/Unity-Screenshot-Outside.png and Unity project C:/cseliot/1-Development/4-Unity/2-Projects/old_school_model.

### Diagnosis census (2026-11-25, READ-ONLY HYPOTHESES - none run)
Full record: C:/Users/Eliot/AppData/Local/hermes/profiles/hermes-sbox/cache/scratch/taskmaster-nov25/diagnosis-census.md, section 2. Line numbers are pinned to HEAD 8bb1eaa; re-grep before relying on them.
- M1 HYPOTHESIS: Flowerbeds_03_LOD00 is a renamed child LOD node folded into the converted parent, so no GameObject of that name exists (MergeV2.unity:135646-135668). OBS-M1 headless: a fresh generated prefab/scene containing a GameObject or Name exactly Flowerbeds_03_LOD00 falsifies M1.
- M2 HYPOTHESIS: negative-scale instances route through _mirror_axis variants; a missing/unselected variant makes the object absent while the collider still uses the unmirrored model (UnityPrefabExport.cs:730-761, UnityImport.cs:446-484). OBS-M2 headless: resolve the Floor_03 scene stub's prefab path; a plain prefab or a nonexistent variant falsifies M2 as stated.
- M3 HYPOTHESIS: a nested/root PrefabInstance transform override is dropped or applied in the wrong channel (UnityPrefabExport.cs:1592-1623, :1706-1753; basis at :222-228). OBS-M3 headless: independently derive Position/Rotation/Scale for the exact Restroom_Floor_02 and Outdoor_Floor_01 Unity instance chains and compare to their generated stubs; all six channels matching falsifies M3.
- M4 HYPOTHESIS: colliders use baked ancestor chains while visible stubs use override values, which would explain "the collider is much more correct" (UnityPrefabExport.cs:126-179, :473-493). OBS-M4 headless: extract both the visible ModelRenderer host transform and the collider transform for Outdoor_Floor_01 from one fresh output and compare with the same source-derived world transform; agreement falsifies M4.
- M5 HYPOTHESIS: an inactive ancestor makes the absence fidelity, not a defect (activation-fidelity rule; UnityPrefabExport.cs:1721). OBS-M5 headless: walk m_Father to the root for each named instance and cross-check the generated stub's Enabled/__enabled; every ancestor active and every stub enabled falsifies M5.
- M6 HYPOTHESIS: "wrong location" is a comparison artefact - a different same-named instance or a camera/reference mismatch (MergeV2.unity has many same-named instances). OBS-M6 perceptual: project the source-derived world position into a fresh play frame and identify that object by local geometry; landing at the projected point falsifies M6.
- OBS-M1 through OBS-M5 are headless YAML/JSON and independent transform derivations. Archived task 14's 67/67 and 154/0 figures are prior-pass records, not re-run here. None carries a verdict. Prior generated artifact paths predate HEAD 8bb1eaa.

### Worker Phase 1 (2026-11-25; TRUNCATED worker - findings transcribed by the MASTER)
Provenance: sa-0-44a89c13 hit its 40-call iteration cap (637.5 s) and wrote NO project file. This section was transcribed by the Task Master from its completion summary plus its scratch probe outputs, all verified present on disk: C:/Users/Eliot/AppData/Local/hermes/profiles/hermes-sbox/cache/scratch/taskmaster-nov25/phase1-scene-probe.py/.out (286,835 bytes), phase1-world-probe.py/.out, phase1-unity-instances.py/.out, phase1-parent-probe.py, phase1-derive.py/.out, phase1-prefab-probe.py/.out. Setup at worker time: HEAD 7c8a519, clean tree, 370 generated prefab/scene files, mergev2.scene mtime 2026-11-25 02:03; only generated TEXT read. No code edited, no editor launched, no staging/commit. Census line numbers were pinned to 8bb1eaa - re-grep before relying on them.

OBS-M1 - RUN; FALSIFIER CONDITION MET; M1 FALSE AS STATED. Source MergeV2.unity:135646-135668: instance 849709595 renames its root to `Flowerbeds_03_Snaps014 (1)` (:135656) and separately renames child `Flowerbeds_03_LOD00` (:135664-135667). Generated mergev2.scene:78782 holds `"Property": "Name", "Value": "Flowerbeds_03_LOD00"` targeting prefab guid 0b9748e2..., mapped at :78788 to instance guid 271179fd... - the name DOES exist in the generated scene, on the instance root, while the prefab root keeps `Flowerbeds_03_Snaps014`. A name-target mismatch is a separate Phase 2 candidate, not evidence the object is missing.

OBS-M2 - RUN; FALSIFIER CONDITION NOT MET; M2 REMAINS HYPOTHESIS. `Floor_03` stub uses `imported/school/art/prefab/structure/floor/floor_03_snaps014_mirror_x.prefab`, which exists. NEW OBJECTIVE OBSERVATION: `diff` of plain vs `_mirror_x` prefabs (excluding `__guid`) is EMPTY for Floor_03, Restroom_Floor_02 and Flowerbeds_03; no `_mirror_*.vmdl` exists (the five "mirror" model files are an unrelated `mirror_01` asset). Code: `UnityPrefabExport.cs:1157` and `:1268` call `Context.ModelFor`, bypassing `ModelThrough` (:949-953) and `MirroredModelFor` (`UnityImport.cs:454-465`); the mirror sign split from `Unmirror` (:739-761) is dropped for wrapper prefabs. Log as new **M7 HYPOTHESIS**: mirror variants are emitted as identical prefabs with no mirrored model, so a negative-scale instance renders unmirrored. Falsifier OBS-M7: a fresh mirror prefab referencing a `_mirror_<axis>.vmdl` with the mirror modifier set.

OBS-M3 - RUN; FALSIFIER CONDITION MET FOR BOTH NAMED OBJECTS; M3 FALSE AS STATED. Independent YAML derivation (6589 blocks, 2861 PrefabInstances; parents 1125265808 at :175047, 829418666 at :131528, 29758579 at :3922 are identity transforms). Restroom_Floor_02, instance 1020339436 (:158597): Unity world (21.89736, 0.11493969, -0.593), q (0, 0.7071068, 0, 0.7071068), s (-1,1,1) -> ToSbox P(-23.3464, -862.099, 4.52518) R(0,0,-0.707107,0.707107) S(1,1,1); generated stub 52c39bd3-... matches ALL SIX channels (delta P 8.0e-7). Outdoor_Floor_01 reported point comes from TWO Unity instances 732102719 (:119530) and 1548424615 (:249214), local (-10.000011, -0.28499985, 13.60004), identity rotation/scale -> ToSbox P(535.434, 393.7, -11.2204) R identity S identity; generated scene has exactly TWO stubs there, c973220f-... and fc1e3aba-... (delta P 6.1e-6).

OBS-M4 - RUN; FALSIFIER CONDITION MET; M4 FALSE AS STATED. Generated `outdoor_floor_01_snaps014.prefab`: one host GameObject holds both `Sandbox.ModelRenderer` (:33) and `Sandbox.BoxCollider` (:50-74) at local identity; the collider `Center "78.74,-78.74,0"`, `Scale "157.48,157.48,0"` is a baked shape offset from Unity `!u!65 &1112693558352971108` (`Outdoor_Floor_01_Snaps014.prefab:3-4`), NOT a different transform path. The scene stub adds no collider override. "The collider is much more correct" is not explained by divergent transforms.

OBS-M5 - RUN; FALSIFIER CONDITION MET; M5 FALSE. Activation walk: Flowerbeds (External/Props), Floor_03 (Floor/Structure), Restroom_Floor_02 (Restroom/Props), Outdoor_Floor_01 (Floor/Structure) are all active to root; only two inactive GameObjects exist scene-wide (`Post-process Volume` :151210, `Setting` :319092), neither an ancestor. Generated prefab roots `Enabled: true`, stubs carry no `Enabled` override.

M6 - remains HYPOTHESIS, perceptual, NOT RUN. The source has many same-named `Outdoor_Floor_01` instances (including two at the reported point). No "wrong location" verdict was made.

Status after Phase 1: M1, M3, M4, M5 falsified by their named observations; M2 not falsified; M7 newly logged as HYPOTHESIS; M6 unrun. The generated transforms are faithful to independently derived Unity world transforms for both named objects, so the reported symptom is NOT explained by a transform defect in current output. The symptom stays OPEN (common.md: finished when the symptom is GONE, not when a spec is satisfied). DONE? stays NO!. PERMISSION for Phase 2 was requested and is HELD by the Master because LOD Phase 2 is in flight and implementation is serialised.

## Clarification Q&A

Kickoff batch 02 / 03 (Director, 2026-11-25, chat; full record in eliots-agentics/session.md):
An (Q1, verbatim): "Adopt as baseline: commit those 4 files by explicit path in ONE baseline commit so every task diff and critic review is against a clean HEAD. Consequence: fog removal, directional scale and wrapper-root guard become official HEAD." Landed c91e87b.
An (Q2, verbatim): "ignore the git diff"; clarified: "And also, what I meant by 2: Ignore what you learned from the git diff of session.md".
An (Q3, verbatim): "The skybox and tree color you should be able to discern from a screenshot. the others you'll have to marke done yes i guess because that's on me to finalize."
An (Q4, verbatim): "Yes: run the full clean re-import loop (launch editor, open scene, play, capture two play frames against Testing/Unity-Screenshot-Outside.png). Consequence: costs a full editor open/compile/play cycle per verification round."
This task is one of "the others": closes with an Open: line (Q3).

## Relevant History 

Reopens archived task 14 "Objects Are Misplaced" (eliots-agentics/archive/14 Objects Are Misplaced.md; DONE? NO!, critic verdict "no further changes" 2026-11-24 22:23). It reported a 67/67 independent placement audit and six gates standing, but Restroom_Wall_01 (3) remained misplaced/rotated and was not identifiable in OUTDOOR-main-640.jpg; untested channels were rotation/scale basis, mirror variants, and rotation/scale nested overrides. New symptom (2026-11-25) names Flowerbeds_03_LOD00 and Floor_03 missing, Restroom_Floor_02 and Outdoor_Floor_01 in wrong locations, with the collider much more correct. The wrapper-root override guard in UnityPrefabExport.cs is now baseline (c91e87b). Prior evidence: C:/Users/Eliot/AppData/Local/hermes/profiles/hermes-sbox/cache/scratch/taskmaster-nov22/outdoor-compare/COMPARISON-2026-11-24.md and the archived differential reports. Baseline c91e87b; HEAD 2c17e22 removed the old standing constraints.

## Stylistic Task Notes:
References to variables like X and Y automatically imply that these are to be exposed properties for developer tweaking.
When programming, portability and DRY are a priority.

## Post-Task Follow Ups 
