# TextToVoice

A C# text-to-voice module designed as part of a larger pipeline: voice-to-text → validation & logic → result → **text-to-voice**.

## Features

- Direct audio playback
- Export to WAV files
- Multiple voice support
- Adjustable speech rate and volume
- SSML support (native on Windows, preprocessed on other engines)
- Embedded ONNX inference via sherpa-onnx (no external process needed)
- Cloud-based high-quality TTS via ElevenLabs API
- Interface-based design with pluggable engines
- Settings file for persistent defaults
- Configurable leading silence to prevent audio clipping

## Requirements

- .NET 10
- Windows (for System.Speech engine), Piper (cross-platform), SherpaOnnx (embedded, cross-platform), or ElevenLabs (cloud, requires API key)

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

# SherpaOnnx engine (embedded ONNX, no external process)
dotnet run --project src/TextToVoice.Apps.Console -- "Hello" --engine sherpaonnx --model path/to/voice.onnx --tokens-path path/to/tokens.txt --data-dir path/to/espeak-ng-data

# ElevenLabs engine (cloud API)
dotnet run --project src/TextToVoice.Apps.Console -- "Hello" --engine elevenlabs --api-key sk_your_key_here
```

### SSML input

SSML is auto-detected when input starts with `<speak>`, or use `--ssml` explicitly.

```bash
# Auto-detected SSML (Windows requires full xmlns namespace)
dotnet run --project src/TextToVoice.Apps.Console -- "<speak version='1.0' xmlns='http://www.w3.org/2001/10/synthesis' xml:lang='en-US'>Hello<break time='500ms'/><prosody rate='slow'>world</prosody></speak>"

# Explicit --ssml flag
dotnet run --project src/TextToVoice.Apps.Console -- --ssml "<speak version='1.0' xmlns='http://www.w3.org/2001/10/synthesis' xml:lang='en-US'>Hello world</speak>"
```

**Note:** The Windows engine supports SSML natively but requires the full `xmlns` namespace in the `<speak>` tag. The Piper engine preprocesses SSML — it extracts breaks (as pauses), prosody rate/volume, and voice hints, then speaks the plain text.

## Command Line Options

| Option | Description |
|--------|-------------|
| `text` | The text to speak |
| `-o, --output <file>` | Save audio to file instead of playing |
| `-v, --voice <name>` | Voice to use for speech |
| `-r, --rate <-10..10>` | Speech rate (default: 0) |
| `--volume <0..100>` | Volume level (default: 100) |
| `--list-voices` | List available voices |
| `-e, --engine <name>` | TTS engine: auto, windows, piper, sherpaonnx, elevenlabs |
| `-m, --model <path>` | Path to voice model file (.onnx) |
| `--piper-path <path>` | Path to Piper executable |
| `--tokens-path <path>` | Path to tokens.txt (sherpa-onnx) |
| `--data-dir <path>` | Path to espeak-ng-data directory (sherpa-onnx) |
| `--api-key <key>` | API key for ElevenLabs (also: settings or ELEVENLABS_API_KEY env var) |
| `--leading-silence <ms>` | Silence before playback to prevent clipping (default: 150, 0 to disable) |
| `--ssml` | Treat input as SSML (auto-detected if starts with `<speak>`) |

## Configuration

Settings are loaded using the standard .NET configuration hierarchy (highest priority wins):

1. **CLI arguments** — always override everything
2. **Environment variables** — prefixed with `TTV_` (e.g. `TTV_ElevenLabs__ApiKey`)
3. **User secrets** — `dotnet user-secrets` (Development, ideal for API keys)
4. **`appsettings.{Environment}.json`** — per-environment overrides (optional)
5. **`appsettings.json`** — base defaults (optional)

### appsettings.json

Create `appsettings.json` next to the executable to set persistent defaults.

```json
{
  "engine": "piper",
  "voice": null,
  "rate": 0,
  "volume": 100,
  "leadingSilenceMs": 150,
  "piper": {
    "modelPath": "C:\\Users\\me\\tools\\piper\\en_US-lessac-medium.onnx",
    "executablePath": "C:\\Users\\me\\tools\\piper\\piper\\piper.exe"
  },
  "sherpaOnnx": {
    "modelPath": "C:\\Users\\me\\models\\en_US-amy-low.onnx",
    "tokensPath": "C:\\Users\\me\\models\\tokens.txt",
    "dataDir": "C:\\Users\\me\\models\\espeak-ng-data"
  },
  "elevenlabs": {
    "voiceId": "21m00Tcm4TlvDq8ikWAM",
    "modelId": "eleven_multilingual_v2"
  }
}
```

With settings configured, you can simply run:

```bash
dotnet run --project src/TextToVoice.Apps.Console -- "Hello world"
```

### User Secrets (recommended for API keys)

Store sensitive values like API keys in user secrets instead of config files:

```bash
# Initialize (one-time)
dotnet user-secrets init --project src/TextToVoice.Apps.Console

# Set the ElevenLabs API key
dotnet user-secrets set "ElevenLabs:ApiKey" "sk_your_key_here" --project src/TextToVoice.Apps.Console

# Verify
dotnet user-secrets list --project src/TextToVoice.Apps.Console
```

Secrets are stored in `~/.microsoft/usersecrets/` (outside the repo) and never committed to git.

### Environment Variables

All settings can be set via environment variables with the `TTV_` prefix. Nested keys use `__` (double underscore):

```bash
export TTV_Engine=elevenlabs
export TTV_ElevenLabs__ApiKey=sk_your_key_here
export TTV_Piper__ModelPath=/opt/models/voice.onnx
```

The `ELEVENLABS_API_KEY` environment variable (without prefix) is also supported as a fallback.

## ElevenLabs Engine

### Cost Warning

ElevenLabs is a **paid cloud API**. Usage is billed by character count.

| Plan | Characters/month | Price |
|------|----------------:|------:|
| Free | 10,000 | $0 |
| Starter | 30,000 | $5/month |
| Creator | 100,000 | $22/month |
| Pro | 500,000 | $99/month |

See [elevenlabs.io/pricing](https://elevenlabs.io/pricing) for current pricing.

### Setup

1. Create an account at [elevenlabs.io](https://elevenlabs.io)
2. Go to **Profile + API Key** (click your profile icon → Profile + API key)
3. Copy your API key

### API Key Configuration

The API key is resolved in this order (first found wins):

1. `--api-key` CLI flag
2. User secrets (`ElevenLabs:ApiKey`)
3. `appsettings.json` (`elevenlabs.apiKey`)
4. `TTV_ElevenLabs__ApiKey` environment variable
5. `ELEVENLABS_API_KEY` environment variable (legacy fallback)

```bash
# Option 1: CLI flag
dotnet run --project src/TextToVoice.Apps.Console -- "Hello" --engine elevenlabs --api-key sk_xxx

# Option 2: User secrets (recommended — keeps key out of files and env)
dotnet user-secrets set "ElevenLabs:ApiKey" "sk_xxx" --project src/TextToVoice.Apps.Console
dotnet run --project src/TextToVoice.Apps.Console -- "Hello" --engine elevenlabs

# Option 3: Environment variable
export ELEVENLABS_API_KEY=sk_xxx
dotnet run --project src/TextToVoice.Apps.Console -- "Hello" --engine elevenlabs
```

### Available Models

| Model | Quality | Speed | Multilingual |
|-------|---------|-------|:------------:|
| `eleven_multilingual_v2` (default) | Best | Slower | 29 languages |
| `eleven_turbo_v2_5` | Good | Fastest | 32 languages |
| `eleven_turbo_v2` | Good | Fast | English only |
| `eleven_monolingual_v1` | Good | Fast | English only |

Set the model in `appsettings.json` or user secrets, or use the default (`eleven_multilingual_v2`).

### List Available Voices

```bash
dotnet run --project src/TextToVoice.Apps.Console -- --list-voices --engine elevenlabs --api-key sk_xxx
```

This queries the ElevenLabs API and returns all voices available to your account (built-in + any custom voices you've created).

### Examples

```bash
# Speak with default voice (Rachel)
dotnet run --project src/TextToVoice.Apps.Console -- "Hello world" --engine elevenlabs --api-key sk_xxx

# Save to file
dotnet run --project src/TextToVoice.Apps.Console -- "Hello world" --engine elevenlabs --api-key sk_xxx -o output.wav

# Use a specific voice (pass voice ID from --list-voices)
dotnet run --project src/TextToVoice.Apps.Console -- "Hello" --engine elevenlabs --api-key sk_xxx -v "EXAVITQu4vr4xnSDxMaL"
```

## Architecture

The project uses an interface-based design (`ITtsEngine`) with pluggable engine implementations:

- **TextToVoice.Core** — Interfaces and models (platform-agnostic)
- **TextToVoice.Engines.Windows** — Windows SAPI implementation
- **TextToVoice.Engines.Piper** — Cross-platform Piper TTS (external process)
- **TextToVoice.Engines.SherpaOnnx** — Embedded ONNX inference (no external process)
- **TextToVoice.Engines.ElevenLabs** — ElevenLabs cloud API (high quality, paid)
- **TextToVoice.Apps.Console** — CLI application

### Available Engines

| Engine | Platform | Status | Notes |
|--------|----------|--------|-------|
| Windows | Windows | Done | System.Speech (SAPI) |
| Piper | Cross-platform | Done | Requires piper executable + model |
| SherpaOnnx | Cross-platform | Done | Embedded ONNX inference, no external process |
| ElevenLabs | Cloud | Done | High quality, API-based (paid) |

## Testing

```bash
dotnet test
```

## License

[MIT](LICENSE)
