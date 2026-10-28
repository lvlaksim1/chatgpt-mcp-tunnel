#ifndef AppVersion
  #define AppVersion "0.1.0"
#endif
#ifndef PublishDir
  #define PublishDir "..\publish"
#endif
#ifndef OutputDir
  #define OutputDir "..\dist"
#endif
#define AppName "ChatGPT MCP Agent"
#define AppExeName "ChatGptMcpAgent.exe"
#define AppId "{{E4CEB6E3-91C8-44D1-8A2E-2F309DA8F40A}"

[Setup]
AppId={#AppId}
AppName={#AppName}
AppVersion={#AppVersion}
AppPublisher=lvlaksim1
AppPublisherURL=https://github.com/lvlaksim1/chatgpt-mcp-tunnel
AppSupportURL=https://github.com/lvlaksim1/chatgpt-mcp-tunnel/issues
DefaultDirName={localappdata}\Programs\ChatGPTMCPAgent
DisableDirPage=yes
DisableProgramGroupPage=yes
DisableWelcomePage=yes
DisableReadyPage=yes
DisableFinishedPage=yes
DirExistsWarning=no
UsePreviousAppDir=no
OutputDir={#OutputDir}
OutputBaseFilename=ChatGPT_MCP_Agent_Update_v{#AppVersion}
Compression=lzma2/max
SolidCompression=yes
WizardStyle=modern
PrivilegesRequired=lowest
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
UninstallDisplayIcon={app}\{#AppExeName}
CloseApplications=yes
RestartApplications=no
SetupLogging=no
VersionInfoVersion={#AppVersion}
VersionInfoProductName={#AppName}
VersionInfoDescription=ChatGPT MCP Agent update installer

[Files]
Source: "{#PublishDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

; Preserve existing settings, DPAPI credentials, autostart and all shortcuts.
; The update creates no new shortcuts and leaves desktop icon positions intact.
[Registry]
Root: HKCU; Subkey: "Software\ChatGPTMCPAgent"; Flags: uninsdeletekey

[UninstallRun]
Filename: "{app}\{#AppExeName}"; Parameters: "--shutdown"; Flags: runhidden waituntilterminated

[UninstallDelete]
Type: filesandordirs; Name: "{localappdata}\ChatGPTMCPAgent"
Type: filesandordirs; Name: "{userappdata}\ChatGPTMCPAgent"
Type: files; Name: "{autoprograms}\ChatGPT MCP Agent.lnk"
Type: files; Name: "{userdesktop}\ChatGPT MCP Agent.lnk"

[Run]
Filename: "{app}\{#AppExeName}"; Flags: nowait runhidden

[Code]
function InitializeSetup(): Boolean;
var
  InstalledExe: String;
begin
  InstalledExe := ExpandConstant('{localappdata}\Programs\ChatGPTMCPAgent\{#AppExeName}');
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

procedure CurUninstallStepChanged(CurUninstallStep: TUninstallStep);
begin
  if CurUninstallStep = usUninstall then
    RegDeleteValue(HKCU, 'Software\Microsoft\Windows\CurrentVersion\Run', 'ChatGPTMCPAgent');
end;
