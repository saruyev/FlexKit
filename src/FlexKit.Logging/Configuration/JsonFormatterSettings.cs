using JetBrains.Annotations;

namespace FlexKit.Logging.Configuration;

/// <summary>
/// Configuration settings specific to the JSON formatter.
/// Controls JSON serialization behavior and output format.
/// </summary>
public class JsonFormatterSettings
{
    /// <summary>
    /// Gets or sets whether to format JSON output with indentation for readability.
    /// When true, JSON is formatted with proper indentation and line breaks.
    /// </summary>
    /// <value>True for pretty-printed JSON; false for compact JSON. Default is false.</value>
    public bool PrettyPrint { get; [UsedImplicitly] set; }

    /// <summary>
    /// Gets or sets custom property names for JSON fields.
    /// Allows customization of JSON property names for different logging standards.
    /// </summary>
    /// <value>Dictionary mapping internal field names to custom JSON property names.</value>
    [UsedImplicitly]
    public Dictionary<string, string> CustomPropertyNames { get; [UsedImplicitly] set; } = [];
}
