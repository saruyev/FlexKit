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
    /// Gets the collection of key-value pairs that represent the parameters used to format the message template.
    /// </summary>
    public IReadOnlyDictionary<string, object?> Parameters { get; private init; }

    /// <summary>
    /// Gets the string template used to define the format of the log message.
    /// </summary>
    public string Template { get; private init; }

    /// <summary>
    /// Creates a successful formatted message.
    /// </summary>
    /// <param name="message">The formatted message string.</param>
    public static FormattedMessage Success(string message) =>
        new()
        {
            Message = message,
            IsSuccess = true,
            IsFallback = false,
        };

    /// <summary>
    /// Creates a successfully formatted message based on the provided template and parameters.
    /// </summary>
    /// <param name="template">The string template that defines the message format.</param>
    /// <param name="parameters">The collection of key-value pairs to replace placeholders in the template.</param>
    /// <returns>A successfully formatted message containing the provided template and parameters.</returns>
    public static FormattedMessage Success(
        string template,
        IReadOnlyDictionary<string, object?> parameters) =>
        new()
        {
            IsSuccess = true,
            IsFallback = false,
            Template = template,
            Parameters = parameters
        };

    /// <summary>
    /// Creates a failed formatted message.
    /// </summary>
    /// <param name="errorMessage">The error message describing the failure.</param>
    public static FormattedMessage Failure(string errorMessage) =>
        new()
        {
            Message = $"[Formatting Error: {errorMessage}]",
            IsSuccess = false,
            ErrorMessage = errorMessage,
            IsFallback = false
        };

    /// <summary>
    /// Returns a new instance of the formatted message with the specified parameters applied.
    /// </summary>
    /// <param name="parameters">The collection of key-value pairs to be set as the message parameters.</param>
    /// <returns>A new <see cref="FormattedMessage"/> instance with the updated parameters.</returns>
    public FormattedMessage WithParameters(IReadOnlyDictionary<string, object?> parameters) =>
        this with { Parameters = parameters };

    /// <summary>
    /// Marks the current formatted message as a fallback and optionally assigns an error message.
    /// </summary>
    /// <param name="errorMessage">The optional error message to associate with the fallback message.</param>
    /// <returns>A new instance of the formatted message marked as a fallback.</returns>
    public FormattedMessage WithFallback(string? errorMessage = null) =>
        this with { IsFallback = true, ErrorMessage = errorMessage };
}
