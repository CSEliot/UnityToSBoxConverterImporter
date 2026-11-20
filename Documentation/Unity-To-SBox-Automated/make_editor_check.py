#!/usr/bin/env python3
# Make an offline compile check for the unityimporterplus EDITOR code (UI files that need the
# engine). The project's own Editor/*.csproj is written by the s&box editor with that machine's
# install paths (e.g. S:/steamapps/common/sbox under Proton), so it does not build as-is.
# This writes <out dir>/editor-check.csproj with those paths pointed at a real s&box game folder.
# usage: make_editor_check.py <unityimporterplus dir> <s&box game dir> <out dir>
#   s&box game dir = the folder holding bin/managed/Sandbox.Engine.dll, e.g.
#   ~/1-Development/1-SBox/1-Engine-Builds/Dev/sbox-public/game  or the Steam sbox install.
import os, re, sys

project, game, out = (os.path.abspath(os.path.expanduser(a)) for a in sys.argv[1:4])
src = os.path.join(project, "Editor", "unityimporterplus.editor.csproj")
text = open(src, encoding="utf-8-sig").read()
if not os.path.isfile(os.path.join(game, "bin", "managed", "Sandbox.Engine.dll")):
    sys.exit(f"{game} has no bin/managed/Sandbox.Engine.dll")
# Every engine path in the file starts with the install root the editor saw; swap it wholesale.
roots = set(re.findall(r'Include="([^"]*?)/bin/managed/', text))
if len(roots) != 1: sys.exit(f"expected one engine root in {src}, found {roots}")
root = roots.pop()
text = text.replace(root + "/", game.replace(os.sep, "/") + "/")
text = re.sub(r"<OutputPath>.*?</OutputPath>", f"<OutputPath>{out}/bin/</OutputPath>", text)
text = re.sub(r"<DocumentationFile>.*?</DocumentationFile>", "", text)
# The game-side Code project is not needed to compile Editor/ and its csproj has the same problem.
text = re.sub(r'\s*<ProjectReference Include="\.\./Code/[^"]*"\s*/>', "", text)
editor = os.path.join(project, "Editor").replace(os.sep, "/")
text = text.replace("</Project>",
    "\t<PropertyGroup><EnableDefaultCompileItems>false</EnableDefaultCompileItems>"
    "<GenerateAssemblyInfo>false</GenerateAssemblyInfo><GenerateTargetFrameworkAttribute>false</GenerateTargetFrameworkAttribute>"
    f"<BaseIntermediateOutputPath>{out}/obj/</BaseIntermediateOutputPath></PropertyGroup>\n"
    f'\t<ItemGroup><Compile Include="{editor}/**/*.cs" Exclude="{editor}/obj/**" /></ItemGroup>\n</Project>')
os.makedirs(out, exist_ok=True)
open(os.path.join(out, "editor-check.csproj"), "w", encoding="utf-8").write(text)
print(f"wrote {out}/editor-check.csproj (engine root {root} -> {game})")
