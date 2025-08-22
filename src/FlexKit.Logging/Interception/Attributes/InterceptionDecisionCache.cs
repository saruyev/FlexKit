using System.Collections.Concurrent;
using System.Reflection;
using FlexKit.Logging.Configuration;

namespace FlexKit.Logging.Interception.Attributes;

/// <summary>
/// High-performance cache for interception decisions using a three-tier precedence system:
/// 1. Attribute-based (the highest priority)
/// 2. Configuration-based patterns (medium priority)
/// 3. Auto-interception (the lowest priority)
/// Pre-computes and caches interception decisions for types and methods to maintain ~50ns performance target.
/// </summary>
/// <remarks>
/// This cache is populated once during DI container registration and provides O(1) lookup
/// during method interception to avoid any reflection or attribute resolution overhead
/// in the hot path.
/// </remarks>
/// <remarks>
/// Initializes a new instance of the InterceptionDecisionCache.
/// </remarks>
/// <param name="loggingConfig">The logging configuration for pattern matching and defaults.</param>
public sealed class InterceptionDecisionCache(LoggingConfig loggingConfig)
{
    private readonly ConcurrentDictionary<Type, ConcurrentDictionary<MethodInfo, InterceptionDecision?>>
        _typeDecisions = new();
    private readonly LoggingConfig _loggingConfig =
        loggingConfig ?? throw new ArgumentNullException(nameof(loggingConfig));

    /// <summary>
    /// Gets the cached interception decision for a method, or computes and caches it if not present.
    /// </summary>
    /// <param name="method">The method to get the interception decision for.</param>
    /// <returns>The interception decision if logging should occur; null if the method should not be logged.</returns>
    public InterceptionDecision? GetInterceptionDecision(MethodInfo method)
    {
        var declaringType = method.DeclaringType;
        if (declaringType == null)
        {
            return null;
        }

        // Try direct lookup first (for concrete classes)
        if (_typeDecisions.TryGetValue(declaringType, out var typeCache))
        {
            return typeCache.TryGetValue(method, out var cachedDecision) ? cachedDecision : null;
        }

        // If declaring type is an interface, find the implementation type
        if (!declaringType.IsInterface)
        {
            return ComputeMethodDecision(method);
        }

        var implementationType = FindImplementationType(declaringType);
        if (
            implementationType == null ||
            !_typeDecisions.TryGetValue(implementationType, out var implTypeCache))
        {
            return ComputeMethodDecision(method);
        }

        // Find the implementation method with a matching signature
        var implMethod = FindImplementationMethod(method, implementationType);
        if (implMethod != null)
        {
            return implTypeCache.TryGetValue(implMethod, out var implDecision) ? implDecision : null;
        }

        // Compute decision on-demand for types not in cache
        return ComputeMethodDecision(method);
    }

    /// <summary>
    /// Pre-caches decisions for all public methods of a type using the three-tier precedence system.
    /// Called during DI registration to front-load all expensive operations.
    /// </summary>
    /// <param name="type">The type to cache all method decisions for.</param>
    public void CacheTypeDecisions(Type type)
    {
        var methodDecisions = new ConcurrentDictionary<MethodInfo, InterceptionDecision?>();

        // Cache individual method decisions using three-tier precedence
        foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.Instance)
                     .Where(ShouldInterceptMethod))
        {
            methodDecisions[method] = ComputeMethodDecision(method);
        }

        _typeDecisions[type] = methodDecisions;
    }

    /// <summary>
    /// Computes the interception decision for a specific method using the three-tier precedence system:
    /// 1. Attributes (highest priority) - explicit method/class attributes
    /// 2. Configuration (medium priority) - patterns in LoggingConfig.Services
    /// 3. Auto-interception (the lowest priority) - default behavior when AutoIntercept is enabled
    /// </summary>
    /// <param name="method">The method to compute the decision for.</param>
    /// <returns>The computed interception decision; null if no logging should occur.</returns>
    private InterceptionDecision? ComputeMethodDecision(MethodInfo method)
    {
        var declaringType = method.DeclaringType;
        if (declaringType == null || !ShouldInterceptMethod(method))
        {
            return null;
        }

        // TIER 1: Check for disabled attributes (the highest precedence within attributes)
        if (AttributeResolver.IsLoggingDisabled(method))
        {
            return null; // No interception - disabled attributes override everything
        }

        // TIER 1: Check for explicit logging attributes (the highest priority)
        var attributeDecision = AttributeResolver.ResolveInterceptionDecision(method);
        if (attributeDecision.HasValue)
        {
            return attributeDecision.Value; // Use attribute-based decision
        }

        // TIER 2: Check configuration patterns (medium priority)
        var configDecision = ResolveFromConfiguration(declaringType);
        if (configDecision.HasValue)
        {
            return configDecision.Value; // Use configuration-based decision
        }

        // TIER 3: Check auto-interception (the lowest priority)
        return ResolveFromAutoIntercept(declaringType);
    }

    /// <summary>
    /// Resolves interception decision from configuration patterns, including exact matches and wildcard patterns.
    /// Checks exact type name matches first, then falls back to wildcard pattern matching.
    /// </summary>
    /// <param name="type">The type to resolve configuration for.</param>
    /// <returns>The configured InterceptionDecision if a matching pattern is found; null otherwise.</returns>
    private InterceptionDecision? ResolveFromConfiguration(Type type)
    {
        if (type.FullName == null || _loggingConfig.Services.Count == 0)
        {
            return null;
        }

        // Check for the exact match first (the highest precedence within configuration)
        if (_loggingConfig.Services.TryGetValue(type.FullName, out var exactConfig))
        {
            return exactConfig.GetDecision();
        }

        // Check for wildcard patterns (lower precedence within configuration)
        foreach (var (pattern, config) in _loggingConfig.Services)
        {
            if (pattern.EndsWith('*') && type.FullName.StartsWith(pattern[..^1], StringComparison.InvariantCulture))
            {
                return config.GetDecision();
            }
        }

        return null; // No configuration pattern matched
    }

    /// <summary>
    /// Resolves interception decision from auto-intercept settings.
    /// Only applies when AutoIntercept is enabled and the type is not explicitly excluded.
    /// </summary>
    /// <param name="type">The type to resolve auto-interception for.</param>
    /// <returns>The auto-interception decision if applicable; null if auto-interception should not apply.</returns>
    private InterceptionDecision? ResolveFromAutoIntercept(Type type)
    {
        // Only auto-intercept if enabled in configuration
        if (!_loggingConfig.AutoIntercept)
        {
            return null;
        }

        // Don't auto-intercept if explicitly disabled at type level
        if (IsTypeCompletelyDisabled(type))
        {
            return null;
        }

        // Use the default auto-interception behavior (LogInput with Information level)
        return new InterceptionDecision()
            .WithBehavior(InterceptionBehavior.LogInput)
            .WithLevel(Microsoft.Extensions.Logging.LogLevel.Information)
            .WithExceptionLevel(Microsoft.Extensions.Logging.LogLevel.Error);
    }

    /// <summary>
    /// Checks if a type is completely disabled for logging via NoLog or NoAutoLog attributes.
    /// Used to determine if all methods of a type should skip interception.
    /// </summary>
    /// <param name="type">The type to check for disabled attributes.</param>
    /// <returns>True if the type has disabled attributes; false if logging is allowed.</returns>
    private static bool IsTypeCompletelyDisabled(Type type) =>
        type.GetCustomAttribute<NoLogAttribute>() != null ||
        type.GetCustomAttribute<NoAutoLogAttribute>() != null;

    /// <summary>
    /// Finds the implementation type for a given interface by searching through cached types.
    /// </summary>
    /// <param name="interfaceType">The interface type to find an implementation for.</param>
    /// <returns>The implementation type if found; null if no implementation is found in cached types.</returns>
    private Type? FindImplementationType(Type interfaceType) =>
        _typeDecisions.Keys
            .FirstOrDefault(cachedType => interfaceType.IsAssignableFrom(cachedType) && !cachedType.IsInterface);

    /// <summary>
    /// Finds the implementation method for a given interface method.
    /// </summary>
    /// <param name="interfaceMethod">The interface method to find an implementation for.</param>
    /// <param name="implementationType">The type that implements the interface.</param>
    /// <returns>The implementation method if found; null if no implementation is found.</returns>
    private static MethodInfo? FindImplementationMethod(
        MethodInfo interfaceMethod,
        Type implementationType)
    {
        var parameterTypes = interfaceMethod.GetParameters().Select(p => p.ParameterType).ToArray();
        return implementationType.GetMethod(interfaceMethod.Name, parameterTypes);
    }

    /// <summary>
    /// Determines if a method should be considered for interception based on its characteristics.
    /// Excludes private methods, static methods, constructors, property accessors, and Object methods.
    /// </summary>
    /// <param name="method">The method to evaluate.</param>
    /// <returns>True if the method should be considered for interception; false if it should be ignored.</returns>
    private static bool ShouldInterceptMethod(MethodInfo method) =>
        method is { IsPublic: true, IsStatic: false } and
        { IsConstructor: false, IsSpecialName: false } && // Excludes property getters/setters, event handlers
        method.DeclaringType != typeof(object); // Exclude Object methods
}
