#define MyAppName "CyberVideoPlayer"
#define MyAppPublisher "Cybertron-Cube"
#define MyAppURL "https://github.com/cybertron-cube/CyberVideoPlayer"
#define MyAppExeName "CyberVideoPlayer.exe"
#define MyRepoPath SourcePath + "\.."
#define MyAppBuildPath MyRepoPath + "\build\win-x64-single"  
#ifndef MyAppVersion
  #define MyAppVersion "1.0.0"
#endif

[Setup]
AppId={{8EC49017-B0B5-4EDE-83EE-7E2799BCB935}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
;AppVerName={#MyAppName} {#MyAppVersion}
VersionInfoVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}
ArchitecturesAllowed=x64
ArchitecturesInstallIn64BitMode=x64
DefaultDirName={autopf}\{#MyAppName}
DisableProgramGroupPage=yes
LicenseFile={#MyAppBuildPath}\LICENSE.md
InfoBeforeFile={#MyAppBuildPath}\LICENSE-3RD-PARTY.md
InfoAfterFile={#MyAppBuildPath}\README.md
; Uncomment the following line to run in non administrative install mode (install for current user only.)
;PrivilegesRequired=lowest
OutputDir={#MyRepoPath}\package
OutputBaseFilename=CVP-win-x64-setup
SetupIconFile={#MyRepoPath}\src\CyberPlayer.Player\Assets\Logo\cyber-logo-ocean.ico
Compression=lzma
SolidCompression=yes
WizardStyle=modern

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
Source: "{#MyAppBuildPath}\{#MyAppExeName}"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#MyAppBuildPath}\ffmpeg\*"; DestDir: "{app}\ffmpeg"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "{#MyAppBuildPath}\mediainfo\*"; DestDir: "{app}\mediainfo"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "{#MyAppBuildPath}\updater\*"; DestDir: "{app}\updater"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "{#MyAppBuildPath}\av_libglesv2.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#MyAppBuildPath}\libHarfBuzzSharp.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#MyAppBuildPath}\libmpv-2.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#MyAppBuildPath}\libSkiaSharp.dll"; DestDir: "{app}"; Flags: ignoreversion
; NOTE: Don't use "Flags: ignoreversion" on any shared system files

[Icons]
Name: "{autoprograms}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent

