using System.ComponentModel;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using ModelContextProtocol.Server;

[McpServerToolType]
public static class GithubTools
{
    private static readonly JsonSerializerOptions Pretty = new() { WriteIndented = true };
    private static string J(object value) => GithubRuntime.Json(value);
    private static string R(string repo) => GithubRuntime.Repo(repo);
    private static string E(string value) => GithubRuntime.Segment(value);
    private static object? Body(string? json) => GithubRuntime.ParseObject(json);
    private static string PathPart(string path)
    {
        if (string.IsNullOrWhiteSpace(path) || path.StartsWith('/') || path.Contains("..") || path.Contains('\\'))
            throw new ArgumentException("Invalid repository file path.");
        return string.Join("/", path.Split('/').Select(E));
    }
    private static async Task<string> Guard(Func<Task<string>> run)
    {
        try { return await run(); }
        catch (Exception ex) { return J(new { success = false, error = ex.Message }); }
    }
    private static async Task<string> A(string method, string path, object? body = null) =>
        await GithubRuntime.ApiOutput(method, path, body);
    private static Dictionary<string, object?> Fields(params (string, object?)[] entries) =>
        entries.Where(p => p.Item2 != null).ToDictionary(p => p.Item1, p => p.Item2);

    [McpServerTool(Name="github.auth", Title="GitHub · Проверить авторизацию")]
    [Description("Категория «GitHub»: проверить доступ нашего MCP Tunnel к GitHub, текущую учётную запись и ограничения API. Использует защищённую локальную авторизацию GitHub CLI, не раскрывает токен.")]
    public static Task<string> Auth() => Guard(async () =>
    {
        var user = await GithubRuntime.Api("GET", "/user");
        var rate = await GithubRuntime.Api("GET", "/rate_limit");
        return J(new { authenticated = true, login = user?["login"]?.ToString(),
                       name = user?["name"]?.ToString(),
                       user_url = user?["html_url"]?.ToString(),
                       rate_limit = rate?["rate"]?.DeepClone() });
    });

    [McpServerTool(Name="github.api", Title="GitHub · Любой REST API")]
    [Description("Категория «GitHub»: универсальный вызов GitHub REST API для функций, которых нет в специализированных командах. Поддерживает GET/POST/PATCH/PUT/DELETE; endpoint строго относительный путь API, например /repos/owner/repo/actions/runs. Аутентификация локальная, token не возвращается.")]
    public static Task<string> Api(
        [Description("HTTP method: GET, POST, PATCH, PUT, DELETE.")] string method,
        [Description("Relative GitHub API endpoint beginning with / (including optional query).")] string endpoint,
        [Description("Optional JSON request body for mutations, not a token.")] string? body_json = null,
        [Description("Maximum response characters, 1000-1000000.")] int max_chars = 120000) =>
        Guard(() => GithubRuntime.ApiOutput(method, endpoint, Body(body_json), max_chars));

    [McpServerTool(Name="github.graphql", Title="GitHub · GraphQL")]
    [Description("Категория «GitHub»: выполнить запрос или мутацию GitHub GraphQL API с переменными. Для недостающих REST-операций и пакетного чтения.")]
    public static Task<string> Graphql(
        [Description("GitHub GraphQL query or mutation.")] string query,
        [Description("Optional JSON object with GraphQL variables.")] string? variables_json = null) =>
        Guard(() => A("POST","/graphql",Fields(("query",query),("variables",Body(variables_json)))));

    [McpServerTool(Name="github.repositories", Title="GitHub · Репозитории")]
    [Description("Категория «GitHub»: перечень репозиториев, чтение, создание, настройка и удаление. actions: list/get/create/update/delete. Для create/update требуется body_json с полями GitHub REST API; удаление необратимо.")]
    public static Task<string> Repositories(
        [Description("Action list/get/create/update/delete.")] string action,
        [Description("Repository owner/name, not required for list/create.")] string? repository = null,
        [Description("JSON options for create/update, e.g. {\"name\":\"test-repo\",\"private\":true}.")] string? body_json = null,
        [Description("Page number >=1 for list.")] int page = 1,
        [Description("Page size 1-100.")] int per_page = 50) => Guard(async () =>
    {
        string path = repository != null ? R(repository) : "";
        if (page < 1 || per_page < 1 || per_page > 100) throw new ArgumentOutOfRangeException(nameof(page));
        return action switch
        {
            "list" => await A("GET",$"/user/repos?affiliation=owner,collaborator,organization_member&per_page={per_page}&page={page}&sort=updated"),
            "get" => await A("GET",path),
            "create" => await A("POST","/user/repos", Body(body_json) ?? throw new ArgumentException("body_json required")),
            "update" => await A("PATCH",path, Body(body_json) ?? throw new ArgumentException("body_json required")),
            "delete" => await A("DELETE",path),
            _ => throw new ArgumentException("Unknown repository action.")
        };
    });

    [McpServerTool(Name="github.files", Title="GitHub · Файлы репозитория")]
    [Description("Категория «GitHub»: просмотр файлов и папок, чтение UTF-8 файла, создание или обновление с защитой SHA, удаление файла. actions: list/get/put/delete. При записи содержимое base64-кодируется автоматически.")]
    public static Task<string> Files(
        [Description("Action: list/get/put/delete.")] string action,
        [Description("Repository owner/name.")] string repository,
        [Description("File/folder path, empty for root when listing.")] string path = "",
        [Description("Text contents for put; does not accept binary data.")] string? content = null,
        [Description("Commit message for put/delete.")] string? message = null,
        [Description("Branch or ref (default default branch).")] string? branch = null,
        [Description("Optional expected blob SHA to reject stale writes.")] string? expected_sha = null) => Guard(async () =>
    {
        string endpoint = R(repository) + "/contents" + (path.Length > 0 ? "/" + PathPart(path) : "");
        string suffix = branch == null ? "" : "?ref=" + E(branch);
        if (action is "list" or "get")
        {
            var item = await GithubRuntime.Api("GET", endpoint + suffix);
            if (action=="get" && item is JsonObject obj && (string?)obj["encoding"]=="base64")
            {
                string encoded = ((string?)obj["content"] ?? "").Replace("\n","");
                byte[] bytes = Convert.FromBase64String(encoded);
                bool binary = bytes.AsSpan().IndexOf((byte)0) >= 0;
                string decoded = binary ? "" : Encoding.UTF8.GetString(bytes);
                return J(new { repository, path, sha=(string?)obj["sha"],size=bytes.Length,
                               is_binary=binary, text=binary ? null : decoded[..Math.Min(decoded.Length,200000)],
                               truncated=decoded.Length>200000,download_url=(string?)obj["download_url"] });
            }
            return J(new { repository, path, data = item });
        }
        if (path.Length==0) throw new ArgumentException("File path required.");
        string? existing = null;
        try { existing = (string?)((await GithubRuntime.Api("GET",endpoint + suffix))?["sha"]); }
        catch (Exception ex) when (ex.Message.Contains("404")) { }
        if (expected_sha!=null && existing != expected_sha) throw new InvalidOperationException("File SHA changed; refusing stale edit.");
        var data = Fields(("message",message ?? (action=="put"?"Update "+path:"Delete "+path)),
                          ("sha",existing),("branch",branch));
        if (action=="put")
        {
            data["content"]=Convert.ToBase64String(Encoding.UTF8.GetBytes(content ?? throw new ArgumentException("content required")));
            return await A("PUT",endpoint,data);
        }
        if (action=="delete")
        {
            if (existing==null) throw new FileNotFoundException("GitHub file not found",path);
            return await A("DELETE",endpoint,data);
        }
        throw new ArgumentException("Unknown files action.");
    });

    [McpServerTool(Name="github.branches", Title="GitHub · Ветки")]
    [Description("Категория «GitHub»: list/get/create/delete GitHub branches; создание от base_ref или SHA.")]
    public static Task<string> Branches(
        [Description("Action list/get/create/delete.")] string action,
        [Description("Repository owner/name.")] string repository,
        [Description("Branch name for get/create/delete.")] string? branch = null,
        [Description("Existing starting ref, default main for create (use branch if needed).")] string base_ref = "main") => Guard(async () =>
    {
        string repo=R(repository);
        return action switch
        {
            "list" => await A("GET",repo+"/branches?per_page=100"),
            "get" => await A("GET",repo+"/branches/"+E(branch ?? throw new ArgumentException("branch needed"))),
            "delete" => await A("DELETE",repo+"/git/refs/heads/"+E(branch ?? throw new ArgumentException("branch needed"))),
            "create" => await CreateBranch(repo, branch ?? throw new ArgumentException("branch needed"),base_ref),
            _ => throw new ArgumentException("Unknown branch action")
        };
    });
    private static async Task<string> CreateBranch(string repo,string name,string baseRef)
    {
        var source=await GithubRuntime.Api("GET",repo+"/git/ref/heads/"+E(baseRef));
        var sha=(string?)source?["object"]?["sha"] ?? throw new InvalidOperationException("Cannot read base ref SHA");
        return await A("POST",repo+"/git/refs",new { @ref="refs/heads/"+name,sha });
    }

    [McpServerTool(Name="github.commits", Title="GitHub · История и сравнение коммитов")]
    [Description("Категория «GitHub»: история, один коммит, сравнение веток/коммитов. actions list/get/compare.")]
    public static Task<string> Commits(
        [Description("Action list/get/compare.")] string action,
        [Description("Repository owner/name.")] string repository,
        [Description("Commit SHA or branch for get/list.")] string? reference = null,
        [Description("Base branch or SHA for compare.")] string? @base = null,
        [Description("Head branch or SHA for compare.")] string? head = null,
        [Description("Page number >=1.")] int page = 1) => Guard(() =>
    {
        string repo=R(repository);
        string path=action switch
        {
            "list" => repo+"/commits?per_page=100&page="+page+(reference!=null?"&sha="+E(reference):""),
            "get" => repo+"/commits/"+E(reference ?? throw new ArgumentException("reference required")),
            "compare" => repo+"/compare/"+E(@base ?? throw new ArgumentException("base required"))+"..."+E(head ?? throw new ArgumentException("head required")),
            _ => throw new ArgumentException("Unknown commits action")
        };
        return A("GET",path);
    });

    [McpServerTool(Name="github.issues", Title="GitHub · Задачи и комментарии")]
    [Description("Категория «GitHub»: list/get/create/update/comment для Issues; полное body_json принимает GitHub поля title,body,labels,assignees,milestone,state.")]
    public static Task<string> Issues(
        [Description("Action list/get/create/update/comment.")] string action,
        [Description("Repository owner/name.")] string repository,
        [Description("Issue number for get/update/comment.")] int number = 0,
        [Description("JSON issue body for create/update, e.g. {\"title\":\"bug\"}.")] string? body_json = null,
        [Description("Text of a new issue comment.")] string? comment = null,
        [Description("Issue filter: open/closed/all.")] string state = "open") => Guard(() =>
    {
        string p=R(repository)+"/issues";
        return action switch
        {
            "list" => A("GET",p+"?state="+E(state)+"&per_page=100"),
            "get" => A("GET",p+"/"+number),
            "create" => A("POST",p,Body(body_json) ?? throw new ArgumentException("body_json needed")),
            "update" => A("PATCH",p+"/"+number,Body(body_json) ?? throw new ArgumentException("body_json needed")),
            "comment" => A("POST",p+"/"+number+"/comments",new {body=comment ?? throw new ArgumentException("comment needed")}),
            _ => throw new ArgumentException("Unknown issue action")
        };
    });

    [McpServerTool(Name="github.pulls", Title="GitHub · Pull Requests")]
    [Description("Категория «GitHub»: list/get/create/update/merge/files/reviews для PR. body_json для создания/изменения, merge поддерживает merge_method merge/squash/rebase.")]
    public static Task<string> Pulls(
        [Description("Action list/get/create/update/merge/files/reviews.")] string action,
        [Description("Repository owner/name.")] string repository,
        [Description("PR number for get/update/merge/files/reviews.")] int number = 0,
        [Description("JSON body for create/update, e.g. {\"title\":\"feature\",\"head\":\"branch\",\"base\":\"main\"}.")] string? body_json = null,
        [Description("Merge method for merge: merge/squash/rebase.")] string merge_method = "squash") => Guard(() =>
    {
        string p=R(repository)+"/pulls";
        return action switch
        {
            "list" => A("GET",p+"?state=all&per_page=100"),
            "get" => A("GET",p+"/"+number),
            "create" => A("POST",p,Body(body_json) ?? throw new ArgumentException("body_json needed")),
            "update" => A("PATCH",p+"/"+number,Body(body_json) ?? throw new ArgumentException("body_json needed")),
            "merge" => A("PUT",p+"/"+number+"/merge",new {merge_method}),
            "files" => A("GET",p+"/"+number+"/files?per_page=100"),
            "reviews" => A("GET",p+"/"+number+"/reviews?per_page=100"),
            _ => throw new ArgumentException("Unknown pull action")
        };
    });

    [McpServerTool(Name="github.actions", Title="GitHub · Рабочие процессы Actions")]
    [Description("Категория «GitHub»: list workflows, runs, get run, dispatch workflow, cancel or rerun run, rerun_failed; Github Actions workflow_dispatch supports ref and inputs_json.")]
    public static Task<string> Actions(
        [Description("Action workflows/runs/get/dispatch/cancel/rerun/rerun_failed.")] string action,
        [Description("Repository owner/name.")] string repository,
        [Description("Workflow file or numeric workflow id for dispatch.")] string? workflow = null,
        [Description("Workflow run ID for get/cancel/rerun.")] long run_id = 0,
        [Description("Ref for workflow dispatch (branch).")] string reference = "main",
        [Description("Optional workflow dispatch inputs JSON.")] string? inputs_json = null) => Guard(() =>
    {
        string p=R(repository)+"/actions";
        return action switch
        {
            "workflows" => A("GET",p+"/workflows?per_page=100"),
            "runs" => A("GET",p+"/runs?per_page=100"),
            "get" => A("GET",p+"/runs/"+run_id),
            "dispatch" => A("POST",p+"/workflows/"+E(workflow ?? throw new ArgumentException("workflow required"))+"/dispatches",
                            Fields(("ref",reference),("inputs",Body(inputs_json)))),
            "cancel" => A("POST",p+"/runs/"+run_id+"/cancel"),
            "rerun" => A("POST",p+"/runs/"+run_id+"/rerun"),
            "rerun_failed" => A("POST",p+"/runs/"+run_id+"/rerun-failed-jobs"),
            _ => throw new ArgumentException("Unknown Actions operation")
        };
    });

    [McpServerTool(Name="github.jobs", Title="GitHub · Задания и журналы Actions")]
    [Description("Категория «GitHub»: список jobs конкретного run, отдельный job, или сохранить журнал job на диск. actions list/get/download_logs.")]
    public static Task<string> Jobs(
        [Description("Action list/get/download_logs.")] string action,
        [Description("Repository owner/name.")] string repository,
        [Description("Workflow run id for list.")] long run_id = 0,
        [Description("Job id for get/download_logs.")] long job_id = 0,
        [Description("Absolute destination file for downloaded logs.")] string? destination = null) => Guard(() =>
    {
        string p=R(repository)+"/actions";
        return action switch
        {
            "list" => A("GET",p+"/runs/"+run_id+"/jobs?per_page=100"),
            "get" => A("GET",p+"/jobs/"+job_id),
            "download_logs" => GithubRuntime.Download(p+"/jobs/"+job_id+"/logs", destination ?? throw new ArgumentException("destination required")),
            _ => throw new ArgumentException("Unknown jobs action")
        };
    });

    [McpServerTool(Name="github.releases", Title="GitHub · Релизы и версии")]
    [Description("Категория «GitHub»: list/latest/get/by_tag/create/update/delete releases. body_json supports tag_name, name, body, draft, prerelease, target_commitish.")]
    public static Task<string> Releases(
        [Description("Action list/latest/get/by_tag/create/update/delete.")] string action,
        [Description("Repository owner/name.")] string repository,
        [Description("Release id for get/update/delete.")] long release_id = 0,
        [Description("Tag name for by_tag.")] string? tag = null,
        [Description("JSON body for create/update.")] string? body_json = null) => Guard(() =>
    {
        string p=R(repository)+"/releases";
        return action switch
        {
            "list" => A("GET",p+"?per_page=100"),
            "latest" => A("GET",p+"/latest"),
            "get" => A("GET",p+"/"+release_id),
            "by_tag" => A("GET",p+"/tags/"+E(tag ?? throw new ArgumentException("tag required"))),
            "create" => A("POST",p,Body(body_json) ?? throw new ArgumentException("body_json required")),
            "update" => A("PATCH",p+"/"+release_id,Body(body_json) ?? throw new ArgumentException("body_json required")),
            "delete" => A("DELETE",p+"/"+release_id),
            _ => throw new ArgumentException("Unknown releases action")
        };
    });

    [McpServerTool(Name="github.release_assets", Title="GitHub · Файлы релизов")]
    [Description("Категория «GitHub»: list/upload/download/delete release assets. upload uses local file and uploads.github.com; download writes an artifact to absolute local file.")]
    public static Task<string> ReleaseAssets(
        [Description("Action list/upload/download/delete.")] string action,
        [Description("Repository owner/name.")] string repository,
        [Description("Release ID for list/upload.")] long release_id = 0,
        [Description("Asset ID for download/delete.")] long asset_id = 0,
        [Description("Existing local file for upload, absolute download destination otherwise.")] string? local_path = null,
        [Description("Optional release asset name for upload.")] string? asset_name = null) => Guard(() =>
    {
        string p=R(repository)+"/releases";
        return action switch
        {
            "list" => A("GET",p+"/"+release_id+"/assets?per_page=100"),
            "upload" => GithubRuntime.UploadReleaseAsset(repository,release_id,local_path ?? throw new ArgumentException("local_path required"),asset_name),
            "download" => GithubRuntime.Download(p+"/assets/"+asset_id,local_path ?? throw new ArgumentException("local_path required")),
            "delete" => A("DELETE",p+"/assets/"+asset_id),
            _ => throw new ArgumentException("Unknown assets action")
        };
    });

    [McpServerTool(Name="github.artifacts", Title="GitHub · Артефакты Actions")]
    [Description("Категория «GitHub»: list/get/download/delete workflow artifacts. Download saves archive ZIP locally.")]
    public static Task<string> Artifacts(
        [Description("Action list/get/download/delete.")] string action,
        [Description("Repository owner/name.")] string repository,
        [Description("Run ID for list (0 lists repository artifacts).")] long run_id = 0,
        [Description("Artifact id for get/download/delete.")] long artifact_id = 0,
        [Description("Absolute destination ZIP path for download.")] string? destination = null) => Guard(() =>
    {
        string p=R(repository)+"/actions/artifacts";
        return action switch
        {
            "list" => A("GET",run_id > 0 ? R(repository)+"/actions/runs/"+run_id+"/artifacts?per_page=100" : p+"?per_page=100"),
            "get" => A("GET",p+"/"+artifact_id),
            "download" => GithubRuntime.Download(p+"/"+artifact_id+"/zip",destination ?? throw new ArgumentException("destination required")),
            "delete" => A("DELETE",p+"/"+artifact_id),
            _ => throw new ArgumentException("Unknown artifacts action")
        };
    });

    [McpServerTool(Name="github.search", Title="GitHub · Поиск")]
    [Description("Категория «GitHub»: поиск code/issues/repositories/commits по GitHub Search API. q — поисковое выражение с repo:owner/name и другими фильтрами.")]
    public static Task<string> Search(
        [Description("Search kind: code, issues, repositories, commits, users.")] string kind,
        [Description("GitHub search expression.")] string q,
        [Description("Page number 1..100.")] int page = 1,
        [Description("Page size 1..100.")] int per_page = 30) => Guard(() =>
    {
        if (kind is not ("code" or "issues" or "repositories" or "commits" or "users"))
            throw new ArgumentException("Invalid search kind");
        if (page<1 || per_page<1 || per_page>100)throw new ArgumentOutOfRangeException(nameof(per_page));
        return A("GET","/search/"+kind+"?q="+E(q)+"&page="+page+"&per_page="+per_page);
    });

    [McpServerTool(Name="github.git", Title="GitHub · Локальный Git")]
    [Description("Категория «GitHub»: локальные git status/pull/fetch/checkout/log, clone или commit_push через заранее выбранные пути. Выполняется без shell-интерпретатора; commit_push требует paths_json и не добавляет посторонние файлы.")]
    public static Task<string> Git(
        [Description("Action: status/pull/fetch/checkout/log/clone/commit_push.")] string action,
        [Description("Absolute local repository path; for clone, destination folder that does not exist.")] string local_path,
        [Description("Repository owner/name for clone.")] string? repository = null,
        [Description("Branch name for checkout.")] string? branch = null,
        [Description("Commit message required for commit_push.")] string? message = null,
        [Description("JSON array of selected relative file paths for commit_push.")] string? paths_json = null) => Guard(async () =>
    {
        switch(action)
        {
            case "status": return await GithubRuntime.RunGit(local_path,"status","--short","--branch");
            case "pull": return await GithubRuntime.RunGit(local_path,"pull","--ff-only");
            case "fetch": return await GithubRuntime.RunGit(local_path,"fetch","--all","--prune");
            case "checkout": return await GithubRuntime.RunGit(local_path,"checkout",branch ?? throw new ArgumentException("branch required"));
            case "log": return await GithubRuntime.RunGit(local_path,"log","-12","--oneline","--decorate");
            case "clone":
            {
                string parent=Path.GetDirectoryName(local_path) ?? throw new ArgumentException("local_path invalid");
                if (Directory.Exists(local_path)) throw new InvalidOperationException("Clone target already exists.");
                if (!Directory.Exists(parent)) Directory.CreateDirectory(parent);
                return await GithubRuntime.RunGit(parent,"clone","https://github.com/"+repository+".git",local_path);
            }
            case "commit_push":
            {
                if (string.IsNullOrWhiteSpace(message)) throw new ArgumentException("message required");
                if (paths_json==null) throw new ArgumentException("paths_json required");
                var paths=JsonSerializer.Deserialize<string[]>(paths_json) ?? throw new ArgumentException("Invalid paths");
                if(paths.Length==0 || paths.Length>150 || paths.Any(x=>x.StartsWith('/') || x.Contains("..") || Path.IsPathRooted(x)))
                    throw new ArgumentException("Invalid path selections");
                var stage=await GithubRuntime.RunGit(local_path, (new[]{"add","--"}).Concat(paths).ToArray());
                if (JsonNode.Parse(stage)?["exit_code"]?.GetValue<int>()!=0) return stage;
                var commit=await GithubRuntime.RunGit(local_path,"commit","-m",message);
                if (JsonNode.Parse(commit)?["exit_code"]?.GetValue<int>()!=0) return commit;
                return await GithubRuntime.RunGit(local_path,"push");
            }
            default: throw new ArgumentException("Unknown Git action");
        }
    });
}
