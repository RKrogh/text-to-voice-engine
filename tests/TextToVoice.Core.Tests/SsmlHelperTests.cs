using TextToVoice.Core;

namespace TextToVoice.Core.Tests;

public class SsmlHelperTests
{
    private readonly SsmlPreprocessor _preprocessor = new();

    [Fact]
    public async Task ExecuteWithPreprocessingAsync_CallsApplyAndExecuteWithPlainText()
    {
        SsmlPreprocessResult? capturedResult = null;
        string? capturedText = null;

        await SsmlHelper.ExecuteWithPreprocessingAsync(
            _preprocessor,
            "<speak>Hello world</speak>",
            result => capturedResult = result,
            text => { capturedText = text; return Task.CompletedTask; },
            () => { });

        Assert.NotNull(capturedResult);
        Assert.Equal("Hello world", capturedResult!.PlainText);
        Assert.Equal("Hello world", capturedText);
    }

    [Fact]
    public async Task ExecuteWithPreprocessingAsync_Generic_ReturnsResult()
    {
        var result = await SsmlHelper.ExecuteWithPreprocessingAsync(
            _preprocessor,
            "<speak>Hello</speak>",
            _ => { },
            text => Task.FromResult(new byte[] { 1, 2, 3 }),
            () => { });

        Assert.Equal(new byte[] { 1, 2, 3 }, result);
    }

    [Fact]
    public async Task ExecuteWithPreprocessingAsync_RestoresState_OnSuccess()
    {
        var restored = false;

        await SsmlHelper.ExecuteWithPreprocessingAsync(
            _preprocessor,
            "<speak>Hello</speak>",
            _ => { },
            _ => Task.CompletedTask,
            () => restored = true);

        Assert.True(restored);
    }

    [Fact]
    public async Task ExecuteWithPreprocessingAsync_RestoresState_OnException()
    {
        var restored = false;

        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await SsmlHelper.ExecuteWithPreprocessingAsync(
                _preprocessor,
                "<speak>Hello</speak>",
                _ => { },
                _ => throw new InvalidOperationException("boom"),
                () => restored = true));

        Assert.True(restored);
    }

    [Fact]
    public async Task ExecuteWithPreprocessingAsync_Generic_RestoresState_OnException()
    {
        var restored = false;

        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await SsmlHelper.ExecuteWithPreprocessingAsync<byte[]>(
                _preprocessor,
                "<speak>Hello</speak>",
                _ => { },
                _ => throw new InvalidOperationException("boom"),
                () => restored = true));

        Assert.True(restored);
    }

    [Fact]
    public async Task ExecuteWithPreprocessingAsync_PassesProsodyHints()
    {
        float? capturedRate = null;

        await SsmlHelper.ExecuteWithPreprocessingAsync(
            _preprocessor,
            "<speak><prosody rate=\"fast\">Hello</prosody></speak>",
            result => capturedRate = result.RateMultiplier,
            _ => Task.CompletedTask,
            () => { });

        Assert.Equal(1.5f, capturedRate);
    }
}
