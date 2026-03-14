namespace TextToVoice.Core.Tests;

public class TtsEngineFactoryTests : IDisposable
{
    public void Dispose() => TtsEngineFactory.Reset();

    [Theory]
    [InlineData(null, TtsEngineType.Auto)]
    [InlineData("", TtsEngineType.Auto)]
    [InlineData("  ", TtsEngineType.Auto)]
    [InlineData("auto", TtsEngineType.Auto)]
    [InlineData("windows", TtsEngineType.Windows)]
    [InlineData("piper", TtsEngineType.Piper)]
    [InlineData("elevenlabs", TtsEngineType.ElevenLabs)]
    public void Parse_ValidInput_ReturnsExpectedType(string? input, TtsEngineType expected)
    {
        var result = TtsEngineFactory.Parse(input);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("Windows")]
    [InlineData("PIPER")]
    [InlineData("Piper")]
    public void Parse_CaseInsensitive(string input)
    {
        var exception = Record.Exception(() => TtsEngineFactory.Parse(input));
        Assert.Null(exception);
    }

    [Theory]
    [InlineData("unknown")]
    [InlineData("espeak")]
    [InlineData("azure")]
    public void Parse_UnknownEngine_ThrowsArgumentException(string input)
    {
        Assert.Throws<ArgumentException>(() => TtsEngineFactory.Parse(input));
    }

    [Fact]
    public void Create_UnregisteredType_ThrowsInvalidOperationException()
    {
        // ElevenLabs is never registered in tests
        Assert.Throws<InvalidOperationException>(
            () => TtsEngineFactory.Create(TtsEngineType.ElevenLabs)
        );
    }

    [Fact]
    public void Register_AndCreate_ReturnsEngineInstance()
    {
        var created = false;
        TtsEngineFactory.Register(
            TtsEngineType.Piper,
            () =>
            {
                created = true;
                return new StubEngine();
            }
        );

        var engine = TtsEngineFactory.Create(TtsEngineType.Piper);

        Assert.True(created);
        Assert.NotNull(engine);
        Assert.IsType<StubEngine>(engine);
    }

    [Fact]
    public void GetAvailableTypes_ContainsRegisteredTypes()
    {
        TtsEngineFactory.Register(TtsEngineType.Piper, () => new StubEngine());

        var types = TtsEngineFactory.GetAvailableTypes();

        Assert.Contains(TtsEngineType.Piper, types);
    }

    [Fact]
    public async Task ConcurrentRegistration_DoesNotThrow()
    {
        var tasks = Enumerable.Range(0, 100).Select(i =>
            Task.Run(() =>
            {
                TtsEngineFactory.Register(TtsEngineType.Piper, () => new StubEngine());
                _ = TtsEngineFactory.GetAvailableTypes().ToList();
            }));

        await Task.WhenAll(tasks);
    }

    [Fact]
    public async Task ConcurrentCreateAndRegister_DoesNotThrow()
    {
        TtsEngineFactory.Register(TtsEngineType.Piper, () => new StubEngine());

        var tasks = Enumerable.Range(0, 100).Select(i =>
            Task.Run(() =>
            {
                if (i % 2 == 0)
                    TtsEngineFactory.Register(TtsEngineType.Piper, () => new StubEngine());
                else
                    TtsEngineFactory.Create(TtsEngineType.Piper).Dispose();
            }));

        await Task.WhenAll(tasks);
    }

    private class StubEngine : ITtsEngine
    {
        public Task SpeakAsync(string text, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task<byte[]> SynthesizeToAudioAsync(
            string text,
            CancellationToken cancellationToken = default
        ) => Task.FromResult(Array.Empty<byte>());

        public Task SaveToFileAsync(
            string text,
            string filePath,
            CancellationToken cancellationToken = default
        ) => Task.CompletedTask;

        public IReadOnlyList<VoiceInfo> GetAvailableVoices() => [];

        public void SetVoice(string voiceName) { }

        public void SetRate(int rate) { }

        public void SetVolume(int volume) { }

        public void Dispose() { }
    }
}
