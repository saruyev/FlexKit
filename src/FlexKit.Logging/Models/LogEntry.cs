using System.Diagnostics;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;

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
    public Guid Id { get; private init; }

    /// <summary>
    /// Gets the timestamp when the method execution began.
    /// Uses high-precision timestamp for accurate performance measurements.
    /// </summary>
    private long TimestampTicks { get; init; }

    /// <summary>
    /// Gets the name of the method being logged.
    /// </summary>
    public string MethodName { get; private init; }

    /// <summary>
    /// Gets the full name of the type containing the method.
    /// </summary>
    public string TypeName { get; private init; }

    /// <summary>
    /// Gets the execution duration in ticks, or null if execution is still in progress.
    /// </summary>
    public long? DurationTicks { get; private init; }

    /// <summary>
    /// Gets whether the method execution completed successfully.
    /// </summary>
    public bool Success { get; private init; }

    /// <summary>
    /// Gets the exception type name if the method failed, or null if successful.
    /// </summary>
    public string? ExceptionType { get; private init; }

    /// <summary>
    /// Gets the exception stack trace if the method failed, or null if successful.
    /// </summary>
    public string? StackTrace { get; private init; }

    /// <summary>
    /// Gets the exception message if the method failed, or null if successful.
    /// </summary>
    public string? ExceptionMessage { get; private init; }

    /// <summary>
    /// Gets the current activity ID for distributed tracing correlation.
    /// </summary>
    public string? ActivityId { get; private init; }

    /// <summary>
    /// Gets the thread ID where the method executed.
    /// </summary>
    public int ThreadId { get; private init; }

    /// <summary>
    /// Gets the serialized input parameters if LogInput or LogBoth behavior was used.
    /// Null if input logging was not enabled for this method.
    /// </summary>
    public string? InputParameters { get; private init; }

    /// <summary>
    /// Gets the serialized output value if LogOutput or LogBoth behavior was used.
    /// Null if output logging was not enabled or the method returned void.
    /// </summary>
    public string? OutputValue { get; private init; }

    /// <summary>
    /// Gets the name of the logging template associated with this log entry.
    /// </summary>
    public string? TemplateName { get; private init; }

    /// <summary>
    /// Gets the log level that should be used when outputting this log entry.
    /// Defaults to Information if not specified.
    /// </summary>
    public LogLevel Level { get; private init; }

    /// <summary>
    /// Gets the log level to use when an exception is thrown.
    /// </summary>
    public LogLevel ExceptionLevel { get; private init; }

    /// <summary>
    /// Gets the timestamp when the method execution began as a DateTimeOffset.
    /// </summary>
    public DateTimeOffset Timestamp => GetActualTimestamp(TimestampTicks);

    /// <summary>
    /// Gets the target or destination associated with the log entry, indicating the intended
    /// recipient or endpoint related to this logging operation.
    /// </summary>
    public string? Target { get; private init; }

    /// <summary>
    /// Calculates the actual timestamp from Stopwatch ticks.
    /// </summary>
    /// <param name="stopwatchTicks">The stopwatch ticks when the method began.</param>
    private static DateTimeOffset GetActualTimestamp(
        long stopwatchTicks)
    {
        var currentStopwatchTicks = Stopwatch.GetTimestamp();
        var elapsedSinceStart = currentStopwatchTicks - stopwatchTicks;
        var elapsedTimeSpan = TimeSpan.FromTicks((long)(elapsedSinceStart * TimeSpan.TicksPerSecond / (double)Stopwatch.Frequency));

        return DateTimeOffset.UtcNow.Subtract(elapsedTimeSpan);
    }

    /// <summary>
    /// Creates a new log entry for method start.
    /// </summary>
    /// <param name="methodName">The name of the method being logged.</param>
    /// <param name="typeName">The full name of the type containing the method.</param>
    /// <param name="level">The log level for this entry.</param>
    public static LogEntry CreateStart(
        string methodName,
        string typeName,
        LogLevel level = LogLevel.Information) =>
        new()
        {
            Id = Guid.NewGuid(),
            TimestampTicks = Stopwatch.GetTimestamp(),
            MethodName = methodName,
            TypeName = typeName,
            Success = true,
            Level = level,
            ExceptionLevel = LogLevel.Error,
            ActivityId = Activity.Current?.Id,
            ThreadId = Environment.CurrentManagedThreadId
        };

    /// <summary>
    /// Returns a new <see cref="LogEntry"/> with the specified exception log level.
    /// </summary>
    /// <param name="level">The log level to set for exceptions. If null, the current instance is returned unchanged.</param>
    /// <returns>A new <see cref="LogEntry"/> instance with the updated exception log level, or the current instance if no level is provided.</returns>
    public LogEntry WithErrorLevel(LogLevel? level) =>
        level is null
            ? this
            : this with
            {
                ExceptionLevel = level.Value
            };

    /// <summary>
    /// Associates a specified target with the log entry.
    /// </summary>
    /// <param name="target">The target or recipient related to the log entry, if applicable.</param>
    /// <return>A new instance of <see cref="LogEntry"/> with the specified target set.</return>
    public LogEntry WithTarget(string? target) =>
        this with
        {
            Target = target
        };

    /// <summary>
    /// Creates a new log entry for a method start with input parameters.
    /// Used when LogInput or LogBoth behavior is enabled.
    /// </summary>
    /// <param name="inputParameters">The serialized input parameters.</param>
    public LogEntry WithInput(string? inputParameters) =>
        this with
        {
            InputParameters = inputParameters
        };

    /// <summary>
    /// Creates a completion entry based on a start entry.
    /// </summary>
    /// <param name="success">Whether the method completed successfully.</param>
    /// <param name="durationTicks">The duration of the method execution in ticks.</param>
    /// <param name="exception">The exception that was thrown, if any.</param>
    public LogEntry WithCompletion(
        bool success,
        long durationTicks,
        Exception? exception = null) =>
        this with
        {
            DurationTicks = durationTicks,
            Success = success,
            ExceptionType = exception?.GetType().Name,
            ExceptionMessage = exception?.Message,
            StackTrace = exception?.StackTrace
        };

    /// <summary>
    /// Creates a completion entry based on a start entry.
    /// </summary>
    /// <param name="success">Whether the method completed successfully.</param>
    /// <param name="exception">The exception that was thrown, if any.</param>
    [UsedImplicitly]
    public LogEntry WithCompletion(
        bool success,
        Exception? exception = null)
    {
        var currentTicks = Stopwatch.GetTimestamp();
        var elapsedTicks = currentTicks - TimestampTicks;
        var durationTicks = (long)(elapsedTicks * TimeSpan.TicksPerSecond / (double)Stopwatch.Frequency);

        return WithCompletion(success, durationTicks, exception);
    }

    /// <summary>
    /// Creates a completion entry with an output value.
    /// Used when LogOutput or LogBoth behavior is enabled.
    /// </summary>
    /// <param name="outputValue">The output value to log.</param>
    public LogEntry WithOutput(string? outputValue) =>
        this with
        {
            OutputValue = outputValue
        };

    /// <summary>
    /// Associates a specified template name with the log entry.
    /// </summary>
    /// <param name="name">The name of the template to associate with the log entry.</param>
    /// <returns>A new instance of <see cref="LogEntry"/> with the template name updated.</returns>
    [UsedImplicitly]
    public LogEntry WithTemplate(string name) =>
        this with
        {
            TemplateName = name
        };
}
