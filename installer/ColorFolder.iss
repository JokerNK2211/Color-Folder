; Inno Setup script for Color Folder. Per-user install, no admin needed.
#define AppVersion GetEnv("APP_VERSION")
#if AppVersion == ""
  #define AppVersion "0.1.0"
#endif

[Setup]
AppId={{6B3A8C2E-6C1B-4D3B-9E57-0C7F2B1D9A41}
AppName=Color Folder
AppVersion={#AppVersion}
AppPublisher=Color Folder
DefaultDirName={localappdata}\Programs\ColorFolder
DefaultGroupName=Color Folder
PrivilegesRequired=lowest
DisableProgramGroupPage=yes
OutputDir=..\dist
OutputBaseFilename=ColorFolder-Setup-{#AppVersion}
SetupIconFile=..\icons\mac\folders\sky.ico
UninstallDisplayIcon={app}\ColorFolder.exe
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
ArchitecturesInstallIn64BitMode=x64compatible
ArchitecturesAllowed=x64compatible

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "contextmenu"; Description: "Add ""Color Folder"" to the right-click menu for folders"; Flags: checkedonce
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; Flags: unchecked

[Files]
Source: "..\publish\*"; DestDir: "{app}"; Flags: recursesubdirs ignoreversion

[Icons]
Name: "{autoprograms}\Color Folder"; Filename: "{app}\ColorFolder.exe"
Name: "{autodesktop}\Color Folder"; Filename: "{app}\ColorFolder.exe"; Tasks: desktopicon

[Run]
Filename: "{app}\ColorFolder.exe"; Parameters: "--register"; Tasks: contextmenu; Flags: runhidden
Filename: "{app}\ColorFolder.exe"; Description: "{cm:LaunchProgram,Color Folder}"; Flags: nowait postinstall skipifsilent

[UninstallRun]
Filename: "{app}\ColorFolder.exe"; Parameters: "--reset-all"; RunOnceId: "ResetAll"
Filename: "{app}\ColorFolder.exe"; Parameters: "--unregister"; RunOnceId: "Unregister"; Flags: runhidden
