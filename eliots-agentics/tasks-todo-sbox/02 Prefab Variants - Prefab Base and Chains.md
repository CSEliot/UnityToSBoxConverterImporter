## Status: STARTED? NO! CLARIFIED? NO! HISTORY? NO! REVIEWED? NO! DONE? NO! WORKER: NONE OTHER: backlog, generated from tasks/sbox-expert-research.md; depends on task 01

## Task
	Convert prefab-base variants and variant chains (prefab of prefab, up to the depth seen in the school package), including added and removed objects/components.

### Description
	After task 01 handles variants whose base is an FBX, the school package still leaves 18 prefabs that are variants of another variant: 12 one step from a prefab and 6 two steps. All chains terminate at an in-package FBX, so no external asset is needed.

	Evidence: 346 .prefab assets in Testing/old_school_project.unitypackage are all variants; 328 point at an FBX, 12 at another prefab, 6 at a prefab two steps from an FBX. The 18 chained prefabs and the 18 prefabs that add a component are disjoint sets: the chained files add nothing, and the 18 with m_AddedComponents are direct FBX-base variants (task 01). Across all 346, no prefab has a non-empty m_AddedGameObjects or m_RemovedGameObjects and none removes a component; every file's m_RemovedComponents list is empty. UnitySceneExport currently skips every scene instance of an unconverted prefab, so the 18 chains matter for the 2,861-instance MergeV2 scene and the 201-instance Test_Range scene.

	Required work:
	1. Resolve a variant's base through .prefab bases recursively until an FBX base is reached, reusing task 01's model-prefab construction. Keep a visited set and report a cycle as a per-file error; never recurse unbounded.
	2. Convert bases before the variants that use them. Extend UnityPrefabExport.Order (and the scene's SourceOf path) so a variant's source JSON is available when it converts, including when only the outer variant was selected.
	3. Merge m_Modifications from the outermost variant inward: outer values win, unset values come from the base. Same key mapping as task 01 (name, active, TRS, static flags, materials, root order).
	4. Implement m_AddedGameObjects, m_AddedComponents and the removed lists as pass-through semantics on the converted base, the same model UnitySceneExport already uses for scene instances. The school package exercises none of them, so prove them with the synthetic fixture below; added colliders would convert, other component types are reported as Information with counts.
	5. Keep Unity's object-ID mapping straight through the chain: a scene instance of a variant targets the variant's own exposed IDs, while the variant's modifications target its base's IDs. Convert both to the generated prefab's stable GUIDs, and add a regression check for an instance override on a chained variant.
	6. Keep task 01's non-variant and FBX-variant output byte-identical.

	Acceptance:
	- All 346 school prefabs convert with tasks 01 and 02 in place; 0 "Prefab has no root transform" errors.
	- MergeV2.scene has 2,861 __Prefab stubs and Test_Range.scene 201, with per-instance name/transform/material overrides present on sampled instances from both scenes.
	- A variant-of-variant fixture (three levels, one added object, one removed component) converts to a prefab whose JSON contains the added object and omits the removed one.
	- No cycle or missing-base case throws; each is a reported per-file issue.
	- Two harness runs byte-identical; verify_output.py 0 problems; building and city--1 outputs unchanged.

## Notes

	Implementation home: Editor/Core/UnityPrefabExport.cs (Converter/Source/Order, current instance handling around 752-830 and 1053+), Editor/Core/UnitySceneExport.cs (Anchor at ~103, Owner at ~74), Editor/Core/UnityImport.cs (prefab loop around 360-410 and Order call site).

	Watch the current scene path: UnitySceneExport.Anchor resolves a converted prefab's object GUID with StableGuid($"{source.Key}:{g}") where g is the id from the scene's modification. For prefab variants the scene id is in the variant's own id space, not the base's, so the mapping built here must be exposed to the scene converter rather than guessed there.

## Clarification Q&A


## Relevant History 

	Source: tasks/sbox-expert-research.md. Task 01 must land first; this task closes the remaining 18 school prefabs and proves the chain semantics so later packages do not reopen the same guard.

## Stylistic Task Notes:
References to variables like X and Y automatically imply that these are to be exposed properties for developer tweaking.

## Skills To Know: for you and/or subagents:
	unityimporterplus
	unity-to-sbox-conversion
	sbox-prefab-editing
	sbox-scene-prefab-inspection
	sbox-offline-compile-check

## Post-Task Follow Ups
	Confirm with a third-party package that mixes variants and ordinary prefabs; if the package's bases are outside the archive, record the exact report line it produces.