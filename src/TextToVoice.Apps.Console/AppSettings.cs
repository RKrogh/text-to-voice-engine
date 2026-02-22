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

    [JsonPropertyName("leadingSilenceMs")]
    public int? LeadingSilenceMs { get; set; }

    [JsonPropertyName("piper")]
    public PiperSettings? Piper { get; set; }

    [JsonPropertyName("sherpaOnnx")]
    public SherpaOnnxSettings? SherpaOnnx { get; set; }
}

public class PiperSettings
{
    [JsonPropertyName("modelPath")]
    public string? ModelPath { get; set; }

    [JsonPropertyName("executablePath")]
    public string? ExecutablePath { get; set; }
}

public class SherpaOnnxSettings
{
    [JsonPropertyName("modelPath")]
    public string? ModelPath { get; set; }

    [JsonPropertyName("tokensPath")]
    public string? TokensPath { get; set; }

    [JsonPropertyName("dataDir")]
    public string? DataDir { get; set; }
}
