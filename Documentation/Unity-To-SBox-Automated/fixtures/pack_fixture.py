#!/usr/bin/env python3
"""Pack a fixture folder into a .unitypackage the importer can read, reproducibly.

A Unity package is a gzipped tar of one directory per asset guid holding `pathname`, `asset` and
`asset.meta`. This writes exactly those entries in guid order with zeroed timestamps, ownership and
mode, so the tar it builds is identical wherever it runs. The gzip wrapper around that tar is not:
its bytes come from the local zlib, so two machines can disagree on the archive size while holding
the same tar. Compare the tar (the script prints its md5), not the .unitypackage, when checking that
a repack found a real change to the fixture.

usage: pack_fixture.py <fixture folder> <output .unitypackage>
The fixture folder holds an Assets/ tree; each asset's guid comes from its .meta beside it.
"""
import gzip
import hashlib
import io
import os
import re
import sys
import tarfile

GUID = re.compile(r"^guid:\s*([0-9a-fA-F]{32})\s*$", re.MULTILINE)

# Unity stores text assets with this trailer after the pathname; the importer ignores it.
PATHNAME_TRAILER = "\n00"


def add(tar, name, data):
	info = tarfile.TarInfo(name)
	info.size = len(data)
	info.mtime = 0
	info.mode = 0o644
	info.uid = info.gid = 0
	info.uname = info.gname = ""
	tar.addfile(info, io.BytesIO(data))


def build(fixture, output):
	assets = os.path.join(fixture, "Assets")
	if not os.path.isdir(assets):
		sys.exit(f"missing {assets}")
	entries = {}
	for directory, _, files in os.walk(assets):
		for name in files:
			if name.endswith(".meta"):
				continue
			path = os.path.join(directory, name)
			meta = path + ".meta"
			if not os.path.isfile(meta):
				sys.exit(f"{path} has no .meta, so it has no guid")
			guid = GUID.search(open(meta, encoding="utf-8").read())
			if not guid:
				sys.exit(f"{meta} has no guid")
			relative = os.path.relpath(path, fixture).replace(os.sep, "/")
			entries[guid.group(1).lower()] = (
				relative + PATHNAME_TRAILER,
				open(path, "rb").read(),
				open(meta, "rb").read(),
			)
	raw = io.BytesIO()
	with tarfile.open(fileobj=raw, mode="w", format=tarfile.USTAR_FORMAT) as tar:
		for guid in sorted(entries):
			pathname, asset, meta = entries[guid]
			add(tar, f"{guid}/pathname", pathname.encode("utf-8"))
			add(tar, f"{guid}/asset", asset)
			add(tar, f"{guid}/asset.meta", meta)
	buffer = io.BytesIO()
	with gzip.GzipFile(fileobj=buffer, mode="wb", mtime=0) as compressed:
		compressed.write(raw.getvalue())
	open(output, "wb").write(buffer.getvalue())
	# The tar is the reproducible part; the gzip bytes depend on the zlib build that produced them.
	print(f"{output}: {len(entries)} assets, tar {len(raw.getvalue())} bytes md5 {hashlib.md5(raw.getvalue()).hexdigest()}, archive {len(buffer.getvalue())} bytes")


if __name__ == "__main__":
	if len(sys.argv) != 3:
		sys.exit("usage: pack_fixture.py <fixture folder> <output .unitypackage>")
	build(sys.argv[1], sys.argv[2])