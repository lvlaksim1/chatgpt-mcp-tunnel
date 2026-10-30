using System.Diagnostics;
using System.Windows.Forms;

namespace ChatGptMcpAgent;

internal static class ControlTests
{
    internal static int TestTrayIntegration()
    {
        var original = Environment.GetEnvironmentVariable("CHATGPT_MCP_CONTROL_TEST_ROOT");
        var root = Path.Combine(Path.GetTempPath(), "mcp-tray-overlay-" + Guid.NewGuid().ToString("N"));
        Environment.SetEnvironmentVariable("CHATGPT_MCP_CONTROL_TEST_ROOT", root);
        try
        {
            ApplicationConfiguration.Initialize();
            using var shutdown = new EventWaitHandle(false, EventResetMode.ManualReset);
            using var settings = new EventWaitHandle(false, EventResetMode.AutoReset);
            using var ctx = new TrayContext(shutdown, settings, diagnosticMode: true);
            using var activity = ControlState.Begin("shell.exec", executor: "project-manager");
            var visible = false;
            using var timer = new System.Windows.Forms.Timer { Interval = 2600 };
            timer.Tick += (_, _) =>
            {
                timer.Stop();
                visible = !ctx.HasControlMenuItem && ctx.ControlOverlayVisible &&
                    ctx.ControlOverlayExecutorCaption == "Исполнитель: Менеджер проекта" &&
                    ctx.ControlOverlayVersionCaption.EndsWith(Application.ProductVersion.Split('+')[0], StringComparison.Ordinal);
                ctx.ExitThread();
            };
            timer.Start();
            Application.Run(ctx);
            return visible ? 0 : 72;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine("TRAY_OVERLAY_TEST_ERROR " + ex.GetType().Name);
            return 73;
        }
        finally
        {
            Environment.SetEnvironmentVariable("CHATGPT_MCP_CONTROL_TEST_ROOT", original);
            try { if (Directory.Exists(root)) Directory.Delete(root, true); }
            catch (IOException) { }
        }
    }

    internal static int TestAutomaticOverlay()
    {
        var original = Environment.GetEnvironmentVariable("CHATGPT_MCP_CONTROL_TEST_ROOT");
        var temp = Path.Combine(Path.GetTempPath(), "mcp-overlay-auto-" + Guid.NewGuid().ToString("N"));
        Environment.SetEnvironmentVariable("CHATGPT_MCP_CONTROL_TEST_ROOT", temp);
        try
        {
            ApplicationConfiguration.Initialize();
            using var activity = ControlState.Begin("shell.exec", executor: "project-manager");
            using var ctx = new ApplicationContext();
            using var overlay = new ControlOverlay();
            var visible = false;
            using var stopCheck = new System.Windows.Forms.Timer { Interval = 2200 };
            stopCheck.Tick += (_, _) =>
            {
                stopCheck.Stop();
                visible = overlay.NativeVisible &&
                    overlay.ExecutorCaption == "Исполнитель: Менеджер проекта" &&
                    overlay.VersionCaption.EndsWith(Application.ProductVersion.Split('+')[0], StringComparison.Ordinal);
                // X must hide, never dispose the tray-owned window.
                overlay.Close();
                overlay.Poll();
                var closedSafely = !overlay.IsDisposed && !overlay.NativeVisible;
                overlay.ShowStatus();
                visible = visible && closedSafely && overlay.NativeVisible && !overlay.IsDisposed;
                ctx.ExitThread();
            };
            stopCheck.Start();
            Application.Run(ctx);
            return visible ? 0 : 71;
        }
        finally
        {
            Environment.SetEnvironmentVariable("CHATGPT_MCP_CONTROL_TEST_ROOT", original);
            try { if (Directory.Exists(temp)) Directory.Delete(temp, true); }
            catch (IOException) { }
        }
    }
    internal static int Run()
    {
        var original = Environment.GetEnvironmentVariable("CHATGPT_MCP_CONTROL_TEST_ROOT");
        var temp = Path.Combine(Path.GetTempPath(), "mcp-control-test-" + Guid.NewGuid().ToString("N"));
        Environment.SetEnvironmentVariable("CHATGPT_MCP_CONTROL_TEST_ROOT", temp);
        try
        {
            // Human-readable descriptions are derived from allowlisted keys,
            // never copied from potentially sensitive command arguments.
            if (ControlActivityLabels.Shell("dotnet build src/Test.csproj") != "project_build") return 81;
            if (ControlActivityLabels.Shell("git -C D:/Dev/Test status --short") != "git_status") return 82;
            if (ControlActivityLabels.Shell("python tests/test_mcp_stdio.py") != "tests_run") return 83;
            if (ControlActivityLabels.Shell("dotnet build Test.csproj; python tests/test_mcp_stdio.py") != "build_and_test") return 84;
            if (ControlActivityLabels.Shell("git push origin main") != "git_push") return 85;
            if (ControlActivityLabels.Shell("Remove-Item x.txt; git status", "git_status") != "files_delete") return 90;
            if (ControlActivityLabels.Shell("Stop-Process -Id 123", "project_build") != "process_stop") return 91;
            if (ControlActivityLabels.Display("desktop.uia.action", "set_value") is null) return 86;
            if (ControlActivityLabels.Display("shell.exec", "password=secret") is not null) return 87;
            var sensitive = "Set-Content -Path C:/secret.txt -Value 'MY_PRIVATE_TOKEN_TEST_123'";
            using (var safe = ControlState.Begin("shell.exec", activity: ControlActivityLabels.Shell(sensitive))) { }
            using (var bad = ControlState.Begin("shell.exec", activity: "MY_PRIVATE_TOKEN_TEST_123")) { }
            var persisted = File.ReadAllText(ControlState.StateFile);
            if (persisted.Contains("MY_PRIVATE_TOKEN_TEST_123", StringComparison.Ordinal) ||
                persisted.Contains("C:/secret.txt", StringComparison.Ordinal))
                return 88;
            if (ControlState.Read().Recent[0].Activity != "") return 89;
            if (!ControlActivityLabels.DisplayLabelsValid) return 92;
            if (ControlActivityLabels.Display("shell.exec", "files_delete") != "Удаляю файлы") return 94;
            if (ControlActivityLabels.Display("shell.exec", "process_stop") != "Завершаю процессы") return 95;
            if (ControlActivityLabels.Display("shell.exec", "disk_change") != "Изменяю разделы диска") return 96;
            if (ControlActivityLabels.Display("shell.exec", "system_change") != "Изменяю системные настройки") return 97;
            if (ControlExecutor.Normalize(null) != "chatgpt") return 101;
            if (ControlExecutor.Display(null) != "ChatGPT") return 102;
            if (ControlExecutor.Display("project-manager") != "Менеджер проекта") return 103;
            if (ControlExecutor.Display("supervisor") != "Супервизор") return 104;
            if (ControlExecutor.Display("persona:Старший разработчик") != "Старший разработчик") return 105;
            if (ControlExecutor.Normalize("token=SECRET_99") != "chatgpt") return 106;
            using (ControlState.Begin("shell.exec", executor: "project-manager")) { }
            using (ControlState.Begin("shell.exec", executor: null)) { }
            using (ControlState.Begin("shell.exec", executor: "supervisor")) { }
            var actorRecords = ControlState.Read().Recent.Take(3).Select(x => x.Executor).ToArray();
            if (!actorRecords.SequenceEqual(new[] { "supervisor", "chatgpt", "project-manager" })) return 107;
            if (File.ReadAllText(ControlState.StateFile).Contains("SECRET_99", StringComparison.Ordinal)) return 108;
            if (ControlState.IsStopped) return 61;
            using (var operation = ControlState.Begin("shell.exec")) { }
            ControlState.Stop();
            if (!ControlState.Read().Stopped || !File.Exists(ControlState.StateFile)) return 62;
            try
            {
                using var unexpected = ControlState.Begin("shell.exec");
                return 63;
            }
            catch (OperationCanceledException) { }
            using (var read = ControlState.Begin("system.info", readOnly: true)) { }
            ControlState.Resume();
            if (ControlState.IsStopped) return 64;
            using (var gui = ControlState.Begin("desktop.mouse.click", guiAction: true)) { }
            var clock = Stopwatch.StartNew();
            using (var gui = ControlState.Begin("desktop.mouse.click", guiAction: true)) { }
            if (clock.ElapsedMilliseconds < 880 || clock.ElapsedMilliseconds > 1600) return 65;
            clock.Restart();
            using (var cli = ControlState.Begin("shell.exec")) { }
            if (clock.ElapsedMilliseconds > 500) return 66;
            // A remote STOP during a pacing interval must prevent the next mutation.
            using (var gui = ControlState.Begin("desktop.mouse.click", guiAction: true)) { }
            var pending = Task.Run(() =>
            {
                try { using var x = ControlState.Begin("desktop.mouse.click", guiAction: true); return false; }
                catch (OperationCanceledException) { return true; }
            });
            Thread.Sleep(120);
            ControlState.Stop();
            if (!pending.GetAwaiter().GetResult()) return 67;
            if (ControlState.Read().Active.Count != 0) return 68;
            Application.EnableVisualStyles();
            using var overlay = new ControlOverlay();
            if (overlay.Handle == IntPtr.Zero || !overlay.TopMost ||
                !overlay.Controls.OfType<Button>().Any()) return 69;
            ControlState.Resume();
            Console.WriteLine("CONTROL_STOP_PERSISTENCE_GUI_PACING_CLI_SPEED_OVERLAY_OK");
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine("CONTROL_TEST_ERROR " + ex.GetType().Name);
            return 70;
        }
        finally
        {
            Environment.SetEnvironmentVariable("CHATGPT_MCP_CONTROL_TEST_ROOT", original);
            try { if (Directory.Exists(temp)) Directory.Delete(temp, true); }
            catch (IOException) { }
        }
    }
}
