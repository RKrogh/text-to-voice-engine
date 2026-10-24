using TextToVoice.Engines.SherpaOnnx;

namespace TextToVoice.Core.Tests;

public class SherpaOnnxPcmTests
{
    [Fact]
    public void Volume_survives_normalisation()
    {
        var samples = Pcm.Shape([0.1f, -0.25f, 0.05f], normalize: true, volume: 0.5f);

        Assert.Equal(0.5f, samples.Max(MathF.Abs), 3);
    }

    [Fact]
    public void Without_normalisation_only_volume_applies()
    {
        var samples = Pcm.Shape([0.1f, -0.2f], normalize: false, volume: 0.5f);

        Assert.Equal([0.05f, -0.1f], samples);
    }

    [Fact]
    public void Silence_stays_silent()
    {
        Assert.Equal([0f, 0f], Pcm.Shape([0f, 0f], normalize: true, volume: 1f));
    }

    [Fact]
    public void Wav_is_16_bit_mono_and_clips()
    {
        var wav = Pcm.ToWav([1.5f, -1.5f, 0.5f], 22050);

        Assert.Equal(44 + 6, wav.Length);
        Assert.Equal(22050, BitConverter.ToInt32(wav, 24));
        Assert.Equal((short)1, BitConverter.ToInt16(wav, 22));
        Assert.Equal(short.MaxValue, BitConverter.ToInt16(wav, 44));
        Assert.Equal(short.MinValue, BitConverter.ToInt16(wav, 46));
        Assert.Equal((short)16383, BitConverter.ToInt16(wav, 48));
    }

    [Fact]
    public void Voice_defaults_are_neutral()
    {
        var voice = new SherpaOnnxVoice();

        Assert.Equal(0, voice.SpeakerId);
        Assert.Equal(1f, voice.Speed);
        Assert.Equal(1f, voice.Volume);
    }

    [Fact]
    public void New_options_default_to_previous_behaviour()
    {
        var options = new SherpaOnnxOptions { ModelPath = "test.onnx" };

        Assert.True(options.Normalize);
        Assert.Null(options.VoicesPath);
    }
}
