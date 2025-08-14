using JetBrains.Annotations;

namespace FlexKit.Logging.Configuration;

/// <summary>
/// Configuration settings specific to custom template formatters.
/// Controls template loading and validation behavior.
/// </summary>
public class CustomTemplateFormatterSettings
{
    /// <summary>
    /// Gets or sets whether to enable strict template validation.
    /// When true, templates are validated for correct placeholder syntax and available properties.
    /// </summary>
    /// <value>True for strict validation; false for lenient validation. Default is true.</value>
    public bool StrictValidation { get; [UsedImplicitly] set; } = true;

    /// <summary>
    /// Gets or sets whether to cache parsed templates for better performance.
    /// When true, templates are parsed once and cached for later use.
    /// </summary>
    /// <value>True to cache templates; false to parse each time. Default is true.</value>
    public bool CacheTemplates { get; [UsedImplicitly] set; } = true;

    /// <summary>
    /// Gets or sets the default template to use when no specific template is configured.
    /// Used as a fallback when service-specific templates are not found.
    /// </summary>
    /// <value>The default template string.</value>
    public string DefaultTemplate { get; [UsedImplicitly] set; } = "Method {TypeName}.{MethodName} executed";

    /// <summary>
    /// Gets or sets per-service template overrides.
    /// Allows different templates for different services or namespaces.
    /// </summary>
    /// <value>Dictionary mapping service patterns to template names.</value>
    [UsedImplicitly]
    public Dictionary<string, string> ServiceTemplates { get; set; } = new();
}
