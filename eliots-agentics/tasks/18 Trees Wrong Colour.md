## Status: STARTED? YES! CLARIFIED? YES! HISTORY? YES! REVIEWED? NO! DONE? NO! WORKER: NONE OTHER: fresh symptom task; split from investigated-only task 15; probes-report.md on disk; all tree-colour mechanisms UNTESTED (no verdict); assets located; likely downstream of lighting/fog

## Task
The trees aren't the correct color.

### Description
Director's verbatim symptom (2026-11-22): "The trees aren't the correct color." Acceptance check:
the imported MergeV2 / Test_Range trees must render with the colour they have in the Unity source.
The symptom is finished when it is GONE, not when a spec is satisfied. Do not set DONE? YES!
while it is still reported.

Split fresh 2026-11-23 from task 15 "Scene Visual Fidelity" (closed b11952e), a verification-only
task that declared "no confirmed gap" against its own converter. common.md forbids that as a
verdict. The old table is in ## Relevant History for reference only; do not reuse its conclusion.

Unity source: C:/cseliot/1-Development/4-Unity/2-Projects/old_school_model. Generated output:
Assets/imported/assetpackschoolnameverylong/schoolhdrpnameverylong/ (scenes, prefabs, materials,
models, textures).

Required method:
1. For EACH candidate mechanism, write down the named observation that would FALSIFY it. No
   "investigated" verdict without both a mechanism and its falsifier.
2. Candidate mechanisms (verify, do not assume; earlier scratch `part-c-unity.md` named (a),(b)
   as UNVERIFIED candidates and found no confirmed gap):
   a. VERTEX COLOURS: the tree/leaf FBX may carry a colour stream that the `_iup_` mesh rewrite
      drops. Falsifier: read the source FBX colour stream (or Unity `.mesh` import) and compare
      to the generated `.vmdl` / mesh data; if no colour stream exists in the source, false.
   b. PREFAB MATERIAL OVERRIDES: `m_Materials` overrides on tree PrefabInstances may be dropped,
      so instances fall back to a neutral/unassigned `.vmat`. Falsifier: enumerate tree
      PrefabInstance material overrides in the source and check the generated scene/prefab
      assignments; if all resolve to the intended leaf/bark `.vmat`, false.
   c. ALBEDO TEXTURE / COLOUR SPACE: base-colour map, `_BaseColor` tint, alpha-cut
      (`UnityMaterial.cs:29,36-43,202-204,268,285-290,300`), `_AlphaCutoff 0.19` ->
      `g_flAlphaTestReference "0.190"`, colour-space (sRGB vs linear) conversion. The 16-bpp TGA
      normalisation (`UnityTgaRewrite.cs`) changed exactly 2 poster albedos; check whether any
      tree/leaf albedo TGA is among the affected 16-bpp files or got an inverted alpha. Falsifier:
      open the source tree albedo TGA and the generated `.vmat` `TextureColor` target with a
      pixel probe; if the source pixel colours and the texture referenced by the generated
      material match, false.
   d. METALLIC / SMOOTHNESS REMAP: task 08 fixed a metallic remap; verify it did not leave tree
      materials with a metalness that darkens/desaturates the green. Falsifier: read the
      generated tree/leaf `.vmat` `g_flMetalness` / `g_flSmoothness` against source.
   e. DOWNSTREAM OF LIGHTING/FOG: a flat under-lit, fog-washed viewport makes every material's
      colour wrong. Falsifier: with the lighting/fog defects from tasks 15/17 addressed, the
      trees render the correct green. If they still do not, the cause is in the material path.
      A component value is not the cause when the same value renders correctly elsewhere.
3. Differential ablation: find a WORKING case from the same importer (city--1 `demo.scene`, or
   another imported material whose colour is correct) and enumerate what differs in the
   material/colour path from the trees. Identify which difference would cause the symptom.
4. Screenshot evidence: no editor is currently up (both installs' `--list` return `[]`); launch
   the editor belonging to the install you use, read `editor_status` BEFORE capture, do not
   save/play/change if `SceneHasUnsavedChanges` is true. Capture the viewport with the trees in
   frame, copy the PNG to scratch, downscale to ~960x540 JPEG, and LOOK at it with
   vision_analyze. Corroborate any colour claim with a CROP and a PIXEL measurement (sample the
   leaf region's RGB), not an adjective: vision models invent detail on low-contrast frames.
   Before/after pair around any fix. State the bridge and evidence used.
5. If a mechanism survives, fix the ROOT CAUSE in the engine-free importer core, fix the CLASS.
   If all are falsified, say so with observations and name the next candidate; do not close on
   self-consistency. Do not file an objective data defect as an `Open:` editor-visual check.
6. Regression gates IF code changes land: harness build 0/0; `building.unitypackage` (24 files)
   and `city--1.unitypackage` (1,166 files) byte-identical to fresh HEAD baseline (no --scenes,
   BOTH trees populated); `verify_output.py <outRoot> <outRoot>/imported` 0 problems; two
   identical runs byte-identical; BOM-less; no stray artifacts.
7. Put all evidence (source/generated material excerpts, TGA pixel probes, FBX colour-stream
   checks, screenshots, crops, pixel measurements, gate logs) in scratch files and cite absolute
   paths in ## Notes.

Stage by explicit path only. No push until the Master verifies. Do not add unrequested tooling.
Final answer in English.

### Work Review


## Notes

MASTER LIVE-EDITOR FINDING 2026-11-23 (shared root cause; tree colour unjudgeable yet):
The imported mergev2.scene renders as a flat uniform grey WALL (main-camera frame mean
114.74, stddev 0.51, 0 coloured pixels, i.e. NO colour anywhere). Root cause is an opaque
height-fog layer: importer UnitySceneExport.cs emits HeightColor "0.5,0.5,0.5,1" (alpha 1
= FULL opacity per engine Fog.cs:172) with HeightOffset=0, occluding the whole frame. Verified
by 28-variant in-editor ablation: fog OFF yields 132767 coloured pixels (a real scene);
HeightColor.a 1->0 restores geometry (0->177 grey levels). Full evidence:
scratch/taskmaster-nov22/visual/LIVE-EDITOR-FOG-DIAGNOSIS.md. Tree colour/greenness CANNOT
be judged while the entire frame is a flat grey wash — the earlier verdict "likely downstream
of lighting/fog" is now CONFIRMED as downstream of fog. Reassess this task AFTER the task 15
fog fix with a fresh rendered frame that actually shows trees. Do not close on self-consistency.

MASTER RENDERED VERIFICATION 2026-11-23 (task 15 fix landed; this task STILL cannot be judged):
The opaque height-fog wall is CONFIRMED removed (rendered before/after: 0 -> 177 grey levels,
geometry visible; six headless gates PASS; evidence visual/RENDERED-VERIFICATION.md). BUT the
FIX frame shows an UNDER-ROOF INTERIOR (dark roof slab, columns, floor, bright window openings)
and vision_analyze explicitly reports NO green trees, NO foliage, NO warm light — the frame is
"essentially MONOCHROME GREY" with 37 coloured pixels. Even with MaximumOpacity 1->0.3 (which
restores 123562 coloured pixels and a blue-grey sky) vision reports "No visible trees, people,
green foliage" (11_fixed_max03_small.jpg). So NO rendered frame has yet shown a tree at all;
tree colour/greenness remains UNJUDGEABLE. This is not a verdict. Two named residuals block it:
(1) distance fog desaturation (shared with tasks 16/17, needs Director decision); (2) the camera
is aimed at a dark interior, so a fresh frame that actually contains trees is required — likely
after repositioning the editor camera or choosing another scene/camera. HONESTY CAVEAT: the live
editor scene is STALE (mtime 02:09, predates all today's fixes) and carries editor-added fog
fields; a re-import + reload is required before perceptual judgement of current output. No Unity
reference render exists. Do NOT close this task on the shared-root-cause inference.

MASTER ADDENDUM 16:25 — SIX-CAMERA RENDER SWEEP (visual/camera_frames.py,
camera_frames_paths.json, camera_sheet.jpg, cam*_fix.jpg). Rendered all SIX scene cameras with
the FIX fog values + SkyAffect=0 applied in memory; all edits discarded after (editor left clean).
Result: a tree IS now in frame, but NOT as green foliage. cam0_Setting_Main shows a dark,
backlit TREE-CROWN SILHOUETTE (charcoal/black, not green) at the edge of a window with pale-blue
sky (vision_analyze); cam1_Setting_Main1 shows pale-blue sky with cloud streaks; none of the six
cameras show recognisably GREEN trees/foliage. Greenish-pixel counts (G>R,G>B) are tiny:
cam0 217, cam1 54, cam2 29, cam4 132, cam5 164 out of 921600 pixels.
This SHARPENS the mechanism: the reported "trees are the wrong colour (greenness?)" is very
likely NOT a tree material/colour bug but an EXPOSURE problem — the trees are backlit silhouettes
crushed to black by the same underexposure documented in task 17 (with fog fully OFF the scene is
also dark, mean 17.48). Falsifier to run AFTER re-import: brighten exposure / pick a camera facing
the trees from the lit side; if the crowns turn green, the colour was never wrong. Do NOT close
this task on a shared-root-cause inference; it needs a lit, post-re-import frame with visibly
green foliage plus the Director's editor observation. No Unity reference render exists.

CORRECTION 16:35 — that last claim is WRONG. A Unity reference render DOES exist
(Testing/Unity-Screenshot.png; analysis visual/UNITY-ORACLE.md). Looked at: it is a well-lit
classroom interior and ALSO shows NO trees / green foliage (greenish 112 px of ~1.2M) and NO
visible open sky. So this oracle cannot judge tree colour at all — the symptom needs a view
that contains trees in BOTH engines. Do not use "no oracle" as a reason to defer an objective
comparison; the oracle exists but is the wrong view for this symptom. Task stays open.

MASTER ADDENDUM 16:10 — one blocker has a principled fix: engine Fog.cs:280-283 `SkyAffect`
(default 1, "How strongly fog covers the sky") is not emitted, so distance fog covers the SKY
with flat grey. Falsifier RUN: SkyAffect 1->0 restores 31846 coloured pixels and a blue-to-pale
grey sky (vision_analyze confirmed) while KEEPING Unity's authored custom grey; it is an
ENGINE-DEFINED knob, not a pinned magic opacity. Evidence: visual/skyaffect_probe.py,
skyaffect_paths.json, RENDERED-VERIFICATION.md addendum. BUT even with that, vision_analyze of
b_fixed_skyaffect0_small.jpg reports NO recognizable trees/foliage (only a tiny green patch),
because the camera looks out of an interior. Blocker (2) stands: a frame that actually contains
trees is required — likely a different scene camera or a repositioned editor camera. Tree colour
remains UNJUDGEABLE; do not close.

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
6a65cb1 the worker recorded: assets `Tree_Snaps014.mat` / `Tree_Leaf_Snaps014.mat` — HDRP Lit,
`_BaseColorMap`, white `_BaseColor`, `_AlphaCutoff 0.19`, metal remap; generated leaf `.vmat`:
`F_ALPHA_TEST 1`, `F_RENDER_BACKFACES 1`, `g_flAlphaTestReference "0.190"`,
`g_flMetalness "0.000"`, white `g_vColorTint`, real albedo TGA, 10/10 textures present; 0
tree/leaf unresolved/warn lines; verdict "no confirmed gap", "actual green is editor-visual";
critic 2026-11-22 22:10 `no further changes`. Task 08 (archived) fixed a metallic remap;
`tga-fix` (656f5b9) normalised 16-bpp poster albedos and fixed an inverted-alpha regression.

WHY THIS IS NOT A FIX: "no confirmed gap" compared the generated `.vmat` to what our own
converter was written to emit, and the residual was filed as an editor-visual `Open:` line. The
Director still reports the trees are the wrong colour. The four `Open:` lines from old task 15
were cleared on reopening and the tree-colour symptom got its own task. Vertex-colour and
prefab-override candidates were explicitly left unverified in scratch `part-c-unity.md`.


## Stylistic Task Notes:
References to variables like X and Y automatically imply that these are to be exposed properties for developer tweaking.
When programming, portability and DRY are a priority.

## Post-Task Follow Ups 
