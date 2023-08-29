using System;
using System.IO;
using System.Text.Json;
using Serilog;

namespace CyberPlayer.Player.AppSettings;

public class Settings
{
    public bool UpdaterIncludePreReleases { get; set; } = false;

    public int SeekRefreshRate { get; set; } = 100;

    public int TimeCodeLength { get; set; } = 8;

    public static Settings Import(string settingsPath)
    {
        Settings? settings = null;
        try
        {
            var settingsJson = File.ReadAllText(settingsPath);
            settings = JsonSerializer.Deserialize(settingsJson, SettingsJsonContext.Default.Settings);
        }
        catch (Exception e)
        {
            Log.Error(e, "Failed to import settings");
        }

        return settings ?? new Settings();
    }

    public void Export(string settingsPath)
    {
        var settingsJson = JsonSerializer.Serialize(this, SettingsJsonContext.Default.Settings);
        Directory.CreateDirectory(Path.GetDirectoryName(settingsPath));
        File.WriteAllText(settingsPath, settingsJson);
    }
}