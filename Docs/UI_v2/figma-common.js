const created=[];
const track=n=>{created.push(n.id);return n;};
await Promise.all([{family:'Inter',style:'Regular'},{family:'Inter',style:'Semi Bold'},{family:'Space Grotesk',style:'Bold'}].map(f=>figma.loadFontAsync(f)));
const vars=Object.fromEntries((await figma.variables.getLocalVariablesAsync()).map(v=>[v.name,v]));
const styles=Object.fromEntries((await figma.getLocalTextStylesAsync()).map(s=>[s.name.replace('OHM/',''),s]));
const effects=await figma.getLocalEffectStylesAsync();
const rgb=s=>({r:parseInt(s.slice(1,3),16)/255,g:parseInt(s.slice(3,5),16)/255,b:parseInt(s.slice(5,7),16)/255});
function paint(key,opacity=1){const bound=figma.variables.setBoundVariableForPaint({type:'SOLID',color:{r:0,g:0,b:0}},'color',vars[key]);return {...bound,opacity};}
function color(n,key,opacity=1){n.fills=[paint(key,opacity)];}
function edge(n,key='color/border',weight=1){n.strokes=[paint(key)];n.strokeWeight=weight;n.strokeAlign='INSIDE';}
function radius(n,size=16){for(const k of ['topLeftRadius','topRightRadius','bottomLeftRadius','bottomRightRadius'])n.setBoundVariable(k,vars['radius/'+size]);}
function pad(n,size=24){for(const k of ['paddingLeft','paddingRight','paddingTop','paddingBottom']){if(size===0)n[k]=0;else n.setBoundVariable(k,vars['space/'+size]);}}
function gap(n,size=16){n.setBoundVariable('itemSpacing',vars['space/'+size]);}
function txt(parent,name,chars,style='Body',width=0,key='color/text'){const n=track(figma.createText());n.name=name;const st=styles[style];n.fontName=st.fontName;n.textStyleId=st.id;n.characters=chars;color(n,key);parent.appendChild(n);if(width){n.textAutoResize='HEIGHT';n.resize(width,n.height);}else n.textAutoResize='WIDTH_AND_HEIGHT';return n;}
function box(parent,name,w,h=100,fill='color/surface',direction='VERTICAL',p=24){const n=track(figma.createAutoLayout(direction));n.name=name;color(n,fill);n.resize(w,h);n.layoutSizingHorizontal='FIXED';n.layoutSizingVertical='HUG';gap(n);pad(n,p);radius(n);if(parent)parent.appendChild(n);return n;}
function fixed(n,w,h){n.resize(w,h);n.layoutSizingHorizontal='FIXED';n.layoutSizingVertical='FIXED';return n;}
function at(n,x,y){n.x=x;n.y=y;return n;}
function prop(comp,textNode,label){const k=comp.addComponentProperty(label,'TEXT',textNode.characters);textNode.componentPropertyReferences={characters:k};return k;}
function comp(name,w,h=100,direction='VERTICAL',p=24){const n=track(figma.createComponent());n.name=name;n.layoutMode=direction;color(n,'color/surface');fixed(n,w,h);gap(n);pad(n,p);radius(n);edge(n);n.description='OhmsLaw UI v2. Editable uGUI presentation. Runtime bindings documented in Docs/UI_v2; sample values are not simulation state.';return n;}
function imageNode(parent,name,hash,w,h){const n=track(figma.createRectangle());n.name=name;n.resize(w,h);n.fills=[{type:'IMAGE',imageHash:hash,scaleMode:'FILL'}];parent.appendChild(n);return n;}
function idsFor(node){return[node.id,...('findAll' in node?node.findAll(()=>true).map(n=>n.id):[])];}
function evidence(node){const all=node.findAll(()=>true);const counts={};for(const n of all)counts[n.type]=(counts[n.type]||0)+1;return{id:node.id,name:node.name,width:node.width,height:node.height,counts,imageNodes:all.filter(n=>'fills'in n&&Array.isArray(n.fills)&&n.fills.some(p=>p.type==='IMAGE')).map(n=>({id:n.id,name:n.name,type:n.type,width:n.width,height:n.height})),fonts:[...new Set(all.filter(n=>n.type==='TEXT').map(n=>n.fontName.family))]};}
