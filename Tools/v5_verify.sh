#!/bin/bash
# The v5 verification run against the OPEN editor: compile, rebuild Scene 4 and Scenes 5-13, re-apply the wire polish,
# bake the voice, run the self-checks, then the hands-free play test and the contact sheets.
# Usage: bash Tools/v5_verify.sh [steps...]   steps: compile build04 build vo apply tests play sheets (default: all, in this order)
#        PLAY="all 2x hud ask" bash Tools/v5_verify.sh play sheets
ROOT="$(cd "$(dirname "$0")/.." && pwd)"; cd "$ROOT" || exit 1
STEPS="${*:-compile build04 build vo apply tests play sheets}"  # vo rebuilds the scenes it re-times, so the polish goes after it
PLAY="${PLAY:-all 2x}"
SHEETS="${SHEETS:-Logs/Blockout/sheets}"
# Every step but "compile" and "sheets" drives the open editor: stop at once when it is not running.
if [ "$STEPS" != "compile" ] && ! powershell -NoProfile -Command "if (Get-Process Unity -ErrorAction SilentlyContinue | Where-Object { \$_.MainWindowTitle -like '*OHM*' }) { exit 0 } else { exit 1 }"; then
  echo "The Ohm's Law editor is not open: open the project in Unity 6000.3, wait for it to load, then run this again."; exit 1
fi
req() { echo "== $1"; bash Tools/unity_request.sh "$1" | grep -v "^$" | head -${2:-40}; }
for step in $STEPS; do
  case $step in
    compile) echo "== compile"; bash Tools/compile_check.sh v5 ;;
    build04) req "menu Ohm's Law/Build Scene 04" 20 ;;
    build)   req "all" 200 ;;
    apply)   req "menu Ohm's Law/Polish/Apply later scenes" 20 ;;
    vo)      req "vo all" 80 ;;
    tests)   for t in "Ohm Pose" "Wattson Pose" "Wattson Actions" "Lab Continuity" "Voice Pipeline" "Wire Electrons" "HUD v5" "Wire Sample"; do out=$(bash Tools/unity_request.sh "menu Ohm's Law/Tests/$t"); echo "== test $t: $(echo "$out" | grep -c "^PASS") pass, $(echo "$out" | grep -cE "^FAIL|ERROR|Exception") fail"; echo "$out" | grep -E "^FAIL|ERROR|Exception" | head -10; done ;;
    play)
      echo "== play test: $PLAY"
      rm -f Logs/Blockout/play-report.txt; rm -rf Logs/Blockout/shots
      echo "$PLAY" > Logs/ohm-play.req
      for i in $(seq 1 1000); do grep -q "^RESULT" Logs/Blockout/play-report.txt 2>/dev/null && break; sleep 5; done
      grep -E "^(RESULT|RES |FAIL|ERROR|STALL|GATE|===|END|PASS  module)" Logs/Blockout/play-report.txt | tail -60 ;;
    sheets)  python Tools/play_sheets.py "$SHEETS" ;;
  esac
done
