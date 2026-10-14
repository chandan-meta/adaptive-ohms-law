// Part 4 — gameplay mockups: seven moments of the module, desktop (1920×1080) and tablet (1024×768), on HUD-free frames
// captured from the game (BlockoutPlayTest "clean"), built only from library instances. Text is the game's own.

const BAR = {
  historical: ['Chapter 1 of 10 · Meet Wattson', 'Step 2 of 4', 'Listen to the case', '50'],
  conductor: ['Chapter 2 of 10 · Journey Inside the Wire', 'Step 4 of 6', 'Apply a voltage', '70'],
  experiment: ['Chapter 7 of 10 · Become the Experimenter', 'Step 3 of 5', 'Set the battery to 3 V', '60'],
  graph: ['Chapter 8 of 10 · The Pattern', 'Step 1 of 3', 'Draw the line', '30'],
  materials: ['Chapter 9 of 10 · Discover Resistance', 'Step 3 of 6', 'Quick check', '50'],
  quiz: ['Chapter 7 of 10 · Become the Experimenter', 'Step 3 of 5', 'Check your prediction', '60'],
  complete: ['Chapter 10 of 10 · Ohm’s Law', 'Step 4 of 4', 'Case closed', '100'],
};

function chrome(f, S, key) {
  const m = S === 'Desktop' ? 32 : 20, b = BAR[key];
  const lb = inst('Kids/Lesson Bar', { Progress: b[3], Size: S });
  setProps(lb, { Chapter: b[0], Step: b[1], Task: b[2] });
  place(f, lb, m, m);
  const tb = inst('Kids/Toolbar', { Size: S });
  place(f, tb, f.width - m - tb.width, m);
  return { m: m, W: f.width, H: f.height, lb: lb, tb: tb, top: Math.max(lb.y + lb.height, tb.y + tb.height) };
}
function ask(f, c, S) { const a = inst('Kids/Ask Wattson', { State: 'Default', Size: S }); return place(f, a, c.W - c.m - a.width, c.H - c.m - a.height); }
function dialogue(f, c, S, line, w) { const d = inst('Kids/Dialogue', { Kind: 'Panel', Size: S }); setProps(d, { Line: line }); if (w) d.resize(w, d.height); return place(f, d, c.m, c.H - c.m - d.height); }
function bubble(f, S, line, x, y, w) { const d = inst('Kids/Dialogue', { Kind: 'Bubble', Size: S }); setProps(d, { Line: line }); if (w) d.resize(w, d.height); return place(f, d, x, y); }
function objective(f, kind, S, title, body, x, y) { const o = inst('Kids/Objective Card', { Kind: kind, Size: S }); setProps(o, { Title: title, Body: body }); return place(f, o, x, y); }
function readings(f, c, S, list) {
  const row = AL('Readings', { dir: 'HORIZONTAL', gap: S === 'Desktop' ? 16 : 10 });
  for (const r of list) { const i = inst('Kids/Reading', { Quantity: r[0], Size: S }); setProps(i, { Value: r[1], Delta: r[2] }); i.name = r[0]; add(row, i); }
  f.appendChild(row);
  const left = c.lb.x + c.lb.width + 24, right = c.tb.x - 24;
  if (right - left >= row.width) { row.x = left + (right - left - row.width) / 2; row.y = c.m; }
  else { row.x = (c.W - row.width) / 2; row.y = c.top + (S === 'Desktop' ? 20 : 12); }
  return row;
}
function scrim(f) { const r = rect(f.width, f.height, null); r.name = 'Scrim'; f.appendChild(r); return fillStyle(r, 'Kids/Scrim'); }

async function screen(key, S, f) {
  const c = chrome(f, S, key), D = S === 'Desktop';
  if (key === 'historical') {
    objective(f, 'Try This', S, 'Look around Ohm’s study.', 'Drag to turn your head. Find the glowing wire on his desk.', c.W - c.m - SZ[S].card, c.top + 20);
    dialogue(f, c, S, 'Your Christmas lights gave you a question. Nearly two centuries ago, Georg Ohm was investigating the relationship that can help us answer it.');
    ask(f, c, S);
  }
  if (key === 'conductor') {
    const cc = inst('Kids/Counter Card', { Size: S }); place(f, cc, c.m, c.top + 20);
    objective(f, 'Objective', S, 'Apply a voltage.', 'Tap the + terminal to connect the battery.', c.W - c.m - SZ[S].card, c.top + 20);
    dialogue(f, c, S, 'Between the two ends there is now a difference in electric potential. That push across the wire is the voltage, written V.');
    ask(f, c, S);
  }
  if (key === 'experiment' || key === 'quiz') {
    const rd = readings(f, c, S, [['Voltage', '3.0 V', '▲ was 2.0 V'], ['Current', '0.60 A', '▲ was 0.40 A']]);
    const below = Math.max(c.top, rd.y + rd.height) + (D ? 20 : 12); // first free line under the HUD and the readings
    const vc = inst('Kids/Voltage Control', { Value: '3 V', State: key === 'experiment' ? 'Focus' : 'Disabled', Size: S });
    place(f, vc, c.m, c.H - c.m - vc.height);
    const rec = button('Primary', S, 'Record reading', { lead: 'Data', state: key === 'quiz' ? 'Disabled' : 'Default' });
    rec.name = 'Record reading';
    place(f, rec, vc.x + vc.width + (D ? 24 : 12), vc.y + vc.height - rec.height - (D ? 40 : 28));
    if (key === 'experiment') {
      const cl = inst('Kids/Checklist', { Size: S });
      const a = ask(f, c, S);
      if (D) place(f, cl, a.x - 24 - cl.width, c.H - c.m - cl.height);
      else place(f, cl, c.W - c.m - cl.width, a.y - 12 - cl.height); // tablet: above Ask Wattson, clear of the dock
      if (D) {
        const ob = objective(f, 'Objective', S, 'Set the battery to 3 V.', 'Slide the battery, then watch the meters.', c.W - c.m - SZ[S].card, below);
        const t = inst('Kids/Data Table', { Size: S });
        const rows = t.findAll(n => n.type === 'INSTANCE' && n.name.indexOf('Row ') === 0);
        setProps(rows[2], { State: 'New' });
        setProps(rows[3], { I: '—', R: '—' });
        setProps(rows[4], { State: 'Default', I: '—', R: '—' });
        place(f, t, c.W - c.m - t.width, ob.y + ob.height + 20);
        bubble(f, S, 'Now set the battery to 3 volts. Watch the ammeter.', 640, 190);
      } else {
        objective(f, 'Objective', S, 'Set the battery to 3 V.', 'Slide the battery, then watch the meters.', c.W - c.m - SZ[S].card, below);
      }
    } else {
      const q = inst('Kids/Question Card', { Size: S });
      setProps(q, { Question: 'At 3 volts, what will the ammeter read?', Count: 'Question 2 of 3' });
      const opts = q.findAll(n => n.type === 'INSTANCE' && n.name.indexOf('Option ') === 0);
      const ans = [['0.40 A', 'Disabled'], ['0.60 A', 'Correct'], ['0.80 A', 'Disabled']];
      for (let i = 0; i < 3; i++) setProps(opts[i], { State: ans[i][1], Letter: 'ABC'.charAt(i), Answer: ans[i][0] });
      q.findOne(n => n.name === 'Dot 2').fills = [solid(P.gold)];
      setTexts(q, { Hint: 'You picked B before setting 3 V. The ammeter agrees.' });
      place(f, q, c.W - c.m - q.width, below + (D ? 40 : 0));
      const fb = inst('Kids/Feedback Card', { Kind: 'Correct', Size: S });
      setTexts(fb, { Title: 'Spot on!', Body: 'Three volts, zero point six amperes: the pattern holds.' });
      setProps(fb.findOne(n => n.type === 'INSTANCE' && n.name.indexOf('Button') === 0), { Label: 'Next: 4 volts' });
      place(f, fb, q.x - (D ? 24 : 12) - fb.width, q.y + (D ? 60 : 30));
      if (D) ask(f, c, S);
    }
  }
  if (key === 'graph') {
    const view = AL('Data view', { gap: 16, pad: D ? 24 : 16, radius: 28 });
    const tabs = inst('Kids/Tabs', { Selected: 'Graph', Size: S }); add(view, tabs);
    const row = add(view, AL('Row', { dir: 'HORIZONTAL', gap: 16 }));
    add(row, inst('Kids/Graph', { Size: S }));
    const side = add(row, AL('Side', { gap: 16 }));
    add(side, inst('Kids/Formula', { Size: S }));
    if (D) {
      const an = inst('Kids/Glass Panel', { Tone: 'Cyan' }); setProps(an, { Title: 'Analysis', Body: 'Five points, one straight line through zero. Double the current and the voltage doubles too: V ∝ I.' });
      an.resize(380, an.height); add(side, an);
    }
    f.appendChild(view); await glass(view, 'Active');
    view.x = c.W - c.m - view.width; view.y = c.top + (D ? 24 : 12);
    if (D) bubble(f, S, 'Now we’ve got our biggest clue. They’re all in a line!', 360, 170);
    else bubble(f, S, 'They’re all in a line!', c.m, c.H - c.m - 90, 260);
    if (D) ask(f, c, S);
  }
  if (key === 'materials') {
    await scrim(f);
    f.appendChild(c.lb); f.appendChild(c.tb); // keep the HUD above the scrim
    const pop = AL('Pop-up · Copper vs nichrome', { gap: D ? 20 : 12, pad: D ? 28 : 16, radius: 28 });
    const head = add(pop, AL('Header', { dir: 'HORIZONTAL', cross: 'CENTER', gap: 12 }), {});
    add(head, icon('Lab', D ? 26 : 22, P.cyan));
    const ht = add(head, AL('Titles', { gap: 0 }));
    await TX(ht, 'Copper vs nichrome', 'Title', S, 'color/text', { name: 'Title' });
    await TX(ht, 'Same voltage · same length · same thickness', D ? 'Caption' : 'Micro', S, 'color/text-muted', { name: 'Subtitle' });
    const body = add(pop, AL('Compare', { dir: 'HORIZONTAL', cross: 'CENTER', gap: D ? 24 : 12 }));
    for (const m of ['Copper', 'VS', 'Nichrome']) {
      const card = add(body, m === 'VS' ? inst('Kids/VS Badge') : inst('Kids/Material Card', { Material: m, Size: S }));
      if (!D && m !== 'VS') card.findOne(n => n.name === 'Facts').visible = false; // tablet: the chip carries the resistance, the facts stay in narration
    }
    head.layoutSizingHorizontal = 'FILL';
    const close = inst('Kids/Icon Button', { State: 'Default', Size: S }); setProps(close, { Icon: 'Kids/Icon/Close', Label: 'Close' });
    spacer(head); add(head, close);
    f.appendChild(pop); await glass(pop, 'Active');
    const q = inst('Kids/Question Card', { Size: S });
    setProps(q, { Question: 'Same voltage, less current. Which wire has the higher resistance?', Count: 'Question 3 of 3' });
    const opts = q.findAll(n => n.type === 'INSTANCE' && n.name.indexOf('Option ') === 0);
    setProps(opts[0], { Letter: 'A', Answer: 'Copper' });
    setProps(opts[1], { State: 'Selected', Letter: 'B', Answer: 'Nichrome' });
    opts[2].visible = false;
    for (const d of ['Dot 1', 'Dot 2', 'Dot 3']) q.findOne(n => n.name === d).fills = [solid(P.gold)];
    setTexts(q, { Hint: 'Tap an answer, or press 1 or 2.' });
    f.appendChild(q);
    if (D) {
      const total = pop.width + 24 + q.width;
      pop.x = (c.W - total) / 2; pop.y = c.top + 60; q.x = pop.x + pop.width + 24; q.y = pop.y + (pop.height - q.height) / 2;
      dialogue(f, c, S, 'Quick check, detective. Same voltage, less current. Which wire has the higher resistance?', 1100);
    } else {
      pop.x = (c.W - pop.width) / 2; pop.y = c.top + 8;
      q.x = (c.W - q.width) / 2; q.y = pop.y + pop.height + 12;
    }
  }
  if (key === 'complete') {
    const p = AL('Case closed', { gap: D ? 16 : 10, pad: D ? 28 : 18, w: D ? 560 : 430, radius: 28 });
    const head = add(p, AL('Header', { dir: 'HORIZONTAL', cross: 'CENTER', gap: 14 }));
    const em = add(head, AL('Emblem', { main: 'CENTER', cross: 'CENTER', w: D ? 64 : 48, h: D ? 64 : 48, radius: 999 }));
    await fillStyle(em, 'Kids/Fill Gold'); await fx(em, 'Kids/Glow Gold Strong'); add(em, icon('Trophy', D ? 36 : 28, P.ink));
    const ht = add(head, AL('Titles', { gap: 0 }));
    await TX(ht, 'Case closed!', 'Title', S, P.gold, { name: 'Title' });
    await TX(ht, 'You discovered Ohm’s law', D ? 'Caption' : 'Micro', S, 'color/text-muted', { name: 'Subtitle' });
    const three = add(p, AL('Three pieces', { dir: 'HORIZONTAL', gap: D ? 12 : 8 }), { fillW: true });
    for (const q of [['V', 'The push', 'potential difference, volts', P.blue], ['I', 'The flow', 'current, amperes', P.gold], ['R', 'The opposition', 'resistance, ohms', P.coral]]) {
      const card = add(three, AL('Piece ' + q[0], { gap: 2, pad: D ? 14 : 10, radius: 16 }), { fillW: true });
      await TX(card, q[0], 'Title', S, q[3], { name: 'Letter' });
      await TX(card, q[1], 'Button', S, 'color/text', { name: 'Word' });
      await TX(card, q[2], 'Micro', S, 'color/text-muted', { name: 'Meaning', fillW: true });
      await fillStyle(card, 'Kids/Well'); card.strokes = [solid(q[3], 0.8)]; card.strokeWeight = 1.5; card.strokeAlign = 'INSIDE';
    }
    const law = add(p, AL('Law', { main: 'CENTER', cross: 'CENTER', pad: [8, 16], radius: 16 }), { fillW: true });
    await fillStyle(law, 'Kids/Well');
    const lt = await TX(law, 'V = I × R', 'Measure', S, 'color/text', { name: 'Formula', align: 'CENTER' }); runs(lt, [['V', P.blue], ['I', P.gold], ['R', P.coral]]);
    const wire = await TX(p, 'Your nichrome wire: 5.0 V ÷ 1.00 A = 5.0 Ω', D ? 'Caption' : 'Micro', S, 'color/text', { name: 'Your wire' });
    runs(wire, [['5.0 V', P.blue], ['1.00 A', P.gold], ['5.0 Ω', P.coral]]);
    add(p, button('Primary', S, 'Replay the investigation', { lead: 'Replay' }), { fillW: true });
    const row = add(p, AL('More', { dir: 'HORIZONTAL', gap: 12 }), { fillW: true });
    add(row, button('Secondary', S, 'Chapters', { lead: 'Chapters' }), { fillW: true });
    add(row, button('Secondary', S, 'Exit', { lead: 'Exit' }), { fillW: true });
    f.appendChild(p); await glass(p, 'Gold');
    p.x = c.W - c.m - p.width; p.y = c.top + (D ? 24 : 8);
    const ach = inst('Kids/Feedback Card', { Kind: 'Achievement', Size: S });
    place(f, ach, c.m, c.top + (D ? 24 : 8));
    if (D) dialogue(f, c, S, 'You’ve followed the charges, measured the current, and discovered the pattern. Voltage, current and resistance are connected by one law.', 1000);
  }
}

async function screens() {
  const pg = await page(SCREENS_PAGE, 'Desktop');
  clear(pg, 'screens');
  const gapX = 160, gapY = 240;
  const h1 = own(await heading(pg, 'Desktop 16:9 · 1920 × 1080 — seven gameplay moments, real frames, HUD hidden', 0, -220, P.gold), 'screens'); h1.fontSize = 56;
  for (let i = 0; i < SCREENS.length; i++) {
    const s = SCREENS[i], x = (i % 4) * (1920 + gapX), y = Math.floor(i / 4) * (1080 + gapY);
    const f = own(figma.createFrame(), 'screens'); f.name = 'D' + s[1]; f.resize(1920, 1080); f.x = x; f.y = y; f.clipsContent = true;
    f.fills = [imgPaint('d_' + s[0])]; pg.appendChild(f);
    own(await label(pg, s[1] + ' — ' + s[2], x, y - 48, 1900, P.text), 'screens').fontSize = 28;
    await screen(s[0], 'Desktop', f);
    log('Desktop ' + s[1]);
  }
  const ty = 2 * (1080 + gapY) + 200;
  const h2 = own(await heading(pg, 'Tablet 4:3 · 1024 × 768 — the same moments, tablet components (48 px targets, larger relative type)', 0, ty - 220, P.gold), 'screens'); h2.fontSize = 56;
  for (let i = 0; i < SCREENS.length; i++) {
    const s = SCREENS[i], x = (i % 4) * (1024 + 120) * 1, y = ty + Math.floor(i / 4) * (768 + 200);
    const f = own(figma.createFrame(), 'screens'); f.name = 'T' + s[1]; f.resize(1024, 768); f.x = x; f.y = y; f.clipsContent = true;
    f.fills = [imgPaint('t_' + s[0])]; pg.appendChild(f);
    own(await label(pg, s[1] + ' — ' + s[2], x, y - 40, 1000, P.text), 'screens');
    await screen(s[0], 'Tablet', f);
    log('Tablet ' + s[1]);
  }
}
