using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using TextToVoice.Core;

namespace TextToVoice.Engines.ElevenLabs;

/// <summary>
/// Cloud TTS engine using the ElevenLabs API.
/// Requires an API key — see https://elevenlabs.io for pricing.
/// Free tier: 10,000 characters/month. Paid plans start at $5/month.
/// </summary>
public class ElevenLabsEngine : ITtsEngine, ISsmlCapable
{
    private const string BaseUrl = "https://api.elevenlabs.io";

    private readonly ElevenLabsOptions _options;
    private readonly HttpClient _httpClient;
    private readonly ISsmlPreprocessor _preprocessor;
    private string _voiceId;
    private float _speed;
    private bool _disposed;

    public ElevenLabsEngine(ElevenLabsOptions options, ISsmlPreprocessor? preprocessor = null)
        : this(options, preprocessor, null) { }

    internal ElevenLabsEngine(
        ElevenLabsOptions options,
        ISsmlPreprocessor? preprocessor,
        HttpClient? httpClient
    )
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));

        if (string.IsNullOrEmpty(options.ApiKey))
            throw new ArgumentException("ApiKey is required", nameof(options));

        _preprocessor = preprocessor ?? new SsmlPreprocessor();
        _voiceId = options.VoiceId;
        _speed = options.Speed;

        _httpClient = httpClient ?? new HttpClient();
        _httpClient.DefaultRequestHeaders.TryAddWithoutValidation("xi-api-key", options.ApiKey);
    }

    public async Task SpeakAsync(string text, CancellationToken cancellationToken = default)
    {
        var ext = IsPcmFormat() ? ".wav" : $".{GetFormatExtension()}";
        var tempFile = Path.Combine(Path.GetTempPath(), $"elevenlabs_{Guid.NewGuid()}{ext}");

        try
        {
            await SaveToFileAsync(text, tempFile, cancellationToken);

            if (IsPcmFormat() && _options.LeadingSilenceMs > 0)
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
        CancellationToken cancellationToken = default
    )
    {
        var apiData = await CallTtsApiAsync(text, cancellationToken);

        if (IsPcmFormat())
            return WrapPcmInWav(apiData, sampleRate: 44100);

        // MP3 and other container formats are returned ready to use
        return apiData;
    }

    public async Task SaveToFileAsync(
        string text,
        string filePath,
        CancellationToken cancellationToken = default
    )
    {
        var audioBytes = await SynthesizeToAudioAsync(text, cancellationToken);
        await File.WriteAllBytesAsync(filePath, audioBytes, cancellationToken);
    }

    public IReadOnlyList<VoiceInfo> GetAvailableVoices()
    {
        // Synchronous wrapper — calls the API to list voices
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
        CancellationToken cancellationToken = default
    )
    {
        var response = await _httpClient.GetAsync($"{BaseUrl}/v2/voices", cancellationToken);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadFromJsonAsync<VoicesResponse>(cancellationToken);
        if (json?.Voices == null)
            return [];

        return json
            .Voices.Select(v => new VoiceInfo(
                v.VoiceId,
                v.Labels?.GetValueOrDefault("accent") ?? "unknown",
                v.Labels?.GetValueOrDefault("gender") ?? "Unknown",
                $"{v.Name} — {v.Category}"
            ))
            .ToList();
    }

    public void SetVoice(string voiceName)
    {
        // Accept either a voice ID directly or a voice name
        // The user can get voice IDs from --list-voices
        _voiceId = voiceName;
    }

    public void SetRate(int rate)
    {
        // Map -10..10 to speed 0.7..1.2
        // -10 → 0.7, 0 → 1.0 (approximately), 10 → 1.2
        var normalized = (rate + 10) / 20.0f; // 0.0 to 1.0
        _speed = 0.7f + (normalized * 0.5f); // 0.7 to 1.2
    }

    public void SetVolume(int volume)
    {
        // ElevenLabs doesn't have a direct volume control.
        // Volume is best handled client-side. We store it but don't send it to the API.
    }

    // ISsmlCapable

    public bool SupportsNativeSsml => false;

    public async Task SpeakSsmlAsync(string ssml, CancellationToken cancellationToken = default)
    {
        var savedSpeed = _speed;
        var savedVoiceId = _voiceId;
        try
        {
            var result = _preprocessor.Preprocess(ssml);
            ApplyPreprocessResult(result);
            await SpeakAsync(result.PlainText, cancellationToken);
        }
        finally
        {
            _speed = savedSpeed;
            _voiceId = savedVoiceId;
        }
    }

    public async Task<byte[]> SynthesizeSsmlToAudioAsync(
        string ssml,
        CancellationToken cancellationToken = default
    )
    {
        var savedSpeed = _speed;
        var savedVoiceId = _voiceId;
        try
        {
            var result = _preprocessor.Preprocess(ssml);
            ApplyPreprocessResult(result);
            return await SynthesizeToAudioAsync(result.PlainText, cancellationToken);
        }
        finally
        {
            _speed = savedSpeed;
            _voiceId = savedVoiceId;
        }
    }

    public async Task SaveSsmlToFileAsync(
        string ssml,
        string filePath,
        CancellationToken cancellationToken = default
    )
    {
        var savedSpeed = _speed;
        var savedVoiceId = _voiceId;
        try
        {
            var result = _preprocessor.Preprocess(ssml);
            ApplyPreprocessResult(result);
            await SaveToFileAsync(result.PlainText, filePath, cancellationToken);
        }
        finally
        {
            _speed = savedSpeed;
            _voiceId = savedVoiceId;
        }
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _httpClient.Dispose();
            _disposed = true;
        }
    }

    private void ApplyPreprocessResult(SsmlPreprocessResult result)
    {
        if (result.RateMultiplier.HasValue)
        {
            // Clamp to ElevenLabs speed range 0.7..1.2
            _speed = Math.Clamp(result.RateMultiplier.Value, 0.7f, 1.2f);
        }

        if (result.VoiceName != null)
            _voiceId = result.VoiceName;
    }

    private bool IsPcmFormat() =>
        _options.OutputFormat.StartsWith("pcm_", StringComparison.OrdinalIgnoreCase);

    private string GetFormatExtension()
    {
        var format = _options.OutputFormat.ToLowerInvariant();
        if (format.StartsWith("mp3_")) return "mp3";
        if (format.StartsWith("pcm_")) return "wav";
        if (format.StartsWith("ulaw_")) return "wav";
        return "mp3"; // safe default for unknown formats
    }

    private async Task<byte[]> CallTtsApiAsync(string text, CancellationToken cancellationToken)
    {
        var url = $"{BaseUrl}/v1/text-to-speech/{_voiceId}?output_format={_options.OutputFormat}";

        var payload = new
        {
            text,
            model_id = _options.ModelId,
            voice_settings = new
            {
                stability = _options.Stability,
                similarity_boost = _options.SimilarityBoost,
                style = _options.Style,
                use_speaker_boost = true,
            },
            speed = _speed,
        };

        var response = await _httpClient.PostAsJsonAsync(url, payload, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new HttpRequestException(
                $"ElevenLabs API returned {(int)response.StatusCode}: {errorBody}"
            );
        }

        return await response.Content.ReadAsByteArrayAsync(cancellationToken);
    }

    /// <summary>
    /// Wraps raw PCM (16-bit mono) data in a WAV container.
    /// </summary>
    private static byte[] WrapPcmInWav(byte[] pcmData, int sampleRate)
    {
        var headerSize = 44;
        var dataSize = pcmData.Length;

        using var ms = new MemoryStream(headerSize + dataSize);
        using var writer = new BinaryWriter(ms);

        // RIFF header
        writer.Write("RIFF"u8);
        writer.Write(36 + dataSize);
        writer.Write("WAVE"u8);

        // fmt chunk
        writer.Write("fmt "u8);
        writer.Write(16); // chunk size
        writer.Write((short)1); // PCM format
        writer.Write((short)1); // mono
        writer.Write(sampleRate);
        writer.Write(sampleRate * 2); // byte rate (16-bit mono)
        writer.Write((short)2); // block align
        writer.Write((short)16); // bits per sample

        // data chunk
        writer.Write("data"u8);
        writer.Write(dataSize);
        writer.Write(pcmData);

        return ms.ToArray();
    }

    // JSON models for API responses

    private class VoicesResponse
    {
        [JsonPropertyName("voices")]
        public List<VoiceEntry>? Voices { get; set; }
    }

    private class VoiceEntry
    {
        [JsonPropertyName("voice_id")]
        public string VoiceId { get; set; } = "";

        [JsonPropertyName("name")]
        public string Name { get; set; } = "";

        [JsonPropertyName("category")]
        public string Category { get; set; } = "";

        [JsonPropertyName("labels")]
        public Dictionary<string, string>? Labels { get; set; }
    }
}
