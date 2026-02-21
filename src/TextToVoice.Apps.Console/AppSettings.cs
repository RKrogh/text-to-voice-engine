using System.Text.Json.Serialization;

namespace TextToVoice.Apps.Console;

public class AppSettings
{
    [JsonPropertyName("engine")]
    public string? Engine { get; set; }

    [JsonPropertyName("voice")]
    public string? Voice { get; set; }

    [JsonPropertyName("rate")]
    public int? Rate { get; set; }

    [JsonPropertyName("volume")]
    public int? Volume { get; set; }

    [JsonPropertyName("piper")]
    public PiperSettings? Piper { get; set; }
}

public class PiperSettings
{
    [JsonPropertyName("modelPath")]
    public string? ModelPath { get; set; }

    [JsonPropertyName("executablePath")]
    public string? ExecutablePath { get; set; }
}
