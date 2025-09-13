// <copyright file="IFlexConfig.cs" company="Michael Saruyev">
// Copyright (c) Michael Saruyev. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

using System.Dynamic;
using JetBrains.Annotations;
using Microsoft.Extensions.Configuration;

namespace FlexKit.Configuration.Core;

/// <summary>
/// Central configuration unit contract providing dynamic access to configuration properties.
/// Defines the interface for FlexKit's enhanced configuration system that bridges traditional
/// .NET configuration with dynamic access patterns and type conversion capabilities.
/// </summary>
/// <remarks>
/// IFlexConfig serves as the primary interface for accessing configuration in FlexKit applications.
/// It extends the capabilities of standard IConfiguration by providing:
///
/// <list type="bullet">
/// <item>Dynamic member access through IDynamicMetaObjectProvider implementation</item>
/// <item>Traditional indexer access compatible with IConfiguration patterns</item>
/// <item>Numeric indexing for array-like configuration access</item>
/// <item>Seamless integration with dependency injection containers</item>
/// <item>Type conversion and collection handling capabilities</item>
/// </list>
///
/// <para>
/// <strong>Design Philosophy:</strong>
/// The interface is designed to provide maximum flexibility while maintaining compatibility
/// with existing .NET configuration patterns. It allows developers to choose between
/// dynamic access for convenience and traditional access for explicitness, depending
/// on their specific use case and coding style preferences.
/// </para>
///
/// <para>
/// <strong>Implementation Requirements:</strong>
/// Implementations of this interface must:
/// <list type="bullet">
/// <item>Support dynamic member access through IDynamicMetaObjectProvider</item>
/// <item>Provide access to the underlying IConfiguration instance</item>
/// <item>Handle null and missing configuration values gracefully</item>
/// <item>Support hierarchical configuration navigation</item>
/// <item>Maintain thread-safety for read operations</item>
/// </list>
/// </para>
///
/// <para>
/// <strong>Usage Patterns:</strong>
/// The interface supports multiple access patterns to accommodate different scenarios:
/// <code>
/// // Dynamic access - natural property-like syntax
/// dynamic config = flexConfig;
/// var apiKey = config.External.Api.Key;
///
/// // Traditional indexer access - explicit and familiar
/// var connectionString = flexConfig["ConnectionStrings:DefaultConnection"];
///
/// // Numeric indexing - for array-like configuration
/// var firstServer = flexConfig[0];
///
/// // Direct IConfiguration access - for advanced scenarios
/// var section = flexConfig.Configuration.GetSection("MySection");
/// </code>
/// </para>
///
/// <para>
/// <strong>Integration with Dependency Injection:</strong>
/// This interface is designed to work seamlessly with dependency injection containers,
/// particularly Autofac. Services can depend on IFlexConfig and receive instances
/// that provide access to the application's configuration with all FlexKit enhancements.
/// </para>
///
/// <para>
/// <strong>Thread Safety:</strong>
/// Implementations should be thread-safe for read operations, allowing concurrent
/// access to configuration values from multiple threads. Configuration updates
/// (if supported) should be handled with appropriate synchronization.
/// </para>
///
/// <para>
/// <strong>Performance Considerations:</strong>
/// <list type="bullet">
/// <item>Dynamic access has runtime overhead due to reflection and dynamic dispatch</item>
/// <item>Indexer access should have performance similar to standard IConfiguration</item>
/// <item>Consider caching frequently accessed configuration values in performance-critical scenarios</item>
/// <item>Numeric indexing may involve string conversions and should be used judiciously</item>
/// </list>
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Dependency injection registration
/// builder.RegisterInstance(flexConfig).As&lt;IFlexConfig&gt;();
///
/// // Service consuming IFlexConfig
/// public class EmailService(IFlexConfig config)
/// {
///     public async Task SendEmailAsync(string to, string subject, string body)
///     {
///         // Dynamic access
///         dynamic emailConfig = config;
///         var smtpHost = emailConfig.Email.Smtp.Host;
///         var smtpPort = emailConfig.Email.Smtp.Port;
///         var enableSsl = emailConfig.Email.Smtp.EnableSsl;
///
///         // Traditional access
///         var apiKey = config["Email:SendGrid:ApiKey"];
///
///         // Type conversion
///         var timeout = config["Email:Timeout"].ToType&lt;int&gt;();
///
///         // Use configuration to send email...
///     }
/// }
///
/// // Property injection scenario
/// public class BackgroundWorker
/// {
///     public IFlexConfig? FlexConfiguration { get; set; }
///
///     public void ProcessWork()
///     {
///         if (FlexConfiguration != null)
///         {
///             dynamic config = FlexConfiguration;
///             var batchSize = config.Processing.BatchSize;
///             // Process work...
///         }
///     }
/// }
/// </code>
/// </example>
public interface IFlexConfig : IDynamicMetaObjectProvider
{
    /// <summary>
    /// Gets the underlying IConfiguration instance for compatibility with standard .NET configuration patterns.
    /// Provides direct access to the Microsoft.Extensions.Configuration infrastructure when advanced
    /// configuration operations are needed that are not exposed through the FlexConfig interface.
    /// </summary>
    /// <value>
    /// The IConfiguration instance that provides the actual configuration data and standard
    /// .NET configuration functionality.
    /// </value>
    /// <remarks>
    /// This property serves as an escape hatch to access the full functionality of the underlying
    /// IConfiguration system when FlexConfig's simplified interface is insufficient. Common scenarios include:
    ///
    /// <list type="bullet">
    /// <item>Binding configuration sections to strongly typed objects using GetSection().Get&lt;T&gt;()</item>
    /// <item>Using configuration with the Options pattern and IOptionsMonitor</item>
    /// <item>Accessing configuration metadata like section existence or child enumeration</item>
    /// <item>Integrating with third-party libraries that expect IConfiguration</item>
    /// <item>Implementing configuration change notifications and monitoring</item>
    /// </list>
    ///
    /// <para>
    /// <strong>Compatibility Guarantee:</strong>
    /// The returned IConfiguration instance is fully functional and maintains all the capabilities
    /// of the original configuration system. Any changes to the underlying configuration (such as
    /// from reloadable configuration sources) are reflected through this property.
    /// </para>
    ///
    /// <para>
    /// <strong>Performance Note:</strong>
    /// Accessing this property has minimal overhead as it simply returns a reference to the
    /// underlying configuration instance. There is no additional processing or wrapping involved.
    /// </para>
    ///
    /// <para>
    /// <strong>Integration Examples:</strong>
    /// <code>
    /// // Strongly typed configuration binding
    /// var dbOptions = flexConfig.Configuration.GetSection("Database").Get&lt;DatabaseOptions&gt;();
    ///
    /// // Options pattern integration
    /// services.Configure&lt;ApiSettings&gt;(flexConfig.Configuration.GetSection("Api"));
    ///
    /// // Section existence checking
    /// var hasEmailConfig = flexConfig.Configuration.GetSection("Email").Exists();
    ///
    /// // Child section enumeration
    /// var allSections = flexConfig.Configuration.GetChildren();
    ///
    /// // Configuration change monitoring
    /// ChangeToken.OnChange(
    ///     () => flexConfig.Configuration.GetReloadToken(),
    ///     () => Console.WriteLine("Configuration changed"));
    /// </code>
    /// </para>
    /// </remarks>
    IConfiguration Configuration { get; }

    /// <summary>
    /// Gets a configuration value by key using traditional hierarchical notation.
    /// Provides familiar indexer access to configuration values using the standard colon-separated
    /// key format that is consistent with Microsoft.Extensions.Configuration patterns.
    /// </summary>
    /// <param name="key">
    /// The configuration key in hierarchical notation using colons as separators.
    /// For example, "Database:ConnectionString", "Logging:LogLevel:Default", or "ApiSettings:Timeout".
    /// Can be null or empty, in which case null is returned.
    /// </param>
    /// <returns>
    /// The configuration value as a string if found, or null if the key doesn't exist,
    /// is null or is empty. For configuration sections (non-leaf nodes), returns null.
    /// </returns>
    /// <remarks>
    /// This indexer provides the primary mechanism for explicit configuration access in FlexConfig.
    /// It maintains full compatibility with standard IConfiguration key patterns while being part
    /// of the enhanced FlexConfig interface.
    ///
    /// <para>
    /// <strong>Key Format and Hierarchy:</strong>
    /// Configuration keys use colon (:) as the hierarchy separator, following .NET configuration conventions:
    /// <list type="table">
    /// <listheader>
    /// <term>Key Pattern</term>
    /// <description>Example Usage</description>
    /// </listheader>
    /// <item>
    /// <term>Simple key</term>
    /// <description>"AllowedHosts" → Gets top-level AllowedHosts value</description>
    /// </item>
    /// <item>
    /// <term>Nested key</term>
    /// <description>"Database:ConnectionString" → Gets ConnectionString from a Database section</description>
    /// </item>
    /// <item>
    /// <term>Deep nesting</term>
    /// <description>"Logging:LogLevel:Microsoft" → Gets Microsoft log level from nested structure</description>
    /// </item>
    /// <item>
    /// <term>Array access</term>
    /// <description>"Servers:0:Name" → Gets Name property of the first server in a Servers array</description>
    /// </item>
    /// </list>
    /// </para>
    ///
    /// <para>
    /// <strong>Return Value Behavior:</strong>
    /// <list type="bullet">
    /// <item>Leaf values (strings, numbers, booleans) are returned as strings</item>
    /// <item>Configuration sections (objects with children) return null</item>
    /// <item>Non-existent keys return null</item>
    /// <item>Null or empty key parameters return null</item>
    /// <item>Array elements can be accessed using index notation in the key</item>
    /// </list>
    /// </para>
    ///
    /// <para>
    /// <strong>Type Conversion Integration:</strong>
    /// The string values returned by this indexer can be used with FlexKit's type conversion
    /// extension methods to get strongly typed values:
    /// <code>
    /// var port = flexConfig["Server:Port"].ToType&lt;int&gt;();
    /// var isEnabled = flexConfig["Features:Cache"].ToType&lt;bool&gt;();
    /// var timeout = flexConfig["Api:Timeout"].ToType&lt;TimeSpan&gt;();
    /// </code>
    /// </para>
    ///
    /// <para>
    /// <strong>Case Sensitivity:</strong>
    /// Key matching behavior depends on the underlying configuration provider:
    /// <list type="bullet">
    /// <item>JSON configuration: Case-insensitive by default</item>
    /// <item>Environment variables: Case-insensitive on Windows, case-sensitive on Linux/macOS</item>
    /// <item>Custom providers: Depends on provider implementation</item>
    /// </list>
    /// </para>
    ///
    /// <para>
    /// <strong>Performance Characteristics:</strong>
    /// <list type="bullet">
    /// <item>Similar performance to standard IConfiguration indexer access</item>
    /// <item>Key lookup time depends on configuration provider implementation</item>
    /// <item>No additional overhead beyond the standard configuration system</item>
    /// <item>Consider caching frequently accessed values for performance-critical code</item>
    /// </list>
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentException">
    /// May be thrown by underlying configuration providers if the key format is invalid
    /// for that specific provider.
    /// </exception>
    /// <example>
    /// <code>
    /// // Basic configuration access
    /// var appName = flexConfig["ApplicationName"];
    /// var version = flexConfig["Version"];
    ///
    /// // Hierarchical access
    /// var dbConnectionString = flexConfig["ConnectionStrings:DefaultConnection"];
    /// var logLevel = flexConfig["Logging:LogLevel:Default"];
    ///
    /// // API configuration
    /// var apiBaseUrl = flexConfig["External:PaymentApi:BaseUrl"];
    /// var apiTimeout = flexConfig["External:PaymentApi:Timeout"];
    ///
    /// // Array element access
    /// var firstServerName = flexConfig["LoadBalancers:0:Name"];
    /// var secondServerPort = flexConfig["LoadBalancers:1:Port"];
    ///
    /// // Feature flags
    /// var cacheEnabled = flexConfig["Features:EnableCaching"];
    /// var debugMode = flexConfig["Features:DebugMode"];
    ///
    /// // Type conversion usage
    /// var portNumber = flexConfig["Server:Port"].ToType&lt;int&gt;();
    /// var enableSsl = flexConfig["Server:EnableSsl"].ToType&lt;bool&gt;();
    /// var hosts = flexConfig["Server:AllowedHosts"].GetCollection&lt;string&gt;();
    ///
    /// // Null handling
    /// var optional = flexConfig["Optional:Setting"] ?? "default-value";
    ///
    /// // Empty key handling
    /// var invalid1 = flexConfig[""]; // Returns null
    /// var invalid2 = flexConfig[null]; // Returns null
    /// </code>
    /// </example>
    [UsedImplicitly]
    string? this[string key] { get; }

    /// <summary>
    /// Gets a configuration section by numeric index, enabling array-like access to configuration elements.
    /// Provides ordered access to configuration sections when the configuration structure represents
    /// indexed collections or when positional access is more appropriate than named access.
    /// </summary>
    /// <param name="index">
    /// The zero-based index of the configuration section to retrieve.
    /// The index is converted to a string and used as a configuration key.
    /// </param>
    /// <returns>
    /// An IFlexConfig instance representing the configuration section at the specified index,
    /// or null if no section exists at that index or if the indexed element has no value.
    /// </returns>
    /// <remarks>
    /// This indexer enables array-style access to configuration, which is particularly useful
    /// for configuration structures that represent ordered collections, lists, or when you need
    /// to iterate through configuration elements without knowing their names in advance.
    ///
    /// <para>
    /// <strong>Index Resolution Process:</strong>
    /// <list type="number">
    /// <item>The numeric index is converted to a string using invariant culture</item>
    /// <item>The string is used as a configuration key to look up the corresponding section</item>
    /// <item>If a matching configuration section exists, it's wrapped in a new IFlexConfig instance</item>
    /// <item>If no matching section exists, null is returned</item>
    /// </list>
    /// </para>
    ///
    /// <para>
    /// <strong>Configuration Structure Requirements:</strong>
    /// This indexer works best with configuration structures that have numeric keys, such as:
    /// <code>
    /// // JSON configuration example
    /// {
    ///   "Servers": [
    ///     { "Name": "Server1", "Port": 8080 },
    ///     { "Name": "Server2", "Port": 8081 },
    ///     { "Name": "Server3", "Port": 8082 }
    ///   ],
    ///   "DatabaseConnections": {
    ///     "0": { "Name": "Primary", "ConnectionString": "..." },
    ///     "1": { "Name": "Secondary", "ConnectionString": "..." }
    ///   }
    /// }
    /// </code>
    /// </para>
    ///
    /// <para>
    /// <strong>Array Access Patterns:</strong>
    /// <list type="bullet">
    /// <item>JSON arrays are automatically indexed starting from 0</item>
    /// <item>Configuration sections with numeric names can be accessed by index</item>
    /// <item>Mixed numeric and named sections can coexist (numeric access won't interfere with named access)</item>
    /// <item>Sparse arrays (missing indices) return null for missing elements</item>
    /// </list>
    /// </para>
    ///
    /// <para>
    /// <strong>Integration with Dynamic Access:</strong>
    /// The returned IFlexConfig instances support the same dynamic access patterns as the parent:
    /// <code>
    /// var firstServer = flexConfig[0]; // Get first server configuration
    /// dynamic server = firstServer;
    /// var serverName = server?.Name;   // Dynamic access to server properties
    /// var serverPort = server?.Port;
    /// </code>
    /// </para>
    ///
    /// <para>
    /// <strong>Iteration Patterns:</strong>
    /// This indexer enables iteration through configuration collections:
    /// <code>
    /// // Iterate through indexed configuration elements
    /// for (int i = 0; i &lt; 10; i++)
    /// {
    ///     var section = flexConfig[i];
    ///     if (section == null) break; // No more elements
    ///
    ///     // Process section...
    ///     dynamic item = section;
    ///     Console.WriteLine($"Item {i}: {item?.Name}");
    /// }
    /// </code>
    /// </para>
    ///
    /// <para>
    /// <strong>Error Handling and Edge Cases:</strong>
    /// <list type="bullet">
    /// <item>Negative indices are converted to strings and may match configuration keys</item>
    /// <item>Large indices that don't exist return null rather than throwing exceptions</item>
    /// <item>Index 0 on non-array configuration may return the first child section</item>
    /// <item>Empty configuration sections return null</item>
    /// </list>
    /// </para>
    ///
    /// <para>
    /// <strong>Performance Considerations:</strong>
    /// <list type="bullet">
    /// <item>Index-to-string conversion is performed on each access</item>
    /// <item>Configuration lookup performance depends on the underlying provider</item>
    /// <item>Consider caching IFlexConfig instances for frequently accessed indexed elements</item>
    /// <item>For known configuration structures, named access may be more efficient</item>
    /// </list>
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Basic indexed access
    /// var firstItem = flexConfig[0];
    /// var secondItem = flexConfig[1];
    /// var thirdItem = flexConfig[2];
    ///
    /// // Dynamic access on indexed elements
    /// dynamic first = flexConfig[0];
    /// var name = first?.Name;
    /// var port = first?.Port;
    /// var isActive = first?.IsActive;
    ///
    /// // Iteration through configuration array
    /// var servers = new List&lt;ServerConfig&gt;();
    /// for (int i = 0; i &lt; 100; i++) // Reasonable upper bound
    /// {
    ///     var serverConfig = flexConfig[i];
    ///     if (serverConfig == null) break;
    ///
    ///     dynamic server = serverConfig;
    ///     servers.Add(new ServerConfig
    ///     {
    ///         Name = server?.Name,
    ///         Port = server?.Port?.ToType&lt;int&gt;() ?? 80,
    ///         IsEnabled = server?.IsEnabled?.ToType&lt;bool&gt;() ?? true
    ///     });
    /// }
    ///
    /// // Null checking for safe access
    /// var optionalItem = flexConfig[5];
    /// if (optionalItem != null)
    /// {
    ///     dynamic item = optionalItem;
    ///     Console.WriteLine($"Found item: {item?.Description}");
    /// }
    ///
    /// // Combined with traditional indexer access
    /// var serverName = flexConfig[0]?["Name"];
    /// var serverPort = flexConfig[0]?["Port"];
    ///
    /// // Type conversion on indexed elements
    /// var timeout = flexConfig[0]?["Timeout"].ToType&lt;int&gt;();
    /// var endpoints = flexConfig[0]?["Endpoints"].GetCollection&lt;string&gt;();
    /// </code>
    /// </example>
    [UsedImplicitly]
    IFlexConfig? this[int index] { get; }

    /// <summary>
    /// Retrieves a subsection of the configuration hierarchy based on the specified key.
    /// This method is used to navigate deeper into the configuration structure.
    /// </summary>
    /// <param name="key">The key identifying the configuration subsection to retrieve.</param>
    /// <returns>
    /// The configuration subsection corresponding to the provided key if found;
    /// otherwise, null if the key is empty or not found.
    /// </returns>
    [UsedImplicitly]
    IFlexConfig? GetSection(string key);
}
