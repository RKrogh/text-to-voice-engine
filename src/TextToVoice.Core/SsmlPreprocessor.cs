using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace TextToVoice.Core;

/// <summary>
/// Default SSML preprocessor. Parses SSML XML, extracts prosody hints,
/// converts break elements to pauses, and strips all other markup.
/// </summary>
public class SsmlPreprocessor : ISsmlPreprocessor
{
    private static readonly XmlReaderSettings SafeXmlSettings = new()
    {
        DtdProcessing = DtdProcessing.Prohibit,
        XmlResolver = null,
    };

    /// <inheritdoc />
    public SsmlPreprocessResult Preprocess(string ssml)
    {
        using var reader = XmlReader.Create(new StringReader(ssml), SafeXmlSettings);
        var doc = XDocument.Load(reader);
        var root =
            doc.Root ?? throw new InvalidOperationException("SSML document has no root element.");

        float? rateMultiplier = null;
        int? volume = null;
        string? voiceName = null;
        var textBuilder = new StringBuilder();

        ProcessElement(root, textBuilder, ref rateMultiplier, ref volume, ref voiceName);

        return new SsmlPreprocessResult(
            PlainText: textBuilder.ToString().Trim(),
            RateMultiplier: rateMultiplier,
            Volume: volume,
            VoiceName: voiceName
        );
    }

    private static void ProcessElement(
        XElement element,
        StringBuilder sb,
        ref float? rate,
        ref int? volume,
        ref string? voice
    )
    {
        var localName = element.Name.LocalName.ToLowerInvariant();

        switch (localName)
        {
            case "break":
                HandleBreak(element, sb);
                return; // break is self-closing, no children

            case "prosody":
                HandleProsody(element, ref rate, ref volume);
                break;

            case "voice":
                var nameAttr = element.Attribute("name");
                if (nameAttr != null)
                    voice = nameAttr.Value;
                break;
        }

        foreach (var node in element.Nodes())
        {
            if (node is XText textNode)
            {
                sb.Append(textNode.Value);
            }
            else if (node is XElement childElement)
            {
                ProcessElement(childElement, sb, ref rate, ref volume, ref voice);
            }
        }
    }

    private static void HandleBreak(XElement element, StringBuilder sb)
    {
        var timeAttr = element.Attribute("time");
        if (timeAttr != null)
        {
            var ms = ParseDuration(timeAttr.Value);
            sb.Append(ms > 500 ? "\n\n" : "... ");
        }
        else
        {
            sb.Append("... ");
        }
    }

    private static void HandleProsody(XElement element, ref float? rate, ref int? volume)
    {
        var rateAttr = element.Attribute("rate");
        if (rateAttr != null)
            rate = ParseRate(rateAttr.Value);

        var volumeAttr = element.Attribute("volume");
        if (volumeAttr != null)
            volume = ParseVolume(volumeAttr.Value);
    }

    private static int ParseDuration(string duration)
    {
        if (duration.EndsWith("ms", StringComparison.OrdinalIgnoreCase))
        {
            if (int.TryParse(duration[..^2], out var ms))
                return ms;
        }
        else if (duration.EndsWith("s", StringComparison.OrdinalIgnoreCase))
        {
            if (float.TryParse(duration[..^1], out var s))
                return (int)(s * 1000);
        }

        return 250; // default
    }

    private static float ParseRate(string rate)
    {
        return rate.ToLowerInvariant() switch
        {
            "x-slow" => 0.5f,
            "slow" => 0.75f,
            "medium" => 1.0f,
            "fast" => 1.5f,
            "x-fast" => 2.0f,
            _ when rate.EndsWith('%') && float.TryParse(rate[..^1], out var pct) => pct / 100f,
            _ => 1.0f,
        };
    }

    private static int ParseVolume(string vol)
    {
        return vol.ToLowerInvariant() switch
        {
            "silent" => 0,
            "x-soft" => 20,
            "soft" => 40,
            "medium" => 60,
            "loud" => 80,
            "x-loud" => 100,
            _ => 60,
        };
    }
}
