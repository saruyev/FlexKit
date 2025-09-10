using FlexKit.Configuration.Providers.Yaml.Sources;
using FlexKit.IntegrationTests.Utils;
using Reqnroll;
using System.Text;
using FlexKit.Configuration.Core;
using Microsoft.Extensions.Configuration;
// ReSharper disable MethodTooLong
// ReSharper disable TooManyArguments
// ReSharper disable ComplexConditionExpression
// ReSharper disable TooManyDeclarations

namespace FlexKit.Configuration.Providers.Yaml.IntegrationTests.Utils;

/// <summary>
/// Test configuration builder specifically for YAML configuration testing.
/// Inherits from BaseTestConfigurationBuilder to provide YAML-specific configuration methods.
/// </summary>
/// <remarks>
/// Initializes a new instance of YamlTestConfigurationBuilder.
/// </remarks>
/// <param name="scenarioContext">Optional scenario context for automatic cleanup</param>
public class YamlTestConfigurationBuilder(ScenarioContext? scenarioContext = null) : BaseTestConfigurationBuilder<YamlTestConfigurationBuilder>(scenarioContext)
{
    public YamlTestConfigurationBuilder() : this(null) { }

    /// <summary>
    /// Adds an existing YAML file as a configuration source.
    /// </summary>
    /// <param name="path">Path to the YAML file</param>
    /// <param name="optional">Whether the file is optional</param>
    /// <returns>This builder for method chaining</returns>
    public YamlTestConfigurationBuilder AddYamlFile(string path, bool optional = true)
    {
        var yamlSource = new YamlConfigurationSource
        {
            Path = path,
            Optional = optional
        };

        return AddSource(yamlSource);
    }

    /// <summary>
    /// Creates a temporary YAML file with the provided content and adds it as a configuration source.
    /// </summary>
    /// <param name="yamlContent">YAML file content</param>
    /// <param name="optional">Whether the file is optional</param>
    /// <returns>This builder for method chaining</returns>
    public YamlTestConfigurationBuilder AddTempYamlFile(string yamlContent, bool optional = true)
    {
        var tempFile = CreateTempFile(yamlContent, ".yaml");
        return AddYamlFile(tempFile, optional);
    }

    /// <summary>
    /// Creates a temporary YAML file from a dictionary and adds it as a configuration source.
    /// </summary>
    /// <param name="yamlData">Dictionary of configuration data</param>
    /// <param name="optional">Whether the file is optional</param>
    /// <returns>This builder for method chaining</returns>
    public YamlTestConfigurationBuilder AddTempYamlFile(Dictionary<string, string?> yamlData, bool optional = true)
    {
        var yamlContent = ConvertDictionaryToYaml(yamlData);
        return AddTempYamlFile(yamlContent, optional);
    }

    /// <summary>
    /// Converts a flat dictionary with colon-separated keys into YAML format.
    /// </summary>
    /// <param name="data">Dictionary with colon-separated keys</param>
    /// <returns>YAML content as a string</returns>
    private static string ConvertDictionaryToYaml(Dictionary<string, string?> data)
    {
        var yaml = new StringBuilder();
        var processedKeys = new HashSet<string>();

        // Process root-level keys first
        foreach (var kvp in data.Where(kvp => !kvp.Key.Contains(':')))
        {
            yaml.AppendLine($"{kvp.Key}: {FormatYamlValue(kvp.Value)}");
            processedKeys.Add(kvp.Key);
        }

        // Group hierarchical keys by sections
        var sections = data.Keys
            .Where(k => k.Contains(':') && !processedKeys.Contains(k))
            .Select(k => k.Split(':')[0])
            .Distinct()
            .OrderBy(s => s)
            .ToList();

        foreach (var section in sections)
        {
            yaml.AppendLine($"{section}:");

            var sectionKeys = data.Keys
                .Where(k => k.StartsWith($"{section}:") && !processedKeys.Contains(k))
                .OrderBy(k => k)
                .ToList();

            foreach (var key in sectionKeys)
            {
                ProcessHierarchicalKey(yaml, key, data[key], section);
                processedKeys.Add(key);
            }
        }

        return yaml.ToString();
    }

    /// <summary>
    /// Processes a hierarchical configuration key and adds it to the YAML output.
    /// </summary>
    /// <param name="yaml">StringBuilder for YAML content</param>
    /// <param name="key">The full configuration key</param>
    /// <param name="value">The configuration value</param>
    /// <param name="section">The root section name</param>
    private static void ProcessHierarchicalKey(StringBuilder yaml, string key, string? value, string section)
    {
        var remainingPath = key.Substring(section.Length + 1);
        var pathParts = remainingPath.Split(':');

        var indent = 2;
        for (int i = 0; i < pathParts.Length; i++)
        {
            var indentStr = new string(' ', indent);

            if (i == pathParts.Length - 1)
            {
                // Last part - add the value
                yaml.AppendLine($"{indentStr}{pathParts[i]}: {FormatYamlValue(value)}");
            }
            else
            {
                // Intermediate part - add as a section
                yaml.AppendLine($"{indentStr}{pathParts[i]}:");
                indent += 2;
            }
        }
    }

    /// <summary>
    /// Formats a value for YAML output.
    /// </summary>
    /// <param name="value">The value to format</param>
    /// <returns>Properly formatted YAML value</returns>
    private static string FormatYamlValue(string? value)
    {
        if (value == null) return "null";
        if (string.IsNullOrEmpty(value)) return "\"\"";

        // Quote strings that contain special characters or could be misinterpreted
        if (NeedsQuoting(value))
        {
            return $"\"{value.Replace("\"", "\\\"")}\"";
        }

        return value;
    }

    /// <summary>
    /// Determines if a string value needs to be quoted in YAML.
    /// </summary>
    /// <param name="value">The value to check</param>
    /// <returns>True if the value needs quoting</returns>
    private static bool NeedsQuoting(string value)
    {
        if (string.IsNullOrEmpty(value)) return true;

        // Check for characters that require quoting
        return value.Contains(' ') ||
               value.Contains(':') ||
               value.Contains('#') ||
               value.Contains('[') ||
               value.Contains(']') ||
               value.Contains('{') ||
               value.Contains('}') ||
               value.StartsWith('"') ||
               value.StartsWith('\'') ||
               value.StartsWith('*') ||
               value.StartsWith('&') ||
               IsYamlKeyword(value);
    }

    /// <summary>
    /// Checks if a value is a YAML keyword that should be quoted.
    /// </summary>
    /// <param name="value">The value to check</param>
    /// <returns>True if the value is a YAML keyword</returns>
    private static bool IsYamlKeyword(string value)
    {
        var lowerValue = value.ToLowerInvariant();
        return lowerValue is "true" or "false" or "null" or "yes" or "no" or "on" or "off";
    }

    /// <summary>
    /// Helper method to access dynamic properties safely using reflection.
    /// </summary>
    /// <param name="obj">
    ///     The current object in the navigation chain
    /// </param>
    /// <param name="propertyName">
    ///     The property/section name to navigate to
    /// </param>
    /// <returns>The property value or null if not found</returns>
    /// <summary>
    /// Helper method to navigate dynamic property access through FlexConfig sections.
    /// Handles the dynamic navigation like config.app.title by traversing configuration sections.
    /// </summary>
    /// <returns>The next object in the chain or the final value</returns>
    public static object? GetDynamicProperty(object? obj, string propertyName)
    {
        if (obj == null) return null;

        // Handle FlexConfig objects (which implement dynamic behavior)
        if (obj is IFlexConfig flexConfig)
        {
            // Try to get as a configuration section first
            var section = flexConfig.Configuration.GetSection(propertyName);
            if (section.Exists())
            {
                // If the section has children, return it as FlexConfig for further navigation
                if (section.GetChildren().Any())
                {
                    return section.GetFlexConfiguration();
                }

                // If it's a leaf value, return the string value
                return section.Value;
            }

            // If not found as a section, try direct indexer access
            return flexConfig[propertyName];
        }

        // If we get here, we're not dealing with FlexConfig anymore
        // This shouldn't happen in normal FlexConfig navigation
        return null;
    }
}