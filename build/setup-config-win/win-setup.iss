#define MyAppName "CyberVideoPlayer"
#define MyAppPublisher "Cybertron-Cube"
#define MyAppURL "https://github.com/cybertron-cube/CyberVideoPlayer"
#define MyAppExeName "CyberVideoPlayer.exe"
#define MyRepoPath SourcePath + "\..\.."
#define MyOutputPath SourcePath + "\..\output"
#define MyAppBuildPath SourcePath + "\..\output\win-x64"
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
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
DefaultDirName={autopf}\{#MyAppName}
DisableProgramGroupPage=yes
LicenseFile={#MyAppBuildPath}\LICENSE.md
InfoBeforeFile={#MyAppBuildPath}\LICENSE-3RD-PARTY.md
InfoAfterFile={#MyAppBuildPath}\README.md
; Uncomment the following line to run in non administrative install mode (install for current user only.)
;PrivilegesRequired=lowest
OutputDir={#MyOutputPath}
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

[Registry]
Root: HKLM; Subkey: "SOFTWARE\Classes\SystemFileAssociations\video\OpenWithList\{#MyAppExeName}"; Flags: uninsdeletevalue uninsdeletekeyifempty; ValueType: string; ValueData: "";
Root: HKLM; Subkey: "SOFTWARE\Classes\Applications\{#MyAppExeName}"; ValueName: FriendlyAppName; Flags: uninsdeletevalue uninsdeletekeyifempty; ValueType: string; ValueData: "CVP";
Root: HKLM; Subkey: "SOFTWARE\Classes\Applications\{#MyAppExeName}\shell"; Flags: uninsdeletevalue uninsdeletekeyifempty; ValueType: string; ValueData: "play";
Root: HKLM; Subkey: "SOFTWARE\Classes\Applications\{#MyAppExeName}\shell\play"; Flags: uninsdeletevalue uninsdeletekeyifempty; ValueType: string; ValueData: "&Play";
Root: HKLM; Subkey: "SOFTWARE\Classes\Applications\{#MyAppExeName}\shell\play\command"; Flags: uninsdeletevalue uninsdeletekeyifempty; ValueType: string; ValueData: """{app}\{#MyAppExeName}"" ""%1""";
Root: HKLM; Subkey: "SOFTWARE\Classes\Applications\{#MyAppExeName}\SupportedTypes"; ValueName: .264; Flags: uninsdeletevalue uninsdeletekeyifempty; ValueType: string; ValueData: "";  
Root: HKLM; Subkey: "SOFTWARE\Classes\Applications\{#MyAppExeName}\SupportedTypes"; ValueName: .264; Flags: uninsdeletevalue uninsdeletekeyifempty; ValueType: string; ValueData: "";
Root: HKLM; Subkey: "SOFTWARE\Classes\Applications\{#MyAppExeName}\SupportedTypes"; ValueName: .265; Flags: uninsdeletevalue uninsdeletekeyifempty; ValueType: string; ValueData: "";
Root: HKLM; Subkey: "SOFTWARE\Classes\Applications\{#MyAppExeName}\SupportedTypes"; ValueName: .3g2; Flags: uninsdeletevalue uninsdeletekeyifempty; ValueType: string; ValueData: "";
Root: HKLM; Subkey: "SOFTWARE\Classes\Applications\{#MyAppExeName}\SupportedTypes"; ValueName: .3ga; Flags: uninsdeletevalue uninsdeletekeyifempty; ValueType: string; ValueData: "";
Root: HKLM; Subkey: "SOFTWARE\Classes\Applications\{#MyAppExeName}\SupportedTypes"; ValueName: .3ga2; Flags: uninsdeletevalue uninsdeletekeyifempty; ValueType: string; ValueData: "";
Root: HKLM; Subkey: "SOFTWARE\Classes\Applications\{#MyAppExeName}\SupportedTypes"; ValueName: .3gp; Flags: uninsdeletevalue uninsdeletekeyifempty; ValueType: string; ValueData: "";
Root: HKLM; Subkey: "SOFTWARE\Classes\Applications\{#MyAppExeName}\SupportedTypes"; ValueName: .3gp2; Flags: uninsdeletevalue uninsdeletekeyifempty; ValueType: string; ValueData: "";
Root: HKLM; Subkey: "SOFTWARE\Classes\Applications\{#MyAppExeName}\SupportedTypes"; ValueName: .3gpp; Flags: uninsdeletevalue uninsdeletekeyifempty; ValueType: string; ValueData: "";
Root: HKLM; Subkey: "SOFTWARE\Classes\Applications\{#MyAppExeName}\SupportedTypes"; ValueName: .3iv; Flags: uninsdeletevalue uninsdeletekeyifempty; ValueType: string; ValueData: "";
Root: HKLM; Subkey: "SOFTWARE\Classes\Applications\{#MyAppExeName}\SupportedTypes"; ValueName: .a52; Flags: uninsdeletevalue uninsdeletekeyifempty; ValueType: string; ValueData: "";
Root: HKLM; Subkey: "SOFTWARE\Classes\Applications\{#MyAppExeName}\SupportedTypes"; ValueName: .aac; Flags: uninsdeletevalue uninsdeletekeyifempty; ValueType: string; ValueData: "";
Root: HKLM; Subkey: "SOFTWARE\Classes\Applications\{#MyAppExeName}\SupportedTypes"; ValueName: .ac3; Flags: uninsdeletevalue uninsdeletekeyifempty; ValueType: string; ValueData: "";
Root: HKLM; Subkey: "SOFTWARE\Classes\Applications\{#MyAppExeName}\SupportedTypes"; ValueName: .adt; Flags: uninsdeletevalue uninsdeletekeyifempty; ValueType: string; ValueData: "";
Root: HKLM; Subkey: "SOFTWARE\Classes\Applications\{#MyAppExeName}\SupportedTypes"; ValueName: .adts; Flags: uninsdeletevalue uninsdeletekeyifempty; ValueType: string; ValueData: "";
Root: HKLM; Subkey: "SOFTWARE\Classes\Applications\{#MyAppExeName}\SupportedTypes"; ValueName: .aif; Flags: uninsdeletevalue uninsdeletekeyifempty; ValueType: string; ValueData: "";
Root: HKLM; Subkey: "SOFTWARE\Classes\Applications\{#MyAppExeName}\SupportedTypes"; ValueName: .aifc; Flags: uninsdeletevalue uninsdeletekeyifempty; ValueType: string; ValueData: "";
Root: HKLM; Subkey: "SOFTWARE\Classes\Applications\{#MyAppExeName}\SupportedTypes"; ValueName: .aiff; Flags: uninsdeletevalue uninsdeletekeyifempty; ValueType: string; ValueData: "";
Root: HKLM; Subkey: "SOFTWARE\Classes\Applications\{#MyAppExeName}\SupportedTypes"; ValueName: .amr; Flags: uninsdeletevalue uninsdeletekeyifempty; ValueType: string; ValueData: "";
Root: HKLM; Subkey: "SOFTWARE\Classes\Applications\{#MyAppExeName}\SupportedTypes"; ValueName: .ape; Flags: uninsdeletevalue uninsdeletekeyifempty; ValueType: string; ValueData: "";
Root: HKLM; Subkey: "SOFTWARE\Classes\Applications\{#MyAppExeName}\SupportedTypes"; ValueName: .asf; Flags: uninsdeletevalue uninsdeletekeyifempty; ValueType: string; ValueData: "";
Root: HKLM; Subkey: "SOFTWARE\Classes\Applications\{#MyAppExeName}\SupportedTypes"; ValueName: .au; Flags: uninsdeletevalue uninsdeletekeyifempty; ValueType: string; ValueData: "";
Root: HKLM; Subkey: "SOFTWARE\Classes\Applications\{#MyAppExeName}\SupportedTypes"; ValueName: .avc; Flags: uninsdeletevalue uninsdeletekeyifempty; ValueType: string; ValueData: "";
Root: HKLM; Subkey: "SOFTWARE\Classes\Applications\{#MyAppExeName}\SupportedTypes"; ValueName: .avi; Flags: uninsdeletevalue uninsdeletekeyifempty; ValueType: string; ValueData: "";
Root: HKLM; Subkey: "SOFTWARE\Classes\Applications\{#MyAppExeName}\SupportedTypes"; ValueName: .awb; Flags: uninsdeletevalue uninsdeletekeyifempty; ValueType: string; ValueData: "";
Root: HKLM; Subkey: "SOFTWARE\Classes\Applications\{#MyAppExeName}\SupportedTypes"; ValueName: .ay; Flags: uninsdeletevalue uninsdeletekeyifempty; ValueType: string; ValueData: "";
Root: HKLM; Subkey: "SOFTWARE\Classes\Applications\{#MyAppExeName}\SupportedTypes"; ValueName: .cue; Flags: uninsdeletevalue uninsdeletekeyifempty; ValueType: string; ValueData: "";
Root: HKLM; Subkey: "SOFTWARE\Classes\Applications\{#MyAppExeName}\SupportedTypes"; ValueName: .divx; Flags: uninsdeletevalue uninsdeletekeyifempty; ValueType: string; ValueData: "";
Root: HKLM; Subkey: "SOFTWARE\Classes\Applications\{#MyAppExeName}\SupportedTypes"; ValueName: .dts; Flags: uninsdeletevalue uninsdeletekeyifempty; ValueType: string; ValueData: "";
Root: HKLM; Subkey: "SOFTWARE\Classes\Applications\{#MyAppExeName}\SupportedTypes"; ValueName: .dtshd; Flags: uninsdeletevalue uninsdeletekeyifempty; ValueType: string; ValueData: "";
Root: HKLM; Subkey: "SOFTWARE\Classes\Applications\{#MyAppExeName}\SupportedTypes"; ValueName: .dts-hd; Flags: uninsdeletevalue uninsdeletekeyifempty; ValueType: string; ValueData: "";
Root: HKLM; Subkey: "SOFTWARE\Classes\Applications\{#MyAppExeName}\SupportedTypes"; ValueName: .dv; Flags: uninsdeletevalue uninsdeletekeyifempty; ValueType: string; ValueData: "";
Root: HKLM; Subkey: "SOFTWARE\Classes\Applications\{#MyAppExeName}\SupportedTypes"; ValueName: .dvr; Flags: uninsdeletevalue uninsdeletekeyifempty; ValueType: string; ValueData: "";
Root: HKLM; Subkey: "SOFTWARE\Classes\Applications\{#MyAppExeName}\SupportedTypes"; ValueName: .dvr-ms; Flags: uninsdeletevalue uninsdeletekeyifempty; ValueType: string; ValueData: "";
Root: HKLM; Subkey: "SOFTWARE\Classes\Applications\{#MyAppExeName}\SupportedTypes"; ValueName: .eac3; Flags: uninsdeletevalue uninsdeletekeyifempty; ValueType: string; ValueData: "";
Root: HKLM; Subkey: "SOFTWARE\Classes\Applications\{#MyAppExeName}\SupportedTypes"; ValueName: .evo; Flags: uninsdeletevalue uninsdeletekeyifempty; ValueType: string; ValueData: "";
Root: HKLM; Subkey: "SOFTWARE\Classes\Applications\{#MyAppExeName}\SupportedTypes"; ValueName: .evob; Flags: uninsdeletevalue uninsdeletekeyifempty; ValueType: string; ValueData: "";
Root: HKLM; Subkey: "SOFTWARE\Classes\Applications\{#MyAppExeName}\SupportedTypes"; ValueName: .f4a; Flags: uninsdeletevalue uninsdeletekeyifempty; ValueType: string; ValueData: "";
Root: HKLM; Subkey: "SOFTWARE\Classes\Applications\{#MyAppExeName}\SupportedTypes"; ValueName: .f4v; Flags: uninsdeletevalue uninsdeletekeyifempty; ValueType: string; ValueData: "";
Root: HKLM; Subkey: "SOFTWARE\Classes\Applications\{#MyAppExeName}\SupportedTypes"; ValueName: .flac; Flags: uninsdeletevalue uninsdeletekeyifempty; ValueType: string; ValueData: "";
Root: HKLM; Subkey: "SOFTWARE\Classes\Applications\{#MyAppExeName}\SupportedTypes"; ValueName: .flc; Flags: uninsdeletevalue uninsdeletekeyifempty; ValueType: string; ValueData: "";
Root: HKLM; Subkey: "SOFTWARE\Classes\Applications\{#MyAppExeName}\SupportedTypes"; ValueName: .fli; Flags: uninsdeletevalue uninsdeletekeyifempty; ValueType: string; ValueData: "";
Root: HKLM; Subkey: "SOFTWARE\Classes\Applications\{#MyAppExeName}\SupportedTypes"; ValueName: .flic; Flags: uninsdeletevalue uninsdeletekeyifempty; ValueType: string; ValueData: "";
Root: HKLM; Subkey: "SOFTWARE\Classes\Applications\{#MyAppExeName}\SupportedTypes"; ValueName: .flv; Flags: uninsdeletevalue uninsdeletekeyifempty; ValueType: string; ValueData: "";
Root: HKLM; Subkey: "SOFTWARE\Classes\Applications\{#MyAppExeName}\SupportedTypes"; ValueName: .gbs; Flags: uninsdeletevalue uninsdeletekeyifempty; ValueType: string; ValueData: "";
Root: HKLM; Subkey: "SOFTWARE\Classes\Applications\{#MyAppExeName}\SupportedTypes"; ValueName: .gxf; Flags: uninsdeletevalue uninsdeletekeyifempty; ValueType: string; ValueData: "";
Root: HKLM; Subkey: "SOFTWARE\Classes\Applications\{#MyAppExeName}\SupportedTypes"; ValueName: .gym; Flags: uninsdeletevalue uninsdeletekeyifempty; ValueType: string; ValueData: "";
Root: HKLM; Subkey: "SOFTWARE\Classes\Applications\{#MyAppExeName}\SupportedTypes"; ValueName: .h264; Flags: uninsdeletevalue uninsdeletekeyifempty; ValueType: string; ValueData: "";
Root: HKLM; Subkey: "SOFTWARE\Classes\Applications\{#MyAppExeName}\SupportedTypes"; ValueName: .h265; Flags: uninsdeletevalue uninsdeletekeyifempty; ValueType: string; ValueData: "";
Root: HKLM; Subkey: "SOFTWARE\Classes\Applications\{#MyAppExeName}\SupportedTypes"; ValueName: .hdmov; Flags: uninsdeletevalue uninsdeletekeyifempty; ValueType: string; ValueData: "";
Root: HKLM; Subkey: "SOFTWARE\Classes\Applications\{#MyAppExeName}\SupportedTypes"; ValueName: .hdv; Flags: uninsdeletevalue uninsdeletekeyifempty; ValueType: string; ValueData: "";
Root: HKLM; Subkey: "SOFTWARE\Classes\Applications\{#MyAppExeName}\SupportedTypes"; ValueName: .hes; Flags: uninsdeletevalue uninsdeletekeyifempty; ValueType: string; ValueData: "";
Root: HKLM; Subkey: "SOFTWARE\Classes\Applications\{#MyAppExeName}\SupportedTypes"; ValueName: .hevc; Flags: uninsdeletevalue uninsdeletekeyifempty; ValueType: string; ValueData: "";
Root: HKLM; Subkey: "SOFTWARE\Classes\Applications\{#MyAppExeName}\SupportedTypes"; ValueName: .kss; Flags: uninsdeletevalue uninsdeletekeyifempty; ValueType: string; ValueData: "";
Root: HKLM; Subkey: "SOFTWARE\Classes\Applications\{#MyAppExeName}\SupportedTypes"; ValueName: .lpcm; Flags: uninsdeletevalue uninsdeletekeyifempty; ValueType: string; ValueData: "";
Root: HKLM; Subkey: "SOFTWARE\Classes\Applications\{#MyAppExeName}\SupportedTypes"; ValueName: .m1a; Flags: uninsdeletevalue uninsdeletekeyifempty; ValueType: string; ValueData: "";
Root: HKLM; Subkey: "SOFTWARE\Classes\Applications\{#MyAppExeName}\SupportedTypes"; ValueName: .m1v; Flags: uninsdeletevalue uninsdeletekeyifempty; ValueType: string; ValueData: "";
Root: HKLM; Subkey: "SOFTWARE\Classes\Applications\{#MyAppExeName}\SupportedTypes"; ValueName: .m2a; Flags: uninsdeletevalue uninsdeletekeyifempty; ValueType: string; ValueData: "";
Root: HKLM; Subkey: "SOFTWARE\Classes\Applications\{#MyAppExeName}\SupportedTypes"; ValueName: .m2t; Flags: uninsdeletevalue uninsdeletekeyifempty; ValueType: string; ValueData: "";
Root: HKLM; Subkey: "SOFTWARE\Classes\Applications\{#MyAppExeName}\SupportedTypes"; ValueName: .m2ts; Flags: uninsdeletevalue uninsdeletekeyifempty; ValueType: string; ValueData: "";
Root: HKLM; Subkey: "SOFTWARE\Classes\Applications\{#MyAppExeName}\SupportedTypes"; ValueName: .m2v; Flags: uninsdeletevalue uninsdeletekeyifempty; ValueType: string; ValueData: "";
Root: HKLM; Subkey: "SOFTWARE\Classes\Applications\{#MyAppExeName}\SupportedTypes"; ValueName: .m3u; Flags: uninsdeletevalue uninsdeletekeyifempty; ValueType: string; ValueData: "";
Root: HKLM; Subkey: "SOFTWARE\Classes\Applications\{#MyAppExeName}\SupportedTypes"; ValueName: .m3u8; Flags: uninsdeletevalue uninsdeletekeyifempty; ValueType: string; ValueData: "";
Root: HKLM; Subkey: "SOFTWARE\Classes\Applications\{#MyAppExeName}\SupportedTypes"; ValueName: .m4a; Flags: uninsdeletevalue uninsdeletekeyifempty; ValueType: string; ValueData: "";
Root: HKLM; Subkey: "SOFTWARE\Classes\Applications\{#MyAppExeName}\SupportedTypes"; ValueName: .m4v; Flags: uninsdeletevalue uninsdeletekeyifempty; ValueType: string; ValueData: "";
Root: HKLM; Subkey: "SOFTWARE\Classes\Applications\{#MyAppExeName}\SupportedTypes"; ValueName: .mk3d; Flags: uninsdeletevalue uninsdeletekeyifempty; ValueType: string; ValueData: "";
Root: HKLM; Subkey: "SOFTWARE\Classes\Applications\{#MyAppExeName}\SupportedTypes"; ValueName: .mka; Flags: uninsdeletevalue uninsdeletekeyifempty; ValueType: string; ValueData: "";
Root: HKLM; Subkey: "SOFTWARE\Classes\Applications\{#MyAppExeName}\SupportedTypes"; ValueName: .mkv; Flags: uninsdeletevalue uninsdeletekeyifempty; ValueType: string; ValueData: "";
Root: HKLM; Subkey: "SOFTWARE\Classes\Applications\{#MyAppExeName}\SupportedTypes"; ValueName: .mlp; Flags: uninsdeletevalue uninsdeletekeyifempty; ValueType: string; ValueData: "";
Root: HKLM; Subkey: "SOFTWARE\Classes\Applications\{#MyAppExeName}\SupportedTypes"; ValueName: .mod; Flags: uninsdeletevalue uninsdeletekeyifempty; ValueType: string; ValueData: "";
Root: HKLM; Subkey: "SOFTWARE\Classes\Applications\{#MyAppExeName}\SupportedTypes"; ValueName: .mov; Flags: uninsdeletevalue uninsdeletekeyifempty; ValueType: string; ValueData: "";
Root: HKLM; Subkey: "SOFTWARE\Classes\Applications\{#MyAppExeName}\SupportedTypes"; ValueName: .mp1; Flags: uninsdeletevalue uninsdeletekeyifempty; ValueType: string; ValueData: "";
Root: HKLM; Subkey: "SOFTWARE\Classes\Applications\{#MyAppExeName}\SupportedTypes"; ValueName: .mp2; Flags: uninsdeletevalue uninsdeletekeyifempty; ValueType: string; ValueData: "";
Root: HKLM; Subkey: "SOFTWARE\Classes\Applications\{#MyAppExeName}\SupportedTypes"; ValueName: .mp2v; Flags: uninsdeletevalue uninsdeletekeyifempty; ValueType: string; ValueData: "";
Root: HKLM; Subkey: "SOFTWARE\Classes\Applications\{#MyAppExeName}\SupportedTypes"; ValueName: .mp3; Flags: uninsdeletevalue uninsdeletekeyifempty; ValueType: string; ValueData: "";
Root: HKLM; Subkey: "SOFTWARE\Classes\Applications\{#MyAppExeName}\SupportedTypes"; ValueName: .mp4; Flags: uninsdeletevalue uninsdeletekeyifempty; ValueType: string; ValueData: "";
Root: HKLM; Subkey: "SOFTWARE\Classes\Applications\{#MyAppExeName}\SupportedTypes"; ValueName: .mp4v; Flags: uninsdeletevalue uninsdeletekeyifempty; ValueType: string; ValueData: "";
Root: HKLM; Subkey: "SOFTWARE\Classes\Applications\{#MyAppExeName}\SupportedTypes"; ValueName: .mpa; Flags: uninsdeletevalue uninsdeletekeyifempty; ValueType: string; ValueData: "";
Root: HKLM; Subkey: "SOFTWARE\Classes\Applications\{#MyAppExeName}\SupportedTypes"; ValueName: .mpe; Flags: uninsdeletevalue uninsdeletekeyifempty; ValueType: string; ValueData: "";
Root: HKLM; Subkey: "SOFTWARE\Classes\Applications\{#MyAppExeName}\SupportedTypes"; ValueName: .mpeg; Flags: uninsdeletevalue uninsdeletekeyifempty; ValueType: string; ValueData: "";
Root: HKLM; Subkey: "SOFTWARE\Classes\Applications\{#MyAppExeName}\SupportedTypes"; ValueName: .mpeg2; Flags: uninsdeletevalue uninsdeletekeyifempty; ValueType: string; ValueData: "";
Root: HKLM; Subkey: "SOFTWARE\Classes\Applications\{#MyAppExeName}\SupportedTypes"; ValueName: .mpeg4; Flags: uninsdeletevalue uninsdeletekeyifempty; ValueType: string; ValueData: "";
Root: HKLM; Subkey: "SOFTWARE\Classes\Applications\{#MyAppExeName}\SupportedTypes"; ValueName: .mpg; Flags: uninsdeletevalue uninsdeletekeyifempty; ValueType: string; ValueData: "";
Root: HKLM; Subkey: "SOFTWARE\Classes\Applications\{#MyAppExeName}\SupportedTypes"; ValueName: .mpg4; Flags: uninsdeletevalue uninsdeletekeyifempty; ValueType: string; ValueData: "";
Root: HKLM; Subkey: "SOFTWARE\Classes\Applications\{#MyAppExeName}\SupportedTypes"; ValueName: .mpv; Flags: uninsdeletevalue uninsdeletekeyifempty; ValueType: string; ValueData: "";
Root: HKLM; Subkey: "SOFTWARE\Classes\Applications\{#MyAppExeName}\SupportedTypes"; ValueName: .mpv2; Flags: uninsdeletevalue uninsdeletekeyifempty; ValueType: string; ValueData: "";
Root: HKLM; Subkey: "SOFTWARE\Classes\Applications\{#MyAppExeName}\SupportedTypes"; ValueName: .mts; Flags: uninsdeletevalue uninsdeletekeyifempty; ValueType: string; ValueData: "";
Root: HKLM; Subkey: "SOFTWARE\Classes\Applications\{#MyAppExeName}\SupportedTypes"; ValueName: .mtv; Flags: uninsdeletevalue uninsdeletekeyifempty; ValueType: string; ValueData: "";
Root: HKLM; Subkey: "SOFTWARE\Classes\Applications\{#MyAppExeName}\SupportedTypes"; ValueName: .mxf; Flags: uninsdeletevalue uninsdeletekeyifempty; ValueType: string; ValueData: "";
Root: HKLM; Subkey: "SOFTWARE\Classes\Applications\{#MyAppExeName}\SupportedTypes"; ValueName: .nsf; Flags: uninsdeletevalue uninsdeletekeyifempty; ValueType: string; ValueData: "";
Root: HKLM; Subkey: "SOFTWARE\Classes\Applications\{#MyAppExeName}\SupportedTypes"; ValueName: .nsfe; Flags: uninsdeletevalue uninsdeletekeyifempty; ValueType: string; ValueData: "";
Root: HKLM; Subkey: "SOFTWARE\Classes\Applications\{#MyAppExeName}\SupportedTypes"; ValueName: .nsv; Flags: uninsdeletevalue uninsdeletekeyifempty; ValueType: string; ValueData: "";
Root: HKLM; Subkey: "SOFTWARE\Classes\Applications\{#MyAppExeName}\SupportedTypes"; ValueName: .nut; Flags: uninsdeletevalue uninsdeletekeyifempty; ValueType: string; ValueData: "";
Root: HKLM; Subkey: "SOFTWARE\Classes\Applications\{#MyAppExeName}\SupportedTypes"; ValueName: .oga; Flags: uninsdeletevalue uninsdeletekeyifempty; ValueType: string; ValueData: "";
Root: HKLM; Subkey: "SOFTWARE\Classes\Applications\{#MyAppExeName}\SupportedTypes"; ValueName: .ogg; Flags: uninsdeletevalue uninsdeletekeyifempty; ValueType: string; ValueData: "";
Root: HKLM; Subkey: "SOFTWARE\Classes\Applications\{#MyAppExeName}\SupportedTypes"; ValueName: .ogm; Flags: uninsdeletevalue uninsdeletekeyifempty; ValueType: string; ValueData: "";
Root: HKLM; Subkey: "SOFTWARE\Classes\Applications\{#MyAppExeName}\SupportedTypes"; ValueName: .ogv; Flags: uninsdeletevalue uninsdeletekeyifempty; ValueType: string; ValueData: "";
Root: HKLM; Subkey: "SOFTWARE\Classes\Applications\{#MyAppExeName}\SupportedTypes"; ValueName: .ogx; Flags: uninsdeletevalue uninsdeletekeyifempty; ValueType: string; ValueData: "";
Root: HKLM; Subkey: "SOFTWARE\Classes\Applications\{#MyAppExeName}\SupportedTypes"; ValueName: .opus; Flags: uninsdeletevalue uninsdeletekeyifempty; ValueType: string; ValueData: "";
Root: HKLM; Subkey: "SOFTWARE\Classes\Applications\{#MyAppExeName}\SupportedTypes"; ValueName: .pcm; Flags: uninsdeletevalue uninsdeletekeyifempty; ValueType: string; ValueData: "";
Root: HKLM; Subkey: "SOFTWARE\Classes\Applications\{#MyAppExeName}\SupportedTypes"; ValueName: .pls; Flags: uninsdeletevalue uninsdeletekeyifempty; ValueType: string; ValueData: "";
Root: HKLM; Subkey: "SOFTWARE\Classes\Applications\{#MyAppExeName}\SupportedTypes"; ValueName: .qt; Flags: uninsdeletevalue uninsdeletekeyifempty; ValueType: string; ValueData: "";
Root: HKLM; Subkey: "SOFTWARE\Classes\Applications\{#MyAppExeName}\SupportedTypes"; ValueName: .ra; Flags: uninsdeletevalue uninsdeletekeyifempty; ValueType: string; ValueData: "";
Root: HKLM; Subkey: "SOFTWARE\Classes\Applications\{#MyAppExeName}\SupportedTypes"; ValueName: .ram; Flags: uninsdeletevalue uninsdeletekeyifempty; ValueType: string; ValueData: "";
Root: HKLM; Subkey: "SOFTWARE\Classes\Applications\{#MyAppExeName}\SupportedTypes"; ValueName: .rm; Flags: uninsdeletevalue uninsdeletekeyifempty; ValueType: string; ValueData: "";
Root: HKLM; Subkey: "SOFTWARE\Classes\Applications\{#MyAppExeName}\SupportedTypes"; ValueName: .rmvb; Flags: uninsdeletevalue uninsdeletekeyifempty; ValueType: string; ValueData: "";
Root: HKLM; Subkey: "SOFTWARE\Classes\Applications\{#MyAppExeName}\SupportedTypes"; ValueName: .sap; Flags: uninsdeletevalue uninsdeletekeyifempty; ValueType: string; ValueData: "";
Root: HKLM; Subkey: "SOFTWARE\Classes\Applications\{#MyAppExeName}\SupportedTypes"; ValueName: .shn; Flags: uninsdeletevalue uninsdeletekeyifempty; ValueType: string; ValueData: "";
Root: HKLM; Subkey: "SOFTWARE\Classes\Applications\{#MyAppExeName}\SupportedTypes"; ValueName: .snd; Flags: uninsdeletevalue uninsdeletekeyifempty; ValueType: string; ValueData: "";
Root: HKLM; Subkey: "SOFTWARE\Classes\Applications\{#MyAppExeName}\SupportedTypes"; ValueName: .spc; Flags: uninsdeletevalue uninsdeletekeyifempty; ValueType: string; ValueData: "";
Root: HKLM; Subkey: "SOFTWARE\Classes\Applications\{#MyAppExeName}\SupportedTypes"; ValueName: .spx; Flags: uninsdeletevalue uninsdeletekeyifempty; ValueType: string; ValueData: "";
Root: HKLM; Subkey: "SOFTWARE\Classes\Applications\{#MyAppExeName}\SupportedTypes"; ValueName: .thd; Flags: uninsdeletevalue uninsdeletekeyifempty; ValueType: string; ValueData: "";
Root: HKLM; Subkey: "SOFTWARE\Classes\Applications\{#MyAppExeName}\SupportedTypes"; ValueName: .thd+ac3; Flags: uninsdeletevalue uninsdeletekeyifempty; ValueType: string; ValueData: "";
Root: HKLM; Subkey: "SOFTWARE\Classes\Applications\{#MyAppExeName}\SupportedTypes"; ValueName: .tod; Flags: uninsdeletevalue uninsdeletekeyifempty; ValueType: string; ValueData: "";
Root: HKLM; Subkey: "SOFTWARE\Classes\Applications\{#MyAppExeName}\SupportedTypes"; ValueName: .trp; Flags: uninsdeletevalue uninsdeletekeyifempty; ValueType: string; ValueData: "";
Root: HKLM; Subkey: "SOFTWARE\Classes\Applications\{#MyAppExeName}\SupportedTypes"; ValueName: .truehd; Flags: uninsdeletevalue uninsdeletekeyifempty; ValueType: string; ValueData: "";
Root: HKLM; Subkey: "SOFTWARE\Classes\Applications\{#MyAppExeName}\SupportedTypes"; ValueName: .true-hd; Flags: uninsdeletevalue uninsdeletekeyifempty; ValueType: string; ValueData: "";
Root: HKLM; Subkey: "SOFTWARE\Classes\Applications\{#MyAppExeName}\SupportedTypes"; ValueName: .ts; Flags: uninsdeletevalue uninsdeletekeyifempty; ValueType: string; ValueData: "";
Root: HKLM; Subkey: "SOFTWARE\Classes\Applications\{#MyAppExeName}\SupportedTypes"; ValueName: .tsa; Flags: uninsdeletevalue uninsdeletekeyifempty; ValueType: string; ValueData: "";
Root: HKLM; Subkey: "SOFTWARE\Classes\Applications\{#MyAppExeName}\SupportedTypes"; ValueName: .tsv; Flags: uninsdeletevalue uninsdeletekeyifempty; ValueType: string; ValueData: "";
Root: HKLM; Subkey: "SOFTWARE\Classes\Applications\{#MyAppExeName}\SupportedTypes"; ValueName: .tta; Flags: uninsdeletevalue uninsdeletekeyifempty; ValueType: string; ValueData: "";
Root: HKLM; Subkey: "SOFTWARE\Classes\Applications\{#MyAppExeName}\SupportedTypes"; ValueName: .tts; Flags: uninsdeletevalue uninsdeletekeyifempty; ValueType: string; ValueData: "";
Root: HKLM; Subkey: "SOFTWARE\Classes\Applications\{#MyAppExeName}\SupportedTypes"; ValueName: .vfw; Flags: uninsdeletevalue uninsdeletekeyifempty; ValueType: string; ValueData: "";
Root: HKLM; Subkey: "SOFTWARE\Classes\Applications\{#MyAppExeName}\SupportedTypes"; ValueName: .vgm; Flags: uninsdeletevalue uninsdeletekeyifempty; ValueType: string; ValueData: "";
Root: HKLM; Subkey: "SOFTWARE\Classes\Applications\{#MyAppExeName}\SupportedTypes"; ValueName: .vgz; Flags: uninsdeletevalue uninsdeletekeyifempty; ValueType: string; ValueData: "";
Root: HKLM; Subkey: "SOFTWARE\Classes\Applications\{#MyAppExeName}\SupportedTypes"; ValueName: .vob; Flags: uninsdeletevalue uninsdeletekeyifempty; ValueType: string; ValueData: "";
Root: HKLM; Subkey: "SOFTWARE\Classes\Applications\{#MyAppExeName}\SupportedTypes"; ValueName: .vro; Flags: uninsdeletevalue uninsdeletekeyifempty; ValueType: string; ValueData: "";
Root: HKLM; Subkey: "SOFTWARE\Classes\Applications\{#MyAppExeName}\SupportedTypes"; ValueName: .wav; Flags: uninsdeletevalue uninsdeletekeyifempty; ValueType: string; ValueData: "";
Root: HKLM; Subkey: "SOFTWARE\Classes\Applications\{#MyAppExeName}\SupportedTypes"; ValueName: .weba; Flags: uninsdeletevalue uninsdeletekeyifempty; ValueType: string; ValueData: "";
Root: HKLM; Subkey: "SOFTWARE\Classes\Applications\{#MyAppExeName}\SupportedTypes"; ValueName: .webm; Flags: uninsdeletevalue uninsdeletekeyifempty; ValueType: string; ValueData: "";
Root: HKLM; Subkey: "SOFTWARE\Classes\Applications\{#MyAppExeName}\SupportedTypes"; ValueName: .wm; Flags: uninsdeletevalue uninsdeletekeyifempty; ValueType: string; ValueData: "";
Root: HKLM; Subkey: "SOFTWARE\Classes\Applications\{#MyAppExeName}\SupportedTypes"; ValueName: .wma; Flags: uninsdeletevalue uninsdeletekeyifempty; ValueType: string; ValueData: "";
Root: HKLM; Subkey: "SOFTWARE\Classes\Applications\{#MyAppExeName}\SupportedTypes"; ValueName: .wmv; Flags: uninsdeletevalue uninsdeletekeyifempty; ValueType: string; ValueData: "";
Root: HKLM; Subkey: "SOFTWARE\Classes\Applications\{#MyAppExeName}\SupportedTypes"; ValueName: .wtv; Flags: uninsdeletevalue uninsdeletekeyifempty; ValueType: string; ValueData: "";
Root: HKLM; Subkey: "SOFTWARE\Classes\Applications\{#MyAppExeName}\SupportedTypes"; ValueName: .wv; Flags: uninsdeletevalue uninsdeletekeyifempty; ValueType: string; ValueData: "";
Root: HKLM; Subkey: "SOFTWARE\Classes\Applications\{#MyAppExeName}\SupportedTypes"; ValueName: .x264; Flags: uninsdeletevalue uninsdeletekeyifempty; ValueType: string; ValueData: "";
Root: HKLM; Subkey: "SOFTWARE\Classes\Applications\{#MyAppExeName}\SupportedTypes"; ValueName: .x265; Flags: uninsdeletevalue uninsdeletekeyifempty; ValueType: string; ValueData: "";
Root: HKLM; Subkey: "SOFTWARE\Classes\Applications\{#MyAppExeName}\SupportedTypes"; ValueName: .xvid; Flags: uninsdeletevalue uninsdeletekeyifempty; ValueType: string; ValueData: "";
Root: HKLM; Subkey: "SOFTWARE\Classes\Applications\{#MyAppExeName}\SupportedTypes"; ValueName: .y4m; Flags: uninsdeletevalue uninsdeletekeyifempty; ValueType: string; ValueData: "";
Root: HKLM; Subkey: "SOFTWARE\Classes\Applications\{#MyAppExeName}\SupportedTypes"; ValueName: .yuv; Flags: uninsdeletevalue uninsdeletekeyifempty; ValueType: string; ValueData: "";

[Icons]
Name: "{autoprograms}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent

