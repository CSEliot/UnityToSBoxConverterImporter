### Master disposition (2026-11-25 07:53) - task REOPENED
The Director's Q3 `Open:` line is ANSWERED: "I see Floor_stairs_middle (1) popping in and out of its 2 LODs, good job! Don't see any more z-fighting. Good job!" The original symptom (z-fighting from all Unity LODs emitted together as components) is RESOLVED and reviewed commit `1f1c92d` stands for that symptom.

Per master.md step 8 the two NEW `## Post-Task Follow Ups` are non-addressed issues and must be driven before this task can close again. Header set to `REVIEWED? NO!`, `DONE? NO!`, `WORKER: MASTER`. This is a FRESH SYMPTOM TASK, not a continuation of the z-fighting fix.

Symptom A (verbatim): "Floor_stairs_top (1) and Floor_stairs_bottom (1) are essentially the same thing, yet they don't swap LOD's when I move back and forth w the camera like the middle one does. Why? They're essentially the same."
- A1 HYPOTHESIS: the top/bottom families have only ONE LOD level in the source FBX / generated VMDL, so no `LODGroupList` with a second `LODGroup` is emitted - "essentially the same thing" describes the visible mesh, not the LOD data. FALSIFIER: a generated `floor_stairs_top*.vmdl` or `floor_stairs_bottom*.vmdl` containing an LODGroupList with two or more LODGroups with distinct switch thresholds, comparable to `floor_stairs_middle_snaps014.vmdl`.
- A2 HYPOTHESIS: `ModelSplit`'s family grouping keyed on the LOD stem merged top/bottom with a different family and collapsed the switch thresholds. FALSIFIER: count `LODGroup` children per generated VMDL for all three families and compare them.

Symptom B (verbatim): "It appears some far-away LODs are reporting errors from sbox. Perhaps missing materials? An example is Windows_05_Frame_Snaps014. When i get close it looks fine but far away it shows the typical sbox texture/material error."
- B1 HYPOTHESIS: the family-based `needsNeutral` evaluation that dropped the three neutral `.vmat` files also left LOD1+ mesh slots in consolidated VMDLs pointing at a material path that does not exist, so the far LOD renders the error material while LOD0 (up close) resolves correctly. FALSIFIER: grep the generated `windows_05_frame*` VMDL for every LOD mesh's material exception and confirm ALL referenced `.vmat` paths exist on disk; any missing path corroborates, all-present falsifies.
- B2 HYPOTHESIS: the LOD1 mesh uses a different embedded material slot name than LOD0 and the importer's slot map covers only the LOD0 name. FALSIFIER: compare the slot keys of the two LOD levels' `RenderMeshFile` nodes against the VMDL `exception_list` / `from` mappings.

Read-only diagnosis dispatched (findings recorded in `EVID/LOD-FOLLOWUP-DIAGNOSIS.md`). NO code edit, no editor, no `Assets/imported` writes, no commit, no DONE?.
