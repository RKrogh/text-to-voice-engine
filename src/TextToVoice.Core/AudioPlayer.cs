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
            var escaped = filePath.Replace("'", "''");
            psi.FileName = "powershell";
            psi.ArgumentList.Add("-NoProfile");
            psi.ArgumentList.Add("-Command");
            psi.ArgumentList.Add($"(New-Object System.Media.SoundPlayer '{escaped}').PlaySync()");
        }
        else if (OperatingSystem.IsLinux())
        {
            if (IsWsl())
            {
                // WSL: convert path and play via Windows MediaPlayer (PresentationCore).
                // Media.SoundPlayer is too restrictive on WAV formats.
                var winPath = ToWindowsPath(filePath);
                var escaped = winPath.Replace("'", "''");
                psi.FileName = "powershell.exe";
                psi.ArgumentList.Add("-NoProfile");
                psi.ArgumentList.Add("-Command");
                psi.ArgumentList.Add(
                    "Add-Type -AssemblyName PresentationCore; " +
                    "$p = New-Object System.Windows.Media.MediaPlayer; " +
                    $"$p.Open([uri]'{escaped}'); " +
                    "Start-Sleep -Milliseconds 500; " +
                    "$p.Play(); " +
                    "while ($p.Position -lt $p.NaturalDuration.TimeSpan) { Start-Sleep -Milliseconds 200 }; " +
                    "$p.Close()");
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
