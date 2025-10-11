using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using CyberPlayer.Player.RendererVideoViews;
using Cybertron;
using Serilog.Events;

namespace CyberPlayer.Player.AppSettings;

public class Settings
{
    public bool UpdaterIncludePreReleases { get; set; } = false;

    public LogEventLevel LogLevel { get; set; } = LogEventLevel.Information;

    public int LogInstances { get; set; } = 5;

    public bool MultipleAppInstances { get; set; } = false;

    public int TimeCodeLength { get; set; } = 8;

    public Renderer Renderer { get; set; } = Renderer.Hardware;

    public bool AutoCenter { get; set; } = true;

    public bool AutoResize { get; set; } = true;

    public bool AutoFocus { get; set; } = true;

    public double SeekChange { get; set; } = 5;

    public int VolumeChange { get; set; } = 10;

    public int Volume { get; set; } = 100;

    public string LibMpvPath { get; set; } = Resolve;

    public string MediaInfoPath { get; set; } = Resolve;

    public string FFmpegPath { get; set; } = Resolve;

    public string FFprobePath { get; set; } = Resolve;

    public string ExtraTrimArgs { get; set; } = "-avoid_negative_ts make_zero";

    [JsonIgnore]
    public string? PlaceholderLibMpvPath { get; set; }
    
    [JsonIgnore]
    public string? PlaceholderMediaInfoPath { get; set; }
    
    [JsonIgnore]
    public string? PlaceholderFFmpegPath { get; set; }
    
    [JsonIgnore]
    public string? PlaceholderFFprobePath { get; set; }
    
    private const string Resolve = "resolve";
    private const string Executable = "bin";
    private const string Library = "lib";
    private const string UnixBinPath = "/usr/bin";
    private const string UnixLocalPath = "/usr/local";
    private const string LinuxGnuLibPath = "/usr/lib/x86_64-linux-gnu";
    private const string MacX64Brew = UnixLocalPath;
    private const string MacArm64Brew = "/opt/homebrew";
    private const string LinuxBrew = "/home/linuxbrew/.linuxbrew";

    private const string MpvFileName = "libmpv";
    private const string MediaInfoFileName = "libmediainfo";
    private const string FFmpegFileName = "ffmpeg";
    private const string FFprobeFileName = "ffprobe";

    private static readonly string PackagedMpv = AppDomain.CurrentDomain.BaseDirectory;
    private static readonly string PackagedMediaInfo = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "mediainfo");
    private static readonly string PackagedFFmpeg =
        GenStatic.Platform.ExecutablePath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ffmpeg", FFmpegFileName));
    private static readonly string PackagedFFprobe =
        GenStatic.Platform.ExecutablePath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ffmpeg", FFprobeFileName));
    
    public static (Settings, Exception?) Import(string settingsPath)
    {
        Settings? settings = null;
        Exception? exception = null;
        try
        {
            using var stream = new FileStream(settingsPath, FileMode.Open, FileAccess.Read, FileShare.Read);
            settings = JsonSerializer.Deserialize(stream, SettingsJsonContext.Default.Settings);
        }
        catch (Exception e)
        {
            exception = e;
        }
        
        settings ??= new Settings();
        CheckPaths(settings);
        
        return (settings, exception);
    }

    private static void CheckPaths(Settings settings)
    {
        switch (settings.LibMpvPath)
        {
            case Resolve:
                settings.PlaceholderLibMpvPath = settings.LibMpvPath;
                settings.LibMpvPath = GetDir(MpvFileName, Library);
                break;
            case "":
                settings.PlaceholderLibMpvPath = settings.LibMpvPath;
                settings.LibMpvPath = PackagedMpv;
                break;
        }

        switch (settings.MediaInfoPath)
        {
            case Resolve:
                settings.PlaceholderMediaInfoPath = settings.MediaInfoPath;
                settings.MediaInfoPath = GetDir(MediaInfoFileName, Library);
                break;
            case "":
                settings.PlaceholderMediaInfoPath = settings.MediaInfoPath;
                settings.MediaInfoPath = PackagedMediaInfo;
                break;
        }
        
        switch (settings.FFmpegPath)
        {
            case Resolve:
                settings.PlaceholderFFmpegPath = settings.FFmpegPath;
                settings.FFmpegPath = GetDir(FFmpegFileName, Executable);
                break;
            case "":
                settings.PlaceholderFFmpegPath = settings.FFmpegPath;
                settings.FFmpegPath = PackagedFFmpeg;
                break;
        }
        
        switch (settings.FFprobePath)
        {
            case Resolve:
                settings.PlaceholderFFprobePath = settings.FFprobePath;
                settings.FFprobePath = GetDir(FFprobeFileName, Executable);
                break;
            case "":
                settings.PlaceholderFFprobePath = settings.FFprobePath;
                settings.FFprobePath = PackagedFFprobe;
                break;
        }
    }

    private static void ApplyPlaceholders(Settings settings)
    {
        if (settings.PlaceholderLibMpvPath is not null)
            settings.LibMpvPath = settings.PlaceholderLibMpvPath;
        if (settings.PlaceholderMediaInfoPath is not null)
            settings.MediaInfoPath = settings.PlaceholderMediaInfoPath;
        if (settings.PlaceholderFFmpegPath is not null)
            settings.FFmpegPath = settings.PlaceholderFFmpegPath;
        if (settings.PlaceholderFFprobePath is not null)
            settings.FFprobePath = settings.PlaceholderFFprobePath;
    }

    private static string GetDir(string name, string type)
    {
        string result;
        
        if (OperatingSystem.IsMacOS()) 
            result = GetMacDir(name, type);
        else if (OperatingSystem.IsLinux()) 
            result = GetLinuxDir(name, type);
        else if (OperatingSystem.IsWindows()) 
            result = string.Empty; // TODO Maybe check path environment variable
        else throw new PlatformNotSupportedException();

        if (result == string.Empty)
        {
            return name switch
            {
                MpvFileName => PackagedMpv,
                MediaInfoFileName => PackagedMediaInfo,
                FFmpegFileName => PackagedFFmpeg,
                FFprobeFileName => PackagedFFprobe,
                _ => throw new ArgumentException("Dynamic dependency file name not valid", nameof(name))
            };
        }

        return result;
    }
    
    private static string GetMacDir(string name, string type)
    {
        var homebrewBase = RuntimeInformation.OSArchitecture == Architecture.Arm64 ? MacArm64Brew : MacX64Brew;
        var homebrewPath = Path.Combine(homebrewBase, type);
        return FileExists(homebrewPath, name) ? homebrewPath : string.Empty;
    }
    
    private static string GetLinuxDir(string name, string type)
    {
        var homebrewPath = Path.Combine(LinuxBrew, type);
        if (FileExists(homebrewPath, name)) return homebrewPath;

        var path = type == Executable ? UnixBinPath : LinuxGnuLibPath;
        return FileExists(path, name) ? path : string.Empty;
    }

    private static bool FileExists(string dir, string name)
    {
        var directoryInfo = new DirectoryInfo(dir);
        if (!directoryInfo.Exists) return false;
        
        var exists = directoryInfo.EnumerateFiles().Any(x => x.Name.Contains(name));
        return exists;
    }

    public void Export(string settingsPath)
    {
        ApplyPlaceholders(this);
        var settingsJson = JsonSerializer.Serialize(this, SettingsJsonContext.Default.Settings);
        var dirName = Path.GetDirectoryName(settingsPath);
        if (dirName is not null)
            Directory.CreateDirectory(dirName);
        File.WriteAllText(settingsPath, settingsJson);
    }
}
