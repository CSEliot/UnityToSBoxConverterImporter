## Status: STARTED? YES! CLARIFIED? YES! HISTORY? YES! REVIEWED? YES! DONE? YES! WORKER: sa-0-cf69542a OTHER: reviewed 17:56 no further changes; inverted-alpha regression fixed and re-verified

## Task
Fix tga not working on sbox.

### Description
Certain tga files load fine in image editors, but error from sbox perspective. SBox states "Unknown Format!".
See: C:/cseliot/1-Development/1-SBox/2-Projects/unityimporterplus/Assets/imported/a/s/art/textures/props/classroom/poster_01/poster_01_albedo_snaps014.tga as example of one of these problematic tga files.

Independent Research:
────────────────────────────────────────
☤ Hermes
Conclusion: yes, s&box fully supports .tga files. Here's the breakdown.

What I found first (quick web research):
- Official Facepunch documentation (sbox.game/learn/facepunch/modeldoc-basics) lists .png, .jpeg & .tga as supported texture formats. Bob's custom playermodel guide also includes .tga.

What the subagent verified in the engine source:
- Dedicated TGA loader exists: engine/Sandbox.Engine/Resources/Textures/Bitmap/Bitmap.Loading.Tga.cs with CreateFromTgaBytes, IsTga, IsTgaPayloadValid. ImageLoader.cs explicitly routes .tga to FloatBitMap_t.LoadFromFile.
- Default materials use TGA: dozens of .tga files shipped under core/materials/default — default_color.tga, default_normal.tga, default_rough.tga, etc. Many .vmat files reference .tga paths directly.
- Symbols exist in shipped binaries: Sandbox.Engine.dll contains CreateFromTgaBytes and FloatBitMap_t; engine2.dll / assetsystem.dll contain FloatBitMap and TGA strings. Unit tests (TgaValidationTests.cs) exercise TGA detection/validation/loading.
- Underlying path: SkiaSharp (SKCodec) can't decode TGA, so it falls back to the engine's native FloatBitMap_t decoder (LoadFromInMemoryTGA) which writes unpremultiplied RGBA8888 pixels. Stb_image may be bundled but native source is closed so that can't be confirmed.

Caveats:
- Supported variants: type 2 (uncompressed 24/32-bit truecolor), type 3 (8-bit grayscale), type 10 (RLE 24/32-bit). Paletted/color-mapped TGA, 16-bit pixels, and >8192×8192 images are rejected.
- 32-bit TGA gives you alpha; 24-bit does not.
- TGA is an import format — compiled into .vtex with DXT/BC7 compression for materials, not a runtime GPU format.
- Hammer FGD editors rewrite 'tga' to 'jpg' in editor args, but .tga loads fine when used directly.

All supported image formats found: .png, .jpg, .jpeg, .gif, .webp, .tga, .psd, .tif, .ies, .bmp, .dds, .exr, .pfm.
────────────────────────────────────────

### Work Review

2026-11-22 17:56 - Verdict: no further changes

1. [High] CRITICAL INVERTED-ALPHA CHECK - PASS, independently re-verified from files on disk (scratch/tga-fix-run/critic-alpha-probe.py). Both real poster albedos decode FULLY OPAQUE before and after normalization: corpus-baseline-out and corpus-fixed-out, poster_01 (1024x1024) and poster_02 (2048x2048), all four decode as Pillow RGBA with alpha extrema (255,255). Per-pixel alpha is an exact match on both (ImageChops difference bbox None = 0 mismatching pixels over 1,048,576 and 4,194,304 pixels). RGB max per-channel delta (R,G,B) = (1,1,1), within the <=1 tolerance (Pillow truncates v*255/31 vs the spec's (v<<3)|(v>>2)). Source bit15 SET count is 0 on every pixel of both posters, confirming fully opaque sources; Editor/Core/UnityTgaRewrite.cs:101 inverted rule (bit15 clear -> 255, set -> 0) preserves opacity. The first implementation's naive mapping would have produced (0,0); that regression is fixed. Fix: none.

2. [High] ENGINE VALIDATOR + .vmat PATH - PASS. Re-implemented Bitmap.Loading.Tga.cs IsTga + IsTgaPayloadValid (Dev/sbox-public engine source) against the fixed poster bytes: type 2, depth 32, colormap length 0, pixel count within 8192x8192, offset+pixels*4 (4,194,322 and 16,777,234) <= file length (4,194,348 and 16,777,260); baseline 16-bpp files correctly fail on depth 16. Fixed headers are type 2 / 32 bpp / desc 0x08 with the TRUEVISION-XFILE. footer intact. TextureColor is byte-identical between baseline and fixed in both poster .vmat files ("imported/a/s/art/textures/props/classroom/poster_0X/poster_0X_albedo_snaps014.tga"); no .vmat, .vtex, Resolve, DocumentPath, NeedsTextureResource, harness or GUI change. Fix: none.

3. [Medium] GATES 1-3 AND 6 - PASS. Fresh harness build from the current on-disk sources: 0 errors / 0 warnings (matching gate1-harness-build.log; evidence postdates source mtimes). Fixture source TGA is type-2 16-bpp (raw18 ends 10 01), staged TGA is type 2 / 32-bpp / desc 0x08 and Pillow-openable; fixture .vmat TextureColor still "imported/fixture/textures/tga16.tga"; verify_output.py reports 0 problems. Fixture packed with the SHARED pack_fixture.py - an independent repack produced the identical tar md5 79c1659b2513148201d1e3849c3f557e (2 assets, 1083-byte archive), no per-fixture packer or extra tooling. All 25 Editor/Core/*.cs are BOM-less (first bytes 75 73 69), both modified sources are LF, and no stray import-report.json, bin/, obj/, __pycache__ or probe file is in the repo. Fix: none.

4. [Medium] GATE 4 REGRESSIONS - PASS with BOTH trees populated. building baseline/fixed 24/24 files and city--1 baseline/fixed 1166/1166 files; harness lines assets=15 converted=10 files=24 and assets=729 converted=776 files=1166 on both sides. Fresh diff -r --strip-trailing-cr of each pair returned rc 0 / 0 bytes. The 0-byte diffs are backed by populated trees, not empty output. Fix: none.

5. [Medium] GATES 5, 7, 8 - PASS. Two fixture runs are byte-identical (3/3 files, 0-byte diff); report JSON differs only in Destination and TextureProcessing.ElapsedMilliseconds. Report counts independently parsed from the 1.3 MB JSONs: baseline Assets 1277 / Converted 940 / Files 2119 / Unresolved 64 / Warnings 80 / Information 2771 vs fixed 2773 (exactly the two new Information lines; all other counts unchanged). Exactly 2 of 248 imported TGAs differ (the two posters); the other 2117 files are byte-identical. git status --short shows only task-scoped paths; the .humans-only/.template-task.md deletion is the Director's archive move and is not staged; nothing staged, no commit, no push. The resolve-errors-B* / fix-unresolved-material-slots.md files are a different, already-committed task and absent from scope. Fix: none.

6. [Low] CODE REVIEW - no functional defect. UnityTgaRewrite.Normalize declines every variant except uncompressed type-2 16-bpp with no colormap, dims <=8192 and a complete payload, and names declined real variants for reporting; BGRA channel order, (v<<3)|(v>>2) expansion, vertical-origin bit preservation and zeroed-offset footer all match the pinned spec. UnityImport.cs adds one .tga branch beside the existing .fbx branch without altering other copy paths. Minor non-defect: colorMapLength is read from header bytes 3-4 rather than the engine validator's bytes 5-6, but colorMapType (byte 1) is also checked, so color-mapped TGAs are still declined and no supported file is mis-handled. Fix: none.

Tree: git hash-object --path="Editor/Core/UnityTgaRewrite.cs" "Editor/Core/UnityTgaRewrite.cs" = 88928e7094758cffe6889006ec37f004bcc38388
Tree: git hash-object --path="Editor/Core/UnityImport.cs" "Editor/Core/UnityImport.cs" = 6f7058da6446c6bbffc644d75bb394634decafbf
Tree: git hash-object --path="Documentation/Unity-To-SBox-Automated/fixtures/tga-16bit/Assets/Fixture/Materials/Tga16Mat.mat" "Documentation/Unity-To-SBox-Automated/fixtures/tga-16bit/Assets/Fixture/Materials/Tga16Mat.mat" = 230c756d72a193fa994e7d8767a75ecbdab506bd
Tree: git hash-object --path="Documentation/Unity-To-SBox-Automated/fixtures/tga-16bit/Assets/Fixture/Materials/Tga16Mat.mat.meta" "Documentation/Unity-To-SBox-Automated/fixtures/tga-16bit/Assets/Fixture/Materials/Tga16Mat.mat.meta" = d57e348e8259fdabb7730f13c3b8765b9c3e11b3
Tree: git hash-object --path="Documentation/Unity-To-SBox-Automated/fixtures/tga-16bit/Assets/Fixture/Textures/Tga16.tga" "Documentation/Unity-To-SBox-Automated/fixtures/tga-16bit/Assets/Fixture/Textures/Tga16.tga" = 139bf2fd8d297c5d378442602cb9d94a240df54a
Tree: git hash-object --path="Documentation/Unity-To-SBox-Automated/fixtures/tga-16bit/Assets/Fixture/Textures/Tga16.tga.meta" "Documentation/Unity-To-SBox-Automated/fixtures/tga-16bit/Assets/Fixture/Textures/Tga16.tga.meta" = e2b0c8f416139ec4ae547ffd8e5a8f176ec83058
Tree: git hash-object --path="Documentation/Unity-To-SBox-Automated/fixtures/tga-16bit.unitypackage" "Documentation/Unity-To-SBox-Automated/fixtures/tga-16bit.unitypackage" = 8b2cc3e1b48b5ab44c11bfe2e689f12358e0c9de

## Notes

Implementation (Task Worker sa-0-cf69542a):
- Added Editor/Core/UnityTgaRewrite.cs: one engine-free entry point Normalize(byte[] data, out string reason). It parses the 18-byte TGA header and returns null (verbatim) for already-supported variants, non-TGAs and anything it cannot safely expand; it rewrites only type 2 / 16 bpp / no colormap / positive dims <= 8192 / complete payload. 5:5:5:1 is expanded to BGRA 8:8:8:8 with (v<<3)|(v>>2) per channel and the INVERTED alpha rule documented below (Pillow rawmode BGRA;15Z: bit 15 clear -> 255 opaque, set -> 0 transparent). A fresh type-2 header keeps the ID field, width/height and vertical-origin bit, sets depth 32 and descriptor (orig & 0x30)|0x08; the TRUEVISION-XFILE. footer is re-emitted with zeroed extension offsets. No Python/Pillow/SkiaSharp, no new delegate, LF, no BOM.
- Wired into Editor/Core/UnityImport.cs PrepareFiles staging seam beside the .fbx rewrite. .tga sources run through Normalize and the rewritten bytes are staged at the same path with one Information line; a declined real TGA variant is copied verbatim with an Information line naming it; every other asset keeps the original raw-copy path. No .vmat, Resolve, DocumentPath, NeedsTextureResource, harness Program.cs, channels.py, Editor/TextureChannels.cs or GUI file was touched, so the offline editor-compile gate does not apply.
- Added fixture Documentation/Unity-To-SBox-Automated/fixtures/tga-16bit/ (4x4 type-2 16-bpp Tga16.tga with a TRUEVISION-XFILE. footer, texture .meta guid 1111222233334444aaaabbbbccccdddd, Tga16Mat.mat with _MainTex referencing that guid, material .meta guid ddddccccbbbbaaaa4444333322221111) and tga-16bit.unitypackage packed with the shared pack_fixture.py: 2 assets, tar 10240 bytes, md5 79c1659b2513148201d1e3849c3f557e, archive 1083 bytes. No per-fixture packer or other tooling added.

Acceptance gates (all evidence on disk in scratch/tga-fix-run/; regenerated after the alpha correction):

Master gate finding + fix: the Master caught a real regression on the first implementation - the naive mapping (bit 15 set -> alpha 255) made both fully opaque poster albedos decode as fully transparent (alpha extrema 0,0) after normalization. Verified with a controlled 2x2 16-bpp probe, PIL/TgaImagePlugin.py:41,161-174 (Pillow decodes 16-bpp via rawmode BGRA;15Z) and the posters' source alpha extrema (255,255). Corrected UnityTgaRewrite.cs to the inverted rule (bit 15 set -> 0, clear -> 255), rewrote its comments to state the rule and why, regenerated gate2-tga-probe.txt and gate3-posters-pillow-rgb.txt, and re-ran gates 1, 2, 3, 5, 6 and 7 (plus gate 4 for evidence freshness). No commit, no push, Status header untouched.

1. PASS - harness build: 0 errors, 0 warnings (gate1-harness-build.log).
2. PASS - fixture harness with --scenes: staged TGA header type 2 / 32 bpp / 4x4 / descriptor 0x08, Pillow-openable RGBA; .vmat TextureColor still "imported/fixture/textures/tga16.tga"; verify_output.py <outRoot> <outRoot>/imported = 0 problems. Pillow cross-check: RGB max per-channel delta 1 (Pillow truncates 5->8 as v*255/31 vs the spec's (v<<3)|(v>>2)); alpha now matches the source per pixel (Pillow BGRA;15Z: bit 15 set -> 0/transparent, clear -> 255/opaque), fixture alpha extrema (0,255)/(0,255). Evidence: gate2-fixture-pack.log, gate2-fixture-harness.log, fixture-report.json, gate2-verify-output.log, gate2-tga-probe.txt, fixture-out/.
3. PASS - real corpus Testing/s.unitypackage with --scenes, baseline vs fixed: assets=1277 converted=940 files=2119, 248 TGAs in each tree; exactly the two poster albedos differ (1024x1024 and 2048x2048 now type 2 / 32 bpp / descriptor 0x08, Pillow-openable, RGB max delta 1 per channel and alpha extrema (255,255) matching the baseline source), the other 246 TGAs and all other files are byte-identical; unresolved 64 and warnings 80 unchanged, Information 2771 -> 2773 (the two new lines). Evidence: gate3-corpus-baseline.log, gate3-corpus-fixed.log, corpus-baseline-report.json, corpus-fixed-report.json, gate3-corpus-diff.txt, gate3-posters-pillow-rgb.txt, corpus-baseline-out/, corpus-fixed-out/.
4. PASS - no --scenes regressions against a fresh HEAD-code baseline mirror (24 HEAD Core .cs files, LF, same harness csproj/Program.cs/channels.py, build 0/0): building assets=15 converted=10 files=24 on both sides and city--1 assets=729 converted=776 files=1166 on both sides; diff -r --strip-trailing-cr is 0 bytes with both trees populated (24/24 and 1166/1166 files). Evidence: gate4-baseline-build.log, gate4-building-baseline.log, gate4-building-fixed.log, gate4-building-diff.txt, building-baseline-out/, building-fixed-out/, gate4-city--1-baseline.log, gate4-city--1-fixed.log, gate4-city--1-diff.txt, city--1-baseline-out/, city--1-fixed-out/.
5. PASS - two fixture runs: output trees byte-identical (3/3 files, 0-byte diff); report JSON differs only in Destination and TextureProcessing.ElapsedMilliseconds. Evidence: gate5-fixture-run2.log, gate5-fixture-diff.txt, gate5-report-compare.txt, fixture-out-2/, fixture-report-2.json.
6. PASS - no BOM in any of the 25 Editor/Core/*.cs; modified sources are LF; no new import-report.json, bin/, obj/, __pycache__ entry or scratch probe file in the repo (the tracked __pycache__ .pyc and ignored harness bin/obj pre-existed). Evidence: gate6-gate7-status.txt.
7. PASS - git status shows only the intended modification/additions plus the Director-owned .humans-only deletion; nothing staged, no commit, no push. Evidence: gate6-gate7-status.txt.
8. PASS - this Notes record and the Relevant History summary complete the task-file evidence; all eight gates are reported above and in the return.

Nothing is committed or pushed: awaiting the Review Critic's verdict. Stage exactly Editor/Core/UnityTgaRewrite.cs, Editor/Core/UnityImport.cs, fixtures/tga-16bit/ (all files), fixtures/tga-16bit.unitypackage and this task file only after "no further changes".

## Clarification Q&A

	Q1: What is the exact reason s&box says "Unknown Format!" for these TGA files?
	An: Answered by evidence, not the Director. The two poster albedos are TGA type 2 at 16 bpp
	(5:5:5:1), 1024x1024 and 2048x2048. engine/Sandbox.Engine/Resources/Textures/Bitmap/
	Bitmap.Loading.Tga.cs:70 rejects every non-type-3 image whose pixel depth is not 24 or 32;
	TgaValidationTests.cs:36 test-locks 16-bpp rejection. Verified by the Master reading the raw
	18-byte headers directly. No Director question needed.

	Q2: Does the importer corrupt the files, or are they authored this way?
	An: Answered by evidence. md5/sha-256 of both imported albedos match the Unity source project
	and the tar payload in Testing/old_school_project.unitypackage. The importer copies verbatim.
	No Director question needed.

	Q3: Should the fix be a byte-level TGA rewrite, a PNG conversion, or a .vtex sidecar?
	An: Parent decision (change if wrong): expand 16-bpp type-2 TGAs to 32-bpp type-2 in place at
	the staging seam, keeping the .tga path unchanged so no .vmat / Resolve / DocumentPath change
	is needed. Pure C#, engine-free, no Pillow/SkiaSharp dependency, no new delegate. Leave every
	other TGA variant verbatim and report it rather than faking a fix. Pinned in the spec.

## Relevant History 

	The prior console-log/FBX task (c7f922a) established that absolute author-machine texture
	paths embedded in binary FBX were the cause of "could not be made content-relative" errors;
	bad source texture headers and resource-compiler stalls were explicitly left as separate
	authoring/engine issues to report, not fake-fix. This task is the Director's follow-up to
	actually fix the TGA header case: two poster albedo TGAs in Assets/imported are valid 16-bpp
	TGAs that image editors open but s&box's TGALoader rejects with "Unknown Format!".

	Research on 2026-11-22 (three read-only children + a Master byte re-read) pinned the rejection
	rule, confirmed exactly 2 of 248 imported TGAs are 16-bpp (exactly the two in the console
	errors), traced every .tga copy site and the .vmat TextureColor reference, and confirmed Pillow
	12.2.0 is available for both python3 and py -3 on this Windows host. Scratch reports live in
	the taskmaster-nov22 scratch dir; the full census is in resolve-errors-B-research.md. Implementation landed at the staging seam in UnityImport.PrepareFiles; all eight acceptance gates passed in scratch/tga-fix-run/ and no commit or push was made.

## Stylistic Task Notes:
References to variables like X and Y automatically imply that these are to be exposed properties for developer tweaking.
When programming, portability and DRY are a priority.

## Post-Task Follow Ups 
