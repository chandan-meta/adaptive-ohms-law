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
let set = await variantSet(b, 'Kids/Icon Tile', { Tone: TILE_TONES, Size: SIZES }, async (p) => {
const s = SZ[p.Size];
const c = AL('Icon Tile', { component: true, main: 'CENTER', cross: 'CENTER', w: s.tile, h: s.tile, radius: p.Size === 'Desktop' ? 16 : 12 });
add(c, icon('Home', s.icon, TONE_ICON[p.Tone]));
await glass(c, p.Tone === 'Glass' ? 'Glass' : p.Tone, { sheenA: 0.22 });
return c;
}, { width: 3072, gap: 24 });
swapProp(set, 'Icon', 'Icon', 'Kids/Icon/Home');
await placeSet(b, set, 'Icon Tile', 'Rounded tile for every icon in the HUD and panels. Tone = rim, glow and icon colour; swap the icon.');
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
await init();
await controls();
return LOG.join(String.fromCharCode(10));