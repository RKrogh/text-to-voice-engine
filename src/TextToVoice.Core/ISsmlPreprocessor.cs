namespace TextToVoice.Core;

/// <summary>
/// Preprocesses SSML into plain text and extracted parameters for engines
/// that do not support SSML natively.
/// </summary>
public interface ISsmlPreprocessor
{
    /// <summary>
    /// Converts SSML markup into plain text and extracted metadata.
    /// </summary>
    SsmlPreprocessResult Preprocess(string ssml);
}
