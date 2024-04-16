#define MyAppName "CyberVideoPlayer"
#define MyAppPublisher "Cybertron-Cube"
#define MyAppURL "https://github.com/cybertron-cube/CyberVideoPlayer"
#define MyAppExeName "CyberVideoPlayer.exe"  
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
LicenseFile=A:\CyberPlayerMPV\build\win-x64-single\LICENSE.md
InfoBeforeFile=A:\CyberPlayerMPV\build\win-x64-single\LICENSE-3RD-PARTY.md
InfoAfterFile=A:\CyberPlayerMPV\build\win-x64-single\README.md
; Uncomment the following line to run in non administrative install mode (install for current user only.)
;PrivilegesRequired=lowest
OutputDir=A:\CyberPlayerMPV\package
OutputBaseFilename=CVP-WinSetup
SetupIconFile=A:\CyberPlayerMPV\src\CyberPlayer.Player\Assets\Logo\cyber-logo-ocean.ico
Compression=lzma
SolidCompression=yes
WizardStyle=modern

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
Source: "A:\CyberPlayerMPV\build\win-x64-single\{#MyAppExeName}"; DestDir: "{app}"; Flags: ignoreversion
Source: "A:\CyberPlayerMPV\build\win-x64-single\ffmpeg\*"; DestDir: "{app}\ffmpeg"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "A:\CyberPlayerMPV\build\win-x64-single\mediainfo\*"; DestDir: "{app}\mediainfo"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "A:\CyberPlayerMPV\build\win-x64-single\updater\*"; DestDir: "{app}\updater"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "A:\CyberPlayerMPV\build\win-x64-single\av_libglesv2.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "A:\CyberPlayerMPV\build\win-x64-single\libHarfBuzzSharp.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "A:\CyberPlayerMPV\build\win-x64-single\libmpv-2.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "A:\CyberPlayerMPV\build\win-x64-single\libSkiaSharp.dll"; DestDir: "{app}"; Flags: ignoreversion
; NOTE: Don't use "Flags: ignoreversion" on any shared system files

[Icons]
Name: "{autoprograms}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent

