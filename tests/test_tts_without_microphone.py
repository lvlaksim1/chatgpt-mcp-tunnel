from pathlib import Path
root = Path(__file__).resolve().parents[1]
gui = (root / "src/ChatGptMcpAgent/ChatGptMainForm.Voice.cs").read_text(encoding="utf-8")
handler = gui.split("private async Task HandleVoiceWebMessageAsync", 1)[1].split("private async Task UpdateVoiceEnabledOnTabsAsync", 1)[0]
assert "await EnsureVoiceWorkerAsync()" in handler
assert "||_voiceWorker is null" not in handler
assert "_voiceWorker!.SendAsync(new{command=\"speak\"" in handler
worker = (root / "src/ChatGptMcpVoice/Program.cs").read_text(encoding="utf-8")
assert "microphoneCount == 0" in worker
assert "Озвучивание ответов работает без микрофона" in worker
print("TTS_WITHOUT_MICROPHONE_REGRESSION_OK")
voice_js=(root/"src/ChatGptMcpAgent/Themes/ChatGPT-Voice.js").read_text(encoding="utf-8")
assert "DilResponseRoot" in voice_js, "DIL_RENDERER_FALLBACK missing"
assert "data-message-author-role" in voice_js, "LEGACY_RENDERER_FALLBACK missing"
