// Part 3b — panels: Lesson Bar, Toolbar, Objective Card, Dialogue, Voltage Control, Checklist, Question Card, Feedback
// Card, Table Row, Data Table, Graph, Formula, Material Card, Hero, Counter Card, Ask Wattson, Title Card. Built from the
// controls in part 3a; sample text is the game's own (Scenes 4–13).

async function panels() {
  const pg = await page(DS_PAGE, 'Components');
  const b = board(pg, 'Kids UI / 04 Components · Panels', 6800, 0, 3200, 1000);
  CY = 64;
  const D = 'Desktop';

  // Lesson Bar
  const STEPS = ['10', '20', '30', '40', '50', '60', '70', '80', '90', '100'];
  let set = await variantSet(b, 'Kids/Lesson Bar', { Progress: STEPS, Size: SIZES }, async (p) => {
    const T = p.Size === 'Tablet', t = Number(p.Progress) / 100;
    const c = AL('Lesson Bar', { component: true, dir: 'HORIZONTAL', cross: 'CENTER', gap: T ? 12 : 16, pad: T ? [8, 16, 8, 8] : [12, 24, 12, 12], radius: 20 });
    add(c, iconTile('Amber', p.Size, 'Bolt'));
    const title = add(c, AL('Title', { gap: 0 }));
    await TX(title, 'Ohm’s law', 'Label', p.Size, P.gold, { name: 'Module' });
    await TX(title, 'Chapter 7 of 10 · Become the Experimenter', T ? 'Micro' : 'Caption', p.Size, 'color/text', { name: 'Chapter' });
    add(c, rect(1.5, T ? 32 : 40, solid(P.blue, 0.35))).name = 'Divider';
    const prog = add(c, AL('Progress', { gap: 6 }));
    const row = add(prog, AL('Row', { dir: 'HORIZONTAL', cross: 'CENTER', gap: 10 }));
    await TX(row, 'Step 3 of 5', 'Button', p.Size, 'color/text', { name: 'Step' });
    await TX(row, 'Set the battery to 3 V', T ? 'Micro' : 'Caption', p.Size, 'color/text-muted', { name: 'Task' });
    const W = T ? 200 : 260;
    const track = add(prog, figma.createFrame()); track.name = 'Track'; track.resize(W, 12); track.fills = []; track.clipsContent = false;
    const bg = rect(W, 10, solid(P.navy, 0.8), 999); bg.y = 1; bg.strokes = [solid(P.blue, 0.4)]; bg.strokeWeight = 1; track.appendChild(bg); bg.name = 'Rail';
    const fill = rect(W * t, 10, grad([[0, P.blue], [1, P.cyan]], 1, HORZ), 999); fill.y = 1; fill.name = 'Fill'; fill.effects = [shadow(P.blue, 0.7, 8)]; track.appendChild(fill);
    const knob = ellipse(18, solid(P.gold)); knob.x = W * t - 9; knob.y = -3; knob.name = 'Knob'; knob.strokes = [solid(P.white, 0.8)]; knob.strokeWeight = 2; knob.effects = [shadow(P.amber, 0.9, 10)]; track.appendChild(knob);
    await glass(c, 'Glass');
    return c;
  }, { width: 3072, gap: 24 });
  textProp(set, 'Module', 'Module', 'Ohm’s law');
  textProp(set, 'Chapter', 'Chapter', 'Chapter 7 of 10 · Become the Experimenter');
  textProp(set, 'Step', 'Step', 'Step 3 of 5');
  textProp(set, 'Task', 'Task', 'Set the battery to 3 V');
  await placeSet(b, set, 'Lesson Bar', 'Top left, always: module, chapter n of 10 with its name, step n of m with the task, and the step progress (Progress = percent).');

  // Toolbar
  set = await variantSet(b, 'Kids/Toolbar', { Size: SIZES }, async (p) => {
    const c = AL('Toolbar', { component: true, dir: 'HORIZONTAL', gap: p.Size === 'Desktop' ? 12 : 8 });
    for (const t of [['Pause', 'Pause', 'Default'], ['Replay', 'Replay', 'Default'], ['Captions', 'Captions', 'Active'], ['Audio', 'Audio', 'Default'], ['Skip', 'Skip', 'Default']]) {
      const i = inst('Kids/Icon Button', { State: t[2], Size: p.Size }); setProps(i, { Icon: 'Kids/Icon/' + t[1], Label: t[0] }); i.name = t[0]; add(c, i);
    }
    return c;
  });
  await placeSet(b, set, 'Toolbar', 'Top right: the game’s five controls with labels. Captions shows Active while captions are on.');

  // Objective Card
  const OBJ = {
    Objective: { tone: 'Active', tile: ['Amber', 'Target'], eyebrow: 'Your objective', ec: P.gold },
    'Try This': { tone: 'Glass', tile: ['Gold', 'Bulb'], eyebrow: 'Try this!', ec: P.gold },
    Done: { tone: 'Success', tile: ['Success', 'Check'], eyebrow: 'Done', ec: P.green },
  };
  set = await variantSet(b, 'Kids/Objective Card', { Kind: Object.keys(OBJ), Size: SIZES }, async (p) => {
    const s = SZ[p.Size], O = OBJ[p.Kind];
    const c = AL('Objective Card', { component: true, dir: 'HORIZONTAL', cross: 'CENTER', gap: s.gap, pad: s.pad, w: s.card, radius: 20 });
    add(c, iconTile(O.tile[0], p.Size, O.tile[1]));
    const col = add(c, AL('Text', { gap: 4 }), { fillW: true });
    if (p.Kind === 'Try This') {
      const pill = add(col, AL('Eyebrow pill', { pad: [3, 12], radius: 999 }));
      pill.strokes = [solid(P.gold, 0.9)]; pill.strokeWeight = 1.5;
      await TX(pill, O.eyebrow, 'Label', p.Size, O.ec, { name: 'Eyebrow' });
    } else await TX(col, O.eyebrow, 'Label', p.Size, O.ec, { name: 'Eyebrow' });
    await TX(col, 'Set the battery to 3 V.', 'Panel', p.Size, 'color/text', { name: 'Title', fillW: true });
    await TX(col, 'Slide the battery, then watch the meters.', p.Size === 'Desktop' ? 'Caption' : 'Micro', p.Size, 'color/text-muted', { name: 'Body', fillW: true });
    if (p.Kind === 'Objective') {
      const go = add(c, AL('Go', { main: 'CENTER', cross: 'CENTER', w: s.btnH, h: s.btnH, radius: 999 }));
      add(go, icon('Arrow', s.icon, P.ink));
      await fillStyle(go, 'Kids/Fill Amber'); await fx(go, 'Kids/Glow Amber'); go.strokes = [solid(P.white, 0.45)]; go.strokeWeight = 1.5;
    }
    await glass(c, O.tone);
    return c;
  }, { width: 3072 });
  textProp(set, 'Title', 'Title', 'Set the battery to 3 V.');
  textProp(set, 'Body', 'Body', 'Slide the battery, then watch the meters.');
  await placeSet(b, set, 'Objective Card', 'The “Your turn” card. Objective = the action the scene waits for (glowing rim, amber go button); Try this = a nudge; Done = confirmation with what was recorded.');

  // Dialogue
  set = await variantSet(b, 'Kids/Dialogue', { Kind: ['Panel', 'Bubble'], Size: SIZES }, async (p) => {
    const s = SZ[p.Size], T = p.Size === 'Tablet';
    let c;
    if (p.Kind === 'Panel') {
      c = AL('Dialogue', { component: true, dir: 'HORIZONTAL', cross: 'CENTER', gap: s.gap + 4, pad: T ? [12, 20, 12, 12] : [16, 32, 16, 16], w: s.dlg, radius: 24 });
      const face = add(c, ellipse(s.portrait, imgPaint('wattson')));
      face.name = 'Portrait'; face.strokes = [solid(P.cyan)]; face.strokeWeight = 3; face.effects = [shadow(P.blue, 0.7, 18)];
      const col = add(c, AL('Text', { gap: 2 }), { fillW: true });
      await TX(col, 'Wattson', 'Panel', p.Size, P.gold, { name: 'Name' });
      await TX(col, 'Your Christmas lights gave you a question. Nearly two centuries ago, Georg Ohm was investigating the relationship that can help us answer it.', 'Body', p.Size, 'color/text', { name: 'Line', fillW: true });
      await glass(c, 'Glass');
    } else {
      c = AL('Dialogue', { component: true, gap: 2, pad: T ? [10, 14] : [14, 20], w: s.bubble, radius: 20 });
      await TX(c, 'Wattson', 'Label', p.Size, P.cyan, { name: 'Name' });
      await TX(c, 'Let’s start low. Set the battery to 1 volt.', 'Body', p.Size, 'color/text', { name: 'Line', fillW: true });
      await glass(c, 'Glass');
      const tail = figma.createVector();
      tail.vectorPaths = [{ windingRule: 'NONZERO', data: 'M 0 0 L 30 0 L 4 22 Z' }];
      tail.fills = [solid('#0E1B30', 0.92)]; tail.strokes = []; tail.name = 'Tail';
      c.appendChild(tail); tail.layoutPositioning = 'ABSOLUTE'; tail.x = 28; tail.y = c.height - 2;
      tail.constraints = { horizontal: 'MIN', vertical: 'MAX' };
    }
    return c;
  }, { width: 3072 });
  textProp(set, 'Name', 'Name', 'Wattson');
  textProp(set, 'Line', 'Line', 'Let’s start low. Set the battery to 1 volt.');
  await placeSet(b, set, 'Dialogue', 'Panel = the caption with Wattson’s portrait (bottom left, ≤ 2 lines desktop / 3 tablet). Bubble = a short line next to the character in the scene; the tail points at the speaker.');

  // Voltage Control
  set = await variantSet(b, 'Kids/Voltage Control', { Value: ['1 V', '2 V', '3 V', '4 V', '5 V'], State: ['Default', 'Focus', 'Disabled'], Size: SIZES }, async (p) => {
    const s = SZ[p.Size], T = p.Size === 'Tablet', W = T ? 328 : 420, v = parseInt(p.Value, 10), t = v / 5;
    const c = AL('Voltage Control', { component: true, gap: 14, pad: s.pad, w: W + 2 * s.pad, radius: 20 });
    const head = add(c, AL('Header', { dir: 'HORIZONTAL', cross: 'CENTER', gap: 10 }), { fillW: true });
    add(head, icon('Voltmeter', T ? 22 : 26, P.blue));
    await TX(head, 'Set the battery', 'Panel', p.Size, 'color/text', { name: 'Title' });
    spacer(head);
    await TX(head, '0–5 V', 'Label', p.Size, 'color/text-muted', { name: 'Range' });
    const row = add(c, AL('Stepper', { dir: 'HORIZONTAL', cross: 'CENTER', gap: 12 }), { fillW: true });
    const minus = add(row, iconTile('Cyan', p.Size, 'Minus')); minus.name = 'Minus';
    const well = add(row, AL('Value', { main: 'CENTER', cross: 'CENTER', h: s.tile, radius: 14 }), { fillW: true });
    await fillStyle(well, 'Kids/Well'); well.strokes = [solid(P.blue, 0.5)]; well.strokeWeight = 1.5; well.strokeAlign = 'INSIDE';
    await TX(well, v.toFixed(1) + ' V', 'Title', p.Size, P.blue, { name: 'Voltage', align: 'CENTER' });
    const plus = add(row, iconTile('Amber', p.Size, 'Plus')); plus.name = 'Plus';
    const slider = add(c, figma.createFrame()); slider.name = 'Slider'; slider.resize(W, 32); slider.fills = []; slider.clipsContent = false;
    const rail = rect(W, 12, solid(P.navy, 0.85), 999); rail.y = 10; rail.strokes = [solid(P.blue, 0.45)]; rail.strokeWeight = 1; rail.name = 'Rail'; slider.appendChild(rail);
    const f = rect(W * t, 12, grad([[0, P.blue], [1, P.cyan]], 1, HORZ), 999); f.y = 10; f.name = 'Fill'; f.effects = [shadow(P.blue, 0.8, 10)]; slider.appendChild(f);
    const k = ellipse(32, grad([[0, P.white], [1, P.cyan]])); k.x = Math.min(W - 32, W * t - 16); k.y = 0; k.name = 'Knob';
    k.effects = [shadow(P.blue, 0.9, 14), shadow(P.shade, 0.4, 6, 3)];
    if (p.State === 'Focus') { k.strokes = [solid(P.cyan)]; k.strokeWeight = 3; k.strokeAlign = 'OUTSIDE'; }
    slider.appendChild(k);
    const ticks = add(c, AL('Ticks', { dir: 'HORIZONTAL', main: 'SPACE_BETWEEN' }), { fillW: true });
    for (let i = 0; i <= 5; i++) await TX(ticks, i + ' V', 'Micro', p.Size, i === v ? P.blue : 'color/text-muted', { name: 'Tick ' + i });
    const help = add(c, AL('Help', { dir: 'HORIZONTAL', cross: 'CENTER', gap: 8 }));
    if (p.State === 'Disabled') add(help, icon('Lock', 18, P.muted));
    await TX(help, p.State === 'Disabled' ? 'Listen to Wattson first.' : 'Current is measured, never set.', 'Micro', p.Size, 'color/text-muted', { name: 'Helper' });
    await glass(c, p.State === 'Focus' ? 'Active' : 'Glass');
    if (p.State === 'Disabled') { c.opacity = 0.5; c.effects = []; }
    return c;
  }, { width: 3072 });
  await placeSet(b, set, 'Voltage Control', 'The battery: − / value / + (48 px targets) and a 0–5 V slider with labelled ticks (Value = the setting). Focus rings the knob; Disabled says why.');

  // Checklist
  set = await variantSet(b, 'Kids/Checklist', { Size: SIZES }, async (p) => {
    const s = SZ[p.Size];
    const c = AL('Checklist', { component: true, gap: 12, pad: s.pad, w: p.Size === 'Desktop' ? 380 : 290, radius: 20 });
    await TX(c, 'Your steps', 'Panel', p.Size, 'color/text', { name: 'Title' });
    const items = [['Done', 'Set the voltage'], ['Done', 'Watch the meters'], ['Current', 'Record the reading'], ['To do', 'Look for a pattern']];
    for (let i = 0; i < items.length; i++) { const it = inst('Kids/Checklist Item', { State: items[i][0], Size: p.Size }); setProps(it, { Step: items[i][1] }); it.name = 'Item ' + (i + 1); add(c, it); }
    await glass(c, 'Glass');
    return c;
  });
  await placeSet(b, set, 'Checklist', 'Scene 10’s steps for each voltage. Change each item’s State as the learner goes.');

  // Question Card
  set = await variantSet(b, 'Kids/Question Card', { Size: SIZES }, async (p) => {
    const s = SZ[p.Size], T = p.Size === 'Tablet';
    const c = AL('Question Card', { component: true, gap: 14, pad: T ? 18 : 28, w: T ? 456 : 616, radius: 24 });
    const head = add(c, AL('Header', { dir: 'HORIZONTAL', cross: 'CENTER', gap: 10 }), { fillW: true });
    add(head, icon('Help', T ? 20 : 24, P.cyan));
    await TX(head, 'Quick check', 'Label', p.Size, P.cyan, { name: 'Eyebrow' });
    spacer(head);
    await TX(head, 'Question 1 of 3', T ? 'Micro' : 'Caption', p.Size, 'color/text-muted', { name: 'Count' });
    const dots = add(head, AL('Dots', { dir: 'HORIZONTAL', cross: 'CENTER', gap: 6 }));
    for (let i = 0; i < 3; i++) { const d = add(dots, ellipse(12, i === 0 ? solid(P.gold) : null)); d.name = 'Dot ' + (i + 1); if (i) { d.strokes = [solid(P.muted, 0.7)]; d.strokeWeight = 2; } else d.effects = [shadow(P.amber, 0.8, 8)]; }
    await TX(c, 'If we increase the voltage across the wire, with everything else the same, what happens to the current?', 'Panel', p.Size, 'color/text', { name: 'Question', fillW: true });
    const opts = add(c, AL('Options', { gap: 10 }), { fillW: true });
    const A = [['A', 'Increase'], ['B', 'Decrease'], ['C', 'Stay the same']];
    for (const a of A) { const o = inst('Kids/Quiz Option', { State: 'Default', Size: p.Size }); setProps(o, { Letter: a[0], Answer: a[1] }); o.name = 'Option ' + a[0]; add(opts, o, { fillW: true }); }
    await TX(c, 'Tap an answer, or press 1, 2 or 3. The experiment will decide.', 'Micro', p.Size, 'color/text-muted', { name: 'Hint', fillW: true });
    await glass(c, 'Active');
    return c;
  }, { width: 3072 });
  textProp(set, 'Question', 'Question', 'If we increase the voltage across the wire, with everything else the same, what happens to the current?');
  textProp(set, 'Count', 'Count', 'Question 1 of 3');
  await placeSet(b, set, 'Question Card', 'Prediction and quick checks (Scenes 6, 10, 12 = questions 1–3). Set each option’s State: Selected on tap, then Correct / Wrong.');

  // Feedback Card
  const FB = {
    Correct: { tone: 'Success', emblem: ['Success', 'Check'], title: 'Correct!', body: 'More voltage, more push: the current increases.', btn: ['Success', 'Next question', null, 'Arrow'] },
    'Well Done': { tone: 'Success', emblem: ['Gold', 'Trophy'], title: 'Well done!', body: 'You recorded all five readings. Keep going!', btn: ['Primary', 'Great!', null, null] },
    'Try Again': { tone: 'Error', emblem: ['Error', 'Cross'], title: 'Try again', body: 'Careful: copper let more current through, so it opposes less.', btn: ['Danger', 'Try again', 'Replay', null] },
    Hint: { tone: 'Cyan', emblem: ['Cyan', 'Bulb'], title: 'Hint', body: 'Watch the ammeter as you raise the voltage.', btn: ['Secondary', 'Got it!', null, null] },
    Achievement: { tone: 'Purple', emblem: ['Purple', 'Star'], title: 'Voltage explorer!', body: 'You set every voltage from 1 V to 5 V.', btn: ['Achievement', 'Awesome!', null, null] },
    'Module Complete': { tone: 'Gold', emblem: ['Gold', 'Trophy'], title: 'Case closed!', body: 'You discovered Ohm’s law: V = I × R.', btn: ['Primary', 'Continue', null, 'Arrow'] },
  };
  set = await variantSet(b, 'Kids/Feedback Card', { Kind: Object.keys(FB), Size: SIZES }, async (p) => {
    const F = FB[p.Kind], T = p.Size === 'Tablet', e = T ? 56 : 72;
    const c = AL('Feedback Card', { component: true, cross: 'CENTER', gap: T ? 10 : 12, pad: T ? 20 : 28, w: T ? 320 : 400, radius: 24 });
    const em = add(c, AL('Emblem', { main: 'CENTER', cross: 'CENTER', w: e, h: e, radius: 999 }));
    await fillStyle(em, 'Kids/Fill ' + (F.emblem[0] === 'Success' ? 'Success' : F.emblem[0])); await fx(em, 'Kids/Glow ' + F.emblem[0] + ' Strong');
    em.strokes = [solid(P.white, 0.5)]; em.strokeWeight = 2;
    add(em, icon(F.emblem[1], e * 0.55, P.ink)); sheen(em, e * 0.45, 0.4);
    await TX(c, F.title, 'Title', p.Size, TONE[F.tone].c, { name: 'Title', align: 'CENTER' });
    await TX(c, F.body, 'Body', p.Size, 'color/text', { name: 'Body', align: 'CENTER', fillW: true });
    add(c, button(F.btn[0], p.Size, F.btn[1], { lead: F.btn[2], trail: F.btn[3] }));
    await glass(c, F.tone);
    return c;
  }, { width: 3072 });
  await placeSet(b, set, 'Feedback Card', 'Every result has an emblem, a word and a next step. Wrong answers keep the learner’s pick visible and offer “Try again”.');

  // Table Row + Data Table
  set = await variantSet(b, 'Kids/Table Row', { State: ['Default', 'New', 'Highlight'], Size: SIZES }, async (p) => {
    const T = p.Size === 'Tablet';
    const c = AL('Table Row', { component: true, dir: 'HORIZONTAL', cross: 'CENTER', pad: [0, 16], w: T ? 308 : 412, h: T ? 40 : 46, radius: 10 });
    for (const t of [['V', '3.0 V', P.blue], ['I', '0.60 A', P.gold], ['R', '5 Ω', P.coral]]) await TX(c, t[1], 'Body', p.Size, t[2], { name: t[0], fillW: true });
    if (p.State === 'New') { await fillStyle(c, 'Kids/Card Cyan'); c.strokes = [solid(P.cyan, 0.9)]; c.strokeWeight = 1.5; await fx(c, 'Kids/Glow Cyan'); }
    else if (p.State === 'Highlight') { await fillStyle(c, 'Kids/Card Gold'); c.strokes = [solid(P.gold, 0.9)]; c.strokeWeight = 1.5; }
    else c.fills = [solid(P.navy, 0.35)];
    return c;
  }, { width: 2400 });
  textProp(set, 'V', 'V', '3.0 V'); textProp(set, 'I', 'I', '0.60 A'); textProp(set, 'R', 'R', '5 Ω');
  await placeSet(b, set, 'Table Row', 'New = the row just recorded; Highlight = the rows the narration compares.');

  set = await variantSet(b, 'Kids/Data Table', { Size: SIZES }, async (p) => {
    const s = SZ[p.Size], T = p.Size === 'Tablet';
    const c = AL('Data Table', { component: true, gap: 6, pad: s.pad, radius: 20 });
    const head = add(c, AL('Title', { dir: 'HORIZONTAL', cross: 'CENTER', gap: 10 }));
    add(head, icon('Table', T ? 20 : 24, P.cyan));
    await TX(head, 'Your readings', 'Panel', p.Size, 'color/text', { name: 'Title' });
    const cols = add(c, AL('Columns', { dir: 'HORIZONTAL', pad: [6, 16] }), { fillW: true });
    for (const t of [['Voltage V', P.blue], ['Current I', P.gold], ['V ÷ I', P.coral]]) await TX(cols, t[0], 'Label', p.Size, t[1], { name: t[0], fillW: true });
    const rows = [['1.0 V', '0.20 A'], ['2.0 V', '0.40 A'], ['3.0 V', '0.60 A'], ['4.0 V', '0.80 A'], ['5.0 V', '1.00 A']];
    for (let i = 0; i < rows.length; i++) { const r = inst('Kids/Table Row', { State: i === 4 ? 'New' : 'Default', Size: p.Size }); setProps(r, { V: rows[i][0], I: rows[i][1], R: '5 Ω' }); r.name = 'Row ' + (i + 1); add(c, r); }
    await glass(c, 'Glass');
    return c;
  });
  await placeSet(b, set, 'Data Table', 'The observation table (ObservationTableUI): five readings at 5 Ω, as the game records them.');

  // Graph
  set = await variantSet(b, 'Kids/Graph', { Size: SIZES }, async (p) => {
    const s = SZ[p.Size], T = p.Size === 'Tablet';
    const W = T ? 400 : 560, H = T ? 250 : 330, L = T ? 44 : 56, B = T ? 36 : 44, top = 10, right = 18;
    const c = AL('Graph', { component: true, gap: 10, pad: s.pad, radius: 20 });
    const head = add(c, AL('Title', { dir: 'HORIZONTAL', cross: 'CENTER', gap: 10 }));
    add(head, icon('Graph', T ? 20 : 24, P.cyan));
    await TX(head, 'Voltage vs current', 'Panel', p.Size, 'color/text', { name: 'Title' });
    const plot = add(c, figma.createFrame()); plot.name = 'Plot'; plot.resize(W, H); plot.fills = []; plot.clipsContent = false;
    const px = i => L + (i / 1.2) * (W - L - right), py = v => H - B - (v / 6) * (H - B - top);
    for (let i = 1; i <= 6; i++) { const g = rect(W - L - right, 1, solid(P.blue, 0.14)); g.x = L; g.y = py(i); g.name = 'Grid V ' + i; plot.appendChild(g); }
    for (let i = 1; i <= 6; i++) { const g = rect(1, H - B - top, solid(P.blue, 0.14)); g.x = px(i * 0.2); g.y = top; g.name = 'Grid I ' + i; plot.appendChild(g); }
    const ax = rect(W - L - right + 2, 2, solid(P.muted, 0.8)); ax.x = L - 1; ax.y = H - B; ax.name = 'Axis I'; plot.appendChild(ax);
    const ay = rect(2, H - B - top + 2, solid(P.muted, 0.8)); ay.x = L - 1; ay.y = top; ay.name = 'Axis V'; plot.appendChild(ay);
    for (let i = 0; i <= 6; i++) { const t = await TX(null, (i * 0.2).toFixed(1), 'Micro', p.Size, 'color/text-muted', { name: 'Tick I' }); plot.appendChild(t); t.x = px(i * 0.2) - t.width / 2; t.y = H - B + 6; }
    for (let v = 1; v <= 6; v++) { const t = await TX(null, String(v), 'Micro', p.Size, 'color/text-muted', { name: 'Tick V' }); plot.appendChild(t); t.x = L - 10 - t.width; t.y = py(v) - t.height / 2; }
    const xt = await TX(null, 'Current, I (A)', 'Micro', p.Size, P.gold, { name: 'Axis title I' }); plot.appendChild(xt); xt.x = L + (W - L) / 2 - xt.width / 2; xt.y = H - xt.height + 4;
    const yt = await TX(null, 'Voltage, V (V)', 'Micro', p.Size, P.blue, { name: 'Axis title V' }); plot.appendChild(yt); yt.rotation = 90; yt.x = -2; yt.y = top + (H - B) / 2 + yt.width / 2;
    const line = figma.createVector(); line.name = 'Line';
    line.vectorPaths = [{ windingRule: 'NONE', data: 'M ' + px(0) + ' ' + py(0) + ' L ' + px(1.1) + ' ' + py(5.5) }];
    line.strokes = [solid(P.cyan)]; line.strokeWeight = 4; line.strokeCap = 'ROUND'; line.effects = [shadow(P.blue, 0.9, 12)]; plot.appendChild(line);
    const pts = [[0.2, 1], [0.4, 2], [0.6, 3], [0.8, 4], [1.0, 5]];
    for (const q of pts) {
      const d = ellipse(T ? 12 : 14, solid(P.white)); d.strokes = [solid(P.blue)]; d.strokeWeight = 3; d.effects = [shadow(P.blue, 0.9, 10)];
      d.x = px(q[0]) - d.width / 2; d.y = py(q[1]) - d.height / 2; d.name = 'Point ' + q[1] + ' V'; plot.appendChild(d);
      const t = await TX(null, '(' + q[0].toFixed(2) + ', ' + q[1].toFixed(1) + ')', 'Micro', p.Size, 'color/text', { name: 'Point label' });
      plot.appendChild(t); t.x = px(q[0]) + 10; t.y = py(q[1]) + 4;
    }
    await TX(c, 'A straight line through zero: V ∝ I.', p.Size === 'Desktop' ? 'Caption' : 'Micro', p.Size, 'color/text-muted', { name: 'Caption' });
    await glass(c, 'Glass');
    return c;
  });
  await placeSet(b, set, 'Graph', 'GraphUI: V up, I across (the slope is R, as Chapter 9 says). Line + markers + direct labels; the table is its text alternative.');

  // Formula
  set = await variantSet(b, 'Kids/Formula', { Size: SIZES }, async (p) => {
    const s = SZ[p.Size], T = p.Size === 'Tablet';
    const c = AL('Formula', { component: true, gap: 12, pad: s.pad, w: T ? 300 : 380, radius: 20 });
    const head = add(c, AL('Title', { dir: 'HORIZONTAL', cross: 'CENTER', gap: 10 }));
    add(head, icon('Bolt', T ? 20 : 24, P.gold));
    await TX(head, 'Ohm’s law', 'Panel', p.Size, 'color/text', { name: 'Title' });
    const well = add(c, AL('Well', { main: 'CENTER', cross: 'CENTER', pad: [10, 16], radius: 16 }), { fillW: true });
    await fillStyle(well, 'Kids/Well'); well.strokes = [solid(P.blue, 0.4)]; well.strokeWeight = 1.5; well.strokeAlign = 'INSIDE';
    const f = await TX(well, 'V = I × R', 'Measure', p.Size, 'color/text', { name: 'Formula', align: 'CENTER' });
    runs(f, [['V', P.blue], ['I', P.gold], ['R', P.coral]]);
    for (const l of [['V', 'voltage, in volts (V)', P.blue], ['I', 'current, in amperes (A)', P.gold], ['R', 'resistance, in ohms (Ω)', P.coral]]) {
      const t = await TX(c, l[0] + '   ' + l[1], p.Size === 'Desktop' ? 'Caption' : 'Micro', p.Size, 'color/text', { name: 'Legend ' + l[0] });
      t.setRangeFills(0, 1, [solid(l[2])]);
    }
    await glass(c, 'Glass');
    return c;
  });
  await placeSet(b, set, 'Formula', 'Ohm’s law with its legend; letters keep the quantity colours.');

  // Material Card
  const MAT = {
    Copper: { c: '#F09A5C', fill: 'Kids/Copper', facts: ['Low resistance', 'Electrons slip past easily', 'Big current'], chip: 'Resistance: low', glow: 'Kids/Card Glow Amber' },
    Nichrome: { c: '#D5DCE4', fill: 'Kids/Nichrome', facts: ['High resistance', 'Electrons bump and scatter', 'Less current, warms up'], chip: 'Resistance: high', glow: 'Kids/Glass Panel' },
  };
  set = await variantSet(b, 'Kids/Material Card', { Material: Object.keys(MAT), Size: SIZES }, async (p) => {
    const M = MAT[p.Material], s = SZ[p.Size], T = p.Size === 'Tablet', RW = T ? 250 : 330, RH = T ? 44 : 58;
    const c = AL('Material Card', { component: true, gap: 12, pad: s.pad, w: RW + 2 * s.pad, radius: 20 });
    await TX(c, p.Material, 'Title', p.Size, M.c, { name: 'Title' });
    const art = add(c, figma.createFrame()); art.name = 'Rod'; art.resize(RW, RH + 16); art.fills = []; art.clipsContent = false;
    const rod = rect(RW, RH, null, RH / 2); rod.y = 8; rod.name = 'Wire'; await fillStyle(rod, M.fill);
    rod.effects = p.Material === 'Nichrome' ? [shadow('#FF7A2E', 0.55, 22), shadow(P.shade, 0.5, 12, 6)] : [shadow(P.shade, 0.5, 12, 6)];
    art.appendChild(rod);
    const hi = rect(RW - RH, 5, solid(P.white, 0.55), 3); hi.x = RH / 2; hi.y = 8 + RH * 0.2; hi.name = 'Highlight'; art.appendChild(hi);
    const n = p.Material === 'Copper' ? 9 : 12;
    for (let i = 0; i < n; i++) {
      const e = ellipse(T ? 7 : 9, solid(P.cyan)); e.name = 'Electron'; e.effects = [shadow(P.blue, 1, 8)];
      const jitter = p.Material === 'Copper' ? 0 : ((i * 37) % 11 - 5) * (RH / 22);
      e.x = RH / 2 + i * (RW - RH) / (n - 1) - e.width / 2; e.y = 8 + RH / 2 - e.height / 2 + jitter; art.appendChild(e);
    }
    const facts = add(c, AL('Facts', { gap: 6 }));
    for (const f of M.facts) { const r = add(facts, AL('Fact', { dir: 'HORIZONTAL', cross: 'CENTER', gap: 8 })); add(r, icon('Check', T ? 16 : 20, P.green)); await TX(r, f, p.Size === 'Desktop' ? 'Caption' : 'Micro', p.Size, 'color/text', { name: 'Fact' }); }
    const chip = add(c, AL('Chip', { pad: [6, 14], radius: 999 }));
    await fillStyle(chip, 'Kids/Well'); chip.strokes = [solid(M.c, 0.9)]; chip.strokeWeight = 1.5;
    await TX(chip, M.chip, 'Button', p.Size, M.c, { name: 'Resistance' });
    await fillStyle(c, 'Kids/Glass'); c.strokes = [solid(M.c, 0.75)]; c.strokeWeight = 1.5; c.strokeAlign = 'INSIDE'; await fx(c, M.glow); sheen(c);
    return c;
  }, { width: 3072 });
  await placeSet(b, set, 'Material Card', 'Copper vs nichrome (Scene 12 pop-up): same voltage, length and thickness. Electrons line up in copper and scatter in nichrome, which glows warm.');

  // Hero
  set = await variantSet(b, 'Kids/Hero', { Size: SIZES }, async (p) => {
    const T = p.Size === 'Tablet';
    const c = AL('Hero', { component: true, cross: 'CENTER', gap: 4, pad: T ? [16, 32] : [24, 56], radius: 28 });
    const f = await TX(c, 'V × 2  →  I × 2', 'Display', p.Size, 'color/text', { name: 'Formula', align: 'CENTER' });
    runs(f, [['V', P.blue], ['I', P.gold], ['→', P.cyan]]);
    await TX(c, 'double the voltage, double the current', 'Body', p.Size, 'color/text-muted', { name: 'Caption', align: 'CENTER' });
    await glass(c, 'Active');
    return c;
  });
  textProp(set, 'Caption', 'Caption', 'double the voltage, double the current');
  await placeSet(b, set, 'Hero', 'Centre-screen discoveries (LessonPanel hero): V × 2 → I × 2, V ∝ I, 1 Ω = 1 V ÷ 1 A.');

  // Counter Card
  set = await variantSet(b, 'Kids/Counter Card', { Size: SIZES }, async (p) => {
    const s = SZ[p.Size], T = p.Size === 'Tablet', d = T ? 56 : 72;
    const c = AL('Counter Card', { component: true, dir: 'HORIZONTAL', cross: 'CENTER', gap: s.gap, pad: s.pad, w: T ? 360 : 470, radius: 20 });
    const n = add(c, AL('Count', { main: 'CENTER', cross: 'CENTER', w: d, h: d, radius: 999 }));
    n.strokes = [solid(P.gold)]; n.strokeWeight = 3; n.fills = [solid(P.navy, 0.7)]; n.effects = [shadow(P.amber, 0.8, 16)];
    await TX(n, '3', 'Title', p.Size, P.gold, { name: 'Number', align: 'CENTER' });
    const col = add(c, AL('Text', { gap: 2 }), { fillW: true });
    await TX(col, 'Charge through the section', 'Label', p.Size, 'color/text-muted', { name: 'Eyebrow' });
    await TX(col, 'Last second: 3 charges', 'Panel', p.Size, 'color/text', { name: 'Title' });
    const f = await TX(col, 'charge ÷ time = current (I)', p.Size === 'Desktop' ? 'Caption' : 'Micro', p.Size, 'color/text', { name: 'Formula' });
    runs(f, [['current (I)', P.gold]]);
    await TX(col, 'Simplified: a few electrons, slowed down so you can count them.', 'Micro', p.Size, 'color/text-muted', { name: 'Note', fillW: true });
    await glass(c, 'Glass');
    return c;
  }, { width: 3072 });
  await placeSet(b, set, 'Counter Card', 'Inside the wire (Scenes 5–6): charges counted through the glowing section, the first meaning of current.');

  // Ask Wattson
  set = await variantSet(b, 'Kids/Ask Wattson', { State: ['Default', 'Hover', 'Listening'], Size: SIZES }, async (p) => {
    const T = p.Size === 'Tablet', d = T ? 64 : 88, ring = p.State === 'Listening' ? P.amber : P.cyan;
    const c = AL('Ask Wattson', { component: true, cross: 'CENTER', gap: 6 });
    const face = add(c, AL('Face', { w: d, h: d, radius: 999 }));
    face.fills = [imgPaint('wattson')]; face.strokes = [solid(ring)]; face.strokeWeight = p.State === 'Default' ? 3 : 4;
    face.effects = [shadow(ring, p.State === 'Default' ? 0.6 : 0.95, p.State === 'Default' ? 14 : 24)];
    const badge = add(face, AL('Badge', { main: 'CENTER', cross: 'CENTER', w: T ? 24 : 30, h: T ? 24 : 30, radius: 999 }), { abs: [d - (T ? 22 : 28), d - (T ? 22 : 28)] });
    await fillStyle(badge, p.State === 'Listening' ? 'Kids/Fill Amber' : 'Kids/Fill Cyan');
    add(badge, icon(p.State === 'Listening' ? 'Mic' : 'Chat', T ? 14 : 18, P.ink));
    const pill = add(c, AL('Pill', { pad: [4, 12], radius: 999 }));
    await fillStyle(pill, 'Kids/Glass'); pill.strokes = [solid(ring, 0.7)]; pill.strokeWeight = 1;
    await TX(pill, p.State === 'Listening' ? 'Listening…' : 'Ask Wattson', 'Micro', p.Size, 'color/text', { name: 'Label' });
    return c;
  }, { width: 2400 });
  await placeSet(b, set, 'Ask Wattson', 'Bottom right: opens the question box. Listening = amber ring and mic while Wattson hears the learner.');

  // Title Card
  set = await variantSet(b, 'Kids/Title Card', { Size: SIZES }, async (p) => {
    const T = p.Size === 'Tablet';
    const c = AL('Title Card', { component: true, gap: T ? 12 : 18, w: T ? 520 : 820 });
    await TX(c, 'A Wattson investigation', 'Label', p.Size, P.cyan, { name: 'Eyebrow' });
    const t = await TX(c, 'OHM’S LAW', 'Display', p.Size, 'color/text', { name: 'Title' });
    t.fontSize = T ? 80 : 128; t.lineHeight = { unit: 'PERCENT', value: 100 };
    await fillStyle(t, 'Kids/Title Gradient'); await fx(t, 'Kids/Text Glow Amber');
    await TX(c, 'A journey of curiosity, current and discovery', 'Heading', p.Size, 'color/text', { name: 'Subtitle' });
    add(c, button('Primary', p.Size, 'Start journey', { lead: 'Play' }));
    await TX(c, 'Explore  •  Experiment  •  Discover', 'Label', p.Size, P.gold, { name: 'Steps' });
    await TX(c, 'About 10 minutes · Captions on · Sound on', T ? 'Micro' : 'Caption', p.Size, 'color/text-muted', { name: 'Info' });
    return c;
  });
  await placeSet(b, set, 'Title Card', 'The title screen over the Scene 4 study (HudScreens title).');

  b.resize(3200, CY);
  for (const k of Object.keys(COMPS)) delete COMPS[k];
  log('Panels: 18 component sets');
}
