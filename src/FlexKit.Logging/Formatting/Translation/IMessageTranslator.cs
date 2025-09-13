using FlexKit.Logging.Configuration;

namespace FlexKit.Logging.Formatting.Translation;

/// <summary>
/// Defines the contract for translating message templates and parameters between different logging provider syntaxes.
/// Enables cross-provider compatibility by converting FlexKit templates to provider-specific formats.
/// </summary>
/// <remarks>
/// <para>
/// Message translators handle the syntax differences between logging providers:
/// <list type="bullet">
/// <item>MEL (Microsoft.Extensions.Logging): Uses standard template syntax like "{MethodName}"</item>
/// <item>Serilog: May use destructuring operators like "{@Parameters}" (future implementation)</item>
/// <item>NLog: Uses layout renderers like "${methodName}" (future implementation)</item>
/// <item>Log4Net: Uses property patterns like "%property{MethodName}" (future implementation)</item>
/// </list>
/// </para>
/// <para>
/// <strong>Provider Discovery:</strong>
/// Future FlexKit.Logging provider packages will implement this interface and be auto-discovered
/// by the FlexKit dependency injection system, allowing seamless provider-specific formatting.
/// </para>
/// <para>
/// <strong>Template Processing:</strong>
/// Translators should handle:
/// <list type="bullet">
/// <item>Parameter name conversion (case sensitivity, naming conventions)</item>
/// <item>Special formatting directives (destructuring, layout renderers)</item>
/// <item>Provider-specific escape sequences and special characters</item>
/// <item>Parameter type hints and formatting instructions</item>
/// </list>
/// </para>
/// </remarks>
public interface IMessageTranslator
{
    /// <summary>
    /// Translates a message template from FlexKit format to the target provider's syntax.
    /// </summary>
    /// <param name="messageTemplate">The message template in FlexKit format using standard placeholder syntax.</param>
    /// <param name="config">The FlexKit logging configuration.</param>
    /// <returns>The translated template in the target provider's syntax.</returns>
    /// <remarks>
    /// <para>
    /// This method converts template placeholders from the standard FlexKit format to
    /// provider-specific syntax. For example:
    /// <list type="bullet">
    /// <item>Input: "Method {MethodName} completed in {Duration}ms"</item>
    /// <item>MEL Output: "Method {MethodName} completed in {Duration}ms" (unchanged)</item>
    /// <item>NLog Output: "Method ${MethodName} completed in ${Duration}ms" (future)</item>
    /// <item>Serilog Output: "Method {MethodName} completed in {Duration}ms" (future, may use destructuring)</item>
    /// </list>
    /// </para>
    /// <para>
    /// <strong>Error Handling:</strong>
    /// If translation fails, implementations should return the original template
    /// rather than throwing exceptions, ensuring graceful degradation.
    /// </para>
    /// </remarks>
    string TranslateTemplate(
        string? messageTemplate,
        LoggingConfig? config = null);

    /// <summary>
    /// Translates parameter names and values for provider-specific formatting requirements.
    /// </summary>
    /// <param name="parameters">Dictionary of parameter names and their values.</param>
    /// <param name="currentTemplate">The template string containing placeholders for parameter names.</param>
    /// <returns>Translated parameters with provider-specific names and formatting.</returns>
    /// <remarks>
    /// <para>
    /// This method handles parameter-level translations that may be required by specific providers:
    /// <list type="bullet">
    /// <item>Parameter name case conversion (camelCase vs PascalCase)</item>
    /// <item>Provider-specific parameter prefixes or suffixes</item>
    /// <item>Value type formatting and conversion</item>
    /// <item>Special parameter handling for structured logging</item>
    /// </list>
    /// </para>
    /// <para>
    /// Most implementations will return the parameters unchanged, but this method
    /// provides flexibility for providers with specific parameter requirements.
    /// </para>
    /// </remarks>
    IReadOnlyDictionary<string, object?> TranslateParameters(
        IReadOnlyDictionary<string, object?>? parameters,
        string currentTemplate);
}
