namespace TextToVoice.Core;

/// <summary>
/// Configuration options for TTS engine initialization.
/// </summary>
public class TtsOptions
{
    /// <summary>
    /// Voice name to use. If null, uses the system default.
    /// </summary>
    public string? VoiceName { get; set; }

    /// <summary>
    /// Speech rate from -10 (slowest) to 10 (fastest). Default: 0.
    /// </summary>
    public int Rate { get; set; } = 0;

    /// <summary>
    /// Volume from 0 (silent) to 100 (loudest). Default: 100.
    /// </summary>
    public int Volume { get; set; } = 100;

    /// <summary>
    /// Output audio format for file exports. Default: Wav.
    /// </summary>
    public AudioFormat OutputFormat { get; set; } = AudioFormat.Wav;

    /// <summary>
    /// Milliseconds of silence to prepend before playback to avoid audio clipping.
    /// Only affects SpeakAsync (live playback), not file output. Default: 150. Set to 0 to disable.
    /// </summary>
    public int LeadingSilenceMs { get; set; } = 150;
}
