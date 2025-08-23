using System.Text.RegularExpressions;
using JetBrains.Annotations;

namespace FlexKit.Logging.Formatting.Translation;

/// <summary>
/// Message translator for Microsoft.Extensions.Logging (MEL) provider.
/// Provides pass-through translation since MEL uses the standard FlexKit template syntax.
/// </summary>
/// <remarks>
/// <para>
/// This translator serves as the default implementation and handles the baseline
/// template syntax used by FlexKit.Logging. Since MEL uses standard placeholder
/// syntax with curly braces, most operations are pass-through.
/// </para>
/// <para>
/// <strong>Template Syntax:</strong>
/// MEL uses the same template syntax as FlexKit templates:
/// <list type="bullet">
/// <item>"{MethodName}" - Standard parameter placeholder</item>
/// <item>"{Duration:N2}" - Parameter with format specifier</item>
/// <item>"{Success}" - Boolean parameter</item>
/// <item>"{@Object}" - Structured logging (supported by MEL)</item>
/// </list>
/// </para>
/// <para>
/// <strong>Performance:</strong>
/// This implementation prioritizes performance with minimal overhead,
/// as it serves as the fallback translator for all FlexKit.Logging operations.
/// </para>
/// </remarks>
[UsedImplicitly]
public partial class DefaultMessageTranslator : IMessageTranslator
{
    [GeneratedRegex(@"\{([^}]+):[^}]+\}")]
    private static partial Regex SerilogRegex();
    [GeneratedRegex(@"\$\{([^}]+)\}")]
    private static partial Regex NlogRenderRegex();
    [GeneratedRegex(@"\$\{when:[^}]+\}")]
    private static partial Regex NlogConditionalRegex();
    [GeneratedRegex(@"\$\{var:[^}]+\}")]
    private static partial Regex NlogVariablesRegex();
    [GeneratedRegex(@"%([a-zA-Z]+)")]
    private static partial Regex Log4NetRegex();
    [GeneratedRegex(@"%property\{([^}]+)\}")]
    private static partial Regex Log4NetPropertyRegex();
    [GeneratedRegex(@"%date\{[^}]+\}")]
    private static partial Regex Log4NetDateRegex();

    /// <inheritdoc />
    public string TranslateTemplate(string? messageTemplate) =>
        CleanNotSupportedFeatures(messageTemplate ?? string.Empty);

    /// <inheritdoc />
    public IReadOnlyDictionary<string, object?> TranslateParameters(
        IReadOnlyDictionary<string, object?>? parameters) =>
        parameters ?? new Dictionary<string, object?>();

    /// <inheritdoc />
    public bool CanTranslate(
        string? messageTemplate,
        IReadOnlyDictionary<string, object?> parameters) => true;

    /// <summary>
    /// Removes provider-specific features from a log message template, leaving only the basic property format.
    /// </summary>
    /// <param name="template">The string template to be cleaned of provider-specific syntax.</param>
    /// <returns>A cleaned template string with only the basic {Property} format retained.</returns>
    private static string CleanNotSupportedFeatures(string template)
    {
        // Clean all provider-specific syntax, leaving only the basic {Property} format

        // 1. Serilog features
        template = CleanSerilogFeatures(template);

        // 2. NLog features
        template = CleanNLogFeatures(template);

        // 3. Log4Net features
        template = CleanLog4NetFeatures(template);

        // 4. ZLogger features
        template = CleanZLoggerFeatures(template);

        return template;
    }

    /// <summary>
    /// Cleans the provided template by removing Serilog-specific formatting features, such as
    /// destructuring and format specifiers.
    /// </summary>
    /// <param name="template">The message template string to be cleaned.</param>
    /// <returns>A cleaned string with only basic property formatting supported.</returns>
    [UsedImplicitly]
    protected static string CleanSerilogFeatures(string template)
    {
        // Remove destructuring: {@Object} → {Object}
        template = template.Replace("{@", "{").Replace("{$", "{");

        // Remove format specifiers: {Duration:N2} → {Duration}
        template = SerilogRegex().Replace(template, "{$1}");

        return template;
    }

    /// <summary>
    /// Removes NLog-specific syntax from a given template and converts supported layout
    /// renderers to a compatible format.
    /// </summary>
    /// <param name="template">The template string containing NLog-specific syntax to be cleaned.</param>
    /// <returns>
    /// The cleaned template with unsupported NLog features removed and supported renderers converted.
    /// </returns>
    [UsedImplicitly]
    protected static string CleanNLogFeatures(string template)
    {
        // Convert NLog layout renderers: ${property} → {property}
        template = NlogRenderRegex().Replace(template, "{$1}");

        // Remove NLog-specific renderers that don't map to FlexKit
        template = NlogConditionalRegex().Replace(template, ""); // Conditional
        template = NlogVariablesRegex().Replace(template, "");  // Variables

        return template;
    }

    /// <summary>
    /// Cleans Log4Net-specific features from the provided logging template,
    /// converting Log4Net syntax into a standardized format.
    /// </summary>
    /// <param name="template">The logging template that may contain Log4Net-specific syntax.</param>
    /// <returns>
    /// A string with Log4Net-specific features converted to a standardized format.
    /// </returns>
    [UsedImplicitly]
    protected static string CleanLog4NetFeatures(string template)
    {
        // Convert Log4Net patterns: %property → {property}
        template = Log4NetRegex().Replace(template, "{$1}");

        // Convert property syntax: %property{Name} → {Name}
        template = Log4NetPropertyRegex().Replace(template, "{$1}");

        // Remove Log4Net date formatting: %date{format} → {Timestamp}
        template = Log4NetDateRegex().Replace(template, "{Timestamp}");

        return template;
    }

    /// <summary>
    /// Cleans ZLogger-specific syntax from the provided template, ensuring compatibility with
    /// standard logging message formatting conventions.
    /// </summary>
    /// <param name="template">The message template containing potential ZLogger-specific syntax elements.</param>
    /// <returns>A cleaned message template where ZLogger-specific syntax has been removed.</returns>
    [UsedImplicitly]
    protected static string CleanZLoggerFeatures(string template)
    {
        // ZLogger is mostly compatible, just clean special features
        template = template.Replace("{@", "{");     // JSON serialization
        template = template.Replace("{raw}", "{}"); // Raw output

        return template;
    }
}
