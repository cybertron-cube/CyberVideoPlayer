using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using CyberPlayer.Player.RendererVideoViews;
using Serilog.Events;

namespace CyberPlayer.Player.AppSettings;

public class Settings
{
    public bool UpdaterIncludePreReleases { get; set; } = false;

    public LogEventLevel LogLevel { get; set; } = LogEventLevel.Information;

    public int LogInstances { get; set; } = 5;

    public int TimeCodeLength { get; set; } = 8;

    public Renderer Renderer { get; set; } = Renderer.Hardware;

    public bool AutoCenter { get; set; } = true;

    public bool AutoResize { get; set; } = true;

    public bool AutoFocus { get; set; } = true;

    public double SeekChange { get; set; } = 5;

    public double VolumeChange { get; set; } = 10;

    public string LibMpvDir { get; set; } =
        OperatingSystem.IsMacOS() ?
            GetMacMpvDir()
        : OperatingSystem.IsLinux() ?
            GetLinuxMpvDir()
        : string.Empty;

    public string MediaInfoDir { get; set; } = string.Empty;

    public string FFmpegDir { get; set; } = string.Empty;

    public string FFprobeDir { get; set; } = string.Empty;

    public string ExtraTrimArgs { get; set; } = "-avoid_negative_ts make_zero";

    public static (Settings, Exception?) Import(string settingsPath)
    {
        Settings? settings = null;
        Exception? exception = null;
        try
        {
            var settingsJson = File.ReadAllText(settingsPath);
            settings = JsonSerializer.Deserialize(settingsJson, SettingsJsonContext.Default.Settings);
        }
        catch (Exception e)
        {
            exception = e;
        }
        
        settings ??= new Settings();
        return (settings, exception);
    }
    
    private static string GetMacMpvDir()
    {
        var homebrewMpv = GetHomebrewLatestLibPath("/opt/homebrew/Cellar/mpv");
        return homebrewMpv ?? string.Empty;
    }
    
    private static string GetLinuxMpvDir()
    {
        var homebrewMpv = GetHomebrewLatestLibPath("/home/linuxbrew/.linuxbrew/Cellar/mpv");
        if (homebrewMpv is not null) return homebrewMpv;
        
        const string gnuLibPath = "/usr/lib/x86_64-linux-gnu";
        var gnuLibDirectory = new DirectoryInfo(gnuLibPath);
        var mpvExists = gnuLibDirectory.EnumerateFiles().Any(x => x.Name.Contains("libmpv"));
        return mpvExists ? gnuLibPath : string.Empty;
    }

    private static string? GetHomebrewLatestLibPath(string cellarPath)
    {
        var homebrew = new DirectoryInfo(cellarPath);
        if (!homebrew.Exists) return null;
        
        var orderedVersions = homebrew.EnumerateDirectories().OrderBy(x => x.Name);
        var latestDir = orderedVersions.LastOrDefault()?.FullName;
        return latestDir is null ? null : Path.Combine(latestDir, "lib");
    }

    public void Export(string settingsPath)
    {
        var settingsJson = JsonSerializer.Serialize(this, SettingsJsonContext.Default.Settings);
        Directory.CreateDirectory(Path.GetDirectoryName(settingsPath));
        File.WriteAllText(settingsPath, settingsJson);
    }
}