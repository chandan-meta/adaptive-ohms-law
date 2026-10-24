const made=[];const track=n=>{made.push(n.id);return n;};
await Promise.all([{family:'Baloo 2',style:'Bold'},{family:'Inter',style:'Regular'},{family:'Inter',style:'Semi Bold'}].map(f=>figma.loadFontAsync(f)));
const vars=Object.fromEntries((await figma.variables.getLocalVariablesAsync()).map(v=>[v.name,v]));
const styles=Object.fromEntries((await figma.getLocalTextStylesAsync()).filter(s=>s.name.startsWith('ED/')).map(s=>[s.name.slice(3),s]));
const shadow=(await figma.getLocalEffectStylesAsync()).find(s=>s.name==='ED / Elevation');
function paint(k){return figma.variables.setBoundVariableForPaint({type:'SOLID',color:{r:0,g:0,b:0}},'color',vars[k]);}
function fill(n,k){n.fills=[paint(k)];}function edge(n,k='line',w=1){n.strokes=[paint(k)];n.strokeWeight=w;n.strokeAlign='INSIDE';}
function rad(n,v=12){n.setBoundVariable('cornerRadius',vars['radius/'+v]);}
function padding(n,v){for(const k of ['paddingTop','paddingBottom','paddingLeft','paddingRight']){if(v)n.setBoundVariable(k,vars['space/'+v]);else n[k]=0;}}
function text(p,s,style='Body',color='white',w=0){const n=track(figma.createText());n.name=s.slice(0,70);n.fontName=styles[style].fontName;n.textStyleId=styles[style].id;n.characters=s;fill(n,color);p.appendChild(n);n.textAutoResize='HEIGHT';if(w)n.resize(w,n.height);return n;}
function box(p,name,w,h,fillKey='surface',dir='VERTICAL',pad=24){const n=track(figma.createAutoLayout(dir));n.name=name;n.resize(w,h);n.layoutSizingHorizontal='FIXED';n.layoutSizingVertical='FIXED';n.setBoundVariable('itemSpacing',vars['space/16']);padding(n,pad);rad(n);fillKey?fill(n,fillKey):n.fills=[];if(p)p.appendChild(n);return n;}
function comp(name,w,h,color='surface',dir='VERTICAL',pad=24){const n=track(figma.createComponent());n.name=name;n.layoutMode=dir;n.resize(w,h);n.primaryAxisSizingMode='FIXED';n.counterAxisSizingMode='FIXED';n.itemSpacing=16;padding(n,pad);rad(n);fill(n,color);edge(n,color==='paper'?'brass':'line');n.effectStyleId=shadow.id;n.description='Electric Detective · editable reusable UI. Design preview only; runtime integration requires approval.';return n;}
function prop(c,t,name='Label'){const k=c.addComponentProperty(name,'TEXT',t.characters);t.componentPropertyReferences={characters:k};return k;}
function place(n,p,x,y){p.appendChild(n);if(p.layoutMode&&p.layoutMode!=='NONE')n.layoutPositioning='ABSOLUTE';n.x=x;n.y=y;return n;}
function rect(p,name,x,y,w,h,color,r=0){const n=track(figma.createRectangle());n.name=name;n.resize(w,h);fill(n,color);if(r)n.cornerRadius=r;place(n,p,x,y);return n;}
function dot(p,x,y,size,color){const n=track(figma.createEllipse());n.resize(size,size);fill(n,color);place(n,p,x,y);return n;}
function board(p,name,x,y,w,h,color='navy'){const n=track(figma.createFrame());n.name=name;n.resize(w,h);fill(n,color);p.appendChild(n);n.x=x;n.y=y;n.clipsContent=true;return n;}
function row(p,w,h=64){return box(p,'Row',w,h,null,'HORIZONTAL',0);}
function atText(p,s,x,y,style='Body',color='white',w=0){return place(text(p,s,style,color,w),p,x,y);}
function evidence(n){const all=n.findAll(()=>true);return{id:n.id,name:n.name,w:n.width,h:n.height,counts:all.reduce((a,n)=>(a[n.type]=(a[n.type]||0)+1,a),{}),fonts:[...new Set(all.filter(n=>n.type==='TEXT').map(n=>n.fontName.family))]};}
function instance(c,p,label){const n=track(c.createInstance());p.appendChild(n);if(label){const key=Object.keys(n.componentProperties).find(k=>k.startsWith('Label#'));if(key)n.setProperties({[key]:label});}return n;}
function finish(extra={}){return{createdNodeIds:made,...extra};}
