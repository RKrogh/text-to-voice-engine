namespace TextToVoice.Core;

/// <summary>
/// Result of SSML preprocessing for engines that don't support SSML natively.
/// Contains plain text plus extracted prosody hints.
/// </summary>
public record SsmlPreprocessResult(
    /// <summary>Plain text with SSML tags stripped, pauses replaced with ellipsis or newlines.</summary>
    string PlainText,
    /// <summary>Extracted rate multiplier from prosody element. Null if not specified.</summary>
    float? RateMultiplier,
    /// <summary>Extracted volume from prosody element. Null if not specified.</summary>
    int? Volume,
    /// <summary>Extracted voice name from voice element. Null if not specified.</summary>
    string? VoiceName
);
