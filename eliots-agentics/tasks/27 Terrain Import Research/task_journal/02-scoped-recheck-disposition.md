# Scoped re-check disposition - task 27 (2026-11-26 06:23)

Critic: sa-0-51f248d3 (deleg_3a578af6). Verdict: changes required. All 8 original fixes from the 06:12 review were confirmed grounded; two NEW [Low] regressions from the rewrite were reported.

Both ACCEPTED and re-verified against primary source before fixing:

1. [Low] "seven omitted keys" count at deliverable `:101,116,135`: WRONG. Parent re-counted: `sample_terrain_material.tmat:2-15` has 14 keys; `git show HEAD:Editor/Core/UnityMaterial.cs` `ToTmat` at `:418-426` emits exactly 6 (`__version`, `AlbedoImage`, `NormalImage`, `RoughnessImage`, `AOImage`, `HeightImage`), so EIGHT are omitted (`UVScale`, `Metalness`, `NormalStrength`, `HeightBlendStrength`, `DisplacementScale`, `NoTiling`, `Surface`, `__references`), as `:70` already lists. Fixed: global `seven` to `eight` in the deliverable; no other "seven" occurrence existed.
2. [Low] "Both projects ... created 2026-11-25" at `:6`: WRONG for the Unity project. `git -C unity_samples_oracle log --reverse --format='%ci %h %s'` returns `2026-11-23 22:59:27 +0300 e01fd19 Initial check-in`; only `Assets/sample_terrain_asset.asset` has mtime 2026-11-25 22:52. The s&box oracle scene mtime is 2026-11-25 09:38. Fixed: replaced the single date with both specific dates.

No regressions from the rewrite: no numbered task folder beyond 27, no task 26 amendment, no oracle project created by task 27, `Editor/Core/*.cs` diffs remain task 22's, scratch reports still outside the repo, acceptance #1-6 still met. The critic set `REVIEWED? YES!` after its real verdict; the Task Master set it back to `REVIEWED? NO!` because reviewed bytes changed again.

Next: bounded final re-check with the SAME critic, scoped to only these two fixes.
