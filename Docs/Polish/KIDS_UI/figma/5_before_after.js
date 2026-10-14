// Part 5 — before / after: for each mockup, the same gameplay moment captured with the current v5 HUD in Unity (before)
// next to the proposal (after, a linked copy of the desktop mockup), plus the user's reference sheet as the target.

async function beforeAfter() {
  const pg = await page(SCREENS_PAGE, 'Desktop');
  clear(pg, 'before-after');
  const x0 = 4 * (1920 + 160) + 600, gap = 120;
  const h = own(await heading(pg, 'Before / After — the current Unity HUD (v5) and the Kids UI proposal, same frame', x0, -220, P.gold), 'before-after'); h.fontSize = 56;
  let y = 0;
  if (IMG.reference) {
    const ref = own(rect(3344, 1882, imgPaint('reference', 'FIT'), 24), 'before-after'); ref.name = 'Target · the user’s reference sheet (visual authority)';
    place(pg, ref, x0, y);
    own(await label(pg, 'TARGET · “Ohm’s Law — Gameplay-matched Kids UI” (the user’s reference: style, glass, glow, colour)', x0, y - 48, 3300, P.cyan), 'before-after').fontSize = 28;
    y += 1882 + 260;
  }
  for (const s of SCREENS) {
    const after = pg.findOne(n => n.type === 'FRAME' && n.name === 'D' + s[1] && n.getSharedPluginData('kidsui', 'part') === 'screens');
    const before = own(figma.createFrame(), 'before-after');
    before.name = 'Before · ' + s[1]; before.resize(1920, 1080); before.clipsContent = true;
    before.fills = [imgPaint('before_' + s[0])];
    place(pg, before, x0, y);
    own(await label(pg, 'BEFORE · ' + s[1] + ' — v5 HUD as it runs in Unity today', x0, y - 48, 1900, P.muted), 'before-after').fontSize = 28;
    if (after) {
      const copy = own(after.clone(), 'before-after');
      copy.name = 'After · ' + s[1];
      place(pg, copy, x0 + 1920 + gap, y);
      own(await label(pg, 'AFTER · ' + s[1] + ' — Kids UI proposal (' + s[2] + ')', x0 + 1920 + gap, y - 48, 1900, P.gold), 'before-after').fontSize = 28;
    }
    y += 1080 + 220;
  }
  log('Before / After: ' + SCREENS.length + ' pairs');
}
