# Session

## Notes

Standing constraints (Director 2026-11-25, recorded by Task Master; copy into every child brief):
- Baseline HEAD is c91e87b (the 2026-11-24 importer WIP adopted by ONE explicit-path baseline commit, Q1=1A). Pushed to origin/main.
- Parent decisions (change if wrong): the 2026-11-24 standing constraints that used to live here (HDRP abandonment, "no fog", work order trees -> skybox -> lighting, BEST EFFORT, Restroom_Wall_01 note) were deliberately removed by the Director (HEAD 2c17e22 "cleanup") and are NOT in force. Q2 answer "ignore the git diff" / "Ignore what you learned from the git diff of session.md" means no ban is invented from them. Scope is set by the four live task files and the Director's fresh instruction only.
- Q4=4B: the full clean re-import loop is AUTHORIZED and REQUIRED (launch editor, open scene, play, capture two play frames against Testing/Unity-Screenshot-Outside.png). Not out-of-scope extra testing.
- Q3: skybox and tree colour are agent-runnable from a rendered frame compared to the Unity screenshot; LOD/z-fighting and objects-misplaced may close DONE? YES! with a self-contained `Open:` line for the Director's own final judgement. Objective data defects and unchanged reported symptoms never close with an Open: line.
- Oracle: Testing/Unity-Screenshot-Outside.png; Unity source C:/cseliot/1-Development/4-Unity/2-Projects/old_school_model (HDRP GraphicsSettings).
- Session-start harness gate on the adopted baseline: 0 warnings / 0 errors, log C:/Users/Eliot/AppData/Local/hermes/profiles/hermes-sbox/cache/scratch/taskmaster-nov25/session-start-harness-build.log.

### Dispatch record 2026-11-25 (parent decisions, not task-local)
- sa-0-9e2a362d (deleg_74fb5993): clean re-import loop from HEAD 8bb1eaa + two editor play frames + measured comparison; evidence dir C:/Users/Eliot/AppData/Local/hermes/profiles/hermes-sbox/cache/scratch/taskmaster-nov25/reimport-evidence/. Authorised by Q4=4B. Owning install: Dev checkout; no unityimporterplus editor was open (the only live sbox-dev.exe, PID 3464, runs good_boy_come_home and is untouched).
- sa-0-44e5326e (deleg_744073f9): COMPLETE 2026-11-25 02:03 (34 api calls, 455 s). Read-only per-symptom mechanism census verified on disk: C:/Users/Eliot/AppData/Local/hermes/profiles/hermes-sbox/cache/scratch/taskmaster-nov25/diagnosis-census.md, 34990 bytes / 169 lines, sections 1-4, 4+6+5+8 HYPOTHESES with named OBS falsifiers, all explicitly unrun. No project file edited, no editor launched, nothing under Assets/imported read. Its per-task mechanism lines were copied into each task file's ## Notes under "Diagnosis census"; line numbers there are pinned to HEAD 8bb1eaa and must be re-grepped.
- Parent decisions from the census: S3/T6 are ONE shared light-rig/exposure hypothesis viewed from two symptoms (do not build two fixes); S4/T7 are HYPOTHESES about the screenshot oracle being a baked-GI state with an inactive live light - test headlessly (lighting assets + activation chain) before treating the reference frame as an oracle, and never re-enable a disabled light or invent intensity as a symptom mask. No authored dark-sky or red-canopy value exists in the current source chain; a new mapping is not a recovered source value.
- No Task Workers spawned yet: all four task headers remain STARTED? NO!, CLARIFIED? YES!, HISTORY? YES!, REVIEWED? NO!, DONE? NO!, WORKER: NONE. The census is hypotheses, not a gate.
- Assets/imported held 5431 files before the loop (editor-compiled); a fresh headless import is expected to be smaller.
- NTFY status ping fired (HTTP 200) announcing the authorised editor loop before it started.

### Master-run headless observation 2026-11-25 (ONE discriminating observation before implementers)
Evidence file: C:/Users/Eliot/AppData/Local/hermes/profiles/hermes-sbox/cache/scratch/taskmaster-nov25/oracle-state-observation-2026-11-25.md. Run on the CURRENT Unity source (MergeV2.unity mtime 2026-11-25 00:32), not archived records.
- Confirmed: MergeV2.unity:53 m_EnableBakedLightmaps: 1; Assets/School/Scenes/MergeV2/LightingData.asset exists; 71 *_comp_light.exr files beside the scene.
- Confirmed: only 2 inactive GameObjects in MergeV2.unity - 'Post-process Volume' (151205) and 'Setting' (319087), whose Transform is 2006277128.
- Confirmed: the 20,000-lux Lux directional light (GameObject 2035111233, Light 2035111234, HD data m_Intensity 20000) has Transform 2035111235 -> m_Father 129731605 -> m_Father 2006277128 (Setting). It is therefore OFF in the serialized scene by the ancestor activation rule despite its own m_IsActive: 1 / m_Enabled: 1. The other active directional light is only 12 lux.
- Partial OBS-L4: 0 class-205 LODGroup blocks in any source .unity/.prefab. Partial OBS-T8: no red/burgundy foliage asset by NAME (only a red wall material).
- Parent decision: this is an OBSERVATION, not a verdict. The render-state proof (did the reference screenshot come from the baked state?) and the scene identity (MergeV2 versus OutdoorsScene) are unverified. common.md precedence makes the Director's dated Q3 instruction ('skybox and tree color you should be able to discern from a screenshot') outrank the skill's 'Director-level blocker' wording, so I am NOT unilaterally closing or blocking those tasks: I am reporting the mismatch and asking him to confirm the acceptance oracle. The skill's ban still binds: never emit an inactive light as active or map an unrelated sky/celestial intensity field into Intensity to fake the missing baked sunlight.

## Clarification Q&A

### Kickoff batch 01 / 02 / 03 (Director, 2026-11-25, chat)
ASK_SINCE=1795560225 (batch 02). Batch 01 text: C:/Users/Eliot/AppData/Local/hermes/profiles/hermes-sbox/cache/scratch/taskmaster-nov25/batch-01.txt; its chat clarify timed out with all four unanswered at 01:25, once-only Signal + ntfy relay fired 01:28:44 / 01:28:50. Batch 02 text: .../taskmaster-nov25/batch-02.txt; partly answered (Q1/Q2 answered, Q3/Q4 timed out), once-only Signal + ntfy relay fired 2026-11-25 (SEND_EXIT=0, ntfy True HTTP 200), relay text .../taskmaster-nov25/relay-batch-02.txt. Q3/Q4 re-asked in chat as batch 03 and answered (outcome submitted).

Q1 (tree baseline): four Editor/Core files modified 2026-11-24 05:24-05:47, absent from HEAD: UnitySceneExport.cs (omits Sandbox.Fog), UnityHdrpLighting.cs (directional lights bypass /1000 candela scale, clamp 8), UnityPrefabExport.cs (wrapper-root override guard), UnityVolume.cs (fog doc comment).
An (Q1, verbatim, chat): "Adopt as baseline: commit those 4 files by explicit path in ONE baseline commit so every task diff and critic review is against a clean HEAD. Consequence: fog removal, directional scale and wrapper-root guard become official HEAD." (= 1A). Landed c91e87b; origin/main == c91e87b.

Q2 (HDRP scope): the 2026-11-24 standing constraints learned from the git diff of session.md.
An (Q2, verbatim, chat): "ignore the git diff". Clarified in a later real user turn: "And also, what I meant by 2: Ignore what you learned from the git diff of session.md".
Parent decision: those diff-derived constraints are not carried into this loop; HEAD 2c17e22 committed their deletion, so none of them are on disk. No HDRP abandonment ban is invented.

Q3 (closing rule, batch 03):
An (Q3, verbatim, chat): "The skybox and tree color you should be able to discern from a screenshot. the others you'll have to marke done yes i guess because that's on me to finalize."

Q4 (extras, batch 03):
An (Q4, verbatim, chat): "Yes: run the full clean re-import loop (launch editor, open scene, play, capture two play frames against Testing/Unity-Screenshot-Outside.png). Consequence: costs a full editor open/compile/play cycle per verification round." (= 4B)

