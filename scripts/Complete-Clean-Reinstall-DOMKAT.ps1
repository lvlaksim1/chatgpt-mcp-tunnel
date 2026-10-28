# Runs OUTSIDE the old tunnel. Never calls OpenAI tunnel creation APIs.
# A user launches Start-Clean-Install.cmd after staging verified installer.
$ErrorActionPreference = 'Stop'
$stage = $PSScriptRoot
$log = Join-Path $stage 'clean-install-result.log'
function Report([string] $step) {
    $message = (Get-Date -Format 'yyyy-MM-dd HH:mm:ss') + ' ' + $step
    Add-Content -LiteralPath $log -Value $message -Encoding UTF8
    Write-Host $message
}
try {
    if ($env:COMPUTERNAME -ne 'DOMKAT') { throw 'This operation is limited to DOMKAT.' }
    $idFile = Join-Path $stage 'existing-tunnel-id.txt'
    if (!(Test-Path $idFile)) { throw 'Existing tunnel identity missing. Stop.' }
    $tunnelId = (Get-Content -LiteralPath $idFile -Raw).Trim()
    if ($tunnelId -notmatch '^tunnel_(?:[a-z0-9]{4}_)?[a-z0-9]{32}$') {
        throw 'Existing logical tunnel identity invalid. Stop.'
    }
    $installers = @(Get-ChildItem $stage -Filter 'ChatGPT_MCP_Tunnel_Setup_v*.exe' -File)
    if ($installers.Count -ne 1) { throw 'Expected one staged installer, nothing will be stopped.' }
    $installer = $installers[0].FullName
    $digestFile = Join-Path $stage 'installer-sha256.txt'
    if (!(Test-Path $digestFile)) { throw 'Missing staged installer SHA256. Old tunnel unchanged.' }
    $expectedDigest = (Get-Content -LiteralPath $digestFile -Raw).Trim()
    $actualDigest = (Get-FileHash -LiteralPath $installer -Algorithm SHA256).Hash.ToLowerInvariant()
    if ($actualDigest -ne $expectedDigest) { throw 'Staged installer digest mismatch; old tunnel unchanged.' }
    $target = 'D:\ChatGPTMCPAgent'
    if (!(Test-Path 'D:\')) { throw 'Drive D: is unavailable. No changes.' }
    $oldProject = 'D:\ChatGPT-MCP-Tunnel'
    $oldCli = Join-Path $oldProject '.tools\tunnel-client-v0.0.18\tunnel-client.exe'
    if (!(Test-Path $oldCli)) { throw 'Old tunnel installation not recognized. No changes.' }
    $newExe = Join-Path $target 'ChatGptMcpAgent.exe'
    if (Test-Path $newExe) { throw 'New server is already installed. Stop to avoid clobbering it.' }
    Report 'STAGED: existing logical Tunnel ID retained, new installer ready.'
    # Disable old automatic launch first so the legacy tunnel cannot restart after removal.
    $task = Get-ScheduledTask -TaskName 'ChatGPT MCP Tunnel' -ErrorAction SilentlyContinue
    if ($task) {
        Disable-ScheduledTask -TaskName 'ChatGPT MCP Tunnel' -ErrorAction Stop | Out-Null
        Report 'Old scheduled autostart disabled.'
    }
    # Stop only the old installation, do not kill other tunnel-client instances.
    $oldProcesses = @(Get-CimInstance Win32_Process |
        Where-Object { $_.Name -eq 'tunnel-client.exe' -and
            $_.ExecutablePath -and
            $_.ExecutablePath.Equals($oldCli,[StringComparison]::OrdinalIgnoreCase) })
    foreach ($item in $oldProcesses) {
        Stop-Process -Id $item.ProcessId -Force -ErrorAction Stop
    }
    $deadline = (Get-Date).AddSeconds(15)
    do {
        Start-Sleep -Milliseconds 250
        $alive = @(Get-CimInstance Win32_Process | Where-Object {
            $_.Name -eq 'tunnel-client.exe' -and
            $_.ExecutablePath -and
            $_.ExecutablePath.Equals($oldCli,[StringComparison]::OrdinalIgnoreCase)
        })
    } while ($alive.Count -gt 0 -and (Get-Date) -lt $deadline)
    if ($alive.Count) { throw 'Old tunnel-client did not stop. Do not remove files.' }
    Report 'Old local tunnel-client stopped.'
    if ($task) {
        Unregister-ScheduledTask -TaskName 'ChatGPT MCP Tunnel' -Confirm:$false -ErrorAction Stop
        Report 'Old scheduled task removed.'
    }
    # Remove old, user-specific secrets/profile. Keep the developer's Git repository.
    foreach ($item in @(
       (Join-Path $env:LOCALAPPDATA 'ChatGptMcpTunnel'),
       (Join-Path $oldProject '.tools\tunnel-profiles'),
       (Join-Path $oldProject '.tools\tunnel-client-v0.0.18')
    )) {
        if (Test-Path -LiteralPath $item) {
            Remove-Item -LiteralPath $item -Force -Recurse -ErrorAction Stop
        }
    }
    Report 'Old runtime, profiles and stored authorization removed. Source Git repository retained.'
    # Existing Tunnel ID belongs to the cloud registration and MUST NOT be recreated.
    # Install silently so the config can be prefilled before the first GUI launch.
    $args = @('/VERYSILENT','/SUPPRESSMSGBOXES','/NORESTART','/ROLE=server',
              '/TASKS=autostart',('/DIR='+$target))
    $process = Start-Process -FilePath $installer -ArgumentList $args -PassThru
    if (!$process.WaitForExit(240000)) {
        $process.Kill()
        throw 'New installer timed out.'
    }
    if ($process.ExitCode -ne 0) { throw "Installer failed with exit code $($process.ExitCode)." }
    if (!(Test-Path $newExe)) { throw 'New executable missing after successful installer.' }
    Report 'Universal ChatGPT MCP Tunnel installed on D: in Server role.'
    $settingsFolder = Join-Path $env:LOCALAPPDATA 'ChatGPTMCPAgent'
    New-Item -ItemType Directory -Force -Path $settingsFolder | Out-Null
    $address = '127.0.0.1'
    $network = @(Get-NetIPAddress -AddressFamily IPv4 -ErrorAction SilentlyContinue |
        Where-Object { $_.AddressState -eq 'Preferred' -and
           $_.IPAddress -like '192.168.*' } | Select-Object -First 1)
    if ($network.Count -gt 0) { $address = $network[0].IPAddress }
    $settings = @{
        Computer = $env:COMPUTERNAME
        RouterUrl = ''
        AutoStart = $true
        Role = 'server'
        TunnelId = $tunnelId
        ListenAddress = $address
    }
    $settings | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $settingsFolder 'settings.json') -Encoding UTF8
    Report 'Existing tunnel identity set; NO credential imported. Enter new token in server window.'
    Start-Process -FilePath $newExe -WorkingDirectory $target
    Report 'New server settings window requested. Finish setup by entering your token.'
    Report 'CLEAN_INSTALL_COMPLETED (awaiting token).'
} catch {
    Report ('ERROR: '+$_.Exception.Message)
    Report 'No new logical OpenAI tunnel was created. Review the error before retrying.'
    exit 1
}
