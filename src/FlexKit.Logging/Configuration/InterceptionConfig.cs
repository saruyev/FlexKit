using JetBrains.Annotations;

namespace FlexKit.Logging.Configuration;

/// <summary>
/// Configuration settings for method interception behavior on specific services or service patterns.
/// Defines what type of logging should occur when methods are intercepted.
/// </summary>
public class InterceptionConfig
{
    /// <summary>
    /// Gets or sets whether to log input parameters when methods are called.
    /// When true, method arguments will be captured and logged.
    /// </summary>
    /// <value>True to log input parameters; false to skip input logging. Default is false.</value>
    public bool LogInput { get; [UsedImplicitly] set; }

    /// <summary>
    /// Gets or sets whether to log output values when methods complete successfully.
    /// When true, method return values will be captured and logged.
    /// </summary>
    /// <value>True to log output values; false to skip output logging. Default is false.</value>
    public bool LogOutput { get; [UsedImplicitly] set; }

    /// <summary>
    /// Gets the effective interception behavior based on the configured flags.
    /// </summary>
    /// <returns>The appropriate InterceptionBehavior enum value.</returns>
    public InterceptionBehavior GetBehavior() =>
        LogOutput switch
        {
            true when LogInput => InterceptionBehavior.LogBoth,
            true when !LogInput => InterceptionBehavior.LogOutput,
            _ => InterceptionBehavior.LogInput
        };
}
