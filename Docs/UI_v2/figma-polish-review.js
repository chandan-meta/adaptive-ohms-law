const p=await figma.getNodeByIdAsync('2:6');await figma.setCurrentPageAsync(p);
const before=await figma.getNodeByIdAsync(BEFORE_ID),source=await figma.getNodeByIdAsync('5:2489');
const review=box(p,'Reference Match — Before / After / Target',5700,100,'color/bg','VERTICAL',48);at(review,2300,1500);gap(review,32);
txt(review,'Title','REFERENCE MATCH — BEFORE / AFTER / TARGET','Title');
txt(review,'Scope','Same gameplay asset and content • corrected live component instances • editable gradients / halos / reflections • target styling reference','Body',5500,'color/text-muted');
const row=box(review,'Full-screen comparison',5500,100,'color/bg','HORIZONTAL',0);gap(row,48);
const beforeCol=box(row,'Before column',1920,100,'color/bg','VERTICAL',0);txt(beforeCol,'Heading','BEFORE / flat styling','Heading');beforeCol.appendChild(before);before.x=before.y=0;
const afterCol=box(row,'After column',1920,100,'color/bg','VERTICAL',0);txt(afterCol,'Heading','AFTER / luminous acrylic','Heading');const after=track(source.clone());afterCol.appendChild(after);after.name='After / live linked experiment components';
const targetCol=box(row,'Target column',1491,100,'color/bg','VERTICAL',0);txt(targetCol,'Heading','TARGET / visual finish, not curriculum data','Heading',1491);
imageNode(targetCol,'Target / supplied reference sheet','646d5ff5f190f05e645e63b3cb8b20d3847ab698',1491,1055);
txt(review,'Method','Native editable gradient paints + narrow cyan shadow + wider blue shadow + clipped reflection + inner depth. No Bloom shader was available; no Glass/background-blur stack or body-text blur was used.','Body',5500);
const comparisons={};
function crop(parent,name,[x,y,w,h],width){const f=track(figma.createFrame());f.name=name;f.resize(width,h*width/w);f.fills=[];f.clipsContent=true;parent.appendChild(f);const r=imageNode(f,'Target crop / source sheet','646d5ff5f190f05e645e63b3cb8b20d3847ab698',1491*width/w,1055*width/w);r.x=-x*width/w;r.y=-y*width/w;return f;}
function clone(parent,node,width){const n=track(node.clone());parent.appendChild(n);if(n.width!==width)n.rescale(width/n.width);return n;}
const beforeButton=before.children.find(n=>n.name==='Button/Primary');const afterButton=source.children.find(n=>n.name==='Button/Primary');
const beforePanel=before.children.find(n=>n.name==='Panel/Objective');const afterPanel=source.children.find(n=>n.name==='Panel/Objective');
const beforeMeasure=before.findAllWithCriteria({types:['FRAME']}).find(n=>n.children.some(n=>n.type==='TEXT'&&n.name==='Value'&&n.characters==='3.00 V'));
const afterMeasure=source.findAllWithCriteria({types:['INSTANCE']}).find(n=>n.findAllWithCriteria({types:['TEXT']}).some(n=>n.name==='Value'&&n.characters==='3.00 V'));
const beforeDialogue=before.children.find(n=>n.name==='Panel/WattsonDialogue');const afterDialogue=source.children.find(n=>n.name==='Panel/WattsonDialogue');
const specs=[['Button',beforeButton,afterButton,[1153,151,82,34],200],['Objective panel',beforePanel,afterPanel,[1084,437,184,161],340],['Measurement',beforeMeasure,afterMeasure,[1286,438,186,48],240],['Dialogue',beforeDialogue,afterDialogue,[359,505,563,83],1180]];
let y=3000;
for(const[name,b,a,rect,width]of specs){const vertical=name==='Dialogue';const frame=box(p,'Comparison / '+name,vertical?1280:Math.max(1120,3*(width+32)+128),100,'color/bg','VERTICAL',48);at(frame,2300,y);txt(frame,'Title',name+' / equal-width finish comparison','Heading');const r=box(frame,'Before / after / target',frame.width-96,100,'color/bg',vertical?'VERTICAL':'HORIZONTAL',0);gap(r,32);for(const[label,node]of [['Before',b],['After',a],['Target crop',null]]){const cell=box(r,label,width,100,'color/bg','VERTICAL',0);txt(cell,'Label',label,'Label',width,'color/text-muted');if(node)clone(cell,node,width);else crop(cell,'Target / '+name,rect,width);}comparisons[name]=frame.id;y+=frame.height+100;}
return{createdNodeIds:[...new Set([...created,...review.findAll(()=>true).map(n=>n.id),...Object.values(comparisons).flatMap(id=>figma.getNodeById(id).findAll(()=>true).map(n=>n.id))])],mutatedNodeIds:[before.id],reviewId:review.id,afterId:after.id,comparisons};
