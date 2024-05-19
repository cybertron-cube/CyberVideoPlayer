using System.Collections.Frozen;
using System.Collections.Generic;
using CyberPlayer.Player.AppSettings;
using CyberPlayer.Player.Business;
using CyberPlayer.Player.Services;

namespace CyberPlayer.Player.ViewModels;

public class FFprobeInfoViewModel : VideoInfoViewModel
{
    private const string DefaultFormat = "json";
    
    private static readonly FrozenDictionary<string, string> FileTypes = new Dictionary<string, string>
    {
        { "default", "ffprobe.default.txt" },
        { "csv", "csv" },
        { "ini", "ini" },
        { "json", "ffprobe.json" },
        { "xml", "ffprobe.xml" },
        { "compact", "compact.txt" },
        { "flat", "flat.txt" }
    }.ToFrozenDictionary();

    public override IEnumerable<string> FormatOptions => FileTypes.Keys;

    protected override FrozenDictionary<string, string> FileExtensions => FileTypes;

    public FFprobeInfoViewModel(MpvPlayer mpvPlayer, Settings settings)
        : base(VideoInfoType.FFprobe, DefaultFormat, mpvPlayer, settings)
    { }

    protected override void SetFormat()
    {
        using (var ffmpeg = new FFmpeg(MpvPlayer.MediaPath, Settings))
        {
            RawText = CurrentFormat == "default" ? ffmpeg.Probe() : ffmpeg.ProbeFormat(CurrentFormat);
        }
    }
}