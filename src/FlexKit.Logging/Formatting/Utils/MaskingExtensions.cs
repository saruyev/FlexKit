using System.Collections.Concurrent;
using System.Reflection;
using FlexKit.Logging.Configuration;
using FlexKit.Logging.Interception;
using FlexKit.Logging.Interception.Attributes;

namespace FlexKit.Logging.Formatting.Utils;

/// <summary>
/// Provides high-performance masking functionality for sensitive data in log entries.
/// Supports both attribute-based and configuration-based masking with minimal performance overhead.
/// </summary>
internal static class MaskingExtensions
{
    /// <summary>
    /// A thread-safe cache that stores whether a given type has properties marked with the [Mask] attribute.
    /// </summary>
    /// <remarks>
    /// This <see cref="ConcurrentDictionary{TKey, TValue}"/> is used to improve performance by avoiding
    /// repetitive reflection-based checks for property attributes on types. Each type is evaluated once,
    /// and the result is stored for further lookups.
    /// </remarks>
    private static readonly ConcurrentDictionary<Type, bool> _typeHasMaskedPropsCache = new();

    /// <summary>
    /// A thread-safe cache that stores the properties of a type that are marked with the [Mask] attribute.
    /// </summary>
    /// <remarks>
    /// This <see cref="ConcurrentDictionary{TKey, TValue}"/> is used to enhance performance by caching
    /// the results of reflection-based lookups for properties marked with the [Mask] attribute. The cache
    /// ensures that each type is processed only once, and the following lookups retrieve the stored result
    /// directly, avoiding redundant reflection operations.
    /// </remarks>
    private static readonly ConcurrentDictionary<Type, PropertyInfo[]> _maskedPropertiesCache = new();

    /// <summary>
    /// A thread-safe cache that stores whether a specific type has one or more members marked with
    /// the [Mask] attribute.
    /// </summary>
    /// <remarks>
    /// This <see cref="ConcurrentDictionary{TKey, TValue}"/> is used to improve performance by caching
    /// the results of reflection-based checks for the presence of the [Mask] attribute on a type.
    /// Once a type is evaluated, the result is stored to prevent redundant evaluations during further operations.
    /// </remarks>
    private static readonly ConcurrentDictionary<Type, bool> _typeHasMaskAttributeCache = new();

    /// <summary>
    /// Applies masking specifically for method parameters with parameter info.
    /// Used during input parameter processing in the interceptor.
    /// </summary>
    /// <param name="value">The parameter value to potentially mask.</param>
    /// <param name="parameterInfo">The parameter metadata.</param>
    /// <param name="context">The input context containing method arguments and metadata.</param>
    /// <returns>The original value or a masked version depending on masking rules.</returns>
    internal static object? ApplyParameterMasking(
        this object? value,
        ParameterInfo? parameterInfo,
        MethodLoggingInterceptor.InputContext context)
    {
        if (value == null || parameterInfo == null)
        {
            return value;
        }

        if (HasParameterMaskAttribute(value.GetType()))
        {
            return GetDefaultMaskReplacement(context.Config);
        }

        if (parameterInfo.GetCustomAttribute<MaskAttribute>() != null)
        {
            return parameterInfo.GetCustomAttribute<MaskAttribute>()?.Replacement ??
                   GetDefaultMaskReplacement(context.Config);
        }

        // Check parameter-level masking (cases 2 & 3)
        var parameterMaskReplacement = GetParameterMaskReplacement(
            parameterInfo.Name ?? "",
            context.DeclaringType,
            context.Config);

        if (parameterMaskReplacement != null)
        {
            return parameterMaskReplacement;
        }

        // Check property-level masking (cases 1 & 3)
        return HasMaskedProperties(value.GetType()) ? CreateMaskedCopy(value, context.Config) : value;
    }

    /// <summary>
    /// Applies masking to output values based on configuration patterns and attributes.
    /// Used for masking return values that may contain sensitive data.
    /// </summary>
    /// <param name="value">The output value to potentially mask.</param>
    /// <param name="declaringType">The type that declares the method (for config lookup).</param>
    /// <param name="config">The logging configuration.</param>
    /// <returns>The original value or a masked version depending on masking rules.</returns>
    internal static object? ApplyOutputMasking(
        this object? value,
        Type? declaringType,
        LoggingConfig config)
    {
        if (value == null)
        {
            return null;
        }

        // Check if the output value should be masked based on configuration patterns
        var outputMaskReplacement = GetOutputMaskReplacement(value, declaringType, config);
        if (outputMaskReplacement != null)
        {
            return outputMaskReplacement;
        }

        // Check property-level masking (cases 1 & 3)
        return HasMaskedProperties(value.GetType()) ? CreateMaskedCopy(value, config) : value;
    }

    /// <summary>
    /// Gets the mask replacement text for output values based on configuration patterns.
    /// </summary>
    /// <param name="outputValue">The output value to check.</param>
    /// <param name="declaringType">The type that declares the method (service class).</param>
    /// <param name="config">The logging configuration.</param>
    /// <returns>The mask replacement text if output should be masked; null otherwise.</returns>
    private static string? GetOutputMaskReplacement(
        object? outputValue,
        Type? declaringType,
        LoggingConfig config)
    {
        if (outputValue == null || declaringType == null)
        {
            return null;
        }

        var matchingConfig = FindMatchingConfigurationForType(declaringType, config);

        return matchingConfig?.MaskOutputPatterns.Any(pattern => MatchesOutputPattern(outputValue, pattern)) == true
            ? matchingConfig.MaskReplacement
            : null;
    }

    /// <summary>
    /// Checks if an output value matches a masking pattern.
    /// </summary>
    /// <param name="outputValue">The output value to check.</param>
    /// <param name="pattern">The pattern to match against.</param>
    /// <returns>True if the output value matches the pattern; false otherwise.</returns>
    private static bool MatchesOutputPattern(
        object outputValue,
        string pattern)
    {
        var outputString = outputValue.ToString();

        if (string.IsNullOrEmpty(outputString))
        {
            return false;
        }

        // For connection strings, check if they contain sensitive patterns
        if (pattern.Equals("*connection*", StringComparison.OrdinalIgnoreCase))
        {
            return outputString.Contains("Password=", StringComparison.InvariantCultureIgnoreCase) ||
                   outputString.Contains("Pwd=", StringComparison.InvariantCultureIgnoreCase);
        }

        // Add more output pattern matching as needed
        return MatchesPattern(outputString, pattern);
    }

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
    private static InterceptionConfig? FindMatchingConfigurationForType(
        Type type,
        LoggingConfig config)
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
        _typeHasMaskAttributeCache.GetOrAdd(
            type,
            t =>
                t.GetCustomAttribute<MaskAttribute>() != null);

    /// <summary>
    /// Checks if a type has any properties marked with the [Mask] attribute.
    /// Uses caching for performance optimization.
    /// </summary>
    /// <param name="type">The type to check.</param>
    /// <returns>True if the type has properties with [Mask] attribute; false otherwise.</returns>
    private static bool HasMaskedProperties(Type type) =>
        _typeHasMaskedPropsCache.GetOrAdd(
            type,
            t =>
                t.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                    .Any(prop => prop.GetCustomAttribute<MaskAttribute>() != null));

    /// <summary>
    /// Creates a masked copy of an object, replacing marked properties with mask text.
    /// Uses reflection to clone the object and apply masking to specific properties.
    /// </summary>
    /// <param name="original">The original object to mask.</param>
    /// <param name="config">The logging configuration containing mask settings.</param>
    /// <returns>A new object with masked properties.</returns>
    private static object CreateMaskedCopy(
        object original,
        LoggingConfig config)
    {
        var context = new MaskingContext(
            original,
            config,
            original.GetType(),
            GetMaskedProperties(original.GetType()),
            GetDefaultMaskReplacement(config));

        // If no properties need masking, return the original
        if (context.MaskedProperties.Length == 0)
        {
            return original;
        }

        try
        {
            return CreateClone(context);
        }
        catch (Exception)
        {
            // If cloning fails, return an original object
            return original;
        }
    }

    /// <summary>
    /// Creates a clone of the original object with properties copied from the source object.
    /// Applies masking to properties indicated by the masking context.
    /// </summary>
    /// <param name="context">
    /// The masking context containing the original object, its type, configuration, masked properties,
    /// and default mask text.
    /// </param>
    /// <returns>
    /// A new object instance with copied properties, where masked properties are set according to the masking rules.
    /// </returns>
    private static object CreateClone(MaskingContext context)
    {
        // Create a new instance
        var clone = Activator.CreateInstance(context.Type);
        if (clone == null)
        {
            return context.Original;
        }

        // Copy all properties, masking the marked ones
        foreach (var prop in context.Type.GetProperties(
                     BindingFlags.Public | BindingFlags.Instance))
        {
            if (!prop.CanRead || !prop.CanWrite)
            {
                continue;
            }

            ProcessPropertyValue(context, prop, clone);
        }

        return clone;
    }

    /// <summary>
    /// Processes the specified property value during the creation of a cloned object.
    /// Determines whether the property should be masked or copied as is,
    /// based on the supplied masking context.
    /// </summary>
    /// <param name="context">
    /// The masking context containing the original object, configuration, masked properties, and masking rules.
    /// </param>
    /// <param name="prop">The property metadata representing the property to be processed.</param>
    /// <param name="clone">The cloned object where the processed property value will be set.</param>
    private static void ProcessPropertyValue(
        MaskingContext context,
        PropertyInfo prop,
        object clone)
    {
        var originalValue = prop.GetValue(context.Original);

        if (context.MaskedProperties.Contains(prop))
        {
            // Use custom mask text from attribute or default
            prop.SetMaskText(context, clone);
        }
        else
        {
            prop.SetValue(clone, originalValue);
        }
    }

    /// <summary>
    /// Sets the masking text on the given property of an object clone, using masking rules
    /// from the property's custom attribute, default context settings, or configuration patterns.
    /// </summary>
    /// <param name="prop">The property to apply the masking text to.</param>
    /// <param name="context">The context containing the masking configurations and default rules.</param>
    /// <param name="clone">The cloned object where the masked value will be set.</param>
    private static void SetMaskText(
        this PropertyInfo prop,
        MaskingContext context,
        object clone)
    {
        var maskAttr = prop.GetCustomAttribute<MaskAttribute>();
        var maskText = maskAttr?.Replacement ?? context.DefaultMaskText;

        // Also check configuration patterns for this property
        var configMaskText = GetPropertyMaskReplacement(prop.Name, context.Type, context.Config);
        if (configMaskText != null)
        {
            maskText = configMaskText;
        }

        prop.SetValue(clone, maskText);
    }

    /// <summary>
    /// Gets the properties of a type that are marked for masking.
    /// Uses caching for performance optimization.
    /// </summary>
    /// <param name="type">The type to get masked properties for.</param>
    /// <returns>Array of properties that should be masked.</returns>
    private static PropertyInfo[] GetMaskedProperties(Type type) =>
        _maskedPropertiesCache.GetOrAdd(
            type,
            t =>
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
    private static bool MatchesPattern(
        string name,
        string pattern)
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
    private static string GetDefaultMaskReplacement(LoggingConfig config) =>
        config
            .Services
            .Values
            .FirstOrDefault(c => !string.IsNullOrEmpty(c.MaskReplacement))?
            .MaskReplacement ?? "***MASKED***";

    /// <summary>
    /// Represents the contextual information required for applying masking logic to sensitive
    /// properties within an object. This includes the original object, associated logging configuration,
    /// metadata for the object's type, properties flagged for masking, and a default mask replacement text.
    /// </summary>
    /// <param name="Original">The original object to mask.</param>
    /// <param name="Config">The logging configuration containing mask settings.</param>
    /// <param name="Type">The type of the original object.</param>
    /// <param name="MaskedProperties">Array of properties that should be masked.</param>
    /// <param name="DefaultMaskText">The default mask replacement text.</param>
    private sealed record MaskingContext(
        object Original,
        LoggingConfig Config,
        Type Type,
        PropertyInfo[] MaskedProperties,
        string DefaultMaskText);
}
