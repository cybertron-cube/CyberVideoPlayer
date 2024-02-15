using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.IO;
using System.Reactive;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using Avalonia.Platform.Storage;
using CyberPlayer.Player.AppSettings;
using CyberPlayer.Player.Services;
using DynamicData.Binding;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace CyberPlayer.Player.ViewModels;

public abstract class VideoInfoViewModel : ViewModelBase
{
    protected abstract FrozenDictionary<string, string> FileExtensions { get; }
    
    public VideoInfoType VideoInfoType { get; init; }
    
    [Reactive]
    public string? RawText { get; set; }
    
    [Reactive]
    public bool JsonTreeView { get; set; }
    
    public abstract IEnumerable<string> FormatOptions { get; }
    
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
    
    public Subject<Unit> ExportFinished { get; }

    protected readonly MpvPlayer _mpvPlayer;
    protected readonly Settings _settings;
    private IStorageFolder? _lastFolderLocation;

#if DEBUG
    //Previewer constructor
    public VideoInfoViewModel()
    {
        //FormatOptions = new[] { "1", "2", "3" };
        _currentFormat = "1";
        ExportCommand = ReactiveCommand.CreateFromTask(Export);

        RawText = BuildConfig.GetTestInfo("mediainfo-default-output.txt");
    }
#endif
    
    public VideoInfoViewModel(VideoInfoType videoInfoType, string currentFormat, MpvPlayer mpvPlayer, Settings settings)
    {
        VideoInfoType = videoInfoType;
        _mpvPlayer = mpvPlayer;
        _settings = settings;
        _currentFormat = currentFormat;

        ExportCommand = ReactiveCommand.CreateFromTask(Export);
        ExportFinished = new Subject<Unit>();
        
        if (_currentFormat.Equals("json", StringComparison.CurrentCultureIgnoreCase))
            JsonTreeView = true;
        
        //TODO This probably won't work for mpv as the TrackListJson property most likely
        //... will not be changed until after RawText is set (loaded event will take too long)
        //... maybe need a separate subscription for TrackListJson if videotypeinfo is Mpv
        mpvPlayer.WhenPropertyChanged(x => x.MediaPath).Subscribe(_ => SetFormat());
    }

    private async Task Export()
    {
        if (Sidecar)
        {
            var extension = FileExtensions[CurrentFormat];
            await File.WriteAllTextAsync($"{_mpvPlayer.MediaPath}.{extension}", RawText);
        }
        else
        {
            var saveFile = await this.SaveFileDialog(new FilePickerSaveOptions
            {
                Title = $"Save {VideoInfoType} Output",
                DefaultExtension = FileExtensions[CurrentFormat.ToLower()],
                SuggestedStartLocation = _lastFolderLocation,
                SuggestedFileName = "untitled"
            });

            if (saveFile == null) return;
            
            _lastFolderLocation = await saveFile.GetParentAsync();

            await using var stream = await saveFile.OpenWriteAsync();
            await using var streamWriter = new StreamWriter(stream);
            await streamWriter.WriteAsync(RawText);
        }
        
        ExportFinished.OnNext(Unit.Default);
    }

    protected abstract void SetFormat();
}