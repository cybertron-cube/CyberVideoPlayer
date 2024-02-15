using System.Collections.Frozen;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reactive;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CyberPlayer.Player.AppSettings;
using CyberPlayer.Player.Business;
using CyberPlayer.Player.Models;
using Cybertron;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace CyberPlayer.Player.ViewModels;

public class ExportWindowViewModel : ViewModelBase
{
    [Reactive]
    public IEnumerable<TrackInfo>? AudioTrackInfos { get; set; }
    
    [Reactive]
    public string? Extension { get; set; }

    private MpvPlayer _mpvPlayer;
    private Settings _settings;
    
    
    public IList<TrackInfo> AudioTrackSelection { get; } = new List<TrackInfo>();

    public FrozenDictionary<string, string> FFmpegFormats { get; } = new Dictionary<string, string>
    {
        {"3g2", "3GP2 (3GPP2 file format)"},
        {"3gp", "3GP (3GPP file format)"},
        {"a64", "a64 - video for Commodore 64"},
        {"ac3", "raw AC-3"},
        {"adts", "ADTS AAC (Advanced Audio Coding)"},
        {"adx", "CRI ADX"},
        {"aiff", "Audio IFF"},
        {"alaw", "PCM A-law"},
        {"amr", "3GPP AMR"},
        {"apng", "Animated Portable Network Graphics"},
        {"aptx", "raw aptX (Audio Processing Technology for Bluetooth)"},
        {"aptx_hd", "raw aptX HD (Audio Processing Technology for Bluetooth)"},
        {"asf", "ASF (Advanced / Active Streaming Format)"},
        {"ass", "SSA (SubStation Alpha) subtitle"},
        {"ast", "AST (Audio Stream)"},
        {"au", "Sun AU"},
        {"avi", "AVI (Audio Video Interleaved)"},
        {"avm2", "SWF (ShockWave Flash) (AVM2)"},
        {"avs2", "raw AVS2-P2/IEEE1857.4 video"},
        {"bit", "G.729 BIT file format"},
        {"caf", "Apple CAF (Core Audio Format)"},
        {"cavsvideo", "raw Chinese AVS (Audio Video Standard) video"},
        {"codec2", "codec2 .c2 muxer"},
        {"codec2raw", "raw codec2 muxer"},
        {"dash", "DASH Muxer"},
        {"data", "raw data"},
        {"daud", "D-Cinema audio"},
        {"dirac", "raw Dirac"},
        {"dnxhd", "raw DNxHD (SMPTE VC-3)"},
        {"dts", "raw DTS"},
        {"dv", "DV (Digital Video)"},
        {"dvd", "MPEG-2 PS (DVD VOB)"},
        {"eac3", "raw E-AC-3"},
        {"f32be", "PCM 32-bit floating-point big-endian"},
        {"f32le", "PCM 32-bit floating-point little-endian"},
        {"f4v", "F4V Adobe Flash Video"},
        {"f64be", "PCM 64-bit floating-point big-endian"},
        {"f64le", "PCM 64-bit floating-point little-endian"},
        {"ffmetadata", "FFmpeg metadata in text"},
        {"film_cpk", "Sega FILM / CPK"},
        {"filmstrip", "Adobe Filmstrip"},
        {"fits", "Flexible Image Transport System"},
        {"flac", "raw FLAC"},
        {"flv", "FLV (Flash Video)"},
        {"g722", "raw G.722"},
        {"g723_1", "raw G.723.1"},
        {"g726", "raw big-endian G.726 (\"left-justified\")"},
        {"g726le", "raw little-endian G.726 (\"right-justified\")"},
        {"gif", "CompuServe Graphics Interchange Format (GIF)"},
        {"gsm", "raw GSM"},
        {"gxf", "GXF (General eXchange Format)"},
        {"h261", "raw H.261"},
        {"h263", "raw H.263"},
        {"h264", "raw H.264 video"},
        {"hds", "HDS Muxer"},
        {"hevc", "raw HEVC video"},
        {"hls", "Apple HTTP Live Streaming"},
        {"ico", "Microsoft Windows ICO"},
        {"ilbc", "iLBC storage"},
        {"image2", "image2 sequence"},
        {"ipod", "iPod H.264 MP4 (MPEG-4 Part 14)"},
        {"ircam", "Berkeley/IRCAM/CARL Sound Format"},
        {"ismv", "ISMV/ISMA (Smooth Streaming)"},
        {"ivf", "On2 IVF"},
        {"jacosub", "JACOsub subtitle format"},
        {"latm", "LOAS/LATM"},
        {"lrc", "LRC lyrics"},
        {"m4v", "raw MPEG-4 video"},
        {"matroska", "Matroska"},
        {"microdvd", "MicroDVD subtitle format"},
        {"mjpeg", "raw MJPEG video"},
        {"mkvtimestamp_v2", "extract pts as timecode v2 format}, as defined by mkvtoolnix"},
        {"mlp", "raw MLP"},
        {"mmf", "Yamaha SMAF"},
        {"mov", "QuickTime / MOV"},
        {"mp2", "MP2 (MPEG audio layer 2)"},
        {"mp3", "MP3 (MPEG audio layer 3)"},
        {"mp4", "MP4 (MPEG-4 Part 14)"},
        {"mpeg", "MPEG-1 Systems / MPEG program stream"},
        {"mpeg1video", "raw MPEG-1 video"},
        {"mpeg2video", "raw MPEG-2 video"},
        {"mpegts", "MPEG-TS (MPEG-2 Transport Stream)"},
        {"mpjpeg", "MIME multipart JPEG"},
        {"mulaw", "PCM mu-law"},
        {"mxf", "MXF (Material eXchange Format)"},
        {"mxf_d10", "MXF (Material eXchange Format) D-10 Mapping"},
        {"mxf_opatom", "MXF (Material eXchange Format) Operational Pattern Atom"},
        {"nut", "NUT"},
        {"oga", "Ogg Audio"},
        {"ogg", "Ogg"},
        {"ogv", "Ogg Video"},
        {"oma", "Sony OpenMG audio"},
        {"opus", "Ogg Opus"},
        {"psp", "PSP MP4 (MPEG-4 Part 14)"},
        {"rawvideo", "raw video"},
        {"rm", "RealMedia"},
        {"roq", "raw id RoQ"},
        {"rso", "Lego Mindstorms RSO"},
        {"s16be", "PCM signed 16-bit big-endian"},
        {"s16le", "PCM signed 16-bit little-endian"},
        {"s24be", "PCM signed 24-bit big-endian"},
        {"s24le", "PCM signed 24-bit little-endian"},
        {"s32be", "PCM signed 32-bit big-endian"},
        {"s32le", "PCM signed 32-bit little-endian"},
        {"s8", "PCM signed 8-bit"},
        {"sap", "SAP output"},
        {"sbc", "raw SBC"},
        {"scc", "Scenarist Closed Captions"},
        {"sdl", "SDL2 output device"},
        {"smjpeg", "Loki SDL MJPEG"},
        {"smoothstreaming", "Smooth Streaming Muxer"},
        {"sox", "SoX native"},
        {"spdif", "IEC 61937 (used on S/PDIF - IEC958)"},
        {"spx", "Ogg Speex"},
        {"srt", "SubRip subtitle"},
        {"sup", "raw HDMV Presentation Graphic Stream subtitles"},
        {"svcd", "MPEG-2 PS (SVCD)"},
        {"swf", "SWF (ShockWave Flash)"},
        {"truehd", "raw TrueHD"},
        {"tta", "TTA (True Audio)"},
        {"u16be", "PCM unsigned 16-bit big-endian"},
        {"u16le", "PCM unsigned 16-bit little-endian"},
        {"u24be", "PCM unsigned 24-bit big-endian"},
        {"u24le", "PCM unsigned 24-bit little-endian"},
        {"u32be", "PCM unsigned 32-bit big-endian"},
        {"u32le", "PCM unsigned 32-bit little-endian"},
        {"u8", "PCM unsigned 8-bit"},
        {"vc1", "raw VC-1 video"},
        {"vc1test", "VC-1 test bitstream"},
        {"vcd", "MPEG-1 Systems / MPEG program stream (VCD)"},
        {"vidc", "PCM Archimedes VIDC"},
        {"vob", "MPEG-2 PS (VOB)"},
        {"voc", "Creative Voice"},
        {"w64", "Sony Wave64"},
        {"wav", "WAV / WAVE (Waveform Audio)"},
        {"webm", "WebM"},
        {"webm_chunk", "WebM Chunk Muxer"},
        {"webm_dash_manifest", "WebM DASH Manifest"},
        {"webp", "WebP"},
        {"webvtt", "WebVTT subtitle"},
        {"wtv", "Windows Television (WTV)"},
        {"wv", "raw WavPack"},
    }.ToFrozenDictionary();

    public ReactiveCommand<Unit, Unit> ExportCommand { get; }

#if DEBUG
    public ExportWindowViewModel()
    {
        AudioTrackInfos = new[]
        {
            new TrackInfo() { Id = 0, Codec = "yeet", AudioDemuxSampleRate = 100, AudioDemuxChannels = "skeet" },
            new TrackInfo() { Id = 1, Codec = "yeet", AudioDemuxSampleRate = 100, AudioDemuxChannels = "skeet" },
            new TrackInfo() { Id = 2, Codec = "yeet", AudioDemuxSampleRate = 100, AudioDemuxChannels = "skeet" },
        };
    }
#endif
    
    public ExportWindowViewModel(MpvPlayer mpvPlayer, Settings settings)
    {
        _mpvPlayer = mpvPlayer;
        _settings = settings;
        AudioTrackInfos = _mpvPlayer.AudioTrackInfos;

        ExportCommand = ReactiveCommand.CreateFromTask(Export);
    }

    public async Task Export()
    {
        var audioStreamArgs = "";
        if (AudioTrackSelection.Count < AudioTrackInfos?.Last().Id)
        {
            StringBuilder sb = new();
            foreach (var audioTrack in AudioTrackSelection)
            {
                sb.Append($" -map -0:a:{audioTrack.Id - 1}");
            }

            audioStreamArgs = sb.ToString();
        }

        var newFileName = GenStatic.AppendFileName(_mpvPlayer.MediaPath, "-custom");
        if (Extension is not null) newFileName = GenStatic.ChangeExtension(newFileName, Extension);
        
        CancellationTokenSource cts = new();
        
        using (var ffmpeg = new FFmpeg(_mpvPlayer.MediaPath, _settings))
        {
            var result = await ffmpeg.FFmpegCommandAsync(_mpvPlayer.TrimStartTimeCode,
                _mpvPlayer.TrimEndTimeCode,
                "CustomCommand",
                $"-ss {_mpvPlayer.TrimStartTimeCode.FormattedString} -to {_mpvPlayer.TrimEndTimeCode.FormattedString} -i \"{_mpvPlayer.MediaPath}\" -map 0{audioStreamArgs} -codec copy {_settings.ExtraTrimArgs} \"{newFileName}\"",
                cts.Token);
            Debug.WriteLine("Finished");
        }
    }
}