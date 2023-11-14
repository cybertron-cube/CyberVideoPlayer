using System.Text.Json.Serialization;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace CyberPlayer.Player.Models;

public class TrackInfo
{
    [JsonPropertyName("id")]
    public int Id { get; set; }
    
    [JsonPropertyName("type")]
    public string? Type { get; set; }
    
    [JsonPropertyName("src-id")]
    public int SrcId { get; set; }
    
    [JsonPropertyName("image")]
    public bool Image { get; set; }
    
    [JsonPropertyName("albumart")]
    public bool AlbumArt { get; set; }
    
    [JsonPropertyName("default")]
    public bool Default { get; set; }
    
    [JsonPropertyName("forced")]
    public bool Forced { get; set; }
    
    [JsonPropertyName("dependent")]
    public bool Dependent { get; set; }
    
    [JsonPropertyName("visual-impaired")]
    public bool VisualImpaired { get; set; }
    
    [JsonPropertyName("hearing-impaired")]
    public bool HearingImpaired { get; set; }
    
    [JsonPropertyName("external")]
    public bool External { get; set; }
    
    [JsonPropertyName("selected")]
    [Reactive]
    public bool Selected { get; set; }
    
    [JsonPropertyName("ff-index")]
    public int FFIndex { get; set; }
    
    [JsonPropertyName("codec")]
    public string? Codec { get; set; }
    
    [JsonPropertyName("main-selection")]
    public int? MainSelection { get; set; }
    
    [JsonPropertyName("decoder-desc")]
    public string? DecoderDescription { get; set; }
    
    [JsonPropertyName("demux-w")]
    public int? VideoDemuxWidth { get; set; }
    
    [JsonPropertyName("demux-h")]
    public int? VideoDemuxHeight { get; set; }
    
    [JsonPropertyName("demux-fps")]
    public double? VideoDemuxFps { get; set; }
    
    [JsonPropertyName("demux-par")]
    public double? VideoDemuxPar { get; set; }
    
    [JsonPropertyName("audio-channels")]
    public int? AudioChannels { get; set; }
    
    [JsonPropertyName("demux-channel-count")]
    public int? AudioDemuxChannelCount { get; set; }
    
    [JsonPropertyName("demux-channels")]
    public string? AudioDemuxChannels { get; set; }
    
    [JsonPropertyName("demux-samplerate")]
    public int? AudioDemuxSampleRate { get; set; }
    
    [JsonPropertyName("demux-bitrate")]
    public int? AudioDemuxBitrate { get; set; }
    
    public override string ToString()
    {
        return $"Track {Id}: {Codec}, {AudioDemuxSampleRate} Hz, {AudioDemuxChannels}";
    }
}