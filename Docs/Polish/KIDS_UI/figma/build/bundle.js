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
