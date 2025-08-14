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
public sealed class MelMessageTranslator : IMessageTranslator
{
    /// <inheritdoc />
    public string TranslateTemplate(string? messageTemplate) => messageTemplate ?? string.Empty;

    /// <inheritdoc />
    public IReadOnlyDictionary<string, object?> TranslateParameters(
        IReadOnlyDictionary<string, object?>? parameters) =>
        parameters ?? new Dictionary<string, object?>();

    /// <inheritdoc />
    public bool CanTranslate(
        string? messageTemplate,
        IReadOnlyDictionary<string, object?> parameters) => true;
}
