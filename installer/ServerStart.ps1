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
[Console]::Error.WriteLine('Router Lite launching at ' + $env:CHATGPT_MCP_ROUTER_LISTEN)
# Do not merge native stderr into the PowerShell error stream here.
# MCP writes informational startup messages to stderr; Windows PowerShell 5.1
# with ErrorActionPreference=Stop treats those as terminating NativeCommandError.
$previousPreference = $ErrorActionPreference
try {
    $ErrorActionPreference = 'Continue'
    & $cli run --profile $profileName --profile-dir $profileDir --health.url-file $health --log.file $log
    $runCode = $LASTEXITCODE
} finally {
    $ErrorActionPreference = $previousPreference
}
if ($runCode -ne 0) { throw "Official tunnel-client exited: $runCode" }
