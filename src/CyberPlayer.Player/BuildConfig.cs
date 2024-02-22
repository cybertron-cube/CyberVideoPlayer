using System;
using System.IO;
using System.Linq;
using Cybertron;

namespace CyberPlayer.Player;

public static class BuildConfig
{
    public const string SettingsFileName = "settings.json";
    
    public const string WildCardPreservables = "*.log";
    
    //RELATIVE PATHS ONLY
    public static readonly string[] Preservables =
    {
        SettingsFileName
    };

    public static readonly Version Version = new(1, 0, 0, 0);

    public static readonly string SettingsPath = OperatingSystem.IsMacOS()
        ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Library",
            "Application Support", "CyberVideoPlayer", SettingsFileName)
        : GenStatic.GetFullPathFromRelative(SettingsFileName);
    
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

#if DEBUG
    public static string GetSrcDir()
    {
        var srcDir = AppDomain.CurrentDomain.BaseDirectory;
        while (Path.GetFileName(srcDir) != "src")
        {
            srcDir = Path.GetDirectoryName(srcDir);
        }

        return srcDir;
    }

    public static string GetTestInfo(string fileName) => File.ReadAllText(Path.Combine(GetSrcDir(), "Tests", fileName));

    public static string GetTestMedia()
    {
        var testFiles = new DirectoryInfo(Path.Combine(GetSrcDir(), "Tests")).EnumerateFiles();
        return testFiles.First(x => x.Extension.Equals(".mkv", StringComparison.OrdinalIgnoreCase) ||
                                    x.Extension.Equals(".mp4", StringComparison.OrdinalIgnoreCase)).FullName;
    }
#endif
}