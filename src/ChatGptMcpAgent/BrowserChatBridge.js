"use strict";
const fs=require("fs"), path=require("path"),os=require("os"),net=require("net");
const {spawn}=require("child_process"),{randomUUID}=require("crypto");
const root=process.env.CHATGPT_MCP_CHAT_TEST_ROOT||path.join(process.env.LOCALAPPDATA||path.join(os.homedir(),"AppData","Local"),"ChatGPTMCPAgent","Chat");
const dest=path.join(root,"browser-threads"), claims=path.join(root,"browser-claims"), messages=path.join(root,"messages"), sessions=path.join(root,"sessions"), forwarded=path.join(root,"ui-forwarded"),logs=path.join(root,"browser-bridge");
const browserExe=process.env.CHATGPT_MCP_BROWSER_EXE||"C:\\Program Files\\Yandex\\YandexBrowser\\Application\\browser.exe";
const profile=path.join(root,"dedicated-browser-profile"),id=/^[a-f0-9]{32}$/i;
const safeUrl=u=>typeof u==="string"&&/^https:\/\/chatgpt\.com\/c\/[a-f0-9-]{36}$/i.test(u)&&!u.includes("?")&&!u.includes("#");
const read=p=>{try{return JSON.parse(fs.readFileSync(p,"utf8"))}catch{return null}};
function save(p,obj){fs.mkdirSync(path.dirname(p),{recursive:true});const tmp=p+"."+randomUUID()+".tmp";fs.writeFileSync(tmp,JSON.stringify(obj),"utf8");fs.renameSync(tmp,p)}
function event(type,data={}){fs.mkdirSync(logs,{recursive:true});fs.appendFileSync(path.join(logs,"events.jsonl"),JSON.stringify({at:new Date().toISOString(),type,...data})+"\n")}
const pause=ms=>new Promise(r=>setTimeout(r,ms));
function pending(){
 if(!fs.existsSync(messages))return [];
 return fs.readdirSync(messages).filter(f=>/^[a-f0-9]{32}\.json$/i.test(f)).map(f=>read(path.join(messages,f))).filter(m=>
  m&&id.test(m.Id||"")&&id.test(m.ChatId||"")&&m.Status==="pending"&&typeof m.Text==="string"&&m.Text.trim()&&
  !(m.Attachments||[]).length&&!fs.existsSync(path.join(forwarded,m.Id+".json"))&&!fs.existsSync(path.join(claims,m.Id+".json"))
 ).sort((a,b)=>new Date(a.CreatedUtc||0)-new Date(b.CreatedUtc||0));
}
function route(chatId){
 const f=read(path.join(dest,chatId+".json"));
 if(safeUrl(f?.url))return f.url;
 const b=read(path.join(root,"thread-bindings",chatId+".json"));
 if(safeUrl(b?.Url))return b.Url;
 if(read(path.join(sessions,chatId+".json"))?.CreateCloudThread===true)return "https://chatgpt.com/";
 return null; // never create a cloud conversation for unbound existing chats
}
const selectors={editor:"#prompt-textarea,[data-testid='composer-text-input'],[contenteditable='true'][role='textbox']",
  send:"[data-testid='send-button'],button[aria-label='Send prompt'],button[aria-label='Отправить сообщение']",
  assistants:"[data-message-author-role='assistant']",stop:"[data-testid='stop-button']"};
const snapshot="(()=>{const e=document.querySelector("+JSON.stringify(selectors.editor)+");const a=[...document.querySelectorAll("+JSON.stringify(selectors.assistants)+")];return {url:location.href,ready:!!e,assistants:a.length,last:(a.at(-1)?.innerText||'').trim(),generating:!!document.querySelector("+JSON.stringify(selectors.stop)+")};})()";
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
  let target=tabs.find(t=>t.type==="page"&&t.url===url&&t.webSocketDebuggerUrl);
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
async function response(cdp,base){
 let last="",since=0;
 for(let i=0;i<170;i++){await pause(1200);let s;try{s=await cdp.eval(snapshot)}catch{continue}
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
  event("message_sent",{id:m.Id,chatId:m.ChatId});
  let answer=await response(cdp,first.assistants);
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
    const answer=await response(cdp,claim.baseline||0);
    const current=read(path.join(messages,m.Id+".json"));
    if(current?.Status!=="pending")continue;
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
async function cycle(){
 if(await recoverSent())return;
 for(let m of pending()){
  if(!route(m.ChatId)||Date.now()<(loginRetry.get(m.ChatId)||0))continue;
  try{await deliver(m)}catch(e){event("delivery_error",{id:m.Id,reason:String(e.message).slice(0,160)})}
  return;
 }
}
async function run(){
 event("worker_start",{pid:process.pid});
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
if(require.main===module){if(process.argv.includes("--selftest"))selftest();else run().catch(e=>{event("fatal",{reason:String(e)});process.exitCode=1})}
module.exports={pending,route,safeUrl,selftest};
