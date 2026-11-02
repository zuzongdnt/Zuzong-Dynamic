#ifndef AppVersion
  #define AppVersion "1.0.5"
#endif
#ifndef PayloadDir
  #define PayloadDir "..\work\setup-payload"
#endif
#ifndef SetupOutputDir
  #define SetupOutputDir "..\outputs"
#endif
#ifdef QABUILD
  #define ProductId "ZuzongDynamic.QA"
  #define ProductName "Zuzong Dynamic QA"
  #define ProductMutex "Local\ZuzongDynamic.ActiveApp.QA"
  #define SetupFile "ZuzongDynamic-Setup-QA"
#else
  #define ProductId "ZuzongDynamic"
  #define ProductName "Zuzong Dynamic"
  #define ProductMutex "Local\ZuzongDynamic.ActiveApp"
  #define SetupFile "ZuzongDynamic-Setup-" + AppVersion + "-x64"
#endif
; Versioned payloads let Windows finish releasing a crashed old image without
; blocking a repair install. Shortcuts and startup always select this version.
#define AppBin "versions\" + AppVersion
#define VersionDash Pos("-", AppVersion)
#define NumericVersion (VersionDash > 0 ? Copy(AppVersion, 1, VersionDash - 1) : AppVersion)

[Setup]
AppId={#ProductId}
AppName={#ProductName}
AppVersion={#AppVersion}
AppVerName={#ProductName} {#AppVersion}
AppPublisher=Zuzong
AppPublisherURL=https://github.com/zuzongdnt
AppSupportURL=https://github.com/zuzongdnt/Zuzong-Dynamic/issues
DefaultDirName={localappdata}\Programs\{#ProductId}
DefaultGroupName={#ProductName}
DisableProgramGroupPage=yes
DisableDirPage=auto
UsePreviousTasks=yes
PrivilegesRequired=lowest
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
MinVersion=10.0.19041
SetupArchitecture=x64
OutputDir={#SetupOutputDir}
OutputBaseFilename={#SetupFile}
SetupIconFile=..\src\ZuzongNotch\Services\icons\logo.ico
UninstallDisplayIcon={app}\{#AppBin}\ZuzongDynamic.exe
UninstallDisplayName={#ProductName}
AppMutex={#ProductMutex}
CloseApplications=no
RestartApplications=no
WizardStyle=modern
Compression=lzma2
SolidCompression=yes
LZMANumBlockThreads=2
LicenseFile=..\src\ZuzongNotch\LICENSE
InfoBeforeFile=InstallerNotes.txt
VersionInfoVersion={#NumericVersion}.0
VersionInfoCompany=Zuzong
VersionInfoDescription=Zuzong Dynamic Installer
VersionInfoCopyright=Zuzong; based on V-Notch by w0otdev (Apache-2.0)
Uninstallable=yes

[Languages]
Name: "vietnamese"; MessagesFile: "compiler:Default.isl,Vietnamese.isl"
Name: "english"; MessagesFile: "compiler:Default.isl"

[CustomMessages]
vietnamese.DesktopTask=Tạo biểu tượng trên Desktop
vietnamese.StartupTask=Tự chạy khi đăng nhập Windows
vietnamese.LaunchTask=Mở Zuzong Dynamic
english.DesktopTask=Create a desktop shortcut
english.StartupTask=Start when signing in to Windows
english.LaunchTask=Launch Zuzong Dynamic

[Tasks]
Name: "desktopicon"; Description: "{cm:DesktopTask}"; GroupDescription: "{cm:AdditionalIcons}"
Name: "autostart"; Description: "{cm:StartupTask}"; Flags: unchecked

[Files]
Source: "{#PayloadDir}\*"; DestDir: "{app}\{#AppBin}"; Flags: ignoreversion recursesubdirs createallsubdirs; Excludes: "*.pdb,*.log,*.log.old,notch-route.json,notch-route.json.bak,Bridge\config.json"

[Icons]
Name: "{group}\{#ProductName}"; Filename: "{app}\{#AppBin}\ZuzongDynamic.exe"; WorkingDir: "{app}\{#AppBin}"; AppUserModelID: "{#ProductId}"
Name: "{autodesktop}\{#ProductName}"; Filename: "{app}\{#AppBin}\ZuzongDynamic.exe"; WorkingDir: "{app}\{#AppBin}"; Tasks: desktopicon; AppUserModelID: "{#ProductId}"

[Registry]
Root: HKCU; Subkey: "Software\Microsoft\Windows\CurrentVersion\Run"; ValueType: string; ValueName: "{#ProductId}"; ValueData: """{app}\{#AppBin}\ZuzongDynamic.exe"" --startup"; Tasks: autostart; Flags: uninsdeletevalue

[Run]
Filename: "{app}\{#AppBin}\ZuzongDynamic.exe"; Description: "{cm:LaunchTask}"; Flags: nowait postinstall skipifsilent

; Inno Setup removes only the installed files and shortcuts. The separate
; user profile is deliberately not listed in UninstallDelete.
