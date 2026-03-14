using System.Diagnostics;

namespace TextToVoice.Core;

/// <summary>
/// Platform-specific audio file playback.
/// Uses argument lists (not shell interpolation) to prevent command injection.
/// </summary>
public static class AudioPlayer
{
    public static async Task PlayAsync(string filePath, CancellationToken cancellationToken = default)
    {
        var psi = new ProcessStartInfo
        {
            UseShellExecute = false,
            CreateNoWindow = true,
        };

        if (OperatingSystem.IsWindows())
        {
            // Use ArgumentList to avoid shell injection via filePath.
            psi.FileName = "powershell";
            psi.ArgumentList.Add("-NoProfile");
            psi.ArgumentList.Add("-Command");
            psi.ArgumentList.Add($"(New-Object Media.SoundPlayer '{filePath.Replace("'", "''")}').PlaySync()");
        }
        else if (OperatingSystem.IsLinux())
        {
            psi.FileName = "aplay";
            psi.ArgumentList.Add(filePath);
        }
        else if (OperatingSystem.IsMacOS())
        {
            psi.FileName = "afplay";
            psi.ArgumentList.Add(filePath);
        }
        else
        {
            throw new PlatformNotSupportedException(
                "Audio playback not supported on this platform"
            );
        }

        using var process = new Process { StartInfo = psi };

        process.Start();
        await process.WaitForExitAsync(cancellationToken);
    }
}
