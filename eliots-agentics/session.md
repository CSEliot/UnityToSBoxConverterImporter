# Session

## Notes

Standing constraints (Director 2026-11-24, recorded by Task Master; copy into
every child brief):
- HDRP support is ABANDONED for this project. Do not work on HDRP-specific
  behaviour: PhysicallyBased sky, HDRI, sunIntensity / celestial body, baked
  lightmaps / GI, HDRP light-unit conversion, Exposure EV100, Volume semantics.
- Unity shows NO visible fog (at most ~1000 ft, barely noticeable). The imported
  s&box output must show no fog. The Director removed the Fog component himself
  and got much closer to parity.
- The skybox symptom is NOT HDRP-related; it is downstream of fog/lighting.
- AFTER fog is solved, increase directional light intensity to reach lighting
  parity. That is the Director's own successful experiment.
- `Restroom_Wall_01 (3)` is still misplaced and/or rotated; archived task 14
  (b06f29c) is reopened. Its untested channels: rotation/scale basis, mirror
  variants, rotation/scale nested overrides.
- SUPERSEDED 2026-11-24 22:09: the earlier "live Dev editor holds the
  Director's UNSAVED in-memory scene work" condition no longer holds. The parent
  checked editor_status: clean Untitled scene, SceneHasUnsavedChanges:false. The
  parent then launched a Dev editor (PID 43028) on the real project for
  read-only play captures; no scene was saved and nothing on disk was changed.

Parent decisions (recorded 2026-11-24 22:09, per goal-doc-taskmaster):
- The comparison oracle is now Testing/Unity-Screenshot-Outside.png; the Unity
  source at HEAD 49a49ea carries the Director's camera-position update and
  19:19 HDRP lighting/exposure changes.
- The acceptance bar is BEST EFFORT (oracle-check-screenshot.md BIG NOTE).
  HDRP-specific gaps (exposure/EV100, PhysicallyBasedSky, emissive windows) are
  out of scope per the 2026-11-24 abandonment and are not chased.
- Captures used the real project editor rather than a scratch mirror, because
  the unsaved-work condition that required the mirror is gone.
- The outdoor comparison is the closing gate; its evidence file is
  <scratch>/taskmaster-nov22/outdoor-compare/COMPARISON-2026-11-24.md.

## Director decision 2026-11-24 (acknowledged; /goal loop resumes next)
Verbatim: "Fix trees and skybox 100% (i'm confident those are fixable) and
then fix lighting to "best ability"."
Parent decisions: work order trees -> skybox -> lighting; BEST EFFORT now
applies to lighting only, not trees/skybox. This real Director turn IS the
fresh decision that unblocks re-tuning directional intensity against the
outdoor oracle. Any code change resets REVIEWED? to NO! on the affected tasks
and needs a fresh critic "no further changes" before DONE? YES!.
Standing evidence: <scratch>/taskmaster-nov22/outdoor-compare/COMPARISON-2026-11-24.md.

## Clarification Q&A

