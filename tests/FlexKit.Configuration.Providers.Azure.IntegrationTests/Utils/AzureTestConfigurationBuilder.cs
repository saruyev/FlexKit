using FlexKit.Configuration.Core;
using System.Text.Json;
using JetBrains.Annotations;
// ReSharper disable NullableWarningSuppressionIsUsed

namespace FlexKit.Configuration.Providers.Azure.IntegrationTests.Utils;

/// <summary>
/// Helper class for extracting dynamic property values from FlexConfiguration in tests.
/// Contains static utility methods for testing Azure configuration scenarios.
/// </summary>
public static class AzureTestHelper
{
    /// <summary>
    /// Gets dynamic property value from FlexConfiguration for testing.
    /// </summary>
    /// <param name="flexConfig">FlexConfiguration instance</param>
    /// <param name="propertyPath">Dot-separated property path</param>
    /// <returns>Property value or null if not found</returns>
    public static object? GetDynamicProperty(IFlexConfig flexConfig, string propertyPath)
    {
        dynamic config = flexConfig;
        var parts = propertyPath.Split('.');
        
        object? current = config;
        foreach (var part in parts)
        {
            if (current == null) return null;
            
            try
            {
                current = ((dynamic)current)[part];
            }
            catch
            {
                return null;
            }
        }
        
        return current;
    }

    /// <summary>
    /// Flattens nested configuration data into configuration key format.
    /// </summary>
    /// <param name="data">Nested configuration data</param>
    /// <param name="prefix">Key prefix for nested data</param>
    /// <returns>Flattened configuration dictionary</returns>
    public static Dictionary<string, string?> FlattenConfiguration(Dictionary<string, object> data, string prefix = "")
    {
        var result = new Dictionary<string, string?>();

        foreach (var kvp in data)
        {
            var key = string.IsNullOrEmpty(prefix) ? kvp.Key : $"{prefix}:{kvp.Key}";

            if (kvp.Value is JsonElement jsonElement)
            {
                HandleJsonElement(result, key, jsonElement);
            }
            else if (kvp.Value is Dictionary<string, object> nestedDict)
            {
                var nested = FlattenConfiguration(nestedDict, key);
                foreach (var nestedKvp in nested)
                {
                    result[nestedKvp.Key] = nestedKvp.Value;
                }
            }
            else
            {
                result[key] = kvp.Value?.ToString();
            }
        }

        return result;
    }

    /// <summary>
    /// Handles JsonElement conversion to configuration values.
    /// </summary>
    /// <param name="result">Result dictionary to populate</param>
    /// <param name="key">Configuration key</param>
    /// <param name="jsonElement">JsonElement to process</param>
    private static void HandleJsonElement(Dictionary<string, string?> result, string key, JsonElement jsonElement)
    {
        switch (jsonElement.ValueKind)
        {
            case JsonValueKind.Object:
                var nestedDict = new Dictionary<string, object>();
                foreach (var property in jsonElement.EnumerateObject())
                {
                    nestedDict[property.Name] = property.Value;
                }
                var nested = FlattenConfiguration(nestedDict, key);
                foreach (var nestedKvp in nested)
                {
                    result[nestedKvp.Key] = nestedKvp.Value;
                }
                break;
            
            case JsonValueKind.Array:
                var index = 0;
                foreach (var arrayElement in jsonElement.EnumerateArray())
                {
                    HandleJsonElement(result, $"{key}:{index}", arrayElement);
                    index++;
                }
                break;
            
            default:
                result[key] = jsonElement.ToString();
                break;
        }
    }
}
