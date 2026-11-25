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

While work is being done, the following oracle data is available:
Unity play screenshots to compare:
GameObject Name: Japanese School Camera (1), Screenshot Location: /Testing/Unity-Screenshot-Outside-Facing-School-Camera-1.png.
GameObject Name: Japanese School Camera (2), Screenshot Location: /Testing/Unity-Screenshot-Outside-Facing-Sun-Camera-2.png.
GameObject Name: Japanese School Camera (3), Screenshot Location: /Testing/Unity-Screenshot-Inside-Facing-Hallway-Camera-3.png.
GameObject Name: Japanese School Camera (4), Screenshot Location: /Testing/Unity-Screenshot-Outside-Facing-Windows-Camera-4.png.
These screenshots were taken from 4 cameras in the scene, enable one each after importing into sbox to get validation from in-game sbox screenshots.
Unity project we're importing: C:\cseliot\1-Development\4-Unity\2-Projects\old_school_model

### Master label (2026-11-25 19:12): the Director-supplied oracle data above is on disk under `/Testing/` (gitignored, so absent from `git ls-files`) and gates this task. The four Unity play screenshots are the reference. The matching sbox four-camera play capture already exists at EVID/reimport-skybox-tint-play/play-camera1.png .. play-camera4.png (controlled play capture, one Japanese School Camera enabled at a time with IsMainCamera=true, genuine screenshot_highres frames); a second set is at EVID/reimport-skybox-phase4/. Those are Phase 3 / Phase 4 builds, not a fresh import; placement geometry is unaffected by sky or camera post-processing, but that provenance is recorded. No DONE? change - this only makes the oracle usable by a worker.


### Master note (2026-11-25, worker recon - diagnosis record, NOT a verdict)
Headless derivation in `task_journal/03-worker-phase-1.md` shows the generated world transforms for Restroom_Floor_02 and Outdoor_Floor_01 match independently derived Unity world transforms to within 8.0e-7 / 6.1e-6; Flowerbeds_03_LOD00 DOES exist in the generated scene; and no inactive ancestor explains an absence. The literal "wrong/missing in transform" premise is therefore not supported by the current generated data. The reported symptom is still OPEN: common.md says a symptom task is finished when the symptom is GONE, not when a spec is satisfied, and the Director still reports it. Live candidates are the name-target mismatch (from M1), mirror-variant handling (new M7) and the perceptual comparison artefact (M6). This note does not set DONE?.



### Work Review

2026-11-25 19:40 - Verdict: no further changes

SCOPE: This review covers ONLY the two M7 hunks in `Editor/Core/UnityPrefabExport.cs` - the renderer model selection in `ModelRoot` (~:1159) and the MeshCollider model selection in `WrapperRoot` (~:1270). It explicitly EXCLUDES the unshipped Phase 4 code: the +4/+173/+20 deltas in `UnityImport.cs`, `UnitySceneExport.cs`, `UnityVolume.cs`, the untouched Phase 4 `foreach ( var cameraComponent in UnitySceneExport.Camera(...) )` line at ~:1504 of this same file (observed in the diff, not reviewed), and the Director's `eliots-agentics/common.md` edit. No code was edited, staged, committed or pushed; DONE? unchanged.

1. [Low] (a) `ModelThrough` exists with the assumed signature and behaviour. `string ModelThrough( int[] mirror, UnityAsset model, string mesh, string plain )` is a private `Converter` method at `Editor/Core/UnityPrefabExport.cs:949-953`; both new call sites pass `(RootMirror, model, mesh, Context.ModelFor( model, mesh ))`, matching it exactly. It returns `plain` unless `IsMirror( mirror )` (:725) is true, then calls `Context.MirroredModelFor?.Invoke( model, mesh, UnityPrefabExport.MirrorAxis( mirror ) ) ?? plain`. `MirroredModelFor` is `Func<UnityAsset, string, int, string>` (:711), wired at `UnityImport.cs:541` to the local function at `UnityImport.cs:459-470`, which demand-generates and caches a `<name>_mirror_<axis>.vmdl`. The same helper already serves the scene renderer paths (:1467, :1526), so no new dependency. Fix: none needed.

2. [Low] (b) `RootMirror` is in scope and correct at BOTH call sites. It is a private `Converter` property at :918 derived from `readonly int MirrorAxis` (:797) set by the constructor (:872-877); `ModelRoot` (:1149) and `WrapperRoot` (:1197) are methods of that same class. A mirrored wrapper variant is converted via `MirroredPrefabFor` -> `Convert( asset, context, axis )` (`UnityImport.cs:480`, `UnityPrefabExport.cs:1814-1818`), so `RootMirror` has exactly one -1 and both model paths request the mirror VMDL. Plain conversion and `ConvertModel` (:1844-1849) use the default `MirrorAxis = -1`, giving `NoMirror`. `RootMirror` matches the parallel `Source.RootPending` formula (:1021). Fix: none needed.

3. [Low] (c) Non-mirrored prefabs cannot change, and no null or wrong collider is produced. With `RootMirror == NoMirror`, `IsMirror` is false and `ModelThrough` returns the exact `Context.ModelFor( model, meshName )` value the original expression produced. The `meshName == null ? null : ...` guard (:1270), `renderers[0].Path` fallback and warning (:1271-1272) are untouched; `ModelThrough` falls back to `plain` on any mirror miss (:952, `UnityImport.cs:461`), so it cannot return null where the old code did not. The mirror VMDL is a byte-for-byte copy of the plain VMDL except one `mirror_<axis> = false` -> `true` line (independently diffed on disk: `floor_03_snaps014` differs only at :101, `flowerbeds_03_snaps014` only at :104), so hull, geometry and material slots are preserved; the wrapper's collider and renderer point at the same mirror model (floor_03 prefab :33/:58, restroom_floor_02 :33/:58, flowerbeds_03 :34/:59). Fix: none needed.

4. [Low] (d) Wrapper-root guard and Converter invariants preserved. The diff changes only the two model-selection expressions plus a comment. `hasWrapperRootOverride` / `wrapperRoot` (:1589-1603, :1622-1625), `Key`-derived GUIDs (:877, :1176, :1183), `rendererOf` and renderer dedup (:1160-1173), the null-model warning (:1165-1169), material-slot remap (:1226-1249) and collider fallbacks/counts (:1271-1295) are all untouched. Fix: none needed.

5. [Low] (e) No unhandled mirror axis, LOD or material-slot interaction. `Unmirror` (:739-762) owes no axis, exactly one axis, or one x-mirror plus a 180-degree rotation for three negatives; two negatives fold into the rotation (:749-753), so `MirrorAxis( int[] )` (:727) always returns a real 0..2 index whenever `IsMirror` holds, and `MirroredModelFor` maps it with `"yzx"[axis]` / `"xyz"[axis]` (`UnityImport.cs:465-466`). Verified on disk: x-axis variants carry `mirror_y = true`, z-axis variants `mirror_x = true`. LOD meshes are ordinary `RenderedMeshes`/`explicitMeshes` entries (:1151-1154) and each receives a mirror model on demand (the flowerbeds mirror_z VMDL exists). Material slots are index-based (:1233) and the mirror VMDL keeps slot order. The separate name-target mismatch (M1) and the visual placement symptom are outside these two hunks and remain for the Director's Q3 judgement; this verdict covers only the headless M7 objective defect. Fix: none needed.

6. [Low] Objective symptom observed gone on disk (critic.md evidence rule): fresh mirror prefabs name `_mirror_<axis>.vmdl` files that did not exist at baseline; plain-vs-mirror diffs excluding `__guid` are non-empty (386/422/398 bytes) and contain only `__guid`/`Model` lines; 21 mirror VMDLs emitted (baseline 0); harness build `0 Warning(s) / 0 Error(s)`; `verify_output.py` `checked 862 files, 0 problems`; scoped generated-output diff 42 lines, all mirror-related, 0 non-mirror lines. Re-read directly from `EVID/m7-fix/out`, `*.log` and `*.txt`, not from the report.

Tree: 0e30c439ab5872c5a536b48260e3f2bd0469d2c8  Editor/Core/UnityPrefabExport.cs
`git hash-object --path=Editor/Core/UnityPrefabExport.cs Editor/Core/UnityPrefabExport.cs`


## Clarification Q&A

Kickoff batch 02 / 03 (Director, 2026-11-25, chat; full record in eliots-agentics/archive/sessions/):
An (Q1, verbatim): "Adopt as baseline: commit those 4 files by explicit path in ONE baseline commit so every task diff and critic review is against a clean HEAD. Consequence: fog removal, directional scale and wrapper-root guard become official HEAD." Landed c91e87b.
An (Q2, verbatim): "ignore the git diff"; clarified: "And also, what I meant by 2: Ignore what you learned from the git diff of session.md".
An (Q3, verbatim): "The skybox and tree color you should be able to discern from a screenshot. the others you'll have to marke done yes i guess because that's on me to finalize."
An (Q4, verbatim): "Yes: run the full clean re-import loop (launch editor, open scene, play, capture two play frames against Testing/Unity-Screenshot-Outside.png). Consequence: costs a full editor open/compile/play cycle per verification round."
This task is one of "the others": closes with an Open: line (Q3), unless the Director rules it done directly.

An (2026-11-25 19:46, TIMED-OUT clarify - relayed by Signal + ntfy; PROVISIONAL DEFAULT C adopted, awaiting Director correction): Q1 "The objects M7 fix is reviewed and verified, but committing it is blocked: UnityPrefabExport.cs also carries your unshipped Phase 4 foreach line, and committing that one file alone would break the build against HEAD's UnitySceneExport.cs. How should I commit the remaining Core work?" Options A) ship Phase 4 together with the reviewed fixes as one commit of all four Core files (recommended, keeps the critic's Tree manifest exact); B) set Phase 4 aside on clean HEAD and commit only the reviewed fixes (overrides the no-checkout/restore constraint); C) keep all Core code unstaged and continue evidence work without committing. Outcome: TIMED OUT, no answer. Default adopted: C. This is recorded, not silently shipped.
SUPERSEDED 2026-11-25 19:58 by the Director's reply in chat / Signal reply file: **1A** - "ship Phase 4 together with the reviewed fixes as one commit of all four Core files. Name Phase 4 explicitly in the commit message and run the build gate on that exact tree, so the critic's Tree hash is the committed artifact. Not 1B ... Not 1C: unstaged Core code blocks every terminal verdict on LOD and objects." Provisional default C is WITHDRAWN. All Core code will be committed as one, Phase 4 named in the message, build gate re-run on the exact tree.

## Relevant History 

Reopens archived task 14 "Objects Are Misplaced" (eliots-agentics/archive/tasks/14 Objects Are Misplaced.md; DONE? NO!, critic verdict "no further changes" 2026-11-24 22:23). It reported a 67/67 independent placement audit and six gates standing, but Restroom_Wall_01 (3) remained misplaced/rotated and was not identifiable in OUTDOOR-main-640.jpg; untested channels were rotation/scale basis, mirror variants, and rotation/scale nested overrides. New symptom (2026-11-25) names Flowerbeds_03_LOD00 and Floor_03 missing, Restroom_Floor_02 and Outdoor_Floor_01 in wrong locations, with the collider much more correct. The wrapper-root override guard in UnityPrefabExport.cs is now baseline (c91e87b). Prior evidence: C:/Users/Eliot/AppData/Local/hermes/profiles/hermes-sbox/cache/scratch/taskmaster-nov22/outdoor-compare/COMPARISON-2026-11-24.md and the archived differential reports. Baseline c91e87b; HEAD was 2c17e22 when this task opened and is bf099d4 now.

## Stylistic Task Notes:
References to variables like X and Y automatically imply that these are to be exposed properties for developer tweaking.
When programming, portability and DRY are a priority.

## Post-Task Follow Ups 
