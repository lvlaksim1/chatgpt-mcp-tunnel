"use strict";
const fs=require("fs"),vm=require("vm"),path=require("path"),assert=require("assert").strict;
const source=fs.readFileSync(path.join(__dirname,"../src/ChatGptMcpTunnel/ChatGlobalHubApp.html"),"utf8")
 .match(/<script>([\s\S]*?)<\/script>/)[1];
const calls=[],logs=[],timeouts=new Map(),intervals=[],listeners={};
const status={textContent:""};
let next=0;
const host={postMessage(msg){
 calls.push(msg);
 if(msg.method==="ui/notifications/initialized")return;
 if(msg.method==="ui/initialize"){
  listeners.message({source:host,data:{jsonrpc:"2.0",id:msg.id,result:{
   hostInfo:{name:"chatgpt"},hostCapabilities:{message:{text:{}},experimental:{"openai/message":{}},
   updateModelContext:{}},hostContext:{"openai/deepLink":{url:"/global"}}
  }}});
  return;
 }
 if(msg.method==="tools/call"){
  assert.equal(msg.params.name,"chat.lifecycle.event","Global observer must never poll message queue");
  logs.push(msg.params.arguments);
  listeners.message({source:host,data:{jsonrpc:"2.0",id:msg.id,
   result:{content:[{type:"text",text:'{"ok":true}'}]}}});
  return;
 }
 if(msg.id===700&&msg.result)return;
 throw Error("Unexpected host invocation: "+msg.method);
}};
const win={parent:host,addEventListener(n,fn){listeners[n]=fn}};
const doc={getElementById(){return status},visibilityState:"visible",addEventListener(){}};
const fake={window:win,document:doc,Math,Date,JSON,console,
 setTimeout(fn){const id=++next;timeouts.set(id,fn);return id},
 clearTimeout(id){timeouts.delete(id)},setInterval(fn){const id=++next;intervals.push([id,fn]);return id},
 clearInterval(){}};
async function drain(){for(let i=0;i<65;i++)await Promise.resolve();}
(async()=>{
 vm.runInNewContext(source,fake);
 await drain();
 assert.ok(logs.some(x=>x.kind==="ui-initialize"));
 assert.ok(logs.some(x=>x.kind==="ui-initialized"));
 assert.equal(timeouts.size,0,"Global entrypoint must not schedule queue polling");
 assert.equal(calls.filter(x=>x.method==="ui/message").length,0);
 assert.equal(status.textContent,"MCP Tunnel: Global status observer");
 assert.ok(logs.every(x=>x.instance_id.startsWith("hub-")));
 listeners.message({source:host,data:{jsonrpc:"2.0",id:700,method:"ui/resource-teardown",params:{}}});
 await drain();
 assert.ok(logs.some(x=>x.kind==="teardown"));
 console.log("GLOBAL_OBSERVER_CANNOT_CREATE_WORK_MESSAGES_OK");
})().catch(e=>{console.error(e);process.exitCode=1;});
