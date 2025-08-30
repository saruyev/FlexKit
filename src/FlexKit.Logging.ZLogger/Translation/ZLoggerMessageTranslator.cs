using System.Text.RegularExpressions;
using FlexKit.Logging.Formatting.Translation;

namespace FlexKit.Logging.ZLogger.Translation;

/// <summary>
/// Translates log message parameters to align them with the expected template format.
/// This implementation extends the DefaultMessageTranslator class and provides
/// specific logic to clean and reorder parameters for logs created using ZLogger.
/// </summary>
public partial class ZLoggerMessageTranslator : DefaultMessageTranslator
{
    [GeneratedRegex(@"\{([^}]+)\}")]
    private static partial Regex ParameterRegex();

    /// <inheritdoc />
    public override IReadOnlyDictionary<string, object?> TranslateParameters(
        IReadOnlyDictionary<string, object?>? parameters,
        string currentTemplate) =>
        parameters == null || string.IsNullOrEmpty(currentTemplate)
            ? parameters ?? new Dictionary<string, object?>()
            : OrderForTemplate(
                parameters,
                currentTemplate,
                ParameterRegex());
}
