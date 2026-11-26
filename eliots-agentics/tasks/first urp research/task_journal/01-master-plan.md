# 01 - Master plan: first urp research

2026-11-26 03:52 JST
Role: Task Master, per eliots-agentics/roles/roles.md and eliots-agentics/roles/master.md.

## Task shape
"first urp research" asks: "Do comprehensive research for this unity -> sbox importer for support for Unity 6.7 URP games. (Reminder, this is NOT currently a code importer project. Just the visuals.)"
Its Description is empty and its Clarification Q&A was empty at session start; status.md was a whole template copy with {subagent_id} / {state_here} placeholders. Because task.md has a real Task body, this is WORK, not an unfilled stub: the header placeholders were replaced with concrete values and the task was started.

Per the unityimporterplus skill, a planning task whose deliverable is tasks/ files is Master work, not a Task Worker job. The Master spawns 2-3 read-only research subagents, writes a <topic>-research.md plus one self-contained backlog task folder per independently shippable change, and commits only the research and task files by explicit path. No importer code is implemented during the planning phase.

## Grounded state (verified on disk this turn)
- Branch urp_6_7_support, HEAD bfc189b, tracking origin/main. Push remote: https://github.com/CSEliot/UnityToSBoxConverterImporter.
- SyntyForest test project: Unity 6000.3.29f1 (96d2fe17529c), URP com.unity.render-pipelines.universal 17.3.0. No literal "Unity 6.7" editor build exists in the test project; that fact is relayed to the Director as Q1.
- Importer has no URP pipeline. Editor/Core/UnityHdrp.cs declares UnityPipeline as Unknown/Hdrp only; PipelineOf returns Hdrp only for HDRP shader guids. HDRP-only gates: UnityHdrp.cs, UnityHdrpLighting.cs, UnityHdrpProbes.cs, UnityImport.cs. Editor/Core/UnityMaterial.cs header says "Best-effort conversion of text-serialized Standard/URP material properties", so URP materials currently ride the Standard path.
- Session-start harness compile gate: Build succeeded, 0 Warning(s), 0 Error(s), dotnet 10.0.302.
- Director archive deletions under tasks/ and other uncommitted Director-side files stay UNSTAGED.

## Loop
1. Session-start compile gate (done, background log in Hermes scratch: iup-harness-session-start.log).
2. Five-question batched clarification (done 03:50; timed out, all unanswered). Relayed once via Signal + NTFY. Verbatim batch and provisional defaults in task.md ## Clarification Q&A; dated cross-task record in session-latest.md.
3. Three read-only research children, each with a narrow area and a scratch report path pinned in SPEC.md:
   - 01-importer-census: current render-pipeline detection, HDRP gate sites, Standard/URP material conversion, report/issue codes.
   - 02-syntyforest-census: URP shader guids/names actually used, material properties and keywords, scene-level visual components and URP MonoBehaviours, asset inventory.
   - 03-sbox-capability-map: s&box material/shader and scene-component surface, what the importer already emits, which URP features have no counterpart.
4. Master verifies each report on disk, re-derives load-bearing claims, then writes urp-support-research.md plus numbered backlog task folders. Commit research + task files by explicit path only.
5. Drive the generated backlog folders to terminal verdicts in the same loop (standing goal), but implementation workers are HELD until Q2 is answered because that is the consequential step.
6. Review Critic before each terminal verdict; NTFY on task done; Post-Task Follow Ups addressed before closing.

## Constraints carried into every child brief
- Read-only. No edits to the project tree, skills, memory or config. No git operations. No editor/browser/CLI bridge. No harness import run. No screenshot round.
- Never read, write, list or stage {project}/eliots-agentics/.humans-only/.
- Final answer in English. Reports are scratch working files; scratch paths must not be cited in any repo-side deliverable.
- Counts must carry the exact command or pattern used to produce them.
- Rewrite the report file after EACH answered question (skeleton-first is fine) so a truncated run still leaves findings on disk.
- No process wording (agent/child/parent/spec/brief) in report content.

## Exit criteria for this task
REVIEWED? YES! with a critic verdict of "no further changes", a real commit containing exactly the reviewed tree, a Tree: manifest whose hashes match `git rev-parse HEAD:<path>`, and `git status --short -- <committed paths>` empty. A timed-out clarify is not authorisation to close.
