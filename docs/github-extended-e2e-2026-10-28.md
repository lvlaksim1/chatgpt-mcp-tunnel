# GitHub Connector — расширенные приёмочные испытания

Дата: 2026-10-28. Компьютер: DOMKAT. Репозиторий сервера: `lvlaksim1/chatgpt-mcp-tunnel`.

## Назначение и состав

В сервере **41 MCP-инструмент**: 24 прежних (включая 15 категории «Мультимедиа») и 17 из категории **«GitHub»**. Все 17 имеют общий префикс в `title` («GitHub · …») и `description` («Категория «GitHub»»), а оригинальные имена `github.*` сохранены. Это логическая разговорная категория, а не отдельный реестр групп в MCP.

## Изолированная проверка

Создан приватный репозиторий `lvlaksim1/chatgpt-mcp-tunnel-api-e2e-20261028` с автоматически инициализированной веткой `main`. Все операции с тестовыми данными выполнялись только в нём.

| Подсистема | Подтверждённые операции | Результат |
|---|---|---|
| Repositories | create, get, update; delete через REST | Создание/чтение/обновление — OK; REST delete — HTTP 403, требуется дополнительное разрешение |
| Issues | create, get, list, update, comment, close | OK (Issue #1) |
| Pull Requests | create, get, list, update, files, reviews, merge | OK (PR #2, `squash`); review-comment через универсальный REST |
| Branches/Files | create branch, запись и чтение изменённого файла | OK |
| Releases | create, update, get, by_tag, latest, list, delete | OK; проверены prerelease и обычный release |
| Release assets | upload, list, download, проверка содержимого, delete | OK; текстовый файл 95 байт |
| Actions | workflows, dispatch, runs, get, cancel, rerun, rerun_failed | OK; запуски success, failure, cancelled; повторные попытки подтверждены |
| Jobs | list, get, download_logs | OK; текст журнала 4820 байт |
| Artifacts | list, get, download, delete | OK; ZIP 177 байт, сигнатура PK |
| Generic GitHub API | REST и GraphQL чтение и запись | OK; через `github.api` проверена публикация комментария review |
| Локальный Git | clone, checkout, commit, push | OK в ранее проведённом E2E на отдельной временной ветке |

Доказательства состояний Actions: успешный run `40624093618` (в том числе `run_attempt=2`), преднамеренно упавший `40624389129` (`run_attempt=2`), отменённый `40624437483` (итог `cancelled`). Тестовый репозиторий впоследствии удалён; эти ссылки более не доступны.

## Найденные проблемы и исправления

1. **HTTP 415** при скачивании job logs и Actions artifact ZIP. В `GithubRuntime.Download` использовался `Accept: application/octet-stream`, который подходит для файлов релизов, но не для этих API. Исправлено: `application/vnd.github+json` для журналов и артефактов Actions; `application/octet-stream` остаётся для release assets. Добавлено подробное сообщение для неуспешного ответа и `tests/test_github_downloads.py`.
2. **Удаление репозитория через REST** отклонено: `HTTP 403 Must have admin rights to Repository`. Текущая авторизация GitHub CLI включает `repo` и `workflow`, но не `delete_repo`. Веб-интерфейс запросил отдельную проверку личности. Впоследствии `GET /repos/.../chatgpt-mcp-tunnel-api-e2e-20261028` и список его workflows вернули 404, что подтверждает отсутствие тестового репозитория. Для будущего автоматического удаления потребуется отдельно предоставить соответствующие разрешения.
3. **GraphQL `deleteRepository`** отсутствует в текущей схеме GraphQL GitHub и не может заменять REST-удаление.

## Регрессия скачивания после исправления

- Actions artifact ZIP: получено 177 байт с ZIP-сигнатурой `PK`.
- Job logs: получено 4820 байт читаемого текста.
- Release asset: с новой сборкой скачан публичный файл `gh_2.103.3_checksums.txt` из `cli/cli`, 1971 байт, содержимое проверено, не JSON-метаданные.
- `tests/test_github_downloads.py` поддерживает независимую регрессию через локальный MCP без изменения репозиториев.

## Обязательные дальнейшие проверки

Перед отключением штатного GitHub-коннектора: подтвердить доступность 41 инструмента в новом чате после обновления списка; выполнять прикладные операции через нашу категорию «GitHub». Для API-удаления репозиториев требуется отдельное полномочие, если оно необходимо в текущей работе.

Тестовые данные не помещались в основной репозиторий. Локальные загрузки и сценарии размещались только под `D:\ChatGPT-MCP-Tunnel\.tools\github-e2e` и удаляются по завершении проверки.
