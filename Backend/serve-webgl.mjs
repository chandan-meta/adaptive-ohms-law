import http from 'node:http';
import {createReadStream, statSync, readFileSync} from 'node:fs';
import path from 'node:path';
import {fileURLToPath} from 'node:url';
const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '../Builds/WebGL');
const port = 8080;
const types = {'.html':'text/html; charset=utf-8','.js':'application/javascript','.wasm':'application/wasm','.data':'application/octet-stream','.json':'application/json','.png':'image/png','.css':'text/css'};
http.createServer((req,res) => {
  // Only this locally served application may use the loopback backend proxy.
  if (req.headers.origin && req.headers.origin !== `http://localhost:${port}` && req.headers.origin !== `http://127.0.0.1:${port}`) {res.writeHead(403);res.end();return;}
  let url;
  try {url = new URL(req.url,'http://localhost');} catch {res.writeHead(400);res.end();return;}
  if (url.pathname === '/chat' || url.pathname.startsWith('/sources/')) {
    const proxy = http.request({hostname:'127.0.0.1',port:8787,path:url.pathname,method:req.method,headers:{'Content-Type':'application/json'}}, upstream => {res.writeHead(upstream.statusCode,upstream.headers);upstream.pipe(res);});
    proxy.on('error',()=>{res.writeHead(503,{'Content-Type':'application/json'});res.end('{"error":"service_not_configured"}');});
    req.pipe(proxy);return;
  }
  if(req.method !== 'GET' && req.method !== 'HEAD') {res.writeHead(405);res.end();return;}
  let file;
  try {file=path.resolve(root,'.'+decodeURIComponent(url.pathname === '/' ? '/index.html' : url.pathname));}catch{res.writeHead(400);res.end();return;}
  if(!file.startsWith(root+path.sep)) {res.writeHead(403);res.end();return;}
  try {if(!statSync(file).isFile())throw new Error();}catch {res.writeHead(404);res.end('Build file not found');return;}
  res.writeHead(200,{'Content-Type':types[path.extname(file)]||'application/octet-stream','Cache-Control':'no-cache'});
  if(req.method==='HEAD')res.end();
  else if(path.basename(file)==='index.html') {
    let html=readFileSync(file,'utf8');
    html=html.replace('</head>', '<style>body{margin:0;background:#26312e}#unity-container.unity-desktop{position:fixed;inset:0;transform:none;width:100%;height:100%}#unity-canvas{width:100%!important;height:100%!important;display:block}#unity-footer{display:none}#lesson-start{position:fixed;inset:0;z-index:999;display:grid;place-content:center;text-align:center;background:#26312e;color:#f7f3ea;font:20px system-ui}#lesson-start button{padding:20px 40px;background:#f2b544;color:#26312e;border:0;border-radius:12px;font:bold 22px system-ui;cursor:pointer}</style></head>');
    html=html.replace('document.body.appendChild(script);', `
      config.autoSyncPersistentDataPath=true;
      var gate=document.createElement('div');gate.id='lesson-start';
      gate.innerHTML='<h1>Ohm’s Law</h1><p>Investigate electricity with Wattson</p><button>Start lesson</button><p>Mouse or touch • sound recommended</p>';
      document.body.appendChild(gate);
      gate.querySelector('button').onclick=function(){
        if(window.speechSynthesis){var unlock=new SpeechSynthesisUtterance('');window.speechSynthesis.speak(unlock);}
        gate.remove();document.body.appendChild(script);
      };
    `);
    res.end(html);
  } else createReadStream(file).pipe(res);
}).listen(port,'127.0.0.1',()=>console.log(`Ohm's Law: http://localhost:${port}`));
