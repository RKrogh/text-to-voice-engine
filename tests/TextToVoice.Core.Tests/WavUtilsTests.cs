namespace TextToVoice.Core.Tests;

public class WavUtilsTests : IDisposable
{
    private readonly string _tempDir;

    public WavUtilsTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"wavutils_tests_{Guid.NewGuid()}");
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, true);
    }

    [Fact]
    public void PrependSilence_ZeroMs_DoesNotModifyFile()
    {
        var wav = BuildWav(sampleCount: 100);
        var inputPath = WriteTempWav(wav);
        var outputPath = Path.Combine(_tempDir, "out.wav");

        WavUtils.PrependSilence(inputPath, outputPath, 0);

        Assert.False(File.Exists(outputPath));
    }

    [Fact]
    public void PrependSilence_NegativeMs_DoesNotModifyFile()
    {
        var wav = BuildWav(sampleCount: 100);
        var inputPath = WriteTempWav(wav);
        var outputPath = Path.Combine(_tempDir, "out.wav");

        WavUtils.PrependSilence(inputPath, outputPath, -10);

        Assert.False(File.Exists(outputPath));
    }

    [Fact]
    public void PrependSilence_ValidWav_IncreasesFileSize()
    {
        var wav = BuildWav(sampleCount: 100);
        var inputPath = WriteTempWav(wav);
        var outputPath = Path.Combine(_tempDir, "out.wav");

        WavUtils.PrependSilence(inputPath, outputPath, 100);

        Assert.True(File.Exists(outputPath));
        var outputBytes = File.ReadAllBytes(outputPath);
        Assert.True(outputBytes.Length > wav.Length);
    }

    [Fact]
    public void PrependSilence_ValidWav_OutputIsValidWav()
    {
        var wav = BuildWav(sampleCount: 100);
        var inputPath = WriteTempWav(wav);
        var outputPath = Path.Combine(_tempDir, "out.wav");

        WavUtils.PrependSilence(inputPath, outputPath, 100);

        var output = File.ReadAllBytes(outputPath);

        // Verify RIFF header
        Assert.Equal((byte)'R', output[0]);
        Assert.Equal((byte)'I', output[1]);
        Assert.Equal((byte)'F', output[2]);
        Assert.Equal((byte)'F', output[3]);

        // Verify WAVE
        Assert.Equal((byte)'W', output[8]);
        Assert.Equal((byte)'A', output[9]);
        Assert.Equal((byte)'V', output[10]);
        Assert.Equal((byte)'E', output[11]);

        // Verify RIFF size field is consistent
        var riffSize = BitConverter.ToInt32(output, 4);
        Assert.Equal(output.Length - 8, riffSize);
    }

    [Fact]
    public void PrependSilence_ValidWav_SilenceBytesAreZero()
    {
        var sampleRate = 24000;
        var silenceMs = 100;
        var expectedSilenceSamples = (int)(sampleRate * (silenceMs / 1000.0));
        var expectedSilenceBytes = expectedSilenceSamples * 2; // 16-bit mono

        var wav = BuildWav(sampleCount: 10, sampleRate: sampleRate);
        var inputPath = WriteTempWav(wav);
        var outputPath = Path.Combine(_tempDir, "out.wav");

        WavUtils.PrependSilence(inputPath, outputPath, silenceMs);

        var output = File.ReadAllBytes(outputPath);
        var dataOffset = FindDataChunkOffset(output);
        var dataStart = dataOffset + 8;

        // All silence bytes should be zero
        for (int i = dataStart; i < dataStart + expectedSilenceBytes; i++)
        {
            Assert.Equal(0, output[i]);
        }
    }

    [Fact]
    public void PrependSilence_InPlace_Works()
    {
        var wav = BuildWav(sampleCount: 100);
        var filePath = WriteTempWav(wav);
        var originalSize = wav.Length;

        WavUtils.PrependSilence(filePath, filePath, 100);

        var modified = File.ReadAllBytes(filePath);
        Assert.True(modified.Length > originalSize);
    }

    [Fact]
    public void PrependSilence_StreamingWav_UnknownSizeMarkers_HandledCorrectly()
    {
        // Simulate a Voxtral-style WAV with 0xFFFFFFFF size markers
        var audioData = new byte[200];
        for (int i = 0; i < audioData.Length; i++)
            audioData[i] = (byte)(i % 256);

        var wav = BuildStreamingWav(audioData, sampleRate: 24000);
        var inputPath = WriteTempWav(wav);
        var outputPath = Path.Combine(_tempDir, "out.wav");

        // Should not throw
        WavUtils.PrependSilence(inputPath, outputPath, 100);

        Assert.True(File.Exists(outputPath));
        var output = File.ReadAllBytes(outputPath);

        // Output should be larger than input (silence added)
        Assert.True(output.Length > wav.Length);

        // Output should have valid (non-0xFFFFFFFF) RIFF size
        var riffSize = BitConverter.ToInt32(output, 4);
        Assert.NotEqual(-1, riffSize);
        Assert.Equal(output.Length - 8, riffSize);

        // Output should have valid data chunk size
        var dataOffset = FindDataChunkOffset(output);
        var dataSize = BitConverter.ToInt32(output, dataOffset + 4);
        Assert.True(dataSize > 0);
        Assert.NotEqual(-1, dataSize);
    }

    [Fact]
    public void PrependSilence_StreamingWav_PreservesOriginalAudioData()
    {
        var audioData = new byte[] { 0x11, 0x22, 0x33, 0x44, 0x55, 0x66 };
        var wav = BuildStreamingWav(audioData, sampleRate: 24000);
        var inputPath = WriteTempWav(wav);
        var outputPath = Path.Combine(_tempDir, "out.wav");

        var silenceMs = 100;
        var expectedSilenceSamples = (int)(24000 * (silenceMs / 1000.0));
        var expectedSilenceBytes = expectedSilenceSamples * 2; // 16-bit mono

        WavUtils.PrependSilence(inputPath, outputPath, silenceMs);

        var output = File.ReadAllBytes(outputPath);
        var dataOffset = FindDataChunkOffset(output);
        var dataStart = dataOffset + 8;

        // Original audio data should appear after the silence
        var audioStart = dataStart + expectedSilenceBytes;
        for (int i = 0; i < audioData.Length; i++)
        {
            Assert.Equal(audioData[i], output[audioStart + i]);
        }
    }

    [Fact]
    public void PrependSilence_TooSmallFile_DoesNothing()
    {
        var inputPath = Path.Combine(_tempDir, "tiny.wav");
        File.WriteAllBytes(inputPath, new byte[10]);
        var outputPath = Path.Combine(_tempDir, "out.wav");

        WavUtils.PrependSilence(inputPath, outputPath, 100);

        Assert.False(File.Exists(outputPath));
    }

    [Fact]
    public void PrependSilence_NoFmtChunk_DoesNothing()
    {
        // RIFF header with WAVE but no fmt chunk
        using var ms = new MemoryStream();
        using var w = new BinaryWriter(ms);
        w.Write("RIFF"u8);
        w.Write(100);
        w.Write("WAVE"u8);
        w.Write("junk"u8); // not fmt
        w.Write(88);
        w.Write(new byte[88]);
        var wav = ms.ToArray();

        var inputPath = WriteTempWav(wav);
        var outputPath = Path.Combine(_tempDir, "out.wav");

        WavUtils.PrependSilence(inputPath, outputPath, 100);

        Assert.False(File.Exists(outputPath));
    }

    [Fact]
    public void PrependSilence_NoDataChunk_DoesNothing()
    {
        // Valid fmt chunk but no data chunk
        using var ms = new MemoryStream();
        using var w = new BinaryWriter(ms);
        w.Write("RIFF"u8);
        w.Write(36);
        w.Write("WAVE"u8);
        w.Write("fmt "u8);
        w.Write(16);
        w.Write((short)1);
        w.Write((short)1);
        w.Write(24000);
        w.Write(48000);
        w.Write((short)2);
        w.Write((short)16);
        // No data chunk
        var wav = ms.ToArray();

        var inputPath = WriteTempWav(wav);
        var outputPath = Path.Combine(_tempDir, "out.wav");

        WavUtils.PrependSilence(inputPath, outputPath, 100);

        Assert.False(File.Exists(outputPath));
    }

    // Helpers

    private string WriteTempWav(byte[] wav)
    {
        var path = Path.Combine(_tempDir, $"{Guid.NewGuid()}.wav");
        File.WriteAllBytes(path, wav);
        return path;
    }

    private static byte[] BuildWav(int sampleCount, int sampleRate = 24000)
    {
        var dataSize = sampleCount * 2; // 16-bit mono
        using var ms = new MemoryStream();
        using var w = new BinaryWriter(ms);
        w.Write("RIFF"u8);
        w.Write(36 + dataSize);
        w.Write("WAVE"u8);
        w.Write("fmt "u8);
        w.Write(16);
        w.Write((short)1);  // PCM
        w.Write((short)1);  // mono
        w.Write(sampleRate);
        w.Write(sampleRate * 2); // byte rate
        w.Write((short)2);  // block align
        w.Write((short)16); // bits per sample
        w.Write("data"u8);
        w.Write(dataSize);
        w.Write(new byte[dataSize]);
        return ms.ToArray();
    }

    /// <summary>
    /// Builds a WAV with 0xFFFFFFFF size markers, mimicking Voxtral streaming output.
    /// </summary>
    private static byte[] BuildStreamingWav(byte[] audioData, int sampleRate = 24000)
    {
        using var ms = new MemoryStream();
        using var w = new BinaryWriter(ms);
        w.Write("RIFF"u8);
        w.Write(unchecked((int)0xFFFFFFFF)); // unknown RIFF size
        w.Write("WAVE"u8);
        w.Write("fmt "u8);
        w.Write(16);
        w.Write((short)1);  // PCM
        w.Write((short)1);  // mono
        w.Write(sampleRate);
        w.Write(sampleRate * 2);
        w.Write((short)2);
        w.Write((short)16);
        w.Write("data"u8);
        w.Write(unchecked((int)0xFFFFFFFF)); // unknown data size
        w.Write(audioData);
        return ms.ToArray();
    }

    private static int FindDataChunkOffset(byte[] wav)
    {
        var id = "data"u8;
        for (int i = 12; i < wav.Length - 8; i++)
        {
            if (wav[i] == id[0] && wav[i + 1] == id[1] && wav[i + 2] == id[2] && wav[i + 3] == id[3])
                return i;
        }
        return -1;
    }
}
