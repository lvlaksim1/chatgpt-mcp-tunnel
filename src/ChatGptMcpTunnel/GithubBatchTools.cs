using System.ComponentModel;
using System.Text;
using System.Text.Json.Nodes;
using ModelContextProtocol.Server;

[McpServerToolType]
public static class GithubBatchTools
{
    [McpServerTool(Name="github.batch_commit", Title="GitHub · Атомарный коммит нескольких файлов")]
    [Description("Категория «GitHub»: создать один GitHub commit из множества изменений файлов через Git data API. Без локального checkout. changes_json: [{path,content}] для текста, [{path,base64}] для бинарных файлов или [{path,delete:true}]. Проверяет ожидаемый head SHA и публикует fast-forward без force.")]
    public static async Task<string> BatchCommit(
        [Description("Repository owner/name.")] string repository,
        [Description("Existing target branch e.g. main or feature/test.")] string branch,
        [Description("Git commit message.")] string message,
        [Description("JSON array of up to 100 changes: {path,content} or {path,base64} or {path,delete:true}.")] string changes_json,
        [Description("Optional expected current branch head SHA to prevent stale writes.")] string? expected_head = null)
    {
        try
        {
            if(string.IsNullOrWhiteSpace(message)) throw new ArgumentException("message required");
            var arr=JsonNode.Parse(changes_json) as JsonArray ?? throw new ArgumentException("changes_json must be JSON array");
            if(arr.Count<1 || arr.Count>100) throw new ArgumentException("Provide between 1 and 100 file changes");
            string repo=GithubRuntime.Repo(repository);
            string branchPath=string.Join("/",branch.Split('/').Select(GithubRuntime.Segment));
            var reference=await GithubRuntime.Api("GET",repo+"/git/ref/heads/"+branchPath);
            string parent=reference?["object"]?["sha"]?.ToString() ?? throw new InvalidOperationException("Head SHA not found");
            if(expected_head!=null && !string.Equals(parent,expected_head,StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Branch has changed since expected_head; refusing commit.");
            var commit=await GithubRuntime.Api("GET",repo+"/git/commits/"+parent);
            string baseTree=commit?["tree"]?["sha"]?.ToString() ?? throw new InvalidOperationException("Tree SHA not found");
            var tree = new List<Dictionary<string,object?>>();
            var seen=new HashSet<string>(StringComparer.Ordinal);
            foreach(var row in arr)
            {
                if(row is not JsonObject obj)throw new ArgumentException("Each change must be object");
                string path=obj["path"]?.ToString() ?? throw new ArgumentException("Missing path");
                if(path.Length==0 || path.StartsWith('/') || path.Contains("..") || path.Contains('\\') || !seen.Add(path))
                    throw new ArgumentException("Invalid/duplicate change path: "+path);
                bool delete=obj["delete"]?.ToString().Equals("true",StringComparison.OrdinalIgnoreCase) == true;
                string? sha = null;
                if (!delete)
                {
                    string? content=(string?)obj["content"];
                    string? encoded=(string?)obj["base64"];
                    if((content==null)==(encoded==null))throw new ArgumentException("Exactly one of content or base64 required for "+path);
                    byte[] bytes=content!=null?Encoding.UTF8.GetBytes(content):Convert.FromBase64String(encoded!);
                    if(bytes.Length>2_000_000)throw new ArgumentException("Individual file exceeds 2 MB limit: "+path);
                    var blob=await GithubRuntime.Api("POST",repo+"/git/blobs",new {
                        content=Convert.ToBase64String(bytes),encoding="base64"
                    });
                    sha=blob?["sha"]?.ToString() ?? throw new InvalidOperationException("Blob creation failed");
                }
                tree.Add(new Dictionary<string,object?> {
                    ["path"]=path, ["mode"]="100644", ["type"]="blob", ["sha"]=sha
                });
            }
            var newTree=await GithubRuntime.Api("POST",repo+"/git/trees",new {base_tree=baseTree,tree});
            var treeSha=newTree?["sha"]?.ToString() ?? throw new InvalidOperationException("Git tree failed");
            var newCommit=await GithubRuntime.Api("POST",repo+"/git/commits",new {message,tree=treeSha,parents=new[]{parent}});
            string shaCommit=newCommit?["sha"]?.ToString() ?? throw new InvalidOperationException("Git commit creation failed");
            // GitHub checks fast-forward by default; a concurrent branch update rejects this operation.
            await GithubRuntime.Api("PATCH",repo+"/git/refs/heads/"+branchPath,new {sha=shaCommit,force=false});
            return GithubRuntime.Json(new {
                success=true, repository, branch, previous_head=parent,
                commit_sha=shaCommit, files_changed=tree.Count,
                url="https://github.com/"+repository+"/commit/"+shaCommit
            });
        }
        catch(Exception ex) { return GithubRuntime.Json(new {success=false,error=ex.Message}); }
    }
}
