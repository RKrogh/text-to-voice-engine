namespace TextToVoice.Engines.SherpaOnnx;

/// <summary>Sample shaping and WAV encoding for the engine's output.</summary>
internal static class Pcm
{
    /// <summary>
    /// Optionally peak-normalises, then applies volume. Volume comes last so normalisation
    /// cannot undo it. Works in place on the model's buffer, which nothing else holds.
    /// </summary>
    public static float[] Shape(float[] samples, bool normalize, float volume)
    {
        var gain = volume;
        if (normalize)
        {
            float peak = 0;
            foreach (var s in samples)
                peak = Math.Max(peak, Math.Abs(s));

            if (peak > 0)
                gain *= 1.0f / peak;
        }

        if (Math.Abs(gain - 1.0f) > 1e-6f)
        {
            for (var i = 0; i < samples.Length; i++)
                samples[i] *= gain;
        }

        return samples;
    }

    /// <summary>16-bit PCM mono WAV. Samples outside [-1, 1] are clipped.</summary>
    public static byte[] ToWav(float[] samples, int sampleRate)
    {
        const int bytesPerSample = 2;
        var dataSize = samples.Length * bytesPerSample;

        using var ms = new MemoryStream(44 + dataSize);
        using var writer = new BinaryWriter(ms);

        writer.Write("RIFF"u8);
        writer.Write(36 + dataSize);
        writer.Write("WAVE"u8);

        writer.Write("fmt "u8);
        writer.Write(16); // chunk size
        writer.Write((short)1); // PCM format
        writer.Write((short)1); // mono
        writer.Write(sampleRate);
        writer.Write(sampleRate * bytesPerSample); // byte rate
        writer.Write((short)bytesPerSample); // block align
        writer.Write((short)16); // bits per sample

        writer.Write("data"u8);
        writer.Write(dataSize);
        foreach (var s in samples)
            writer.Write((short)Math.Clamp(s * 32767.0f, short.MinValue, short.MaxValue));

        return ms.ToArray();
    }
}
