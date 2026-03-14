# TextToVoice.Engines.SherpaOnnx

Embedded ONNX inference engine for TextToVoice. Runs Piper VITS models directly in-process — no external executables needed.

## Installation

```
dotnet add package TextToVoice.Engines.SherpaOnnx
```

Download a pre-converted model from [sherpa-onnx releases](https://github.com/k2-fsa/sherpa-onnx/releases) (look for `vits-piper-*` archives).

## Quick Start

```csharp
using TextToVoice.Engines.SherpaOnnx;

var options = new SherpaOnnxOptions
{
    ModelPath = "/path/to/en_US-amy-low.onnx",
    // tokens.txt and espeak-ng-data are auto-detected from the model directory
};

using var engine = new SherpaOnnxEngine(options);

await engine.SpeakAsync("Hello world");
await engine.SaveToFileAsync("Hello world", "output.wav");

// Multi-speaker models
engine.SetVoice("0");              // by speaker ID
engine.SetVoice("en_US-amy#2");    // by modelname#id format
```

## Configuration

```csharp
var options = new SherpaOnnxOptions
{
    ModelPath = "/path/to/voice.onnx",   // required
    TokensPath = null,                    // auto-detected from model directory
    DataDir = null,                       // auto-detected (espeak-ng-data)
    SpeakerId = 0,                        // for multi-speaker models
    LengthScale = 1.0f,                  // speech speed
    NumThreads = 1,                       // ONNX inference threads
    LeadingSilenceMs = 150,              // silence before playback (0 to disable)
};
```

## Requirements

- .NET 10+
- ONNX voice model (Piper VITS format with sherpa-onnx metadata)

## License

[MIT](https://github.com/RKrogh/text-to-voice-engine/blob/main/LICENSE)
