using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Cybertron;
using static System.Environment;

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

    public static readonly string SettingsPath =
        OperatingSystem.IsMacOS() ?
            Path.Combine(GetFolderPath(SpecialFolder.UserProfile), "Library", "Application Support",
                "CyberVideoPlayer", SettingsFileName)
        : OperatingSystem.IsWindows() ?
            Path.Combine(GetFolderPath(SpecialFolder.ApplicationData), "CyberVideoPlayer", SettingsFileName)
        : GenStatic.GetFullPathFromRelative(SettingsFileName);
    
    public static readonly string LogDirectory =
        OperatingSystem.IsMacOS() ?
            Path.Combine(GetFolderPath(SpecialFolder.UserProfile), "Library", "Logs", "CyberVideoPlayer")
        : OperatingSystem.IsWindows() ?
            Path.Combine(GetFolderPath(SpecialFolder.ApplicationData), "CyberVideoPlayer", "Logs")
        : GenStatic.GetFullPathFromRelative("logs");
    
    public static readonly string AssetIdentifierArchitecture = RuntimeInformation.OSArchitecture.ToString().ToLower();

#if WINDOWS
    public const string UpdaterPath = @"updater\CybertronUpdater";
#elif PORTABLE
    public static readonly string UpdaterPath = $"updater{Path.DirectorySeparatorChar}CybertronUpdater";
#else
    public const string UpdaterPath = "updater/CybertronUpdater";
#endif

#if PORTABLE
    public const string AssetIdentifierPlatform = "portable";
#elif WINDOWS
    public const string AssetIdentifierPlatform = "win";
#elif LINUX
    public const string AssetIdentifierPlatform = "linux";
#elif OSX
    public const string AssetIdentifierPlatform = "osx";
#endif

#if MULTI
    public const string AssetIdentifierInstance = "multi";
#elif SINGLE
    public const string AssetIdentifierInstance = "single";
    public const string Guid = "8EC49017-B0B5-4EDE-83EE-7E2799BCB935";
    public const string MutexId = "Global\\{8EC49017-B0B5-4EDE-83EE-7E2799BCB935}";
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