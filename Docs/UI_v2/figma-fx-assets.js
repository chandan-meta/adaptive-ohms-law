const p=await figma.getNodeByIdAsync('2:2');await figma.setCurrentPageAsync(p);
const fillStyles=await figma.getLocalPaintStylesAsync(),effectStyles=await figma.getLocalEffectStylesAsync();
const assets={};let assetY=80;
function asset(name,w,h,padding,kind,accent='#22D3EE'){
 const frame=track(figma.createFrame());frame.name='Export / '+name;frame.resize(w+padding*2,h+padding*2);frame.fills=[];frame.clipsContent=true;p.appendChild(frame);at(frame,2500,assetY);assetY+=frame.height+64;
 const r=track(figma.createRectangle());r.name='Decoration only / no text';r.resize(w,h);r.cornerRadius=name.startsWith('Button')?12:16;frame.appendChild(r);at(r,padding,padding);
 if(kind==='surface'){r.fillStyleId=fillStyles.find(s=>s.name===(name.startsWith('Button')?'OHM/FX/Illuminated primary':'OHM/FX/Navy acrylic')).id;}
 if(kind==='halo'){r.fills=[];r.strokes=[{type:'SOLID',color:rgb(accent),opacity:.83}];r.strokeWeight=1.25;r.effects=effectStyles.find(s=>s.name===(name.startsWith('Button')?'OHM/FX/Primary illumination':'OHM/FX/Panel halo')).effects.map(e=>e.type==='DROP_SHADOW'&&e.offset.y===0?{...e,color:{...rgb(accent),a:e.color.a}}:e);}
 if(kind==='reflection'){r.fills=[{type:'GRADIENT_LINEAR',gradientTransform:[[0,1,0],[-1,0,1]],gradientStops:[{position:0,color:{r:.78,g:.98,b:1,a:.13}},{position:.35,color:{r:.21,g:.73,b:1,a:.03}},{position:1,color:{r:1,g:1,b:1,a:0}}]}];}
 assets[name]={id:frame.id,width:frame.width,height:frame.height,padding,core:[w,h],corner:name.startsWith('Button')?12:16};
}
asset('PanelSurface',128,128,0,'surface');asset('PanelCyanHalo',128,128,64,'halo');asset('PanelAmberHalo',128,128,64,'halo','#F59E0B');asset('PanelBlueHalo',128,128,64,'halo','#3682F6');asset('PanelReflection',128,48,0,'reflection');asset('ButtonSurface',200,56,0,'surface');asset('ButtonHalo',200,56,64,'halo');
const acrylic=fillStyles.find(s=>s.name==='OHM/FX/Navy acrylic');let solid=fillStyles.find(s=>s.name==='OHM/FX/Navy solid fallback');if(!solid)solid=figma.createPaintStyle();solid.name='OHM/FX/Navy solid fallback';solid.paints=acrylic.paints.map(p=>({...p,opacity:1}));
const fallback=box(p,'Low cost / solid tablet fallback',1120,100,'color/bg','VERTICAL',48);at(fallback,80,2300);txt(fallback,'Title','LOW-COST TABLET FALLBACK','Title');txt(fallback,'Specification','Keep gradients, crisp borders and sharp text. Disable broad halos and sheen; retain one primary/selected glow. No scene-camera bloom is required.','Body',1024,'color/text-muted');
const source=await figma.getNodeByIdAsync('9:1318');const c=track(source.clone());c.name='Fallback/TabletObjective';p.appendChild(c);c.x=3700;c.y=80;c.fillStyleId=solid.id;c.effectStyleId='';c.effects=[];for(const n of c.findAll(()=>true)){if(n.name.startsWith('FX /'))n.visible=false;if(n.type==='INSTANCE'){n.effects=[];n.effectStyleId='';}}
fallback.appendChild(track(c.createInstance()));
return{createdNodeIds:[...new Set([...created,...c.findAll(()=>true).map(n=>n.id),...fallback.findAll(()=>true).map(n=>n.id)])],assets,fallback:fallback.id,solidStyle:solid.id};
