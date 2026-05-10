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
            if (IsWsl())
            {
                // WSL: convert path and play via Windows PowerShell
                var winPath = ToWindowsPath(filePath);
                psi.FileName = "powershell.exe";
                psi.ArgumentList.Add("-NoProfile");
                psi.ArgumentList.Add("-Command");
                psi.ArgumentList.Add($"(New-Object Media.SoundPlayer '{winPath.Replace("'", "''")}').PlaySync()");
            }
            else
            {
                psi.FileName = "aplay";
                psi.ArgumentList.Add(filePath);
            }
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

    private static bool IsWsl()
    {
        try
        {
            var release = File.ReadAllText("/proc/version");
            return release.Contains("microsoft", StringComparison.OrdinalIgnoreCase)
                || release.Contains("WSL", StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }

    private static string ToWindowsPath(string linuxPath)
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "wslpath",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                CreateNoWindow = true,
            };
            psi.ArgumentList.Add("-w");
            psi.ArgumentList.Add(linuxPath);

            using var process = Process.Start(psi);
            if (process == null) return linuxPath;

            var result = process.StandardOutput.ReadToEnd().Trim();
            process.WaitForExit();
            return string.IsNullOrEmpty(result) ? linuxPath : result;
        }
        catch
        {
            return linuxPath;
        }
    }
}
