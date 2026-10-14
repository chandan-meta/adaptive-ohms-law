#!/bin/bash
# After editing Exports/InsideTheWire/InsideTheWire.blend: export the set to the project, rebuild Scenes 5 and 6 around it
# and re-apply their polish. Needs Blender 5.1 and the Ohm's Law editor open.
# Usage: bash Tools/update_wire_set.sh [path/to/InsideTheWire.blend]
ROOT="$(cd "$(dirname "$0")/.." && pwd)"; cd "$ROOT" || exit 1
BLEND="${1:-Exports/InsideTheWire/InsideTheWire.blend}"
BLENDER="${BLENDER:-/c/Program Files/Blender Foundation/Blender 5.1/blender.exe}"
"$BLENDER" -b "$(cygpath -w "$(realpath "$BLEND")")" --python "$(cygpath -w "$ROOT/Tools/blend_to_unity_set.py")" -- \
  "$(cygpath -w "$ROOT/Assets/OhmsLaw/Shared/Art/WireSet")" < /dev/null 2>&1 | grep -E "^SET|Error|Traceback" || exit 1
for r in "Scene05" "Scene06" "menu Ohm's Law/Polish/Apply wire chamber (Scenes 5, 6)"; do
  echo "== $r"; bash Tools/unity_request.sh "$r" | grep -E "^OK|^ERROR|wire set|built:"
done
