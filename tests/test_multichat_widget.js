"use strict";
// Simulate the MCP Apps iframe when another local chat needs a new ChatGPT
// conversation. Does not call OpenAI or touch user data.
const fs=require("fs"),vm=require("vm"),assert=require("assert").strict,path=require("path");
const html=fs.readFileSync(path.join(__dirname,"../src/ChatGptMcpTunnel/ChatRelayApp.html"),"utf8");
const script=html.match(/<script>([\s\S]*?)<\/script>/)[1];
const legacy="0".repeat(32),newId="a".repeat(32),messageId="b".repeat(32);
const messages=[];
const listeners={};
const buttons=new Map(["start","stop","probe","check","status","details"].map(id=>
 [id,{id,disabled:false,textContent:"",handlers:{},addEventListener(t,fn){this.handlers[t]=fn},
  click(){return this.handlers.click?.()}}]));
let nextId=0,clock=0,done=false;
const timeouts=[];
const setTimeoutFake=(fn,ms)=>{const task={id:++nextId,at:clock+(ms||0),fn,on:true};timeouts.push(task);return task.id};
const clearTimeoutFake=id=>{const t=timeouts.find(t=>t.id===id);if(t)t.on=false};
const host={postMessage(msg){
 messages.push(msg);
 if(!("id" in msg))return;
 let result={};
 if(msg.method==="ui/initialize")result={protocolVersion:"2026-01-26",hostCapabilities:{message:{text:{}}}};
 else if(msg.method==="tools/call") {
   const name=msg.params.name,args=msg.params.arguments||{};
   let payload;
   if(name==="chat.ui.pending") {
     if(args.chat_id===legacy&&!args.discover) payload={ok:true,count:0,message:null};
     else if(args.chat_id===legacy&&args.discover)payload={ok:true,count:1,message:{
       id:messageId,chat_id:newId,chat_title:"Другой чат",text:"Привет из нового чата",attachments:[]
     }};
     else throw Error("unexpected poll "+JSON.stringify(args));
   }else if(name==="chat.ui.ack") {
     assert.equal(args.message_id,messageId);
     assert.equal(args.new_chat,true);
     payload={ok:true,message_id:messageId};
   }else throw Error("unexpected "+name);
   result={content:[{type:"text",text:JSON.stringify(payload)}]};
 }else if(msg.method==="ui/message"){
   assert.equal(msg.params._meta["openai/message"].target,"new");
   const sent=msg.params.content[0].text;
   assert.ok(msg.params.content[1].text.includes("chat.ui.open"),msg.params.content[1].text);
   assert.ok(msg.params.content[1].text.includes(newId));
   assert.equal(msg.params.content[1].annotations.audience[0],"assistant");
   assert.ok(!sent.includes("chat.ui.open"),"Technical content leaked");
   assert.ok(sent.includes("Привет из нового чата"));
   done=true;
 }else throw Error("unexpected method "+msg.method);
 listeners.message({source:host,data:{jsonrpc:"2.0",id:msg.id,result}});
}};
const fake={console,Date,document:{getElementById:id=>buttons.get(id)},
  window:{parent:host,addEventListener(t,fn){listeners[t]=fn}},
  setTimeout:setTimeoutFake,clearTimeout:clearTimeoutFake};
vm.runInNewContext(script,fake);
(async()=>{
 for(let i=0;i<12;i++)await Promise.resolve();
 listeners.message({source:host,data:{jsonrpc:"2.0",method:"ui/notifications/tool-result",
   params:{result:{content:[{type:"text",text:JSON.stringify({ok:true,chat_id:legacy})}]}}}});
 assert.equal(buttons.get("start").disabled,true,"auto-start expected");
 for(let c=0;c<3;c++){
   const t=timeouts.find(t=>t.on&&t.at<=clock);
   if(!t)break;
   t.on=false;t.fn();for(let k=0;k<15;k++)await Promise.resolve();
 }
 assert.ok(done,"target:new message not sent");
 const ack=messages.filter(x=>x.method==="tools/call"&&x.params.name==="chat.ui.ack");
 assert.equal(ack.length,1);
 buttons.get("stop").click();
 console.log("MULTICHAT_UI_MESSAGE_TARGET_NEW_HANDOFF_OK");
})().catch(e=>{console.error(e);process.exitCode=1});
