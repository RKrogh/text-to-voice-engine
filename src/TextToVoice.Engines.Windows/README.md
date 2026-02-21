# TextToVoice.Engines.Windows

Windows TTS engine using the built-in System.Speech (SAPI) synthesizer.

## Platform

Windows only. No additional installation required — uses the speech synthesis API included with Windows.

## Usage

All commands below are for PowerShell.

```powershell
# Speak text (uses Windows engine by default on Windows)
dotnet run --project src/TextToVoice.Apps.Console -- "Hello world"

# Explicitly select the Windows engine
dotnet run --project src/TextToVoice.Apps.Console -- "Hello world" --engine windows

# List available voices
dotnet run --project src/TextToVoice.Apps.Console -- --list-voices

# Select a specific voice
dotnet run --project src/TextToVoice.Apps.Console -- "Hello world" -v "Microsoft David Desktop"

# Adjust speech rate (-10 slowest, 10 fastest)
dotnet run --project src/TextToVoice.Apps.Console -- "Hello world" -r 3

# Adjust volume (0 to 100)
dotnet run --project src/TextToVoice.Apps.Console -- "Hello world" --volume 50

# Save to file
dotnet run --project src/TextToVoice.Apps.Console -- "Hello world" -o output.wav

# Combine options
dotnet run --project src/TextToVoice.Apps.Console -- "Hello world" `
  -v "Microsoft Zira Desktop" -r 2 --volume 80 -o output.wav
```

## Installing additional voices

Windows comes with a default voice. Additional voices can be installed via Settings.

```powershell
# Open Windows speech settings
Start-Process "ms-settings:speech"
```

From there, scroll to **Manage voices** and click **Add voices** to install additional languages and voices.

## Features

- Direct audio playback
- Save to WAV file
- Synthesize to byte array
- Voice selection from installed Windows voices
- Rate control (-10 to 10)
- Volume control (0 to 100)
