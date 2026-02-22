namespace TextToVoice.Core;

/// <summary>
/// Detects whether input text is SSML markup.
/// </summary>
public static class SsmlDetector
{
    /// <summary>
    /// Returns true if the text appears to be SSML (starts with a &lt;speak&gt; tag).
    /// </summary>
    public static bool IsSsml(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return false;

        return text.TrimStart().StartsWith("<speak", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Wraps plain text in a minimal SSML speak element.
    /// </summary>
    public static string WrapInSsml(string text, string lang = "en-US")
    {
        return $"<speak version=\"1.0\" xmlns=\"http://www.w3.org/2001/10/synthesis\" xml:lang=\"{lang}\">{text}</speak>";
    }
}
