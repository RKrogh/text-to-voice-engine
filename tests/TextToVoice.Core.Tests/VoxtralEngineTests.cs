using System.Net;
using System.Text;
using System.Text.Json;
using TextToVoice.Core;
using TextToVoice.Engines.Voxtral;

namespace TextToVoice.Core.Tests;

public class VoxtralEngineTests : IDisposable
{
    private readonly string _tempDir;

    public VoxtralEngineTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"voxtral_tests_{Guid.NewGuid()}");
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, true);
    }

    private static VoxtralOptions DefaultOptions(string apiKey = "test-key") =>
        new() { ApiKey = apiKey };

    // Constructor tests

    [Fact]
    public void Constructor_NullOptions_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new VoxtralEngine(null!));
    }

    [Fact]
    public void Constructor_EmptyApiKey_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(
            () => new VoxtralEngine(new VoxtralOptions { ApiKey = "" }));
    }

    [Fact]
    public void Constructor_ValidOptions_Succeeds()
    {
        using var engine = new VoxtralEngine(DefaultOptions());
        Assert.NotNull(engine);
    }

    [Fact]
    public void Constructor_WithRefAudioPath_FileNotFound_Throws()
    {
        Assert.Throws<FileNotFoundException>(
            () => new VoxtralEngine(new VoxtralOptions
            {
                ApiKey = "test-key",
                RefAudioPath = "/nonexistent/file.mp3",
            }));
    }

    [Fact]
    public void Constructor_WithRefAudioPath_ValidFile_Succeeds()
    {
        var refAudioPath = Path.Combine(_tempDir, "ref.mp3");
        File.WriteAllBytes(refAudioPath, [0xFF, 0xFB, 0x90, 0x00]); // minimal MP3-like header

        using var engine = new VoxtralEngine(new VoxtralOptions
        {
            ApiKey = "test-key",
            RefAudioPath = refAudioPath,
        });

        Assert.NotNull(engine);
    }

    // Options defaults

    [Fact]
    public void Options_DefaultValues_AreCorrect()
    {
        var options = new VoxtralOptions { ApiKey = "test-key" };

        Assert.Equal("gb_jane_neutral", options.VoiceId);
        Assert.Equal("voxtral-mini-tts-2603", options.ModelId);
        Assert.Equal("wav", options.ResponseFormat);
        Assert.False(options.Stream);
        Assert.Equal(150, options.LeadingSilenceMs);
        Assert.Equal(300, options.MaxWordCountWarning);
        Assert.Null(options.RefAudioPath);
        Assert.NotNull(options.OnWarning);
    }

    // SetVoice / SetRate / SetVolume

    [Fact]
    public void SetVoice_UpdatesVoice()
    {
        using var engine = new VoxtralEngine(DefaultOptions());
        engine.SetVoice("en_paul_neutral");
        // No exception; voice is applied on next API call
    }

    [Fact]
    public void SetRate_DoesNotThrow()
    {
        using var engine = new VoxtralEngine(DefaultOptions());
        engine.SetRate(-10);
        engine.SetRate(0);
        engine.SetRate(10);
    }

    [Fact]
    public void SetVolume_DoesNotThrow()
    {
        using var engine = new VoxtralEngine(DefaultOptions());
        engine.SetVolume(0);
        engine.SetVolume(50);
        engine.SetVolume(100);
    }

    // ISsmlCapable

    [Fact]
    public void SupportsNativeSsml_ReturnsFalse()
    {
        using var engine = new VoxtralEngine(DefaultOptions());
        Assert.False(((ISsmlCapable)engine).SupportsNativeSsml);
    }

    [Fact]
    public void ImplementsISsmlCapable()
    {
        using var engine = new VoxtralEngine(DefaultOptions());
        Assert.IsAssignableFrom<ISsmlCapable>(engine);
    }

    // IAsyncDisposable

    [Fact]
    public void ImplementsIAsyncDisposable()
    {
        using var engine = new VoxtralEngine(DefaultOptions());
        Assert.IsAssignableFrom<IAsyncDisposable>(engine);
    }

    [Fact]
    public void Dispose_CanBeCalledMultipleTimes()
    {
        var engine = new VoxtralEngine(DefaultOptions());
        engine.Dispose();
        engine.Dispose();
    }

    [Fact]
    public async Task DisposeAsync_CanBeCalledMultipleTimes()
    {
        var engine = new VoxtralEngine(DefaultOptions());
        await engine.DisposeAsync();
        await engine.DisposeAsync();
    }

    [Fact]
    public async Task DisposeAsync_ThenDispose_DoesNotThrow()
    {
        var engine = new VoxtralEngine(DefaultOptions());
        await engine.DisposeAsync();
        engine.Dispose();
    }

    // Factory parse

    [Theory]
    [InlineData("voxtral", TtsEngineType.Voxtral)]
    [InlineData("mistral", TtsEngineType.Voxtral)]
    [InlineData("Voxtral", TtsEngineType.Voxtral)]
    [InlineData("MISTRAL", TtsEngineType.Voxtral)]
    public void FactoryParse_Voxtral(string input, TtsEngineType expected)
    {
        Assert.Equal(expected, TtsEngineFactory.Parse(input));
    }

    // HTTP API tests with mocked HttpClient

    [Fact]
    public async Task SynthesizeToAudioAsync_ReturnsDecodedAudio()
    {
        var expectedAudio = BuildMinimalWav();
        var base64Audio = Convert.ToBase64String(expectedAudio);
        var responseJson = JsonSerializer.Serialize(new { audio_data = base64Audio });

        var handler = new MockHttpHandler(responseJson, HttpStatusCode.OK);
        var httpClient = new HttpClient(handler);

        using var engine = new VoxtralEngine(DefaultOptions(), null, httpClient);
        var result = await engine.SynthesizeToAudioAsync("Hello");

        Assert.Equal(expectedAudio, result);
    }

    [Fact]
    public async Task SynthesizeToAudioAsync_ApiError_ThrowsHttpRequestException()
    {
        var handler = new MockHttpHandler(
            """{"object":"error","message":"Bad request"}""",
            HttpStatusCode.BadRequest);
        var httpClient = new HttpClient(handler);

        using var engine = new VoxtralEngine(DefaultOptions(), null, httpClient);

        var ex = await Assert.ThrowsAsync<HttpRequestException>(
            () => engine.SynthesizeToAudioAsync("Hello"));

        Assert.Contains("400", ex.Message);
        Assert.Contains("Bad request", ex.Message);
    }

    [Fact]
    public async Task SynthesizeToAudioAsync_NullAudioData_ThrowsInvalidOperation()
    {
        var handler = new MockHttpHandler(
            """{"audio_data": null}""",
            HttpStatusCode.OK);
        var httpClient = new HttpClient(handler);

        using var engine = new VoxtralEngine(DefaultOptions(), null, httpClient);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => engine.SynthesizeToAudioAsync("Hello"));
    }

    [Fact]
    public async Task SaveToFileAsync_WritesFile()
    {
        var audioBytes = BuildMinimalWav();
        var responseJson = JsonSerializer.Serialize(new { audio_data = Convert.ToBase64String(audioBytes) });
        var handler = new MockHttpHandler(responseJson, HttpStatusCode.OK);
        var httpClient = new HttpClient(handler);

        var outputPath = Path.Combine(_tempDir, "output.wav");

        using var engine = new VoxtralEngine(DefaultOptions(), null, httpClient);
        await engine.SaveToFileAsync("Hello", outputPath);

        Assert.True(File.Exists(outputPath));
        Assert.Equal(audioBytes, await File.ReadAllBytesAsync(outputPath));
    }

    [Fact]
    public async Task SynthesizeToAudioAsync_SendsCorrectPayload()
    {
        var audioBytes = BuildMinimalWav();
        var responseJson = JsonSerializer.Serialize(new { audio_data = Convert.ToBase64String(audioBytes) });
        var handler = new MockHttpHandler(responseJson, HttpStatusCode.OK);
        var httpClient = new HttpClient(handler);

        using var engine = new VoxtralEngine(
            new VoxtralOptions { ApiKey = "test-key", VoiceId = "en_paul_neutral" },
            null, httpClient);

        await engine.SynthesizeToAudioAsync("Test text");

        Assert.NotNull(handler.LastRequestBody);

        var payload = JsonDocument.Parse(handler.LastRequestBody);
        Assert.Equal("Test text", payload.RootElement.GetProperty("input").GetString());
        Assert.Equal("voxtral-mini-tts-2603", payload.RootElement.GetProperty("model").GetString());
        Assert.Equal("en_paul_neutral", payload.RootElement.GetProperty("voice_id").GetString());
        Assert.Equal("wav", payload.RootElement.GetProperty("response_format").GetString());
        Assert.False(payload.RootElement.GetProperty("stream").GetBoolean());
    }

    [Fact]
    public async Task SynthesizeToAudioAsync_WithRefAudio_SendsRefAudioInsteadOfVoiceId()
    {
        var refAudioPath = Path.Combine(_tempDir, "ref.mp3");
        var refAudioBytes = new byte[] { 0xFF, 0xFB, 0x90, 0x00 };
        File.WriteAllBytes(refAudioPath, refAudioBytes);

        var audioBytes = BuildMinimalWav();
        var responseJson = JsonSerializer.Serialize(new { audio_data = Convert.ToBase64String(audioBytes) });
        var handler = new MockHttpHandler(responseJson, HttpStatusCode.OK);
        var httpClient = new HttpClient(handler);

        using var engine = new VoxtralEngine(
            new VoxtralOptions { ApiKey = "test-key", RefAudioPath = refAudioPath },
            null, httpClient);

        await engine.SynthesizeToAudioAsync("Hello");

        var payload = JsonDocument.Parse(handler.LastRequestBody!);
        Assert.True(payload.RootElement.TryGetProperty("ref_audio", out var refAudio));
        Assert.Equal(Convert.ToBase64String(refAudioBytes), refAudio.GetString());
        Assert.False(payload.RootElement.TryGetProperty("voice_id", out _));
    }

    [Fact]
    public async Task SynthesizeToAudioAsync_WarnsOnLongInput()
    {
        var audioBytes = BuildMinimalWav();
        var responseJson = JsonSerializer.Serialize(new { audio_data = Convert.ToBase64String(audioBytes) });
        var handler = new MockHttpHandler(responseJson, HttpStatusCode.OK);
        var httpClient = new HttpClient(handler);

        string? warningMessage = null;
        using var engine = new VoxtralEngine(
            new VoxtralOptions
            {
                ApiKey = "test-key",
                MaxWordCountWarning = 5,
                OnWarning = msg => warningMessage = msg,
            },
            null, httpClient);

        var longText = string.Join(" ", Enumerable.Repeat("word", 10));
        await engine.SynthesizeToAudioAsync(longText);

        Assert.NotNull(warningMessage);
        Assert.Contains("10 words", warningMessage);
    }

    [Fact]
    public async Task GetAvailableVoicesAsync_ParsesResponse()
    {
        var voicesJson = """
        {
            "data": [
                {"id": "gb_jane_neutral", "name": "Jane Neutral", "languages": ["en"], "gender": "female"},
                {"id": "en_paul_neutral", "name": "Paul Neutral", "languages": ["en", "fr"], "gender": "male"}
            ]
        }
        """;
        var handler = new MockHttpHandler(voicesJson, HttpStatusCode.OK);
        var httpClient = new HttpClient(handler);

        using var engine = new VoxtralEngine(DefaultOptions(), null, httpClient);
        var voices = await engine.GetAvailableVoicesAsync();

        Assert.Equal(2, voices.Count);
        Assert.Equal("gb_jane_neutral", voices[0].Name);
        Assert.Equal("female", voices[0].Gender);
        Assert.Equal("en", voices[0].Culture);
        Assert.Equal("en_paul_neutral", voices[1].Name);
        Assert.Equal("en, fr", voices[1].Culture);
    }

    [Fact]
    public async Task GetAvailableVoicesAsync_EmptyResponse_ReturnsEmptyList()
    {
        var handler = new MockHttpHandler("""{"data": []}""", HttpStatusCode.OK);
        var httpClient = new HttpClient(handler);

        using var engine = new VoxtralEngine(DefaultOptions(), null, httpClient);
        var voices = await engine.GetAvailableVoicesAsync();

        Assert.Empty(voices);
    }

    [Fact]
    public void GetAvailableVoices_OnApiError_ReturnsEmptyList()
    {
        var handler = new MockHttpHandler("error", HttpStatusCode.InternalServerError);
        var httpClient = new HttpClient(handler);

        using var engine = new VoxtralEngine(DefaultOptions(), null, httpClient);
        var voices = engine.GetAvailableVoices();

        Assert.Empty(voices);
    }

    // SSE streaming tests

    [Fact]
    public async Task SynthesizeToAudioAsync_Streaming_CollectsChunks()
    {
        var chunk1 = new byte[] { 0x01, 0x02, 0x03 };
        var chunk2 = new byte[] { 0x04, 0x05, 0x06 };

        var sseContent = new StringBuilder();
        sseContent.AppendLine("event: speech.audio.delta");
        sseContent.AppendLine($"data: {{\"audio_data\":\"{Convert.ToBase64String(chunk1)}\"}}");
        sseContent.AppendLine();
        sseContent.AppendLine("event: speech.audio.delta");
        sseContent.AppendLine($"data: {{\"audio_data\":\"{Convert.ToBase64String(chunk2)}\"}}");
        sseContent.AppendLine();
        sseContent.AppendLine("event: speech.audio.done");
        sseContent.AppendLine("data: {\"usage\":{\"input_tokens\":5}}");
        sseContent.AppendLine();
        sseContent.AppendLine("data: [DONE]");

        var handler = new MockHttpHandler(sseContent.ToString(), HttpStatusCode.OK, "text/event-stream");
        var httpClient = new HttpClient(handler);

        using var engine = new VoxtralEngine(
            new VoxtralOptions { ApiKey = "test-key", Stream = true },
            null, httpClient);

        var result = await engine.SynthesizeToAudioAsync("Hello");

        Assert.Equal([0x01, 0x02, 0x03, 0x04, 0x05, 0x06], result);
    }

    [Fact]
    public async Task SynthesizeToAudioAsync_Streaming_NoChunks_Throws()
    {
        var sseContent = "data: [DONE]\n";
        var handler = new MockHttpHandler(sseContent, HttpStatusCode.OK, "text/event-stream");
        var httpClient = new HttpClient(handler);

        using var engine = new VoxtralEngine(
            new VoxtralOptions { ApiKey = "test-key", Stream = true },
            null, httpClient);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => engine.SynthesizeToAudioAsync("Hello"));
    }

    [Fact]
    public async Task SynthesizeToAudioAsync_Streaming_MalformedChunks_AreSkipped()
    {
        var validChunk = new byte[] { 0x01, 0x02 };

        var sseContent = new StringBuilder();
        sseContent.AppendLine("event: speech.audio.delta");
        sseContent.AppendLine("data: {not valid json}");
        sseContent.AppendLine();
        sseContent.AppendLine("event: speech.audio.delta");
        sseContent.AppendLine($"data: {{\"audio_data\":\"{Convert.ToBase64String(validChunk)}\"}}");
        sseContent.AppendLine();
        sseContent.AppendLine("data: [DONE]");

        var handler = new MockHttpHandler(sseContent.ToString(), HttpStatusCode.OK, "text/event-stream");
        var httpClient = new HttpClient(handler);

        using var engine = new VoxtralEngine(
            new VoxtralOptions { ApiKey = "test-key", Stream = true },
            null, httpClient);

        var result = await engine.SynthesizeToAudioAsync("Hello");

        Assert.Equal(validChunk, result);
    }

    // Helpers

    private static byte[] BuildMinimalWav()
    {
        using var ms = new MemoryStream();
        using var w = new BinaryWriter(ms);
        w.Write("RIFF"u8);
        w.Write(36); // file size - 8
        w.Write("WAVE"u8);
        w.Write("fmt "u8);
        w.Write(16);       // chunk size
        w.Write((short)1); // PCM
        w.Write((short)1); // mono
        w.Write(24000);    // sample rate
        w.Write(48000);    // byte rate
        w.Write((short)2); // block align
        w.Write((short)16);// bits per sample
        w.Write("data"u8);
        w.Write(0);        // data size (empty)
        return ms.ToArray();
    }

    private class MockHttpHandler(
        string responseBody,
        HttpStatusCode statusCode,
        string contentType = "application/json") : HttpMessageHandler
    {
        public string? LastRequestBody { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            if (request.Content != null)
                LastRequestBody = await request.Content.ReadAsStringAsync(cancellationToken);

            return new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(responseBody, Encoding.UTF8, contentType),
            };
        }
    }
}
