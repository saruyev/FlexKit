using JetBrains.Annotations;

namespace FlexKit.Logging.Configuration;

/// <summary>
/// Configuration settings specific to the hybrid formatter.
/// Controls how structured messages and JSON metadata are combined.
/// </summary>
public class HybridFormatterSettings
{
    /// <summary>
    /// Gets or sets the template for the structured message portion of hybrid output.
    /// This template is used for the human-readable message part.
    /// </summary>
    /// <value>The message template string. Default provides basic method execution information.</value>
    public string MessageTemplate { get; [UsedImplicitly] set; } = "Method {MethodName} completed in {Duration}ms";

    /// <summary>
    /// Gets or sets whether to include metadata in the hybrid output.
    /// When true, additional JSON metadata is appended to the structured message.
    /// </summary>
    /// <value>True to include metadata; false for message-only output. Default is true.</value>
    public bool IncludeMetadata { get; [UsedImplicitly] set; } = true;

    /// <summary>
    /// Gets or sets the separator between the structured message and JSON metadata.
    /// Only used when <see cref="IncludeMetadata"/> is true.
    /// </summary>
    /// <value>The separator string. Default is a space followed by a pipe symbol.</value>
    public string MetadataSeparator { get; [UsedImplicitly] set; } = " | ";

    /// <summary>
    /// Gets or sets which metadata fields to include in the hybrid output.
    /// When empty, all available metadata is included.
    /// </summary>
    /// <value>List of metadata field names to include. Empty list includes all fields.</value>
    [UsedImplicitly]
    public List<string> MetadataFields { get; set; } = [];
}
