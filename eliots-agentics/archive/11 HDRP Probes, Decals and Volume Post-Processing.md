## Status: STARTED? YES! CLARIFIED? YES! HISTORY? YES! REVIEWED? YES! DONE? YES! WORKER: sa-0-548f4431 OTHER: reviewed 23:27 no further changes; HDRP probes, decals and volume post-processing committed

## Task

	Convert HDRP reflection probes and global Volume post-processing, and report the HDRP features
	that have no s&box counterpart (planar probes, decal projectors, static lighting sky, light layers).

### Description

	MergeV2 has 44 ReflectionProbe components, each with HDAdditionalReflectionData
	(d0ef8dc2c2eabfa4e8cb77be57a837c0), plus 1 PlanarReflectionProbe
	(a4ee7c3a3b205a14a94094d01ff91d6b), 2 global Volume components and 2 StaticLightingSky
	components. Test_Range has one of each needed component set. The importer currently:
	- reports 44 ReflectionProbe as unconverted and emits no EnvmapProbe;
	- reports the planar probe, Volumes and StaticLightingSky as MonoBehaviours;
	- does not convert any Volume post-processing (Bloom, Tonemapping, DepthOfField, ColorAdjustments,
	  SSAO, vignette, SSR, path tracing);
	- has no DecalProjector handling (none in the source scenes, but 98 materials are decal-capable).

	s&box has Sandbox.EnvmapProbe (bounds, resolution, priority, tint, feathering, near/far, max
	distance, multi-bounce) and Sandbox.PostProcessVolume with effect components (Bloom, Tonemapping,
	ColorGrading, AmbientOcclusion, DepthOfField). It has no public planar reflection component, no
	rectangular area light, no light-layer mask on lights, and no direct equivalents for SSR, volumetric
	clouds, path tracing or HDRP's full override graph. Those must be reported, not faked.

	The task-09 UnityVolume reader already parses VolumeProfile assets and consumes sky/fog/ambient.
	This task extends it to post-processing and probes.

### Required work

	1. Reflection probes: for each Unity ReflectionProbe with HDAdditionalReflectionData, emit one
	   Sandbox.EnvmapProbe with the probe's local bounds/size/center, resolution, importance/priority,
	   blend/feathering distance, near/far and tint where present. Keep the object's transform. If the
	   HD data is absent, still convert the plain ReflectionProbe with defaults.
	2. PlanarReflectionProbe: report once with count and names; do not create a fake EnvmapProbe or a
	   WaterBody.
	3. Global Volumes: for each distinct global Volume profile, emit a Sandbox.PostProcessVolume
	   GameObject with Priority and BlendWeight from the Volume and the supported effects as child
	   components:
	   - Tonemapping (mode, auto exposure / EV range where it maps)
	   - Bloom (strength, threshold, tint where present)
	   - DepthOfField (focal distance/range/blur)
	   - AmbientOcclusion (intensity/radius)
	   - ColorGrading (temperature/tint/lookup where present)
	   For each override with no s&box counterpart (SSR, path tracing, volumetric clouds, vignette,
	   white balance curves, etc.) add one grouped report line naming the override and the volume.
	   Do not emit a PostProcessVolume for a local Volume; report it.
	4. DecalProjector: if one appears, report it with its material and bounds; do not create a
	   Sandbox.Decal in this task. Add one to the fixture so the report path is tested.
	5. StaticLightingSky and light layers: report once per scene with counts; no conversion.
	6. Mark the consumed ReflectionProbe/Volume/StaticLightingSky components as converted in the
	   step-7 census so they no longer appear as generic "MonoBehaviour (script)" or "ReflectionProbe"
	   losses.

### Acceptance

	- MergeV2 output contains 44 Sandbox.EnvmapProbe components and no "44 ReflectionProbe ... not
	  converted" line; Test_Range contains its one probe. Bounds/positions match the source probes.
	- MergeV2 output contains a Sandbox.PostProcessVolume for each distinct global profile (2 expected),
	  with at least Tonemapping/Bloom/DepthOfField/AmbientOcclusion where the profile supplies data;
	  unsupported overrides are named in one grouped line per volume.
	- The planar probe, StaticLightingSky, light layers and decal projector are reported, not silently
	  dropped; no generic HDRP MonoBehaviour loss lines remain.
	- building (26 files) and city--1 (1,192 files) stay byte-identical; their scenes have no HDRP
	  probes/volumes.
	- The HDRP fixture asserts one EnvmapProbe, one PostProcessVolume, one planar report and one decal
	  report; harness builds 0 warnings / 0 errors; two runs byte-identical; verify_output.py 0 problems.

## Notes

	Implementation home: UnitySceneExport.cs component dispatch and step-7 census, the task-09
	UnityVolume reader, and UnityPrefabExport.cs:1444 if a prefab carries a probe/volume. Reflection
	probe bounds are in the component's local space; preserve the object transform.

	PostProcessVolume is a scene GameObject with effect components as children; do not put effects on
	the same object if the engine expects a volume parent. Check the engine's PostProcessSystem parent
	lookup before choosing the layout. The volume object needs a stable GUID and no renderer.

	The 44 probes are real scene objects, not prefab instances; ensure the generated scene's node count
	and roots remain consistent with task 05's loss accounting.

## Clarification Q&A

	Q: Should local Volumes be blended into the global one?
	A: No. Report them; only global profiles are applied.

	Q: Should decals become Sandbox.Decal?
	A: Not in this task. Report the projector and its material; a later task can map the decal asset.

## Relevant History

	Task 09 introduced the VolumeProfile reader and environment conversion. Task 10 handles lights and
	cameras. This task closes the remaining scene-level HDRP objects without overpromising unsupported
	features.

## Stylistic Task Notes:

	References to variables like N and M automatically imply that these are to be exposed properties
	for developer tweaking, if they become code knobs.

## Skills To Know: for you and/or subagents:
	sbox-offline-compile-check
	unityimporterplus
	unity-to-sbox-conversion
	sbox-scene-prefab-inspection

## Evidence

	Gate run 2026-11-21 22:45 JST, working tree = 597523a + this task's changes (uncommitted). Artifacts:
	C:/Users/Eliot/AppData/Local/hermes/profiles/hermes-sbox/cache/scratch/task11/

	Build: dotnet build Documentation/Unity-To-SBox-Automated/harness -c Release -> 0 Warning(s), 0 Error(s).

	Fixture, repacked after the new scene objects, the four new profile overrides and the non-origin Volume
	(pack_fixture.py <folder> <out>): 25 assets, tar md5 1ff2815532db7b1a484e9a7f542caede, archive 9670 bytes
	md5 d329b60b3cd517adedd0920034376307.
	harness --scenes hdrp.unitypackage run1 run1.json and again run2: assets=24 converted=21 files=48; diff -r run1
	run2 identical; verify_output.py run1 run1/imported/fixture/scenes/hdrp.scene -> checked 1 files, 0 problems.
	hdrp.scene carries 1 Sandbox.EnvmapProbe and 1 Sandbox.PostProcessVolume (Priority 0, BlendWeight 1, SceneVolume
	Infinite) attached to the existing "Global Volume" object - the only node of that name - at
	-78.74,-157.48,39.37 (Unity local 4,1,-2) with tags "postprocess" and four child objects: Tonemapping (ACES),
	Bloom (0.8 / threshold 1.2 / tint 1,0.9,0.8), DepthOfField (472.44 / 1102.36) and AmbientOcclusion (1.4). The
	probe has Bounds Mins -196.85,-196.85,-118.11 and Maxs 196.85,196.85,118.11 (Unity box 10x6x10 at offset 0),
	sits at 0,0,78.74 (Unity 0,2,0), Resolution Small (128), Priority 1, Feathering 39.37, ZNear 11.811, ZFar
	39370, Projection Box.
	Fixture report lines: "1 reflection probe(s) converted ...", "1 PlanarReflectionProbe component(s) were not
	converted (Planar Probe)", "1 DecalProjector component(s) were not converted (Decal Projector [material
	0e34fa04c86b6a3ae251ab682d02ebe3, size 2,2,1])", "All 3 HD light(s) and 1 probe(s) use HDRP's default light layer",
	"Global Volume: options on the converted overrides have no s&box field and were not converted: Bloom scatter 0.7,
	DepthOfField focusMode 0, ScreenSpaceAmbientOcclusion radius 1.5, m_MaximumRadiusInPixels 40"; no
	HDAdditionalReflectionData loss line.

	Regressions without --scenes, against HEAD 597523a built in a scratch worktree (git worktree add --detach head
	597523a; same harness line endings): building head 24 files vs current 24 files, diff -r identical; city--1 head
	1,166 files vs current 1,166 files, diff -r identical. Counts unchanged (assets=15 converted=10 files=24; assets=729
	converted=776 files=1166). The two report JSONs differ only in ElapsedMilliseconds and Destination.

	Full corpus: harness --scenes old_school_project.unitypackage oldschool oldschool.json -> assets=1277 converted=936
	files=2115 (same file count as the task-10 baseline). mergev2.scene: 44 Sandbox.EnvmapProbe, 2
	Sandbox.PostProcessVolume, each attached to its own source Volume object ("Scene Settings" at 0,0,0 with
	Tonemapping/Bloom/DepthOfField/AmbientOcclusion; "Volume Settings" at 2125.98,2480.31,-301.18054 from Unity
	(-63,-7.65,54) with no effect because VolumeSettings supplies none), 1 PlanarReflectionProbe line, a 0
	DecalProjector line, "2 StaticLightingSky component(s)", "All 119 HD light(s) and 44 probe(s) use HDRP's default
	light layer", and no "44 ReflectionProbe" or "HDAdditionalReflectionData ... not converted" line. test_range.scene:
	1 EnvmapProbe, 1 PostProcessVolume ("Volume Settings" at -18.165453,-72.618004,-301.18054 with the four effects),
	1 StaticLightingSky line, 0 decal line, "All 1 HD light(s) and 1 probe(s)".

	Probes against the source (scratch check_probes.py, output probe-check.txt): recomputes each MergeV2 probe's expected
	EnvmapProbe from MergeV2.unity (Unity m_BoxSize/m_BoxOffset, HD influence shape and blend distances, HD frustum
	resolution, m_Importance, clip planes, m_BlendDistance, HD multiplier) and compares it with the generated scene:
	source 44, generated 44, matched 44, field mismatches 0, exit 0. Fields compared: object name, local position, Bounds
	Mins/Maxs, Resolution, Priority, ZNear, ZFar, Feathering, Projection, TintColor.

	Mapping decisions a reviewer should check:
	- Mode is Realtime on every probe because Unity's baked cubemaps are not in the package and a Baked probe would render
	  nothing; the report says so once per scene.
	- HDRP lighting.m_FadeDistance is not mapped to EnvmapProbe.MaxDistance (fade-out distance vs realtime update gate),
	  so the engine default 512 is kept.
	- SSAO radius is metres in HDRP and screen pixels in s&box: only intensity converts, and the unit mismatch is a
	  report line per volume.
	- Tonemapping None (HDRP 0) becomes HableFilmic with a report line (s&box has no untonemapped mode); Neutral -> AgX;
	  ACES and Custom -> ACES.
	- A PostProcessVolume attaches to the Unity Volume's own object (looked up by its GameObject GUID), so a Volume yields
	  one object, at the transform Unity gave it, with the effects as its children - which is what PostProcessSystem's
	  AddEffects (FindMode.EnabledInSelfAndDescendants) collects. Only a Volume inside a prefab instance, which has no
	  plain node to attach to, keeps a root of its own.
	- Volume lines are written once per emitted PostProcessVolume, so two Unity Volumes sharing a profile (and therefore
	  one object) are one line, not two.
	- Overridden options on a mapped effect that s&box has no field for are named in the per-volume note (Bloom scatter
	  and quality, Tonemapping's curve parameters, DOF focus mode, SSAO's metre/pixel radius), never dropped silently;
	  unset object references such as lutTexture {fileID: 0} are not reported as values.
	- The old merged "N HDRP volume override(s) are not converted by the environment step" line is replaced by one
	  grouped unsupported-override line per volume, so every override is named once with the volume it came from.
	- No checker or tooling was added to the repo; check_probes.py exists only in the scratch dir.

	Files changed: Editor/Core/UnityHdrpProbes.cs (new), Editor/Core/UnitySceneExport.cs, Editor/Core/UnityVolume.cs,
	Editor/Core/UnityPrefabExport.cs, Documentation/Unity-To-SBox-Automated/fixtures/hdrp/Assets/Fixture/Scenes/HDRP.unity,
	.../Assets/Fixture/Settings/HDRPVolumeProfile.asset, Documentation/Unity-To-SBox-Automated/fixtures/hdrp.unitypackage,
	and this task file. Not committed: awaiting the Review Critic.

## Post-Task Follow Ups

### Work Review

2026-11-21 22:48 - Verdict: changes required
Tree: 597523a + uncommitted task-11 changes
Files: Editor/Core/UnityHdrpProbes.cs 023488ac96d6b74a2aba3e117645e4fadc4499a7; Editor/Core/UnitySceneExport.cs 9d37232d700b7f8831e52a8bc33b4754c3df3895; Editor/Core/UnityVolume.cs 6b5211b5751ef3db8e73f807c705dfd9066b4753; Editor/Core/UnityPrefabExport.cs f56bd8a54012372b38fb43cf0c243b3e15da05ad
1. [Medium] PostProcessVolume is placed at the origin, not at the Volume's transform. UnityVolume.cs:266 passes block.Id (the Volume MonoBehaviour fileID) as VolumePost.ObjectId, but UnitySceneExport.cs:367 looks it up in c.Transforms, which UnityPrefabExport.cs:862 keys by GameObject fileID, so the lookup always misses and NoTransform is used. MergeV2 "Volume Settings" is at Unity (-63,-7.65,54) -> s&box (2125.98,2480.31,-301.18), yet its PPV is at 0,0,0 while the empty converted twin has the right position; the fixture volume is at the origin so the gate cannot see it, and the Evidence "at the Volume's transform" is false. Fix: pass IdOf(Field(block.Body,"m_GameObject")) into Post and add a non-origin volume to the fixture.
2. [Low] Each global Volume still emits an empty source object beside the new PPV root, giving two objects of the same name (fixture: two "Global Volume" nodes). Fix: attach the PostProcessVolume to the existing object, or say why the twin is kept.
3. [Low] UnitySceneExport.cs:360-362 keeps one PPV per distinct profile while UnityVolume.cs:268 already prints one PPV line per volume, so two volumes sharing a profile would report two conversions for one object. Fix: report at export time per emitted PPV.
4. [Low] Mapped overrides drop unsupported sub-fields silently (Bloom scatter 0.7 in the fixture, Tonemapping useFullACES/gamma, DOF focusMode/nearFocusEnd, SSAO max radius in pixels) with no report line. Fix: fold them into the per-volume note.
Verified: build 0/0; fixture archive 61a8996e6af72ebd04cb6c7483825221 / tar 684f795feb4e1112e953b3dbed7d9b53 / 9653 bytes / 25 assets (24 counted + 1 script); two runs identical; verify_output 0 problems; 1 EnvmapProbe, 1 PPV Infinite with 4 child effects, 1 planar line, 1 decal line, one grouped light-layer line, no probe-loss census line; building 24 and city--1 1,166 byte-identical to a 597523a worktree; old_school 2,115 files, MergeV2 44 probes/2 PPV/2 StaticLightingSky, Test_Range 1/1; check_probes.py 44/44 matched, 0 mismatches; probe bounds basis agrees with UnityPrefabExport.Position/Scale; probes come from class-215 + HD data only (no Renderer/MeshCollider path); only the four allowed overrides map and only on m_OverrideState:1; local volumes report-only; no stray tooling (check_probes.py is scratch-only).

2026-11-21 23:05 - Worker reply: all four findings fixed, gates re-run on the new bytes (REVIEWED? NO!, DONE? NO!).
1. [Medium] UnityVolume.Read now passes IdOf( Field( block.Body, "m_GameObject" ) ) into Post, so the placement lookup
   in Converter.Transforms hits. Fixture: the Volume moved to Unity local (4,1,-2) -> the PPV node is at
   -78.74,-157.48,39.37. Corpus: MergeV2 "Volume Settings" PPV is now at 2125.98,2480.31,-301.18054 (the value the
   review computed) and Test_Range's at -18.165453,-72.618004,-301.18054.
2. [Low] The PPV attaches to the Unity Volume's existing node (found by GUID with UnitySceneExport.Find, guarded against
   prefab-instance stubs), so a Volume is one object at its own transform with the effects as children; only a
   prefab-hosted Volume falls back to a root of its own. The fixture now has exactly one "Global Volume" node, tagged
   "postprocess", carrying the PPV and four effect children.
3. [Low] AddPostNotes moved out of Read and called by the scene export once per emitted PPV, so volumes sharing a profile
   are reported once. Corpus line counts are unchanged because MergeV2's two volumes have distinct profiles.
4. [Low] VolumePost.Dropped records overridden options on the mapped effects that s&box has no field for, summarised per
   override in one note per volume, e.g. "Tonemapping useFullACES 0, toeStrength 0, toeLength 0.5 and 14 more, Bloom
   quality 1, scatter 0.7, dirtIntensity 0 and 4 more, DepthOfField ..., ScreenSpaceAmbientOcclusion ..."; unset object
   references (lutTexture {fileID: 0}) are skipped. Fixture line: "Bloom scatter 0.7, DepthOfField focusMode 0,
   ScreenSpaceAmbientOcclusion radius 1.5, m_MaximumRadiusInPixels 40".
Re-gated on these bytes: harness build 0/0; fixture repacked (25 assets, tar md5 1ff2815532db7b1a484e9a7f542caede,
archive 9670 bytes), 48 files, two runs byte-identical, verify_output 0 problems, 1 EnvmapProbe, 1 PPV, 1 planar line,
1 decal line, one light-layer line; building 24 and city--1 1,166 files identical to the 597523a worktree baseline;
old_school --scenes 2,115 files with MergeV2 44 probes / 2 PPV / 2 StaticLightingSky and Test_Range 1/1;
check_probes.py 44/44 matched, 0 mismatches. The Evidence section is updated in place. No commit.

2026-11-21 23:27 - Verdict: no further changes
Tree: 597523a + uncommitted task-11 changes (post-fix bytes)
Files: Editor/Core/UnityHdrpProbes.cs 023488ac96d6b74a2aba3e117645e4fadc4499a7; Editor/Core/UnitySceneExport.cs d803e3590d666cea4c76e8c3609d8b43c9f2ac73; Editor/Core/UnityVolume.cs c3b0f68d389bb02356510f3b7dc2af97147ea43e; Editor/Core/UnityPrefabExport.cs f56bd8a54012372b38fb43cf0c243b3e15da05ad
Re-check of the four findings on the new bytes:
1. [Medium, fixed] UnityVolume.cs:280 now passes IdOf(Field(block.Body,"m_GameObject")) into Post, and the placement lookup in UnitySceneExport.cs:381 hits. Verified: fixture PPV at -78.74,-157.48,39.37 (Unity 4,1,-2, the fixture node was moved accordingly); MergeV2 "Volume Settings" at 2125.98,2480.31,-301.18054, exactly the position the first review computed; Test_Range "Volume Settings" at -18.165453,-72.618004,-301.18054.
2. [Low, fixed] One object per Volume, at its own transform, tagged "postprocess", PPV component plus four effect children and no duplicate root: UnitySceneExport.cs:376-385 finds the existing node by StableGuid and falls back to a root only when there is no plain node (prefab-hosted volume). The fixture root list has a single "Global Volume"; MergeV2 has one "Scene Settings" (0,0,0) and one "Volume Settings" (2125.98,...), each with the expected children, and no second root of either name.
3. [Low, fixed] AddPostNotes has exactly one caller, UnitySceneExport.cs:405, i.e. once per emitted PPV; the duplicate call in UnityVolume.Read is gone. Report carries exactly one PPV line per emitted volume: fixture 1, MergeV2 2, Test_Range 1, no repeats.
4. [Low, fixed] UnityVolume.cs:476-495 records overridden options on the mapped effects that s&box has no field for, and AddPostNotes names them per volume. Fixture line: "Global Volume: options on the converted overrides have no s&box field and were not converted: Bloom scatter 0.7, DepthOfField focusMode 0, ScreenSpaceAmbientOcclusion radius 1.5, m_MaximumRadiusInPixels 40." MergeV2/Test_Range get the same line for the Scene Settings Profile profile; unset object references are skipped.
Verified independently on these bytes: harness build 0 Warning(s) 0 Error(s); fixture archive d329b60b3cd517adedd0920034376307, uncompressed tar 1ff2815532db7b1a484e9a7f542caede, 9,670 bytes, 75 entries = 25 assets (harness counts 24 converted 21 with 1 excluded script), 48 output files, two runs byte-identical, verify_output 1 file 0 problems; fixture scene keeps 1 EnvmapProbe (Bounds -196.85,-196.85,-118.11 / 196.85,196.85,118.11, pos 0,0,78.74, Small, Priority 1, Feathering 39.37, ZNear 11.811, ZFar 39370, Box), 1 PostProcessVolume Infinite with Tonemapping ACES / Bloom 0.8-1.2-(1,0.9,0.8) / DOF 472.44-1102.36 / AO 1.4, 1 PlanarReflectionProbe line, 1 DecalProjector line, one grouped light-layer line, no probe-loss census line; building 24 files and city--1 1,166 files byte-identical to the 597523a worktree baseline built in the first review (city still exits 1 on its 3 pre-existing unresolved assets, same both sides); old_school --scenes 2,115 files, MergeV2 44 Sandbox.EnvmapProbe / 2 PostProcessVolume / 2 StaticLightingSky / 1 planar line / 0 decal line / "All 119 HD light(s) and 44 probe(s)", Test_Range 1 / 1. UnityHdrpProbes.cs and UnityPrefabExport.cs are byte-unchanged from the reviewed versions, no checker or tooling was added to the repo (check_probes.py stays in the scratch dir), and the only tree changes are the four reviewed C# files, the three HDRP fixture files and this task file; the deleted eliots-agentics/tasks/01-05 files are mirrored by untracked .humans-only/archive copies and are Master housekeeping, not task work.
Notes, no change required: [Low] the AO radius is now named twice per volume (the metres-vs-pixels line plus the dropped-options list); [Low] on a large HDRP profile the dropped-options summary truncates ("... and 14 more"), which is honest but long. Neither affects the gate.