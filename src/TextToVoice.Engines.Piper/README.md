# TextToVoice.Engines.Piper

Cross-platform TTS engine using [Piper](https://github.com/rhasspy/piper), a fast local neural text-to-speech system.

## Platform

Windows, Linux, macOS.

## Installation

### 1. Download Piper

#### PowerShell (Windows)

```powershell
# Create tools directory
New-Item -ItemType Directory -Force -Path "$HOME\tools\piper"

# Download Piper for Windows
Invoke-WebRequest -Uri "https://github.com/rhasspy/piper/releases/download/2023.11.14-2/piper_windows_amd64.zip" -OutFile "$HOME\tools\piper\piper.zip"

# Extract
Expand-Archive -Path "$HOME\tools\piper\piper.zip" -DestinationPath "$HOME\tools\piper" -Force

# Clean up
Remove-Item "$HOME\tools\piper\piper.zip"
```

#### Bash (Linux)

```bash
# Create tools directory
mkdir -p ~/tools/piper

# Download Piper for Linux
curl -L -o ~/tools/piper/piper.tar.gz \
  https://github.com/rhasspy/piper/releases/download/2023.11.14-2/piper_linux_x86_64.tar.gz

# Extract
tar -xzf ~/tools/piper/piper.tar.gz -C ~/tools/piper

# Clean up
rm ~/tools/piper/piper.tar.gz

# Make executable
chmod +x ~/tools/piper/piper/piper
```

#### Bash (macOS)

```bash
mkdir -p ~/tools/piper

curl -L -o ~/tools/piper/piper.tar.gz \
  https://github.com/rhasspy/piper/releases/download/2023.11.14-2/piper_macos_x64.tar.gz

tar -xzf ~/tools/piper/piper.tar.gz -C ~/tools/piper
rm ~/tools/piper/piper.tar.gz
chmod +x ~/tools/piper/piper/piper
```

### 2. Download a voice model

Browse available voices at [huggingface.co/rhasspy/piper-voices](https://huggingface.co/rhasspy/piper-voices).

Download both the `.onnx` model file and the `.onnx.json` config file. They must be in the same directory.

#### PowerShell (Windows)

```powershell
# Download en_US-lessac-medium voice model and config
Invoke-WebRequest -Uri "https://huggingface.co/rhasspy/piper-voices/resolve/main/en/en_US/lessac/medium/en_US-lessac-medium.onnx" -OutFile "$HOME\tools\piper\en_US-lessac-medium.onnx"
Invoke-WebRequest -Uri "https://huggingface.co/rhasspy/piper-voices/resolve/main/en/en_US/lessac/medium/en_US-lessac-medium.onnx.json" -OutFile "$HOME\tools\piper\en_US-lessac-medium.onnx.json"
```

#### Bash (Linux/macOS)

```bash
# Download en_US-lessac-medium voice model and config
curl -L -o ~/tools/piper/en_US-lessac-medium.onnx \
  https://huggingface.co/rhasspy/piper-voices/resolve/main/en/en_US/lessac/medium/en_US-lessac-medium.onnx

curl -L -o ~/tools/piper/en_US-lessac-medium.onnx.json \
  https://huggingface.co/rhasspy/piper-voices/resolve/main/en/en_US/lessac/medium/en_US-lessac-medium.onnx.json
```

## Usage

Both `--model` and `--piper-path` are required unless `piper` is in your system PATH.

### PowerShell (Windows)

```powershell
# Speak text
dotnet run --project src/TextToVoice.Apps.Console -- "Hello world" --engine piper `
  --model "$HOME\tools\piper\en_US-lessac-medium.onnx" `
  --piper-path "$HOME\tools\piper\piper\piper.exe"

# Save to file
dotnet run --project src/TextToVoice.Apps.Console -- "Hello world" --engine piper `
  --model "$HOME\tools\piper\en_US-lessac-medium.onnx" `
  --piper-path "$HOME\tools\piper\piper\piper.exe" `
  -o output.wav
```

### Bash (Linux/macOS)

```bash
# Speak text
dotnet run --project src/TextToVoice.Apps.Console -- "Hello world" --engine piper \
  --model ~/tools/piper/en_US-lessac-medium.onnx \
  --piper-path ~/tools/piper/piper/piper

# Save to file
dotnet run --project src/TextToVoice.Apps.Console -- "Hello world" --engine piper \
  --model ~/tools/piper/en_US-lessac-medium.onnx \
  --piper-path ~/tools/piper/piper/piper \
  -o output.wav
```

### Adding Piper to PATH (optional)

Instead of using `--piper-path` each time, add the directory containing the piper executable to your system PATH.

**PowerShell (Windows — current session):**
```powershell
$env:PATH += ";$HOME\tools\piper\piper"
```

**PowerShell (Windows — permanent):**
```powershell
[Environment]::SetEnvironmentVariable("PATH", $env:PATH + ";$HOME\tools\piper\piper", "User")
```

**Bash (Linux/macOS — current session):**
```bash
export PATH="$PATH:$HOME/tools/piper/piper"
```

**Bash (Linux/macOS — permanent):**
```bash
echo 'export PATH="$PATH:$HOME/tools/piper/piper"' >> ~/.bashrc
source ~/.bashrc
```

## Features

- Save to WAV file
- Synthesize to byte array
- Direct playback (via system audio player: PowerShell on Windows, `aplay` on Linux, `afplay` on macOS)
- Rate control via length scale
- Multi-speaker model support via speaker ID

## Configuration

| Option | CLI Flag | Default | Description |
|--------|----------|---------|-------------|
| Model path | `--model` | (required) | Path to `.onnx` voice model |
| Executable | `--piper-path` | `piper` | Path to piper executable |
| Rate | `-r`, `--rate` | 0 | Speech rate (-10 to 10) |
