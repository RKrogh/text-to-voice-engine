namespace TextToVoice.Core;

/// <summary>
/// Utilities for manipulating WAV audio data.
/// </summary>
public static class WavUtils
{
    /// <summary>
    /// Prepends silence to a WAV file by writing a new file with zero-value
    /// samples inserted at the beginning.
    /// </summary>
    /// <param name="inputPath">Path to the original WAV file.</param>
    /// <param name="outputPath">Path to write the modified WAV file (can be the same as input).</param>
    /// <param name="silenceMs">Duration of silence to prepend in milliseconds.</param>
    public static void PrependSilence(string inputPath, string outputPath, int silenceMs)
    {
        if (silenceMs <= 0)
            return;

        var inputBytes = File.ReadAllBytes(inputPath);

        // Parse WAV header to get format details
        // Standard WAV: RIFF header (12 bytes) + fmt chunk (24 bytes minimum) + data chunk
        if (inputBytes.Length < 44)
            return; // Too small to be a valid WAV

        // Find the fmt chunk to get sample rate, channels, bits per sample
        var fmtOffset = FindChunk(inputBytes, "fmt ");
        if (fmtOffset < 0)
            return;

        var channels = BitConverter.ToInt16(inputBytes, fmtOffset + 10);
        var sampleRate = BitConverter.ToInt32(inputBytes, fmtOffset + 12);
        var bitsPerSample = BitConverter.ToInt16(inputBytes, fmtOffset + 22);

        var bytesPerSample = bitsPerSample / 8;
        var silenceSamples = (int)(sampleRate * (silenceMs / 1000.0));
        var silenceBytes = silenceSamples * channels * bytesPerSample;

        // Find the data chunk
        var dataOffset = FindChunk(inputBytes, "data");
        if (dataOffset < 0)
            return;

        var originalDataSize = BitConverter.ToInt32(inputBytes, dataOffset + 4);
        var dataStart = dataOffset + 8;
        var newDataSize = originalDataSize + silenceBytes;

        using var output = File.Create(outputPath);
        using var writer = new BinaryWriter(output);

        // Write RIFF header with updated size
        writer.Write(inputBytes, 0, 4); // "RIFF"
        writer.Write(BitConverter.ToInt32(inputBytes, 4) + silenceBytes); // updated file size
        writer.Write(inputBytes, 8, dataOffset - 8); // everything from "WAVE" up to data chunk ID

        // Write data chunk header with updated size
        writer.Write(inputBytes, dataOffset, 4); // "data"
        writer.Write(newDataSize);

        // Write silence
        writer.Write(new byte[silenceBytes]);

        // Write original audio data
        writer.Write(inputBytes, dataStart, originalDataSize);
    }

    private static int FindChunk(byte[] wav, string chunkId)
    {
        var id = System.Text.Encoding.ASCII.GetBytes(chunkId);
        for (int i = 12; i < wav.Length - 8; i++)
        {
            if (
                wav[i] == id[0]
                && wav[i + 1] == id[1]
                && wav[i + 2] == id[2]
                && wav[i + 3] == id[3]
            )
                return i;
        }
        return -1;
    }
}
