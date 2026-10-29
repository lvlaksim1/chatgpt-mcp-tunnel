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
            using var activity = ControlState.Begin("shell.exec");
            var visible = false;
            using var timer = new System.Windows.Forms.Timer { Interval = 2600 };
            timer.Tick += (_, _) =>
            {
                timer.Stop();
                visible = ctx.ControlOverlayVisible;
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
            using var activity = ControlState.Begin("shell.exec");
            using var ctx = new ApplicationContext();
            using var overlay = new ControlOverlay();
            var visible = false;
            using var stopCheck = new System.Windows.Forms.Timer { Interval = 2200 };
            stopCheck.Tick += (_, _) =>
            {
                stopCheck.Stop();
                visible = overlay.NativeVisible;
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
