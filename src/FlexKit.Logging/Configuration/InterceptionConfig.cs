using FlexKit.Logging.Interception;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;

namespace FlexKit.Logging.Configuration;

/// <summary>
/// Configuration settings for method interception behavior on specific services or service patterns.
/// Defines what type of logging should occur when methods are intercepted and at what level.
/// </summary>
public class InterceptionConfig
{
    /// <summary>
    /// Gets or sets whether to log input parameters when methods are called.
    /// When true, method arguments will be captured and logged.
    /// </summary>
    /// <value>True to log input parameters; false to skip input logging. Default is false.</value>
    [UsedImplicitly]
    public bool LogInput { get; set; }

    /// <summary>
    /// Gets or sets whether to log output values when methods complete successfully.
    /// When true, method return values will be captured and logged.
    /// </summary>
    /// <value>True to log output values; false to skip output logging. Default is false.</value>
    [UsedImplicitly]
    public bool LogOutput { get; set; }

    /// <summary>
    /// Gets or sets the log level to use for logging.
    /// Defaults to Information if not specified.
    /// </summary>
    /// <value>The log level for logging. Default is Information.</value>
    [UsedImplicitly]
    public LogLevel Level { get; set; } = LogLevel.Information;

    /// <summary>
    /// Gets or sets the log level to use when an exception is thrown.
    /// </summary>
    /// <value>The log level for exception logging. Default is Error.</value>
    [UsedImplicitly]
    public LogLevel ExceptionLevel { get; set; } = LogLevel.Error;

    /// <summary>
    /// Gets or sets the target name to route logs to.
    /// If null, uses the default target. Allows configuration-based target routing.
    /// </summary>
    /// <value>The target name for routing logs. Default is null (use default target).</value>
    [UsedImplicitly]
    public string? Target { get; set; }

    /// <summary>
    /// Gets or sets the patterns of method names to exclude from interception and logging.
    /// Methods matching any of these patterns will be skipped during the interception process.
    /// </summary>
    /// <value>
    /// A list of string patterns representing method names to exclude from interception. Default is an empty list.
    /// </value>
    [UsedImplicitly]
    public List<string> ExcludeMethodPatterns { get; set; } = [];

    /// <summary>
    /// Gets the effective interception decision based on the configured flags and level.
    /// </summary>
    /// <returns>The appropriate InterceptionDecision with behavior and log level.</returns>
    public InterceptionDecision GetDecision() =>
        LogOutput switch
        {
            true when LogInput => new InterceptionDecision()
                .WithBehavior(InterceptionBehavior.LogBoth)
                .WithLevel(Level)
                .WithExceptionLevel(ExceptionLevel)
                .WithTarget(Target),
            true when !LogInput => new InterceptionDecision()
                .WithBehavior(InterceptionBehavior.LogOutput)
                .WithLevel(Level)
                .WithExceptionLevel(ExceptionLevel)
                .WithTarget(Target),
            _ => new InterceptionDecision()
                .WithBehavior(InterceptionBehavior.LogInput)
                .WithLevel(Level)
                .WithExceptionLevel(ExceptionLevel)
                .WithTarget(Target)
        };
}
