using System.Drawing;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;
namespace ChatGptMcpAgent;

internal static class VoiceDomTests
{
    internal static int Run()
    {
        var folder=Path.Combine(Path.GetTempPath(),"McpVoiceDom-"+Guid.NewGuid().ToString("N"));
        var webroot=Path.Combine(folder,"site");
        Directory.CreateDirectory(webroot);
        File.WriteAllText(Path.Combine(webroot,"test.html"),"""
<!doctype html><html><head><meta charset="utf-8"></head><body>
<form data-testid="composer">
 <div id="prompt-textarea" contenteditable="true"></div>
 <button type="button" data-testid="send-button" onclick="window.sent++">Send</button>
</form>
<div id="answer" data-message-author-role="assistant">
 <div class="markdown" id="answer-text">Прежний ответ</div>
</div>
<script>window.sent=0;</script>
</body></html>
""");
        var result=99;
        using var window=new Form{Width=720,Height=460,ShowInTaskbar=false,
            StartPosition=FormStartPosition.Manual,Location=new Point(-19000,-19000)};
        using var web=new WebView2{Dock=DockStyle.Fill};
        window.Controls.Add(web);
        using var timeout=new System.Windows.Forms.Timer{Interval=13000};
        timeout.Tick+=(_,_)=>{timeout.Stop();result=98;window.Close();};
        window.Shown+=async (_,_)=>{
            try{
                var env=await CoreWebView2Environment.CreateAsync(
                    userDataFolder:Path.Combine(folder,"profile"));
                await web.EnsureCoreWebView2Async(env);
                var core=web.CoreWebView2;
                core.SetVirtualHostNameToFolderMapping("chatgpt.com",webroot,
                    CoreWebView2HostResourceAccessKind.Allow);
                var navigation=new TaskCompletionSource<bool>(
                    TaskCreationOptions.RunContinuationsAsynchronously);
                core.NavigationCompleted+=(_,e)=>navigation.TrySetResult(e.IsSuccess);
                core.Navigate("https://chatgpt.com/test.html");
                if(!await navigation.Task)throw new InvalidOperationException("Mock not loaded");
                var events=0;
                var eventText="";
                core.WebMessageReceived+=(_,msg)=>{
                    var json=msg.WebMessageAsJson;
                    if(json.Contains("voice_reply",StringComparison.Ordinal)){
                        events++;
                        eventText=json;
                    }
                };
                var script=File.ReadAllText(Path.Combine(AppContext.BaseDirectory,
                    "Themes","ChatGPT-Voice.js"));
                await core.ExecuteScriptAsync(script);
                var value=await core.ExecuteScriptAsync("""
(() => window.__mcpVoiceBridge?.setComposer("Проверка голосового текста",false) === true &&
    document.getElementById('prompt-textarea').innerText.includes("Проверка голосового текста") &&
    window.sent===0)()
""");
                if(value!="true"){result=97;return;}
                await core.ExecuteScriptAsync("""
(() => {
 window.__mcpVoiceBridge.setEnabled(true);
 document.getElementById('answer-text').textContent="Новый ответ голосовой модели.";
})()
""");
                await Task.Delay(1700);
                if(events!=1||!eventText.Contains("Новый ответ голосовой модели.")){
                    result=96;return;
                }
                await core.ExecuteScriptAsync(
                    "document.getElementById('answer-text').textContent='Новый ответ голосовой модели.'");
                await Task.Delay(1250);
                if(events!=1){result=95;return;}
                await core.ExecuteScriptAsync(
                    "window.__mcpVoiceBridge.setComposer('Не отправлять автоматически',false)");
                result=await core.ExecuteScriptAsync("window.sent===0")=="true"?0:94;
            }catch(Exception e){
                Console.Error.WriteLine("VOICE_DOM_TEST_FAILED "+e.GetType().Name+" "+e.Message);
                result=93;
            }finally{timeout.Stop();window.Close();}
        };
        timeout.Start();
        Application.Run(window);
        try{Directory.Delete(folder,true);}catch(IOException){}catch(UnauthorizedAccessException){}
        return result;
    }
}
