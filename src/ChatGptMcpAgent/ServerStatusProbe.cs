using System.Net;
using System.Net.Http;
using System.Net.Sockets;

namespace ChatGptMcpAgent;

internal enum ServerConnectivity
{
    TunnelUnavailable,
    RouterUnavailable,
    Ready
}

/// <summary>
/// A server cannot infer its OpenAI/control-plane state from a client agent's
/// " connected." stderr message. Use bounded, independent local readiness
/// checks and never inspect credentials or write response bodies to logs.
/// </summary>
internal static class ServerStatusProbe
{
    private static readonly HttpClient Health = new(new HttpClientHandler
    {
        UseProxy = false,
        AllowAutoRedirect = false
    })
    {
        Timeout = TimeSpan.FromSeconds(2)
    };

    internal static string Describe(ServerConnectivity state) => state switch
    {
        ServerConnectivity.Ready => "Сервер подключён",
        ServerConnectivity.RouterUnavailable => "Маршрутизатор не готов",
        _ => "Туннель не готов"
    };

    internal static async Task<ServerConnectivity> CheckAsync(
        string listenAddress, CancellationToken cancellationToken)
    {
        try
        {
            using var response = await Health.GetAsync(
                "http://127.0.0.1:55198/readyz",
                HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            if (response.StatusCode != HttpStatusCode.OK)
                return ServerConnectivity.TunnelUnavailable;
        }
        catch (Exception e) when (e is HttpRequestException or TaskCanceledException)
        {
            return ServerConnectivity.TunnelUnavailable;
        }

        // The router endpoint is locally owned by the bundled MCP runtime.
        // Connecting to TCP only, without WebSocket upgrade or auth, never
        // creates a remote agent session and never exposes credentials.
        if (!IPAddress.TryParse(listenAddress, out var address))
            return ServerConnectivity.RouterUnavailable;
        try
        {
            using var tcp = new TcpClient(address.AddressFamily);
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(TimeSpan.FromSeconds(1));
            await tcp.ConnectAsync(address, 55201, timeout.Token);
            return ServerConnectivity.Ready;
        }
        catch (Exception e) when (e is SocketException or OperationCanceledException)
        {
            return ServerConnectivity.RouterUnavailable;
        }
    }
}
