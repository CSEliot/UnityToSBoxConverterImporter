## Task
Read URP UniversalAdditionalLightData / UniversalAdditionalCameraData sibling bodies and decide, with engine-source evidence, which fields map to s&box light and camera components and which are report-only.

### Description
Grounding: Editor/Core/UnitySceneExport.cs:780-813 emits Sandbox.CameraComponent from generic Unity Camera fields; :829-877 emits Sandbox.DirectionalLight / PointLight / SpotLight from generic Unity Light fields. The HDRP path finds HDAdditional*Data bodies by script guid at Editor/Core/UnityPrefabExport.cs:858-885 and hands them to the converters; there is no URP equivalent. Generated components go through Component(...) at :718-728. Engine oracles: engine/Sandbox.Engine/Scene/Components/Camera/CameraComponent.cs:23-110, Light/Light.cs:140-223, game/core/templates/gameobject/Camera.prefab:10-66, game/templates/game.minimal/Assets/scenes/minimal.scene:12-20.

SyntyForest: 5 Lights (all m_Type: 1; interpreting that as directional is UNVERIFIED from in-scope files - ground it in Unity/URP source), 8 Cameras (clear flags 1; `rg -o --no-filename 'm_BackGroundColor: \{[^}]*\}' Assets --glob '*.unity' | sort | uniq -c | sort -rn` -> 5 at `{r: 0, g: 0, b: 0, a: 1}` and 3 at `{r: 0.19215687, g: 0.3019608, b: 0.4745098, a: 0}`), 0 ReflectionProbes. URP MonoBehaviours: UniversalAdditionalCameraData a79441f348de89743a2939f4d699eac1 (7), UniversalAdditionalLightData 474bcb49853aa07438625e644c072ee6 (5), resolved against Library/PackageCache/.../Runtime/*.cs.meta:2. Example serialized fields: SampleScene.unity:373-376 (m_RenderingLayers, m_CustomShadowLayers), :363-365 (m_UsePipelineSettings: 1, m_AdditionalLightsShadowResolutionTier: 2, m_LightLayerMask: 1). No engine analogue for the remaining URP-specific fields was located; that absence is UNVERIFIED, not proven.

Work:
1. Enumerate EVERY field in the two sibling bodies, apply the same filter the engine uses (component class, script guid, override state, priority/weight), and identify which component wins. A wrong selector looks exactly like missing source data; prove absence before approximating.
2. Map only fields with a grounded engine counterpart (clear flags, background colour, main-camera flag, FOV/axis, near/far, orthographic; light colour, range, spot angle, shadows, temperature). Report every unmapped URP field by name with its value; do not emit an approximation or a fabricated default.
3. Compare each generated component to an engine-authored artifact of the same __type (engine schema conformance), including __version, and fix any key the engine does not deserialise.
4. Gate on `Pipeline == Urp`; Standard and HDRP output unchanged.

### Acceptance (one explicit pass/fail check per line)
1. A generated camera component carries the source clear flags, background colour, FOV and near/far values, each asserted against the source scene lines; print both sides.
2. Each generated light component carries the source colour, intensity and range; the m_Type: 1 interpretation is grounded in primary source, not assumed.
3. Every URP-only field without an engine counterpart appears in a report line; no approximation is stacked on a value that was actually present but missed by the selector.
4. Each generated __type has the same key set and __version as an engine-authored artifact of that type; list any delta.
5. building and city--1 output byte-identical to a HEAD baseline in the same mode; per-code Unresolved breakdown unchanged.
6. Harness compile gate 0 errors; verify_output.py `checked N files, 0 problems`.

### Work Review


## Clarification Q&A
An (Director 2026-11-26 03:57, 1A/3A/5A): "Unity 6.7 URP" is the session label for the on-disk 6000.3.29f1 / URP 17.3.0 fixture; target the URP 17.x surface, not one editor build. Visual fidelity only. No new tooling or fixture package.

## Relevant History
See eliots-agentics/tasks/urp-support-research.md sections 3-4 for the MonoBehaviour census and engine component citations. HEAD bfc189b; session-start harness compile gate green.

## Notes
Standing constraints: gate every URP rule on `Pipeline == Urp`; keep Editor/Core/*.cs BOM-less and LF; route new paths through UnityImport.DiskPath; pass BOTH the URP additional body and the Unity Light/Camera class body to any converter (HDRP light data is split across two components and silently loses range/shadows if only one is passed). Editor rounds follow the 4A conditions: before any editor round, check that no Director-owned editor is already open on that project; never close an editor this work did not launch; NTFY before each large round.

## Stylistic Task Notes:
References to variables like X and Y automatically imply that these are to be exposed properties for developer tweaking.
When programming, portability and DRY are a priority.

## Post-Task Follow Ups
