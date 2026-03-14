# TextToVoice.Engines.Piper

Cross-platform offline TTS engine using [Piper](https://github.com/rhasspy/piper), a fast local neural text-to-speech system.

## Installation

```
dotnet add package TextToVoice.Engines.Piper
```

Requires the Piper executable and a voice model — see [Piper releases](https://github.com/rhasspy/piper/releases) and [voice models](https://huggingface.co/rhasspy/piper-voices).

## Quick Start

```csharp
using TextToVoice.Engines.Piper;

var options = new PiperOptions
{
    ModelPath = "/path/to/en_US-lessac-medium.onnx",
    ExecutablePath = "/path/to/piper",  // or null if piper is in PATH
};

using var engine = new PiperEngine(options);

await engine.SpeakAsync("Hello world");
await engine.SaveToFileAsync("Hello world", "output.wav");
byte[] wav = await engine.SynthesizeToAudioAsync("Hello");
```

## Configuration

```csharp
var options = new PiperOptions
{
    ModelPath = "/path/to/voice.onnx",  // required
    ExecutablePath = null,               // defaults to "piper" in PATH
    LengthScale = 1.0f,                 // speech speed (lower = faster)
    NoiseScale = 0.667f,                // voice variation
    NoiseWidth = 0.8f,                  // phoneme duration variation
    SpeakerId = 0,                      // for multi-speaker models
    LeadingSilenceMs = 150,             // silence before playback (0 to disable)
};
```

## Requirements

- .NET 10+
- Piper executable + ONNX voice model

## License

[MIT](https://github.com/RKrogh/text-to-voice-engine/blob/main/LICENSE)
