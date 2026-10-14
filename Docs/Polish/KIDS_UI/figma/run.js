// Runner: the parts in order. The plugin's Build button and the mock call runAll(); a use_figma call runs one part.
const PARTS = [
  ['Foundations', async () => { await foundations(); await foundationsBoard(); }],
  ['Icons', () => icons()],
  ['Controls', () => controls()],
  ['Panels', () => panels()],
  ['Screens', () => screens()],
  ['Before / After', () => beforeAfter()],
  ['Sheet', () => sheet()],
];
async function runAll(only) {
  await init();
  for (const p of PARTS) if (!only || only.indexOf(p[0]) >= 0) { log('▶ ' + p[0]); await p[1](); }
}
