namespace TextToVoice.Core.Tests;

public class TtsOptionsTests
{
    [Fact]
    public void DefaultValues_AreCorrect()
    {
        var options = new TtsOptions();

        Assert.Null(options.VoiceName);
        Assert.Equal(0, options.Rate);
        Assert.Equal(100, options.Volume);
        Assert.Equal(AudioFormat.Wav, options.OutputFormat);
    }

    [Fact]
    public void Properties_CanBeSet()
    {
        var options = new TtsOptions
        {
            VoiceName = "Test Voice",
            Rate = 5,
            Volume = 80,
            OutputFormat = AudioFormat.Mp3
        };

        Assert.Equal("Test Voice", options.VoiceName);
        Assert.Equal(5, options.Rate);
        Assert.Equal(80, options.Volume);
        Assert.Equal(AudioFormat.Mp3, options.OutputFormat);
    }

    [Theory]
    [InlineData(-10)]
    [InlineData(0)]
    [InlineData(10)]
    public void Rate_AcceptsValidRange(int rate)
    {
        var options = new TtsOptions { Rate = rate };
        Assert.Equal(rate, options.Rate);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(50)]
    [InlineData(100)]
    public void Volume_AcceptsValidRange(int volume)
    {
        var options = new TtsOptions { Volume = volume };
        Assert.Equal(volume, options.Volume);
    }
}
