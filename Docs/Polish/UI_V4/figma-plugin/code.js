const IDS = [];
const keep = n => { IDS.push(n.id); return n; };
const HEX = h => { const n = parseInt(h.slice(1, 7), 16); return { r: ((n >> 16) & 255) / 255, g: ((n >> 8) & 255) / 255, b: (n & 255) / 255 }; };
const RGBA = (h, a = 1) => Object.assign(HEX(h), { a });
const SOLID = (h, opacity) => opacity === undefined ? { type: 'SOLID', color: HEX(h) } : { type: 'SOLID', color: HEX(h), opacity };
const P = {
navy950: '#050F1C', navy900: '#081A2B', navy800: '#10283D', navy700: '#17395A',
blue: '#2EA7FF', cyan: '#27D0F0', gold: '#FFC857', green: '#27D07D', red: '#FF5B5B',
text: '#F5F7FB', text2: '#AFC5D5', muted: '#7A8FA6', volt: '#3FA9FF', amp: '#FFB830', ohm: '#FF5A5F', charge: '#D6FAFF',
};
const TOK = {};
async function loadTokens() {
for (const v of await figma.variables.getLocalVariablesAsync()) TOK[v.name] = v;
return Object.keys(TOK).length;
}
function tokPaint(name, opacity) {
const v = TOK[name];
if (!v) throw new Error('missing token ' + name);
return figma.variables.setBoundVariableForPaint(SOLID('#000000', opacity), 'color', v);
}
function fillTok(node, name, opacity) { node.fills = [tokPaint(name, opacity)]; return node; }
function strokeTok(node, name, weight = 1, opacity, align = 'INSIDE') {
node.strokes = [tokPaint(name, opacity)]; node.strokeWeight = weight; node.strokeAlign = align; return node;
}
function bindNum(node, prop, name) { const v = TOK[name]; if (!v) throw new Error('missing token ' + name); node.setBoundVariable(prop, v); return node; }
function padTok(node, name) { for (const p of ['paddingTop', 'paddingRight', 'paddingBottom', 'paddingLeft']) bindNum(node, p, name); return node; }
function radiusTok(node, name) { for (const p of ['topLeftRadius', 'topRightRadius', 'bottomLeftRadius', 'bottomRightRadius']) bindNum(node, p, name); return node; }
const FONT = {};   // key → { family, style }; resolved by resolveFonts()
async function resolveFonts() {
const all = await figma.listAvailableFontsAsync();
const has = (family, style) => all.some(f => f.fontName.family === family && f.fontName.style === style);
const pick = (family, styles, fallback) => { for (const s of styles) if (has(family, s)) return { family, style: s }; return fallback; };
FONT.sansBold = pick('Nunito Sans', ['Bold', '700'], { family: 'Inter', style: 'Bold' });
FONT.sansSemi = pick('Nunito Sans', ['SemiBold', 'Semi Bold', '600', 'Bold'], { family: 'Inter', style: 'Semi Bold' });
FONT.serifBlack = pick('Cinzel', ['Black', 'ExtraBold', 'Bold'], { family: 'Inter', style: 'Bold' });
FONT.serifBold = pick('Cinzel', ['Bold', 'SemiBold', 'Regular'], { family: 'Inter', style: 'Bold' });
const uniq = {}; for (const f of Object.values(FONT)) uniq[f.family + '|' + f.style] = f;
await Promise.all(Object.values(uniq).map(f => figma.loadFontAsync(f)));
return FONT;
}
const TYPE = {
'Display/Title':     ['serifBlack', 132, 100, 4, true],
'Display/Chapter':   ['serifBold', 56, 110, 3, false],
'Display/CTA':       ['sansBold', 24, 100, 10, true],
'Display/Subtitle':  ['serifBold', 26, 130, 14, true],
'Heading/Screen':    ['sansBold', 32, 120, 0, false],
'Heading/Turn':      ['sansBold', 28, 120, 0, false],
'Heading/Panel':     ['sansBold', 24, 120, 0, false],
'Body/Large':        ['sansSemi', 21, 135, 0, false],
'Body':              ['sansSemi', 18, 140, 0, false],
'Body/Secondary':    ['sansSemi', 17, 140, 0, false],
'Label/Eyebrow':     ['sansBold', 15, 120, 12, true],
'Label/Control':     ['sansBold', 16, 120, 8, true],
'Label/Small':       ['sansBold', 14, 120, 6, true],
'Button':            ['sansBold', 20, 120, 0, false],
'Button/Small':      ['sansBold', 18, 120, 0, false],
'Caption':           ['sansSemi', 32, 135, 0, false],
'Caption/Speaker':   ['sansBold', 15, 120, 14, true],
'Numeric/Readout':   ['sansBold', 46, 110, 0, false],
'Numeric/Table':     ['sansBold', 22, 120, 0, false],
'Numeric/Tick':      ['sansBold', 16, 120, 0, false],
'Formula/Reveal':    ['sansBold', 64, 110, 2, false],
'Formula/Line':      ['sansBold', 34, 120, 0, false],
'Chip':              ['sansBold', 18, 120, 0, false],
};
const STYLE = {};  // name → TextStyle, filled by loadStyles()
async function loadStyles() {
for (const s of await figma.getLocalTextStylesAsync()) STYLE[s.name] = s;
for (const s of await figma.getLocalEffectStylesAsync()) STYLE[s.name] = s;
return Object.keys(STYLE).length;
}
function applyType(node, name) {
const t = TYPE[name]; if (!t) throw new Error('unknown type ' + name);
node.fontName = FONT[t[0]]; node.fontSize = t[1];
node.lineHeight = { value: t[2], unit: 'PERCENT' }; node.letterSpacing = { value: t[3], unit: 'PERCENT' };
node.textCase = t[4] ? 'UPPER' : 'ORIGINAL';
}
async function txt(parent, type, chars, o = {}) {
const t = figma.createText();
applyType(t, type);
t.characters = chars;
const color = o.color || 'color/text/primary';
t.fills = [color[0] === '#' ? SOLID(color, o.opacity) : tokPaint(color, o.opacity)];
t.textAlignHorizontal = o.align || 'LEFT';
t.name = o.name || chars.slice(0, 24);
if (o.w !== undefined) t.textAutoResize = 'HEIGHT';
if (parent) parent.appendChild(t);
if (o.w === 'fill') { t.layoutSizingHorizontal = 'FILL'; }
else if (o.w !== undefined) { t.resize(o.w, t.height); t.layoutSizingHorizontal = 'FIXED'; }
if (STYLE[type]) await t.setTextStyleIdAsync(STYLE[type].id);
if (o.w !== undefined && !(t.width > 0)) throw new Error('text collapsed: ' + chars);
return keep(t);
}
async function rich(parent, type, segs, o = {}) {
const t = await txt(parent, type, segs.map(s => s[0]).join(''), o);
let at = 0;
for (const [s, hex] of segs) { if (hex) t.setRangeFills(at, at + s.length, [SOLID(hex)]); at += s.length; }
return t;
}
function al(dir, parent, o = {}) {
const f = createAutoLayout(dir);
f.name = o.name || (dir === 'VERTICAL' ? 'Column' : 'Row');
f.itemSpacing = o.gap || 0;
const pad = o.pad === undefined ? 0 : o.pad; const [pt, pr, pb, pl] = Array.isArray(pad) ? pad : [pad, pad, pad, pad];
f.paddingTop = pt; f.paddingRight = pr; f.paddingBottom = pb; f.paddingLeft = pl;
f.primaryAxisAlignItems = o.main || 'MIN'; f.counterAxisAlignItems = o.cross || 'MIN';
paint(f, o);
if (o.radius !== undefined) f.cornerRadius = o.radius;
f.clipsContent = !!o.clip;
if (parent) parent.appendChild(f);
size(f, o);
return keep(f);
}
function paint(node, o) {
node.fills = o.fill ? [o.fill[0] === '#' ? SOLID(o.fill, o.fillOpacity) : tokPaint(o.fill, o.fillOpacity)] : [];
if (o.stroke) { node.strokes = [o.stroke[0] === '#' ? SOLID(o.stroke, o.strokeOpacity) : tokPaint(o.stroke, o.strokeOpacity)]; node.strokeWeight = o.strokeW || 1; node.strokeAlign = o.strokeAlign || 'INSIDE'; }
if (o.effect) node.effects = o.effect;
}
function size(f, o) {
const w = o.w, h = o.h;
const fw = typeof w === 'number' ? w : f.width, fh = typeof h === 'number' ? h : f.height;
if (typeof w === 'number' || typeof h === 'number') f.resize(Math.max(1, fw), Math.max(1, fh));
if ('layoutMode' in f && f.layoutMode !== 'NONE') {
f.layoutSizingHorizontal = w === 'fill' ? 'FILL' : typeof w === 'number' ? 'FIXED' : 'HUG';
f.layoutSizingVertical = h === 'fill' ? 'FILL' : typeof h === 'number' ? 'FIXED' : 'HUG';
} else {
if (w === 'fill') f.layoutSizingHorizontal = 'FILL';
if (h === 'fill') f.layoutSizingVertical = 'FILL';
}
}
function col(parent, o) { return al('VERTICAL', parent, o); }
function row(parent, o) { return al('HORIZONTAL', parent, o); }
function frame(parent, o = {}) {
const f = figma.createFrame();
f.name = o.name || 'Frame';
paint(f, o);
if (o.radius !== undefined) f.cornerRadius = o.radius;
f.clipsContent = o.clip !== false;
if (parent) parent.appendChild(f);
f.resize(o.w || 100, o.h || 100);
if (o.x !== undefined) f.x = o.x; if (o.y !== undefined) f.y = o.y;
return keep(f);
}
function rect(parent, o = {}) {
const r = figma.createRectangle();
r.name = o.name || 'Rect';
paint(r, o);
if (o.radius !== undefined) r.cornerRadius = o.radius;
if (parent) parent.appendChild(r);
r.resize(typeof o.w === 'number' ? o.w : 10, typeof o.h === 'number' ? o.h : 10);
if (o.x !== undefined) r.x = o.x; if (o.y !== undefined) r.y = o.y;
if (o.w === 'fill') r.layoutSizingHorizontal = 'FILL';
if (o.h === 'fill') r.layoutSizingVertical = 'FILL';
return keep(r);
}
function ellipse(parent, o = {}) {
const e = figma.createEllipse();
e.name = o.name || 'Ellipse';
paint(e, o);
if (parent) parent.appendChild(e);
e.resize(o.d || 10, o.d || 10);
if (o.x !== undefined) e.x = o.x; if (o.y !== undefined) e.y = o.y;
return keep(e);
}
function rule(parent, o = {}) { return rect(parent, { name: 'Rule', h: o.h || 1, w: 'fill', fill: o.fill || 'color/border/subtle', fillOpacity: o.opacity }); }
function absolute(node, x, y) { node.layoutPositioning = 'ABSOLUTE'; node.x = x; node.y = y; return node; }
function glow(hex, radius = 24, a = 0.45, spread = 0) {
return { type: 'DROP_SHADOW', color: RGBA(hex, a), offset: { x: 0, y: 0 }, radius, spread, visible: true, blendMode: 'NORMAL' };
}
function shadow(a = 0.45, y = 10, radius = 28) {
return { type: 'DROP_SHADOW', color: { r: 0, g: 0, b: 0, a }, offset: { x: 0, y }, radius, spread: 0, visible: true, blendMode: 'NORMAL' };
}
const ICONS = {
pause:   { f: true, d: '<rect x="6" y="4" width="4.5" height="16" rx="1.2"/><rect x="13.5" y="4" width="4.5" height="16" rx="1.2"/>' },
play:    { f: true, d: '<path d="M7 4.5v15a1 1 0 0 0 1.5.86l12-7.5a1 1 0 0 0 0-1.72l-12-7.5A1 1 0 0 0 7 4.5z"/>' },
stop:    { f: true, d: '<rect x="5.5" y="5.5" width="13" height="13" rx="2.5"/>' },
skip:    { f: true, d: '<path d="M5 5.2v13.6a1 1 0 0 0 1.55.83l10-6.8a1 1 0 0 0 0-1.66l-10-6.8A1 1 0 0 0 5 5.2z"/><rect x="17" y="4" width="3.5" height="16" rx="1.2"/>' },
replay:  { d: '<path d="M3.5 12a8.5 8.5 0 1 0 2.9-6.4"/><path d="M3.5 3.5v6h6"/>' },
cc:      { d: '<rect x="2.5" y="5" width="19" height="14" rx="3"/><path d="M10.5 10.3a2.6 2.6 0 1 0 0 3.4"/><path d="M17.5 10.3a2.6 2.6 0 1 0 0 3.4"/>' },
volume:  { d: '<path d="M4 9.5v5h3.5L12 18.5v-13L7.5 9.5z"/><path d="M15.5 9.2a4 4 0 0 1 0 5.6"/><path d="M18.3 6.4a8 8 0 0 1 0 11.2"/>' },
list:    { d: '<path d="M9 6h12M9 12h12M9 18h12"/><circle cx="4.5" cy="6" r="1.3" fill="CUR" stroke="none"/><circle cx="4.5" cy="12" r="1.3" fill="CUR" stroke="none"/><circle cx="4.5" cy="18" r="1.3" fill="CUR" stroke="none"/>' },
lock:    { d: '<rect x="5" y="11" width="14" height="10" rx="2.5"/><path d="M8 11V7.5a4 4 0 0 1 8 0V11"/>' },
check:   { d: '<path d="M5 12.5l4.5 4.5L19 7.5"/>' },
close:   { d: '<path d="M6.5 6.5l11 11M17.5 6.5l-11 11"/>' },
chevron: { d: '<path d="M9 5.5l6.5 6.5L9 18.5"/>' },
lens:    { d: '<circle cx="10.5" cy="10.5" r="6.5"/><path d="M15.5 15.5L21 21"/>' },
target:  { d: '<circle cx="12" cy="12" r="8.5"/><circle cx="12" cy="12" r="3" fill="CUR" stroke="none"/>' },
mic:     { d: '<rect x="9" y="3" width="6" height="11" rx="3"/><path d="M5.5 11.5a6.5 6.5 0 0 0 13 0"/><path d="M12 18v3"/>' },
send:    { d: '<path d="M4 12h15"/><path d="M13 6l6 6-6 6"/>' },
exit:    { d: '<path d="M10 4.5H5.5v15H10"/><path d="M14.5 8l4.5 4-4.5 4"/><path d="M19 12H9.5"/>' },
bulb:    { d: '<path d="M9.5 18.5h5"/><path d="M10.5 21h3"/><path d="M8.6 15.2a6 6 0 1 1 6.8 0c-.7.6-1.4 1.5-1.4 2.8h-4c0-1.3-.7-2.2-1.4-2.8z"/>' },
info:    { d: '<circle cx="12" cy="12" r="9"/><path d="M12 11v5.5"/><circle cx="12" cy="7.8" r="1.1" fill="CUR" stroke="none"/>' },
bolt:    { f: true, d: '<path d="M13.2 2.2L4.5 13.5h6.3l-1 8.3 8.7-11.3h-6.3z"/>' },
swap:    { d: '<path d="M4 7.5h12.5l-3-3"/><path d="M20 16.5H7.5l3 3"/>' },
plus:    { d: '<path d="M12 5v14M5 12h14"/>' },
graph:   { d: '<path d="M4 20h16"/><path d="M4 20V4"/><path d="M7 15l4-4 3 3 5-6"/>' },
settings:{ d: '<path d="M4 7h10M18 7h2M4 17h4M12 17h8"/><circle cx="16" cy="7" r="2.5"/><circle cx="10" cy="17" r="2.5"/>' },
alert:   { d: '<path d="M12 4l9 16H3z"/><path d="M12 10v4"/><circle cx="12" cy="17" r="1" fill="CUR" stroke="none"/>' },
};
function iconSvg(name, hex, size = 24) {
const i = ICONS[name]; if (!i) throw new Error('unknown icon ' + name);
const body = i.d.split('CUR').join(hex);
const attrs = i.f ? `fill="${hex}"` : `fill="none" stroke="${hex}" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"`;
return `<svg width="${size}" height="${size}" viewBox="0 0 24 24" xmlns="http://www.w3.org/2000/svg" ${attrs}>${body}</svg>`;
}
function icon(parent, name, hex = P.text, size = 24) {
const f = figma.createNodeFromSvg(iconSvg(name, hex, size));
f.name = 'Icon/' + name;
f.fills = [];
if (parent) parent.appendChild(f);
f.resize(size, size);
return keep(f);
}
function tint(node, hex) {
for (const v of node.findAllWithCriteria({ types: ['VECTOR', 'ELLIPSE', 'RECTANGLE', 'BOOLEAN_OPERATION'] })) {
if (v.fills && v.fills.length) v.fills = [SOLID(hex)];
if (v.strokes && v.strokes.length) v.strokes = [SOLID(hex)];
}
return node;
}
async function card(parent, o = {}) {
const c = col(parent, Object.assign({ name: 'Card', gap: 12, pad: 22, fill: 'color/bg/primary', fillOpacity: 0.92,
stroke: 'color/border/subtle', strokeW: 1, radius: 18, clip: true, effect: [shadow(0.35, 8, 24)] }, o));
if (STYLE['Effect/Card']) await c.setEffectStyleIdAsync(STYLE['Effect/Card'].id);
if (TOK['radius/lg'] && o.radius === undefined) radiusTok(c, 'radius/lg');
return c;
}
const CURSOR = { x: 0, y: 0 };
function section(page, name, w, h, x, y) {
const s = figma.createSection();
s.name = name;
page.appendChild(s);
s.x = x; s.y = y;
s.resizeWithoutConstraints(w, h);
s.fills = [SOLID('#0B1522')];
return keep(s);
}
function fit(s, pad = 80) {
let mx = 0, my = 0;
for (const c of s.children) { mx = Math.max(mx, c.x + c.width); my = Math.max(my, c.y + c.height); }
s.resizeWithoutConstraints(Math.max(mx + pad, 400), Math.max(my + pad, 200));
return s;
}
function comp(page, name) {
const found = page.findAllWithCriteria({ types: ['COMPONENT', 'COMPONENT_SET'] }).find(n => n.name === name);
if (!found) throw new Error('component not found: ' + name);
return found;
}
function variantOf(set, props) {
if (set.type === 'COMPONENT') return set;
const want = Object.entries(props || {});
const hit = set.children.find(c => want.every(([k, v]) => c.name.split(', ').includes(`${k}=${v}`)));
return hit || set.defaultVariant;
}
function inst(parent, set, props) { const i = variantOf(set, props).createInstance(); if (parent) parent.appendChild(i); return keep(i); }
async function setText(node, layerName, chars) {
const t = node.findAllWithCriteria({ types: ['TEXT'] }).find(n => n.name === layerName);
if (!t) throw new Error('text layer not found: ' + layerName);
await Promise.all(t.getStyledTextSegments(['fontName']).map(s => figma.loadFontAsync(s.fontName)));
t.characters = chars;
return t;
}
function placeGrid(nodes, cols, gapX, gapY, x0 = 0, y0 = 0) {
let x = x0, y = y0, rowH = 0, c = 0;
for (const n of nodes) {
n.x = x; n.y = y; rowH = Math.max(rowH, n.height); x += n.width + gapX; c++;
if (c === cols) { c = 0; x = x0; y += rowH + gapY; rowH = 0; }
}
}
function cAL(dir, o = {}) {
const c = figma.createComponent();
c.name = o.name || 'Component';
if (dir !== 'NONE') {
c.layoutMode = dir;
c.primaryAxisSizingMode = 'AUTO'; c.counterAxisSizingMode = 'AUTO';
c.itemSpacing = o.gap || 0;
const pad = o.pad === undefined ? 0 : o.pad; const [pt, pr, pb, pl] = Array.isArray(pad) ? pad : [pad, pad, pad, pad];
c.paddingTop = pt; c.paddingRight = pr; c.paddingBottom = pb; c.paddingLeft = pl;
c.primaryAxisAlignItems = o.main || 'MIN'; c.counterAxisAlignItems = o.cross || 'MIN';
}
paint(c, o);
if (o.radius !== undefined) c.cornerRadius = o.radius;
c.clipsContent = !!o.clip;
size(c, o);
if (o.desc) c.description = o.desc;
return keep(c);
}
function variantSet(name, parent, comps, cols, desc) {
const set = figma.combineAsVariants(comps, parent);
set.name = name;
if (desc) set.description = desc;
placeGrid(set.children, cols, 24, 24, 20, 20);
let mx = 0, my = 0;
for (const ch of set.children) { mx = Math.max(mx, ch.x + ch.width); my = Math.max(my, ch.y + ch.height); }
set.resizeWithoutConstraints(mx + 20, my + 20);
return keep(set);
}
function setProps(i, values) {
const props = i.componentProperties, out = {};
for (const [name, val] of Object.entries(values)) {
const key = Object.keys(props).find(k => k.split('#')[0] === name);
if (!key) throw new Error('no property ' + name + ' on ' + i.name);
out[key] = val;
}
i.setProperties(out);
return i;
}
const ICONC = {};
function loadIcons(page) {
for (const c of page.findAllWithCriteria({ types: ['COMPONENT'] })) if (c.name.startsWith('Icon/')) ICONC[c.name.slice(5)] = c;
if (!ICONC.pause) throw new Error('icon components missing — run script 01 first');
return Object.keys(ICONC).length;
}
function iconInst(parent, name, hex, size = 24) {
const c = ICONC[name]; if (!c) throw new Error('no icon component ' + name);
const i = c.createInstance();
if (parent) parent.appendChild(i);
i.resize(size, size);
if (hex && hex !== P.text) tint(i, hex);
return keep(i);
}
async function slot(sec, cursor, node, caption) {
const cap = await txt(sec, 'Label/Small', caption, { color: '#7A8FA6' });
cap.x = 40; cap.y = cursor.y;
node.x = 40; node.y = cursor.y + 28;
cursor.y += 28 + node.height + 64;
return node;
}
const BG = {};   // backdrop key → imageHash (set at the top of each screens script after upload_assets)
const SCREEN = { w: 1920, h: 1080, margin: 32, top: 28, dockTop: 104, dock: 480, wide: 580, bottom: 40 };
const CMP = {};  // component sets/components by name, loaded by loadComponents(dsPage)
function loadComponents(page) {
for (const n of page.findAllWithCriteria({ types: ['COMPONENT', 'COMPONENT_SET'] })) if (!n.name.startsWith('Icon/') && !(n.parent && n.parent.type === 'COMPONENT_SET')) CMP[n.name] = n;
if (!CMP['Control bar']) throw new Error('design system components missing — run scripts 01, 02a, 02b first');
return Object.keys(CMP).length;
}
function screen(page, name, bgKey, w = SCREEN.w, h = SCREEN.h) {
const f = frame(page, { name, w, h, fill: '#081A2B', clip: true });
const bg = rect(f, { name: 'Backdrop · 3D scene (' + bgKey + ', not UI)', w, h, x: 0, y: 0 });
bg.fills = BG[bgKey] ? [{ type: 'IMAGE', scaleMode: 'FILL', imageHash: BG[bgKey] }] : [SOLID('#0E2238')];
bg.locked = true;
return f;
}
function group(page, name) {
const old = page.children.find(n => n.type === 'SECTION' && n.name === name); if (old) old.remove();
let y = 0; for (const c of page.children) y = Math.max(y, c.y + c.height); if (page.children.length) y += 240;
return section(page, name, 1000, 1000, 0, y);
}
async function placeScreens(sec, items, cols = 2, gapX = 160, gapY = 220) {
const head = await txt(sec, 'Heading/Screen', sec.name, { color: '#F5F7FB' }); head.x = 40; head.y = 36;
let i = 0, w = 0, h = 0;
for (const f of items.map(it => it[0])) { w = Math.max(w, f.width); h = Math.max(h, f.height); }
for (const [f, title, note] of items) {
const c = i % cols, r = Math.floor(i / cols);
f.x = 40 + c * (w + gapX); f.y = 200 + r * (h + gapY);
const t = await txt(sec, 'Heading/Panel', title, { color: '#F5F7FB' }); t.x = f.x; t.y = f.y - 70;
if (note) { const n = await txt(sec, 'Body/Secondary', note, { color: '#AFC5D5', w: f.width }); n.x = f.x; n.y = f.y - 38; }
i++;
}
fit(sec, 120);
return sec;
}
async function setRich(node, layerName, segs) {
const t = await setText(node, layerName, segs.map(s => s[0]).join(''));
let at = 0;
for (const [s, hex] of segs) { if (hex) t.setRangeFills(at, at + s.length, [SOLID(hex)]); at += s.length; }
return t;
}
function nested(instance, name) { return instance.findAllWithCriteria({ types: ['INSTANCE'] }).find(n => n.name === name) || null; }
function swapState(instance, set, props) { const v = variantOf(set, props); if (v !== instance.mainComponent) instance.swapComponent(v); return instance; }
async function chipAt(f, s, chapter, title) {
const i = inst(f, CMP['Chapter chip'], { Variant: title ? 'Titled' : 'Compact' });
await setRich(i, 'Number', [[String(chapter), null], [' / 10', P.text2]]);
if (title) await setText(i, 'Title', title);
const rail = nested(i, 'Rail');
if (rail) rail.findAllWithCriteria({ types: ['INSTANCE'] }).forEach((seg, n) => swapState(seg, CMP['Rail segment'], { State: n + 1 < chapter ? 'Done' : n + 1 === chapter ? 'Now' : 'Ahead' }));
if (s !== 1) i.rescale(s);
i.x = SCREEN.margin * s; i.y = SCREEN.top * s;
return i;
}
function controlsAt(f, s, o = {}) {
const i = inst(f, CMP['Control bar'], { Variant: o.folded ? 'Folded' : 'Full' });
for (const [label, state] of Object.entries(o.states || {})) { const c = nested(i, 'Control ' + label); if (c) swapState(c, CMP.Control, { State: state }); }
if (o.resume) { const c = nested(i, 'Control PAUSE'); if (c) { setProps(c, { Label: 'RESUME', Icon: ICONC.play.id }); } }
if (s !== 1) i.rescale(s);
i.x = f.width - SCREEN.margin * s - i.width; i.y = SCREEN.top * s;
return i;
}
function avatarAt(f, s, state = 'Idle') {
const i = inst(f, CMP['Ask Wattson avatar'], { State: state });
if (s !== 1) i.rescale(s);
i.x = f.width - 26 * s - i.width; i.y = f.height - 26 * s - i.height;
return i;
}
async function captionAt(f, s, speaker, line, o = {}) {
const i = inst(f, CMP['Caption bar'], { Variant: o.portrait ? 'Portrait' : 'Centred' });
await setText(i, 'Speaker', speaker); await setText(i, 'Line', line);
if (s !== 1) i.rescale(s);
const cover = (o.cover || 0) * s;
i.x = (f.width - cover - i.width) / 2; i.y = f.height - SCREEN.bottom * s - i.height;
return i;
}
function dockAt(f, s, wide = false) {
const w = (wide ? SCREEN.wide : SCREEN.dock) * s;
const d = col(f, { name: 'UI_Dock', gap: 12 * s, w });
d.x = f.width - SCREEN.margin * s - w; d.y = SCREEN.dockTop * s;
return d;
}
function dockItem(d, i) { d.appendChild(i); i.layoutSizingHorizontal = 'FILL'; return i; }
async function turnAt(d, s, state, eyebrowText, title, hint) {
const i = inst(null, CMP['Turn card'], { State: state });
await setText(i, 'Eyebrow', eyebrowText); await setText(i, 'Title', title);
if (hint !== undefined && state !== 'Done') await setText(i, 'Hint', hint);
if (s !== 1) i.rescale(s);
return dockItem(d, i);
}
async function clueAt(d, s, title, segs, variant = 'Clue') {
const i = inst(null, CMP['Clue card'], { Variant: variant });
if (variant === 'Clue') await setText(i, 'Title', title);
await setRich(i, 'Body', segs);
if (s !== 1) i.rescale(s);
return dockItem(d, i);
}
function hintAt(f, s, kind, message) {
const i = inst(f, CMP.Toast, { Kind: kind });
setProps(i, { Message: message });
if (s !== 1) i.rescale(s);
i.x = f.width - SCREEN.margin * s - i.width; i.y = (SCREEN.top + 56 + 10) * s;
return i;
}


// ---- plugin runtime shims (the MCP runtime has these built in) ----
function createAutoLayout(dir) { const f = figma.createFrame(); f.layoutMode = typeof dir === 'string' ? dir : 'HORIZONTAL'; f.primaryAxisSizingMode = 'AUTO'; f.counterAxisSizingMode = 'AUTO'; f.fills = []; return f; }
const log = m => { try { figma.ui.postMessage({ log: m }); } catch (e) {} };

async function part_01_foundations() {
await resolveFonts();
const PAGES = ['01 Design System', '02 Screens', '03 Reference Board'];
const PAGE = {};
const spare = figma.root.children.filter(c => !PAGES.includes(c.name));
for (const name of PAGES) {
let p = figma.root.children.find(c => c.name === name);
if (!p) { p = spare.shift(); if (p) p.name = name; else { p = figma.createPage(); p.name = name; } }
PAGE[name] = p;
}
const ds = PAGE[PAGES[0]];
await figma.setCurrentPageAsync(ds);
{
const shots = ds.children.filter(n => n.type !== 'SECTION' && 'fills' in n && Array.isArray(n.fills) && n.fills.some(f => f.type === 'IMAGE'));
if (shots.length) {
let sec = ds.children.find(n => n.type === 'SECTION' && n.name.startsWith('00 Backdrops'));
if (!sec) { sec = figma.createSection(); sec.name = '00 Backdrops · 3D frames captured from the editor (not UI)'; ds.appendChild(sec); sec.fills = [SOLID('#0B1522')]; }
let x = 40;
for (const n of shots) { sec.appendChild(n); if (n.width > 0) n.rescale(480 / n.width); n.x = x; n.y = 60; x += n.width + 24; }
sec.x = 0; sec.y = -520; sec.resizeWithoutConstraints(x + 40, 60 + 270 + 60);
keep(sec);
}
}
const collections = await figma.variables.getLocalVariableCollectionsAsync();
function collection(name, mode) {
let c = collections.find(x => x.name === name);
if (!c) { c = figma.variables.createVariableCollection(name); c.renameMode(c.modes[0].modeId, mode); }
return c;
}
const prim = collection('Primitives', 'Value'), sem = collection('Ohm UI', 'Default');
await loadTokens();
const made = [];
function variable(coll, name, type, value, scopes, css) {
let v = TOK[name];
if (!v) { v = figma.variables.createVariable(name, coll, type); made.push(name); }
v.setValueForMode(coll.modes[0].modeId, value);
v.scopes = scopes;
if (css) v.setVariableCodeSyntax('WEB', 'var(--' + css + ')');
TOK[name] = v;
return v;
}
const alias = v => ({ type: 'VARIABLE_ALIAS', id: v.id });
const PRIM = { 'navy/950': P.navy950, 'navy/900': P.navy900, 'navy/800': P.navy800, 'navy/700': P.navy700, 'blue/500': P.blue, 'cyan/400': P.cyan,
'gold/400': P.gold, 'green/400': P.green, 'red/400': P.red, 'white/100': P.text, 'grey/300': P.text2, 'grey/500': P.muted,
'quantity/volt': P.volt, 'quantity/amp': P.amp, 'quantity/ohm': P.ohm, 'quantity/charge': P.charge };
for (const [n, hex] of Object.entries(PRIM)) variable(prim, n, 'COLOR', RGBA(hex), []);
const FILL = ['FRAME_FILL', 'SHAPE_FILL'], ANY = ['ALL_FILLS', 'STROKE_COLOR'], TEXTC = ['TEXT_FILL'], STROKE = ['STROKE_COLOR'];
const SEM = [
['color/bg/primary', 'navy/900', FILL, 'Cards, menus, HUD bars (92 % over the scene)'],
['color/bg/secondary', 'navy/800', FILL, 'Rows, inputs, option tiles'],
['color/bg/hover', 'navy/700', FILL, 'Hovered rows and tiles'],
['color/bg/scrim', 'navy/950', FILL, 'Pause scrim at 72 %'],
['color/accent/blue', 'blue/500', ANY, 'Selected, progress done, graph line'],
['color/accent/cyan', 'cyan/400', ANY, 'Interactive borders, eyebrows, icons, Wattson'],
['color/accent/gold', 'gold/400', ANY, 'Primary CTA, discoveries, the formula'],
['color/state/success', 'green/400', ANY, 'Done, chapter complete'],
['color/state/error', 'red/400', ANY, 'A wrong tile; nothing else'],
['color/text/primary', 'white/100', TEXTC, 'Text'],
['color/text/secondary', 'grey/300', TEXTC, 'Descriptions, hints'],
['color/text/muted', 'grey/500', TEXTC, 'Disabled, later steps'],
['color/text/on-gold', 'navy/900', TEXTC, 'Label on the gold CTA'],
['color/quantity/voltage', 'quantity/volt', ANY, 'V — the push (project palette)'],
['color/quantity/current', 'quantity/amp', ANY, 'I — the flow (project palette)'],
['color/quantity/resistance', 'quantity/ohm', ANY, 'R — the opposition (project palette)'],
['color/quantity/charge', 'quantity/charge', ANY, 'Electrons'],
];
for (const [n, p, scopes] of SEM) variable(sem, n, 'COLOR', alias(TOK[p]), scopes, n.replace(/\//g, '-'));
variable(sem, 'color/border/subtle', 'COLOR', RGBA(P.cyan, 0.22), STROKE, 'color-border-subtle');
variable(sem, 'color/border/interactive', 'COLOR', RGBA(P.cyan, 0.55), STROKE, 'color-border-interactive');
variable(sem, 'color/border/focus', 'COLOR', RGBA(P.cyan, 1), STROKE, 'color-border-focus');
variable(sem, 'color/border/gold', 'COLOR', RGBA(P.gold, 0.8), STROKE, 'color-border-gold');
const NUM = { 'space/1': [4, ['GAP']], 'space/2': [8, ['GAP']], 'space/3': [12, ['GAP']], 'space/4': [16, ['GAP']], 'space/5': [20, ['GAP']], 'space/6': [24, ['GAP']], 'space/8': [32, ['GAP']],
'radius/sm': [8, ['CORNER_RADIUS']], 'radius/md': [12, ['CORNER_RADIUS']], 'radius/lg': [18, ['CORNER_RADIUS']], 'radius/xl': [24, ['CORNER_RADIUS']], 'radius/pill': [999, ['CORNER_RADIUS']],
'size/control': [44, ['WIDTH_HEIGHT']], 'size/touch': [48, ['WIDTH_HEIGHT']], 'size/button': [56, ['WIDTH_HEIGHT']], 'size/cta': [68, ['WIDTH_HEIGHT']],
'stroke/hairline': [1, ['STROKE_FLOAT']], 'stroke/outline': [1.5, ['STROKE_FLOAT']] };
for (const [n, [v, scopes]] of Object.entries(NUM)) variable(sem, n, 'FLOAT', v, scopes, n.replace(/\//g, '-'));
await loadStyles();
const UNITY_FONT = { sansBold: 'Nunito Sans Bold (Nunito SDF)', sansSemi: 'Nunito Sans SemiBold (new TMP asset, OFL)', serifBlack: 'Cinzel Black (new TMP asset, OFL)', serifBold: 'Cinzel Bold (new TMP asset, OFL)' };
const styleIds = {};
for (const [name, [key, size, lh, ls, upper]] of Object.entries(TYPE)) {
let s = STYLE[name];
if (!s) { s = figma.createTextStyle(); s.name = name; STYLE[name] = s; }
s.fontName = FONT[key]; s.fontSize = size;
s.lineHeight = { value: lh, unit: 'PERCENT' }; s.letterSpacing = { value: ls, unit: 'PERCENT' };
s.textCase = upper ? 'UPPER' : 'ORIGINAL';
s.description = `Unity: ${UNITY_FONT[key]} ${size} px at 1080p · line ${lh} % · tracking ${ls} %${upper ? ' · caps' : ''}`;
styleIds[name] = s.id;
}
const EFFECTS = {
'Effect/Card': [shadow(0.35, 8, 24)],
'Effect/Glow Cyan': [glow(P.cyan, 20, 0.55)],
'Effect/Glow Blue': [glow(P.blue, 18, 0.65)],
'Effect/Glow Gold': [glow(P.gold, 26, 0.6)],
'Effect/Focus Ring': [glow(P.cyan, 0, 1, 3)],
'Effect/Glass Blur': [{ type: 'BACKGROUND_BLUR', radius: 16, visible: true }],
};
for (const [name, effects] of Object.entries(EFFECTS)) {
let s = STYLE[name];
if (!s) { s = figma.createEffectStyle(); s.name = name; STYLE[name] = s; }
s.effects = effects;
s.description = name === 'Effect/Glass Blur' ? 'Optional in Unity (URP blur costs a pass); cards read fine without it' : 'Unity: baked into the card sprite / UI shadow';
}
for (const s of ds.findAllWithCriteria({ types: ['SECTION'] })) if (/^0[1-5] /.test(s.name)) s.remove();
async function titled(sec, title, sub) {
const t = await txt(sec, 'Heading/Screen', title, { color: '#F5F7FB' }); t.x = 40; t.y = 28;
if (sub) { const s = await txt(sec, 'Body/Secondary', sub, { color: '#AFC5D5', w: 1200 }); s.x = 40; s.y = 28 + t.height + 6; }
}
let Y = 0;
const W = 2200;
const cover = section(ds, '01 Cover', W, 420, 0, Y);
const coverCol = col(cover, { name: 'Cover', gap: 10, pad: 48, w: W, fill: '#081A2B' });
await txt(coverCol, 'Label/Eyebrow', 'Ohm’s Law  ·  UI v4  ·  design system', { color: 'color/accent/cyan' });
await txt(coverCol, 'Display/Title', 'OHM’S LAW', { color: 'color/accent/gold' });
await txt(coverCol, 'Body/Large', 'Cinematic scientific mystery. Dark navy, electric blue, warm gold, cyan hairlines. One system for the whole module: title, HUD, cards, experiment, Ask Wattson, pause, finale.', { color: 'color/text/secondary', w: 1400 });
await txt(coverCol, 'Body/Secondary', 'Reference: Ohms_Law_UI_UX_Figma_Reference_Guide.pdf (10 pages) · Source of truth for behaviour: the running Unity module (Scenes 04–13). Every component here maps to a real HUD element in Shared/LessonPanel.cs, HudScreens.cs, SubtitleUI.cs, PanelViews.cs, AskWattsonUI.cs.', { color: 'color/text/muted', w: 1400 });
fit(cover, 0); Y += cover.height + 120;
const colour = section(ds, '02 Colour', W, 100, 0, Y);
await titled(colour, 'Colour', 'Semantic tokens bound on every component. Blue / amber / red on quantities only mean V, I and R; the UI uses blue for selection, gold for the primary call to action, cyan for every interactive edge.');
const swatches = al('HORIZONTAL', colour, { name: 'Swatches', gap: 16, pad: 0 });
swatches.layoutWrap = 'WRAP'; swatches.counterAxisSpacing = 16;
swatches.x = 40; swatches.y = 120;
swatches.resize(W - 80, 100); swatches.layoutSizingHorizontal = 'FIXED'; swatches.layoutSizingVertical = 'HUG';
const SWATCH = [...SEM.map(([n, , , use]) => [n, use]), ['color/border/subtle', 'Card edge · 22 %'], ['color/border/interactive', 'Buttons, chips · 55 %'], ['color/border/focus', 'Keyboard focus'], ['color/border/gold', 'CTA edge']];
for (const [name, use] of SWATCH) {
const tile = col(swatches, { name, gap: 10, pad: 0, w: 248 });
const chip = rect(tile, { name: 'Swatch', w: 248, h: 72, radius: 12, fill: '#000000' });
fillTok(chip, name);
await txt(tile, 'Label/Small', name, { color: 'color/text/primary' });
await txt(tile, 'Body/Secondary', use, { color: 'color/text/secondary', w: 248 });
}
fit(colour); Y += colour.height + 120;
const type = section(ds, '03 Type', W, 100, 0, Y);
await titled(type, 'Type', 'Cinzel for the two cinematic moments (title, chapter card). Nunito Sans for everything else — Bold for headings, buttons and labels; SemiBold for body and captions. Nothing under 15 px at 1080p.');
const specimen = col(type, { name: 'Specimen', gap: 18, pad: 0 });
specimen.x = 40; specimen.y = 120;
for (const name of Object.keys(TYPE)) {
const line = row(specimen, { name, gap: 24, cross: 'CENTER' });
const meta = await txt(line, 'Label/Small', `${name}  ·  ${TYPE[name][1]} px`, { color: 'color/text/muted', w: 300 });
const sample = name.startsWith('Numeric') ? '5.0 V   1.00 A   5.0 Ω' : name.startsWith('Formula') ? 'V = I × R' : name.startsWith('Caption') ? (name === 'Caption/Speaker' ? 'WATTSON' : 'These are electric charges.') : name === 'Display/Title' ? 'OHM’S LAW' : name === 'Display/Chapter' ? 'Journey Inside the Wire' : name === 'Display/CTA' ? 'START JOURNEY' : name.startsWith('Label') ? 'YOUR TURN · STEP 2 OF 3' : 'Raise the voltage step by step and record the current.';
await txt(line, name, sample, { color: name.includes('Secondary') ? 'color/text/secondary' : name === 'Display/Title' || name.startsWith('Formula') ? 'color/accent/gold' : 'color/text/primary' });
meta.name = 'meta';
}
fit(type); Y += type.height + 120;
const sp = section(ds, '04 Spacing · Radius · Effects', W, 100, 0, Y);
await titled(sp, 'Spacing, radius, effects', 'Spacing 4 / 8 / 12 / 16 / 20 / 24 / 32. Radius 8 / 12 / 18 / 24 / pill. Controls 44, touch 48, buttons 56, CTA 68. Hairline 1, outline 1.5.');
const spRow = row(sp, { name: 'Spacing', gap: 24, cross: 'MAX' }); spRow.x = 40; spRow.y = 120;
for (const n of ['space/1', 'space/2', 'space/3', 'space/4', 'space/5', 'space/6', 'space/8']) {
const v = NUM[n][0];
const cell = col(spRow, { name: n, gap: 8, cross: 'CENTER' });
const bar = rect(cell, { name: 'bar', w: v * 3 + 8, h: v, fill: 'color/accent/cyan' });
bindNum(bar, 'height', n);
await txt(cell, 'Label/Small', `${n.split('/')[1]} · ${v}`, { color: 'color/text/secondary' });
}
const rdRow = row(sp, { name: 'Radius', gap: 24, cross: 'MAX' }); rdRow.x = 40; rdRow.y = 240;
for (const n of ['radius/sm', 'radius/md', 'radius/lg', 'radius/xl', 'radius/pill']) {
const cell = col(rdRow, { name: n, gap: 8, cross: 'CENTER' });
const box = rect(cell, { name: 'box', w: 120, h: 72, fill: 'color/bg/secondary', stroke: 'color/border/interactive', radius: Math.min(NUM[n][0], 36) });
radiusTok(box, n);
await txt(cell, 'Label/Small', `${n.split('/')[1]} · ${NUM[n][0]}`, { color: 'color/text/secondary' });
}
const fxRow = row(sp, { name: 'Effects', gap: 40, cross: 'MAX' }); fxRow.x = 40; fxRow.y = 380;
for (const n of Object.keys(EFFECTS)) {
const cell = col(fxRow, { name: n, gap: 12, cross: 'CENTER' });
const box = rect(cell, { name: 'box', w: 160, h: 90, fill: 'color/bg/primary', stroke: 'color/border/subtle', radius: 18 });
await box.setEffectStyleIdAsync(STYLE[n].id);
await txt(cell, 'Label/Small', n.split('/')[1], { color: 'color/text/secondary' });
}
fit(sp); Y += sp.height + 120;
const ic = section(ds, '05 Icons', W, 100, 0, Y);
await titled(ic, 'Icons', '24 px, 2 px round strokes (filled glyphs for transport controls). Unity: rasterised from these SVG paths in HudKit (replace the SDF glyphs).');
const iconGrid = al('HORIZONTAL', ic, { name: 'Grid', gap: 28, pad: 0 });
iconGrid.layoutWrap = 'WRAP'; iconGrid.counterAxisSpacing = 28; iconGrid.x = 40; iconGrid.y = 120;
iconGrid.resize(W - 80, 60); iconGrid.layoutSizingHorizontal = 'FIXED'; iconGrid.layoutSizingVertical = 'HUG';
const iconIds = {};
for (const name of Object.keys(ICONS)) {
const cell = col(iconGrid, { name: 'Icon cell ' + name, gap: 8, cross: 'CENTER', w: 96 });
const c = figma.createComponent();
c.name = 'Icon/' + name;
c.fills = [];
cell.appendChild(c);
c.resize(24, 24);
const svg = figma.createNodeFromSvg(iconSvg(name, '#F5F7FB', 24));
for (const child of [...svg.children]) c.appendChild(child);
svg.remove();
c.description = `HUD icon "${name}". Recolour by tinting the vectors; never add a variant per colour.`;
keep(c); iconIds[name] = c.id;
await txt(cell, 'Label/Small', name, { color: 'color/text/secondary' });
}
fit(ic); Y += ic.height + 120;
return {
pages: Object.fromEntries(Object.entries(PAGE).map(([k, v]) => [k, v.id])),
variablesCreated: made.length, variablesTotal: Object.keys(TOK).length,
fonts: FONT, textStyles: Object.keys(styleIds).length, effectStyles: Object.keys(EFFECTS).length,
icons: iconIds, sections: [cover.id, colour.id, type.id, sp.id, ic.id], nextY: Y, createdNodeIds: IDS.length,
};
}

async function part_02a_atoms() {
const AVATAR_HASH = 'c19228980982cb924feb4bb5da183c83a852a1cd';   // imageHash of WattsonFace.png once uploaded (upload_assets); empty = drawn fallback
await resolveFonts(); await loadTokens(); await loadStyles();
const ds = figma.root.children.find(p => p.name === '01 Design System');
if (!ds) throw new Error('run script 01 first');
await figma.setCurrentPageAsync(ds);
loadIcons(ds);
for (const s of ds.findAllWithCriteria({ types: ['SECTION'] })) if (/^0[6-8] /.test(s.name)) s.remove();
let Y = 0; for (const c of ds.children) Y = Math.max(Y, c.y + c.height); Y += 120;
const W = 2200;
const ROYAL = '#1557B8', ROYAL_HI = '#1D6BD9', ROYAL_LO = '#0F3F8F';
async function head(sec, title, sub) {
const t = await txt(sec, 'Heading/Screen', title, { color: '#F5F7FB' }); t.x = 40; t.y = 28;
const s = await txt(sec, 'Body/Secondary', sub, { color: '#AFC5D5', w: 1400 }); s.x = 40; s.y = 28 + t.height + 6;
}
const OUT = {};
const S6 = section(ds, '06 Buttons & controls', W, 100, 0, Y);
await head(S6, 'Buttons & controls', 'Button (4 styles × 4 states, Label text property, optional icon), the START JOURNEY call to action, the HUD Control (icon swap + label + 5 states) and the control bar in its full and idle-folded forms.');
const cur = { y: 120 };
const STYLES = ['Primary', 'Secondary', 'Ghost', 'Raised'], STATES = ['Default', 'Hover', 'Pressed', 'Disabled'];
const buttons = [];
for (const st of STYLES) for (const s of STATES) {
const c = cAL('HORIZONTAL', { name: `Style=${st}, State=${s}`, gap: 10, pad: [0, 28, 0, 28], main: 'CENTER', cross: 'CENTER', h: 56, radius: 28 });
bindNum(c, 'height', 'size/button');
let textTok = 'color/text/primary', iconHex = P.text;
if (st === 'Primary') {
c.fills = [s === 'Hover' ? SOLID('#FFD47A') : s === 'Pressed' ? SOLID('#E6B44A') : tokPaint('color/accent/gold', s === 'Disabled' ? 0.35 : 1)];
strokeTok(c, 'color/border/gold', 1); textTok = 'color/text/on-gold'; iconHex = P.navy900;
if (s === 'Hover') await c.setEffectStyleIdAsync(STYLE['Effect/Glow Gold'].id);
} else if (st === 'Secondary') {
fillTok(c, s === 'Hover' ? 'color/bg/hover' : s === 'Pressed' ? 'color/bg/primary' : 'color/bg/secondary', 0.85);
strokeTok(c, s === 'Default' || s === 'Disabled' ? 'color/border/interactive' : 'color/border/focus', 1.5);
if (s === 'Hover') await c.setEffectStyleIdAsync(STYLE['Effect/Glow Cyan'].id);
} else if (st === 'Ghost') {
c.fills = s === 'Hover' ? [SOLID('#FFFFFF', 0.08)] : s === 'Pressed' ? [SOLID('#FFFFFF', 0.14)] : [];
textTok = 'color/text/secondary'; iconHex = P.text2;
} else {
fillTok(c, s === 'Hover' ? 'color/bg/hover' : s === 'Pressed' ? 'color/bg/primary' : 'color/bg/secondary');
strokeTok(c, s === 'Hover' ? 'color/border/interactive' : 'color/border/subtle', 1);
}
if (s === 'Disabled' && st !== 'Primary') c.opacity = 0.36;
const showKey = c.addComponentProperty('Show icon', 'BOOLEAN', true);
const ic = iconInst(c, 'play', iconHex, 20);
ic.componentPropertyReferences = { visible: showKey };
const labelKey = c.addComponentProperty('Label', 'TEXT', 'Button');
const t = await txt(c, 'Button', 'Button', { color: textTok, name: 'Label' });
if (s === 'Disabled' && st === 'Primary') t.opacity = 0.6;
t.componentPropertyReferences = { characters: labelKey };
buttons.push(c);
}
OUT.Button = await slot(S6, cur, variantSet('Button', S6, buttons, 4, 'Unity: LessonPanel.Pill — Kind Primary / Secondary / Ghost / Raised; HudButton drives hover (150 ms), press (0.97, 90 ms), disabled (36 %).'), 'Button — Style × State');
const ctas = [];
for (const s of STATES) {
const c = cAL('HORIZONTAL', { name: `State=${s}`, gap: 14, pad: [0, 40, 0, 36], main: 'CENTER', cross: 'CENTER', h: 68, radius: 14, stroke: 'color/border/gold', strokeW: 2 });
bindNum(c, 'height', 'size/cta');
c.fills = [SOLID(s === 'Hover' ? ROYAL_HI : s === 'Pressed' ? ROYAL_LO : ROYAL)];
if (s !== 'Disabled') await c.setEffectStyleIdAsync(STYLE['Effect/Glow Gold'].id);
if (s === 'Disabled') c.opacity = 0.4;
const labelKey = c.addComponentProperty('Label', 'TEXT', 'START JOURNEY');
const t = await txt(c, 'Display/CTA', 'START JOURNEY', { color: 'color/text/primary', name: 'Label' });
t.componentPropertyReferences = { characters: labelKey };
iconInst(c, 'chevron', P.gold, 22);
ctas.push(c);
}
OUT.CTA = await slot(S6, cur, variantSet('Button/CTA', S6, ctas, 4, 'The one gold-framed call to action: START JOURNEY on the title screen (Replay the investigation on the Case closed card uses Button/Primary).'), 'Button/CTA — START JOURNEY');
const CSTATES = ['Default', 'Hover', 'Pressed', 'On', 'Disabled'];
const controls = [];
for (const s of CSTATES) {
const c = cAL('HORIZONTAL', { name: `State=${s}`, gap: 8, pad: [0, 14, 0, 12], main: 'CENTER', cross: 'CENTER', h: 44, radius: 10 });
bindNum(c, 'height', 'size/control');
c.fills = s === 'Hover' ? [SOLID('#FFFFFF', 0.08)] : s === 'Pressed' ? [SOLID('#FFFFFF', 0.14)] : s === 'On' ? [tokPaint('color/accent/cyan', 0.16)] : [];
if (s === 'Disabled') c.opacity = 0.36;
const hex = s === 'On' ? P.cyan : P.text;
const iconKey = c.addComponentProperty('Icon', 'INSTANCE_SWAP', ICONC.pause.id);
const ic = iconInst(c, 'pause', hex, 22);
ic.componentPropertyReferences = { mainComponent: iconKey };
const showKey = c.addComponentProperty('Show label', 'BOOLEAN', true);
const labelKey = c.addComponentProperty('Label', 'TEXT', 'PAUSE');
const t = await txt(c, 'Label/Control', 'PAUSE', { color: s === 'On' ? 'color/accent/cyan' : 'color/text/primary', name: 'Label' });
t.componentPropertyReferences = { characters: labelKey, visible: showKey };
controls.push(c);
}
OUT.Control = await slot(S6, cur, variantSet('Control', S6, controls, 5, 'One item of UI_TopControls (LessonPanel.Control). 44 px hit area, 9 px between items. On = CC enabled, PAUSE while paused, VOLUME while its popup is open.'), 'Control — HUD item states');
const bars = [];
for (const v of ['Full', 'Folded']) {
const c = cAL('HORIZONTAL', { name: `Variant=${v}`, gap: 6, pad: [0, 10, 0, 10], main: 'CENTER', cross: 'CENTER', h: 56, radius: 28, fill: 'color/bg/primary', fillOpacity: v === 'Full' ? 0.88 : 0.72, stroke: 'color/border/subtle', strokeW: 1 });
await c.setEffectStyleIdAsync(STYLE['Effect/Card'].id);
const items = [['pause', 'PAUSE'], ['replay', 'REPLAY'], ['cc', 'CC'], ['volume', 'VOLUME'], ['skip', 'SKIP']];
items.forEach(([ic, label], n) => {
if (n > 0) rect(c, { name: 'Divider', w: 1, h: 22, fill: '#FFFFFF', fillOpacity: 0.12 });
const i = inst(c, OUT.Control, { State: ic === 'cc' ? 'On' : 'Default' });
i.name = 'Control ' + label;
setProps(i, { Label: label, Icon: ICONC[ic].id, 'Show label': v === 'Full' });
});
bars.push(c);
}
OUT.ControlBar = await slot(S6, cur, variantSet('Control bar', S6, bars, 2, 'UI_TopControls: PAUSE · REPLAY · CC · VOLUME · SKIP in one capsule, top-right 32 / 28 px. Folds to icons after 3 s idle in cinematic chapters (1, 4, 5).'), 'Control bar — Full / Folded (idle)');
fit(S6); Y += S6.height + 120;
const S7 = section(ds, '07 Progress · chips · inputs', W, 100, 0, Y);
await head(S7, 'Progress, chips and inputs', 'Rail segment and the 10-step rail, the chapter chip, toggle, slider, key cap and the toast / hint pill. Everything here is a real HUD control: HudScreens.Rail, ChapterBanner chip, SwitchRow, LessonPanel.VolumeRow, PauseMenu keys, LessonPanel.ShowHint.');
cur.y = 120;
const segs = [];
for (const s of ['Done', 'Now', 'Ahead']) {
const c = cAL('NONE', { name: `State=${s}`, w: s === 'Now' ? 28 : 18, h: 4, radius: 2 });
c.fills = s === 'Done' ? [tokPaint('color/accent/blue')] : s === 'Now' ? [tokPaint('color/accent/cyan')] : [SOLID('#FFFFFF', 0.24)];
segs.push(c);
}
OUT.Segment = await slot(S7, cur, variantSet('Rail segment', S7, segs, 3, 'One chapter on the rail: done (blue), now (cyan, longer — shape as well as colour), ahead (white 24 %).'), 'Rail segment');
const rail = cAL('HORIZONTAL', { name: 'Progress rail', gap: 4, cross: 'CENTER', desc: 'Ten segments, one per chapter. Swap segment variants per chapter state.' });
for (let i = 0; i < 10; i++) { const seg = inst(rail, OUT.Segment, { State: i === 0 ? 'Done' : i === 1 ? 'Now' : 'Ahead' }); seg.name = 'Chapter ' + (i + 1); }
S7.appendChild(rail);
OUT.Rail = await slot(S7, cur, rail, 'Progress rail — 10 chapters');
const chips = [];
for (const v of ['Compact', 'Titled']) {
const c = cAL('HORIZONTAL', { name: `Variant=${v}`, gap: 14, pad: [0, 16, 0, 20], main: 'CENTER', cross: 'CENTER', h: 56, radius: 28, fill: 'color/bg/primary', fillOpacity: 0.88, stroke: 'color/border/subtle' });
await c.setEffectStyleIdAsync(STYLE['Effect/Card'].id);
await rich(c, 'Chip', [['2', null], [' / 10', P.text2]], { name: 'Number' });
if (v === 'Titled') await txt(c, 'Button/Small', 'Journey Inside the Wire', { name: 'Title' });
inst(c, OUT.Rail).name = 'Rail';
iconInst(c, 'list', P.text2, 22);
chips.push(c);
}
OUT.Chip = await slot(S7, cur, variantSet('Chapter chip', S7, chips, 2, 'UI_ChapterChip, top-left 32 / 28 px: "n / 10", the rail, the chapters icon. Tap → pause menu. Titled while an activity chapter runs.'), 'Chapter chip — Compact / Titled');
const toggles = [];
for (const s of ['On', 'Off']) {
const c = cAL('NONE', { name: `State=${s}`, w: 52, h: 30, radius: 15 });
if (s === 'On') fillTok(c, 'color/accent/blue'); else { c.fills = [SOLID('#FFFFFF', 0.12)]; c.strokes = [SOLID('#FFFFFF', 0.4)]; c.strokeWeight = 1.5; c.strokeAlign = 'INSIDE'; }
const knob = ellipse(c, { name: 'Knob', d: 24, fill: '#FFFFFF', x: s === 'On' ? 25 : 3, y: 3 });
knob.effects = [shadow(0.35, 1, 3)];
toggles.push(c);
}
OUT.Toggle = await slot(S7, cur, variantSet('Toggle', S7, toggles, 2, 'SwitchRow: Captions, Larger text, Reduce motion. 48 px row is the hit area.'), 'Toggle');
const slider = cAL('NONE', { name: 'Slider', w: 300, h: 48, desc: 'LessonPanel.VolumeRow: track 6 px, fill blue, 24 px handle with a cyan ring; the 48 px row is the hit area.' });
rect(slider, { name: 'Track', w: 300, h: 6, radius: 3, fill: '#FFFFFF', fillOpacity: 0.2, x: 0, y: 21 });
fillTok(rect(slider, { name: 'Fill', w: 198, h: 6, radius: 3, x: 0, y: 21 }), 'color/accent/blue');
const handle = ellipse(slider, { name: 'Handle', d: 24, fill: '#FFFFFF', x: 186, y: 12 });
handle.strokes = [SOLID(P.cyan)]; handle.strokeWeight = 2; handle.strokeAlign = 'INSIDE';
S7.appendChild(slider);
OUT.Slider = await slot(S7, cur, slider, 'Slider');
const key = cAL('HORIZONTAL', { name: 'Key cap', pad: [4, 9, 4, 9], main: 'CENTER', cross: 'CENTER', radius: 6, fill: 'color/bg/secondary', stroke: 'color/border/subtle', desc: 'Keyboard hint on the pause menu: Space, Esc, R, C, Enter.' });
const keyKey = key.addComponentProperty('Key', 'TEXT', 'Space');
(await txt(key, 'Label/Small', 'Space', { name: 'Key' })).componentPropertyReferences = { characters: keyKey };
S7.appendChild(key);
OUT.Key = await slot(S7, cur, key, 'Key cap');
const toasts = [];
for (const [k, ic, tok] of [['Info', 'info', 'color/accent/cyan'], ['Success', 'check', 'color/state/success'], ['Error', 'alert', 'color/state/error']]) {
const c = cAL('HORIZONTAL', { name: `Kind=${k}`, gap: 10, pad: [0, 18, 0, 14], main: 'CENTER', cross: 'CENTER', h: 44, radius: 22, fill: 'color/bg/primary', fillOpacity: 0.92 });
strokeTok(c, tok, 1.5, 0.7);
await c.setEffectStyleIdAsync(STYLE['Effect/Card'].id);
iconInst(c, ic, k === 'Info' ? P.cyan : k === 'Success' ? P.green : P.red, 20);
const mk = c.addComponentProperty('Message', 'TEXT', 'Finish this step first');
(await txt(c, 'Body/Secondary', k === 'Info' ? 'Finish this step first' : k === 'Success' ? 'Row 3 recorded: 3.0 V, 0.60 A.' : 'Start with V, the push.', { color: 'color/text/primary', name: 'Message' })).componentPropertyReferences = { characters: mk };
toasts.push(c);
}
OUT.Toast = await slot(S7, cur, variantSet('Toast', S7, toasts, 3, 'UI_Hint under the controls (2.5 s): Info for a held control, Success echoes a Done card, Error only for the wrong law tile in Chapter 10.'), 'Toast / hint pill — Info / Success / Error');
fit(S7); Y += S7.height + 120;
const S8 = section(ds, '08 Choices · steps · readouts · bubbles · portrait', W, 100, 0, Y);
await head(S8, 'Choices, voltage steps, readouts, law tiles, chat bubbles, portrait and the Ask Wattson avatar', 'MCQPanel choice rows, PanelViews.VoltageButtons steps, LiveReadingsPanel readouts, LawTiles, AskWattsonUI bubbles, the face disc used by the avatar, the caption bar and the Ask header.');
cur.y = 120;
const choices = [];
for (const s of ['Default', 'Hover', 'Selected', 'Dimmed']) {
const c = cAL('HORIZONTAL', { name: `State=${s}`, gap: 14, pad: [0, 18, 0, 14], cross: 'CENTER', h: 64, w: 436, radius: 14 });
if (s === 'Selected') { fillTok(c, 'color/accent/blue'); }
else { fillTok(c, s === 'Hover' ? 'color/bg/hover' : 'color/bg/secondary'); strokeTok(c, s === 'Hover' ? 'color/border/interactive' : 'color/border/subtle', 1); }
if (s === 'Dimmed') c.opacity = 0.45;
const badge = al('HORIZONTAL', c, { name: 'Badge', w: 30, h: 30, main: 'CENTER', cross: 'CENTER', radius: 15, fill: s === 'Selected' ? '#F5F7FB' : 'color/bg/primary', stroke: s === 'Selected' ? undefined : 'color/border/interactive' });
await txt(badge, 'Label/Small', 'A', { color: s === 'Selected' ? 'color/text/on-gold' : 'color/text/primary', name: 'Letter' });
const lk = c.addComponentProperty('Label', 'TEXT', 'Increase');
(await txt(c, 'Button', 'Increase', { name: 'Label' })).componentPropertyReferences = { characters: lk };
choices.push(c);
}
OUT.Choice = await slot(S8, cur, variantSet('Choice', S8, choices, 4, 'MCQPanel row (Chapter 3 prediction): the chosen row turns blue, the others dim to 45 %. Never marked right or wrong.'), 'Choice — prediction row');
const steps = [];
for (const s of ['Done', 'Now', 'Next', 'Later']) {
const c = cAL('HORIZONTAL', { name: `State=${s}`, gap: 6, pad: [0, 16, 0, 14], main: 'CENTER', cross: 'CENTER', h: 62, radius: 12 });
c.minWidth = 76;
if (s === 'Now') { fillTok(c, 'color/accent/blue'); await c.setEffectStyleIdAsync(STYLE['Effect/Glow Blue'].id); }
else if (s === 'Next') { c.fills = []; strokeTok(c, 'color/border/focus', 1.5); await c.setEffectStyleIdAsync(STYLE['Effect/Glow Cyan'].id); }
else { fillTok(c, 'color/bg/secondary'); strokeTok(c, 'color/border/subtle', 1); if (s === 'Later') c.opacity = 0.36; }
if (s === 'Done') iconInst(c, 'check', P.text2, 16);
const lk = c.addComponentProperty('Label', 'TEXT', '1 V');
(await txt(c, 'Button/Small', s === 'Done' ? '1 V' : s === 'Now' ? '2 V' : s === 'Next' ? '3 V' : '4 V', { color: s === 'Now' ? 'color/text/primary' : s === 'Next' ? 'color/accent/cyan' : s === 'Done' ? 'color/text/secondary' : 'color/text/muted', name: 'Label' })).componentPropertyReferences = { characters: lk };
steps.push(c);
}
OUT.Step = await slot(S8, cur, variantSet('Voltage step', S8, steps, 4, 'PanelViews.VoltageButtons: done (check), now (blue), next (cyan outline, pulses while Wattson asks for it), later (36 %; a tap says why). Reuses the existing SetVoltage events.'), 'Voltage step — Done / Now / Next / Later');
const reads = [];
for (const [q, val, label, tok] of [['V', '5.0 V', 'voltage', 'color/quantity/voltage'], ['I', '1.00 A', 'current', 'color/quantity/current'], ['R', '5.0 Ω', 'R = V ÷ I', 'color/quantity/resistance']]) {
const c = cAL('VERTICAL', { name: `Quantity=${q}`, gap: 2, cross: 'CENTER', pad: [0, 8, 0, 8] });
const vk = c.addComponentProperty('Value', 'TEXT', val);
(await txt(c, 'Numeric/Readout', val, { color: tok, name: 'Value' })).componentPropertyReferences = { characters: vk };
await txt(c, 'Label/Small', label, { color: 'color/text/secondary', name: 'Unit' });
reads.push(c);
}
OUT.Readout = await slot(S8, cur, variantSet('Readout', S8, reads, 3, 'LiveReadingsPanel: the live meter values in the dock (the 3D meters keep their needles). Values come from CircuitRig at runtime — never hardcoded.'), 'Readout — V / I / R');
const tiles = [];
for (const [q, tok] of [['V', 'color/quantity/voltage'], ['I', 'color/quantity/current'], ['R', 'color/quantity/resistance']]) {
const c = cAL('HORIZONTAL', { name: `Quantity=${q}`, w: 64, h: 64, main: 'CENTER', cross: 'CENTER', radius: 12, fill: 'color/bg/secondary', stroke: 'color/border/interactive', strokeW: 1.5 });
await txt(c, 'Heading/Screen', q, { color: tok, name: 'Letter' });
tiles.push(c);
}
OUT.Tile = await slot(S8, cur, variantSet('Law tile', S8, tiles, 3, 'LawTiles (Chapter 10): tap V, then I, then R to assemble V = I × R.'), 'Law tile');
const bubbles = [];
for (const who of ['You', 'Wattson']) {
const c = cAL('VERTICAL', { name: `Speaker=${who}`, gap: 6, pad: [14, 18, 14, 18], w: 436, radius: 18, cross: who === 'You' ? 'MAX' : 'MIN' });
if (who === 'You') c.fills = [SOLID('#FFFFFF', 0.13)]; else { fillTok(c, 'color/bg/secondary'); strokeTok(c, 'color/border/subtle', 1); await txt(c, 'Label/Small', 'WATTSON', { color: 'color/accent/cyan', name: 'Who' }); }
const mk = c.addComponentProperty('Message', 'TEXT', 'What is electric current?');
(await txt(c, 'Body', who === 'You' ? 'What is electric current?' : 'Current is the charge passing a section of the wire each second — amperes.', { name: 'Message', w: 400, align: who === 'You' ? 'RIGHT' : 'LEFT' })).componentPropertyReferences = { characters: mk };
bubbles.push(c);
}
OUT.Bubble = await slot(S8, cur, variantSet('Chat bubble', S8, bubbles, 2, 'AskWattsonUI messages: YOU right-aligned on white 13 %, WATTSON on the raised surface with a cyan label (also "WATTSON · LAB CONTROLS").'), 'Chat bubble — You / Wattson');
const portraits = [];
for (const d of [48, 92]) {
const c = cAL('NONE', { name: `Size=${d}`, w: d, h: d });
c.fills = [];
const ring = ellipse(c, { name: 'Ring', d, x: 0, y: 0 }); fillTok(ring, 'color/accent/cyan');
const disc = ellipse(c, { name: 'Disc', d: d - 4, x: 2, y: 2 }); fillTok(disc, 'color/bg/primary');
const face = ellipse(c, { name: 'Face', d: d - 6, x: 3, y: 3 });
if (AVATAR_HASH) face.fills = [{ type: 'IMAGE', scaleMode: 'FILL', imageHash: AVATAR_HASH }];
else { face.fills = [SOLID('#F2C230')]; const b = iconInst(c, 'bolt', P.navy900, Math.round(d * 0.5)); b.x = Math.round(d * 0.25); b.y = Math.round(d * 0.25); }
portraits.push(c);
}
OUT.Portrait = await slot(S8, cur, variantSet('Portrait', S8, portraits, 2, 'Wattson’s face (ModulePalette.wattsonFace) in a cyan ring: 48 px in the caption bar and the Ask header, 92 px for the avatar.'), 'Portrait — 48 / 92');
const avatars = [];
for (const s of ['Idle', 'Listening', 'Thinking', 'Speaking', 'Error', 'Close']) {
const c = cAL('VERTICAL', { name: `State=${s}`, gap: 6, cross: 'CENTER', w: 160 });
c.fills = [];
const holder = al('VERTICAL', c, { name: 'Face', cross: 'CENTER', main: 'CENTER', pad: 8, radius: 60 });
const pi = inst(holder, OUT.Portrait, { Size: 92 });
const ring = pi.findAllWithCriteria({ types: ['ELLIPSE'] }).find(n => n.name === 'Ring');
if (ring) ring.fills = [s === 'Error' ? SOLID(P.muted) : s === 'Speaking' ? tokPaint('color/accent/blue') : s === 'Idle' ? tokPaint('color/accent/cyan', 0.7) : tokPaint('color/accent/cyan')];
if (s === 'Listening') await holder.setEffectStyleIdAsync(STYLE['Effect/Glow Cyan'].id);
if (s === 'Speaking') await holder.setEffectStyleIdAsync(STYLE['Effect/Glow Blue'].id);
if (s === 'Thinking') { const dots = row(c, { name: 'Dots', gap: 6 }); for (let i = 0; i < 3; i++) ellipse(dots, { d: 8, fill: P.cyan, fillOpacity: 0.4 + i * 0.3 }); }
if (s === 'Close') { const x = al('HORIZONTAL', c, { name: 'X', w: 28, h: 28, main: 'CENTER', cross: 'CENTER', radius: 14, fill: '#F5F7FB' }); absolute(x, 116, 6); iconInst(x, 'close', P.navy900, 16); }
await txt(c, 'Label/Small', s === 'Close' ? 'CLOSE' : s === 'Error' ? 'ASK WATTSON\nOFFLINE' : 'ASK\nWATTSON', { align: 'CENTER', color: s === 'Error' ? 'color/text/muted' : 'color/text/primary', name: 'Label' });
avatars.push(c);
}
OUT.Avatar = await slot(S8, cur, variantSet('Ask Wattson avatar', S8, avatars, 6, 'UI_AskWattson, bottom-right: idle / listening (push-to-talk) / thinking / speaking / error (offline notes) / close while the conversation is open. 108 px hit area.'), 'Ask Wattson avatar — states');
fit(S8); Y += S8.height + 120;
return { components: Object.fromEntries(Object.entries(OUT).map(([k, v]) => [k, v.id])), sections: [S6.id, S7.id, S8.id], nextY: Y, createdNodeIds: IDS.length };
}

async function part_02b_cards() {
await resolveFonts(); await loadTokens(); await loadStyles();
const ds = figma.root.children.find(p => p.name === '01 Design System');
if (!ds) throw new Error('run scripts 01 and 02a first');
await figma.setCurrentPageAsync(ds);
loadIcons(ds);
const C = {};
for (const n of ['Button', 'Button/CTA', 'Control', 'Control bar', 'Rail segment', 'Progress rail', 'Chapter chip', 'Toggle', 'Slider', 'Key cap', 'Toast', 'Choice', 'Voltage step', 'Readout', 'Law tile', 'Chat bubble', 'Portrait', 'Ask Wattson avatar']) C[n] = comp(ds, n);
for (const s of ds.findAllWithCriteria({ types: ['SECTION'] })) if (/^(09|10) /.test(s.name)) s.remove();
let Y = 0; for (const c of ds.children) Y = Math.max(Y, c.y + c.height); Y += 120;
const W = 2200;
const OUT = {};
async function head(sec, title, sub) {
const t = await txt(sec, 'Heading/Screen', title, { color: '#F5F7FB' }); t.x = 40; t.y = 28;
const s = await txt(sec, 'Body/Secondary', sub, { color: '#AFC5D5', w: 1400 }); s.x = 40; s.y = 28 + t.height + 6;
}
function button(parent, style, label, iconName, o = {}) {
const i = inst(parent, C.Button, { Style: style, State: o.state || 'Default' });
i.name = 'Button ' + label;
setProps(i, { Label: label, 'Show icon': !!iconName });
if (iconName) { const ic = i.findAllWithCriteria({ types: ['INSTANCE'] }).find(n => n.name.startsWith('Icon/')); if (ic) { ic.swapComponent(ICONC[iconName]); tint(ic, style === 'Primary' ? P.navy900 : style === 'Ghost' ? P.text2 : P.text); } }
if (o.w === 'fill') { i.layoutSizingHorizontal = 'FILL'; i.primaryAxisAlignItems = 'CENTER'; }
return i;
}
async function eyebrow(parent, text, iconName, tok = 'color/accent/cyan') {
const r = row(parent, { name: 'Eyebrow', gap: 8, cross: 'CENTER' });
if (iconName) iconInst(r, iconName, tok === 'color/accent/cyan' ? P.cyan : tok === 'color/state/success' ? P.green : P.gold, 18);
await txt(r, 'Label/Eyebrow', text, { color: tok, name: 'Eyebrow' });
return r;
}
async function cardComp(name, w, desc, o = {}) {
const c = cAL('VERTICAL', Object.assign({ name, gap: 12, pad: 22, w, fill: 'color/bg/primary', fillOpacity: 0.92, stroke: 'color/border/subtle', strokeW: 1, radius: 18, clip: true, desc }, o));
await c.setEffectStyleIdAsync(STYLE['Effect/Card'].id);
radiusTok(c, 'radius/lg');
return c;
}
const S9 = section(ds, '09 HUD cards', W, 100, 0, Y);
await head(S9, 'HUD cards', 'Chapter banner, caption bar, the turn card (your turn / done / next step), clue card, prediction card and the hint-bearing feedback states. All sit in the 480 px dock or the bottom caption zone defined in HUD_DESIGN.md.');
const cur = { y: 120 };
{
const c = await cardComp('Chapter banner', 760, 'UI_ChapterTitle: over the first shot of every chapter, 56 / 50 px in. Fades in 0.6 s, holds ~3.5 s, out 0.4 s; a tap sends it away.', { gap: 10, pad: [24, 32, 24, 28], fillOpacity: 0.86 });
await txt(c, 'Label/Eyebrow', 'CHAPTER 2 OF 10', { color: 'color/accent/gold', name: 'Kicker' });
await txt(c, 'Display/Chapter', 'Journey Inside the Wire', { name: 'Title' });
await txt(c, 'Body/Large', 'Shrink down and watch what moves when current flows.', { color: 'color/text/secondary', name: 'Purpose', w: 'fill' });
const r = inst(c, C['Progress rail']); r.name = 'Rail';
S9.appendChild(c); OUT.Banner = await slot(S9, cur, c, 'Chapter banner');
}
{
const bars = [];
for (const v of ['Centred', 'Portrait']) {
const c = cAL(v === 'Centred' ? 'VERTICAL' : 'HORIZONTAL', { name: `Variant=${v}`, gap: v === 'Centred' ? 6 : 16, pad: v === 'Centred' ? [14, 28, 17, 28] : [12, 28, 12, 14], cross: 'CENTER', main: 'CENTER', fill: 'color/bg/primary', fillOpacity: 0.8, stroke: 'color/border/subtle', radius: 16 });
await c.setEffectStyleIdAsync(STYLE['Effect/Card'].id);
let host = c;
if (v === 'Portrait') { inst(c, C.Portrait, { Size: 48 }).name = 'Portrait'; host = col(c, { name: 'Text', gap: 4 }); }
const align = v === 'Centred' ? 'CENTER' : 'LEFT';
await txt(host, 'Caption/Speaker', 'WATTSON', { color: 'color/accent/cyan', name: 'Speaker', align });
await txt(host, 'Caption', 'These are electric charges.', { name: 'Line', align });
bars.push(c);
}
OUT.Caption = await slot(S9, cur, variantSet('Caption bar', S9, bars, 2, 'UI_SubtitlePanel: hugs the line, max 1080 px, two lines a page, centred in the frame the dock leaves free, 40 px up. Centred for scripted narration; Portrait while Ask Wattson answers live.'), 'Caption bar — Centred / Portrait');
}
{
const cards = [];
for (const s of ['Waiting', 'Done', 'Next step']) {
const c = await cardComp(`State=${s}`, 480, '', { pad: [18, 22, 20, 18] });
strokeTok(c, s === 'Waiting' ? 'color/border/interactive' : s === 'Done' ? 'color/state/success' : 'color/border/subtle', 1.5, s === 'Done' ? 0.6 : 1);
const r = row(c, { name: 'Turn', gap: 16, w: 'fill' });
const badge = al('HORIZONTAL', r, { name: 'Badge', w: 60, h: 60, main: 'CENTER', cross: 'CENTER', radius: 30 });
badge.fills = [s === 'Done' ? tokPaint('color/state/success', 0.2) : tokPaint('color/accent/cyan', s === 'Waiting' ? 0.16 : 0.1)];
iconInst(badge, s === 'Done' ? 'check' : s === 'Waiting' ? 'target' : 'volume', s === 'Done' ? P.green : P.cyan, 30);
const t = col(r, { name: 'Text', gap: 4, w: 'fill' });
await txt(t, 'Label/Eyebrow', s === 'Waiting' ? 'YOUR TURN · STEP 1 OF 3' : s === 'Done' ? 'DONE' : 'NEXT STEP', { color: s === 'Waiting' ? 'color/accent/cyan' : s === 'Done' ? 'color/state/success' : 'color/text/secondary', name: 'Eyebrow' });
await txt(t, 'Heading/Turn', s === 'Waiting' ? 'Select the ammeter.' : s === 'Done' ? 'Ammeter placed, in series.' : 'Listen to Wattson.', { name: 'Title', w: 'fill' });
if (s !== 'Done') await txt(t, 'Body/Secondary', s === 'Waiting' ? 'Tap the meter marked A on the bench.' : 'He’ll give you the next step.', { color: 'color/text/secondary', name: 'Hint', w: 'fill' });
cards.push(c);
}
OUT.Turn = await slot(S9, cur, variantSet('Turn card', S9, cards, 3, 'LessonPanel.Turn / Done: the instruction card at the top of the dock. Waiting pulses once on arrival and again after 8 s idle (with the hint sound); Done holds ~0.9 s with a chime; Next step while Wattson talks between steps. Enter taps the target.'), 'Turn card — Waiting / Done / Next step');
}
{
const cards = [];
for (const v of ['Clue', 'Reminder']) {
const c = await cardComp(`Variant=${v}`, 480, '', { gap: 8 });
await eyebrow(c, v === 'Clue' ? 'CLUE' : 'YOUR PREDICTION', 'lens');
if (v === 'Clue') {
await txt(c, 'Heading/Panel', 'Ammeter', { name: 'Title' });
await rich(c, 'Body/Large', [['Measures ', null], ['current', P.amp], [' — I — ampere (A). Connected in series, so the same current passes through it and the test wire.', null]], { name: 'Body', w: 'fill' });
} else {
await rich(c, 'Body/Large', [['You said the ', null], ['current', P.amp], [' will ', null], ['increase', P.text], ['. Let’s see if Ohm’s experiment agrees with you.', null]], { name: 'Body', w: 'fill' });
}
cards.push(c);
}
OUT.Clue = await slot(S9, cur, variantSet('Clue card', S9, cards, 2, 'LessonPanel.Info: lens + CLUE, title, body with the quantity words in their colours (LessonPanel.Quantities). The reminder variant carries the learner’s own prediction into Chapter 7.'), 'Clue card — Clue / Prediction reminder');
}
{
const c = await cardComp('Prediction card', 480, 'MCQPanel (Chapter 3): one tap, never marked. 1 / 2 / 3 on the keyboard pick a row.', { gap: 14 });
await eyebrow(c, 'YOUR PREDICTION', 'target');
await rich(c, 'Body/Large', [['If we increase the ', null], ['potential difference', P.volt], [' while everything else stays the same, what will happen to the ', null], ['current', P.amp], ['?', null]], { name: 'Question', w: 'fill' });
const list = col(c, { name: 'Choices', gap: 8, w: 'fill' });
for (const [letter, label, state] of [['A', 'Increase', 'Default'], ['B', 'Decrease', 'Default'], ['C', 'Stay the same', 'Default']]) {
const i = inst(list, C.Choice, { State: state }); i.name = 'Choice ' + letter; i.layoutSizingHorizontal = 'FILL';
setProps(i, { Label: label }); await setText(i, 'Letter', letter);
}
S9.appendChild(c); OUT.Prediction = await slot(S9, cur, c, 'Prediction card');
}
fit(S9); Y += S9.height + 120;
const S10 = section(ds, '10 Experiment cards', W, 100, 0, Y);
await head(S10, 'Experiment cards', 'Voltage strip, live readings, the observation table, the V–I graph, formula lines, the law builder, the charge counter, the circuit diagram and the copper / nichrome inset. Values shown are the module’s own dataset (5 Ω, 1–5 V) and come from ExperimentData at runtime.');
cur.y = 120;
{
const c = await cardComp('Voltage strip', 480, 'PanelViews.VoltageButtons: SET THE BATTERY · n OF 5 and the five steps. Reuses CircuitRig.SetVoltage.', { gap: 12 });
await eyebrow(c, 'SET THE BATTERY · 3 OF 5', null, 'color/text/secondary');
const r = row(c, { name: 'Steps', gap: 8, w: 'fill' });
for (const [v, s] of [['1 V', 'Done'], ['2 V', 'Done'], ['3 V', 'Now'], ['4 V', 'Next'], ['5 V', 'Later']]) { const i = inst(r, C['Voltage step'], { State: s }); i.name = v; setProps(i, { Label: v }); i.layoutSizingHorizontal = 'FILL'; }
S10.appendChild(c); OUT.Voltage = await slot(S10, cur, c, 'Voltage strip');
}
{
const c = await cardComp('Readings card', 480, 'LiveReadingsPanel: the dock echo of the two meters (three with R from Chapter 9). Steps aside while the table is up.', { pad: [16, 22, 16, 22] });
const r = row(c, { name: 'Readouts', main: 'SPACE_BETWEEN', w: 'fill', pad: [0, 24, 0, 24] });
inst(r, C.Readout, { Quantity: 'V' }).name = 'V'; inst(r, C.Readout, { Quantity: 'I' }).name = 'I';
S10.appendChild(c); OUT.Readings = await slot(S10, cur, c, 'Readings card');
}
{
const c = await cardComp('Observation table', 536, 'ObservationTableUI: headers in the quantity colours, paper values, hairlines, a neutral blue highlight for the row being discussed. V / I column appears in Chapter 9.', { gap: 0, pad: [14, 20, 14, 20] });
const widths = [160, 150, 186];
const hdr = row(c, { name: 'Header', w: 'fill', pad: [0, 0, 10, 0] });
const H = [['Potential Difference\nV (V)', P.volt], ['Current\nI (A)', P.amp], ['V / I', P.ohm]];
for (let n = 0; n < H.length; n++) { const cell = al('HORIZONTAL', hdr, { name: 'Head', w: widths[n], main: 'CENTER' }); await txt(cell, 'Label/Small', H[n][0], { color: H[n][1], align: 'CENTER', name: 'Head' }); }
rule(c, { opacity: 0.6 });
const rows = [['1.0 V', '0.20 A', '1.0 ÷ 0.20 = 5'], ['2.0 V', '0.40 A', '2.0 ÷ 0.40 = 5'], ['3.0 V', '0.60 A', '3.0 ÷ 0.60 = 5'], ['4.0 V', '0.80 A', '5'], ['5.0 V', '1.00 A', '5']];
for (let i = 0; i < rows.length; i++) {
const r = row(c, { name: 'Row ' + (i + 1), w: 'fill', h: 44, cross: 'CENTER', radius: 8 });
if (i === 2) r.fills = [tokPaint('color/accent/blue', 0.14)];
for (let n = 0; n < 3; n++) { const cell = al('HORIZONTAL', r, { name: 'Cell', w: widths[n], main: 'CENTER', cross: 'CENTER' }); await txt(cell, 'Numeric/Table', rows[i][n], { align: 'CENTER', name: 'Value', color: n === 2 ? 'color/text/secondary' : 'color/text/primary' }); }
if (i < rows.length - 1) rule(c, { opacity: 0.35 });
}
S10.appendChild(c); OUT.Table = await slot(S10, cur, c, 'Observation table');
}
{
const c = await cardComp('Graph container', 536, 'GraphUI: I on X, V on Y (the slope is R, as Chapter 9 says). Grid cyan 12 %, line blue, points blue with paper labels, the live point amber. Slope triangle: rise ΔV blue, run ΔI amber.', { pad: [16, 20, 12, 20] });
const PW = 496, PH = 380, L = 72, B = 52, T = 18, R = 24, IW = PW - L - R, IH = PH - T - B;
const X = i => L + (i / 1.2) * IW, Yv = v => T + IH - (v / 6) * IH;
const pts = [[0.2, 1], [0.4, 2], [0.6, 3], [0.8, 4], [1, 5]];
let svg = `<svg width="${PW}" height="${PH}" viewBox="0 0 ${PW} ${PH}" xmlns="http://www.w3.org/2000/svg" fill="none" stroke-linecap="round">`;
for (let i = 1; i <= 6; i++) svg += `<line x1="${X(i * 0.2)}" y1="${T}" x2="${X(i * 0.2)}" y2="${T + IH}" stroke="${P.cyan}" stroke-opacity="0.14" stroke-width="1"/>`;
for (let v = 1; v <= 6; v++) svg += `<line x1="${L}" y1="${Yv(v)}" x2="${L + IW}" y2="${Yv(v)}" stroke="${P.cyan}" stroke-opacity="0.14" stroke-width="1"/>`;
svg += `<path d="M${L} ${T} V${T + IH} H${L + IW}" stroke="#F5F7FB" stroke-opacity="0.55" stroke-width="1.5"/>`;
svg += `<line x1="${X(0.2)}" y1="${Yv(1)}" x2="${X(1.12)}" y2="${Yv(5.6)}" stroke="${P.blue}" stroke-width="3"/>`;
for (const [i, v] of pts) svg += `<circle cx="${X(i)}" cy="${Yv(v)}" r="6" fill="${P.blue}" stroke="#F5F7FB" stroke-width="2"/>`;
svg += '</svg>';
const plot = frame(c, { name: 'Plot', w: PW, h: PH, clip: false });
plot.fills = [];
const g = figma.createNodeFromSvg(svg); g.name = 'Graph lines'; plot.appendChild(g); g.x = 0; g.y = 0; keep(g);
for (let i = 1; i <= 6; i++) { const t = await txt(plot, 'Numeric/Tick', (i * 0.2).toFixed(1), { color: 'color/text/secondary', name: 'tick x' }); t.x = X(i * 0.2) - t.width / 2; t.y = T + IH + 8; }
for (let v = 1; v <= 6; v++) { const t = await txt(plot, 'Numeric/Tick', String(v), { color: 'color/text/secondary', name: 'tick y' }); t.x = L - 12 - t.width; t.y = Yv(v) - t.height / 2; }
for (const [i, v] of pts) { const t = await txt(plot, 'Numeric/Tick', `(${i.toFixed(2)}, ${v.toFixed(1)})`, { name: 'point label' }); t.x = X(i) - t.width - 10; t.y = Yv(v) - t.height - 4; }
const xt = await txt(plot, 'Label/Small', 'Current, I (A)', { color: P.amp, name: 'x title' }); xt.x = L + IW / 2 - xt.width / 2; xt.y = PH - 20;
const yt = await txt(plot, 'Label/Small', 'Potential Difference, V (V)', { color: P.volt, name: 'y title' }); yt.rotation = 90; yt.x = 2; yt.y = T + IH / 2 + yt.width / 2;
S10.appendChild(c); OUT.Graph = await slot(S10, cur, c, 'Graph container');
}
{
const c = await cardComp('Formula card', 480, 'LessonPanel.Formula / Reveal: the lines Wattson builds in Chapters 8–10. V blue, I amber, R red, Ω red; everything else paper.', { gap: 10, cross: 'CENTER' });
await rich(c, 'Formula/Line', [['1 V → 0.20 A', null], ['      ', null], ['2 V → 0.40 A', null]], { name: 'Pairs', align: 'CENTER', color: 'color/text/secondary' });
await rich(c, 'Formula/Line', [['V', P.volt], [' × 2   →   ', null], ['I', P.amp], [' × 2', null]], { name: 'Line', align: 'CENTER' });
rule(c, { opacity: 0.4 });
await rich(c, 'Formula/Reveal', [['V', P.volt], ['  ∝  ', P.text], ['I', P.amp]], { name: 'Reveal', align: 'CENTER' });
await txt(c, 'Body/Secondary', 'directly proportional, for this wire at constant temperature', { color: 'color/text/secondary', name: 'Sub', align: 'CENTER', w: 'fill' });
S10.appendChild(c); OUT.Formula = await slot(S10, cur, c, 'Formula card');
}
{
const c = await cardComp('Law card', 480, 'LawTiles (Chapter 10): tap V, then I, then R; a wrong tile shows the Error toast line "Start with V, the push."', { gap: 14, cross: 'CENTER' });
await eyebrow(c, 'BUILD THE LAW', null, 'color/text/secondary');
await rich(c, 'Formula/Reveal', [['V', P.volt], ['  =  ', P.text], ['I', P.muted], ['  ×  ', P.text], ['R', P.muted]], { name: 'Slots', align: 'CENTER' });
const r = row(c, { name: 'Tiles', gap: 12 });
for (const q of ['R', 'V', 'I']) inst(r, C['Law tile'], { Quantity: q }).name = 'Tile ' + q;
await txt(c, 'Body/Secondary', 'Next comes I, the flow.', { color: 'color/text/secondary', name: 'Hint', align: 'CENTER' });
S10.appendChild(c); OUT.Law = await slot(S10, cur, c, 'Law card');
}
{
const c = await cardComp('Counter card', 480, 'The Chapter 2 charge counter (LessonPanel "Counter" block): charges through the glowing section in the last second.', { gap: 12 });
await eyebrow(c, 'CHARGE THROUGH THE SECTION', null, 'color/text/secondary');
const r = row(c, { name: 'Count', gap: 16, cross: 'CENTER', w: 'fill' });
const badge = al('HORIZONTAL', r, { name: 'Badge', w: 64, h: 64, main: 'CENTER', cross: 'CENTER', radius: 32, fill: 'color/accent/cyan', fillOpacity: 0.12, stroke: 'color/accent/cyan', strokeW: 2 });
await txt(badge, 'Heading/Screen', '2', { name: 'Count' });
const t = col(r, { name: 'Text', gap: 4, w: 'fill' });
await rich(t, 'Body', [['last second: ', null], ['3 charges', P.charge]], { name: 'Last', w: 'fill' });
await rich(t, 'Body', [['charge ÷ time = ', null], ['current (I)', P.amp]], { name: 'Rule', w: 'fill' });
await txt(c, 'Body/Secondary', 'Simplified: a few representative electrons, slowed down so you can count them. The real wire carries billions per second.', { color: 'color/text/muted', name: 'Note', w: 'fill' });
S10.appendChild(c); OUT.Counter = await slot(S10, cur, c, 'Counter card');
}
{
const c = await cardComp('Diagram card', 480, 'The schematic the dock shows in Chapter 6 ("Schematic" block): source, switch, ammeter in series, voltmeter across the nichrome wire X–Y.', { gap: 10 });
await eyebrow(c, 'CIRCUIT · NICHROME WIRE X–Y', null, 'color/text/secondary');
const DW = 436, DH = 160;
let svg = `<svg width="${DW}" height="${DH}" viewBox="0 0 ${DW} ${DH}" xmlns="http://www.w3.org/2000/svg" fill="none" stroke="#F5F7FB" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">`;
svg += '<path d="M40 40 H150 M210 40 H300 M340 40 H396 V120 H300 M60 120 H40 V40"/>';
svg += '<path d="M150 40 L205 20"/>';   // open switch
svg += `<circle cx="320" cy="40" r="20" stroke="${P.amp}"/>`;   // ammeter
svg += '<path d="M60 108 V132 M72 100 V140"/>';                 // battery
svg += '<path d="M72 120 H120 M180 120 H300" />';
svg += `<path d="M120 120 H180" stroke="${P.ohm}" stroke-width="4"/>`;   // nichrome X–Y
svg += `<path d="M120 120 V80 H180 V120" stroke="${P.volt}"/><circle cx="150" cy="80" r="18" fill="#0B1522" stroke="${P.volt}"/>`;  // voltmeter
svg += '</svg>';
const plot = frame(c, { name: 'Schematic', w: DW, h: DH, clip: false }); plot.fills = [];
const g = figma.createNodeFromSvg(svg); g.name = 'Lines'; plot.appendChild(g); g.x = 0; g.y = 0; keep(g);
for (const [l, x, y, hex] of [['A', 320, 40, P.amp], ['V', 150, 80, P.volt], ['X', 118, 136, P.text], ['Y', 182, 136, P.text], ['source', 86, 140, P.text2], ['key', 240, 8, P.text2]]) { const t = await txt(plot, 'Label/Small', l, { color: hex, name: l }); t.x = x - t.width / 2; t.y = y - t.height / 2; }
S10.appendChild(c); OUT.Diagram = await slot(S10, cur, c, 'Diagram card');
}
{
const c = await cardComp('Comparison inset', 536, 'PanelViews.Inset (Chapter 9): copper against nichrome at the same voltage; the learner taps Swap in nichrome.', { gap: 12 });
await txt(c, 'Label/Small', 'MAGNIFIED, NOT TO SCALE · same voltage, length, thickness and temperature', { color: 'color/text/muted', name: 'Note', w: 'fill' });
for (const [name, hex, dots, tag, tagHex] of [['Copper', '#DB8050', 11, 'MORE current', P.amp], ['Nichrome', '#949A85', 5, 'LESS current', P.text2]]) {
const r = row(c, { name, gap: 14, cross: 'CENTER', w: 'fill' });
await txt(r, 'Body', name, { name: 'Name', w: 90 });
const strip = frame(r, { name: 'Strip', w: 270, h: 36, radius: 8, fill: hex, fillOpacity: 0.85 });
for (let i = 0; i < dots; i++) ellipse(strip, { name: 'e', d: 7, fill: '#FFFFFF', x: 14 + i * (242 / Math.max(1, dots - 1)), y: 8 + (i % 3) * 7 });
await txt(r, 'Label/Small', tag, { color: tagHex, name: 'Tag' });
}
button(c, 'Primary', 'Swap in nichrome', 'swap', { w: 'fill' });
S10.appendChild(c); OUT.Inset = await slot(S10, cur, c, 'Comparison inset');
}
fit(S10); Y += S10.height + 120;
return { components: Object.fromEntries(Object.entries(OUT).map(([k, v]) => [k, v.id])), sections: [S9.id, S10.id], nextY: Y, createdNodeIds: IDS.length };
}

async function part_02c_panels() {
await resolveFonts(); await loadTokens(); await loadStyles();
const ds = figma.root.children.find(p => p.name === '01 Design System');
if (!ds) throw new Error('run scripts 01 and 02a first');
await figma.setCurrentPageAsync(ds);
loadIcons(ds);
const C = {};
for (const n of ['Button', 'Button/CTA', 'Control', 'Control bar', 'Rail segment', 'Progress rail', 'Chapter chip', 'Toggle', 'Slider', 'Key cap', 'Toast', 'Choice', 'Voltage step', 'Readout', 'Law tile', 'Chat bubble', 'Portrait', 'Ask Wattson avatar']) C[n] = comp(ds, n);
for (const s of ds.findAllWithCriteria({ types: ['SECTION'] })) if (/^11 /.test(s.name)) s.remove();
let Y = 0; for (const c of ds.children) Y = Math.max(Y, c.y + c.height); Y += 120;
const W = 2200;
const OUT = {};
async function head(sec, title, sub) {
const t = await txt(sec, 'Heading/Screen', title, { color: '#F5F7FB' }); t.x = 40; t.y = 28;
const s = await txt(sec, 'Body/Secondary', sub, { color: '#AFC5D5', w: 1400 }); s.x = 40; s.y = 28 + t.height + 6;
}
function button(parent, style, label, iconName, o = {}) {
const i = inst(parent, C.Button, { Style: style, State: o.state || 'Default' });
i.name = 'Button ' + label;
setProps(i, { Label: label, 'Show icon': !!iconName });
if (iconName) { const ic = i.findAllWithCriteria({ types: ['INSTANCE'] }).find(n => n.name.startsWith('Icon/')); if (ic) { ic.swapComponent(ICONC[iconName]); tint(ic, style === 'Primary' ? P.navy900 : style === 'Ghost' ? P.text2 : P.text); } }
if (o.w === 'fill') { i.layoutSizingHorizontal = 'FILL'; i.primaryAxisAlignItems = 'CENTER'; }
return i;
}
async function eyebrow(parent, text, iconName, tok = 'color/accent/cyan') {
const r = row(parent, { name: 'Eyebrow', gap: 8, cross: 'CENTER' });
if (iconName) iconInst(r, iconName, tok === 'color/accent/cyan' ? P.cyan : tok === 'color/state/success' ? P.green : P.gold, 18);
await txt(r, 'Label/Eyebrow', text, { color: tok, name: 'Eyebrow' });
return r;
}
async function cardComp(name, w, desc, o = {}) {
const c = cAL('VERTICAL', Object.assign({ name, gap: 12, pad: 22, w, fill: 'color/bg/primary', fillOpacity: 0.92, stroke: 'color/border/subtle', strokeW: 1, radius: 18, clip: true, desc }, o));
await c.setEffectStyleIdAsync(STYLE['Effect/Card'].id);
radiusTok(c, 'radius/lg');
return c;
}
const S11 = section(ds, '11 Panels', W, 100, 0, Y);
const cur = { y: 120 };
await head(S11, 'Panels', 'Case closed, the Ask Wattson conversation, the settings rows, chapter rows, the pause menu, the volume popup and the title block. Every control maps to an existing handler (PauseMenu, SwitchRow, VolumeRow, AskWattsonUI, TitleScreen).');
cur.y = 120;
{
const c = await cardComp('Case closed card', 580, 'LessonPanel.End: the finale card. Replay the investigation, Chapters (pause menu), Exit.', { gap: 16, pad: [26, 26, 26, 26] });
const stamp = al('HORIZONTAL', c, { name: 'Stamp', pad: [6, 16, 6, 16], stroke: '#F5F7FB', strokeW: 3, radius: 6 });
await txt(stamp, 'Heading/Screen', 'CASE CLOSED', { name: 'Stamp text' });
stamp.rotation = 4;
await eyebrow(c, 'WHAT YOU FOUND', null, 'color/text/secondary');
const tiles = row(c, { name: 'Found', gap: 10, w: 'fill' });
for (const [q, hex, name, unit] of [['V', P.volt, 'Potential difference', 'the push · volts (V)'], ['I', P.amp, 'Current', 'the flow · amperes (A)'], ['R', P.ohm, 'Resistance', 'the opposition · ohms (Ω)']]) {
const t = col(tiles, { name: q, gap: 4, pad: 14, w: 'fill', radius: 12, fill: 'color/bg/secondary', stroke: 'color/border/subtle' });
await txt(t, 'Heading/Screen', q, { color: hex, name: 'Letter' });
await txt(t, 'Body/Secondary', name, { name: 'Name', w: 'fill' });
await txt(t, 'Label/Small', unit, { color: 'color/text/muted', name: 'Unit', w: 'fill' });
}
await rich(c, 'Formula/Reveal', [['V', P.volt], ['  =  ', P.text], ['I', P.amp], ['  ×  ', P.text], ['R', P.ohm]], { name: 'Law', align: 'CENTER', w: 'fill' });
await txt(c, 'Body/Secondary', 'Your nichrome wire: 5.0 V ÷ 1.00 A = 5.0 Ω', { color: 'color/text/secondary', name: 'Numbers', align: 'CENTER', w: 'fill' });
button(c, 'Primary', 'Replay the investigation', 'replay', { w: 'fill' });
const more = row(c, { name: 'More', gap: 10 });
button(more, 'Secondary', 'Chapters', 'list'); button(more, 'Ghost', 'Exit', 'exit');
S11.appendChild(c); OUT.End = await slot(S11, cur, c, 'Case closed card');
}
{
const c = await cardComp('Ask Wattson panel', 580, 'AskWattsonUI inside the dock (580 wide): header with his face, name and plain status, History / Close, the conversation, suggested questions (LocalFAQ, max 3), Hold to talk · field · Ask (Stop while he speaks). Same WattsonAgent calls as today.', { gap: 14, pad: 18 });
const h = row(c, { name: 'Header', gap: 12, cross: 'CENTER', w: 'fill' });
inst(h, C.Portrait, { Size: 48 }).name = 'Portrait';
const who = col(h, { name: 'Who', gap: 2, w: 'fill' });
await txt(who, 'Heading/Panel', 'Wattson', { name: 'Name' });
const st = row(who, { name: 'Status', gap: 8, cross: 'CENTER' });
ellipse(st, { name: 'Dot', d: 10, fill: P.cyan });
await txt(st, 'Body/Secondary', 'Speaking · tap Stop to cut in', { color: 'color/text/secondary', name: 'Status text' });
button(h, 'Ghost', 'History', null); button(h, 'Secondary', 'Close', 'close');
const msgs = col(c, { name: 'Conversation', gap: 10, w: 'fill' });
const you = inst(msgs, C['Chat bubble'], { Speaker: 'You' }); you.layoutSizingHorizontal = 'FILL';
const w = inst(msgs, C['Chat bubble'], { Speaker: 'Wattson' }); w.layoutSizingHorizontal = 'FILL';
await eyebrow(c, 'TRY ASKING', null, 'color/text/secondary');
const chips = col(c, { name: 'Suggestions', gap: 8, w: 'fill' });
for (const q of ['Why did Ohm repeat it so many times?', 'What is a galvanometer?', 'Why did Ohm use a thermoelectric source instead of a battery?']) {
const chip = row(chips, { name: 'Chip', pad: [10, 20, 10, 20], radius: 26, stroke: 'color/border/interactive', strokeW: 1.5, w: 'fill', cross: 'CENTER' });
await txt(chip, 'Body', q, { name: 'Question', w: 'fill' });
}
await eyebrow(c, 'OR ASK YOUR OWN', null, 'color/text/secondary');
const input = row(c, { name: 'Input', gap: 10, cross: 'CENTER', w: 'fill' });
const mic = button(input, 'Secondary', 'Hold to talk', 'mic'); tint(mic.findAllWithCriteria({ types: ['INSTANCE'] })[0], P.cyan);
const field = row(input, { name: 'Field', pad: [0, 22, 0, 22], h: 56, radius: 28, fill: 'color/bg/secondary', stroke: 'color/border/interactive', strokeW: 1.5, w: 'fill', cross: 'CENTER' });
await txt(field, 'Body', 'Type a question', { color: 'color/text/secondary', name: 'Placeholder' });
button(input, 'Primary', 'Ask', 'send');
S11.appendChild(c); OUT.Ask = await slot(S11, cur, c, 'Ask Wattson panel');
}
{
const sw = cAL('HORIZONTAL', { name: 'Settings row / switch', w: 300, h: 48, main: 'SPACE_BETWEEN', cross: 'CENTER', desc: 'SwitchRow: Captions · Larger text · Reduce motion. The whole 48 px row toggles.' });
const lk = sw.addComponentProperty('Label', 'TEXT', 'Captions');
(await txt(sw, 'Body', 'Captions', { name: 'Label' })).componentPropertyReferences = { characters: lk };
inst(sw, C.Toggle, { State: 'On' }).name = 'Toggle';
S11.appendChild(sw); OUT.SwitchRow = await slot(S11, cur, sw, 'Settings row — switch'); C['Settings row / switch'] = sw;
const vr = cAL('HORIZONTAL', { name: 'Settings row / volume', w: 300, h: 48, gap: 12, cross: 'CENTER', desc: 'LessonPanel.VolumeRow: Wattson · Effects · Music (AudioBus gains).' });
const vk = vr.addComponentProperty('Label', 'TEXT', 'Wattson');
(await txt(vr, 'Body', 'Wattson', { name: 'Label', w: 88 })).componentPropertyReferences = { characters: vk };
const sl = inst(vr, C.Slider); sl.name = 'Slider'; sl.layoutSizingHorizontal = 'FILL';
S11.appendChild(vr); OUT.VolumeRow = await slot(S11, cur, vr, 'Settings row — volume'); C['Settings row / volume'] = vr;
}
{
const rows = [];
for (const s of ['Done', 'Now', 'Locked']) {
const c = cAL('HORIZONTAL', { name: `State=${s}`, w: 400, h: 52, gap: 12, pad: [0, 14, 0, 12], cross: 'CENTER', radius: 12, fill: s === 'Now' ? 'color/bg/hover' : 'color/bg/secondary', stroke: s === 'Now' ? 'color/border/interactive' : 'color/border/subtle', strokeW: s === 'Now' ? 1.5 : 1 });
const b = al('HORIZONTAL', c, { name: 'Badge', w: 28, h: 28, main: 'CENTER', cross: 'CENTER', radius: 14 });
if (s === 'Done') { b.fills = [tokPaint('color/state/success', 0.2)]; iconInst(b, 'check', P.green, 16); }
else if (s === 'Now') { fillTok(b, 'color/accent/blue'); await txt(b, 'Label/Small', '2', { name: 'Number' }); }
else { b.fills = [SOLID('#FFFFFF', 0.08)]; iconInst(b, 'lock', P.muted, 14); }
const lk = c.addComponentProperty('Label', 'TEXT', 'Journey Inside the Wire');
(await txt(c, 'Button/Small', s === 'Done' ? 'Meet Wattson' : s === 'Now' ? 'Journey Inside the Wire' : 'Your Prediction', { name: 'Label', w: 'fill', color: s === 'Locked' ? 'color/text/muted' : 'color/text/primary' })).componentPropertyReferences = { characters: lk };
if (s !== 'Locked') iconInst(c, s === 'Done' ? 'replay' : 'play', s === 'Done' ? P.text2 : P.text, 18);
if (s === 'Locked') c.opacity = 0.7;
rows.push(c);
}
OUT.ChapterRow = await slot(S11, cur, variantSet('Chapter row', S11, rows, 3, 'PauseMenu chapter list: done (replay returns you to where you were), now, locked ("Reach this chapter in the story first.").'), 'Chapter row — Done / Now / Locked');
C['Chapter row'] = OUT.ChapterRow;
}
{
const c = await cardComp('Pause menu', 1240, 'PauseMenu over a 72 % scrim: where you are, Resume, Restart chapter, the ten chapters, the unlock line, keys; settings column; Exit the module asks "Tap again to leave".', { gap: 0, pad: 0 });
const body = row(c, { name: 'Body', w: 'fill' });
const left = col(body, { name: 'Left', gap: 14, pad: 32, w: 'fill' });
await txt(left, 'Label/Eyebrow', 'PAUSED · CHAPTER 2 OF 10', { color: 'color/accent/gold', name: 'Eyebrow' });
await txt(left, 'Heading/Screen', 'Journey Inside the Wire', { name: 'Title' });
await txt(left, 'Body/Secondary', 'Shrink down and watch what moves when current flows.', { color: 'color/text/secondary', name: 'Purpose', w: 'fill' });
const actions = row(left, { name: 'Actions', gap: 10 });
button(actions, 'Primary', 'Resume', 'play'); button(actions, 'Secondary', 'Restart chapter', 'replay');
const grid = row(left, { name: 'Chapters', gap: 12, w: 'fill' });
const titles = ['Meet Wattson', 'Journey Inside the Wire', 'Your Prediction', 'Back to Georg Ohm', 'From Ohm’s Lab to Our Lab', 'Build the Circuit', 'Become the Experimenter', 'The Pattern', 'Discover Resistance', 'Ohm’s Law'];
for (let colN = 0; colN < 2; colN++) {
const gc = col(grid, { name: 'Column ' + (colN + 1), gap: 8, w: 'fill' });
for (let i = colN * 5; i < colN * 5 + 5; i++) {
const state = i < 1 ? 'Done' : i === 1 ? 'Now' : 'Locked';
const r = inst(gc, C['Chapter row'], { State: state }); r.name = 'Chapter ' + (i + 1); r.layoutSizingHorizontal = 'FILL';
setProps(r, { Label: titles[i] });
if (state === 'Now') await setText(r, 'Number', String(i + 1));
}
}
await txt(left, 'Body/Secondary', 'Chapters unlock as you reach them. After the finale, all ten are open.', { color: 'color/text/muted', name: 'Unlock', w: 'fill' });
const keys = row(left, { name: 'Keys', gap: 18, cross: 'CENTER' });
for (const [k, does] of [['Space', 'pause'], ['Esc', 'menu'], ['R', 'replay line'], ['C', 'captions'], ['Enter', 'do the step']]) {
const pair = row(keys, { name: k, gap: 6, cross: 'CENTER' });
setProps(inst(pair, C['Key cap']), { Key: k });
await txt(pair, 'Body/Secondary', does, { color: 'color/text/secondary' });
}
rect(body, { name: 'Divider', w: 1, h: 'fill', fill: '#FFFFFF', fillOpacity: 0.1 }).layoutSizingVertical = 'FILL';
const right = col(body, { name: 'Settings', gap: 12, pad: 32, w: 372 });
await txt(right, 'Label/Eyebrow', 'SETTINGS', { color: 'color/text/secondary', name: 'Eyebrow' });
for (const l of ['Captions', 'Larger text', 'Reduce motion']) { const i = inst(right, C['Settings row / switch']); i.layoutSizingHorizontal = 'FILL'; setProps(i, { Label: l }); if (l !== 'Captions') { const tg = i.findAllWithCriteria({ types: ['INSTANCE'] }).find(n => n.name === 'Toggle'); if (tg) tg.swapComponent(variantOf(C.Toggle, { State: 'Off' })); } }
for (const l of ['Wattson', 'Effects', 'Music']) { const i = inst(right, C['Settings row / volume']); i.layoutSizingHorizontal = 'FILL'; setProps(i, { Label: l }); }
button(right, 'Ghost', 'Exit the module', 'exit');
S11.appendChild(c); OUT.Pause = await slot(S11, cur, c, 'Pause menu');
}
{
const c = await cardComp('Volume popup', 480, 'UI_VolumePopup under the VOLUME control: the three buses and Reduce motion; a click elsewhere closes it.', { gap: 6, pad: [16, 22, 16, 22] });
for (const l of ['Wattson', 'Effects', 'Music']) { const i = inst(c, C['Settings row / volume']); i.layoutSizingHorizontal = 'FILL'; setProps(i, { Label: l }); }
const sw = inst(c, C['Settings row / switch']); sw.layoutSizingHorizontal = 'FILL'; setProps(sw, { Label: 'Reduce motion' });
const tg = sw.findAllWithCriteria({ types: ['INSTANCE'] }).find(n => n.name === 'Toggle'); if (tg) tg.swapComponent(variantOf(C.Toggle, { State: 'Off' }));
S11.appendChild(c); OUT.Volume = await slot(S11, cur, c, 'Volume popup');
}
{
const c = cAL('VERTICAL', { name: 'Title block', gap: 18, w: 820, desc: 'TitleScreen (UI_TitleScreen): kicker, OHM’S LAW, the subtitle, START JOURNEY (starts the original intro once), Continue · Chapter n when a run is saved, the tagline and the meta line.' });
c.fills = [];
await txt(c, 'Label/Eyebrow', 'A WATTSON INVESTIGATION', { color: 'color/accent/cyan', name: 'Kicker' });
const title = await txt(c, 'Display/Title', 'OHM’S LAW', { color: 'color/accent/gold', name: 'Title' });
await title.setEffectStyleIdAsync(STYLE['Effect/Glow Gold'].id);
await txt(c, 'Display/Subtitle', 'A Journey of Curiosity,\nCurrent and Discovery', { name: 'Subtitle' });
const actions = row(c, { name: 'Actions', gap: 14, cross: 'CENTER' });
inst(actions, C['Button/CTA'], { State: 'Default' }).name = 'Start';
button(actions, 'Secondary', 'Continue · Chapter 4', null);
await txt(c, 'Label/Eyebrow', 'EXPLORE   ·   EXPERIMENT   ·   DISCOVER', { color: 'color/accent/cyan', name: 'Tagline' });
await txt(c, 'Body/Secondary', 'About 10 minutes  ·  Captions on  ·  Sound on', { color: 'color/text/muted', name: 'Meta' });
S11.appendChild(c); OUT.Title = await slot(S11, cur, c, 'Title block');
}
fit(S11); Y += S11.height + 120;
return { components: Object.fromEntries(Object.entries(OUT).map(([k, v]) => [k, v.id])), sections: [S11.id], nextY: Y, createdNodeIds: IDS.length };
}

async function part_03_screens_start_hud() {
Object.assign(BG, { title: '4899c56cb8b1fb2a2feaa45aed168c45ad6e6fa3', study: '39d0bdf1aab2acd6cb1c4006a82ab54192171e58', wireEnter: '2b43e12bf507cf52f7ec332972c4ab0df7725077', wireApply: '0ab7035e5ab3da6dce115ef25eca55fb17265411', wireDrift: '22ce12bdfd93498ec3125b0ca088cc3da97a64f8', wireCount: '04a0d1448f4e60b81783778adebfef8d4df6ad28', ohm: '235bfc55a9c48c23a2941e3906b80862f3d89ddb' });
await resolveFonts(); await loadTokens(); await loadStyles();
const ds = figma.root.children.find(p => p.name === '01 Design System');
const scr = figma.root.children.find(p => p.name === '02 Screens');
if (!ds || !scr) throw new Error('run scripts 01, 02a, 02b, 02c first');
await figma.setCurrentPageAsync(scr);
await ds.loadAsync();
loadIcons(ds); loadComponents(ds);
const s = 1;
const pStart = group(scr, '02 Start Screen');
async function titleScreen(name, returning) {
const f = screen(pStart, name, 'title');
const vig = rect(f, { name: 'Vignette left', w: 1100, h: 1080, x: 0, y: 0 });
vig.fills = [{ type: 'GRADIENT_LINEAR', gradientTransform: [[1, 0, 0], [0, 1, 0]], gradientStops: [{ position: 0, color: RGBA(P.navy950, 0.88) }, { position: 0.62, color: RGBA(P.navy950, 0.35) }, { position: 1, color: RGBA(P.navy950, 0) }] }];
const vig2 = rect(f, { name: 'Vignette bottom', w: 1920, h: 520, x: 0, y: 560 });
vig2.fills = [{ type: 'GRADIENT_LINEAR', gradientTransform: [[0, 1, 0], [-1, 0, 1]], gradientStops: [{ position: 0, color: RGBA(P.navy950, 0) }, { position: 1, color: RGBA(P.navy950, 0.8) }] }];
const block = inst(f, CMP['Title block']);
const cont = nested(block, 'Button Continue · Chapter 4');
if (cont) cont.visible = returning;
block.x = 110; block.y = 1080 - block.height - 96;
const bar = row(f, { name: 'UI_TopControls (title)', gap: 6, pad: [0, 10, 0, 10], h: 56, radius: 28, cross: 'CENTER', fill: 'color/bg/primary', fillOpacity: 0.88, stroke: 'color/border/subtle' });
await bar.setEffectStyleIdAsync(STYLE['Effect/Card'].id);
const cc = inst(bar, CMP.Control, { State: 'On' }); setProps(cc, { Label: 'CC', Icon: ICONC.cc.id });
rect(bar, { name: 'Divider', w: 1, h: 22, fill: '#FFFFFF', fillOpacity: 0.12 });
const vol = inst(bar, CMP.Control, { State: 'Default' }); setProps(vol, { Label: 'VOLUME', Icon: ICONC.volume.id });
bar.x = 1920 - 32 - bar.width; bar.y = 28;
return f;
}
const S01 = await titleScreen('S01 Title · first visit', false);
const S02 = await titleScreen('S02 Title · returning learner', true);
const S03 = frame(pStart, { name: 'S03 Title · START JOURNEY states', w: 1920, h: 300, fill: '#081A2B' });
const strip = row(S03, { name: 'States', gap: 40, cross: 'CENTER', pad: 60 });
for (const st of ['Default', 'Hover', 'Pressed', 'Disabled']) { const c = col(strip, { name: st, gap: 14, cross: 'CENTER' }); inst(c, CMP['Button/CTA'], { State: st }); await txt(c, 'Label/Small', st === 'Disabled' ? 'Disabled (while the intro loads)' : st, { color: 'color/text/secondary' }); }
await placeScreens(pStart, [
[S01, 'S01 · Title screen, first visit', 'New: cinematic title over the real study (Ohm at his desk, Wattson arriving on the spark swirl). START JOURNEY starts the original Chapter 1 intro exactly once. Only CC and VOLUME before the case begins.'],
[S02, 'S02 · Title screen, returning learner', 'Continue · Chapter n appears when a run is saved (TitleScreen reads ModuleFlow.Reached). Nothing else changes.'],
[S03, 'S03 · START JOURNEY states', 'Hover brightens the plate and warms the gold glow; pressed darkens 90 ms; disabled only while a scene loads.'],
], 2);
const pHud = group(scr, '03 Gameplay HUD');
const items = [];
{
const f = screen(pHud, 'S04 Chapter banner', 'wireEnter');
const b = inst(f, CMP['Chapter banner']); b.x = 56; b.y = 50;
controlsAt(f, s, { states: { REPLAY: 'Disabled', SKIP: 'Disabled' } });
avatarAt(f, s);
items.push([f, 'S04 · Chapter banner (Chapter 2 opens)', 'UI_ChapterTitle over the first shot: kicker, serif title, purpose, rail. Holds ~3.5 s, a tap sends it away; the chip takes over.']);
}
{
const f = screen(pHud, 'S05 HUD · cinematic narration', 'wireDrift');
await chipAt(f, s, 2);
controlsAt(f, s);
await captionAt(f, s, 'WATTSON', 'That creates the electrical push that gives the electrons an overall\ndirection. They still jiggle, but now they all drift together.');
avatarAt(f, s);
items.push([f, 'S05 · HUD while Wattson narrates', 'Chip top-left, control capsule top-right, caption hugging the line at the bottom (two lines max, then pages), Ask Wattson bottom-right. Centre of the frame stays clear.']);
}
{
const f = screen(pHud, 'S06 HUD · idle (controls folded)', 'wireCount');
await chipAt(f, s, 2);
controlsAt(f, s, { folded: true });
const d = dockAt(f, s);
await clueAt(d, s, 'Current (I)', [['The charge passing a section each second: I = ΔQ ÷ Δt. Unit: ampere (A).', null]]);
await captionAt(f, s, 'WATTSON', 'That rate of charge flow, the charge passing each second, is the current.', { cover: 512 });
avatarAt(f, s);
items.push([f, 'S06 · Idle HUD with a clue card', 'In cinematic chapters the capsule folds to icons after 3 s without input (full contrast, same hit areas). Clue cards sit at the top of the 480 px dock; the caption centres in the free width.']);
}
{
const f = screen(pHud, 'S07 Your turn · Apply the voltage', 'wireApply');
await chipAt(f, s, 2, 'Journey Inside the Wire');
controlsAt(f, s, { states: { SKIP: 'Disabled' } });
const d = dockAt(f, s);
await turnAt(d, s, 'Waiting', 'YOUR TURN', 'Apply the voltage.', 'Tap the + terminal to connect the push.');
await clueAt(d, s, 'Voltage (V)', [['The difference in electric potential between two points. Applying it across the wire creates the push on the electrons. Unit: volt (V).', null]]);
await captionAt(f, s, 'WATTSON', 'That end, to the positive terminal.', { cover: 512 });
avatarAt(f, s);
items.push([f, 'S07 · Your turn card (Chapter 2 gate)', 'Turn card: cyan target badge, YOUR TURN, the instruction, a hint. The world target (+ terminal) glows; Enter taps it. SKIP is held until the step is done.']);
}
{
const f = screen(pHud, 'S08 Done + hint toast', 'wireDrift');
await chipAt(f, s, 2, 'Journey Inside the Wire');
controlsAt(f, s, { states: { SKIP: 'Disabled' } });
hintAt(f, s, 'Info', 'Finish this step first');
const d = dockAt(f, s);
await turnAt(d, s, 'Done', 'DONE', 'Voltage applied.');
await clueAt(d, s, 'Voltage (V)', [['The difference in electric potential between two points. Applying it across the wire creates the push on the electrons. Unit: volt (V).', null]]);
avatarAt(f, s);
items.push([f, 'S08 · Done confirmation and the hint pill', 'Done: green check badge, ~0.9 s with the chime. The hint pill under the controls answers a tap on a held control (2.5 s): "Finish this step first" / "Close Ask Wattson first" / "One moment, Wattson is talking".']);
}
{
const f = screen(pHud, 'S09 Volume popup', 'wireDrift');
await chipAt(f, s, 2);
controlsAt(f, s, { states: { VOLUME: 'On' } });
const pop = inst(f, CMP['Volume popup']); pop.x = 1920 - 32 - pop.width; pop.y = 28 + 56 + 10;
await captionAt(f, s, 'WATTSON', 'These are electric charges.');
avatarAt(f, s);
items.push([f, 'S09 · VOLUME popup', 'Wattson · Effects · Music sliders and the Reduce motion switch, under the lit VOLUME control. A click anywhere else closes it.']);
}
{
const f = screen(pHud, 'S10 Paused controls · CC off', 'wireDrift');
await chipAt(f, s, 2);
controlsAt(f, s, { resume: true, states: { PAUSE: 'On', CC: 'Default' } });
avatarAt(f, s);
items.push([f, 'S10 · PAUSE reads RESUME, CC off', 'While paused the first control reads RESUME (lit). With CC off the caption bar is simply absent; nothing else moves.']);
}
await placeScreens(pHud, items, 2);
return { start: [S01.id, S02.id, S03.id], hud: items.map(i => i[0].id), createdNodeIds: IDS.length };
}

async function part_04_screens_experiment() {
Object.assign(BG, { prediction: '192f0b19011c1fb4b1c25e02c67c6bcaa927406e', wireCount: '04a0d1448f4e60b81783778adebfef8d4df6ad28', benchAmmeter: 'c51fbfac68d4297d726e8f28a32c4c27f8eb3cea', benchSwitch: 'e02a16827143e03bc1391926d53066eba5481240', exp3v: '36174d3ca720a18775c7f96e422fb79093eea427', graph: 'fc5a111c39c5bb87bb78a736483b303f0394658c', resist: '955f0ee0f09a0eac80c7049159cc6439557ba088', slope: '7784676d1628b5d3eabd9cca2538b8f9f18bcdf7', law: '96cd9a45a961a01db1696b2eb13d130c38351c51' });
await resolveFonts(); await loadTokens(); await loadStyles();
const ds = figma.root.children.find(p => p.name === '01 Design System');
const scr = figma.root.children.find(p => p.name === '02 Screens');
if (!ds || !scr) throw new Error('run scripts 01, 02a, 02b, 02c first');
await figma.setCurrentPageAsync(scr);
await ds.loadAsync();
loadIcons(ds); loadComponents(ds);
const page = group(scr, '04 Investigation & Experiment');
const s = 1;
const items = [];
function readings(d, values) {   // values: [['V','2.0 V'], ['I','0.40 A'], ['R','5.0 Ω']]
const i = inst(null, CMP['Readings card']);
for (const [q, v] of values.slice(0, 2)) { const r = nested(i, q); if (r) setProps(r, { Value: v }); }
return dockItem(d, i);
}
async function readingsThree(d) {   // Chapter 9–10: a third readout (R) joins — built inline since the card instance cannot grow
const c = await card(null, { name: 'Readings card (V · I · R)', pad: [16, 22, 16, 22], w: 480 });
const r = row(c, { name: 'Readouts', main: 'SPACE_BETWEEN', w: 'fill', pad: [0, 12, 0, 12] });
for (const [q, v] of [['V', '5.0 V'], ['I', '1.00 A'], ['R', '5.0 Ω']]) { const ro = inst(r, CMP.Readout, { Quantity: q }); setProps(ro, { Value: v }); }
return dockItem(d, c);
}
function table(d, rowsFilled, ratio, scale = 0.62, highlight = -1) {
const i = inst(null, CMP['Observation table']);
const values = i.findAllWithCriteria({ types: ['TEXT'] }).filter(n => n.name === 'Value');
values.forEach((t, n) => { const r = Math.floor(n / 3); if (r >= rowsFilled) t.visible = false; });
if (!ratio) for (const r of i.findAll(n => n.name.startsWith('Row ') || n.name === 'Header')) if (r.children && r.children[2]) r.children[2].visible = false;
for (const r of i.findAll(n => n.name.startsWith('Row '))) r.fills = highlight >= 0 && r.name === 'Row ' + (highlight + 1) ? [tokPaint('color/accent/blue', 0.14)] : [];
i.rescale(scale);
d.appendChild(i);
return i;
}
function graph(d, scale = 0.6) { const i = inst(null, CMP['Graph container']); i.rescale(scale); d.appendChild(i); return i; }
async function formula(d, lines, scale = 1) {   // lines: { Pairs, Line, Reveal, Sub } → segs or null (hidden)
const i = inst(null, CMP['Formula card']);
for (const [layer, segs] of Object.entries(lines)) {
const t = i.findAllWithCriteria({ types: ['TEXT'] }).find(n => n.name === layer);
if (!t) continue;
if (!segs) { t.visible = false; continue; }
await setRich(i, layer, segs);
}
if (scale !== 1) i.rescale(scale);
return dockItem(d, i);
}
function voltage(d, states) {   // states for 1 V … 5 V
const i = inst(null, CMP['Voltage strip']);
['1 V', '2 V', '3 V', '4 V', '5 V'].forEach((v, n) => { const st = nested(i, v); if (st) swapState(st, CMP['Voltage step'], { State: states[n] }); });
return dockItem(d, i);
}
for (const after of [false, true]) {
const f = screen(page, after ? 'S12 Prediction · locked in' : 'S11 Prediction · your turn', 'prediction');
await chipAt(f, s, 3, 'Your Prediction');
controlsAt(f, s, { states: { SKIP: 'Disabled' } });
const d = dockAt(f, s);
if (after) await turnAt(d, s, 'Done', 'DONE', 'Prediction locked in.');
else await turnAt(d, s, 'Waiting', 'YOUR TURN', 'Make your prediction.', 'Tap one answer. The experiment will decide.');
const pc = inst(null, CMP['Prediction card']);
if (after) ['A', 'B', 'C'].forEach(l => { const ch = nested(pc, 'Choice ' + l); if (ch) swapState(ch, CMP.Choice, { State: l === 'A' ? 'Selected' : 'Dimmed' }); });
dockItem(d, pc);
if (!after) await captionAt(f, s, 'WATTSON', 'If we increase the potential difference while everything else stays\nthe same, what do you predict will happen to the current?', { cover: 512 });
else await captionAt(f, s, 'WATTSON', 'Interesting. Remember your answer. Let’s see if Ohm’s experiment agrees with you.', { cover: 512 });
avatarAt(f, s);
items.push([f, after ? 'S12 · Prediction locked in' : 'S11 · Your prediction (Chapter 3)', after ? 'The chosen row turns blue, the others dim; the turn card confirms. Never marked right or wrong — Chapter 7 calls the prediction back.' : 'MCQPanel as a prediction card under the turn card. Keys 1 / 2 / 3 pick a row.']);
}
{
const f = screen(page, 'S13 Charge counter', 'wireCount');
await chipAt(f, s, 2);
controlsAt(f, s);
const d = dockAt(f, s);
const cc = inst(null, CMP['Counter card']); dockItem(d, cc);
await clueAt(d, s, 'Current (I)', [['The charge passing a section each second: I = ΔQ ÷ Δt. Unit: ampere (A).', null]]);
await captionAt(f, s, 'WATTSON', 'Watch this section. One, two, three… Now count how much charge\npasses in one second.', { cover: 512 });
avatarAt(f, s);
items.push([f, 'S13 · Charge through the section (Chapter 2)', 'The counter card echoes the glowing ring in the wire: count badge, last second, charge ÷ time = current, and the "simplified" note.']);
}
{
const f = screen(page, 'S14 Build the circuit · select the ammeter', 'benchAmmeter');
await chipAt(f, s, 6, 'Build the Circuit');
controlsAt(f, s, { states: { SKIP: 'Disabled' } });
const d = dockAt(f, s);
await turnAt(d, s, 'Waiting', 'YOUR TURN · STEP 1 OF 3', 'Select the ammeter.', 'Tap the meter marked A on the bench.');
await clueAt(d, s, 'Ammeter', [['Measures ', null], ['current', P.amp], [' — I — ampere (A). Connected in series.', null]]);
dockItem(d, inst(null, CMP['Diagram card']));
await captionAt(f, s, 'WATTSON', 'If we want to know how much current flows through the nichrome wire,\nwe use an ammeter.', { cover: 512 });
avatarAt(f, s);
items.push([f, 'S14 · Build the circuit (Chapter 6, step 1 of 3)', 'Turn card with the step count, the clue for the instrument, the schematic card. The bench, the meters and the key stay uncovered; the target meter glows in the world.']);
}
{
const f = screen(page, 'S15 Build the circuit · close the switch', 'benchSwitch');
await chipAt(f, s, 6, 'Build the Circuit');
controlsAt(f, s, { states: { SKIP: 'Disabled' } });
const d = dockAt(f, s);
await turnAt(d, s, 'Waiting', 'YOUR TURN · STEP 3 OF 3', 'Close the switch.', 'Tap the key to complete the circuit.');
dockItem(d, inst(null, CMP['Diagram card']));
await captionAt(f, s, 'WATTSON', 'Close the switch to complete the circuit.', { cover: 512 });
avatarAt(f, s);
items.push([f, 'S15 · Close the switch (step 3 of 3)', 'Same card, last step. The diagram card stays as the reference while the learner works on the bench.']);
}
{
const f = screen(page, 'S16 Become the experimenter · set 3 V', 'exp3v');
await chipAt(f, s, 7, 'Become the Experimenter');
controlsAt(f, s, { states: { REPLAY: 'Disabled', SKIP: 'Disabled' } });
const d = dockAt(f, s, true); d.counterAxisAlignItems = 'CENTER';
await turnAt(d, s, 'Waiting', 'YOUR TURN · STEP 3 OF 5', 'Set the battery to 3 V.', 'Tap the button, then watch the meters.');
voltage(d, ['Done', 'Done', 'Next', 'Later', 'Later']);
table(d, 2, false, 0.62);
graph(d, 0.56);
await captionAt(f, s, 'WATTSON', 'Now set the voltage to 3 volts.', { cover: 612 });
avatarAt(f, s);
items.push([f, 'S16 · Set the battery (Chapter 7, 3 of 5)', 'Wide dock (580): turn card, the five voltage steps (done / next / later), the table filling row by row, the graph growing point by point. The readings card steps aside while the table is up (HUD_DESIGN rule).']);
}
{
const f = screen(page, 'S17 The pattern · draw the line', 'graph');
await chipAt(f, s, 8, 'The Pattern');
controlsAt(f, s, { states: { SKIP: 'Disabled' } });
const d = dockAt(f, s, true); d.counterAxisAlignItems = 'CENTER';
await turnAt(d, s, 'Waiting', 'YOUR TURN', 'Draw the line.', 'Tap the graph to run a line through your five points.');
table(d, 5, false, 0.56);
graph(d, 0.56);
await formula(d, { Pairs: [['1 V → 0.20 A', null], ['      ', null], ['2 V → 0.40 A', null]], Line: [['V', P.volt], [' × 2   →   ', null], ['I', P.amp], [' × 2', null]], Reveal: null, Sub: null }, 0.9);
await captionAt(f, s, 'WATTSON', 'Look at the pattern! Double the voltage, and the current doubles too.', { cover: 612 });
avatarAt(f, s);
items.push([f, 'S17 · The pattern (Chapter 8)', 'All five rows, the graph the learner draws the line on (tap = Enter), and the formula card building V × 2 → I × 2. V ∝ I is revealed on the same card a beat later.']);
}
{
const f = screen(page, 'S18 Discover resistance · swap the wire', 'resist');
await chipAt(f, s, 9, 'Discover Resistance');
controlsAt(f, s, { states: { SKIP: 'Disabled' } });
const d = dockAt(f, s, true); d.counterAxisAlignItems = 'CENTER';
await turnAt(d, s, 'Waiting', 'YOUR TURN', 'Change the wire material.', 'Same voltage, same length: tap Swap in nichrome.');
readings(d, [['V', '5.0 V'], ['I', '1.00 A']]);
dockItem(d, inst(null, CMP['Comparison inset']));
await captionAt(f, s, 'WATTSON', 'Keep the voltage the same, but change the wire material. This one\nallows more current. The other opposes the flow more.', { cover: 612 });
avatarAt(f, s);
items.push([f, 'S18 · Discover resistance (Chapter 9)', 'Readings, then the magnified copper / nichrome inset with its swap button (the one primary button in the dock).']);
}
{
const f = screen(page, 'S19 Slope → resistance → V = I × R', 'slope');
await chipAt(f, s, 9, 'Discover Resistance');
controlsAt(f, s);
const d = dockAt(f, s, true); d.counterAxisAlignItems = 'CENTER';
readings(d, [['V', '5.0 V'], ['I', '1.00 A']]);
const g = graph(d, 0.56);
const tri = frame(f, { name: 'Slope triangle (overlay)', w: 200, h: 120, clip: false }); tri.fills = [];
const tsvg = figma.createNodeFromSvg(`<svg width="200" height="120" viewBox="0 0 200 120" xmlns="http://www.w3.org/2000/svg" fill="none" stroke-width="3" stroke-linecap="round"><path d="M20 100 H150" stroke="${P.amp}"/><path d="M150 100 V20" stroke="${P.volt}"/><path d="M20 100 L150 20" stroke="${P.blue}" stroke-opacity="0.5" stroke-dasharray="6 6"/></svg>`);
tri.appendChild(tsvg); tsvg.x = 0; tsvg.y = 0; keep(tsvg);
const run = await txt(tri, 'Label/Small', 'run: ΔI = 0.40 A', { color: P.amp }); run.x = 40; run.y = 104;
const rise = await txt(tri, 'Label/Small', 'rise: ΔV = 2.0 V', { color: P.volt }); rise.x = 158; rise.y = 50;
await formula(d, { Pairs: [['V', P.volt], [' ∝ ', null], ['I', P.amp], ['        ', null], ['V', P.volt], [' = k ', null], ['I', P.amp]], Line: [['V', P.volt], [' ÷ ', null], ['I', P.amp], [' = k   (the same 5 every time)', null]], Reveal: [['V', P.volt], ['  =  ', null], ['I', P.amp], ['  ×  ', null], ['R', P.ohm]], Sub: [['slope = ΔV ÷ ΔI = 2.0 V ÷ 0.40 A = 5  →  5 Ω', null]] }, 0.9);
d.appendChild(tri); tri.layoutPositioning = 'ABSOLUTE'; tri.x = d.width - 300; tri.y = g.y + 60;
await captionAt(f, s, 'WATTSON', 'So for a V-versus-I graph, the slope tells us the resistance of the\nnichrome wire XY.', { cover: 612 });
avatarAt(f, s);
items.push([f, 'S19 · Slope → resistance (Chapter 9)', 'The graph keeps I on X and V on Y so the slope is R, exactly as the narration says. The formula card walks V ∝ I → V = k I → k = R → V = I × R.']);
}
for (const wrong of [false, true]) {
const f = screen(page, wrong ? 'S21 Build the law · wrong tile' : 'S20 Build the law', 'law');
await chipAt(f, s, 10, 'Ohm’s Law');
controlsAt(f, s, { states: { SKIP: 'Disabled' } });
if (wrong) hintAt(f, s, 'Error', 'Start with V, the push.');
const d = dockAt(f, s);
await turnAt(d, s, 'Waiting', 'YOUR TURN', 'Build the law.', 'Tap V, then I, then R.');
await readingsThree(d);
const gl = await card(null, { name: 'Glossary card', gap: 8, w: 480 });
for (const [sym, hex, rest] of [['V', P.volt, ' — the push: potential difference'], ['I', P.amp, ' — the flow: current'], ['R', P.ohm, ' — the opposition: resistance']]) await rich(gl, 'Body/Large', [[sym, hex], [rest, null]], { w: 'fill' });
dockItem(d, gl);
const law = inst(null, CMP['Law card']);
await setRich(law, 'Slots', [['V', P.muted], ['  =  ', P.text], ['I', P.muted], ['  ×  ', P.text], ['R', P.muted]]);
if (wrong) { const h = await setText(law, 'Hint', 'Start with V, the push.'); h.fills = [SOLID(P.red)]; }
else await setText(law, 'Hint', 'Start with V, the push.');
dockItem(d, law);
avatarAt(f, s);
items.push([f, wrong ? 'S21 · Wrong tile' : 'S20 · Build the law (Chapter 10)', wrong ? 'A wrong tile (R first): the hint line turns red and the error pill echoes it. Nothing is locked; the learner simply tries again.' : 'Turn card, the three readouts, the glossary and the law builder: tap V, then I, then R. Each placed letter lights in its colour.']);
}
await placeScreens(page, items, 2);
return { screens: items.map(i => i[0].id), createdNodeIds: IDS.length };
}

async function part_05_screens_info() {
Object.assign(BG, { ohm: '235bfc55a9c48c23a2941e3906b80862f3d89ddb', exp4v: '2202fd1d3a738ed89a746841fef1b47793059710', reveal: '4919094a7208e0b87eba7d5166fb55a251e0a9ea', finale: '672278c2d1020fad157b2da0b9ba118a88c9f50c', wireDrift: '22ce12bdfd93498ec3125b0ca088cc3da97a64f8' });
await resolveFonts(); await loadTokens(); await loadStyles();
const ds = figma.root.children.find(p => p.name === '01 Design System');
const scr = figma.root.children.find(p => p.name === '02 Screens');
if (!ds || !scr) throw new Error('run scripts 01, 02a, 02b, 02c first');
await figma.setCurrentPageAsync(scr);
await ds.loadAsync();
loadIcons(ds); loadComponents(ds);
const s = 1;
async function smallBar(f) {   // CC · VOLUME only (title screen and the finale)
const bar = row(f, { name: 'UI_TopControls (CC · VOLUME)', gap: 6, pad: [0, 10, 0, 10], h: 56, radius: 28, cross: 'CENTER', fill: 'color/bg/primary', fillOpacity: 0.88, stroke: 'color/border/subtle' });
await bar.setEffectStyleIdAsync(STYLE['Effect/Card'].id);
setProps(inst(bar, CMP.Control, { State: 'On' }), { Label: 'CC', Icon: ICONC.cc.id });
rect(bar, { name: 'Divider', w: 1, h: 22, fill: '#FFFFFF', fillOpacity: 0.12 });
setProps(inst(bar, CMP.Control, { State: 'Default' }), { Label: 'VOLUME', Icon: ICONC.volume.id });
bar.x = f.width - 32 - bar.width; bar.y = 28;
return bar;
}
async function sheet(page, name, h, title, sub) {
const f = frame(page, { name, w: 1920, h, fill: '#081A2B' });
const t = await txt(f, 'Heading/Panel', title); t.x = 60; t.y = 44;
const u = await txt(f, 'Body/Secondary', sub, { color: 'color/text/secondary', w: 1500 }); u.x = 60; u.y = 44 + t.height + 6;
return f;
}
const pAI = group(scr, '05 AI & Information');
const ai = [];
{
const f = screen(pAI, 'S22 Ask Wattson · open', 'ohm');
await chipAt(f, s, 4);
controlsAt(f, s, { states: { REPLAY: 'Disabled', SKIP: 'Disabled' } });
const panel = inst(f, CMP['Ask Wattson panel']); panel.x = 1920 - 32 - panel.width; panel.y = 104;
await captionAt(f, s, 'WATTSON', 'Current is the charge passing a section of the wire each second — amperes.', { cover: 612, portrait: true });
avatarAt(f, s, 'Close');
ai.push([f, 'S22 · Ask Wattson (Chapter 4)', 'The conversation takes the dock column (580). The lesson pauses; REPLAY and SKIP are held ("Close Ask Wattson first"). Only his answer is captioned, with his portrait. Same WattsonAgent calls, LocalFAQ chips, push-to-talk on Space.']);
}
{
const f = await sheet(pAI, 'S23 Avatar states · chat bubbles · status lines', 620, 'Ask Wattson avatar states, bubbles and the plain-words status line', 'idle · listening (push-to-talk) · thinking · speaking · error (offline notes) · close. Status copy is the existing AskWattsonUI.StatusText: "Listening · let go to send", "Thinking...", "Speaking · tap Stop to cut in", "His radio is fuzzy. Try a suggested question.", "Answering from his case notes", "Ask about this chapter".');
const r = row(f, { name: 'States', gap: 36, cross: 'MIN' }); r.x = 60; r.y = 160;
for (const st of ['Idle', 'Listening', 'Thinking', 'Speaking', 'Error', 'Close']) inst(r, CMP['Ask Wattson avatar'], { State: st });
const b = col(f, { name: 'Bubbles', gap: 10, w: 460 }); b.x = 1340; b.y = 160;
inst(b, CMP['Chat bubble'], { Speaker: 'You' }).layoutSizingHorizontal = 'FILL';
inst(b, CMP['Chat bubble'], { Speaker: 'Wattson' }).layoutSizingHorizontal = 'FILL';
ai.push([f, 'S23 · Avatar states and bubbles', '']);
}
{
const f = await sheet(pAI, 'S24 Clue cards', 560, 'Clue cards with the module’s own explanations', 'Lens + CLUE, a title, the body with quantity words in their colours. Text is the existing lesson copy (SceneDirector Info cues) — no new lesson content. Electric symbols only; nothing from other subjects.');
const r = row(f, { name: 'Clues', gap: 24, cross: 'MIN' }); r.x = 60; r.y = 160;
const clues = [
['Voltage (V)', [['The difference in electric potential between two points. Applying it across the wire creates the push on the electrons. Unit: volt (V).', null]]],
['Current (I)', [['The charge passing a section each second: I = ΔQ ÷ Δt. Unit: ampere (A).', null]]],
['Resistance (R)', [['How much a conductor opposes the flow of ', null], ['current', P.amp], ['. Same ', null], ['voltage', P.volt], [', less ', null], ['current', P.amp], [' = more ', null], ['resistance', P.ohm], ['. The charges are not lost; they just get through more slowly.', null]]],
['Voltmeter', [['Measures ', null], ['potential difference', P.volt], [' — V — volt (V). Connected across the wire, X to Y.', null]]],
];
for (const [title, segs] of clues) { const i = inst(r, CMP['Clue card'], { Variant: 'Clue' }); await setText(i, 'Title', title); await setRich(i, 'Body', segs); }
ai.push([f, 'S24 · Clue cards', '']);
}
{
const f = await sheet(pAI, 'S25 Hints and toasts', 340, 'Hints', 'The hint pill (UI_Hint) answers a tap on a held control for 2.5 s; Success echoes a Done card when the dock is busy; Error only for the wrong law tile. Idle 8 s in a turn: the turn card pulses once with the hint sound and the step’s recorded hint line. "Reach this chapter in the story first." is the pause menu’s locked-chapter reply.');
const r = row(f, { name: 'Toasts', gap: 24, cross: 'CENTER' }); r.x = 60; r.y = 180;
setProps(inst(r, CMP.Toast, { Kind: 'Info' }), { Message: 'Close Ask Wattson first' });
setProps(inst(r, CMP.Toast, { Kind: 'Info' }), { Message: 'One moment, Wattson is talking' });
setProps(inst(r, CMP.Toast, { Kind: 'Success' }), { Message: 'Row 3 recorded: 3.0 V, 0.60 A.' });
setProps(inst(r, CMP.Toast, { Kind: 'Error' }), { Message: 'Start with V, the push.' });
ai.push([f, 'S25 · Hints and toasts', '']);
}
await placeScreens(pAI, ai, 1, 160, 160);
const pFb = group(scr, '06 Feedback & Completion');
const fb = [];
{
const f = screen(pFb, 'S26 Row recorded', 'exp4v');
await chipAt(f, s, 7, 'Become the Experimenter');
controlsAt(f, s, { states: { REPLAY: 'Disabled', SKIP: 'Disabled' } });
const d = dockAt(f, s, true); d.counterAxisAlignItems = 'CENTER';
await turnAt(d, s, 'Done', 'DONE', 'Row 3 recorded: 3.0 V, 0.60 A.');
const strip = inst(null, CMP['Voltage strip']);
['1 V', '2 V', '3 V', '4 V', '5 V'].forEach((v, n) => { const st = nested(strip, v); if (st) swapState(st, CMP['Voltage step'], { State: n < 3 ? 'Done' : n === 3 ? 'Next' : 'Later' }); });
dockItem(d, strip);
const t = inst(null, CMP['Observation table']);
t.findAllWithCriteria({ types: ['TEXT'] }).filter(n => n.name === 'Value').forEach((v, n) => { if (Math.floor(n / 3) >= 3) v.visible = false; });
for (const r of t.findAll(n => n.name.startsWith('Row ') || n.name === 'Header')) { if (r.children && r.children[2]) r.children[2].visible = false; if (r.name.startsWith('Row ')) r.fills = r.name === 'Row 3' ? [tokPaint('color/accent/blue', 0.14)] : []; }
t.rescale(0.62); d.appendChild(t);
const g = inst(null, CMP['Graph container']); g.rescale(0.56); d.appendChild(g);
await captionAt(f, s, 'WATTSON', 'Three volts, and the ammeter reads 0.60 amperes.', { cover: 612 });
avatarAt(f, s);
fb.push([f, 'S26 · Row recorded (feedback while measuring)', 'The Done card confirms the row with the real numbers; the recorded step gets its check; the new table row highlights in blue; the graph gains its point. No modal, no quiz.']);
}
{
const f = screen(pFb, 'S27 Formula reveal', 'reveal');
await chipAt(f, s, 10);
controlsAt(f, s);
const d = dockAt(f, s);
const fc = inst(null, CMP['Formula card']);
for (const layer of ['Pairs', 'Line']) { const t = fc.findAllWithCriteria({ types: ['TEXT'] }).find(n => n.name === layer); if (t) t.visible = false; }
await setRich(fc, 'Reveal', [['V', P.volt], ['  =  ', P.text], ['I', P.amp], ['  ×  ', P.text], ['R', P.ohm]]);
const sub = await setText(fc, 'Sub', 'OHM’S LAW'); sub.fills = [SOLID(P.gold)];
dockItem(d, fc);
await captionAt(f, s, 'WATTSON', 'And today… we call it OHM’S LAW.', { cover: 512 });
avatarAt(f, s);
fb.push([f, 'S27 · Formula reveal (Chapter 10)', 'The reveal card: V = I × R in the quantity colours, OHM’S LAW in gold under it, a one-time gold glow on entry (no continuous pulse).']);
}
{
const f = screen(pFb, 'S28 Case closed', 'finale');
await chipAt(f, s, 10);
await smallBar(f);
const d = dockAt(f, s, true);
dockItem(d, inst(null, CMP['Case closed card']));
avatarAt(f, s);
fb.push([f, 'S28 · Case closed (finale)', 'The stamp, what you found (V / I / R), the law, the learner’s own numbers, Replay the investigation, Chapters, Exit. Ohm and Wattson keep the frame; the dock keeps its normal width so the shot is not pushed.']);
}
await placeScreens(pFb, fb, 2);
const pSet = group(scr, '07 Settings & Pause');
const st = [];
for (const exitArmed of [false, true]) {
const f = screen(pSet, exitArmed ? 'S30 Pause menu · exit armed' : 'S29 Pause menu', 'wireDrift');
const scrim = rect(f, { name: 'Scrim', w: 1920, h: 1080, x: 0, y: 0 }); scrim.fills = [tokPaint('color/bg/scrim', 0.72)];
await chipAt(f, s, 2, 'Journey Inside the Wire');
controlsAt(f, s, { resume: true, states: { PAUSE: 'On', REPLAY: 'Disabled', SKIP: 'Disabled' } });
const menu = inst(f, CMP['Pause menu']);
if (exitArmed) { const ex = nested(menu, 'Button Exit the module'); if (ex) setProps(ex, { Label: 'Tap again to leave' }); }
menu.x = (1920 - menu.width) / 2; menu.y = Math.max(104, (1080 - menu.height) / 2 - 20);
await captionAt(f, s, 'WATTSON', 'These are electric charges.');
avatarAt(f, s);
st.push([f, exitArmed ? 'S30 · Exit the module, armed' : 'S29 · Pause menu', exitArmed ? 'Exit is confirmed inline: the ghost button reads "Tap again to leave" for a few seconds. No extra dialog.' : 'PauseMenu over a 72 % scrim: where you are, Resume, Restart chapter, the ten chapters (done / now / locked), the unlock line, keys; settings column with the real switches and volumes; Exit the module. Space / Esc close it.']);
}
{
const f = await sheet(pSet, 'S31 Settings', 520, 'Settings — exactly the module’s real options', 'Captions → SubtitleUI.Enabled · Larger text → HudScale (+15 %) · Reduce motion → ReducedMotion · Wattson / Effects / Music → AudioBus gains. Not designed: Graphics quality and Fullscreen (nothing in the module reads them today; Fullscreen is a one-line add on desktop if wanted).');
const r = row(f, { name: 'Rows', gap: 80, cross: 'MIN' }); r.x = 60; r.y = 170;
const sw = col(r, { name: 'Switches', gap: 8, w: 320 });
for (const [l, on] of [['Captions', true], ['Larger text', false], ['Reduce motion', false]]) { const i = inst(sw, CMP['Settings row / switch']); i.layoutSizingHorizontal = 'FILL'; setProps(i, { Label: l }); if (!on) { const tg = nested(i, 'Toggle'); if (tg) swapState(tg, CMP.Toggle, { State: 'Off' }); } }
const vo = col(r, { name: 'Volumes', gap: 8, w: 360 });
for (const l of ['Wattson', 'Effects', 'Music']) { const i = inst(vo, CMP['Settings row / volume']); i.layoutSizingHorizontal = 'FILL'; setProps(i, { Label: l }); }
const keys = col(r, { name: 'Keys', gap: 10 });
await txt(keys, 'Label/Eyebrow', 'KEYBOARD', { color: 'color/text/secondary' });
for (const [k, does] of [['Space', 'pause · Start on the title · push-to-talk while Ask is open'], ['Esc', 'this menu (closes the popup or Ask first)'], ['R', 'replay line'], ['C', 'captions'], ['Enter', 'do the step'], ['1 / 2 / 3', 'pick a prediction']]) { const pair = row(keys, { gap: 10, cross: 'CENTER' }); setProps(inst(pair, CMP['Key cap']), { Key: k }); await txt(pair, 'Body/Secondary', does, { color: 'color/text/secondary' }); }
st.push([f, 'S31 · Settings and keys', '']);
}
await placeScreens(pSet, st, 1, 160, 160);
return { ai: ai.map(i => i[0].id), feedback: fb.map(i => i[0].id), settings: st.map(i => i[0].id), createdNodeIds: IDS.length };
}

async function part_06_screens_responsive_board() {
Object.assign(BG, { exp3v: '36174d3ca720a18775c7f96e422fb79093eea427' });
await resolveFonts(); await loadTokens(); await loadStyles();
const ds = figma.root.children.find(p => p.name === '01 Design System');
const scr = figma.root.children.find(p => p.name === '02 Screens');
const pBoard = figma.root.children.find(p => p.name === '03 Reference Board');
if (!ds || !scr || !pBoard) throw new Error('run scripts 01-05 first');
await figma.setCurrentPageAsync(pBoard);
await scr.loadAsync(); await ds.loadAsync();
loadIcons(ds); loadComponents(ds);
for (const c of [...pBoard.children]) c.remove();
const pResp = group(scr, '08 Responsive Layouts');
function hudScale(w, h) { return Math.max(0.75, Math.sqrt((w / 1920) * (h / 1080))); }
async function composition(name, w, h) {
const s = hudScale(w, h);
const f = screen(pResp, `${name} · ${w}×${h} · UI ×${s.toFixed(2)}`, 'exp3v', w, h);
await chipAt(f, s, 7, 'Become the Experimenter');
controlsAt(f, s, { states: { REPLAY: 'Disabled', SKIP: 'Disabled' } });
const d = dockAt(f, s, true); d.counterAxisAlignItems = 'CENTER';
await turnAt(d, s, 'Waiting', 'YOUR TURN · STEP 3 OF 5', 'Set the battery to 3 V.', 'Tap the button, then watch the meters.');
const strip = inst(null, CMP['Voltage strip']);
['1 V', '2 V', '3 V', '4 V', '5 V'].forEach((v, n) => { const st = nested(strip, v); if (st) swapState(st, CMP['Voltage step'], { State: n < 2 ? 'Done' : n === 2 ? 'Next' : 'Later' }); });
if (s !== 1) strip.rescale(s);
dockItem(d, strip);
const avail = h - (SCREEN.dockTop + 196) * s;
const tight = avail < 700 * s;
const t = inst(null, CMP['Observation table']);
t.findAllWithCriteria({ types: ['TEXT'] }).filter(n => n.name === 'Value').forEach((v, n) => { if (Math.floor(n / 3) >= 2) v.visible = false; });
for (const r of t.findAll(n => n.name.startsWith('Row ') || n.name === 'Header')) if (r.children && r.children[2]) r.children[2].visible = false;
t.rescale((tight ? 0.52 : 0.62) * s); d.appendChild(t);
const g = inst(null, CMP['Graph container']); g.rescale((tight ? 0.46 : 0.56) * s); d.appendChild(g);
await captionAt(f, s, 'WATTSON', 'Now set the voltage to 3 volts.', { cover: 612 });
avatarAt(f, s);
const guide = rect(f, { name: 'Safe area guide', w: w - 2 * 24 * s, h: h - 2 * 20 * s, x: 24 * s, y: 20 * s }); guide.fills = []; guide.strokes = [SOLID(P.cyan, 0.5)]; guide.strokeWeight = 1; guide.dashPattern = [8, 8]; guide.locked = true;
return [f, s];
}
const R = [];
for (const [name, w, h, note] of [
['S32 Desktop 16:9', 1920, 1080, 'Reference layout. Everything in HUD_DESIGN.md is specified at this size.'],
['S33 Desktop 16:10', 1920, 1200, 'Canvas scales ×1.05; corners stay anchored, the dock gains height, captions sit 40 px up as always.'],
['S34 Tablet 4:3 (iPad)', 2048, 1536, 'Canvas scales ×1.23: targets grow to ~54 px; the dock takes 29 % of the width, so the camera framing (PanelFraming) keeps the bench centred in the free part.'],
['S35 Laptop 1366×768 (floor)', 1366, 768, 'HudScale holds the canvas at ×0.75 (never smaller): card text 15.75 px, captions 24 px. The dock views shrink (table ×0.52, graph ×0.46) so the turn card is never squeezed.'],
]) { const [f, s] = await composition(name, w, h); R.push([f, `${name} — ${w}×${h}, UI ×${s.toFixed(2)}`, note]); }
await placeScreens(pResp, R, 2, 200, 220);
const rules = frame(pResp, { name: 'S36 Layout rules', w: 1920, h: 520, fill: '#081A2B' });
rules.x = 40; rules.y = R[2][0].y + R[2][0].height + 260;
const rc = col(rules, { name: 'Rules', gap: 10, pad: 60 });
await txt(rc, 'Heading/Panel', 'Safe area, anchors and scaling rules');
for (const line of [
'Canvas 1920 × 1080 reference, ScaleWithScreenSize, match 0.5; HudScale floor 0.75; "Larger text" +15 %. Landscape only; AdaptiveOhmsLaw.SafeAreaLayout insets the HUD on notched tablets.',
'Chip top-left 32 / 28 · control capsule top-right 32 / 28 · dock right 32, top 104, bottom 196, width 480 (580 with table / graph / inset / case closed) · captions bottom 40, centred in the free width, max 1080, two lines a page · Ask avatar bottom-right 26 / 26.',
'Hit areas: controls 44, options 64, steps 62, chips 52, avatar 108, rows 48. Nothing under 15 px at 1080p. Interactive borders ≥ 3:1 (cyan 55 % on navy).',
'The 3D hero stays clear: no UI in the centre band except the chapter banner (top-left) and the pause scrim. Cards never steal input; a tap on the world target is always possible.',
]) await txt(rc, 'Body', line, { color: 'color/text/secondary', w: 1780 });
const t36 = await txt(pResp, 'Heading/Panel', 'S36 · Layout rules'); t36.x = rules.x; t36.y = rules.y - 70;
fit(pResp, 120);
const board = frame(pBoard, { name: 'OHM’S LAW — UI v4 REFERENCE SHEET', w: 4000, h: 2960, fill: '#081A2B' });
const headRow = row(board, { name: 'Header', gap: 40, cross: 'CENTER', pad: [60, 80, 0, 80], w: 4000, main: 'SPACE_BETWEEN' });
const hl = col(headRow, { name: 'Title', gap: 8 });
await txt(hl, 'Label/Eyebrow', 'OHM’S LAW  ·  UI / UX REFERENCE SHEET  ·  v4  ·  same journey, a more immersive experience', { color: 'color/accent/cyan' });
await txt(hl, 'Display/Chapter', 'OHM’S LAW — the complete UI', { color: 'color/accent/gold' });
await txt(headRow, 'Label/Eyebrow', 'EDUCATION  ·  CURIOSITY  ·  DISCOVERY', { color: 'color/text/secondary' });
const panels = [
['02 Start Screen', 'S01 Title · first visit', '01  START SCREEN (GAME BANNER)'],
['03 Gameplay HUD', 'S05 HUD · cinematic narration', '02  IN-GAME HUD + WATTSON CAPTION'],
['03 Gameplay HUD', 'S04 Chapter banner', '03  CHAPTER TITLE + PROGRESS'],
['03 Gameplay HUD', 'S07 Your turn · Apply the voltage', '04  INSTRUCTION + CLUE CARDS'],
['04 Investigation & Experiment', 'S11 Prediction · your turn', '05  PREDICTION (THE ONE QUESTION)'],
['04 Investigation & Experiment', 'S14 Build the circuit · select the ammeter', '06  CIRCUIT INTERACTION'],
['04 Investigation & Experiment', 'S16 Become the experimenter · set 3 V', '07  VOLTAGE STEPS + TABLE + GRAPH'],
['04 Investigation & Experiment', 'S19 Slope → resistance → V = I × R', '08  GRAPH / RESULTS'],
['06 Feedback & Completion', 'S26 Row recorded', '09  FEEDBACK (DONE)'],
['04 Investigation & Experiment', 'S20 Build the law', '10  BUILD THE LAW'],
['06 Feedback & Completion', 'S28 Case closed', '11  FINAL FORMULA / CASE CLOSED'],
['05 AI & Information', 'S22 Ask Wattson · open', '12  ASK WATTSON'],
['07 Settings & Pause', 'S29 Pause menu', '13  PAUSE MENU + SETTINGS'],
['03 Gameplay HUD', 'S09 Volume popup', '14  VOLUME POPUP'],
['03 Gameplay HUD', 'S08 Done + hint toast', '15  TOASTS / HINTS'],
['08 Responsive Layouts', 'S34 Tablet 4:3 (iPad) · 2048×1536 · UI ×1.23', '16  TABLET LAYOUT'],
];
const grid = al('HORIZONTAL', board, { name: 'Panels', gap: 40, pad: [40, 80, 40, 80] });
grid.layoutWrap = 'WRAP'; grid.counterAxisSpacing = 48;
grid.resize(4000, 100); grid.layoutSizingHorizontal = 'FIXED'; grid.layoutSizingVertical = 'HUG';
const cloned = [];
for (const [pageName, frameName, label] of panels) {
const src = scr.findOne(n => n.type === 'FRAME' && n.name === frameName);
if (!src) throw new Error('missing screen ' + frameName);
const cell = col(grid, { name: label, gap: 10, w: 920 });
await txt(cell, 'Label/Eyebrow', label, { color: 'color/accent/cyan' });
const c = src.clone(); cell.appendChild(c); c.rescale(920 / src.width); c.name = frameName; keep(c);
c.strokes = [SOLID(P.cyan, 0.3)]; c.strokeWeight = 2; c.strokeAlign = 'OUTSIDE';
cloned.push(c.id);
}
const kit = row(board, { name: 'Component strip', gap: 28, cross: 'CENTER', pad: [20, 80, 60, 80], w: 4000 });
kit.layoutWrap = 'WRAP'; kit.counterAxisSpacing = 20;
await txt(kit, 'Label/Eyebrow', '17  COMPONENTS', { color: 'color/accent/cyan' });
for (const st of ['Primary', 'Secondary', 'Ghost', 'Raised']) { const b = inst(kit, CMP.Button, { Style: st, State: 'Default' }); setProps(b, { Label: st, 'Show icon': false }); }
inst(kit, CMP['Button/CTA'], { State: 'Default' });
inst(kit, CMP['Control bar'], { Variant: 'Full' });
inst(kit, CMP['Chapter chip'], { Variant: 'Compact' });
for (const st of ['On', 'Off']) inst(kit, CMP.Toggle, { State: st });
inst(kit, CMP.Slider);
for (const st of ['Done', 'Now', 'Next', 'Later']) inst(kit, CMP['Voltage step'], { State: st });
for (const k of ['Info', 'Success', 'Error']) inst(kit, CMP.Toast, { Kind: k });
for (const q of ['V', 'I', 'R']) inst(kit, CMP['Law tile'], { Quantity: q });
inst(kit, CMP['Ask Wattson avatar'], { State: 'Idle' });
let bottom = 0; for (const c of board.children) bottom = Math.max(bottom, c.y + c.height);
headRow.y = 0; grid.y = headRow.height + 20; kit.y = grid.y + grid.height + 20;
board.resize(4000, kit.y + kit.height + 40);
const foot = await txt(board, 'Label/Small', 'Ohm’s Law  ·  UI design reference  ·  same flow, better experience  ·  every element maps to an existing Unity HUD object', { color: 'color/text/muted' });
foot.x = 80; foot.y = board.height - 36;
const bytes = await board.exportAsync({ format: 'JPG', constraint: { type: 'SCALE', value: 0.3 } });
return { responsive: R.map(i => i[0].id), rules: rules.id, board: board.id, cloned, boardJpegBase64: figma.base64Encode(bytes), createdNodeIds: IDS.length };
}


figma.showUI(__html__, { width: 420, height: 420 });
(async () => {
  const results = {};
  const steps = [['01_foundations', part_01_foundations], ['02a_atoms', part_02a_atoms], ['02b_cards', part_02b_cards], ['02c_panels', part_02c_panels], ['03_screens_start_hud', part_03_screens_start_hud], ['04_screens_experiment', part_04_screens_experiment], ['05_screens_info', part_05_screens_info], ['06_screens_responsive_board', part_06_screens_responsive_board]];
  const t0 = Date.now();
  for (const [name, fn] of steps) {
    log('▶ ' + name);
    try {
      results[name] = await fn();
      const r = Object.assign({}, results[name]); delete r.boardJpegBase64;
      log('✓ ' + name + '  ' + JSON.stringify(r).slice(0, 400));
    } catch (e) {
      results[name] = { error: String(e && e.message || e), stack: String(e && e.stack || '').slice(0, 600) };
      log('✗ ' + name + ' FAILED: ' + (e && e.stack || e) + ' | later parts may fail in turn; send this log to Claude');
    }
  }
  const last = results['06_screens_responsive_board'] || {};
  figma.ui.postMessage({ done: true, seconds: Math.round((Date.now() - t0) / 1000), jpeg: last.boardJpegBase64 || null, result: JSON.stringify(Object.fromEntries(Object.entries(results).map(([k, v]) => { const c = Object.assign({}, v); delete c.boardJpegBase64; return [k, c]; })), null, 1) });
})().catch(e => { log('✗ ' + (e && e.stack || e)); figma.ui.postMessage({ error: String(e && e.message || e) }); });
figma.ui.onmessage = m => { if (m === 'close') figma.closePlugin(); };
