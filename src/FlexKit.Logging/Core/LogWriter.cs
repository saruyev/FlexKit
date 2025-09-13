using Microsoft.Extensions.Logging;

namespace FlexKit.Logging.Core;

/// <summary>
/// Provides pre-defined delegate actions for logging messages at various log levels.
/// These delegates are optimized for structured logging and can be used to
/// efficiently log messages with a specific log level and an optional exception.
/// </summary>
internal static class LogWriter
{
    /// <summary>
    /// Represents a predefined logging delegate for writing log messages with a trace severity level.
    /// </summary>
    /// <remarks>
    /// This delegate is used to log trace-level messages, typically for diagnostic purposes.
    /// It is defined as a static member of the <c>LogWriter</c> class and represents an action
    /// that processes logger instances, messages, and optional exceptions using a trace log level.
    /// </remarks>
    internal static readonly Action<ILogger, string, Exception?> LogTrace =
        LoggerMessage.Define<string>(LogLevel.Trace, new EventId(1), "{Message}");

    /// <summary>
    /// Represents a predefined logging delegate for writing log messages with a debug severity level.
    /// </summary>
    /// <remarks>
    /// This delegate is used to log debug-level messages, typically for detailed debugging purposes.
    /// It is defined as a static member of the <c>LogWriter</c> class and represents an action
    /// that processes logger instances, messages, and optional exceptions using a debug log level.
    /// </remarks>
    internal static readonly Action<ILogger, string, Exception?> LogDebug =
        LoggerMessage.Define<string>(LogLevel.Debug, new EventId(2), "{Message}");

    /// <summary>
    /// Represents a predefined logging delegate for writing log messages with an information severity level.
    /// </summary>
    /// <remarks>
    /// This delegate is used to log informational messages, typically to communicate general operational details.
    /// It is defined as a static member of the <c>LogWriter</c> class and facilitates logging by processing
    /// logger instances, messages, and optional exceptions using an information log level.
    /// </remarks>
    internal static readonly Action<ILogger, string, Exception?> LogInfo =
        LoggerMessage.Define<string>(LogLevel.Information, new EventId(3), "{Message}");

    /// <summary>
    /// Represents a predefined logging delegate for writing log messages with a warning severity level.
    /// </summary>
    /// <remarks>
    /// This delegate is used to log warning-level messages, typically for situations that require attention
    /// but do not disrupt the normal functionality of the application. It is defined as a static member
    /// of the <c>LogWriter</c> class and represents an action that processes logger instances, messages,
    /// and optional exceptions using a warning log level.
    /// </remarks>
    internal static readonly Action<ILogger, string, Exception?> LogWarning =
        LoggerMessage.Define<string>(LogLevel.Warning, new EventId(4), "{Message}");

    /// <summary>
    /// Represents a predefined logging delegate for writing log messages with an error severity level.
    /// </summary>
    /// <remarks>
    /// This delegate is used to log error-level messages, typically for significant issues or failures
    /// in the application. It is defined as a static member of the <c>LogWriter</c> class and facilitates logging
    /// of error messages along with optional exception details, helping to identify and diagnose critical problems.
    /// </remarks>
    internal static readonly Action<ILogger, string, Exception?> LogError =
        LoggerMessage.Define<string>(LogLevel.Error, new EventId(5), "{Message}");

    /// <summary>
    /// Represents a predefined logging delegate for writing log messages with a critical severity level.
    /// </summary>
    /// <remarks>
    /// This delegate is used to log critical-level messages, typically indicating severe issues that require
    /// immediate attention. It is defined as a static member of the <c>LogWriter</c> class and represents an action
    /// that processes logger instances, messages, and optional exceptions using a critical log level.
    /// </remarks>
    internal static readonly Action<ILogger, string, Exception?> LogCritical =
        LoggerMessage.Define<string>(LogLevel.Critical, new EventId(6), "{Message}");
}
