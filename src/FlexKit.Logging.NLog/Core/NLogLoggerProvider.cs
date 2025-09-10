using FlexKit.Logging.Configuration;
using Microsoft.Extensions.Logging;

namespace FlexKit.Logging.NLog.Core;

/// <summary>
/// Logger provider that bridges Microsoft.Extensions.Logging to NLog.
/// Routes all ASP.NET Core framework logs to the configured NLog targets.
/// </summary>
/// <param name="config">The FlexKit logging configuration.</param>
#pragma warning disable S3881
public class NLogLoggerProvider(LoggingConfig config) : ILoggerProvider
#pragma warning restore S3881
{
    /// <inheritdoc />
    public ILogger CreateLogger(string categoryName)
    {
        return new NLogLogger(categoryName, config);
    }

    /// <inheritdoc />
#pragma warning disable CA1816
    public void Dispose()
#pragma warning restore CA1816
    {
        // NLog handles its own disposal and shutdown
    }
}
