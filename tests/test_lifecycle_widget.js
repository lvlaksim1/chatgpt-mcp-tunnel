// A deterministic iframe/host protocol test, no network, no user message.
"use strict";
const fs=require("fs"),vm=require("vm"),path=require("path"),assert=require("assert").strict;
const raw=fs.readFileSync(path.join(__dirname,"../src/ChatGptMcpTunnel/ChatLifecycleApp.html"),"utf8");
const script=raw.match(/<script>([\s\S]*?)<\/script>/)[1];
async function simulate(mode) {
 const log=[],sent=[],listeners={},docListeners={},ticks=new Map();
 let nextTimer=0;
 const status={textContent:""};
 const parent={
  postMessage(msg){
   sent.push(msg);
   if(msg.method==="ui/notifications/initialized")return;
   if(msg.id===999 && msg.result) return;
   if(msg.method==="ui/initialize"){
    listeners.message({source:parent,data:{jsonrpc:"2.0",id:msg.id,result:{
     protocolVersion:"2026-01-26",
     hostInfo:{name:"ChatGPT"},
     hostCapabilities:{message:{text:true},tools:{},experimental:{"openai/message":true}},
     hostContext:{"openai/deepLink":{url:"/chat/private?token=DO_NOT_LOG"},displayMode:"fullscreen"}
    }}});
    return;
   }
   if(msg.method==="tools/call"){
    assert.equal(msg.params.name,"chat.lifecycle.event");
    const d=msg.params.arguments;
    log.push(d);
    assert.equal(d.mode,mode);
    assert.ok(!d.details_json.includes("private"),d.details_json);
    assert.ok(!d.details_json.includes("DO_NOT_LOG"),d.details_json);
    listeners.message({source:parent,data:{jsonrpc:"2.0",id:msg.id,
     result:{content:[{type:"text",text:"{\"ok\":true}"}]}}});
    return;
   }
   throw Error("unexpected "+msg.method);
  }
 };
 const doc={
  getElementById(id){assert.equal(id,"status");return status},
  addEventListener(t,cb){docListeners[t]=cb},
  visibilityState:"visible"
 };
 const win={parent,addEventListener(t,cb){listeners[t]=cb}};
 const setTimeoutFake=(fn)=>{const id=++nextTimer;ticks.set(id,fn);return id};
 const clearTimeoutFake=id=>ticks.delete(id);
 const setIntervalFake=fn=>{const id=++nextTimer;ticks.set(id,fn);return id};
 const clearIntervalFake=id=>ticks.delete(id);
 const fake={window:win,document:doc,Date,Math,JSON,
  setTimeout:setTimeoutFake,clearTimeout:clearTimeoutFake,
  setInterval:setIntervalFake,clearInterval:clearIntervalFake,console};
 vm.runInNewContext(script.replace("__LIFECYCLE_MODE__",mode),fake);
 for(let i=0;i<30;i++)await Promise.resolve();
 assert.ok(log.some(e=>e.kind==="ui-initialize"));
 assert.ok(log.some(e=>e.kind==="ui-initialized"));
 assert.ok(status.textContent.startsWith("MCP Tunnel:"));
 const init=JSON.parse(log.find(e=>e.kind==="ui-initialize").details_json);
 assert.equal(init.hasDeepLink,true);
 assert.equal(init.host,"ChatGPT");
 assert.equal(init.displayMode,"fullscreen");
 listeners.message({source:parent,data:{
  jsonrpc:"2.0",method:"ui/notifications/host-context-changed",
  params:{"openai/deepLink":{url:"/chat/other?token=SECRET2"}}
 }});
 for(let i=0;i<20;i++)await Promise.resolve();
 assert.ok(log.some(e=>e.kind==="host-context-changed"));
 doc.visibilityState="hidden";docListeners.visibilitychange();
 for(let i=0;i<20;i++)await Promise.resolve();
 assert.ok(log.some(e=>e.kind==="visibility-change"));
 listeners.message({source:parent,data:{jsonrpc:"2.0",id:999,
  method:"ui/resource-teardown",params:{}}});
 for(let i=0;i<20;i++)await Promise.resolve();
 assert.ok(sent.some(x=>x.id===999&&x.result));
 assert.ok(log.some(e=>e.kind==="teardown"));
 assert.ok(!sent.some(x=>x.method==="ui/message"),"diagnostic must not invoke model");
 console.log("LIFECYCLE_"+mode.toUpperCase()+"_IFRAME_HANDSHAKE_CONTEXT_TEARDOWN_OK");
}
(async()=>{await simulate("global");await simulate("thread")})().catch(e=>{console.error(e);process.exitCode=1});
