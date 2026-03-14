# TextToVoice.Engines.Windows

Windows SAPI (System.Speech) engine for TextToVoice. Native SSML support with automatic namespace normalization.

## Installation

```
dotnet add package TextToVoice.Engines.Windows
```

## Quick Start

```csharp
using TextToVoice.Engines.Windows;

using var engine = new SystemSpeechEngine();

// Speak text
await engine.SpeakAsync("Hello world");

// Save to file
await engine.SaveToFileAsync("Hello world", "output.wav");

// List and select voices
var voices = engine.GetAvailableVoices();
engine.SetVoice("Microsoft David Desktop");

// Adjust rate and volume
engine.SetRate(3);     // -10 to 10
engine.SetVolume(80);  // 0 to 100
```

## SSML Support

The Windows engine supports SSML natively. Missing `xmlns` namespaces are auto-normalized.

```csharp
using TextToVoice.Core;

if (engine is ISsmlCapable ssml)
{
    // xmlns is added automatically if missing
    await ssml.SpeakSsmlAsync("""
        <speak version='1.0' xml:lang='en-US'>
            Hello <break time='500ms'/>
            <prosody rate='slow'>world</prosody>
        </speak>
        """);
}
```

## Requirements

- Windows
- .NET 10+

## License

[MIT](https://github.com/RKrogh/text-to-voice-engine/blob/main/LICENSE)
