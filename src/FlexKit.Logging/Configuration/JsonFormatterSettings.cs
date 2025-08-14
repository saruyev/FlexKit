namespace FlexKit.Logging.Configuration;

/// <summary>
/// Configuration settings specific to the JSON formatter.
/// Controls JSON serialization behavior and output format.
/// </summary>
public class JsonFormatterSettings
{
    /// <summary>
    /// Gets or sets whether to include exception stack traces in JSON output.
    /// When true, full stack traces are included for failed method executions.
    /// </summary>
    /// <value>True to include stack traces; false to exclude them. Default is false.</value>
    public bool IncludeStackTrace { get; set; }

    /// <summary>
    /// Gets or sets whether to format JSON output with indentation for readability.
    /// When true, JSON is formatted with proper indentation and line breaks.
    /// </summary>
    /// <value>True for pretty-printed JSON; false for compact JSON. Default is false.</value>
    public bool PrettyPrint { get; set; }

    /// <summary>
    /// Gets or sets whether to include thread information in JSON output.
    /// When true, thread ID and activity ID are included in the JSON.
    /// </summary>
    /// <value>True to include thread info; false to exclude it. Default is true.</value>
    public bool IncludeThreadInfo { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to include timing information in JSON output.
    /// When true, timestamp and duration are included in the JSON.
    /// </summary>
    /// <value>True to include timing info; false to exclude it. Default is true.</value>
    public bool IncludeTimingInfo { get; set; } = true;

    /// <summary>
    /// Gets or sets custom property names for JSON fields.
    /// Allows customization of JSON property names for different logging standards.
    /// </summary>
    /// <value>Dictionary mapping internal field names to custom JSON property names.</value>
    public Dictionary<string, string> CustomPropertyNames { get; set; } = new();
}
