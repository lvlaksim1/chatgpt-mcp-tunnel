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

[Files]
Source: "{#PublishDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "ServerStart.ps1"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{autoprograms}\ChatGPT MCP Agent"; Filename: "{app}\{#AppExeName}"; WorkingDir: "{app}"
Name: "{userdesktop}\ChatGPT MCP Agent"; Filename: "{app}\{#AppExeName}"; WorkingDir: "{app}"; Tasks: desktopicon

[Registry]
Root: HKCU; Subkey: "Software\ChatGPTMCPAgent"; Flags: uninsdeletekey

[UninstallRun]
Filename: "{app}\{#AppExeName}"; Parameters: "--shutdown"; Flags: runhidden waituntilterminated; RunOnceId: "ChatGptMcpAgentShutdown"

[UninstallDelete]
Type: filesandordirs; Name: "{localappdata}\ChatGPTMCPAgent"
Type: filesandordirs; Name: "{userappdata}\ChatGPTMCPAgent"
Type: files; Name: "{autoprograms}\ChatGPT MCP Agent.lnk"
Type: files; Name: "{userdesktop}\ChatGPT MCP Agent.lnk"

[Run]
Filename: "{app}\{#AppExeName}"; Description: "Запустить ChatGPT MCP Agent"; Flags: nowait postinstall skipifsilent

[Code]
var
  RolePage: TInputOptionWizardPage;

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
end;

procedure CurStepChanged(CurStep: TSetupStep);
var
  Name: String;
begin
  if CurStep <> ssPostInstall then exit;
  Name := 'client';
  if RolePage.Values[0] then Name := 'server';
  RegWriteStringValue(HKCU, 'Software\ChatGPTMCPAgent', 'Role', Name);
  RegWriteStringValue(HKCU, 'Software\ChatGPTMCPAgent',
    'InstallDir', ExpandConstant('{app}'));

  if WizardIsTaskSelected('autostart') then
    RegWriteStringValue(HKCU,
      'Software\Microsoft\Windows\CurrentVersion\Run',
      'ChatGPTMCPAgent',
      '"' + ExpandConstant('{app}\{#AppExeName}') + '" --minimized')
  else
    RegDeleteValue(HKCU, 'Software\Microsoft\Windows\CurrentVersion\Run',
      'ChatGPTMCPAgent');
end;

procedure CurUninstallStepChanged(CurUninstallStep: TUninstallStep);
begin
  if CurUninstallStep = usUninstall then
    RegDeleteValue(HKCU, 'Software\Microsoft\Windows\CurrentVersion\Run', 'ChatGPTMCPAgent');
end;
