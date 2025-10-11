using System.Collections.Frozen;
using System.Collections.Generic;
using System.Threading.Tasks;
using CyberPlayer.Player.AppSettings;
using CyberPlayer.Player.Services;
using ReactiveUI;
using Serilog;

namespace CyberPlayer.Player.ViewModels;

public class MpvInfoViewModel(MpvPlayer mpvPlayer, Settings settings, ILogger log) :
    VideoInfoViewModel(VideoInfoType.Mpv, DefaultFormat, mpvPlayer, settings,
        log.ForContext<MpvInfoViewModel>(),
        mpvPlayer.ObservableForProperty(x => x.TrackListJson))
{
    private const string DefaultFormat = "json";
    
    private static readonly FrozenDictionary<string, string> FileTypes = new Dictionary<string, string>
    {
        { "json", "mpv.json" },
    }.ToFrozenDictionary();

    public override IEnumerable<string> FormatOptions => FileTypes.Keys;

    protected override FrozenDictionary<string, string> FileExtensions => FileTypes;

    protected override Task SetFormat()
    {
        if (!string.IsNullOrWhiteSpace(MpvPlayer.TrackListJson))
            RawText = $"{{\"Track \":{MpvPlayer.TrackListJson}}}";
        return Task.CompletedTask;
    }
}