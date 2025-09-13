using FlexKit.Logging.Configuration;
using log4net.Repository;
using Microsoft.Extensions.Logging;

namespace FlexKit.Logging.Log4Net.Core;

/// <summary>
/// Logger provider that bridges Microsoft.Extensions.Logging to Log4Net.
/// Routes all ASP.NET Core framework logs to the configured Log4Net appenders.
/// </summary>
/// <param name="repository">The Log4Net repository to configure.</param>
/// <param name="config">The FlexKit logging configuration.</param>
#pragma warning disable S3881
internal sealed class Log4NetLoggerProvider(
    ILoggerRepository repository,
    LoggingConfig config) : ILoggerProvider
#pragma warning restore S3881
{
    /// <inheritdoc />
    public ILogger CreateLogger(string categoryName) => new Log4NetLogger(categoryName, repository, config);

    /// <inheritdoc />
#pragma warning disable CA1816
    public void Dispose()
#pragma warning restore CA1816
    {
        // Autofac handles repository disposal
    }
}
