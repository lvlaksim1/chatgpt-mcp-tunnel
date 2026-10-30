using System.Text.RegularExpressions;

// Display-only attribution of a tool invocation. This is NOT a verified identity
// or an authentication/authorization mechanism. Absent metadata means ChatGPT.
public static class ControlExecutor
{
    private static readonly Dictionary<string, string> Known = new(StringComparer.OrdinalIgnoreCase)
    {
        ["chatgpt"] = "ChatGPT",
        ["project-manager"] = "Менеджер проекта",
        ["supervisor"] = "Супервизор"
    };

    public static string Normalize(string? executor)
    {
        if (string.IsNullOrWhiteSpace(executor)) return "chatgpt";
        var value = executor.Trim();
        if (Known.ContainsKey(value)) return value.ToLowerInvariant();
        // Explicitly declared, bounded personalized executor display name.
        // Disallow control characters, paths, URLs and punctuation that could
        // leak arbitrary tool arguments or impersonate the shell output.
        if (value.StartsWith("persona:", StringComparison.Ordinal) &&
            value.Length is >= 10 and <= 55)
        {
            var label = value["persona:".Length..];
            if (Regex.IsMatch(label,
                @"\A[\p{L}][\p{L}\p{N} _.-]{1,39}\z",
                RegexOptions.CultureInvariant, TimeSpan.FromMilliseconds(100)))
                return value;
        }
        return "chatgpt";
    }

    public static string Display(string? executor)
    {
        var id = Normalize(executor);
        if (Known.TryGetValue(id, out var label)) return label;
        return id.StartsWith("persona:", StringComparison.Ordinal)
            ? id["persona:".Length..] : "ChatGPT";
    }
}
