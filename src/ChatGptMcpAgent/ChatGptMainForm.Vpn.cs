using System.Text.Json;
using System.Windows.Forms;

namespace ChatGptMcpAgent;

internal sealed partial class ChatGptMainForm
{
    internal event Action<VpnConnectionMode>? VpnModeChanged;

    internal void SelectVpnModeFromTray(VpnConnectionMode mode)
    {
        using var doc = JsonDocument.Parse(JsonSerializer.Serialize(new {
            mode = mode == VpnConnectionMode.FreeReality ? "free" : "happ"
        }));
        HandleVpnChromeMessage(doc.RootElement);
    }

    private async void HandleVpnChromeMessage(JsonElement data)
    {
        if (!data.TryGetProperty("mode", out var next)) return;
        var requested = next.GetString() switch
        {
            "happ" => VpnConnectionMode.Happ,
            "free" => VpnConnectionMode.FreeReality,
            _ => (VpnConnectionMode?)null
        };
        if (requested is null || requested == VpnModeController.Current) return;

        if (requested == VpnConnectionMode.FreeReality)
        {
            if (!VpnModeController.ProfileReady(out var issue))
            {
                MessageBox.Show(this, issue, "Бесплатные серверы", MessageBoxButtons.OK,
                                MessageBoxIcon.Warning);
                return;
            }
            var decision = MessageBox.Show(this,
                "Бесплатные серверы принадлежат неизвестным операторам.\n\n" +
                "Оператор видит адреса подключений, но не содержимое HTTPS. " +
                "Для использования режима потребуется пересоздать окно ChatGPT; " +
                "используется текущий авторизованный профиль.\n\n" +
                "Переключиться на бесплатные серверы?",
                "Переключение VPN",
                MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (decision != DialogResult.Yes) return;
            // Prepare the server-side route BEFORE reconstructing the WebView2
            // environment with new proxy flags on the same authorized profile.
            VpnModeController.IsTransitioning = true;
            try
            {
                VpnModeController.Save(VpnConnectionMode.FreeReality);
                if (!await VpnModeController.ProbeFreeViaBridgeAsync())
                {
                    if (VpnModeController.Current == VpnConnectionMode.FreeReality)
                        VpnModeController.Save(VpnConnectionMode.Happ);
                    if (!IsDisposed)
                        MessageBox.Show(this,
                            "Бесплатный сервер не ответил. Соединение Happ сохранено.",
                            "VPN не подключён", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
            }
            finally { VpnModeController.IsTransitioning = false; }
        }
        else VpnModeController.Save(VpnConnectionMode.Happ);
        if (!IsDisposed && VpnModeController.Current == requested.Value)
            VpnModeChanged?.Invoke(requested.Value);
    }
}
