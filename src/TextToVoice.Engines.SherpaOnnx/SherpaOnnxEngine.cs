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
/// serialized, and disposal waits for a running call to finish. The setters change the default
/// voice for calls that do not pass one.
/// </remarks>
public class SherpaOnnxEngine : ITtsEngine, ISsmlCapable
{
    private readonly SherpaOnnxOptions _options;
    private readonly OfflineTts _tts;
    private readonly ISsmlPreprocessor _preprocessor;
    private readonly Lock _generate = new();
    private readonly Lock _settings = new();
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
    public int SpeakerCount
    {
        get
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            return _tts.NumSpeakers;
        }
    }

    /// <summary>
    /// Synthesizes to raw mono samples in [-1, 1], for callers that feed an audio engine directly
    /// instead of going through WAV bytes.
    /// </summary>
    public SynthesizedAudio Synthesize(string text, SherpaOnnxVoice? voice = null)
    {
        ArgumentNullException.ThrowIfNull(text);
        ObjectDisposedException.ThrowIf(_disposed, this);

        var v = voice ?? _voice;
        float[] samples;
        int sampleRate;
        lock (_generate)
        {
            // Checked again under the lock: Dispose takes it too, so the model cannot be freed mid-call.
            ObjectDisposedException.ThrowIf(_disposed, this);
            var audio = _tts.Generate(text, v.Speed, v.SpeakerId);
            samples = audio.Samples; // a fresh managed copy on every read
            sampleRate = audio.SampleRate;
        }

        return new SynthesizedAudio(Pcm.Shape(samples, _options.Normalize, v.Volume), sampleRate);
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

    /// <summary>Writes a WAV file with this call's own voice settings.</summary>
    public async Task SaveToFileAsync(
        string text,
        string filePath,
        SherpaOnnxVoice voice,
        CancellationToken cancellationToken = default
    )
    {
        var audioBytes = await SynthesizeToAudioAsync(text, voice, cancellationToken);
        await File.WriteAllBytesAsync(filePath, audioBytes, cancellationToken);
    }

    public IReadOnlyList<VoiceInfo> GetAvailableVoices()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
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
            Update(v => v with { SpeakerId = id });
            return;
        }

        // Try parsing as plain speaker ID
        if (int.TryParse(voiceName, out var speakerId))
        {
            Update(v => v with { SpeakerId = speakerId });
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
        Update(v => v with { Speed = 0.5f + (normalized * 1.5f) }); // 0.5 to 2.0
    }

    public void SetVolume(int volume)
    {
        // Map 0..100 to amplitude scale 0.0..1.0
        Update(v => v with { Volume = Math.Clamp(volume / 100.0f, 0.0f, 1.0f) });
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
        lock (_generate)
        {
            if (_disposed)
                return;

            _disposed = true;
            _tts.Dispose();
        }
    }

    // Readers take the current value without locking; the lock only keeps two setters from
    // overwriting each other's change.
    private void Update(Func<SherpaOnnxVoice, SherpaOnnxVoice> change)
    {
        lock (_settings)
        {
            _voice = change(_voice);
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

    /// <summary>Plays the text through the platform audio player with this call's own voice.</summary>
    public async Task SpeakAsync(string text, SherpaOnnxVoice voice, CancellationToken cancellationToken = default)
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
}
