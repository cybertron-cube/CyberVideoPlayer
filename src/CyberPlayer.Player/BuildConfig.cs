using System;
using System.IO;
using Cybertron;

namespace CyberPlayer.Player;

public static class BuildConfig
{
    public const string SettingsPath = "settings.json";
    
    public const string WildCardPreservables = "*.log";
    
    //RELATIVE PATHS ONLY
    public static readonly string[] Preservables = new[]
    {
        SettingsPath
    };

    public static readonly Version Version = new(1, 0, 0, 0);

    public static readonly string LogDirectory = OperatingSystem.IsMacOS() ?
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Library", "Logs",
            "CyberVideoPlayer")
        : GenStatic.GetFullPathFromRelative("logs");

#if WINX64
    public const string UpdaterPath = @"updater\CybertronUpdater";
#elif PORTABLE
    public static readonly string UpdaterPath = $"updater{Path.DirectorySeparatorChar}CybertronUpdater";
#else
    public const string UpdaterPath = "updater/CybertronUpdater";
#endif

#if PORTABLE
    public const string AssetIdentifierPlatform = "portable";
#elif WINX64
    public const string AssetIdentifierPlatform = "win-x64";
#elif LINUXX64
    public const string AssetIdentifierPlatform = "linux-x64";
#elif OSXX64
    public const string AssetIdentifierPlatform = "osx-x64";
#endif

#if MULTI
    public const string AssetIdentifierInstance = "multi";
#elif SINGLE
    public const string AssetIdentifierInstance = "single";
    public const string Guid = "{8EC49017-B0B5-4EDE-83EE-7E2799BCB935}";
#endif
}