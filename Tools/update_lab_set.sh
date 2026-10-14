#!/bin/bash
# After editing Exports/OurLab/OurLab.blend: export the room to the project (Tools/sets/lab.json: the bench and apparatus
# stay code-built), rebuild Scenes 8-13 around it and re-apply their polish. Needs Blender 5.1 and the Ohm's Law editor open.
# Usage: bash Tools/update_lab_set.sh [path/to/OurLab.blend]
ROOT="$(cd "$(dirname "$0")/.." && pwd)"; cd "$ROOT" || exit 1
BLEND="${1:-Exports/OurLab/OurLab.blend}"
BLENDER="${BLENDER:-/c/Program Files/Blender Foundation/Blender 5.1/blender.exe}"
"$BLENDER" -b "$(cygpath -w "$(realpath "$BLEND")")" --python "$(cygpath -w "$ROOT/Tools/blend_to_unity_set.py")" -- \
  "$(cygpath -w "$ROOT/Assets/OhmsLaw/Shared/Art/LabSet")" "$(cygpath -w "$ROOT/Tools/sets/lab.json")" < /dev/null 2>&1 | grep -E "^SET|^SKIP|^GLOW|Error|Traceback" || exit 1
for r in Scene08 Scene09 Scene10 Scene11 Scene12 Scene13 "menu Ohm's Law/Polish/Apply lab (Scenes 8-13)"; do
  echo "== $r"; bash Tools/unity_request.sh "$r" | grep -E "^OK|^ERROR|lab set|built:"
done
