using System.Text.Json;
using Microsoft.Web.WebView2.Core;

namespace ChatGptMcpAgent;

internal sealed partial class ChatGptMainForm
{
    private readonly VoicePreferences _voicePreferences=VoicePreferences.Load();
    private VoiceWorkerClient? _voiceWorker;
    private VoiceSettingsDialog? _voiceDialog;
    private bool _voiceRecording;
    private bool _voicePlaying;
    private string _voiceStatus="Голос готов к запуску";
    private string _lastVoiceReply="";
    private string _lastVoiceReplyUrl="";
    private readonly SemaphoreSlim _voiceCommands=new(1,1);

    private async Task<bool> EnsureVoiceWorkerAsync()
    {
        _voiceWorker??=new VoiceWorkerClient(OnVoiceWorkerMessage,
            error=>VoiceUiInvoke(()=>SetVoiceStatus(error)));
        if(!await _voiceWorker.EnsureStartedAsync()){
            SetVoiceStatus(_voiceWorker.InstallError??"Голосовой движок недоступен");
            return false;
        }
        return true;
    }

    private async Task HandleVoiceChromeCommandAsync(string action,JsonElement args)
    {
        switch(action){
            case "voice-settings":
                if(!await EnsureVoiceWorkerAsync())return;
                using(_voiceDialog=new VoiceSettingsDialog(_voicePreferences)){
                    await _voiceWorker!.SendAsync(new{command="devices"});
                    if(_voiceDialog.ShowDialog(this)==DialogResult.OK){
                        var config=_voiceDialog.Preferences;
                        _voicePreferences.Mode=config.Mode;
                        _voicePreferences.Microphone=config.Microphone;
                        _voicePreferences.Speaker=config.Speaker;
                        _voicePreferences.PauseMs=config.PauseMs;
                        _voicePreferences.Speed=config.Speed;
                        _voicePreferences.AutoSend=config.AutoSend;
                        _voicePreferences.AutoSpeak=config.AutoSpeak;
                        _voicePreferences.Save();
                        if(_voiceRecording) await StopVoiceAsync();
                        await UpdateVoiceEnabledOnTabsAsync();
                    }
                }
                _voiceDialog=null;
                PublishVoiceState();
                break;
            case "voice-toggle-speech":
                _voicePreferences.AutoSpeak=!_voicePreferences.AutoSpeak;
                _voicePreferences.Save();
                await UpdateVoiceEnabledOnTabsAsync();
                if(!_voicePreferences.AutoSpeak)
                    await (_voiceWorker?.SendAsync(new{command="cancel"})??Task.CompletedTask);
                SetVoiceStatus(_voicePreferences.AutoSpeak?
                    "Озвучивание ответов включено":"Озвучивание ответов выключено");
                break;
            case "voice-start":
                if(!await EnsureVoiceWorkerAsync())return;
                await _voiceCommands.WaitAsync();
                try{
                    if(_voiceRecording)return;
                    await _voiceWorker!.SendAsync(new{command="cancel"});
                    await _voiceWorker!.SendAsync(new{
                        command="start",microphone=_voicePreferences.Microphone,
                        pauseMs=_voicePreferences.PauseMs,
                        continuous=_voicePreferences.Mode=="continuous"
                    });
                    _voiceRecording=true;
                    SetVoiceStatus("Слушаю…");
                }finally{_voiceCommands.Release();}
                break;
            case "voice-stop":
                await StopVoiceAsync();
                break;
        }
    }

    private async Task StopVoiceAsync()
    {
        await _voiceCommands.WaitAsync();
        try{
            if(!_voiceRecording)return;
            _voiceRecording=false;
            if(_voiceWorker is not null)
                await _voiceWorker.SendAsync(new{command="stop"});
            SetVoiceStatus("Распознаю фразу…");
        }finally{_voiceCommands.Release();}
    }

    private void VoiceUiInvoke(Action action)
    {
        if(IsDisposed||Disposing||!IsHandleCreated)return;
        try{BeginInvoke((Action)(()=>{
            if(!IsDisposed&&!Disposing)action();
        }));}catch(InvalidOperationException){}
    }
    private void OnVoiceWorkerMessage(JsonElement message)
    {
        VoiceUiInvoke(async ()=>{
            try{
                if(!message.TryGetProperty("kind",out var kind))return;
                var type=kind.GetString();
                var data=message.TryGetProperty("data",out var payload)?payload:default;
                switch(type){
                    case "ready": SetVoiceStatus("Локальная голосовая модель готова");break;
                    case "devices":
                        if(_voiceDialog is {IsDisposed:false})_voiceDialog.SetDevices(data);
                        break;
                    case "recording":
                        if(data.ValueKind==JsonValueKind.Object&&
                           data.TryGetProperty("active",out var active))
                            _voiceRecording=active.GetBoolean();
                        PublishVoiceState();
                        break;
                    case "speaking":
                        _voicePlaying=true;PublishVoiceState();break;
                    case "speech_finished":
                    case "speech_cancelled":
                        _voicePlaying=false;PublishVoiceState();break;
                    case "recognized":
                        if(data.ValueKind!=JsonValueKind.Object||
                           !data.TryGetProperty("text",out var text))break;
                        var sentence=text.GetString();
                        if(string.IsNullOrWhiteSpace(sentence))break;
                        await InsertVoiceTextAsync(sentence);
                        break;
                    case "error":
                        string error=data.ValueKind==JsonValueKind.Object&&
                            data.TryGetProperty("message",out var e)?e.GetString()??"Ошибка":"Ошибка";
                        SetVoiceStatus("Ошибка голосового движка: "+
                            error[..Math.Min(error.Length,120)]);
                        _voiceRecording=false;
                        _voicePlaying=false;
                        PublishVoiceState();
                        break;
                }
            }catch(Exception e)when(e is JsonException or InvalidOperationException or
                System.Runtime.InteropServices.COMException or ObjectDisposedException){
                SetVoiceStatus("Ошибка обработки голосового события");
            }
        });
    }

    private async Task InsertVoiceTextAsync(string text)
    {
        var core=_active?.View.CoreWebView2;
        if(core is null||!core.Source.StartsWith("https://chatgpt.com/",StringComparison.OrdinalIgnoreCase)){
            SetVoiceStatus("Выбери вкладку ChatGPT для отправки речи");
            return;
        }
        var script="window.__mcpVoiceBridge?.setComposer("+
            JsonSerializer.Serialize(text)+","+
            (_voicePreferences.AutoSend?"true":"false")+") ?? false";
        try{
            var result=await core.ExecuteScriptAsync(script);
            SetVoiceStatus(result=="true"?
                (_voicePreferences.AutoSend?"Фраза отправляется":"Текст перенесён в поле чата"):
                "Поле ввода ChatGPT не найдено");
        }catch(Exception e)when(e is InvalidOperationException or
            System.Runtime.InteropServices.COMException){
            SetVoiceStatus("Не удалось перенести фразу в ChatGPT");
        }
    }

    private async Task HandleVoiceWebMessageAsync(BrowserTab tab,JsonElement data)
    {
        if(!_voicePreferences.AutoSpeak || tab!=_active)return;
        if(!data.TryGetProperty("mcp_kind",out var kind)||
            kind.GetString()!="voice_reply")return;
        var url=data.TryGetProperty("url",out var u)?u.GetString():"";
        var text=data.TryGetProperty("text",out var t)?t.GetString():"";
        if(string.IsNullOrWhiteSpace(url)||string.IsNullOrWhiteSpace(text))return;
        if(!url.StartsWith("https://chatgpt.com/",StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(url,tab.View.CoreWebView2?.Source,StringComparison.Ordinal))return;

        // Serialize dispatch so concurrent DOM events cannot launch two voice
        // workers or cause duplicate speech on an unloaded/reloaded chat.
        await _voiceCommands.WaitAsync();
        try {
            if(!_voicePreferences.AutoSpeak || tab!=_active)return;
            if(text==_lastVoiceReply && url==_lastVoiceReplyUrl)return;
            // Text-to-speech must start independently of microphone capture.
            if(!await EnsureVoiceWorkerAsync())return;
            await _voiceWorker!.SendAsync(new{command="speak",
                text=text[..Math.Min(text.Length,2400)],
                speed=_voicePreferences.Speed,speaker=_voicePreferences.Speaker,
                id=Guid.NewGuid().ToString("N")});
            _lastVoiceReply=text;
            _lastVoiceReplyUrl=url;
        } finally { _voiceCommands.Release(); }
    }
    private async Task UpdateVoiceEnabledOnTabsAsync()
    {
        foreach(var tab in _pages.ToArray()){
            if(tab.View.IsDisposed||tab.View.CoreWebView2 is not { } core)continue;
            if(!core.Source.StartsWith("https://chatgpt.com/",StringComparison.OrdinalIgnoreCase))continue;
            try{await core.ExecuteScriptAsync("window.__mcpVoiceBridge?.setEnabled("+
                (_voicePreferences.AutoSpeak?"true":"false")+")");}
            catch(Exception e)when(e is InvalidOperationException or
                System.Runtime.InteropServices.COMException){}
        }
    }

    private void SetVoiceStatus(string value)
    {
        _voiceStatus=value;
        PublishVoiceState();
    }
    private void PublishVoiceState()
    {
        if(IsDisposed||!_chromeNavigated||_chrome.CoreWebView2 is null)return;
        try{
            var state=JsonSerializer.Serialize(new{
                recording=_voiceRecording,playing=_voicePlaying,
                mode=_voicePreferences.Mode,autoSpeak=_voicePreferences.AutoSpeak,
                status=_voiceStatus
            });
            _ = _chrome.CoreWebView2.ExecuteScriptAsync(
                "window.applyVoiceState?.("+state+")");
        }catch(Exception e)when(e is InvalidOperationException or
            System.Runtime.InteropServices.COMException){}
    }
    private void ShutdownVoice()
    {
        try{_voiceWorker?.Dispose();}
        catch(Exception e)when(e is InvalidOperationException or IOException){}
        _voiceWorker=null;
    }
}
