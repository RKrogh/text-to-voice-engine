namespace TextToVoice.Engines.SherpaOnnx;

/// <summary>
/// Voice settings for one synthesis call: which speaker of a multi-speaker model, how fast, how
/// loud. Passing one per call lets a single engine voice several characters without changing
/// shared engine state.
/// </summary>
/// <param name="SpeakerId">Speaker index; 0 for single-speaker models.</param>
/// <param name="Speed">1.0 is normal, 2.0 twice as fast.</param>
/// <param name="Volume">Amplitude scale from 0.0 to 1.0, applied after normalisation.</param>
public sealed record SherpaOnnxVoice(int SpeakerId = 0, float Speed = 1.0f, float Volume = 1.0f);

/// <summary>Mono samples in [-1, 1] and their sample rate.</summary>
public sealed record SynthesizedAudio(float[] Samples, int SampleRate)
{
    public TimeSpan Duration => TimeSpan.FromSeconds(Samples.Length / (double)SampleRate);
}
