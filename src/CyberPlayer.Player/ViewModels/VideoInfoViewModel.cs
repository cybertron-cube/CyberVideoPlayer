using System;
using System.Collections.Generic;
using System.IO;
using System.Reactive;
using System.Threading.Tasks;
using Avalonia.Platform.Storage;
using CyberPlayer.Player.AppSettings;
using CyberPlayer.Player.Business;
using CyberPlayer.Player.Services;
using DynamicData.Binding;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace CyberPlayer.Player.ViewModels;

public class VideoInfoViewModel : ViewModelBase
{
    public static readonly string[] MediaInfoOptions = new []
    {
        "Default",
        "XML",
        "HTML",
        "JSON",
        "MPEG-7",
        "PBCore",
        "PBCore2",
        "EBUCore",
        "FIMS_1.1",
        "MIXML"
    };

    public static readonly string[] FFprobeOptions = new[]
    {
        "default",
        "csv",
        "ini",
        "json",
        "xml",
        "compact",
        "flat"
    };

    public static readonly Dictionary<string, string> FileTypes = new()
    {
        { "default", "txt" },
        { "xml", "xml" },
        { "html", "html" },
        { "json", "json" },
        { "mpeg-7", "xml" },
        { "pbcore", "xml" },
        { "pbcore2", "xml" },
        { "ebucore", "xml" },
        { "fims_1.1", "xml" },
        { "mixml", "xml" },
        { "csv", "csv" },
        { "ini", "ini" },
        { "compact", "txt" },
        { "flat", "txt" }
    };
    
    public VideoInfoType VideoInfoType { get; init; }
    
    [Reactive]
    public string? RawText { get; set; }
    
    [Reactive]
    public bool JsonTreeView { get; set; }
    
    [Reactive]
    public IEnumerable<string> FormatOptions { get; set; }
    
    [Reactive]
    public bool Sidecar { get; set; }

    private string _currentFormat;

    public string CurrentFormat
    {
        get => _currentFormat;
        set
        {
            //if (value != _currentFormat) return;
            //We want to be able to update the info if the mediapath changes
            //... even if the format doesn't
            _currentFormat = value;
            this.RaisePropertyChanged();
            SetFormat();
        }
    }

    public ReactiveCommand<Unit, Unit> ExportCommand { get; }

    private readonly MpvPlayer _mpvPlayer;
    private readonly Settings _settings;
    private IStorageFolder? _lastFolderLocation;

#if DEBUG
    //Previewer constructor
    public VideoInfoViewModel()
    {
        FormatOptions = new[] { "1", "2", "3" };
        _currentFormat = "1";
    }
#endif
    
    public VideoInfoViewModel(VideoInfoType videoInfoType, MpvPlayer mpvPlayer, Settings settings)
    {
        VideoInfoType = videoInfoType;
        _mpvPlayer = mpvPlayer;
        _settings = settings;

        ExportCommand = ReactiveCommand.CreateFromTask(Export);
        
        JsonTreeView = true;
        switch (videoInfoType)
        {
            case VideoInfoType.MediaInfo:
                FormatOptions = MediaInfoOptions;
                _currentFormat = "JSON";
                break;
            case VideoInfoType.FFprobe:
                FormatOptions = FFprobeOptions;
                _currentFormat = "json";
                break;
            case VideoInfoType.Mpv:
                FormatOptions = new[] { "json" };
                _currentFormat = "json";
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(videoInfoType), videoInfoType, null);
        }
        
        //TODO This probably won't work for mpv as the TrackListJson property most likely
        //... will not be changed until after RawText is set (loaded event will take too long)
        //... maybe need a separate subscription for TrackListJson if videotypeinfo is Mpv
        mpvPlayer.WhenPropertyChanged(x => x.MediaPath).Subscribe(_ => SetFormat());
    }

    private async Task Export()
    {
        if (Sidecar)
        {
            await File.WriteAllTextAsync($"{_mpvPlayer.MediaPath}.{FileTypes[CurrentFormat.ToLower()]}", RawText);
        }
        else
        {
            var saveFile = await this.SaveFileDialog(new FilePickerSaveOptions
            {
                Title = $"Save {VideoInfoType} Output",
                DefaultExtension = FileTypes[CurrentFormat.ToLower()],
                SuggestedStartLocation = _lastFolderLocation,
                SuggestedFileName = "untitled"
            });

            if (saveFile == null) return;
            
            _lastFolderLocation = await saveFile.GetParentAsync();

            await using var stream = await saveFile.OpenWriteAsync();
            await using var streamWriter = new StreamWriter(stream);
            await streamWriter.WriteAsync(RawText);
        }
    }

    private void SetFormat()
    {
        switch (VideoInfoType)
        {
            case VideoInfoType.MediaInfo:
                using (var mediaInfo = new MediaInfo(_settings))
                {
                    //TODO Complete should be an option
                    //Complete information is automatically shown if requesting json though
                    //mediaInfo.Option("Complete", "1");
                    mediaInfo.Option("output", CurrentFormat);
                    mediaInfo.Open(_mpvPlayer.MediaPath);
                    RawText = mediaInfo.Inform();
                }
                break;
            case VideoInfoType.FFprobe:
                using (var ffmpeg = new FFmpeg(_mpvPlayer.MediaPath, _settings))
                {
                    RawText = CurrentFormat == "default" ? ffmpeg.Probe() : ffmpeg.ProbeFormat(CurrentFormat);
                }
                break;
            case VideoInfoType.Mpv:
                RawText = $"{{\"Track \":{_mpvPlayer.TrackListJson}}}";
                break;
        }
    }
}