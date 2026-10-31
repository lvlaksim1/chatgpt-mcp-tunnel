#ifndef AppVersion
  #define AppVersion "0.1.0"
#endif
#ifndef PublishDir
  #define PublishDir "..\publish"
#endif
#ifndef OutputDir
  #define OutputDir "..\dist"
#endif
#define AppName "ChatGPT MCP Tunnel"
#define AppExeName "ChatGptMcpAgent.exe"
#define AppId "{{E4CEB6E3-91C8-44D1-8A2E-2F309DA8F40A}"

[Setup]
AppId={#AppId}
AppName={#AppName}
AppVersion={#AppVersion}
AppPublisher=lvlaksim1
AppPublisherURL=https://github.com/lvlaksim1/chatgpt-mcp-tunnel
AppSupportURL=https://github.com/lvlaksim1/chatgpt-mcp-tunnel/issues
DefaultDirName={code:GetInstalledDir}
DisableDirPage=yes
DisableProgramGroupPage=yes
DisableWelcomePage=yes
DisableReadyPage=yes
DisableFinishedPage=yes
DirExistsWarning=no
UsePreviousAppDir=yes
OutputDir={#OutputDir}
OutputBaseFilename=ChatGPT_MCP_Tunnel_Update_v{#AppVersion}
Compression=lzma2/max
SolidCompression=yes
WizardStyle=modern
SetupIconFile={#SourcePath}..\src\ChatGptMcpAgent\Resources\App.ico
PrivilegesRequired=lowest
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
UninstallDisplayIcon={app}\{#AppExeName}
CloseApplications=yes
RestartApplications=no
SetupLogging=no
VersionInfoVersion={#AppVersion}
VersionInfoProductName={#AppName}
VersionInfoDescription=ChatGPT MCP Tunnel update installer

[InstallDelete]
Type: files; Name: "{app}\Runtime\BrowserChatBridge.js"
Type: files; Name: "{app}\OpenAI\node.exe"
Type: files; Name: "{app}\OpenAI\LICENSE.node.txt"

[Files]
Source: "{#PublishDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "ServerStart.ps1"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\scripts\Recovery\Client-FreeXraySupervisor.ps1"; DestDir: "{app}\Recovery"; Flags: ignoreversion
Source: "..\scripts\Recovery\Install-ClientFreeVpnTasks.ps1"; DestDir: "{app}\Recovery"; Flags: ignoreversion
Source: "..\scripts\Vpn\free_reality_updater.py"; DestDir: "{app}\Recovery\scripts\Vpn"; Flags: ignoreversion
Source: "..\scripts\Vpn\free_reality_sources.json"; DestDir: "{app}\Recovery\scripts\Vpn"; Flags: ignoreversion

; Preserve existing settings, DPAPI credentials, autostart and all shortcuts.
; The update creates no new shortcuts and leaves desktop icon positions intact.
[Registry]
Root: HKCU; Subkey: "Software\ChatGPTMCPAgent"; Flags: uninsdeletekey

[UninstallRun]
Filename: "{app}\{#AppExeName}"; Parameters: "--watchdog-unregister"; Flags: runhidden waituntilterminated; RunOnceId: "ChatGptMcpWatchdogUnregister"
Filename: "{app}\{#AppExeName}"; Parameters: "--shutdown"; Flags: runhidden waituntilterminated; RunOnceId: "ChatGptMcpAgentShutdown"

[UninstallDelete]
Type: filesandordirs; Name: "{localappdata}\ChatGPTMCPAgent"
Type: filesandordirs; Name: "{userappdata}\ChatGPTMCPAgent"
Type: files; Name: "{autoprograms}\ChatGPT MCP Agent.lnk"
Type: files; Name: "{userdesktop}\ChatGPT MCP Agent.lnk"

[Run]
; Integrated recovery registration is performed in ssPostInstall after installation completes.
Filename: "{app}\{#AppExeName}"; Flags: nowait

[Code]
var
  AdminPage: TInputOptionWizardPage;

procedure InitializeWizard;
begin
  AdminPage := CreateInputOptionPage(wpWelcome,
    'Полномочия обновления', 'Способ обновления',
    'По умолчанию обновление выполняется без повышения прав.',
    False, False);
  AdminPage.Add('Установить с правами администратора');
  AdminPage.Values[0] := False;
end;

function GetInstalledDir(Param: String): String;
begin
  Result := '';
  RegQueryStringValue(HKCU, 'Software\ChatGPTMCPAgent', 'InstallDir', Result);
  if Result = '' then
    Result := ExpandConstant('{localappdata}\Programs\ChatGPTMCPAgent');
end;

function InitializeSetup(): Boolean;
var
  InstalledExe: String;
begin
  InstalledExe := AddBackslash(GetInstalledDir('')) + '{#AppExeName}';
  if not FileExists(InstalledExe) then
  begin
    MsgBox('ChatGPT MCP Agent не установлен.' + #13#10 +
      'Для первой установки используйте ChatGPT_MCP_Agent_Setup.',
      mbError, MB_OK);
    Result := False;
    exit;
  end;
  Result := True;
end;

procedure CurStepChanged(CurStep: TSetupStep);
var
  RecoveryExitCode: Integer;
begin
  if CurStep <> ssPostInstall then exit;
  if Exec(ExpandConstant('{app}\{#AppExeName}'), '--watchdog-register',
     ExpandConstant('{app}'), SW_HIDE, ewWaitUntilTerminated, RecoveryExitCode) then
    Log('Update watchdog registration exit=' + IntToStr(RecoveryExitCode))
  else
    Log('Update watchdog registration could not start');
end;

procedure CurUninstallStepChanged(CurUninstallStep: TUninstallStep);
begin
  if CurUninstallStep = usUninstall then
    RegDeleteValue(HKCU, 'Software\Microsoft\Windows\CurrentVersion\Run', 'ChatGPTMCPAgent');
end;


function PrepareToInstall(var NeedsRestart: Boolean): String;
var
  ExitCode: Integer;
  InstalledExe: String;
  ParamLine: String;
begin
  Result := '';
  if not WizardSilent and AdminPage.Values[0] and not IsAdminLoggedOn then
  begin
    ParamLine := '/ADMIN_RESTART /DIR="' + WizardDirValue + '"';

    if not ShellExec('runas', ExpandConstant('{srcexe}'), ParamLine, '',
       SW_SHOW, ewNoWait, ExitCode) then
      Result := 'Не удалось запросить повышение прав: ' + SysErrorMessage(ExitCode)
    else
      Result := 'Запущен установщик с правами администратора. Закройте это окно.';
    exit;
  end;
  // Solve Inno Setup's endless wait for a running Tunnel:
  // stop the agent by its IPC event, wait for mutex release, then install.
  InstalledExe := AddBackslash(GetInstalledDir('')) + '{#AppExeName}';
  if FileExists(InstalledExe) then
  begin
    if not Exec(InstalledExe, '--shutdown', '', SW_HIDE,
       ewWaitUntilTerminated, ExitCode) then
      Result := 'Не удалось закрыть MCP Tunnel: ' + SysErrorMessage(ExitCode)
    else if ExitCode <> 0 then
      Result := 'MCP Tunnel не завершился, код ' + IntToStr(ExitCode);
  end;
end;
