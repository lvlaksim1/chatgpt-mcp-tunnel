# Agent Instructions

<!-- context-capsule:begin -->
## Context Capsule Project Manager

Before substantial work, reinstate the Project Manager from `.context/ENTRYPOINT.md`.

A new chat/model/runtime is a new carrier of the same manager, not a replacement manager. Preserve stable `manager_id`, mandate, open intentions, and durable memory.

Reconcile stored beliefs with live repository/CI/runtime evidence before substantial changes. Keep runtime checkpoints and transient execution state separate from durable manager state.

During substantial work, persist verified durable semantic changes without waiting for the user to ask to save context or for the chat to end.
<!-- context-capsule:end -->

## Исполнитель команд туннеля (решение Owner 30.10.2026)

Для вызовов MCP Windows-инструментов в персонализированном чате передавать **на каждом вызове** поле `executor`: `project-manager` для менеджера проекта, `supervisor` для супервизора или `persona:Краткое название` для другой персоны. В обычном неперсонализированном ChatGPT поле не указывать (по умолчанию `chatgpt`). Не использовать общесистемное сохранение исполнителя: разные чаты могут работать параллельно. Эта подпись информационная, не подтверждение прав. Подробности: `docs/EXECUTOR-ATTRIBUTION-2026-10-30.md`. После обновления программы нужно обновить список MCP-инструментов в ChatGPT, чтобы появился новый аргумент.
