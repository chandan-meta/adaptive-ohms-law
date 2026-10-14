// Kids UI Figma build — shared helpers. Prepended to every part (the development plugin runs all parts in one go; a
// use_figma call runs lib + one part). Parts find tokens, styles and components by name, so each can run on its own and
// re-running one rebuilds only what it owns. Plain ES2017: no ?. or ?? (the plugin sandbox).

const P = {
  navy: '#0B1626', panel: '#1A2B45', surface: '#2E3B55', blue: '#00D1FF', cyan: '#7EE9FF', amber: '#FFB347',
  gold: '#FFD166', green: '#2ECC71', red: '#FF6B6B', purple: '#A78BFA', text: '#F4FAFF', muted: '#B9CBE3',
  coral: '#FF8C7A', ink: '#0B1626', white: '#FFFFFF', shade: '#000814',
};
const DS_PAGE = 'Kids UI · Design System', SCREENS_PAGE = 'Kids UI · Screens';
const HEX = h => ({ r: parseInt(h.slice(1, 3), 16) / 255, g: parseInt(h.slice(3, 5), 16) / 255, b: parseInt(h.slice(5, 7), 16) / 255 });
const RGBA = (h, a) => Object.assign(HEX(h), { a: a === undefined ? 1 : a });
const solid = (h, opacity) => ({ type: 'SOLID', color: HEX(h), opacity: opacity === undefined ? 1 : opacity });
const VERT = [[0, 1, 0], [-1, 0, 1]], HORZ = [[1, 0, 0], [0, 1, 0]];
const grad = (stops, opacity, transform) => ({
  type: 'GRADIENT_LINEAR', opacity: opacity === undefined ? 1 : opacity, gradientTransform: transform || VERT,
  gradientStops: stops.map(s => ({ position: s[0], color: RGBA(s[1], s[2]) })),
});
const shadow = (h, a, radius, y, type) => {
  const e = { type: type || 'DROP_SHADOW', color: RGBA(h, a), offset: { x: 0, y: y || 0 }, radius: radius, spread: 0, visible: true, blendMode: 'NORMAL' };
  if (e.type === 'DROP_SHADOW') e.showShadowBehindNode = false;
  return e;
};
const inner = (h, a, radius, y) => shadow(h, a, radius, y, 'INNER_SHADOW');
const SIZES = ['Desktop', 'Tablet'];
// Per size: padding, gaps, radii, control heights, icon tiles, portrait and card widths (desktop = the 1920×1080 canvas,
// tablet = 1024×768 points; tablet text is relatively larger, never below 13 px, targets never below 48).
const SZ = {
  Desktop: { pad: 24, gap: 16, r: 20, btnH: 56, btnX: 28, tile: 56, icon: 26, opt: 64, badge: 36, portrait: 112, card: 520, dlg: 860, bubble: 400 },
  Tablet: { pad: 16, gap: 12, r: 16, btnH: 48, btnX: 20, tile: 48, icon: 22, opt: 56, badge: 30, portrait: 80, card: 380, dlg: 600, bubble: 300 },
};

const V = {}, STY = {}, PST = {}, EST = {}, LOG = [];
// Image hashes by name (gameplay frames, the Wattson portrait): filled by the plugin from its bundled images, or
// injected as IMG_HASHES for a use_figma run after upload_assets.
const IMG = typeof IMG_HASHES !== 'undefined' ? IMG_HASHES : {};
const imgPaint = (name, mode) => IMG[name] ? { type: 'IMAGE', imageHash: IMG[name], scaleMode: mode || 'FILL' } : grad([[0, '#22406A'], [1, '#0B1626']]);
async function init() {
  await figma.loadAllPagesAsync();
  for (const k of Object.keys(V)) delete V[k];
  for (const v of await figma.variables.getLocalVariablesAsync()) V[v.name] = v;
  for (const s of await figma.getLocalTextStylesAsync()) STY[s.name] = s;
  for (const s of await figma.getLocalPaintStylesAsync()) PST[s.name] = s;
  for (const s of await figma.getLocalEffectStylesAsync()) EST[s.name] = s;
  const seen = {};
  for (const s of Object.values(STY)) {
    const key = s.fontName.family + '|' + s.fontName.style;
    if (s.name.indexOf('OHM/') === 0 && !seen[key]) { seen[key] = 1; try { await figma.loadFontAsync(s.fontName); } catch (e) { log('Font not available: ' + key + ' (style ' + s.name + ')'); } }
  }
}
function log(msg) { LOG.push(msg); if (typeof process !== 'undefined' && process.stdout) process.stdout.write('· ' + msg + String.fromCharCode(10)); if (typeof figma.ui !== 'undefined' && figma.ui.postMessage) try { figma.ui.postMessage({ type: 'log', msg: msg }); } catch (e) { /* no UI in use_figma */ } }

async function page(name, fallback) {
  let p = figma.root.children.find(x => x.name === name);
  if (!p) {
    try { p = figma.createPage(); p.name = name; } catch (e) { // plan page limit: build on an existing page
      p = figma.root.children.find(x => x.name.indexOf(fallback) >= 0) || figma.root.children[figma.root.children.length - 1];
      log('Could not add page "' + name + '" (' + e.message + '); using "' + p.name + '"');
    }
  }
  await figma.setCurrentPageAsync(p);
  return p;
}
// A top-level board frame, rebuilt on each run (the old one with the same name is removed first).
function board(pg, name, x, y, w, h, fill) {
  for (const old of pg.children.filter(n => n.name === name)) old.remove();
  const b = figma.createFrame();
  b.name = name; b.x = x; b.y = y; b.resize(w, h); b.clipsContent = false;
  b.fills = [solid(fill || '#060D18')];
  b.cornerRadius = 32;
  pg.appendChild(b);
  return b;
}

// Paints: '#hex' → solid, 'color/…' → bound to the semantic variable.
function vp(name, opacity) {
  const v = V[name];
  if (!v) throw new Error('missing variable ' + name);
  const p = figma.variables.setBoundVariableForPaint(solid('#000000'), 'color', v);
  return opacity === undefined || opacity === 1 ? p : Object.assign({}, p, { opacity: opacity });
}
const paintOf = (c, opacity) => c.charAt(0) === '#' ? solid(c, opacity) : vp(c, opacity);
async function fillStyle(n, name) { const s = PST[name]; if (!s) throw new Error('missing paint style ' + name); await n.setFillStyleIdAsync(s.id); }
async function fx(n, name) { if (!name) { n.effects = []; return; } const s = EST[name]; if (!s) throw new Error('missing effect style ' + name); await n.setEffectStyleIdAsync(s.id); }
function bindNum(n, prop, value, family) { const v = V[family + '/' + value]; if (v) n.setBoundVariable(prop, v); else n[prop] = value; }
function radius(n, r) { for (const k of ['topLeftRadius', 'topRightRadius', 'bottomLeftRadius', 'bottomRightRadius']) bindNum(n, k, r, 'radius'); }
function pad(n, t, r, b, l) {
  bindNum(n, 'paddingTop', t, 'space'); bindNum(n, 'paddingRight', r, 'space');
  bindNum(n, 'paddingBottom', b, 'space'); bindNum(n, 'paddingLeft', l, 'space');
}
function gap(n, g) { bindNum(n, 'itemSpacing', g, 'space'); }

// Auto-layout frame (or component): dir, w/h (fixed when given, else hug), pad (n | [v,h] | [t,r,b,l]), gap, radius,
// main/cross alignment, fill.
function AL(name, o) {
  o = o || {};
  const n = o.component ? figma.createComponent() : figma.createFrame();
  n.name = name;
  n.layoutMode = o.dir || 'VERTICAL';
  n.fills = [];
  n.clipsContent = false;
  n.resize(o.w || 100, o.h || 100);
  n.layoutSizingHorizontal = o.w ? 'FIXED' : 'HUG';
  n.layoutSizingVertical = o.h ? 'FIXED' : 'HUG';
  n.primaryAxisAlignItems = o.main || 'MIN';
  n.counterAxisAlignItems = o.cross || 'MIN';
  if (o.pad !== undefined) {
    const p = [].concat(o.pad);
    const t = p[0], r = p.length > 1 ? p[1] : t, b = p.length > 2 ? p[2] : t, l = p.length > 3 ? p[3] : r;
    pad(n, t, r, b, l);
  }
  if (o.gap !== undefined) gap(n, o.gap);
  if (o.radius !== undefined) radius(n, o.radius);
  if (o.fill) n.fills = [].concat(o.fill).map(f => typeof f === 'string' ? paintOf(f) : f);
  return n;
}
function add(parent, child, o) {
  o = o || {};
  parent.appendChild(child);
  if (o.abs) { child.layoutPositioning = 'ABSOLUTE'; child.x = o.abs[0]; child.y = o.abs[1]; }
  if (o.fillW) child.layoutSizingHorizontal = 'FILL';
  if (o.fillH) child.layoutSizingVertical = 'FILL';
  return child;
}
function rect(w, h, fill, r) { const n = figma.createRectangle(); n.resize(w, h); n.fills = fill ? [].concat(fill).map(f => typeof f === 'string' ? paintOf(f) : f) : []; if (r !== undefined) n.cornerRadius = r; return n; }
function ellipse(d, fill) { const n = figma.createEllipse(); n.resize(d, d); n.fills = fill ? [].concat(fill).map(f => typeof f === 'string' ? paintOf(f) : f) : []; return n; }

// Text in one of the OHM/ styles; role = Display | Title | Heading | Panel | Measure | Body | Button | Label | Caption |
// Micro, picked for the size (Tablet* styles on tablet). o: name, fillW, w, align.
function styleName(role, size) { return 'OHM/' + (size === 'Tablet' ? 'Tablet' + role : role); }
async function TX(parent, chars, role, size, color, o) {
  o = o || {};
  const st = STY[styleName(role, size)] || STY['OHM/' + role];
  if (!st) throw new Error('missing text style ' + styleName(role, size));
  const t = figma.createText();
  t.fontName = st.fontName;
  t.characters = chars;
  await t.setTextStyleIdAsync(st.id);
  t.fills = [paintOf(color || 'color/text')];
  t.name = o.name || chars.replace(/\n/g, ' ').slice(0, 40);
  if (o.align) t.textAlignHorizontal = o.align;
  if (parent) parent.appendChild(t);
  if (o.fillW) { t.textAutoResize = 'HEIGHT'; t.layoutSizingHorizontal = 'FILL'; }
  else if (o.w) { t.resize(o.w, t.height); t.textAutoResize = 'HEIGHT'; }
  else t.textAutoResize = 'WIDTH_AND_HEIGHT';
  return t;
}
// Colour runs: [[substring, colour], …] — every occurrence of each substring.
function runs(t, list) {
  for (const pair of list) {
    let i = t.characters.indexOf(pair[0]);
    while (i >= 0) { t.setRangeFills(i, i + pair[0].length, [paintOf(pair[1])]); i = t.characters.indexOf(pair[0], i + pair[0].length); }
  }
}

// Tones: card fill style, rim colour, glow style. Buttons use Kids/Fill <Tone> (+ Hover / Pressed).
const TONE = {
  Glass: { card: 'Kids/Glass', rim: P.blue, rimA: 0.55, glow: 'Kids/Glass Panel', c: P.blue },
  Active: { card: 'Kids/Glass', rim: P.cyan, rimA: 0.95, glow: 'Kids/Glass Active', c: P.cyan },
  Amber: { card: 'Kids/Card Amber', rim: P.amber, rimA: 0.9, glow: 'Kids/Card Glow Amber', c: P.amber },
  Cyan: { card: 'Kids/Card Cyan', rim: P.blue, rimA: 0.9, glow: 'Kids/Card Glow Cyan', c: P.blue },
  Success: { card: 'Kids/Card Success', rim: P.green, rimA: 0.9, glow: 'Kids/Card Glow Success', c: P.green },
  Error: { card: 'Kids/Card Error', rim: P.red, rimA: 0.9, glow: 'Kids/Card Glow Error', c: P.red },
  Purple: { card: 'Kids/Card Purple', rim: P.purple, rimA: 0.9, glow: 'Kids/Card Glow Purple', c: P.purple },
  Gold: { card: 'Kids/Card Gold', rim: P.gold, rimA: 0.95, glow: 'Kids/Card Glow Gold', c: P.gold },
};
async function glass(n, tone, o) {
  o = o || {};
  const T = TONE[tone || 'Glass'];
  await fillStyle(n, T.card);
  n.strokes = [solid(T.rim, o.rimA || T.rimA)];
  n.strokeWeight = o.sw || 1.5;
  n.strokeAlign = 'INSIDE';
  await fx(n, o.glow === undefined ? T.glow : o.glow);
  if (o.sheen !== false) sheen(n, o.sheenH, o.sheenA);
  return n;
}
// The glass reflection: a soft white band along the top inside edge, behind the content, stretching with the card.
function sheen(n, h, a) {
  const r = typeof n.topLeftRadius === 'number' ? n.topLeftRadius : 16;
  const s = figma.createRectangle();
  s.name = 'FX / Reflection';
  s.resize(Math.max(1, n.width - 4), h || Math.max(8, Math.min(44, n.height * 0.42)));
  s.fills = [grad([[0, P.white, a || 0.16], [1, P.white, 0]])];
  s.topLeftRadius = s.topRightRadius = Math.max(0, r - 2);
  n.insertChild(0, s);
  if (n.layoutMode && n.layoutMode !== 'NONE') s.layoutPositioning = 'ABSOLUTE';
  s.x = 2; s.y = 2;
  s.constraints = { horizontal: 'STRETCH', vertical: 'MIN' };
  return s;
}

// Ownership: top-level nodes a part makes are tagged so a re-run removes exactly them.
function own(node, part) { node.setSharedPluginData('kidsui', 'part', part); return node; }
function clear(pg, part) { for (const n of pg.children.filter(x => x.getSharedPluginData('kidsui', 'part') === part)) n.remove(); }

// Library lookups (the design-system page).
function dsPage() { return figma.root.children.find(x => x.name === DS_PAGE) || figma.root.children.find(x => x.name.indexOf('Components') >= 0); }
const COMPS = {};
function comp(name) {
  if (COMPS[name] && !COMPS[name].removed) return COMPS[name];
  const pg = dsPage();
  const n = pg && pg.findOne(x => (x.type === 'COMPONENT_SET' || (x.type === 'COMPONENT' && x.parent.type !== 'COMPONENT_SET')) && x.name === name);
  if (!n) throw new Error('missing component ' + name);
  COMPS[name] = n;
  return n;
}
function variantOf(name, props) {
  const n = comp(name);
  if (n.type === 'COMPONENT') return n;
  const keys = Object.keys(props || {});
  const v = n.children.find(c => keys.every(k => c.variantProperties[k] === props[k]));
  if (!v) throw new Error('no variant ' + JSON.stringify(props) + ' in ' + name);
  return v;
}
// An instance; texts = { 'Layer name': 'text' } (any text layer, nested too); swaps = { 'Layer': 'Kids/Icon/X' }.
function inst(name, props, texts, swaps) {
  const i = variantOf(name, props).createInstance();
  setTexts(i, texts);
  if (swaps) for (const k of Object.keys(swaps)) {
    const node = i.findOne(x => x.type === 'INSTANCE' && x.name === k);
    if (!node) throw new Error('no instance layer ' + k + ' in ' + name);
    node.swapComponent(comp(swaps[k]));
  }
  return i;
}
function setTexts(node, texts) {
  for (const k of Object.keys(texts || {})) {
    const t = node.type === 'TEXT' && node.name === k ? node : node.findOne(x => x.type === 'TEXT' && x.name === k);
    if (!t) throw new Error('no text layer "' + k + '" in ' + node.name);
    t.characters = texts[k];
  }
}
// Component properties on an instance by plain name (Label, Trailing icon, Kind…); swaps take 'Kids/…' names.
function setProps(i, obj) {
  const defs = i.componentProperties, upd = {};
  for (const k of Object.keys(obj)) {
    const key = Object.keys(defs).find(d => d === k || d.split('#')[0] === k);
    if (!key) throw new Error('no property ' + k + ' on ' + i.name);
    let v = obj[k];
    if (defs[key].type === 'INSTANCE_SWAP' && typeof v === 'string') v = comp(v).id;
    upd[key] = v;
  }
  i.setProperties(upd);
  return i;
}
function setVisible(node, layer, on) { const n = node.findOne(x => x.name === layer); if (!n) throw new Error('no layer ' + layer); n.visible = on; }
function place(parent, node, x, y) { parent.appendChild(node); node.x = x; node.y = y; return node; }

// Icons (Kids/Icon/<Name>, 24×24 strokes) and tinting of an icon instance or a whole subtree.
function icon(name, px, color) {
  const i = comp('Kids/Icon/' + name).createInstance();
  i.name = 'Icon';
  if (px && Math.abs(px - i.width) > 0.01) i.rescale(px / i.width);
  if (color) tint(i, color);
  return i;
}
function tint(node, color) {
  const p = paintOf(color);
  const all = node.findAll ? node.findAll(x => x.type === 'VECTOR' || x.type === 'ELLIPSE' || x.type === 'RECTANGLE' || x.type === 'LINE' || x.type === 'BOOLEAN_OPERATION') : [];
  for (const v of all) {
    if (v.name.indexOf('FX /') === 0) continue;
    if (v.strokes && v.strokes.length) v.strokes = [p];
    if (v.fills && v.fills.length && v.name !== 'Hole') v.fills = [p];
  }
}

// Variant sets: axes = { Kind: [...], Size: [...] }; build(props) returns a ComponentNode.
function combos(axes) {
  let out = [{}];
  for (const k of Object.keys(axes)) out = [].concat.apply([], out.map(o => axes[k].map(v => { const c = Object.assign({}, o); c[k] = v; return c; })));
  return out;
}
async function variantSet(parent, name, axes, build, o) {
  o = o || {};
  for (const old of parent.children.filter(n => n.name === name)) old.remove();
  const comps = [];
  for (const props of combos(axes)) {
    const c = await build(props);
    c.name = Object.keys(props).map(k => k + '=' + props[k]).join(', ');
    comps.push(c);
  }
  const set = figma.combineAsVariants(comps, parent);
  set.name = name;
  set.description = o.desc || '';
  set.layoutMode = 'HORIZONTAL';
  set.layoutWrap = 'WRAP';
  set.itemSpacing = o.gap || 32;
  set.counterAxisSpacing = o.gap || 32;
  set.counterAxisAlignItems = 'MIN';
  set.paddingTop = set.paddingBottom = set.paddingLeft = set.paddingRight = 40;
  set.fills = [solid('#0A1424')];
  set.strokes = [solid(P.blue, 0.22)];
  set.strokeWeight = 1;
  set.cornerRadius = 24;
  set.resize(o.width || 1800, 200);
  set.layoutSizingHorizontal = 'FIXED';
  set.layoutSizingVertical = 'HUG';
  return set;
}
function single(parent, name, c, desc) {
  for (const old of parent.children.filter(n => n.name === name && n !== c)) old.remove();
  c.name = name; c.description = desc || '';
  parent.appendChild(c);
  return c;
}
// Component properties on a set (or a single component) wired to layers by name.
function textProp(set, prop, layer, def) {
  const key = set.addComponentProperty(prop, 'TEXT', def);
  for (const t of set.findAll(x => x.type === 'TEXT' && x.name === layer)) t.componentPropertyReferences = Object.assign({}, t.componentPropertyReferences, { characters: key });
  return key;
}
function boolProp(set, prop, layer, def) {
  const key = set.addComponentProperty(prop, 'BOOLEAN', def !== false);
  for (const n of set.findAll(x => x.name === layer)) n.componentPropertyReferences = Object.assign({}, n.componentPropertyReferences, { visible: key });
  return key;
}
function swapProp(set, prop, layer, defName, preferred) {
  const key = set.addComponentProperty(prop, 'INSTANCE_SWAP', comp(defName).id,
    preferred ? { preferredValues: preferred.map(p => ({ type: 'COMPONENT', key: comp(p).key })) } : undefined);
  for (const n of set.findAll(x => x.type === 'INSTANCE' && x.name === layer)) n.componentPropertyReferences = Object.assign({}, n.componentPropertyReferences, { mainComponent: key });
  return key;
}
// Instances of the library used inside other components and screens.
function iconTile(tone, size, name) { const i = inst('Kids/Icon Tile', { Tone: tone, Size: size }); setProps(i, { Icon: 'Kids/Icon/' + name }); i.name = 'Tile ' + name; return i; }
function button(kind, size, text, o) {
  o = o || {};
  const i = inst('Kids/Button', { Kind: kind, State: o.state || 'Default', Size: size });
  const pr = { Label: text };
  if (o.lead) { pr['Leading icon'] = true; pr.Leading = 'Kids/Icon/' + o.lead; }
  if (o.trail) { pr['Trailing icon'] = true; pr.Trailing = 'Kids/Icon/' + o.trail; }
  setProps(i, pr);
  i.name = 'Button ' + text;
  return i;
}
function spacer(parent) { const s = add(parent, AL('Spacer', { h: 1 })); s.layoutGrow = 1; return s; }

let CY = 0; // board cursor
async function placeSet(b, set, title, note) {
  await heading(b, title, 64, CY, P.gold);
  if (note) await label(b, note, 64, CY + 58, 3000);
  set.x = 64; set.y = CY + (note ? 100 : 70);
  CY = set.y + set.height + 110;
}

const SCREENS = [
  ['historical', '1 · Historical room', 'Scene 4 · Meet Wattson — Ohm’s study, 1826'],
  ['conductor', '2 · Electric conductor', 'Scene 5 · Inside the wire — the user’s Blender set'],
  ['experiment', '3 · Classroom experiment', 'Scene 10 · Set the battery to 3 V'],
  ['graph', '4 · Data and graph', 'Scene 11 · The pattern'],
  ['materials', '5 · Copper vs nichrome', 'Scene 12 · Quick check 3 of 3'],
  ['quiz', '6 · Quiz and feedback', 'Scene 10 · Quick check 2 of 3, judged by the ammeter'],
  ['complete', '7 · Completion', 'Scene 13 · Case closed'],
];

// Board label: a caption above a library item or screen.
async function label(parent, text, x, y, w, color) {
  const t = await TX(null, text, 'Caption', 'Desktop', color || P.muted, { name: 'Label / ' + text.slice(0, 30) });
  parent.appendChild(t); t.x = x; t.y = y;
  if (w) { t.resize(w, t.height); t.textAutoResize = 'HEIGHT'; }
  return t;
}
async function heading(parent, text, x, y, color) {
  const t = await TX(null, text, 'Title', 'Desktop', color || P.text, { name: 'Heading / ' + text.slice(0, 30) });
  parent.appendChild(t); t.x = x; t.y = y;
  return t;
}
