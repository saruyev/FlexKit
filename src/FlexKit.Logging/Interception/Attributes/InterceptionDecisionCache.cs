using System.Collections.Concurrent;
using System.Reflection;
using FlexKit.Logging.Configuration;

namespace FlexKit.Logging.Interception.Attributes;

/// <summary>
/// High-performance cache for interception decisions to avoid reflection overhead during method calls.
/// Pre-computes and caches interception behavior for types and methods to maintain ~50ns performance target.
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
    private readonly ConcurrentDictionary<string, InterceptionBehavior?> _methodCache = new();
    private readonly ConcurrentDictionary<Type, bool> _disabledTypesCache = new();
    private readonly LoggingConfig _loggingConfig = loggingConfig ?? throw new ArgumentNullException(nameof(loggingConfig));

    /// <summary>
    /// Gets the cached interception behavior for a specific method.
    /// This method is designed for high-performance lookup during method interception.
    /// </summary>
    /// <param name="method">The method to get behavior for.</param>
    /// <returns>
    /// The cached InterceptionBehavior, or null if the method should not be intercepted.
    /// </returns>
    /// <remarks>
    /// This method must be extremely fast (~1-2 ns) as it's called on every intercepted method.
    /// All heavy computation (reflection, pattern matching) is done during cache population.
    /// </remarks>
    public InterceptionBehavior? GetInterceptionBehavior(MethodInfo method)
    {
        // First, try the method as-is (for class interception)
        var cacheKey = CreateMethodCacheKey(method);
        if (_methodCache.TryGetValue(cacheKey, out var behavior))
        {
            return behavior;
        }

        // If not found and this is an interface method, try to find the implementation method
        if (method.DeclaringType?.IsInterface != true)
        {
            return null;
        }

        // Try to find the corresponding implementation method
        var implementationMethod = FindImplementationMethod(method);
        if (implementationMethod == null)
        {
            return null;
        }

        var implCacheKey = CreateMethodCacheKey(implementationMethod);
        return _methodCache.TryGetValue(implCacheKey, out var implBehavior) ? implBehavior : null;
    }

    /// <summary>
    /// Pre-caches decisions for all public methods of a type.
    /// Called during DI registration to front-load all expensive operations.
    /// </summary>
    /// <param name="type">The type to cache all method decisions for.</param>
    public void CacheTypeDecisions(Type type)
    {
        // Cache type-level disabled status first
        var typeDisabled = IsTypeCompletelyDisabled(type);
        _disabledTypesCache[type] = typeDisabled;

        if (typeDisabled)
        {
            // If the type is disabled, mark all methods as null (no interception)
            foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.Instance)
                         .Where(ShouldConsiderMethod))
            {
                var cacheKey = CreateMethodCacheKey(method);
                _methodCache[cacheKey] = null;
            }
            return;
        }

        // Cache individual method decisions
        foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.Instance)
                     .Where(ShouldConsiderMethod))
        {
            CacheMethodDecision(method, type);
        }
    }

    /// <summary>
    /// Attempts to find the implementation method for a given interface method by searching through cached types.
    /// Used to handle interface-based interception where the decision is cached on the implementation type.
    /// </summary>
    /// <param name="interfaceMethod">The interface method to find an implementation for.</param>
    /// <returns>The implementation method if found; null if no implementation is found in cached types.</returns>
    private MethodInfo? FindImplementationMethod(MethodInfo interfaceMethod)
    {
        // Look through all cached types to find one that implements this interface method
        foreach (var cachedType in _disabledTypesCache.Keys)
        {
            if (interfaceMethod.DeclaringType?.IsAssignableFrom(cachedType) == true)
            {
                // Find the implementation method with a matching signature
                var implMethod = cachedType.GetMethod(interfaceMethod.Name,
                    [.. interfaceMethod.GetParameters().Select(p => p.ParameterType)]);

                if (implMethod != null)
                {
                    return implMethod;
                }
            }
        }

        return null;
    }

    /// <summary>
    /// Pre-computes and caches the interception decision for a specific method.
    /// This method performs all expensive operations (reflection, pattern matching) once
    /// during container registration rather than during method execution.
    /// </summary>
    /// <param name="method">The method to cache decisions for.</param>
    /// <param name="declaringType">The type that declares the method.</param>
    private void CacheMethodDecision(
        MethodInfo method,
        Type declaringType)
    {
        var cacheKey = CreateMethodCacheKey(method);
        var behavior = ResolveInterceptionBehaviorForMethod(method, declaringType);

        _methodCache[cacheKey] = behavior;

        // Also cache if the entire type is disabled
        if (behavior != null || _disabledTypesCache.ContainsKey(declaringType))
        {
            return;
        }

        _disabledTypesCache[declaringType] = IsTypeCompletelyDisabled(declaringType);
    }

    /// <summary>
    /// Creates a unique cache key for a method that includes type name, method name, and parameter signature.
    /// This ensures correct handling of method overloads by including parameter types in the key.
    /// </summary>
    /// <param name="method">The method to create a cache key for.</param>
    /// <returns>A unique string key that can distinguish between overloaded methods.</returns>
    private static string CreateMethodCacheKey(MethodInfo method)
    {
        // Create a unique key that includes type name, method name, and parameter signature
        // to handle method overloads correctly
        var parameterTypes = string.Join(",", method.GetParameters()
            .Select(p => p.ParameterType.Name));
        return $"{method.DeclaringType?.FullName}.{method.Name}({parameterTypes})";
    }

    /// <summary>
    /// Resolves the interception behavior for a method using the complete decision hierarchy.
    /// Follows the precedence: NoLog attributes > Logging attributes > Configuration patterns > Default behavior.
    /// </summary>
    /// <param name="method">The method to resolve behavior for.</param>
    /// <param name="declaringType">The type that declares the method.</param>
    /// <returns>The resolved InterceptionBehavior, or null if no interception should occur.</returns>
    private InterceptionBehavior? ResolveInterceptionBehaviorForMethod(
        MethodInfo method,
        Type declaringType)
    {
        // Decision hierarchy: NoLog > Attributes > Configuration > Default

        // 1. Check for disabled attributes (the highest precedence)
        if (AttributeResolver.IsLoggingDisabled(method))
        {
            return null; // No interception
        }

        // 2. Check for explicit logging attributes
        var attributeBehavior = AttributeResolver.ResolveInterceptionBehavior(method);
        if (attributeBehavior.HasValue)
        {
            return attributeBehavior.Value;
        }

        // 3. Check configuration patterns
        var configBehavior = ResolveFromConfiguration(declaringType);
        if (configBehavior.HasValue)
        {
            return configBehavior.Value;
        }

        // 4. Use default behavior
        return _loggingConfig.AutoIntercept ? InterceptionBehavior.LogInput : null;
    }

    /// <summary>
    /// Resolves interception behavior from configuration patterns, including exact matches and wildcard patterns.
    /// Checks exact type name matches first, then falls back to wildcard pattern matching.
    /// </summary>
    /// <param name="type">The type to resolve configuration for.</param>
    /// <returns>The configured InterceptionBehavior if a matching pattern is found; null otherwise.</returns>
    private InterceptionBehavior? ResolveFromConfiguration(Type type)
    {
        if (type.FullName == null)
        {
            return null;
        }

        // Check for the exact match first
        if (_loggingConfig.Services.TryGetValue(type.FullName, out var exactConfig))
        {
            return exactConfig.GetBehavior();
        }

        // Check for wildcard patterns
        foreach (var (pattern, config) in _loggingConfig.Services)
        {
            if (pattern.EndsWith('*') && type.FullName.StartsWith(pattern[..^1], StringComparison.InvariantCulture))
            {
                return config.GetBehavior();
            }
        }

        return null;
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
    /// Determines if a method should be considered for interception based on its characteristics.
    /// Excludes private methods, static methods, constructors, property accessors, and Object methods.
    /// </summary>
    /// <param name="method">The method to evaluate.</param>
    /// <returns>True if the method should be considered for interception; false if it should be ignored.</returns>
    private static bool ShouldConsiderMethod(MethodInfo method) =>
        method is { IsPublic: true, IsStatic: false } and
        { IsConstructor: false, IsSpecialName: false } && // Excludes property getters/setters, event handlers
        method.DeclaringType != typeof(object); // Exclude Object methods
}
