using System.CommandLine;
using TextToVoice.Core;
using TextToVoice.Engines.Piper;
using TextToVoice.Engines.Windows;

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
    description: "TTS engine to use (auto, windows, piper)"
);

var modelOption = new Option<string?>(
    aliases: ["-m", "--model"],
    description: "Path to Piper model file (.onnx) - required for Piper engine"
);

var piperPathOption = new Option<string?>(
    aliases: ["--piper-path"],
    description: "Path to the Piper executable (defaults to 'piper' in PATH)"
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
};

rootCommand.SetHandler(async (context) =>
{
    var text = context.ParseResult.GetValueForArgument(textArgument);
    var output = context.ParseResult.GetValueForOption(outputOption);
    var voice = context.ParseResult.GetValueForOption(voiceOption);
    var rate = context.ParseResult.GetValueForOption(rateOption);
    var volume = context.ParseResult.GetValueForOption(volumeOption);
    var listVoices = context.ParseResult.GetValueForOption(listVoicesOption);
    var engineName = context.ParseResult.GetValueForOption(engineOption);
    var modelPath = context.ParseResult.GetValueForOption(modelOption);
    var piperPath = context.ParseResult.GetValueForOption(piperPathOption);

    var engineType = TtsEngineFactory.Parse(engineName);

    // Register Piper if model provided or Piper explicitly requested
    if (!string.IsNullOrEmpty(modelPath))
    {
        TtsEngineFactory.Register(TtsEngineType.Piper, () => new PiperEngine(
            new PiperOptions { ModelPath = modelPath, ExecutablePath = piperPath }));
    }

    // Validate Piper has model if explicitly requested
    if (engineType == TtsEngineType.Piper && string.IsNullOrEmpty(modelPath))
    {
        Console.Error.WriteLine("Error: Piper engine requires --model path to .onnx file");
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
        Console.Error.WriteLine("Available engines: " +
            string.Join(", ", TtsEngineFactory.GetAvailableTypes()));
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
            Console.Error.WriteLine("Error: No text provided. Use --help for usage information.");
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
                Console.Error.WriteLine($"Warning: This engine doesn't support voice switching at runtime.");
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error: Could not select voice '{voice}': {ex.Message}");
                context.ExitCode = 1;
                return;
            }
        }

        engine.SetRate(rate);
        engine.SetVolume(volume);

        try
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
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error: {ex.Message}");
            context.ExitCode = 1;
        }
    }
});

return await rootCommand.InvokeAsync(args);
