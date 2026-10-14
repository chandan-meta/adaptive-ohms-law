// Part 6 — the independent component sheet: the reference sheet's twelve sections rebuilt from the library (instances
// only), plus readings / hero and title / Ask Wattson, with scaled copies of the real mockups for "scene match" and
// "responsive". One frame, 3600 wide, on the design-system page.

async function sheetSection(row, num, title, w) {
  const s = AL('Section ' + num, { gap: 20, pad: 28, w: w, radius: 24 });
  const t = await TX(s, num + '.  ' + title, 'Panel', 'Desktop', 'color/text', { name: 'Section title' });
  t.textCase = 'UPPER';
  row.appendChild(s);
  return s;
}
async function finishSection(s) { await fillStyle(s, 'Kids/Glass'); s.strokes = [solid(P.blue, 0.45)]; s.strokeWeight = 1.5; s.strokeAlign = 'INSIDE'; await fx(s, 'Kids/Glass Panel'); sheen(s, 40, 0.1); }
function wrapRow(parent, gapX) { const r = add(parent, AL('Content', { dir: 'HORIZONTAL', gap: gapX || 24 }), { fillW: true }); r.layoutWrap = 'WRAP'; r.counterAxisSpacing = 24; return r; }
function thumb(name, w) {
  const pg = figma.root.children.find(x => x.name === SCREENS_PAGE);
  const f = pg && pg.findOne(n => n.type === 'FRAME' && n.name === name && n.getSharedPluginData('kidsui', 'part') === 'screens');
  if (!f) return null;
  const c = f.clone(); c.rescale(w / f.width); c.cornerRadius = 16; c.name = 'Thumb · ' + name;
  return c;
}

async function sheet() {
  const pg = await page(DS_PAGE, 'Components');
  clear(pg, 'sheet');
  const sh = own(AL('Kids UI / 00 Component Sheet', { gap: 24, pad: 40, w: 3600, radius: 32 }), 'sheet');
  sh.fills = [grad([[0, '#0E2342'], [0.5, '#081428'], [1, '#050C18']])];
  pg.appendChild(sh); sh.x = -3900; sh.y = 0;

  // Header
  const head = add(sh, AL('Header', { dir: 'HORIZONTAL', cross: 'CENTER', gap: 40 }), { fillW: true });
  const titles = add(head, AL('Titles', { gap: 4 }));
  const t = await TX(titles, 'OHM’S LAW', 'Display', 'Desktop', 'color/text', { name: 'Title' });
  t.fontSize = 150; t.lineHeight = { unit: 'PERCENT', value: 100 }; await fillStyle(t, 'Kids/Title Gradient'); await fx(t, 'Kids/Text Glow Amber');
  const sub = await TX(titles, 'Gameplay-matched kids UI', 'Title', 'Desktop', P.cyan, { name: 'Subtitle' }); sub.textCase = 'UPPER'; await fx(sub, 'Kids/Text Glow Blue');
  spacer(head);
  await TX(head, 'Explore · Experiment · Discover\nReal-world physics, one clue at a time.', 'Heading', 'Desktop', 'color/text-muted', { name: 'Tagline', align: 'RIGHT' });
  const face = add(head, ellipse(150, imgPaint('wattson'))); face.name = 'Wattson'; face.strokes = [solid(P.cyan)]; face.strokeWeight = 4; face.effects = [shadow(P.blue, 0.8, 30)];

  // Row A — brand, scene match
  let row = add(sh, AL('Row A', { dir: 'HORIZONTAL', gap: 24 }), { fillW: true });
  let s = await sheetSection(row, 1, 'Brand / visual language', 2100);
  let c = wrapRow(s, 40);
  const pal = add(c, AL('Colour palette', { gap: 12 }));
  await TX(pal, 'Colour palette', 'Button', 'Desktop', 'color/text', { name: 'Heading' });
  const grid = add(pal, AL('Swatches', { dir: 'HORIZONTAL', gap: 18, w: 640 })); grid.layoutWrap = 'WRAP'; grid.counterAxisSpacing = 16;
  for (const q of [['bg', 'Deep Navy', P.navy], ['accent', 'Electric Blue', P.blue], ['glow', 'Cyan Glow', P.cyan], ['action', 'Warm Amber', P.amber], ['gold', 'Gold', P.gold],
    ['success', 'Success', P.green], ['error', 'Error', P.red], ['achievement', 'Purple', P.purple], ['surface', 'Panel Navy', P.panel], ['raised', 'Surface', P.surface]]) {
    const cell = add(grid, AL(q[1], { cross: 'CENTER', gap: 4, w: 110 }));
    const e = add(cell, ellipse(64, 'color/' + q[0])); e.strokes = [solid(P.white, 0.3)]; e.strokeWeight = 2; e.effects = [shadow(q[2], 0.5, 14)];
    await TX(cell, q[1], 'Micro', 'Desktop', 'color/text', { align: 'CENTER' });
    await TX(cell, q[2], 'Micro', 'Desktop', 'color/text-muted', { align: 'CENTER' });
  }
  const typ = add(c, AL('Typography', { gap: 8, w: 520 }));
  await TX(typ, 'Typography', 'Button', 'Desktop', 'color/text', { name: 'Heading' });
  await TX(typ, 'Fredoka + Nunito (rounded, friendly)', 'Caption', 'Desktop', 'color/text-muted');
  await TX(typ, 'Aa Bb Cc Dd Ee Ff Gg', 'Title', 'Desktop', 'color/text');
  await TX(typ, '1234567890  V I R Ω', 'Measure', 'Desktop', P.cyan);
  for (const r of [['Title — bold and friendly', 'Panel'], ['Heading — clear and rounded', 'Button'], ['Body — easy to read', 'Body'], ['UI label — clean', 'Label']]) await TX(typ, r[0], r[1], 'Desktop', 'color/text');
  const ic = add(c, AL('Icon style', { gap: 12, w: 420 }));
  await TX(ic, 'Icon style', 'Button', 'Desktop', 'color/text', { name: 'Heading' });
  const ig = add(ic, AL('Icons', { dir: 'HORIZONTAL', gap: 16, w: 420 })); ig.layoutWrap = 'WRAP'; ig.counterAxisSpacing = 16;
  for (const q of [['Home', 'Amber', 'Home'], ['Lab', 'Cyan', 'Lab'], ['Data', 'Cyan', 'Data'], ['Trophy', 'Gold', 'Quiz'], ['Help', 'Cyan', 'Help'], ['Settings', 'Glass', 'Settings'], ['Audio', 'Glass', 'Audio'], ['Star', 'Amber', 'Achievement']]) {
    const cell = add(ig, AL(q[2], { cross: 'CENTER', gap: 6, w: 90 }));
    add(cell, iconTile(q[1], 'Desktop', q[0]));
    await TX(cell, q[2], 'Micro', 'Desktop', 'color/text-muted', { align: 'CENTER' });
  }
  await TX(ic, 'Rounded, colourful, one stroke family. Never emoji.', 'Micro', 'Desktop', 'color/text-muted');
  const ps = add(c, AL('Panel style', { gap: 12 }));
  await TX(ps, 'Panel style', 'Button', 'Desktop', 'color/text', { name: 'Heading' });
  add(ps, inst('Kids/Glass Panel', { Tone: 'Active' }));
  await finishSection(s);

  s = await sheetSection(row, 2, 'Scene match overview', 1396);
  await TX(s, 'UI that feels native to each gameplay environment', 'Caption', 'Desktop', 'color/text-muted');
  c = wrapRow(s);
  for (const n of [['D1 · Historical room', 'Historical room (Ohm’s study)'], ['D2 · Electric conductor', 'Electric conductor (inside the wire)'], ['D3 · Classroom experiment', 'Classroom lab']]) {
    const th = thumb(n[0], 424); if (!th) continue;
    const cell = add(c, AL(n[1], { gap: 8 })); add(cell, th);
    await TX(cell, n[1], 'Button', 'Desktop', 'color/text');
  }
  await finishSection(s);

  // Row B — HUD, objective, dialogue
  row = add(sh, AL('Row B', { dir: 'HORIZONTAL', gap: 24 }), { fillW: true });
  s = await sheetSection(row, 3, 'HUD / lesson bar', 1300);
  c = wrapRow(s);
  add(c, inst('Kids/Lesson Bar', { Progress: '60', Size: 'Desktop' }));
  add(c, inst('Kids/Toolbar', { Size: 'Desktop' }));
  await finishSection(s);
  s = await sheetSection(row, 4, 'Objective / instruction panels', 1220);
  c = wrapRow(s);
  add(c, inst('Kids/Objective Card', { Kind: 'Objective', Size: 'Desktop' }));
  const tt = inst('Kids/Objective Card', { Kind: 'Try This', Size: 'Desktop' }); setProps(tt, { Title: 'Raise the voltage step by step.', Body: 'Watch what the current does each time.' }); add(c, tt);
  await finishSection(s);
  s = await sheetSection(row, 5, 'Dialogue panel with Wattson', 952);
  add(s, inst('Kids/Dialogue', { Kind: 'Panel', Size: 'Desktop' }));
  await TX(s, 'Small steps lead to big discoveries.', 'Caption', 'Desktop', P.gold);
  await finishSection(s);

  // Row C — experiment, quiz, data
  row = add(sh, AL('Row C', { dir: 'HORIZONTAL', gap: 24 }), { fillW: true });
  s = await sheetSection(row, 6, 'Experiment UI', 1232);
  c = wrapRow(s);
  add(c, inst('Kids/Voltage Control', { Value: '3 V', State: 'Default', Size: 'Desktop' }));
  const mc = add(c, AL('Measured', { gap: 16 }));
  add(mc, inst('Kids/Reading', { Quantity: 'Current', Size: 'Desktop' }));
  add(mc, button('Primary', 'Desktop', 'Record reading', { lead: 'Data' }), { fillW: true });
  add(c, inst('Kids/Checklist', { Size: 'Desktop' }));
  await finishSection(s);
  s = await sheetSection(row, 7, 'Quiz UI', 1096);
  c = wrapRow(s);
  const q = inst('Kids/Question Card', { Size: 'Desktop' });
  const opts = q.findAll(n => n.type === 'INSTANCE' && n.name.indexOf('Option ') === 0);
  setProps(opts[0], { State: 'Correct' }); setProps(opts[1], { State: 'Hover' });
  add(c, q);
  add(c, inst('Kids/Feedback Card', { Kind: 'Correct', Size: 'Desktop' }));
  await finishSection(s);
  s = await sheetSection(row, 8, 'Data & analysis UI', 1144);
  add(s, inst('Kids/Tabs', { Selected: 'Graph', Size: 'Desktop' }));
  c = wrapRow(s);
  add(c, inst('Kids/Graph', { Size: 'Desktop' }));
  add(c, inst('Kids/Formula', { Size: 'Desktop' }));
  await finishSection(s);

  // Row D — materials, pop-ups, navigation
  row = add(sh, AL('Row D', { dir: 'HORIZONTAL', gap: 24 }), { fillW: true });
  s = await sheetSection(row, 9, 'Material comparison UI', 932);
  c = add(s, AL('Compare', { dir: 'HORIZONTAL', cross: 'CENTER', gap: 24 }));
  add(c, inst('Kids/Material Card', { Material: 'Copper', Size: 'Desktop' }));
  add(c, inst('Kids/VS Badge'));
  add(c, inst('Kids/Material Card', { Material: 'Nichrome', Size: 'Desktop' }));
  await finishSection(s);
  s = await sheetSection(row, 10, 'Pop-ups & feedback', 1752);
  c = wrapRow(s);
  for (const k of ['Well Done', 'Try Again', 'Hint', 'Achievement', 'Module Complete']) add(c, inst('Kids/Feedback Card', { Kind: k, Size: 'Tablet' }));
  await finishSection(s);
  s = await sheetSection(row, 11, 'Navigation / icon row', 788);
  c = add(s, AL('Nav', { dir: 'HORIZONTAL', gap: 22 }));
  const nav = [['Home', 'Home', P.amber], ['Lab', 'Lab', P.cyan], ['Data', 'Data', P.blue], ['Quiz', 'Trophy', P.gold], ['Help', 'Help', P.blue], ['Settings', 'Settings', P.cyan]];
  for (let i = 0; i < nav.length; i++) {
    const n = inst('Kids/Nav Tile', { Selected: i === 0 ? 'Yes' : 'No', Size: 'Desktop' });
    setProps(n, { Label: nav[i][0], Icon: 'Kids/Icon/' + nav[i][1] });
    if (i) tint(n.findOne(x => x.name === 'Icon'), nav[i][2]);
    add(c, n);
  }
  await TX(s, 'Explore  •  Experiment  •  Learn  •  Grow', 'Label', 'Desktop', P.cyan);
  await finishSection(s);

  // Row E — responsive, readings + hero, title + ask
  row = add(sh, AL('Row E', { dir: 'HORIZONTAL', gap: 24 }), { fillW: true });
  s = await sheetSection(row, 12, 'Responsive layout examples', 1600);
  await TX(s, 'Consistent, clear and playable across devices: the same components, desktop and tablet sizes.', 'Caption', 'Desktop', 'color/text-muted');
  c = wrapRow(s);
  for (const n of [['D3 · Classroom experiment', 'Desktop (16:9)', 820], ['T3 · Classroom experiment', 'Tablet (4:3)', 600]]) {
    const th = thumb(n[0], n[2]); if (!th) continue;
    const cell = add(c, AL(n[1], { gap: 8 })); await TX(cell, n[1], 'Button', 'Desktop', 'color/text'); add(cell, th);
  }
  await finishSection(s);
  s = await sheetSection(row, 13, 'Readings & discoveries', 1100);
  c = wrapRow(s);
  for (const qn of ['Voltage', 'Current', 'Resistance']) add(c, inst('Kids/Reading', { Quantity: qn, Size: 'Desktop' }));
  add(c, inst('Kids/Hero', { Size: 'Desktop' }));
  await finishSection(s);
  s = await sheetSection(row, 14, 'Title & Ask Wattson', 772);
  add(s, inst('Kids/Title Card', { Size: 'Tablet' }));
  c = wrapRow(s, 32);
  for (const st of ['Default', 'Hover', 'Listening']) add(c, inst('Kids/Ask Wattson', { State: st, Size: 'Desktop' }));
  await finishSection(s);

  // Equal heights per row
  for (const r of sh.children.filter(n => n.name.indexOf('Row ') === 0)) {
    const hmax = Math.max.apply(null, r.children.map(n => n.height));
    for (const n of r.children) { n.layoutSizingVertical = 'FIXED'; n.resize(n.width, hmax); }
  }
  log('Component sheet: 14 sections');
}
