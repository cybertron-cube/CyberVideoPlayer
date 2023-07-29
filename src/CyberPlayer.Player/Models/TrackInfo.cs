using System.Text.Json.Serialization;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace CyberPlayer.Player.Models;

public class TrackInfo// : ReactiveObject
{
    [JsonRequired]
    [JsonPropertyName("id")]
    //[Reactive]
    public int Id { get; set; }
    
    [JsonRequired]
    [JsonPropertyName("type")]
    //[Reactive]
    public string Type { get; set; }
    
    [JsonRequired]
    [JsonPropertyName("src-id")]
    //[Reactive]
    public int SrcId { get; set; }
    
    [JsonRequired]
    [JsonPropertyName("image")]
    //[Reactive]
    public bool Image { get; set; }
    
    [JsonRequired]
    [JsonPropertyName("albumart")]
    //[Reactive]
    public bool AlbumArt { get; set; }
    
    [JsonRequired]
    [JsonPropertyName("default")]
    //[Reactive]
    public bool Default { get; set; }
    
    [JsonRequired]
    [JsonPropertyName("forced")]
    //[Reactive]
    public bool Forced { get; set; }
    
    [JsonRequired]
    [JsonPropertyName("dependent")]
    //[Reactive]
    public bool Dependent { get; set; }
    
    [JsonRequired]
    [JsonPropertyName("visual-impaired")]
    //[Reactive]
    public bool VisualImpaired { get; set; }
    
    [JsonRequired]
    [JsonPropertyName("hearing-impaired")]
    //[Reactive]
    public bool HearingImpaired { get; set; }
    
    [JsonRequired]
    [JsonPropertyName("external")]
    //[Reactive]
    public bool External { get; set; }
    
    [JsonRequired]
    [JsonPropertyName("selected")]
    //[Reactive]
    public bool Selected { get; set; }
    
    [JsonRequired]
    [JsonPropertyName("ff-index")]
    //[Reactive]
    public int FFIndex { get; set; }
    
    [JsonRequired]
    [JsonPropertyName("codec")]
    //[Reactive]
    public string Codec { get; set; }
    
    [JsonPropertyName("main-selection")]
    //[Reactive]
    public int? MainSelection { get; set; }
    
    [JsonPropertyName("decoder-desc")]
    //[Reactive]
    public string? DecoderDescription { get; set; }
    
    [JsonPropertyName("demux-w")]
    //[Reactive]
    public int? VideoDemuxWidth { get; set; }
    
    [JsonPropertyName("demux-h")]
    //[Reactive]
    public int? VideoDemuxHeight { get; set; }
    
    [JsonPropertyName("demux-fps")]
    //[Reactive]
    public double? VideoDemuxFps { get; set; }
    
    [JsonPropertyName("demux-par")]
    //[Reactive]
    public double? VideoDemuxPar { get; set; }
    
    [JsonPropertyName("audio-channels")]
    //[Reactive]
    public int? AudioChannels { get; set; }
    
    [JsonPropertyName("demux-channel-count")]
    //[Reactive]
    public int? AudioDemuxChannelCount { get; set; }
    
    [JsonPropertyName("demux-channels")]
    //[Reactive]
    public string? AudioDemuxChannels { get; set; }
    
    [JsonPropertyName("demux-samplerate")]
    //[Reactive]
    public int? AudioDemuxSampleRate { get; set; }
    
    [JsonPropertyName("demux-bitrate")]
    //[Reactive]
    public int? AudioDemuxBitrate { get; set; }
    
    public override string ToString()
    {
        return $"Track {Id}: {Codec}, {AudioDemuxSampleRate} Hz, {AudioDemuxChannels}";
    }
}