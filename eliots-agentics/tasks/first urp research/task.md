## Task
Do comprehensive research for this unity -> sbox importer for support for Unity 6.7 URP games. (Reminder, this is NOT currently a code importer project. Just the visuals.)

### Description


### Work Review

2026-11-26 04:21 - Verdict: changes required

1. [High] `urp-support-research.md:38,71,80`; `21 URP Detection and Fixture/task.md:18`; `26 URP Report-Only Features and Shader Graph Census/task.md:2,7,17`: the 187 Shader Graph count does not reproduce. The shader-guid census over 217 `.mat` is 27 URP/Lit + 1 Particles/Unlit + 186 across the 14 resolved graph guids + 2 built-in-resource + 1 unresolved = 217; the record's own per-graph list (58+40+26+24+13+8+5+3+3+2+1+1+1+1) sums to 186. Fix: change every "187" to 186 and rewrite task 21 acceptance #2 and task 26 acceptance #1 to assert 186 and a per-graph sum of 186.
2. [High] `urp-support-research.md:59`; `24 URP Environment from Volumes/task.md:9`: "27 effect classes under PostProcessing/Effects/" does not reproduce. `find engine/Sandbox.Engine/Scene/Components/PostProcessing/Effects -type f -name '*.cs' | wc -l` -> 18, and class declarations in those files -> 18. Fix: state the reproducible count (18) and the exact command; align task 24's "12 matches" with the named class set actually present.
3. [Medium] `urp-support-research.md:29`: "38 Warnings.Add sites, 79 Information.Add sites" do not reproduce. Repo-wide, hidden files included, `rg -o --no-filename 'Warnings\.Add' --hidden --no-ignore -g '*.cs' | wc -l` -> 34 and the same for `Information\.Add` -> 51. Fix: replace both numbers with the reproducible counts and name the command.
4. [Medium] `urp-support-research.md:49`; `24 URP Environment from Volumes/task.md:7`: the profile census is framed around the two unreferenced `settings:` profiles and omits the two profiles the Global Volumes actually reference. `SampleScene.unity:410` points at guid `10fc4df2da32a41aaa32d77bc913491c` (`Assets/Settings/SampleSceneProfile.asset`), whose `components:` list contains Bloom intensity 0.25 (`m_OverrideState: 1`) and Vignette intensity 0.2; `Demo.unity:369` points at guid `7679a2a2468dc7742a7ece2dea61ab3b` (`.../Demo/Global Volume Profile.asset`), whose `components:` list contains Bloom intensity 2.19, LiftGammaGain and WhiteBalance. Fix: add those two guids and override values, and stop using a root `settings:` list as the discriminator.
5. [Medium] `urp-support-research.md:69,101`; `23 URP Lit PBR and Emission Fidelity/task.md:5,32`: `F_SELF_ILLUM` acceptance and `g_flRefractionStrength` consumption are marked UNVERIFIED although both are directly checkable. 28 engine-authored `.vmat` set `F_SELF_ILLUM` (e.g. `game/core/materials/editor/air_node.vmat:4`); `game/core/shaders/glass.shader:121,183,217` reads `g_flRefractionStrength`. Fix: replace the UNVERIFIED labels with these artifact lines, or narrow the unverified claim to what is actually absent.
6. [Medium] `25 URP Lights and Cameras/task.md:7`: "background colours cited in the research record" is false — `urp-support-research.md:45` records scene/component counts but no camera background colours. Observed values are 5 cameras at `{r: 0, g: 0, b: 0, a: 1}` and 3 at `{r: 0.19215687, g: 0.3019608, b: 0.4745098, a: 0}`. Fix: put those values in the task's grounding section or remove the cross-reference.
7. [Medium] `23 URP Lit PBR and Emission Fidelity/task.md:33`; `24 URP Environment from Volumes/task.md:34`; `25 URP Lights and Cameras/task.md:33`; `26 URP Report-Only Features and Shader Graph Census/task.md:34`: these four briefs say only "Editor rounds follow the 4A conditions". The parent task at `first urp research/task.md:66` requires the three conditions copied into every child brief that touches an editor (21 and 22 do this). Fix: copy the three conditions verbatim into each of the four Notes lines.
8. [Low] `26 URP Report-Only Features and Shader Graph Census/task.md:17,19`: the SSR clause of acceptance #2 cannot fail on this fixture — `rg -l -i 'screenspacereflection' Assets --glob '*.asset' --glob '*.unity' | wc -l` -> 0 in SyntyForest. Fix: name a report-only field that is actually present in the source assets, or restrict the SSR assertion to code-path inspection on a supplied override.

Tree: d3aa3a0e714cd94dc50974aa5ff34ae49c36d262 eliots-agentics/tasks/urp-support-research.md (`git hash-object --path="eliots-agentics/tasks/urp-support-research.md" "C:/cseliot/1-Development/1-SBox/2-Projects/unityimporterplus_branch_2/eliots-agentics/tasks/urp-support-research.md"`)
Tree: 9efb12a788291857d4ae6198c7c27183040bfa8c eliots-agentics/tasks/first urp research/status.md (`git hash-object --path="eliots-agentics/tasks/first urp research/status.md" "C:/cseliot/1-Development/1-SBox/2-Projects/unityimporterplus_branch_2/eliots-agentics/tasks/first urp research/status.md"`)
Tree: 9826cab06d1df43447bd0bf5874abc90a64e172f eliots-agentics/tasks/first urp research/task.md (`git hash-object --path="eliots-agentics/tasks/first urp research/task.md" "C:/cseliot/1-Development/1-SBox/2-Projects/unityimporterplus_branch_2/eliots-agentics/tasks/first urp research/task.md"`)
Tree: a2268dc6cd6ef172d93402fb5e761c22c220d5fb eliots-agentics/tasks/21 URP Detection and Fixture/status.md (`git hash-object --path="eliots-agentics/tasks/21 URP Detection and Fixture/status.md" "C:/cseliot/1-Development/1-SBox/2-Projects/unityimporterplus_branch_2/eliots-agentics/tasks/21 URP Detection and Fixture/status.md"`)
Tree: e1fede4897cde892568b2f2168a15f63498b9b4f eliots-agentics/tasks/21 URP Detection and Fixture/task.md (`git hash-object --path="eliots-agentics/tasks/21 URP Detection and Fixture/task.md" "C:/cseliot/1-Development/1-SBox/2-Projects/unityimporterplus_branch_2/eliots-agentics/tasks/21 URP Detection and Fixture/task.md"`)
Tree: a2268dc6cd6ef172d93402fb5e761c22c220d5fb eliots-agentics/tasks/22 URP Lit Surface Semantics/status.md (`git hash-object --path="eliots-agentics/tasks/22 URP Lit Surface Semantics/status.md" "C:/cseliot/1-Development/1-SBox/2-Projects/unityimporterplus_branch_2/eliots-agentics/tasks/22 URP Lit Surface Semantics/status.md"`)
Tree: 3dfb82ed31045ec50e944a17279c761624926ff2 eliots-agentics/tasks/22 URP Lit Surface Semantics/task.md (`git hash-object --path="eliots-agentics/tasks/22 URP Lit Surface Semantics/task.md" "C:/cseliot/1-Development/1-SBox/2-Projects/unityimporterplus_branch_2/eliots-agentics/tasks/22 URP Lit Surface Semantics/task.md"`)
Tree: a2268dc6cd6ef172d93402fb5e761c22c220d5fb eliots-agentics/tasks/23 URP Lit PBR and Emission Fidelity/status.md (`git hash-object --path="eliots-agentics/tasks/23 URP Lit PBR and Emission Fidelity/status.md" "C:/cseliot/1-Development/1-SBox/2-Projects/unityimporterplus_branch_2/eliots-agentics/tasks/23 URP Lit PBR and Emission Fidelity/status.md"`)
Tree: 33fb5c18394036cce71acca362d6a590d4111da3 eliots-agentics/tasks/23 URP Lit PBR and Emission Fidelity/task.md (`git hash-object --path="eliots-agentics/tasks/23 URP Lit PBR and Emission Fidelity/task.md" "C:/cseliot/1-Development/1-SBox/2-Projects/unityimporterplus_branch_2/eliots-agentics/tasks/23 URP Lit PBR and Emission Fidelity/task.md"`)
Tree: a2268dc6cd6ef172d93402fb5e761c22c220d5fb eliots-agentics/tasks/24 URP Environment from Volumes/status.md (`git hash-object --path="eliots-agentics/tasks/24 URP Environment from Volumes/status.md" "C:/cseliot/1-Development/1-SBox/2-Projects/unityimporterplus_branch_2/eliots-agentics/tasks/24 URP Environment from Volumes/status.md"`)
Tree: 2dc1b27872ab27d3edf972e93c2b8da22561767a eliots-agentics/tasks/24 URP Environment from Volumes/task.md (`git hash-object --path="eliots-agentics/tasks/24 URP Environment from Volumes/task.md" "C:/cseliot/1-Development/1-SBox/2-Projects/unityimporterplus_branch_2/eliots-agentics/tasks/24 URP Environment from Volumes/task.md"`)
Tree: a2268dc6cd6ef172d93402fb5e761c22c220d5fb eliots-agentics/tasks/25 URP Lights and Cameras/status.md (`git hash-object --path="eliots-agentics/tasks/25 URP Lights and Cameras/status.md" "C:/cseliot/1-Development/1-SBox/2-Projects/unityimporterplus_branch_2/eliots-agentics/tasks/25 URP Lights and Cameras/status.md"`)
Tree: 5632be98ed565385f004fc6536af8a7a04e1f097 eliots-agentics/tasks/25 URP Lights and Cameras/task.md (`git hash-object --path="eliots-agentics/tasks/25 URP Lights and Cameras/task.md" "C:/cseliot/1-Development/1-SBox/2-Projects/unityimporterplus_branch_2/eliots-agentics/tasks/25 URP Lights and Cameras/task.md"`)
Tree: a2268dc6cd6ef172d93402fb5e761c22c220d5fb eliots-agentics/tasks/26 URP Report-Only Features and Shader Graph Census/status.md (`git hash-object --path="eliots-agentics/tasks/26 URP Report-Only Features and Shader Graph Census/status.md" "C:/cseliot/1-Development/1-SBox/2-Projects/unityimporterplus_branch_2/eliots-agentics/tasks/26 URP Report-Only Features and Shader Graph Census/status.md"`)
Tree: 1bca748c2917d6b65ef3157c0c867143a31def51 eliots-agentics/tasks/26 URP Report-Only Features and Shader Graph Census/task.md (`git hash-object --path="eliots-agentics/tasks/26 URP Report-Only Features and Shader Graph Census/task.md" "C:/cseliot/1-Development/1-SBox/2-Projects/unityimporterplus_branch_2/eliots-agentics/tasks/26 URP Report-Only Features and Shader Graph Census/task.md"`)

The two `first urp research/` hashes are pre-entry snapshots: those state files are changed by this review, so their final blob hashes are self-referential.

#### Re-check 2026-11-26 04:39

Verdict: no further changes

1. [High - closed] Finding 1 (186 vs 187): `urp-support-research.md:38,71`, `21/task.md:18`, `26/task.md:2,7,17` now say 186. The 14 per-graph counts sum to 186; the `-6465566751694194690` Shader-Graph fileID occurs 187 times, the extra one carrying unresolved guid `1d8f5c5fc8a9f6d428ac325779c841cd`; 27 + 1 + 186 + 2 + 1 = 217. Fix: none required.
2. [High - closed] Finding 2 (effect classes): `urp-support-research.md:59` pins both commands and numbers; `find ... Effects -type f -name '*.cs' | wc -l` -> 18 and the exact `grep -rlE ... *.cs | wc -l` -> 12. `24/task.md:9` already said 12 and now names that command. Fix: none required.
3. [Medium - closed] Finding 3 (report-site counts): `urp-support-research.md:29` pins `Warnings\.Add\(|warnings\.Add\(` -> 38 and `Information\.Add\(|information\.Add\(` -> 79 over `Editor/Core/*.cs`; both reproduce. The earlier 34/51 was a different measurement and is not re-raised. Fix: none required.
4. [Medium - closed] Finding 4 (profiles): `urp-support-research.md:49` and `24/task.md:7` name sharedProfile guids `10fc4df2da32a41aaa32d77bc913491c` (`SampleScene.unity:410`) and `7679a2a2468dc7742a7ece2dea61ab3b` (`Demo.unity:371369`), their `components:` lists (Tonemapping/Bloom/MotionBlur/Vignette and Bloom/ColorAdjustments/LiftGammaGain/ShadowsMidtonesHighlights/WhiteBalance), and state the guid, not a root `settings:` list, is the discriminator. Cited intensity `m_OverrideState: 1` lines reproduce. Fix: none required.
5. [Medium - closed] Finding 5 (false UNVERIFIED): `urp-support-research.md:69,101` and `23/task.md:5` now say VERIFIED; `rg -l 'F_SELF_ILLUM' game --glob '*.vmat' | wc -l` -> 27 with `game/core/materials/editor/air_node.vmat:4`, and `game/core/shaders/glass.shader:121,183,217` reads `g_flRefractionStrength`. Fix: none required.
6. [Medium - closed] Finding 6 (camera background colours): `25/task.md:7` carries the command and values inline; it reproduces 5 x `{r: 0, g: 0, b: 0, a: 1}` and 3 x `{r: 0.19215687, g: 0.3019608, b: 0.4745098, a: 0}`. Fix: none required.
7. [Medium - closed] Finding 7 (4A conditions): tasks 23:33, 24:34, 25:33, 26:34 now carry all three conditions verbatim. Fix: none required.
8. [Low - closed] Finding 8 (SSR unfalsifiable): `26/task.md:18` names present fields (5 `m_LightLayerMask`, 6 `m_RenderingLayers`, 5 `m_UsePipelineSettings`, 5 `m_CustomShadowLayers` - all reproduce) and restricts SSR to code-path inspection with its 0-reference command. Fix: none required.
9. [Medium - closed] Extra wording item: all six backlog Notes lines (21:35, 22:35, 23:33, 24:34, 25:33, 26:34) now use "check that no Director-owned editor is already open on that project", plus the never-close and NTFY conditions. Fix: none required.
10. [Low - no finding] `187` remains only in the three intentional fileID parentheticals, `PostProcessing/Fog.cs:30-187`, and finding 1 in the first review; the seven prose deliverables have no scratch paths or process wording; all seven status.md line-1 headers have the seven fields in order with DONE? NO! and are either unstarted or awaiting re-check; `git diff --cached --name-only` is empty. Fix: none required.

Tree: 362aeee8b6c5698250b9f1f36fc47203b3e2ab26 eliots-agentics/tasks/urp-support-research.md (`git hash-object --path="eliots-agentics/tasks/urp-support-research.md" "C:/cseliot/1-Development/1-SBox/2-Projects/unityimporterplus_branch_2/eliots-agentics/tasks/urp-support-research.md"`)
Tree: a944ddaae17f284af08e12fc1104cc55a1f0c58c eliots-agentics/tasks/first urp research/status.md (`git hash-object --path="eliots-agentics/tasks/first urp research/status.md" "C:/cseliot/1-Development/1-SBox/2-Projects/unityimporterplus_branch_2/eliots-agentics/tasks/first urp research/status.md"`)
Tree: 37f58a9a1c2def74acae2ab7a2a4d38436c4a907 eliots-agentics/tasks/first urp research/task.md (`git hash-object --path="eliots-agentics/tasks/first urp research/task.md" "C:/cseliot/1-Development/1-SBox/2-Projects/unityimporterplus_branch_2/eliots-agentics/tasks/first urp research/task.md"`)
Tree: a2268dc6cd6ef172d93402fb5e761c22c220d5fb eliots-agentics/tasks/21 URP Detection and Fixture/status.md (`git hash-object --path="eliots-agentics/tasks/21 URP Detection and Fixture/status.md" "C:/cseliot/1-Development/1-SBox/2-Projects/unityimporterplus_branch_2/eliots-agentics/tasks/21 URP Detection and Fixture/status.md"`)
Tree: 50d4af824b26a5b162f4f0fd19702834c40169f6 eliots-agentics/tasks/21 URP Detection and Fixture/task.md (`git hash-object --path="eliots-agentics/tasks/21 URP Detection and Fixture/task.md" "C:/cseliot/1-Development/1-SBox/2-Projects/unityimporterplus_branch_2/eliots-agentics/tasks/21 URP Detection and Fixture/task.md"`)
Tree: a2268dc6cd6ef172d93402fb5e761c22c220d5fb eliots-agentics/tasks/22 URP Lit Surface Semantics/status.md (`git hash-object --path="eliots-agentics/tasks/22 URP Lit Surface Semantics/status.md" "C:/cseliot/1-Development/1-SBox/2-Projects/unityimporterplus_branch_2/eliots-agentics/tasks/22 URP Lit Surface Semantics/status.md"`)
Tree: 2eb69e787148771fc2805784f146ca6a312718fe eliots-agentics/tasks/22 URP Lit Surface Semantics/task.md (`git hash-object --path="eliots-agentics/tasks/22 URP Lit Surface Semantics/task.md" "C:/cseliot/1-Development/1-SBox/2-Projects/unityimporterplus_branch_2/eliots-agentics/tasks/22 URP Lit Surface Semantics/task.md"`)
Tree: a2268dc6cd6ef172d93402fb5e761c22c220d5fb eliots-agentics/tasks/23 URP Lit PBR and Emission Fidelity/status.md (`git hash-object --path="eliots-agentics/tasks/23 URP Lit PBR and Emission Fidelity/status.md" "C:/cseliot/1-Development/1-SBox/2-Projects/unityimporterplus_branch_2/eliots-agentics/tasks/23 URP Lit PBR and Emission Fidelity/status.md"`)
Tree: 3066963859700c296ad188168717a623fa4e2d01 eliots-agentics/tasks/23 URP Lit PBR and Emission Fidelity/task.md (`git hash-object --path="eliots-agentics/tasks/23 URP Lit PBR and Emission Fidelity/task.md" "C:/cseliot/1-Development/1-SBox/2-Projects/unityimporterplus_branch_2/eliots-agentics/tasks/23 URP Lit PBR and Emission Fidelity/task.md"`)
Tree: a2268dc6cd6ef172d93402fb5e761c22c220d5fb eliots-agentics/tasks/24 URP Environment from Volumes/status.md (`git hash-object --path="eliots-agentics/tasks/24 URP Environment from Volumes/status.md" "C:/cseliot/1-Development/1-SBox/2-Projects/unityimporterplus_branch_2/eliots-agentics/tasks/24 URP Environment from Volumes/status.md"`)
Tree: 8b4525ae345b0bb712a0c10d33cbce9cb058e185 eliots-agentics/tasks/24 URP Environment from Volumes/task.md (`git hash-object --path="eliots-agentics/tasks/24 URP Environment from Volumes/task.md" "C:/cseliot/1-Development/1-SBox/2-Projects/unityimporterplus_branch_2/eliots-agentics/tasks/24 URP Environment from Volumes/task.md"`)
Tree: a2268dc6cd6ef172d93402fb5e761c22c220d5fb eliots-agentics/tasks/25 URP Lights and Cameras/status.md (`git hash-object --path="eliots-agentics/tasks/25 URP Lights and Cameras/status.md" "C:/cseliot/1-Development/1-SBox/2-Projects/unityimporterplus_branch_2/eliots-agentics/tasks/25 URP Lights and Cameras/status.md"`)
Tree: d6ac9dfe952e9cd536200e7280e028f81f9bb93a eliots-agentics/tasks/25 URP Lights and Cameras/task.md (`git hash-object --path="eliots-agentics/tasks/25 URP Lights and Cameras/task.md" "C:/cseliot/1-Development/1-SBox/2-Projects/unityimporterplus_branch_2/eliots-agentics/tasks/25 URP Lights and Cameras/task.md"`)
Tree: a2268dc6cd6ef172d93402fb5e761c22c220d5fb eliots-agentics/tasks/26 URP Report-Only Features and Shader Graph Census/status.md (`git hash-object --path="eliots-agentics/tasks/26 URP Report-Only Features and Shader Graph Census/status.md" "C:/cseliot/1-Development/1-SBox/2-Projects/unityimporterplus_branch_2/eliots-agentics/tasks/26 URP Report-Only Features and Shader Graph Census/status.md"`)
Tree: 29dd9767eb9883e106db43829cd9a302df356e12 eliots-agentics/tasks/26 URP Report-Only Features and Shader Graph Census/task.md (`git hash-object --path="eliots-agentics/tasks/26 URP Report-Only Features and Shader Graph Census/task.md" "C:/cseliot/1-Development/1-SBox/2-Projects/unityimporterplus_branch_2/eliots-agentics/tasks/26 URP Report-Only Features and Shader Graph Census/task.md"`)

The `first urp research/` hashes above are snapshots taken before this re-check wrote its own entry and flag; their final blob hashes are self-referential.

## Clarification Q&A

### Batch 2026-11-26 03:50 JST - TIMED OUT (all 5 unanswered), relayed via Signal + NTFY 03:52 JST

Q1 (Task first urp research): Verified on disk: SyntyForest/ProjectSettings/ProjectVersion.txt is m_EditorVersion 6000.3.29f1 (96d2fe17529c) and Packages/manifest.json pins com.unity.render-pipelines.universal 17.3.0. There is no literal "Unity 6.7" editor build in the test project. How should I treat the session's "Unity 6.7 URP" target?
  A) [recommended] Treat "Unity 6.7 URP" as this session's label for the on-disk setup (6000.3.29f1 + URP 17.3.0): research and build against SyntyForest exactly as it is, and record the version fact in the plan.
  B) A literal/different Unity 6.7 editor build is the real target: hold research until I supply that project or version (research briefs would otherwise be pinned to the wrong shader surface).
  C) "6.7" is not load-bearing: target the URP 17.x shader/component surface on Unity 6 generally, note the version fact, and do not tie acceptance to a specific editor build.
An: NO ANSWER (timeout). PROVISIONAL default A adopted 2026-11-26 03:52. Research the on-disk setup, record the version fact. Read-only and reversible; NOT consent. A late answer overrides this.

Q2 (Task first urp research): Your standing goal this session is driving every task to DONE? YES/REJECTED/FAILED, and the session context says the session is about building Unity 6.7 URP support. What should the loop deliver?
  A) [recommended] Research doc plus numbered backlog task folders, then implement those backlog tasks in this same loop until each reaches a terminal verdict (a full URP visual-support build).
  B) Research doc plus backlog folders only: stop after committing the plan and leave implementation for a later session (leaves "building URP support" unfulfilled this session).
  C) No research deliverable: go straight to implementing URP visual support on the existing importer code, with the plan kept as journal notes only.
An: NO ANSWER (timeout). PROVISIONAL default A adopted 2026-11-26 03:52, BUT implementation workers are HELD until Q2 is answered, because dispatching implementers is the consequential/irreversible step. Read-only research proceeds now. A late answer overrides this.

Q3 (Task first urp research): task.md says "NOT currently a code importer project. Just the visuals." What should URP support cover?
  A) [recommended] Visual fidelity only: URP materials/shaders and textures, lights, cameras, skybox, fog, reflection probes, volumes/post-processing and terrain visuals. Out of scope: C# scripts, animation controllers, physics, audio, UI.
  B) Everything the archive contains including component scripts; "just the visuals" only means gameplay logic is not a concern (larger scope, more backlog tasks).
  C) Materials and lighting only; skip cameras, volumes, probes, sky/fog and terrain (smaller scope, matches the existing HDRP task split most closely).
An: NO ANSWER (timeout). PROVISIONAL default A adopted 2026-11-26 03:52. NOT consent on a user-visible look choice; any look decision stays with the Director. A late answer overrides this.

Q4 (Task first urp research): session-latest.md's standing note says "Use the projects build cli, use screenshots to validate". Large testing rounds are the ones you want an NTFY before. What validation am I authorised to run?
  A) Authorised: run harness imports of SyntyForest and s&box editor play/screenshot captures as needed, sending an NTFY before each large testing round.
  B) [recommended as provisional least-authority] Headless only: harness/verify_output.py gates are fine, but no editor screenshot rounds without asking me first.
  C) No large testing this session: validate with headless compile/structure gates only; I will do the visual checks myself.
An: NO ANSWER (timeout). PROVISIONAL default B adopted 2026-11-26 03:52 (least authority; do not assume authorisation for large testing). NTFY precedes any large testing round regardless. A late answer overrides this.

Q5 (Task first urp research): Extra tooling beyond the existing harness, verify_output.py, and the SyntyForest project?
  A) [recommended] No: use only the existing harness, verify_output.py and SyntyForest; build no new tooling (keeps the deliverable to research plus importer code).
  B) Yes: build a small dedicated URP fixture package for repeatable gates (extra tooling, one more task).
  C) Decide later: ask again if research shows a gap the existing tooling cannot cover.
An: NO ANSWER (timeout). PROVISIONAL default A adopted 2026-11-26 03:52. A late answer overrides this.

Reply-file check 2026-11-26 03:52 JST (immediately after the relay): C:/cseliot/Mods/hermes-comms-reply.md mtime was still 2026-11-25 22:58:28 and contained a 4-answer block (`1A, 2A, 3A, 4A`) about a restructure/push decision from an earlier batch. It mismatched this 5-question batch in count and context, so it was NOT applied. A NEW reply block landed at 03:57 (below) and is the authoritative answer to this batch.

### Director answer 2026-11-26 03:57 JST (Signal relay) - CLARIFIED? YES!
Verbatim newest block from C:/cseliot/Mods/hermes-comms-reply.md, headed `## Phone reply - URP scope (2026-11-26 03:50)`:
  1A 2A 3A 4A 5A
  1A: SyntyForest on 6000.3.29f1 / URP 17.3.0 is the fixture; record the version fact and treat "Unity 6.7 URP" as this session's label, but keep the work on the URP 17.x surface rather than tying it to one editor build.
  4A with conditions: before any editor round, check that no editor of mine is already open on that project, never close an editor you did not launch, and NTFY before each large round.
  5A now; take 5C only if the research shows a real gap - do not pre-build a fixture package.

Full text of each chosen option (from the 03:50 batch above):
  Q1 A: Treat "Unity 6.7 URP" as this session's label for the on-disk setup (6000.3.29f1 + URP 17.3.0): research and build against SyntyForest exactly as it is, and record the version fact in the plan. [Director adds the Q1 C nuance: keep the work on the URP 17.x surface, do not tie acceptance to one editor build.]
  Q2 A: Research doc plus numbered backlog task folders, then implement those backlog tasks in this same loop until each reaches a terminal verdict (a full URP visual-support build).
  Q3 A: Visual fidelity only: URP materials/shaders and textures, lights, cameras, skybox, fog, reflection probes, volumes/post-processing and terrain visuals. Out of scope: C# scripts, animation controllers, physics, audio, UI.
  Q4 A: Authorised: run harness imports of SyntyForest and s&box editor play/screenshot captures as needed, sending an NTFY before each large testing round. [Director's three conditions apply.]
  Q5 A: No: use only the existing harness, verify_output.py and SyntyForest; build no new tooling. [Director: take 5C only if research shows a real gap; do not pre-build a fixture package.]

Disposition of the provisional defaults:
  - Q1 provisional A: SUPERSEDED by 1A plus the 1C nuance. Acceptance targets the URP 17.x shader/component surface, pinned to the on-disk 6000.3.29f1 / URP 17.3.0 fixture but not to a single editor build. Record the version fact.
  - Q2 provisional A with implementation workers HELD: CONFIRMED and the HOLD IS LIFTED. Implementation workers may be dispatched; drive the generated backlog folders to terminal verdicts in the same loop.
  - Q3 provisional A: CONFIRMED.
  - Q4 provisional B (headless only): SUPERSEDED by 4A. Editor rounds are authorised subject to the three conditions; NTFY precedes each large round.
  - Q5 provisional A: CONFIRMED, plus the conditional 5C fallback only on a demonstrated research gap. No fixture package is pre-built.
Per the missed-clarify rule this late answer is a correction, not a footnote: the previously announced plan (implementation held, headless only) is RETRACTED and the loop is re-planned from these answers. 4A's conditions are standing constraints copied into every child brief that touches an editor.

### Parent decisions (provisional; change if wrong)
- P1 (2026-11-26 03:52): "first urp research" is a PLANNING task whose deliverable is tasks/ files. Per the unityimporterplus skill, a planning task is Master work, not a Task Worker job: the Master runs 2-3 read-only research subagents, writes a <topic>-research.md plus numbered backlog task folders, and commits only research + task files by explicit path. No importer code is implemented during the planning phase. status.md WORKER: MASTER.
- P2: The four deleted tasks/ folders (LOD Support, objects misplaced, skybox incorrect, tree-color-issue), the modified Libraries/based.pathtool build record, the modified session-latest.md, and the untracked unityimporterplus_branch_2.sbproj are Director-side state; they stay UNSTAGED and are never committed by us.
- P3: No editor/browser/CLI bridge, no harness import run and no screenshot round during the research phase (Q4 provisional B). The session-start harness compile gate already proved the build route works.
- P4: Every research child writes its report to an absolute scratch path and returns a compact census in its summary; the Master synthesises the repo-side research doc from those reports.
- P5: Terminal verdict for this task requires REVIEWED? YES!, a real commit, a Tree: manifest matching `git rev-parse HEAD:<path>`, and clean git status on the committed paths. A timed-out clarify does not authorise closing it.

## Relevant History

2026-11-26 03:52 (Master, session start). Verified on disk:
- Branch urp_6_7_support, HEAD bfc189b ("update to better agentics work. ===NOTE===!!!! real changes/notes were LIKELY clobbered into this commit. RIP"), tracking origin/main. Push remote present: https://github.com/CSEliot/UnityToSBoxConverterImporter.
- Test project C:/cseliot/1-Development/4-Unity/2-Projects/SyntyForest: Unity 6000.3.29f1 (96d2fe17529c), URP com.unity.render-pipelines.universal 17.3.0. Also present: a 7.2 MB Unity Project Auditor report dated 2026-11-26.
- Importer has NO URP pipeline: Editor/Core/UnityHdrp.cs declares UnityPipeline as Unknown/Hdrp only; HDRP-only gates live in UnityHdrp.cs, UnityHdrpLighting.cs, UnityHdrpProbes.cs, UnityImport.cs. Editor/Core/UnityMaterial.cs header says "Best-effort conversion of text-serialized Standard/URP material properties", i.e. URP materials currently ride the Standard path.
- Live task folders: only "first urp research". All other tasks/ entries are Director archive moves (unstaged deletions).
- Session-start compile gate: dotnet build Documentation/Unity-To-SBox-Automated/harness/harness.csproj -c Release -p:IUP_ROOT=<project> --no-incremental -> Build succeeded, 0 Warning(s), 0 Error(s). dotnet 10.0.302. Log in the Hermes scratch dir as iup-harness-session-start.log.
- eliots-agentics/session-latest.md standing constraint: "Validation and Testing Specification: Use the projects build cli, use screenshots to validate, etc."


## Stylistic Task Notes:
References to variables like X and Y automatically imply that these are to be exposed properties for developer tweaking.
When programming, portability and DRY are a priority.

## Post-Task Follow Ups
