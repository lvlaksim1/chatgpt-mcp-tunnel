"use strict";
const fs=require("fs"),vm=require("vm"),path=require("path"),assert=require("assert").strict;
const html=fs.readFileSync(path.join(__dirname,"../src/ChatGptMcpTunnel/ChatBootstrapApp.html"),"utf8");
const src=html.match(/<script>([\s\S]*?)<\/script>/)[1];
const calls=[],logs=[],messages=[],timeouts=new Map(),intervals=new Map();
let sequence=0,acknowledged=false;
const chatId="6".repeat(32),messageId="7".repeat(32),status={textContent:""};
const testMessage={id:messageId,chat_id:chatId,chat_title:"Cold-start test",
 text:"MCP HUB AUTOMATIC TEST - reply via chat.reply.",attachments:[]};
const host={postMessage(msg){
 calls.push(msg);
 if(msg.method==="ui/notifications/initialized")return;
 if(msg.method==="ui/initialize"){
  callbacks.message({source:host,data:{jsonrpc:"2.0",id:msg.id,
   result:{hostInfo:{name:"chatgpt"},hostCapabilities:{
    message:{text:true},experimental:{"openai/message":true},
    updateModelContext:{}
   },hostContext:{"openai/deepLink":{url:"/c/01234567-89ab-cdef-0123-456789abcdef"},displayMode:"fullscreen"}}}});
  return;
 }
 if(msg.method==="ui/message"){
  messages.push(msg);
  assert.equal(msg.params._meta["openai/message"].target,"new");
  assert.equal(msg.params._meta["openai/message"].send,true);
  assert.ok(msg.params.content[0].text.includes(chatId));
  assert.ok(msg.params.content[0].text.includes(messageId));
  assert.ok(msg.params.content[0].text.includes("PREVIOUS_REPLY_21"),
    "Ordinary bridge did not restore prior logical chat context");
  assert.ok(msg.params.content[0].text.includes("chat.reply(message_id="),
    "Model reply callback instructions are missing");
  callbacks.message({source:host,data:{jsonrpc:"2.0",id:msg.id,result:{ok:true}}});
  return;
 }
 if(msg.method==="tools/call"){
  const {name,arguments:args}=msg.params;
  let payload;
  if(name==="chat.lifecycle.event"){
   logs.push(args);
   assert.ok(!args.details_json.includes("cold-test"));
   payload={ok:true};
  }else if(name==="chat.ui.pending"){
   assert.equal(args.discover,true);
   assert.equal(args.chat_id,"0".repeat(32));
   payload={ok:true,count:acknowledged?0:1,message:acknowledged?null:testMessage,
    context:"User: Previous question\nAssistant: PREVIOUS_REPLY_21\n"};
  }else if(name==="chat.ui.ack"){
   assert.equal(args.new_chat,true);
   assert.equal(args.message_id,messageId);
   acknowledged=true;
   payload={ok:true};
  }else throw Error("Unexpected MCP tool "+name);
  callbacks.message({source:host,data:{jsonrpc:"2.0",id:msg.id,
   result:{content:[{type:"text",text:JSON.stringify(payload)}]}}});
  return;
 }
 if(msg.id===901 && msg.result)return;
 throw Error("Unexpected host method "+msg.method);
}};
const callbacks={},docCallbacks={};
const window={parent:host,addEventListener(n,f){callbacks[n]=f}};
const enableButton={hidden:true,addEventListener(){}};
const document={getElementById(id){return id==="enable"?enableButton:status;},visibilityState:"visible",
 addEventListener(n,f){docCallbacks[n]=f}};
function setTimeoutFake(fn){let id=++sequence;timeouts.set(id,fn);return id}
function clearTimeoutFake(id){timeouts.delete(id)}
function setIntervalFake(fn){let id=++sequence;intervals.set(id,fn);return id}
function clearIntervalFake(id){intervals.delete(id)}
const context={window,document,Math,Date,JSON,URL,console,
 setTimeout:setTimeoutFake,clearTimeout:clearTimeoutFake,
 setInterval:setIntervalFake,clearInterval:clearIntervalFake};
async function drain(){for(let i=0;i<45;i++)await Promise.resolve();}
(async()=>{
 vm.runInNewContext(src,context);
 await drain();
 assert.ok(logs.some(x=>x.kind==="ui-initialize"),"init missing");
 assert.ok(logs.some(x=>x.kind==="ui-initialized"),"initialized missing");
 assert.equal(messages.length,0,"Must not send empty message on startup");
 let schedule=[...timeouts.entries()].find(([id])=>id===Math.max(...timeouts.keys()));
 assert.ok(schedule,"poll not scheduled");
 timeouts.delete(schedule[0]);
 schedule[1]();
 await drain();
 assert.equal(messages.length,1);
 assert.equal(acknowledged,true);
 assert.ok(logs.some(x=>x.kind==="hub-forwarded"));
 document.visibilityState="hidden";docCallbacks.visibilitychange();
 await drain();
 assert.ok(logs.some(x=>x.kind==="visibility-change"));
 const again=[...timeouts.entries()].filter(([id])=>id===Math.max(...timeouts.keys()))[0];
 assert.ok(again);
 timeouts.delete(again[0]);again[1]();
 await drain();
 assert.equal(messages.length,1,"Duplicate message after ACK");
 callbacks.message({source:host,data:{jsonrpc:"2.0",id:901,
  method:"ui/resource-teardown",params:{}}});
 await drain();
 assert.ok(logs.some(x=>x.kind==="teardown"));
 assert.ok(calls.some(x=>x.id===901&&x.result));
 console.log("ORDINARY_CHAT_BOOTSTRAP_AUTOPOLL_TARGET_NEW_ACK_NO_DUPLICATES_OK");
})().catch(e=>{console.error(e);process.exitCode=1;});
