# TextToVoice.Engines.ElevenLabs

High-quality cloud-based TTS engine using the [ElevenLabs](https://elevenlabs.io) REST API. No external NuGet dependencies.

## Installation

```
dotnet add package TextToVoice.Engines.ElevenLabs
```

Requires an API key from [elevenlabs.io](https://elevenlabs.io). Free tier: 10,000 characters/month.

## Quick Start

```csharp
using TextToVoice.Engines.ElevenLabs;

var options = new ElevenLabsOptions
{
    ApiKey = "sk_your_key_here",
};

await using var engine = new ElevenLabsEngine(options);

await engine.SpeakAsync("Hello world");
await engine.SaveToFileAsync("Hello world", "output.wav");

// List available voices (calls the API)
var voices = await engine.GetAvailableVoicesAsync();
engine.SetVoice("EXAVITQu4vr4xnSDxMaL"); // voice ID from the list
```

## Configuration

```csharp
var options = new ElevenLabsOptions
{
    ApiKey = "sk_...",                          // required
    VoiceId = "21m00Tcm4TlvDq8ikWAM",         // default: Rachel
    ModelId = "eleven_multilingual_v2",         // default model
    OutputFormat = "mp3_44100_128",             // or "pcm_44100" (Pro tier)
    Stability = 0.5f,                           // 0.0-1.0
    SimilarityBoost = 0.75f,                    // 0.0-1.0
    Speed = 1.0f,                               // 0.7-1.2
    LeadingSilenceMs = 150,                     // silence before playback
};
```

## Pricing

| Plan | Characters/month | Price |
|------|----------------:|------:|
| Free | 10,000 | $0 |
| Starter | 30,000 | $5/month |
| Creator | 100,000 | $22/month |

See [elevenlabs.io/pricing](https://elevenlabs.io/pricing) for current pricing.

## Requirements

- .NET 10+
- ElevenLabs API key

## License

[MIT](https://github.com/RKrogh/text-to-voice-engine/blob/main/LICENSE)
