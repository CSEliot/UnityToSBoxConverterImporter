## Status: STARTED? YES! CLARIFIED? YES! HISTORY? YES! REVIEWED? NO! DONE? NO! WORKER: sa-0-2fd743fc OTHER: Q1-Q3 answered, Q4 provisional push; spec in scratch

## Task
Build support for importing directly from a unity project directory.

### Description
Reusing as much code as possible, give the option (in both the terminal and in-editor GUI) to do the import directly by pointing at a unity folder.
Only other instructions are:
Have 'IsValidUnityFolder' detection.
Ask which folders from the /Assets/ that the user wishes to import.

### Work Review


## Notes


## Clarification Q&A

	Q1: When importing from a Unity project folder, which /Assets/ folders should be offered for selection?
	An: Immediate subfolders of Assets/ (Recommended): short checkbox list, e.g. AssetStoreOriginals, Scenes, Settings.

	Q2: How should the terminal ask which folders to import?
	An: Interactive numbered prompt when stdin is a terminal, plus a --folders flag for scripts/LLMs.

	Q3: What should IsValidUnityFolder require to accept a directory?
	An: An Assets/ directory plus a ProjectSettings/ directory.

	Q4: common.md now says 'Always git push'. origin is your own fork and main is ahead 20 commits. Confirm the push policy for this task?
	An: No answer (timed out). PROVISIONAL parent decision: push after the commit, as common.md now requires; origin is CSEliot/UnityToSBoxConverterImporter.git (your own fork) and main is ahead 20. Say the word and I will keep it local-only instead.

	Parent decisions (change if wrong):
	- First positional arg stays load-bearing: a DIRECTORY means a Unity project, a FILE means a .unitypackage. No new mode switch; the legacy positional contract is preserved.
	- GUI gets a new "Browse folder..." button plus a new menu item; the existing .unitypackage flow is untouched.
	- Folder selection is a checkbox dialog of the immediate Assets/ subfolders, default all selected; the existing package-contents tree still governs per-file selection after load.
	- IsValidUnityFolder is a public static engine-free method in Editor/Core, called by BOTH the CLI and GUI (DRY). Its criteria are exposed as tweakable constants.
	- Output paths, report JSON, ImportOptions and all conversion logic are unchanged; only the archive source changes. building 24 and city--1 1,166 no-scenes outputs must stay byte-identical.
	- The s&box editor is not available headlessly; GUI code is proven to COMPILE offline, and the visual/interaction check is an explicit Open follow-up.

## Relevant History 


## Stylistic Task Notes:
References to variables like X and Y automatically imply that these are to be exposed properties for developer tweaking.
When programming, portability and DRY are a priority.

## Post-Task Follow Ups 
