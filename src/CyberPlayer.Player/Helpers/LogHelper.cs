using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Threading;
using CyberPlayer.Player.AppSettings;
using Serilog;
using Serilog.Configuration;
using Serilog.Enrichers;
using Serilog.Events;
using Serilog.Templates;
using Splat;
using Splat.Serilog;
using ILogger = Serilog.ILogger;

namespace CyberPlayer.Player.Helpers;

public static class LogHelper
{
    public const string DateTimeFormat = "yyyy-MM-dd_HH-mm-ss";

    public const string LogConsoleTemplate =
        "[{Timestamp:HH:mm:ss.fff} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}";

    public const string LogFileTemplate =
        "{@t:yyyy-MM-dd HH:mm:ss.fff zzz} [{@l:u3}] {@m:lj} {#if Contains(@m, '\n')}\n{#end}" +
        "({#if SourceContext is not null}<{Substring(SourceContext, LastIndexOf(SourceContext, '.') + 1)}> {#end}" +
        "<ThreadId={ThreadId}> <ThreadName={ThreadName}> <MemoryUsage={MemoryUsage / 1048576:N2}MB>)" +
        "{#if @x is not null}\n{@x}{#end}\n";
    
    public static readonly ExpressionTemplate LogFileExpressionTemplate = new(LogFileTemplate);
    
    public static void SetupSerilog(Settings settings, Exception? settingsImportException)
    {
        Thread.CurrentThread.Name = "Main";
        
        var timeStamp = DateTime.Now.ToString(DateTimeFormat);
        var filePath = Path.Combine(BuildConfig.LogDirectory, $"debug_{timeStamp}.log");
        
        Directory.CreateDirectory(BuildConfig.LogDirectory);
        
        Log.Logger = new LoggerConfiguration()
            .ConfigureDefaults(filePath, settings.LogLevel)
            .CreateLogger();
        
        AppDomain.CurrentDomain.ProcessExit += (_, _) =>
        {
            Log.Logger.Information("Exiting");
            Log.CloseAndFlush();
        };
        
        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
        {
            Log.Logger.Fatal((Exception)e.ExceptionObject, "");
            Log.CloseAndFlush();
        };
        
        InitialLogging(settingsImportException);
        
        Log.Logger.CleanupLogFiles(BuildConfig.LogDirectory, "debug*.log", settings.LogInstances);
        
        Locator.CurrentMutable.UseSerilogFullLogger(Log.Logger);
        Locator.CurrentMutable.RegisterConstant(Log.Logger);
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

    public static LoggerConfiguration ConfigureDefaults(this LoggerConfiguration loggerConfiguration, string filePath, LogEventLevel logLevel)
    {
        return loggerConfiguration
#if DEBUG
        .MinimumLevel.Verbose()
#else
        .MinimumLevel.Is(logLevel)
#endif
        .WriteTo.Console(outputTemplate: LogConsoleTemplate)
        .WriteTo.Async(sink =>
            sink.File(
                path: filePath,
                formatter: LogFileExpressionTemplate,
                fileSizeLimitBytes: 52428800,
                rollOnFileSizeLimit: true
            )
        )
        .Enrich.FromLogContext()
        .Enrich.WithThreadId()
        .Enrich.WithThreadName()
        .Enrich.WithProperty(ThreadNameEnricher.ThreadNamePropertyName, "None")
        .Enrich.WithMemoryUsage();
    }
    
    public static LoggerConfiguration WithMemoryUsage(this LoggerEnrichmentConfiguration loggerEnrichmentConfiguration)
    {
        return loggerEnrichmentConfiguration.With<LogMemoryEnricher>();
    }

    private static void InitialLogging(Exception? settingsImportException)
    {
        if (settingsImportException is not null)
            Log.Logger.Error(settingsImportException, "Failed to import settings");
        Log.Logger.Debug("Launched with Command Line Arguments: {Args}", Environment.GetCommandLineArgs());

        var sortedEntries =
            Environment.GetEnvironmentVariables().Cast<DictionaryEntry>().OrderBy(x => x.Key);
        var maxKeyLen = sortedEntries.Max(entry => ((string)entry.Key).Length);
        var logMessage = sortedEntries.Aggregate("Environment Variables: ", (current, entry) => current + (Environment.NewLine + (entry.Key + ": ").PadRight(maxKeyLen + 2) + entry.Value));
        Log.Logger.Debug(logMessage);
    }
}