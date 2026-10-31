using System.Globalization;
using System.Text.Json;
using System.Threading.Channels;
using NAudio.Wave;
using NAudio.CoreAudioApi;
using SherpaOnnx;

namespace ChatGptMcpVoice;

internal static class Program
{
    private static readonly object OutputLock = new();
    private static VoiceEngine? _engine;
    internal static void Emit(string kind, object? data = null)
    {
        lock (OutputLock)
        {
            Console.WriteLine(JsonSerializer.Serialize(new { kind, data }));
            Console.Out.Flush();
        }
    }

    private static async Task<int> Main(string[] args)
    {
        try
        {
            if (args.Length == 1 && args[0] == "--selftest")
            {
                Console.WriteLine(VoiceConfig.SelfTest() ? "VOICE_SELFTEST_OK" : "VOICE_SELFTEST_FAILED");
                return VoiceConfig.SelfTest() ? 0 : 2;
            }
            string models = VoiceConfig.ModelRoot;
            for (int i = 0; i + 1 < args.Length; i++)
                if (args[i] == "--models") models = args[i + 1];
            if (args.Length > 0 && args[0] == "--check-models")
            {
                var config = new VoiceConfig { Models = models };
                Console.WriteLine(JsonSerializer.Serialize(config.Inspect()));
                return config.Inspect().Ready ? 0 : 3;
            }
            if (args.Length > 0 && args[0] == "--synth")
            {
                if (args.Length < 3) return 4;
                var config = new VoiceConfig { Models = models };
                using var tts = new VoiceSynthesizer(config);
                string output = args[1];
                var words = args[2];
                await Task.Run(() => tts.Save(words, output, 1.0f));
                Console.WriteLine("SYNTH_OK " + new FileInfo(output).Length);
                return 0;
            }
            if (args.Length > 0 && args[0] == "--transcribe")
            {
                if (args.Length < 2) return 4;
                var config = new VoiceConfig { Models = models };
                using var asr = new VoiceRecognizer(config);
                using var reader = new AudioFileReader(args[1]);
                var samples = new List<float>();
                float[] batch = new float[4096];
                int count;
                while ((count = reader.Read(batch, 0, batch.Length)) > 0)
                {
                    // AudioFileReader yields interleaved 32-bit float samples.
                    for (int i = 0; i < count; i += reader.WaveFormat.Channels)
                        samples.Add(batch[i]);
                }
                var data = VoiceRecognizer.Resample(samples.ToArray(), reader.WaveFormat.SampleRate, 16000);
                Console.WriteLine(asr.Recognize(data));
                return 0;
            }
            if (args.Length > 0 && args[0] == "--stdio")
            {
                _engine = new VoiceEngine(new VoiceConfig { Models = models });
                Emit("ready", new { version = "0.2.61", models = _engine.Models });
                while (await Console.In.ReadLineAsync() is { } line)
                {
                    try
                    {
                        using var doc = JsonDocument.Parse(line);
                        var json = doc.RootElement;
                        if (!json.TryGetProperty("command", out var cmd)) continue;
                        string command = cmd.GetString() ?? "";
                        var id = json.TryGetProperty("id", out var cid) ? cid.GetString() : null;
                        if (command == "quit") break;
                        switch (command)
                        {
                            case "devices":
                                Emit("devices", new
                                {
                                    microphones = Enumerable.Range(0, WaveInEvent.DeviceCount)
                                        .Select(i => new { index=i, name=WaveInEvent.GetCapabilities(i).ProductName }),
                                    speakers = new MMDeviceEnumerator().EnumerateAudioEndPoints(DataFlow.Render,DeviceState.Active)
                                        .Select((d,i) => new { index=i, name=d.FriendlyName })
                                });
                                break;
                            case "start":
                                await _engine.StartAsync(
                                    json.TryGetProperty("microphone", out var mic) ? mic.GetInt32() : -1,
                                    json.TryGetProperty("pauseMs", out var pause) ? pause.GetInt32() : 700,
                                    json.TryGetProperty("continuous", out var cont) && cont.GetBoolean());
                                Emit("recording", new { active=true });
                                break;
                            case "stop":
                                await _engine.StopAsync();
                                Emit("recording", new { active=false });
                                break;
                            case "speak":
                                if (json.TryGetProperty("text", out var t))
                                {
                                    string text=t.GetString()??"";
                                    int speaker = json.TryGetProperty("speaker", out var dev)?dev.GetInt32():-1;
                                    float speed=json.TryGetProperty("speed", out var sp)?sp.GetSingle():1;
                                    _ = _engine.SpeakAsync(text, Math.Clamp(speed,0.7f,1.3f),speaker,id);
                                }
                                break;
                            case "cancel":
                                _engine.CancelSpeech();
                                Emit("speech_cancelled",new {id});
                                break;
                            case "status":
                                Emit("status",new { recording=_engine.IsRecording,
                                    playing=_engine.IsPlaying, models=_engine.Models });
                                break;
                        }
                    }
                    catch (Exception e)
                    {
                        Emit("error",new { type=e.GetType().Name, message=e.Message });
                    }
                }
                await _engine.StopAsync();
                _engine.CancelSpeech();
                return 0;
            }
            Console.Error.WriteLine("Usage: --stdio [--models path] | --check-models [--models path] | --selftest | --synth output.wav text | --transcribe input.wav");
            return 1;
        }
        catch (Exception e)
        {
            Console.Error.WriteLine("VOICE_FATAL " + e.GetType().Name + " " + e.Message);
            return 9;
        }
        finally { _engine?.Dispose(); }
    }
}

internal sealed class VoiceConfig
{
    internal static string ModelRoot => Path.Combine(
        AppContext.BaseDirectory, "Models");

    public string Models { get; init; } = ModelRoot;
    internal string Asr => Path.Combine(Models,"sherpa-onnx-zipformer-ru-2024-09-18");
    internal string Tts => Path.Combine(Models,"vits-piper-ru_RU-irina-medium");
    internal string Vad => Path.Combine(Models,"silero_vad.onnx");
    internal string TtsModel => Path.Combine(Tts,"ru_RU-irina-medium.onnx");
    internal string AsrEncoder => Path.Combine(Asr,"encoder.int8.onnx");
    internal string AsrDecoder => Path.Combine(Asr,"decoder.int8.onnx");
    internal string AsrJoiner => Path.Combine(Asr,"joiner.int8.onnx");
    internal string AsrTokens => Path.Combine(Asr,"tokens.txt");
    public ModelHealth Inspect()
    {
        var a=File.Exists(AsrEncoder)&&File.Exists(AsrDecoder)&&File.Exists(AsrJoiner)&&File.Exists(AsrTokens);
        var v=File.Exists(Vad);
        var t=File.Exists(TtsModel)&&File.Exists(Path.Combine(Tts,"tokens.txt"))&&
              Directory.Exists(Path.Combine(Tts,"espeak-ng-data"));
        return new ModelHealth(a&&v&&t,a,v,t,Models);
    }
    internal static bool SelfTest()
    {
        var c=new VoiceConfig();
        var f=Path.GetFullPath(c.AsrEncoder);
        return f.EndsWith("encoder.int8.onnx",StringComparison.Ordinal) &&
            Math.Clamp(700,300,1500)==700 &&
            Math.Clamp(150,300,1500)==300 &&
            Math.Clamp(1700,300,1500)==1500;
    }
}

internal sealed record ModelHealth(bool Ready,bool Stt,bool Vad,bool Tts,string Models);

internal sealed class VoiceRecognizer : IDisposable
{
    private readonly OfflineRecognizer _recognizer;
    internal VoiceRecognizer(VoiceConfig cfg)
    {
        var c=new OfflineRecognizerConfig();
        c.FeatConfig.SampleRate=16000;
        c.FeatConfig.FeatureDim=80;
        c.ModelConfig.Tokens=cfg.AsrTokens;
        c.ModelConfig.Transducer.Encoder=cfg.AsrEncoder;
        c.ModelConfig.Transducer.Decoder=cfg.AsrDecoder;
        c.ModelConfig.Transducer.Joiner=cfg.AsrJoiner;
        c.ModelConfig.NumThreads=2;
        c.ModelConfig.Provider="cpu";
        c.ModelConfig.Debug=0;
        c.DecodingMethod="greedy_search";
        _recognizer=new OfflineRecognizer(c);
    }
    internal string Recognize(float[] samples)
    {
        if(samples.Length<1600) return "";
        using var stream=_recognizer.CreateStream();
        stream.AcceptWaveform(16000,samples);
        _recognizer.Decode(stream);
        return stream.Result.Text.Trim();
    }
    internal static float[] Resample(float[] input, int src, int dst)
    {
        if(src==dst) return input;
        if(src<=0||dst<=0)throw new ArgumentOutOfRangeException(nameof(src));
        int length=(int)Math.Ceiling(input.Length*(double)dst/src);
        var result=new float[length];
        for(int i=0;i<length;i++){
            double point=i*(double)src/dst;
            int j=Math.Min(input.Length-1,(int)point);
            int k=Math.Min(input.Length-1,j+1);
            result[i]=(float)(input[j]*(1-(point-j))+input[k]*(point-j));
        }
        return result;
    }
    public void Dispose() => _recognizer.Dispose();
}

internal sealed class VoiceSynthesizer : IDisposable
{
    private readonly OfflineTts _tts;
    internal VoiceSynthesizer(VoiceConfig cfg)
    {
        var c=new OfflineTtsConfig();
        c.Model.Vits.Model=cfg.TtsModel;
        c.Model.Vits.Tokens=Path.Combine(cfg.Tts,"tokens.txt");
        c.Model.Vits.DataDir=Path.Combine(cfg.Tts,"espeak-ng-data");
        c.Model.NumThreads=2;
        c.Model.Provider="cpu";
        c.Model.Debug=0;
        _tts=new OfflineTts(c);
    }
    internal void Save(string text,string output,float speed)
    {
        var generation=new OfflineTtsGenerationConfig{Sid=0,Speed=speed,SilenceScale=0.2f};
        var result=_tts.GenerateWithConfig(text,generation,null);
        if(!result.SaveToWaveFile(output)) throw new IOException("Synthesizer failed to write wave");
    }
    public void Dispose()=>_tts.Dispose();
}

internal sealed class VoiceEngine : IDisposable
{
    private readonly VoiceConfig _config;
    private VoiceRecognizer? _asr;
    private VoiceSynthesizer? _tts;
    private VoiceActivityDetector? _vad;
    private WaveInEvent? _mic;
    private Channel<float[]>? _samples;
    private Task? _reader;
    private readonly SemaphoreSlim _synthesisLock=new(1,1);
    private CancellationTokenSource? _speechCancel;
    private IWavePlayer? _output;
    private AudioFileReader? _playFile;
    private string? _currentWave;
    private bool _continuous;
    private volatile bool _recording;
    private volatile bool _playing;
    private long _lastEmissionTick;
    private readonly object _sync=new();

    internal VoiceEngine(VoiceConfig config) => _config=config;
    internal object Models => _config.Inspect();
    internal bool IsRecording => _recording;
    internal bool IsPlaying => _playing;

    internal Task StartAsync(int microphone,int pauseMs,bool continuous)
    {
        if(_recording) return Task.CompletedTask;
        int microphoneCount = WaveInEvent.DeviceCount;
        if(microphoneCount == 0)
            throw new InvalidOperationException("Микрофон не подключён. Озвучивание ответов работает без микрофона.");
        if(microphone >= microphoneCount)
            throw new ArgumentOutOfRangeException(nameof(microphone), "Выбранный микрофон недоступен.");
        if(!_config.Inspect().Ready)throw new FileNotFoundException("Russian voice models are missing: "+_config.Models);
        _asr??=new VoiceRecognizer(_config);
        var config=new VadModelConfig();
        config.SampleRate=16000;
        config.SileroVad.Model=_config.Vad;
        config.SileroVad.Threshold=0.5f;
        config.SileroVad.MinSilenceDuration=Math.Clamp(pauseMs,300,1500)/1000f;
        config.SileroVad.MinSpeechDuration=0.18f;
        config.SileroVad.MaxSpeechDuration=25f;
        config.SileroVad.WindowSize=512;
        _vad?.Dispose();
        _vad=new VoiceActivityDetector(config,60);
        _continuous=continuous;
        _samples=Channel.CreateBounded<float[]>(new BoundedChannelOptions(96) {
            SingleReader=true,SingleWriter=true,FullMode=BoundedChannelFullMode.DropOldest
        });
        _mic=new WaveInEvent{
            DeviceNumber=microphone>=0?microphone:0,
            WaveFormat=new WaveFormat(16000,16,1),
            BufferMilliseconds=32,
            NumberOfBuffers=4
        };
        _mic.DataAvailable+=(_,e)=>{
            if(_playing) return; // never feed synthesized audio back to microphone
            var bytes=e.Buffer;
            var n=e.BytesRecorded/2;
            var samples=new float[n];
            for(int i=0;i<n;i++)
                samples[i]=BitConverter.ToInt16(bytes,i*2)/32768f;
            _samples.Writer.TryWrite(samples);
        };
        _recording=true;
        _reader=Task.Run(ProcessMicrophoneAsync);
        try{_mic.StartRecording();}
        catch{_recording=false;_samples.Writer.TryComplete();_mic.Dispose();_mic=null;throw;}
        return Task.CompletedTask;
    }

    private async Task ProcessMicrophoneAsync()
    {
        var pending=new List<float>(1024);
        try{
            while(_samples is { } channel&&
                await channel.Reader.WaitToReadAsync()){
                while(channel.Reader.TryRead(out var chunk)){
                    if(_playing)continue;
                    pending.AddRange(chunk);
                    while(pending.Count>=512){
                        var block=pending.GetRange(0,512).ToArray();
                        pending.RemoveRange(0,512);
                        _vad?.AcceptWaveform(block);
                        await DrainVadAsync();
                    }
                }
            }
        }catch(Exception e){Program.Emit("error",new{type="microphone",message=e.Message});}
    }

    private async Task DrainVadAsync()
    {
        if(_vad is null||_asr is null)return;
        while(!_vad.IsEmpty()){
            var segment=_vad.Front();
            _vad.Pop();
            if(segment.Samples.Length<3200)continue;
            var text=await Task.Run(()=>_asr.Recognize(segment.Samples));
            // No duplicate emission of the same recognition run.
            if(string.IsNullOrWhiteSpace(text))continue;
            var now=Environment.TickCount64;
            if(now-_lastEmissionTick<150)continue;
            _lastEmissionTick=now;
            Program.Emit("recognized",new{text,continuous=_continuous});
        }
    }

    internal async Task StopAsync()
    {
        if(!_recording)return;
        _recording=false;
        _mic?.StopRecording();
        _mic?.Dispose();
        _mic=null;
        _samples?.Writer.TryComplete();
        if(_reader is not null) await _reader;
        _reader=null;
        _vad?.Flush();
        await DrainVadAsync();
        _vad?.Dispose();_vad=null;
    }

    internal async Task SpeakAsync(string text,float speed,int speaker,string? id)
    {
        if(string.IsNullOrWhiteSpace(text)||text.Length>2500)return;
        CancelSpeech();
        var cancel=new CancellationTokenSource();
        _speechCancel=cancel;
        await _synthesisLock.WaitAsync();
        try{
            if(cancel.IsCancellationRequested)return;
            _tts??=new VoiceSynthesizer(_config);
            _playing=true;
            Program.Emit("speaking",new{id});
            var file=Path.Combine(Path.GetTempPath(),"mcp-voice-"+Guid.NewGuid().ToString("N")+".wav");
            await Task.Run(()=>_tts.Save(text,file,speed));
            if(cancel.IsCancellationRequested){File.Delete(file);return;}
            _currentWave=file;
            var reader=new AudioFileReader(file);
            IWavePlayer output = speaker >= 0 ? CreateOutput(speaker) : new WaveOutEvent {DeviceNumber=-1,DesiredLatency=120};
            _playFile=reader;_output=output;
            var completion=new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            output.PlaybackStopped+=(_,e)=>completion.TrySetResult();
            output.Init(reader);
            output.Play();
            using(cancel.Token.Register(()=>output.Stop()))await completion.Task;
            Program.Emit("speech_finished",new{id,interrupted=cancel.IsCancellationRequested});
        }catch(Exception e){Program.Emit("error",new{type="tts",message=e.Message});}
        finally{
            lock(_sync){_output?.Dispose();_output=null;_playFile?.Dispose();_playFile=null;}
            if(_currentWave is { } wave){try{File.Delete(wave);}catch(IOException){} _currentWave=null;}
            _playing=false;
            _synthesisLock.Release();
        }
    }

    private static IWavePlayer CreateOutput(int index)
    {
        using var enumerator=new MMDeviceEnumerator();
        var devices=enumerator.EnumerateAudioEndPoints(DataFlow.Render,DeviceState.Active);
        if(index<0 || index>=devices.Count) throw new ArgumentOutOfRangeException(nameof(index));
        return new WasapiOut(devices[index],AudioClientShareMode.Shared,false,120);
    }

    internal void CancelSpeech()
    {
        _speechCancel?.Cancel();
        lock(_sync){try{_output?.Stop();}catch(InvalidOperationException){}}
    }
    public void Dispose()
    {
        CancelSpeech();
        _mic?.Dispose();_vad?.Dispose();_asr?.Dispose();_tts?.Dispose();
        _synthesisLock.Dispose();_speechCancel?.Dispose();
    }
}
