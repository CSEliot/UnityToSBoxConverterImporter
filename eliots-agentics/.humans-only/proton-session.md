# Session

## Notes

	Use the currently live proton-spawned sbox editor session instead of the linux native editor. The shader involved in current tasks currently only renders currently inside the
		proton version of the editor. So subagents should NOT shut it down after they are done, and must take turns if multiple running at once.

	How to reach it (verified 2026-11-02): the Linux sbox-cli cannot see a Proton editor
	(--list returns []). Run the Windows sbox-cli.exe inside the same prefix:
		eliots-agentics/proton-cli.sh --list
	then use the Windows pid it prints:
		eliots-agentics/proton-cli.sh --connect <pid> --call editor_status
	The pid changes whenever the editor restarts; always start with --list. Screenshots come
	back as PNG paths printed by the tool (they need the same red/blue channel swap as before:
	Image.merge('RGB', (b, g, r))). Real Linux path of a returned image:
	<pfx>/drive_c/users/steamuser/AppData/Local/Temp/sbox-cli/<id>/<file>.png

	Editor compile fix (2026-11-02 23:05): the editor stopped compiling game code with
	"Could not find a part of the path 'S:\steamapps\common\sbox\bin\managed\Sandbox.System.dll'"
	(0 compile errors, LastCompileSucceeded=false, ~every 4 s after any Code/ save). Cause: the
	Wine prefix's dosdevices lost its s: mapping (it was rewritten at 22:55, most likely by a
	"proton run" attempt from this session that ran prefix setup without Steam's library mounts).
	Fix: ln -sfn /home/cseliot/.local/share/Steam <pfx>/dosdevices/s: then touch any Code/ file and
	poll editor_status until IsCompiling=false / LastCompileSucceeded=true. Do not run "proton run"
	against this prefix again; use the wine binary directly as proton-cli.sh does.

## Clarification Q&A

