// Part 3a — controls: Icon Tile, Button, Icon Button, Nav Tile, Glass Panel, Quiz Option, Checklist Item, Tabs, Reading,
// VS Badge. Each is a variant set with Auto Layout, token-bound spacing / radius / colours, and the Kids/ styles.

const BTN = {
  Primary: { tone: 'Amber', ink: 'color/on-action' },
  Secondary: { tone: null, ink: 'color/text' },
  Success: { tone: 'Success', ink: 'color/on-action' },
  Danger: { tone: 'Error', ink: 'color/on-action' },
  Achievement: { tone: 'Purple', ink: 'color/on-action' },
};
const TILE_TONES = ['Glass', 'Amber', 'Cyan', 'Success', 'Error', 'Purple', 'Gold'];
const TONE_ICON = { Glass: P.cyan, Active: P.cyan, Amber: P.amber, Cyan: P.blue, Success: P.green, Error: P.red, Purple: P.purple, Gold: P.gold };


async function controls() {
  const pg = await page(DS_PAGE, 'Components');
  const b = board(pg, 'Kids UI / 03 Components · Controls', 3400, 0, 3200, 1000);
  CY = 64;

  // Icon Tile
  let set = await variantSet(b, 'Kids/Icon Tile', { Tone: TILE_TONES, Size: SIZES }, async (p) => {
    const s = SZ[p.Size];
    const c = AL('Icon Tile', { component: true, main: 'CENTER', cross: 'CENTER', w: s.tile, h: s.tile, radius: p.Size === 'Desktop' ? 16 : 12 });
    add(c, icon('Home', s.icon, TONE_ICON[p.Tone]));
    await glass(c, p.Tone === 'Glass' ? 'Glass' : p.Tone, { sheenA: 0.22 });
    return c;
  }, { width: 3072, gap: 24 });
  swapProp(set, 'Icon', 'Icon', 'Kids/Icon/Home');
  await placeSet(b, set, 'Icon Tile', 'Rounded tile for every icon in the HUD and panels. Tone = rim, glow and icon colour; swap the icon.');

  // Button
  set = await variantSet(b, 'Kids/Button', { Kind: Object.keys(BTN), State: ['Default', 'Hover', 'Pressed', 'Focus', 'Disabled'], Size: SIZES }, async (p) => {
    const s = SZ[p.Size], K = BTN[p.Kind];
    const c = AL('Button', { component: true, dir: 'HORIZONTAL', main: 'CENTER', cross: 'CENTER', gap: 8, pad: [0, s.btnX], h: s.btnH, radius: 16 });
    const lead = add(c, icon('Bolt', s.icon, K.ink)); lead.name = 'Leading';
    await TX(c, 'Button', 'Button', p.Size, K.ink, { name: 'Label' });
    const trail = add(c, icon('Arrow', s.icon, K.ink)); trail.name = 'Trailing';
    const base = K.tone ? 'Kids/Fill ' + K.tone : 'Kids/Glass Raised';
    if (p.State === 'Hover') await fillStyle(c, K.tone ? base + ' Hover' : base);
    else if (p.State === 'Pressed') await fillStyle(c, K.tone ? base + ' Pressed' : 'Kids/Well');
    else await fillStyle(c, base);
    c.strokes = [K.tone ? solid(P.white, 0.45) : solid(p.State === 'Hover' ? P.cyan : P.blue, p.State === 'Hover' ? 0.95 : 0.7)];
    c.strokeWeight = 1.5; c.strokeAlign = 'INSIDE';
    const glow = K.tone ? 'Kids/Glow ' + K.tone : 'Kids/Glass Panel';
    if (p.State === 'Default') await fx(c, glow);
    if (p.State === 'Hover') await fx(c, K.tone ? glow + ' Strong' : 'Kids/Glass Active');
    if (p.State === 'Pressed') await fx(c, 'Kids/Pressed');
    if (p.State === 'Focus') { await fx(c, 'Kids/Focus Glow'); c.strokes = [solid(P.cyan)]; c.strokeWeight = 3; c.strokeAlign = 'OUTSIDE'; }
    if (p.State === 'Disabled') { c.opacity = 0.4; c.effects = []; }
    if (p.State !== 'Pressed') sheen(c, s.btnH * 0.48, K.tone ? 0.38 : 0.14);
    return c;
  }, { width: 3072, gap: 24 });
  textProp(set, 'Label', 'Label', 'Button');
  boolProp(set, 'Leading icon', 'Leading', false);
  boolProp(set, 'Trailing icon', 'Trailing', false);
  swapProp(set, 'Leading', 'Leading', 'Kids/Icon/Bolt');
  swapProp(set, 'Trailing', 'Trailing', 'Kids/Icon/Arrow');
  await placeSet(b, set, 'Button', 'Primary = the one main action per screen (amber, ink label). Hover brightens, Pressed recesses, Focus adds a 3 px Cyan Glow ring, Disabled is 40 % with no glow. Desktop 56 tall, tablet 48.');

  // Icon Button (toolbar)
  set = await variantSet(b, 'Kids/Icon Button', { State: ['Default', 'Hover', 'Pressed', 'Active', 'Disabled'], Size: SIZES }, async (p) => {
    const s = SZ[p.Size];
    const c = AL('Icon Button', { component: true, cross: 'CENTER', gap: 6 });
    const t = add(c, AL('Tile', { main: 'CENTER', cross: 'CENTER', w: s.tile, h: s.tile, radius: p.Size === 'Desktop' ? 16 : 12 }));
    add(t, icon('Pause', s.icon, p.State === 'Active' ? P.amber : P.cyan));
    if (p.State === 'Pressed') { await fillStyle(t, 'Kids/Well'); t.strokes = [solid(P.blue, 0.5)]; t.strokeWeight = 1.5; await fx(t, 'Kids/Pressed'); }
    else await glass(t, p.State === 'Active' ? 'Amber' : p.State === 'Hover' ? 'Active' : 'Glass', { sheenA: 0.2 });
    await TX(c, 'Pause', 'Micro', p.Size, p.State === 'Active' ? P.gold : 'color/text-muted', { name: 'Label', align: 'CENTER' });
    if (p.State === 'Disabled') { c.opacity = 0.4; t.effects = []; }
    return c;
  }, { width: 3072, gap: 32 });
  textProp(set, 'Label', 'Label', 'Pause');
  boolProp(set, 'Show label', 'Label', true);
  swapProp(set, 'Icon', 'Icon', 'Kids/Icon/Pause');
  await placeSet(b, set, 'Icon Button', 'Toolbar: Pause, Replay, Captions, Audio, Skip. Active = a toggle that is on (captions). Label under the tile so icons never stand alone.');

  // Nav Tile
  set = await variantSet(b, 'Kids/Nav Tile', { Selected: ['No', 'Yes'], Size: SIZES }, async (p) => {
    const big = p.Size === 'Desktop' ? 88 : 64;
    const c = AL('Nav Tile', { component: true, cross: 'CENTER', gap: 8 });
    const t = add(c, AL('Tile', { main: 'CENTER', cross: 'CENTER', w: big, h: big, radius: p.Size === 'Desktop' ? 24 : 18 }));
    add(t, icon('Home', big * 0.5, p.Selected === 'Yes' ? P.ink : P.amber));
    if (p.Selected === 'Yes') { await fillStyle(t, 'Kids/Fill Amber'); t.strokes = [solid(P.white, 0.5)]; t.strokeWeight = 1.5; await fx(t, 'Kids/Glow Amber Strong'); sheen(t, big * 0.45, 0.4); }
    else await glass(t, 'Glass', { sheenA: 0.2 });
    await TX(c, 'Home', 'Caption', p.Size, p.Selected === 'Yes' ? P.gold : 'color/text', { name: 'Label', align: 'CENTER' });
    return c;
  }, { width: 1400, gap: 40 });
  textProp(set, 'Label', 'Label', 'Home');
  swapProp(set, 'Icon', 'Icon', 'Kids/Icon/Home');
  await placeSet(b, set, 'Nav Tile', 'Pause menu, chapters and end screens. Selected = amber tile, ink icon, gold label.');

  // Glass Panel
  set = await variantSet(b, 'Kids/Glass Panel', { Tone: ['Glass', 'Active', 'Amber', 'Cyan', 'Success', 'Error', 'Purple', 'Gold'] }, async (p) => {
    const c = AL('Glass Panel', { component: true, gap: 8, pad: 24, w: 420, radius: 20 });
    await TX(c, 'Title here', 'Panel', 'Desktop', p.Tone === 'Glass' || p.Tone === 'Active' ? 'color/text' : TONE[p.Tone].c, { name: 'Title', fillW: true });
    await TX(c, 'A glass panel: clear, readable, and part of the game’s world.', 'Caption', 'Desktop', 'color/text-muted', { name: 'Body', fillW: true });
    await glass(c, p.Tone);
    return c;
  }, { width: 3072, gap: 32 });
  textProp(set, 'Title', 'Title', 'Title here');
  textProp(set, 'Body', 'Body', 'A glass panel: clear, readable, and part of the game’s world.');
  await placeSet(b, set, 'Glass Panel', 'The surface every card is made of: Panel Navy glass at 84–94 %, 1.5 px rim, edge glow, top reflection. Tone = meaning.');

  // Quiz Option
  const OPT = {
    Default: { fill: 'Kids/Glass Raised', rim: [P.blue, 0.45, 1.5], glow: null, badge: P.blue },
    Hover: { fill: 'Kids/Glass Raised', rim: [P.cyan, 0.95, 1.5], glow: 'Kids/Glass Active', badge: P.cyan },
    Selected: { fill: 'Kids/Card Cyan', rim: [P.cyan, 1, 2.5], glow: 'Kids/Glass Active', badge: P.cyan },
    Correct: { fill: 'Kids/Card Success', rim: [P.green, 1, 2.5], glow: 'Kids/Card Glow Success', badge: P.green },
    Wrong: { fill: 'Kids/Card Error', rim: [P.red, 1, 2.5], glow: 'Kids/Card Glow Error', badge: P.red },
    Disabled: { fill: 'Kids/Glass Raised', rim: [P.blue, 0.3, 1.5], glow: null, badge: P.muted },
  };
  set = await variantSet(b, 'Kids/Quiz Option', { State: Object.keys(OPT), Size: SIZES }, async (p) => {
    const s = SZ[p.Size], O = OPT[p.State];
    const c = AL('Quiz Option', { component: true, dir: 'HORIZONTAL', cross: 'CENTER', gap: 14, pad: [0, 16, 0, 12], w: p.Size === 'Desktop' ? 560 : 420, h: s.opt, radius: 16 });
    const badge = add(c, AL('Badge', { main: 'CENTER', cross: 'CENTER', w: s.badge, h: s.badge, radius: 999, fill: O.badge }));
    await TX(badge, 'A', 'Button', p.Size, 'color/on-action', { name: 'Letter', align: 'CENTER' });
    await TX(c, 'It increases', 'Body', p.Size, 'color/text', { name: 'Answer', fillW: true });
    const mark = add(c, AL('Mark', { main: 'CENTER', cross: 'CENTER', w: 28, h: 28, radius: 999 }));
    if (p.State === 'Correct' || p.State === 'Wrong') { mark.fills = [solid(O.badge)]; add(mark, icon(p.State === 'Correct' ? 'Check' : 'Cross', 20, P.ink)); }
    else if (p.State === 'Selected') { mark.strokes = [solid(P.cyan)]; mark.strokeWeight = 2.5; add(mark, ellipse(12, P.cyan)); }
    else { mark.strokes = [solid(P.muted, 0.6)]; mark.strokeWeight = 2; }
    await fillStyle(c, O.fill);
    c.strokes = [solid(O.rim[0], O.rim[1])]; c.strokeWeight = O.rim[2]; c.strokeAlign = 'INSIDE';
    await fx(c, O.glow);
    if (p.State === 'Disabled') c.opacity = 0.45;
    sheen(c, s.opt * 0.45, 0.12);
    return c;
  }, { width: 3072, gap: 24 });
  textProp(set, 'Letter', 'Letter', 'A');
  textProp(set, 'Answer', 'Answer', 'It increases');
  await placeSet(b, set, 'Quiz Option', 'Letter badge + answer + mark. Selected shows a filled ring; Correct a check and Wrong a cross, so colour is never the only signal. Whole row is the target (64 / 56 tall).');

  // Checklist Item
  set = await variantSet(b, 'Kids/Checklist Item', { State: ['Done', 'Current', 'To do'], Size: SIZES }, async (p) => {
    const d = p.Size === 'Desktop' ? 30 : 26;
    const c = AL('Checklist Item', { component: true, dir: 'HORIZONTAL', cross: 'CENTER', gap: 12 });
    const box = add(c, AL('Box', { main: 'CENTER', cross: 'CENTER', w: d, h: d, radius: 8 }));
    if (p.State === 'Done') { await fillStyle(box, 'Kids/Fill Success'); add(box, icon('Check', d * 0.72, P.ink)); await fx(box, 'Kids/Glow Success'); }
    else if (p.State === 'Current') { box.strokes = [solid(P.amber)]; box.strokeWeight = 2.5; add(box, ellipse(d * 0.36, P.amber)); await fx(box, 'Kids/Card Glow Amber'); }
    else { box.strokes = [solid(P.muted, 0.55)]; box.strokeWeight = 2; }
    await TX(c, 'Set the voltage', 'Body', p.Size, p.State === 'Current' ? P.gold : p.State === 'Done' ? 'color/text' : 'color/text-muted', { name: 'Step' });
    return c;
  }, { width: 2000, gap: 40 });
  textProp(set, 'Step', 'Step', 'Set the voltage');
  await placeSet(b, set, 'Checklist Item', 'Done = green box + check; Current = amber ring + gold text; To do = muted outline.');

  // Tabs
  set = await variantSet(b, 'Kids/Tabs', { Selected: ['Table', 'Graph', 'Analysis'], Size: SIZES }, async (p) => {
    const c = AL('Tabs', { component: true, dir: 'HORIZONTAL', cross: 'CENTER', gap: 6, pad: 6, radius: 16 });
    await fillStyle(c, 'Kids/Well'); c.strokes = [solid(P.blue, 0.35)]; c.strokeWeight = 1.5; c.strokeAlign = 'INSIDE';
    for (const t of [['Table', 'Data table', 'Table'], ['Graph', 'V–I graph', 'Graph'], ['Analysis', 'Analysis', 'Bulb']]) {
      const on = p.Selected === t[0];
      const seg = add(c, AL('Tab ' + t[0], { dir: 'HORIZONTAL', main: 'CENTER', cross: 'CENTER', gap: 8, pad: [0, p.Size === 'Desktop' ? 24 : 16], h: p.Size === 'Desktop' ? 48 : 44, radius: 12 }));
      add(seg, icon(t[2], p.Size === 'Desktop' ? 22 : 18, on ? P.ink : P.muted));
      await TX(seg, t[1], 'Button', p.Size, on ? 'color/on-action' : 'color/text-muted', { name: 'Label ' + t[0] });
      if (on) { await fillStyle(seg, 'Kids/Fill Cyan'); await fx(seg, 'Kids/Glow Cyan'); sheen(seg, 20, 0.35); }
    }
    return c;
  }, { width: 2400, gap: 40 });
  await placeSet(b, set, 'Tabs', 'Scene 11 data view: table, graph, analysis. Selected = Electric Blue segment with ink label.');

  // Reading
  const Q = {
    Voltage: { icon: 'Voltmeter', c: P.blue, glow: 'Kids/Card Glow Cyan', label: 'Voltage', value: '3.0 V', delta: '▲ was 2.0 V' },
    Current: { icon: 'Ammeter', c: P.gold, glow: 'Kids/Card Glow Gold', label: 'Current', value: '0.60 A', delta: '▲ was 0.40 A' },
    Resistance: { icon: 'Ohm', c: P.coral, glow: 'Kids/Card Glow Error', label: 'Resistance', value: '5.0 Ω', delta: 'R = V ÷ I' },
  };
  set = await variantSet(b, 'Kids/Reading', { Quantity: Object.keys(Q), Size: SIZES }, async (p) => {
    const q = Q[p.Quantity], D = p.Size === 'Desktop';
    const c = AL('Reading', { component: true, gap: 2, pad: D ? [16, 22] : [10, 14], w: D ? 280 : 196, radius: D ? 20 : 16 });
    const head = add(c, AL('Header', { dir: 'HORIZONTAL', cross: 'CENTER', gap: 8 }));
    add(head, icon(q.icon, D ? 24 : 20, q.c));
    await TX(head, q.label, 'Label', p.Size, 'color/text-muted', { name: 'Label' });
    await TX(c, q.value, 'Measure', p.Size, q.c, { name: 'Value' });
    await TX(c, q.delta, 'Micro', p.Size, 'color/text-muted', { name: 'Delta' });
    await fillStyle(c, 'Kids/Glass'); c.strokes = [solid(q.c, 0.85)]; c.strokeWeight = 1.5; c.strokeAlign = 'INSIDE';
    await fx(c, q.glow); sheen(c, D ? 40 : 28, 0.16);
    return c;
  }, { width: 2400, gap: 32 });
  textProp(set, 'Label', 'Label', 'Current');
  textProp(set, 'Value', 'Value', '0.60 A');
  textProp(set, 'Delta', 'Delta', '▲ was 0.40 A');
  boolProp(set, 'Show delta', 'Delta', true);
  await placeSet(b, set, 'Reading', 'Live meter values. V Electric Blue, I Gold, R Coral (the game’s 3D colours), each with its meter icon, word and unit.');

  // VS Badge
  const vs = AL('VS Badge', { component: true, main: 'CENTER', cross: 'CENTER', w: 72, h: 72, radius: 999 });
  await TX(vs, 'VS', 'Panel', 'Desktop', P.cyan, { name: 'Label' });
  await glass(vs, 'Active', { sheenA: 0.25 });
  single(b, 'Kids/VS Badge', vs, 'Between the copper and nichrome cards.');
  await heading(b, 'VS Badge', 64, CY, P.gold);
  vs.x = 64; vs.y = CY + 70; CY = vs.y + 72 + 110;

  b.resize(3200, CY);
  for (const k of Object.keys(COMPS)) delete COMPS[k];
  log('Controls: Icon Tile, Button, Icon Button, Nav Tile, Glass Panel, Quiz Option, Checklist Item, Tabs, Reading, VS Badge');
}
