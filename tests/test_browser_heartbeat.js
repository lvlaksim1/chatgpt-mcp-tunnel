"use strict";
const fs=require("node:fs"),os=require("node:os"),path=require("node:path"),assert=require("node:assert/strict");
const {spawn}=require("node:child_process");
const root=fs.mkdtempSync(path.join(os.tmpdir(),"tunnel-worker-heartbeat-"));
const script=path.join(__dirname,"../src/ChatGptMcpAgent/BrowserChatBridge.js");
const sleep=ms=>new Promise(ok=>setTimeout(ok,ms));
(async()=>{
 const child=spawn(process.execPath,[script],{env:{...process.env,CHATGPT_MCP_CHAT_TEST_ROOT:root},stdio:"ignore"});
 try{
  await sleep(3500);
  const heartbeat=path.join(root,"browser-bridge","heartbeat.json");
  assert.ok(fs.existsSync(heartbeat),"Worker heartbeat not created");
  let a=JSON.parse(fs.readFileSync(heartbeat,"utf8"));
  assert.equal(a.mode,"waiting_login");
  fs.writeFileSync(path.join(root,"browser-bridge","auth-ready.json"),"{}","utf8");
  await sleep(3100);
  let b=JSON.parse(fs.readFileSync(heartbeat,"utf8"));
  assert.equal(b.mode,"browser");
  assert.ok(Date.parse(b.at)>Date.parse(a.at),"Heartbeat did not advance");
  console.log("BROWSER_WORKER_NO_LOGIN_FALLBACK_AND_HEARTBEAT_PASS");
 }finally{child.kill();await sleep(250);fs.rmSync(root,{recursive:true,force:true})}
})().catch(e=>{console.error(e);process.exitCode=1});
