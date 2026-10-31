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
DefaultDirName={code:GetDefaultInstallDir}
DefaultGroupName=ChatGPT MCP Tunnel
DisableDirPage=no
DisableProgramGroupPage=yes
DirExistsWarning=no
UsePreviousAppDir=yes
OutputDir={#OutputDir}
OutputBaseFilename=ChatGPT_MCP_Tunnel_Setup_v{#AppVersion}
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
VersionInfoDescription=ChatGPT MCP Tunnel universal installer

[Tasks]
Name: "autostart"; Description: "Запускать автоматически при входе в Windows"; Flags: checkedonce
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[InstallDelete]
Type: files; Name: "{app}\Runtime\BrowserChatBridge.js"
Type: files; Name: "{app}\OpenAI\node.exe"
Type: files; Name: "{app}\OpenAI\LICENSE.node.txt"

[Files]
Source: "{#PublishDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "ServerStart.ps1"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\scripts\Recovery\Install-RecoveryKit.ps1"; Flags: dontcopy
Source: "..\scripts\Recovery\Client-FreeXraySupervisor.ps1"; DestDir: "{app}\Recovery"; Flags: ignoreversion
Source: "..\scripts\Recovery\Install-ClientFreeVpnTasks.ps1"; DestDir: "{app}\Recovery"; Flags: ignoreversion
Source: "..\scripts\Vpn\free_reality_updater.py"; DestDir: "{app}\Recovery\scripts\Vpn"; Flags: ignoreversion
Source: "..\scripts\Vpn\free_reality_sources.json"; DestDir: "{app}\Recovery\scripts\Vpn"; Flags: ignoreversion

[Icons]
Name: "{autoprograms}\ChatGPT MCP Agent"; Filename: "{app}\{#AppExeName}"; WorkingDir: "{app}"
Name: "{userdesktop}\ChatGPT MCP Agent"; Filename: "{app}\{#AppExeName}"; WorkingDir: "{app}"; Tasks: desktopicon

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
; Registration takes place after settings are finalized by CurStepChanged.
Filename: "{app}\{#AppExeName}"; Description: "Запустить ChatGPT MCP Agent"; Flags: nowait postinstall skipifsilent

[Code]
var
  RolePage: TInputOptionWizardPage;
  AdminPage: TInputOptionWizardPage;

function ExistingInstallDir(): String;
begin
  Result := '';
  RegQueryStringValue(HKCU, 'Software\ChatGPTMCPAgent', 'InstallDir', Result);
  if (Result <> '') and not FileExists(AddBackslash(Result) + '{#AppExeName}') then
    Result := '';
  if (Result = '') and
     FileExists(ExpandConstant('{localappdata}\Programs\ChatGPTMCPAgent\{#AppExeName}')) then
    Result := ExpandConstant('{localappdata}\Programs\ChatGPTMCPAgent');
end;

function RoleDefaultInstallDir(Server: Boolean): String;
begin
  if Server and DirExists('D:\') then
    Result := 'D:\ChatGPTMCPAgent'
  else
    Result := ExpandConstant('{localappdata}\Programs\ChatGPTMCPAgent');
end;

function GetDefaultInstallDir(Param: String): String;
var
  Role: String;
begin
  Result := ExistingInstallDir();
  if Result <> '' then exit;
  Role := ExpandConstant('{param:ROLE|}');
  if (Role <> 'server') and (Role <> 'client') then
    RegQueryStringValue(HKCU, 'Software\ChatGPTMCPAgent', 'Role', Role);
  Result := RoleDefaultInstallDir(Role = 'server');
end;

function NextButtonClick(CurPageID: Integer): Boolean;
var
  CurrentDir, ClientDefault, ServerDefault: String;
begin
  Result := True;
  if CurPageID <> RolePage.ID then exit;
  if ExistingInstallDir() <> '' then exit;
  CurrentDir := RemoveBackslashUnlessRoot(WizardForm.DirEdit.Text);
  ClientDefault := RemoveBackslashUnlessRoot(RoleDefaultInstallDir(False));
  ServerDefault := RemoveBackslashUnlessRoot(RoleDefaultInstallDir(True));
  // Preserve a manually selected destination; only change the suggested one.
  if (CompareText(CurrentDir, ClientDefault) = 0) or
     (CompareText(CurrentDir, ServerDefault) = 0) then
    WizardForm.DirEdit.Text := RoleDefaultInstallDir(RolePage.Values[0]);
end;

procedure InitializeWizard;
var
  SavedRole: String;
begin
  RolePage := CreateInputOptionPage(wpWelcome,
    'Роль компьютера', 'Выберите режим работы ChatGPT MCP Tunnel',
    'Сервер подключается к ChatGPT и управляет компьютерами. ' +
    'Клиент подключается к серверу и выполняет команды.', True, False);
  RolePage.Add('Сервер — основной компьютер с маршрутизатором');
  RolePage.Add('Клиент — дополнительный управляемый компьютер');
  SavedRole := '';
  RegQueryStringValue(HKCU, 'Software\ChatGPTMCPAgent', 'Role', SavedRole);
  if (ExpandConstant('{param:ROLE|}') = 'server') or
     (ExpandConstant('{param:ROLE|}') = 'client') then
    SavedRole := ExpandConstant('{param:ROLE|}');
  RolePage.Values[0] := SavedRole = 'server';
  RolePage.Values[1] := SavedRole <> 'server';
  AdminPage := CreateInputOptionPage(RolePage.ID,
    'Полномочия установки', 'Способ установки',
    'Обычная установка не требует повышения прав. При выборе опции Windows запросит UAC.',
    False, False);
  AdminPage.Add('Установить с правами администратора');
  AdminPage.Values[0] := False;
end;

procedure CurStepChanged(CurStep: TSetupStep);
var
  Name: String;
  RecoveryExitCode: Integer;
  KitExitCode: Integer;
begin
  if CurStep <> ssPostInstall then exit;
  Name := 'client';
  if RolePage.Values[0] then Name := 'server';
  RegWriteStringValue(HKCU, 'Software\ChatGPTMCPAgent', 'Role', Name);
  RegWriteStringValue(HKCU, 'Software\ChatGPTMCPAgent',
    'InstallDir', ExpandConstant('{app}'));

  // RecoveryKit installs only with the main SERVER role (never on client).
  if Name = 'server' then
  begin
    ExtractTemporaryFile('Install-RecoveryKit.ps1');
    if not Exec(ExpandConstant('{sys}\WindowsPowerShell\v1.0\powershell.exe'),
      '-NoProfile -NonInteractive -ExecutionPolicy Bypass -File "' +
      ExpandConstant('{tmp}\Install-RecoveryKit.ps1') + '" -Kit "' +
      ExpandConstant('{src}\RecoveryKit') + '" -InstallDir "' +
      ExpandConstant('{app}') + '" -Version "{#AppVersion}"',
      '', SW_HIDE, ewWaitUntilTerminated, KitExitCode) or (KitExitCode <> 0) then
      RaiseException('Server RecoveryKit could not be installed');
  end;

  if WizardIsTaskSelected('autostart') then
    RegWriteStringValue(HKCU,
      'Software\Microsoft\Windows\CurrentVersion\Run',
      'ChatGPTMCPAgent',
      '"' + ExpandConstant('{app}\{#AppExeName}') + '" --minimized')
  else
    RegDeleteValue(HKCU, 'Software\Microsoft\Windows\CurrentVersion\Run',
      'ChatGPTMCPAgent');

  // InstallDir, role and autostart now exist: registering in [Run] was too early.
  if Exec(ExpandConstant('{app}\{#AppExeName}'), '--watchdog-register',
     ExpandConstant('{app}'), SW_HIDE, ewWaitUntilTerminated, RecoveryExitCode) then
    Log('Recovery task registration exit=' + IntToStr(RecoveryExitCode))
  else
    Log('Recovery task registration launch failed');
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
    if RolePage.Values[0] then ParamLine := ParamLine + ' /ROLE=server'
    else ParamLine := ParamLine + ' /ROLE=client';
    if not ShellExec('runas', ExpandConstant('{srcexe}'), ParamLine, '',
       SW_SHOW, ewNoWait, ExitCode) then
      Result := 'Не удалось запросить повышение прав: ' + SysErrorMessage(ExitCode)
    else
      Result := 'Запущен установщик с правами администратора. Закройте это окно.';
    exit;
  end;
  // Verify matching companion before touching the existing installation.
  if RolePage.Values[0] then
  begin
    if not FileExists(ExpandConstant('{src}\RecoveryKit\Verify-RecoveryKit.ps1')) then
    begin
      Result := 'Для серверной установки необходим RecoveryKit рядом с Setup.';
      exit;
    end;
    if not Exec(ExpandConstant('{sys}\WindowsPowerShell\v1.0\powershell.exe'),
       '-NoProfile -NonInteractive -ExecutionPolicy Bypass -File "' +
       ExpandConstant('{src}\RecoveryKit\Verify-RecoveryKit.ps1') + '" -Kit "' +
       ExpandConstant('{src}\RecoveryKit') + '" -ExpectedVersion "{#AppVersion}"',
       '', SW_HIDE, ewWaitUntilTerminated, ExitCode) or (ExitCode <> 0) then
    begin
      Result := 'Серверный RecoveryKit повреждён или имеет другую версию.';
      exit;
    end;
  end;
  // Solve Inno Setup's endless wait for a running Tunnel:
  // stop the agent by its IPC event, wait for mutex release, then install.
  InstalledExe := AddBackslash(ExistingInstallDir()) + '{#AppExeName}';
  if FileExists(InstalledExe) then
  begin
    if not Exec(InstalledExe, '--shutdown', '', SW_HIDE,
       ewWaitUntilTerminated, ExitCode) then
      Result := 'Не удалось закрыть MCP Tunnel: ' + SysErrorMessage(ExitCode)
    else if ExitCode <> 0 then
      Result := 'MCP Tunnel не завершился, код ' + IntToStr(ExitCode);
  end;
end;
