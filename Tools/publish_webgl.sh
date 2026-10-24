#!/bin/bash
# Publishes Builds/WebGL (Ohm's Law/Build WebGL) to the gh-pages branch, which GitHub Pages serves from its root: one
# commit holding only the web build and a .nojekyll (no Jekyll pass), made with a temporary index so the working
# tree and main stay untouched. Advances the deployment branch without rewriting its history. Refuses files
# near GitHub's 100 MB push limit. Usage: bash Tools/publish_webgl.sh
ROOT="$(cd "$(dirname "$0")/.." && pwd)"; cd "$ROOT" || exit 1
set -euo pipefail
SITE=Builds/WebGL
[ -f "$SITE/index.html" ] || { echo "no build in $SITE (run Ohm's Law/Build WebGL)"; exit 1; }
big=$(find "$SITE" -type f -size +95M)
[ -z "$big" ] || { echo "over GitHub's 100 MB file limit: $big"; exit 1; }
touch "$SITE/.nojekyll"
# The git directory as git itself names it: Git for Windows cannot use a /d/... path from this shell.
index_path="$(mktemp "$(git rev-parse --absolute-git-dir)/gh-pages.index.XXXXXX")"
export GIT_INDEX_FILE="$index_path"
trap 'rm -f "$index_path"' EXIT
rm -f "$index_path"
git --work-tree="$SITE" add -A -f . || exit 1
tree=$(git write-tree) || exit 1
parent=()
if [ -n "$(git ls-remote --heads origin gh-pages)" ]; then
  git fetch --no-tags origin gh-pages
  parent=(-p "$(git rev-parse FETCH_HEAD)")
fi
commit=$(printf 'WebGL build %s\n' "$(date +%Y-%m-%d)" | git commit-tree "$tree" "${parent[@]}") || exit 1
rm -f "$index_path"
unset GIT_INDEX_FILE
# HTTP/1.1 and a large post buffer: a 100 MB pack over HTTP/2 timed out (HTTP 408) once.
git -c http.version=HTTP/1.1 -c http.postBuffer=524288000 push origin "$commit:refs/heads/gh-pages"
printf 'Published commit %s\n' "$commit"
