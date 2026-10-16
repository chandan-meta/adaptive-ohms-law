#!/bin/bash
# Publishes Builds/WebGL (Ohm's Law/Build WebGL) to the gh-pages branch, which GitHub Pages serves from its root: one
# fresh commit holding only the web build and a .nojekyll (no Jekyll pass), made with a temporary index so the working
# tree and main stay untouched, then force-pushed (the branch is a deploy target, never edited by hand). Refuses files
# near GitHub's 100 MB push limit. Usage: bash Tools/publish_webgl.sh
ROOT="$(cd "$(dirname "$0")/.." && pwd)"; cd "$ROOT" || exit 1
SITE=Builds/WebGL
[ -f "$SITE/index.html" ] || { echo "no build in $SITE (run Ohm's Law/Build WebGL)"; exit 1; }
big=$(find "$SITE" -type f -size +95M)
[ -z "$big" ] || { echo "over GitHub's 100 MB file limit: $big"; exit 1; }
touch "$SITE/.nojekyll"
export GIT_INDEX_FILE="$ROOT/.git/gh-pages.index"
rm -f "$GIT_INDEX_FILE"
git --work-tree="$SITE" add -A -f . || exit 1
tree=$(git write-tree) || exit 1
commit=$(printf 'WebGL build %s\n\nCo-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>\n' "$(date +%Y-%m-%d)" | git commit-tree "$tree") || exit 1
rm -f "$GIT_INDEX_FILE"
unset GIT_INDEX_FILE
git update-ref refs/heads/gh-pages "$commit"
# HTTP/1.1 and a large post buffer: a 100 MB pack over HTTP/2 timed out (HTTP 408) once.
git -c http.version=HTTP/1.1 -c http.postBuffer=524288000 push -f origin gh-pages
