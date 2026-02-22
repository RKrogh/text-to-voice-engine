using TextToVoice.Core;
using TextToVoice.Engines.SherpaOnnx;

namespace TextToVoice.Core.Tests;

public class SherpaOnnxEngineTests
{
    [Fact]
    public void Constructor_NullOptions_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new SherpaOnnxEngine(null!));
    }

    [Fact]
    public void Constructor_EmptyModelPath_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(
            () => new SherpaOnnxEngine(new SherpaOnnxOptions { ModelPath = "" })
        );
    }

    [Fact]
    public void Options_DefaultValues_AreCorrect()
    {
        var options = new SherpaOnnxOptions { ModelPath = "test.onnx" };

        Assert.Equal(0, options.SpeakerId);
        Assert.Equal(1.0f, options.LengthScale);
        Assert.Equal(0.667f, options.NoiseScale);
        Assert.Equal(0.8f, options.NoiseScaleW);
        Assert.Equal(1, options.NumThreads);
        Assert.Null(options.TokensPath);
        Assert.Null(options.DataDir);
    }

    [Fact]
    public void FactoryParse_SherpaOnnx_Variants()
    {
        Assert.Equal(TtsEngineType.SherpaOnnx, TtsEngineFactory.Parse("sherpaonnx"));
        Assert.Equal(TtsEngineType.SherpaOnnx, TtsEngineFactory.Parse("sherpa-onnx"));
        Assert.Equal(TtsEngineType.SherpaOnnx, TtsEngineFactory.Parse("sherpa"));
        Assert.Equal(TtsEngineType.SherpaOnnx, TtsEngineFactory.Parse("onnx"));
    }
}
