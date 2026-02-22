using TextToVoice.Core;
using TextToVoice.Engines.Piper;

namespace TextToVoice.Core.Tests;

public class PiperEngineTests
{
    [Fact]
    public void Constructor_WithModelPath_Succeeds()
    {
        var exception = Record.Exception(
            () => new PiperEngine(new PiperOptions { ModelPath = "voice.onnx" })
        );

        Assert.Null(exception);
    }

    [Fact]
    public void Constructor_WithExecutablePath_Succeeds()
    {
        var exception = Record.Exception(
            () =>
                new PiperEngine(
                    new PiperOptions
                    {
                        ModelPath = "voice.onnx",
                        ExecutablePath = "/usr/local/bin/piper",
                    }
                )
        );

        Assert.Null(exception);
    }

    [Fact]
    public void Constructor_NullOptions_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new PiperEngine(null!));
    }

    [Fact]
    public void Constructor_EmptyModelPath_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(
            () => new PiperEngine(new PiperOptions { ModelPath = "" })
        );
    }

    [Fact]
    public void GetAvailableVoices_ReturnsModelAsVoice()
    {
        using var engine = new PiperEngine(
            new PiperOptions { ModelPath = "en_US-lessac-medium.onnx" }
        );

        var voices = engine.GetAvailableVoices();

        Assert.Single(voices);
        Assert.Equal("en_US-lessac-medium", voices[0].Name);
    }

    [Fact]
    public void SetVoice_ThrowsNotSupportedException()
    {
        using var engine = new PiperEngine(new PiperOptions { ModelPath = "voice.onnx" });

        Assert.Throws<NotSupportedException>(() => engine.SetVoice("any"));
    }

    [Fact]
    public void SupportsNativeSsml_ReturnsFalse()
    {
        using var engine = new PiperEngine(new PiperOptions { ModelPath = "voice.onnx" });
        Assert.False(((ISsmlCapable)engine).SupportsNativeSsml);
    }

    [Fact]
    public void Dispose_CanBeCalledMultipleTimes()
    {
        var engine = new PiperEngine(new PiperOptions { ModelPath = "voice.onnx" });

        var exception = Record.Exception(() =>
        {
            engine.Dispose();
            engine.Dispose();
        });

        Assert.Null(exception);
    }
}
