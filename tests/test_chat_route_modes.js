"use strict";
const assert=require("node:assert/strict"),fs=require("node:fs"),vm=require("node:vm"),path=require("node:path");
const html=fs.readFileSync(path.join(__dirname,"../src/ChatGptMcpTunnel/ChatRelayApp.html"),"utf8");
const script=html.match(/<script>([\s\S]*?)<\/script>/)[1];
const bound="a".repeat(32),newChat="b".repeat(32),messageId="c".repeat(32);
const timers=[],listeners={},calls=[],buttons=new Map();
for(const id of ["start","stop","check","probe","status","details"]){
 const handlers={};buttons.set(id,{id,disabled:false,textContent:"",
 addEventListener:(type,fn)=>handlers[type]=fn,click:()=>handlers.click?.()});
}
let order=0,now=0;
const parent={postMessage(req){
 calls.push(req);if(!req.id)return;
 let result;
 if(req.method==="ui/initialize")result={hostCapabilities:{message:{text:{}}}};
 else if(req.method==="tools/call"){
   const {name,arguments:args}=req.params;
   if(name==="chat.ui.pending"){
     if(!args.discover){assert.equal(args.chat_id,bound);result={content:[{type:"text",text:JSON.stringify({ok:true,count:0,message:null})}]};}
     else {result={content:[{type:"text",text:JSON.stringify({ok:true,count:1,message:{
        id:messageId,chat_id:newChat,chat_title:"New",text:"NEW_USER_MESSAGE",attachments:[]
       }})}]};}
   }else if(name==="chat.ui.ack"){
      assert.equal(args.message_id,messageId);assert.equal(args.new_chat,true);
      result={content:[{type:"text",text:'{"ok":true}' }]};
   }else throw Error(name);
 }else if(req.method==="ui/message"){
   assert.equal(req.params._meta["openai/message"].target,"new");
   assert.ok(req.params.content[0].text.includes("NEW_USER_MESSAGE"));
   assert.ok(!req.params.content[0].text.includes("LOCAL_PRIOR_"));
   assert.ok(req.params.content[0].text.length<550);
   assert.equal(req.params.content[1].annotations.audience[0],"assistant");
   assert.ok(req.params.content[1].text.includes(messageId));
   assert.ok(!req.params.content[0].text.includes(messageId));
   result={};
 }else throw Error(req.method);
 listeners.message({source:parent,data:{jsonrpc:"2.0",id:req.id,result}});
}};
const sandbox={
 document:{getElementById:id=>buttons.get(id)},
 window:{parent,addEventListener:(k,f)=>listeners[k]=f},
 setTimeout:(fn,ms=0)=>{const id=++order;timers.push({id,at:now+ms,fn,active:true});return id;},
 clearTimeout:id=>{const t=timers.find(x=>x.id===id);if(t)t.active=false;},
 console,Date,Math,JSON
};
(async()=>{
 vm.runInNewContext(script,sandbox);
 for(let i=0;i<10;i++)await Promise.resolve();
 listeners.message({source:parent,data:{jsonrpc:"2.0",method:"ui/notifications/tool-result",
  params:{result:{content:[{type:"text",text:JSON.stringify({chat_id:bound})}]}}}});
 assert.equal(buttons.get("start").disabled,true,"host tool-result should auto-start");
 const t=timers.find(x=>x.active && x.at===0);assert.ok(t);t.active=false;t.fn();
 for(let i=0;i<30;i++)await Promise.resolve();
 assert.equal(calls.filter(x=>x.method==="ui/message").length,1);
 assert.equal(calls.filter(x=>x.method==="tools/call"&&x.params.name==="chat.ui.ack").length,1);
 buttons.get("stop").click();
 console.log("EXPLICIT_NEW_CLOUD_THREAD_AND_SINGLE_ACK_OK");
})().catch(e=>{console.error(e);process.exitCode=1});
