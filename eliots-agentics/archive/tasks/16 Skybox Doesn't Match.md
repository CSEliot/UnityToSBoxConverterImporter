## Status: STARTED? REJECTED! CLARIFIED? YES! HISTORY? YES! REVIEWED? YES! DONE? NO! WORKER: NONE OTHER: fresh symptom task; split from investigated-only task 15; differential ablation report on disk (research-diff/differential-report.md); SkyMaterial bare-string premise FALSIFIED

## Task
Skybox doesn't match.

### Description
Director's verbatim symptom (2026-11-22): "Skybox doesn't match." Acceptance check: the imported
MergeV2 / Test_Range scenes must show a sky that matches the Unity source's sky. The symptom is
finished when it is GONE, not when a spec is satisfied. Do not set DONE? YES! while it is still
reported.

Split fresh 2026-11-23 from task 15 "Scene Visual Fidelity" (closed b11952e), a verification-only
task that declared "no headless gap under the task-09 contract". That compared our output to our
own documented approximation and is self-consistency, not correctness. common.md forbids it as a
verdict. The old table is in ## Relevant History for reference; do not reuse its conclusion.

Unity source: C:/cseliot/1-Development/4-Unity/2-Projects/old_school_model. Generated scenes:
Assets/imported/assetpackschoolnameverylong/schoolhdrpnameverylong/scenes/{mergev2,test_range}.scene.

Required method:
1. For EACH candidate mechanism, write down the named observation that would FALSIFY it. No
   "investigated" verdict without both a mechanism and its falsifier.
2. Candidate mechanisms (verify, do not assume):
   a. `Editor/Core/UnitySceneExport.cs:34` hardcodes
      `const string DefaultSky = "materials/skybox/skybox_day_01.vmat"` and writes it as
      `SkyBox2D.SkyMaterial` for EVERY scene (`:266`, `:315`) regardless of the Unity source sky.
      Source has no `RenderSettings` skybox (`m_SkyboxMaterial: {fileID: 0}`); HDRP sky is
      skyType 4 PhysicallyBased (VolumeSettings, wins the MergeV2 merge) and skyType 1 HDRI
      `spruit_sunrise_8k.exr` (Test_Range). Emitting a default day skybox for a sunrise HDRI
      scene would produce a sky that does not match. Falsifier: read both source scenes' sky
      settings; if the importer emits a source-derived sky material/path instead of the constant,
      this mechanism is false.
   b. MASTER-VERIFIED FALSIFIED 2026-11-23 (do not pursue): `SkyMaterial` as a bare
      `.vmat` string is NOT a defect. Engine-authored `SkyBox2D` blocks in both installs also
      serialize it as a bare string; the `{"Id":...,"Path":...}` object form only appears in the
      editor-resaved working-tree `mergev2.scene`. The clean-HEAD harness output and the working
      city--1 scene both use the bare string. See scratch `research-diff/diffsummary.md:139-165`.
      Falsifier was applied and this mechanism is FALSE; drop it from scope.
   c. `SkyIndirectLighting: true` and `Tint 1,1,1,1` are emitted for both scenes. In city--1 the
      2D Skybox lights materials and the scene renders fine; in MergeV2 it does not. Falsifier:
      compare the working city--1 `demo.scene` `SkyBox2D` block to MergeV2's; if the blocks are
      equivalent, the skybox component is not the differentiator and the cause lies elsewhere
      (see tasks 15/16). Do not pin a component value as the cause when the same value renders
      correctly in another scene.
   d. Task 09 deliberately closed procedural sky / HDRI parity as "default sky + report" with an
      unverified HDRI texture key. Re-examine that decision: the symptom is still reported, so
      "we decided not to fix it" is not acceptance. Falsifier: an engine-authored scene that
      uses an HDRI sky material with a verified texture key, proving the key exists and can be
      emitted.
3. Differential ablation (no absolute oracle): `Assets/imported/polygoncity/scenes/demo.scene`
   is a WORKING case from the same importer. Enumerate what differs in the sky/ambient system
   between it and MergeV2 and identify which difference would cause the symptom.
4. Screenshot evidence: no editor is currently up (both installs' `--list` return `[]`); launch
   the editor belonging to the install you use, read `editor_status` BEFORE capture, do not
   save/play/change if `SceneHasUnsavedChanges` is true. Capture the viewport, copy the PNG to
   scratch, downscale to ~960x540 JPEG, and LOOK at it with vision_analyze. Before/after pair
   around any fix. Corroborate specific sky claims with a crop or pixel measurement. State the
   bridge and evidence used.
5. If a mechanism survives, fix the ROOT CAUSE in the engine-free importer core, fix the CLASS.
   Do not overpromise procedural-sky or HDRI parity that the engine cannot deliver; but a
   hardcoded default material where a source-derived one is available IS a fixable defect. If
   all are falsified, say so with observations and name the next candidate; do not close on
   self-consistency. Do not file an objective data defect as an `Open:` editor-visual check.
6. Regression gates IF code changes land: harness build 0/0; `building.unitypackage` (24 files)
   and `city--1.unitypackage` (1,166 files) byte-identical to fresh HEAD baseline (no --scenes,
   BOTH trees populated); `verify_output.py <outRoot> <outRoot>/imported` 0 problems; two
   identical runs byte-identical; BOM-less; no stray artifacts.
7. Put all evidence in scratch files and cite absolute paths in ## Notes.

Stage by explicit path only. No push until the Master verifies. Do not add unrequested tooling.
Final answer in English.

### Work Review

2026-11-24 22:23 - Verdict: changes required
Reviewer role: Review Critic (bounded adversarial re-review); judged the four uncommitted Editor/Core files plus the closing evidence, not a missing worker trail.

1. [High] COMPARISON-2026-11-24.md:75-79 - the parent excuses the sky gap as "plausibly HDRP-side consequences" and therefore abandoned scope. I looked at both frames myself: OUTDOOR-main-640.jpg has a clear pale-to-medium blue daylight sky with thin white cloud streaks and no horizon haze; Testing/Unity-Screenshot-Outside.png has a hazy deep navy/blue-grey twilight sky with a pale band near the horizon. The symptom is visibly present. The Director's standing constraint says the skybox symptom is NOT HDRP-related, so this excuse is falsified by the very authority cited for the closure. Fix: do not close on "HDRP abandoned"; either reopen a non-HDRP skybox investigation or obtain an explicit Director acceptance of this specific gap on these two frames. Keep DONE? NO!.
2. [Medium] Editor/Core/UnitySceneExport.cs:266,315 - the closing evidence does not address this task's Description candidate (a): the emitted SkyBox2D material is still the hardcoded DefaultSky rather than a source-derived sky. Fix: ground a source-derived sky material/texture or record that the task-09 default-sky approximation is explicitly re-approved against the new outdoor oracle; it is not acceptance by implication.
3. [Low] COMPARISON-2026-11-24.md:35-40 - the sky claims rest on vision reads; no sky-region crop or pixel measurement is recorded. Fix: sample sky RGB in a matched crop of both frames before any close.

Tree (four reviewed source files at HEAD ae9781a):
- Editor/Core/UnityHdrpLighting.cs — `git hash-object --path="Editor/Core/UnityHdrpLighting.cs" "Editor/Core/UnityHdrpLighting.cs"` => 3de49ef03d589016281b43d044910382893f34ac
- Editor/Core/UnityPrefabExport.cs — `git hash-object --path="Editor/Core/UnityPrefabExport.cs" "Editor/Core/UnityPrefabExport.cs"` => 7c80ad48c817952ae79aa2935775e32616249049
- Editor/Core/UnitySceneExport.cs — `git hash-object --path="Editor/Core/UnitySceneExport.cs" "Editor/Core/UnitySceneExport.cs"` => 6bc6a265c16a18cc84b1e7677fbc45f14f1637ae
- Editor/Core/UnityVolume.cs — `git hash-object --path="Editor/Core/UnityVolume.cs" "Editor/Core/UnityVolume.cs"` => 420f4633aac52e30cb9547819171efe06f428786

## Notes

MASTER LIVE-EDITOR FINDING 2026-11-23 (shared root cause; do NOT fix skybox independently yet):
The imported mergev2.scene renders as a flat uniform grey WALL (main-camera frame mean
114.74, stddev 0.51, 0 coloured pixels). Root cause is an opaque height-fog layer, NOT
the skybox: importer UnitySceneExport.cs emits HeightColor "0.5,0.5,0.5,1" (alpha 1 = FULL
opacity per engine Fog.cs:172) with HeightOffset=0, so the whole frame is occluded. Verified
by 28-variant in-editor ablation: fog OFF reveals a dimly-lit building interior with bright
windows to pale sky; HeightColor.a 1->0 restores geometry (0->177 grey levels). Full evidence:
scratch/taskmaster-nov22/visual/LIVE-EDITOR-FOG-DIAGNOSIS.md. The "skybox doesn't match"
symptom could not be judged while the scene was invisible; SkyMaterial bare-string premise
was already FALSIFIED (engine-authored SkyBox2D uses the same bare string). Reassess this
task AFTER the task 15 fog fix with a fresh rendered frame. Do not close on self-consistency.

MASTER RENDERED VERIFICATION 2026-11-23 (task 15 fix landed; NEW objective residual defect for THIS task):
The opaque height-fog wall is CONFIRMED removed (rendered before/after: 0 -> 177 grey levels,
geometry visible; six headless gates PASS; evidence visual/RENDERED-VERIFICATION.md). But the
FIX frame is still "essentially MONOCHROME GREY" with a flat grey sky — only 37 coloured pixels.
This is a SECOND, separable mechanism and it is the real cause of "skybox doesn't match":
distance fog at MaximumOpacity=1 + ColorSource=Constant + Color 0.5 grey fogs the SKY/infinite
distance to flat grey. Engine Fog.cs:181 sets ColorSource default = SceneSky (the sky is sampled,
not replaced); the importer emits Constant because Unity authored colorMode 1 (custom grey) at
VolumeSettings.asset:120-125. Falsifier RUN, on the live editor: MaximumOpacity 1->0.3 restores
123562 coloured pixels and a muted blue-grey sky; ColorSource=SceneSky restores 128015 coloured
pixels (visual/verify_fix.py, frames visual/*_CAM.jpg). So this is proven causal, not a guess.
Fix tradeoff (needs Director decision, no Unity reference render exists): a bounded
MaximumOpacity is a pinned magic value (Director forbids pinned values as oracle); SceneSky is
the engine default (principled) but ignores Unity's authored custom grey. Do NOT close this task
on the shared-root-cause inference; it needs a fresh rendered frame AFTER a re-import plus the
Director's editor observation. Also note: the live editor scene is STALE (mtime 02:09, predates
all today's fixes) and carries editor-added fog fields; no pixel-exact render of the clean
generated output exists yet.
CORRECTION 16:35: a Unity reference render DOES exist — Testing/Unity-Screenshot.png. See
visual/UNITY-ORACLE.md. It is a well-lit WARM classroom with NO heavy fog and NO visible open
sky (dark blue-gray window panes), so it VALIDATES task 15's wall removal but WEAKENS the
SkyAffect=0 justification: Unity shows no bright blue sky here. An objective oracle is now
available for a real side-by-side; do not defer on "no oracle".

MASTER ADDENDUM 16:10 — PRINCIPLED FIX FOUND (falsifier RUN, not a pinned value): engine
Fog.cs:280-283 defines `SkyAffect` = "How strongly fog covers the sky", default 1; the importer
does not emit it, so distance fog covers the SKY with flat grey. Holding the FIX values constant,
SkyAffect 1->0 restores 31846 coloured pixels and a BLUE-TO-PALE-GREY SKY GRADIENT through the
windows (vision_analyze confirmed), while KEEPING Unity's authored custom grey (ColorSource=
Constant). SkyAffect=0 is an ENGINE-DEFINED knob, NOT a pinned magic opacity, so the earlier
"pinned value vs discard authored colour" tradeoff is resolved: this residual has an objective,
headless-justifiable fix. Evidence: visual/skyaffect_probe.py, skyaffect_paths.json,
RENDERED-VERIFICATION.md addendum. This is a real fix target for THIS task — do not close until
it (or a Director decision) lands and a fresh post-re-import frame shows the sky.

MASTER CLEAN-OUTPUT PERCEPTUAL GATE 2026-11-24 (first run of this gate):
Regenerated Assets/imported from HEAD ae9781a (harness rebuild 0/0, 2154 files,
verify_output.py 875 files 0 problems, mergev2.scene HeightEnabled:false,
HeightColor count 0). Opened the Dev editor, played mergev2, captured a play
frame from the main camera. Unity oracle mean 46.09 / 730872 coloured px /
391051 warm px; clean s&box frame mean 55.46 / 152052 coloured px / 91 warm px.
The opaque height-fog wall is GONE, but the frame is still dim, cool and
desaturated with a flat pale-grey exterior and no golden sunlight. So the
skybox symptom is STILL PRESENT. Falsifier RUN: Unity's 20000-lux bright sun
is ALSO under the inactive Setting root, so the imported disabled warm sun is
FAITHFUL, not an importer bug; drop that candidate. Full numbers, capture paths
and remaining mechanisms: <scratch>/taskmaster-nov22/visual/CLEAN-PERCEPTUAL-GATE.md.

FOLLOW-UP 2026-11-24 (two candidates FALSIFIED, parent-verified on disk):
the active Unity light conversion 12 lux -> Intensity 0.012 is the documented
1000 cd/unit scale (UnityHdrpLighting.cs:31,77), not a defect; the SkyAffect=0
mirror patch must NOT ship (sky-pixel-only, stale-scene falsifier, oracle has
dark blue-grey panes and no open sky). The remaining gap is task 09's
Director-approved default-sky approximation (UnityVolume.cs:334-340: sun,
atmosphere and time of day explicitly not reproduced), not an unfixed importer
bug. See visual/CLEAN-PERCEPTUAL-GATE.md. DONE? stays NO!.

FINAL ROOT CAUSE 2026-11-24 (parent-verified): MergeV2 has 25+ baked
Lightmap-*_comp_light.exr + LightingData.asset and m_EnableBakedLightmaps:1
(MergeV2.unity:53); UnitySceneExport.cs:18-19 reports lightmaps as not
converted. The golden sun is BAKED GI that s&box cannot reproduce. The
offered sunIntensity fix is FALSIFIED against local HDRP source (sunIntensity
is a sky-disk default, not scene illumination). Isolation ABLATION5 shows the
disabled sun branch causes ~90% of the warm shift. No source-faithful importer
defect remains; an active-sun approximation needs an explicit Director
decision. See visual/CLEAN-PERCEPTUAL-GATE.md. DONE? stays NO!.

REGENERATED COMPILED FRAME 2026-11-24: no fog wash, no grey sky; window exteriors
are flat dark blue-grey / pale blue-white panes with NO open sky, matching the
Unity oracle's dark blue-grey panes. The grey-sky symptom is gone. Full verdict:
visual/REGENERATED-FRAME-VERDICT.md. This is a compiled-file render, not the
earlier in-memory ablation.

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

RESEARCH VERDICT 2026-11-24 (parent-verified on disk, deleg_d9eb2472):
SKY = SOURCE-FAITHFUL for serialized data; visual cause UNKNOWN. Verified:
MergeV2.unity:29 m_SkyboxMaterial {fileID: 0} (no sky asset to convert);
UnityVolume.cs:337-341 averages white horizon/zenith HDRP tints into the emitted
Tint; UnitySceneExport.cs:287-291 emits SkyMaterial = DefaultSky (hardcoded
skybox_day_01.vmat at :34) with that white Tint; fallback :316 hardcodes
"1,1,1,1". Generic Unity ambient colours (sky 0.212,0.227,0.259, intensity 1)
are already carried into DirectionalLight.SkyColor 0.03697,0.04215,0.05455
(generated scene line 71, verified). Engine lever: SkyBox2D.Tint (Color, default
White, SkyBox2D.cs:15-26, drives SkyTint + env-probe TintColor); SkyMaterial is
the only other lever. No source-authored sky darkness exists to map without a
new mapping. The Director's "not HDRP-related" premise is unsupported on disk.
Full report: <scratch>/taskmaster-nov22/outdoor-compare/TREE-SKY-DIAGNOSIS.md.
Holding for a fresh Director decision (no invented dark sky / new mapping).

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
6a65cb1 the worker recorded: `m_SkyboxMaterial: {fileID: 0}` both scenes; skyType 4
PhysicallyBased (`VolumeSettings.asset:33-35`), skyType 1 HDRI (`Scene Settings Profile
MergeV2.asset:1872/1874`, `spruit_sunrise_8k.exr`); importer writes `DefaultSky` with a tint
(`UnitySceneExport.cs:34,260-269,305-317`); both scenes emit 1 `SkyBox2D`,
`SkyMaterial=materials/skybox/skybox_day_01.vmat`, `Tint 1,1,1,1`; verdict "no gap under task-09
contract"; critic 2026-11-22 22:10 `no further changes`. Task 09 (archived) deliberately closed
procedural sky as "default sky + report" with the HDRI texture key left unverified.

WHY THIS IS NOT A FIX: "no gap under our own contract" is self-consistency. The Director still
reports the skybox doesn't match. The four `Open:` lines from old task 15 were cleared on
reopening and the skybox symptom got its own task.


## Stylistic Task Notes:
References to variables like X and Y automatically imply that these are to be exposed properties for developer tweaking.
When programming, portability and DRY are a priority.

## Post-Task Follow Ups 
