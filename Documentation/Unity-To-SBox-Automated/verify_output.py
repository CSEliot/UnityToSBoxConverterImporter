#!/usr/bin/env python3
# Check generated s&box output: every .prefab/.scene parses as JSON, every .vmdl/.vmat has
# balanced braces, and every resource path written inside them resolves (case-insensitively,
# as s&box does) to a file under the Assets folder.
# usage: verify_output.py <Assets dir> <file or folder> [...]
# Exits 1 on any problem. Paths that are not in the project but look like engine content
# (materials/default/, materials/skybox/, models/dev/ ...) are listed as "engine?" for review.
import json, os, re, sys

EXT = (".vmdl", ".vmat", ".prefab", ".fbx", ".obj", ".png", ".tga", ".jpg", ".jpeg", ".tif", ".tiff", ".exr", ".psd", ".vtex", ".vpcf", ".sound", ".surface")
ENGINE = ("materials/default/", "materials/skybox/", "materials/dev/", "models/dev/", "shaders/")

def index(assets):
    out = set()
    for d, _, files in os.walk(assets):
        for f in files:
            out.add(os.path.relpath(os.path.join(d, f), assets).replace(os.sep, "/").lower())
    return out

def strings(node):
    if isinstance(node, dict):
        for v in node.values(): yield from strings(v)
    elif isinstance(node, list):
        for v in node: yield from strings(v)
    elif isinstance(node, str): yield node

def main(assets, targets):
    known = index(assets)
    files = []
    for t in targets:
        if os.path.isdir(t):
            for d, _, fs in os.walk(t):
                files += [os.path.join(d, f) for f in fs if f.lower().endswith((".prefab", ".scene", ".vmdl", ".vmat"))]
        else: files.append(t)
    bad = 0
    for path in sorted(files):
        text = open(path, encoding="utf-8-sig").read()
        refs = []
        if path.lower().endswith((".prefab", ".scene")):
            try: doc = json.loads(text)
            except ValueError as e: print(f"BAD JSON  {path}: {e}"); bad += 1; continue
            refs = [s for s in strings(doc) if s.lower().endswith(EXT)]
        else:
            if text.count("{") != text.count("}") or text.count("[") != text.count("]"):
                print(f"UNBALANCED {path}"); bad += 1
            refs = [m for m in re.findall(r'"([^"]+)"', text) if m.lower().endswith(EXT)]
        for ref in sorted(set(refs)):
            r = ref.replace("\\", "/").lstrip("/").lower()
            if r in known: continue
            if r.startswith(ENGINE): print(f"engine?   {path}: {ref}"); continue
            print(f"MISSING   {path}: {ref}"); bad += 1
    print(f"checked {len(files)} files, {bad} problems")
    return 1 if bad else 0

if __name__ == "__main__":
    if len(sys.argv) < 3: sys.exit("usage: verify_output.py <Assets dir> <file or folder> [...]")
    sys.exit(main(sys.argv[1], sys.argv[2:]))
