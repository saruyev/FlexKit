using System.Reflection;
using FlexKit.Logging.Configuration;

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
    /// Resolves the effective interception behavior for the specified method based on attributes.
    /// Returns null if no logging attributes are found, allowing fallback to configuration or defaults.
    /// </summary>
    /// <param name="method">The method to resolve attributes for.</param>
    /// <returns>
    /// The effective InterceptionBehavior if logging attributes are found;
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
    public static InterceptionBehavior? ResolveInterceptionBehavior(MethodInfo method)
    {
        // Check method-level attributes first (the highest precedence)
        var methodBehavior = GetMethodLevelBehavior(method);
        if (methodBehavior.HasValue)
        {
            return methodBehavior.Value;
        }

        // Check class-level attributes
        var classType = method.DeclaringType;
        return classType == null ? null : GetClassLevelBehavior(classType);
    }

    /// <summary>
    /// Checks if the method has any logging-related attributes at the method level.
    /// Includes both enabling attributes (LogInput, LogOutput, LogBoth) and disabling attributes (NoLog, NoAutoLog).
    /// </summary>
    /// <param name="method">The method to check for logging attributes.</param>
    /// <returns>True if the method has any logging-related attributes; false otherwise.</returns>
    public static bool HasLoggingAttributes(MethodInfo method) =>
        method.GetCustomAttribute<LogInputAttribute>() != null ||
        method.GetCustomAttribute<LogOutputAttribute>() != null ||
        method.GetCustomAttribute<LogBothAttribute>() != null ||
        method.GetCustomAttribute<NoLogAttribute>() != null ||
        method.GetCustomAttribute<NoAutoLogAttribute>() != null;

    /// <summary>
    /// Checks if the type has any logging-related attributes at the class level.
    /// Includes both enabling attributes (LogInput, LogOutput, LogBoth) and disabling attributes (NoLog, NoAutoLog).
    /// </summary>
    /// <param name="type">The type to check for logging attributes.</param>
    /// <returns>True if the type has any logging-related attributes; false otherwise.</returns>
    public static bool HasLoggingAttributes(Type type) =>
        type.GetCustomAttribute<LogInputAttribute>() != null ||
        type.GetCustomAttribute<LogOutputAttribute>() != null ||
        type.GetCustomAttribute<LogBothAttribute>() != null ||
        type.GetCustomAttribute<NoLogAttribute>() != null ||
        type.GetCustomAttribute<NoAutoLogAttribute>() != null;

    /// <summary>
    /// Resolves the interception behavior from method-level attributes.
    /// Handles a combination of LogInput and LogOutput attributes and prioritizes LogBoth.
    /// </summary>
    /// <param name="method">The method to check for method-level logging attributes.</param>
    /// <returns>
    /// The resolved InterceptionBehavior if logging attributes are found at the method level;
    /// null if no method-level logging attributes are present.
    /// </returns>
    private static InterceptionBehavior? GetMethodLevelBehavior(MethodInfo method)
    {
        if (method.GetCustomAttribute<LogBothAttribute>() != null)
        {
            return InterceptionBehavior.LogBoth;
        }

        var hasLogInput = method.GetCustomAttribute<LogInputAttribute>() != null;
        var hasLogOutput = method.GetCustomAttribute<LogOutputAttribute>() != null;

        return hasLogInput switch
        {
            true when hasLogOutput => InterceptionBehavior.LogBoth,
            true => InterceptionBehavior.LogInput,
            false when hasLogOutput => InterceptionBehavior.LogOutput,
            _ => null
        };
    }

    /// <summary>
    /// Resolves the interception behavior from class-level attributes.
    /// Handles a combination of LogInput and LogOutput attributes and prioritizes LogBoth.
    /// </summary>
    /// <param name="classType">The type to check for class-level logging attributes.</param>
    /// <returns>
    /// The resolved InterceptionBehavior if logging attributes are found at the class level;
    /// null if no class-level logging attributes are present.
    /// </returns>
    private static InterceptionBehavior? GetClassLevelBehavior(Type classType)
    {
        if (classType.GetCustomAttribute<LogBothAttribute>() != null)
        {
            return InterceptionBehavior.LogBoth;
        }

        var hasLogInput = classType.GetCustomAttribute<LogInputAttribute>() != null;
        var hasLogOutput = classType.GetCustomAttribute<LogOutputAttribute>() != null;

        return hasLogInput switch
        {
            true when hasLogOutput => InterceptionBehavior.LogBoth,
            true => InterceptionBehavior.LogInput,
            false when hasLogOutput => InterceptionBehavior.LogOutput,
            _ => null
        };
    }
}
