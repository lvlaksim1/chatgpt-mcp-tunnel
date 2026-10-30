"use strict";
const fs=require("node:fs"),os=require("node:os"),path=require("node:path"),assert=require("node:assert/strict");
const root=fs.mkdtempSync(path.join(os.tmpdir(),"browser-bridge-unit-"));
process.env.CHATGPT_MCP_CHAT_TEST_ROOT=root;
const worker=require("../src/ChatGptMcpAgent/BrowserChatBridge.js");
const sessions=path.join(root,"sessions"),messages=path.join(root,"messages"),claims=path.join(root,"browser-claims"),threads=path.join(root,"browser-threads");
for(let dir of [sessions,messages,claims,threads])fs.mkdirSync(dir,{recursive:true});
const newChat="a".repeat(32),legacy="b".repeat(32),message="c".repeat(32);
const write=(dir,id,obj)=>fs.writeFileSync(path.join(dir,id+".json"),JSON.stringify(obj),"utf8");
try{
 write(sessions,newChat,{Id:newChat,CreateCloudThread:true});
 write(sessions,legacy,{Id:legacy,CreateCloudThread:false});
 write(messages,message,{Id:message,ChatId:newChat,Text:"Привет, Ёж",Status:"pending",CreatedUtc:new Date().toISOString(),Attachments:[]});
 assert.equal(worker.route(newChat),"https://chatgpt.com/");
 assert.equal(worker.route(legacy),null,"Old conversations must not silently be recreated");
 assert.equal(worker.pending().length,1);
 write(claims,message,{state:"sent"});
 assert.equal(worker.pending().length,0,"Uncertain message must not be duplicated");
 fs.unlinkSync(path.join(claims,message+".json"));
 const bound="https://chatgpt.com/c/01234567-89ab-cdef-0123-456789abcdef";
 write(threads,newChat,{chatId:newChat,url:bound});
 assert.equal(worker.route(newChat),bound,"Existing cloud thread must be reopened");
 assert.equal(worker.safeUrl("https://chatgpt.com/work"),false);
 assert.equal(worker.safeUrl("https://chatgpt.com/c/01234567-89ab-cdef-0123-456789abcdef?x=1"),false);
 write(messages,message,{Id:message,ChatId:newChat,Text:"Привет, Ёж",Status:"answered",CreatedUtc:new Date().toISOString(),Attachments:[]});
 assert.equal(worker.pending().length,0);
 console.log("BROWSER_CHAT_QUEUE_CLAIMS_EXISTING_AND_NEW_ROUTES_PASS");
}finally{fs.rmSync(root,{recursive:true,force:true})}
