namespace TextToVoice.Core;

/// <summary>
/// Shared helper for engines that preprocess SSML to plain text.
/// Handles the save-state → preprocess → apply → execute → restore-state lifecycle.
/// </summary>
public static class SsmlHelper
{
    /// <summary>
    /// Preprocesses SSML, applies extracted parameters, executes an async action with the plain text,
    /// then restores original engine state.
    /// </summary>
    /// <param name="preprocessor">The SSML preprocessor to use.</param>
    /// <param name="ssml">The SSML markup to preprocess.</param>
    /// <param name="applyResult">Called with the preprocessing result to apply rate/volume/voice changes.</param>
    /// <param name="execute">The async action to run with the extracted plain text.</param>
    /// <param name="restoreState">Called in finally to restore original engine state.</param>
    public static async Task ExecuteWithPreprocessingAsync(
        ISsmlPreprocessor preprocessor,
        string ssml,
        Action<SsmlPreprocessResult> applyResult,
        Func<string, Task> execute,
        Action restoreState)
    {
        try
        {
            var result = preprocessor.Preprocess(ssml);
            applyResult(result);
            await execute(result.PlainText);
        }
        finally
        {
            restoreState();
        }
    }

    /// <summary>
    /// Preprocesses SSML, applies extracted parameters, executes an async function with the plain text,
    /// then restores original engine state.
    /// </summary>
    public static async Task<T> ExecuteWithPreprocessingAsync<T>(
        ISsmlPreprocessor preprocessor,
        string ssml,
        Action<SsmlPreprocessResult> applyResult,
        Func<string, Task<T>> execute,
        Action restoreState)
    {
        try
        {
            var result = preprocessor.Preprocess(ssml);
            applyResult(result);
            return await execute(result.PlainText);
        }
        finally
        {
            restoreState();
        }
    }
}
