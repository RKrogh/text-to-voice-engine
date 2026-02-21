using System.Runtime.Versioning;
using System.Speech.Synthesis;
using TextToVoice.Core;

namespace TextToVoice.Engines.Windows;

/// <summary>
/// Windows TTS engine using the built-in System.Speech (SAPI) synthesizer.
/// </summary>
[SupportedOSPlatform("windows")]
public class SystemSpeechEngine : ITtsEngine
{
    private readonly SpeechSynthesizer _synthesizer;
    private bool _disposed;

    public SystemSpeechEngine()
    {
        _synthesizer = new SpeechSynthesizer();
    }

    public SystemSpeechEngine(TtsOptions options)
        : this()
    {
        if (!string.IsNullOrEmpty(options.VoiceName))
        {
            SetVoice(options.VoiceName);
        }
        SetRate(options.Rate);
        SetVolume(options.Volume);
    }

    public Task SpeakAsync(string text, CancellationToken cancellationToken = default)
    {
        return Task.Run(
            () =>
            {
                var prompt = _synthesizer.SpeakAsync(text);

                while (!prompt.IsCompleted)
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        _synthesizer.SpeakAsyncCancelAll();
                        cancellationToken.ThrowIfCancellationRequested();
                    }
                    Thread.Sleep(50);
                }
            },
            cancellationToken
        );
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

                using var stream = new MemoryStream();
                _synthesizer.SetOutputToWaveStream(stream);
                _synthesizer.Speak(text);
                _synthesizer.SetOutputToDefaultAudioDevice();

                return stream.ToArray();
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

                _synthesizer.SetOutputToWaveFile(filePath);
                _synthesizer.Speak(text);
                _synthesizer.SetOutputToDefaultAudioDevice();
            },
            cancellationToken
        );
    }

    public IReadOnlyList<Core.VoiceInfo> GetAvailableVoices()
    {
        return _synthesizer
            .GetInstalledVoices()
            .Where(v => v.Enabled)
            .Select(v => new Core.VoiceInfo(
                v.VoiceInfo.Name,
                v.VoiceInfo.Culture.Name,
                v.VoiceInfo.Gender.ToString(),
                v.VoiceInfo.Description
            ))
            .ToList();
    }

    public void SetVoice(string voiceName)
    {
        _synthesizer.SelectVoice(voiceName);
    }

    public void SetRate(int rate)
    {
        _synthesizer.Rate = Math.Clamp(rate, -10, 10);
    }

    public void SetVolume(int volume)
    {
        _synthesizer.Volume = Math.Clamp(volume, 0, 100);
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _synthesizer.Dispose();
            _disposed = true;
        }
    }
}
