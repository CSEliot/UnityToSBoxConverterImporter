# Task 24 - implementation note (worker, 2026-11-26)

## Files changed (all UNSTAGED)
- `Editor/Core/UnityUrp.cs` - additive GUID constants only: `VolumeScriptGuid` (Core `UnityEngine.Rendering.Volume`, verified in URP 17.7 Core cache `com.unity.render-pipelines.core@7aa87ac5532f/Runtime/Volume/Volume.cs.meta:2`) and `DefaultVolumeProfileGuid`.
- `Editor/Core/UnityVolume.cs` - separate `UrpPost`, `ScenePipeline` (URP camera/light/global-volume evidence, HDRP evidence wins), `ReadUrp`, `UrpVolumePost`, `EnumerateUrp`, `AddUrpPostNotes`. HDRP `Read`/`Post`/`AddPostNotes` untouched.
- `Editor/Core/UnitySceneExport.cs` - URP-only branch after the render-settings block; `PostProcessVolume` with `SceneVolume.Type = Infinite` attached to the Volume GameObject by `m_GameObject` fileID; nested effect GameObjects from `UrpEffects` (fresh unparented `List<JsonObject>`); URP census skip; developer-tweakable NEW MAPPING properties. `CameraPostEffects` and its two callers unchanged.

## Decisions recorded
- Scene pipeline: `UnityPipeline.Urp` only when a URP additional camera/light script or a global Core Volume is present AND no HDRP component is present. Every new rule is inside `if (pipeline == UnityPipeline.Urp)`.
- Winning profile: the `sharedProfile` guid on the Volume MonoBehaviour chooses the profile. URP's `DefaultVolumeProfile` is the project base stack (UniversalRenderPipelineGlobalSettings), reported once and deliberately NOT emitted as a second volume; scene profiles win. Non-scene root-`settings:` profiles remain report-only.
- Source-only effects: Bloom Strength/Threshold/Tint, Vignette Intensity/Color/Center, SSAO m_Intensity, ColorAdjustments contrast/saturation, Tonemapping mode. A field with `m_OverrideState: 0` is not emitted; the engine default applies and the report enumerates it.
- NEW MAPPINGS, all developer-tweakable: `UrpNeutralExposureCompensationEv = 1.58` (mode 1 Neutral -> HableFilmic, `Documentation/unity_6_7_to_sbox.md:38`; mode 1 is NOT AgX), `UrpNoneTonemapMode = "Linear"`, `UrpTonemapAutoExposureEnabled = false`, `UrpSsaoIntensityScale = 1.0`, `UrpColorAdjustmentDivisor = 100.0` (URP -100..100 neutral 0 -> engine 0..2 neutral 1).
- No SSR conversion: `Sandbox.ScreenSpaceReflections` is obsolete/no-op; the report says so. Baked data / unmapped overrides (LiftGammaGain, WhiteBalance, ShadowsMidtonesHighlights, MotionBlur, bloom scatter/dirt/quality, SSAO radius/quality, etc.) are report lines only.
- Camera template effects remain on cameras (unchanged conversion path); the URP volume itself carries only SOURCE values. Report prints both source and template numbers.
- Line endings: `UnityVolume.cs` and `UnitySceneExport.cs` were already CRLF on arrival (534/534 and 969/969 CR/LF); edits preserved the existing EOL style and introduced no BOM. `UnityUrp.cs` remains LF. No EOL mass-conversion, to keep the HDRP path byte-identical.

## Compile gate
`dotnet build .../harness/harness.csproj -c Release -p:IUP_ROOT=... --no-incremental` -> **Build succeeded, 0 Warning(s), 0 Error(s)**.

## Dated fix note (2026-11-26, after first verification round)
Parent-confirmed defect: `ScenePipeline` treated the shared Core Volume guid (`172515602e62fb746b5d573b38a5fe58`, also in `UnityHdrp.cs:117`) as HDRP evidence, so SyntyForest returned `Unknown` and the URP branch never ran (first run had camera-template effects only). Fixed by excluding exactly that guid from the HDRP-evidence early return; any other HDRP guid still returns `Unknown`. Secondary fix: when `pipeline == Urp`, `UnityVolume.Read` receives a block list with the shared-guid Volume MonoBehaviours filtered out, so the HDRP path cannot emit a second template-effect PostProcessVolume or consume URP profiles. HDRP/Unknown scenes keep the unfiltered call. Rerun results in `03-verification.md`.

## Dated fix note (2026-11-27, Work Review round)
Accepted all six Low findings: shared Volume guid made NEUTRAL (only URP camera/light siblings decide the pipeline); Vignette Smoothness/Roundness mapped from source; conditional `Tonemapping not overridden` report clause; three developer-tweakable clamp properties (`UrpBloomStrengthMax`, `UrpBloomThresholdMax`, `UrpNeutralExposureClampEv`) documented as NEW MAPPINGS; and `active: 1` gating for mapped URP overrides with an `active: 0` report line. Evidence and gate numbers in `03-verification.md` "Fix round".

Known limitation (deliberately not fixed, no fixture case): a URP scene with ONLY a local Volume and no URP additional camera/light MonoBehaviour classifies `Unknown`, so no local-volume report line is emitted. Adding a report-only path outside the Urp gate was out of scope for this fix round.
