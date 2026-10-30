"use strict";
const fs=require("fs"),path=require("path"),vm=require("vm"),assert=require("assert").strict;
const source=fs.readFileSync(path.join(__dirname,"../src/ChatGptMcpTunnel/ChatBootstrapApp.html"),"utf8")
 .match(/<script>([\s\S]*?)<\/script>/)[1];
const ordinary="/c/01234567-89ab-cdef-0123-456789abcdef";
async function check(label,{route,extension}){
 let sequence=0;
 const sent=[],listeners={},timers=new Map(),status={textContent:""};
 const host={postMessage(msg){
  sent.push(msg);
  if(msg.method==="ui/notifications/initialized")return;
  if(msg.method==="ui/initialize"){
   listeners.message({source:host,data:{jsonrpc:"2.0",id:msg.id,result:{
    hostInfo:{name:"chatgpt"},
    hostCapabilities:{message:{text:{}},experimental:extension?{"openai/message":{}}:{}},
    hostContext:route?{"openai/deepLink":{url:route}}:{}
   }}});
   return;
  }
  if(msg.method==="tools/call"){
   assert.equal(msg.params.name,"chat.lifecycle.event",label);
   listeners.message({source:host,data:{jsonrpc:"2.0",id:msg.id,
    result:{content:[{type:"text",text:'{"ok":true}'}]}}});
   return;
  }
  throw Error("Forbidden host call "+msg.method);
 }};
 const w={parent:host,addEventListener(name,fn){listeners[name]=fn}};
 const enableButton={hidden:true,click:null,addEventListener(name,fn){if(name==="click")this.click=fn}};
 const doc={getElementById(id){return id==="enable"?enableButton:status},addEventListener(){},visibilityState:"visible"};
 const sandbox={window:w,document:doc,URL,Date,Math,JSON,console,
  setTimeout(fn){const id=++sequence;timers.set(id,fn);return id},
  clearTimeout(id){timers.delete(id)},
  setInterval(){return ++sequence;},clearInterval(){}};
 vm.runInNewContext(source,sandbox);
 for(let i=0;i<80;i++)await Promise.resolve();
 assert.equal(sent.filter(x=>x.method==="ui/message").length,0,label);
 assert.equal(sent.filter(x=>x.params?.name==="chat.ui.pending").length,0,label);
 const allowed=route===ordinary&&extension;
 assert.equal(timers.size,allowed?1:0,label+": timers");
 if(!allowed)assert(status.textContent.includes("queue retained"),label+": safe explanation");
 if(!allowed && !route && extension){
  assert.equal(enableButton.hidden,false,"Host without URL must ask for confirmation");
  assert.equal(typeof enableButton.click,"function");
  enableButton.click();
  for(let i=0;i<60;i++)await Promise.resolve();
  assert.equal(timers.size,1,"Explicit confirmation should enable ordinary dispatcher");
 }
 if(allowed || (!route && extension)){
  listeners.message({source:host,data:{jsonrpc:"2.0",
    method:"ui/notifications/host-context-changed",
    params:{"openai/deepLink":{url:"/mcp-app/host"}}}});
  for(let i=0;i<80;i++)await Promise.resolve();
  assert.equal(timers.size,0,label+": Work mode clears pending poll");
 }
}
(async()=>{
 await check("missing-feature",{route:ordinary,extension:false});
 await check("global-work",{route:"/mcp-app/test/app",extension:true});
 await check("unknown-host",{route:null,extension:true});
 await check("wrong-origin",{route:"https://evil.test/c/01234567-89ab-cdef-0123-456789abcdef",extension:true});
 await check("normal-can-arm",{route:ordinary,extension:true});
 console.log("BOOTSTRAP_WORK_AND_UNSUPPORTED_HOST_BLOCKED_WITHOUT_QUEUE_LOSS_OK");
})().catch(e=>{console.error(e);process.exitCode=1});
