// <copyright file="FlexConfiguration.cs" company="Michael Saruyev">
// Copyright (c) Michael Saruyev. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

using System.Dynamic;
using System.Globalization;
using FlexKit.Configuration.Conversion;
using Microsoft.Extensions.Configuration;

namespace FlexKit.Configuration.Core;

/// <summary>
/// Central configuration unit providing dynamic access to configuration properties.
/// Implements both <see cref="DynamicObject"/> and <see cref="IFlexConfig"/> to enable
/// flexible access patterns including dynamic property access, indexer access, and type conversion.
/// </summary>
/// <param name="root">The root of the current application configuration hierarchy. Must not be null.</param>
/// <remarks>
/// FlexConfiguration serves as the core implementation of the FlexKit configuration system,
/// bridging the gap between Microsoft's structured IConfiguration system and dynamic,
/// developer-friendly access patterns. It provides multiple ways to access the same
/// configuration data to suit different coding styles and scenarios.
///
/// <para>
/// <strong>Access Patterns Supported:</strong>
/// <list type="table">
/// <listheader>
/// <term>Pattern</term>
/// <description>Use Case</description>
/// </listheader>
/// <item>
/// <term>Dynamic Access</term>
/// <description>config.Database.ConnectionString - Natural, IDE-friendly syntax</description>
/// </item>
/// <item>
/// <term>Indexer Access</term>
/// <description>config["Database:ConnectionString"] - Traditional configuration access</description>
/// </item>
/// <item>
/// <term>Type Conversion</term>
/// <description>config["Port"].ToType&lt;int&gt;() - Strong typing with conversion</description>
/// </item>
/// <item>
/// <term>Numeric Indexing</term>
/// <description>config[0] - Array-like access for indexed configuration</description>
/// </item>
/// </list>
/// </para>
///
/// <para>
/// <strong>Dynamic Object Implementation:</strong>
/// The class inherits from DynamicObject to provide runtime member resolution,
/// enabling natural property access syntax. This allows configuration access
/// that closely resembles object property access while maintaining the flexibility
/// of configuration-based values.
/// </para>
///
/// <para>
/// <strong>Type Safety and Conversion:</strong>
/// While dynamic access provides flexibility, the class supports explicit type
/// conversion through the ToType extension methods, allowing developers to choose
/// between convenience and type safety as appropriate for their scenarios.
/// </para>
///
/// <para>
/// <strong>Integration with Standard Configuration:</strong>
/// FlexConfiguration wraps but does not replace IConfiguration, maintaining full
/// compatibility with existing .NET configuration patterns and allowing gradual
/// adoption in existing applications.
/// </para>
///
/// <para>
/// <strong>Performance Characteristics:</strong>
/// <list type="bullet">
/// <item>Dynamic access has runtime overhead due to reflection and dynamic dispatch</item>
/// <item>Indexer access has similar performance to standard IConfiguration</item>
/// <item>Type conversion adds parsing overhead, but results are not cached</item>
/// <item>Suitable for configuration access patterns (infrequent, startup-time access)</item>
/// </list>
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Creating a FlexConfiguration instance
/// IConfiguration standardConfig = new ConfigurationBuilder()
///     .AddJsonFile("appsettings.json")
///     .Build();
/// var flexConfig = new FlexConfiguration(standardConfig);
///
/// // Dynamic access
/// dynamic config = flexConfig;
/// var apiKey = config.External.ApiKey;
/// var timeout = config.External.Timeout;
///
/// // Traditional access
/// var connectionString = flexConfig["ConnectionStrings:DefaultConnection"];
///
/// // Type-safe access
/// var port = flexConfig["Server:Port"].ToType&lt;int&gt;();
/// var isEnabled = flexConfig["Features:NewFeature"].ToType&lt;bool&gt;();
///
/// // Numeric indexing for arrays
/// var firstServer = flexConfig[0]; // Access first configuration section
/// </code>
/// </example>
public sealed class FlexConfiguration(IConfiguration root) : DynamicObject, IFlexConfig
{
    /// <summary>
    /// The underlying IConfiguration instance that provides the actual configuration data.
    /// This field maintains the connection to the Microsoft configuration system while
    /// adding FlexKit's enhanced access capabilities on top.
    /// </summary>
    private readonly IConfiguration _root = root ?? throw new ArgumentNullException(nameof(root));

    /// <summary>
    /// Gets the underlying IConfiguration instance for compatibility with standard .NET configuration patterns.
    /// Provides direct access to the wrapped configuration when standard IConfiguration methods are needed.
    /// </summary>
    /// <value>
    /// The IConfiguration instance that was passed to the constructor.
    /// </value>
    /// <remarks>
    /// This property enables seamless integration with existing code that expects IConfiguration
    /// while still providing FlexKit's enhanced capabilities. Common use cases include:
    ///
    /// <list type="bullet">
    /// <item>Binding configuration sections to strongly  typed objects</item>
    /// <item>Using IConfiguration extension methods from other libraries</item>
    /// <item>Passing configuration to services that expect IConfiguration</item>
    /// <item>Accessing advanced IConfiguration features not exposed by FlexConfig</item>
    /// </list>
    ///
    /// <para>
    /// <strong>Example Usage:</strong>
    /// <code>
    /// // Bind to strongly typed configuration object
    /// var settings = flexConfig.Configuration.GetSection("AppSettings").Get&lt;AppSettings&gt;();
    ///
    /// // Use with IOptionsPattern
    /// services.Configure&lt;DatabaseOptions&gt;(flexConfig.Configuration.GetSection("Database"));
    ///
    /// // Access configuration metadata
    /// var children = flexConfig.Configuration.GetChildren();
    /// var exists = flexConfig.Configuration.GetSection("SomeSection").Exists();
    /// </code>
    /// </para>
    /// </remarks>
    public IConfiguration Configuration => _root;

    /// <summary>
    /// Gets a configuration value by key using traditional indexer syntax.
    /// Provides direct access to configuration values using the standard colon-separated
    /// hierarchical key notation familiar from IConfiguration.
    /// </summary>
    /// <param name="key">
    /// The configuration key in hierarchical notation (e.g., "Database:ConnectionString").
    /// If null or empty, returns null.
    /// </param>
    /// <returns>
    /// The configuration value as a string, or null if the key is not found or is null/empty.
    /// </returns>
    /// <remarks>
    /// This indexer provides compatibility with standard IConfiguration access patterns
    /// while being part of the FlexConfig interface. It uses the same key resolution
    /// logic as the underlying IConfiguration system.
    ///
    /// <para>
    /// <strong>Key Format:</strong>
    /// Keys use colon (:) as the hierarchy separator, following standard .NET configuration conventions:
    /// <list type="bullet">
    /// <item>"ConnectionStrings:DefaultConnection"</item>
    /// <item>"Logging:LogLevel:Default"</item>
    /// <item>"Features:EnableNewUI"</item>
    /// </list>
    /// </para>
    ///
    /// <para>
    /// <strong>Return Value Behavior:</strong>
    /// <list type="bullet">
    /// <item>Returns the string value for simple configuration values</item>
    /// <item>Returns null for missing keys</item>
    /// <item>Returns null for configuration sections (use dynamic access for sections)</item>
    /// <item>Returns null for null or empty key parameters</item>
    /// </list>
    /// </para>
    ///
    /// <para>
    /// <strong>Case Sensitivity:</strong>
    /// Key matching follows the behavior of the underlying configuration provider.
    /// Most providers (JSON, environment variables) are case-insensitive, but this
    /// can vary by provider implementation.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Simple value access
    /// var connectionString = flexConfig["ConnectionStrings:DefaultConnection"];
    /// var logLevel = flexConfig["Logging:LogLevel:Default"];
    ///
    /// // Missing keys return null
    /// var missing = flexConfig["NonExistent:Key"]; // Returns null
    ///
    /// // Empty/null keys return null
    /// var empty = flexConfig[""]; // Returns null
    /// var nullKey = flexConfig[null]; // Returns null
    ///
    /// // Type conversion can be applied to results
    /// var port = flexConfig["Server:Port"]?.ToType&lt;int&gt;();
    /// </code>
    /// </example>
    public string? this[string key] => !string.IsNullOrEmpty(key.Trim()) ? _root[key] : null;

    /// <summary>
    /// Gets a configuration section by numeric index, treating the configuration as an array-like structure.
    /// Enables numeric indexing into configuration sections, useful for accessing ordered configuration
    /// elements or treating configuration sections as enumerable collections.
    /// </summary>
    /// <param name="index">The zero-based index of the configuration section to retrieve.</param>
    /// <returns>
    /// An IFlexConfig instance representing the configuration section at the specified index,
    /// or null if no section exists at that index.
    /// </returns>
    /// <remarks>
    /// This indexer enables array-like access to configuration sections, which is particularly
    /// useful for configuration structures that represent ordered collections or when you need
    /// to access configuration sections by position rather than by name.
    ///
    /// <para>
    /// <strong>Index Resolution:</strong>
    /// The numeric index is converted to a string and used to look up a configuration section.
    /// This means that configuration with numeric keys (0, 1, 2, etc.) can be accessed using
    /// this indexer syntax.
    /// </para>
    ///
    /// <para>
    /// <strong>Common Use Cases:</strong>
    /// <list type="bullet">
    /// <item>Accessing array elements in JSON configuration</item>
    /// <item>Iterating through ordered configuration sections</item>
    /// <item>Accessing configuration by position when names are not known</item>
    /// <item>Working with indexed configuration patterns</item>
    /// </list>
    /// </para>
    ///
    /// <para>
    /// <strong>JSON Configuration Example:</strong>
    /// For a JSON configuration like:
    /// <code>
    /// {
    ///   "Servers": [
    ///     { "Name": "Server1", "Port": 8080 },
    ///     { "Name": "Server2", "Port": 8081 }
    ///   ]
    /// }
    /// </code>
    /// You could access individual servers using:
    /// <code>
    /// var serversConfig = flexConfig.CurrentConfig("Servers");
    /// var firstServer = serversConfig[0]; // Gets the first server configuration
    /// var secondServer = serversConfig[1]; // Gets the second server configuration
    /// </code>
    /// </para>
    ///
    /// <para>
    /// <strong>Error Handling:</strong>
    /// <list type="bullet">
    /// <item>Returns null for indices that don't correspond to existing sections</item>
    /// <item>Negative indices are converted to strings and may match configuration keys</item>
    /// <item>Large indices that don't exist return null rather than throwing exceptions</item>
    /// </list>
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Access array-like configuration
    /// var firstItem = flexConfig[0];
    /// var secondItem = flexConfig[1];
    ///
    /// // Use with dynamic access
    /// dynamic first = flexConfig[0];
    /// var name = first?.Name;
    /// var port = first?.Port;
    ///
    /// // Iterate through indexed sections
    /// for (int i = 0; i &lt; 10; i++)
    /// {
    ///     var section = flexConfig[i];
    ///     if (section == null) break; // No more sections
    ///
    ///     // Process section...
    /// }
    /// </code>
    /// </example>
    public IFlexConfig? this[int index] => _root.CurrentConfig(index.ToString(CultureInfo.InvariantCulture));

    /// <summary>
    /// Retrieves a subsection of the configuration hierarchy based on the specified key.
    /// This method is used to navigate deeper into the configuration structure.
    /// </summary>
    /// <param name="key">The key identifying the configuration subsection to retrieve.</param>
    /// <returns>
    /// The configuration subsection corresponding to the provided key if found;
    /// otherwise, null if the key is empty or not found.
    /// </returns>
    public IFlexConfig? GetSection(string key) => !string.IsNullOrEmpty(key.Trim()) ? _root.CurrentConfig(key) : null;

    /// <summary>
    /// Attempts to convert the configuration value to the specified type.
    /// This method is called by the .NET dynamic runtime when explicit type conversion
    /// is requested on a dynamic FlexConfiguration object.
    /// </summary>
    /// <param name="binder">Provides information about the requested conversion, including the target type.</param>
    /// <param name="result">
    /// When this method returns, contains the converted value if the conversion succeeded,
    /// or the default value for the target type if conversion failed.
    /// </param>
    /// <returns>
    /// Always returns true to indicate that the conversion attempt was handled,
    /// even if the actual conversion was not successful.
    /// </returns>
    /// <remarks>
    /// This method enables explicit type conversion syntax when using FlexConfiguration
    /// as a dynamic object. It supports conversion to various types, including:
    ///
    /// <list type="bullet">
    /// <item>Value types (int, bool, double, etc.) - converted from configuration values</item>
    /// <item>String type - returns the configuration value as-is</item>
    /// <item>Array types - converts configuration children to typed arrays</item>
    /// <item>Generic dictionary types - converts configuration sections to dictionaries</item>
    /// </list>
    ///
    /// <para>
    /// <strong>Conversion Logic:</strong>
    /// <list type="number">
    /// <item>For value types and strings: Uses the configuration section's Value property</item>
    /// <item>For arrays: Converts child configuration sections to an array of the element type</item>
    /// <item>For generic types: Attempts dictionary conversion for dictionary types</item>
    /// <item>For unsupported types: Returns the default value for the type</item>
    /// </list>
    /// </para>
    ///
    /// <para>
    /// <strong>Dynamic Runtime Integration:</strong>
    /// This method is typically not called directly but is invoked by the .NET dynamic
    /// runtime when code like the following is executed:
    /// <code>
    /// dynamic config = flexConfig.GetSection("SomeSection");
    /// int value = (int)config; // Triggers TryConvert
    /// string text = (string)config; // Triggers TryConvert
    /// </code>
    /// </para>
    ///
    /// <para>
    /// <strong>Error Handling:</strong>
    /// The method always returns true to indicate that it handled the conversion attempt.
    /// If the actual conversion fails (e.g., invalid format), the result will be the
    /// default value for the target type rather than throwing an exception.
    /// </para>
    ///
    /// <para>
    /// <strong>Performance Considerations:</strong>
    /// Type conversion involves reflection and parsing operations, which can be expensive.
    /// For performance-critical scenarios, consider using the ToType extension methods
    /// directly or caching converted values.
    /// </para>
    /// </remarks>
    /// <exception cref="InvalidOperationException">
    /// May be thrown by underlying conversion methods if the configuration value
    /// cannot be converted to the target type.
    /// </exception>
    /// <example>
    /// <code>
    /// // Explicit type conversion
    /// dynamic configSection = flexConfig.CurrentConfig("Settings");
    ///
    /// // Convert to different types
    /// int intValue = (int)configSection;           // Converts Value to int
    /// string stringValue = (string)configSection; // Returns Value as string
    /// bool boolValue = (bool)configSection;       // Converts Value to bool
    ///
    /// // Array conversion
    /// dynamic arraySection = flexConfig.CurrentConfig("Items");
    /// string[] items = (string[])arraySection;    // Converts children to string array
    ///
    /// // Dictionary conversion
    /// dynamic dictSection = flexConfig.CurrentConfig("KeyValuePairs");
    /// Dictionary&lt;string, int&gt; dict = (Dictionary&lt;string, int&gt;)dictSection;
    /// </code>
    /// </example>
    public override bool TryConvert(
        ConvertBinder binder,
        out object? result)
    {
        result = binder.Type.IsValueType ? Activator.CreateInstance(binder.Type) : null;
        var section = _root as IConfigurationSection;

        if (binder.Type.IsValueType || binder.Type == typeof(string))
        {
            result = section?.Value?.ToType(binder.Type);
        }
        else if (binder.Type.IsArray)
        {
            result = section?.GetChildren().Select(c => c.Value).ToArray(binder.Type);
        }
        else if (binder.Type.IsGenericType)
        {
            result = GetGenericValue(section, binder);
        }

        return true;
    }

    /// <summary>
    /// Attempts to get a member value using dynamic member access syntax.
    /// This method is called by the .NET dynamic runtime when property access
    /// is performed on a dynamic FlexConfiguration object.
    /// </summary>
    /// <param name="binder">Provides information about the member being accessed, including the member name.</param>
    /// <param name="result">
    /// When this method returns, contains an IFlexConfig instance representing
    /// the requested configuration section, or null if the section doesn't exist.
    /// </param>
    /// <returns>
    /// Always returns true to indicate that the member access attempt was handled,
    /// regardless of whether the member was found.
    /// </returns>
    /// <remarks>
    /// This method enables natural property access syntax for configuration sections,
    /// allowing developers to navigate configuration hierarchies using familiar
    /// object-oriented property access patterns.
    ///
    /// <para>
    /// <strong>Dynamic Property Access:</strong>
    /// When code like <c>config.Database.ConnectionString</c> is executed on a dynamic
    /// FlexConfiguration object, this method is called for each property access:
    /// <list type="number">
    /// <item>The first call: binder.Name = "Database", returns FlexConfig for a Database section</item>
    /// <item>The second call: binder.Name = "ConnectionString", returns FlexConfig for that subsection</item>
    /// </list>
    /// </para>
    ///
    /// <para>
    /// <strong>Section Resolution:</strong>
    /// The method uses the CurrentConfig extension method to locate the requested
    /// configuration section by name. This follows standard IConfiguration section
    /// resolution logic, including case-insensitive matching where supported by
    /// the configuration provider.
    /// </para>
    ///
    /// <para>
    /// <strong>Chaining Support:</strong>
    /// Since the result is always an IFlexConfig (even for non-existent sections),
    /// property access can be chained without explicit null checks:
    /// <code>
    /// dynamic config = flexConfig;
    /// var value = config.Level1.Level2.Level3.SomeValue; // Won't throw even if sections don't exist
    /// </code>
    /// </para>
    ///
    /// <para>
    /// <strong>Member Name Handling:</strong>
    /// The member name from the binder is used as-is to look up configuration sections.
    /// This means that property names in dynamic access correspond directly to
    /// configuration section names.
    /// </para>
    ///
    /// <para>
    /// <strong>Performance Implications:</strong>
    /// Each dynamic property access results in a configuration section lookup and
    /// the creation of a new FlexConfiguration wrapper. For frequently accessed
    /// configuration values, consider caching the results or using direct indexer access.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Dynamic property access chain
    /// dynamic config = flexConfig;
    ///
    /// // Each property access calls TryGetMember
    /// var database = config.Database;              // TryGetMember called with "Database"
    /// var connectionString = database.ConnectionString; // TryGetMember called with "ConnectionString"
    ///
    /// // Chained access (multiple TryGetMember calls)
    /// var apiKey = config.External.Services.Api.Key;
    ///
    /// // Works even for non-existent sections (returns null FlexConfig)
    /// var missing = config.NonExistent.Section.Value; // No exceptions thrown
    ///
    /// // Convert to string to get actual value
    /// string value = config.SomeSection.SomeValue?.ToString();
    /// </code>
    /// </example>
    public override bool TryGetMember(
        GetMemberBinder binder,
        out object? result)
    {
        result = _root.CurrentConfig(binder.Name);
        return true;
    }

    /// <summary>
    /// Returns a string representation of this FlexConfiguration instance.
    /// For configuration sections, returns the section's value; for root configurations,
    /// returns an empty string.
    /// </summary>
    /// <returns>
    /// The string value of the configuration section if this instance represents a section
    /// with a value, otherwise an empty string.
    /// </returns>
    /// <remarks>
    /// This method provides a convenient way to get the string representation of a
    /// configuration value when the FlexConfiguration instance represents a specific
    /// configuration section rather than a configuration root or branch.
    ///
    /// <para>
    /// <strong>Behavior for Different Configuration Types:</strong>
    /// <list type="table">
    /// <listheader>
    /// <term>Configuration Type</term>
    /// <description>ToString() Result</description>
    /// </listheader>
    /// <item>
    /// <term>Configuration Section with Value</term>
    /// <description>The section's string value</description>
    /// </item>
    /// <item>
    /// <term>Configuration Section without Value</term>
    /// <description>Empty string</description>
    /// </item>
    /// <item>
    /// <term>Root Configuration</term>
    /// <description>Empty string</description>
    /// </item>
    /// <item>
    /// <term>Configuration Branch (has children)</term>
    /// <description>Empty string</description>
    /// </item>
    /// </list>
    /// </para>
    ///
    /// <para>
    /// <strong>Usage Patterns:</strong>
    /// This method is particularly useful when:
    /// <list type="bullet">
    /// <item>Converting dynamic configuration access to strings</item>
    /// <item>Logging or debugging configuration values</item>
    /// <item>Implicitly converting configuration values in string contexts</item>
    /// <item>Providing fallback string representation for configuration objects</item>
    /// </list>
    /// </para>
    ///
    /// <para>
    /// <strong>Alternative Access Methods:</strong>
    /// For more explicit string access, consider using:
    /// <list type="bullet">
    /// <item>Indexer access: <c>flexConfig["SectionName"]</c></item>
    /// <item>Type conversion: <c>flexConfig.CurrentConfig("SectionName")?.Configuration.Value</c></item>
    /// <item>Direct IConfiguration access: <c>flexConfig.Configuration["SectionName"]</c></item>
    /// </list>
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Configuration with values
    /// var portConfig = flexConfig.CurrentConfig("Server:Port");
    /// string portString = portConfig.ToString(); // Returns the port value as string
    ///
    /// // Configuration sections without values
    /// var serverConfig = flexConfig.CurrentConfig("Server");
    /// string serverString = serverConfig.ToString(); // Returns empty string
    ///
    /// // Root configuration
    /// string rootString = flexConfig.ToString(); // Returns empty string
    ///
    /// // Implicit string conversion
    /// var logMessage = $"Current port: {portConfig}"; // Uses ToString() implicitly
    ///
    /// // Debugging output
    /// Console.WriteLine($"Config value: {flexConfig.CurrentConfig("SomeSetting")}");
    /// </code>
    /// </example>
    public override string ToString() =>
        _root is IConfigurationSection configRoot ? configRoot.Value ?? string.Empty : string.Empty;

    /// <summary>
    /// Retrieves a generic dictionary value from the provided configuration section.
    /// Handles conversion of configuration sections to strongly typed dictionary objects
    /// when explicit type conversion to dictionary types is requested.
    /// </summary>
    /// <param name="section">The configuration section to convert to a dictionary. Can be null.</param>
    /// <param name="binder">Provides information about the conversion operation, including the target dictionary type.</param>
    /// <returns>
    /// A dictionary instance of the requested type populated with values from the configuration section,
    /// or null if the conversion cannot be performed.
    /// </returns>
    /// <remarks>
    /// This private helper method supports the TryConvert functionality for dictionary types.
    /// It specifically handles conversion to IDictionary&lt;,&gt; and Dictionary&lt;,&gt; types by
    /// examining the configuration section's children and converting them to key-value pairs.
    ///
    /// <para>
    /// <strong>Supported Dictionary Types:</strong>
    /// <list type="bullet">
    /// <item>IDictionary&lt;TKey, TValue&gt; - Interface-based dictionary access</item>
    /// <item>Dictionary&lt;TKey, TValue&gt; - Concrete dictionary implementation</item>
    /// <item>Other generic dictionary types that follow the same pattern</item>
    /// </list>
    /// </para>
    ///
    /// <para>
    /// <strong>Conversion Process:</strong>
    /// <list type="number">
    /// <item>Validates that the target type is a supported dictionary type</item>
    /// <item>Extracts the generic type definition to check for IDictionary or Dictionary</item>
    /// <item>Delegates to the ToDictionary extension method for actual conversion</item>
    /// <item>Returns the populated dictionary or null if conversion fails</item>
    /// </list>
    /// </para>
    ///
    /// <para>
    /// <strong>Configuration Structure Requirements:</strong>
    /// The configuration section should contain child sections where each child
    /// represents a dictionary entry. The exact structure depends on the ToDictionary
    /// implementation but typically follows this pattern:
    /// <code>
    /// {
    ///   "MyDictionary": {
    ///     "Entry1": { "Key": "value1" },
    ///     "Entry2": { "Key": "value2" }
    ///   }
    /// }
    /// </code>
    /// </para>
    ///
    /// <para>
    /// <strong>Error Handling:</strong>
    /// <list type="bullet">
    /// <item>Returns null for unsupported dictionary types</item>
    /// <item>Returns null if the configuration section is null</item>
    /// <item>Propagates conversion errors from the underlying ToDictionary method</item>
    /// </list>
    /// </para>
    /// </remarks>
    private static object? GetGenericValue(
        IConfigurationSection? section,
        ConvertBinder binder)
    {
        var genericType = binder.Type.GetGenericTypeDefinition();
        var isExpectedType = genericType == typeof(IDictionary<,>);

        return isExpectedType || genericType == typeof(Dictionary<,>) ? section?.GetChildren().ToDictionary(binder.Type) : null;
    }
}
