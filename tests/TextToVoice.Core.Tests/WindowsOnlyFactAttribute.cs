using System.Runtime.InteropServices;

namespace TextToVoice.Core.Tests;

public sealed class WindowsOnlyFactAttribute : FactAttribute
{
    public WindowsOnlyFactAttribute()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows) || !IsSpeechAvailable())
        {
            Skip = "This test requires Windows with Speech API available.";
        }
    }

    private static bool IsSpeechAvailable()
    {
        try
        {
            using var synth = new System.Speech.Synthesis.SpeechSynthesizer();
            return true;
        }
        catch
        {
            return false;
        }
    }
}

public sealed class WindowsOnlyTheoryAttribute : TheoryAttribute
{
    public WindowsOnlyTheoryAttribute()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows) || !IsSpeechAvailable())
        {
            Skip = "This test requires Windows with Speech API available.";
        }
    }

    private static bool IsSpeechAvailable()
    {
        try
        {
            using var synth = new System.Speech.Synthesis.SpeechSynthesizer();
            return true;
        }
        catch
        {
            return false;
        }
    }
}
