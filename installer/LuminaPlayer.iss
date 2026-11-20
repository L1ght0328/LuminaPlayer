#define MyAppName "Lumina Player"
#define MyAppVersion "1.3.0"
#define MyAppPublisher "Lumina Player"
#define MyAppExeName "LuminaPlayer.exe"

[Setup]
AppId={{C7E16D55-6F8D-4C53-A8D7-3D5E7F1A9C21}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
DefaultDirName={autopf}\Lumina Player
DefaultGroupName=Lumina Player
DisableProgramGroupPage=yes
OutputDir=..\dist
OutputBaseFilename=LuminaPlayer-Setup-win-x64
ArchitecturesInstallIn64BitMode=x64
ArchitecturesAllowed=x64
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
UninstallDisplayIcon={app}\{#MyAppExeName}
PrivilegesRequired=admin

[Files]
Source: "..\publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{autoprograms}\Lumina Player"; Filename: "{app}\{#MyAppExeName}"
Name: "{autodesktop}\Lumina Player"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Tasks]
Name: "desktopicon"; Description: "Criar atalho na área de trabalho"; GroupDescription: "Atalhos:"

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "Abrir Lumina Player"; Flags: postinstall nowait skipifsilent
