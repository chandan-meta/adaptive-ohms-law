// Part 1 — foundations. Updates the file's existing UI v2 token collections, text styles and luminous FX styles in place
// to the kids reference (so every v2 screen bound to them follows), adds the new tokens and the Kids/ paint and effect
// styles, and draws the Foundations board on the design-system page.

const PRIMS = {
  navy: P.navy, surface: P.panel, raised: P.surface, blue: P.blue, blueAccessible: P.blue, cyan: P.cyan, amber: P.amber,
  paper: P.text, white: P.white, muted: P.muted, border: P.blue, success: P.green, error: P.red,
  errorSurface: '#3A1622', successSurface: '#0F3326', scrim: '#050B14',
  gold: P.gold, purple: P.purple, coral: P.coral, amberDeep: '#F0921A', ink: P.ink,
  purpleSurface: '#251A4A', goldSurface: '#3A2C0E', hintSurface: '#0E2C44',
};
// semantic name → primitive (same in Acrylic and Solid; the glass difference is opacity/surface).
const SEM = {
  bg: 'navy', surface: 'surface', raised: 'raised', action: 'amber', 'action-hover': 'gold', 'action-deep': 'amberDeep',
  accent: 'blue', 'measurement-voltage': 'blue', 'measurement-current': 'gold', 'measurement-resistance': 'coral',
  text: 'paper', 'text-strong': 'white', 'text-muted': 'muted', 'on-action': 'ink', border: 'border', focus: 'cyan',
  glow: 'cyan', success: 'success', error: 'error', 'error-bg': 'errorSurface', 'success-bg': 'successSurface',
  achievement: 'purple', 'achievement-bg': 'purpleSurface', gold: 'gold', 'gold-bg': 'goldSurface', hint: 'blue',
  'hint-bg': 'hintSurface', scrim: 'scrim',
};
const LAYOUT = {
  'space/4': 4, 'space/8': 8, 'space/12': 12, 'space/16': 16, 'space/24': 24, 'space/32': 32, 'space/48': 48, 'space/64': 64,
  'radius/8': 8, 'radius/12': 12, 'radius/16': 16, 'radius/20': 20, 'radius/24': 24, 'radius/28': 28, 'radius/999': 999,
  'size/touch-min': 48, 'size/button': 56, 'motion/button-ms': 140, 'motion/panel-ms': 220, 'motion/exit-ms': 160,
};
// Tone ramps for buttons: [light, mid1, mid2, deep]; card glass: [top, bottom].
const RAMP = {
  Amber: ['#FFE7A3', P.gold, P.amber, '#F0921A'], Cyan: ['#C8F7FF', P.cyan, P.blue, '#00A3D9'],
  Success: ['#B8F5CF', '#5BE08F', P.green, '#1FA45A'], Error: ['#FFC9C3', '#FF8F86', P.red, '#E04848'],
  Purple: ['#E4DBFF', '#C2AEFF', P.purple, '#8462EE'], Gold: ['#FFF1C2', '#FFE08A', P.gold, '#F2B530'],
};
const CARD = {
  Amber: ['#4A3214', '#24180A'], Cyan: ['#16406A', '#0B2240'], Success: ['#174A33', '#0B2419'],
  Error: ['#4A1A24', '#26101A'], Purple: ['#33245E', '#1A1336'], Gold: ['#4A3A12', '#241B08'],
};

async function pickFonts() {
  const avail = await figma.listAvailableFontsAsync();
  const has = (family, style) => avail.some(f => f.fontName.family === family && f.fontName.style === style);
  const pick = (cands) => { for (const c of cands) if (has(c[0], c[1])) return { family: c[0], style: c[1] }; return null; };
  const F = {
    headBold: pick([['Fredoka', 'Bold'], ['Fredoka', 'SemiBold'], ['Nunito', 'Black'], ['Nunito', 'ExtraBold']]),
    head: pick([['Fredoka', 'SemiBold'], ['Fredoka', 'Semi Bold'], ['Fredoka', 'Medium'], ['Nunito', 'ExtraBold']]),
    body: pick([['Nunito', 'Bold'], ['Nunito', 'SemiBold'], ['Nunito Sans', 'Bold'], ['Inter', 'Semi Bold']]),
    bodyX: pick([['Nunito', 'ExtraBold'], ['Nunito', 'Black'], ['Nunito', 'Bold'], ['Inter', 'Bold']]),
  };
  for (const k of Object.keys(F)) { if (!F[k]) F[k] = { family: 'Inter', style: 'Bold' }; await figma.loadFontAsync(F[k]); }
  log('Fonts: ' + Object.keys(F).map(k => k + ' ' + F[k].family + ' ' + F[k].style).join(', '));
  return F;
}

async function foundations() {
  // Variables: the existing collections (created if missing), primitives → semantic aliases → layout numbers.
  const cols = await figma.variables.getLocalVariableCollectionsAsync();
  const all = await figma.variables.getLocalVariablesAsync();
  const col = (name, modes) => {
    let c = cols.find(x => x.name === name);
    if (!c) { c = figma.variables.createVariableCollection(name); c.renameMode(c.modes[0].modeId, modes[0]); cols.push(c); }
    for (const m of modes) if (!c.modes.find(x => x.name === m)) c.addMode(m);
    return c;
  };
  const mode = (c, name) => c.modes.find(m => m.name === name).modeId;
  const varIn = (c, name, type) => {
    let v = all.find(x => x.name === name && x.variableCollectionId === c.id);
    if (!v) { v = figma.variables.createVariable(name, c, type); all.push(v); }
    return v;
  };
  const prim = col('OHM / Primitives', ['Value']), sem = col('OHM / Semantic', ['Acrylic', 'Solid']), lay = col('OHM / Layout and Motion', ['Value']);
  const pv = {};
  for (const k of Object.keys(PRIMS)) { pv[k] = varIn(prim, 'primitive/' + k, 'COLOR'); pv[k].setValueForMode(mode(prim, 'Value'), RGBA(PRIMS[k])); }
  for (const k of Object.keys(SEM)) {
    const v = varIn(sem, 'color/' + k, 'COLOR');
    const alias = figma.variables.createVariableAlias(pv[SEM[k]]);
    v.setValueForMode(mode(sem, 'Acrylic'), alias);
    v.setValueForMode(mode(sem, 'Solid'), alias);
    v.description = 'Kids UI: ' + SEM[k] + ' ' + PRIMS[SEM[k]];
  }
  const op = varIn(sem, 'opacity/surface', 'FLOAT');
  op.setValueForMode(mode(sem, 'Acrylic'), 84); op.setValueForMode(mode(sem, 'Solid'), 100);
  op.description = 'Glass panel opacity in percent (Unity alpha 0.84 / 1.0)';
  for (const k of Object.keys(LAYOUT)) varIn(lay, k, 'FLOAT').setValueForMode(mode(lay, 'Value'), LAYOUT[k]);

  // Text styles: the existing OHM/ names restyled (Fredoka headings, Nunito body) plus the new roles and tablet set.
  const F = await pickFonts();
  const TS = {
    Display: [F.headBold, 72, 80], Title: [F.head, 40, 48], Heading: [F.head, 28, 34], Panel: [F.head, 24, 30],
    Measure: [F.head, 56, 60], Body: [F.body, 22, 32], Button: [F.bodyX, 22, 28], Label: [F.bodyX, 15, 20, 6, 'UPPER'],
    Caption: [F.body, 17, 24], Micro: [F.body, 15, 20],
    TabletDisplay: [F.headBold, 48, 56], TabletTitle: [F.head, 28, 34], TabletHeading: [F.head, 22, 28], TabletPanel: [F.head, 19, 24],
    TabletMeasure: [F.head, 36, 40], TabletBody: [F.body, 17, 24], TabletButton: [F.bodyX, 18, 22],
    TabletLabel: [F.bodyX, 13, 16, 6, 'UPPER'], TabletCaption: [F.body, 14, 20], TabletMicro: [F.body, 13, 18],
  };
  const texts = await figma.getLocalTextStylesAsync();
  for (const k of Object.keys(TS)) {
    const s = TS[k], name = 'OHM/' + k;
    let st = texts.find(x => x.name === name);
    if (!st) st = figma.createTextStyle();
    st.name = name;
    st.fontName = s[0];
    st.fontSize = s[1];
    st.lineHeight = { unit: 'PIXELS', value: s[2] };
    st.letterSpacing = { unit: 'PERCENT', value: s[3] || 0 };
    st.textCase = s[4] || 'ORIGINAL';
    st.description = 'Kids UI ' + (k.indexOf('Tablet') === 0 ? 'tablet (1024×768 pt)' : 'desktop (1920×1080)') + ': ' + s[0].family + ' ' + s[0].style + ' ' + s[1] + '/' + s[2];
  }

  // Paint and effect styles.
  const paints = await figma.getLocalPaintStylesAsync(), effects = await figma.getLocalEffectStylesAsync();
  const paintStyle = (name, list, desc) => { let s = paints.find(x => x.name === name); if (!s) { s = figma.createPaintStyle(); paints.push(s); } s.name = name; s.paints = list; s.description = desc || ''; return s; };
  const effectStyle = (name, list, desc) => { let s = effects.find(x => x.name === name); if (!s) { s = figma.createEffectStyle(); effects.push(s); } s.name = name; s.effects = list; s.description = desc || ''; return s; };
  const G = {
    glass: [grad([[0, '#22406A', 0.9], [0.45, '#15294A', 0.86], [1, '#0B1830', 0.86]])],
    raised: [grad([[0, '#2E4A74', 0.94], [1, '#1A2B45', 0.94]])],
    well: [solid(P.navy, 0.72)],
  };
  paintStyle('Kids/Glass', G.glass, 'Glass panel: Panel Navy gradient at 84–90 %, no blur');
  paintStyle('Kids/Glass Raised', G.raised, 'Secondary buttons, inactive tabs, value wells on hover');
  paintStyle('Kids/Well', G.well, 'Inset value wells, slider tracks, table rows');
  paintStyle('Kids/Scrim', [solid(P.navy, 0.55)], 'Behind pop-ups');
  for (const t of Object.keys(RAMP)) {
    const r = RAMP[t];
    paintStyle('Kids/Fill ' + t, [grad([[0, r[0]], [0.25, r[1]], [0.7, r[2]], [1, r[3]]])], t + ' button, default');
    paintStyle('Kids/Fill ' + t + ' Hover', [grad([[0, P.white], [0.25, r[0]], [0.7, r[1]], [1, r[2]]])], t + ' button, hover');
    paintStyle('Kids/Fill ' + t + ' Pressed', [grad([[0, r[3]], [1, r[2]]])], t + ' button, pressed');
    effectStyle('Kids/Glow ' + t, [shadow(r[2], 0.55, 20), shadow(P.shade, 0.35, 16, 6), inner(P.white, 0.5, 0, 2), inner(r[3], 0.5, 0, -3)], t + ' button glow with glossy bevel');
    effectStyle('Kids/Glow ' + t + ' Strong', [shadow(r[2], 0.85, 28), shadow(r[1], 0.35, 56), shadow(P.shade, 0.35, 16, 6), inner(P.white, 0.6, 0, 2), inner(r[3], 0.5, 0, -3)], t + ' hover / selected glow');
  }
  for (const t of Object.keys(CARD)) {
    paintStyle('Kids/Card ' + t, [grad([[0, CARD[t][0], 0.94], [1, CARD[t][1], 0.94]])], t + ' tinted glass card');
    effectStyle('Kids/Card Glow ' + t, [shadow(RAMP[t][2], 0.45, 22), shadow(P.shade, 0.45, 32, 12), inner(P.white, 0.18, 0, 1)], t + ' card rim glow');
  }
  paintStyle('Kids/Copper', [grad([[0, '#FFD2AD'], [0.22, '#F09A5C'], [0.6, '#B5582A'], [1, '#5E2A10']])], 'Copper rod');
  paintStyle('Kids/Nichrome', [grad([[0, P.white], [0.22, '#D5DCE4'], [0.6, '#8A96A3'], [1, '#3C444E']])], 'Nichrome rod');
  paintStyle('Kids/Title Gradient', [grad([[0, '#FFF1C2'], [0.45, P.gold], [1, '#F0921A']])], 'OHM’S LAW title fill');
  effectStyle('Kids/Glass Panel', [shadow(P.blue, 0.32, 18), shadow(P.shade, 0.45, 32, 12), inner(P.white, 0.2, 0, 1)], 'Cyan edge glow, soft depth, inner top highlight');
  effectStyle('Kids/Glass Active', [shadow(P.cyan, 0.6, 22), shadow(P.blue, 0.28, 56), shadow(P.shade, 0.45, 32, 12), inner(P.white, 0.28, 0, 1)], 'The panel asking for the learner’s action');
  effectStyle('Kids/Pressed', [inner(P.shade, 0.55, 6, 3)], 'Pressed: recessed');
  effectStyle('Kids/Focus Glow', [shadow(P.cyan, 0.6, 16)], 'Keyboard / controller focus (with a 3 px Cyan Glow outside stroke)');
  effectStyle('Kids/Depth Soft', [shadow(P.shade, 0.45, 32, 12)], 'Soft drop shadow');
  effectStyle('Kids/Bloom Blue', [shadow(P.blue, 0.35, 48)], 'Wide blue bloom behind hero elements');
  effectStyle('Kids/Text Glow Blue', [shadow(P.blue, 0.75, 14)], 'Neon text glow');
  effectStyle('Kids/Text Glow Amber', [shadow(P.amber, 0.7, 16)], 'Title glow');

  // The UI v2 luminous styles, restyled so the older v2 screens follow the kids look.
  const legacyPaint = {
    'OHM/FX/Navy acrylic': G.glass, 'OHM/FX/Secondary glass': G.raised, 'OHM/FX/Muted surface': G.well,
    'OHM/FX/Illuminated primary': 'Amber', 'OHM/FX/Primary hover': 'Amber Hover', 'OHM/FX/Recessed primary': 'Amber Pressed',
  };
  const legacyFx = {
    'OHM/FX/Panel halo': 'Kids/Glass Panel', 'OHM/FX/Secondary halo': 'Kids/Glass Panel', 'OHM/FX/Active objective': 'Kids/Glass Active',
    'OHM/FX/Primary illumination': 'Kids/Glow Amber', 'OHM/FX/Primary hover halo': 'Kids/Glow Amber Strong', 'OHM/FX/Pressed depth': 'Kids/Pressed',
  };
  let legacy = 0;
  for (const s of paints) if (legacyPaint[s.name]) {
    const v = legacyPaint[s.name];
    s.paints = typeof v === 'string' ? paints.find(x => x.name === 'Kids/Fill ' + v).paints : v; legacy++;
  }
  for (const s of effects) if (legacyFx[s.name]) { s.effects = effects.find(x => x.name === legacyFx[s.name]).effects; legacy++; }
  log('Foundations: ' + Object.keys(PRIMS).length + ' primitives, ' + Object.keys(SEM).length + ' semantic colours, ' +
    Object.keys(TS).length + ' text styles, ' + legacy + ' UI v2 FX styles restyled');
  await init();
}

// The Foundations board: colour tokens, type, effects, spacing / radius, contrast rules.
async function foundationsBoard() {
  const pg = await page(DS_PAGE, 'Foundations');
  const b = board(pg, 'Kids UI / 01 Foundations', 0, 0, 3200, 2300);
  await heading(b, 'Foundations · tokens, type, light', 64, 56, P.gold);
  await label(b, 'Every colour below is a variable (OHM / Semantic, modes Acrylic and Solid). Styles: OHM/… text, Kids/… paint and effect.', 64, 120, 2400);
  // Colour tokens, as in the reference palette.
  const sw = [['bg', 'Deep Navy'], ['surface', 'Panel Navy'], ['raised', 'Surface'], ['accent', 'Electric Blue'], ['glow', 'Cyan Glow'],
    ['action', 'Warm Amber'], ['gold', 'Gold'], ['success', 'Success'], ['error', 'Error'], ['achievement', 'Purple'],
    ['text', 'Text'], ['text-muted', 'Text muted'], ['measurement-voltage', 'V voltage'], ['measurement-current', 'I current'], ['measurement-resistance', 'R resistance']];
  for (let i = 0; i < sw.length; i++) {
    const x = 64 + (i % 8) * 200, y = 200 + Math.floor(i / 8) * 200;
    const c = ellipse(104, 'color/' + sw[i][0]); c.name = 'Swatch / ' + sw[i][0];
    c.strokes = [solid(P.white, 0.25)]; c.strokeWeight = 2; c.effects = [shadow(P.shade, 0.5, 16, 6)];
    place(b, c, x + 28, y);
    await label(b, sw[i][1], x, y + 116, 160, P.text);
    await label(b, 'color/' + sw[i][0], x, y + 142, 180);
  }
  // Type.
  let y = 640;
  await heading(b, 'Type · Fredoka + Nunito', 64, y, P.cyan);
  y += 72;
  const specimens = [['Display', 'OHM’S LAW'], ['Title', 'Copper vs nichrome'], ['Heading', 'Set the battery'], ['Panel', 'Your objective'],
    ['Measure', '0.60 A'], ['Body', 'Slide the battery, then watch the meters.'], ['Button', 'Record reading'], ['Label', 'Quick check'], ['Caption', 'Current is measured, never set.']];
  for (const s of specimens) {
    const t = await TX(null, s[1], s[0], 'Desktop', 'color/text');
    place(b, t, 360, y);
    await label(b, 'OHM/' + s[0] + ' · ' + STY['OHM/' + s[0]].fontSize + ' px', 64, y + 4, 280);
    y += t.height + 20;
  }
  await label(b, 'Tablet set: OHM/Tablet… (Display 48, Title 28, Heading 22, Measure 36, Body 17, Button 18, Label 13, Caption 14). Nothing below 13 px.', 64, y + 8, 1500);
  // Light: effect styles on sample cards.
  const fxs = ['Kids/Glass Panel', 'Kids/Glass Active', 'Kids/Glow Amber', 'Kids/Glow Amber Strong', 'Kids/Card Glow Success', 'Kids/Card Glow Error',
    'Kids/Card Glow Purple', 'Kids/Card Glow Gold', 'Kids/Pressed', 'Kids/Focus Glow'];
  const fills = ['Kids/Glass', 'Kids/Glass', 'Kids/Fill Amber', 'Kids/Fill Amber Hover', 'Kids/Card Success', 'Kids/Card Error', 'Kids/Card Purple', 'Kids/Card Gold', 'Kids/Fill Amber Pressed', 'Kids/Glass Raised'];
  await heading(b, 'Light · glass, glow, bevel', 1760, 200, P.cyan);
  for (let i = 0; i < fxs.length; i++) {
    const x = 1760 + (i % 3) * 460, yy = 280 + Math.floor(i / 3) * 230;
    const card = figma.createFrame(); card.resize(400, 150); card.cornerRadius = 20; card.clipsContent = false;
    await fillStyle(card, fills[i]); await fx(card, fxs[i]);
    card.strokes = [solid(i === 9 ? P.cyan : P.white, i === 9 ? 1 : 0.22)]; card.strokeWeight = i === 9 ? 3 : 1.5; card.strokeAlign = i === 9 ? 'OUTSIDE' : 'INSIDE';
    card.name = 'Effect / ' + fxs[i];
    place(b, card, x, yy); sheen(card);
    await label(b, fxs[i] + '  +  ' + fills[i], x, yy + 162, 420);
  }
  // Space, radius, rules.
  const ry = 1260;
  await heading(b, 'Space 4–64 · radius 12 / 16 / 20 / 28 / pill · targets ≥ 48', 1760, ry, P.cyan);
  let x = 1760;
  for (const s of [4, 8, 12, 16, 24, 32, 48, 64]) { const r = rect(s, 64, P.blue, 2); r.name = 'space/' + s; place(b, r, x, ry + 80); await label(b, String(s), x, ry + 152); x += s + 40; }
  x = 1760;
  for (const r of [12, 16, 20, 28, 999]) { const n = rect(120, 72, 'color/raised'); n.name = 'radius/' + r; radius(n, r); n.strokes = [solid(P.blue, 0.6)]; n.strokeWeight = 1.5; place(b, n, x, ry + 200); await label(b, r === 999 ? 'pill' : String(r), x, ry + 282); x += 150; }
  const rules = ['Ink #0B1626 on every bright button (6.5–12.6 : 1); never white (1.4–2.8 : 1).',
    'Text #F4FAFF on glass: 14.8 : 1 over dark scenes, 8.4 : 1 over the brightest lab frame.',
    'Red, purple and coral text only large (≥ 24 px bold) or on their own tinted card.',
    'Colour never alone: each state has an icon and a word; each quantity its letter and unit.',
    'Glass is 84 % with no background blur (Unity overlay canvas); Solid mode = 100 % for low-end tablets.',
    'Motion: buttons 140 ms, panels 220 ms ease-out, exits faster; reduced motion = fades only.'];
  y = ry + 360;
  for (const r of rules) { await label(b, '•  ' + r, 1760, y, 1360, P.text); y += 40; }
}
