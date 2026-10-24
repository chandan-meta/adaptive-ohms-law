const assert = require('node:assert/strict');
const fs = require('node:fs');
const vm = require('node:vm');
const path = require('node:path');
const template = path.join(__dirname, '../Assets/WebGLTemplates/OhmsLaw');
const source = fs.readFileSync(path.join(template, 'TemplateData/loader.js'), 'utf8');

async function fixture({supported = true, reject = false, webkit = false} = {}) {
  const timers = [], frames = [], events = {}, nodes = {};
  const context = {clearRect() {}, drawImage() {}, createRadialGradient() { return {addColorStop() {}}; }, fillRect() {}};
  function element() {
    return {hidden: true, style: {}, attrs: {}, events: {}, children: [], classList: {add() {}},
      setAttribute(key, value) { this.attrs[key] = value; },
      addEventListener(name, fn) { this.events[name] = fn; },
      getBoundingClientRect() { return {width: 1280, height: 720}; },
      getContext() { return context; }, remove() {},
      appendChild(child) { this.children.push(child); }};
  }
  for (const id of ['loader', 'stage', 'bar', 'fill', 'dust', 'fullscreen-button', 'unity-warning', 'unity-canvas']) nodes[id] = element();
  const page = element();
  const doc = {documentElement: page, body: element(), getElementById(id) { return nodes[id]; },
    createElement: element, addEventListener(name, fn) { events[name] = fn; }};
  let entered = 0, exited = 0;
  const property = webkit ? 'webkitFullscreenElement' : 'fullscreenElement';
  const changed = webkit ? 'webkitfullscreenchange' : 'fullscreenchange';
  if (supported) {
    doc[webkit ? 'webkitFullscreenEnabled' : 'fullscreenEnabled'] = true;
    page[webkit ? 'webkitRequestFullscreen' : 'requestFullscreen'] = function () {
      assert.equal(this, page);
      entered++;
      if (reject) return Promise.reject(new Error('Denied'));
      doc[property] = page; events[changed](); return Promise.resolve();
    };
    doc[webkit ? 'webkitExitFullscreen' : 'exitFullscreen'] = function () {
      assert.equal(this, doc);
      exited++; doc[property] = null; events[changed](); return Promise.resolve();
    };
  }
  let finishUnity;
  const unity = new Promise(resolve => { finishUnity = resolve; });
  const sandbox = {document: doc, window: {devicePixelRatio: 1, addEventListener() {}},
    performance: {now() { return 0; }}, config: {}, loaderUrl: 'Build/test.loader.js',
    requestAnimationFrame(fn) { frames.push(fn); }, setTimeout(fn) { timers.push(fn); },
    createUnityInstance() { return unity; }};
  vm.runInNewContext(source, sandbox);
  const button = nodes['fullscreen-button'];
  assert.equal(button.hidden, true, 'hidden before Unity is ready');
  doc.body.children[0].onload();
  finishUnity(); await unity; await Promise.resolve();
  assert.equal(button.hidden, true, 'hidden while loading artwork fades');
  for (let frame = 1; frame <= 30; frame++) frames.shift()(frame * 100);
  while (timers.length) timers.shift()();
  assert.equal(button.hidden, !supported, 'only available after loading, on supported browsers');
  async function click() { button.events.click(); await new Promise(setImmediate); }
  return {button, nodes, doc, page, events, property, changed, click, counts() { return {entered, exited}; }};
}

(async () => {
  const html = fs.readFileSync(path.join(template, 'index.html'), 'utf8');
  const css = fs.readFileSync(path.join(template, 'TemplateData/style.css'), 'utf8');
  assert.match(html, /<button id="fullscreen-button"[^>]*type="button"[^>]*aria-label="Enter fullscreen"[^>]*hidden/);
  assert.match(html, /TemplateData\/style\.css\?v=20261024-fullscreen/);
  assert.match(html, /TemplateData\/loader\.js\?v=20261024-fullscreen/);
  assert.match(css, /#fullscreen-button\[hidden\]\s*\{\s*display:\s*none/);
  assert.match(css, /left:\s*max\(12px, env\(safe-area-inset-left\)\)/);
  assert.match(css, /bottom:\s*max\(12px, env\(safe-area-inset-bottom\)\)/);
  for (const webkit of [false, true]) {
    const f = await fixture({webkit});
    await f.click();
    assert.equal(f.button.attrs['aria-pressed'], 'true');
    assert.equal(f.button.attrs['aria-label'], 'Exit fullscreen');
    await f.click();
    assert.deepEqual(f.counts(), {entered: 1, exited: 1});
    assert.equal(f.button.attrs['aria-label'], 'Enter fullscreen');
    await f.click();
    f.doc[f.property] = null; f.events[f.changed]();
    assert.equal(f.button.attrs['aria-pressed'], 'false', 'Esc/browser exit updates state');
  }
  await fixture({supported: false});
  const denied = await fixture({reject: true});
  await denied.click();
  assert.equal(denied.button.attrs['aria-pressed'], 'false');
  assert.equal(denied.button.hidden, false, 'rejected request remains available for retry');
  assert.equal(denied.nodes['unity-warning'].children.length, 1);
  console.log('PASS: fullscreen readiness, enter/exit, Esc state, Safari API, unsupported/denied requests and accessible bottom-left control.');
})().catch(error => { console.error(error); process.exitCode = 1; });
