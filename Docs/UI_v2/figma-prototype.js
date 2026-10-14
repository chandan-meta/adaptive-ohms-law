const p=await figma.getNodeByIdAsync(PAGE_ID);await figma.setCurrentPageAsync(p);
const frames={};for(const n of p.children){if(n.type==='FRAME'){const id=n.name.split(' / ')[1]?.split(' — ')[0];if(id)frames[id]=n;}}
const chain=['Launch','History04','ConductorRandom','ConductorVoltage','CrossSection','Prediction','History07','LabTransition08','Instruments','CircuitSetup','Experiment','ReadingSaved','GraphPlot','GraphComplete','Comparison','ConstantRatio','ResistanceSlope','ResistanceUnit','Equation','Challenge','Condition','Quiz','Results'];
const mutated=[],links=[];
async function link(n,dest){await n.setReactionsAsync([{trigger:{type:'ON_CLICK'},actions:[{type:'NODE',destinationId:frames[dest].id,navigation:'NAVIGATE',transition:{type:'DISSOLVE',duration:.22,easing:{type:'EASE_OUT'}},resetScrollPosition:true}]}]);mutated.push(n.id);links.push({from:n.id,to:frames[dest].id});}
async function back(n){await n.setReactionsAsync([{trigger:{type:'ON_CLICK'},actions:[{type:'BACK'}]}]);mutated.push(n.id);}
for(let i=0;i<chain.length;i++){const f=frames[chain[i]];const b=f.children.find(n=>n.type==='INSTANCE'&&n.name.includes('Button/Primary'));if(b)await link(b,chain[i+1]||'Launch');}
for(const [id,f]of Object.entries(frames)){
const instances=f.findAllWithCriteria({types:['INSTANCE']});
for(const n of instances){if(id!=='Pause'&&(n.name==='Control/Pause'||n.name==='Pause'))await link(n,'Pause');if(id!=='Settings'&&(n.name==='Control/Settings'||n.name==='Settings'))await link(n,'Settings');}
if(['Pause','Settings','Help'].includes(id)){const modal=f.children.find(n=>n.type==='INSTANCE'&&n.name.includes('Modal/'));if(modal){for(const n of modal.findAllWithCriteria({types:['INSTANCE']})){if(n.name.includes('Button/'))await back(n);}}}
if(id==='CircuitError'){const n=f.children.find(n=>n.type==='INSTANCE'&&n.name.includes('Button/'));if(n)await link(n,'CircuitSetup');}
if(id==='QuizRetry'){const n=f.children.find(n=>n.type==='INSTANCE'&&n.name.includes('Button/Primary'));if(n)await link(n,'Quiz');}
if(['WattsonChat','VoiceError'].includes(id)){const n=f.children.find(n=>n.type==='INSTANCE'&&n.name.includes('Button/Secondary'));if(n)await back(n);}
if(id==='LoadError'){const n=f.children.find(n=>n.type==='INSTANCE'&&n.name.includes('Button/Primary'));if(n)await link(n,'Experiment');}
}
const quiz=frames.Quiz;const answers=quiz.children.filter(n=>n.type==='INSTANCE'&&n.name.includes('Quiz/AnswerOption'));for(let i=0;i<answers.length;i++)await link(answers[i],i===0?'Results':'QuizRetry');
return{createdNodeIds:[],mutatedNodeIds:mutated,links,frameCount:Object.keys(frames).length,prototypeStart:frames.Launch.id};

