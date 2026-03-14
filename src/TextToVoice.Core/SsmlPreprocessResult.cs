namespace TextToVoice.Core;

/// <summary>
/// Result of SSML preprocessing for engines that don't support SSML natively.
/// Contains plain text plus extracted prosody hints.
/// </summary>
/// <param name="PlainText">Plain text with SSML tags stripped, pauses replaced with ellipsis or newlines.</param>
/// <param name="RateMultiplier">Extracted rate multiplier from prosody element. Null if not specified.</param>
/// <param name="Volume">Extracted volume from prosody element. Null if not specified.</param>
/// <param name="VoiceName">Extracted voice name from voice element. Null if not specified.</param>
public record SsmlPreprocessResult(
    string PlainText,
    float? RateMultiplier,
    int? Volume,
    string? VoiceName
);
