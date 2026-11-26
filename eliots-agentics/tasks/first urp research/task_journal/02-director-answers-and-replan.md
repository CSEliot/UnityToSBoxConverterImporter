# 02 - Director answers and re-plan (supersedes parts of 01)

2026-11-26 03:57 JST. A late Signal answer landed for the 03:50 five-question batch: `1A 2A 3A 4A 5A`. Verbatim text, full chosen-option wording and per-question disposition are in `../task.md` under ## Clarification Q&A; the cross-task dated record is in `eliots-agentics/session-latest.md` at 03:57.

## What changes from 01-master-plan.md
- 01 said "implementation workers are HELD until Q2 is answered". SUPERSEDED: Q2 = 2A, the HOLD IS LIFTED. After the research deliverable is committed, drive the generated backlog task folders to DONE? YES/REJECTED/FAILED in this same loop.
- 01's "Constraints carried into every child brief" said no editor/browser/CLI bridge and no screenshot round. That still holds for the READ-ONLY RESEARCH PHASE only. Q4 = 4A authorises harness imports and editor play/screenshot rounds for implementation and validation, subject to the three conditions below.
- 01 said "Q4 provisional = headless only" in its loop. SUPERSEDED by 4A.
- Q1 is now 1A plus the Q1 C nuance: the fixture is SyntyForest on 6000.3.29f1 / URP 17.3.0, "Unity 6.7 URP" is the session label, but acceptance targets the URP 17.x shader/component surface and is NOT tied to one editor build. Record the version fact.
- Q3 = visual fidelity only (materials/shaders/textures, lights, cameras, skybox, fog, reflection probes, volumes/post-processing, terrain visuals). Scripts, animation controllers, physics, audio and UI are out of scope.
- Q5 = no new tooling now. 5C (a small dedicated URP fixture package) is available ONLY if research demonstrates a real gap the existing harness, verify_output.py and SyntyForest cannot cover; do not pre-build it.

## Standing 4A conditions (copy into every child brief that touches an editor)
1. Before any editor round, check that no Director-owned editor is already open on that project.
2. Never close an editor we did not launch.
3. NTFY before each large testing round.
Load multiple-sbox-dev-exe and sbox-cli-driving before any editor round.

## Verified research state (03:57-04:06)
- Three read-only reports exist in the scratch research dir; none contain process wording or scratch-path citations. Parent re-derived Child 1's two literal `Pipeline == UnityPipeline.Hdrp` gates (UnityImport.cs:246, UnityMaterial.cs:31) and Child 2's extension counts (4289 files, 695 fbx, 687 prefab, 232 png, 67 tga, 24 shadergraph, 217 mat).
- Child 2 hit the 40-call cap with Q4 written as "NOT CHECKED"; it is being continued once with a bounded Q4-only brief, cross-checked against the parent's own counts. No research re-run.
- Child 3's engine-authored .scene JSON counts are being re-derived by the parent from the exact command in its report.

## Next steps (re-planned loop)
1. Finish Child 2 Q4; verify all three reports.
2. Write `urp-support-research.md` plus one numbered backlog task folder per independently shippable URP visual change, each self-contained with a failing-or-verifiable acceptance bar. No importer code in this planning phase.
3. Hold research + task files unstaged; spawn a Review Critic on `git diff HEAD` plus the explicit new-path list; answer each finding.
4. Commit exactly the reviewed tree by explicit path; verify Tree hashes and clean status; push per the folder's policy.
5. Dispatch implementation workers onto the backlog folders; drive each to a terminal verdict with its own critic and evidence.
6. NTFY on each task done; address ## Post-Task Follow Ups before closing.
