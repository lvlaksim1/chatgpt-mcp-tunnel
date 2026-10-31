[CmdletBinding()]
param(
  [Parameter(Mandatory=$true)][string]$Kit,
  [Parameter(Mandatory=$true)][string]$InstallDir,
  [Parameter(Mandatory=$true)][string]$Version,
  [switch]$Preflight
)
$ErrorActionPreference='Stop'
Set-StrictMode -Version Latest
function Require([bool]$condition,[string]$message){if(-not $condition){throw $message}}
Require ($Version -match '^(?:0|[1-9][0-9]*)\.(?:0|[1-9][0-9]*)\.(?:0|[1-9][0-9]*)$') 'Invalid install version'
$role=(Get-ItemProperty 'HKCU:\Software\ChatGPTMCPAgent' -ErrorAction SilentlyContinue).Role
# Inno checks the selected server role for new installations; the script
# refuses client targets independently after role has been registered.
if($role -eq 'client'){
  if($Preflight){
    # This is not used on clients by Inno. Defensive refusal for direct calls.
    throw 'RecoveryKit is exclusively for a Router Lite server; client role rejected'
  }
  throw 'RecoveryKit installation blocked on client role'
}
if(-not $Preflight){Require ($role -eq 'server') 'RecoveryKit installation requires server role'}
$Kit=[IO.Path]::GetFullPath($Kit)
$InstallDir=[IO.Path]::GetFullPath($InstallDir)
if(-not $Preflight){Require (Test-Path -LiteralPath (Join-Path $InstallDir 'ChatGptMcpAgent.exe') -PathType Leaf) 'Main program must be installed'}
$verifier=Join-Path $Kit 'Verify-RecoveryKit.ps1'
Require (Test-Path -LiteralPath $verifier -PathType Leaf) 'RecoveryKit companion missing or incomplete'
& powershell.exe -NoProfile -NonInteractive -ExecutionPolicy Bypass -File $verifier -Kit $Kit -ExpectedVersion $Version | Out-Null
Require ($LASTEXITCODE -eq 0) 'RecoveryKit verification failed'
if($Preflight){
  [pscustomobject]@{status='PREFLIGHT_OK';version=$Version;serverOnly=$true}|ConvertTo-Json -Compress
  return
}
$manifest=Get-Content -LiteralPath (Join-Path $Kit 'recovery-manifest.json') -Raw -Encoding UTF8|ConvertFrom-Json
$root=Join-Path $InstallDir 'RecoveryKit'
$versions=Join-Path $root 'Versions'
New-Item -ItemType Directory -Force -Path $versions|Out-Null
$lock=Join-Path $root '.install-lock'
Require (-not(Test-Path -LiteralPath $lock)) 'RecoveryKit installer already active'
New-Item -ItemType Directory -Path $lock -ErrorAction Stop |Out-Null
try {
  $destination=Join-Path $versions ('v'+$Version)
  if(Test-Path -LiteralPath $destination){Require (-not((Get-Item -LiteralPath $destination -Force).Attributes -band [IO.FileAttributes]::ReparsePoint)) 'Symlinked RecoveryKit destination is forbidden'}
  $stage=$null
  if(Test-Path -LiteralPath $destination) {
    & powershell.exe -NoProfile -NonInteractive -ExecutionPolicy Bypass -File (Join-Path $destination 'Verify-RecoveryKit.ps1') -Kit $destination -ExpectedVersion $Version | Out-Null
    Require ($LASTEXITCODE -eq 0) 'Previous RecoveryKit files are corrupted'
    $existingHash=(Get-FileHash -LiteralPath (Join-Path $destination 'recovery-manifest.json') -Algorithm SHA256).Hash
    $incomingHash=(Get-FileHash -LiteralPath (Join-Path $Kit 'recovery-manifest.json') -Algorithm SHA256).Hash
    Require ($existingHash -ieq $incomingHash) 'RecoveryKit version conflict; refuse overwrite'
  } else {
    $stage=Join-Path $versions ('.staging-'+[guid]::NewGuid().ToString('N'))
    New-Item -ItemType Directory -Path $stage -ErrorAction Stop|Out-Null
    try {
      foreach($item in (Get-ChildItem -LiteralPath $Kit -File)) {
        Copy-Item -LiteralPath $item.FullName -Destination $stage -ErrorAction Stop
      }
      & powershell.exe -NoProfile -NonInteractive -ExecutionPolicy Bypass -File (Join-Path $stage 'Verify-RecoveryKit.ps1') -Kit $stage -ExpectedVersion $Version|Out-Null
      Require ($LASTEXITCODE -eq 0) 'Staged RecoveryKit failed independent integrity verification'
      Move-Item -LiteralPath $stage -Destination $destination -ErrorAction Stop
      $stage=$null
    }finally{if($stage -and(Test-Path -LiteralPath $stage)){Remove-Item -LiteralPath $stage -Recurse -Force -ErrorAction SilentlyContinue}}
  }
  $index=[ordered]@{
    schema='chatgpt-mcp-tunnel-recovery-sidecar-v1'
    latestVersion=$Version
    relativePath=('Versions/v'+$Version)
    sourceCommit=$manifest.sourceCommit
    manifestSha256=(Get-FileHash -LiteralPath (Join-Path $destination 'recovery-manifest.json') -Algorithm SHA256).Hash.ToLowerInvariant()
    createdUtc=(Get-Date).ToUniversalTime().ToString('o')
    containsSecrets=$false
    semantics='latest verified installed server release'
  }
  $latest=Join-Path $root 'latest.json'
  $temp=Join-Path $root ('.latest-'+[guid]::NewGuid().ToString('N')+'.tmp')
  try{
    [IO.File]::WriteAllText($temp,($index|ConvertTo-Json -Depth 5),[Text.UTF8Encoding]::new($false))
    if(Test-Path -LiteralPath $latest){
      $backup=Join-Path $root ('.latestbak-'+[guid]::NewGuid().ToString('N'))
      try{[IO.File]::Replace($temp,$latest,$backup)}
      finally{if(Test-Path -LiteralPath $backup){Remove-Item -LiteralPath $backup -Force}}
    }else{[IO.File]::Move($temp,$latest)}
  }finally{if(Test-Path -LiteralPath $temp){Remove-Item -LiteralPath $temp -Force}}
  & powershell.exe -NoProfile -NonInteractive -ExecutionPolicy Bypass -File (Join-Path $destination 'Verify-RecoveryKit.ps1') -InstallDir $InstallDir -ExpectedVersion $Version |Out-Null
  Require ($LASTEXITCODE -eq 0) 'Installed server RecoveryKit verification failed'
  [pscustomobject]@{status='INSTALLED';role='server';version=$Version;installDir=$InstallDir}|ConvertTo-Json -Compress
}finally{
  Remove-Item -LiteralPath $lock -Force -ErrorAction SilentlyContinue
}
