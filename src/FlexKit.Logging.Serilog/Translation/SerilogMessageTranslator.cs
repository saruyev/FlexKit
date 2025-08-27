using System.Text.RegularExpressions;
using FlexKit.Logging.Formatting.Translation;

namespace FlexKit.Logging.Serilog.Translation;

/// <summary>
/// The SerilogMessageTranslator class is a custom implementation of the DefaultMessageTranslator used
/// to translate and process log message templates.
/// </summary>
/// <remarks>
/// This class provides specific translation logic for adapting log message templates to be compatible
/// with and leveraging features of Serilog. It also ensures that any non-Serilog-specific syntax in a
/// message template is cleaned to avoid incompatibility or formatting issues.
/// </remarks>
public partial class SerilogMessageTranslator : DefaultMessageTranslator
{
    [GeneratedRegex(@"\{([^}:]+)")]
    private static partial Regex ParameterRegex();

    /// <summary>
    /// Translates and reorders parameters to match their usage in the specified template.
    /// </summary>
    /// <param name="parameters">
    /// The collection of parameters to be translated and reordered, where keys are parameter names.
    /// </param>
    /// <param name="currentTemplate">The template string containing placeholders for parameter names.</param>
    /// <returns>
    /// A dictionary containing the translated and reordered parameters that match the placeholders
    /// in the given template. Any parameters not used in the template will be excluded.
    /// </returns>
    public override IReadOnlyDictionary<string, object?> TranslateParameters(
        IReadOnlyDictionary<string, object?>? parameters,
        string currentTemplate)
    {
        if (parameters == null || string.IsNullOrEmpty(currentTemplate))
        {
            return parameters ?? new Dictionary<string, object?>();
        }

        currentTemplate = CleanSerilogFeatures(currentTemplate);

        // Extract parameter names in order they appear in a template
        var matches = ParameterRegex().Matches(currentTemplate);
        var orderedParams = new Dictionary<string, object?>();

        foreach (Match match in matches)
        {
            var paramName = match.Groups[1].Value;
            if (parameters.TryGetValue(paramName, out var value))
            {
                orderedParams[paramName] = value;
            }
        }

        var metadata = new Dictionary<string, object?>();

        foreach (var key in parameters.Keys.Except(orderedParams.Keys))
        {
            metadata[key] = parameters[key];
        }

        orderedParams["Metadata"] = metadata;

        return orderedParams;
    }

    /// <summary>
    /// Translates the provided message template by cleaning non-Serilog syntax and enhancing it with Serilog features.
    /// </summary>
    /// <param name="messageTemplate">The message template string to be translated. Can be null or empty.</param>
    /// <returns>A translated string formatted specifically to enhance compatibility with Serilog.</returns>
    public override string TranslateTemplate(string? messageTemplate)
    {
        if (string.IsNullOrEmpty(messageTemplate))
        {
            return string.Empty;
        }

        var template = messageTemplate;

        // 1. Clean other provider syntax (NLog, Log4Net, ZLogger) but NOT Serilog
        template = CleanNonSerilogFeatures(template);

        // 2. Enhance with Serilog features (only if not already present)
        template = EnhanceWithSerilogFeatures(template);

        return template;
    }

    /// <summary>
    /// Cleans non-Serilog-specific syntax from a given template.
    /// </summary>
    /// <param name="template">The template string containing non-Serilog-specific syntax to be cleaned.</param>
    /// <returns>A cleaned template string with only the basic {Property} format retained.</returns>
    private static string CleanNonSerilogFeatures(string template)
    {
        // Use inherited methods but skip Serilog cleaning
        template = CleanNLogFeatures(template);
        template = CleanLog4NetFeatures(template);
        template = CleanZLoggerFeatures(template);

        return template;
    }

    /// <summary>
    /// Enhances the given message template by adding Serilog-specific syntax and features
    /// for structured logging, provided no existing Serilog syntax is detected in the template.
    /// </summary>
    /// <param name="template">The message template string that needs enhancement. Cannot be null.</param>
    /// <returns>
    /// A string with added Serilog-specific enhancements, or the original string if Serilog syntax already exists.
    /// </returns>
    private static string EnhanceWithSerilogFeatures(string template)
    {
        // Only enhance if no existing Serilog syntax detected
        if (HasExistingSerilogSyntax(template))
        {
            return template;
        }

        // Add structured logging destructuring
        template = template.Replace("{InputParameters}", "{@InputParameters}");
        template = template.Replace("{OutputValue}", "{@OutputValue}");
        template = template.Replace("{Metadata}", "{@Metadata}");

        // Add format specifiers
        template = template.Replace("{Duration}", "{Duration:N2}");

        return template;
    }

    /// <summary>
    /// Determines whether the provided template already contains existing Serilog-specific syntax.
    /// </summary>
    /// <param name="template">The message template string to analyze for Serilog syntax. Cannot be null.</param>
    /// <returns>
    /// A boolean value indicating whether the template contains existing Serilog syntax.
    /// Returns true if the template includes Serilog-specific syntax patterns; otherwise, false.
    /// </returns>
    private static bool HasExistingSerilogSyntax(string template) =>
        template.Contains("{@") || template.Contains("{$") ||
        SerilogRegex().IsMatch(template);
}
