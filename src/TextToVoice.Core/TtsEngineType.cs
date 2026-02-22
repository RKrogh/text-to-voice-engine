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

    /// <summary>Embedded ONNX inference via sherpa-onnx (cross-platform, offline, no external process).</summary>
    SherpaOnnx,
}
