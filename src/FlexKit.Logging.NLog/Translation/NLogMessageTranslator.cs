using System.Text.RegularExpressions;
using FlexKit.Logging.Configuration;
using FlexKit.Logging.Formatting.Translation;

namespace FlexKit.Logging.NLog.Translation;

/// <summary>
/// Message translator for NLog provider that converts FlexKit templates to NLog layout renderers.
/// Converts FlexKit template syntax like "{MethodName}" to NLog syntax like "${methodName}".
/// </summary>
/// <remarks>
/// <para>
/// This translator handles the conversion from FlexKit's standard template format to NLog's
/// layout renderer syntax. NLog uses ${property} syntax instead of {Property} format.
/// </para>
/// <para>
/// <strong>Template Conversion Examples:</strong>
/// <list type="bullet">
/// <item>"{MethodName}" → "${methodName}"</item>
/// <item>"{TypeName}" → "${typeName}"</item>
/// <item>"{Duration}" → "${duration}"</item>
/// <item>"{Success}" → "${success}"</item>
/// <item>"{InputParameters}" → "${inputParameters}"</item>
/// <item>"{OutputValue}" → "${outputValue}"</item>
/// </list>
/// </para>
/// <para>
/// <strong>Parameter Handling:</strong>
/// NLog handles structured logging differently than Serilog. Parameters are converted
/// to NLog's event-properties and accessed using ${event-properties:item=propertyName} syntax.
/// However, for simplicity and compatibility, we use the direct ${propertyName} format.
/// </para>
/// </remarks>
public partial class NLogMessageTranslator : DefaultMessageTranslator
{
    [GeneratedRegex(@"\{([^}]+)\}")]
    private static partial Regex ParameterRegex();

    /// <inheritdoc />
    public override string TranslateTemplate(string? messageTemplate, LoggingConfig? config = null)
    {
        if (string.IsNullOrEmpty(messageTemplate))
        {
            return string.Empty;
        }

        var template = messageTemplate;
        template = CleanNonNLogFeatures(template);

        return config is { DefaultFormatter: FormatterType.Json, Formatters.Json.PrettyPrint: true } ? template :
            ConvertToNLogSyntax(template);
    }

    /// <summary>
    /// Translates parameter names to NLog-compatible format and ensures they match the template.
    /// NLog parameters are typically camelCase and accessed as event properties.
    /// </summary>
    /// <param name="parameters">The parameters dictionary from FlexKit logging.</param>
    /// <param name="currentTemplate">The template string containing parameter references.</param>
    /// <returns>A dictionary with NLog-compatible parameter names and values.</returns>
    /// <remarks>
    /// <para>
    /// This method ensures parameter names match the layout renderer references in the template.
    /// It converts PascalCase parameter names to camelCase to match NLog conventions.
    /// </para>
    /// <para>
    /// NLog accesses parameters through event properties, so we ensure the parameter names
    /// in the dictionary match what the layout renderers expect.
    /// </para>
    /// </remarks>
    public override IReadOnlyDictionary<string, object?> TranslateParameters(
        IReadOnlyDictionary<string, object?>? parameters,
        string currentTemplate) =>
        parameters == null || string.IsNullOrEmpty(currentTemplate)
            ? parameters ?? new Dictionary<string, object?>()
            : OrderForTemplate(
                parameters,
                CleanNLogFeatures(currentTemplate),
                ParameterRegex());

    /// <summary>
    /// Cleans non-NLog-specific syntax from the template while preserving existing NLog syntax.
    /// </summary>
    /// <param name="template">The template to clean.</param>
    /// <returns>A template with non-NLog syntax removed.</returns>
    private static string CleanNonNLogFeatures(string template) => CleanSerilogFeatures(template);

    /// <summary>
    /// Converts FlexKit template syntax {Property} to NLog layout renderer syntax ${property}.
    /// </summary>
    /// <param name="template">The FlexKit template to convert.</param>
    /// <returns>The template converted to NLog syntax.</returns>
    private static string ConvertToNLogSyntax(string template)
    {
        // Convert {Property} to ${property} with camelCase conversion
        return ParameterRegex().Replace(template, match =>
        {
            var propertyName = match.Groups[1].Value;

            // Skip if it's already NLog syntax (starts with $)
            return propertyName.StartsWith('$') ? match.Value : $"{{@{propertyName}}}";
        });
    }
}
