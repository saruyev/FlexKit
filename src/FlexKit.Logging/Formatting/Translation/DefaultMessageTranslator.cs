using System.Text.RegularExpressions;
using FlexKit.Logging.Configuration;
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
    /// <summary>
    /// A compiled regular expression for matching Serilog message templates with destructuring or format specifiers.
    /// </summary>
    /// <returns>
    /// A <see cref="Regex"/> instance to match and identify patterns in Serilog message templates.
    /// </returns>
    [UsedImplicitly]
    [GeneratedRegex(@"\{([^}]+):[^}]+\}")]
    protected static partial Regex SerilogRegex();

    /// <summary>
    /// A compiled regular expression for matching NLog layout renderers in the format
    /// of ${property} or other renderers.
    /// </summary>
    /// <returns>
    /// A <see cref="Regex"/> instance to identify and process NLog renderer patterns in log message templates.
    /// </returns>
    [UsedImplicitly]
    [GeneratedRegex(@"\$\{([^}]+)\}")]
    protected static partial Regex NlogRenderRegex();

    /// <summary>
    /// A compiled regular expression for matching NLog conditional renderers within message templates.
    /// </summary>
    /// <returns>
    /// A <see cref="Regex"/> instance to identify and process NLog conditional patterns like `${when:...}`.
    /// </returns>
    [UsedImplicitly]
    [GeneratedRegex(@"\{when:[^}]+\}")]
    protected static partial Regex NlogConditionalRegex();

    /// <summary>
    /// A compiled regular expression for matching NLog variable layout renderers in message templates,
    /// such as `${var:...}` patterns.
    /// </summary>
    /// <returns>
    /// A <see cref="Regex"/> instance to locate and identify variable renderers in NLog message templates.
    /// </returns>
    [UsedImplicitly]
    [GeneratedRegex(@"\{var:[^}]+\}")]
    protected static partial Regex NlogVariablesRegex();

    /// <inheritdoc />
    public virtual string TranslateTemplate(
        string? messageTemplate,
        LoggingConfig? config = null) =>
        CleanNotSupportedFeatures(messageTemplate ?? string.Empty);

    /// <inheritdoc />
    public virtual IReadOnlyDictionary<string, object?> TranslateParameters(
        IReadOnlyDictionary<string, object?>? parameters,
        string currentTemplate) =>
        parameters ?? new Dictionary<string, object?>();

    /// <summary>
    /// Removes provider-specific features from a log message template, leaving only the basic property format.
    /// </summary>
    /// <param name="template">The string template to be cleaned of provider-specific syntax.</param>
    /// <returns>A cleaned template string with only the basic {Property} format retained.</returns>
    private static string CleanNotSupportedFeatures(string template)
    {
        // Clean all provider-specific syntax, leaving only the basic {Property} format
        template = CleanNLogFeatures(template);
        template = CleanSerilogFeatures(template);

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
        template = template.Replace("{@", "{").Replace("{$", "{");
        template = NlogRenderRegex().Replace(template, "{$1}");

        // Remove NLog-specific renderers that don't map to FlexKit
        template = NlogConditionalRegex().Replace(template, ""); // Conditional
        template = NlogVariablesRegex().Replace(template, m => "{" + m.Value[5..^1] + "}");

        return template;
    }

    /// <summary>
    /// Orders the parameters based on the sequence they appear in the provided template
    /// while also appending metadata for the remaining unmatched parameters.
    /// </summary>
    /// <param name="parameters">A read-only dictionary containing the parameters to be ordered.</param>
    /// <param name="currentTemplate">The template string used to determine the parameter ordering.</param>
    /// <param name="parameterRegex">
    /// The regular expression used to identify parameter names within the template.
    /// </param>
    /// <returns>
    /// A dictionary with the ordered parameters and an additional "Metadata" entry for unmatched parameters.
    /// </returns>
    [UsedImplicitly]
    protected static Dictionary<string, object?> OrderForTemplate(
        IReadOnlyDictionary<string, object?> parameters,
        string currentTemplate,
        Regex parameterRegex)
    {
        // Extract parameter names in order they appear in a template
        var matches = parameterRegex.Matches(currentTemplate);
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
}
