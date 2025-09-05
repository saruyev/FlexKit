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
    private static InterceptionDecision? GetMethodLevelDecision(MethodInfo method) => MemberInfoDecision(method);

    /// <summary>
    /// Resolves the interception decision from class-level attributes.
    /// Handles a combination of LogInput and LogOutput attributes and prioritizes LogBoth.
    /// </summary>
    /// <param name="classType">The type to check for class-level logging attributes.</param>
    /// <returns>
    /// The resolved InterceptionDecision if logging attributes are found at the class level;
    /// null if no class-level logging attributes are present.
    /// </returns>
    private static InterceptionDecision? GetClassLevelDecision(Type classType) => MemberInfoDecision(classType);

    /// <summary>
    /// Determines the logging interception decision based on the attributes applied to the provided member information.
    /// Evaluates attributes such as LogBothAttribute, LogInputAttribute, and LogOutputAttribute to decide
    /// the appropriate logging behavior and levels.
    /// </summary>
    /// <param name="member">The member (method or type) to inspect for logging-related attributes.</param>
    /// <returns>
    /// An InterceptionDecision object representing the resolved logging behavior, or null if no relevant attributes are found.
    /// </returns>
    private static InterceptionDecision? MemberInfoDecision(MemberInfo member)
    {
        var logBothAttr = member.GetCustomAttribute<LogBothAttribute>();
        if (logBothAttr != null)
        {
            return GetDecision(logBothAttr, InterceptionBehavior.LogBoth);
        }

        var logInputAttr = member.GetCustomAttribute<LogInputAttribute>();
        var logOutputAttr = member.GetCustomAttribute<LogOutputAttribute>();

        if (logInputAttr != null && logOutputAttr != null)
        {
            // Both input and output attributes present - use the higher log level
            var attribute = new LogBothAttribute(
                level: (LogLevel)Math.Min((int)logInputAttr.Level, (int)logOutputAttr.Level),
                exceptionLevel: (LogLevel)Math.Min((int)logInputAttr.ExceptionLevel!, (int)logOutputAttr.ExceptionLevel!),
                target: logInputAttr.Target ?? logOutputAttr.Target,
                formatter: logInputAttr.Formatter?.ToString() ?? logOutputAttr.Formatter?.ToString());

            return GetDecision(attribute, InterceptionBehavior.LogBoth);
        }

        if (logInputAttr != null)
        {
            return GetDecision(logInputAttr, InterceptionBehavior.LogInput);
        }

        return logOutputAttr != null ? GetDecision(logOutputAttr, InterceptionBehavior.LogInput) : null;
    }

    /// <summary>
    /// Constructs a decision based on the provided logging attribute and behavior.
    /// </summary>
    /// <param name="attribute">
    /// The logging attribute containing configuration for logging, such as level, target, and formatter.
    /// </param>
    /// <param name="behavior">
    /// The logging behavior specifying how the interception should be processed (e.g., LogInput, LogOutput, or LogBoth).
    /// </param>
    /// <returns>
    /// An <see cref="InterceptionDecision"/> instance encapsulating all relevant configurations for the interception decision.
    /// </returns>
    private static InterceptionDecision? GetDecision(LoggingAttribute attribute, InterceptionBehavior behavior) =>
        new InterceptionDecision()
            .WithBehavior(behavior)
            .WithTarget(attribute.Target)
            .WithLevel(attribute.Level)
            .WithExceptionLevel(attribute.ExceptionLevel ?? LogLevel.Error)
            .WithFormatter(attribute.Formatter);
}
