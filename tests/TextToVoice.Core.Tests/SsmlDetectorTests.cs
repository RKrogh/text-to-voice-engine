using TextToVoice.Core;

namespace TextToVoice.Core.Tests;

public class SsmlDetectorTests
{
    [Fact]
    public void IsSsml_WithSpeakTag_ReturnsTrue()
    {
        Assert.True(SsmlDetector.IsSsml("<speak>Hello</speak>"));
    }

    [Fact]
    public void IsSsml_WithSpeakTagAndAttributes_ReturnsTrue()
    {
        var ssml =
            "<speak version=\"1.0\" xmlns=\"http://www.w3.org/2001/10/synthesis\">Hello</speak>";
        Assert.True(SsmlDetector.IsSsml(ssml));
    }

    [Fact]
    public void IsSsml_WithLeadingWhitespace_ReturnsTrue()
    {
        Assert.True(SsmlDetector.IsSsml("  \n  <speak>Hello</speak>"));
    }

    [Fact]
    public void IsSsml_CaseInsensitive_ReturnsTrue()
    {
        Assert.True(SsmlDetector.IsSsml("<SPEAK>Hello</SPEAK>"));
    }

    [Fact]
    public void IsSsml_WithPlainText_ReturnsFalse()
    {
        Assert.False(SsmlDetector.IsSsml("Hello world"));
    }

    [Fact]
    public void IsSsml_WithNull_ReturnsFalse()
    {
        Assert.False(SsmlDetector.IsSsml(null));
    }

    [Fact]
    public void IsSsml_WithEmpty_ReturnsFalse()
    {
        Assert.False(SsmlDetector.IsSsml(""));
    }

    [Fact]
    public void IsSsml_WithWhitespaceOnly_ReturnsFalse()
    {
        Assert.False(SsmlDetector.IsSsml("   "));
    }

    [Fact]
    public void WrapInSsml_WrapsPlainText()
    {
        var result = SsmlDetector.WrapInSsml("Hello world");

        Assert.Contains("<speak", result);
        Assert.Contains("Hello world", result);
        Assert.Contains("</speak>", result);
        Assert.Contains("xml:lang=\"en-US\"", result);
    }

    [Fact]
    public void WrapInSsml_WithCustomLang_UsesSpecifiedLang()
    {
        var result = SsmlDetector.WrapInSsml("Hallo", "de-DE");

        Assert.Contains("xml:lang=\"de-DE\"", result);
    }

    [Fact]
    public void NormalizeNamespace_AddsXmlnsAndVersion_WhenMissing()
    {
        var result = SsmlDetector.NormalizeNamespace("<speak>text</speak>");

        Assert.Contains("xmlns='http://www.w3.org/2001/10/synthesis'", result);
        Assert.Contains("version='1.0'", result);
        Assert.Contains("text</speak>", result);
    }

    [Fact]
    public void NormalizeNamespace_AddsXmlns_KeepsExistingVersion()
    {
        var result = SsmlDetector.NormalizeNamespace("<speak version='1.0'>text</speak>");

        Assert.Contains("xmlns='http://www.w3.org/2001/10/synthesis'", result);
        // Should not duplicate version
        Assert.Equal(1, CountOccurrences(result, "version="));
    }

    [Fact]
    public void NormalizeNamespace_Unchanged_WhenXmlnsPresent()
    {
        var ssml = "<speak xmlns='http://www.w3.org/2001/10/synthesis'>text</speak>";
        var result = SsmlDetector.NormalizeNamespace(ssml);

        Assert.Equal(ssml, result);
    }

    [Fact]
    public void NormalizeNamespace_Unchanged_WhenFullyQualified()
    {
        var ssml =
            "<speak version='1.0' xmlns='http://www.w3.org/2001/10/synthesis' xml:lang='en-US'>text</speak>";
        var result = SsmlDetector.NormalizeNamespace(ssml);

        Assert.Equal(ssml, result);
    }

    [Fact]
    public void NormalizeNamespace_ReturnsInput_WhenNotSsml()
    {
        var text = "Hello world";
        Assert.Equal(text, SsmlDetector.NormalizeNamespace(text));
    }

    [Fact]
    public void NormalizeNamespace_ReturnsInput_WhenNullOrEmpty()
    {
        Assert.Null(SsmlDetector.NormalizeNamespace(null!));
        Assert.Equal("", SsmlDetector.NormalizeNamespace(""));
    }

    private static int CountOccurrences(string text, string pattern)
    {
        int count = 0, index = 0;
        while ((index = text.IndexOf(pattern, index, StringComparison.Ordinal)) != -1)
        {
            count++;
            index += pattern.Length;
        }
        return count;
    }
}
