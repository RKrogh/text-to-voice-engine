namespace TextToVoice.Engines.Voxtral;

/// <summary>
/// Configuration options for the Voxtral (Mistral AI) cloud TTS engine.
/// </summary>
public class VoxtralOptions
{
    /// <summary>
    /// Mistral API key. Required.
    /// </summary>
    public required string ApiKey { get; set; }

    /// <summary>
    /// Voice ID: a preset name or a custom voice ID.
    /// Default: "gb_jane_neutral". Presets follow the pattern: {locale}_{name}_{style}.
    /// Known presets: gb_jane_neutral, en_paul_neutral. Discover more in AI Studio.
    /// </summary>
    public string VoiceId { get; set; } = "gb_jane_neutral";

    /// <summary>
    /// Path to a reference audio file for zero-shot voice cloning.
    /// The file is base64-encoded and sent with each request.
    /// When set, overrides VoiceId. Use a 2-3 second audio clip for best results.
    /// </summary>
    public string? RefAudioPath { get; set; }

    /// <summary>
    /// Model ID. Default: "voxtral-mini-tts-2603".
    /// </summary>
    public string ModelId { get; set; } = "voxtral-mini-tts-2603";

    /// <summary>
    /// Output audio format: mp3, wav, pcm, flac, opus.
    /// Default: "wav". Use "pcm" for lowest streaming latency (~0.7s vs ~2s for mp3).
    /// </summary>
    public string ResponseFormat { get; set; } = "wav";

    /// <summary>
    /// Enable streaming via Server-Sent Events. Default: false.
    /// When enabled, audio chunks arrive incrementally, reducing time-to-first-byte.
    /// </summary>
    public bool Stream { get; set; } = false;

    /// <summary>
    /// Milliseconds of silence to prepend before playback. Default: 150. Set to 0 to disable.
    /// </summary>
    public int LeadingSilenceMs { get; set; } = 150;

    /// <summary>
    /// Maximum input length in words before a warning is raised.
    /// Voxtral recommends keeping input under ~300 words per request.
    /// Set to 0 to disable. Default: 300.
    /// </summary>
    public int MaxWordCountWarning { get; set; } = 300;

    /// <summary>
    /// Callback invoked when input exceeds <see cref="MaxWordCountWarning"/>.
    /// Default: writes to stderr. Set to null to suppress.
    /// </summary>
    public Action<string>? OnWarning { get; set; } = message => Console.Error.WriteLine(message);
}
