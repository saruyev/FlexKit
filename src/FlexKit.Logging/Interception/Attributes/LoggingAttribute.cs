using FlexKit.Logging.Configuration;
using Microsoft.Extensions.Logging;

namespace FlexKit.Logging.Interception.Attributes;

/// <summary>
/// An attribute used to enable and configure logging for methods or classes in an application.
/// Provides options to specify the log level, exception log level, target, and formatter type.
/// </summary>
/// <param name="level">The log level to use when logging both input and output.</param>
/// <param name="exceptionLevel">The log level to use when an exception is thrown.</param>
/// <param name="target">The target name to route logs to.</param>
/// <param name="formatter">The formatter to use for formatting log messages.</param>
public abstract class LoggingAttribute(
    LogLevel level = LogLevel.Information,
    LogLevel exceptionLevel = LogLevel.Error,
    string? target = null,
    string? formatter = null) : Attribute
{
    /// <summary>
    /// Gets the log level to use when logging both input parameters and output values.
    /// Defaults to Information if not specified.
    /// </summary>
    public LogLevel Level { get; } = level;

    /// <summary>
    /// Gets the log level to use when an exception is thrown.
    /// </summary>
    public LogLevel? ExceptionLevel { get; } = exceptionLevel;

    /// <summary>
    /// Gets the target name to route logs to.
    /// If null, uses the default target.
    /// </summary>
    public string? Target { get; } = target;

    /// <summary>
    /// Gets or sets the formatter type for logging messages.
    /// Specifies the formatting strategy to be used, such as structured logging, JSON, or custom templates.
    /// </summary>
    public FormatterType? Formatter { get; } =
        string.IsNullOrEmpty(formatter) ? null : Enum.Parse<FormatterType>(formatter);
}
