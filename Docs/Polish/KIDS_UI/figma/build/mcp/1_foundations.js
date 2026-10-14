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
const PRIMS = {
navy: P.navy, surface: P.panel, raised: P.surface, blue: P.blue, blueAccessible: P.blue, cyan: P.cyan, amber: P.amber,
paper: P.text, white: P.white, muted: P.muted, border: P.blue, success: P.green, error: P.red,
errorSurface: '#3A1622', successSurface: '#0F3326', scrim: '#050B14',
gold: P.gold, purple: P.purple, coral: P.coral, amberDeep: '#F0921A', ink: P.ink,
purpleSurface: '#251A4A', goldSurface: '#3A2C0E', hintSurface: '#0E2C44',
};
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
async function foundationsBoard() {
const pg = await page(DS_PAGE, 'Foundations');
const b = board(pg, 'Kids UI / 01 Foundations', 0, 0, 3200, 2300);
await heading(b, 'Foundations · tokens, type, light', 64, 56, P.gold);
await label(b, 'Every colour below is a variable (OHM / Semantic, modes Acrylic and Solid). Styles: OHM/… text, Kids/… paint and effect.', 64, 120, 2400);
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
await init();
await foundations(); await foundationsBoard();
return LOG.join(String.fromCharCode(10));