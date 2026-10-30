"use strict";
const fs=require("fs"), path=require("path"),os=require("os"),net=require("net");
const {spawn}=require("child_process"),{randomUUID,createHash}=require("crypto");
const root=process.env.CHATGPT_MCP_CHAT_TEST_ROOT||path.join(process.env.LOCALAPPDATA||path.join(os.homedir(),"AppData","Local"),"ChatGPTMCPAgent","Chat");
const dest=path.join(root,"browser-threads"), claims=path.join(root,"browser-claims"), messages=path.join(root,"messages"), sessions=path.join(root,"sessions"), forwarded=path.join(root,"ui-forwarded"),logs=path.join(root,"browser-bridge");
const browserExe=process.env.CHATGPT_MCP_BROWSER_EXE||"C:\\Program Files\\Yandex\\YandexBrowser\\Application\\browser.exe";
const profile=path.join(root,"dedicated-browser-profile"),id=/^[a-f0-9]{32}$/i;
const catalogFile=path.join(logs,"catalog.json"),activationFile=path.join(logs,"activated.json");
const historyDir=path.join(root,"browser-history"),partialDir=path.join(root,"browser-partials");
const loginRequest=path.join(logs,"login-request.json");

function readActivatedTime(){try{return JSON.parse(fs.readFileSync(activationFile,"utf8")).at||null}catch{return null}}
const safeUrl=u=>typeof u==="string"&&/^https:\/\/chatgpt\.com\/c\/[a-f0-9]{8}-[a-f0-9]{4}-[a-f0-9]{4}-[a-f0-9]{4}-[a-f0-9]{12}$/i.test(u);
const read=p=>{try{return JSON.parse(fs.readFileSync(p,"utf8"))}catch{return null}};
function save(p,obj){fs.mkdirSync(path.dirname(p),{recursive:true});const tmp=p+"."+randomUUID()+".tmp";fs.writeFileSync(tmp,JSON.stringify(obj),"utf8");fs.renameSync(tmp,p)}
function event(type,data={}){fs.mkdirSync(logs,{recursive:true});fs.appendFileSync(path.join(logs,"events.jsonl"),JSON.stringify({at:new Date().toISOString(),type,...data})+"\n")}
const pause=ms=>new Promise(r=>setTimeout(r,ms));
function pending(){
 if(!fs.existsSync(messages))return [];
 const startedAt=readActivatedTime();
 return fs.readdirSync(messages).filter(f=>/^[a-f0-9]{32}\.json$/i.test(f)).map(f=>read(path.join(messages,f))).filter(m=>
  m&&id.test(m.Id||"")&&id.test(m.ChatId||"")&&m.Status==="pending"&&typeof m.Text==="string"&&(m.Text.trim()||(m.Attachments||[]).length>0)&&
  (!startedAt||Date.parse(m.CreatedUtc||0)>=Date.parse(startedAt))&&
  !fs.existsSync(path.join(forwarded,m.Id+".json"))&&!fs.existsSync(path.join(claims,m.Id+".json"))
 ).sort((a,b)=>new Date(a.CreatedUtc||0)-new Date(b.CreatedUtc||0));
}
function restoreCloudBinding(m,url){
 if(!m||!id.test(m.ChatId||"")||!safeUrl(url))return false;
 const session=read(path.join(sessions,m.ChatId+".json"));
 if(session?.CreateCloudThread!==true)return false;
 const file=path.join(dest,m.ChatId+".json");
 const existing=read(file);
 if(existing?.url && existing.url!==url)throw Error("Cloud chat binding conflict");
 if(!safeUrl(existing?.url))save(file,{chatId:m.ChatId,url});
 return true;
}
function repairAnsweredBindings(){
 if(!fs.existsSync(claims))return 0;
 let repaired=0;
 for(const entry of fs.readdirSync(claims).filter(f=>/^[a-f0-9]{32}\.json$/i.test(f))){
   const claim=read(path.join(claims,entry));
   if(claim?.state!=="answered"||!safeUrl(claim.url))continue;
   const m=read(path.join(messages,claim.id+".json"));
   if(m?.Status!=="answered"||m?.ChatId!==claim.chatId)continue;
   try{
     const before=read(path.join(dest,m.ChatId+".json"));
     if(restoreCloudBinding(m,claim.url)&&!safeUrl(before?.url))repaired++;
   }catch(e){event("binding_repair_error",{chatId:m.ChatId,reason:String(e.message).slice(0,110)})}
 }
 return repaired;
}
function route(chatId){
 const f=read(path.join(dest,chatId+".json"));
 if(safeUrl(f?.url))return f.url;
 const b=read(path.join(root,"thread-bindings",chatId+".json"));
 if(safeUrl(b?.Url))return b.Url;
 const session=read(path.join(sessions,chatId+".json"));
 if(safeUrl(session?.CloudUrl))return session.CloudUrl;
 if(session?.CreateCloudThread===true)return "https://chatgpt.com/";
 return null; // never create a cloud conversation for unbound existing chats
}
const selectors={editor:"#prompt-textarea,[data-testid='composer-text-input'],[contenteditable='true'][role='textbox']",
  send:"[data-testid='send-button'],button[aria-label='Send prompt'],button[aria-label='Отправить сообщение']",
  assistants:"[data-message-author-role='assistant']",stop:"[data-testid='stop-button']"};
// ChatGPT 2026 has h4[data-conversation-role=assistant] and MarkdownRoot;
 // older pages have data-message-author-role. Handle both without reading cookies.
const snapshot="(()=>{const e=document.querySelector("+JSON.stringify(selectors.editor)+");const legacy=[...document.querySelectorAll("+JSON.stringify(selectors.assistants)+")];const modern=[...document.querySelectorAll('h4[data-conversation-role=assistant]')];let last='';if(modern.length){const p=modern.at(-1).parentElement;const body=p?.querySelector('[data-markdown-text-style=assistant-message]');last=(body?.innerText||p?.innerText?.replace(/^ChatGPT[^\\n]*\\n/,'')||'').trim()}else last=(legacy.at(-1)?.innerText||'').trim();return {url:location.href,ready:!!e,assistants:modern.length||legacy.length,last,generating:!!document.querySelector("+JSON.stringify(selectors.stop)+")||!!document.querySelector('button[aria-label^=Stop],button[aria-label^=Остановить]')};})()";
async function attachFiles(cdp,m){
 const attachments=m.Attachments||[];
 if(attachments.length===0)return;
 if(attachments.length>20)throw Error("Too many attachments");
 const files=attachments.map(a=>{
   if(!id.test(a.Id||"")||!id.test(m.Id||""))throw Error("Invalid attachment ID");
   const file=path.join(root,"attachments",m.Id,a.Id);
   const stat=fs.statSync(file);
   if(!stat.isFile()||stat.size!==a.SizeBytes)throw Error("Attachment size mismatch");
   if(createHash("sha256").update(fs.readFileSync(file)).digest("hex").toLowerCase()!==String(a.Sha256||"").toLowerCase())
     throw Error("Attachment integrity failed");
   return file;
 });
 let document=await cdp.call("DOM.getDocument",{depth:1,pierce:true});
 let node=await cdp.call("DOM.querySelector",{nodeId:document.root.nodeId,selector:"input[type=file]"});
 if(!node.nodeId){
   await cdp.eval("(() => { const b=document.querySelector('[data-testid=composer-plus-btn],button[aria-label*=Attach]');if(b)b.click();return !!b;})()");
   await pause(500);
   document=await cdp.call("DOM.getDocument",{depth:1,pierce:true});
   node=await cdp.call("DOM.querySelector",{nodeId:document.root.nodeId,selector:"input[type=file]"});
 }
 if(!node.nodeId)throw Error("ChatGPT file input not found; attachment was not sent");
 await cdp.call("DOM.setFileInputFiles",{files,nodeId:node.nodeId});
 await pause(2200);
 event("attachment_upload_started",{id:m.Id,count:files.length});
}
function canonicalItems(items){
 const found=new Map();
 for(const item of items){
   if(safeUrl(item.url)&&!found.has(item.url))
     found.set(item.url,{url:item.url,title:String(item.title||"Без названия").slice(0,120)});
 }
 return Array.from(found.values()).slice(0,300);
}
async function scanCatalog(){
 await browser.ensure();
 const cdp=await browser.tab("https://chatgpt.com/");
 try{
   const snap=await waitReady(cdp);
   if(!snap)return false;
   save(path.join(logs,"auth-ready.json"),{confirmedAt:new Date().toISOString()});
   const expr="(() => Array.from(document.querySelectorAll('a[href^=\"/c/\"]')).map(a=>({url:new URL(a.getAttribute('href'),location.origin).origin+new URL(a.getAttribute('href'),location.origin).pathname,title:(a.innerText||a.getAttribute('aria-label')||'').trim()})))()";
   const found=await cdp.eval(expr);
   save(catalogFile,{at:new Date().toISOString(),items:canonicalItems(found||[])});
   return true;
 }finally{cdp.close()}
}
async function syncImportedHistory(){
 if(!fs.existsSync(sessions))return false;
 for(const file of fs.readdirSync(sessions).filter(f=>id.test(f.slice(0,-5))&&f.endsWith(".json"))){
   const session=read(path.join(sessions,file));
   if(!safeUrl(session?.CloudUrl))continue;
   const dest=path.join(historyDir,session.Id+".json");
   if(fs.existsSync(dest))continue;
   await browser.ensure();
   const cdp=await browser.tab(session.CloudUrl);
   try{
     const snap=await waitReady(cdp);
     if(!snap||snap.url!==session.CloudUrl)continue;
     const expr="(() => Array.from(document.querySelectorAll('[data-message-author-role]')).filter(e=>['user','assistant'].includes(e.getAttribute('data-message-author-role'))).map(e=>({role:e.getAttribute('data-message-author-role'),text:(e.innerText||'').trim()})).filter(e=>e.text))()";
     const items=await cdp.eval(expr);
     if(!Array.isArray(items)||items.length===0)continue;
     save(dest,{at:new Date().toISOString(),url:session.CloudUrl,items});
     event("import_history_saved",{chatId:session.Id,items:items.length});
     return true;
   }finally{cdp.close()}
 }
 return false;
}
class Cdp {
 constructor(ws){this.ws=ws;this.id=0;this.pending=new Map();ws.addEventListener("message",e=>{try{const m=JSON.parse(e.data),w=this.pending.get(m.id);if(!w)return;this.pending.delete(m.id);m.error?w.reject(Error(m.error.message)):w.resolve(m.result)}catch{}})}
 static async connect(uri){const ws=new WebSocket(uri);await new Promise((ok,no)=>{let t=setTimeout(()=>no(Error("websocket timeout")),12000);ws.addEventListener("open",()=>{clearTimeout(t);ok()},{once:true});ws.addEventListener("error",()=>{clearTimeout(t);no(Error("websocket failed"))},{once:true})});return new Cdp(ws)}
 call(method,params={}){let id=++this.id;return new Promise((resolve,reject)=>{let timer=setTimeout(()=>{this.pending.delete(id);reject(Error("CDP timeout: "+method))},15000);this.pending.set(id,{resolve:v=>{clearTimeout(timer);resolve(v)},reject:e=>{clearTimeout(timer);reject(e)}});this.ws.send(JSON.stringify({id,method,params}))})}
 async eval(expr){let r=await this.call("Runtime.evaluate",{expression:expr,returnByValue:true,awaitPromise:true});if(r.exceptionDetails)throw Error("DOM evaluation failed");return r.result?.value}
 close(){try{this.ws.close()}catch{}}
}
class Browser {
 constructor(){this.port=0}
 async endpoint(route){let res=await fetch("http://127.0.0.1:"+this.port+route,{method:route.startsWith("/json/new")?"PUT":"GET",signal:AbortSignal.timeout(5000)});if(!res.ok)throw Error("CDP HTTP "+res.status);return res.json()}
 async ensure(){
  if(this.port){try{await this.endpoint("/json/version");return}catch{}}
  let state=read(path.join(logs,"browser-state.json"));this.port=state?.port||0;
  if(this.port){try{await this.endpoint("/json/version");return}catch{}}
  if(!fs.existsSync(browserExe))throw Error("Yandex Browser not found");
  const server=net.createServer();this.port=await new Promise((ok,no)=>{server.on("error",no);server.listen(0,"127.0.0.1",()=>{let p=server.address().port;server.close(()=>ok(p))})});
  fs.mkdirSync(profile,{recursive:true});
  let args=["--user-data-dir="+profile,"--remote-debugging-address=127.0.0.1","--remote-debugging-port="+this.port,"--no-first-run","--disable-background-timer-throttling","--disable-renderer-backgrounding","--new-window","https://chatgpt.com/"];
  if(fs.existsSync(path.join(logs,"auth-ready.json")))args.push("--start-minimized");
  spawn(browserExe,args,{detached:true,stdio:"ignore"}).unref();save(path.join(logs,"browser-state.json"),{port:this.port});event("browser_opened");
  for(let i=0;i<40;i++){await pause(750);try{await this.endpoint("/json/version");return}catch{}}
  throw Error("Browser did not start local debugging session");
 }
 async tab(url){
  const tabs=await this.endpoint("/json/list");
  const candidates=tabs.filter(t=>t.type==="page"&&t.url===url&&t.webSocketDebuggerUrl);
  // A stale pre-login tab can coexist with an authenticated tab at the same URL.
  // Never use it for new conversation delivery.
  if(url==="https://chatgpt.com/"){
    for(const target of candidates){
      let cdp;
      try{
        cdp=await Cdp.connect(target.webSocketDebuggerUrl);
        if((await cdp.eval(snapshot))?.ready)return cdp;
      }catch(e){event("stale_home_tab",{reason:String(e.message).slice(0,90)})}
      cdp?.close();
    }
    const target=await this.endpoint("/json/new?"+encodeURIComponent(url));
    if(!target.webSocketDebuggerUrl)throw Error("No browser target");
    return Cdp.connect(target.webSocketDebuggerUrl);
  }
  let target=candidates[0];
  if(!target)target=await this.endpoint("/json/new?"+encodeURIComponent(url));
  if(!target.webSocketDebuggerUrl)throw Error("No browser target");
  return Cdp.connect(target.webSocketDebuggerUrl);
 }
}
let browser=new Browser();
const loginRetry=new Map();
async function waitReady(cdp){
 for(let i=0;i<60;i++){let s=await cdp.eval(snapshot);if(s.ready)return s;await pause(1000)}
 return null;
}
async function send(cdp,text){
 let focused=await cdp.eval("(()=>{let el=document.querySelector("+JSON.stringify(selectors.editor)+");if(!el)return false;el.focus();return document.activeElement===el||el.contains(document.activeElement)})()");
 if(!focused)throw Error("Cannot focus composer");
 await cdp.call("Input.insertText",{text});await pause(350);
 let value=await cdp.eval("document.querySelector("+JSON.stringify(selectors.editor)+")?.innerText||''");
 if(!value.includes(text))throw Error("Composer text not confirmed");
 let sent=await cdp.eval("(()=>{let b=document.querySelector("+JSON.stringify(selectors.send)+");if(!b||b.disabled)return false;b.click();return true})()");
 if(!sent){await cdp.call("Input.dispatchKeyEvent",{type:"keyDown",key:"Enter",code:"Enter",windowsVirtualKeyCode:13});await cdp.call("Input.dispatchKeyEvent",{type:"keyUp",key:"Enter",code:"Enter",windowsVirtualKeyCode:13})}
}
async function response(cdp,base,messageId){
 let last="",since=0;
 for(let i=0;i<170;i++){await pause(1200);let s;try{s=await cdp.eval(snapshot)}catch{continue}
  if(messageId&&s.assistants>base&&s.last)
    save(path.join(partialDir,messageId+".json"),{text:s.last,at:new Date().toISOString()});
  if(s.assistants>base&&s.last&&!s.generating){
   if(s.last!==last){last=s.last;since=Date.now()}else if(Date.now()-since>3000)return {text:last,url:s.url}
  }else since=0;
 }
 throw Error("Response not confirmed; retained claim to prevent duplicate send");
}
async function deliver(m){
 let url=route(m.ChatId);if(!url)return;
 await browser.ensure();let cdp=await browser.tab(url);
 try{
  let first=await waitReady(cdp);
  if(!first){
   loginRetry.set(m.ChatId,Date.now()+60000);
   const authenticated=path.join(logs,"auth-ready.json");
   if(fs.existsSync(authenticated))fs.unlinkSync(authenticated);
   event("needs_initial_browser_login",{chatId:m.ChatId});
   return;
  }
  loginRetry.delete(m.ChatId);
  save(path.join(logs,"auth-ready.json"),{confirmedAt:new Date().toISOString()});
  if(url!=="https://chatgpt.com/"&&first.url!==url)throw Error("Existing cloud chat redirected");
  await attachFiles(cdp,m);
  let marker=path.join(claims,m.Id+".json");
  save(marker,{id:m.Id,chatId:m.ChatId,state:"reserved",at:new Date().toISOString()});
  await send(cdp,m.Text);
  // Capture the durable /c/<uuid> URL immediately after send, before
  // waiting for model completion. A restarted worker can then recover the reply.
  let conversationUrl=first.url;
  for(let attempt=0;attempt<25;attempt++){
    let snap=await cdp.eval(snapshot);
    if(safeUrl(snap?.url)){conversationUrl=snap.url;break}
    await pause(400);
  }
  save(marker,{id:m.Id,chatId:m.ChatId,state:"sent",
    baseline:first.assistants,url:conversationUrl,at:new Date().toISOString()});
  // Save the /c/ binding as soon as the cloud URL is confirmed,
  // even if the browser worker dies before the model finishes its answer.
  if(url==="https://chatgpt.com/"&&safeUrl(conversationUrl))
    restoreCloudBinding(m,conversationUrl);
  event("message_sent",{id:m.Id,chatId:m.ChatId});
  let answer=await response(cdp,first.assistants,m.Id);
  if(url==="https://chatgpt.com/"){
   if(!safeUrl(answer.url))throw Error("New conversation URL not confirmed");
   save(path.join(dest,m.ChatId+".json"),{chatId:m.ChatId,url:answer.url});
  }
  const f=path.join(messages,m.Id+".json"),current=read(f);
  if(!current||current.Status!=="pending"||current.ChatId!==m.ChatId)throw Error("Queue entry changed");
  current.Reply=answer.text;current.RepliedUtc=new Date().toISOString();current.Status="answered";save(f,current);
  save(marker,{id:m.Id,chatId:m.ChatId,state:"answered",at:new Date().toISOString()});
  event("answer_saved",{id:m.Id,chatId:m.ChatId,length:answer.text.length});
 }finally{cdp.close()}
}
async function recoverSent(){
 if(!fs.existsSync(claims))return false;
 for(const entry of fs.readdirSync(claims).filter(f=>/^[a-f0-9]{32}\.json$/i.test(f))){
  const claim=read(path.join(claims,entry));
  if(claim?.state!=="sent"||!safeUrl(claim.url))continue;
  const m=read(path.join(messages,claim.id+".json"));
  if(!m||m.Status!=="pending"||m.ChatId!==claim.chatId)continue;
  if(Date.now()-Date.parse(claim.at||0)<30000)continue;
  try{
   await browser.ensure();
   let cdp=await browser.tab(claim.url);
   try{
    let snap=await waitReady(cdp);
    if(!snap||snap.url!==claim.url)continue;
    const answer=await response(cdp,claim.baseline||0,m.Id);
    const current=read(path.join(messages,m.Id+".json"));
    if(current?.Status!=="pending")continue;
    restoreCloudBinding(m,claim.url);
    current.Reply=answer.text;current.RepliedUtc=new Date().toISOString();current.Status="answered";
    save(path.join(messages,m.Id+".json"),current);
    save(path.join(claims,m.Id+".json"),{...claim,state:"answered",recoveredAt:new Date().toISOString()});
    event("reply_recovered",{id:m.Id});
    return true;
   }finally{cdp.close()}
  }catch(e){event("recovery_retry",{id:m.Id,reason:String(e.message).slice(0,100)})}
  return true;
 }
 return false;
}
let lastCatalogAt=0;
async function cycle(){
 // Reply/recovery and new user messages have priority over catalog maintenance.
 if(await recoverSent())return;
 for(const m of pending()){
  if(!route(m.ChatId)||Date.now()<(loginRetry.get(m.ChatId)||0))continue;
  try{await deliver(m)}catch(e){event("delivery_error",{id:m.Id,reason:String(e.message).slice(0,180)})}
  return;
 }
 if(Date.now()-lastCatalogAt>90000 && fs.existsSync(loginRequest)){
   lastCatalogAt=Date.now();
   try{await scanCatalog()}catch(e){event("catalog_error",{reason:String(e.message).slice(0,150)})}
 }
 if(fs.existsSync(path.join(logs,"auth-ready.json"))){
   try{await syncImportedHistory()}catch(e){event("history_sync_error",{reason:String(e.message).slice(0,130)})}
 }
}

async function run(){
 if(!fs.existsSync(activationFile))save(activationFile,{at:new Date().toISOString()});
 const repaired=repairAnsweredBindings();
 event("worker_start",{pid:process.pid,bindingsRepaired:repaired});
 // This timer must not pause while waiting for login or model streaming.
 const heartbeat=()=>save(path.join(logs,"heartbeat.json"),
   {at:new Date().toISOString(),pid:process.pid,
    mode:fs.existsSync(path.join(logs,"auth-ready.json"))?"browser":"waiting_login"});
 heartbeat();
 const timer=setInterval(heartbeat,2500);
 try{
  for(;;){
   try{await cycle()}catch(e){event("worker_error",{reason:String(e.message).slice(0,160)})}
   await pause(1800);
  }
 }finally{clearInterval(timer)}
}
function selftest(){
 if(!safeUrl("https://chatgpt.com/c/01234567-89ab-cdef-0123-456789abcdef"))throw Error("safe URL rejected");
 if(safeUrl("https://chatgpt.com.evil.test/c/01234567-89ab-cdef-0123-456789abcdef"))throw Error("unsafe URL accepted");
 if(!snapshot.includes("assistants"))throw Error("probe missing");
 console.log("BROWSER_BRIDGE_ISOLATED_SELFTEST_OK");
}
if(require.main===module){
 if(process.argv.includes("--selftest"))selftest();
 else if(process.argv.includes("--login")){
   save(loginRequest,{at:new Date().toISOString()});
   browser.ensure().then(async()=>{try{
     const view=await browser.tab("https://chatgpt.com/"); view.close();
   const pages=await browser.endpoint("/json/list");
     const home=pages.find(p=>p.type==="page"&&p.url.startsWith("https://chatgpt.com/"));
     if(home?.id)await fetch("http://127.0.0.1:"+browser.port+"/json/activate/"+home.id);
   }catch(e){event("login_activate_error",{reason:String(e.message)})}})
     .catch(e=>{event("login_start_error",{reason:String(e.message)});process.exitCode=1});
 }else run().catch(e=>{event("fatal",{reason:String(e)});process.exitCode=1});
}
module.exports={pending,route,safeUrl,selftest,canonicalItems,snapshot,restoreCloudBinding,repairAnsweredBindings};
