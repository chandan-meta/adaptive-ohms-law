// Composes editable UI from existing OHM components; never imports a complete screenshot.
await figma.setCurrentPageAsync(await figma.getNodeByIdAsync('65:2'));
await Promise.all([{family:'Inter',style:'Regular'},{family:'Inter',style:'Semi Bold'},{family:'Space Grotesk',style:'Bold'}].map(f=>figma.loadFontAsync(f)));
const created=[],mutated=[],links=[];
const track=n=>{created.push(n.id);return n;};
const vars=Object.fromEntries((await figma.variables.getLocalVariablesAsync()).map(v=>[v.name,v]));
const texts=Object.fromEntries((await figma.getLocalTextStylesAsync()).map(s=>[s.name.replace('OHM/',''),s]));
const paints=Object.fromEntries((await figma.getLocalPaintStylesAsync()).map(s=>[s.name.replace('OHM/FX/',''),s]));
const effects=Object.fromEntries((await figma.getLocalEffectStylesAsync()).map(s=>[s.name.replace('OHM/FX/',''),s]));
const mains={};
for(const id of ['3:277','3:327','3:345','3:348','3:351','3:359','3:373','3:405','3:436','3:590','3:196','3:211','3:204','3:440','3:460','3:464','3:468'])mains[id]=await figma.getNodeByIdAsync(id);
const frames=FRAME_IDS;
const paint=(key,opacity=1)=>({...figma.variables.setBoundVariableForPaint({type:'SOLID',color:{r:0,g:0,b:0}},'color',vars[key]),opacity});
function pos(n,x,y){n.x=x;n.y=y;return n;}
function text(parent,name,value,style='Body',width=0,key='color/text'){
 const n=track(figma.createText()),s=texts[style];n.name=name;n.fontName=s.fontName;n.textStyleId=s.id;n.characters=value;n.fills=[paint(key)];parent.appendChild(n);
 if(width){n.textAutoResize='HEIGHT';n.resize(width,n.height);}else n.textAutoResize='WIDTH_AND_HEIGHT';return n;
}
function panel(parent,name,w,h,direction='VERTICAL',padding=24){
 const n=track(figma.createAutoLayout(direction));parent.appendChild(n);n.name=name;n.resize(w,h);n.layoutSizingHorizontal='FIXED';n.layoutSizingVertical='FIXED';
 n.setBoundVariable('itemSpacing',vars['space/16']);for(const p of ['paddingLeft','paddingTop','paddingRight','paddingBottom'])n.setBoundVariable(p,vars['space/'+padding]);
 for(const p of ['topLeftRadius','topRightRadius','bottomLeftRadius','bottomRightRadius'])n.setBoundVariable(p,vars['radius/16']);
 n.fillStyleId=paints['Navy acrylic'].id;n.effectStyleId=effects['Panel halo'].id;n.strokes=[paint('color/accent',.8)];n.strokeWeight=1.25;n.clipsContent=false;return n;
}
function instance(parent,id,name,x=0,y=0,scale=1,overrides={}){
 const n=track(mains[id].createInstance());parent.appendChild(n);n.name=name;if(scale!==1)n.rescale(scale);
 for(const t of n.findAllWithCriteria({types:['TEXT']})){if(Object.hasOwn(overrides,t.name))t.characters=overrides[t.name];}
 pos(n,x,y);return n;
}
function button(parent,label,primary=false,w=200,target=null){const n=instance(parent,primary?'3:196':'3:211',label,0,0,1,{Label:label});n.resize(w,56);if(target)links.push([n,target]);return n;}
function rect(parent,name,w,h,key,opacity=1){const n=track(figma.createRectangle());parent.appendChild(n);n.name=name;n.resize(w,h);n.fills=[paint(key,opacity)];return n;}
function circle(parent,name,x,y,size,key){const n=track(figma.createEllipse());parent.appendChild(n);n.name=name;n.resize(size,size);n.fills=[paint(key)];return pos(n,x,y);}
function vector(parent,name,points,key,width=2){const n=track(figma.createVector());parent.appendChild(n);n.name=name;n.vectorPaths=[{windingRule:'NONE',data:points.map((p,i)=>(i?'L ':'M ')+p[0]+' '+p[1]).join(' ')}];n.fills=[];n.strokes=[paint(key)];n.strokeWeight=width;return n;}
function headingCard(parent,title,body,x,y,w=480,h=220,eyebrow='YOUR TURN'){
 const p=pos(panel(parent,title,w,h),x,y);text(p,'Eyebrow',eyebrow,'Micro',w-48,'color/accent');text(p,'Title',title,'Heading',w-48);if(body)text(p,'Body',body,'Body',w-48);return p;
}
function caption(parent,value){const n=instance(parent,'3:327','Wattson dialogue',48,896,1,{Caption:value});n.resize(1248,136);const speech=n.findOne(n=>n.name==='Speech');if(speech)speech.resize(1060,88);const t=n.findOne(n=>n.type==='TEXT'&&n.name==='Caption');if(t)t.resize(1060,t.height);return n;}
function help(parent,target=null){const p=pos(panel(parent,'Ask Wattson',144,120,'VERTICAL',16),1728,912);p.counterAxisAlignItems='CENTER';
 const face=track(figma.createEllipse());p.appendChild(face);face.name='Wattson portrait';face.resize(56,56);face.fills=[{type:'IMAGE',imageHash:'c19228980982cb924feb4bb5da183c83a852a1cd',scaleMode:'FILL'}];
 text(p,'Label','Ask Wattson','Micro');if(target)links.push([p,target]);return p;}
function base(f,chapter,title,world='lab'){
 f.fills=[paint('color/bg')];mutated.push(f.id);
 const bg=track(figma.createRectangle());f.appendChild(bg);bg.name='Scene context / UI-free project preview';bg.resize(1920,1080);bg.fills=[{type:'IMAGE',imageHash:world==='wire'?'6d3aefb8145394ffa6791b89705b6b452fc46382':'08ffb09ff3ac923b6d72a7d9d02975ce9f08fa1c',scaleMode:'FILL'}];bg.locked=true;
 rect(f,'Subtle scene tint',1920,1080,'color/bg',.08);
 instance(f,'3:277','Lesson HUD',48,32,1,{Title:'Ohm’s Law · A Wattson investigation',Step:'Chapter '+chapter+' / 10 · '+title,'Progress label':'Explore • measure • think • discover'});
}
function measures(f,V='5.0 V',I='1.00 A',R=null,x=448,y=160){const p=pos(panel(f,'Live measurements',R?752:496,116,'HORIZONTAL',16),x,y);p.fills=[];p.strokes=[];p.effects=[];p.paddingLeft=p.paddingRight=p.paddingTop=p.paddingBottom=0;
 instance(p,'3:345','Voltage',0,0,1,{Value:V});instance(p,'3:348','Current',0,0,1,{Value:I});if(R)instance(p,'3:351','Resistance',0,0,1,{Value:R});return p;}
function observations(f,x=176,y=336,ratio=true,rows=5,highlight=false){const n=instance(f,'3:373','Observation table',x,y);
 if(!ratio){const hd=n.findOne(n=>n.name==='V / I (Ω)'&&n.type==='TEXT');if(hd)hd.characters='';}
 const rs=n.children.filter(n=>n.name.startsWith('Row '));rs.forEach((r,i)=>{const cs=r.findAllWithCriteria({types:['TEXT']});if(!ratio&&cs[3])cs[3].characters='';if(highlight&&i<2){r.fills=[paint('color/accent',.16)];r.strokes=[paint('color/accent',.65)];r.strokeWeight=1;r.cornerRadius=6;}if(i>=rows)r.visible=false;});return n;}
function graph(f,x=828,y=336,selected=false,points=5,line=true,scale=1){const n=instance(f,'3:405','Voltage against current · editable plot',x,y,scale,{'Data provenance':'Nichrome wire · constant temperature'});
 const ps=n.findAll(n=>n.type==='ELLIPSE'&&n.name.startsWith('Measured sample point'));ps.forEach((p,i)=>{p.visible=i<points;if(selected&&i<2){p.strokes=[paint('color/accent')];p.strokeWeight=3;}});
 for(const p of n.findAll(n=>n.type==='VECTOR'&&n.name==='Vector'))p.visible=line;return n;}
function quiz(f,resistance=false,retry=false){
 const q=resistance?'Same voltage. If the resistance increases, what happens to the current?':'If voltage increases while resistance stays constant, what happens to current?';
 headingCard(f,retry?'Try again.':'Detective challenge.',retry?'One option is out. Pick again.':'Tap an answer or press 1, 2 or 3.',1336,160,536,176);
 const p=pos(panel(f,'Detective challenge',536,retry?636:516),1336,360);text(p,'Eyebrow','DETECTIVE CHALLENGE','Micro',488,'color/accent');text(p,'Question',q,'Heading',488);
 ['It increases','It decreases','It stays the same'].forEach((label,i)=>{const bad=retry&&i===(resistance?2:1);const n=instance(p,bad?'3:468':'3:440',bad?'Eliminated answer':'Answer '+String.fromCharCode(65+i),0,0,1,{Letter:bad?'×':String.fromCharCode(65+i),Answer:label});n.resize(488,76);const t=n.findOne(n=>n.type==='TEXT'&&n.name==='Answer');t.resize(370,t.height);if(!bad)links.push([n,frames[resistance?(i===1?16:15):(i===0?6:5)]]);});
 if(retry){text(p,'Targeted hint',resistance?'Same push, but nichrome lets less current through. More resistance means less current.':'Same wire, same temperature: resistance stays put. Voltage is the push. Would a bigger push give less flow?','Caption',488,'color/accent');}
 help(f,frames[resistance?15:5]);return p;
}
function lawCard(f,forms=false,demo=false){
 const n=instance(f,'3:436','Ohm’s Law',448,320);n.resize(1024,336);n.paddingLeft=n.paddingRight=48;n.itemSpacing=24;
 for(const t of n.findAllWithCriteria({types:['TEXT']})){t.resize(928,t.height);if(t.name==='Equation'){t.fontSize=96;t.lineHeight={unit:'PIXELS',value:112};}}
 if(forms){const row=pos(panel(f,'Equivalent forms',928,80,'HORIZONTAL',16),496,680);text(row,'Current form','I = V ÷ R','Title',420,'color/measurement-current');text(row,'Resistance form','R = V ÷ I','Title',420,'color/measurement-resistance');}
 if(demo){const p=pos(panel(f,'Worked example',928,104,'HORIZONTAL',16),496,776);text(p,'Worked example','4.0 V = 0.80 A × 5 Ω','Heading',560);text(p,'Fixed resistance','R stays 5 Ω','Label',300,'color/accent');}return n;
}
function clues(f,build=false){const p=pos(panel(f,'The three clues',1024,416),448,336);text(p,'Eyebrow','THE THREE CLUES','Micro',976,'color/accent');const row=panel(p,'Clue cards',976,240,'HORIZONTAL',16);row.fills=[];row.strokes=[];row.effects=[];
 const items=build?[['R','Resistance','The opposition · ohm (Ω)','color/measurement-resistance'],['V','Potential difference','The push · volt (V)','color/measurement-voltage'],['I','Current','The flow · ampere (A)','color/measurement-current']]:[['V','Potential difference','The push · volt (V)','color/measurement-voltage'],['I','Current','The flow · ampere (A)','color/measurement-current'],['R','Resistance','The opposition · ohm (Ω)','color/measurement-resistance']];
 for(const [symbol,label,unit,key]of items){const c=panel(row,label,304,208,'VERTICAL',16);c.fillStyleId=paints['Secondary glass'].id;c.effectStyleId=effects['Secondary halo'].id;c.strokes=[paint(key)];text(c,'Symbol',symbol,'Display',272,key);text(c,'Quantity',label,'Label',272);text(c,'Unit',unit,'Micro',272);if(build)links.push([c,frames[symbol==='V'?20:19]]);}
 if(build)text(p,'Instruction','Start with V, the push.','Label',976,'color/accent');return p;}
function comparison(f,both=false){const p=pos(panel(f,'Copper vs nichrome',1024,568),112,224);const row=panel(p,'Comparison header',976,56,'HORIZONTAL',16);row.fills=[];row.strokes=[];row.effects=[];text(row,'Title','Copper vs nichrome','Heading',680);button(row,'Close',false,200,frames[16]);
 const viewport=panel(p,'Particle model',976,360,'VERTICAL',16);viewport.fillStyleId=paints['Navy solid fallback'].id;viewport.strokes=[];viewport.effects=[];
 for(let r=0;r<(both?2:1);r++){const name=r?'NICHROME':'COPPER';const strip=panel(viewport,name,928,both?148:260,'VERTICAL',16);strip.fills=[];strip.effects=[];strip.strokes=[];text(strip,'Material label',name,'Panel',880,r?'color/text-muted':'color/measurement-resistance');
 const atoms=track(figma.createFrame());strip.appendChild(atoms);atoms.name='Editable representative atoms and charges';atoms.resize(880,both?72:140);atoms.fills=[paint('color/raised')];atoms.cornerRadius=24;atoms.clipsContent=true;
 const count=r?36:18;for(let j=0;j<count;j++)circle(atoms,'Atom '+(j+1),20+(j*73)%820,12+(j*17)%(both?36:100),r?18:14,'color/measurement-resistance');
 for(let j=0;j<8;j++)circle(atoms,'Representative electron '+(j+1),40+j*100,22+(j%2)*16,10,'color/accent');}
 text(p,'Controlled conditions','Same voltage · same length · same thickness · same temperature','Caption',976,'color/text-muted');return p;}
const built=[];
for(let i=START;i<END;i++){
 const f=await figma.getNodeByIdAsync(frames[i]);if(f.children.length){built.push({id:f.id,name:f.name,skipped:true});continue;}
 const chapter=i<2?1:i<4?2:i<7?3:i<9?6:i<12?7:i<14?8:i<18?9:10;
 base(f,chapter,f.name.split(' / ')[1],i>=2&&i<=6?'wire':'lab');
 if(i===0){const p=pos(panel(f,'Welcome',720,688,'VERTICAL',48),96,224);text(p,'Eyebrow','A WATTSON INVESTIGATION','Label',624,'color/accent');text(p,'Title','OHM’S LAW','Display',624);text(p,'Subtitle','A journey of curiosity, current and discovery.','Title',624);button(p,'Start journey',true,304,frames[1]);text(p,'Values','EXPLORE · EXPERIMENT · DISCOVER','Label',624,'color/accent');text(p,'Duration','About 10 minutes · Captions on · Sound on','Caption',624);}
 if(i===1){headingCard(f,'Tap the glowing wire.','Find out what happens inside it.',1392,160);const target=circle(f,'Wire interaction target',872,480,96,'color/accent');links.push([target,frames[2]]);help(f);}
 if(i===2){headingCard(f,'Apply the voltage.','Tap the + terminal to connect the push.',1392,160);headingCard(f,'Voltage (V)','The difference in electric potential between two points. Applying it across the wire creates the push on the electrons. Unit: volt (V).',1392,408,480,292,'CLUE');const target=circle(f,'Positive terminal interaction target',1136,480,88,'color/accent');links.push([target,frames[3]]);help(f);}
 if(i===3){headingCard(f,'Current (I)','Charge passing a section each second: I = ΔQ ÷ Δt. Unit: ampere (A).',1392,160,480,252,'CLUE');headingCard(f,'Charge through the section','2 representative charges\nLast second: 3 charges\n\nCharge ÷ time = current (I).',1392,440,480,296,'OBSERVE');caption(f,'This much charge, from negative to positive, through this area, every second. That rate of flow is the current, written I.');help(f);}
 if(i===4||i===5)quiz(f,false,i===5);
 if(i===6){headingCard(f,'Correct · More current','At constant resistance, increasing voltage increases the current.',1392,160,480,244,'DETECTIVE CHALLENGE');caption(f,'More current, you say.');help(f);}
 if(i===7||i===8){instance(f,'3:590','Circuit connection guide',192,208,1.6);headingCard(f,i===7?'Tap the ammeter.':'Circuit complete.',i===7?'The meter marked A joins the loop in series, in line with the wire.':'Both meters are connected. The circuit is ready.',1392,160,480,244,i===7?'YOUR TURN · STEP 1 OF 2':'DONE');headingCard(f,i===7?'Ammeter · in series':'Voltmeter · in parallel',i===7?'Measures the current I through XY, in amperes (A).':'Measures the voltage V across X and Y, in volts (V).',1392,432,480,240,'CLUE');if(i===8)caption(f,'That’s why a voltmeter is connected in parallel across the nichrome wire.');help(f);}
 if(i===9){measures(f,'2.0 V','0.40 A',null,448,160);headingCard(f,'Set the battery to 3 V.','Slide the battery, then watch the meters.',1336,160,536,200,'YOUR TURN · 3 OF 5');const v=instance(f,'3:359','Set the battery',112,656);v.findOne(n=>n.type==='TEXT'&&n.name==='Note').characters='Current is measured, never set independently.';for(const b of v.findAllWithCriteria({types:['INSTANCE']}).filter(b=>b.findOne(t=>t.type==='TEXT'&&t.name==='Label'))){const t=b.findOne(t=>t.type==='TEXT'&&t.name==='Label');const value=t.characters,w=b.width,h=b.height;b.swapComponent(mains[value==='2 V'?'3:204':'3:211']);b.resize(w,h);b.findOne(t=>t.type==='TEXT'&&t.name==='Label').characters=value;}observations(f,1248,376,false,2);graph(f,1248,700,false,2,false,.78);pos(help(f),1040,912);}
 if(i>=10&&i<=13){measures(f);if(i===11){observations(f,1248,312,false,5,true);graph(f,1312,640,true,5,false,.74);}else{observations(f,192,344,false,5,i!==12);graph(f,864,344,i===10||i===13,5,i>=12);}if(i===11){const p=pos(panel(f,'Proportional relationship',1000,244),112,388);text(p,'Equation','V × 2 → I × 2','Display',952,'color/accent');text(p,'Explanation','Double the voltage, double the current.','Body',952);}
 caption(f,i===10?'Compare just these two.':i===11?'What happened to the current when the potential difference doubled? It also doubled.':i===12?'Voltage up the side, current along the bottom.':'Look at the pattern! Double the voltage, and the current doubles too. They change together in the same proportion.');help(f);}
 if(i===14||i===15){comparison(f,i===15);if(i===14)caption(f,'Two wires: same length, same thickness, same temperature, same voltage. Only the metal is different.');else quiz(f,true,true);if(i===14)help(f);}
 if(i===16){observations(f,624,384,true);caption(f,'For this wire, V divided by I is 5 V/A for every reading.');help(f);}
 if(i===17){const g=graph(f,608,264,false);const plot=g.findOne(n=>n.type==='FRAME'&&n.name.startsWith('Editable plot'));if(plot){const overlay=track(figma.createFrame());f.appendChild(overlay);overlay.name='Editable slope triangle annotation';overlay.resize(plot.width,plot.height);overlay.fills=[];pos(overlay,plot.absoluteBoundingBox.x-f.absoluteBoundingBox.x,plot.absoluteBoundingBox.y-f.absoluteBoundingBox.y);vector(overlay,'Rise ΔV',[[140,190],[340,190],[340,110]],'color/measurement-resistance',3);const t=text(overlay,'Rise and run','ΔV = 2.0 V · ΔI = 0.40 A','Micro',360,'color/accent');pos(t,170,205);}const law=instance(f,'3:436','Slope gives resistance',608,768);law.resize(700,100);for(const t of law.findAllWithCriteria({types:['TEXT']}))if(t.name!=='Equation')t.visible=false;const eq=law.findOne(n=>n.type==='TEXT'&&n.name==='Equation');eq.fontSize=44;eq.lineHeight={unit:'PIXELS',value:52};caption(f,'Every straight line has a slope.');help(f);}
 if(i===18||i===19){measures(f,'5.0 V','1.00 A','5.0 Ω',448,160);clues(f,i===19);if(i===18)caption(f,'And the opposition.');help(f);}
 if(i>=20&&i<=22){measures(f,i===22?'4.0 V':'5.0 V',i===22?'0.80 A':'1.00 A','5.0 Ω',448,160);lawCard(f,i>=21,i===22);caption(f,i===20?'For an ohmic conductor at a constant temperature, V equals I times R.':i===21?'Turn it around: I equals V over R, and R equals V over I.':'Keep R the same and raise the push: the current rises in step.');help(f);}
 if(i===23){const p=pos(panel(f,'Case closed',600,848),1272,160);text(p,'Title','CASE CLOSED','Display',552,'color/accent');text(p,'Eyebrow','WHAT YOU FOUND','Label',552);text(p,'Clues','V · Potential difference · the push\nI · Current · the flow\nR · Resistance · the opposition','Body',552);text(p,'Law','V = I × R','Display',552);text(p,'Example','Your nichrome wire: 5.0 V ÷ 1.00 A = 5.0 Ω','Body',552);button(p,'Explore now',true,552,frames[24]);button(p,'Replay investigation',false,552,frames[0]);const row=panel(p,'Navigation',552,56,'HORIZONTAL',16);row.fills=[];row.effects=[];row.strokes=[];button(row,'Chapters',false,240,frames[0]);button(row,'Exit',false,240,frames[0]);}
 if(i===24){
 const p=pos(panel(f,'Free experiment',752,880),1120,160);p.setBoundVariable('itemSpacing',vars['space/12']);
 text(p,'Title','Ohm’s Law Lab','Heading',704);text(p,'Live values','V = 1.5 V     I = 0.15 A     R = 10 Ω','Label',704,'color/accent');
 const ctrl=panel(p,'Continuous battery voltage control',704,132,'VERTICAL',16);ctrl.setBoundVariable('itemSpacing',vars['space/8']);text(ctrl,'Title','BATTERY VOLTAGE · 1.5 V','Label',672,'color/measurement-voltage');
 const rail=track(figma.createFrame());ctrl.appendChild(rail);rail.name='Slider · 0–5 V';rail.resize(672,16);rail.fills=[];pos(rect(rail,'Track',672,6,'color/border'),0,5);pos(rect(rail,'1.5 V fill',201.6,6,'color/measurement-voltage'),0,5);circle(rail,'Slider thumb · 1.5 V',193.6,0,16,'color/text-strong');text(ctrl,'Ticks','0 V          1 V          2 V          3 V          4 V          5 V','Micro',672);
 const row=panel(p,'Explore observations and graph',704,224,'HORIZONTAL',16);row.paddingLeft=row.paddingRight=0;row.fills=[];row.strokes=[];row.effects=[];
 const data=panel(row,'Recorded data',240,192,'VERTICAL',16);text(data,'Column headings','V (V)     I (A)','Micro',208);text(data,'Saved reading','3.0        0.60','Body',208);text(data,'Recorded condition','Saved at R = 5 Ω','Micro',208,'color/text-muted');
 const plot=track(figma.createFrame());row.appendChild(plot);plot.name='Editable live and recorded V–I plot';plot.resize(432,192);plot.fills=[paint('color/surface')];plot.cornerRadius=12;
 for(let j=0;j<5;j++){vector(plot,'Grid horizontal '+j,[[36,16+j*32],[408,16+j*32]],'color/border',.5);vector(plot,'Grid vertical '+j,[[36+j*93,16],[36+j*93,144]],'color/border',.5);}
 vector(plot,'Axes · V vertical, I horizontal',[[36,16],[36,144],[408,144]],'color/text',1.5);
 circle(plot,'Recorded point · 0.60 A, 3.0 V',218,62,12,'color/text-strong');circle(plot,'Live point · 0.15 A, 1.5 V',77,98,14,'color/accent');pos(text(plot,'Axes labels','V (V) ↑                 I (A) →','Micro',360,'color/text-muted'),36,160);
 const actions=panel(p,'Record actions',704,56,'HORIZONTAL',16);actions.paddingTop=actions.paddingBottom=0;actions.fills=[];actions.strokes=[];actions.effects=[];button(actions,'Record reading',true,280);button(actions,'Clear',false,160);
 text(p,'Wire','WIRE · Twice as long · 10 Ω','Label',704,'color/measurement-resistance');text(p,'Relation','Same voltage, twice the resistance: half the current.','Body',704);button(p,'Back to the case',false,280,frames[23]);pos(help(f),912,912);
 }

 built.push({id:f.id,name:f.name,children:f.children.length,textCount:f.findAllWithCriteria({types:['TEXT']}).length,instances:f.findAllWithCriteria({types:['INSTANCE']}).length});
}
for(const [n,destinationId]of links){let root=n;while(root.parent&&root.parent.type!=='PAGE')root=root.parent;if(root.id===destinationId)continue;await n.setReactionsAsync([{trigger:{type:'ON_CLICK'},actions:[{type:'NODE',destinationId,navigation:'NAVIGATE',transition:{type:'DISSOLVE',easing:{type:'EASE_OUT'},duration:.18},preserveScrollPosition:false}]}]);
}
return {createdNodeIds:created,mutatedNodeIds:mutated,built,prototypeConnections:links.length};
