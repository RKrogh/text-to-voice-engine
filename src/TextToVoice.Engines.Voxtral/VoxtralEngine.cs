using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using TextToVoice.Core;

namespace TextToVoice.Engines.Voxtral;

/// <summary>
/// Cloud TTS engine using the Mistral Voxtral API.
/// Supports streaming (SSE), voice cloning, and multiple output formats.
/// API docs: https://docs.mistral.ai/capabilities/audio/text_to_speech/
/// </summary>
public class VoxtralEngine : ITtsEngine, ISsmlCapable, IAsyncDisposable
{
    private const string BaseUrl = "https://api.mistral.ai";

    private readonly VoxtralOptions _options;
    private readonly HttpClient _httpClient;
    private readonly ISsmlPreprocessor _preprocessor;
    private readonly string? _refAudioBase64;
    private string? _voiceId;
    private bool _disposed;

    public VoxtralEngine(VoxtralOptions options, ISsmlPreprocessor? preprocessor = null)
        : this(options, preprocessor, null) { }

    internal VoxtralEngine(
        VoxtralOptions options,
        ISsmlPreprocessor? preprocessor,
        HttpClient? httpClient)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));

        if (string.IsNullOrEmpty(options.ApiKey))
            throw new ArgumentException("ApiKey is required", nameof(options));

        _preprocessor = preprocessor ?? new SsmlPreprocessor();
        _voiceId = options.VoiceId;

        if (!string.IsNullOrEmpty(options.RefAudioPath))
        {
            if (!File.Exists(options.RefAudioPath))
                throw new FileNotFoundException(
                    $"Reference audio file not found: {options.RefAudioPath}", options.RefAudioPath);

            _refAudioBase64 = Convert.ToBase64String(File.ReadAllBytes(options.RefAudioPath));
        }

        _httpClient = httpClient ?? new HttpClient();
        _httpClient.DefaultRequestHeaders.TryAddWithoutValidation(
            "Authorization", $"Bearer {options.ApiKey}");
    }

    public async Task SpeakAsync(string text, CancellationToken cancellationToken = default)
    {
        var ext = $".{_options.ResponseFormat}";
        var tempFile = Path.Combine(Path.GetTempPath(), $"voxtral_{Guid.NewGuid()}{ext}");

        try
        {
            await SaveToFileAsync(text, tempFile, cancellationToken);

            if (_options.ResponseFormat == "wav" && _options.LeadingSilenceMs > 0)
                WavUtils.PrependSilence(tempFile, tempFile, _options.LeadingSilenceMs);

            await AudioPlayer.PlayAsync(tempFile, cancellationToken);
        }
        finally
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }

    public async Task<byte[]> SynthesizeToAudioAsync(
        string text,
        CancellationToken cancellationToken = default)
    {
        if (_options.Stream)
            return await CallTtsStreamingAsync(text, cancellationToken);

        return await CallTtsAsync(text, cancellationToken);
    }

    public async Task SaveToFileAsync(
        string text,
        string filePath,
        CancellationToken cancellationToken = default)
    {
        var audioBytes = await SynthesizeToAudioAsync(text, cancellationToken);
        await File.WriteAllBytesAsync(filePath, audioBytes, cancellationToken);
    }

    public IReadOnlyList<VoiceInfo> GetAvailableVoices()
    {
        try
        {
            return GetAvailableVoicesAsync().GetAwaiter().GetResult();
        }
        catch
        {
            return [];
        }
    }

    public async Task<IReadOnlyList<VoiceInfo>> GetAvailableVoicesAsync(
        CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync(
            $"{BaseUrl}/v1/audio/voices", cancellationToken);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadFromJsonAsync<VoicesListResponse>(cancellationToken);
        if (json?.Data == null)
            return [];

        return json.Data
            .Select(v => new VoiceInfo(
                v.Id,
                string.Join(", ", v.Languages ?? []),
                v.Gender ?? "Unknown",
                v.Name))
            .ToList();
    }

    public void SetVoice(string voiceName)
    {
        _voiceId = voiceName;
    }

    public void SetRate(int rate)
    {
        // Voxtral doesn't expose a speed parameter in the API.
        // Rate is best handled via SSML preprocessing or voice prompt style.
    }

    public void SetVolume(int volume)
    {
        // Voxtral doesn't have a direct volume control.
        // Volume is best handled client-side.
    }

    // ISsmlCapable

    public bool SupportsNativeSsml => false;

    public Task SpeakSsmlAsync(string ssml, CancellationToken cancellationToken = default)
    {
        var savedVoiceId = _voiceId;
        return SsmlHelper.ExecuteWithPreprocessingAsync(
            _preprocessor, ssml,
            ApplyPreprocessResult,
            text => SpeakAsync(text, cancellationToken),
            () => { _voiceId = savedVoiceId; });
    }

    public Task<byte[]> SynthesizeSsmlToAudioAsync(
        string ssml,
        CancellationToken cancellationToken = default)
    {
        var savedVoiceId = _voiceId;
        return SsmlHelper.ExecuteWithPreprocessingAsync(
            _preprocessor, ssml,
            ApplyPreprocessResult,
            text => SynthesizeToAudioAsync(text, cancellationToken),
            () => { _voiceId = savedVoiceId; });
    }

    public Task SaveSsmlToFileAsync(
        string ssml,
        string filePath,
        CancellationToken cancellationToken = default)
    {
        var savedVoiceId = _voiceId;
        return SsmlHelper.ExecuteWithPreprocessingAsync(
            _preprocessor, ssml,
            ApplyPreprocessResult,
            text => SaveToFileAsync(text, filePath, cancellationToken),
            () => { _voiceId = savedVoiceId; });
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _httpClient.Dispose();
            _disposed = true;
        }
    }

    public ValueTask DisposeAsync()
    {
        Dispose();
        return ValueTask.CompletedTask;
    }

    private void ApplyPreprocessResult(SsmlPreprocessResult result)
    {
        if (result.VoiceName != null)
            _voiceId = result.VoiceName;
    }

    private void WarnIfLongInput(string text)
    {
        if (_options.MaxWordCountWarning <= 0) return;

        var wordCount = text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).Length;
        if (wordCount > _options.MaxWordCountWarning)
        {
            _options.OnWarning?.Invoke(
                $"Warning: Input is ~{wordCount} words. " +
                $"Voxtral recommends keeping input under {_options.MaxWordCountWarning} words per request.");
        }
    }

    /// <summary>
    /// Non-streaming API call. Returns the full audio as a byte array.
    /// </summary>
    private async Task<byte[]> CallTtsAsync(string text, CancellationToken cancellationToken)
    {
        WarnIfLongInput(text);

        var payload = BuildPayload(text, stream: false);

        var response = await _httpClient.PostAsJsonAsync(
            $"{BaseUrl}/v1/audio/speech", payload, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new HttpRequestException(
                $"Voxtral API returned {(int)response.StatusCode}: {errorBody}");
        }

        var result = await response.Content.ReadFromJsonAsync<SpeechResponse>(cancellationToken);
        if (result?.AudioData == null)
            throw new InvalidOperationException("Voxtral API returned no audio data");

        return Convert.FromBase64String(result.AudioData);
    }

    /// <summary>
    /// Streaming API call via SSE. Collects audio chunks and returns the concatenated result.
    /// Reduces time-to-first-byte compared to non-streaming.
    /// </summary>
    private async Task<byte[]> CallTtsStreamingAsync(string text, CancellationToken cancellationToken)
    {
        WarnIfLongInput(text);

        var payload = BuildPayload(text, stream: true);
        var jsonContent = JsonSerializer.Serialize(payload);

        using var request = new HttpRequestMessage(HttpMethod.Post, $"{BaseUrl}/v1/audio/speech")
        {
            Content = new StringContent(jsonContent, Encoding.UTF8, "application/json")
        };

        using var response = await _httpClient.SendAsync(
            request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new HttpRequestException(
                $"Voxtral API returned {(int)response.StatusCode}: {errorBody}");
        }

        return await ReadSseAudioChunksAsync(response, cancellationToken);
    }

    /// <summary>
    /// Reads SSE stream, extracts base64 audio chunks from speech.audio.delta events,
    /// and concatenates them into a single byte array.
    /// </summary>
    private static async Task<byte[]> ReadSseAudioChunksAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var reader = new StreamReader(stream, Encoding.UTF8);

        var audioChunks = new List<byte[]>();
        string? eventType = null;
        string? line;

        while ((line = await reader.ReadLineAsync(cancellationToken)) != null)
        {

            if (line.StartsWith("event:", StringComparison.Ordinal))
            {
                eventType = line[6..].Trim();
                continue;
            }

            if (line.StartsWith("data:", StringComparison.Ordinal))
            {
                var data = line[5..].Trim();

                if (data == "[DONE]")
                    break;

                if (eventType == "speech.audio.delta")
                {
                    try
                    {
                        var chunk = JsonSerializer.Deserialize<SseDataChunk>(data);
                        if (chunk?.AudioData != null)
                        {
                            audioChunks.Add(Convert.FromBase64String(chunk.AudioData));
                        }
                    }
                    catch (JsonException)
                    {
                        // Skip malformed chunks
                    }
                }

                eventType = null;
                continue;
            }

            // Empty line resets event state (SSE spec)
            if (string.IsNullOrEmpty(line))
            {
                eventType = null;
            }
        }

        if (audioChunks.Count == 0)
            throw new InvalidOperationException("Voxtral streaming returned no audio data");

        // Concatenate all chunks
        var totalLength = audioChunks.Sum(c => c.Length);
        var result = new byte[totalLength];
        var offset = 0;
        foreach (var chunk in audioChunks)
        {
            Buffer.BlockCopy(chunk, 0, result, offset, chunk.Length);
            offset += chunk.Length;
        }

        return result;
    }

    private object BuildPayload(string text, bool stream)
    {
        // ref_audio takes priority over voice_id (voice cloning overrides preset)
        if (_refAudioBase64 != null)
        {
            return new
            {
                model = _options.ModelId,
                input = text,
                ref_audio = _refAudioBase64,
                response_format = _options.ResponseFormat,
                stream,
            };
        }

        return new
        {
            model = _options.ModelId,
            input = text,
            voice_id = _voiceId,
            response_format = _options.ResponseFormat,
            stream,
        };
    }

    // JSON models

    private class SpeechResponse
    {
        [JsonPropertyName("audio_data")]
        public string? AudioData { get; set; }
    }

    private class SseDataChunk
    {
        [JsonPropertyName("audio_data")]
        public string? AudioData { get; set; }
    }

    private class VoicesListResponse
    {
        [JsonPropertyName("data")]
        public List<VoiceEntry>? Data { get; set; }
    }

    private class VoiceEntry
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = "";

        [JsonPropertyName("name")]
        public string Name { get; set; } = "";

        [JsonPropertyName("languages")]
        public List<string>? Languages { get; set; }

        [JsonPropertyName("gender")]
        public string? Gender { get; set; }
    }
}
