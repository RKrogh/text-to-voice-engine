namespace TextToVoice.Core;

/// <summary>
/// Implemented by TTS engines that can handle SSML input directly or via preprocessing.
/// </summary>
public interface ISsmlCapable
{
    /// <summary>
    /// Whether this engine supports SSML natively (true) or via preprocessing (false).
    /// </summary>
    bool SupportsNativeSsml { get; }

    /// <summary>
    /// Speaks SSML markup aloud through the default audio device.
    /// </summary>
    Task SpeakSsmlAsync(string ssml, CancellationToken cancellationToken = default);

    /// <summary>
    /// Synthesizes SSML markup to audio data in WAV format.
    /// </summary>
    Task<byte[]> SynthesizeSsmlToAudioAsync(
        string ssml,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Synthesizes SSML markup and saves the audio to a file.
    /// </summary>
    Task SaveSsmlToFileAsync(
        string ssml,
        string filePath,
        CancellationToken cancellationToken = default
    );
}
