using FlexKit.Logging.Configuration;
using Microsoft.Extensions.Logging;
using ISerilogLogger = Serilog.ILogger;

namespace FlexKit.Logging.Serilog.Core;

/// <summary>
/// Logger provider that bridges Microsoft.Extensions.Logging to Serilog.
/// Routes all ASP.NET Core framework logs to the configured Serilog sinks.
/// </summary>
/// <param name="config">The FlexKit logging configuration.</param>
/// <param name="serilogLogger">The Serilog logger instance.</param>
#pragma warning disable S3881
internal sealed class SerilogLoggerProvider(
    LoggingConfig config,
    ISerilogLogger serilogLogger) : ILoggerProvider
#pragma warning restore S3881
{
    /// <inheritdoc />
    public ILogger CreateLogger(string categoryName) => new SerilogLogger(categoryName, config, serilogLogger);

    /// <inheritdoc />
#pragma warning disable CA1816
    public void Dispose()
#pragma warning restore CA1816
    {
        // Serilog handles its own disposal and shutdown
    }
}
