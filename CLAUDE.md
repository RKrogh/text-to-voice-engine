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
dotnet run --project src/TextToVoice.Apps.Console -- "Hello" --engine piper --model path/to/voice.onnx --piper-path path/to/piper.exe

# Save to file
dotnet run --project src/TextToVoice.Apps.Console -- "Hello" -o output.wav

# List available voices
dotnet run --project src/TextToVoice.Apps.Console -- --list-voices

# SSML input (auto-detected by <speak> tag, or use --ssml flag)
dotnet run --project src/TextToVoice.Apps.Console -- "<speak version='1.0' xmlns='http://www.w3.org/2001/10/synthesis' xml:lang='en-US'>Hello<break time='500ms'/>world</speak>"
```

## Architecture

```
texttovoice/
├── src/
│   ├── TextToVoice.Core/              # Interfaces and models (no platform deps)
│   │   ├── ITtsEngine.cs              # Main TTS interface
│   │   ├── ISsmlCapable.cs            # SSML support interface
│   │   ├── ISsmlPreprocessor.cs       # SSML-to-text preprocessor interface
│   │   ├── SsmlPreprocessor.cs        # Default SSML preprocessor (XML-based)
│   │   ├── SsmlPreprocessResult.cs    # Preprocessor output record
│   │   ├── SsmlDetector.cs            # SSML auto-detection utility
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
│       ├── Program.cs
│       └── AppSettings.cs             # Settings file model
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

- **ISsmlCapable** - Optional interface for SSML support (separate from ITtsEngine):
  - `SupportsNativeSsml` - Whether engine handles SSML directly
  - `SpeakSsmlAsync()` - Speak SSML markup
  - `SynthesizeSsmlToAudioAsync()` - Synthesize SSML to bytes
  - `SaveSsmlToFileAsync()` - Save SSML audio to file
  - Windows engine: native SSML via `SpeakSsml()` (requires full xmlns namespace)
  - Piper engine: preprocesses SSML to plain text, extracts rate/volume/voice hints

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

### Settings File

Place `settings.json` next to the executable (`AppContext.BaseDirectory`) to set defaults. CLI args override settings. See `AppSettings.cs` for the model. The file is gitignored.

### Piper Setup

1. Download Piper from https://github.com/rhasspy/piper/releases
2. Download voice model from https://huggingface.co/rhasspy/piper-voices
3. Run: `texttovoice "Hello" --engine piper --model path/to/voice.onnx --piper-path path/to/piper.exe`

### Known Issues

- **Windows SSML requires full namespace**: `SpeakSsml()` silently produces no audio if the `<speak>` tag lacks `xmlns='http://www.w3.org/2001/10/synthesis'`. A future improvement would auto-normalize this.
- **Piper SSML is best-effort**: The preprocessor extracts breaks, rate, volume, and voice hints, but complex SSML (emphasis, phonemes, say-as) is stripped to plain text.
- **Piper rate mutation**: `ApplyPreprocessResult` modifies `_options.LengthScale` as a side effect that persists after the SSML call. Should save/restore the original value.

### Next Steps (suggested order)

1. **SSML namespace auto-normalization** — Detect `<speak>` without xmlns and add it automatically for Windows engine
2. **ElevenLabs engine** — Cloud-based high-quality TTS (`TextToVoice.Engines.ElevenLabs`)
3. **Streaming audio** — Play audio as it's generated rather than waiting for full synthesis
4. **MP3/OGG export** — Additional output formats beyond WAV
5. **Multiple language support** — Voice/model selection per language

### Future Investigation

**Embedded/offline engine (no external process):**
- Load Piper ONNX models directly via ONNX Runtime in .NET — eliminates piper executable dependency
- Would make deployment and packaging much simpler
- Significant architecture change but aligns with the interface pattern

**Expressive/conversational TTS engines to evaluate:**
- **Sesame CSM** (`sesame/csm-1b`) — Open-source conversational speech model, very natural. PyTorch/GPU, would need Python sidecar or HTTP wrapper
- **StyleTTS 2** — Open-source, natural prosody, academic project
- **Parler-TTS** — HuggingFace, control voice style via text descriptions
- **Fish Speech** — Open-source, multilingual, good quality
- **Coqui/XTTS** — Company shut down but models still on HuggingFace, voice cloning capable

Any of these could be added as a new engine following the `ITtsEngine`/`ISsmlCapable` pattern.

### Dependencies

- .NET 10
- System.Speech (in Engines.Windows)
- System.CommandLine (in Apps.Console)
