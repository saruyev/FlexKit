namespace FlexKit.Logging.Configuration;

/// <summary>
/// Defines the type of method interception logging behavior to apply.
/// Controls whether input parameters, output values, or both are logged during method execution.
/// </summary>
public enum InterceptionBehavior
{
    /// <summary>
    /// Log input parameters when methods are called.
    /// Captures method arguments for analysis and debugging.
    /// </summary>
    LogInput,

    /// <summary>
    /// Log output values when methods complete successfully.
    /// Captures return values for analysis and debugging.
    /// </summary>
    LogOutput,

    /// <summary>
    /// Log both input parameters and output values.
    /// Provides complete visibility into method execution.
    /// </summary>
    LogBoth,
}
