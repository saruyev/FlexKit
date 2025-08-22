using JetBrains.Annotations;
using Microsoft.Extensions.Configuration;

namespace FlexKit.Logging.Configuration;

/// <summary>
/// Configuration for a logging target with flexible key-value properties.
/// Supports both auto-detected targets and user-configured targets.
/// </summary>
public class LoggingTarget
{
    /// <summary>
    /// Gets or sets the target type (e.g., "Console", "File", "Elasticsearch").
    /// Used for target resolution and auto-detection matching.
    /// </summary>
    [UsedImplicitly]
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether this target is enabled.
    /// Defaults to true. Can be used to disable auto-detected targets.
    /// </summary>
    [UsedImplicitly]
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Gets or sets flexible properties for target configuration.
    /// Properties are framework-specific and passed to the target implementation.
    /// </summary>
    [UsedImplicitly]
    public Dictionary<string, IConfigurationSection?> Properties { get; set; } = new();
}
