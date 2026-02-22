using Microsoft.Extensions.Configuration;
using TextToVoice.Apps.Console;

namespace TextToVoice.Core.Tests;

public class AppSettingsTests
{
    private static AppSettings LoadFromJson(string json)
    {
        // Write JSON to a temp file, load via ConfigurationBuilder — same as the real app
        var tempFile = Path.GetTempFileName();
        try
        {
            File.WriteAllText(tempFile, json);
            var config = new ConfigurationBuilder().AddJsonFile(tempFile, optional: false).Build();
            return config.Get<AppSettings>() ?? new AppSettings();
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public void Load_FullSettings_AllPropertiesMapped()
    {
        var json = """
            {
                "engine": "piper",
                "voice": "en_US-lessac-medium",
                "rate": 3,
                "volume": 80,
                "piper": {
                    "modelPath": "C:\\models\\voice.onnx",
                    "executablePath": "C:\\tools\\piper\\piper.exe"
                }
            }
            """;

        var settings = LoadFromJson(json);

        Assert.Equal("piper", settings.Engine);
        Assert.Equal("en_US-lessac-medium", settings.Voice);
        Assert.Equal(3, settings.Rate);
        Assert.Equal(80, settings.Volume);
        Assert.NotNull(settings.Piper);
        Assert.Equal("C:\\models\\voice.onnx", settings.Piper.ModelPath);
        Assert.Equal("C:\\tools\\piper\\piper.exe", settings.Piper.ExecutablePath);
    }

    [Fact]
    public void Load_EmptyJson_AllPropertiesNull()
    {
        var json = "{}";

        var settings = LoadFromJson(json);

        Assert.Null(settings.Engine);
        Assert.Null(settings.Voice);
        Assert.Null(settings.Rate);
        Assert.Null(settings.Volume);
        Assert.Null(settings.Piper);
    }

    [Fact]
    public void Load_PartialSettings_OnlySpecifiedPropertiesSet()
    {
        var json = """
            {
                "engine": "windows",
                "volume": 50
            }
            """;

        var settings = LoadFromJson(json);

        Assert.Equal("windows", settings.Engine);
        Assert.Null(settings.Voice);
        Assert.Null(settings.Rate);
        Assert.Equal(50, settings.Volume);
        Assert.Null(settings.Piper);
    }

    [Fact]
    public void Load_PiperOnly_ModelPathWithoutExecutable()
    {
        var json = """
            {
                "piper": {
                    "modelPath": "/opt/models/voice.onnx"
                }
            }
            """;

        var settings = LoadFromJson(json);

        Assert.NotNull(settings.Piper);
        Assert.Equal("/opt/models/voice.onnx", settings.Piper.ModelPath);
        Assert.Null(settings.Piper.ExecutablePath);
    }

    [Fact]
    public void Load_ElevenLabsSettings_PropertiesMapped()
    {
        var json = """
            {
                "elevenlabs": {
                    "apiKey": "sk_test_123",
                    "voiceId": "voice123",
                    "modelId": "eleven_turbo_v2"
                }
            }
            """;

        var settings = LoadFromJson(json);

        Assert.NotNull(settings.ElevenLabs);
        Assert.Equal("sk_test_123", settings.ElevenLabs.ApiKey);
        Assert.Equal("voice123", settings.ElevenLabs.VoiceId);
        Assert.Equal("eleven_turbo_v2", settings.ElevenLabs.ModelId);
    }

    [Fact]
    public void Load_SherpaOnnxSettings_PropertiesMapped()
    {
        var json = """
            {
                "sherpaOnnx": {
                    "modelPath": "/models/voice.onnx",
                    "tokensPath": "/models/tokens.txt",
                    "dataDir": "/models/espeak-ng-data"
                }
            }
            """;

        var settings = LoadFromJson(json);

        Assert.NotNull(settings.SherpaOnnx);
        Assert.Equal("/models/voice.onnx", settings.SherpaOnnx.ModelPath);
        Assert.Equal("/models/tokens.txt", settings.SherpaOnnx.TokensPath);
        Assert.Equal("/models/espeak-ng-data", settings.SherpaOnnx.DataDir);
    }

    [Fact]
    public void ConfigurationHierarchy_LaterSourceOverridesEarlier()
    {
        var baseJson = Path.GetTempFileName();
        var overrideJson = Path.GetTempFileName();
        try
        {
            File.WriteAllText(baseJson, """{"engine": "windows", "volume": 100}""");
            File.WriteAllText(overrideJson, """{"engine": "piper"}""");

            var config = new ConfigurationBuilder()
                .AddJsonFile(baseJson, optional: false)
                .AddJsonFile(overrideJson, optional: false)
                .Build();

            var settings = config.Get<AppSettings>()!;

            Assert.Equal("piper", settings.Engine); // overridden
            Assert.Equal(100, settings.Volume); // kept from base
        }
        finally
        {
            File.Delete(baseJson);
            File.Delete(overrideJson);
        }
    }
}
