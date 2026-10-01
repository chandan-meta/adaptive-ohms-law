#!/bin/bash
# Fast C# compile check of Assets/**/*.cs outside Unity (same references and C# 9 as Unity's csproj).
# Usage: bash Tools/compile_check.sh [name]   (a name keeps parallel runs in separate build folders)
NAME="${1:-default}"
OUT="$(cygpath -w "$LOCALAPPDATA/Temp/ohm_cc/$NAME" 2>/dev/null || echo "$LOCALAPPDATA/Temp/ohm_cc/$NAME")"
ROOT="$(cygpath -w "$(cd "$(dirname "$0")/.." && pwd)" 2>/dev/null || (cd "$(dirname "$0")/.." && pwd))"
python "$ROOT/Tools/gen_csproj.py" "$OUT" < /dev/null || exit 1
for p in Runtime Editor; do
  dotnet build "$OUT/$p.csproj" -nologo -v:q -clp:NoSummary -p:WarningLevel=0 2>&1 | grep -E "error|Build FAILED" | sed 's/.*Assets/Assets/' | sort -u | head -40
done
echo "compile check done ($NAME)"
