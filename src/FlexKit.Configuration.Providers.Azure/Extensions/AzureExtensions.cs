// <copyright file="AzureExtensions.cs" company="FlexKit">
// Copyright (c) FlexKit. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

using System.Collections.Concurrent;
using System.Text.Json;
using FlexKit.Configuration.Core;
using FlexKit.Configuration.Providers.Azure.Options;
using FlexKit.Configuration.Providers.Azure.Sources;
using JetBrains.Annotations;

namespace FlexKit.Configuration.Providers.Azure.Extensions;

/// <summary>
/// Extension methods for <see cref="FlexConfigurationBuilder"/> that add Azure configuration source support.
/// Provides fluent API methods to integrate Azure services like Key Vault and App Configuration
/// with the FlexKit configuration system.
/// </summary>
/// <remarks>
/// These extension methods follow the FlexKit configuration pattern of providing a fluent,
/// strongly typed API for adding configuration sources. They maintain consistency with other
/// FlexKit configuration providers while providing Azure-specific functionality and options.
///
/// <para>
/// <strong>Azure Integration Benefits:</strong>
/// <list type="bullet">
/// <item>Centralized configuration management across multiple applications and environments</item>
/// <item>Built-in encryption for sensitive configuration data using Azure Key Vault</item>
/// <item>Hierarchical organization of configuration parameters</item>
/// <item>Automatic credential management using Azure Managed Identity</item>
/// <item>Support for configuration versioning and change tracking</item>
/// <item>Integration with Azure DevOps and other infrastructure-as-code tools</item>
/// </list>
/// </para>
///
/// <para>
/// <strong>Security Considerations:</strong>
/// Azure configuration sources automatically use the Azure credential resolution chain,
/// ensuring secure access to configuration data without hardcoding credentials in
/// application code. Azure RBAC can be used to provide fine-grained access control
/// to specific configuration parameters.
/// </para>
/// </remarks>
public static class AzureExtensions
{
    /// <summary>
    /// Adds Azure Key Vault as a configuration source to the FlexKit configuration builder.
    /// Enables applications to load configuration data from Azure Key Vault
    /// with support for hierarchical secrets, JSON processing, and automatic reloading.
    /// </summary>
    /// <param name="builder">The FlexKit configuration builder to add the Key Vault source to.</param>
    /// <param name="vaultUri">
    /// The Key Vault URI to load secrets from.
    /// Should be in the format "https://{vault-name}.vault.azure.net/".
    /// </param>
    /// <param name="optional">
    /// Indicates whether the Key Vault source is optional.
    /// When true, failures to load secrets will not cause configuration building to fail.
    /// Default is true.
    /// </param>
    /// <returns>The same FlexKit configuration builder instance to enable method chaining.</returns>
    /// <remarks>
    /// This is the simplest overload for adding Key Vault support. It uses default
    /// Azure credential resolution and basic secret processing without JSON flattening
    /// or automatic reloading.
    ///
    /// <para>
    /// <strong>Default Behavior:</strong>
    /// <list type="bullet">
    /// <item>Uses the default Azure credential resolution chain</item>
    /// <item>Loads all secrets from the specified Key Vault</item>
    /// <item>Transforms secret names from Azure format to .NET configuration keys</item>
    /// <item>Does not process JSON secrets (treats them as simple strings)</item>
    /// <item>Does not automatically reload secrets</item>
    /// </list>
    /// </para>
    ///
    /// <para>
    /// <strong>Secret Name Transformation Example:</strong>
    /// <code>
    /// // Key Vault:
    /// // myapp--database--host = "localhost"
    /// // myapp--database--port = "5432"
    /// // myapp--features--caching = "true"
    ///
    /// // Resulting configuration keys:
    /// // myapp:database:host = "localhost"
    /// // myapp:database:port = "5432"
    /// // myapp:features:caching = "true"
    /// </code>
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="builder"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="vaultUri"/> is null or empty.</exception>
    /// <example>
    /// <code>
    /// // Basic Key Vault configuration
    /// var config = new FlexConfigurationBuilder()
    ///     .AddAzureKeyVault("https://myapp-vault.vault.azure.net/")
    ///     .Build();
    ///
    /// // Access secret values
    /// var dbHost = config["myapp:database:host"];
    /// var caching = config["myapp:features:caching"];
    ///
    /// // With other configuration sources
    /// var config = new FlexConfigurationBuilder()
    ///     .AddJsonFile("appsettings.json")
    ///     .AddAzureKeyVault("https://myapp-vault.vault.azure.net/")
    ///     .AddEnvironmentVariables()
    ///     .Build();
    /// </code>
    /// </example>
    [UsedImplicitly]
    public static FlexConfigurationBuilder AddAzureKeyVault(
        this FlexConfigurationBuilder builder,
        string vaultUri,
        bool optional = true)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrWhiteSpace(vaultUri);

        return builder.AddSource(new AzureKeyVaultConfigurationSource
        {
            VaultUri = vaultUri,
            Optional = optional
        });
    }

    /// <summary>
    /// Adds Azure Key Vault as a configuration source with advanced configuration options.
    /// Provides full control over Key Vault integration, including Azure credentials,
    /// JSON processing, automatic reloading, and custom secret transformation.
    /// </summary>
    /// <param name="builder">The FlexKit configuration builder to add the Key Vault source to.</param>
    /// <param name="configure">
    /// An action to configure the Key Vault options, including vault URI, Azure settings,
    /// JSON processing, reloading, and error handling.
    /// </param>
    /// <returns>The same FlexKit configuration builder instance to enable method chaining.</returns>
    /// <remarks>
    /// This overload provides access to all Key Vault configuration options,
    /// making it suitable for production scenarios that require specific Azure settings,
    /// automatic reloading, or advanced secret processing.
    ///
    /// <para>
    /// <strong>Advanced Configuration Options:</strong>
    /// <list type="bullet">
    /// <item><strong>Azure Options:</strong> Custom credentials, tenants, and client settings</item>
    /// <item><strong>JSON Processing:</strong> Automatic flattening of JSON secrets</item>
    /// <item><strong>Automatic Reloading:</strong> Periodic refresh of secrets from Azure</item>
    /// <item><strong>Custom Processing:</strong> Secret name transformation and filtering</item>
    /// <item><strong>Error Handling:</strong> Custom logic for handling loading failures</item>
    /// <item><strong>Selective JSON Processing:</strong> Apply JSON processing only to specific secrets</item>
    /// </list>
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="builder"/> or <paramref name="configure"/> is null.</exception>
    /// <example>
    /// <code>
    /// // Development configuration with JSON processing and reloading
    /// var config = new FlexConfigurationBuilder()
    ///     .AddAzureKeyVault(options =>
    ///     {
    ///         options.VaultUri = "https://dev-vault.vault.azure.net/";
    ///         options.Optional = true;
    ///         options.JsonProcessor = true;
    ///         options.ReloadAfter = TimeSpan.FromMinutes(2);
    ///     })
    ///     .Build();
    /// </code>
    /// </example>
    [UsedImplicitly]
    public static FlexConfigurationBuilder AddAzureKeyVault(
        this FlexConfigurationBuilder builder,
        Action<AzureKeyVaultOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(configure);

        var options = new AzureKeyVaultOptions();
        configure(options);

        return builder.AddSource(new AzureKeyVaultConfigurationSource
        {
            VaultUri = options.VaultUri ?? string.Empty,
            Optional = options.Optional,
            ReloadAfter = options.ReloadAfter,
            Credential = options.Credential,
            JsonProcessor = options.JsonProcessor,
            JsonProcessorSecrets = options.JsonProcessorSecrets,
            SecretProcessor = options.SecretProcessor,
            OnLoadException = options.OnLoadException,
            SecretClient = options.SecretClient
        });
    }

    /// <summary>
    /// Adds Azure App Configuration as a configuration source to the FlexKit configuration builder.
    /// Enables applications to load configuration data from Azure App Configuration
    /// with support for key filters, labels, and automatic reloading.
    /// </summary>
    /// <param name="builder">The FlexKit configuration builder to add the App Configuration source to.</param>
    /// <param name="connectionString">
    /// The App Configuration connection string or endpoint URI.
    /// Can be a connection string or an endpoint like "https://myapp-config.azconfig.io".
    /// </param>
    /// <param name="optional">
    /// Indicates whether the App Configuration source is optional.
    /// When true, failures to load configuration will not cause configuration building to fail.
    /// Default is true.
    /// </param>
    /// <returns>The same FlexKit configuration builder instance to enable method chaining.</returns>
    /// <remarks>
    /// This is the simplest overload for adding App Configuration support. It uses default
    /// Azure credential resolution and loads all configuration keys without filtering.
    ///
    /// <para>
    /// <strong>Default Behavior:</strong>
    /// <list type="bullet">
    /// <item>Uses the default Azure credential resolution chain</item>
    /// <item>Loads all configuration keys from the specified App Configuration store</item>
    /// <item>Uses the default label (no label)</item>
    /// <item>Does not automatically reload configuration</item>
    /// </list>
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="builder"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="connectionString"/> is null or empty.</exception>
    /// <example>
    /// <code>
    /// // Basic App Configuration
    /// var config = new FlexConfigurationBuilder()
    ///     .AddAzureAppConfiguration("https://myapp-config.azconfig.io")
    ///     .Build();
    ///
    /// // With connection string
    /// var config = new FlexConfigurationBuilder()
    ///     .AddAzureAppConfiguration("Endpoint=https://myapp-config.azconfig.io;Id=...;Secret=...")
    ///     .Build();
    /// </code>
    /// </example>
    [UsedImplicitly]
    public static FlexConfigurationBuilder AddAzureAppConfiguration(
        this FlexConfigurationBuilder builder,
        string connectionString,
        bool optional = true)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        return builder.AddSource(new AzureAppConfigurationSource
        {
            ConnectionString = connectionString,
            Optional = optional
        });
    }

    /// <summary>
    /// Adds Azure App Configuration as a configuration source with advanced configuration options.
    /// Provides full control over App Configuration integration, including Azure credentials,
    /// key filters, labels, automatic reloading, and custom processing.
    /// </summary>
    /// <param name="builder">The FlexKit configuration builder to add the App Configuration source to.</param>
    /// <param name="configure">
    /// An action to configure the App Configuration options, including connection string, Azure settings,
    /// key filters, labels, reloading, and error handling.
    /// </param>
    /// <returns>The same FlexKit configuration builder instance to enable method chaining.</returns>
    /// <remarks>
    /// This overload provides access to all App Configuration options,
    /// making it suitable for production scenarios that require specific Azure settings,
    /// key filtering, labels, or automatic reloading.
    /// </remarks>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="builder"/> or <paramref name="configure"/> is null.</exception>
    /// <example>
    /// <code>
    /// // Production configuration with labels and filtering
    /// var config = new FlexConfigurationBuilder()
    ///     .AddAzureAppConfiguration(options =>
    ///     {
    ///         options.ConnectionString = "https://prod-config.azconfig.io";
    ///         options.Optional = false;
    ///         options.KeyFilter = "myapp:*";
    ///         options.Label = "production";
    ///         options.ReloadAfter = TimeSpan.FromMinutes(5);
    ///     })
    ///     .Build();
    /// </code>
    /// </example>
    [UsedImplicitly]
    public static FlexConfigurationBuilder AddAzureAppConfiguration(
        this FlexConfigurationBuilder builder,
        Action<AzureAppConfigurationOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(configure);

        var options = new AzureAppConfigurationOptions();
        configure(options);

        return builder.AddSource(new AzureAppConfigurationSource
        {
            ConnectionString = options.ConnectionString ?? string.Empty,
            Optional = options.Optional,
            KeyFilter = options.KeyFilter,
            Label = options.Label,
            ReloadAfter = options.ReloadAfter,
            Credential = options.Credential,
            OnLoadException = options.OnLoadException,
            ConfigurationClient = options.ConfigurationClient
        });
    }

    /// <summary>
    /// Checks if a string value contains valid JSON that can be parsed and flattened.
    /// Uses JSON parsing to validate the structure without throwing exceptions.
    /// </summary>
    /// <param name="value">The string value to check.</param>
    /// <returns>True if the value contains valid JSON, false otherwise.</returns>
    internal static bool IsValidJson(this string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var trimmedValue = value.Trim();

        return HasValidJsonStructure(trimmedValue) && CanParseAsJson(trimmedValue);
    }

    /// <summary>
    /// Validates if the JSON string has a valid structure by checking for matching start and end delimiters.
    /// Checks if the string starts and ends with either object or array delimiters.
    /// </summary>
    /// <param name="json">The JSON string to validate.</param>
    /// <returns>True if the string has a valid JSON structure (object or array), false otherwise.</returns>
    private static bool HasValidJsonStructure(string json)
    {
        var isValidObject = json.StartsWith('{') && json.EndsWith('}');
        var isValidArray = json.StartsWith('[') && json.EndsWith(']');

        return isValidObject || isValidArray;
    }

    /// <summary>
    /// Attempts to parse the string as a JSON document to validate its content.
    /// Uses System.Text.JSON parsing to ensure the string contains well-formed JSON.
    /// </summary>
    /// <param name="json">The JSON string to parse.</param>
    /// <returns>True if the string can be parsed as valid JSON, false if parsing fails.</returns>
    private static bool CanParseAsJson(string json)
    {
        try
        {
            JsonDocument.Parse(json);
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    /// <summary>
    /// Flattens a <see cref="JsonElement"/> into key-value pairs, storing the results in the provided dictionary.
    /// Complex objects and arrays are traversed recursively, using <c>parentKey</c> as the prefix for each generated key.
    /// </summary>
    /// <param name="jsonElement">The JSON element to flatten.</param>
    /// <param name="output">The dictionary where flattened key-value pairs will be stored.</param>
    /// <param name="parentKey">The prefix key under which all values from this element will be stored.</param>
    private static void FlattenJsonElement(this JsonElement jsonElement, ConcurrentDictionary<string, string?> output, string parentKey)
    {
        switch (jsonElement.ValueKind)
        {
            case JsonValueKind.Object:
                ProcessObject(jsonElement, output, parentKey);
                break;
            case JsonValueKind.Array:
                ProcessArray(jsonElement, output, parentKey);
                break;
            case JsonValueKind.String:
            case JsonValueKind.Number:
            case JsonValueKind.True:
            case JsonValueKind.False:
            case JsonValueKind.Null:
                ProcessPrimitive(jsonElement, output, parentKey);
                break;
            case JsonValueKind.Undefined:
            default:
                // Skip undefined values.
                break;
        }
    }

    /// <summary>
    /// Processes a <see cref="JsonElement"/> of type <c>Object</c> and flattens its properties into the dictionary.
    /// </summary>
    /// <param name="element">The object JSON element to process.</param>
    /// <param name="output">The target dictionary for flattened key-value pairs.</param>
    /// <param name="parentKey">The key prefix representing the current nesting level.</param>
    private static void ProcessObject(JsonElement element, ConcurrentDictionary<string, string?> output, string parentKey)
    {
        const string keyDelimiter = ":";
        foreach (var property in element.EnumerateObject())
        {
            var key = string.IsNullOrEmpty(parentKey) ? property.Name : $"{parentKey}{keyDelimiter}{property.Name}";
            FlattenJsonElement(property.Value, output, key);
        }
    }

    /// <summary>
    /// Processes a <see cref="JsonElement"/> of type <c>Array</c> and flattens its items into the dictionary.
    /// </summary>
    /// <param name="element">The array JSON element to process.</param>
    /// <param name="output">The target dictionary for flattened key-value pairs.</param>
    /// <param name="parentKey">The key prefix representing the current nesting level.</param>
    private static void ProcessArray(JsonElement element, ConcurrentDictionary<string, string?> output, string parentKey)
    {
        const string keyDelimiter = ":";
        var index = 0;
        foreach (var item in element.EnumerateArray())
        {
            var key = $"{parentKey}{keyDelimiter}{index}";
            FlattenJsonElement(item, output, key);
            index++;
        }
    }

    /// <summary>
    /// Processes a primitive <see cref="JsonElement"/> (string, number, true, false, or null)
    /// and stores its value in the dictionary using the provided key.
    /// </summary>
    /// <param name="element">The primitive JSON element to process.</param>
    /// <param name="output">The target dictionary for flattened key-value pairs.</param>
    /// <param name="key">The resulting key under which the value will be stored.</param>
    private static void ProcessPrimitive(JsonElement element, ConcurrentDictionary<string, string?> output, string key)
    {
        output[key] = element.ValueKind switch
        {
            JsonValueKind.String => element.GetString(),
            JsonValueKind.Number => element.GetRawText(),
            JsonValueKind.True => "true",
            JsonValueKind.False => "false",
            _ => null
        };
    }

    /// <summary>
    /// Flattens a JSON string into hierarchical configuration keys following .NET configuration conventions.
    /// Converts JSON objects and arrays into a flat key-value structure that can be used with strongly typed binding.
    /// </summary>
    /// <param name="jsonValue">The JSON string to flatten.</param>
    /// <param name="configurationData">The dictionary to store the flattened configuration data.</param>
    /// <param name="prefix">The key prefix to prepend to all flattened keys.</param>
    internal static void FlattenJsonValue(this string jsonValue, ConcurrentDictionary<string, string?> configurationData, string prefix)
    {
        try
        {
            using var document = JsonDocument.Parse(jsonValue);
            document.RootElement.FlattenJsonElement(configurationData, prefix);
        }
        catch (JsonException)
        {
            // If JSON parsing fails, store as a simple string value
            configurationData[prefix] = jsonValue;
        }
    }
}
