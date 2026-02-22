using System.CommandLine;
using Microsoft.Extensions.Configuration;
using TextToVoice.Apps.Console;
using TextToVoice.Core;
using TextToVoice.Engines.ElevenLabs;
using TextToVoice.Engines.Piper;
using TextToVoice.Engines.SherpaOnnx;
using TextToVoice.Engines.Windows;

// Build configuration: appsettings.json → appsettings.{env}.json → user secrets → env vars
// CLI args (System.CommandLine) override everything in the handler below.
var environment = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ?? "Production";
var config = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: true)
    .AddJsonFile($"appsettings.{environment}.json", optional: true)
    .AddUserSecrets(typeof(AppSettings).Assembly, optional: true)
    .AddEnvironmentVariables("TTV_")
    .Build();

var settings = config.Get<AppSettings>() ?? new AppSettings();

// Register available engines
if (OperatingSystem.IsWindows())
{
#pragma warning disable CA1416 // Platform compatibility - guarded by IsWindows()
    TtsEngineFactory.Register(TtsEngineType.Windows, () => new SystemSpeechEngine());
#pragma warning restore CA1416
}

// Piper registration requires model path, handled in command handler

var textArgument = new Argument<string?>(
    name: "text",
    description: "The text to speak",
    getDefaultValue: () => null
);

var outputOption = new Option<string?>(
    aliases: ["-o", "--output"],
    description: "Save audio to file instead of playing"
);

var voiceOption = new Option<string?>(
    aliases: ["-v", "--voice"],
    description: "Voice to use for speech"
);

var rateOption = new Option<int>(
    aliases: ["-r", "--rate"],
    getDefaultValue: () => 0,
    description: "Speech rate (-10 to 10)"
);

var volumeOption = new Option<int>(
    aliases: ["--volume"],
    getDefaultValue: () => 100,
    description: "Volume (0 to 100)"
);

var listVoicesOption = new Option<bool>(
    aliases: ["--list-voices"],
    description: "List available voices"
);

var engineOption = new Option<string?>(
    aliases: ["-e", "--engine"],
    description: "TTS engine to use (auto, windows, piper, sherpaonnx, elevenlabs)"
);

var modelOption = new Option<string?>(
    aliases: ["-m", "--model"],
    description: "Path to Piper model file (.onnx) - required for Piper engine"
);

var piperPathOption = new Option<string?>(
    aliases: ["--piper-path"],
    description: "Path to the Piper executable (defaults to 'piper' in PATH)"
);

var tokensPathOption = new Option<string?>(
    aliases: ["--tokens-path"],
    description: "Path to tokens.txt file for sherpa-onnx engine"
);

var dataDirOption = new Option<string?>(
    aliases: ["--data-dir"],
    description: "Path to espeak-ng-data directory for sherpa-onnx engine"
);

var leadingSilenceOption = new Option<int?>(
    aliases: ["--leading-silence"],
    description: "Milliseconds of silence before playback to prevent clipping (default: 150, 0 to disable)"
);

var apiKeyOption = new Option<string?>(
    aliases: ["--api-key"],
    description: "API key for ElevenLabs engine (also: settings elevenlabs.apiKey or ELEVENLABS_API_KEY env var)"
);

var ssmlOption = new Option<bool>(
    aliases: ["--ssml"],
    description: "Treat input as SSML markup (auto-detected if input starts with <speak>)"
);

var rootCommand = new RootCommand("Text-to-voice synthesizer")
{
    textArgument,
    outputOption,
    voiceOption,
    rateOption,
    volumeOption,
    listVoicesOption,
    engineOption,
    modelOption,
    piperPathOption,
    tokensPathOption,
    dataDirOption,
    leadingSilenceOption,
    apiKeyOption,
    ssmlOption,
};

rootCommand.SetHandler(
    async (context) =>
    {
        var parseResult = context.ParseResult;

        // Merge CLI args with settings — CLI takes precedence
        var text = parseResult.GetValueForArgument(textArgument);
        var output = parseResult.GetValueForOption(outputOption);
        var listVoices = parseResult.GetValueForOption(listVoicesOption);

        var engineName = parseResult.FindResultFor(engineOption) is not null
            ? parseResult.GetValueForOption(engineOption)
            : settings.Engine;

        var voice = parseResult.FindResultFor(voiceOption) is not null
            ? parseResult.GetValueForOption(voiceOption)
            : settings.Voice;

        var rate = parseResult.FindResultFor(rateOption) is not null
            ? parseResult.GetValueForOption(rateOption)
            : settings.Rate ?? 0;

        var volume = parseResult.FindResultFor(volumeOption) is not null
            ? parseResult.GetValueForOption(volumeOption)
            : settings.Volume ?? 100;

        var modelPath = parseResult.FindResultFor(modelOption) is not null
            ? parseResult.GetValueForOption(modelOption)
            : settings.Piper?.ModelPath;

        var piperPath = parseResult.FindResultFor(piperPathOption) is not null
            ? parseResult.GetValueForOption(piperPathOption)
            : settings.Piper?.ExecutablePath;

        var tokensPath = parseResult.FindResultFor(tokensPathOption) is not null
            ? parseResult.GetValueForOption(tokensPathOption)
            : settings.SherpaOnnx?.TokensPath;

        var dataDir = parseResult.FindResultFor(dataDirOption) is not null
            ? parseResult.GetValueForOption(dataDirOption)
            : settings.SherpaOnnx?.DataDir;

        var leadingSilenceMs = parseResult.FindResultFor(leadingSilenceOption) is not null
            ? parseResult.GetValueForOption(leadingSilenceOption) ?? 150
            : settings.LeadingSilenceMs ?? 150;

        // Resolve ElevenLabs API key: CLI → settings → env var
        var apiKey = parseResult.FindResultFor(apiKeyOption) is not null
            ? parseResult.GetValueForOption(apiKeyOption)
            : settings.ElevenLabs?.ApiKey
                ?? Environment.GetEnvironmentVariable("ELEVENLABS_API_KEY");

        // Resolve sherpa-onnx model path: dedicated setting or shared --model
        var sherpaModelPath = settings.SherpaOnnx?.ModelPath;

        var engineType = TtsEngineFactory.Parse(engineName);

        // Register Piper if model provided or Piper explicitly requested
        if (!string.IsNullOrEmpty(modelPath))
        {
            TtsEngineFactory.Register(
                TtsEngineType.Piper,
                () =>
                    new PiperEngine(
                        new PiperOptions
                        {
                            ModelPath = modelPath,
                            ExecutablePath = piperPath,
                            LeadingSilenceMs = leadingSilenceMs,
                        }
                    )
            );
        }

        // Validate Piper has model if explicitly requested
        if (engineType == TtsEngineType.Piper && string.IsNullOrEmpty(modelPath))
        {
            Console.Error.WriteLine("Error: Piper engine requires --model path to .onnx file");
            Console.Error.WriteLine(
                "Provide --model on the command line or set piper.modelPath in settings.json"
            );
            context.ExitCode = 1;
            return;
        }

        // Register SherpaOnnx if model provided (--model flag or sherpaOnnx.modelPath in settings)
        var sherpaModel =
            engineType == TtsEngineType.SherpaOnnx
                ? (modelPath ?? sherpaModelPath)
                : sherpaModelPath;

        if (!string.IsNullOrEmpty(sherpaModel))
        {
            TtsEngineFactory.Register(
                TtsEngineType.SherpaOnnx,
                () =>
                    new SherpaOnnxEngine(
                        new SherpaOnnxOptions
                        {
                            ModelPath = sherpaModel,
                            TokensPath = tokensPath,
                            DataDir = dataDir,
                            LeadingSilenceMs = leadingSilenceMs,
                        }
                    )
            );
        }

        // Validate SherpaOnnx has model if explicitly requested
        if (engineType == TtsEngineType.SherpaOnnx && string.IsNullOrEmpty(sherpaModel))
        {
            Console.Error.WriteLine("Error: SherpaOnnx engine requires --model path to .onnx file");
            Console.Error.WriteLine(
                "Provide --model on the command line or set sherpaOnnx.modelPath in settings.json"
            );
            context.ExitCode = 1;
            return;
        }

        // Register ElevenLabs if API key is available
        if (!string.IsNullOrEmpty(apiKey))
        {
            TtsEngineFactory.Register(
                TtsEngineType.ElevenLabs,
                () =>
                    new ElevenLabsEngine(
                        new ElevenLabsOptions
                        {
                            ApiKey = apiKey,
                            VoiceId = settings.ElevenLabs?.VoiceId ?? "21m00Tcm4TlvDq8ikWAM",
                            ModelId = settings.ElevenLabs?.ModelId ?? "eleven_multilingual_v2",
                            LeadingSilenceMs = leadingSilenceMs,
                        }
                    )
            );
        }

        // Validate ElevenLabs has API key if explicitly requested
        if (engineType == TtsEngineType.ElevenLabs && string.IsNullOrEmpty(apiKey))
        {
            Console.Error.WriteLine("Error: ElevenLabs engine requires an API key");
            Console.Error.WriteLine(
                "Provide --api-key, set elevenlabs.apiKey in settings.json, or set ELEVENLABS_API_KEY env var"
            );
            context.ExitCode = 1;
            return;
        }

        ITtsEngine engine;
        try
        {
            engine = TtsEngineFactory.Create(engineType);
        }
        catch (InvalidOperationException ex)
        {
            Console.Error.WriteLine($"Error: {ex.Message}");
            Console.Error.WriteLine(
                "Available engines: " + string.Join(", ", TtsEngineFactory.GetAvailableTypes())
            );
            context.ExitCode = 1;
            return;
        }

        using (engine)
        {
            if (listVoices)
            {
                var voices = engine.GetAvailableVoices();
                Console.WriteLine("Available voices:");
                Console.WriteLine();
                foreach (var v in voices)
                {
                    Console.WriteLine($"  {v.Name}");
                    Console.WriteLine($"    Culture: {v.Culture}");
                    Console.WriteLine($"    Gender:  {v.Gender}");
                    Console.WriteLine();
                }
                return;
            }

            if (string.IsNullOrWhiteSpace(text))
            {
                Console.Error.WriteLine(
                    "Error: No text provided. Use --help for usage information."
                );
                context.ExitCode = 1;
                return;
            }

            if (!string.IsNullOrEmpty(voice))
            {
                try
                {
                    engine.SetVoice(voice);
                }
                catch (NotSupportedException)
                {
                    Console.Error.WriteLine(
                        $"Warning: This engine doesn't support voice switching at runtime."
                    );
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine(
                        $"Error: Could not select voice '{voice}': {ex.Message}"
                    );
                    context.ExitCode = 1;
                    return;
                }
            }

            engine.SetRate(rate);
            engine.SetVolume(volume);

            // Detect SSML input
            var isSsml = parseResult.GetValueForOption(ssmlOption);
            if (!isSsml)
                isSsml = SsmlDetector.IsSsml(text);

            try
            {
                if (isSsml && engine is ISsmlCapable ssmlEngine)
                {
                    if (!string.IsNullOrEmpty(output))
                    {
                        await ssmlEngine.SaveSsmlToFileAsync(text, output);
                        Console.WriteLine($"Audio saved to: {output}");
                    }
                    else
                    {
                        await ssmlEngine.SpeakSsmlAsync(text);
                    }
                }
                else if (isSsml)
                {
                    Console.Error.WriteLine(
                        "Warning: Selected engine does not support SSML. Speaking as plain text."
                    );
                    if (!string.IsNullOrEmpty(output))
                    {
                        await engine.SaveToFileAsync(text, output);
                        Console.WriteLine($"Audio saved to: {output}");
                    }
                    else
                    {
                        await engine.SpeakAsync(text);
                    }
                }
                else
                {
                    if (!string.IsNullOrEmpty(output))
                    {
                        await engine.SaveToFileAsync(text, output);
                        Console.WriteLine($"Audio saved to: {output}");
                    }
                    else
                    {
                        await engine.SpeakAsync(text);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error: {ex.Message}");
                context.ExitCode = 1;
            }
        }
    }
);

return await rootCommand.InvokeAsync(args);
