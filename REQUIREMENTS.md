# TextToVoice Requirements

Living requirements document for the text-to-voice module.

## Version 1.0 (Prototype)

### Platform
- [x] Windows support (primary)
- [x] Cross-platform support (Piper engine)

### Output
- [x] Direct audio playback
- [x] Audio file export (WAV format)

### Application Type
- [x] Console application

### Languages
- [x] English only

### Architecture
- [x] Interface-based abstraction (ITtsEngine)
- [x] Windows implementation using System.Speech

### .NET Version
- [x] .NET 10

### Licensing
- [x] Open source friendly dependencies

## Future Requirements

### Additional TTS Engines
- [ ] Azure Cognitive Services integration
- [ ] Google Cloud TTS integration
- [ ] Amazon Polly integration
- [x] Piper TTS (offline, cross-platform)
- [ ] eSpeak (lightweight, cross-platform)

### Additional Output Formats
- [ ] MP3 export
- [ ] OGG export

### Additional Features
- [x] SSML support (native on Windows, preprocessed on Piper)
- [x] Settings file for persistent defaults
- [ ] SSML namespace auto-normalization (Windows requires full xmlns)
- [ ] ElevenLabs cloud engine
- [ ] Streaming audio
- [ ] Voice caching
- [ ] Multiple language support
