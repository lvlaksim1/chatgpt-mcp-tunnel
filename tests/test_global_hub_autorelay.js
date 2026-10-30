"use strict";
// Stable v0.2.18 compatibility: cached bootstrap widgets must never steal
// messages from a manual chat.ui.open relay, even with valid host capabilities.
const fs=require("fs"),path=require("path"),vm=require("vm"),assert=require("assert").strict;
const html=fs.readFileSync(path.join(__dirname,"../src/ChatGptMcpTunnel/ChatBootstrapApp.html"),"utf8");
const source=html.match(/<script>([\s\S]*?)<\/script>/)[1];
let seq=0, calls=[], logs=[], timers=new Map(), handlers={};
const state={textContent:""};
const enable={hidden:true,addEventListener(_name,fn){this.click=fn}};
const host={postMessage(msg){
  calls.push(msg);
  if(msg.method==="ui/notifications/initialized")return;
  if(msg.method==="ui/initialize"){
    handlers.message({source:host,data:{jsonrpc:"2.0",id:msg.id,result:{
      hostInfo:{name:"chatgpt"},
      hostCapabilities:{message:{text:true},experimental:{"openai/message":true}},
      hostContext:{"openai/deepLink":{url:"/c/01234567-89ab-cdef-0123-456789abcdef"}}
    }}});
  }else if(msg.method==="tools/call"){
    assert.equal(msg.params.name,"chat.lifecycle.event","No chat queue calls in stable mode");
    logs.push(msg.params.arguments);
    handlers.message({source:host,data:{jsonrpc:"2.0",id:msg.id,
      result:{content:[{type:"text",text:'{"ok":true}'}]}}});
  }else throw Error("forbidden bootstrap call "+msg.method);
}};
const doc={getElementById(id){return id==="enable"?enable:state},
  addEventListener(){},visibilityState:"visible"};
vm.runInNewContext(source,{
  window:{parent:host,addEventListener(t,fn){handlers[t]=fn}},document:doc,
  URL,Math,Date,JSON,console,
  setTimeout(fn){const id=++seq;timers.set(id,fn);return id},
  clearTimeout(id){timers.delete(id)},
  setInterval(){return ++seq},clearInterval(){}
});
(async()=>{
  for(let i=0;i<90;i++)await Promise.resolve();
  assert.ok(logs.some(x=>x.kind==="ui-initialize"));
  assert.equal(timers.size,0);
  assert.equal(enable.hidden,true);
  assert.ok(state.textContent.includes("стабильный режим"));
  enable.click?.();
  for(let i=0;i<90;i++)await Promise.resolve();
  assert.equal(timers.size,0);
  assert.equal(calls.filter(x=>x.method==="ui/message").length,0);
  assert.equal(calls.filter(x=>x.params?.name==="chat.ui.pending").length,0);
  console.log("AUTO_HUB_ISOLATED_RELAY_V0218_RETAINS_QUEUE_OK");
})().catch(e=>{console.error(e);process.exitCode=1;});
