#!/usr/bin/env bash
# Run sbox-cli against the live Proton editor.
#
# The editor runs under Proton, so its MCP pipe only exists inside the Wine
# prefix: the Linux sbox-cli cannot see it (--list returns []). This runs the
# Windows sbox-cli.exe from the Steam install inside the same prefix instead.
#
# Usage:
#   ./proton-cli.sh --list
#   ./proton-cli.sh --connect <windows-pid> --call editor_status
#   ./proton-cli.sh --connect <windows-pid> --call screenshot '{"path":"..."}'
#
# The Windows pid comes from --list and changes every time the editor restarts.
# Never stop the editor; if several agents need it, take turns.
set -euo pipefail

export WINEPREFIX="${WINEPREFIX:-/home/cseliot/.local/share/Steam/steamapps/compatdata/2129370/pfx}"
export WINEDEBUG="${WINEDEBUG:--all}"

WINE="/home/cseliot/.local/share/Steam/steamapps/common/Proton 11.0/files/bin/wine"
EXE='Z:\home\cseliot\.local\share\Steam\steamapps\common\sbox\bin\managed\sbox-cli.exe'

exec "$WINE" "$EXE" "$@"
