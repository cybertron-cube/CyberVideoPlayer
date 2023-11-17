﻿using System;
using System.IO;
using System.Text.Json;
using CyberPlayer.Player.RendererVideoViews;
using Serilog;

namespace CyberPlayer.Player.AppSettings;

public class Settings
{
    public bool UpdaterIncludePreReleases { get; set; } = false;

    public int SeekRefreshRate { get; set; } = 100;

    public int FrameStepUpdateDelay { get; set; } = 100;

    public int TimeCodeLength { get; set; } = 8;

    public Renderer Renderer { get; set; } = Renderer.Hardware;

    public double SeekChange { get; set; } = 5;

    public double VolumeChange { get; set; } = 10;

    public string LibMpvDir { get; set; } = OperatingSystem.IsMacOS() ? "/opt/homebrew/Cellar/mpv/0.36.0/lib"
        : AppDomain.CurrentDomain.BaseDirectory;

    public string MediaInfoDir { get; set; } = string.Empty;

    public string ExtraTrimArgs { get; set; } = "-avoid_negative_ts make_zero";

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