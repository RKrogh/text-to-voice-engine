using SherpaOnnx;
using TextToVoice.Core;

namespace TextToVoice.Engines.SherpaOnnx;

/// <summary>
/// Embedded TTS engine using sherpa-onnx for in-process ONNX inference.
/// Loads Piper VITS models directly — no external executable needed.
/// </summary>
public class SherpaOnnxEngine : ITtsEngine, ISsmlCapable
{
    private readonly SherpaOnnxOptions _options;
    private readonly OfflineTts _tts;
    private readonly ISsmlPreprocessor _preprocessor;
    private int _speakerId;
    private float _speed;
    private float _volumeScale;
    private bool _disposed;

    public SherpaOnnxEngine(SherpaOnnxOptions options, ISsmlPreprocessor? preprocessor = null)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));

        if (string.IsNullOrEmpty(options.ModelPath))
            throw new ArgumentException("ModelPath is required", nameof(options));

        _preprocessor = preprocessor ?? new SsmlPreprocessor();
        _speakerId = options.SpeakerId;
        // speed is inverse of length scale: speed 2.0 = length_scale 0.5
        _speed = 1.0f / options.LengthScale;
        _volumeScale = 1.0f;

        var modelDir = Path.GetDirectoryName(Path.GetFullPath(options.ModelPath)) ?? ".";
        var tokensPath = options.TokensPath
            ?? Path.Combine(modelDir, "tokens.txt");
        var dataDir = options.DataDir
            ?? Path.Combine(modelDir, "espeak-ng-data");

        var config = new OfflineTtsConfig();
        config.Model.Vits.Model = options.ModelPath;
        config.Model.Vits.Tokens = tokensPath;
        config.Model.Vits.DataDir = dataDir;
        config.Model.Vits.NoiseScale = options.NoiseScale;
        config.Model.Vits.NoiseScaleW = options.NoiseScaleW;
        config.Model.Vits.LengthScale = options.LengthScale;
        config.Model.NumThreads = options.NumThreads;
        config.Model.Provider = "cpu";

        _tts = new OfflineTts(config);
    }

    public async Task SpeakAsync(string text, CancellationToken cancellationToken = default)
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"sherpa_{Guid.NewGuid()}.wav");

        try
        {
            await SaveToFileAsync(text, tempFile, cancellationToken);

            if (_options.LeadingSilenceMs > 0)
                WavUtils.PrependSilence(tempFile, tempFile, _options.LeadingSilenceMs);

            await AudioPlayer.PlayAsync(tempFile, cancellationToken);
        }
        finally
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }

    public Task<byte[]> SynthesizeToAudioAsync(
        string text,
        CancellationToken cancellationToken = default
    )
    {
        return Task.Run(
            () =>
            {
                cancellationToken.ThrowIfCancellationRequested();

                var audio = _tts.Generate(text, _speed, _speakerId);
                return BuildWav(audio.Samples, audio.SampleRate);
            },
            cancellationToken
        );
    }

    public Task SaveToFileAsync(
        string text,
        string filePath,
        CancellationToken cancellationToken = default
    )
    {
        return Task.Run(
            () =>
            {
                cancellationToken.ThrowIfCancellationRequested();

                var audio = _tts.Generate(text, _speed, _speakerId);

                if (Math.Abs(_volumeScale - 1.0f) > 0.001f)
                {
                    var samples = audio.Samples;
                    for (int i = 0; i < samples.Length; i++)
                        samples[i] *= _volumeScale;
                }

                audio.SaveToWaveFile(filePath);
            },
            cancellationToken
        );
    }

    public IReadOnlyList<VoiceInfo> GetAvailableVoices()
    {
        var modelName = Path.GetFileNameWithoutExtension(_options.ModelPath);
        var numSpeakers = _tts.NumSpeakers;

        if (numSpeakers <= 1)
        {
            return new List<VoiceInfo>
            {
                new(modelName, "en-US", "Unknown", $"sherpa-onnx model: {_options.ModelPath}"),
            };
        }

        var voices = new List<VoiceInfo>();
        for (int i = 0; i < numSpeakers; i++)
        {
            voices.Add(
                new($"{modelName}#{i}", "en-US", "Unknown", $"Speaker {i} of {modelName}")
            );
        }
        return voices;
    }

    public void SetVoice(string voiceName)
    {
        // Support "modelname#speakerid" format
        var hashIndex = voiceName.LastIndexOf('#');
        if (hashIndex >= 0 && int.TryParse(voiceName[(hashIndex + 1)..], out var id))
        {
            _speakerId = id;
            return;
        }

        // Try parsing as plain speaker ID
        if (int.TryParse(voiceName, out var speakerId))
        {
            _speakerId = speakerId;
            return;
        }

        throw new NotSupportedException(
            "SherpaOnnx voice is configured via ModelPath. "
                + "Use a speaker ID or 'modelname#id' for multi-speaker models."
        );
    }

    public void SetRate(int rate)
    {
        // Map -10..10 to speed (0.5..2.0, where 1.0 is normal)
        var normalized = (rate + 10) / 20.0f; // 0.0 to 1.0
        _speed = 0.5f + (normalized * 1.5f); // 0.5 to 2.0
    }

    public void SetVolume(int volume)
    {
        // Map 0..100 to amplitude scale 0.0..1.0
        _volumeScale = Math.Clamp(volume / 100.0f, 0.0f, 1.0f);
    }

    // ISsmlCapable

    public bool SupportsNativeSsml => false;

    public async Task SpeakSsmlAsync(string ssml, CancellationToken cancellationToken = default)
    {
        var savedSpeed = _speed;
        var savedVolume = _volumeScale;
        try
        {
            var result = _preprocessor.Preprocess(ssml);
            ApplyPreprocessResult(result);
            await SpeakAsync(result.PlainText, cancellationToken);
        }
        finally
        {
            _speed = savedSpeed;
            _volumeScale = savedVolume;
        }
    }

    public async Task<byte[]> SynthesizeSsmlToAudioAsync(
        string ssml,
        CancellationToken cancellationToken = default
    )
    {
        var savedSpeed = _speed;
        var savedVolume = _volumeScale;
        try
        {
            var result = _preprocessor.Preprocess(ssml);
            ApplyPreprocessResult(result);
            return await SynthesizeToAudioAsync(result.PlainText, cancellationToken);
        }
        finally
        {
            _speed = savedSpeed;
            _volumeScale = savedVolume;
        }
    }

    public async Task SaveSsmlToFileAsync(
        string ssml,
        string filePath,
        CancellationToken cancellationToken = default
    )
    {
        var savedSpeed = _speed;
        var savedVolume = _volumeScale;
        try
        {
            var result = _preprocessor.Preprocess(ssml);
            ApplyPreprocessResult(result);
            await SaveToFileAsync(result.PlainText, filePath, cancellationToken);
        }
        finally
        {
            _speed = savedSpeed;
            _volumeScale = savedVolume;
        }
    }

    private void ApplyPreprocessResult(SsmlPreprocessResult result)
    {
        if (result.RateMultiplier.HasValue)
        {
            // SSML rate multiplier: >1.0 = faster. sherpa-onnx speed: >1.0 = faster. Direct mapping.
            _speed = result.RateMultiplier.Value;
        }

        if (result.Volume.HasValue)
        {
            _volumeScale = Math.Clamp(result.Volume.Value / 100.0f, 0.0f, 1.0f);
        }
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _tts.Dispose();
            _disposed = true;
        }
    }

    private byte[] BuildWav(float[] samples, int sampleRate)
    {
        // Apply volume scaling
        if (Math.Abs(_volumeScale - 1.0f) > 0.001f)
        {
            for (int i = 0; i < samples.Length; i++)
                samples[i] *= _volumeScale;
        }

        // Convert float samples to 16-bit PCM WAV
        var numSamples = samples.Length;
        var bytesPerSample = 2; // 16-bit
        var dataSize = numSamples * bytesPerSample;
        var headerSize = 44;

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
        writer.Write(sampleRate * bytesPerSample); // byte rate
        writer.Write((short)bytesPerSample); // block align
        writer.Write((short)16); // bits per sample

        // data chunk
        writer.Write("data"u8);
        writer.Write(dataSize);

        // Normalize and write samples
        float maxAbs = 0;
        for (int i = 0; i < numSamples; i++)
        {
            var abs = Math.Abs(samples[i]);
            if (abs > maxAbs) maxAbs = abs;
        }

        var scale = maxAbs > 0 ? 32767.0f / maxAbs : 1.0f;
        for (int i = 0; i < numSamples; i++)
        {
            var sample = (short)Math.Clamp(samples[i] * scale, short.MinValue, short.MaxValue);
            writer.Write(sample);
        }

        return ms.ToArray();
    }

}
