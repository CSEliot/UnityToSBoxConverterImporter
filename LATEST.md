# LATEST

Working notes for the in-progress task: make the importer's generated output match
`Assets/Manual/` (hand-made, known-good) instead of the current `Assets/Imported/`.

Reference package: `{cseliot}/1-Development/4-Unity/DELETEMECITY/building.unitypackage`
(Synty PolygonCity subset: 3 FBX, 3 .mat, 5 textures, 3 prefabs, 3 collision .asset).

---

## Ground truth established (verified on disk, not assumed)

FBX contents (parsed from the binary, all three are FBX 7400):

| FBX | meshes | material slots | UnitScaleFactor |
|---|---|---|---|
| SM_Bld_OfficeRound_02 | `SM_Bld_OfficeRound_02` | `blinn23` | 1 (cm) |
| SM_Bld_OfficeRound_Base_01 | `SM_Bld_OfficeRound_Base_01`, `SM_Bld_OfficeRound_Base_01_Glass` | `blinn23` | 1 (cm) |
| SM_Bld_OfficeRound_Roof_01 | `SM_Bld_OfficeRound_Roof_01` | `blinn23` | 1 (cm) |

Prefab assignments (from the `.prefab` YAML):

| mesh | material | MeshCollider? |
|---|---|---|
| SM_Bld_OfficeRound_02 | PolygonCity_Mat_01_A | yes |
| SM_Bld_OfficeRound_Roof_01 | PolygonCity_Mat_01_A | yes |
| SM_Bld_OfficeRound_Base_01 | PolygonCity_Mat_02_A | yes (x2) |
| SM_Bld_OfficeRound_Base_01_Glass | PolygonCity_Glass_Mat | no |

Materials → albedo: Mat_01_A → `PolygonCity_Texture_01_A.png`,
Mat_02_A → `PolygonCity_Texture_02_A.png`, Glass_Mat → none (tinted translucent).

How `Assets/Manual/` was actually produced: engine right-click **Create Material** on the
albedo PNG (matches `game/addons/tools/.../Material/MaterialMenu.cs` line-for-line: name
derivation, `_metal`/`_rough`/`_normal` peer search, `materials/default/default_ao.tga`
fallback), re-saved by the Material Editor (hence the header, `//---- Section ----` blocks and
full default property set), then hand-edited to swap AO/SelfIllum. The `.vmdl`s are stock
ModelDoc output with the material assigned by hand.

---

## Root-cause bugs found

1. **Material remaps never match.** `UnityImport.PrepareFiles` builds the ModelDoc remap table
   from converted `.vmat` *filenames* (`polygoncity_mat_01_a.vmat`), but ModelDoc keys remaps on
   the **FBX material slot name** (`blinn23.vmat`). Every model currently ships with three remaps
   that match nothing, and the one real slot unmapped.
2. **Prefab binding silently discards legacy assets.** `UnityPrefabBindings` resolves a
   MeshFilter's `m_Mesh` fileID via `UnityFileId.Hash("Type:Mesh->"+mesh+"0")` (modern xxHash
   sub-asset IDs). These 2017-era assets use *recycle IDs* (`4300000`, `4300002`) listed in the
   `.meta`'s `fileIDToRecycleName`. Nothing matches, so every real assignment is dropped as an
   "unidentified renderer".
3. **import_scale double-applies units.** `UnityModel.ImportScale` returns
   `globalScale * metersPerUnit / 0.0254` = 39.37. The `/0.0254` (metres→inches) is already done
   by ModelDoc from the FBX unit header, so models import ~39x too large.
4. **Two meshes, one slot.** Base_01's meshes share FBX material `blinn23` but take different
   Unity materials. A single ModelDoc remap table (keyed by material name) cannot express that —
   the current code records a conflict and drops both.

---

## Decisions (confirmed with the user)

| # | Decision |
|---|---|
| 1 | `import_scale` = `globalScale * metersPerUnit` (drop the `/0.0254`). Yields 1.0 here. |
| 2 | One `.vmat` per Unity `.mat`, but **named after its albedo texture**, kept in the `.mat`'s own folder (`PolygonCity/Materials/polygoncity_texture_01_a.vmat`). Fall back to the `.mat` name when there is no albedo or the name would collide. |
| 3 | `.vmat` body reproduces the Material Editor's on-disk style: `// THIS FILE IS AUTO-GENERATED` header, alphabetical `//---- Section ----` groups, all shader defaults written out, `"0.000"` scalars / `[1.000000 ...]` colours, and `shaders/complex.shader` **without** `_c`. |
| 4 | When meshes in one FBX resolve to different materials, **split into one `.vmdl` per mesh**, named exactly after the FBX mesh (lowercased). Otherwise keep a single `.vmdl` named after the FBX. |
| 5 | `.vmdl` = `modeldoc30` with the full RootNode/RenderMeshFile field set (`align_origin_*`, `parent_bone`, `base_model_name`, `primary_associated_entity`). |
| 6 | `PhysicsShapeList` / `PhysicsHullFromRender` only when the Unity prefab put a MeshCollider on that mesh (so Base_01_Glass correctly gets none). |
| 7 | Lowercase generated output filenames (`.vmat`/`.vmdl`/`.vtex`) and lowercase every reference path written inside generated documents. Copied source files and derived `_iup_` textures keep their original case — this is exactly what `Assets/Manual/` shows. |
| 8 | `import_filter` always sets `exclude_by_default = true` and names every rendered mesh, not just LOD0. |

---

## Progress

- [x] Reverse-engineer `Assets/Manual/` and confirm how it was produced
- [x] Parse the source package, FBXs and prefabs to establish ground truth
- [x] Identify root causes (4 bugs above)
- [x] Confirm the 8 decisions with the user
- [x] `UnityModel`: fix `ImportScale`; parse `fileIDToRecycleName`; expose per-mesh materials + collision meshes
- [x] `UnityPrefabBindings`: resolve legacy recycle IDs; record per-mesh material and MeshCollider presence
- [x] `UnityMaterial.ToVmat`: Material-Editor output style
- [x] `UnityImport`: albedo-based `.vmat` naming, slot-keyed remaps, per-mesh `.vmdl` split, modeldoc30 template, lowercase generated names/references
- [x] Offline compile check (clean, 0 warnings / 0 errors)
- [x] End-to-end run against the real package
- [x] Adversarial code review, respond to findings (see Phase 2 below)
- [ ] User verifies a real import in the editor (open `manual-imported-example.scene`; the
      Steam editor's MCP can't start under Proton — known multi-prefix `HttpListener` bug,
      see `sbox-proton-diagnostics`; that in-editor HTTP MCP server has since been replaced
      by `sbox-cli`, the engine's named-pipe bridge, so this startup failure no longer applies.
      Whether `sbox-cli` itself works under Proton is not known.)

## Phase 1 — Verification performed

Compile gate (Core has no engine dependency, so it builds standalone):

    cd ~/.hermes/profiles/hermes-sbox/cache/scratch/iupcheck
    dotnet build CoreCompileCheck.csproj -c Release      # 0 warnings, 0 errors

End-to-end harness that drives the real `UnityImport.Run` against the real package
(stub channel extractor, since the real one needs the engine's bitmap wrapper):

    cd ~/.hermes/profiles/hermes-sbox/cache/scratch/iupcheck/harness
    mkdir -p runN && dotnet run -c Release --project harness.csproj -- \
        {cseliot}/1-Development/4-Unity/DELETEMECITY/building.unitypackage runN

Observed on the final run: 11 assets, 7 converted, **zero** unresolved errors
(previously every model reported "no converted material was resolved"), and:

| output | result |
|---|---|
| `sm_bld_officeround_02.vmdl` | byte-identical to Manual (modulo asset paths) |
| `sm_bld_officeround_roof_01.vmdl` | byte-identical to Manual (modulo asset paths) |
| `sm_bld_officeround_base_01.vmdl` | `blinn23` → `polygoncity_texture_02_a.vmat`, physics present |
| `sm_bld_officeround_base_01_glass.vmdl` | `blinn23` → `polygoncity_glass_mat.vmat`, no physics (correct: no MeshCollider) |
| `polygoncity_texture_01_a.vmat` | identical to Manual except `g_vColorTint` alpha (see below) |

Defects the real run exposed in the first cut, since fixed: LOD-suffix remaps were
generated for slots the FBX never declared (a 200-line remap table of dead entries);
`g_vSelfIllumTint` was written as black from Unity's unused `_EmissionColor`, which
would have multiplied the self-illum away; `import_scale` printed `1` instead of `1.0`;
remap/exception-list indentation was two tabs too deep; `.vmat` had a trailing newline
the material editor does not write.

## Phase 1 — Known divergences from `Assets/Manual/` (deliberate)

- A 4th model, `sm_bld_officeround_base_01_glass.vmdl`, is produced (decision 4). Manual has no
  glass model because the glass mesh was skipped by hand.
- `polygoncity_texture_02_a.vmat` and `polygoncity_glass_mat.vmat` are produced. Manual only has
  the one material the user needed.
- `g_vColorTint` alpha is written from Unity's real `_BaseColor` (1.0 opaque, 0.435 for glass).
  Manual has `0.000000` — that is the material editor's default-Color artifact, not a real value.
  Writing 0.0 would make every model fully transparent under a translucent shader.

---

# Phase 2 (2026-09-28/29): whole-package conversion, prefabs included

Scope grew past "match Manual": the importer now converts the entire `building.unitypackage`,
including the three Unity prefabs, into s&box prefabs. `Assets/Manual/` untouched.

## What the tool now does (Editor/Core)

- **Unity prefabs → s&box prefabs** (new file `UnityPrefabExport.cs`): nested prefab
  instances, transforms, names, enabled state; per-instance material and enabled overrides
  (e.g. Mat_02_A on Roof_03/Roof_04). Overrides on objects below a nested prefab's root
  (pos/rot/scale/name/enabled) are implemented but not exercised by any real package output yet.
- **Scale**: `import_scale` stays Unity-unit (1.0); generated models apply
  `ModelModifier_ScaleAndMirror` 39.37 (metres→inches).
- Unity convex mesh colliders → convex hulls (`HullPerMesh`) in the model.
- Untextured transparent materials → glass materials; normal maps carry strength.
- Output folders keep Unity casing, file names lowercase (fixes split
  PolygonCity/polygoncity folders on Linux).
- Linux temp cleanup no longer mistakes a busy lock file for an error.

Output: the harness reports 24 files = 23 under `Assets/Imported/PolygonCity/` (incl.
`Prefabs/sm_bld_officeround_base_01.prefab` + `Prefabs/Buildings/` nested pair) plus
`TutorialInfo/Icons/URP.png`. As of 2026-09-29 09:42 the worktree holds the FULL package
import (~1167 source files under `Assets/Imported/`, ~55 building prefabs) — a later, larger
reimport landed after this note was written; check mtimes before quoting file counts.
Scene `manual-imported-example.scene` building instance →
`imported/polygoncity/prefabs/sm_bld_officeround_base_01.prefab`. The light and camera in that
scene were converted by the scratch script `~/.hermes/profiles/hermes-sbox/cache/scratch/iup/scene_edit.py`
(pos `(uz,-ux,uy)*39.37`, quat `(-z,x,-y,w)`), **not** by Editor/Core. Global Volume not converted;
2D skybox kept.

## Reviewer findings — disposition

- Fixed: dropped instance overrides (no `objectReference:` line); oversized/broken prefab
  crash on package load/import (now a reported `conversion-skipped` issue — Error severity,
  not a warning); duplicate-transform crash; YAML quotes in
  object names; unused code removed.
- Not fixed: prefab variants still skipped, now reported as unsupported rather than
  "no root transform".
- Pushed back: "prefab root transform discarded" — the scene instance sets root
  pos/rot, scene check measured 0.000 error; empty `{}` Manual prefabs trace to commit 49d16c1.
- Kept by design: transparent no-colour-texture Unity material → additive glass (changes
  output for other packages with such materials).

## Known gaps

- Unity's two baked convex colliders on the base → one hull from the rendered mesh;
  their source `.asset` meshes can't be converted.
- Camera background colour doesn't match Unity (hidden behind skybox).
- Bloom/vignette/motion blur from Global Volume not ported (agreed).
- One deliberate divergence from Manual: base renders with Mat_02_A per Unity; the
  hand-made Manual copy is the one that's off.

## Verification status

- Core builds + runs headless against `building.unitypackage`; output deterministic and
  matches project files. Editor project compiles offline.
- Scene checker vs Unity: all 10 building pieces, position/rotation error 0.000; every
  material incl. override targets matched; coordinate conversion and light direction
  checked against engine source.
- NOT done: real editor run (Steam editor under Proton cannot start its MCP server —
  `[MCP] Couldn't start MCP server on port 7269 (Call not implemented.)`, the documented
  Wine multi-prefix `httpapi.dll` gap; that in-editor HTTP MCP server has since been
  replaced by `sbox-cli`, so this failure no longer applies, though whether `sbox-cli`
  works under Proton is not known). Opening the scene in the editor is the check left.

Working tree (nothing committed): modified Editor files (Core + UnityAssetTree/UnityPackageWindow +
GeneratedAssetFiles/ImportPlan/ImportWorkspace/UnityArchive), new `UnityPrefabExport.cs`,
`Assets/Imported/` untracked. Not from this task: `minimal.scene` deletion, `unityimporterplus.sbproj` change.
Backups, harness, review reports: `~/.hermes/profiles/hermes-sbox/cache/scratch/iup/`.


---

# Phase 3 (2026-09-30): skinned characters

Scope: the 18 `PolygonCity/Prefabs/Characters/**` prefabs and the two `Characters.fbx` files they
use. Decisions from the user: outfits as **bodygroups**, import **both** FBX variants, animation
deferred to a later step (Citizen anim graph).

## Ground truth (parsed from the binary FBX and the prefab YAML)

| | `Models/Characters.fbx` | `Models/FixedScaleCharacters/Characters.fbx` |
|---|---|---|
| Skeleton | 49 LimbNodes, Root → Hips → …, identical hierarchy | same |
| Skinned meshes | 9, one Skin deformer each, 49 clusters, max 2 influences/vertex | same |
| Blend shapes | none | none |
| Material / texture | `lambert14` / one atlas | same |
| Header `UnitScaleFactor` | 100 (metres) | 1 (centimetres) |
| Vertex data | 0..188 units tall (centimetre numbers) | same |
| 0.01 correction | `Lcl Scaling` 0.01 on the Root bone **and** on the 9 mesh nodes, inside the FBX | none; all scales 1 |
| Animation | one take, 490 curves, all constant (1–2 keys) — nothing moves | same |

Both import to 1.88 m in Unity. The earlier note that "the plain file is in centimetres" was
imprecise: its header says metres and the geometry is centimetre-sized; Synty corrected that with
the 0.01 node scales, which the Unity prefab merely mirrors (Root and outfit objects at 0.01).

Prefabs: 59 objects each = root (Animator) + 9 outfit objects (one `SkinnedMeshRenderer` each,
one active) + 49 bone objects. No colliders, no scripts. The Animator controller
`3ed6a4ec…` is not in the package (FixedScale prefabs have no controller).
`FixedScaleCharacters/Character_Male_Jacket.prefab` has all 9 outfits inactive (Synty bug).

## What changed (Editor/Core)

- `UnityFbxBindings`/`UnityModel`: record Geometry, Skin/BlendShape deformers and LimbNodes
  (binary and ASCII); `SkinnedMeshes`, `BoneCount`, `HasBlendShapes`, `HasAnimation` (decodes
  `KeyValueFloat` arrays, zlib included, and looks for any curve that changes).
- `UnityPrefabBindings`: per prefab, which skinned meshes are shown (active hierarchy + enabled).
- `UnityModel.BodyGroups`: one group named after the FBX with a choice per mesh + `none` when
  every prefab shows ≤ 1 mesh; otherwise per-mesh on/off groups. `BodyGroupMask()` = one bit per
  choice, groups in order (the Citizen's 5×2 groups with first choices = 341 in the engine's own
  `avatar.scene`, which pins the layout).
- `UnityImport`: skinned FBX is never split per mesh; `.vmdl` gets a `BodyGroupList` between
  `MaterialGroupList` and `ModelModifierList`; information lines for skeleton/bodygroups/animation.
- `UnityPrefabExport`: `SkinnedMeshRenderer`s of one skeleton → one `Sandbox.SkinnedModelRenderer`
  (property set copied from the engine's `avatar.scene`) on the parent of the root bone, mask from
  the active meshes; outfit-only objects and pure bone objects dropped; Animator → INFO.
- `ImportPlan`: label **Skinned model → VMDL**.

## Verification (headless, harness on `Assets/city--1.unitypackage`)

- Core builds: 0 warnings, 0 errors. Two runs byte-identical. Only the 20 character files differ
  from the pre-change run (2 `.vmdl`, 18 `.prefab`); `verify_output.py` reports 0 problems.
- Masks: Police 1, Shirt 2, Suit 4, Hoodie 8, Male_Jacket 16, Female_Coat 32, BusinessWoman 64,
  Female_Police 128, Female_Jacket 256, FixedScale Male_Jacket 512 (`none`).
- Detection: 330 FBX in the package, exactly the 2 character files are skinned; no animation or
  blend shapes reported anywhere.

## Left for the editor (cannot be judged headless)

1. Size: plain variant relies on ModelDoc honouring the Root bone's 0.01 scale
   (`import_scale` 1.0 × 39.37); FixedScale uses `import_scale` 0.01. Both should be ~74 inches.
2. Bone orientation (`flip_bone_forward`) and skinning correctness on one character.
3. Bodygroup mask numbering: confirm one prefab shows the outfit its name says.
4. Animation: none yet; planned as the Citizen anim graph step.

## Editor feedback (2026-10-01) and fix

ModelDoc reported `Unknown mesh 'Character_…' in bodygroup` for all nine choices,
`'unnamed_1' not in any bodygroups`, and `Found multiple bones using the name: Finger_01 …
renamed duplicates to Finger_01_duplicate`; the prefabs rendered nothing and the Characters
dropdown had ten blank entries. Cause: ModelDoc names a render mesh after the FBX **Geometry**
object, not the mesh node. Maya writes every Geometry as `Geometry::` (empty name), so ModelDoc
merged all nine outfits into one `unnamed_1` mesh, which no bodygroup choice named.

Fix: `UnityFbxRewrite` writes a derived binary FBX (`characters_iup_skinned.fbx`, via the usual
`GeneratedAssetFiles` naming) in which each Geometry carries its mesh node's name and the 22
duplicate finger/thumb bones get `_L`/`_R` from their nearest sided ancestor. Everything else is
copied byte for byte; offsets and the footer (file id, pad to 16 with a full 16 when aligned,
4 zero bytes, version, 120 zero bytes, magic) are recomputed. The skinned `.vmdl` now points at
the copy; the original FBX is still imported beside it. Verified: parse-and-write without renames
reproduces all 332 binary FBX files in `city--1` and `building` byte for byte; the rewritten files
parse with an independent Python reader and keep hierarchy, skins and clusters intact.

Still to confirm in the editor: the nine warnings gone, the Characters dropdown listing the
outfit names, one outfit visible per prefab, size, and bone orientation.

## Second round (2026-10-01): the choices stayed empty

After the FBX rewrite the ModelDoc warnings were gone but the Characters dropdown still had ten
blank entries, the compiled outliner still showed `Characters > Empty` ten times, and the prefabs
rendered nothing. The Citizen's `citizen_rendermeshlist.vmdl_prefab` shows why: it imports each
mesh through its own `RenderMeshFile` with a `name` ("Torso_LOD0") and an `import_filter` on the
FBX node, and the bodygroup choices name those **node** names. The compiled mesh is named after
the `RenderMeshFile`, not after anything in the FBX, so our one unnamed node importing all nine
outfits was one `unnamed_1` mesh whatever the geometries were called.

Fix (`UnityImport.RenderMeshFile`): a model with bodygroups writes one named `RenderMeshFile` per
rendered mesh, filtered to that node; static models keep the single node (their output is
byte-identical). The derived `*_iup_skinned.fbx` stays: its bone renames are wanted for the later
Citizen anim-graph step, and named geometries keep ModelDoc's mesh list readable.
