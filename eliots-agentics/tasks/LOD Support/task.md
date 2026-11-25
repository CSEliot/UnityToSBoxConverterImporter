## Task
We got z-fighting which I believe is due to no implementation of LODs?

### Description
Examples:
In SBOX Editor: Floor_stairs_middle (1) at:
{
  "Position": "1255.90295,1413.38306,157.479996",
  "Scale": "1,1,1",
  "Rotation": "0,0,-1,0"
}
has all the LODs from unity just thrown together as components. That's not how sbox handles LoDs.

If you need an "oracle" for correctly writing LODs in S&Box say so. But try first.


While work is being done, the following oracle data is available:
Unity play screenshots to compare:
GameObject Name: Japanese School Camera (1), Screenshot Location: /Testing/Unity-Screenshot-Outside-Facing-School-Camera-1.png.
GameObject Name: Japanese School Camera (2), Screenshot Location: /Testing/Unity-Screenshot-Outside-Facing-Sun-Camera-2.png.
GameObject Name: Japanese School Camera (3), Screenshot Location: /Testing/Unity-Screenshot-Inside-Facing-Hallway-Camera-3.png.
GameObject Name: Japanese School Camera (4), Screenshot Location: /Testing/Unity-Screenshot-Outside-Facing-Windows-Camera-4.png.
These screenshots were taken from 4 cameras in the scene, enable one each after importing into sbox to get validation from in-game sbox screenshots.
Unity project we're importing: C:\cseliot\1-Development\4-Unity\2-Projects\old_school_model


### Work Review

2026-11-25 02:57 - Verdict: changes required

Tree: Editor/Core/UnityImport.cs = c37c6ddacfdd4397bef3bddcfb49e8aa17081097
Tree: Editor/Core/UnityModel.cs = 957379ac9bcfb03de94caf70f4752e16522935b0
Tree: Editor/Core/UnityLod.cs   = c385ce2189e1077aa37f93b608efeef239f683a2 (NEW/untracked; plain hash identical)

Scope reviewed: `git status --short` showed M Editor/Core/UnityImport.cs, M Editor/Core/UnityModel.cs, ?? Editor/Core/UnityLod.cs, plus M eliots-agentics/common.md. The common.md edit is the Director's separate live edit and is excluded, per instruction. `git diff HEAD` was the input for the two modified files; UnityLod.cs was read directly. No code was edited, no build/import/verify gate run, no editor launched, no Assets/imported touched, nothing staged.

1. [High] Symptom has no visual observation on disk - the z-fighting is a visual symptom, but the only frames named in the task file (frameA.png / frameB.png, captured 02:11 2026-11-25) are explicitly the current-HEAD BASELINE, and the task file states "the z-fighting verdict still needs a before/after play-frame pair on the same camera, which the parent will capture after the fix". The Director still reports the symptom. No after-fix rendered frame exists on disk. critic.md: a `no further changes` verdict is invalid unless it cites an observation of the symptom itself; where no visual evidence is supplied for a visual symptom the verdict is `changes required`. Fix: capture the Q4-authorised before/after play-frame pair on the same camera for the Floor_stairs_middle (1) instance and review it before closing. Evidence: task file "Re-import frame evidence" and "Worker Phase 2 verification" (no editor launched, "no perceptual z-fighting verdict"); critic.md:11.

2. [Medium] Mixed-LOD falsification is a probe-convention artifact (`.../lod-phase2/mixed-lod-check.txt`). The 343-FBX count was derived from HEAD-baseline generated VMDL mesh names; at HEAD `RenderedMeshes` returned LOD0-only, so non-LOD meshes in a mixed FBX could never appear. `0 mixed` is true by construction, not evidence. New `UnityModel.cs:120-129` drops every non-LOD mesh whenever any LOD-suffixed mesh exists, so a mixed FBX would silently lose geometry. Fix: re-count from source FBX mesh records, not generated VMDLs. The critic's own independent binary-FBX probes failed on this exporter's conventions - no independent count obtained.

3. [Medium] Sampled audit over 261 changed assets (`.../same-options-changed-common.txt`). 15 `.prefab` + 2 `.scene` un-audited; `scene-semantic-diff.py` exists and was not run. Residual risk: scene `PropertyOverrides` / `RemovedObjects` referencing renderer GUIDs removed by the 6->1 consolidation. Fix: run semantic JSON diff over all 17 non-VMDL files.

4. [Low] 3 lost neutral `.vmat` files - no dangling reference found. Fixed output: 0 references to the three names in any `.vmdl`/`.prefab`/`.scene`/`.vmat`; the three consolidated models remap every slot to real material files. Not a regression on available evidence; no HEAD slot-by-slot comparison was available.

5. [Low] `SwitchDistanceStep = 50` is engine-backed (`ModelDocApp.def:298` = `SetSwitchThreshold(nLod * 50.0f)`; level 0 -> 0.0 matches). The invented mapping is disclosed and exposed as properties, acceptable under "try first"; distances remain arbitrary and need a visual LOD-pop check.

6. [Low] `},{` junction is real tabs, tokens match engine form. Engine-authored `cardboard_carton_01.vmdl:147-149` writes `},` / `{` with a newline; generated line 133 uses tabs - same tokens. No engine same-line example and parser source absent; fixed output has not been through ModelDoc compile/verify_output. Fix: confirm with a compile of the fixed output.

Master transcription note (2026-11-25): findings 2-6 were returned in the capped critic's completion summary (40 api_calls) and transcribed by the Task Master at column 0; the critic wrote finding 1, the `Verdict:`, the `Tree:` lines and the scope note into this file before the cap. The async file-mutation verifier reported one FAILED scratch-probe write (`critic_fbx_mesh_census.py`, refill guard); the critic worked around it with `_v2` / `names_v3` / `names_v4` probes, and no deliverable was affected. `REVIEWED? YES!` describes the CURRENT reviewed bytes; any code fix sets it back to NO! and requires a scoped re-review by the SAME critic (master.md step 5 / unityimporterplus skill). No code was edited, no build/import/editor/git operation was performed by the critic.


### Work Review RE-CHECK (scoped, 2026-11-25)

2026-11-25 03:42 - Verdict: no further changes

Tree: Editor/Core/UnityImport.cs = c37c6ddacfdd4397bef3bddcfb49e8aa17081097
Tree: Editor/Core/UnityModel.cs = 2f3dad6e07a25c476095e65fd5bb0e5e15d12499
Tree: Editor/Core/UnityLod.cs   = c385ce2189e1077aa37f93b608efeef239f683a2 (NEW/untracked; plain hash identical)

Scope: scoped re-check, not a new full review. `git status --short` and `git diff HEAD` were run this turn; Core files remain unstaged. Only new code since the original verdict is `Editor/Core/UnityModel.cs:122-126` (`public string[] RenderedMeshes => Meshes.ToArray();`); import/Lod files are byte-identical to what I reviewed. `eliots-agentics/common.md` and `eliots-agentics/session-latest.md` are Director/Master runtime docs, excluded. Hashes above were computed with `git hash-object --path=<rel> <file>` this turn: all three match the Master's independent values; my original UnityModel.cs `957379ac...` line is superseded. No code, build, import, editor, stage or `Assets/imported` operation was performed.

1. [High] Symptom has no visual observation - ADDRESSED. `### Q4 after-fix evidence` puts two 1280x720 after-fix play frames on disk at `.../taskmaster-nov25/reimport-lod-fixed/frameA.png` and `frameB.png`, with a camera pose matched field-for-field to the baseline (main camera "Japanese School Camera", pos 1574.8002,-1456.69,629.92; rot 0.1479692,-0.06872844,-0.8948113,-0.41556507; FOV 70.00001; IsMainCamera true). Gates: harness 0 warnings / 0 errors, verify_output.py "checked 840 files, 0 problems", content build "937 compiles, 0 failed". I ran `vision_analyze` myself on BOTH frames: the small pale stepped landing near the left building has single continuous step edges, consistent light treads / darker risers, and no doubled contours, checkerboard/moire patches or torn coplanar fragments; the striping on the nearby canopy is roof slats, not step z-fighting. Honest limitation, which I confirm: these are wide aerial frames, the specific `Floor_stairs_middle (1)` instance is not identifiable, and temporal flicker is untestable from a still; the final perceptual call stays the Director's via the Q3 `Open:` line. The objective defect itself (6 enabled renderers drawing LOD0+LOD1) is fixed headlessly (1 ModelRenderer + compiled LOD-aware VMDL). This is the on-disk observation of the symptom critic.md:11 requires.

2. [Medium] Mixed-LOD falsification is a probe-convention artifact - ADDRESSED. My premise was wrong: HEAD's `Regex.IsMatch(n, @"(?i)(?:^|[_ .-])LOD0(?:$|[_ .-])")` cannot match `LOD00`, so HEAD `RenderedMeshes` returned all meshes, not LOD0-only; the Master's primary-source check found 60 non-LOD mesh names in HEAD generated VMDLs, 0 mixed HEAD VMDLs and 0 non-LOD names missing from the new output, so `0 mixed` is not artifactually forced. Independently, the latent geometry-loss risk is gone: `Editor/Core/UnityModel.cs:122-126` now returns `Meshes.ToArray()`, so no non-LOD mesh can be dropped by any filter. `.../lod-phase2/fix-vs-same-options.diff` reports `OLD=1047 NEW=1047 lost=0 gained=0 changed=0`, with a raw-byte comparison also 0 differing. I did not rerun or rely on my failed binary-FBX probes.

3. [Medium] Sampled audit over 261 changed assets - ADDRESSED. The Master ran a full semantic JSON audit with `C:/Users/Eliot/AppData/Local/hermes/profiles/hermes-sbox/cache/scratch/taskmaster-nov25/master_semantic_audit.py` over all 15 `.prefab` + 2 `.scene` changed files: every `__type` delta is the intended `Sandbox.ModelRenderer` reduction, and 0 scene GUID refs removed by the 6->1 consolidation are dangling (mergev2.scene 65 removed refs, test_range.scene 27, both 0 dangling). Correction to my original line: `scene-semantic-diff.py` was NOT found anywhere in scratch; it does not exist as a tool to run. The audit that exists is the Master's own script.

4. [Low] 3 lost neutral `.vmat` files - ADDRESSED (no defect found). My own prior check found 0 references to `counter01_snaps014_iup_unassigned`, `floor_stairs_bottom_snaps014_iup_unassigned` and `windows_03_frame_snaps014_iup_unassigned` in any fixed `.vmdl`/`.prefab`/`.scene`/`.vmat`, and the three consolidated models remap every slot to real material files. The fix run is byte-identical to the pre-fix run (1047/1047, 0 lost/gained/changed), so this remains clean.

5. [Low] `SwitchDistanceStep = 50` is invented - ADDRESSED. `engine/Definitions/modeldoc/ModelDocApp.def:298` is `pLodGroup->SetSwitchThreshold( nLod * 50.0f )`, so 50 m per level (level 0 -> 0.0) is the engine's own default; the mapping is disclosed as NEW and exposed as `UnityLod.FirstSwitchDistance` / `SwitchDistanceStep`, satisfying "try first" and the Stylistic Task Notes. The arbitrary-distance / LOD-pop judgement is explicitly carried by the Q3 `Open:` line for the Director; no code defect.

6. [Low] `},{` junction may be a schema defect - ADDRESSED. The real-tab junction compiled: content build "937 compiles, 0 failed", 253/253 regenerated LOD-aware `.vmdl` sources have compiled `.vmdl_c` siblings, and `sbox-dev.log` has no ModelDoc / `.vmdl` / LODGroup compile errors. The three `ERROR_FILEOPEN` lines are runtime loads of unrelated missing materials (`material #221`, lambert1, reward-unbox), not LOD compilation. No normalization needed.

No objective data defect remains open. DONE? unchanged.


### Work Review (LOD B scoped, 2026-11-25)

2026-11-25 20:32 - Verdict: no further changes

Tree: Editor/Core/UnityImport.cs = 004f31a0fc43c8257cb82ca153d0b802e7c2d9de

Scope: ONLY the two LOD B hunks in the committed `Editor/Core/UnityImport.cs` (`74a4f38`) - the `declaredSlots` expansion at `:349-360` and the declared-slot inheritance fallback at `:361-385`. EXCLUDED: the Phase 4 skybox hunks in the same file (`:145-146`, `:596-598`) and all Phase 4 skybox changes in `UnitySceneExport.cs` / `UnityVolume.cs` / `UnityPrefabExport.cs`, plus the M7 fix; those shipped in `74a4f38` but are outside this task's scope. Inputs: `git show 74a4f38 -- Editor/Core/UnityImport.cs`, `git status --short` (UnityImport.cs clean, no staged Core edit), the committed working-tree bytes, `git hash-object --path=Editor/Core/UnityImport.cs Editor/Core/UnityImport.cs`, `Editor/Core/UnityModel.cs:83-135`, `Editor/Core/UnityLod.cs:12-39`, and on-disk scratch evidence under `EVID/lod-b-fix/`. The header still says `WORKER: MASTER`, but per the task file's Phase 4 record the diff producer was Task Worker `sa-0-e48e4bd2`; this review judges the committed bytes, not a worker trail. No code, build, import, editor, stage or commit operation was performed.

1. [Low] (a) `declaredSlots` includes all distinct FBX-declared slot names and admits no unrelated package-material guesses. `UnityImport.cs:355-359` unions `info.Materials`, every `RendererSlots.Values.EffectiveSlots`, every `MeshSlotMaterials` key and `RenderedMaterials`, lowercased with `.vmat`; `:360` then removes any remap key outside that set. The package-wide name-matched guesses created at `:318-320` and the LOD-suffix variants at `:347-348` still die at `:360` unless the FBX itself declares the slot. Fix: none.

2. [Low] (b) The LOD>0 inheritance fallback is bounded and fails safe. `UnityImport.cs:370-384` runs only for a declared slot with no remap (`:371`), only when some owner renders at LOD>0 (`:373`), and only inherits a LOD0-exclusive slot of the same `UnityModel.LodBase` family that already has a remap (`:375-383`). With no peer, `peer == null` and no remap is written (`:384`); the slot stays unresolved and is handled by the pre-existing `missingSlots` / `needsNeutral` path (`:390`, `:416-429`), so no material is invented. Remaining theoretical edge: the donor is picked alphabetically (`:382`) and the family comes from `owners[0]` (`:374`), so a slot name shared across two LOD families, or several LOD0-exclusive remapped slots in one family, gives a deterministic but arbitrary target. Not realized on this corpus (only the windows pair changed, with a single donor `windows.vmat`), and a model-global remap table already forces one answer for a shared slot name. Fix: none required on the evidence; if such an FBX ever regresses, derive `family` from the LOD>0 owner rather than `owners[0]`.

3. [Low] (c) No behaviour change for non-LOD FBXs or models whose slots all resolve directly. `:371` skips every slot already in `remaps`; `:373` skips every slot without a LOD>0 owner. The `declaredSlots` superset can only retain remaps for slots the FBX actually declares (material objects, renderer bindings, per-mesh assignments), never bare package-material guesses. Independent check: `diff -rq` of `EVID/lod-b-fix/out2/Assets` (pre-final-fix, same options) against `EVID/lod-b-fix/final2/Assets` differs in exactly 2 files, both `windows_05_frame*` VMDLs. Fix: none.

4. [Low] (d) No null/NRE risk from `UnityModel.LodBase` or `UnityLod.Level` on mesh names without LOD digits. `UnityLod.cs:27-32` matches against `mesh ?? ""` and returns `null` without a suffix; callers at `UnityImport.cs:373,376,379` coalesce with `?? 0`. `UnityLod.cs:35` returns `BasePattern.Replace(mesh ?? "", "")`, never null, so `.Equals(family, ...)` at `:376,379` is safe; `owners[0]` at `:374` is read only after the `Any(...)` guard at `:373` proves the array non-empty. `RendererBinding.EffectiveSlots` falling back to `Mesh` (`UnityModel.cs:83-87`) predates this change. Fix: none.

5. [Low] (e) Generated-output evidence supports "only 2 VMDLs changed". I ran `diff -rq` on `EVID/lod-b-fix/out2/Assets` vs `EVID/lod-b-fix/final2/Assets`: exactly 2 differing files, `windows_05_frame_snaps014.vmdl` and `windows_05_frame_snaps014_mirror_x.vmdl`; `diff -u` shows only the added 4-line block `from = "material #221.vmat" / to = "imported/school/art/material/tiling/metallic_01_albedo_snaps014.vmat"` at `:84-85`, matching the HEAD-baseline value in `reimport-evidence/imported-before-loop/.../windows_05_frame_snaps014.vmdl:55-56`, and the `.vmat` target exists on disk (1281 B). 0 prefab/scene/vmat churn in that same-options comparison. Caveat confirmed on disk: `gates.py`'s older `obs-m7` baseline (`gates.out:7-54`) shows 21 changed mirror prefabs + 21 gained mirror VMDLs from M7 landing after that baseline - baseline drift, not this fix, and out of scope here. I re-read `final2/import-report.json`: `1280/958/2141`, issues `63/79/3016`. Fix: none.

No objective defect remains open in the scoped hunks. `DONE?`, `WORKER` and `OTHER` unchanged.


## Clarification Q&A

Kickoff batch 02 / 03 (Director, 2026-11-25, chat; full record in eliots-agentics/archive/sessions/):
An (Q1, verbatim): "Adopt as baseline: commit those 4 files by explicit path in ONE baseline commit so every task diff and critic review is against a clean HEAD. Consequence: fog removal, directional scale and wrapper-root guard become official HEAD." Landed c91e87b.
An (Q2, verbatim): "ignore the git diff"; clarified: "And also, what I meant by 2: Ignore what you learned from the git diff of session.md".
An (Q3, verbatim): "The skybox and tree color you should be able to discern from a screenshot. the others you'll have to marke done yes i guess because that's on me to finalize."
An (Q4, verbatim): "Yes: run the full clean re-import loop (launch editor, open scene, play, capture two play frames against Testing/Unity-Screenshot-Outside.png). Consequence: costs a full editor open/compile/play cycle per verification round."
This task is one of "the others": closes with an Open: line (Q3).

## Relevant History 

Reopens the LOD side of archived task 12 "LOD Split-Model Material Slots" (eliots-agentics/archive/tasks/12 LOD Split-Model Material Slots.md; DONE? YES!, verdict "no further changes" 2026-11-22 22:09): split-model embedded material slots were fixed, but s&box LODs were NOT implemented. New symptom (2026-11-25): z-fighting from all Unity LODs emitted together as components on Floor_stairs_middle (1). Converter context in the unityimporterplus skill: fixed model-prefab IDs 2300000+2n / 4300000+2n, per-mesh RenderMeshFile nodes when bodygroups exist, UnityPrefabBindings silently skips prefabs over 16 MiB, and the split-model material-slot fix in archived 12. Prior visual evidence: C:/Users/Eliot/AppData/Local/hermes/profiles/hermes-sbox/cache/scratch/taskmaster-nov22/outdoor-compare/COMPARISON-2026-11-24.md. Baseline c91e87b; HEAD was 2c17e22 when this task opened and is bf099d4 now.


## Stylistic Task Notes:
References to variables like X and Y automatically imply that these are to be exposed properties for developer tweaking.
When programming, portability and DRY are a priority.

## Post-Task Follow Ups 

Director:
Ok, I took a look.

I see Floor_stairs_middle (1) popping in and out of its 2 LODs, good job! Don't see any more z-fighting. Good job!
Problems:
Floor_stairs_top (1) and Floor_stairs_bottom (1) are essentially the same thing, yet they don't swap LOD's when I move back and forth w the camera like the middle one does. Why? They're essentially the same.
It appears some far-away LODs are reporting errors from sbox. Perhaps missing materials? An example is Windows_05_Frame_Snaps014. When i get close it looks fine but far away it shows the typical sbox texture/material error.
