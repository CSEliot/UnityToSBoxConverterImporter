# Task 25 - implementation evidence (2026-11-27, HEAD 56192a0)

## Files changed (all UNSTAGED; no git writes this turn)

- `Editor/Core/UnityUrp.cs` (+74 lines)
- `Editor/Core/UnitySceneExport.cs` (+22/-6)
- `Editor/Core/UnityPrefabExport.cs` (+25/-2)
- Task-journal notes only; `status.md` is parent-owned and was not edited by this worker.

## Mechanism

- `UnityPrefabExport.Converter` derives `UrpScene = UnityVolume.ScenePipeline( Blocks ) == UnityPipeline.Urp`
  (file-level census, NOT per-material). Only then does it build `UrpCameras` / `UrpLights`:
  it enumerates EVERY `!u!114` block, requires `UnityUrp.IsEnabledSibling(body, kind)` (script guid
  from `m_Script`, kind from the existing `UnityUrp.Component` table, `m_Enabled != 0`, and a class
  name check when `m_EditorClassIdentifier` is actually authored), groups by the `m_GameObject`
  fileID and orders by block id before choosing. No blind `First()` over unfiltered candidates.
- Both camera call sites and both light call sites (`UnityPrefabExport.cs` Node conversion) now pass
  the URP sibling body together with the generic Unity Camera/Light class body and the existing HDRP
  body. `urp` is null on Standard/HDRP/Unknown files, so those emitters are byte-identical.
- `UnitySceneExport.Camera( id, body, hd, urp, label, enabled, main, report )` returns an UNPARENTED
  `List<JsonObject>` as before; it applies the three Urp-gated NEW MAPPINGs and appends report lines.
  `UnitySceneExport.Light( id, body, hd, urp, label, enabled, out tag, out converted, report )` keeps
  the generic Unity conversions (colour, CCT, intensity, range, spot cones, shadows) unchanged and
  appends the light's URP-only report lines. Grep confirmed those were the only two call sites.

## NEW MAPPINGS (all developer-tweakable properties in `UnityUrp.cs`, Urp-gated)

| Source field | Engine counterpart | Property | Rule |
|---|---|---|---|
| `m_RenderPostProcessing` | `CameraComponent.PostProcess.cs:35 EnablePostProcessing` | `UnityUrp.MapRenderPostProcessing` (default true) | `!= 0` -> true |
| `m_Antialiasing` | `CameraComponent.cs:143 DisableAntiAliasing` | `UnityUrp.MapAntialiasing` (default true) | any non-zero -> false; same rule as HDRP |
| `m_ClearDepth` | `CameraComponent.cs:27 ClearFlags` | `UnityUrp.MapClearDepth` (default true) | `0` -> `Color`; `1` -> `All` |

No fixture body has `m_ClearDepth: 0`, so only the `1 -> All` half is acceptance-provable; this is
reported as an unproven half, not silently claimed. URP shadow tier, soft-shadow quality and cookie
size were NOT substituted into `ShadowCascadeCount`, `ShadowHardness` or `Cookie`.

## Report-line format

`URP-only <field>=<value> on <GameObject> (<component>) has no Sandbox counterpart; reported, not converted.`

`UnityUrp.ReportUnmapped` reads P4 field values verbatim (`Scalar`, inline `{}`, or a whitespace-
collapsed nested section) and reports all listed light/camera fields plus every authored
`m_TaaSettings` child. A separate mapped-field line prints both source and emitted values for
`m_RenderPostProcessing`, `m_Antialiasing` and `m_ClearDepth`.

## Encoding / ownership

`UnityUrp.cs` is LF and BOM-less (git warns LF will become CRLF in the index; its index blob is LF).
## 2026-11-27 01:15 - post-review fixes (Master-accepted findings 1-5)

1. `UnityUrp.cs` `UnmappedTaaFields` now stores canonical `m_*` names with the legacy lowercase names as
   aliases; `ReportUnmapped` emits whichever variant is authored and `break`s so one value is never
   reported twice. All 6 camera bodies now produce 6 TAA lines each (36 total).
2. `UnityPrefabExport.cs` `Section` is indentation-aware: it keeps deeper-indented children and
   same-indent YAML sequence items (`- {fileID: ...}`), and stops at the next same-or-shallower field.
   This preserves `m_Roots` / `m_AddedComponents` / `m_Materials` callers while retaining nested `m_Bits`;
   a first draft that broke list items was caught by a 728k-line scene diff and corrected before the
   final gate run (`scene-diff-r2.txt` is now 0 lines).
3. `UnitySceneExport.cs` URP `EnablePostProcessing` now uses the authored value when present and the URP
   source default `false` when absent (`UniversalAdditionalCameraData.cs:501`); `urp == null` (HDRP and
   Standard) stays `true`. No fixture body omits the field, so output is unchanged.
4. The mapped report line derives `DisableAntiAliasing` from the source rule, not from the emitted
   dictionary, and prints `DisableAntiAliasing not mapped (MapAntialiasing off)` when the property is off.
5. `UnityUrp.IsEnabledSibling` strips a `, Assembly` suffix before taking the class name; the
   `UnityPrefabExport.UrpComponents` XML comment now states the lowest-block-id tie-break used by
   `OrderBy(...).First()`.

## Encoding / ownership (still true after the fixes)
`UnityUrp.cs` is LF and BOM-less; `UnitySceneExport.cs` and `UnityPrefabExport.cs` were already CRLF in
this checkout and remain CRLF with no BOM. No other file was edited; no editor was launched; no
commit/stage; no new tooling or fixture in the repo (both probes live only in scratch).
