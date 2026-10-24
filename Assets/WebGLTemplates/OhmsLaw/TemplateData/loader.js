// The module's loading screen. The bar follows the real load (createUnityInstance's progress: the build's download and
// start-up), eased so it never jumps; drifting light in three depths over the artwork plus a few sparks round the Omega.
// When the module is ready the bar fills, pulses once, holds 0.2 s and the screen fades into the module (0.65 s).
(function () {
  var loader = document.getElementById("loader"), stage = document.getElementById("stage");
  var bar = document.getElementById("bar"), fill = document.getElementById("fill");
  var dust = document.getElementById("dust"), g = dust.getContext("2d");
  var calm = window.matchMedia && matchMedia("(prefers-reduced-motion: reduce)").matches;
  var target = 0, shown = 0, ready = false, finishing = false, running = true, last = performance.now();

  // Soft round sprites drawn additively: no blur filters, cheap while the build loads.
  function sprite(rgb) {
    var c = document.createElement("canvas"); c.width = c.height = 64;
    var x = c.getContext("2d"), gr = x.createRadialGradient(32, 32, 0, 32, 32, 32);
    gr.addColorStop(0, "rgba(" + rgb + ",1)"); gr.addColorStop(.3, "rgba(" + rgb + ",.5)"); gr.addColorStop(1, "rgba(" + rgb + ",0)");
    x.fillStyle = gr; x.fillRect(0, 0, 64, 64);
    return c;
  }
  var cyan = sprite("90,200,255"), amber = sprite("255,196,110"), spark = sprite("185,230,255");

  // In artwork pixels (1672 x 941): far motes tiny and slow, middle ones, near ones large, soft and faster (parallax).
  var layers = [
    { n: 34, size: [1.2, 3.2], speed: 5, alpha: [.25, .6], amber: .2 },
    { n: 14, size: [4, 8], speed: 9, alpha: [.12, .3], amber: .25 },
    { n: 6, size: [26, 52], speed: 15, alpha: [.05, .11], amber: .35 },
  ];
  function rnd(a, b) { return a + Math.random() * (b - a); }
  var motes = [];
  layers.forEach(function (l) {
    for (var i = 0; i < (calm ? Math.ceil(l.n / 3) : l.n); i++) {
      var dir = rnd(0, Math.PI * 2), v = l.speed * rnd(.5, 1);
      motes.push({ x: rnd(0, 1672), y: rnd(0, 941), s: rnd(l.size[0], l.size[1]), a: rnd(l.alpha[0], l.alpha[1]),
        vx: Math.cos(dir) * v, vy: Math.sin(dir) * v - l.speed * .3, img: Math.random() < l.amber ? amber : cyan,
        ph: rnd(0, 6.28), tw: rnd(.6, 1.6) });
    }
  });
  // Electric dust round the Omega: a dozen tiny blue sparks rising slowly from its base.
  for (var i = 0; i < (calm ? 4 : 12); i++)
    motes.push({ x: rnd(640, 1050), y: rnd(130, 520), s: rnd(1.5, 3), a: rnd(.35, .8), vx: rnd(-3, 3), vy: rnd(-6, -2),
      img: spark, ph: rnd(0, 6.28), tw: rnd(1.5, 3), spark: true });

  var scale = 1;
  function resize() {
    var r = stage.getBoundingClientRect(), dpr = Math.min(window.devicePixelRatio || 1, 1.5);
    dust.width = Math.round(r.width * dpr);
    dust.height = Math.round(r.height * dpr);
    scale = dust.width / 1672;
  }
  window.addEventListener("resize", resize);
  resize();

  function draw(dt, t) {
    var k = calm ? .3 : 1;
    g.clearRect(0, 0, dust.width, dust.height);
    g.globalCompositeOperation = "lighter";
    for (var i = 0; i < motes.length; i++) {
      var m = motes[i];
      m.x += m.vx * dt * k; m.y += m.vy * dt * k;
      if (m.spark) { if (m.y < 110 || m.x < 600 || m.x > 1090) { m.x = rnd(640, 1050); m.y = rnd(380, 540); } }
      else {
        if (m.x < -60) m.x += 1792; else if (m.x > 1732) m.x -= 1792;
        if (m.y < -60) m.y += 1061; else if (m.y > 1001) m.y -= 1061;
      }
      var s = m.s * 2 * scale;
      g.globalAlpha = m.a * (.65 + .35 * Math.sin(t * m.tw + m.ph));
      g.drawImage(m.img, m.x * scale - s / 2, m.y * scale - s / 2, s, s);
    }
    g.globalAlpha = 1;
  }

  function frame(now) {
    if (!running) return;
    var dt = Math.min((now - last) / 1000, .1);
    last = now;
    shown += (target - shown) * Math.min(1, dt * 4);
    if (ready && shown > .995) shown = 1;
    fill.style.width = (shown * 100).toFixed(2) + "%";
    bar.setAttribute("aria-valuenow", Math.round(shown * 100));
    draw(dt, now / 1000);
    if (ready && shown === 1 && !finishing) finish();
    requestAnimationFrame(frame);
  }
  requestAnimationFrame(frame);

  function finish() {
    finishing = true;
    bar.classList.add("full");                                            // the pulse, 0.45 s
    setTimeout(function () { loader.classList.add("gone"); }, 650);       // a 0.2 s hold, then the 0.65 s fade
    setTimeout(function () { running = false; loader.remove(); fullscreen.hidden = !canFullscreen; }, 1400);
  }

  // Warnings fade after 5 s; errors stay.
  function banner(msg, type) {
    var box = document.getElementById("unity-warning"), div = document.createElement("div");
    div.textContent = msg;
    div.style.cssText = "padding:10px;" + (type == "error" ? "background:#d33;color:#fff" : "background:#fd4");
    box.appendChild(div);
    if (type != "error") setTimeout(function () { div.remove(); }, 5000);
  }
  config.showBanner = banner;

  var fullscreen = document.getElementById("fullscreen-button"), page = document.documentElement;
  var enterFullscreen = page.requestFullscreen || page.webkitRequestFullscreen;
  var exitFullscreen = document.exitFullscreen || document.webkitExitFullscreen;
  var canFullscreen = enterFullscreen && exitFullscreen && (document.fullscreenEnabled || document.webkitFullscreenEnabled);
  function syncFullscreen() {
    var active = !!(document.fullscreenElement || document.webkitFullscreenElement);
    fullscreen.setAttribute("aria-pressed", String(active));
    fullscreen.title = active ? "Exit fullscreen" : "Enter fullscreen";
    fullscreen.setAttribute("aria-label", fullscreen.title);
  }
  document.addEventListener("fullscreenchange", syncFullscreen);
  document.addEventListener("webkitfullscreenchange", syncFullscreen);
  function fullscreenUnavailable() {
    syncFullscreen();
    banner("Fullscreen is unavailable here. Open the game in a browser tab and try again.", "warning");
  }
  fullscreen.addEventListener("click", function () {
    var active = document.fullscreenElement || document.webkitFullscreenElement;
    // Fullscreen the page so the exit button stays alongside the game's canvas.
    try {
      var request = active ? exitFullscreen.call(document) : enterFullscreen.call(page);
      Promise.resolve(request).then(syncFullscreen).catch(fullscreenUnavailable);
    } catch (error) { fullscreenUnavailable(); }
  });

  var script = document.createElement("script");
  script.src = loaderUrl;
  script.onload = function () {
    createUnityInstance(document.getElementById("unity-canvas"), config, function (p) {
      target = Math.max(target, Math.min(p, 1) * .96); // the last few percent are the module starting up
    }).then(function () { target = 1; ready = true; })
      .catch(function (message) { banner(String(message), "error"); });
  };
  document.body.appendChild(script);
})();
