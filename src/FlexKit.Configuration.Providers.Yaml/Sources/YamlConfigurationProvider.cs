using System.Globalization;
using Microsoft.Extensions.Configuration;
using YamlDotNet.Core;
using YamlDotNet.Serialization;

namespace FlexKit.Configuration.Providers.Yaml.Sources;

/// <summary>
/// Configuration provider that loads configuration data from YAML files.
/// Inherits from ConfigurationProvider to integrate with the .NET configuration system
/// and provides support for hierarchical YAML configuration with complex data structures.
/// </summary>
/// <remarks>
/// This provider uses YamlDotNet for parsing YAML files and supports the full range
/// of YAML features, including nested objects, arrays, and complex data types.
/// Unlike the DotEnv provider which handles flat key-value pairs, this provider
/// maintains the hierarchical structure of YAML documents.
///
/// <para>
/// <strong>YAML Features Supported:</strong>
/// <list type="bullet">
/// <item>Nested objects and hierarchical configuration</item>
/// <item>Arrays and sequences</item>
/// <item>Multiple data types (strings, numbers, booleans, null)</item>
/// <item>YAML comments (ignored during parsing)</item>
/// <item>Multi-line strings with various styles</item>
/// <item>YAML anchors and references (&amp; and *)</item>
/// </list>
/// </para>
///
/// <para>
/// <strong>Configuration Key Mapping:</strong>
/// YAML hierarchical keys are flattened to colon-separated keys for compatibility
/// with the .NET configuration system:
/// <code>
/// # YAML structure
/// database:
///   connection:
///     host: localhost
///     port: 5432
///   pool:
///     min: 5
///     max: 20
///
/// # Resulting configuration keys
/// database:connection:host = "localhost"
/// database:connection:port = "5432"
/// database:pool:min = "5"
/// database:pool:max = "20"
/// </code>
/// </para>
///
/// <para>
/// <strong>Array Handling:</strong>
/// YAML arrays are mapped to indexed configuration keys:
/// <code>
/// # YAML array
/// servers:
///   - name: web1
///     port: 8080
///   - name: web2
///     port: 8081
///
/// # Resulting configuration keys
/// servers:0:name = "web1"
/// servers:0:port = "8080"
/// servers:1:name = "web2"
/// servers:1:port = "8081"
/// </code>
/// </para>
///
/// <para>
/// <strong>Type Handling:</strong>
/// All values are stored as strings in the configuration system, following
/// .NET configuration conventions. Type conversion should be handled by
/// consuming code using FlexKit's type conversion utilities.
/// </para>
///
/// <para>
/// <strong>Error Handling:</strong>
/// <list type="bullet">
/// <item>Missing files throw FileNotFoundException when Optional = false</item>
/// <item>Invalid YAML syntax throws YamlException with details</item>
/// <item>Empty or null files result in empty configuration (no error)</item>
/// <item>Files containing only comments or whitespace are treated as empty</item>
/// </list>
/// </para>
///
/// <para>
/// <strong>Security Considerations:</strong>
/// <list type="bullet">
/// <item>YAML files should never contain sensitive data in production</item>
/// <item>Use appropriate file permissions to protect configuration files</item>
/// <item>Be cautious with YAML features like anchors in untrusted input</item>
/// <item>Consider using encrypted configuration providers for sensitive data</item>
/// </list>
/// </para>
///
/// <para>
/// <strong>Best Practices:</strong>
/// <list type="bullet">
/// <item>Use YAML for complex, hierarchical configuration structures</item>
/// <item>Combine with other providers for complete configuration coverage</item>
/// <item>Validate YAML syntax in CI/CD pipelines</item>
/// <item>Use meaningful names and document configuration structure</item>
/// </list>
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Example YAML file (appsettings.yaml):
/// application:
///   name: "My Application"
///   version: "1.0.0"
///   debug: true
///
/// database:
///   host: localhost
///   port: 5432
///   credentials:
///     username: app_user
///     # Password should come from environment variables
///
/// features:
///   - caching
///   - logging
///   - metrics
///
/// endpoints:
///   api:
///     url: "https://api.example.com"
///     timeout: 5000
///   auth:
///     url: "https://auth.example.com"
///     timeout: 3000
/// </code>
/// </example>
/// <remarks>
/// Initializes a new instance of the YamlConfigurationProvider class.
/// </remarks>
/// <param name="source">The configuration source containing YAML file settings and options.</param>
/// <exception cref="ArgumentNullException">Thrown when the source is null.</exception>
internal class YamlConfigurationProvider(YamlConfigurationSource source) : ConfigurationProvider
{
    /// <summary>
    /// The configuration source that defines the YAML file location and loading options.
    /// Used to access a file path, optional flag, and other source-specific settings.
    /// </summary>
    private readonly YamlConfigurationSource _source = source ?? throw new ArgumentNullException(nameof(source));

    /// <summary>
    /// Loads configuration data from the YAML file specified in the configuration source.
    /// Parses the YAML file structure and flattens it into the key-value pairs expected
    /// by the .NET configuration system.
    /// </summary>
    /// <remarks>
    /// This method is called by the configuration system during the Build() phase.
    /// It performs the following operations:
    ///
    /// <list type="number">
    /// <item>Checks if the YAML file exists (handles an Optional flag)</item>
    /// <item>Reads the file content as text</item>
    /// <item>Parses the YAML content using YamlDotNet</item>
    /// <item>Flattens the hierarchical structure to configuration keys</item>
    /// <item>Stores the key-value pairs in the provider's Data dictionary</item>
    /// </list>
    ///
    /// <para>
    /// <strong>File Existence Handling:</strong>
    /// <list type="bullet">
    /// <item>If a file exists: Parse and load configuration data</item>
    /// <item>If a file doesn't exist and Optional = true: Load empty configuration (no error)</item>
    /// <item>If a file doesn't exist and Optional = false: Throw FileNotFoundException</item>
    /// </list>
    /// </para>
    ///
    /// <para>
    /// <strong>YAML Parsing:</strong>
    /// Uses YamlDotNet with standard settings to parse YAML content. The parser
    /// is configured to handle common YAML features while maintaining compatibility
    /// with the .NET configuration system.
    /// </para>
    ///
    /// <para>
    /// <strong>Error Propagation:</strong>
    /// All exceptions from file I/O or YAML parsing are allowed to bubble up
    /// to the configuration building process, ensuring that configuration errors
    /// are detected early in the application lifecycle.
    /// </para>
    /// </remarks>
    /// <exception cref="FileNotFoundException">
    /// Thrown when the YAML file doesn't exist and <see cref="YamlConfigurationSource.Optional"/> is <c>false</c>.
    /// </exception>
    /// <exception cref="UnauthorizedAccessException">
    /// Thrown when the application doesn't have permission to read the YAML file.
    /// </exception>
    /// <exception cref="YamlException">
    /// Thrown when the YAML file contains invalid syntax or structure.
    /// </exception>
    /// <exception cref="IOException">
    /// Thrown when other I/O errors occur while reading the file.
    /// </exception>
    /// <example>
    /// <code>
    /// // This method is typically called automatically by the configuration system
    /// var source = new YamlConfigurationSource { Path = "config.yaml", Optional = false };
    /// var provider = new YamlConfigurationProvider(source);
    ///
    /// // Load configuration data
    /// provider.Load(); // This calls the Load() method
    ///
    /// // Access loaded data
    /// provider.TryGet("database:host", out var host);
    /// provider.TryGet("application:name", out var appName);
    /// </code>
    /// </example>
    /// <exception cref="InvalidDataException">Could not parse YAML file.</exception>
    public override void Load()
    {
        if (!ValidateFileExists())
        {
            return;
        }

        try
        {
            var yamlContent = File.ReadAllText(_source.Path);
            var yamlObject = ParseYamlContent(yamlContent);
            Data = CreateConfigurationData(yamlObject);
        }
        catch (Exception ex) when (!(ex is FileNotFoundException || ex is UnauthorizedAccessException))
        {
            throw new InvalidDataException(
                $"Failed to parse YAML configuration file '{_source.Path}': {ex.Message}",
                ex);
        }
    }

    /// <summary>
    /// Validates if the YAML file exists and handles the optional file logic.
    /// </summary>
    /// <returns>True if processing should continue, false if empty configuration should be loaded.</returns>
    /// <exception cref="FileNotFoundException">The configuration file was not found.</exception>
    private bool ValidateFileExists()
    {
        if (File.Exists(_source.Path))
        {
            return true;
        }

        if (!_source.Optional)
        {
            throw new FileNotFoundException(
                $"The configuration file '{_source.Path}' was not found and is not optional.");
        }

        Data = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        return false;
    }

    /// <summary>
    /// Parses YAML content and returns the deserialized object structure.
    /// </summary>
    /// <param name="yamlContent">The YAML content to parse.</param>
    /// <returns>The deserialized YAML object or null for empty content.</returns>
    private static object? ParseYamlContent(string yamlContent)
    {
        if (string.IsNullOrWhiteSpace(yamlContent))
        {
            return null;
        }

        var deserializer = new DeserializerBuilder()
            .IgnoreUnmatchedProperties()
            .Build();

        return deserializer.Deserialize<object>(yamlContent);
    }

    /// <summary>
    /// Creates the configuration data dictionary from the parsed YAML object.
    /// </summary>
    /// <param name="yamlObject">The parsed YAML object structure.</param>
    /// <returns>A dictionary containing the flattened configuration keys and values.</returns>
    private static Dictionary<string, string?> CreateConfigurationData(object? yamlObject)
    {
        var configData = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        FlattenYamlObject(yamlObject, configData, string.Empty);
        return configData;
    }

    /// <summary>
    /// Recursively flattens a hierarchical YAML object structure into flat configuration keys.
    /// Converts nested objects, arrays, and scalar values into the colon-separated key format
    /// expected by the .NET configuration system.
    /// </summary>
    /// <param name="obj">The YAML object to flatten (can be dictionary, array, or scalar value).</param>
    /// <param name="data">The dictionary to store the flattened key-value pairs.</param>
    /// <param name="prefix">The current key prefix for nested objects (used recursively).</param>
    /// <remarks>
    /// This method handles the following YAML structures:
    ///
    /// <para>
    /// <strong>Scalar Values:</strong>
    /// Direct mapping to configuration keys with string conversion.
    /// </para>
    ///
    /// <para>
    /// <strong>Dictionaries/Objects:</strong>
    /// Keys are appended to the prefix with colon separation.
    /// Nested dictionaries are processed recursively.
    /// </para>
    ///
    /// <para>
    /// <strong>Arrays/Sequences:</strong>
    /// Elements are indexed starting from 0 and appended to the prefix.
    /// Array elements can be scalars, objects, or nested arrays.
    /// </para>
    ///
    /// <para>
    /// <strong>Null Values:</strong>
    /// Represented as null in the configuration dictionary.
    /// </para>
    ///
    /// <para>
    /// <strong>Type Conversion:</strong>
    /// All non-null values are converted to strings using invariant culture
    /// to ensure consistent parsing across different system locales.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Input YAML structure (as objects):
    /// var yamlData = new Dictionary&lt;string, object&gt;
    /// {
    ///     ["app"] = new Dictionary&lt;string, object&gt;
    ///     {
    ///         ["name"] = "MyApp",
    ///         ["features"] = new[] { "auth", "logging" }
    ///     },
    ///     ["version"] = "1.0.0"
    /// };
    ///
    /// // Resulting flattened keys:
    /// // app:name = "MyApp"
    /// // app:features:0 = "auth"
    /// // app:features:1 = "logging"
    /// // version = "1.0.0"
    /// </code>
    /// </example>
    private static void FlattenYamlObject(
        object? obj,
        Dictionary<string, string?> data,
        string prefix)
    {
        switch (obj)
        {
            case null:
                HandleNullValue(data, prefix);
                break;
            case IDictionary<object, object> dict:
                HandleDictionary(dict, data, prefix);
                break;
            case IList<object> list:
                HandleArray(list, data, prefix);
                break;
            default:
                HandleScalarValue(obj, data, prefix);
                break;
        }
    }

    /// <summary>
    /// Handles null values in the YAML structure.
    /// </summary>
    /// <param name="data">The dictionary to store the flattened key-value pairs.</param>
    /// <param name="prefix">The current key prefix for nested objects (used recursively).</param>
    private static void HandleNullValue(
        Dictionary<string, string?> data,
        string prefix)
    {
        if (string.IsNullOrEmpty(prefix))
        {
            return;
        }

        data[prefix] = null;
    }

    /// <summary>
    /// Handles dictionary/object structures in the YAML.
    /// </summary>
    /// <param name="dict">The YAML dictionary to flatten.</param>
    /// <param name="data">The dictionary to store the flattened key-value pairs.</param>
    /// <param name="prefix">The current key prefix for nested objects (used recursively).</param>
    private static void HandleDictionary(
        IDictionary<object, object> dict,
        Dictionary<string, string?> data,
        string prefix)
    {
        foreach (var kvp in dict)
        {
            var key = kvp.Key.ToString() ?? string.Empty;
            var newPrefix = string.IsNullOrEmpty(prefix) ? key : $"{prefix}:{key}";
            FlattenYamlObject(kvp.Value, data, newPrefix);
        }
    }

    /// <summary>
    /// Handles array/list structures in the YAML.
    /// </summary>
    /// <param name="list">The YAML list to flatten.</param>
    /// <param name="data">The dictionary to store the flattened key-value pairs.</param>
    /// <param name="prefix">The current key prefix for nested objects (used recursively).</param>
    private static void HandleArray(
        IList<object> list,
        Dictionary<string, string?> data,
        string prefix)
    {
        for (var i = 0; i < list.Count; i++)
        {
            var newPrefix = string.IsNullOrEmpty(prefix) ?
                i.ToString(CultureInfo.InvariantCulture) : $"{prefix}:{i}";
            FlattenYamlObject(list[i], data, newPrefix);
        }
    }

    /// <summary>
    /// Handles scalar values (strings, numbers, booleans) in the YAML.
    /// </summary>
    /// <param name="obj">The YAML scalar value to flatten.</param>
    /// <param name="data">The dictionary to store the flattened key-value pairs.</param>
    /// <param name="prefix">The current key prefix for nested objects (used recursively).</param>
    private static void HandleScalarValue(
        object obj,
        Dictionary<string, string?> data,
        string prefix) => data[prefix] = obj.ToString();
}
