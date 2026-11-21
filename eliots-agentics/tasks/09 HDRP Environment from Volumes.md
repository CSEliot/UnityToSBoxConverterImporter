## Status: STARTED? YES! CLARIFIED? YES! HISTORY? YES! REVIEWED? YES! DONE? YES! WORKER: sa-0-74868f4e OTHER: reviewed 21:03 no further changes; HDRP environment committed

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
	- The values follow the source profiles, not one shared figure: MergeV2 resolves to PhysicallyBased
	  sky + ExponentialFog (grey ~0.5, 200 m); Test_Range resolves to HDRI sky (texture named, default
	  sky unless the key is verified) + the newer Fog override (meanFreePath 400 -> ~15,748 units).
	  The report names the procedural/HDRI approximation and the static sky/unsupported overrides.
	- The Volume components are no longer reported as generic "MonoBehaviour (script)" losses; local
	  volumes, if any, are reported.
	- building (24 files without --scenes) and city--1 (1,166 files / 3 warnings) stay byte-identical;
	  their scenes have no HDRP Volumes, so no SkyBox2D/Fog is added there.
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

## Evidence

	2026-11-21, worker-09. Code: new `Editor/Core/UnityVolume.cs` (VolumeProfile YAML reader: override list,
	script-guid → override name table for VisualEnvironment/Fog/ExponentialFog/PhysicallyBased/HDRDI/Gradient,
	m_OverrideState-gated m_Value reads, global volume merge by priority then scene order, local/unresolved
	volume reporting, static-sky count, unsupported-override line). `UnitySceneExport.cs` step 6 now reads
	Volumes first (one `Sandbox.SkyBox2D`, `Sandbox.AmbientLight` only for a concrete gradient ambient, one
	`Sandbox.Fog`), falls back to RenderSettings, and no longer writes `Sandbox.GradientFog`; step 7 skips
	Volume/StaticLightingSky so they are not counted as generic MonoBehaviour losses. No lights, cameras,
	probes or tooling touched.

	Fixture (`fixtures/hdrp`, repacked tar md5 955c40df62e7c27fecdcf7faf2745f27, 25 assets):
	`HDRPVolumeProfile.asset` now carries VisualEnvironment (skyType 3 Gradient, fogType 2), GradientSky
	(middle 0.55/0.68/0.85), the legacy ExponentialFog (grey 0.5/0.55/0.6, fogDistance 200, attenuation
	0.005) and a newer Fog override that fogType 2 must not select. New control scene `HDRP_NoSky.unity`
	(RenderSettings only, no Volume). Result: `hdrp.scene` has exactly one SkyBox2D (Tint
	0.55,0.68,0.85,1), one AmbientLight (same colour), one Fog (Color 0.5,0.55,0.6,1, AttenuationDistance
	7874 = 200 m, Mode Exponential, HeightGradientLength 7874 = 200 m) and no GradientFog; `hdrp_nosky.scene`
	has none of the three. Harness: `assets=24 converted=21 files=48`, 0 unresolved; verify_output 0 problems.

	Harness build: 0 warnings / 0 errors (after removing an unused local). `building` 24 files / 0 warnings /
	0 unresolved and `city--1` 1,166 files / 3 warnings / 3 unresolved, each `diff -r --brief` against the
	HEAD bases (`gates/base-building`, `gates/base-city`) empty: byte-identical.

	old_school `--scenes` gate (final bytes, `Testing/old_school_project.unitypackage`): two full runs,
	`assets=1277 converted=936 files=2115 changed=2115`, 2,115 files each, `diff -r --brief` between the
	two output trees empty, `verify_output.py` on the generated scenes 0 problems. Scene counts:
	`mergev2.scene` 1 SkyBox2D (Tint 1,1,1,1 — PhysicallyBased tints are white), 1 Fog (Color
	0.5,0.5,0.5,1, AttenuationDistance 7874 = 200 m, HeightGradientLength 7874, Mode Exponential),
	0 AmbientLight, 0 GradientFog; `test_range.scene` 1 SkyBox2D (default sky, HDRI texture named), 1 Fog
	(Color 0.5,0.5,0.5,1, AttenuationDistance 15748 = 400 m meanFreePath, HeightGradientLength 1968.5 =
	50 m maximumHeight), 0 AmbientLight, 0 GradientFog. Report lines name the approximation per scene
	("PhysicallyBased sky is approximated…" / "HDRI sky texture …spruit_sunrise_8k.exr is not converted…"),
	the 2 and 1 StaticLightingSky components, the 39 unconverted overrides and the ambient-left-to-sky
	decision; MergeV2 logs that its two equal-priority Volumes both override VisualEnvironment and that the
	last in scene order (Volume Settings) won, which is what makes it PhysicallyBased + ExponentialFog.

	Correction to the task premise, for the record: Test_Range's Volume references ca49d0b4 (Scene
	Settings Profile MergeV2), not a VolumeSettings profile, and both school profiles carry an HDRI sky
	plus the newer Fog override with a stale fogType; the ExponentialFog 200 m data exists only in
	VolumeSettings.asset and reaches MergeV2 through the equal-priority merge. Acceptance now matches this
	(re-baselined above).

	Census: MergeV2's former "180 MonoBehaviour (script) component(s)" line is now 6 and lists only the
	camera scripts; Volume and StaticLightingSky no longer appear as unconverted scene components (no
	"Volume component(s)" line in either scene).

	Not covered by evidence: editor-side look of the generated sky/fog (headless harness cannot render);
	nothing was committed and no reviewer has seen this tree yet.

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

2026-11-21 21:03 - Verdict: no further changes

Verified against the uncommitted tree at ab2c26d (Director's archive moves excluded: `tasks/01`-`05`
deletions and `.humans-only/archive/` additions are not reviewed). Only `Editor/Core/UnityVolume.cs`
(new) and `Editor/Core/UnitySceneExport.cs` are code changes; the fixture edits are
`HDRPVolumeProfile.asset` plus new `HDRP_NoSky.unity`/.meta.

1. [Low] `Editor/Core/UnityVolume.cs:271-274` comment contradicts the implemented precedence at :279:
	the comment says the Fog override renders "whether the stale field says Exponential or Volumetric",
	but `useLegacy = fogType == FogExponential && exponential != null` (:279) always prefers the
	deprecated ExponentialFog when both records exist. Fix: state the code's rule (fogType 2 selects the
	legacy ExponentialFog record when the merge carries one, the newer Fog override otherwise) so the
	comment and the branch agree.
2. [Low] `Editor/Core/UnityVolume.cs:283-285`: the tie note "The profile also carries the newer Fog
	override" is imprecise for MergeV2, where the newer Fog comes from the *other* tied Volume (Scene
	Settings Profile) and the ExponentialFog from Volume Settings (`os2/log.txt` line for MergeV2). Fix:
	say "another volume in the merge also carries the newer Fog override".
3. [Low] Evidence wording: the two old_school runs are byte-identical only under `os1/Assets` and
	`os2/Assets` (2,115 files each, `diff -r --brief` empty); `report.json` differs by
	`ElapsedMilliseconds` and the `Destination` path. Fix: scope the "two-run diff empty" claim to the
	generated output tree.

Evidence read on disk (none of it code-critic-only claims):

- Build: `dotnet build` of `Documentation/Unity-To-SBox-Automated/harness` -> 0 warnings / 0 errors.
- Fixture (`hdrp.unitypackage`, md5 ffeac6663802d784a196f6f55f73f235, 25 archive entries,
	`assets=24 converted=21 files=48`): `hdrp.scene` has exactly 1 `Sandbox.SkyBox2D` (SkyMaterial
	`materials/skybox/skybox_day_01.vmat`, Tint 0.55,0.68,0.85,1), 1 `Sandbox.AmbientLight`
	(0.55,0.68,0.85,1), 1 `Sandbox.Fog` (Color 0.5,0.55,0.6,1, AttenuationDistance 7874 = 200 m,
	HeightGradientLength 7874, Mode Exponential) and 0 GradientFog; the `HDRP_NoSky.unity` control emits
	none of the three. The profile's newer Fog override (meanFreePath 1111) is correctly skipped because
	VisualEnvironment.fogType 2 selects the legacy ExponentialFog.
- No `GradientFog` remains anywhere: `grep -rn GradientFog --include=*.cs` = 0 hits, and the scene JSON
	of both real scenes and both fixture scenes counts 0.
- `building` 24 files / 0 warnings / 0 unresolved and `city--1` 1,166 files / 3 warnings / 3 unresolved,
	each byte-identical to the HEAD bases after a fresh run of this tree (`critic09/` vs
	`gates/base-building`, `gates/base-city`: `diff -r --brief` empty, same file counts).
- old_school (`--scenes`): 2,115 generated files in each of two runs, Assets trees byte-identical;
	`mergev2.scene` 1 SkyBox2D (Tint 1,1,1,1) + 1 Fog (7874 = 200 m, HeightGradientLength 7874), 0
	AmbientLight, 0 GradientFog; `test_range.scene` 1 SkyBox2D (default sky) + 1 Fog (15748 = 400 m,
	HeightGradientLength 1968.5 = 50 m), 0 AmbientLight, 0 GradientFog; `verify_output.py` on both = 0
	problems. MergeV2 logs the two equal-priority Volumes, the last-in-scene-order VisualEnvironment win,
	2 StaticLightingSky, 39 unconverted overrides and the ambient-left-to-sky line; Test_Range logs the
	HDRI texture `spruit_sunrise_8k.exr` by path.
- Census: `MergeV2.unity` now reports 6 MonoBehaviour (script) components (the six cameras) and no
	"Volume component(s)" line; `grep -c "Volume component(s)"` over the whole old_school log = 0.
- Re-baseline is justified by the archive, not convenience: extracting `Testing/old_school_project`
	shows MergeV2 has `Scene Settings` (Volume guid ca49d0b4, priority 0, weight 1, global, block 47085716)
	then `Volume Settings` (guid dab0766d, priority 0, weight 1, global, block 2011204168) in scene
	document order, and Test_Range's single Volume also references ca49d0b4. `VolumeSettings.asset` holds
	VisualEnvironment skyType 4 / fogType 2 and ExponentialFog (color 0.5,0.5,0.5, fogDistance 200,
	fogHeightAttenuation 0.005); `Scene Settings Profile MergeV2.asset` holds VisualEnvironment skyType 1 /
	fogType 3, HDRISky (texture guid 74a2a69b) and Fog meanFreePath 400 / maximumHeight 50. The old
	acceptance figure (200 m grey for both scenes) is impossible for Test_Range, and the mode change
	(26/1,192 `--scenes` -> 24/1,166 no-scenes) is the documented same-mode gate.
- Reader scope matches the brief: `m_OverrideState: 0` returns null from `UnityVolume.Value` (:100-113),
	profiles resolve through `sharedProfile|m_Profile` against `context.ByGuid` (the archive-wide index,
	`UnitySceneExport.cs:62,255`), only global volumes at the highest priority are merged in scene order,
	and local/unresolved volumes, a missing profile, StaticLightingSky and unsupported overrides each get
	a report line (`UnityVolume.cs:181-217,300-302`). Every `Sandbox.Fog` key written
	(`UnitySceneExport.cs:283-297`) exists on `Sandbox.Engine/Scene/Components/PostProcessing/Fog.cs`.

Tree:
- Editor/Core/UnityVolume.cs c9b6316a2837d8bd454390e4d762588104ece135
- Editor/Core/UnitySceneExport.cs 77f2dca08521feef4adb3dc82277473588a0e7e8
- Documentation/Unity-To-SBox-Automated/fixtures/hdrp/Assets/Fixture/Settings/HDRPVolumeProfile.asset f6aa8dff2bfe4a236f856c142dc36466150ff699
- Documentation/Unity-To-SBox-Automated/fixtures/hdrp.unitypackage d8a79567967e2037468a3fc63ca3dcddb50e1b95