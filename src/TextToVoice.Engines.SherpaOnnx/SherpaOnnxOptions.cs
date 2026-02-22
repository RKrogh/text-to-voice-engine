namespace TextToVoice.Engines.SherpaOnnx;

/// <summary>
/// Configuration options for the sherpa-onnx embedded TTS engine.
/// </summary>
public class SherpaOnnxOptions
{
    /// <summary>
    /// Path to the ONNX voice model file (.onnx).
    /// Must be a Piper VITS model with sherpa-onnx metadata.
    /// </summary>
    public required string ModelPath { get; set; }

    /// <summary>
    /// Path to the tokens.txt file. If null, looks for tokens.txt
    /// in the same directory as the model.
    /// </summary>
    public string? TokensPath { get; set; }

    /// <summary>
    /// Path to the espeak-ng-data directory. If null, looks for
    /// espeak-ng-data in the same directory as the model.
    /// </summary>
    public string? DataDir { get; set; }

    /// <summary>
    /// Speaker ID for multi-speaker models. Default: 0.
    /// </summary>
    public int SpeakerId { get; set; } = 0;

    /// <summary>
    /// Speech rate multiplier. 1.0 = normal, &lt;1.0 = faster, &gt;1.0 = slower.
    /// </summary>
    public float LengthScale { get; set; } = 1.0f;

    /// <summary>
    /// Noise scale for voice variation. Default: 0.667.
    /// </summary>
    public float NoiseScale { get; set; } = 0.667f;

    /// <summary>
    /// Noise width for phoneme duration variation. Default: 0.8.
    /// </summary>
    public float NoiseScaleW { get; set; } = 0.8f;

    /// <summary>
    /// Number of threads for ONNX inference. Default: 1.
    /// </summary>
    public int NumThreads { get; set; } = 1;

    /// <summary>
    /// Milliseconds of silence to prepend before playback. Default: 150. Set to 0 to disable.
    /// </summary>
    public int LeadingSilenceMs { get; set; } = 150;
}
