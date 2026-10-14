"""Check the rendered corrective pass and exported transparent decoration assets."""
import json
from pathlib import Path
from PIL import Image

folder=Path(__file__).parent
state=json.loads((folder/'polish-state.json').read_text(encoding='utf-8-sig'))
assert state['contentPreservation']['preserved']
assert state['contentPreservation']['beforeImageHash']==state['contentPreservation']['afterImageHash']
for audit in state['screenAudits']:
    assert not audit['outside'],audit['outside']
    assert not audit['overlaps'],audit['overlaps']
    assert not audit['textBlur'],audit['textBlur']
def linear(c):
    return c/12.92 if c<=.04045 else ((c+.055)/1.055)**2.4
def lum(p):
    return sum(linear(c/255)*w for c,w in zip(p[:3],[.2126,.7152,.0722]))
image=Image.open(folder/'polish-section-0.png')
contrast=[]
for state_name,y in [('Default',173),('Hover',253),('Pressed',333),('Focus',413),('Selected',493),('Loading',653)]:
    # The left inset is clear of glyphs and shares the vertical gradient under the label.
    ratios=[1.05/(lum(image.getpixel((195,y+offset)))+.05) for offset in range(14,42)]
    minimum=min(ratios)
    assert minimum>=4.5,(state_name,minimum)
    contrast.append({'state':state_name,'minimumRatio':round(minimum,3),'sampleRange':[14,41],'foreground':'#FFFFFF'})
alpha=[]
for name,asset in state['fxAssets']['assets'].items():
    im=Image.open(folder/'FX'/f'{name}.png')
    assert im.mode=='RGBA',(name,im.mode)
    assert im.size==(asset['width'],asset['height']),(name,im.size)
    if asset['padding']:
        channel=im.getchannel('A')
        edge=list(channel.crop((0,0,im.width,1)).getdata())+list(channel.crop((0,im.height-1,im.width,im.height)).getdata())+list(channel.crop((0,0,1,im.height)).getdata())+list(channel.crop((im.width-1,0,im.width,im.height)).getdata())
        assert max(edge)==0,(name,max(edge))
        assert channel.getextrema()[1]>0,(name,'empty halo')
        alpha.append({'asset':name,'padding':asset['padding'],'outerEdgeAlpha':max(edge),'size':im.size})
report={'textAndValuesPreserved':True,'sameGameplayAsset':True,'screenBoundsAndTextBlur':'PASS','primaryLabelContrast':contrast,'transparentHaloPadding':alpha,
    'method':'Rendered PNG checks at native sizes; no simulated Unity runtime result.',
    'limits':'Contrast samples cover primary-button label gradient bands. Full assistive-technology, Unity/player and physical-device validation remain pending.'}
(folder/'polish-validation.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
print(json.dumps({'content':'PASS','screenBounds':'PASS','primaryContrastMinimum':min(r['minimumRatio'] for r in contrast),'haloAssetsWithClearPadding':len(alpha)}))
