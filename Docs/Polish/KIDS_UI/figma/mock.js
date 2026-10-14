// A strict stand-in for the Figma plugin API, enough to run every part end to end in Node and fail on the mistakes real
// Figma rejects: unloaded fonts, FILL / HUG / ABSOLUTE outside auto layout, malformed paints and effects, unknown
// variables and styles, component properties that don't exist, moving or resizing layers inside instances.
// Usage: node mock.js  (runs build.py's bundle: lib + all parts) → prints the build log and a node census.
'use strict';
const fs = require('fs'), path = require('path');

let nextId = 1;
const id = () => (nextId++) + ':' + (nextId * 7 % 1000);
const byId = new Map();
const loaded = new Set();
const FONTS = [];
for (const s of ['Light', 'Regular', 'Medium', 'SemiBold', 'Bold']) FONTS.push({ family: 'Fredoka', style: s });
for (const s of ['ExtraLight', 'Light', 'Regular', 'Medium', 'SemiBold', 'Bold', 'ExtraBold', 'Black']) FONTS.push({ family: 'Nunito', style: s });
for (const s of ['Regular', 'Medium', 'Semi Bold', 'Bold']) FONTS.push({ family: 'Inter', style: s });
const fkey = f => f.family + '|' + f.style;
const fail = m => { throw new Error('[figma] ' + m); };

function checkColor(c, alpha, where) {
  if (!c || typeof c.r !== 'number' || typeof c.g !== 'number' || typeof c.b !== 'number') fail(where + ': bad colour ' + JSON.stringify(c));
  if (alpha && typeof c.a !== 'number') fail(where + ': colour needs a');
  if (!alpha && 'a' in c) fail(where + ': SOLID colour must not have a (use opacity)');
  for (const k of ['r', 'g', 'b']) if (c[k] < 0 || c[k] > 1 || isNaN(c[k])) fail(where + ': channel out of range');
}
function checkPaints(list, where) {
  if (!Array.isArray(list)) fail(where + ': paints must be an array');
  for (const p of list) {
    if (p.type === 'SOLID') { checkColor(p.color, false, where); if (p.opacity !== undefined && (p.opacity < 0 || p.opacity > 1)) fail(where + ': opacity'); }
    else if (p.type === 'GRADIENT_LINEAR' || p.type === 'GRADIENT_RADIAL') {
      if (!Array.isArray(p.gradientTransform) || p.gradientTransform.length !== 2) fail(where + ': gradientTransform');
      if (!Array.isArray(p.gradientStops) || p.gradientStops.length < 2) fail(where + ': gradientStops');
      for (const s of p.gradientStops) { checkColor(s.color, true, where); if (s.position < 0 || s.position > 1) fail(where + ': stop position'); }
    } else if (p.type === 'IMAGE') { if (!p.imageHash) fail(where + ': image without hash'); if (!['FILL', 'FIT', 'CROP', 'TILE'].includes(p.scaleMode)) fail(where + ': scaleMode'); }
    else fail(where + ': paint type ' + p.type);
  }
  return list.map(p => Object.freeze(Object.assign({}, p)));
}
function checkEffects(list, where) {
  if (!Array.isArray(list)) fail(where + ': effects must be an array');
  for (const e of list) {
    if (e.type === 'DROP_SHADOW' || e.type === 'INNER_SHADOW') {
      checkColor(e.color, true, where);
      if (!e.offset || typeof e.offset.x !== 'number' || typeof e.offset.y !== 'number') fail(where + ': offset');
      if (typeof e.radius !== 'number' || e.radius < 0) fail(where + ': radius');
      if (typeof e.visible !== 'boolean' || !e.blendMode) fail(where + ': visible / blendMode');
      if (e.type === 'INNER_SHADOW' && 'showShadowBehindNode' in e) fail(where + ': inner shadow cannot have showShadowBehindNode');
    } else if (e.type === 'LAYER_BLUR' || e.type === 'BACKGROUND_BLUR') { if (typeof e.radius !== 'number') fail(where + ': blur radius'); }
    else fail(where + ': effect type ' + e.type);
  }
  return list;
}

// Styles and variables
const store = { text: [], paint: [], effect: [], collections: [], variables: [], images: 0 };
function makeStyle(kind) {
  const s = { id: 'S:' + id(), type: kind.toUpperCase(), name: '', description: '', remove() { store[kind].splice(store[kind].indexOf(s), 1); } };
  if (kind === 'text') {
    let font = { family: 'Inter', style: 'Regular' };
    Object.defineProperty(s, 'fontName', { get: () => font, set: v => { if (!loaded.has(fkey(v))) fail('text style font not loaded ' + fkey(v)); font = v; } });
    for (const k of ['fontSize', 'lineHeight', 'letterSpacing', 'textCase']) {
      let val = k === 'fontSize' ? 12 : k === 'textCase' ? 'ORIGINAL' : { unit: 'AUTO' };
      Object.defineProperty(s, k, { get: () => val, set: v => { if (!loaded.has(fkey(font))) fail('style font not loaded for ' + k); val = v; } });
    }
  }
  if (kind === 'paint') { let p = []; Object.defineProperty(s, 'paints', { get: () => p, set: v => { p = checkPaints(v, 'paint style ' + s.name); } }); }
  if (kind === 'effect') { let e = []; Object.defineProperty(s, 'effects', { get: () => e, set: v => { e = checkEffects(v, 'effect style ' + s.name); } }); }
  store[kind].push(s);
  return s;
}
const findStyle = sid => store.text.concat(store.paint, store.effect).find(s => s.id === sid);
const findVar = vid => store.variables.find(v => v.id === vid);

const LAYOUT_PROPS = ['itemSpacing', 'counterAxisSpacing', 'paddingLeft', 'paddingRight', 'paddingTop', 'paddingBottom', 'topLeftRadius', 'topRightRadius', 'bottomLeftRadius', 'bottomRightRadius', 'width', 'height', 'opacity', 'strokeWeight', 'cornerRadius'];
const CONTAINERS = ['PAGE', 'FRAME', 'COMPONENT', 'COMPONENT_SET', 'INSTANCE', 'GROUP', 'SECTION'];

class Node {
  constructor(type) {
    this.id = id(); this.type = type; this.name = type; this.parent = null; this.removed = false; byId.set(this.id, this);
    this._x = 0; this._y = 0; this._w = 100; this._h = 100; this._fills = []; this._strokes = []; this._effects = [];
    this.opacity = 1; this.visible = true; this.rotation = 0; this.strokeWeight = 1; this.strokeAlign = 'INSIDE'; this.cornerRadius = 0;
    this.constraints = { horizontal: 'MIN', vertical: 'MIN' }; this.plugin = {}; this.bound = {};
    this._lsh = 'FIXED'; this._lsv = 'FIXED'; this._lp = 'AUTO'; this._grow = 0;
    this._refs = null; this.description = '';
    if (CONTAINERS.includes(type)) this.children = [];
    if (['FRAME', 'COMPONENT', 'COMPONENT_SET', 'INSTANCE'].includes(type)) {
      this._layoutMode = 'NONE'; this.primaryAxisAlignItems = 'MIN'; this.counterAxisAlignItems = 'MIN'; this.layoutWrap = 'NO_WRAP';
      this.itemSpacing = 0; this.counterAxisSpacing = 0; this.paddingLeft = this.paddingRight = this.paddingTop = this.paddingBottom = 0; this.clipsContent = true;
    }
  }
  inInstance() { let p = this.parent; while (p) { if (p.type === 'INSTANCE') return true; p = p.parent; } return false; }
  guardMove(what) { if (this.inInstance()) fail('cannot ' + what + ' "' + this.name + '": it is inside an instance'); }
  get x() { return this._x; } set x(v) { if (isNaN(v)) fail('x NaN on ' + this.name); this.guardMove('move'); this._x = v; }
  get y() { return this._y; } set y(v) { if (isNaN(v)) fail('y NaN on ' + this.name); this.guardMove('move'); this._y = v; }
  get fills() { return this._fills; } set fills(v) { this._fills = checkPaints(v, 'fills of ' + this.name); this.fillStyleId = ''; }
  get strokes() { return this._strokes; } set strokes(v) { this._strokes = checkPaints(v, 'strokes of ' + this.name); }
  get effects() { return this._effects; } set effects(v) { this._effects = checkEffects(v, 'effects of ' + this.name); }
  get layoutMode() { return this._layoutMode; }
  set layoutMode(v) { if (!['NONE', 'HORIZONTAL', 'VERTICAL'].includes(v)) fail('layoutMode ' + v); if (this._layoutMode === undefined) fail('layoutMode on ' + this.type); this._layoutMode = v; if (v !== 'NONE') { this._lsh = this._lsh || 'FIXED'; } }
  isAL() { return this._layoutMode && this._layoutMode !== 'NONE'; }
  parentAL() { return this.parent && this.parent.isAL && this.parent.isAL(); }
  get layoutSizingHorizontal() { return this._lsh; }
  set layoutSizingHorizontal(v) { this.sizing('h', v); }
  get layoutSizingVertical() { return this._lsv; }
  set layoutSizingVertical(v) { this.sizing('v', v); }
  sizing(axis, v) {
    if (!['FIXED', 'HUG', 'FILL'].includes(v)) fail('sizing ' + v);
    if (v === 'FILL' && !this.parentAL()) fail('FILL on "' + this.name + '" needs an auto-layout parent');
    if (v === 'HUG' && !(this.isAL && this.isAL()) && this.type !== 'TEXT' && this.type !== 'INSTANCE') fail('HUG on "' + this.name + '" needs auto layout or text');
    if (v === 'HUG' && this.type === 'INSTANCE' && !this.isAL()) fail('HUG on instance without auto layout');
    if (axis === 'h') this._lsh = v; else this._lsv = v;
  }
  get layoutPositioning() { return this._lp; }
  set layoutPositioning(v) { if (v === 'ABSOLUTE' && !this.parentAL()) fail('ABSOLUTE on "' + this.name + '" needs an auto-layout parent'); this._lp = v; }
  get layoutGrow() { return this._grow; } set layoutGrow(v) { if (!this.parentAL()) fail('layoutGrow needs an auto-layout parent'); this._grow = v; }
  // Size, with auto-layout hugging computed from children
  get width() { return this.measure('h'); }
  get height() { return this.measure('v'); }
  measure(axis) {
    const sizing = axis === 'h' ? this._lsh : this._lsv;
    if (sizing === 'FILL' && this.parent && this.parent.isAL && this.parent.isAL()) {
      const p = this.parent, primary = (p._layoutMode === 'HORIZONTAL') === (axis === 'h');
      const inner = (axis === 'h' ? p.width - p.paddingLeft - p.paddingRight : p.height - p.paddingTop - p.paddingBottom);
      if (!primary) return Math.max(1, inner);
      const others = p.children.filter(c => c !== this && c.visible && c._lp !== 'ABSOLUTE' && (axis === 'h' ? c._lsh : c._lsv) !== 'FILL');
      const fills = p.children.filter(c => c.visible && c._lp !== 'ABSOLUTE' && (axis === 'h' ? c._lsh : c._lsv) === 'FILL').length;
      const used = others.reduce((a, c) => a + (axis === 'h' ? c.width : c.height), 0) + p.itemSpacing * Math.max(0, p.children.filter(c => c.visible && c._lp !== 'ABSOLUTE').length - 1);
      return Math.max(1, (inner - used) / Math.max(1, fills));
    }
    if (sizing === 'HUG' && this.isAL && this.isAL()) {
      const kids = this.children.filter(c => c.visible && c._lp !== 'ABSOLUTE');
      const primary = (this._layoutMode === 'HORIZONTAL') === (axis === 'h');
      const padA = axis === 'h' ? this.paddingLeft + this.paddingRight : this.paddingTop + this.paddingBottom;
      const sizes = kids.map(c => ((axis === 'h' ? c._lsh : c._lsv) === 'FILL' ? (primary ? 0 : 0) : (axis === 'h' ? c.width : c.height)));
      if (primary && this.layoutWrap === 'WRAP') fail('HUG along a wrapping axis');
      if (primary) return padA + sizes.reduce((a, b) => a + b, 0) + this.itemSpacing * Math.max(0, kids.length - 1);
      if (this.layoutWrap === 'WRAP') { // rows of the fixed width
        const W = this.width - this.paddingLeft - this.paddingRight; let rowW = 0, rowH = 0, total = 0, rows = 0;
        for (const c of kids) { if (rowW && rowW + this.itemSpacing + c.width > W) { total += rowH; rows++; rowW = 0; rowH = 0; } rowW += (rowW ? this.itemSpacing : 0) + c.width; rowH = Math.max(rowH, c.height); }
        total += rowH; rows++;
        return padA + total + this.counterAxisSpacing * (rows - 1);
      }
      return padA + (sizes.length ? Math.max.apply(null, sizes) : 0);
    }
    if (this.type === 'TEXT' && ((axis === 'h' && this._autoResize === 'WIDTH_AND_HEIGHT') || (axis === 'v' && this._autoResize !== 'NONE'))) return this.textSize(axis);
    return axis === 'h' ? this._w : this._h;
  }
  resize(w, h) {
    if (!(w >= 0.01) || !(h >= 0.01)) fail('resize ' + w + '×' + h + ' on ' + this.name);
    if (this.inInstance()) fail('cannot resize "' + this.name + '" inside an instance');
    this._w = w; this._h = h;
    if (this._lsh === 'HUG') this._lsh = 'FIXED';
    if (this._lsv === 'HUG') this._lsv = 'FIXED';
    if (this.type === 'TEXT' && this._autoResize === 'WIDTH_AND_HEIGHT') this._autoResize = 'NONE';
  }
  resizeWithoutConstraints(w, h) { this.resize(w, h); }
  rescale(s) { if (!(s > 0)) fail('rescale ' + s); this._w *= s; this._h *= s; }
  appendChild(c) { this.insertChild(this.children ? this.children.length : 0, c); }
  insertChild(i, c) {
    if (!this.children) fail('appendChild on ' + this.type + ' "' + this.name + '"');
    if (this.type === 'INSTANCE') fail('cannot add children to an instance');
    if (c.removed) fail('appendChild of a removed node');
    let p = this; while (p) { if (p === c) fail('cycle'); p = p.parent; }
    if (this.type === 'COMPONENT_SET' && c.type !== 'COMPONENT') fail('component set can only hold components');
    if (c.parent) c.parent.children.splice(c.parent.children.indexOf(c), 1);
    this.children.splice(i, 0, c); c.parent = this;
  }
  remove() { if (this.inInstance()) fail('cannot remove a layer inside an instance'); if (this.parent) this.parent.children.splice(this.parent.children.indexOf(this), 1); this.parent = null; this.removed = true; }
  findAll(f) { const out = []; const walk = n => { for (const c of n.children || []) { if (!f || f(c)) out.push(c); walk(c); } }; walk(this); return out; }
  findOne(f) { return this.findAll(f)[0] || null; }
  findChild(f) { return (this.children || []).find(f) || null; }
  findAllWithCriteria(c) { return this.findAll(n => !c.types || c.types.includes(n.type)); }
  setSharedPluginData(ns, k, v) { this.plugin[ns + k] = v; }
  getSharedPluginData(ns, k) { return this.plugin[ns + k] || ''; }
  setBoundVariable(prop, v) {
    if (!LAYOUT_PROPS.includes(prop)) fail('cannot bind ' + prop);
    if (v && (!findVar(v.id) || v.resolvedType !== 'FLOAT')) fail('bind ' + prop + ' to a non-FLOAT variable');
    if (prop.indexOf('Radius') > 0 && !['RECTANGLE', 'FRAME', 'COMPONENT', 'INSTANCE', 'COMPONENT_SET'].includes(this.type)) fail('radius on ' + this.type);
    this.bound[prop] = v ? v.id : null;
    if (v) { const val = v.valuesByMode[Object.keys(v.valuesByMode)[0]]; if (prop in this || prop.indexOf('padding') === 0) this[prop] = val; }
  }
  async setFillStyleIdAsync(sid) { const s = findStyle(sid); if (!s || s.type !== 'PAINT') fail('fill style ' + sid); this._fills = s.paints; this.fillStyleId = sid; }
  async setEffectStyleIdAsync(sid) { const s = findStyle(sid); if (!s || s.type !== 'EFFECT') fail('effect style ' + sid); this._effects = s.effects; this.effectStyleId = sid; }
  get componentPropertyReferences() { return this._refs; }
  set componentPropertyReferences(r) {
    let owner = this.parent; while (owner && owner.type !== 'COMPONENT' && owner.type !== 'COMPONENT_SET') owner = owner.parent;
    if (!owner) fail('property reference outside a component on ' + this.name);
    const defsOwner = owner.parent && owner.parent.type === 'COMPONENT_SET' ? owner.parent : owner;
    const defs = defsOwner._defs || {};
    for (const k of Object.keys(r || {})) {
      const d = defs[r[k]]; if (!d) fail('no property ' + r[k] + ' for ' + this.name);
      if (k === 'characters' && (this.type !== 'TEXT' || d.type !== 'TEXT')) fail('characters ref needs TEXT');
      if (k === 'visible' && d.type !== 'BOOLEAN') fail('visible ref needs BOOLEAN');
      if (k === 'mainComponent' && (this.type !== 'INSTANCE' || d.type !== 'INSTANCE_SWAP')) fail('mainComponent ref needs an instance + INSTANCE_SWAP');
    }
    this._refs = r;
  }
  clone() {
    const c = deepCopy(this);
    c.plugin = Object.assign({}, this.plugin);
    if (this.type === 'COMPONENT') c.type = 'FRAME';
    if (this.parent) { this.parent.children.splice(this.parent.children.indexOf(this) + 1, 0, c); c.parent = this.parent; }
    return c;
  }
}
// Text
class Text extends Node {
  constructor() { super('TEXT'); this._font = { family: 'Inter', style: 'Regular' }; this._chars = ''; this._autoResize = 'WIDTH_AND_HEIGHT'; this.fontSize = 12; this.lineHeight = { unit: 'AUTO' }; this.textAlignHorizontal = 'LEFT'; this.textCase = 'ORIGINAL'; this.textStyleId = ''; }
  needFont() { if (!loaded.has(fkey(this._font))) fail('font not loaded: ' + fkey(this._font) + ' on "' + this.name + '"'); }
  get fontName() { return this._font; } set fontName(f) { if (!loaded.has(fkey(f))) fail('font not loaded: ' + fkey(f)); this._font = f; }
  get characters() { return this._chars; } set characters(v) { this.needFont(); if (typeof v !== 'string') fail('characters must be a string'); this._chars = v; }
  get textAutoResize() { return this._autoResize; } set textAutoResize(v) { this._autoResize = v; }
  async setTextStyleIdAsync(sid) { const s = findStyle(sid); if (!s || s.type !== 'TEXT') fail('text style ' + sid); if (!loaded.has(fkey(s.fontName))) fail('text style font not loaded ' + fkey(s.fontName)); this._font = s.fontName; this.fontSize = s.fontSize; this.lineHeight = s.lineHeight; this.textStyleId = sid; }
  setRangeFills(a, b, p) { if (a < 0 || b > this._chars.length || a >= b) fail('setRangeFills range ' + a + '-' + b + ' of ' + this._chars.length); checkPaints(p, 'range fills'); }
  textSize(axis) {
    const lh = this.lineHeight && this.lineHeight.unit === 'PIXELS' ? this.lineHeight.value : this.fontSize * 1.3;
    const lines = this._chars.split('\n');
    if (axis === 'h') return Math.max(1, Math.max.apply(null, lines.map(l => l.length)) * this.fontSize * 0.52);
    const w = this._autoResize === 'WIDTH_AND_HEIGHT' ? 1e9 : this.width;
    const perLine = Math.max(1, Math.floor(w / (this.fontSize * 0.52)));
    return lh * lines.reduce((a, l) => a + Math.max(1, Math.ceil(l.length / perLine)), 0);
  }
}
// Components, sets, instances
class Component extends Node {
  constructor() { super('COMPONENT'); this._defs = {}; this.key = 'key' + this.id; }
  get variantProperties() { if (!this.parent || this.parent.type !== 'COMPONENT_SET') return null; const o = {}; for (const kv of this.name.split(', ')) { const p = kv.split('='); o[p[0]] = p[1]; } return o; }
  addComponentProperty(name, type, def, opts) {
    if (this.parent && this.parent.type === 'COMPONENT_SET') fail('add properties to the set, not a variant');
    return addProp(this, name, type, def, opts);
  }
  get componentPropertyDefinitions() { return this._defs; }
  createInstance() { return makeInstance(this); }
}
function addProp(owner, name, type, def, opts) {
  if (!['TEXT', 'BOOLEAN', 'INSTANCE_SWAP', 'VARIANT'].includes(type)) fail('property type ' + type);
  if (type === 'TEXT' && typeof def !== 'string') fail('TEXT default must be a string');
  if (type === 'BOOLEAN' && typeof def !== 'boolean') fail('BOOLEAN default');
  if (type === 'INSTANCE_SWAP') { const c = byId.get(def); if (!c || c.type !== 'COMPONENT') fail('INSTANCE_SWAP default must be a component id'); }
  const key = name + '#' + id();
  owner._defs[key] = { type: type, defaultValue: def, preferredValues: opts && opts.preferredValues };
  return key;
}
function makeInstance(main) {
  if (main.removed) fail('instance of a removed component');
  const i = new Node('INSTANCE');
  i.name = main.parent && main.parent.type === 'COMPONENT_SET' ? main.parent.name : main.name;
  copyInto(i, main);
  i.main = main;
  return i;
}
function copyInto(i, main) {
  for (const k of ['_w', '_h', '_layoutMode', 'primaryAxisAlignItems', 'counterAxisAlignItems', 'itemSpacing', 'paddingLeft', 'paddingRight', 'paddingTop', 'paddingBottom', '_lsh', '_lsv', '_fills', '_strokes', '_effects', 'opacity', 'layoutWrap', 'counterAxisSpacing']) i[k] = main[k];
  i.children = [];
  for (const ch of main.children) { const c = deepCopy(ch); c.parent = i; i.children.push(c); }
}
function deepCopy(n) {
  const c = n.type === 'TEXT' ? new Text() : n.type === 'INSTANCE' ? new Node('INSTANCE') : new Node(n.type);
  for (const k of Object.keys(n)) if (!['id', 'parent', 'children', 'plugin'].includes(k)) c[k] = n[k];
  if (n.children) { c.children = []; for (const ch of n.children) { const cc = deepCopy(ch); cc.parent = c; c.children.push(cc); } }
  return c;
}
function setOf(main) { return main.parent && main.parent.type === 'COMPONENT_SET' ? main.parent : main; }
Object.defineProperty(Node.prototype, 'componentProperties', {
  get() {
    if (this.type !== 'INSTANCE') fail('componentProperties on ' + this.type);
    const owner = setOf(this.main), out = {};
    for (const k of Object.keys(owner._defs || {})) out[k] = { type: owner._defs[k].type, value: owner._defs[k].defaultValue };
    if (owner.type === 'COMPONENT_SET') for (const k of Object.keys(this.main.variantProperties)) out[k] = { type: 'VARIANT', value: this.main.variantProperties[k] };
    return out;
  },
});
Node.prototype.setProperties = function (obj) {
  if (this.type !== 'INSTANCE') fail('setProperties on ' + this.type);
  const owner = setOf(this.main), defs = owner._defs || {};
  const variant = Object.assign({}, this.main.variantProperties || {});
  let changeVariant = false;
  for (const k of Object.keys(obj)) {
    if (defs[k]) {
      const d = defs[k], v = obj[k];
      const refs = this.findAll(n => n._refs && Object.values(n._refs).includes(k));
      if (d.type === 'TEXT') { if (typeof v !== 'string') fail('TEXT value'); refs.forEach(n => { n._chars = v; }); }
      else if (d.type === 'BOOLEAN') { if (typeof v !== 'boolean') fail('BOOLEAN value'); refs.forEach(n => { n.visible = v; }); }
      else if (d.type === 'INSTANCE_SWAP') { const c = byId.get(v); if (!c || c.type !== 'COMPONENT') fail('swap value must be a component id'); refs.forEach(n => n.swapComponent(c)); }
    } else if (owner.type === 'COMPONENT_SET' && k in variant) { variant[k] = obj[k]; changeVariant = true; }
    else fail('unknown property ' + k + ' on instance of ' + owner.name);
  }
  if (changeVariant) {
    const v = owner.children.find(c => Object.keys(variant).every(k => c.variantProperties[k] === variant[k]));
    if (!v) fail('no variant ' + JSON.stringify(variant) + ' in ' + owner.name);
    this.swapComponent(v);
  }
};
Node.prototype.swapComponent = function (c) { if (this.type !== 'INSTANCE') fail('swapComponent on ' + this.type); const keepName = this.name; const vis = this.visible; copyInto(this, c); this.main = c; this.name = keepName; this.visible = vis; };
Node.prototype.getMainComponentAsync = async function () { return this.main; };
Node.prototype.detachInstance = function () { this.type = 'FRAME'; return this; };

// Document
const root = new Node('DOCUMENT'); root.children = [];
function newPage(name) { const p = new Node('PAGE'); p.name = name; p.parent = root; root.children.push(p); return p; }
function allNodes() { const out = []; const walk = n => { out.push(n); (n.children || []).forEach(walk); }; walk(root); return out; }
// The existing file, as the UI v2 ledger records it (6 pages, OHM collections, OHM/ text styles, OHM/FX styles).
['00 — References and UI Audit', '01 — Foundations and Design Tokens', '02 — Components and States', '03 — Desktop Screens', '04 — Tablet Screens', '05 — Prototype Flows and Unity Handoff'].forEach(newPage);
let current = root.children[0];

const figma = {
  root: root,
  get currentPage() { return current; },
  async setCurrentPageAsync(p) { current = p; },
  async loadAllPagesAsync() {},
  createPage() { if (root.children.length >= 40) fail('page limit'); return newPage('Page'); },
  async loadFontAsync(f) { if (!FONTS.find(x => fkey(x) === fkey(f))) fail('font unavailable ' + fkey(f)); loaded.add(fkey(f)); },
  async listAvailableFontsAsync() { return FONTS.map(f => ({ fontName: f })); },
  async getLocalTextStylesAsync() { return store.text.slice(); },
  async getLocalPaintStylesAsync() { return store.paint.slice(); },
  async getLocalEffectStylesAsync() { return store.effect.slice(); },
  createTextStyle() { return makeStyle('text'); },
  createPaintStyle() { return makeStyle('paint'); },
  createEffectStyle() { return makeStyle('effect'); },
  createFrame() { const n = new Node('FRAME'); current.appendChild(n); n.fills = [{ type: 'SOLID', color: { r: 1, g: 1, b: 1 } }]; return n; },
  createComponent() { const n = new Component(); current.appendChild(n); return n; },
  createRectangle() { const n = new Node('RECTANGLE'); current.appendChild(n); return n; },
  createEllipse() { const n = new Node('ELLIPSE'); current.appendChild(n); return n; },
  createVector() { const n = new Node('VECTOR'); n.vectorPaths = []; current.appendChild(n); return n; },
  createLine() { const n = new Node('LINE'); current.appendChild(n); return n; },
  createText() { const n = new Text(); current.appendChild(n); return n; },
  createSection() { const n = new Node('SECTION'); current.appendChild(n); return n; },
  createImage(bytes) { if (!(bytes instanceof Uint8Array)) fail('createImage needs a Uint8Array'); store.images++; return { hash: 'img' + store.images }; },
  createNodeFromSvg(svg) {
    if (svg.indexOf('<svg') !== 0) fail('svg must start with <svg');
    const f = new Node('FRAME'); f.name = 'svg'; f._w = 24; f._h = 24; current.appendChild(f);
    const els = svg.match(/<(path|circle|rect|line)\b[^>]*\/>/g) || []; if (!els.length) fail('empty svg');
    for (const e of els) { const v = new Node('VECTOR'); v.name = 'Vector'; if (/stroke="#/.test(e)) v._strokes = [{ type: 'SOLID', color: { r: 1, g: 1, b: 1 } }]; if (/fill="#/.test(e)) v._fills = [{ type: 'SOLID', color: { r: 1, g: 1, b: 1 } }]; f.appendChild(v); }
    return f;
  },
  createComponentFromNode(n) {
    if (n.type !== 'FRAME' && n.type !== 'GROUP') fail('createComponentFromNode on ' + n.type);
    const c = new Component(); c.name = n.name; c._w = n._w; c._h = n._h;
    for (const ch of n.children.slice()) c.appendChild(ch);
    const p = n.parent; p.insertChild(p.children.indexOf(n), c); n.remove(); return c;
  },
  combineAsVariants(nodes, parent) {
    if (!nodes.length) fail('combine nothing');
    const seen = new Set();
    for (const n of nodes) {
      if (n.type !== 'COMPONENT') fail('combineAsVariants needs components');
      if (!/^[^=,]+=[^=,]+(, [^=,]+=[^=,]+)*$/.test(n.name)) fail('variant name "' + n.name + '"');
      if (seen.has(n.name)) fail('duplicate variant ' + n.name); seen.add(n.name);
    }
    const s = new Node('COMPONENT_SET'); s._defs = {}; parent.appendChild(s);
    for (const n of nodes) s.appendChild(n);
    s.addComponentProperty = (name, type, def, opts) => addProp(s, name, type, def, opts);
    Object.defineProperty(s, 'defaultVariant', { get: () => s.children[0] });
    return s;
  },
  variables: {
    async getLocalVariableCollectionsAsync() { return store.collections.slice(); },
    async getLocalVariablesAsync() { return store.variables.slice(); },
    createVariableCollection(name) {
      const c = { id: 'VariableCollectionId:' + id(), name: name, modes: [{ modeId: id(), name: 'Mode 1' }],
        renameMode(mid, n) { const m = c.modes.find(x => x.modeId === mid); if (!m) fail('mode'); m.name = n; },
        addMode(n) { const m = { modeId: id(), name: n }; c.modes.push(m); return m.modeId; } };
      store.collections.push(c); return c;
    },
    createVariable(name, col, type) {
      if (typeof col === 'string') fail('pass the collection node, not its id');
      if (!['COLOR', 'FLOAT', 'STRING', 'BOOLEAN'].includes(type)) fail('variable type');
      if (store.variables.find(v => v.name === name && v.variableCollectionId === col.id)) fail('duplicate variable ' + name);
      const v = { id: 'VariableID:' + id(), name: name, variableCollectionId: col.id, resolvedType: type, valuesByMode: {}, description: '', scopes: ['ALL_SCOPES'],
        setValueForMode(mid, val) {
          if (!col.modes.find(m => m.modeId === mid)) fail('unknown mode for ' + name);
          if (type === 'COLOR' && !(val.type === 'VARIABLE_ALIAS' || (typeof val.r === 'number'))) fail('COLOR value');
          if (type === 'FLOAT' && typeof val !== 'number' && !(val && val.type === 'VARIABLE_ALIAS')) fail('FLOAT value for ' + name);
          v.valuesByMode[mid] = val;
        } };
      store.variables.push(v); return v;
    },
    createVariableAlias(v) { if (!findVar(v.id)) fail('alias of unknown variable'); return { type: 'VARIABLE_ALIAS', id: v.id }; },
    setBoundVariableForPaint(p, field, v) { if (p.type !== 'SOLID' || field !== 'color') fail('bind paint'); if (!v || v.resolvedType !== 'COLOR') fail('bind paint to non-colour ' + (v && v.name)); return Object.assign({}, p, { boundVariables: { color: { type: 'VARIABLE_ALIAS', id: v.id } } }); },
  },
};
Object.defineProperty(Node.prototype, 'variantProperties', { get() { return Component.prototype.__lookupGetter__('variantProperties').call(this); }, configurable: true });

// Seed the UI v2 state the ledger describes, so the update path (not just create) is exercised.
(function seed() {
  const prim = figma.variables.createVariableCollection('OHM / Primitives'); prim.renameMode(prim.modes[0].modeId, 'Value');
  const sem = figma.variables.createVariableCollection('OHM / Semantic'); sem.renameMode(sem.modes[0].modeId, 'Acrylic'); sem.addMode('Solid');
  const lay = figma.variables.createVariableCollection('OHM / Layout and Motion'); lay.renameMode(lay.modes[0].modeId, 'Value');
  for (const n of ['navy', 'surface', 'blue', 'amber']) figma.variables.createVariable('primitive/' + n, prim, 'COLOR').setValueForMode(prim.modes[0].modeId, { r: 0, g: 0, b: 0, a: 1 });
  for (const n of ['bg', 'action', 'text']) figma.variables.createVariable('color/' + n, sem, 'COLOR');
  for (const n of [4, 8, 16, 24]) figma.variables.createVariable('space/' + n, lay, 'FLOAT').setValueForMode(lay.modes[0].modeId, n);
  for (const n of ['Display', 'Body', 'TabletBody']) { const s = makeStyle('text'); s.name = 'OHM/' + n; }
  const ps = makeStyle('paint'); ps.name = 'OHM/FX/Illuminated primary'; ps.paints = [{ type: 'SOLID', color: { r: 0, g: 0, b: 1 } }];
  const es = makeStyle('effect'); es.name = 'OHM/FX/Panel halo';
})();

const t00 = Date.now();
async function main() {
  const bundle = fs.readFileSync(path.join(__dirname, 'build', 'bundle.js'), 'utf8');
  const run = new Function('figma', '"use strict"; return (async () => {\nvar IMG_HASHES = {};\n' + bundle + '\nfor (const k of ["wattson", "reference"].concat(SCREENS.map(s => "d_" + s[0]), SCREENS.map(s => "t_" + s[0]), SCREENS.map(s => "before_" + s[0]))) IMG_HASHES[k] = figma.createImage(new Uint8Array([1])).hash;\nawait runAll();\nreturn LOG;\n})();');
  const t0 = Date.now();
  const log = await run(figma);
  console.log(log.join('\n'));
  const census = {};
  for (const n of allNodes()) census[n.type] = (census[n.type] || 0) + 1;
  console.log('census', JSON.stringify(census), 'styles', store.text.length, store.paint.length, store.effect.length, 'variables', store.variables.length, 'images', store.images, (Date.now() - t0) + ' ms');
  // Layout sanity: nothing on a screen frame may poke outside it.
  const pg = root.children.find(p => p.name === 'Kids UI · Screens');
  let bad = 0;
  for (const f of pg.children.filter(n => n.type === 'FRAME' && /^[DT]\d/.test(n.name))) for (const c of f.children) {
    if (c.x < -0.5 || c.y < -0.5 || c.x + c.width > f.width + 0.5 || c.y + c.height > f.height + 0.5) { bad++; console.log('OUT OF FRAME', f.name, '/', c.name, Math.round(c.x), Math.round(c.y), Math.round(c.width) + '×' + Math.round(c.height)); }
  }
  // Overlaps between UI blocks on each screen (the scrim and decoration excluded).
  for (const f of pg.children.filter(n => n.type === 'FRAME' && /^[DT]\d/.test(n.name))) {
    const kids = f.children.filter(c => c.name !== 'Scrim');
    for (let i = 0; i < kids.length; i++) for (let j = i + 1; j < kids.length; j++) {
      const a = kids[i], b = kids[j];
      const ox = Math.min(a.x + a.width, b.x + b.width) - Math.max(a.x, b.x), oy = Math.min(a.y + a.height, b.y + b.height) - Math.max(a.y, b.y);
      if (ox > 2 && oy > 2) { bad++; console.log('OVERLAP', f.name, ':', a.name, '×', b.name, Math.round(ox) + '×' + Math.round(oy)); }
    }
  }
  console.log(bad ? bad + ' layout problems' : 'layout OK');
}
main().catch(e => { console.error('FAILED:', e.stack || e.message); process.exit(1); });
