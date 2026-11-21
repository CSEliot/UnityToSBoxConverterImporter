## Status: STARTED? NO! CLARIFIED? NO! HISTORY? NO! REVIEWED? NO! DONE? NO! WORKER: NONE OTHER: HDRP backlog, depends on 06, 09 and 10

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

## Post-Task Follow Ups