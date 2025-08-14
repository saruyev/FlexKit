namespace FlexKit.Logging.Formatting.Models;

/// <summary>
/// Represents a formatted log message with metadata about the formatting process.
/// Contains the final formatted message string and information about how it was created.
/// </summary>
public readonly record struct FormattedMessage
{
    /// <summary>
    /// Gets the final formatted message string ready for output.
    /// </summary>
    public string Message { get; private init; }

    /// <summary>
    /// Gets whether the formatting process was successful.
    /// </summary>
    public bool IsSuccess { get; private init; }

    /// <summary>
    /// Gets any error message that occurred during formatting.
    /// </summary>
    public string? ErrorMessage { get; private init; }

    /// <summary>
    /// Gets whether this message was created using fallback formatting.
    /// </summary>
    public bool IsFallback { get; private init; }

    /// <summary>
    /// Creates a successful formatted message.
    /// </summary>
    public static FormattedMessage Success(string message) =>
        new()
        {
            Message = message,
            IsSuccess = true,
            IsFallback = false
        };

    /// <summary>
    /// Creates a failed formatted message.
    /// </summary>
    public static FormattedMessage Failure(string errorMessage) =>
        new()
        {
            Message = $"[Formatting Error: {errorMessage}]",
            IsSuccess = false,
            ErrorMessage = errorMessage,
            IsFallback = false
        };
}
