using System.Reflection;
using FlexKit.Logging.Configuration;
using Microsoft.Extensions.Logging;

namespace FlexKit.Logging.Interception.Attributes;

/// <summary>
/// Utility class for detecting and resolving logging attributes on methods and classes.
/// Provides the logic for attribute precedence and inheritance handling according to
/// the interception decision hierarchy.
/// </summary>
public static class AttributeResolver
{
    /// <summary>
    /// Determines if logging should be completely disabled for the specified method.
    /// Checks for NoLog or NoAutoLog attributes at method and class levels.
    /// </summary>
    /// <param name="method">The method to check for disabled attributes.</param>
    /// <returns>True if logging should be disabled; false if logging is allowed.</returns>
    /// <remarks>
    /// This check has the highest priority in the decision hierarchy and will override
    /// any configuration settings or other attributes if a disabled attribute is found.
    /// </remarks>
    public static bool IsLoggingDisabled(MethodInfo method)
    {
        // Check method-level disable attributes first (the highest precedence)
        if (method.GetCustomAttribute<NoLogAttribute>() != null ||
            method.GetCustomAttribute<NoAutoLogAttribute>() != null)
        {
            return true;
        }

        // Check class-level disable attributes
        var declaringType = method.DeclaringType;
        return declaringType?.GetCustomAttribute<NoLogAttribute>() != null ||
               declaringType?.GetCustomAttribute<NoAutoLogAttribute>() != null;
    }

    /// <summary>
    /// Resolves the effective interception decision for the specified method based on attributes.
    /// Returns null if no logging attributes are found, allowing fallback to configuration or defaults.
    /// </summary>
    /// <param name="method">The method to resolve attributes for.</param>
    /// <returns>
    /// The effective InterceptionDecision if logging attributes are found;
    /// null if no attributes are present and configuration should be consulted.
    /// </returns>
    /// <remarks>
    /// <para>
    /// Attribute resolution follows this precedence:
    /// <list type="number">
    /// <item>Method-level attributes (the highest precedence)</item>
    /// <item>Class-level attributes</item>
    /// <item>No attributes found (return null for configuration fallback)</item>
    /// </list>
    /// </para>
    /// <para>
    /// This method should only be called after confirming that logging is not disabled
    /// via <see cref="IsLoggingDisabled(MethodInfo)"/>.
    /// </para>
    /// </remarks>
    public static InterceptionDecision? ResolveInterceptionDecision(MethodInfo method)
    {
        // Check method-level attributes first (the highest precedence)
        var methodDecision = GetMethodLevelDecision(method);
        if (methodDecision.HasValue)
        {
            return methodDecision.Value;
        }

        // Check class-level attributes
        var classType = method.DeclaringType;
        return classType == null ? null : GetClassLevelDecision(classType);
    }

    /// <summary>
    /// Resolves the interception decision from method-level attributes.
    /// Handles a combination of LogInput and LogOutput attributes and prioritizes LogBoth.
    /// </summary>
    /// <param name="method">The method to check for method-level logging attributes.</param>
    /// <returns>
    /// The resolved InterceptionDecision if logging attributes are found at the method level;
    /// null if no method-level logging attributes are present.
    /// </returns>
    private static InterceptionDecision? GetMethodLevelDecision(MethodInfo method)
    {
        var logBothAttr = method.GetCustomAttribute<LogBothAttribute>();
        if (logBothAttr != null)
        {
            return new InterceptionDecision()
                .WithBehavior(InterceptionBehavior.LogBoth)
                .WithTarget(logBothAttr.Target)
                .WithLevel(logBothAttr.Level)
                .WithExceptionLevel(logBothAttr.ExceptionLevel ?? LogLevel.Error);
        }

        var logInputAttr = method.GetCustomAttribute<LogInputAttribute>();
        var logOutputAttr = method.GetCustomAttribute<LogOutputAttribute>();

        if (logInputAttr != null && logOutputAttr != null)
        {
            // Both input and output attributes present - use the higher log level
            //for error level use LogBoth
            var target = logInputAttr.Target ?? logOutputAttr.Target;
            var level = (LogLevel)Math.Min((int)logInputAttr.Level, (int)logOutputAttr.Level);
            return new InterceptionDecision()
                .WithBehavior(InterceptionBehavior.LogBoth)
                .WithTarget(target)
                .WithLevel(level);
        }

        if (logInputAttr != null)
        {
            return new InterceptionDecision()
                .WithBehavior(InterceptionBehavior.LogInput)
                .WithTarget(logInputAttr.Target)
                .WithLevel(logInputAttr.Level)
                .WithExceptionLevel(logInputAttr.ExceptionLevel ?? LogLevel.Error);
        }

        return logOutputAttr != null
            ? new InterceptionDecision()
                .WithBehavior(InterceptionBehavior.LogOutput)
                .WithTarget(logOutputAttr.Target)
                .WithLevel(logOutputAttr.Level)
                .WithExceptionLevel(logOutputAttr.ExceptionLevel ?? LogLevel.Error)
            : null;
    }

    /// <summary>
    /// Resolves the interception decision from class-level attributes.
    /// Handles a combination of LogInput and LogOutput attributes and prioritizes LogBoth.
    /// </summary>
    /// <param name="classType">The type to check for class-level logging attributes.</param>
    /// <returns>
    /// The resolved InterceptionDecision if logging attributes are found at the class level;
    /// null if no class-level logging attributes are present.
    /// </returns>
    private static InterceptionDecision? GetClassLevelDecision(Type classType)
    {
        var logBothAttr = classType.GetCustomAttribute<LogBothAttribute>();
        if (logBothAttr != null)
        {
            return new InterceptionDecision()
                .WithBehavior(InterceptionBehavior.LogBoth)
                .WithTarget(logBothAttr.Target)
                .WithLevel(logBothAttr.Level)
                .WithExceptionLevel(logBothAttr.ExceptionLevel ?? LogLevel.Error);
        }

        var logInputAttr = classType.GetCustomAttribute<LogInputAttribute>();
        var logOutputAttr = classType.GetCustomAttribute<LogOutputAttribute>();

        if (logInputAttr != null && logOutputAttr != null)
        {
            // Both input and output attributes present - use the higher log level
            var level = (LogLevel)Math.Min((int)logInputAttr.Level, (int)logOutputAttr.Level);
            var target = logInputAttr.Target ?? logOutputAttr.Target;
            return new InterceptionDecision()
                .WithBehavior(InterceptionBehavior.LogBoth)
                .WithTarget(target)
                .WithLevel(level);
        }

        if (logInputAttr != null)
        {
            return new InterceptionDecision()
                .WithBehavior(InterceptionBehavior.LogInput)
                .WithTarget(logInputAttr.Target)
                .WithLevel(logInputAttr.Level)
                .WithExceptionLevel(logInputAttr.ExceptionLevel ?? LogLevel.Error);
        }

        return logOutputAttr != null
            ? new InterceptionDecision()
                .WithBehavior(InterceptionBehavior.LogOutput)
                .WithTarget(logOutputAttr.Target)
                .WithLevel(logOutputAttr.Level)
                .WithExceptionLevel(logOutputAttr.ExceptionLevel ?? LogLevel.Error)
            : null;
    }
}
