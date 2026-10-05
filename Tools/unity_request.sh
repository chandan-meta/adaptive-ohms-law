#!/bin/bash
# Sends a request to the OPEN Unity editor (Logs/ohm-build.req) after making sure it has recompiled the latest scripts:
# toggles window focus (the editor only refreshes assets when it gains focus), waits for Logs/ohm-reload.txt to be newer
# than every script (retrying the toggle), then writes the request and prints Logs/ohm-build.txt.
# Usage: bash Tools/unity_request.sh "Scene05"      (also: all | vo all | vo Scene07 | report | flow | inventory | player | hierarchy <path>)
REQ="$1"; ROOT="$(cd "$(dirname "$0")/.." && pwd)"; cd "$ROOT" || exit 1
toggle() {
  powershell -NoProfile -Command "Add-Type -TypeDefinition 'using System; using System.Runtime.InteropServices; public class W { [DllImport(\"user32.dll\")] public static extern bool SetForegroundWindow(IntPtr h); }'; \$u = Get-Process Unity | Where-Object { \$_.MainWindowTitle -like '*OHM*' } | Select-Object -First 1; \$o = Get-Process | Where-Object { \$_.MainWindowTitle -ne '' -and \$_.Id -ne \$u.Id -and \$_.ProcessName -ne 'Unity' } | Select-Object -First 1; if (\$o) { [W]::SetForegroundWindow(\$o.MainWindowHandle) | Out-Null; Start-Sleep -Milliseconds 1200 }; [W]::SetForegroundWindow(\$u.MainWindowHandle) | Out-Null" >/dev/null 2>&1
}
stale() { find Assets -name "*.cs" -newer Logs/ohm-reload.txt 2>/dev/null | head -1; }
# The editor processes request files in the background, so ask it to refresh and recompile ("refresh") first; the focus
# toggle is the fallback for an editor whose request handler predates that request.
refresh() {
  rm -f Logs/ohm-build.txt; echo "refresh" > Logs/ohm-build.req
  for i in $(seq 1 40); do [ -f Logs/ohm-build.txt ] && break; sleep 3; done
}
for attempt in 1 2 3 4; do
  [ -z "$(stale)" ] && break
  if [ "$attempt" -le 2 ]; then refresh; else toggle; fi
  for i in $(seq 1 30); do [ -z "$(stale)" ] && break; sleep 3; done
done
[ -n "$(stale)" ] && echo "warning: editor did not reload (still older than $(stale))"
rm -f Logs/ohm-build.txt
echo "$REQ" > Logs/ohm-build.req
for i in $(seq 1 400); do [ -f Logs/ohm-build.txt ] && break; sleep 3; done
cat Logs/ohm-build.txt 2>/dev/null || echo "no result after timeout"
