using System.Collections.Concurrent;
using System.Reflection;
using FlexKit.Logging.Configuration;
using FlexKit.Logging.Interception.Attributes;

namespace FlexKit.Logging.Formatting.Utils;

/// <summary>
/// Provides high-performance masking functionality for sensitive data in log entries.
/// Supports both attribute-based and configuration-based masking with minimal performance overhead.
/// </summary>
internal static class MaskingExtensions
{
    private static readonly ConcurrentDictionary<Type, bool> _typeHasMaskedPropsCache = new();
    private static readonly ConcurrentDictionary<Type, PropertyInfo[]> _maskedPropertiesCache = new();
    private static readonly ConcurrentDictionary<Type, bool> _typeHasMaskAttributeCache = new();

    /// <summary>
    /// Applies masking to a value based on parameter name, type attributes, and configuration patterns.
    /// This is the main entry point for all masking operations.
    /// </summary>
    /// <param name="value">The value to potentially mask.</param>
    /// <param name="parameterName">The parameter name (for parameter-level masking).</param>
    /// <param name="config">The logging configuration.</param>
    /// <returns>The original value or a masked version depending on masking rules.</returns>
    internal static object? ApplyMasking(
        this object? value,
        string? parameterName,
        LoggingConfig config)
    {
        if (value == null)
        {
            return null;
        }

        // Fast path: check if any masking is configured at all
        if (!IsMaskingConfigured(config))
        {
            return value;
        }

        var valueType = value.GetType();

        // Check parameter-level masking (cases 2 & 3)
        if (!string.IsNullOrEmpty(parameterName))
        {
            var parameterMaskReplacement = GetParameterMaskReplacement(parameterName, valueType, config);
            if (parameterMaskReplacement != null)
            {
                return parameterMaskReplacement;
            }
        }

        // Check if value has [Mask] attribute on parameter level
        if (HasParameterMaskAttribute(valueType))
        {
            return GetDefaultMaskReplacement(config);
        }

        // Check property-level masking (cases 1 & 3)
        return HasMaskedProperties(valueType) ? CreateMaskedCopy(value, config) : value;
    }

    /// <summary>
    /// Applies masking specifically for method parameters with parameter info.
    /// Used during input parameter processing in the interceptor.
    /// </summary>
    /// <param name="value">The parameter value to potentially mask.</param>
    /// <param name="parameterInfo">The parameter metadata.</param>
    /// <param name="config">The logging configuration.</param>
    /// <returns>The original value or a masked version depending on masking rules.</returns>
    internal static object? ApplyParameterMasking(
        this object? value,
        ParameterInfo? parameterInfo,
        LoggingConfig config)
    {
        if (value == null || parameterInfo == null)
        {
            return value;
        }

        // Fast path: check if any masking is configured
        if (!IsMaskingConfigured(config))
        {
            return value;
        }

        // Check the parameter attribute first (the highest priority)
        if (parameterInfo.GetCustomAttribute<MaskAttribute>() != null)
        {
            return parameterInfo.GetCustomAttribute<MaskAttribute>()?.Replacement ?? GetDefaultMaskReplacement(config);
        }

        // Delegate to general masking logic
        return value.ApplyMasking(parameterInfo.Name, config);
    }

    /// <summary>
    /// Checks if any masking is configured in the system to enable fast-path optimization.
    /// </summary>
    /// <param name="config">The logging configuration to check.</param>
    /// <returns>True if any masking configuration is present; false otherwise.</returns>
    private static bool IsMaskingConfigured(LoggingConfig config) =>
        config.Services.Values.Any(HasMaskingPatterns) ||
        _typeHasMaskAttributeCache.Values.Any(hasAttribute => hasAttribute);

    /// <summary>
    /// Checks if the specified interception config has any masking patterns configured.
    /// </summary>
    /// <param name="interceptionConfig">The interception configuration to check.</param>
    /// <returns>True if masking patterns are configured; false otherwise.</returns>
    private static bool HasMaskingPatterns(InterceptionConfig interceptionConfig) =>
        interceptionConfig.MaskParameterPatterns.Count > 0 ||
        interceptionConfig.MaskPropertyPatterns.Count > 0;

    /// <summary>
    /// Gets the mask replacement text for a parameter based on configuration patterns.
    /// </summary>
    /// <param name="parameterName">The parameter name to check.</param>
    /// <param name="parameterType">The parameter type.</param>
    /// <param name="config">The logging configuration.</param>
    /// <returns>The mask replacement text if the parameter should be masked; null otherwise.</returns>
    private static string? GetParameterMaskReplacement(
        string parameterName,
        Type parameterType,
        LoggingConfig config)
    {
        // Find the matching configuration for the parameter's declaring type
        var matchingConfig = FindMatchingConfigurationForType(parameterType, config);
        if (matchingConfig == null)
        {
            return null;
        }

        // Check if parameter name matches any masking patterns
        return matchingConfig.MaskParameterPatterns.Any(pattern => MatchesPattern(parameterName, pattern))
            ? matchingConfig.MaskReplacement
            : null;
    }

    /// <summary>
    /// Finds the interception configuration that applies to the specified type.
    /// </summary>
    /// <param name="type">The type to find configuration for.</param>
    /// <param name="config">The logging configuration.</param>
    /// <returns>The matching interception config; null if none found.</returns>
    private static InterceptionConfig? FindMatchingConfigurationForType(Type type, LoggingConfig config)
    {
        var typeName = type.FullName;
        if (string.IsNullOrEmpty(typeName))
        {
            return null;
        }

        // Check the exact match first
        if (config.Services.TryGetValue(typeName, out var exactConfig))
        {
            return exactConfig;
        }

        // Check wildcard patterns
        foreach (var (pattern, interceptionConfig) in config.Services)
        {
            if (pattern.EndsWith('*') &&
                typeName.StartsWith(pattern[..^1], StringComparison.InvariantCulture))
            {
                return interceptionConfig;
            }
        }

        return null;
    }

    /// <summary>
    /// Checks if a type has any parameter marked with a [Mask] attribute.
    /// Uses caching for performance optimization.
    /// </summary>
    /// <param name="type">The type to check.</param>
    /// <returns>True if the type has a [Mask] attribute; false otherwise.</returns>
    private static bool HasParameterMaskAttribute(Type type) =>
        _typeHasMaskAttributeCache.GetOrAdd(type, t =>
            t.GetCustomAttribute<MaskAttribute>() != null);

    /// <summary>
    /// Checks if a type has any properties marked with the [Mask] attribute.
    /// Uses caching for performance optimization.
    /// </summary>
    /// <param name="type">The type to check.</param>
    /// <returns>True if the type has properties with [Mask] attribute; false otherwise.</returns>
    private static bool HasMaskedProperties(Type type) =>
        _typeHasMaskedPropsCache.GetOrAdd(type, t =>
            t.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Any(prop => prop.GetCustomAttribute<MaskAttribute>() != null));

    /// <summary>
    /// Creates a masked copy of an object, replacing marked properties with mask text.
    /// Uses reflection to clone the object and apply masking to specific properties.
    /// </summary>
    /// <param name="original">The original object to mask.</param>
    /// <param name="config">The logging configuration containing mask settings.</param>
    /// <returns>A new object with masked properties.</returns>
    private static object CreateMaskedCopy(object original, LoggingConfig config)
    {
        var type = original.GetType();
        var maskedProperties = GetMaskedProperties(type);

        // If no properties need masking, return the original
        if (maskedProperties.Length == 0)
        {
            return original;
        }

        try
        {
            // Create a new instance
            var clone = Activator.CreateInstance(type);
            if (clone == null)
            {
                return original;
            }

            var defaultMaskText = GetDefaultMaskReplacement(config);

            // Copy all properties, masking the marked ones
            foreach (var prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (!prop.CanRead || !prop.CanWrite)
                {
                    continue;
                }

                var originalValue = prop.GetValue(original);

                if (maskedProperties.Contains(prop))
                {
                    // Use custom mask text from attribute or default
                    var maskAttr = prop.GetCustomAttribute<MaskAttribute>();
                    var maskText = maskAttr?.Replacement ?? defaultMaskText;

                    // Also check configuration patterns for this property
                    var configMaskText = GetPropertyMaskReplacement(prop.Name, type, config);
                    if (configMaskText != null)
                    {
                        maskText = configMaskText;
                    }

                    prop.SetValue(clone, maskText);
                }
                else
                {
                    prop.SetValue(clone, originalValue);
                }
            }

            return clone;
        }
        catch (Exception)
        {
            // If cloning fails, return an original object
            return original;
        }
    }

    /// <summary>
    /// Gets the properties of a type that are marked for masking.
    /// Uses caching for performance optimization.
    /// </summary>
    /// <param name="type">The type to get masked properties for.</param>
    /// <returns>Array of properties that should be masked.</returns>
    private static PropertyInfo[] GetMaskedProperties(Type type) =>
        _maskedPropertiesCache.GetOrAdd(type, t =>
            [
                .. t.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                    .Where(prop => prop.GetCustomAttribute<MaskAttribute>() != null),
            ]);

    /// <summary>
    /// Gets the mask replacement text for a property based on configuration patterns.
    /// </summary>
    /// <param name="propertyName">The property name to check.</param>
    /// <param name="declaringType">The type that declares the property.</param>
    /// <param name="config">The logging configuration.</param>
    /// <returns>The mask replacement text if property should be masked; null otherwise.</returns>
    private static string? GetPropertyMaskReplacement(
        string propertyName,
        Type declaringType,
        LoggingConfig config)
    {
        var matchingConfig = FindMatchingConfigurationForType(declaringType, config);

        return matchingConfig?.MaskPropertyPatterns.Any(pattern => MatchesPattern(propertyName, pattern)) == true
            ? matchingConfig.MaskReplacement
            : null;
    }

    /// <summary>
    /// Checks if a name matches a pattern. Supports wildcards (*).
    /// </summary>
    /// <param name="name">The name to check.</param>
    /// <param name="pattern">The pattern to match against.</param>
    /// <returns>True if the name matches the pattern; false otherwise.</returns>
    private static bool MatchesPattern(string name, string pattern)
    {
        if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(pattern))
        {
            return false;
        }

        // Exact match
        if (pattern == name)
        {
            return true;
        }

        // Wildcard patterns
        if (!pattern.Contains('*'))
        {
            return false;
        }

        if (pattern.StartsWith('*') && pattern.EndsWith('*'))
        {
            return name.Contains(pattern[1..^1], StringComparison.InvariantCultureIgnoreCase);
        }

        return !pattern.EndsWith('*')
            ? pattern.StartsWith('*') && name.EndsWith(pattern[1..], StringComparison.InvariantCultureIgnoreCase)
            : name.StartsWith(pattern[..^1], StringComparison.InvariantCultureIgnoreCase);
    }

    /// <summary>
    /// Gets the default mask replacement text from configuration.
    /// </summary>
    /// <param name="config">The logging configuration.</param>
    /// <returns>The default mask replacement text.</returns>
    private static string GetDefaultMaskReplacement(LoggingConfig config)
    {
        // Try to get from any configured service or use hardcoded default
        var configWithMask = config.Services.Values.FirstOrDefault(c => !string.IsNullOrEmpty(c.MaskReplacement));
        return configWithMask?.MaskReplacement ?? "***MASKED***";
    }
}
