namespace TextToVoice.Core.Tests;

public class AudioFormatTests
{
    [Fact]
    public void Wav_HasExpectedValue()
    {
        Assert.Equal(0, (int)AudioFormat.Wav);
    }

    [Fact]
    public void Mp3_HasExpectedValue()
    {
        Assert.Equal(1, (int)AudioFormat.Mp3);
    }

    [Fact]
    public void AllFormats_AreDefined()
    {
        var formats = Enum.GetValues<AudioFormat>();
        Assert.Equal(2, formats.Length);
        Assert.Contains(AudioFormat.Wav, formats);
        Assert.Contains(AudioFormat.Mp3, formats);
    }
}
