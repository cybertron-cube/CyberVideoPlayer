using System;
using System.IO;

namespace CyberPlayer.Player;

public static class BuildConfig
{
    public const string SettingsPath = "settings.json";
    
    //RELATIVE PATHS ONLY
    public static readonly string[] Preservables = new[]
    {
        SettingsPath
    };

    public static readonly Version Version = new(1, 0, 0, 0);

#if WINX64
    public const string UpdaterPath = @"updater\CybertronUpdater";
#elif PORTABLE
    public static readonly string UpdaterPath = $"updater{Path.DirectorySeparatorChar}CybertronUpdater";
#else
    public const string UpdaterPath = @"updater/CybertronUpdater";
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
#endif
}