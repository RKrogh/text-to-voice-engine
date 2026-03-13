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

# Run with sherpa-onnx engine (embedded ONNX, no external process)
dotnet run --project src/TextToVoice.Apps.Console -- "Hello" --engine sherpaonnx --model path/to/voice.onnx --tokens-path path/to/tokens.txt --data-dir path/to/espeak-ng-data

# Run with ElevenLabs engine (cloud API, requires API key)
dotnet run --project src/TextToVoice.Apps.Console -- "Hello" --engine elevenlabs --api-key sk_your_key_here

# Save to file
dotnet run --project src/TextToVoice.Apps.Console -- "Hello" -o output.wav

# List available voices
dotnet run --project src/TextToVoice.Apps.Console -- --list-voices

# SSML input (auto-detected by <speak> tag, or use --ssml flag)
dotnet run --project src/TextToVoice.Apps.Console -- "<speak version='1.0' xmlns='http://www.w3.org/2001/10/synthesis' xml:lang='en-US'>Hello<break time='500ms'/>world</speak>"

# Adjust leading silence (prevents audio clipping on playback, default 150ms)
dotnet run --project src/TextToVoice.Apps.Console -- "Hello" --leading-silence 300
dotnet run --project src/TextToVoice.Apps.Console -- "Hello" --leading-silence 0  # disable
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
│   │   ├── SsmlDetector.cs            # SSML auto-detection and namespace normalization
│   │   ├── AudioPlayer.cs            # Cross-platform audio file playback
│   │   ├── TtsEngineFactory.cs        # Factory for creating engines
│   │   ├── TtsEngineType.cs           # Engine type enum
│   │   ├── VoiceInfo.cs               # Voice metadata record
│   │   ├── TtsOptions.cs              # Configuration class
│   │   ├── WavUtils.cs               # WAV manipulation (leading silence)
│   │   └── AudioFormat.cs             # Output format enum
│   ├── TextToVoice.Engines.Windows/   # Windows SAPI implementation
│   │   └── SystemSpeechEngine.cs
│   ├── TextToVoice.Engines.Piper/     # Cross-platform Piper TTS
│   │   ├── PiperEngine.cs
│   │   └── PiperOptions.cs
│   ├── TextToVoice.Engines.SherpaOnnx/ # Embedded ONNX inference (no external process)
│   │   ├── SherpaOnnxEngine.cs
│   │   └── SherpaOnnxOptions.cs
│   ├── TextToVoice.Engines.ElevenLabs/ # ElevenLabs cloud API
│   │   ├── ElevenLabsEngine.cs
│   │   └── ElevenLabsOptions.cs
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
  - Windows engine: native SSML via `SpeakSsml()` (auto-normalizes missing xmlns namespace)
  - Piper engine: preprocesses SSML to plain text, extracts rate/volume/voice hints
  - SherpaOnnx engine: preprocesses SSML to plain text (same as Piper)
  - ElevenLabs engine: preprocesses SSML to plain text (same as Piper/SherpaOnnx)

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
| SherpaOnnx | Cross-platform | ✓ Done | Embedded ONNX inference, no external process |
| ElevenLabs | Cloud | ✓ Done | High quality, API-based (paid, free tier: 10k chars/month) |

### Configuration

Uses `Microsoft.Extensions.Configuration` with the standard .NET hierarchy (highest priority wins):

1. CLI arguments (System.CommandLine)
2. Environment variables (prefixed `TTV_`, e.g. `TTV_ElevenLabs__ApiKey`)
3. User secrets (`dotnet user-secrets`)
4. `appsettings.{DOTNET_ENVIRONMENT}.json` (optional)
5. `appsettings.json` (optional)

Place `appsettings.json` next to the executable (`AppContext.BaseDirectory`). See `AppSettings.cs` for the model. Use `dotnet user-secrets` for API keys and other sensitive values.

### Piper Setup

1. Download Piper from https://github.com/rhasspy/piper/releases
2. Download voice model from https://huggingface.co/rhasspy/piper-voices
3. Run: `texttovoice "Hello" --engine piper --model path/to/voice.onnx --piper-path path/to/piper.exe`

### SherpaOnnx Setup

Embedded ONNX inference — loads Piper VITS models directly in-process via the `org.k2fsa.sherpa.onnx` NuGet package. No external executables needed.

1. Download a pre-converted Piper model (includes tokens.txt + espeak-ng-data):
   `wget https://github.com/k2-fsa/sherpa-onnx/releases/download/tts-models/vits-piper-en_US-amy-low.tar.bz2`
2. Extract: `tar xf vits-piper-en_US-amy-low.tar.bz2`
3. Run: `texttovoice "Hello" --engine sherpaonnx --model ./vits-piper-en_US-amy-low/en_US-amy-low.onnx`
   (tokens.txt and espeak-ng-data are auto-detected from the model directory)

To convert raw Piper models, use the sherpa-onnx conversion script (requires `pip install onnx`).

### Known Issues

- **Piper SSML is best-effort**: The preprocessor extracts breaks, rate, volume, and voice hints, but complex SSML (emphasis, phonemes, say-as) is stripped to plain text.

### ElevenLabs Setup

Cloud-based high-quality TTS via the ElevenLabs REST API. Requires an API key from [elevenlabs.io](https://elevenlabs.io).

- API key resolution: `--api-key` CLI flag → user secrets → `appsettings.json` → `TTV_ElevenLabs__ApiKey` env var → `ELEVENLABS_API_KEY` env var
- Default voice: Rachel (`21m00Tcm4TlvDq8ikWAM`)
- Default model: `eleven_multilingual_v2`
- Output: PCM 44100 Hz, wrapped in WAV by the engine
- No external NuGet packages — uses raw `HttpClient`

### Next Steps (suggested order)

1. **Streaming audio** — Play audio as it's generated rather than waiting for full synthesis
2. **MP3/OGG export** — Additional output formats beyond WAV
3. **Multiple language support** — Voice/model selection per language

### Future Investigation

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
- org.k2fsa.sherpa.onnx (in Engines.SherpaOnnx)
- System.CommandLine (in Apps.Console)
- Microsoft.Extensions.Configuration.* (in Apps.Console — JSON, UserSecrets, EnvironmentVariables, Binder)
- HttpClient / System.Net.Http (in Engines.ElevenLabs, no external NuGet)
