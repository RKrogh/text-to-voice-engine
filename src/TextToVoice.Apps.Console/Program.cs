using System.CommandLine;
using TextToVoice.Engines.Windows;

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

var rootCommand = new RootCommand("Text-to-voice synthesizer")
{
    textArgument,
    outputOption,
    voiceOption,
    rateOption,
    volumeOption,
    listVoicesOption,
};

rootCommand.SetHandler(
    async (text, output, voice, rate, volume, listVoices) =>
    {
        using var engine = new SystemSpeechEngine();

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
            Environment.ExitCode = 1;
            return;
        }

        if (!string.IsNullOrEmpty(voice))
        {
            try
            {
                engine.SetVoice(voice);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error: Could not select voice '{voice}': {ex.Message}");
                Environment.ExitCode = 1;
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
            Environment.ExitCode = 1;
        }
    },
    textArgument,
    outputOption,
    voiceOption,
    rateOption,
    volumeOption,
    listVoicesOption
);

return await rootCommand.InvokeAsync(args);
