using System;
using System.Collections;
using System.IO;
using System.Linq;
using Serilog;
using Splat;
using Splat.Serilog;
using ILogger = Serilog.ILogger;

namespace CyberPlayer.Player.Helpers;

public static class LogHelper
{
    public const string DateTimeFormat = "yyyy-MM-dd_HH-mm-ss";
    
    public static void SetupSerilog()
    {
        var timeStamp = DateTime.Now.ToString(DateTimeFormat);
        var filePath = Path.Combine(BuildConfig.LogDirectory, $"debug_{timeStamp}.log");
        //buffered: true
#if DEBUG
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Verbose()
            .WriteTo.Console()
            .WriteTo.Async(s => s.File(filePath, shared: true))
            .CreateLogger();
#else
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.Async(s => s.File(filePath, shared: true))
            .CreateLogger();
#endif

        AppDomain.CurrentDomain.ProcessExit += (_, _) =>
        {
            Log.Information("Exiting");
            Log.CloseAndFlush();
        };
        
        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
        {
            Log.Fatal((Exception)e.ExceptionObject, "");
            Log.CloseAndFlush();
        };
        
        Log.Logger.CleanupLogFiles(BuildConfig.LogDirectory, "debug*.log", 3);
        
        Locator.CurrentMutable.UseSerilogFullLogger(Log.Logger);
        Locator.CurrentMutable.RegisterConstant(Log.Logger);
        
        InitialLogging();
    }
    
    public static void CleanupLogFiles(this ILogger logger, string location, string searchPattern, int fileInstances)
    {
        var dirInfo = new DirectoryInfo(location);
        var logFiles = dirInfo.EnumerateFiles(searchPattern).OrderBy(x => x.Name).ToArray();

        if (logFiles.Length <= fileInstances) return;
        
        var filesToDelete = logFiles[..^fileInstances];
        foreach (var file in filesToDelete)
        {
            try
            {
                file.Delete();
            }
            catch (IOException e)
            {
                logger.Error(e, "File {FileName} is currently in use, skipping deletion", file.Name);
            }
        }
    }

    private static void InitialLogging()
    {
        Log.Debug("Launched with Command Line Arguments: {Args}", Environment.GetCommandLineArgs());

        var sortedEntries = Environment.GetEnvironmentVariables().Cast<DictionaryEntry>();
        var maxKeyLen = sortedEntries.Max(entry => ((string)entry.Key).Length);
        var logMessage = sortedEntries.Aggregate("Environment Variables: ", (current, entry) => current + (Environment.NewLine + (entry.Key + ": ").PadRight(maxKeyLen + 2) + entry.Value));
        Log.Debug(logMessage);
    }
}