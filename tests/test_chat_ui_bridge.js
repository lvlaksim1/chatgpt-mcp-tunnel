// MCP Apps host bridge simulation. No browser, ChatGPT, API or live queue.
"use strict";
const fs = require("fs");
const vm = require("vm");
const assert = require("node:assert/strict");
const path = require("node:path");
const html = fs.readFileSync(path.join(__dirname,"../src/ChatGptMcpTunnel/ChatRelayApp.html"),"utf8");
const script = html.match(/<script>([\s\S]*?)<\/script>/)[1];
async function scenario(denyMessage) {
  const pendingId = "c".repeat(32);
  const chatId = "a".repeat(32);
  const listeners = {};
  const elements = new Map(["start","stop","check","probe","status","details"].map(id=>[id,{
    id,disabled:false,textContent:"",handlers:{},addEventListener(type,handler){this.handlers[type]=handler;},
    click(){return this.handlers.click?.();}
  }]));
  const queue = [];
  let clock = 0, nextTimer = 0;
  const setTimeoutFake = (fn,ms=0) => {const timer={id:++nextTimer,when:clock+ms,fn,active:true}; queue.push(timer); return timer.id;};
  const clearTimeoutFake = (id) => {const item = queue.find(x=>x.id===id); if(item) item.active=false;};
  const posted = [];
  const mock = {
    document:{ getElementById:id=>elements.get(id) },
    window:{ addEventListener(type,fn){listeners[type]=fn;},parent:{
      postMessage(request,target){
        assert.equal(target,"*");
        posted.push(request);
        if(request.id===undefined)return;
        let response;
        if(request.method==="ui/initialize")response={protocolVersion:"2026-01-26",hostInfo:{name:"simulated-chatgpt"},hostCapabilities:{serverTools:{},message:{text:{}}}};
        else if(request.method==="tools/call"){
          const name=request.params.name;
          let payload;
          if(name==="chat.ui.pending")payload={ok:true,count:1,message:{id:pendingId,chat_id:chatId,text:"TEST LOCAL MESSAGE",attachments:[]}};
          else if(name==="chat.ui.ack")payload={ok:true,message_id:pendingId};
          else throw new Error("unexpected tool: "+name);
          response={content:[{type:"text",text:JSON.stringify(payload)}]};
        }else if(request.method==="ui/message"){
          assert.equal(request.params.role,"user");
          assert.equal(request.params._meta["openai/message"].send,true);
          assert.ok(request.params.content[1]?.text.includes(pendingId) ||
                    request.params.content[0].text.includes("Контрольный сигнал"));
          if (request.params.content[1]) {
            assert.equal(request.params.content[1].annotations.audience[0],"assistant");
            assert.ok(!request.params.content[0].text.includes(pendingId));
          }
          if(denyMessage){
            listeners.message({source:mock.window.parent,data:{jsonrpc:"2.0",id:request.id,error:{code:-32000,message:"Blocked by host"}}});
            return;
          }
          response={};
        }else throw new Error("unexpected method: "+request.method);
        listeners.message({source:mock.window.parent,data:{jsonrpc:"2.0",id:request.id,result:response}});
      }
    }},
    setTimeout:setTimeoutFake,clearTimeout:clearTimeoutFake,Date,
    console
  };
  vm.runInNewContext(script,mock);
  await Promise.resolve();await Promise.resolve();
  assert.ok(posted.some(x=>x.method==="ui/initialize"),"initialize missing");
  assert.ok(posted.some(x=>x.method==="ui/notifications/initialized"),"initialized notify missing");
  assert.equal(elements.get("probe").disabled,false,"probe should enable after initialize");
  await elements.get("probe").click();
  for(let i=0;i<12;i++)await Promise.resolve();
  assert.equal(posted.filter(x=>x.method==="ui/message").length,1,"manual message not requested");
  assert.equal(posted.filter(x=>x.method==="tools/call" && x.params.name==="chat.ui.ack").length,0,
    "probe must not acknowledge any queued message");
  if(denyMessage) assert.ok(elements.get("status").textContent.includes("отклонён"));
  else assert.ok(elements.get("status").textContent.includes("хост принял"));
  listeners.message({source:mock.window.parent,
    data:{jsonrpc:"2.0",method:"ui/notifications/tool-result",
          params:{result:{content:[{type:"text",text:JSON.stringify({chat_id:chatId})}]}}}});
  assert.equal(elements.get("start").disabled,true,"relay must autostart on MCP host tool result");
  const advance=async until=>{
    while(true){
      const next = queue.filter(x=>x.active && x.when<=until).sort((a,b)=>a.when-b.when)[0];
      if(!next) break;
      next.active=false; clock=next.when;next.fn();
      for(let i=0;i<12;i++)await Promise.resolve();
    }
    clock=until;
  };
  await advance(0);
  for(let i=0;i<12;i++)await Promise.resolve();
  assert.ok(posted.some(x=>x.method==="ui/message"),"ui/message missing");
  const delivered=posted.filter(x=>x.method==="ui/message" &&
      x.params.content[0].text.includes("TEST LOCAL MESSAGE"));
  assert.equal(delivered.length,1);
  assert.equal(delivered[0].params._meta["openai/message"].target,"active");
  assert.ok(!delivered[0].params.content[0].text.includes("LOCAL_PRIOR_"));
  assert.ok(!posted.some(x=>x.method==="tools/call" &&
      x.params.name==="chat.ui.pending" && x.params.arguments.discover &&
      x.params.arguments.chat_id!==chatId));
  const ack=posted.filter(x=>x.method==="tools/call" && x.params.name==="chat.ui.ack");
  assert.equal(ack.length,denyMessage?0:1,"ack incorrectly marked forwarded");
  if(denyMessage){
    assert.ok(elements.get("status").textContent.includes("Blocked by host"));
    assert.equal(elements.get("start").disabled,true,"temporary error must keep relay armed");
    assert.ok(elements.get("status").textContent.includes("Повтор через"));
  }else assert.ok(elements.get("status").textContent.includes("передано"));
  elements.get("stop").click();
  const old=posted.length;
  await advance(5000);
  assert.equal(posted.length,old,"poll continued after stop");
  console.log(denyMessage?"HOST_REJECTION_RETAINS_QUEUE_OK":"UI_MESSAGE_SENT_AND_ACKNOWLEDGED_OK");
}
(async()=>{await scenario(false);await scenario(true);})().catch(err=>{console.error(err);process.exitCode=1;});
