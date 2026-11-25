## Status: STARTED? YES! CLARIFIED? YES! HISTORY? YES! REVIEWED? YES! DONE? YES! WORKER: sa-0-0979ea71 OTHER: reviewed 22:19 no further changes; HDRP lights and cameras committed

## Task

	Convert HDRP light units and camera data, including area-light reporting and main-camera
	resolution.

### Description

	HDRP stores physical light values in HDAdditionalLightData, not in Unity's Light component. On
	MergeV2 there are 119 lights: 2 directional, 63 spot, 9 point and 45 rectangle/area lights
	(m_Type 3). 90 are authored in Lumen, 29 in Lux (m_LuxAtDistance 1); 108 have shadows enabled
	(m_Shadows.m_Type 1, Hard) and 11 are off. Colour temperature is on the Unity Light: 119 values in
	five groups (3613, 3849, 6570, 7614, 15000 K), and 88 lights have m_UseColorTemperature 1; the
	other 31 do not.

	The importer reads Unity m_Intensity and ignores HDAdditionalLightData. Consequences:
	- 45 area lights are dropped with one "area light not converted" warning each; s&box has no
	  rectangular light.
	- The 90 lumen lights are converted as if their intensity were a unitless multiplier; the 29 lux
	  lights land at raw lux values.
	- The 20,000-lux sky sun becomes LightColor 19573,11926,6488; the 12-lux fill light is also not
	  normalised. The scene is blown out or inconsistent by orders of magnitude.
	- HD dimmer and shadow settings are ignored.
	- 6 cameras are plain scene blocks; HEAD already emits exactly one IsMainCamera true, so the earlier
	  all-false reading was stale and no prefab-camera machinery is justified. HDAdditionalCameraData
	  background/clear mode, antialiasing and volume mask are not read, and exposure lives in the Volume
	  (task 09's reader), not the camera.

	The Unity Light path is UnitySceneExport.cs:495-531; component dispatch is
	UnityPrefabExport.cs:1444. The source camera data is in HDAdditionalCameraData
	(guid 23c1ce4fb46143f46bc5cb5224c934f6).

### Required work

	1. Read HDAdditionalLightData on each light's GameObject (script guid
	   7a68c43fe1f2a47cfa234b5eeaa98012): m_Intensity, m_LightUnit, m_LightDimmer, m_Range,
	   m_SpotAngle, m_InnerSpotPercent, m_Shadows/m_ShadowResolution and any volumetric fields. Verify
	   the exact serialized names in the source scenes before coding; do not guess.
	2. Define and document one light-unit table for s&box:
	   - Lumen: convert to candela for point/spot (lm / 4π) and then through a single documented
	     exposure scale.
	   - Lux: convert through the same exposure scale for directional lights.
	   - Candela: direct through the same scale.
	   - EV: report only; do not guess.
	   Preserve relative brightness across the 119 lights, clamp a single light so the 20,000-lux sun
	   cannot emit a 19,573-magnitude LightColor, and record the scale in Notes and the report.
	3. Apply m_LightDimmer and the HD range/spot angles; keep the existing CCT application from the
	   Unity Light and ensure it is not applied twice. Keep the existing shadow flag.
	4. Area lights: no s&box rectangular light. Report them as one grouped Information line with the
	   count and up to five names; do not add a fake point/spot unless the task explicitly chooses an
	   approximation, in which case report it and its parameters.
	5. Cameras: verify the tagged/enabled/active selection on the fresh scene output; only add
	   prefab-instance resolution if a camera inside an instance is actually missed. Read
	   HDAdditionalCameraData's background/clear mode, antialiasing and volume mask; map what
	   CameraComponent supports and report the rest. Use the task-09 Volume reader for Exposure and
	   report its EV100 range; AutoExposure is a multiplier tonemap, not EV100.
	6. Extend the HDRP fixture with one directional lux light, one lumen spot, one area light, and two
	   scene cameras (one tagged main, one untagged), and assert the unit conversion and main-camera choice.

### Acceptance

	- The 45 area lights produce one grouped report line, with count and samples; no per-light warning
	  spam.
	- The 90 lumen and 29 lux lights carry a documented scale; their relative ordering matches the
	  source intensities; the 20,000-lux sun no longer produces a 19,573 LightColor. Record the actual
	  converted values for three representative lights in Notes.
	- MergeV2 has exactly one camera with IsMainCamera true; all 6 cameras carry the HDRP background or
	  a documented fallback. Test_Range's single camera is main.
	- Dimmer and shadow flags are reflected; CCT is applied once; area lights are not silently lost.
	- The fixture asserts the same cases; building and city--1 stay byte-identical; harness builds
	  0 warnings / 0 errors; two runs byte-identical.

## Notes

	Implementation home: UnitySceneExport.Light (:495-531), a new HDAdditionalLightData reader, the
	camera call at UnityPrefabExport.cs:1444 and MainCamera() at :879-894. Use the task-09
	UnityVolume reader for exposure; do not duplicate profile parsing.

	Physical-unit parity is impossible in s&box (Light.Intensity is a 0-10-ish multiplier, not a
	photometric unit). The deliverable is a documented, consistent approximation, not a claim of
	photometric accuracy. Put the table and the chosen scale in the task Notes and the import report.

	Implemented: Editor/Core/UnityHdrpLighting.cs holds the unit table, the scale and the grouped
	report; UnitySceneExport.Light/Camera take the sibling HD component body; Converter carries the
	HDAdditionalLightData/HDAdditionalCameraData maps; UnityVolume reads the Exposure override.

	Field names verified in the source scenes before coding, and the task's list is split across two
	components: m_Intensity, m_LightUnit, m_LuxAtDistance, m_LightDimmer, m_InnerSpotPercent,
	m_ShadowResolution{m_Override,m_UseOverride,m_Level}, m_ShadowDimmer, m_VolumetricDimmer,
	m_VolumetricFadeDistance, m_AreaLightShape and m_SpotLightShape live on HDAdditionalLightData
	(guid 7a68c43f...); m_Range, m_SpotAngle, m_InnerSpotAngle, m_Color, m_ColorTemperature,
	m_UseColorTemperature and m_Shadows{m_Type} live on the Unity Light. There is no m_Range and no
	m_SpotAngle on the HD component, so "HD range" and "HD spot angle" are the Unity Light's.

	Light-unit table (one scale, no per-light special case):
	  Lumen    -> candela = value / 4pi (isotropic mean over the sphere; only for spot/point)
	  Lux      -> candela = value x m_LuxAtDistance^2 for a punctual light (HDRP's E = I / d^2,
	              m_LuxAtDistance is 1 in this corpus); a directional light keeps the lux value
	  Candela  -> itself
	  EV100    -> reported only, never guessed: the Unity intensity is kept and the count is reported
	  then s&box intensity = candela x m_LightDimmer / 1000, clamped at 8 per light.
	1 s&box intensity unit = 1000 candela (CandelaPerUnit), MaxIntensity = 8. The clamp exists only
	for the one outlier: the 20,000-lux sun converts to 20 and is clamped, every other light in
	MergeV2 stays below 2, so their ordering matches the source exactly. Dimmer multiplies before
	the clamp. The scale is a deliberately documented exposure choice, not photometric parity: the
	sun-to-fill ratio is ~1,600:1 in the source and a single linear map preserves exactly that.

	CCT is still applied once, in the Unity Light path (m_UseColorTemperature gates it); the HD
	component carries no colour temperature, so nothing can apply it twice. The shadow flag is
	unchanged (Unity m_Shadows.m_Type); HD per-light shadow resolution/dimmer have no counterpart
	and are named once in the report.

	Main camera: the task's premise does not hold for this package. All six MergeV2 cameras are
	plain, non-stripped scene objects (tag MainCamera, enabled, active, depth -1, transform chains
	ending at 2006277128 / 161307974), and MainCamera() already returns exactly one of them; the
	fresh HEAD tree run (scratch/t10/head-os) produced "Main Camera (4)" as the only
	IsMainCamera true. No prefab-instance camera machinery was added. The fixture therefore asserts
	the deterministic rule with scene objects: tagged beats untagged, then the highest depth, then
	file order.

	HDRP camera data mapped: clearColorMode 1 (Color) writes backgroundColorHDR (already linear);
	clearColorMode 0 (Sky) keeps Unity's m_BackGroundColor linearised as the fallback; clearColorMode 2
	(None) also falls back to the Unity background (no corpus camera uses it); clearDepth 0
	maps to ClearFlags Color instead of All; antialiasing (0 None / 1 FXAA / 2 TAA / 3 SMAA) maps to
	CameraComponent.DisableAntiAliasing because s&box has one flag. The volume mask, probe mask,
	physical camera parameters and per-camera frame settings have no CameraComponent counterpart and
	are named once per file. The DisableAntiAliasing key is written only for a camera that carries
	HDAdditionalCameraData, so a non-HDRP camera's property set is unchanged byte for byte.

	Exposure comes from the Volume, not the camera: UnityVolume resolves the Exposure override
	(guid 2d08ce26990eb1a4a9177b860541e702) and reports its EV100 range. It is not mapped to
	CameraComponent.AutoExposure because that setup is a multiplier tonemap (Enabled/Compensation/
	MinimumExposure 1..3/MaximumExposure/Rate), not an EV100 range with fixed mode; mapping one onto
	the other would invent numbers. Scene Settings Profile MergeV2 carries Exposure mode 4
	(AutomaticHistogram), EV100 2..14; both scenes report that range. The fixture reports EV 4..16.

	### Task 10 evidence (worker run)

	Command (native paths; the harness driver is
	Documentation/Unity-To-SBox-Automated/harness, IUP_ROOT=project):
	  dotnet bin/Release/net10.0/harness.dll --scenes <pkg> <out> <report.json>
	  dotnet bin/Release/net10.0/harness.dll <pkg> <out> <report.json>          (no --scenes)
	Artifacts: scratch/t10final (r1), scratch/t10final2 (r2, Assets diff empty), scratch/t10final3
	(final report wording; Assets identical to r1); fixture runs in the worker scratch. The earlier
	scratch/t10 paths were not retained.

	Harness build: 0 warnings / 0 errors (harness csproj compiling Editor/Core/*.cs).

	MergeV2 (t10final3, --scenes, 2115 files, same count as HEAD):
	  - "45 area light(s) not converted; s&box has no rectangle, disc or tube light: Point Light (10),
	    Point Light (11), AreaLightNeon, …" — one line, was 45 lines at HEAD.
	  - "HDRP light units converted at 1 s&box intensity unit = 1000 cd: 90 lumen (value / 4 pi),
	    29 lux (x m_LuxAtDistance^2, directional lux as-is), 0 candela; 1 clamped at 8. The 45 area
	    lights are counted in these totals but are not converted."
	  - "HDRP auto exposure (mode 4, EV100 2 to 14, compensation 0) is reported, not mapped."
	  - "6 HDRP camera(s) read HDAdditionalCameraData: 0 clear to a solid colour instead of the sky,
	    0 request antialiasing."
	  - mergev2.scene: 74 lights converted (119 - 45 area), 6 CameraComponents, exactly 1
	    IsMainCamera true ("/Setting/Main Camera (4)"); all six carry the HDRP background path.
	    Test_Range: 1 light, 1 camera, main true.
	  - Converted values, three representatives:
	      20,000-lux sun (/Setting/Scene Setting/Directional Light): 20000/1000 = 20 -> clamped 8,
	        LightColor "7.82938,4.77047,2.59516,1" (HEAD: "19573,11926,6488,1").
	      12-lux fill (/JapaneseSchoolSettings/Lights/Directional Light): 0.012 ->
	        "0.012,0.00683,0.00332,1".
	      2,000-lux spot (/Setting/Light/Spot Light): 2.0 -> "2,1.80932,1.34489,1", ConeInner 10.3359 /
	        ConeOuter 39.45, Shadows true (matches the source m_SpotAngle 78.9 full cone).
	      (Fixture, the only dimmer != 1 in either package) 1,500-lumen spot, dimmer 0.5:
	        1500 / 4pi = 119.37 cd x 0.5 / 1000 = 0.0597 -> "0.05968,0.02288,0.00697,1".

	HDRP fixture (f1 == f2, byte for byte; 48 files):
	  - HD Light (directional, 20,000 lux) LightColor "8,7.23729,5.37955,1" — clamped, no 19,573.
	  - Lumen Spot: "0.05968,0.02288,0.00697,1", ConeInner 12 / ConeOuter 30 (m_InnerSpotPercent 40),
	    Shadows true, Radius 314.96.
	  - Area Light: no component, one grouped line "1 area light(s) not converted … Area Light".
	  - Main Camera (tag MainCamera, clearColorMode 1): IsMainCamera true, BackgroundColor
	    "0.1,0.2,0.4,1", ClearFlags All, DisableAntiAliasing false (antialiasing 2 = TAA).
	  - Second Camera (Untagged, sky): IsMainCamera false, BackgroundColor "0.0331,0.07324,0.18732,1"
	    (Unity colour linearised), DisableAntiAliasing true (antialiasing 0).
	  - "HDRP auto exposure (EV100 4 to 16, compensation 0.5) is reported, not mapped …".
	  - Fixture package repacked with pack_fixture.py: 25 assets, tar 133120 bytes, tar md5
	    f16a1e1b3ef95aa09cbc656da4c1ff84 (archive md5 78f656f7af51cc9b42e08f0290bf4bef).

	Regression gates (no --scenes, compared with a HEAD 6f2b388 worktree baseline):
	  - building: 24 files, 15 assets, 0 warnings, 0 unresolved — byte-identical.
	  - city--1: 1,166 files, 729 assets, 3 warnings, 3 unresolved — byte-identical.

	Determinism: os-r1 vs os-r2 manifests identical (2,115 files, md5 each); f1 vs f2 identical.
	HEAD vs r1: only mergev2.scene and test_range.scene differ; nothing else moved.

## Clarification Q&A

	Q: Should the 20,000-lux sun be clamped or scaled?
	A: Scale the whole set consistently; clamp only to prevent a single outlier from exceeding the
	s&box range. Do not special-case one light by name.

	Q: What about the 45 area lights?
	A: Report them. A rectangle-light approximation is a follow-up, not this task.

## Relevant History

	Tasks 01-05 fixed scene instancing and loss reporting. Task 09 converts the environment and owns the
	Volume reader. This task uses that reader for exposure but does not convert post-processing.

## Stylistic Task Notes:

	References to variables like N and M automatically imply that these are to be exposed properties
	for developer tweaking, if they become code knobs.

## Skills To Know: for you and/or subagents:
	sbox-offline-compile-check
	unityimporterplus
	unity-to-sbox-conversion
	sbox-scene-prefab-inspection

## Post-Task Follow Ups

### Work Review

2026-11-21 22:19 - Verdict: no further changes

1. No [High]/[Medium]/[Low] findings. Re-verified the Exposure fix against the final tree:
   - UnityHdrpLighting.cs:58-80 keeps the one unit table: Lumen lm / 4π (:68), Lux × m_LuxAtDistance²
     with directional lux as-is (:70-74), Candela direct (:69), EV100 report-only (:75); the dimmer
     multiplies before the clamp (:77-79). One grouped area line (:129); the unit line now names the
     area lights it counts but does not convert (:133).
   - UnityVolume.cs:311-319 reads Exposure mode as a mode, not a bool: 0 is Fixed (fixedExposure),
     every non-zero mode is reported as automatic with its mode number. Matches HDRP's ExposureMode
     (Fixed 0, Automatic 1, CurveMapping 2, UsePhysicalCamera 3, AutomaticHistogram 4) in the
     installed high-definition package; the defaults limitMin -1 / limitMax 14 match the record.
   - Harness rebuild 0 warnings / 0 errors; fixture rerun on the current code (--scenes, exit 0):
     2 lumen / 1 lux / 0 candela / 1 clamped, "1 area light(s) not converted … Area Light" once,
     "The 1 area lights are counted in these totals but are not converted.", auto exposure (mode 1,
     EV100 4 to 16, compensation 0.5), 2 HD cameras (1 main, AA false/true), sun "8,7.23729,5.37955,1",
     lumen spot "0.05968,0.02288,0.00697,1" with ConeInner 12 / ConeOuter 30 / Radius 314.96.
   - t10final3 (post-fix run, 22:15): 2,115 files; mergev2.scene has 6 CameraComponents and exactly one
     IsMainCamera true, sun "7.82938,4.77047,2.59516,1", the 45-area line once, 90 lumen / 29 lux /
     0 candela / 1 clamped; test_range.scene 1 camera / 1 main; diff.txt empty against r1.
   - building 24 files and city--1 1,166 files are byte-identical to a rebuilt HEAD 6f2b388 baseline
     (the worktree checkout's CRLF line endings were normalised first; that alone changes embedded
     template line endings and is not a code difference). No stray harness or checker files in the
     tree; nothing outside the task-scoped paths except the task-doc archival under eliots-agentics/.

2. Non-blocking observations (no action for this task): UnityHdrpLighting.cs:133 writes "The 1 area
   lights …" for the single-area fixture, so pluralise only if that line is touched again; and
   scratch/t10final and t10final2 predate the Exposure fix (they still print "fixed exposure EV100 0"),
   so t10final3 is the post-fix evidence the Notes now cite.

Tree: 6f2b388 (uncommitted working tree)
Editor/Core/UnityHdrpLighting.cs 63e6755dabeab960f1a38d9b3ccfea6808159ffd
Editor/Core/UnitySceneExport.cs d5e5f99bd0fc38c12f9161ef170671c5421203b3
Editor/Core/UnityPrefabExport.cs 98c2c4961677910ea6430e6a8c03ac9b41b45c2f
Editor/Core/UnityVolume.cs 636b216e086b4a0e0163a905344f9d3209317ae3
Documentation/Unity-To-SBox-Automated/fixtures/hdrp.unitypackage 9c801699ec6d91dd1cecd0d852033550b5ee7253 (md5 78f656f7af51cc9b42e08f0290bf4bef)
Documentation/Unity-To-SBox-Automated/fixtures/hdrp/Assets/Fixture/Scenes/HDRP.unity 5d1ffdcfa2c57f59f0182bdaa5173a1d83226e03
Documentation/Unity-To-SBox-Automated/fixtures/hdrp/Assets/Fixture/Settings/HDRPVolumeProfile.asset 6dbd25f10ce3995e4f5be1871ef9fb4791a6c4ef