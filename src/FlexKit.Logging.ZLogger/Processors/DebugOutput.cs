using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using JetBrains.Annotations;
using ZLogger;

namespace FlexKit.Logging.ZLogger.Processors;

/// <summary>
/// Processes log entries broadcasting them to the debug console.
/// </summary>
/// <remarks>
/// This class implements the <see cref="IAsyncLogProcessor"/> interface to process and
/// format log entries, outputting them to the debug console using "Debug.WriteLine".
/// Additionally, it ensures the log entry objects are returned to the pool by calling <see cref="IZLoggerEntry.Return"/>.
/// </remarks>
[UsedImplicitly]
public class DebugOutput : IAsyncLogProcessor
{
    /// <inheritdoc />
    public void Post(IZLoggerEntry log)
    {
        // Format the log entry to string and write to debug output
        var message = log.ToString();
        Debug.WriteLine(message);

        // Always call Return() to return the entry to the pool
        log.Return();
    }

    /// <inheritdoc />
    [SuppressMessage("Usage", "CA1816:Dispose methods should call SuppressFinalize")]
    public ValueTask DisposeAsync() => default;
}
