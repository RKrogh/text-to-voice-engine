using System.Text.RegularExpressions;

namespace TextToVoice.Core;

/// <summary>
/// Detects whether input text is SSML markup.
/// </summary>
public static partial class SsmlDetector
{
    private const string SsmlNamespace = "http://www.w3.org/2001/10/synthesis";

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
        return $"<speak version=\"1.0\" xmlns=\"{SsmlNamespace}\" xml:lang=\"{lang}\">{text}</speak>";
    }

    /// <summary>
    /// Ensures a &lt;speak&gt; tag has the required xmlns and version attributes.
    /// Windows SAPI silently produces no audio if xmlns is missing.
    /// </summary>
    public static string NormalizeNamespace(string ssml)
    {
        if (string.IsNullOrWhiteSpace(ssml))
            return ssml;

        var match = SpeakTagRegex().Match(ssml);
        if (!match.Success)
            return ssml;

        var tag = match.Value;

        // Already has xmlns — leave it alone
        if (tag.Contains("xmlns=", StringComparison.OrdinalIgnoreCase))
            return ssml;

        // Build the attributes to inject
        var inject = "";

        if (!tag.Contains("version=", StringComparison.OrdinalIgnoreCase))
            inject += " version='1.0'";

        inject += $" xmlns='{SsmlNamespace}'";

        // Insert attributes just before the closing > of the <speak ...> tag
        var insertPos = match.Index + match.Length - 1; // position of '>'
        return ssml[..insertPos] + inject + ssml[insertPos..];
    }

    [GeneratedRegex(@"<speak\b[^>]*>", RegexOptions.IgnoreCase)]
    private static partial Regex SpeakTagRegex();
}
