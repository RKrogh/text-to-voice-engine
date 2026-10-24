# TextToVoice.Engines.SherpaOnnx

Embedded ONNX inference engine for TextToVoice. Runs Piper VITS and Kokoro models directly in-process — no external executables needed.

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
    Normalize = true,                     // peak-normalise each line before volume
    VoicesPath = null,                    // set to a Kokoro voices.bin to load a Kokoro model
};
```

## Several voices from one engine

Pass a `SherpaOnnxVoice` per call. One multi-speaker model (for example `libritts_r`, 904
speakers) can then voice a whole cast, from several threads at once, without touching the
engine's default voice:

```csharp
var helm = new SherpaOnnxVoice(SpeakerId: 40);
var pilot = new SherpaOnnxVoice(SpeakerId: 120, Speed: 1.1f, Volume: 0.8f);

byte[] wav = await engine.SynthesizeToAudioAsync("Helm, aye.", helm);

// Raw mono samples in [-1, 1], for feeding an audio engine directly.
SynthesizedAudio audio = engine.Synthesize("Two bandits on my six.", pilot);
```

Calls into the model are serialized, so parallel calls are safe but do not run faster.

Kokoro support covers the English models (for example `kokoro-en-v0_19`). The multilingual
Kokoro v1.0 models also need a lexicon and dictionary directory, which are not exposed yet.

Since 0.3.0, `SetVolume` and `SherpaOnnxVoice.Volume` change the output level. Earlier versions
normalised every line to full scale after applying volume, so volume had no effect.

## Requirements

- .NET 10+
- ONNX voice model (Piper VITS format with sherpa-onnx metadata)

## License

[MIT](https://github.com/RKrogh/text-to-voice-engine/blob/main/LICENSE)
