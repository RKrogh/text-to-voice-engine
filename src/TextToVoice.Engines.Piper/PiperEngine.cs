using System.Diagnostics;
using TextToVoice.Core;

namespace TextToVoice.Engines.Piper;

/// <summary>
/// Cross-platform TTS engine using Piper (https://github.com/rhasspy/piper).
/// Requires Piper executable and voice model to be installed.
/// </summary>
public class PiperEngine : ITtsEngine
{
    private readonly PiperOptions _options;
    private readonly string _executable;
    private bool _disposed;

    public PiperEngine(PiperOptions options)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));

        if (string.IsNullOrEmpty(options.ModelPath))
            throw new ArgumentException("ModelPath is required", nameof(options));

        _executable = options.ExecutablePath ?? "piper";
    }

    public async Task SpeakAsync(string text, CancellationToken cancellationToken = default)
    {
        // Piper doesn't support direct playback, so synthesize to temp file and play
        var tempFile = Path.Combine(Path.GetTempPath(), $"piper_{Guid.NewGuid()}.wav");

        try
        {
            await SaveToFileAsync(text, tempFile, cancellationToken);
            await PlayAudioFileAsync(tempFile, cancellationToken);
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
        var tempFile = Path.Combine(Path.GetTempPath(), $"piper_{Guid.NewGuid()}.wav");

        try
        {
            await SaveToFileAsync(text, tempFile, cancellationToken);
            return await File.ReadAllBytesAsync(tempFile, cancellationToken);
        }
        finally
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }

    public async Task SaveToFileAsync(
        string text,
        string filePath,
        CancellationToken cancellationToken = default
    )
    {
        var args = BuildArguments(filePath);

        using var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = _executable,
                Arguments = args,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            },
        };

        process.Start();

        // Write text to stdin
        await process.StandardInput.WriteAsync(text);
        process.StandardInput.Close();

        // Wait for completion
        await process.WaitForExitAsync(cancellationToken);

        if (process.ExitCode != 0)
        {
            var error = await process.StandardError.ReadToEndAsync(cancellationToken);
            throw new InvalidOperationException(
                $"Piper failed with exit code {process.ExitCode}: {error}"
            );
        }
    }

    public IReadOnlyList<VoiceInfo> GetAvailableVoices()
    {
        // Piper uses model files as "voices" - return the configured model
        var modelName = Path.GetFileNameWithoutExtension(_options.ModelPath);
        return new List<VoiceInfo>
        {
            new VoiceInfo(modelName, "en-US", "Unknown", $"Piper model: {_options.ModelPath}"),
        };
    }

    public void SetVoice(string voiceName)
    {
        // Piper voice is set via model file, not runtime switchable
        // Would need to create new engine with different model
        throw new NotSupportedException(
            "Piper voice is configured via ModelPath. Create a new engine instance with different options."
        );
    }

    public void SetRate(int rate)
    {
        // Map -10..10 to length scale (0.5..2.0, where 1.0 is normal)
        // Negative rate = slower = higher length scale
        // Positive rate = faster = lower length scale
        var normalized = (rate + 10) / 20.0f; // 0.0 to 1.0
        _options.LengthScale = 2.0f - (normalized * 1.5f); // 2.0 to 0.5
    }

    public void SetVolume(int volume)
    {
        // Piper doesn't support volume control directly
        // Would need post-processing or system volume
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _disposed = true;
        }
    }

    private string BuildArguments(string outputFile)
    {
        var args = new List<string>
        {
            "--model",
            Quote(_options.ModelPath),
            "--output_file",
            Quote(outputFile),
            "--length_scale",
            _options.LengthScale.ToString("F2"),
            "--noise_scale",
            _options.NoiseScale.ToString("F2"),
            "--noise_w",
            _options.NoiseWidth.ToString("F2"),
        };

        if (!string.IsNullOrEmpty(_options.ConfigPath))
        {
            args.Add("--config");
            args.Add(Quote(_options.ConfigPath));
        }

        if (_options.SpeakerId > 0)
        {
            args.Add("--speaker");
            args.Add(_options.SpeakerId.ToString());
        }

        return string.Join(" ", args);
    }

    private static string Quote(string path) => path.Contains(' ') ? $"\"{path}\"" : path;

    private static async Task PlayAudioFileAsync(
        string filePath,
        CancellationToken cancellationToken
    )
    {
        // Platform-specific audio playback
        string player;
        string args;

        if (OperatingSystem.IsWindows())
        {
            // Use PowerShell to play audio on Windows
            player = "powershell";
            args = $"-c \"(New-Object Media.SoundPlayer '{filePath}').PlaySync()\"";
        }
        else if (OperatingSystem.IsLinux())
        {
            player = "aplay";
            args = Quote(filePath);
        }
        else if (OperatingSystem.IsMacOS())
        {
            player = "afplay";
            args = Quote(filePath);
        }
        else
        {
            throw new PlatformNotSupportedException(
                "Audio playback not supported on this platform"
            );
        }

        using var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = player,
                Arguments = args,
                UseShellExecute = false,
                CreateNoWindow = true,
            },
        };

        process.Start();
        await process.WaitForExitAsync(cancellationToken);
    }
}
