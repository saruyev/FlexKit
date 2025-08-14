using System.Diagnostics;

namespace FlexKit.Logging.Models;

/// <summary>
/// Lightweight model for capturing method execution data with minimal allocation overhead.
/// Designed to be created quickly (~10ns) and serialized on background threads.
/// </summary>
public readonly record struct LogEntry
{
    /// <summary>
    /// Gets the unique identifier for this log entry.
    /// </summary>
    public Guid Id { get; init; }

    /// <summary>
    /// Gets the timestamp when the method execution began.
    /// Uses high-precision timestamp for accurate performance measurements.
    /// </summary>
    public long TimestampTicks { get; init; }

    /// <summary>
    /// Gets the name of the method being logged.
    /// </summary>
    public string MethodName { get; init; }

    /// <summary>
    /// Gets the full name of the type containing the method.
    /// </summary>
    public string TypeName { get; init; }

    /// <summary>
    /// Gets the execution duration in ticks, or null if execution is still in progress.
    /// </summary>
    public long? DurationTicks { get; init; }

    /// <summary>
    /// Gets whether the method execution completed successfully.
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    /// Gets the exception type name if the method failed, or null if successful.
    /// </summary>
    public string? ExceptionType { get; init; }

    /// <summary>
    /// Gets the exception message if the method failed, or null if successful.
    /// </summary>
    public string? ExceptionMessage { get; init; }

    /// <summary>
    /// Gets the current activity ID for distributed tracing correlation.
    /// </summary>
    public string? ActivityId { get; init; }

    /// <summary>
    /// Gets the thread ID where the method executed.
    /// </summary>
    public int ThreadId { get; init; }

    /// <summary>
    /// Creates a new log entry for method start.
    /// </summary>
    public static LogEntry CreateStart(string methodName, string typeName)
    {
        return new LogEntry
        {
            Id = Guid.NewGuid(),
            TimestampTicks = Stopwatch.GetTimestamp(),
            MethodName = methodName,
            TypeName = typeName,
            Success = true,
            ActivityId = Activity.Current?.Id,
            ThreadId = Environment.CurrentManagedThreadId
        };
    }

    /// <summary>
    /// Creates a completion entry based on a start entry.
    /// </summary>
    public LogEntry WithCompletion(bool success, long durationTicks, Exception? exception = null)
    {
        return this with
        {
            DurationTicks = durationTicks,
            Success = success,
            ExceptionType = exception?.GetType().Name,
            ExceptionMessage = exception?.Message
        };
    }
}
