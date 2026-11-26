# 01 - Task creation (2026-11-27 00:46)

Opened on Director answer 1A (chat, verbatim: "1A, 2A"), asked as Q1 after task 28's prerequisite gathering closed. Full text of the chosen option is in task.md `## Clarification Q&A`.

Step 2 of the terrain work. Step 1 (task 28) established that the two oracles are at parity and that no further Director oracle work would happen; the parities removed the ablation/matched-capture prerequisites and left one decision, which the Director has now answered: add the implementation task as its own numbered task.

Scope guards (option text as posed by the Master, chosen by the Director with the verbatim reply "1A, 2A"):
- read the 41 MB blob only (parent-measured: `_Meadow_Terrain_01.asset` = 41,358,500 bytes, the only TerrainData in the four-scene fixture);
- no editor launch;
- if the round-trip cannot be shown headlessly, the task ends at its own gate rather than widening;
- do not amend report-only task 26, and do not let this task block tasks 25/26.

Queue position: behind tasks 25 (URP lights/cameras, implementation in flight) and 26 (report-only census, held), because all three touch `Editor/Core/UnitySceneExport.cs` / `UnityUrp.cs` and the harness build+gate has one owner at a time. No worker dispatched at creation; `WORKER: NONE`.

Acceptance is grounded in terrain-import-research.md section 7.2 (round-trip constants) and 7.3 item 8 (binary decode fidelity), with the engine's `TEST/TerrainTests.cs:160-200` bit-exact round-trip as the oracle for "correct".