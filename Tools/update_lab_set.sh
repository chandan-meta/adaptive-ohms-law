#!/bin/bash
# After editing the lab in Blender: export the room to the project (Tools/sets/lab.json: the centre table becomes the bench,
# the apparatus stays code-built), wipe the board chalk for the scenes before the law (Tools/blank_board.py), rebuild
# Scenes 8-13 around it and re-apply their polish. Needs Blender 5.1 and the Ohm's Law editor open (editor closed: the batch
# route in BlockoutCommands.Batch). Usage: bash Tools/update_lab_set.sh [path/to/lab.blend]
# The default is the user's physics lab, kept on E: (the project drive is full).
ROOT="$(cd "$(dirname "$0")/.." && pwd)"; cd "$ROOT" || exit 1
BLEND="${1:-/e/kjgeuifqdgvjhwai/lab.blend}"
BLENDER="${BLENDER:-/c/Program Files/Blender Foundation/Blender 5.1/blender.exe}"
"$BLENDER" -b "$(cygpath -w "$(realpath "$BLEND")")" --python "$(cygpath -w "$ROOT/Tools/blend_to_unity_set.py")" -- \
  "$(cygpath -w "$ROOT/Assets/OhmsLaw/Shared/Art/LabSet")" "$(cygpath -w "$ROOT/Tools/sets/lab.json")" < /dev/null 2>&1 | grep -E "^SET|^SKIP|^GLOW|Error|Traceback" || exit 1
python -I Tools/blank_board.py Assets/OhmsLaw/Shared/Art/LabSet LabSet.json black_board1 || exit 1
for r in Scene08 Scene09 Scene10 Scene11 Scene12 Scene13 "menu Ohm's Law/Polish/Apply lab (Scenes 8-13)"; do
  echo "== $r"; bash Tools/unity_request.sh "$r" | grep -E "^OK|^ERROR|lab set|built:"
done
