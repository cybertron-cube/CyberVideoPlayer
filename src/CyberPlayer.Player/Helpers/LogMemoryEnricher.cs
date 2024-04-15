using System;
using Serilog.Core;
using Serilog.Events;

namespace CyberPlayer.Player.Helpers;

public class LogMemoryEnricher : ILogEventEnricher
{
    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("MemoryUsage", GC.GetTotalMemory(false)));
    }
}
