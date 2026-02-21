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

# Run with direct playback
dotnet run --project src/TextToVoice.Console -- "Hello world"

# Run with file output
dotnet run --project src/TextToVoice.Console -- "Hello world" -o output.wav

# List available voices
dotnet run --project src/TextToVoice.Console -- --list-voices
```

## Architecture

```
texttovoice/
├── src/
│   ├── TextToVoice.Core/           # Class library - interfaces and models
│   │   ├── ITtsEngine.cs           # Main TTS interface
│   │   ├── VoiceInfo.cs            # Voice metadata record
│   │   ├── TtsOptions.cs           # Configuration class
│   │   ├── AudioFormat.cs          # Output format enum
│   │   └── Engines/
│   │       └── SystemSpeechEngine.cs  # Windows SAPI implementation
│   └── TextToVoice.Console/        # Console app - CLI interface
│       └── Program.cs              # Entry point with System.CommandLine
├── tests/
│   └── TextToVoice.Core.Tests/     # Unit tests (xUnit)
├── TextToVoice.sln
├── README.md
├── CLAUDE.md
└── REQUIREMENTS.md                 # Living requirements document
```

### Key Interfaces

- **ITtsEngine** - Main abstraction for TTS engines, supports:
  - `SpeakAsync()` - Direct playback
  - `SynthesizeToAudioAsync()` - Get audio bytes
  - `SaveToFileAsync()` - Export to file
  - Voice selection, rate, and volume control

### Current Implementation

- **SystemSpeechEngine** - Windows implementation using System.Speech (SAPI)

### Dependencies

- .NET 10
- System.Speech (Windows TTS)
- System.CommandLine (CLI parsing)
