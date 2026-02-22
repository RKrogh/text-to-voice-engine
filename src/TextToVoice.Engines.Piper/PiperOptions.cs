namespace TextToVoice.Engines.Piper;

/// <summary>
/// Configuration options for Piper TTS engine.
/// </summary>
public class PiperOptions
{
    /// <summary>
    /// Path to the Piper executable. If null, assumes 'piper' is in PATH.
    /// </summary>
    public string? ExecutablePath { get; set; }

    /// <summary>
    /// Path to the voice model (.onnx file).
    /// </summary>
    public required string ModelPath { get; set; }

    /// <summary>
    /// Optional path to model config (.json file). Auto-detected if not specified.
    /// </summary>
    public string? ConfigPath { get; set; }

    /// <summary>
    /// Speaker ID for multi-speaker models. Default: 0.
    /// </summary>
    public int SpeakerId { get; set; } = 0;

    /// <summary>
    /// Speech rate multiplier. Default: 1.0 (normal speed).
    /// </summary>
    public float LengthScale { get; set; } = 1.0f;

    /// <summary>
    /// Noise scale for variation. Default: 0.667.
    /// </summary>
    public float NoiseScale { get; set; } = 0.667f;

    /// <summary>
    /// Noise width. Default: 0.8.
    /// </summary>
    public float NoiseWidth { get; set; } = 0.8f;

    /// <summary>
    /// Milliseconds of silence to prepend before playback. Default: 150. Set to 0 to disable.
    /// </summary>
    public int LeadingSilenceMs { get; set; } = 150;
}
