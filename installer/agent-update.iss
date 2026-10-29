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

[Files]
Source: "{#PublishDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "ServerStart.ps1"; DestDir: "{app}"; Flags: ignoreversion

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
Filename: "{app}\{#AppExeName}"; Flags: nowait runhidden

[Code]
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
