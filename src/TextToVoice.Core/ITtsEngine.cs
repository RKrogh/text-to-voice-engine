namespace TextToVoice.Core;

/// <summary>
/// Abstraction for text-to-speech engines. Implementations provide platform-specific
/// or cloud-based speech synthesis capabilities.
/// </summary>
public interface ITtsEngine : IDisposable
{
    /// <summary>
    /// Speaks the text aloud through the default audio device.
    /// </summary>
    Task SpeakAsync(string text, CancellationToken cancellationToken = default);

    /// <summary>
    /// Synthesizes text to audio data in WAV format.
    /// </summary>
    Task<byte[]> SynthesizeToAudioAsync(string text, CancellationToken cancellationToken = default);

    /// <summary>
    /// Synthesizes text and saves the audio to a file.
    /// </summary>
    Task SaveToFileAsync(
        string text,
        string filePath,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Returns available voices for this engine.
    /// </summary>
    IReadOnlyList<VoiceInfo> GetAvailableVoices();

    /// <summary>
    /// Sets the voice to use for speech synthesis.
    /// </summary>
    void SetVoice(string voiceName);

    /// <summary>
    /// Sets the speech rate. Range: -10 (slowest) to 10 (fastest).
    /// </summary>
    void SetRate(int rate);

    /// <summary>
    /// Sets the volume. Range: 0 (silent) to 100 (loudest).
    /// </summary>
    void SetVolume(int volume);
}
