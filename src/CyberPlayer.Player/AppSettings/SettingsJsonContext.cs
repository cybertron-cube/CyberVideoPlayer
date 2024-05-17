using System.Text.Json.Serialization;

namespace CyberPlayer.Player.AppSettings;

[JsonSourceGenerationOptions(WriteIndented = true, IncludeFields = false, UseStringEnumConverter = true)]
[JsonSerializable(typeof(Settings))]
internal partial class SettingsJsonContext : JsonSerializerContext;
