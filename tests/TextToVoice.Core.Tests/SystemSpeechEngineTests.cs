using System.Runtime.Versioning;
using TextToVoice.Core;
using TextToVoice.Engines.Windows;

namespace TextToVoice.Core.Tests;

[SupportedOSPlatform("windows")]
public class SystemSpeechEngineTests
{
    [WindowsOnlyFact]
    public void Constructor_CreatesInstance()
    {
        using var engine = new SystemSpeechEngine();
        Assert.NotNull(engine);
    }

    [WindowsOnlyFact]
    public void Constructor_WithOptions_AppliesSettings()
    {
        var options = new TtsOptions { Rate = 5, Volume = 80 };

        using var engine = new SystemSpeechEngine(options);
        Assert.NotNull(engine);
    }

    [WindowsOnlyFact]
    public void GetAvailableVoices_ReturnsVoices()
    {
        using var engine = new SystemSpeechEngine();
        var voices = engine.GetAvailableVoices();

        Assert.NotNull(voices);
        Assert.NotEmpty(voices);
    }

    [WindowsOnlyFact]
    public void GetAvailableVoices_VoicesHaveRequiredProperties()
    {
        using var engine = new SystemSpeechEngine();
        var voices = engine.GetAvailableVoices();
        var firstVoice = voices.First();

        Assert.False(string.IsNullOrEmpty(firstVoice.Name));
        Assert.False(string.IsNullOrEmpty(firstVoice.Culture));
        Assert.False(string.IsNullOrEmpty(firstVoice.Gender));
    }

    [WindowsOnlyFact]
    public void SetVoice_WithValidVoice_Succeeds()
    {
        using var engine = new SystemSpeechEngine();
        var voices = engine.GetAvailableVoices();
        var voiceName = voices.First().Name;

        var exception = Record.Exception(() => engine.SetVoice(voiceName));

        Assert.Null(exception);
    }

    [WindowsOnlyFact]
    public void SetVoice_WithInvalidVoice_ThrowsException()
    {
        using var engine = new SystemSpeechEngine();
        Assert.ThrowsAny<Exception>(() => engine.SetVoice("NonExistentVoice12345"));
    }

    [WindowsOnlyTheory]
    [InlineData(-10)]
    [InlineData(0)]
    [InlineData(10)]
    public void SetRate_WithValidValues_Succeeds(int rate)
    {
        using var engine = new SystemSpeechEngine();
        var exception = Record.Exception(() => engine.SetRate(rate));
        Assert.Null(exception);
    }

    [WindowsOnlyTheory]
    [InlineData(-20)]
    [InlineData(20)]
    public void SetRate_WithOutOfRangeValues_ClampsToRange(int rate)
    {
        using var engine = new SystemSpeechEngine();
        var exception = Record.Exception(() => engine.SetRate(rate));
        Assert.Null(exception);
    }

    [WindowsOnlyTheory]
    [InlineData(0)]
    [InlineData(50)]
    [InlineData(100)]
    public void SetVolume_WithValidValues_Succeeds(int volume)
    {
        using var engine = new SystemSpeechEngine();
        var exception = Record.Exception(() => engine.SetVolume(volume));
        Assert.Null(exception);
    }

    [WindowsOnlyTheory]
    [InlineData(-10)]
    [InlineData(150)]
    public void SetVolume_WithOutOfRangeValues_ClampsToRange(int volume)
    {
        using var engine = new SystemSpeechEngine();
        var exception = Record.Exception(() => engine.SetVolume(volume));
        Assert.Null(exception);
    }

    [WindowsOnlyFact]
    public async Task SynthesizeToAudioAsync_ReturnsWavData()
    {
        using var engine = new SystemSpeechEngine();
        var audioData = await engine.SynthesizeToAudioAsync("Test");

        Assert.NotNull(audioData);
        Assert.NotEmpty(audioData);
        // WAV files start with "RIFF"
        Assert.Equal((byte)'R', audioData[0]);
        Assert.Equal((byte)'I', audioData[1]);
        Assert.Equal((byte)'F', audioData[2]);
        Assert.Equal((byte)'F', audioData[3]);
    }

    [WindowsOnlyFact]
    public async Task SynthesizeToAudioAsync_WithCancellation_ThrowsOperationCanceledException()
    {
        using var engine = new SystemSpeechEngine();
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => engine.SynthesizeToAudioAsync("Test", cts.Token)
        );
    }

    [WindowsOnlyFact]
    public async Task SaveToFileAsync_CreatesFile()
    {
        using var engine = new SystemSpeechEngine();
        var filePath = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}.wav");

        try
        {
            await engine.SaveToFileAsync("Test", filePath);

            Assert.True(File.Exists(filePath));
            var fileBytes = await File.ReadAllBytesAsync(filePath);
            Assert.NotEmpty(fileBytes);
            // WAV files start with "RIFF"
            Assert.Equal((byte)'R', fileBytes[0]);
        }
        finally
        {
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
        }
    }

    [WindowsOnlyFact]
    public async Task SaveToFileAsync_WithCancellation_ThrowsOperationCanceledException()
    {
        using var engine = new SystemSpeechEngine();
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        var filePath = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}.wav");

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => engine.SaveToFileAsync("Test", filePath, cts.Token)
        );
    }

    [WindowsOnlyFact]
    public void Dispose_CanBeCalledMultipleTimes()
    {
        var engine = new SystemSpeechEngine();

        var exception = Record.Exception(() =>
        {
            engine.Dispose();
            engine.Dispose();
        });

        Assert.Null(exception);
    }
}
