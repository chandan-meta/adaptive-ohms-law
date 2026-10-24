// Part 2 — icons: one rounded-stroke family on a 24 grid (2.2 px strokes, round caps and joins), drawn for this module.
// Each is a component Kids/Icon/<Name>; instances are tinted and scaled by the components that use them.

const ICONS = {
  Home: ['<path d="M3.5 11.2 12 4l8.5 7.2"/>', '<path d="M5.8 9.6V20h4.6v-5.2h3.2V20h4.6V9.6"/>'],
  Lab: ['<path d="M9 3.5h6"/>', '<path d="M10.2 3.5v5.8L5 18.6A1.7 1.7 0 0 0 6.5 21h11a1.7 1.7 0 0 0 1.5-2.4l-5.2-9.3V3.5"/>', '<path d="M7.4 15h9.2"/>'],
  Data: ['<rect x="4" y="12" width="3.6" height="8" rx="1.2"/>', '<rect x="10.2" y="7.5" width="3.6" height="12.5" rx="1.2"/>', '<rect x="16.4" y="4" width="3.6" height="16" rx="1.2"/>'],
  Trophy: ['<path d="M8 4h8v5.5a4 4 0 0 1-8 0z"/>', '<path d="M8 6H5.2a2.6 2.6 0 0 0 3.2 4.2"/>', '<path d="M16 6h2.8a2.6 2.6 0 0 1-3.2 4.2"/>', '<path d="M12 13.5V17"/>', '<path d="M8.5 20.5h7"/>', '<path d="M9.8 17h4.4"/>'],
  Help: ['<circle cx="12" cy="12" r="9"/>', '<path d="M9.4 9.4a2.7 2.7 0 1 1 3.7 2.5c-.7.3-1.1.9-1.1 1.6v.5"/>', '<circle cx="12" cy="17.2" r="1.2" class="dot"/>'],
  Settings: ['<path d="M12 2.8l1.7 2.4 2.9-.7.7 2.9 2.4 1.7-1.3 2.9 1.3 2.9-2.4 1.7-.7 2.9-2.9-.7L12 21.2l-1.7-2.4-2.9.7-.7-2.9-2.4-1.7 1.3-2.9-1.3-2.9 2.4-1.7.7-2.9 2.9.7z"/>', '<circle cx="12" cy="12" r="3.2"/>'],
  Audio: ['<path d="M4 9.5h3.5L12 5.5v13l-4.5-4H4z"/>', '<path d="M15.5 9a4.2 4.2 0 0 1 0 6"/>', '<path d="M18.2 6.4a8 8 0 0 1 0 11.2"/>'],
  Mute: ['<path d="M4 9.5h3.5L12 5.5v13l-4.5-4H4z"/>', '<path d="M16 9.5l5 5"/>', '<path d="M21 9.5l-5 5"/>'],
  Star: ['<path d="M12 3.5l2.6 5.3 5.8.8-4.2 4.1 1 5.8-5.2-2.7-5.2 2.7 1-5.8-4.2-4.1 5.8-.8z"/>'],
  Pause: ['<path d="M9 6v12"/>', '<path d="M15 6v12"/>'],
  Play: ['<path d="M8 5.5l11 6.5-11 6.5z"/>'],
  Replay: ['<path d="M4.5 12a7.5 7.5 0 1 0 2.3-5.4"/>', '<path d="M4.5 4.5v4.2h4.2"/>'],
  Captions: ['<rect x="3" y="5.5" width="18" height="13" rx="3"/>', '<path d="M10.6 10.2a2.2 2.2 0 1 0 0 3.6"/>', '<path d="M17 10.2a2.2 2.2 0 1 0 0 3.6"/>'],
  Skip: ['<path d="M5.5 6l8 6-8 6z"/>', '<path d="M18.5 6v12"/>'],
  Bolt: ['<path d="M13.2 2.8 5.2 13.6h6.1l-1 7.6 8-10.9h-6.2z"/>'],
  Target: ['<circle cx="12" cy="12" r="8.8"/>', '<circle cx="12" cy="12" r="4.8"/>', '<circle cx="12" cy="12" r="1.4" class="dot"/>'],
  Bulb: ['<path d="M9.2 17.6h5.6"/>', '<path d="M10.2 20.6h3.6"/>', '<path d="M12 3a6 6 0 0 0-3.6 10.8c.6.5 1 1.2 1 2v1.8h5.2v-1.8c0-.8.4-1.5 1-2A6 6 0 0 0 12 3z"/>'],
  Check: ['<path d="M5 12.6l4.4 4.4L19 7.4"/>'],
  Cross: ['<path d="M6.5 6.5l11 11"/>', '<path d="M17.5 6.5l-11 11"/>'],
  Arrow: ['<path d="M5 12h14"/>', '<path d="M13 6l6 6-6 6"/>'],
  Plus: ['<path d="M12 5v14"/>', '<path d="M5 12h14"/>'],
  Minus: ['<path d="M5 12h14"/>'],
  Ammeter: ['<circle cx="12" cy="12" r="9"/>', '<path d="M8.8 16.2 12 7.8l3.2 8.4"/>', '<path d="M10 13.2h4"/>'],
  Voltmeter: ['<circle cx="12" cy="12" r="9"/>', '<path d="M8.8 7.8 12 16.2l3.2-8.4"/>'],
  Ohm: ['<circle cx="12" cy="12" r="9"/>', '<path d="M8.4 16.4h2.4v-1.5a3.6 3.6 0 1 1 2.4 0v1.5h2.4"/>'],
  Table: ['<rect x="3.5" y="4.5" width="17" height="15" rx="2.5"/>', '<path d="M3.5 9.5h17"/>', '<path d="M3.5 14.5h17"/>', '<path d="M12 4.5v15"/>'],
  Graph: ['<path d="M4 3.5V20h16.5"/>', '<path d="M7.5 16l4.5-4.5 3 2.5 5-6.5"/>'],
  Chat: ['<path d="M5 4.8h14a2.2 2.2 0 0 1 2.2 2.2v8a2.2 2.2 0 0 1-2.2 2.2h-6.6L8 20.6v-3.4H5A2.2 2.2 0 0 1 2.8 15V7A2.2 2.2 0 0 1 5 4.8z"/>', '<circle cx="8.2" cy="11" r="1.2" class="dot"/>', '<circle cx="12" cy="11" r="1.2" class="dot"/>', '<circle cx="15.8" cy="11" r="1.2" class="dot"/>'],
  Mic: ['<rect x="9" y="3" width="6" height="11" rx="3"/>', '<path d="M5.8 11a6.2 6.2 0 0 0 12.4 0"/>', '<path d="M12 17.2v3.3"/>'],
  Chapters: ['<path d="M9 6.5h11"/>', '<path d="M9 12h11"/>', '<path d="M9 17.5h11"/>', '<circle cx="4.8" cy="6.5" r="1.3" class="dot"/>', '<circle cx="4.8" cy="12" r="1.3" class="dot"/>', '<circle cx="4.8" cy="17.5" r="1.3" class="dot"/>'],
  Exit: ['<path d="M14 4h3.8A2.2 2.2 0 0 1 20 6.2v11.6a2.2 2.2 0 0 1-2.2 2.2H14"/>', '<path d="M10 16.2 14.2 12 10 7.8"/>', '<path d="M14 12H4"/>'],
  Swap: ['<path d="M4.5 8h14"/>', '<path d="M15 4.5 18.5 8 15 11.5"/>', '<path d="M19.5 16h-14"/>', '<path d="M9 12.5 5.5 16 9 19.5"/>'],
  Close: ['<path d="M7 7l10 10"/>', '<path d="M17 7 7 17"/>'],
  Lock: ['<rect x="5" y="10.5" width="14" height="10" rx="2.5"/>', '<path d="M8 10.5V8a4 4 0 0 1 8 0v2.5"/>'],
};

function iconSvg(parts) {
  const body = parts.map(p => p.indexOf('class="dot"') >= 0
    ? p.replace(' class="dot"', ' fill="#F4FAFF" stroke="none"')
    : p.replace(/^<(\w+)/, '<$1 fill="none" stroke="#F4FAFF" stroke-width="2.2" stroke-linecap="round" stroke-linejoin="round"')).join('');
  return '<svg xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24">' + body + '</svg>';
}

async function icons() {
  const pg = await page(DS_PAGE, 'Components');
  const b = board(pg, 'Kids UI / 02 Icons', 0, 2420, 3200, 520);
  await heading(b, 'Icons · Kids/Icon/… — one family, 24 grid, 2.2 px rounded strokes, tinted per use', 64, 48, P.gold);
  const names = Object.keys(ICONS);
  for (let i = 0; i < names.length; i++) {
    const f = figma.createNodeFromSvg(iconSvg(ICONS[names[i]]));
    const c = figma.createComponentFromNode(f);
    c.name = 'Kids/Icon/' + names[i];
    c.fills = [];
    c.resize(24, 24);
    for (const n of c.findAll(() => true)) { if ('constraints' in n) n.constraints = { horizontal: 'SCALE', vertical: 'SCALE' }; if (n.type !== 'GROUP' && n.type !== 'FRAME') n.name = n.fills && n.fills.length && !(n.strokes && n.strokes.length) ? 'Dot' : 'Stroke'; }
    c.description = 'Kids UI icon. Tint the strokes; scale with the instance.';
    const x = 64 + (i % 18) * 172, y = 140 + Math.floor(i / 18) * 170;
    const tile = rect(72, 72, '#122440', 18); tile.name = 'Tile / ' + names[i]; tile.strokes = [solid(P.blue, 0.4)]; tile.strokeWeight = 1;
    place(b, tile, x + 50, y);
    place(b, c, x + 74, y + 24);
    await label(b, names[i], x, y + 86, 172, P.text);
  }
  for (const k of Object.keys(COMPS)) delete COMPS[k];
  log('Icons: ' + names.length);
}
