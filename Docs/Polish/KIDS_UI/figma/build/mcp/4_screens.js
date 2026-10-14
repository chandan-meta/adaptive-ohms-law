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
await init();
await screens();
return LOG.join(String.fromCharCode(10));