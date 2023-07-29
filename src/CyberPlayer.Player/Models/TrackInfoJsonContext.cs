using System.Collections.ObjectModel;
using System.Text.Json.Serialization;

namespace CyberPlayer.Player.Models;

[JsonSourceGenerationOptions(WriteIndented = true, IncludeFields = false)]
[JsonSerializable(typeof(ObservableCollection<TrackInfo>))]
[JsonSerializable(typeof(TrackInfo[]))]
internal partial class TrackInfoJsonContext : JsonSerializerContext
{ }