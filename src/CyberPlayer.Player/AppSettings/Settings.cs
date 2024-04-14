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

    public string LibMpvDir { get; set; } = OperatingSystem.IsMacOS() ? GetMacMpvDir()
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
        // example: "/opt/homebrew/Cellar/mpv/0.36.0/lib"
        var homebrewMpv = new DirectoryInfo("/opt/homebrew/Cellar/mpv");
        var orderedVersions = homebrewMpv.EnumerateDirectories().OrderBy(x => x.Name);
        var macMpvDir = orderedVersions.LastOrDefault()?.FullName;
        return macMpvDir is not null ? Path.Combine(macMpvDir, "lib") : string.Empty;
    }

    public void Export(string settingsPath)
    {
        var settingsJson = JsonSerializer.Serialize(this, SettingsJsonContext.Default.Settings);
        Directory.CreateDirectory(Path.GetDirectoryName(settingsPath));
        File.WriteAllText(settingsPath, settingsJson);
    }
}