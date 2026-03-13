namespace TextToVoice.Apps.Console;

public class AppSettings
{
    public string? Engine { get; set; }
    public string? Voice { get; set; }
    public int? Rate { get; set; }
    public int? Volume { get; set; }
    public int? LeadingSilenceMs { get; set; }
    public PiperSettings? Piper { get; set; }
    public SherpaOnnxSettings? SherpaOnnx { get; set; }
    public ElevenLabsSettings? ElevenLabs { get; set; }
}

public class PiperSettings
{
    public string? ModelPath { get; set; }
    public string? ExecutablePath { get; set; }
}

public class SherpaOnnxSettings
{
    public string? ModelPath { get; set; }
    public string? TokensPath { get; set; }
    public string? DataDir { get; set; }
}

public class ElevenLabsSettings
{
    public string? ApiKey { get; set; }
    public string? VoiceId { get; set; }
    public string? ModelId { get; set; }
    public string? OutputFormat { get; set; }
}
