## Status: STARTED? YES! CLARIFIED? YES! HISTORY? YES! REVIEWED? NO! DONE? NO! WORKER: NONE OTHER: fresh symptom task; split from investigated-only task 15 per Director bedtime instructions; read-only tree-colour probes (sa-2-32793b5e) in flight

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
