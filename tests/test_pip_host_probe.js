"use strict";
const fs=require("fs"),vm=require("vm"),assert=require("assert").strict,path=require("path");
const html=fs.readFileSync(path.join(__dirname,"../src/ChatGptMcpTunnel/ChatLifecycleApp.html"),"utf8");
const js=html.match(/<script>([\s\S]*?)<\/script>/)[1].replace("__LIFECYCLE_MODE__","thread");
async function scenario(advertised,resultMode){
  const calls=[],listeners={},timers=[],els={},parent={};
  let time=0,seq=0;
  for(const id of ["status","test-pip","test-fullscreen","test-inline","capabilities"]){
    els[id]={disabled:false,textContent:"",callbacks:{},addEventListener(t,f){this.callbacks[t]=f},click(){this.callbacks.click?.()}};
  }
  const emit=m=>listeners.message({source:parent,data:m});
  parent.postMessage=req=>{
    calls.push(req);
    if(!req.id)return;
    if(req.method==="ui/initialize")emit({jsonrpc:"2.0",id:req.id,result:{
      hostInfo:{name:"simulated"},
      hostCapabilities:{message:{text:{}},sampling:advertised?{}:undefined},
      hostContext:{displayMode:"inline",availableDisplayModes:advertised?["inline","fullscreen","pip"]:["inline","fullscreen"]}
    }});
    else if(req.method==="tools/call")emit({jsonrpc:"2.0",id:req.id,result:{content:[{type:"text",text:'{"ok":true}'}]}});
    else if(req.method==="ui/request-display-mode")emit({jsonrpc:"2.0",id:req.id,result:{mode:resultMode}});
    else throw Error("Unexpected request: "+req.method);
  };
  const sandbox={
    window:{parent,addEventListener:(t,fn)=>listeners[t]=fn},
    document:{getElementById:id=>els[id],addEventListener:()=>{},visibilityState:"visible"},
    setTimeout:(fn,ms=0)=>{const x={id:++seq,when:time+ms,fn,active:true};timers.push(x);return x.id},
    clearTimeout:id=>{let x=timers.find(x=>x.id===id);if(x)x.active=false},
    setInterval:()=>111,clearInterval:()=>{},Date,JSON,Math,console
  };
  vm.runInNewContext(js,sandbox);
  for(let i=0;i<60;i++)await Promise.resolve();
  const t=timers.filter(x=>x.active&&x.when===350)[0];assert.ok(t,"pip probe timer missing");
  time=350;t.active=false;t.fn();
  for(let i=0;i<80;i++)await Promise.resolve();
  const requested=calls.filter(x=>x.method==="ui/request-display-mode");
  assert.equal(requested.length,1,"pip request must be issued once");
  assert.equal(requested[0].params.mode,"pip");
  const logged=calls.filter(x=>x.method==="tools/call").map(x=>JSON.parse(x.params.arguments.details_json));
  assert.ok(logged.some(x=>x.source==="display-result"&&x.mode==="pip"&&x.displayMode===resultMode),"actual mode not logged");
  assert.ok(!calls.some(x=>x.method==="ui/message"),"probe must never send chat content");
  console.log("PIP_HOST_SCENARIO_PASS advertised="+advertised+" result="+resultMode);
}
(async()=>{await scenario(false,"inline");await scenario(true,"pip")})().catch(e=>{console.error(e);process.exit(1)});
