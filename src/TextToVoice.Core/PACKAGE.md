# TextToVoice.Core

Interface-based TTS abstraction for .NET. Provides `ITtsEngine`, SSML detection/preprocessing, and audio utilities — with zero dependencies.

## Installation

```
dotnet add package TextToVoice.Core
```

## Quick Start

`TextToVoice.Core` defines the interfaces and shared types. Install an engine package to get actual TTS functionality:

| Package | Engine | Platform |
|---------|--------|----------|
| `TextToVoice.Engines.Windows` | System.Speech (SAPI) | Windows |
| `TextToVoice.Engines.Piper` | Piper neural TTS | Cross-platform |
| `TextToVoice.Engines.SherpaOnnx` | Embedded ONNX inference | Cross-platform |
| `TextToVoice.Engines.ElevenLabs` | ElevenLabs cloud API | Any (requires API key) |

## Core Interfaces

```csharp
// ITtsEngine — main abstraction
using TextToVoice.Core;

ITtsEngine engine = /* create from an engine package */;

await engine.SpeakAsync("Hello world");
byte[] wav = await engine.SynthesizeToAudioAsync("Hello");
await engine.SaveToFileAsync("Hello", "output.wav");

engine.SetRate(3);     // -10 (slowest) to 10 (fastest)
engine.SetVolume(80);  // 0 (silent) to 100 (loudest)
```

```csharp
// ISsmlCapable — SSML support (optional per engine)
if (engine is ISsmlCapable ssml)
{
    await ssml.SpeakSsmlAsync("<speak>Hello <break time='500ms'/> world</speak>");
}
```

```csharp
// SSML detection and preprocessing
bool isSsml = SsmlDetector.IsSsml("<speak>Hello</speak>"); // true
string wrapped = SsmlDetector.WrapInSsml("Hello");          // adds <speak> tags

var preprocessor = new SsmlPreprocessor();
var result = preprocessor.Preprocess("<speak><prosody rate='fast'>Hello</prosody></speak>");
// result.PlainText = "Hello", result.RateMultiplier = 1.5
```

## License

[MIT](https://github.com/RKrogh/text-to-voice-engine/blob/main/LICENSE)
