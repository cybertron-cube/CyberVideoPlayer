using System.Collections.Frozen;
using System.Collections.Generic;
using CyberPlayer.Player.AppSettings;
using CyberPlayer.Player.Services;
using Serilog;

namespace CyberPlayer.Player.ViewModels;

public class MpvInfoViewModel : VideoInfoViewModel
{
    private const string DefaultFormat = "json";
    
    private static readonly FrozenDictionary<string, string> FileTypes = new Dictionary<string, string>
    {
        { "json", "mpv.json" },
    }.ToFrozenDictionary();

    public override IEnumerable<string> FormatOptions => FileTypes.Keys;

    protected override FrozenDictionary<string, string> FileExtensions => FileTypes;

    public MpvInfoViewModel(MpvPlayer mpvPlayer, Settings settings, ILogger log)
        : base(VideoInfoType.Mpv, DefaultFormat, mpvPlayer, settings, log.ForContext<MpvInfoViewModel>())
    { }

    protected override void SetFormat()
    {
        if (!string.IsNullOrWhiteSpace(MpvPlayer.TrackListJson))
            RawText = $"{{\"Track \":{MpvPlayer.TrackListJson}}}";
    }
}