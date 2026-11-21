## Status: STARTED? NO! CLARIFIED? NO! HISTORY? NO! REVIEWED? NO! DONE? NO! WORKER: NONE OTHER: HDRP backlog, depends on 06 and 09

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
	- 6 cameras are all IsMainCamera:false, because MainCamera() scans only non-stripped scene blocks
	  (UnityPrefabExport.cs:879-894) while the main camera's component lives inside a prefab instance.
	  HDAdditionalCameraData background/clear mode, antialiasing and volume mask are not read, and
	  exposure lives in the Volume (task 09's reader), not the camera.

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
	5. Cameras: resolve the main camera through prefab instances (or choose the tagged, enabled, active
	   camera deterministically) so MergeV2 has exactly one IsMainCamera true. Read
	   HDAdditionalCameraData's background/clear mode, antialiasing and volume mask; map what
	   CameraComponent supports and report the rest. Use the task-09 Volume reader for Exposure and
	   map to CameraComponent.AutoExposure if supported, otherwise report the EV range.
	6. Extend the HDRP fixture with one directional lux light, one lumen spot, one area light, and two
	   cameras (one main inside a prefab), and assert the unit conversion and main-camera choice.

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