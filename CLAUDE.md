# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Development Environment

- Claude Code sessions run from **WSL**
- Windows commands can be run via **PowerShell** when needed (e.g., `powershell.exe -Command "..."`)
- System.Speech tests require Windows - they are skipped in WSL but run on Windows

## Project Overview

Text-to-voice module, part of a larger pipeline: voice-to-text → validation & logic → result → **text-to-voice**.

## Build and Development Commands

```bash
# Build the solution
dotnet build

# Run tests
dotnet test

# Run with Windows engine (auto-detected on Windows)
dotnet run --project src/TextToVoice.Apps.Console -- "Hello world"

# Run with specific engine
dotnet run --project src/TextToVoice.Apps.Console -- "Hello" --engine windows
dotnet run --project src/TextToVoice.Apps.Console -- "Hello" --engine piper --model path/to/voice.onnx

# Save to file
dotnet run --project src/TextToVoice.Apps.Console -- "Hello" -o output.wav

# List available voices
dotnet run --project src/TextToVoice.Apps.Console -- --list-voices
```

## Architecture

```
texttovoice/
├── src/
│   ├── TextToVoice.Core/              # Interfaces and models (no platform deps)
│   │   ├── ITtsEngine.cs              # Main TTS interface
│   │   ├── TtsEngineFactory.cs        # Factory for creating engines
│   │   ├── TtsEngineType.cs           # Engine type enum
│   │   ├── VoiceInfo.cs               # Voice metadata record
│   │   ├── TtsOptions.cs              # Configuration class
│   │   └── AudioFormat.cs             # Output format enum
│   ├── TextToVoice.Engines.Windows/   # Windows SAPI implementation
│   │   └── SystemSpeechEngine.cs
│   ├── TextToVoice.Engines.Piper/     # Cross-platform Piper TTS
│   │   ├── PiperEngine.cs
│   │   └── PiperOptions.cs
│   └── TextToVoice.Apps.Console/      # CLI application
│       └── Program.cs
├── tests/
│   └── TextToVoice.Core.Tests/        # Unit tests (xUnit)
├── TextToVoice.sln
├── README.md
├── CLAUDE.md
└── REQUIREMENTS.md
```

### Key Interfaces

- **ITtsEngine** - Main abstraction for TTS engines:
  - `SpeakAsync()` - Direct playback
  - `SynthesizeToAudioAsync()` - Get audio bytes
  - `SaveToFileAsync()` - Export to file
  - Voice selection, rate, and volume control

- **TtsEngineFactory** - Creates engines by type with auto-detection:
  - `Register(type, factory)` - Register an engine
  - `Create(type)` - Create engine instance
  - `Parse(name)` - Parse string to engine type

### Project Naming Convention

- **TextToVoice.Core** - Interfaces and models (platform-agnostic)
- **TextToVoice.Engines.*** - Engine implementations
- **TextToVoice.Apps.*** - Consumer applications

### Available Engines

| Engine | Platform | Status | Notes |
|--------|----------|--------|-------|
| Windows | Windows | ✓ Done | System.Speech (SAPI) |
| Piper | Cross-platform | ✓ Done | Requires piper executable + model |
| ElevenLabs | Cloud | Planned | High quality, API-based |

### Piper Setup

1. Download Piper from https://github.com/rhasspy/piper/releases
2. Download voice model from https://huggingface.co/rhasspy/piper-voices
3. Run: `texttovoice "Hello" --engine piper --model path/to/voice.onnx`

### Dependencies

- .NET 10
- System.Speech (in Engines.Windows)
- System.CommandLine (in Apps.Console)
