using System.Diagnostics;
using System.Drawing;
using System.Text;
using System.Text.Json;
using System.Windows.Forms;
using Microsoft.Web.WebView2.Core;

namespace ChatGptMcpAgent;

internal sealed class VoicePreferences
{
    public string Mode { get; set; } = "push";
    public int PauseMs { get; set; } = 700;
    public int Microphone { get; set; } = -1;
    public int Speaker { get; set; } = -1;
    public bool AutoSend { get; set; } = false;
    public bool AutoSpeak { get; set; } = false;
    public float Speed { get; set; } = 1f;

    internal static string SettingsFile => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "ChatGPTMCPAgent","Voice","settings.json");

    internal static VoicePreferences Load()
    {
        try {
            if(File.Exists(SettingsFile)) {
                var result = JsonSerializer.Deserialize<VoicePreferences>(File.ReadAllText(SettingsFile));
                if(result is not null) return result.Sanitize();
            }
        } catch (Exception e) when (e is IOException or JsonException or UnauthorizedAccessException) { }
        return new VoicePreferences();
    }
    internal VoicePreferences Sanitize()
    {
        if(Mode is not ("push" or "continuous")) Mode="push";
        PauseMs=Math.Clamp(PauseMs,300,1500);
        Speed=Math.Clamp(Speed,.7f,1.3f);
        Microphone=Math.Max(-1,Microphone);
        Speaker=Math.Max(-1,Speaker);
        return this;
    }
    internal void Save()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(SettingsFile)!);
        var tmp=SettingsFile+".tmp";
        File.WriteAllText(tmp,JsonSerializer.Serialize(Sanitize(),new JsonSerializerOptions{WriteIndented=true}));
        File.Move(tmp,SettingsFile,true);
    }
}

internal sealed class VoiceWorkerClient : IDisposable
{
    private Process? _process;
    private readonly SemaphoreSlim _writer=new(1,1);
    private readonly Action<JsonElement> _onMessage;
    private readonly Action<string> _onError;
    internal bool Ready { get; private set; }
    internal int ProcessId => _process?.Id ?? 0;
    internal string? InstallError { get; private set; }
    internal static string Folder => Path.Combine(AppContext.BaseDirectory,"Voice");
    internal static string Exe => Path.Combine(Folder,"ChatGptMcpVoice.exe");
    internal static string ModelFolder => Path.Combine(Folder,"Models");
    internal VoiceWorkerClient(Action<JsonElement> onMessage,Action<string> onError)
    { _onMessage=onMessage;_onError=onError; }

    internal async Task<bool> EnsureStartedAsync()
    {
        if(_process is { HasExited:false })return true;
        if(!File.Exists(Exe)){InstallError="Отсутствует голосовой модуль";return false;}
        if(!File.Exists(Path.Combine(ModelFolder,"silero_vad.onnx"))){
            InstallError="Не установлены голосовые модели";return false;
        }
        var start = new ProcessStartInfo(Exe)
        {
            WorkingDirectory=Folder,
            UseShellExecute=false,
            RedirectStandardInput=true,
            RedirectStandardOutput=true,
            RedirectStandardError=true,
            CreateNoWindow=true
        };
        start.ArgumentList.Add("--stdio");
        start.ArgumentList.Add("--models");
        start.ArgumentList.Add(ModelFolder);
        try{
            var process=new Process{StartInfo=start,EnableRaisingEvents=true};
            if(!process.Start()){InstallError="Не удалось запустить голосовой модуль";return false;}
            _process=process;
            _ = Task.Run(async()=>{
                try {
                    while (await process.StandardOutput.ReadLineAsync() is { } line){
                        try {
                            using var doc=JsonDocument.Parse(line);
                            _onMessage(doc.RootElement.Clone());
                        } catch(JsonException) { }
                    }
                }catch(Exception e)when(e is IOException or ObjectDisposedException){ }
            });
            _ = Task.Run(async()=>{
                try{
                    while(await process.StandardError.ReadLineAsync() is { } line){
                        if(line.StartsWith("VOICE_FATAL",StringComparison.Ordinal))
                            _onError("Ошибка голосового модуля: "+line[..Math.Min(line.Length,250)]);
                    }
                }catch(Exception e)when(e is IOException or ObjectDisposedException){ }
            });
            await Task.Yield();
            return true;
        }catch(Exception e)when(e is IOException or System.ComponentModel.Win32Exception or InvalidOperationException){
            InstallError=e.Message;return false;
        }
    }
    internal async Task SendAsync(object payload)
    {
        if(!await EnsureStartedAsync())return;
        await _writer.WaitAsync();
        try{
            if(_process is {HasExited:false})
            {
                await _process.StandardInput.WriteLineAsync(JsonSerializer.Serialize(payload));
                await _process.StandardInput.FlushAsync();
            }
        }catch(Exception e)when(e is IOException or InvalidOperationException or ObjectDisposedException){}
        finally{_writer.Release();}
    }
    public void Dispose()
    {
        try{
            if(_process is {HasExited:false}){
                _process.StandardInput.WriteLine("{\"command\":\"quit\"}");
                if(!_process.WaitForExit(1000))_process.Kill(entireProcessTree:true);
            }
        }catch(Exception e)when(e is InvalidOperationException or IOException){}
        finally{_process?.Dispose();_process=null;}
        _writer.Dispose();
    }
}

internal sealed class VoiceSettingsDialog : Form
{
    internal readonly VoicePreferences Preferences;
    private readonly ComboBox _microphones = new(){DropDownStyle=ComboBoxStyle.DropDownList,Width=320};
    private readonly ComboBox _speakers = new(){DropDownStyle=ComboBoxStyle.DropDownList,Width=320};
    internal VoiceSettingsDialog(VoicePreferences current)
    {
        Preferences=new VoicePreferences {
            Mode=current.Mode,PauseMs=current.PauseMs,Microphone=current.Microphone,
            Speaker=current.Speaker,AutoSend=current.AutoSend,AutoSpeak=current.AutoSpeak,Speed=current.Speed
        };
        Text="Настройки голоса — ChatGPT MCP Tunnel";
        Width=540;Height=570;MinimumSize=new Size(510,530);
        StartPosition=FormStartPosition.CenterParent;
        Font=new Font("Segoe UI",10);
        BackColor=AppTheme.Background;ForeColor=AppTheme.Text;
        var panel=new FlowLayoutPanel {
            Dock=DockStyle.Fill,FlowDirection=FlowDirection.TopDown,
            AutoScroll=true,WrapContents=false,Padding=new Padding(20),BackColor=AppTheme.Background
        };
        Controls.Add(panel);
        void Label(string value){
            panel.Controls.Add(new Label{Text=value,AutoSize=true,Margin=new Padding(0,9,0,5),ForeColor=AppTheme.Text});
        }
        void Configure(ComboBox cb){
            cb.BackColor=AppTheme.Surface;cb.ForeColor=AppTheme.Text;
            cb.FlatStyle=FlatStyle.Flat;cb.Margin=new Padding(0,0,0,5);
        }
        Label("Микрофон");
        Configure(_microphones);panel.Controls.Add(_microphones);
        Label("Устройство воспроизведения");
        Configure(_speakers);panel.Controls.Add(_speakers);
        var mode=new ComboBox{DropDownStyle=ComboBoxStyle.DropDownList,Width=320};
        Configure(mode);
        mode.Items.AddRange(new object[]{"Удерживать кнопку","Непрерывное прослушивание"});
        mode.SelectedIndex=Preferences.Mode=="continuous"?1:0;
        Label("Режим микрофона");
        panel.Controls.Add(mode);
        Label("Пауза после фразы (300–1500 мс)");
        var pause=new NumericUpDown{Minimum=300,Maximum=1500,Increment=100,Value=Preferences.PauseMs,Width=120};
        panel.Controls.Add(pause);
        Label("Скорость озвучивания (0,7–1,3)");
        var speed=new NumericUpDown{Minimum=0.7m,Maximum=1.3m,DecimalPlaces=1,Increment=0.1m,Value=(decimal)Preferences.Speed,Width=120};
        panel.Controls.Add(speed);
        var autoSend=new CheckBox{Text="Автоматически отправлять распознанный текст",Checked=Preferences.AutoSend,AutoSize=true,ForeColor=AppTheme.Text,Margin=new Padding(0,12,0,4)};
        var autoSpeak=new CheckBox{Text="Автоматически озвучивать ответы ChatGPT",Checked=Preferences.AutoSpeak,AutoSize=true,ForeColor=AppTheme.Text,Margin=new Padding(0,3,0,8)};
        panel.Controls.Add(autoSend);panel.Controls.Add(autoSpeak);
        var info=new Label {
            Text="Модели работают локально. Без автоотправки текст сначала появится в поле ввода ChatGPT.",
            Width=440,Height=48,ForeColor=AppTheme.Secondary
        };
        panel.Controls.Add(info);
        var save=new Button{Text="Сохранить",Width=115,Height=36,
            BackColor=AppTheme.Selected,ForeColor=AppTheme.Text,FlatStyle=FlatStyle.Flat};
        save.Click+=(_,_)=>{
            Preferences.Mode=mode.SelectedIndex==1?"continuous":"push";
            Preferences.PauseMs=(int)pause.Value;Preferences.Speed=(float)speed.Value;
            Preferences.AutoSend=autoSend.Checked;Preferences.AutoSpeak=autoSpeak.Checked;
            Preferences.Microphone=_microphones.SelectedItem is DeviceOption m?m.Index:-1;
            Preferences.Speaker=_speakers.SelectedItem is DeviceOption s?s.Index:-1;
            DialogResult=DialogResult.OK;Close();
        };
        panel.Controls.Add(save);
        SetDevices(null);
    }
    internal void SetDevices(JsonElement? devices)
    {
        _microphones.Items.Clear();_speakers.Items.Clear();
        _microphones.Items.Add(new DeviceOption(-1,"Системный микрофон"));
        _speakers.Items.Add(new DeviceOption(-1,"Системные динамики"));
        if(devices is { ValueKind:JsonValueKind.Object } root){
            foreach(var key in new[]{("microphones",_microphones),("speakers",_speakers)}){
                if(root.TryGetProperty(key.Item1,out var arr)&&arr.ValueKind==JsonValueKind.Array)
                    foreach(var item in arr.EnumerateArray()){
                        int index=item.GetProperty("index").GetInt32();
                        string name=item.GetProperty("name").GetString()??"";
                        key.Item2.Items.Add(new DeviceOption(index,name));
                    }
            }
        }
        int Find(ComboBox cb,int idx){
            for(int i=0;i<cb.Items.Count;i++)
                if(cb.Items[i] is DeviceOption d&&d.Index==idx)return i;
            return 0;
        }
        _microphones.SelectedIndex=Find(_microphones,Preferences.Microphone);
        _speakers.SelectedIndex=Find(_speakers,Preferences.Speaker);
    }
    private sealed record DeviceOption(int Index,string Name)
    {
        public override string ToString()=>Name;
    }
}
