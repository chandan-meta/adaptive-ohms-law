// Plugin main thread: audit, build, renders. The UI sends the images as bytes; renders go back to the UI, which posts them
// to receiver.py on localhost when it runs (so the build can be checked outside Figma).

async function audit() {
  await figma.loadAllPagesAsync();
  const pages = figma.root.children.map(p => ({
    name: p.name, topLevel: p.children.length,
    frames: p.children.filter(n => n.type === 'FRAME' || n.type === 'SECTION').slice(0, 80).map(n => n.name + ' [' + n.id + '] ' + Math.round(n.width) + '×' + Math.round(n.height)),
    componentSets: p.findAllWithCriteria({ types: ['COMPONENT_SET'] }).map(s => s.name + ' (' + s.children.length + ' variants)'),
    components: p.findAllWithCriteria({ types: ['COMPONENT'] }).filter(c => c.parent.type !== 'COMPONENT_SET').map(c => c.name).slice(0, 80),
  }));
  const cols = (await figma.variables.getLocalVariableCollectionsAsync()).map(c => ({ name: c.name, modes: c.modes.map(m => m.name), variables: c.variableIds.length }));
  return {
    file: figma.root.name, at: new Date().toISOString(), pages: pages, collections: cols,
    textStyles: (await figma.getLocalTextStylesAsync()).map(s => s.name + ' — ' + s.fontName.family + ' ' + s.fontName.style + ' ' + s.fontSize),
    paintStyles: (await figma.getLocalPaintStylesAsync()).map(s => s.name),
    effectStyles: (await figma.getLocalEffectStylesAsync()).map(s => s.name),
  };
}

async function renders() {
  const out = [];
  const ds = figma.root.children.find(p => p.name === DS_PAGE), sc = figma.root.children.find(p => p.name === SCREENS_PAGE);
  for (const pg of [ds, sc]) if (pg) for (const n of pg.children) {
    if (n.type !== 'FRAME' || n.removed) continue;
    const part = n.getSharedPluginData('kidsui', 'part');
    if (!(part === 'screens' || part === 'sheet' || n.name.indexOf('Kids UI / ') === 0 || (part === 'before-after' && n.name.indexOf('Before') === 0))) continue;
    const big = n.width > 2000 || n.height > 2000;
    const bytes = await n.exportAsync({ format: 'PNG', constraint: big ? { type: 'WIDTH', value: 2400 } : { type: 'SCALE', value: n.width > 1100 ? 0.5 : 1 } });
    out.push(n.name);
    figma.ui.postMessage({ type: 'render', name: n.name.replace(/[^A-Za-z0-9·._ -]+/g, '_') + '.png', bytes: bytes });
  }
  return out;
}

figma.showUI(__html__, { width: 460, height: 640, themeColors: true });
figma.ui.onmessage = async (msg) => {
  try {
    if (msg.type === 'audit') figma.ui.postMessage({ type: 'audit', data: await audit() });
    if (msg.type === 'build') {
      for (const k of Object.keys(msg.images || {})) IMG_HASHES[k] = figma.createImage(msg.images[k]).hash;
      log('Images: ' + Object.keys(IMG_HASHES).length);
      await runAll(msg.only && msg.only.length ? msg.only : null);
      const before = await audit();
      figma.ui.postMessage({ type: 'audit', data: before, after: true });
      if (msg.renders) { const r = await renders(); log('Renders: ' + r.length); }
      const sc = figma.root.children.find(p => p.name === SCREENS_PAGE);
      if (sc) { await figma.setCurrentPageAsync(sc); figma.viewport.scrollAndZoomIntoView(sc.children.filter(n => n.type === 'FRAME')); }
      figma.ui.postMessage({ type: 'done', log: LOG });
      figma.notify('Kids UI built: design system + screens');
    }
    if (msg.type === 'renders') { const r = await renders(); figma.ui.postMessage({ type: 'done', log: ['Renders: ' + r.length] }); }
  } catch (e) {
    figma.ui.postMessage({ type: 'error', msg: String(e && e.message || e), stack: String(e && e.stack || ''), log: LOG });
  }
};
