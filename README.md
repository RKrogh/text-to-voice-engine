# TextToVoice

A C# text-to-voice module designed as part of a larger pipeline: voice-to-text → validation & logic → result → **text-to-voice**.

## Features

- Direct audio playback
- Export to WAV files
- Multiple voice support
- Adjustable speech rate and volume
- Interface-based design for future extensibility

## Requirements

- Windows (uses System.Speech)
- .NET 10

## Building

```bash
dotnet build
```

## Usage

### Speak text directly

```bash
dotnet run --project src/TextToVoice.Console -- "Hello, this is a test"
```

### Save to file

```bash
dotnet run --project src/TextToVoice.Console -- "Hello world" -o hello.wav
```

### List available voices

```bash
dotnet run --project src/TextToVoice.Console -- --list-voices
```

### Use a specific voice

```bash
dotnet run --project src/TextToVoice.Console -- "Hello" -v "Microsoft David Desktop"
```

### Adjust rate and volume

```bash
dotnet run --project src/TextToVoice.Console -- "Speaking faster" -r 5 --volume 80
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

## Architecture

The project uses an interface-based design (`ITtsEngine`) to allow for future TTS engine implementations:

- **TextToVoice.Core** - Class library containing interfaces and the Windows implementation
- **TextToVoice.Console** - CLI application for testing and direct use

### Future Extensibility

The interface design allows adding providers later:
- Azure Cognitive Services
- Google Cloud TTS
- Amazon Polly
- Piper TTS (open source, cross-platform)
- eSpeak

## License

Open source friendly.
