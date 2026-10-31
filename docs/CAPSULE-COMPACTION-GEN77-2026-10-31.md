# ChatGPT MCP Tunnel Project Manager: default recovery budget remediation

Дата: 31.10.2026, MSK. Owner поручил устранить технический долг, переданный Ecosystem Supervisor: PM capsule generation 75/76 не восстанавливалась командой pmctl recover с обычным ограничением 131072 символа; восстановление требовало --max-chars 262144. Исходный доклад: D:/Dev/supervisor/.context/supervisor/tunnel-manager-authority-closure-2026-10-31.md.

## Что изменено

- Источник: generation 76 на ревизии 9c64554. До изменений существовал отдельный чистый git commit с полным состоянием. Создана локальная резервная копия: D:/Dev/ChatGPT-MCP-Tunnel/.tools/context-compaction-backup-gen76 (не публиковать, без необходимости не удалять).
- Точная история восьми coupled путей сохранена в docs/context-archive/manager-gen76-lossless.zip. Неизменность каждого из 8 архивных файлов контролируется SHA256 из .context/archive/manager-gen76-2026-10-31/manifest.json. Сам ZIP контролируется отдельным SHA256. Человекочитаемые исходники остаются в .context/archive/manager-gen76-2026-10-31/.context/; Git может нормализовать окончания строк в этих копиях, поэтому только ZIP является canonical byte-for-byte archive.
- Рабочие manager/beliefs, goals, intentions, plans и текущие state, blockers, next, handoff объединяют актуальные факты, нерешённые вопросы, существующую identity и owner-directive. Все семь стратегических целей владельца включены без переписывания; отдельная история доступна из архива и старого коммита.
- В восьми coupled файлах прежние 456789 байт заменены ~23 КБ актуального рабочего контекста. Generation увеличено до 77, sealed git-blob SHA1 8/8; Contract, Protocol, project rules, identity, mandate, product code и установленный v0.2.65 не затрагивались.
- Контекстная система не может читать бинарные объекты внутри .context: поэтому lossless ZIP размещён в docs/context-archive, а не внутри .context. Это часть регрессионной приёмки.
- Добавлен tests/test_manager_capsule_compaction.py: проверяет точные хеши 8 архивных файлов, хеш ZIP, текстовую эквивалентность просматриваемых версий после нормализации строк, целостность coupled seal, сохранение семи целей, а также штатные команды pmctl. Никаких секретов, DPAPI профилей или браузерных данных в архив не добавлялось.

## Критерии приёмки

Официальная команда: python D:/Dev/context-capsule-project-manager/installer/pmctl.py recover --target D:/Dev/ChatGPT-MCP-Tunnel; НЕТ параметра --max-chars.

- На feature до объединения в main: pmctl validate PASS, ready --non-authoritative PASS, recover --non-authoritative без увеличения лимита PASS.
- На авторитетной main после fast-forward до c085917: pmctl validate PASS, pmctl ready PASS (PROJECT MANAGER REINSTANTIABLE), pmctl recover по умолчанию PASS; восстановительный пакет 130266 символов. Важно: этот вывод почти целиком заполняет ограничение, но mandatory manager state существенно короче, а дополнительные current/handoff/memory секции добавляются только при доступном бюджете.
- После исправления переносимости архива на main: tests/test_manager_capsule_compaction.py PASS, архив 8/8, validate PASS, ready PASS.
- Критерий полной публикации: commit всех исправлений + ZIP и синхронизация main/feature без force, затем повторная проверка опубликованного HEAD и recover.

## Ограничения

- Это исправляет размер и восстановимость контекста, не проверяет full product E2E, перенос MCP регистрации в другой ChatGPT аккаунт или Disaster Recovery инфраструктуры.
- Исторические claims с устаревшими статусами в архиве остаются свидетельствами, а не новым авторитетным решением. При необходимости точной хронологии требуется выборочное чтение архива и исходного коммита.
- Не удалять полные архивы, даже если работающая капсула значительно короче. Будущие разрастания обязательного BDI должны блокироваться регрессионным тестом до публикации.
