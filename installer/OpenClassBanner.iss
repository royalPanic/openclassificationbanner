#ifndef AppVersion
  #define AppVersion "1.0.0"
#endif

#ifndef PublishDir
  #define PublishDir "..\OpenClassBanner\bin\Release\net8.0-windows\win-x64\publish"
#endif

#define AppName "Open Class Banner"
#define AppExeName "OpenClassBanner.exe"

[Setup]
AppId={{A8D219DB-A2D2-4C24-92A9-58BB5985930E}
AppName={#AppName}
AppVersion={#AppVersion}
DefaultDirName={autopf}\OpenClassBanner
DefaultGroupName={#AppName}
DisableProgramGroupPage=yes
PrivilegesRequired=admin
PrivilegesRequiredOverridesAllowed=dialog
ArchitecturesAllowed=x64
ArchitecturesInstallIn64BitMode=x64
MinVersion=10.0.14393
OutputDir=..\artifacts\installer
OutputBaseFilename=OpenClassBanner-Setup
UninstallDisplayIcon={app}\{#AppExeName}
UsePreviousAppDir=no

[Files]
Source: "{#PublishDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Dirs]
Name: "{commonappdata}\OpenClassBanner"; Permissions: users-modify; Check: IsAdminInstallMode

[Registry]
Root: HKCU; Subkey: "Software\OpenClassBanner"; ValueName: "InstallDir"; ValueType: string; ValueData: "{app}"; Flags: uninsdeletevalue; Check: not IsAdminInstallMode
Root: HKCU; Subkey: "Software\OpenClassBanner"; ValueName: "ConfigDir"; ValueType: string; ValueData: "{app}"; Flags: uninsdeletevalue; Check: not IsAdminInstallMode
Root: HKLM; Subkey: "Software\OpenClassBanner"; ValueName: "InstallDir"; ValueType: string; ValueData: "{app}"; Flags: uninsdeletevalue; Check: IsAdminInstallMode
Root: HKLM; Subkey: "Software\OpenClassBanner"; ValueName: "ConfigDir"; ValueType: string; ValueData: "{commonappdata}\OpenClassBanner"; Flags: uninsdeletevalue; Check: IsAdminInstallMode

[Icons]
Name: "{autoprograms}\{#AppName}"; Filename: "{app}\{#AppExeName}"

[Run]
Filename: "{app}\{#AppExeName}"; Description: "Launch {#AppName}"; Flags: postinstall nowait skipifsilent