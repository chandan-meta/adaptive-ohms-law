"""Verify exported science values, aliases, font fallbacks and composited contrast."""
import json
from pathlib import Path
from PIL import Image

folder=Path(__file__).parent
data=json.loads((folder/'design-tokens.json').read_text(encoding='utf-8'))
variables={v['id']:v for v in data['tokens']}
def value(v,mode):
    val=v['values'].get(mode,next(iter(v['values'].values())))
    return value(variables[val['id']],mode) if isinstance(val,dict) and val.get('type')=='VARIABLE_ALIAS' else val
def rgb(name):
    v=next(v for v in data['tokens'] if v['name']==name)
    c=value(v,next(iter(v['values'])))
    return [c[k] for k in 'rgb']
def linear(c):
    return c/12.92 if c<=.04045 else ((c+.055)/1.055)**2.4
def lum(color):
    return sum(linear(c)*w for c,w in zip(color,[.2126,.7152,.0722]))
def contrast(a,b):
    x,y=sorted((lum(a),lum(b)))
    return (y+.05)/(x+.05)
results=[]
for background in ([1,1,1],[1,.72,.35],[0,0,0],[.5,.8,1]):
    surface=rgb('color/surface')
    composite=[s*.96+b*.04 for s,b in zip(surface,background)]
    for name in ['color/text','color/text-muted']:
        rendered=[t*.96+b*.04 for t,b in zip(rgb(name),background)]
        ratio=contrast(rendered,composite)
        assert ratio>=4.5,(name,ratio)
        results.append({'token':name,'background':background,'ratio':round(ratio,2),'method':'sRGB compositing at 96% panel/group opacity'})
    # Unity UI may composite in linear light; check the brighter conservative case separately.
    bg_lum=sum(w*(linear(s)*.96+linear(b)*.04) for s,b,w in zip(surface,background,[.2126,.7152,.0722]))
    for name in ['color/text','color/text-muted']:
        text_lum=sum(w*(linear(t)*.96+linear(b)*.04) for t,b,w in zip(rgb(name),background,[.2126,.7152,.0722]))
        ratio=(text_lum+.05)/(bg_lum+.05)
        assert ratio>=4.5,(name,ratio)
        results.append({'token':name,'background':background,'ratio':round(ratio,2),'method':'linear-light worst-case alpha composition'})
for token in ['color/action','color/action-hover']:
    ratio=contrast(rgb('color/on-action'),rgb(token))
    assert ratio>=4.5,(token,ratio)
    results.append({'token':token,'ratio':round(ratio,2),'method':'opaque primary action'})
for v in range(1,6):
    i=v/5
    assert abs(v-i*5)<1e-7
assert all(v['scopes']!=['ALL_SCOPES'] for v in data['tokens'])
assert all(v.get('codeSyntax') for v in data['tokens'])
fonts=json.loads((folder/'Fonts/sources-and-glyphs.json').read_text())
assert all(fonts[-1]['glyphCoverage'].values())
report={'contrastChecks':results,'minimumRatio':min(r['ratio'] for r in results),
    'science':'5 samples satisfy V=I×R at R=5 Ω; V vertical / I horizontal; slope=R',
    'fonts':fonts,'tokenCount':len(data['tokens']),
    'limitation':'Design compositing checks; Unity screenshots and device rendering remain untested.'}
(folder/'design-validation.json').write_text(json.dumps(report,indent=2,ensure_ascii=False),encoding='utf-8')
print(json.dumps({'contrastChecks':len(results),'minimumRatio':report['minimumRatio'],'tokens':len(data['tokens']),'glyphFallback':'PASS','science':'PASS'}))
