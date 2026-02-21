namespace TextToVoice.Core;

/// <summary>
/// Metadata about an available TTS voice.
/// </summary>
/// <param name="Name">The voice identifier used with SetVoice().</param>
/// <param name="Culture">Language/culture code (e.g., "en-US").</param>
/// <param name="Gender">Voice gender (e.g., "Male", "Female").</param>
/// <param name="Description">Human-readable description of the voice.</param>
public record VoiceInfo(string Name, string Culture, string Gender, string Description);
