"use strict";
const fs=require('node:fs'),os=require('node:os'),path=require('node:path'),assert=require('node:assert/strict');
const root=fs.mkdtempSync(path.join(os.tmpdir(),'mcp-browser-repair-'));
process.env.CHATGPT_MCP_CHAT_TEST_ROOT=root;
const worker=require('../src/ChatGptMcpAgent/BrowserChatBridge.js');
const ids={chat:'1'.repeat(32),message:'2'.repeat(32),legacy:'3'.repeat(32),legacyMessage:'4'.repeat(32)};
const url='https://chatgpt.com/c/01234567-89ab-cdef-0123-456789abcdef';
const make=(dir,name,data)=>{const p=path.join(root,dir);fs.mkdirSync(p,{recursive:true});fs.writeFileSync(path.join(p,name+'.json'),JSON.stringify(data),'utf8')};
try{
 make('sessions',ids.chat,{Id:ids.chat,CreateCloudThread:true});
 make('messages',ids.message,{Id:ids.message,ChatId:ids.chat,Status:'answered'});
 make('browser-claims',ids.message,{id:ids.message,chatId:ids.chat,state:'answered',url});
 assert.equal(worker.repairAnsweredBindings(),1);
 assert.equal(worker.route(ids.chat),url);
 assert.equal(worker.repairAnsweredBindings(),0);
 assert.equal(worker.restoreCloudBinding({ChatId:ids.chat},url),true);
 assert.equal(worker.route(ids.chat),url);
 assert.throws(()=>worker.restoreCloudBinding({ChatId:ids.chat},'https://chatgpt.com/c/aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee'),/conflict/i);
 assert.equal(worker.route(ids.chat),url);
 make('sessions',ids.legacy,{Id:ids.legacy,CreateCloudThread:false});
 assert.equal(worker.restoreCloudBinding({ChatId:ids.legacy},url),false);
 assert.equal(worker.route(ids.legacy),null);
 console.log('BROWSER_RECOVERY_BINDING_SAVED_AND_IDEMPOTENT_PASS');
}finally{fs.rmSync(root,{recursive:true,force:true});}
