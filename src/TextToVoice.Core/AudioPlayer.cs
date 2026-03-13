using System.Diagnostics;

namespace TextToVoice.Core;

/// <summary>
/// Platform-specific audio file playback.
/// </summary>
public static class AudioPlayer
{
    public static async Task PlayAsync(string filePath, CancellationToken cancellationToken = default)
    {
        string player;
        string args;

        if (OperatingSystem.IsWindows())
        {
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

    private static string Quote(string path) => path.Contains(' ') ? $"\"{path}\"" : path;
}
