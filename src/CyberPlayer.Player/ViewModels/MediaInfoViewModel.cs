using System.Collections.Frozen;
using System.Collections.Generic;
using CyberPlayer.Player.AppSettings;
using CyberPlayer.Player.Business;
using CyberPlayer.Player.Services;

namespace CyberPlayer.Player.ViewModels;

public class MediaInfoViewModel : VideoInfoViewModel
{
    private const string DefaultFormat = "JSON";
    
    private static readonly FrozenDictionary<string, string> FileTypes = new Dictionary<string, string>
    {
        { "Default", "mediainfo.default.txt" },
        { "XML", "mediainfo.xml" },
        { "HTML", "html" },
        { "JSON", "mediainfo.json" },
        { "MPEG-7", "MPEG-7.xml" },
        { "PBCore", "PBCore.xml" },
        { "PBCore2", "PBCore2.xml" },
        { "EBUCore", "EBUCore.xml" },
        { "FIMS_1.1", "FIMS.xml" },
        { "MIXML", "miXML.xml" },
    }.ToFrozenDictionary();

    public override IEnumerable<string> FormatOptions => FileTypes.Keys;

    protected override FrozenDictionary<string, string> FileExtensions => FileTypes;

    public MediaInfoViewModel(MpvPlayer mpvPlayer, Settings settings)
        : base(VideoInfoType.MediaInfo, DefaultFormat, mpvPlayer, settings)
    { }

    protected override void SetFormat()
    {
        using (var mediaInfo = new MediaInfo(Settings))
        {
            //TODO Complete should be an option
            //Complete information is automatically shown if requesting json though
            //mediaInfo.Option("Complete", "1");
            mediaInfo.Option("output", CurrentFormat);
            mediaInfo.Open(MpvPlayer.MediaPath);
            RawText = mediaInfo.Inform();
        }
    }
}