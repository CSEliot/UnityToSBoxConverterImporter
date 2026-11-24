## Status: STARTED? REJECTED! CLARIFIED? YES! HISTORY? YES! REVIEWED? YES! DONE? NO! WORKER: NONE OTHER: fresh symptom task; split from investigated-only task 15; probes-report.md on disk; all tree-colour mechanisms UNTESTED (no verdict); assets located; likely downstream of lighting/fog

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

2026-11-24 22:23 - Verdict: changes required
Reviewer role: Review Critic (bounded adversarial re-review); judged the four uncommitted Editor/Core files plus the closing evidence, not a missing worker trail.

1. [High] COMPARISON-2026-11-24.md:71-73 - the tree-colour symptom is visibly present and the cause is recorded OPEN, not resolved. My own vision read of OUTDOOR-main-640.jpg gives dark olive-green rounded crowns; Testing/Unity-Screenshot-Outside.png gives deep burgundy/red-purple crowns. Acceptance says the symptom is finished when it is GONE; a disclosed gap under BEST EFFORT is not acceptance by the Director. Fix: keep DONE? NO! until the Director accepts this frame pair or a Unity-side cause is diagnosed; no importer code change is justified by the evidence available.
2. [Low] Editor/Core/UnityMaterial.cs / source tree assets - I tried to falsify the parent's "importer carries tree data faithfully" claim and failed, so the claim survives: (a) Tree_01_Snaps014.FBX does contain three LayerElementColor streams, but a binary probe shows all 8365+4526+2789 vertex colours are RGB (0,0,0) with alpha about 0.99, i.e. no burgundy vertex tint; (b) the courtyard also contains 10-11 Tree_Brick_01 instances, whose material references resolve to outdoor_trim and earth_floor in both the Unity prefab and the generated prefab; (c) source and generated albedo TGAs are byte-comparable in mean colour (leaf 61.6,69.9,40.5; earth floor 105.9,94.2,69.7). Fix: record these observations beside the disclosed gap; do not treat them as a pass.

Tree (four reviewed source files at HEAD ae9781a):
- Editor/Core/UnityHdrpLighting.cs — `git hash-object --path="Editor/Core/UnityHdrpLighting.cs" "Editor/Core/UnityHdrpLighting.cs"` => 3de49ef03d589016281b43d044910382893f34ac
- Editor/Core/UnityPrefabExport.cs — `git hash-object --path="Editor/Core/UnityPrefabExport.cs" "Editor/Core/UnityPrefabExport.cs"` => 7c80ad48c817952ae79aa2935775e32616249049
- Editor/Core/UnitySceneExport.cs — `git hash-object --path="Editor/Core/UnitySceneExport.cs" "Editor/Core/UnitySceneExport.cs"` => 6bc6a265c16a18cc84b1e7677fbc45f14f1637ae
- Editor/Core/UnityVolume.cs — `git hash-object --path="Editor/Core/UnityVolume.cs" "Editor/Core/UnityVolume.cs"` => 420f4633aac52e30cb9547819171efe06f428786

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

MASTER CLEAN-OUTPUT PERCEPTUAL GATE 2026-11-24 (first run of this gate):
Regenerated Assets/imported from HEAD ae9781a (harness rebuild 0/0, 2154 files,
verify_output.py 875 files 0 problems). Opened the Dev editor, played mergev2,
captured a play frame from the main camera. Clean frame: 230 green pixels vs
Unity oracle 8630; vision reports only a narrow strip of dark low foliage at
the bottom of the windows and NO readable green tree crowns. The Unity oracle
itself shows NO trees, so this symptom still needs a view containing trees in
BOTH engines; it remains UNJUDGEABLE, not resolved. The clean frame is no
longer a flat grey wall (152052 coloured px) but is dim/cool/desaturated, so
the downstream lighting/fog confound is still live. Full numbers and capture
paths: <scratch>/taskmaster-nov22/visual/CLEAN-PERCEPTUAL-GATE.md.

FOLLOW-UP 2026-11-24: both the disabled-warm-sun and 0.012-conversion
candidates are FALSIFIED (faithful to Unity); the SkyAffect=0 patch is
contraindicated. The clean frame still has no readable green foliage (230 green
px) and the Unity oracle itself shows no trees, so this symptom needs a view
containing trees in BOTH engines. parent-verified evidence and the recorded
task-09 default-sky scope decision: visual/CLEAN-PERCEPTUAL-GATE.md.
DONE? stays NO!.

FINAL ROOT CAUSE 2026-11-24 (parent-verified): MergeV2 has 25+ baked
Lightmap-*_comp_light.exr + LightingData.asset and m_EnableBakedLightmaps:1
(MergeV2.unity:53); UnitySceneExport.cs:18-19 reports lightmaps as not
converted. The golden sun is BAKED GI that s&box cannot reproduce. The
offered sunIntensity fix is FALSIFIED against local HDRP source (sunIntensity
is a sky-disk default, not scene illumination). Isolation ABLATION5 shows the
disabled sun branch causes ~90% of the warm shift. No source-faithful importer
defect remains; an active-sun approximation needs an explicit Director
decision. See visual/CLEAN-PERCEPTUAL-GATE.md. DONE? stays NO!.

REGENERATED COMPILED FRAME 2026-11-24: NO green trees or foliage visible in the
matching-camera render, and the Unity oracle also shows no trees. Tree colour
therefore remains UNJUDGEABLE on this camera; this is not a pass. Full verdict:
visual/REGENERATED-FRAME-VERDICT.md. A view containing trees in BOTH engines is
still required before this header can close.

TREE-CAMERA SWEEP 2026-11-24 (scratch mirror, final regenerated output, play):
captured the five `Setting` Main Camera variants plus the Japanese School Camera.
Green-pixel counts: cam0 87, cam1 96769, cam2 13, cam3 0, cam4 414494, pinned
Japanese School Camera 3743. vision_analyze of the two green-heavy frames
(cam1, cam4) shows NO trees or foliage - the green pixels come from large dark
olive/forest-green FLOORS/carpets and a small EXIT sign, not vegetation. The
Unity oracle also shows no trees. So tree colour remains UNJUDGEABLE on every
available camera: there is no view containing trees in both engines, and no
observation can confirm or refute "the trees aren't the correct color".
Frames: visual/TREE-*.png and -960.jpg. This is a genuine blocker, not a pass;
a view with trees in BOTH engines or the Director's own observation is needed.

TREE-COLOUR DISCRIMINATING OBSERVATION 2026-11-24 (parent-run, headless):
the "importer dropped/mis-mapped the tree colour" candidate is FALSIFIED by
measurement on disk. Unity and generated leaf albedo TGAs have identical
meanRGB (61.6,69.9,40.5) and 75.1% green-ish pixels; both engine materials
carry a white tint (_BaseColor/_Color 1,1,1 and g_vColorTint 1,1,1); all 11
Unity Tree_01_Snaps014 prefab instances have no m_Materials/colour overrides.
The s&box render's dark olive-green trees are faithful to the source data.
Unity's burgundy trees therefore come from Unity-side rendering (cause still
OPEN, not dismissed - the active HDRP Volume's ColorAdjustments/WhiteBalance
are neutral, so a red colour filter is not it). Under the Director's BEST
EFFORT bar and HDRP abandonment this is disclosed, not claimed resolved.
Evidence: <scratch>/taskmaster-nov22/outdoor-compare/COMPARISON-2026-11-24.md.

DIRECTOR DECISION 2026-11-24 (recorded by Task Master): "Fix trees and skybox
100% (i'm confident those are fixable) and then fix lighting to "best ability"."
Work order: trees -> skybox -> lighting. BEST EFFORT now applies to LIGHTING ONLY,
not trees or skybox. This real Director turn is the fresh decision that unblocks
re-tuning directional intensity against the outdoor dusk oracle. Any code change
resets REVIEWED? to NO! on affected tasks and needs a fresh critic "no further
changes" before DONE? YES!. Evidence:
<scratch>/taskmaster-nov22/outdoor-compare/COMPARISON-2026-11-24.md.
TREE-COLOUR DISCRIMINATING OBSERVATION (parent-run): importer data defect
FALSIFIED - Unity/generated leaf albedo TGAs identical (61.6,69.9,40.5), white
tints both engines, 11 prefab instances with no material/colour overrides. The
Director is confident the symptom is fixable, so the mechanism is being
RE-OPENED from the source side (other tree assets, vertex-colour handling,
deleted 2760-file AssetStoreOriginals cleanup). Research child sa-0-1b8f9add.
No code changed yet.

RESEARCH VERDICT 2026-11-24 (parent-verified on disk, deleg_d9eb2472):
TREE = SOURCE-FAITHFUL, no importer-side colour defect. Full census: all 78
flora instances in MergeV2.unity resolve to six prefabs (Tree_01 11, Tree_Brick
10, Shrubs_01 36, Flowerbeds 21) with white _BaseColor/_Color and green/grey-brown
albedos; leaf (61.6,69.9,40.5), shrubs (63.7,73.1,43.4), trunk (129.6,107.2,85.5)
brick-brown. Generated textures/tints identical, no vertex-colour tint path in
Editor/Core, no burgundy asset at 657970f/ca07499/49a49ea (R100 moves only).
Verified myself: trunk/leaf TGAs identical, white tints, MergeV2.unity:29 has
m_SkyboxMaterial {fileID: 0}. A burgundy canopy has NO source representation.
Full report: <scratch>/taskmaster-nov22/outdoor-compare/TREE-SKY-DIAGNOSIS.md.
Holding for a fresh Director decision (no invented red tint).

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
