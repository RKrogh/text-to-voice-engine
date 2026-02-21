namespace TextToVoice.Core;

/// <summary>
/// Available TTS engine types.
/// </summary>
public enum TtsEngineType
{
    /// <summary>Auto-detect based on platform.</summary>
    Auto,

    /// <summary>Windows SAPI (System.Speech).</summary>
    Windows,

    /// <summary>Piper TTS (cross-platform, offline).</summary>
    Piper,

    /// <summary>ElevenLabs cloud API.</summary>
    ElevenLabs,
}
