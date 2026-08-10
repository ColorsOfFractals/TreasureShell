#define MyAppName "TreasureShell"
#define MyAppVersion "1.0.0"
#define MyAppExeName "TreasureShell.exe"

[Setup]
AppId={{D9A76DA6-24AA-4D4C-B6CD-0B5FFDD39442}
AppName={#MyAppName}
AppVersion={#MyAppVersion}

DefaultDirName={autopf}\TreasureShell
DefaultGroupName=TreasureShell

ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible

OutputDir=Output
OutputBaseFilename=TreasureShell-Setup

SetupIconFile=..\Assets\TreasureShell.ico
UninstallDisplayIcon={app}\TreasureShell.exe

Compression=lzma2
SolidCompression=yes

[Files]
Source: "..\bin\Release\net10.0-windows\win-x64\publish\TreasureShell.exe"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{group}\TreasureShell"; Filename: "{app}\TreasureShell.exe"
Name: "{autodesktop}\TreasureShell"; Filename: "{app}\TreasureShell.exe"; Tasks: desktopicon

[Tasks]
Name: "desktopicon"; Description: "Create a desktop shortcut"; GroupDescription: "Additional shortcuts:"; Flags: unchecked

[Run]
Filename: "{app}\TreasureShell.exe"; Description: "Launch TreasureShell"; Flags: nowait postinstall skipifsilent
