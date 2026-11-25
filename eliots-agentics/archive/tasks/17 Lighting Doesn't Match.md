## Status: STARTED? REJECTED! CLARIFIED? YES! HISTORY? YES! REVIEWED? YES! DONE? NO! WORKER: NONE OTHER: fresh symptom task; split from investigated-only task 15; differential report on disk; 3 falsified premises dropped from Description; Intensity/LightColor separation is the real fix (shared with task 19)

## Task
Lighting doesn't match.

### Description
Director's symptom (2026-11-22, deliberately restated 2026-11-23): originally "Sun (directional
light) doesn't match", now "lighting doesn't match". The Director explicitly withdrew the pinned
values ("Sun sky color is black and 1 intensity") after the city--1 counterexample: that imported
scene has the same black sun tint / intensity 1 and renders fine because the 2D Skybox lights the
materials. Do NOT diagnose from those pinned values alone. Acceptance check: the imported
MergeV2 / Test_Range scenes must be lit like the Unity source. The symptom is finished when it is
GONE, not when a spec is satisfied. Do not set DONE? YES! while it is still reported.

Split fresh 2026-11-23 from task 15 "Scene Visual Fidelity" (closed b11952e), a verification-only
task that declared "no headless gap" against its own converter. common.md forbids that as a
verdict. The old table is in ## Relevant History for reference only.

Unity source: C:/cseliot/1-Development/4-Unity/2-Projects/old_school_model. Generated scenes:
Assets/imported/assetpackschoolnameverylong/schoolhdrpnameverylong/scenes/{mergev2,test_range}.scene.
Working case: Assets/imported/polygoncity/scenes/demo.scene (city--1). A 2026-11-23 Dev-editor
capture of MergeV2 (scratch `taskmaster-nov22/mergev2-viewport.png`) showed a flat medium-dark
gray, under-lit viewport: no sun, no sky detail, no shadows, no shading gradients, faint geometry
silhouettes.

Required method:
1. For EACH candidate mechanism, write down the named observation that would FALSIFY it. No
   "investigated" verdict without both a mechanism and its falsifier.
2. Use the engine as an EXTERNAL ORACLE (the importer's own output is not one): engine-authored
   `.scene` files under `<engine>/addons/`, `templates/`, `samples/`, `download/`, and the managed
   API XML docs `<engine>/bin/managed/Sandbox.Engine.xml`. Procedure and worked
   `Sandbox.DirectionalLight` shape are in the skill reference
   `references/engine-schema-conformance.md`. Diff key sets in BOTH directions, check
   `__version` on EVERY generated light, read XML docs for `Light.Intensity`,
   `Light.LightColor`, `Light.ComponentVersion`, `UpgradeDirectionalLightTint`,
   `UpgradeLegacyTint`, `DirectionalLight.SkyColor`, `AmbientLight`.
3. Candidate mechanisms (verify, do not assume):
   a. OBJECTIVE DATA DEFECT (verified against engine source): `Editor/Core/UnitySceneExport.cs:673`
      hardcodes `["SkyColor"] = "0,0,0,0"` for EVERY generated directional light, emits no
      `Intensity` key, and folds intensity INTO `LightColor` (mergev2 sun
      `7.82938,4.77047,2.59516,1`). Engine source
      `C:/cseliot/1-Development/1-SBox/1-Engine-Builds/Dev/sbox-public/engine/Sandbox.Engine/Scene/Components/Light/Light.cs:18,144-172,204-223`
      proves `LightColor` and `Intensity` are SEPARATE serialized properties and rendered colour =
      `LightColor * Intensity * normalized Temperature`; the folding occurs at
      `UnitySceneExport.cs:658-693`. Falsifier: if generated `LightColor` equalled the source tint
      with a sibling `Intensity` equal to source intensity and the product preserved, the defect
      is gone. Two premises from this task's first draft were FALSIFIED 2026-11-23 and must NOT be
      used: (i) authored `LightColor` is NOT limited to 0..2 — authored directional lights reach
      3.96 and point lights ~36.89; (ii) `Light.ComponentVersion=1` upgraders
      (`Light.cs:35-45`) only set `LightColor` when it is MISSING (#E9FAFF), and generated lights
      already have `LightColor`, so the "unversioned light re-defaults to black tint on load"
      mechanism is FALSE in this engine build. The separation of `Intensity` remains the real,
      headless-checkable schema misuse.
   b. The engine upgrade path (`Light.ComponentVersion` "Versions light defaults so existing
      scenes retain their original tint") re-defaults unversioned lights. Falsifier: an
      unversioned generated light in an engine-authored scene that still displays its authored
      tint/intensity.
   c. Missing `Sandbox.AmbientLight`: the conformance reference notes the recommended ambient
      contribution is a separate `AmbientLight` component, while we hardcode `SkyColor` black and
      rely on `SkyBox2D`. Falsifier: a working engine-authored scene whose directional light
      also has black `SkyColor` and no `AmbientLight` yet is well lit (city--1 may be this case) —
      then the cause is the sky/ambient system, not the light block.
   d. Differential ablation: compare the WORKING city--1 `demo.scene` lighting system to the
      FAILING MergeV2 `mergev2.scene` component by component (DirectionalLight blocks,
      SkyBox2D, Fog, EnvmapProbe count, AmbientOcclusion, tonemapping/exposure). Identify which
      difference would cause the symptom. A component's values are not the cause when the same
      values render correctly in another scene.
   e. Photometric conversion: `Editor/Core/UnityHdrpLighting.cs:58-80` uses 1000 cd/unit with a
      clamp of 8; MergeV2's 20,000-lux sun clamps to 8. Check whether the clamp discards the
      source intent. Falsifier: source lux values vs emitted `LightColor`/`Intensity` for a case
      where the clamp materially changes the rendered brightness.
4. Screenshot evidence: no editor is currently up (both installs' `--list` return `[]`); launch
   the editor belonging to the install you use, read `editor_status` BEFORE capture, do not
   save/play/change if `SceneHasUnsavedChanges` is true. Capture the viewport, copy the PNG to
   scratch, downscale to ~960x540 JPEG, and LOOK at it with vision_analyze. Before/after pair
   around any fix. Measure brightness with pixel numbers, not adjectives. State the bridge and
   evidence used.
5. If a mechanism survives, fix the ROOT CAUSE in the engine-free importer core, fix the CLASS.
   If all are falsified, say so with observations and name the next candidate; do not close on
   self-consistency. Do not file an objective data defect as an `Open:` editor-visual check.
6. Regression gates IF code changes land: harness build 0/0; `building.unitypackage` (24 files)
   and `city--1.unitypackage` (1,166 files) byte-identical to fresh HEAD baseline (no --scenes,
   BOTH trees populated); `verify_output.py <outRoot> <outRoot>/imported` 0 problems; two
   identical runs byte-identical; BOM-less; no stray artifacts.
7. Put all evidence (component diffs, XML doc excerpts, scene excerpts, screenshots, pixel
   measurements, gate logs) in scratch files and cite absolute paths in ## Notes.

Stage by explicit path only. No push until the Master verifies. Do not add unrequested tooling.
Final answer in English.

### Work Review

2026-11-24 22:23 - Verdict: changes required
Reviewer role: Review Critic (bounded adversarial re-review); judged the four uncommitted Editor/Core files plus the closing evidence, not a missing worker trail.

1. [High] Editor/Core/UnityHdrpLighting.cs:84 - the emitted directional intensity 8 was selected against the earlier indoor oracle (mean 46.09) and is now measured at 100.16 mean / 0.374 warm against the current outdoor oracle's 48.54 / 0.053. I looked at both frames: s&box is bright clear daylight with hard high-contrast shadows, Unity is dim dusk. That is a 2.06x brightness overshoot, not parity. Fix: re-evaluate the directional scale against the current oracle with an explicit Director decision, or record the Director's acceptance of the overshoot; keep DONE? NO! until then.
2. [Medium] COMPARISON-2026-11-24.md:75-79 - attributing gap 1 to abandoned HDRP-side exposure is incomplete: the Director himself ordered the intensity raise and it is an in-scope importer change, so HDRP abandonment cannot silently excuse its present overshoot. Fix: present the two frames and the numbers to the Director rather than deciding acceptance in the Task Master's notes.
3. [Low] Editor/Core/UnityHdrpLighting.cs:19-28,139 - the rewritten class doc and report line honestly label the change a Director-approved baked-GI approximation with intentional saturation; that wording is accurate and is not a defect.

Tree (four reviewed source files at HEAD ae9781a):
- Editor/Core/UnityHdrpLighting.cs — `git hash-object --path="Editor/Core/UnityHdrpLighting.cs" "Editor/Core/UnityHdrpLighting.cs"` => 3de49ef03d589016281b43d044910382893f34ac
- Editor/Core/UnityPrefabExport.cs — `git hash-object --path="Editor/Core/UnityPrefabExport.cs" "Editor/Core/UnityPrefabExport.cs"` => 7c80ad48c817952ae79aa2935775e32616249049
- Editor/Core/UnitySceneExport.cs — `git hash-object --path="Editor/Core/UnitySceneExport.cs" "Editor/Core/UnitySceneExport.cs"` => 6bc6a265c16a18cc84b1e7677fbc45f14f1637ae
- Editor/Core/UnityVolume.cs — `git hash-object --path="Editor/Core/UnityVolume.cs" "Editor/Core/UnityVolume.cs"` => 420f4633aac52e30cb9547819171efe06f428786

## Notes

MASTER LIVE-EDITOR FINDING 2026-11-23 (shared root cause; sequencing note):
The imported mergev2.scene renders as a flat uniform grey WALL (main-camera frame mean
114.74, stddev 0.51, 0 coloured pixels). Root cause is an opaque height-fog layer, NOT the
lights: importer UnitySceneExport.cs emits HeightColor "0.5,0.5,0.5,1" (alpha 1 = FULL
opacity per engine Fog.cs:172) with HeightOffset=0, so the whole frame is occluded. Verified
by 28-variant in-editor ablation: fog OFF reveals a dimly-lit building interior (roof/beams/
strip lights/bright windows); HeightColor.a 1->0 restores geometry (0->177 grey levels). Full
evidence: scratch/taskmaster-nov22/visual/LIVE-EDITOR-FOG-DIAGNOSIS.md. The task 19 schema
fix (Intensity/LightColor separation, SkyColor, __version, commit 3a548b7) is a real
objective fix and stays closed, but "lighting doesn't match" could not be visually judged
while the scene was invisible. Reassess this task AFTER the task 15 fog fix with a fresh
rendered frame. Do not close on self-consistency.

MASTER RENDERED VERIFICATION 2026-11-23 (task 15 fix landed; residual findings for THIS task):
The opaque height-fog wall is CONFIRMED removed (rendered before/after: 0 -> 177 grey levels,
geometry visible; six headless gates PASS; evidence visual/RENDERED-VERIFICATION.md). But the
FIX frame is still very dark and "essentially MONOCHROME GREY" (37 coloured pixels). Crucially,
with fog FULLY OFF the scene is ALSO dark (mean 17.48), so the darkness is NOT caused by fog —
it is a genuine lighting/exposure question and belongs to THIS task, not task 15. Falsifier RUN
on the live editor (visual/verify_fix.py): MaximumOpacity 1->0.3 restores 123562 coloured pixels
and a blue-grey sky, but the geometry stays "strongly underexposed" with "little visible material
detail" (vision_analyze of 11_fixed_max03_small.jpg). So two separable residuals remain here:
(1) distance fog at MaximumOpacity=1 + ColorSource=Constant + Color 0.5 grey desaturates the
far field/sky (shared with task 16, needs Director decision — pinned value forbidden,
SceneSky is engine default but ignores Unity's authored custom grey); (2) underexposure,
independent of fog. The task 19 schema fix (Intensity/LightColor separation, SkyColor,
__version, commit 3a548b7) is a real objective fix and stays closed. HONESTY CAVEAT: the live
editor scene is STALE (mtime 02:09, predates ALL of today's fixes) and carries editor-added
fog fields; the Director's editor is showing an old import, so NO perceptual judgement of the
CURRENT importer output is possible until a re-import + reload. Re-importing fixed scenes into
Assets/imported is gitignored and will not dirty the repo. Do NOT close on self-consistency.

MASTER ADDENDUM 16:10 — PRINCIPLED FIX for residual (1): engine Fog.cs:280-283 `SkyAffect`
("How strongly fog covers the sky", default 1) is not emitted by the importer, so distance fog
covers the SKY with flat grey. Falsifier RUN: SkyAffect 1->0 restores 31846 coloured pixels and
a blue-to-pale-grey sky gradient (vision_analyze confirmed) while KEEPING Unity's authored custom
grey. It is an ENGINE-DEFINED knob, NOT a pinned magic opacity, so the earlier tradeoff is
resolved. Evidence: visual/skyaffect_probe.py, skyaffect_paths.json, RENDERED-VERIFICATION.md
addendum. Residual (2), the fog-independent underexposure, is still unexplained and is THIS
task's remaining objective question — do not close until both are addressed and a fresh
post-re-import frame is looked at.

CORRECTION 16:35 — a Unity reference render DOES exist (Testing/Unity-Screenshot.png; full
analysis visual/UNITY-ORACLE.md), so "no oracle" must not be used to defer. It is a well-lit
WARM classroom: golden low-angle sunlight patches on warm-brown wood, mean 46.09, 730872
coloured px, NO heavy fog, NO visible sky, NO trees. s&box cam0 (fix+SkyAffect=0) has mean
45.47 — nearly IDENTICAL overall brightness — but only 46699 coloured px vs Unity's 730872.
So the dominant residual here is NOT exposure level but DESATURATION / missing WARM sunlight
and warm wood tones: a LIGHTING/MATERIAL issue, exactly this task's scope. Objective oracle
available for a real side-by-side; compare emitted LightColor/Intensity + auto-exposure against
Unity source, re-import, and capture from the MATCHING camera. Do not close on self-consistency.

MASTER CLEAN-OUTPUT PERCEPTUAL GATE 2026-11-24 (first run of this gate):
Regenerated Assets/imported from HEAD ae9781a (harness rebuild 0/0, 2154 files,
verify_output.py 875 files 0 problems, mergev2.scene HeightEnabled:false,
HeightColor count 0). Opened the Dev editor, played mergev2, captured a play
frame from the main camera. Unity oracle mean 46.09 / 730872 coloured px /
391051 warm px; clean s&box frame mean 55.46 / 152052 coloured px / 91 warm px.
Overall brightness is close (55.46 vs 46.09) but warm sunlight is absent
(91 vs 391051 warm px): the dominant residual is desaturation / missing warm
light, not exposure level. The symptom is STILL PRESENT. Falsifier RUN: Unity's
20000-lux bright sun is ALSO under the inactive Setting root, so the imported
disabled warm sun is FAITHFUL, not an importer bug; drop that candidate. Next
candidate to test: the ACTIVE Unity directional light m_Intensity:12 converts
to Intensity:0.012 (1000 cd/unit) - read the source light unit and engine
Light.Intensity semantics before calling it a defect. Full numbers and capture
paths: <scratch>/taskmaster-nov22/visual/CLEAN-PERCEPTUAL-GATE.md.

FOLLOW-UP 2026-11-24 (two candidates FALSIFIED, parent-verified on disk):
the active Unity light conversion 12 lux -> Intensity 0.012 is the documented
1000 cd/unit scale (UnityHdrpLighting.cs:31,77), not a defect; the SkyAffect=0
mirror patch must NOT ship (sky-pixel-only, stale-scene falsifier). CORRECTION:
sunIntensity 130000 at MergeV2.unity:697-698 is an HDAdditionalLightData
celestial-body field on the 12-lux light itself, NOT a PhysicallyBasedSky
override; its rendering meaning is unverified. The remaining gap is task 09's
Director-approved default-sky approximation (UnityVolume.cs:334-340: sun,
atmosphere and time of day explicitly not reproduced), not an unfixed importer
bug. See visual/CLEAN-PERCEPTUAL-GATE.md. DONE? stays NO!.

DECISIVE DIFFERENTIAL 2026-11-24 (same pinned camera, in-memory, restored):
toggling ONLY `Setting.Enabled` false->true->false in PLAY moves mean 55.52 ->
80.34, coloured 152374 -> 427066, warm 90 -> 297882 (warm fraction 0.3232 vs
Unity 0.3263), and the restore returns to baseline with the scene clean. So the
missing warm light is the DISABLED Setting subtree, not fog, sky material or
the 0.012 conversion. Caveat: a vision read still calls the enabled frame
dim/cool, and mean 80.34 over-brightens vs Unity 46.09, so this identifies the
mechanism, not perceptual parity. Unity has Setting m_IsActive:0 and
m_EnableBakedLightmaps:1, so the golden sun is plausibly baked GI. Full table:
visual/CLEAN-PERCEPTUAL-GATE.md. DONE? stays NO!.

FINAL ROOT CAUSE 2026-11-24 (parent-verified): MergeV2 has 25+ baked
Lightmap-*_comp_light.exr + LightingData.asset and m_EnableBakedLightmaps:1
(MergeV2.unity:53); UnitySceneExport.cs:18-19 reports lightmaps as not
converted. The golden sun is BAKED GI that s&box cannot reproduce. The
offered sunIntensity fix is FALSIFIED against local HDRP source (sunIntensity
is a sky-disk default, not scene illumination). Isolation ABLATION5 shows the
disabled sun branch causes ~90% of the warm shift. No source-faithful importer
defect remains; an active-sun approximation needs an explicit Director
decision. See visual/CLEAN-PERCEPTUAL-GATE.md. DONE? stays NO!.

MASTER INTENSITY SWEEP 2026-11-24 (scratch editor, pinned camera, play mode;
in-memory set_component on the active DirectionalLight, restored to 0.012 after):
  0.012 -> mean 40.92, stddev 15.39, warm 290013 (31.5%)
  0.5   -> 41.70 / 15.87 / 301472
  1     -> 42.17 / 16.55 / 306871
  2     -> 43.14 / 18.00 / 311793
  4     -> 44.14 / 20.40 / 319795
  8     -> 45.75 / 24.03 / 328621 (35.7%)
  16    -> 47.85 / 28.81 / 348475 (37.8%)
  Unity -> 46.09 / 32.68 / 391051 (32.6%)
Mean and contrast improve monotonically with intensity; 8-12 is the closest
source-derived range. Warm fraction overshoots above ~2 and vision still reports
a red/magenta cast with no long shadows, so intensity alone is not full parity.
Log: visual/intensity-sweep.log. Per the Director's note 5 the next code step is
to emit a higher, SOURCE-DERIVED directional intensity (not a pinned magic
value) by abandoning the HDRP 1000 cd/unit divisor for directional lights only;
spec: scratch/taskmaster-nov22/LIGHTING-FIX-SPEC.md.

MASTER FINAL GATES 2026-11-24 (all three fixes, parent-run):
harness build 0/0; old_school --scenes 2154 files, verify_output.py 875 files /
0 problems, grep Sandbox.Fog across generated *.scene = 0; building 24/24 and
city--1 1166/1166 byte-identical to the fresh HEAD baseline (0 diff lines);
two identical runs 0-byte diff; BOM 0; no stray repo artifacts. Diff vs the
pre-lighting output is EXACTLY two Intensity values: mergev2.scene active
directional 0.012 -> 8 (LightColor 1,0.56937,0.27636,1) and test_range.scene
0.005 -> 5; disabled sun stays 8, every point/spot/prefab unchanged. Code diff:
Editor/Core/UnityHdrpLighting.cs:77 one line (`type == 1 ? candela * dimmer :
candela * dimmer / CandelaPerUnit`), clamp and all other members untouched.
Rendered-frame equivalence: SWEEP-8.png was captured from the same generated
output with the active light set to 8 in memory, which is exactly what the fixed
importer now emits, so it is the final frame (mean 45.75, stddev 24.03, warm
35.7% vs Unity 46.09 / 32.68 / 32.6%). Vision still reports a red/magenta cast
and no long golden shadows, so intensity improves brightness/contrast but is NOT
full golden-hour parity. Logs: scratch/taskmaster-nov22/final-gate/out/.

REVIEW DISPOSITIONS 2026-11-24 (adversarial review deleg_a58a8db3):
F1/F5 ACCEPT the stale class doc, comments and report line at
UnityHdrpLighting.cs:8-23,29-34,132; REJECT the ordering fix as a blocker (the
Director abandoned HDRP photometric fidelity, the 20000-lux sun is disabled in
the source so its intensity is inert, and 8 was chosen by rendered measurement).
F2: the reviewer's "12 is closer" claim is FALSIFIED by an actual sweep - 12
gives mean 47.63 vs 8's 45.75 against Unity 46.09; 8 stays best on a multi-metric
criterion. ACCEPT labelling the change a Director-approved baked-GI approximation,
re-rendering the regenerated output, and capturing test_range. Full answers and
the dispatched fixes: scratch/taskmaster-nov22/
REVIEW-DISPOSITIONS-AND-FIX-SPEC.md. REGENERATED frames: visual/
RENDER-FINAL-regenerated.png (mean 47.21, stddev 26.0, warm 351156) and
RENDER-FINAL-test_range.png (mean 88.97; no Unity oracle exists for test_range).

REGENERATED COMPILED FRAME 2026-11-24 (revised; supersedes the in-memory
readings): the actual compiled regenerated output shows a WARM GOLDEN SUNLIGHT
POOL on the floor and NO red/magenta cast (single vision read); dark blue-grey
window panes with no open sky. TWO vision reads of the SAME frame DISAGREE on
long crisp shadows, so NO shadow claim is made - stddev 26.0 vs Unity 32.68 is
consistent with weaker shadow contrast. Full verdict: visual/
REGENERATED-FRAME-VERDICT.md. Numeric residual: mean 47.21 (Unity 46.09),
stddev 26.0 (32.68), warm 38.1% (32.6%). The earlier "intensity is not parity"
note is only PARTLY superseded: brightness/warmth/fog/cast improved, contrast
and shadow structure remain open.

SCOPED RE-REVIEW 2026-11-24 (deleg_3f43681c): verdict "changes required"
with ONE LOW finding R1 - the rewritten report line at UnityHdrpLighting.cs:139
dropped ", directional lux as-is", falsely implying directional lux is scaled by
m_LuxAtDistance^2 (runtime at :80 keeps the lux value for type 1). ACCEPTED and
FIXED by the parent (one-line text change); harness build 0/0 after the fix.
All other checks passed: only the four authorized fixes present, F4's strict
regex correct, runtime expression and clamp byte-unchanged, no new defects.
The same critic was continued for a bounded re-check of R1. No commit until its
final verdict lands.

DIRECTOR DECISION 2026-11-24 (recorded by Task Master): "Fix trees and skybox
100% (i'm confident those are fixable) and then fix lighting to "best ability"."
Work order: trees -> skybox -> lighting. BEST EFFORT now applies to LIGHTING ONLY,
not trees or skybox. This real Director turn is the fresh decision that unblocks
re-tuning directional intensity against the outdoor dusk oracle. Any code change
resets REVIEWED? to NO! on affected tasks and needs a fresh critic "no further
changes" before DONE? YES!. Evidence:
<scratch>/taskmaster-nov22/outdoor-compare/COMPARISON-2026-11-24.md.
Research child sa-0-1b8f9add is pinning the source-authored mechanism; no code
changed yet.

## Clarification Q&A

ANSWERED (Director, 2026-11-23 bedtime directive): "1. All those things we originally mentioned
that were accidentally 'only investigated' 'due to headless verification'. Should now all be
renamed according to our discussion earlier (i.e. skybox doesn't match. lighting doesn't match.
etc.) 2. Open back up the tasks related to these that were supposed to get the fixes in, and
rephrase and empty/move Post-Task details if there are any. These are now fresh. 3. If it makes
sense, create new tasks. 4. Be open to creating NEW tasks based on investigations based on
subagents, given all the work of our conversations today."

ANSWERED (Director, 2026-11-23, oracle counterexample): city--1 has the same black sun tint /
intensity 1 yet renders fine because the 2D Skybox lights materials; "we need to continue without
an oracle for now ... Instead of stating specifically: 'Sun sky color is black and 1 instensity'
I'll just say 'lighting doesn't match' and leave it to you to investigate correctly." No
questions pending; the /goal loop runs unattended. Do not ask the Director.


## Relevant History 

Previously part of task 15 "Scene Visual Fidelity" (closed b11952e), verification-only. At HEAD
6a65cb1 the worker recorded: MergeV2 20,000-lux sun (CCT 3849) + 12-lux fill, Test_Range 5-lux
warm light; `UnityHdrpLighting.cs:31,34,58-80,132-135` (1000 cd/unit, clamp 8);
`UnitySceneExport.cs:652-695` emits `Sandbox.DirectionalLight`; output sun
`7.82938,4.77047,2.59516,1` (20 -> clamp 8), fill `0.012,0.00683,0.00332,1`; verdict "no gap";
critic 2026-11-22 22:10 `no further changes`. Task 10 (archived) recorded the unit/clamp as a
documented approximation.

WHY THIS IS NOT A FIX: "no gap" compared emitted values to our own converter. The Director still
reports lighting doesn't match, and a Dev-editor capture confirmed a flat under-lit viewport.
The four `Open:` lines from old task 15 were cleared on reopening; the sun/lighting symptom got
its own task under the Director's new name. A parallel-session investigation on 2026-11-23
produced `references/engine-schema-conformance.md`, which identifies the hardcoded
`SkyColor "0,0,0,0"`, missing `Intensity` and missing `__version` as objective headless-checkable
defects with the engine's own authored scenes as oracle.


## Stylistic Task Notes:
References to variables like X and Y automatically imply that these are to be exposed properties for developer tweaking.
When programming, portability and DRY are a priority.

## Post-Task Follow Ups 
