## Task
Make URP Lit PBR values, packed maps, emission intensity and texture normalisation faithful, and prove the emitted complex .vmat keys conform to the installed engine schema.

### Description
Grounding: Editor/Core/UnityMaterial.cs reads `_Metallic` (:300 -> g_flMetalness), inverts `_Smoothness` (:315 -> g_flRoughnessScaleFactor :316), reads `_BumpScale` (:308), and extracts packed maps (`_MetallicGlossMap`, `_OcclusionMap`, `_MaskMap`, `_ParallaxMap`) into channel images at :177-197, :265-266, :276, :299, :317, :432. Emission tint/intensity is HDRP-gated at :324-325. The complex .vmat key set is written at :267-374 against `shaders/complex.shader` (:361). Engine-authored complex material `game/addons/content/Assets/models/sbox/warehouse/woodencrates/materials/woodencrates.vmat:5-38` and alpha-test material `.../street/chain_link_fence_panel_01/materials/fence_01_chainlink.vmat:5,11,47-49` are the conformance oracle. VERIFIED: 27 engine-authored `.vmat` set `F_SELF_ILLUM` (`rg -l 'F_SELF_ILLUM' game --glob '*.vmat' | wc -l` -> 27, e.g. `game/core/materials/editor/air_node.vmat:4`), and `game/core/shaders/glass.shader:121,183,217` reads `g_flRefractionStrength`. These two keys may be relied on.

SyntyForest has 67 .tga files, 73 textureType: 1 importer entries, 89 materials with `_MetallicGlossMap` / `_OcclusionMap` / `_ParallaxMap`, 196 with `_EmissionColor`, 195 with `_BumpScale`. The importer's known TGA trap applies: s&box rejects 16-bpp 5:5:5:1 TGA; normalise to 32-bpp at the staging seam in UnityImport.PrepareFiles keeping the SAME path. Verify a binary rewrite by comparing EVERY channel including alpha pixel-for-pixel against a Pillow reference decode, not by header validity plus RGB.

Work:
1. For `Pipeline == Urp`, map URP Lit metal/smooth/occlusion/parallax packed channels and the URP emission tint/intensity to the complex keys, with developer-tweakable properties for every NEW MAPPING.
2. Normalise unsupported TGA variants at the staging seam; keep the same path so no .vmat/Resolve change is needed.
3. Diff the emitted complex key set against the two engine-authored .vmat artifacts named above; fix any key the engine does not deserialise. An objective key/schema defect is headless-checkable and must not be deferred.
4. Gate on Urp; Standard and HDRP output byte-identical in the same mode.

### Acceptance (one explicit pass/fail check per line)
1. For a chosen material with `_MetallicGlossMap`, the generated .vmat metalness/roughness keys match the channel extraction re-derived from the source texture; report the exact channel values.
2. For any TGA rewritten, alpha extrema and per-pixel alpha mismatch count against Pillow are 0; an opaque albedo becoming fully transparent fails the gate.
3. For a chosen material with `_EmissionColor` and `_EmissionMap`, g_vSelfIllumTint and g_flSelfIllumBrightness are non-default and traceable to source values.
4. Every emitted complex .vmat key appears in the engine-authored complex material or is explicitly justified by engine shader source; list any key that does not.
5. building and city--1 output byte-identical to a HEAD baseline in the same mode; per-code Unresolved breakdown unchanged; no new missing-texture growth.
6. Harness compile gate 0 errors; verify_output.py `checked N files, 0 problems`.

### Work Review


## Clarification Q&A
An (Director 2026-11-26 03:57, 1A/3A/5A): "Unity 6.7 URP" is the session label for the on-disk 6000.3.29f1 / URP 17.3.0 fixture; target the URP 17.x surface, not one editor build. Visual fidelity only. No new tooling or fixture package.

## Relevant History
See eliots-agentics/tasks/urp-support-research.md sections 2-4 for the verified census and engine-authored artifact citations. HEAD bfc189b; session-start harness compile gate green.

## Notes
Standing constraints: gate every URP rule on `Pipeline == Urp`; keep Editor/Core/*.cs BOM-less and LF; route new paths through UnityImport.DiskPath; never change import_scale or the single 39.37 modifier to compensate for each other; expose any NEW MAPPING as a developer-tweakable property. Editor rounds follow the 4A conditions: before any editor round, check that no Director-owned editor is already open on that project; never close an editor this work did not launch; NTFY before each large round.

## Stylistic Task Notes:
References to variables like X and Y automatically imply that these are to be exposed properties for developer tweaking.
When programming, portability and DRY are a priority.

## Post-Task Follow Ups
