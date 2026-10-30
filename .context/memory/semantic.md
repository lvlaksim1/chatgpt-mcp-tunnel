# Semantic memory

## 30.10.2026 — значимые факты и уроки
- Авторитетное решение Owner: локальный цикл разработки на DOMKAT `D:\Dev`; GitHub Actions **запрещены** (все workflow disabled). GitHub — хранение исходников и готовых релизов; прежние CI-планы исторические.
- DOMKAT установленная v0.2.27 подтверждена, MCP bridge_ping после обновления успешен; исходники текущего рабочего дерева содержат незакоммиченные Unicode-поправки v0.2.28.
- v0.2.25 индикатор не показывался из-за отсутствия в `TrayContext` даже при успешном selftest формы; v0.2.26 исправила реальную привязку и нативную видимость. v0.2.27 исправила скудные категории, но занесла буквальные `????` в 5 строк.
- Крупные межкомандные задержки возникают из модели/количества roundtrips, а не главным образом из времени выполнения локальных операций. Базовый способ — заранее спланированный локальный сценарий.
- «Управление завершено» после отсутствия активной MCP-команды не доказывает конец всей пользовательской задачи. Authority: owner directive + runtime/git, 30.10.2026.

Record reusable durable knowledge and lessons that are broader than the current working state.

Treat observed information as a candidate first. Admit it only when it has durable future value. Preserve source and authority when the memory could influence future decisions, revalidate it when freshness or risk matters, and revise it with confirm/supersede/conflict semantics rather than silent overwrite.
