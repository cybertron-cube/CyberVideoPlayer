using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using CyberPlayer.Player.RendererVideoViews;
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

    public double VolumeChange { get; set; } = 10;

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
                settings.LibMpvPath = GetDir("libmpv", Library);
                break;
            case "":
                settings.PlaceholderLibMpvPath = settings.LibMpvPath;
                settings.LibMpvPath = AppDomain.CurrentDomain.BaseDirectory;
                break;
        }

        switch (settings.MediaInfoPath)
        {
            case Resolve:
                settings.PlaceholderMediaInfoPath = settings.MediaInfoPath;
                settings.MediaInfoPath = GetDir("libmediainfo", Library);
                break;
            case "":
                settings.PlaceholderMediaInfoPath = settings.MediaInfoPath;
                settings.MediaInfoPath = AppDomain.CurrentDomain.BaseDirectory;
                break;
        }
        
        switch (settings.FFmpegPath)
        {
            case Resolve:
                settings.PlaceholderFFmpegPath = settings.FFmpegPath;
                settings.FFmpegPath = GetDir("ffmpeg", Executable);
                break;
            case "":
                settings.PlaceholderFFmpegPath = settings.FFmpegPath;
                settings.FFmpegPath = AppDomain.CurrentDomain.BaseDirectory;
                break;
        }
        
        switch (settings.FFprobePath)
        {
            case Resolve:
                settings.PlaceholderFFprobePath = settings.FFprobePath;
                settings.FFprobePath = GetDir("ffprobe", Executable);
                break;
            case "":
                settings.PlaceholderFFprobePath = settings.FFprobePath;
                settings.FFprobePath = AppDomain.CurrentDomain.BaseDirectory;
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
        if (OperatingSystem.IsMacOS()) return GetMacDir(name, type);
        if (OperatingSystem.IsLinux()) return GetLinuxDir(name, type);
        if (OperatingSystem.IsWindows()) return AppDomain.CurrentDomain.BaseDirectory; // TODO Maybe check path environment variable
        throw new PlatformNotSupportedException();
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
        Directory.CreateDirectory(Path.GetDirectoryName(settingsPath));
        File.WriteAllText(settingsPath, settingsJson);
    }
}