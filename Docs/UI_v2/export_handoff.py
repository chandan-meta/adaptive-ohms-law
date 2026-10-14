import base64, csv, json
from pathlib import Path

folder=Path(__file__).parent
bundle=json.loads((folder/'handoff-export-bundle.json').read_text(encoding='utf-8-sig'))
out=folder/'Assets'
out.mkdir(exist_ok=True)
for name,svg in bundle['exported']['icons'].items():
    (out/(name.split('/')[-1]+'.svg')).write_text(svg,encoding='utf-8')
(out/'PanelSurface.svg').write_text(bundle['panel']['surface']['svg'],encoding='utf-8')
(out/'PanelSurface.png').write_bytes(base64.b64decode(bundle['panel']['surface']['png']))
(folder/'design-tokens.json').write_text(json.dumps(bundle['exported'],indent=2,ensure_ascii=False),encoding='utf-8')
(folder/'figma-state.json').write_text(json.dumps(bundle['state'],indent=2,ensure_ascii=False),encoding='utf-8')
with (folder/'migration-inventory.csv').open(encoding='utf-8-sig') as f:
    rows=list(csv.DictReader(f))
for row in rows:
    if 'Diagnostic' in row['New Figma component']:
        row['Migration status']='Audited diagnostic/sample exception; retain pending purpose review'
    else:
        row['Migration status']='Figma designed; awaiting approval; Unity migration not started'
with (folder/'migration-inventory.csv').open('w',newline='',encoding='utf-8-sig') as f:
    writer=csv.DictWriter(f,fieldnames=list(rows[0]));writer.writeheader();writer.writerows(rows)
assert len(bundle['exported']['icons'])==15
assert len(rows)==29
print('Exported 15 SVG icons, a 16 px nine-slice panel (SVG/PNG), design tokens, Figma ledger, and 29 inventory rows.')
