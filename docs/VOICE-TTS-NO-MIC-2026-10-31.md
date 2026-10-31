# ChatGPT MCP Tunnel v0.2.61 — текст ответов в локальный TTS без микрофона

Дата: 31.10.2026 (MSK). Источник: прямое требование владельца и живые испытания на DOMKAT.

## Проверенная конфигурация
- Windows 10, установленное GUI `D:\ChatGPTMCPAgent\ChatGptMcpAgent.exe`, v0.2.61.
- Встроенный WebView2 авторизован в обычном ChatGPT. MCP runtime 0.2.58 и tunnel-client оставлены запущенными при установке GUI.
- Windows default render: `Динамики (Realtek USB2.0 Audio)`. Программно перечислены два активных выхода (Realtek USB и NVIDIA HDMI).
- Микрофон `USB2.0 MIC` физически отсутствует: Windows IsPresent=False, NAudio WaveInEvent.DeviceCount=0.
- Sherpa-ONNX 1.13.8, русская Piper Irina, автономный локальный TTS.
- Пользовательская настройка `AutoSpeak=true`, `Speaker=-1` (системный выход), `AutoSend=false`.

## Исправленные дефекты
1. В `ChatGptMainForm.Voice.cs` раньше обработчик `voice_reply` сразу возвращался при `_voiceWorker is null`. Теперь `EnsureVoiceWorkerAsync()` запускает процесс на событие ответа; отправка сериализована через `_voiceCommands`, дедупликация текста+URL сохранена. Захват микрофона для TTS не вызывается.
2. В `ChatGptMcpVoice/Program.cs` перед `WaveInEvent.StartRecording` проверяются `DeviceCount` и диапазон. При нуле появляется понятное уведомление, не WinMM `BadDeviceId`.
3. В реальном ChatGPT 31.10.2026 не обнаружены старые `[data-message-author-role=assistant]` и `.markdown`; новые ответы находятся в классе `DilResponseRoot`. В `Themes/ChatGPT-Voice.js` добавлено чтение обоих форматов. Скрипт hot-reload проверялся в уже запущенном WebView2 (через штатный watcher `ChatGPT-Context.js`).

## Испытания и доказательства
- `ChatGptMcpVoice.exe --stdio`: `ready`, `speaking`, `speech_finished` (`interrupted=false`) для короткой русской фразы через Realtek USB2.0 Audio. Сбой микрофона не блокирует TTS.
- Исходники и локально опубликованные бинарники v0.2.61: GUI/Voice Release build PASS.
- Packaged GUI: `--selftest`, `--test-toolbar-dom`, `--test-voice-dom`, `--test-main-chat-ui` PASS.
- `tests/test_tts_without_microphone.py` и `tests/test_primary_webview_ui.py` PASS; `node --check Themes/ChatGPT-Voice.js` PASS.
- Живой ChatGPT в WebView2 сформировал два проверочных ответа. После hot-fix наблюдатель нашёл два DIL response roots и зарегистрировал последний текст как `reportedText`. Прямое диагностическое `window.chrome.webview.postMessage({mcp_kind:'voice_reply',...})` запустило worker и сформировало WAV.
- Технические моки и диагностические инструменты **не подтверждают физическую слышимость** для владельца и устойчивые 2/2, 10/10. Остаются приёмочными испытаниями; не считать завершёнными.

## Выпуск и безопасность
- Сборка `.tools/local-release/v0.2.61/publish`; локальные `Setup`/`Update` + SHA manifest в соседнем `dist` (после финальной перепаковки с исправленным JS).
- Безопасная установка в `.tools/deploy-v0261-safe.ps1`: backup и PID-only замена GUI, без `--shutdown`; PID MCP-сервера и tunnel-client не изменились.
- Профиль WebView2 и пользовательские настройки не удалялись. Прежние непубликованные правки в рабочей feature-ветке не сбрасывать.
- GitHub Actions не запускать. В GitHub сохранять только проверенные исходники и готовые релизы; не записывать доступы и содержимое приватных чатов.

## Открытые приёмочные шаги
Пользователь подтверждает, что действительно слышит TTS в колонках при новом ответе. Затем 2/2 и 10/10 последовательных ответов, проверка дубликатов/вкладок/перезапуска. Физическое распознавание речи проверять только после появления настоящего микрофона.
