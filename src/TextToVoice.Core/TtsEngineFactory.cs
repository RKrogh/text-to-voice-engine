namespace TextToVoice.Core;

/// <summary>
/// Factory for creating TTS engines. Engines must be registered before use.
/// </summary>
public static class TtsEngineFactory
{
    private static readonly Dictionary<TtsEngineType, Func<ITtsEngine>> _factories = new();
    private static TtsEngineType _defaultType = TtsEngineType.Auto;

    /// <summary>
    /// Registers an engine factory for a specific type.
    /// </summary>
    public static void Register(TtsEngineType type, Func<ITtsEngine> factory)
    {
        _factories[type] = factory;
    }

    /// <summary>
    /// Sets the default engine type for auto-detection fallback.
    /// </summary>
    public static void SetDefault(TtsEngineType type)
    {
        _defaultType = type;
    }

    /// <summary>
    /// Creates an engine of the specified type.
    /// </summary>
    public static ITtsEngine Create(TtsEngineType type)
    {
        var resolvedType = type == TtsEngineType.Auto ? ResolveAutoType() : type;

        if (!_factories.TryGetValue(resolvedType, out var factory))
        {
            throw new InvalidOperationException(
                $"No engine registered for type '{resolvedType}'. "
                    + $"Call TtsEngineFactory.Register() first."
            );
        }

        return factory();
    }

    /// <summary>
    /// Creates an engine using the default/auto-detected type.
    /// </summary>
    public static ITtsEngine Create() => Create(TtsEngineType.Auto);

    /// <summary>
    /// Parses a string to engine type.
    /// </summary>
    public static TtsEngineType Parse(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return TtsEngineType.Auto;

        return name.ToLowerInvariant() switch
        {
            "auto" => TtsEngineType.Auto,
            "windows" => TtsEngineType.Windows,
            "piper" => TtsEngineType.Piper,
            "elevenlabs" => TtsEngineType.ElevenLabs,
            "sherpaonnx" or "sherpa-onnx" or "sherpa" or "onnx" => TtsEngineType.SherpaOnnx,
            _ => throw new ArgumentException($"Unknown engine type: '{name}'"),
        };
    }

    /// <summary>
    /// Returns available registered engine types.
    /// </summary>
    public static IEnumerable<TtsEngineType> GetAvailableTypes() => _factories.Keys;

    private static TtsEngineType ResolveAutoType()
    {
        if (_defaultType != TtsEngineType.Auto)
            return _defaultType;

        // Platform detection
        if (OperatingSystem.IsWindows() && _factories.ContainsKey(TtsEngineType.Windows))
            return TtsEngineType.Windows;

        if (_factories.ContainsKey(TtsEngineType.Piper))
            return TtsEngineType.Piper;

        // Return first available
        return _factories.Keys.FirstOrDefault(t => t != TtsEngineType.Auto);
    }
}
