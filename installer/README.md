# Windows installer

Run `./installer/build-installer.ps1` from the repository root in PowerShell to publish the self-contained `win-x64` application and compile `artifacts/installer/OpenClassBanner-Setup.exe`. The script takes the installer version from `OpenClassBanner.csproj`, packages all publish output files, and selects the newest detected Inno Setup 6 or 7 compiler. Inno Setup is required only to build the installer; the installed application does not require a separate .NET runtime.

The installer supports current-user and all-users installation. Current-user installs keep the executable and configuration in the user's Local AppData Programs folder. All-users installs place the executable under Program Files and shared configuration under ProgramData, with write access granted only to that configuration directory. Existing adjacent config profiles are copied on first launch when a destination file does not already exist. Configurations are retained by default when uninstalling.

The installer targets 64-bit Windows 10 version 1607 or later. The application uses its self-contained .NET publish output, so there is no separate .NET runtime prerequisite.