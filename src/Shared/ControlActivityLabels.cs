using System.Text.RegularExpressions;

// These are display-only identifiers, never command strings, paths, or arguments.
// The UI stores a short allowlisted key and resolves it to fixed Russian text.
public static class ControlActivityLabels
{
    private static readonly Dictionary<string, string> ShellNames = new(StringComparer.Ordinal)
    {
        ["git_status"] = "Проверяю состояние Git",
        ["git_history"] = "Изучаю историю изменений",
        ["git_clone"] = "Клонирую репозиторий",
        ["git_pull"] = "Получаю изменения Git",
        ["git_commit"] = "Сохраняю изменения в Git",
        ["git_push"] = "Публикую изменения в GitHub",
        ["source_read"] = "Изучаю исходный код",
        ["source_edit"] = "Изменяю исходные файлы",
        ["project_build"] = "Собираю .NET-проект",
        ["project_publish"] = "Готовлю сборку для выпуска",
        ["tests_run"] = "Запускаю тесты проекта",
        ["build_and_test"] = "Собираю проект и запускаю тесты",
        ["dependency_restore"] = "Восстанавливаю зависимости",
        ["files_check"] = "Проверяю файлы и папки",
        ["files_copy"] = "Копирую файлы",
        ["process_check"] = "Проверяю процессы Windows",
        ["system_check"] = "Проверяю состояние компьютера",
        ["network_check"] = "Проверяю сетевое соединение",
        ["download"] = "Загружаю файл",
        ["hash_verify"] = "Проверяю контрольную сумму файла",
        ["app_launch"] = "Запускаю приложение",
        ["app_update"] = "Устанавливаю обновление программы",
        ["notepad_open"] = "Открываю Блокнот",
        ["python_script"] = "Выполняю сценарий Python",
        ["powershell_script"] = "Выполняю сценарий PowerShell",
        ["service_check"] = "Проверяю службы Windows",
        ["service_change"] = "Управляю службой Windows",
        ["files_delete"] = "\u0423\u0434\u0430\u043b\u044f\u044e\u0020\u0444\u0430\u0439\u043b\u044b",
        ["process_stop"] = "\u0417\u0430\u0432\u0435\u0440\u0448\u0430\u044e\u0020\u043f\u0440\u043e\u0446\u0435\u0441\u0441\u044b",
        ["disk_change"] = "\u0418\u0437\u043c\u0435\u043d\u044f\u044e\u0020\u0440\u0430\u0437\u0434\u0435\u043b\u044b\u0020\u0434\u0438\u0441\u043a\u0430",
        ["system_change"] = "\u0418\u0437\u043c\u0435\u043d\u044f\u044e\u0020\u0441\u0438\u0441\u0442\u0435\u043c\u043d\u044b\u0435\u0020\u043d\u0430\u0441\u0442\u0440\u043e\u0439\u043a\u0438",
        ["activity_probe"] = "Проверяю работу индикатора",
        ["diagnostic"] = "Выполняю диагностику",
        ["cli_command"] = "Выполняю команду на компьютере",
    };

    private static readonly Dictionary<string, string> UiaNames = new(StringComparer.Ordinal)
    {
        ["invoke"] = "Нажимаю элемент интерфейса",
        ["set_value"] = "Заполняю поле приложения",
        ["select"] = "Выбираю элемент списка",
        ["toggle"] = "Переключаю параметр",
        ["expand"] = "Раскрываю элемент",
        ["collapse"] = "Сворачиваю элемент",
        ["focus"] = "Перевожу фокус на элемент",
        ["scroll_into_view"] = "Перехожу к элементу",
    };

    // Guards against corrupt user-facing resources (e.g. ????? caused by lossy encoding).
    public static bool DisplayLabelsValid =>
        ShellNames.Values.Concat(UiaNames.Values).All(value =>
            !string.IsNullOrWhiteSpace(value) && !value.Contains('?') &&
            value.Any(c => c >= '\u0400' && c <= '\u04ff'));

    public static string? Display(string tool, string? activity)
    {
        if (string.IsNullOrEmpty(activity)) return null;
        return tool switch
        {
            "shell.exec" when ShellNames.TryGetValue(activity, out var label) => label,
            "desktop.uia.action" when UiaNames.TryGetValue(activity, out var label) => label,
            _ => null
        };
    }

    public static string Shell(string command, string? requested = null)
    {
        // This method NEVER returns a substring of the command.
        // Match command verbs only; secrets, arbitrary strings and paths
        // are neither logged nor rendered by the overlay.
        var script = command;
        static bool Has(string script, string pattern) =>
            Regex.IsMatch(script, pattern,
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant,
                TimeSpan.FromMilliseconds(100));

        // Destructive actions override all hints and other classifications.
        if (Has(script, @"\b(?:Format-Volume|Clear-Disk|Remove-Partition|diskpart(?:\.exe)?)\b")) return "disk_change";
        if (Has(script, @"\b(?:Remove-Item|Remove-File|rmdir|rd\s+/[qs]|del\s+/[fsq])\b")) return "files_delete";
        if (Has(script, @"\b(?:Stop-Process|taskkill(?:\.exe)?|kill\s+-[0-9])\b")) return "process_stop";
        if (Has(script, @"\b(?:reg(?:\.exe)?\s+(?:delete|add)|Set-ItemProperty\s+HK(?:LM|CU):)\b")) return "system_change";
        var build = Has(script, @"\bdotnet(?:\.exe)?\s+build\b");
        var test = Has(script, @"\bdotnet(?:\.exe)?\s+test\b|\b(?:python(?:\.exe)?|py|node(?:\.exe)?)\s+(?:\.\\|\.\/)?tests[\\/]");
        if (requested is not null && ShellNames.ContainsKey(requested))
        {
            // A caller cannot relabel an identified operation as another one.
            // For opaque scripts, known safe categories describe the intended goal.
            if (!Has(script, @"\b(?:git(?:\.exe)?|dotnet(?:\.exe)?|Get-Process|Get-CimInstance|Set-Content|Get-Content)\b"))
                return requested;
        }
        if (build && test) return "build_and_test";
        if (Has(script, @"\bgit(?:\.exe)?\b[\s\S]{0,220}\bpush\b")) return "git_push";
        if (Has(script, @"\bgit(?:\.exe)?\b[\s\S]{0,220}\bcommit\b")) return "git_commit";
        if (Has(script, @"\bgit(?:\.exe)?\b[\s\S]{0,220}\bclone\b")) return "git_clone";
        if (Has(script, @"\bgit(?:\.exe)?\b[\s\S]{0,220}\bpull\b")) return "git_pull";
        if (Has(script, @"\bgit(?:\.exe)?\b[\s\S]{0,220}\bstatus\b")) return "git_status";
        if (Has(script, @"\bgit(?:\.exe)?\b[\s\S]{0,220}\b(?:log|show|diff)\b")) return "git_history";
        if (Has(script, @"\bdotnet(?:\.exe)?\s+publish\b")) return "project_publish";
        if (build) return "project_build";
        if (test) return "tests_run";
        if (Has(script, @"\bdotnet(?:\.exe)?\s+restore\b")) return "dependency_restore";
        if (Has(script, @"\b(Get-FileHash|certutil\s+-hashfile)\b")) return "hash_verify";
        if (Has(script, @"\b(?:Invoke-WebRequest|curl(?:\.exe)?)\b[\s\S]{0,150}\b(?:-OutFile|--output|-o)\b")) return "download";
        if (Has(script, @"\b(?:Test-NetConnection|Test-Connection|ping(?:\.exe)?|Resolve-DnsName)\b")) return "network_check";
        if (Has(script, @"\b(?:Set-Service|Start-Service|Stop-Service|Restart-Service)\b")) return "service_change";
        if (Has(script, @"\b(?:Get-Service|Get-WmiObject\s+Win32_Service)\b")) return "service_check";
        if (Has(script, @"\b(?:Get-CimInstance|Get-Process|tasklist(?:\.exe)?)\b")) return "process_check";
        if (Has(script, @"\b(?:Set-Content|Add-Content|WriteAllText|WriteAllBytes|Out-File)\b")) return "source_edit";
        if (Has(script, @"\b(?:Get-Content|Select-String|Get-FileHash|rg(?:\.exe)?|findstr(?:\.exe)?)\b")) return "source_read";
        if (Has(script, @"\b(?:Copy-Item|robocopy(?:\.exe)?|xcopy(?:\.exe)?)\b")) return "files_copy";
        if (Has(script, @"\bStart-Process[\s\S]{0,200}\bnotepad(?:\.exe)?\b")) return "notepad_open";
        if (Has(script, @"\b(?:Start-Process|Start-Job)\b")) return "app_launch";
        if (Has(script, @"\b(?:Get-ChildItem|Test-Path|Get-Item)\b")) return "files_check";
        if (Has(script, @"\b(?:python(?:\.exe)?|py)\s+(?:-c\b|[\w.\\/:-]+\.py\b)")) return "python_script";
        if (Has(script, @"\b(?:powershell(?:\.exe)?|pwsh(?:\.exe)?)\s+.*(?:-File|-Command)\b")) return "powershell_script";
        return "cli_command";
    }
}
