## Status: STARTED? NO! CLARIFIED? NO! HISTORY? NO! REVIEWED? NO! DONE? NO! WORKER: NONE OTHER: HDRP backlog, depends on 06

## Task

	Convert HDRP environment settings from Volume profiles: sky, ambient light and fog.

### Description

	HDRP keeps scene-wide environment settings in Volume overrides, not RenderSettings. In
	old_school's MergeV2 and Test_Range:

	- RenderSettings have m_SkyboxMaterial {fileID: 0}, m_AmbientMode 0 and m_Fog 0, so the current
	  RenderSettings block in UnitySceneExport.cs:252-300 emits no SkyBox2D, no AmbientLight and no
	  fog. The generated scenes are lit geometry with no environment.
	- MergeV2 has two global Volumes: "Scene Settings" (profile 371c407c..., then ca49d0b4...) and
	  "Volume Settings" (profile dab0766d...). Test_Range has one "Volume Settings".
	- VolumeSettings.asset selects skyType 4 (PhysicallyBased sky) and fogType 2 (Exponential), with a
	  grey fog colour around 0.5, density 1, fog distance 200, mip fog far 1000 and attenuation 0.005.
	- The "Scene Settings Profile MergeV2.asset" carries Exposure and related overrides; exposure is
	  task 10's concern, not this task's.
	- There are 2 StaticLightingSky components in MergeV2; they select a Lighting Settings profile for
	  baking. They are reported but not converted.

	The current exporter's fog path writes Sandbox.GradientFog, which is obsolete in the engine
	("Use Fog instead."); no HDRP data reaches it anyway.

	Research: hdrp-research.md; cache/scratch/hdrp/research-scenes.md.

### Required work

	1. Add a VolumeProfile reader in Editor/Core (new UnityVolume.cs or equivalent) that parses
	   text-serialized VolumeProfile .asset files from the package: the override list, component type
	   names and their serialized fields. Resolve the Volume MonoBehaviour's m_Profile guid and the
	   scene's global/local, priority and weight fields.
	2. Read VisualEnvironment (skyType, skyAmbientMode), PhysicallyBasedSky, HDRI Sky, Gradient Sky,
	   Fog and ExponentialFog overrides. Resolve the Volume a scene uses; apply only global volumes
	   (priority highest, weight 1); report local volumes instead of blending them.
	3. Emit one Sandbox.SkyBox2D per scene when HDRP supplies a sky:
	   - PhysicallyBased or Gradient sky: use the current default sky material with a tint/brightness
	     approximation and one Information line saying the procedural sky is approximated.
	   - HDRI Sky with a texture: only generate a texture-backed SkyBox2D if the sky texture parameter
	     key is verified in the editor; otherwise use the default sky and name the texture in the
	     report. Do not guess a vmat key in this task.
	4. Emit Sandbox.AmbientLight from the Volume's ambient/sky data where a concrete colour exists;
	   otherwise report that ambient is left to the sky.
	5. Replace the obsolete Sandbox.GradientFog output with Sandbox.Fog, mapping exponential/linear fog
	   colour, density/distance and falloff from the Volume override. Do not emit fog when the source
	   has none.
	6. Mark the consumed Volume and StaticLightingSky components as converted in the step-7 census so
	   they are not also reported as unconverted MonoBehaviours. Keep one summary line for any
	   unsupported overrides (for example volumetric clouds, SSR, path tracing).
	7. Extend the HDRP fixture with a Volume containing VisualEnvironment and ExponentialFog and assert
	   the generated SkyBox2D/Fog/AmbientLight; keep a second fixture scene with no sky as the control.

### Acceptance

	- MergeV2 and Test_Range each gain exactly one Sandbox.SkyBox2D and one Sandbox.Fog; no
	  Sandbox.GradientFog remains in either output.
	- The generated fog colour/distance reflects VolumeSettings.asset (grey ~0.5, distance 200) within
	  the documented approximation; the report names PhysicallyBased sky as approximated and lists the
	  static sky/unsupported overrides.
	- The Volume components are no longer reported as generic "MonoBehaviour (script)" losses; local
	  volumes, if any, are reported.
	- building (26 files) and city--1 (1,192 files) stay byte-identical; their scenes have no HDRP
	  Volumes, so no SkyBox2D/Fog is added there.
	- Harness builds 0 warnings / 0 errors; fixture and old_school two-run diffs are empty;
	  verify_output.py 0 problems.

## Notes

	Implementation home: UnitySceneExport.cs:251-300 (environment block), the step-7 census at :302-316,
	and a new Editor/Core/UnityVolume.cs for profile parsing. Reuse the YAML field helpers; do not bring
	in a full YAML library.

	VolumeProfile asset YAML stores overrides as component references and the component objects follow
	in the same file. The reader must handle a profile whose overrides are all defaults and a Volume
	with a null profile.

	The source sky is PhysicallyBased, which has no texture; the default-sky approximation is the
	honest ceiling. Do not claim HDRI parity from this project.

## Clarification Q&A

	Q: Should the physically based sky be baked to a cubemap at import time?
	A: No. The importer is engine-free and cannot render. Use the default sky approximation and report.

	Q: Should local Volumes be blended?
	A: No. Report them; applying one global profile is the supported approximation.

## Relevant History

	Task 05 made scene losses visible. Task 09 is the first HDRP scene conversion task; tasks 10 and 11
	consume the same VolumeProfile reader for exposure, post-processing and probes.

## Stylistic Task Notes:

	References to variables like N and M automatically imply that these are to be exposed properties
	for developer tweaking, if they become code knobs.

## Skills To Know: for you and/or subagents:
	sbox-offline-compile-check
	unityimporterplus
	unity-to-sbox-conversion
	sbox-scene-prefab-inspection

## Post-Task Follow Ups