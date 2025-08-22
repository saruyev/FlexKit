using FlexKit.Logging.Configuration;
using Microsoft.Extensions.Logging;

namespace FlexKit.Logging.Interception;

/// <summary>
/// Represents a complete interception decision containing both behavior and log level.
/// Encapsulates what to log (input/output/both) and at what level.
/// </summary>
public readonly record struct InterceptionDecision
{
    /// <summary>
    /// Gets the interception behavior that determines what to log.
    /// </summary>
    public InterceptionBehavior Behavior { get; private init; } = InterceptionBehavior.LogInput;

    /// <summary>
    /// Gets the log level to use when logging.
    /// </summary>
    public LogLevel Level { get; private init; } = LogLevel.Information;

    /// <summary>
    /// Gets the log level to use when an exception is thrown.
    /// </summary>
    public LogLevel ExceptionLevel { get; private init; } = LogLevel.Error;

    /// <summary>
    /// Gets the target name to route logs to.
    /// If null, uses the default target.
    /// </summary>
    public string? Target { get; private init; }

    /// <summary>
    /// Creates a new instance of the InterceptionDecision with the specified interception behavior.
    /// </summary>
    /// <param name="behavior">The interception behavior to apply.</param>
    /// <returns>A new instance of InterceptionDecision with the specified behavior.</returns>
    public InterceptionDecision WithBehavior(InterceptionBehavior behavior) =>
        this with { Behavior = behavior };

    /// <summary>
    /// Creates a new instance of the InterceptionDecision with the specified log level.
    /// </summary>
    /// <param name="level">The log level to apply to the interception decision.</param>
    /// <returns>A new instance of InterceptionDecision with the specified log level.</returns>
    public InterceptionDecision WithLevel(LogLevel level) => this with { Level = level };

    /// <summary>
    /// Creates a new instance of the InterceptionDecision with the specified exception log level.
    /// </summary>
    /// <param name="level">The log level to apply for exceptions in the interception decision.</param>
    /// <returns>A new instance of InterceptionDecision with the specified exception log level.</returns>
    public InterceptionDecision WithExceptionLevel(LogLevel level) => this with { ExceptionLevel = level };

    /// <summary>
    /// Creates a new instance of the InterceptionDecision with the specified target.
    /// </summary>
    /// <param name="target">The target associated with the interception decision.</param>
    /// <returns>A new instance of InterceptionDecision with the specified target.</returns>
    public InterceptionDecision WithTarget(string? target) => this with { Target = target };

    /// <summary>
    /// Initializes a new instance of the InterceptionDecision.
    /// </summary>
    public InterceptionDecision() { }
}
