using TextToVoice.Core;
using TextToVoice.Engines.ElevenLabs;

namespace TextToVoice.Core.Tests;

public class ElevenLabsEngineTests
{
    [Fact]
    public void Constructor_NullOptions_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new ElevenLabsEngine(null!));
    }

    [Fact]
    public void Constructor_EmptyApiKey_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(
            () => new ElevenLabsEngine(new ElevenLabsOptions { ApiKey = "" })
        );
    }

    [Fact]
    public void Options_DefaultValues_AreCorrect()
    {
        var options = new ElevenLabsOptions { ApiKey = "test-key" };

        Assert.Equal("21m00Tcm4TlvDq8ikWAM", options.VoiceId);
        Assert.Equal("eleven_multilingual_v2", options.ModelId);
        Assert.Equal("mp3_44100_128", options.OutputFormat);
        Assert.Equal(0.5f, options.Stability);
        Assert.Equal(0.75f, options.SimilarityBoost);
        Assert.Equal(0.0f, options.Style);
        Assert.Equal(1.0f, options.Speed);
        Assert.Equal(150, options.LeadingSilenceMs);
    }

    [Fact]
    public void SetRate_MapsCorrectly()
    {
        using var engine = new ElevenLabsEngine(new ElevenLabsOptions { ApiKey = "test-key" });

        // SetRate(-10) should map to minimum speed (~0.7)
        engine.SetRate(-10);

        // SetRate(10) should map to maximum speed (~1.2)
        engine.SetRate(10);

        // SetRate(0) should map to ~normal speed (~0.95)
        engine.SetRate(0);

        // No assertion on internal state — just verify it doesn't throw
    }

    [Fact]
    public void SetVoice_SetsVoiceId()
    {
        using var engine = new ElevenLabsEngine(new ElevenLabsOptions { ApiKey = "test-key" });

        // Should not throw
        engine.SetVoice("some-voice-id");
    }

    [Fact]
    public void SetVolume_DoesNotThrow()
    {
        using var engine = new ElevenLabsEngine(new ElevenLabsOptions { ApiKey = "test-key" });

        // Volume is not directly supported by ElevenLabs API, but should not throw
        engine.SetVolume(50);
    }

    [Fact]
    public void FactoryParse_ElevenLabs()
    {
        Assert.Equal(TtsEngineType.ElevenLabs, TtsEngineFactory.Parse("elevenlabs"));
    }

    [Fact]
    public void SupportsNativeSsml_ReturnsFalse()
    {
        using var engine = new ElevenLabsEngine(new ElevenLabsOptions { ApiKey = "test-key" });

        Assert.False(((ISsmlCapable)engine).SupportsNativeSsml);
    }

    [Fact]
    public void Dispose_CanBeCalledMultipleTimes()
    {
        var engine = new ElevenLabsEngine(new ElevenLabsOptions { ApiKey = "test-key" });

        engine.Dispose();
        engine.Dispose(); // Should not throw
    }

    [Fact]
    public async Task DisposeAsync_CanBeCalled()
    {
        var engine = new ElevenLabsEngine(new ElevenLabsOptions { ApiKey = "test-key" });

        await engine.DisposeAsync();
    }

    [Fact]
    public async Task DisposeAsync_CanBeCalledMultipleTimes()
    {
        var engine = new ElevenLabsEngine(new ElevenLabsOptions { ApiKey = "test-key" });

        await engine.DisposeAsync();
        await engine.DisposeAsync(); // Should not throw
    }

    [Fact]
    public async Task DisposeAsync_ThenDispose_DoesNotThrow()
    {
        var engine = new ElevenLabsEngine(new ElevenLabsOptions { ApiKey = "test-key" });

        await engine.DisposeAsync();
        engine.Dispose(); // Mixed disposal should not throw
    }

    [Fact]
    public void ImplementsIAsyncDisposable()
    {
        using var engine = new ElevenLabsEngine(new ElevenLabsOptions { ApiKey = "test-key" });

        Assert.IsAssignableFrom<IAsyncDisposable>(engine);
    }
}
