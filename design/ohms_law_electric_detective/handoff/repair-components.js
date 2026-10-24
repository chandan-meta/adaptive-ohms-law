await figma.setCurrentPageAsync(await figma.getNodeByIdAsync('2:3'));const changed=[];
for(const name of ['Readout','Clue','Banner','Captions','Chat bubble'])for(const c of C[name])for(const t of c.findAllWithCriteria({types:['TEXT']})){if(t.componentPropertyReferences&&Object.keys(t.componentPropertyReferences).length){t.componentPropertyReferences={};changed.push(t.id);}}
for(const c of figma.currentPage.findAllWithCriteria({types:['COMPONENT']})){for(const t of c.findAllWithCriteria({types:['TEXT']})){if(t.width<350&&t.characters.length<70){t.textAutoResize='WIDTH_AND_HEIGHT';changed.push(t.id);}}}
for(const c of C['Voltage slider']){c.itemSpacing=8;c.paddingTop=16;c.paddingBottom=16;changed.push(c.id);}
for(const c of C.Readout){c.itemSpacing=4;const t=c.children.find(n=>n.type==='TEXT'&&n.textStyleId===styles.Measure.id);if(t){t.textAutoResize='WIDTH_AND_HEIGHT';changed.push(t.id);}changed.push(c.id);}
for(const c of C['Observation table']){c.itemSpacing=4;changed.push(c.id);}
for(const c of C['Detective challenge']){c.itemSpacing=12;changed.push(c.id);}

// Gradients are shared paint styles; semantic colors remain available for solid fallback.
const rgba=(h,a=1)=>({r:parseInt(h.slice(1,3),16)/255,g:parseInt(h.slice(3,5),16)/255,b:parseInt(h.slice(5,7),16)/255,a});
const gradients={Paper:['#FFF1D0','#EAD1A0'],Navy:['#203A4C','#102333'],Gold:['#FFE47B','#FFC22C'],Blue:['#177CB9','#105180']};const ps={};for(const [name,cs]of Object.entries(gradients)){const s=figma.createPaintStyle();s.name='ED/Material/'+name;s.paints=[{type:'GRADIENT_LINEAR',gradientTransform:[[0,1,0],[-1,0,1]],gradientStops:[{position:0,color:rgba(cs[0])},{position:1,color:rgba(cs[1])}]}];ps[name]=s.id;}
for(const c of figma.currentPage.findAllWithCriteria({types:['COMPONENT']})){const paper=c.fills.some(p=>p.boundVariables&&p.boundVariables.color&&p.boundVariables.color.id===vars.paper.id);if(paper){c.fillStyleId=ps.Paper;c.cornerRadius=8;c.strokes=[paint('brass')];const deco=rect(c,'Brass top rule',20,9,c.width-40,2,'brass');changed.push(c.id,deco.id);}else if(c.parent.type==='COMPONENT_SET'&&['ED/Button/Primary','ED/Button/Secondary'].includes(c.parent.name)&&c.name==='State=Default'){c.fillStyleId=ps[c.parent.name.endsWith('Primary')?'Gold':'Blue'];changed.push(c.id);} }
for(const c of C.Banner){const t=c.findAllWithCriteria({types:['TEXT']})[0];t.textAutoResize='HEIGHT';t.resize(500,40);changed.push(t.id);}
return {mutatedNodeIds:changed,createdNodeIds:made,materialStyles:ps};

