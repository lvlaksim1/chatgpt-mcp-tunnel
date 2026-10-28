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
DefaultGroupName=ChatGPT MCP Agent
DisableDirPage=yes
DisableProgramGroupPage=yes
DirExistsWarning=no
UsePreviousAppDir=no
OutputDir={#OutputDir}
OutputBaseFilename=ChatGPT_MCP_Agent_Setup_v{#AppVersion}
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
VersionInfoDescription=ChatGPT MCP Agent full installer

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
Source: "{#PublishDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{autoprograms}\ChatGPT MCP Agent"; Filename: "{app}\{#AppExeName}"; WorkingDir: "{app}"
Name: "{userdesktop}\ChatGPT MCP Agent"; Filename: "{app}\{#AppExeName}"; WorkingDir: "{app}"; Tasks: desktopicon

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
Filename: "{app}\{#AppExeName}"; Description: "Запустить ChatGPT MCP Agent"; Flags: nowait postinstall skipifsilent

[Code]
procedure CurUninstallStepChanged(CurUninstallStep: TUninstallStep);
begin
  if CurUninstallStep = usUninstall then
    RegDeleteValue(HKCU, 'Software\Microsoft\Windows\CurrentVersion\Run', 'ChatGPTMCPAgent');
end;
