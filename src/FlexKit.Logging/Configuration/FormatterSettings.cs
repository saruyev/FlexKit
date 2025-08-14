namespace FlexKit.Logging.Configuration;

/// <summary>
/// Configuration settings for specific message formatters.
/// Contains formatter-specific options that control formatting behavior.
/// </summary>
public class FormatterSettings
{
    /// <summary>
    /// Gets or sets JSON formatter-specific settings.
    /// </summary>
    /// <value>Configuration options for the JSON formatter.</value>
    public JsonFormatterSettings Json { get; set; } = new();

    /// <summary>
    /// Gets or sets hybrid formatter-specific settings.
    /// </summary>
    /// <value>Configuration options for the hybrid formatter.</value>
    public HybridFormatterSettings Hybrid { get; set; } = new();

    /// <summary>
    /// Gets or sets custom template formatter-specific settings.
    /// </summary>
    /// <value>Configuration options for custom template formatters.</value>
    public CustomTemplateFormatterSettings CustomTemplate { get; set; } = new();
}
