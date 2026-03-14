namespace TextToVoice.Engines.ElevenLabs;

/// <summary>
/// Configuration options for the ElevenLabs cloud TTS engine.
/// </summary>
public class ElevenLabsOptions
{
    /// <summary>
    /// ElevenLabs API key. Required.
    /// </summary>
    public required string ApiKey { get; set; }

    /// <summary>
    /// Voice ID to use. Default: "21m00Tcm4TlvDq8ikWAM" (Rachel).
    /// </summary>
    public string VoiceId { get; set; } = "21m00Tcm4TlvDq8ikWAM";

    /// <summary>
    /// Model ID. Default: "eleven_multilingual_v2".
    /// </summary>
    public string ModelId { get; set; } = "eleven_multilingual_v2";

    /// <summary>
    /// Output audio format. Default: "mp3_44100_128" (available on all plans including free tier).
    /// Use "pcm_44100" for raw PCM (Pro-tier only, auto-wrapped in WAV by the engine).
    /// See https://elevenlabs.io/docs/api-reference/text-to-speech for all formats.
    /// </summary>
    public string OutputFormat { get; set; } = "mp3_44100_128";

    /// <summary>
    /// Voice stability (0.0 to 1.0). Lower = more expressive, higher = more consistent.
    /// </summary>
    public float Stability { get; set; } = 0.5f;

    /// <summary>
    /// Similarity boost (0.0 to 1.0). Higher = closer to original voice.
    /// </summary>
    public float SimilarityBoost { get; set; } = 0.75f;

    /// <summary>
    /// Style exaggeration (0.0 to 1.0). Higher = more expressive. Default: 0.
    /// </summary>
    public float Style { get; set; } = 0.0f;

    /// <summary>
    /// Speech speed multiplier (0.7 to 1.2 via API). Default: 1.0.
    /// </summary>
    public float Speed { get; set; } = 1.0f;

    /// <summary>
    /// Milliseconds of silence to prepend before playback. Default: 150. Set to 0 to disable.
    /// </summary>
    public int LeadingSilenceMs { get; set; } = 150;

    /// <summary>
    /// Maximum text length in characters before a warning is raised.
    /// ElevenLabs charges per character — this helps prevent accidental large requests.
    /// Set to 0 to disable the warning. Default: 5000.
    /// </summary>
    public int MaxTextLengthWarning { get; set; } = 5000;

    /// <summary>
    /// Callback invoked when text exceeds <see cref="MaxTextLengthWarning"/>.
    /// Default: writes to stderr. Set to null to suppress warnings entirely.
    /// </summary>
    public Action<string>? OnWarning { get; set; } = message => Console.Error.WriteLine(message);
}
