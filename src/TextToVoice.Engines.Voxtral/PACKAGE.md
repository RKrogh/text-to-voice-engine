# TextToVoice.Engines.Voxtral

Cloud-based TTS engine using the [Mistral AI](https://mistral.ai) Voxtral API. Supports streaming SSE and zero-shot voice cloning via reference audio.

## Installation

```
dotnet add package TextToVoice.Engines.Voxtral
```

Requires an API key from [console.mistral.ai](https://console.mistral.ai).

## Quick Start

```csharp
using TextToVoice.Engines.Voxtral;

var options = new VoxtralOptions
{
    ApiKey = "your_api_key",
};

await using var engine = new VoxtralEngine(options);

await engine.SpeakAsync("Hello world");
await engine.SaveToFileAsync("Hello world", "output.wav");

// List available voices
var voices = await engine.GetAvailableVoicesAsync();
engine.SetVoice("en_paul_neutral");
```

## Configuration

```csharp
var options = new VoxtralOptions
{
    ApiKey = "...",                              // required
    VoiceId = "gb_jane_neutral",                // default voice
    ModelId = "voxtral-mini-tts-2603",          // default model
    ResponseFormat = "wav",                      // output format
    Stream = false,                              // SSE streaming
    RefAudioPath = "/path/to/reference.mp3",    // voice cloning (optional)
    LeadingSilenceMs = 150,                      // silence before playback
};
```

## Voice Cloning

Provide a 2-3 second reference audio sample for zero-shot voice cloning:

```csharp
var options = new VoxtralOptions
{
    ApiKey = "...",
    RefAudioPath = "/path/to/sample.mp3",
};
```

When `RefAudioPath` is set, the engine sends the audio as a base64-encoded `ref_audio` field instead of `voice_id`.

## Requirements

- .NET 10+
- Mistral AI API key

## License

[MIT](https://github.com/RKrogh/text-to-voice-engine/blob/main/LICENSE)
