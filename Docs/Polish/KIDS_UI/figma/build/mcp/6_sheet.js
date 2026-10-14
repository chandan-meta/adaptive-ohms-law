const IMG_HASHES = __IMG_HASHES__;
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
const SZ = {
Desktop: { pad: 24, gap: 16, r: 20, btnH: 56, btnX: 28, tile: 56, icon: 26, opt: 64, badge: 36, portrait: 112, card: 520, dlg: 860, bubble: 400 },
Tablet: { pad: 16, gap: 12, r: 16, btnH: 48, btnX: 20, tile: 48, icon: 22, opt: 56, badge: 30, portrait: 80, card: 380, dlg: 600, bubble: 300 },
};
const V = {}, STY = {}, PST = {}, EST = {}, LOG = [];
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
function board(pg, name, x, y, w, h, fill) {
for (const old of pg.children.filter(n => n.name === name)) old.remove();
const b = figma.createFrame();
b.name = name; b.x = x; b.y = y; b.resize(w, h); b.clipsContent = false;
b.fills = [solid(fill || '#060D18')];
b.cornerRadius = 32;
pg.appendChild(b);
return b;
}
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
function runs(t, list) {
for (const pair of list) {
let i = t.characters.indexOf(pair[0]);
while (i >= 0) { t.setRangeFills(i, i + pair[0].length, [paintOf(pair[1])]); i = t.characters.indexOf(pair[0], i + pair[0].length); }
}
}
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
function own(node, part) { node.setSharedPluginData('kidsui', 'part', part); return node; }
function clear(pg, part) { for (const n of pg.children.filter(x => x.getSharedPluginData('kidsui', 'part') === part)) n.remove(); }
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
const head = add(sh, AL('Header', { dir: 'HORIZONTAL', cross: 'CENTER', gap: 40 }), { fillW: true });
const titles = add(head, AL('Titles', { gap: 4 }));
const t = await TX(titles, 'OHM’S LAW', 'Display', 'Desktop', 'color/text', { name: 'Title' });
t.fontSize = 150; t.lineHeight = { unit: 'PERCENT', value: 100 }; await fillStyle(t, 'Kids/Title Gradient'); await fx(t, 'Kids/Text Glow Amber');
const sub = await TX(titles, 'Gameplay-matched kids UI', 'Title', 'Desktop', P.cyan, { name: 'Subtitle' }); sub.textCase = 'UPPER'; await fx(sub, 'Kids/Text Glow Blue');
spacer(head);
await TX(head, 'Explore · Experiment · Discover\nReal-world physics, one clue at a time.', 'Heading', 'Desktop', 'color/text-muted', { name: 'Tagline', align: 'RIGHT' });
const face = add(head, ellipse(150, imgPaint('wattson'))); face.name = 'Wattson'; face.strokes = [solid(P.cyan)]; face.strokeWeight = 4; face.effects = [shadow(P.blue, 0.8, 30)];
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
for (const r of sh.children.filter(n => n.name.indexOf('Row ') === 0)) {
const hmax = Math.max.apply(null, r.children.map(n => n.height));
for (const n of r.children) { n.layoutSizingVertical = 'FIXED'; n.resize(n.width, hmax); }
}
log('Component sheet: 14 sections');
}
await init();
await sheet();
return LOG.join(String.fromCharCode(10));