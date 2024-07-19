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

    public static readonly SemanticVersion Version = new(1, 0, 0);

    public static readonly string SettingsPath =
        OperatingSystem.IsMacOS() ?
            Path.Combine(GetFolderPath(SpecialFolder.UserProfile), "Library", "Application Support",
                "CyberVideoPlayer", SettingsFileName)
        : OperatingSystem.IsWindows() ?
            Path.Combine(GetFolderPath(SpecialFolder.ApplicationData), "CyberVideoPlayer", SettingsFileName)
        : OperatingSystem.IsLinux() ?
            Path.Combine(GetFolderPath(SpecialFolder.UserProfile), ".config", "CyberVideoPlayer", SettingsFileName)
        : GenStatic.GetFullPathFromRelative(SettingsFileName);
    
    public static readonly string LogDirectory =
        OperatingSystem.IsMacOS() ?
            Path.Combine(GetFolderPath(SpecialFolder.UserProfile), "Library", "Logs", "CyberVideoPlayer")
        : OperatingSystem.IsWindows() ?
            Path.Combine(GetFolderPath(SpecialFolder.ApplicationData), "CyberVideoPlayer", "Logs")
        : GenStatic.GetFullPathFromRelative("logs");
    
    public static readonly string AssetIdentifierArchitecture = RuntimeInformation.OSArchitecture.ToString().ToLower();

    public static readonly string UpdaterPath = $"updater{Path.DirectorySeparatorChar}CybertronUpdater";

#if PORTABLE
    public const string AssetIdentifierPlatform = "portable";
#else
    public static readonly string AssetIdentifierPlatform =
        OperatingSystem.IsMacOS() ? "osx"
        : OperatingSystem.IsWindows() ? "win"
        : OperatingSystem.IsLinux() ? "linux"
        : throw new PlatformNotSupportedException();
#endif

    public const string Guid = "8EC49017-B0B5-4EDE-83EE-7E2799BCB935";
    public const string MutexId = "Global\\{8EC49017-B0B5-4EDE-83EE-7E2799BCB935}";

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
        var testFiles = new DirectoryInfo(Path.Combine(GetSrcDir(), "Tests", "Playback")).EnumerateFiles().ToList();
        var firstTestFile = testFiles.Where(x => !x.Name.StartsWith('.')).MinBy(x => x.Name)?.FullName;
        return firstTestFile ?? "http://commondatastorage.googleapis.com/gtv-videos-bucket/sample/BigBuckBunny.mp4";
    }
#endif
}