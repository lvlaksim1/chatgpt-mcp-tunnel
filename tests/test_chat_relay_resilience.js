"use strict";
const assert=require("node:assert/strict"),fs=require("fs"),vm=require("vm"),path=require("path");
const html=fs.readFileSync(path.join(__dirname,"../src/ChatGptMcpTunnel/ChatRelayApp.html"),"utf8");
const script=html.match(/<script>([\s\S]*?)<\/script>/)[1];
const mid="d".repeat(32),cid="a".repeat(32);

async function check(mode){
  const listeners={},queue=[],requests=[],elements=new Map();
  for(const id of ["start","stop","check","probe","status","details"]){
    const events={};
    elements.set(id,{id,disabled:false,textContent:"",
      addEventListener(t,f){events[t]=f;},click(){return events.click?.();}});
  }
  let now=0,serial=0,ackAttempts=0,acceptedMessages=0;
  const fakeTimeout=(fn,ms=0)=>{
    const t={id:++serial,at:now+ms,fn,active:true};queue.push(t);return t.id;
  };
  const fakeClear=(id)=>{const t=queue.find(x=>x.id===id);if(t)t.active=false;};
  const dispatch = (request, response) =>
    listeners.message({source:parent,data:{jsonrpc:"2.0",id:request.id,...response}});
  const parent={postMessage(req){
    requests.push(req);
    if(!("id" in req))return;
    if(req.method==="ui/initialize"){
      dispatch(req,{result:{hostInfo:{name:"test"},hostCapabilities:{message:{text:{}}}}});return;
    }
    if(req.method==="tools/call"){
      if(req.params.name==="chat.ui.pending"){
        dispatch(req,{result:{content:[{type:"text",text:JSON.stringify({
          ok:true,count:1,context:"Пользователь: LOCAL_PRIOR_TEXT_803\\nАссистент: LOCAL_PRIOR_REPLY_803\\n",
          message:{id:mid,chat_id:cid,chat_title:"Диагностика",text:"Проверка",attachments:[]}
        })}]}});return;
      }
      if(req.params.name==="chat.ui.ack"){
        ackAttempts++;
        if(mode==="ack-once" && ackAttempts===1){
          dispatch(req,{error:{code:-32000,message:"temporary MCP failure"}});return;
        }
        dispatch(req,{result:{content:[{type:"text",text:JSON.stringify({ok:true,message_id:mid})}]}});return;
      }
      throw Error("Unexpected tool "+req.params.name);
    }
    if(req.method==="ui/message"){
      assert.ok(req.params.content[0].text.includes("Проверка"),"User text missing");
      assert.equal(req.params.content[1].annotations.audience[0],"assistant");
      assert.ok(req.params.content[1].text.includes(mid),"MCP reply route marker missing");
      assert.ok(!req.params.content[0].text.includes(mid),"MCP route leaked into visible chat");
      assert.ok(!req.params.content[0].text.includes("LOCAL_PRIOR_TEXT_803"),"History leaked into visible chat");
      assert.ok(!req.params.content[0].text.includes("LOCAL_PRIOR_REPLY_803"),"History leaked into visible chat");
      assert.equal(req.params._meta["openai/message"].target,"active","Wrong cloud thread");
      acceptedMessages++;
      if(mode==="cloudflare-once" && acceptedMessages===1 ||
         mode==="cloudflare-forever"){
        dispatch(req,{error:{code:-32001,message:"cloudflare_challenge"}});return;
      }
      if(mode==="timeout"){
        // No response from host: cannot know whether the model got it.
        return;
      }
      dispatch(req,{result:{ok:true}});
      return;
    }
    throw Error("unexpected "+req.method);
  }};
  const sandbox={
    document:{getElementById:id=>elements.get(id)},
    window:{parent,addEventListener:(t,f)=>listeners[t]=f},
    setTimeout:fakeTimeout,clearTimeout:fakeClear,
    console,Date,Math,JSON
  };
  vm.runInNewContext(script,sandbox);
  async function drain(){for(let i=0;i<50;i++)await Promise.resolve();}
  async function advance(until){
    while(true){
      const next=queue.filter(t=>t.active&&t.at<=until).sort((a,b)=>a.at-b.at)[0];
      if(!next)break;
      next.active=false;now=next.at;next.fn();await drain();
    }
    now=until;
  }
  await drain();
  listeners.message({source:parent,data:{jsonrpc:"2.0",
    method:"ui/notifications/tool-result",params:{result:{content:[{
      type:"text",text:JSON.stringify({chat_id:cid})
    }]}}}});
  assert.equal(elements.get("start").disabled,true,"relay should auto-start");
  await advance(0);
  assert.equal(acceptedMessages,1);
  assert.equal(elements.get("start").disabled,true,"relay stopped after first attempt");
  if(mode==="cloudflare-once"){
    assert.equal(ackAttempts,0,"rejected message must not be acknowledged");
    assert.ok(elements.get("status").textContent.includes("cloudflare_challenge"));
    await advance(2999);
    assert.equal(acceptedMessages,1,"no busy-loop");
    await advance(3000);
    assert.equal(acceptedMessages,2,"must retry explicit rejection");
    assert.equal(ackAttempts,1,"must acknowledge after successful send");
  }
  if(mode==="cloudflare-forever"){
    await advance(3000);
    assert.equal(acceptedMessages,2);
    assert.equal(ackAttempts,0);
    assert.equal(elements.get("start").disabled,true);
    await advance(9000);
    assert.equal(acceptedMessages,3,"exponential retry");
  }
  if(mode==="ack-once"){
    assert.equal(ackAttempts,1);
    assert.equal(acceptedMessages,1);
    await advance(3000);
    assert.equal(ackAttempts,2,"must retry ack");
    assert.equal(acceptedMessages,1,"must not send a duplicate ui/message");
  }
  if(mode==="timeout"){
    assert.equal(ackAttempts,0);
    await advance(20000);
    assert.equal(elements.get("start").disabled,true);
    await advance(23000);
    assert.equal(acceptedMessages,1,"ambiguous delivery must not be resent");
    assert.equal(ackAttempts,0);
  }
  elements.get("stop").click();
  const before=requests.length;
  await advance(200000);
  assert.equal(requests.length,before,"manual Stop failed");
  assert.equal(elements.get("start").disabled,false);
  console.log("RELAY_RETRY_"+mode.toUpperCase().replace(/-/g,"_")+"_OK");
}
(async()=>{
  for(const mode of ["cloudflare-once","cloudflare-forever","ack-once","timeout"])
    await check(mode);
})().catch(e=>{console.error(e);process.exitCode=1;});
