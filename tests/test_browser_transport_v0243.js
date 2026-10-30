"use strict";
const fs=require("node:fs"),os=require("node:os"),path=require("node:path"),assert=require("node:assert/strict");
const root=fs.mkdtempSync(path.join(os.tmpdir(),"mcp-v0243-"));
process.env.CHATGPT_MCP_CHAT_TEST_ROOT=root;
const m=require("../src/ChatGptMcpAgent/BrowserChatBridge.js");
const dir=name=>{const p=path.join(root,name);fs.mkdirSync(p,{recursive:true});return p};
const data=(folder,id,value)=>fs.writeFileSync(path.join(dir(folder),id+".json"),JSON.stringify(value));
const id=n=>n.repeat(32);
try{
 const local=id("a"), old=id("b"), message=id("c"), stale=id("d");
 const url="https://chatgpt.com/c/01234567-89ab-cdef-0123-456789abcdef";
 data("sessions",local,{Id:local,CreateCloudThread:false,CloudUrl:url});
 data("sessions",old,{Id:old,CreateCloudThread:false});
 assert.equal(m.route(local),url,"Imported chats must use original cloud conversation");
 assert.equal(m.route(old),null,"Old unbound conversation must never create a new cloud chat");
 const t=new Date().toISOString(), yesterday=new Date(Date.now()-86400000).toISOString();
 data("browser-bridge","activated",{at:t});
 data("messages",message,{Id:message,ChatId:local,Status:"pending",Text:"",CreatedUtc:new Date(Date.now()+1000).toISOString(),Attachments:[{Id:id("e"),Name:"file.txt"}]});
 data("messages",stale,{Id:stale,ChatId:local,Status:"pending",Text:"old",CreatedUtc:yesterday,Attachments:[]});
 assert.deepEqual(m.pending().map(x=>x.Id),[message],"New attachment-only messages should be eligible, historical ones blocked");
 data("browser-claims",message,{state:"reserved"});
 assert.equal(m.pending().length,0,"Claim must prevent duplicate delivery");
 assert.equal(m.safeUrl(url+"?x=1"),false);
 assert.equal(m.safeUrl("https://chatgpt.com/work"),false);
 assert.deepEqual(m.canonicalItems([{url,title:"First"},{url,title:"Dup"},{url:"https://evil.test/c/"+url.slice(-36),title:"Bad"}]),
   [{url,title:"First"}]);
 console.log("BROWSER_TRANSPORT_V0243_IMPORT_CUTOFF_ATTACHMENTS_IDEMPOTENCY_PASS");
}finally{
 fs.rmSync(root,{recursive:true,force:true,maxRetries:5,retryDelay:100});
}
