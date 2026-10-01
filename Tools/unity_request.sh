#!/bin/bash
# Sends a request to the OPEN Unity editor (Logs/ohm-build.req) after making sure it has recompiled the latest
# scripts: toggles window focus so the editor refreshes, waits for Logs/ohm-reload.txt to be newer than every
# script, then writes the request and waits for Logs/ohm-build.txt. Usage: bash Tools/unity_request.sh "Scene05"
REQ="$1"; ROOT="$(cd "$(dirname "$0")/.." && pwd)"; cd "$ROOT" || exit 1
newest=$(find Assets -name "*.cs" -newer Logs/ohm-reload.txt 2>/dev/null | head -1)
if [ -n "$newest" ]; then
  powershell -NoProfile -Command "Add-Type -TypeDefinition 'using System; using System.Runtime.InteropServices; public class W { [DllImport(\"user32.dll\")] public static extern bool SetForegroundWindow(IntPtr h); [DllImport(\"user32.dll\")] public static extern IntPtr GetDesktopWindow(); }'; \$u = Get-Process Unity | Where-Object { \$_.MainWindowTitle -like '*OHM*' } | Select-Object -First 1; \$o = Get-Process | Where-Object { \$_.MainWindowTitle -ne '' -and \$_.Id -ne \$u.Id } | Select-Object -First 1; if (\$o) { [W]::SetForegroundWindow(\$o.MainWindowHandle) | Out-Null; Start-Sleep -Milliseconds 700 }; [W]::SetForegroundWindow(\$u.MainWindowHandle) | Out-Null" 
  for i in $(seq 1 60); do
    newest=$(find Assets -name "*.cs" -newer Logs/ohm-reload.txt 2>/dev/null | head -1)
    [ -z "$newest" ] && break
    sleep 2
  done
  [ -n "$newest" ] && echo "warning: editor did not reload (still older than $newest)"
fi
rm -f Logs/ohm-build.txt
echo "$REQ" > Logs/ohm-build.req
for i in $(seq 1 400); do [ -f Logs/ohm-build.txt ] && break; sleep 3; done
cat Logs/ohm-build.txt 2>/dev/null || echo "no result after timeout"
