# TextToVoice

A C# text-to-voice module designed as part of a larger pipeline: voice-to-text → validation & logic → result → **text-to-voice**.

## Features

- Direct audio playback
- Export to WAV files
- Multiple voice support
- Adjustable speech rate and volume
- Interface-based design with pluggable engines
- Settings file for persistent defaults

## Requirements

- .NET 10
- Windows (for System.Speech engine) or Piper (cross-platform)

## Building

```bash
dotnet build
```

## Usage

### Speak text directly

```bash
dotnet run --project src/TextToVoice.Apps.Console -- "Hello, this is a test"
```

### Save to file

```bash
dotnet run --project src/TextToVoice.Apps.Console -- "Hello world" -o hello.wav
```

### List available voices

```bash
dotnet run --project src/TextToVoice.Apps.Console -- --list-voices
```

### Use a specific voice

```bash
dotnet run --project src/TextToVoice.Apps.Console -- "Hello" -v "Microsoft David Desktop"
```

### Adjust rate and volume

```bash
dotnet run --project src/TextToVoice.Apps.Console -- "Speaking faster" -r 5 --volume 80
```

### Use a specific engine

```bash
# Windows engine (auto-detected on Windows)
dotnet run --project src/TextToVoice.Apps.Console -- "Hello" --engine windows

# Piper engine (cross-platform)
dotnet run --project src/TextToVoice.Apps.Console -- "Hello" --engine piper --model path/to/voice.onnx --piper-path path/to/piper.exe
```

## Command Line Options

| Option | Description |
|--------|-------------|
| `text` | The text to speak |
| `-o, --output <file>` | Save audio to file instead of playing |
| `-v, --voice <name>` | Voice to use for speech |
| `-r, --rate <-10..10>` | Speech rate (default: 0) |
| `--volume <0..100>` | Volume level (default: 100) |
| `--list-voices` | List available voices |
| `-e, --engine <name>` | TTS engine: auto, windows, piper |
| `-m, --model <path>` | Path to Piper model file (.onnx) |
| `--piper-path <path>` | Path to Piper executable |

## Settings File

Create a `settings.json` in the application directory (next to the executable) to set persistent defaults. CLI arguments override settings.

```json
{
  "engine": "piper",
  "voice": null,
  "rate": 0,
  "volume": 100,
  "piper": {
    "modelPath": "C:\\Users\\me\\tools\\piper\\en_US-lessac-medium.onnx",
    "executablePath": "C:\\Users\\me\\tools\\piper\\piper\\piper.exe"
  }
}
```

With settings configured, you can simply run:

```bash
dotnet run --project src/TextToVoice.Apps.Console -- "Hello world"
```

## Architecture

The project uses an interface-based design (`ITtsEngine`) with pluggable engine implementations:

- **TextToVoice.Core** — Interfaces and models (platform-agnostic)
- **TextToVoice.Engines.Windows** — Windows SAPI implementation
- **TextToVoice.Engines.Piper** — Cross-platform Piper TTS
- **TextToVoice.Apps.Console** — CLI application

See individual engine READMEs for setup and usage:
- [Windows Engine](src/TextToVoice.Engines.Windows/README.md)
- [Piper Engine](src/TextToVoice.Engines.Piper/README.md)

### Available Engines

| Engine | Platform | Status | Notes |
|--------|----------|--------|-------|
| Windows | Windows | Done | System.Speech (SAPI) |
| Piper | Cross-platform | Done | Requires piper executable + model |
| ElevenLabs | Cloud | Planned | High quality, API-based |

## Testing

```bash
dotnet test
```

## License

Open source friendly.
