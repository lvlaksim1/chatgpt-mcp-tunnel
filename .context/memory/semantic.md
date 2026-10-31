# Semantic memory

## АКТУАЛЬНО 31.10.2026 — менеджер проекта, поколение 60
**31.10.2026:** v0.2.60 GUI подтверждённо установлена, MCP runtime осознанно на v0.2.58. WebView2 выбран владельцем единственным интерфейсом переписки, MCP — инструменты компьютеров. Отдельный голосовой worker на бесплатных локальных Sherpa-ONNX 1.13.8, Zipformer RU, Silero VAD, Piper Irina. Офлайн ASR/TTS + mock DOM PASS, живой USB микрофон/облачный диалог E2E OPEN. Установленная сборка опережает незакоммиченный source feature branch. Старые утверждения WebView2 auth-only и v0.2.37 текущая superseded.

---


- source: legacy-v2-state
- authority: legacy-unverified
## 30.10.2026 — новая просьба по UI
Пользователь потребовал **показывать номер версии ChatGPT MCP Tunnel в следующей версии программы**. Предполагаемый видимый элемент по контексту — окно уведомления о компьютерном управлении. Номер должен соответствовать установленному/запущенному EXE. Это не реализованная функция, а сохранённая задача для следующего цикла. Source: owner-directive 30.10.2026.

- authority: legacy-unverified
## 2026-10-30 — chat new-target surface and real host capability
- The ChatGPT Apps SDK `ui/message` `_meta["openai/message"].target:"new"` requires advertised host experimental capability. The operation response does not prove newly opened ChatGPT thread mode. A Global entrypoint can be hosted in Work; writing “ordinary” in the prompt does not change mode. Real host may advertise the capability yet omit the `openai/deepLink` URL.
- v0.2.29 explicitly removes all Global dispatch logic; only `chat.bootstrap.open` in existing ordinary ChatGPT can deliver, with positive URL evidence or one-time user confirmation when host omits URL; unsafe contexts never poll or acknowledge pending messages. Four pre-existing pending items preserved. Source: historical code v0.2.18, verified test suite v0.2.29, real host diagnostic 30.10, owner directive. E2E not yet accepted.

- authority: legacy-unverified
## 30.10.2026 — подтверждённый релиз и роль менеджера
- Продолжение текущего диалога — постоянный `project-manager` 2.1.0. Ошибочное высказывание «своя капсула супервизора» было неверной интерпретацией владельца; не создавать/не подменять manager_id и не обновлять чужую капсулу. Owner explicitly corrected role; authority: owner-directive 30.10.2026.
- Verified runtime 30.10.2026 03:46 MSK: DOMKAT `D:\ChatGPTMCPAgent\ChatGptMcpAgent.exe` установлен `0.2.28+ad28e28`, исходный Git HEAD `ad28e28` и чистый status. Пользователь установил обновление сам, GitHub Release v0.2.28 опубликован напрямую после локальной сборки и тестов.
- Встроенная атрибуция `executor` — атрибуция *каждого вызова*, а не автоматически удостоверенная персонализация Host. Текущему менеджеру следует явно передавать `project-manager`, супервизору `supervisor`, а обычному ChatGPT поле опускать. Нельзя сохранять «активную роль» глобально между беседами.
- Реальный GUI-тест всех трёх отображаемых имён в установленном v0.2.28 ещё не подтверждён; успешные unit/integration tests и версия установленного EXE не равны визуальной приёмке.
- Источник: прямые решения владельца, проверенные локальная компиляция, GitHub release и Windows VersionInfo 30.10.2026; повторять только по актуальному окружению.

- source: legacy-v2-state
## 30.10.2026 — идентификация исполнителя
- MCP tool invocation metadata в данной архитектуре не несёт проверенного Host chat persona. Приписывание исполнителя возможно через явное необязательное поле \`executor\` каждого вызова. Вызов без поля = ChatGPT; project-manager/supervisor и короткие persona:Name отображаются отдельно. Нельзя делать из подписи права доступа и нельзя сохранять глобальное имя на процесс: разные чаты работают параллельно. Owner directive, verified local code/tests, v0.2.28.

- source: legacy-v2-state
- authority: legacy-unverified
## 30.10.2026 — значимые факты и уроки
- Авторитетное решение Owner: локальный цикл разработки на DOMKAT `D:\Dev`; GitHub Actions **запрещены** (все workflow disabled). GitHub — хранение исходников и готовых релизов; прежние CI-планы исторические.
- DOMKAT установленная v0.2.27 подтверждена, MCP bridge_ping после обновления успешен; исходники текущего рабочего дерева содержат незакоммиченные Unicode-поправки v0.2.28.
- v0.2.25 индикатор не показывался из-за отсутствия в `TrayContext` даже при успешном selftest формы; v0.2.26 исправила реальную привязку и нативную видимость. v0.2.27 исправила скудные категории, но занесла буквальные `????` в 5 строк.
- Крупные межкомандные задержки возникают из модели/количества roundtrips, а не главным образом из времени выполнения локальных операций. Базовый способ — заранее спланированный локальный сценарий.
- «Управление завершено» после отсутствия активной MCP-команды не доказывает конец всей пользовательской задачи. Authority: owner directive + runtime/git, 30.10.2026.

Record reusable durable knowledge and lessons that are broader than the current working state.

Treat observed information as a candidate first. Admit it only when it has durable future value. Preserve source and authority when the memory could influence future decisions, revalidate it when freshness or risk matters, and revise it with confirm/supersede/conflict semantics rather than silent overwrite.

- source: legacy-v2-state
