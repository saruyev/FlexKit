using Microsoft.Extensions.Hosting;

namespace FlexKit.Logging.Core;

/// <summary>
/// Interface for a background logging service that handles queued log entry processing.
/// Extends IHostedService to provide lifecycle management and adds custom flush operations.
/// </summary>
public interface IBackgroundLoggingService : IHostedService
{
    /// <summary>
    /// Stops the service and flushes any remaining log entries synchronously.
    /// Used for graceful shutdown to ensure no log entries are lost.
    /// </summary>
    void FlushRemainingEntries();
}
