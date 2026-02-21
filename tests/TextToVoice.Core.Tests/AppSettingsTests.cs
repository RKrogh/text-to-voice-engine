using System.Text.Json;
using TextToVoice.Apps.Console;

namespace TextToVoice.Core.Tests;

public class AppSettingsTests
{
    [Fact]
    public void Deserialize_FullSettings_AllPropertiesMapped()
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

        var settings = JsonSerializer.Deserialize<AppSettings>(json);

        Assert.NotNull(settings);
        Assert.Equal("piper", settings.Engine);
        Assert.Equal("en_US-lessac-medium", settings.Voice);
        Assert.Equal(3, settings.Rate);
        Assert.Equal(80, settings.Volume);
        Assert.NotNull(settings.Piper);
        Assert.Equal("C:\\models\\voice.onnx", settings.Piper.ModelPath);
        Assert.Equal("C:\\tools\\piper\\piper.exe", settings.Piper.ExecutablePath);
    }

    [Fact]
    public void Deserialize_EmptyJson_AllPropertiesNull()
    {
        var json = "{}";

        var settings = JsonSerializer.Deserialize<AppSettings>(json);

        Assert.NotNull(settings);
        Assert.Null(settings.Engine);
        Assert.Null(settings.Voice);
        Assert.Null(settings.Rate);
        Assert.Null(settings.Volume);
        Assert.Null(settings.Piper);
    }

    [Fact]
    public void Deserialize_PartialSettings_OnlySpecifiedPropertiesSet()
    {
        var json = """
            {
                "engine": "windows",
                "volume": 50
            }
            """;

        var settings = JsonSerializer.Deserialize<AppSettings>(json);

        Assert.NotNull(settings);
        Assert.Equal("windows", settings.Engine);
        Assert.Null(settings.Voice);
        Assert.Null(settings.Rate);
        Assert.Equal(50, settings.Volume);
        Assert.Null(settings.Piper);
    }

    [Fact]
    public void Deserialize_PiperOnly_ModelPathWithoutExecutable()
    {
        var json = """
            {
                "piper": {
                    "modelPath": "/opt/models/voice.onnx"
                }
            }
            """;

        var settings = JsonSerializer.Deserialize<AppSettings>(json);

        Assert.NotNull(settings);
        Assert.NotNull(settings.Piper);
        Assert.Equal("/opt/models/voice.onnx", settings.Piper.ModelPath);
        Assert.Null(settings.Piper.ExecutablePath);
    }

    [Fact]
    public void Deserialize_NullValues_PropertiesRemainNull()
    {
        var json = """
            {
                "engine": null,
                "voice": null,
                "rate": null,
                "volume": null,
                "piper": null
            }
            """;

        var settings = JsonSerializer.Deserialize<AppSettings>(json);

        Assert.NotNull(settings);
        Assert.Null(settings.Engine);
        Assert.Null(settings.Voice);
        Assert.Null(settings.Rate);
        Assert.Null(settings.Volume);
        Assert.Null(settings.Piper);
    }
}
