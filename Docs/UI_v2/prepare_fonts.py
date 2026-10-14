"""Stage licensed Google Fonts and verify the actual font cmap."""
import json, urllib.request
from pathlib import Path
from fontTools.ttLib import TTFont

out=Path(__file__).parent/'Fonts'
out.mkdir(exist_ok=True)
report=[]
for family in ('inter','spacegrotesk'):
    url=f'https://api.github.com/repos/google/fonts/contents/ofl/{family}'
    req=urllib.request.Request(url,headers={'User-Agent':'OhmsLaw-UI-design'})
    entries=json.load(urllib.request.urlopen(req,timeout=30))
    selected=[e for e in entries if e['name']=='OFL.txt' or e['name'].endswith('.ttf') and 'Italic' not in e['name']]
    for entry in selected:
        target=out/(family+'-'+entry['name'])
        if not target.exists():
            target.write_bytes(urllib.request.urlopen(entry['download_url'],timeout=30).read())
        if target.suffix=='.ttf':
            cmap=TTFont(target).getBestCmap()
            glyphs={ch:ord(ch) in cmap for ch in 'Ω∝Δ→÷×−'}
            report.append({'font':target.name,'source':entry['download_url'],'glyphCoverage':glyphs})
fallback=Path(__file__).resolve().parents[2]/'Assets/OhmsLaw/Shared/UI/Fonts/DejaVuSans-Bold.ttf'
cmap=TTFont(fallback).getBestCmap()
coverage={ch:ord(ch) in cmap for ch in 'Ω∝Δ→÷×−'}
assert all(coverage.values()),coverage
report.append({'fallback':str(fallback),'glyphCoverage':coverage,'requiredFor':[{r['font']:[c for c,ok in r['glyphCoverage'].items() if not ok]} for r in report]})
(out/'sources-and-glyphs.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
print(json.dumps(report))
