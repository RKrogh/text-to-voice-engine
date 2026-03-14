using TextToVoice.Core;

namespace TextToVoice.Core.Tests;

public class SsmlPreprocessorTests
{
    private readonly SsmlPreprocessor _preprocessor = new();

    [Fact]
    public void Preprocess_PlainTextInSpeakTags_ExtractsText()
    {
        var result = _preprocessor.Preprocess("<speak>Hello world</speak>");

        Assert.Equal("Hello world", result.PlainText);
        Assert.Null(result.RateMultiplier);
        Assert.Null(result.Volume);
        Assert.Null(result.VoiceName);
    }

    [Fact]
    public void Preprocess_ShortBreak_ReplacesWithEllipsis()
    {
        var result = _preprocessor.Preprocess("<speak>Hello<break time=\"200ms\"/>world</speak>");

        Assert.Contains("...", result.PlainText);
        Assert.Contains("Hello", result.PlainText);
        Assert.Contains("world", result.PlainText);
    }

    [Fact]
    public void Preprocess_LongBreak_ReplacesWithNewlines()
    {
        var result = _preprocessor.Preprocess("<speak>Hello<break time=\"1s\"/>world</speak>");

        Assert.Contains("\n\n", result.PlainText);
    }

    [Fact]
    public void Preprocess_BreakWithoutTime_DefaultsToEllipsis()
    {
        var result = _preprocessor.Preprocess("<speak>Hello<break/>world</speak>");

        Assert.Contains("...", result.PlainText);
    }

    [Theory]
    [InlineData("x-slow", 0.5f)]
    [InlineData("slow", 0.75f)]
    [InlineData("medium", 1.0f)]
    [InlineData("fast", 1.5f)]
    [InlineData("x-fast", 2.0f)]
    public void Preprocess_ProsodyRateKeyword_ExtractsMultiplier(string keyword, float expected)
    {
        var ssml = $"<speak><prosody rate=\"{keyword}\">Hello</prosody></speak>";
        var result = _preprocessor.Preprocess(ssml);

        Assert.NotNull(result.RateMultiplier);
        Assert.Equal(expected, result.RateMultiplier.Value, 0.01f);
    }

    [Fact]
    public void Preprocess_ProsodyRatePercentage_ExtractsMultiplier()
    {
        var result = _preprocessor.Preprocess(
            "<speak><prosody rate=\"150%\">Hello</prosody></speak>"
        );

        Assert.NotNull(result.RateMultiplier);
        Assert.Equal(1.5f, result.RateMultiplier.Value, 0.01f);
    }

    [Theory]
    [InlineData("silent", 0)]
    [InlineData("x-soft", 20)]
    [InlineData("soft", 40)]
    [InlineData("medium", 60)]
    [InlineData("loud", 80)]
    [InlineData("x-loud", 100)]
    public void Preprocess_ProsodyVolumeKeyword_ExtractsVolume(string keyword, int expected)
    {
        var ssml = $"<speak><prosody volume=\"{keyword}\">Hello</prosody></speak>";
        var result = _preprocessor.Preprocess(ssml);

        Assert.NotNull(result.Volume);
        Assert.Equal(expected, result.Volume.Value);
    }

    [Fact]
    public void Preprocess_VoiceElement_ExtractsName()
    {
        var result = _preprocessor.Preprocess(
            "<speak><voice name=\"en-US-AriaNeural\">Hello</voice></speak>"
        );

        Assert.Equal("en-US-AriaNeural", result.VoiceName);
        Assert.Equal("Hello", result.PlainText);
    }

    [Fact]
    public void Preprocess_NestedElements_ExtractsAllText()
    {
        var ssml =
            "<speak><prosody rate=\"fast\"><voice name=\"Test\">Hello <emphasis>world</emphasis></voice></prosody></speak>";
        var result = _preprocessor.Preprocess(ssml);

        Assert.Contains("Hello", result.PlainText);
        Assert.Contains("world", result.PlainText);
        Assert.Equal("Test", result.VoiceName);
        Assert.Equal(1.5f, result.RateMultiplier);
    }

    [Fact]
    public void Preprocess_InvalidXml_ThrowsException()
    {
        Assert.ThrowsAny<Exception>(() => _preprocessor.Preprocess("<speak>unclosed"));
    }

    [Fact]
    public void Preprocess_BreakWithSecondsFormat_ParsesCorrectly()
    {
        var result = _preprocessor.Preprocess("<speak>Hello<break time=\"0.3s\"/>world</speak>");

        // 300ms is < 500ms, so should be ellipsis
        Assert.Contains("...", result.PlainText);
    }

    [Fact]
    public void Preprocess_WithDtd_ThrowsXmlException()
    {
        var maliciousSsml = """
            <?xml version="1.0"?>
            <!DOCTYPE speak [
              <!ENTITY xxe SYSTEM "file:///etc/passwd">
            ]>
            <speak>&xxe;</speak>
            """;

        Assert.ThrowsAny<Exception>(() => _preprocessor.Preprocess(maliciousSsml));
    }

    [Fact]
    public void Preprocess_WithInternalDtdEntity_ThrowsXmlException()
    {
        var ssml = """
            <?xml version="1.0"?>
            <!DOCTYPE speak [
              <!ENTITY test "injected text">
            ]>
            <speak>&test;</speak>
            """;

        Assert.ThrowsAny<Exception>(() => _preprocessor.Preprocess(ssml));
    }
}
