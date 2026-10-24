#!/bin/bash
# The 2026-10-22 revision's build in batch mode, with the Ohm's Law editor closed (Docs/Polish/REVISION_2026-10-22.md):
# the audio library (the snap), Scene 4 (its runtime beds), the Scene 5-6 Timeline patch (no rebuild: the user hand-edits
# those scenes), Scenes 9-13 from their builders, their new voice lines (the bake rebuilds every scene it re-times), the
# lab polish, FLOW.md, then the self-checks. Usage: bash Tools/revision_build.sh [build|tests|all] (default all).
# Log: Logs/revision-build.log (Unity's), summary lines "OHM-BATCH OK <request>" in it.
ROOT="$(cd "$(dirname "$0")/.." && pwd)"; cd "$ROOT" || exit 1
UNITY="${UNITY:-C:/Program Files/Unity/Hub/Editor/6000.3.0f1/Editor/Unity.exe}"
WHAT="${1:-all}"
if [ -f Temp/UnityLockfile ] && powershell -NoProfile -Command "if (Get-Process Unity -ErrorAction SilentlyContinue | Where-Object { \$_.MainWindowTitle -like '*OHM*' }) { exit 0 } else { exit 1 }"; then
  echo "The Ohm's Law editor is open: close it first (batch mode needs the project to itself)."; exit 1
fi
BUILD="menu Ohm's Law/Audio/Build library|menu Ohm's Law/Build Scene 04|menu Ohm's Law/Revise/Patch Scene 5-6 Timelines (no rebuild)"
BUILD="$BUILD|Scene09|Scene10|Scene11|Scene12|Scene13"
BUILD="$BUILD|menu Ohm's Law/Bake VO/Scene 09|menu Ohm's Law/Bake VO/Scene 10|menu Ohm's Law/Bake VO/Scene 11|menu Ohm's Law/Bake VO/Scene 12|menu Ohm's Law/Bake VO/Scene 13"
BUILD="$BUILD|menu Ohm's Law/Polish/Apply lab (Scenes 8-13)|menu Ohm's Law/Generate FLOW.md"
TESTS="menu Ohm's Law/Tests/Wattson Pose|menu Ohm's Law/Tests/Ohm Pose|menu Ohm's Law/Tests/Wattson Actions|menu Ohm's Law/Tests/Lab Continuity|menu Ohm's Law/Tests/Voice Pipeline|menu Ohm's Law/Tests/HUD v5|menu Ohm's Law/Tests/Wire Sample|menu Ohm's Law/Tests/Wire Electrons"
case $WHAT in
  build) REQ="$BUILD" ;;
  tests) REQ="$TESTS" ;;
  *) REQ="$BUILD|$TESTS" ;;
esac
LOG="$ROOT/Logs/revision-build.log"
mkdir -p Logs
echo "== batch: $WHAT ($(date +%H:%M:%S))"
"$UNITY" -batchmode -quit -projectPath "$(cygpath -w "$ROOT")" -executeMethod OhmsLaw.Editor.BlockoutCommands.Batch -ohmRequests "$REQ" -logFile "$(cygpath -w "$LOG")"
code=$?
echo "== exit $code ($(date +%H:%M:%S))"
grep -E "OHM-BATCH OK|error CS|Exception|^PASS  |^FAIL|FAIL |Bake VO|edge-tts|Revision patch|built: " "$LOG" | grep -v "^UnityEngine" | head -150
exit $code
