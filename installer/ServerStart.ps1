# This script is bundled in the universal installer for the Server role.
# All secrets arrive as environment variables from DPAPI; none are written into repo files.
$ErrorActionPreference = 'Stop'
$base = $PSScriptRoot
$local = Join-Path $env:LOCALAPPDATA 'ChatGPTMCPAgent\Server'
$profileDir = Join-Path $local 'profiles'
$profileName = 'chatgpt-mcp-tunnel'
$cli = Join-Path $base 'OpenAI\tunnel-client.exe'
$dll = Join-Path $base 'Runtime\ChatGptMcpTunnel.dll'
$health = Join-Path $local 'health.url'
$log = Join-Path $local 'runtime.log'
if (!(Test-Path $cli)) { throw "Missing official tunnel-client: $cli" }
if (!(Test-Path $dll)) { throw "Missing MCP Runtime: $dll" }
if (!$env:CONTROL_PLANE_API_KEY -or !$env:CHATGPT_MCP_TUNNEL_ID) { throw 'OpenAI tunnel settings are absent.' }
if (!$env:CHATGPT_MCP_AGENT_TOKEN) { throw 'Router agent key is absent.' }
# Optional process-scoped proxy for a future tunnel-client launch.
# The live MCP connection is never restarted by this setting.
$proxyPortText = $env:CHATGPT_MCP_TUNNEL_PROXY_PORT
if (-not [string]::IsNullOrWhiteSpace($proxyPortText)) {
    $proxyPort = 0
    if (-not [int]::TryParse($proxyPortText, [ref]$proxyPort) -or
        $proxyPort -lt 1025 -or $proxyPort -gt 65535 -or
        $proxyPort -in @(10808, 10809)) {
        throw 'Invalid tunnel-client proxy port (loopback only).'
    }
    $probe = New-Object System.Net.Sockets.TcpClient
    try {
        $async = $probe.BeginConnect('127.0.0.1', $proxyPort, $null, $null)
        if (-not $async.AsyncWaitHandle.WaitOne(550)) {
            throw 'Selected local proxy is unreachable.'
        }
        $probe.EndConnect($async)
    } finally { $probe.Dispose() }
    $localProxy = 'http://127.0.0.1:' + $proxyPort
    $env:HTTPS_PROXY = $localProxy
    $env:HTTP_PROXY = $localProxy
    $env:NO_PROXY = '127.0.0.1,localhost,::1'
    [Console]::Error.WriteLine('Tunnel-client HTTP(S) proxy configured on loopback port ' + $proxyPort)
    # HTTP(S)_PROXY does not guarantee routing for QUIC/UDP.
    # Verify the actual control-plane transport before production use.
}
New-Item -ItemType Directory -Force -Path $profileDir | Out-Null
$mcpDllArgument = $dll.Replace([char]92, [char]47)
$launchCommand = 'dotnet "' + $mcpDllArgument + '"'
# Packaged runtime is self-contained; direct exe works on PCs without .NET.
$runtimeExe = Join-Path $base 'Runtime\ChatGptMcpTunnel.exe'
if (Test-Path $runtimeExe) { $launchCommand = '"' + $runtimeExe.Replace([char]92, [char]47) + '"' }
$profilePath = Join-Path $profileDir ($profileName + '.yaml')
if (!(Test-Path -LiteralPath $profilePath)) {
    & $cli init --sample sample_mcp_stdio_local --profile $profileName --profile-dir $profileDir --tunnel-id $env:CHATGPT_MCP_TUNNEL_ID --mcp-command $launchCommand --health-listen-addr 127.0.0.1:55198 --force 2>&1 | ForEach-Object { [Console]::Error.WriteLine($_) }
    if ($LASTEXITCODE -ne 0) { throw "Tunnel profile setup failed: $LASTEXITCODE" }
}
& $cli doctor --profile $profileName --profile-dir $profileDir --explain 2>&1 | ForEach-Object { [Console]::Error.WriteLine($_) }
if ($LASTEXITCODE -ne 0) { throw "Tunnel doctor failed: $LASTEXITCODE" }
& $cli admin tunnels get $env:CHATGPT_MCP_TUNNEL_ID 1>$null 2>&1
if ($LASTEXITCODE -ne 0) { throw 'The configured OpenAI key cannot access this tunnel.' }
# DOMKAT only: stable local HTTP CONNECT proxy for existing tunnel-client.
$bridgeExe = Join-Path $base 'ChatGptMcpProxyBridge.exe'
if (!(Test-Path -LiteralPath $bridgeExe)) {
    throw 'Missing server bridge; refusing direct fallback.'
}
$bridgePort = 11827
$binding = @(Get-NetTCPConnection -State Listen -LocalAddress '127.0.0.1' -LocalPort $bridgePort -ErrorAction SilentlyContinue)
if ($binding.Count -gt 1) { throw 'Ambiguous bridge listener.' }
if ($binding.Count -eq 1) {
    $owner = Get-CimInstance Win32_Process -Filter "ProcessId=$($binding[0].OwningProcess)"
    if (!$owner -or $owner.ExecutablePath -ine $bridgeExe) {
        throw 'Untrusted process owns MCP bridge port.'
    }
} else {
    $bridgeOut = Join-Path $local 'bridge.stdout.log'
    $bridgeErr = Join-Path $local 'bridge.stderr.log'
    $child = Start-Process -FilePath $bridgeExe -WorkingDirectory $base -PassThru -WindowStyle Hidden -RedirectStandardOutput $bridgeOut -RedirectStandardError $bridgeErr
    $ready = $false
    for ($n=0; $n -lt 40; $n++) {
        if ($child.HasExited) { throw 'MCP proxy bridge exited early.' }
        if (Get-NetTCPConnection -State Listen -LocalAddress '127.0.0.1' -LocalPort $bridgePort -OwningProcess $child.Id -ErrorAction SilentlyContinue) {
            $ready = $true
            break
        }
        Start-Sleep -Milliseconds 125
    }
    if (!$ready) {
        Stop-Process -Id $child.Id -Force -ErrorAction SilentlyContinue
        throw 'MCP proxy bridge unavailable.'
    }
}
[Console]::Error.WriteLine('MCP outbound proxy bridge ready on 127.0.0.1:11827')

[Console]::Error.WriteLine('Router Lite launching at ' + $env:CHATGPT_MCP_ROUTER_LISTEN)
# Do not merge native stderr into the PowerShell error stream here.
# MCP writes informational startup messages to stderr; Windows PowerShell 5.1
# with ErrorActionPreference=Stop treats those as terminating NativeCommandError.
$previousPreference = $ErrorActionPreference
try {
    $ErrorActionPreference = 'Continue'
    & $cli run --profile $profileName --profile-dir $profileDir --health.url-file $health --log.file $log --http-proxy http://127.0.0.1:11827
    $runCode = $LASTEXITCODE
} finally {
    $ErrorActionPreference = $previousPreference
}
if ($runCode -ne 0) { throw "Official tunnel-client exited: $runCode" }
