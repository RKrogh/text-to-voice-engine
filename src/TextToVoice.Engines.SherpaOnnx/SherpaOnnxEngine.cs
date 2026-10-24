using SherpaOnnx;
using TextToVoice.Core;

namespace TextToVoice.Engines.SherpaOnnx;

/// <summary>
/// Embedded TTS engine using sherpa-onnx for in-process ONNX inference.
/// Loads Piper VITS models (or Kokoro models when <see cref="SherpaOnnxOptions.VoicesPath"/> is
/// set) directly — no external executable needed.
/// </summary>
/// <remarks>
/// Safe to call from several threads. Each call can carry its own <see cref="SherpaOnnxVoice"/>,
/// so one multi-speaker model can voice several characters at once; calls into the model are
/// serialized. The setters change the default voice for calls that do not pass one.
/// </remarks>
public class SherpaOnnxEngine : ITtsEngine, ISsmlCapable
{
    private readonly SherpaOnnxOptions _options;
    private readonly OfflineTts _tts;
    private readonly ISsmlPreprocessor _preprocessor;
    private readonly Lock _generate = new();
    private SherpaOnnxVoice _voice;
    private bool _disposed;

    public SherpaOnnxEngine(SherpaOnnxOptions options, ISsmlPreprocessor? preprocessor = null)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));

        if (string.IsNullOrEmpty(options.ModelPath))
            throw new ArgumentException("ModelPath is required", nameof(options));

        _preprocessor = preprocessor ?? new SsmlPreprocessor();
        // speed is inverse of length scale: speed 2.0 = length_scale 0.5
        _voice = new SherpaOnnxVoice(options.SpeakerId, 1.0f / options.LengthScale);

        var modelDir = Path.GetDirectoryName(Path.GetFullPath(options.ModelPath)) ?? ".";
        var tokensPath = options.TokensPath
            ?? Path.Combine(modelDir, "tokens.txt");
        var dataDir = options.DataDir
            ?? Path.Combine(modelDir, "espeak-ng-data");

        var config = new OfflineTtsConfig();
        if (options.VoicesPath is { } voices)
        {
            config.Model.Kokoro.Model = options.ModelPath;
            config.Model.Kokoro.Voices = voices;
            config.Model.Kokoro.Tokens = tokensPath;
            config.Model.Kokoro.DataDir = dataDir;
            config.Model.Kokoro.LengthScale = options.LengthScale;
        }
        else
        {
            config.Model.Vits.Model = options.ModelPath;
            config.Model.Vits.Tokens = tokensPath;
            config.Model.Vits.DataDir = dataDir;
            config.Model.Vits.NoiseScale = options.NoiseScale;
            config.Model.Vits.NoiseScaleW = options.NoiseScaleW;
            config.Model.Vits.LengthScale = options.LengthScale;
        }

        config.Model.NumThreads = options.NumThreads;
        config.Model.Provider = "cpu";

        _tts = new OfflineTts(config);
    }

    /// <summary>The voice used by calls that do not pass one. Changed by the setters.</summary>
    public SherpaOnnxVoice DefaultVoice => _voice;

    /// <summary>Number of speakers in the loaded model; 1 for single-speaker models.</summary>
    public int SpeakerCount => _tts.NumSpeakers;

    /// <summary>
    /// Synthesizes to raw mono samples in [-1, 1], for callers that feed an audio engine directly
    /// instead of going through WAV bytes.
    /// </summary>
    public SynthesizedAudio Synthesize(string text, SherpaOnnxVoice? voice = null)
    {
        ArgumentNullException.ThrowIfNull(text);
        ObjectDisposedException.ThrowIf(_disposed, this);

        var v = voice ?? _voice;
        OfflineTtsGeneratedAudio audio;
        lock (_generate)
        {
            audio = _tts.Generate(text, v.Speed, v.SpeakerId);
        }

        return new SynthesizedAudio(Pcm.Shape(audio.Samples, _options.Normalize, v.Volume), audio.SampleRate);
    }

    public Task SpeakAsync(string text, CancellationToken cancellationToken = default) =>
        SpeakAsync(text, _voice, cancellationToken);

    public Task<byte[]> SynthesizeToAudioAsync(
        string text,
        CancellationToken cancellationToken = default
    ) => SynthesizeToAudioAsync(text, _voice, cancellationToken);

    /// <summary>Synthesizes a 16-bit mono WAV with this call's own voice settings.</summary>
    public Task<byte[]> SynthesizeToAudioAsync(
        string text,
        SherpaOnnxVoice voice,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(voice);
        return Task.Run(
            () =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                var audio = Synthesize(text, voice);
                return Pcm.ToWav(audio.Samples, audio.SampleRate);
            },
            cancellationToken
        );
    }

    public Task SaveToFileAsync(
        string text,
        string filePath,
        CancellationToken cancellationToken = default
    ) => SaveToFileAsync(text, filePath, _voice, cancellationToken);

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
            _voice = _voice with { SpeakerId = id };
            return;
        }

        // Try parsing as plain speaker ID
        if (int.TryParse(voiceName, out var speakerId))
        {
            _voice = _voice with { SpeakerId = speakerId };
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
        _voice = _voice with { Speed = 0.5f + (normalized * 1.5f) }; // 0.5 to 2.0
    }

    public void SetVolume(int volume)
    {
        // Map 0..100 to amplitude scale 0.0..1.0
        _voice = _voice with { Volume = Math.Clamp(volume / 100.0f, 0.0f, 1.0f) };
    }

    // ISsmlCapable. The prosody in the markup applies to that call only; nothing is changed on
    // the engine, so concurrent plain calls are unaffected.

    public bool SupportsNativeSsml => false;

    public Task SpeakSsmlAsync(string ssml, CancellationToken cancellationToken = default)
    {
        var (text, voice) = Preprocess(ssml);
        return SpeakAsync(text, voice, cancellationToken);
    }

    public Task<byte[]> SynthesizeSsmlToAudioAsync(
        string ssml,
        CancellationToken cancellationToken = default
    )
    {
        var (text, voice) = Preprocess(ssml);
        return SynthesizeToAudioAsync(text, voice, cancellationToken);
    }

    public Task SaveSsmlToFileAsync(
        string ssml,
        string filePath,
        CancellationToken cancellationToken = default
    )
    {
        var (text, voice) = Preprocess(ssml);
        return SaveToFileAsync(text, filePath, voice, cancellationToken);
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _tts.Dispose();
            _disposed = true;
        }
    }

    private (string Text, SherpaOnnxVoice Voice) Preprocess(string ssml)
    {
        var result = _preprocessor.Preprocess(ssml);
        var voice = _voice;

        // SSML rate multiplier: >1.0 = faster. sherpa-onnx speed: >1.0 = faster. Direct mapping.
        if (result.RateMultiplier is { } rate)
            voice = voice with { Speed = rate };

        if (result.Volume is { } volume)
            voice = voice with { Volume = Math.Clamp(volume / 100.0f, 0.0f, 1.0f) };

        return (result.PlainText, voice);
    }

    private async Task SpeakAsync(string text, SherpaOnnxVoice voice, CancellationToken cancellationToken)
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"sherpa_{Guid.NewGuid()}.wav");

        try
        {
            await SaveToFileAsync(text, tempFile, voice, cancellationToken);

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

    private async Task SaveToFileAsync(
        string text,
        string filePath,
        SherpaOnnxVoice voice,
        CancellationToken cancellationToken
    )
    {
        var audioBytes = await SynthesizeToAudioAsync(text, voice, cancellationToken);
        await File.WriteAllBytesAsync(filePath, audioBytes, cancellationToken);
    }
}
