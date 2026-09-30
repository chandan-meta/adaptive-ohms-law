import {test} from 'node:test';
import assert from 'node:assert/strict';
import {createServer,payloadFor} from './wattson-server.mjs';
test('context and conversation stay separate from tutor instructions',()=>{
 const p=payloadFor({message:'Why?',context:{concept:6,mastery:50,activity:'Scene06Prediction',question:'Predict the current',unknown:'omit'},history:[{role:'user',text:'What is voltage?'},{role:'assistant',text:'Potential difference.'}]});
 assert.equal(p.contents.length,3);assert.equal(p.contents[1].role,'model');assert.match(p.systemInstruction.parts[0].text,/VoltageCurrentRelationship/);assert.match(p.systemInstruction.parts[0].text,/cannot create graded questions/);assert.doesNotMatch(p.systemInstruction.parts[0].text,/unknown/);
 assert.throws(()=>payloadFor({message:'x'.repeat(1201)}));
 assert.deepEqual(p.tools,[{google_search:{}}]);assert.match(p.systemInstruction.parts[0].text,/incomplete speech/);
 const empty=payloadFor({message:'Help',context:{sourceVoltage:0,currentReading:0,switchClosed:false}});assert.doesNotMatch(empty.systemInstruction.parts[0].text,/"sourceVoltage"/);
 const measured=payloadFor({message:'Help',context:{hasCircuitMeasurements:true,sourceVoltage:2,currentReading:.2,switchClosed:true}});assert.match(measured.systemInstruction.parts[0].text,/"currentReading":0.2/);
});
test('grounding exposes real sources, preserves search suggestions, and does not retry quota',async()=>{
 let calls=0,quota=false;
 const server=createServer({key:'test-only',fetchImpl:async()=>{calls++;return quota?{ok:false,status:429}:{ok:true,json:async()=>({candidates:[{content:{parts:[{text:'Resistance is measured in ohms.'}]},groundingMetadata:{webSearchQueries:['SI resistance unit'],searchEntryPoint:{renderedContent:'<div>Google Search suggestions</div>'},groundingChunks:[{web:{title:'SI reference',uri:'https://www.bipm.org/'}},{web:{title:'Unsafe',uri:'javascript:alert(1)'}}]}}]})};}});
 await new Promise(r=>server.listen(0,'127.0.0.1',r));
 try{
  const base='http://127.0.0.1:'+server.address().port;
  const args={method:'POST',headers:{'Content-Type':'application/json'},body:JSON.stringify({message:'Unit of resistance?'})};
  const response=await (await fetch(base+'/chat',args)).json();assert.equal(response.searched,true);assert.equal(response.sources.length,1);
  const page=await fetch(base+response.sourcesPath);assert.match(page.headers.get('content-security-policy'),/default-src 'none'/);assert.match(await page.text(),/Google Search suggestions/);
  quota=true;const before=calls;const error=await fetch(base+'/chat',args);assert.equal(error.status,429);assert.equal(calls,before+1);assert.equal((await error.json()).error,'provider_quota');
 }finally{await new Promise(r=>server.close(r));}
});
test('HTTP success, blocked browser requests and provider capacity errors',async()=>{
 let busy=false;const server=createServer({key:'test-only',fetchImpl:async(url,options)=>{
   assert.match(url,/^https:\/\/generativelanguage.googleapis.com\//);assert.equal(options.headers['x-goog-api-key'],'test-only');
   return busy?{ok:false,status:503}:{ok:true,json:async()=>({candidates:[{content:{parts:[{text:'Potential difference provides the electrical push.'}]}}]})};
 }});await new Promise(r=>server.listen(0,'127.0.0.1',r));
 try {const url='http://127.0.0.1:'+server.address().port+'/chat';const args={method:'POST',headers:{'Content-Type':'application/json'},body:JSON.stringify({message:'Voltage?'})};
   let r=await fetch(url,args);assert.equal(r.status,200);assert.match((await r.json()).reply,/electrical push/);
   r=await fetch(url,{...args,headers:{...args.headers,Origin:'https://example.org'}});assert.equal(r.status,403);
   busy=true;r=await fetch(url,args);assert.equal(r.status,502);assert.deepEqual(await r.json(),{error:'provider_busy'});
 }finally{await new Promise(r=>server.close(r));}
});
