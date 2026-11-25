## Q4 clean re-import evidence (2026-11-25 20:39-21:13) - import PASS, play capture UNRELIABLE

Driver `deleg_2785c2a9` ran the Q4-authorised loop from committed HEAD `bf099d4` (`q4-final/git-state.txt`).

IMPORT PORTION - PASS:
- Harness build `0 Warning(s) / 0 Error(s)` (`harness-build.log`).
- `Assets/imported` moved aside and regenerated: `Assets 1280`, `Converted 958`, `Files 2141`.
- Harness stdout: `issues: unresolved=63 warnings=79 info=3016`; `import-report.json` agrees (`Unresolved` 63, `Warnings` 79, `Information` 3016, `Conflicts` 0). No growth versus the recorded baseline.
- `verify_output.py`: `checked 862 files, 0 problems`.
- `q4-final/import-report.json`, `harness-import.log`, `gate-results.txt` on disk.

PLAY PORTION - FAILED, evidence NOT usable:
- `screenshot-index.txt` logs four distinct Steam screenshots (21:10:14 / 21:10:25 / 21:10:36 / 21:10:46), but `play-camera1.png`, `play-camera3.png` and `play-camera4.png` are BYTE-IDENTICAL (`md5 0cc4ae95be2f74aecd4d7a3ef28d00a4`); only `play-camera2.png` differs. The camera switch did not take effect, so these frames do not represent cameras 1/3/4.
- `q4-final/report.md` is still the all-PENDING skeleton; sections 7-10 were never filled.

Effect on this task: the Q4 regeneration gate is satisfied (import counts, verify, no growth). The Symptom A close/far play-frame pair is STILL UNRUN and must not be sourced from `q4-final/play-camera*.png`. No `DONE?` change.

Reproduce with: `C:/Users/Eliot/AppData/Local/hermes/profiles/hermes-sbox/cache/scratch/taskmaster-nov25/q4-final/`.