// <copyright file="FlexConfigurationBuilderAwsExtensions.cs" company="FlexKit">
// Copyright (c) FlexKit. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

using System.Text.Json;
using FlexKit.Configuration.Core;
using FlexKit.Configuration.Providers.Aws.Options;
using FlexKit.Configuration.Providers.Aws.Sources;
using JetBrains.Annotations;

namespace FlexKit.Configuration.Providers.Aws.Extensions;

/// <summary>
/// Extension methods for <see cref="FlexConfigurationBuilder"/> that add AWS configuration source support.
/// Provides fluent API methods to integrate AWS services like Parameter Store and Secrets Manager
/// with the FlexKit configuration system.
/// </summary>
/// <remarks>
/// These extension methods follow the FlexKit configuration pattern of providing a fluent,
/// strongly typed API for adding configuration sources. They maintain consistency with other
/// FlexKit configuration providers while providing AWS-specific functionality and options.
///
/// <para>
/// <strong>AWS Integration Benefits:</strong>
/// <list type="bullet">
/// <item>Centralized configuration management across multiple applications and environments</item>
/// <item>Built-in encryption for sensitive configuration data using AWS KMS</item>
/// <item>Hierarchical organization of configuration parameters</item>
/// <item>Automatic credential management using AWS IAM roles and policies</item>
/// <item>Support for configuration versioning and change tracking</item>
/// <item>Integration with AWS CloudFormation and other infrastructure-as-code tools</item>
/// </list>
/// </para>
///
/// <para>
/// <strong>Security Considerations:</strong>
/// AWS configuration sources automatically use the AWS credential resolution chain,
/// ensuring secure access to configuration data without hardcoding credentials in
/// application code. IAM policies can be used to provide fine-grained access control
/// to specific configuration parameters.
/// </para>
/// </remarks>
public static class AwsExtensions
{
    /// <summary>
    /// Adds AWS Parameter Store as a configuration source to the FlexKit configuration builder.
    /// Enables applications to load configuration data from AWS Systems Manager Parameter Store
    /// with support for hierarchical parameters, JSON processing, and automatic reloading.
    /// </summary>
    /// <param name="builder">The FlexKit configuration builder to add the Parameter Store source to.</param>
    /// <param name="path">
    /// The Parameters Store path prefix to load parameters from.
    /// Should start with a forward slash (e.g., "/myapp/", "/prod/database/").
    /// </param>
    /// <param name="optional">
    /// Indicates whether the Parameter Store source is optional.
    /// When true, failures to load parameters will not cause configuration building to fail.
    /// Default is true.
    /// </param>
    /// <returns>The same FlexKit configuration builder instance to enable method chaining.</returns>
    /// <remarks>
    /// This is the simplest overload for adding Parameter Store support. It uses default
    /// AWS credential resolution and basic parameter processing without JSON flattening
    /// or automatic reloading.
    ///
    /// <para>
    /// <strong>Default Behavior:</strong>
    /// <list type="bullet">
    /// <item>Uses the default AWS credential resolution chain</item>
    /// <item>Loads all parameters under the specified path recursively</item>
    /// <item>Transforms parameter names from AWS format to .NET configuration keys</item>
    /// <item>Does not process JSON parameters (treats them as simple strings)</item>
    /// <item>Does not automatically reload parameters</item>
    /// </list>
    /// </para>
    ///
    /// <para>
    /// <strong>Parameter Transformation Example:</strong>
    /// <code>
    /// // AWS Parameter Store:
    /// // /myapp/database/host = "localhost"
    /// // /myapp/database/port = "5432"
    /// // /myapp/features/caching = "true"
    ///
    /// // Resulting configuration keys:
    /// // myapp:database:host = "localhost"
    /// // myapp:database:port = "5432"
    /// // myapp:features:caching = "true"
    /// </code>
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="builder"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="path"/> is null or empty.</exception>
    /// <example>
    /// <code>
    /// // Basic Parameter Store configuration
    /// var config = new FlexConfigurationBuilder()
    ///     .AddAwsParameterStore("/myapp/")
    ///     .Build();
    ///
    /// // Access parameter values
    /// var dbHost = config["myapp:database:host"];
    /// var caching = config["myapp:features:caching"];
    ///
    /// // With other configuration sources
    /// var config = new FlexConfigurationBuilder()
    ///     .AddJsonFile("appsettings.json")
    ///     .AddAwsParameterStore("/myapp/")
    ///     .AddEnvironmentVariables()
    ///     .Build();
    /// </code>
    /// </example>
    [UsedImplicitly]
    public static FlexConfigurationBuilder AddAwsParameterStore(
        this FlexConfigurationBuilder builder,
        string path,
        bool optional = true)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        return builder.AddSource(new AwsParameterStoreConfigurationSource
        {
            Path = path,
            Optional = optional
        });
    }

    /// <summary>
    /// Adds AWS Parameter Store as a configuration source with advanced configuration options.
    /// Provides full control over Parameter Store integration, including AWS credentials,
    /// JSON processing, automatic reloading, and custom parameter transformation.
    /// </summary>
    /// <param name="builder">The FlexKit configuration builder to add the Parameter Store source to.</param>
    /// <param name="configure">
    /// An action to configure the Parameter Store options, including path, AWS settings,
    /// JSON processing, reloading, and error handling.
    /// </param>
    /// <returns>The same FlexKit configuration builder instance to enable method chaining.</returns>
    /// <remarks>
    /// This overload provides access to all Parameter Store configuration options,
    /// making it suitable for production scenarios that require specific AWS settings,
    /// automatic reloading, or advanced parameter processing.
    ///
    /// <para>
    /// <strong>Advanced Configuration Options:</strong>
    /// <list type="bullet">
    /// <item><strong>AWS Options:</strong> Custom credentials, regions, and SDK settings</item>
    /// <item><strong>JSON Processing:</strong> Automatic flattening of JSON parameters</item>
    /// <item><strong>Automatic Reloading:</strong> Periodic refresh of parameters from AWS</item>
    /// <item><strong>Custom Processing:</strong> Parameter name transformation and filtering</item>
    /// <item><strong>Error Handling:</strong> Custom logic for handling loading failures</item>
    /// <item><strong>Selective JSON Processing:</strong> Apply JSON processing only to specific paths</item>
    /// </list>
    /// </para>
    ///
    /// <para>
    /// <strong>Production Configuration Example:</strong>
    /// <code>
    /// var config = new FlexConfigurationBuilder()
    ///     .AddAwsParameterStore(options =>
    ///     {
    ///         options.Path = "/prod/myapp/";
    ///         options.Optional = false; // Required in production
    ///         options.JsonProcessor = true;
    ///         options.JsonProcessorPaths = new[] { "/prod/myapp/database/", "/prod/myapp/cache/" };
    ///         options.ReloadAfter = TimeSpan.FromMinutes(10);
    ///         options.AwsOptions = new AWSOptions
    ///         {
    ///             Region = RegionEndpoint.USEast1,
    ///             Profile = "production"
    ///         };
    ///         options.OnLoadException = ex => logger.LogError(ex, "Parameter Store load failed");
    ///     })
    ///     .Build();
    /// </code>
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="builder"/> or <paramref name="configure"/> is null.
    /// </exception>
    /// <example>
    /// <code>
    /// // Development configuration with JSON processing and reloading
    /// var config = new FlexConfigurationBuilder()
    ///     .AddAwsParameterStore(options =>
    ///     {
    ///         options.Path = "/dev/myapp/";
    ///         options.Optional = true;
    ///         options.JsonProcessor = true;
    ///         options.ReloadAfter = TimeSpan.FromMinutes(2);
    ///     })
    ///     .Build();
    ///
    /// // Production configuration with custom AWS settings
    /// var config = new FlexConfigurationBuilder()
    ///     .AddAwsParameterStore(options =>
    ///     {
    ///         options.Path = "/prod/myapp/";
    ///         options.Optional = false;
    ///         options.AwsOptions = new AWSOptions
    ///         {
    ///             Region = RegionEndpoint.USWest2
    ///         };
    ///         options.OnLoadException = ex =>
    ///         {
    ///             // Log to monitoring system
    ///             logger.LogCritical(ex, "Critical Parameter Store failure");
    ///             // Send alert to operations team
    ///             alertService.SendAlert("Parameter Store Failure", ex.Message);
    ///         };
    ///     })
    ///     .Build();
    ///
    /// // Configuration with custom parameter processing
    /// var config = new FlexConfigurationBuilder()
    ///     .AddAwsParameterStore(options =>
    ///     {
    ///         options.Path = "/shared/";
    ///         options.ParameterProcessor = new EnvironmentPrefixProcessor("staging");
    ///         options.JsonProcessor = true;
    ///         options.JsonProcessorPaths = new[] { "/shared/database/", "/shared/cache/" };
    ///     })
    ///     .Build();
    /// </code>
    /// </example>
    [UsedImplicitly]
    public static FlexConfigurationBuilder AddAwsParameterStore(
        this FlexConfigurationBuilder builder,
        Action<AwsParameterStoreOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(configure);

        var options = new AwsParameterStoreOptions();
        configure(options);

        return builder.AddSource(new AwsParameterStoreConfigurationSource
        {
            Path = options.Path ?? string.Empty,
            Optional = options.Optional,
            ReloadAfter = options.ReloadAfter,
            AwsOptions = options.AwsOptions,
            JsonProcessor = options.JsonProcessor,
            JsonProcessorPaths = options.JsonProcessorPaths,
            ParameterProcessor = options.ParameterProcessor,
            OnLoadException = options.OnLoadException
        });
    }

    /// <summary>
    /// Adds AWS Secrets Manager as a configuration source to the FlexKit configuration builder.
    /// Enables applications to load sensitive configuration data from AWS Secrets Manager
    /// with support for JSON processing, binary secrets, and automatic reloading.
    /// </summary>
    /// <param name="builder">The FlexKit configuration builder to add the Secrets Manager source to.</param>
    /// <param name="secretNames">
    /// The array of secret names or ARNs to load from Secrets Manager.
    /// Can include individual secret names or patterns using wildcards (e.g., "myapp/*").
    /// </param>
    /// <param name="optional">
    /// Indicates whether the Secrets Manager source is optional.
    /// When true, failures to load secrets will not cause configuration building to fail.
    /// Default is true.
    /// </param>
    /// <returns>The same FlexKit configuration builder instance to enable method chaining.</returns>
    /// <remarks>
    /// This is the simplest overload for adding Secrets Manager support. It uses default
    /// AWS credential resolution and basic secret processing without JSON flattening
    /// or automatic reloading.
    ///
    /// <para>
    /// <strong>Default Behavior:</strong>
    /// <list type="bullet">
    /// <item>Uses the default AWS credential resolution chain</item>
    /// <item>Loads specified secrets by name or pattern</item>
    /// <item>Transforms secret names from AWS format to .NET configuration keys</item>
    /// <item>Does not process JSON secrets (treats them as simple strings)</item>
    /// <item>Does not automatically reload secrets</item>
    /// <item>Retrieves AWSCURRENT version of secrets</item>
    /// </list>
    /// </para>
    ///
    /// <para>
    /// <strong>Secret Name Transformation Example:</strong>
    /// <code>
    /// // AWS Secrets Manager:
    /// // myapp-database = '{"host": "localhost", "port": 5432}'
    /// // myapp-api-key = "secret-api-key-12345"
    /// // myapp-certificate (binary data)
    ///
    /// // Resulting configuration keys:
    /// // myapp:database = '{"host": "localhost", "port": 5432}' (as string)
    /// // myapp:api:key = "secret-api-key-12345"
    /// // myapp:certificate = "base64-encoded-certificate-data"
    /// </code>
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="builder"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="secretNames"/> is null or empty.</exception>
    /// <example>
    /// <code>
    /// // Basic Secrets Manager configuration
    /// var config = new FlexConfigurationBuilder()
    ///     .AddAwsSecretsManager(new[] { "myapp-database", "myapp-api-keys" })
    ///     .Build();
    ///
    /// // Access secret values
    /// var dbSecret = config["myapp:database"];
    /// var apiKey = config["myapp:api:keys"];
    ///
    /// // With pattern loading
    /// var config = new FlexConfigurationBuilder()
    ///     .AddAwsSecretsManager(new[] { "myapp/*" }) // Load all secrets starting with "myapp"
    ///     .Build();
    ///
    /// // With other configuration sources
    /// var config = new FlexConfigurationBuilder()
    ///     .AddJsonFile("appsettings.json")
    ///     .AddAwsSecretsManager(new[] { "myapp-database" })
    ///     .AddEnvironmentVariables()
    ///     .Build();
    /// </code>
    /// </example>
    [UsedImplicitly]
    public static FlexConfigurationBuilder AddAwsSecretsManager(
        this FlexConfigurationBuilder builder,
        string[] secretNames,
        bool optional = true)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(secretNames);

        return secretNames.Length == 0
            ? throw new ArgumentException(
                "At least one secret name must be specified.",
                nameof(secretNames))
            : builder.AddSource(new AwsSecretsManagerConfigurationSource
            {
                SecretNames = secretNames,
                Optional = optional
            });
    }

    /// <summary>
    /// Adds AWS Secrets Manager as a configuration source with advanced configuration options.
    /// Provides full control over Secrets Manager integration including AWS credentials,
    /// JSON processing, automatic reloading, version stages, and custom secret transformation.
    /// </summary>
    /// <param name="builder">The FlexKit configuration builder to add the Secrets Manager source to.</param>
    /// <param name="configure">
    /// An action to configure the Secrets Manager options, including secret names, AWS settings,
    /// JSON processing, reloading, version stages, and error handling.
    /// </param>
    /// <returns>The same FlexKit configuration builder instance to enable method chaining.</returns>
    /// <remarks>
    /// This overload provides access to all Secrets Manager configuration options,
    /// making it suitable for production scenarios that require specific AWS settings,
    /// automatic reloading, version control, or advanced secret processing.
    ///
    /// <para>
    /// <strong>Advanced Configuration Options:</strong>
    /// <list type="bullet">
    /// <item><strong>AWS Options:</strong> Custom credentials, regions, and SDK settings</item>
    /// <item><strong>JSON Processing:</strong> Automatic flattening of JSON secrets</item>
    /// <item><strong>Version Control:</strong> Specify version stages (AWSCURRENT, AWSPENDING, etc.)</item>
    /// <item><strong>Automatic Reloading:</strong> Periodic refresh of secrets from AWS</item>
    /// <item><strong>Custom Processing:</strong> Secret name transformation and filtering</item>
    /// <item><strong>Error Handling:</strong> Custom logic for handling loading failures</item>
    /// <item><strong>Selective JSON Processing:</strong> Apply JSON processing only to specific secrets</item>
    /// </list>
    /// </para>
    ///
    /// <para>
    /// <strong>Production Configuration Example:</strong>
    /// <code>
    /// var config = new FlexConfigurationBuilder()
    ///     .AddAwsSecretsManager(options =>
    ///     {
    ///         options.SecretNames = new[] { "prod-myapp-database", "prod-myapp-api-keys" };
    ///         options.Optional = false; // Required in production
    ///         options.JsonProcessor = true;
    ///         options.JsonProcessorSecrets = new[] { "prod-myapp-database" };
    ///         options.ReloadAfter = TimeSpan.FromMinutes(15);
    ///         options.VersionStage = "AWSCURRENT";
    ///         options.AwsOptions = new AWSOptions
    ///         {
    ///             Region = RegionEndpoint.USEast1,
    ///             Profile = "production"
    ///         };
    ///         options.OnLoadException = ex => logger.LogError(ex, "Secrets Manager load failed");
    ///     })
    ///     .Build();
    /// </code>
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="builder"/> or <paramref name="configure"/> is null.
    /// </exception>
    /// <example>
    /// <code>
    /// // Development configuration with JSON processing and reloading
    /// var config = new FlexConfigurationBuilder()
    ///     .AddAwsSecretsManager(options =>
    ///     {
    ///         options.SecretNames = new[] { "dev-myapp/*" };
    ///         options.Optional = true;
    ///         options.JsonProcessor = true;
    ///         options.ReloadAfter = TimeSpan.FromMinutes(2);
    ///     })
    ///     .Build();
    ///
    /// // Production configuration with specific version and custom AWS settings
    /// var config = new FlexConfigurationBuilder()
    ///     .AddAwsSecretsManager(options =>
    ///     {
    ///         options.SecretNames = new[] { "prod-myapp-database", "prod-myapp-cache" };
    ///         options.Optional = false;
    ///         options.VersionStage = "AWSCURRENT";
    ///         options.AwsOptions = new AWSOptions
    ///         {
    ///             Region = RegionEndpoint.USWest2,
    ///             Profile = "production"
    ///         };
    ///         options.OnLoadException = ex =>
    ///         {
    ///             // Log to monitoring system
    ///             logger.LogCritical(ex, "Critical Secrets Manager failure");
    ///             // Send alert to operations team
    ///             alertService.SendAlert("Secrets Manager Failure", ex.Message);
    ///         };
    ///     })
    ///     .Build();
    ///
    /// // Configuration with custom secret processing and selective JSON
    /// var config = new FlexConfigurationBuilder()
    ///     .AddAwsSecretsManager(options =>
    ///     {
    ///         options.SecretNames = new[] { "shared-*" };
    ///         options.SecretProcessor = new EnvironmentSecretProcessor("staging");
    ///         options.JsonProcessor = true;
    ///         options.JsonProcessorSecrets = new[] { "shared-database", "shared-cache" };
    ///         options.ReloadAfter = TimeSpan.FromMinutes(10);
    ///     })
    ///     .Build();
    /// </code>
    /// </example>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="configure"/> does not specify any secret names.
    /// </exception>
    [UsedImplicitly]
    public static FlexConfigurationBuilder AddAwsSecretsManager(
        this FlexConfigurationBuilder builder,
        Action<AwsSecretsManagerOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(configure);

        var options = new AwsSecretsManagerOptions();
        configure(options);

        return options.SecretNames == null || options.SecretNames.Length == 0
            ? throw new ArgumentException(
                "At least one secret name must be specified in SecretNames.",
                nameof(configure))
            : builder.AddSource(new AwsSecretsManagerConfigurationSource
            {
                SecretNames = options.SecretNames,
                Optional = options.Optional,
                VersionStage = options.VersionStage,
                ReloadAfter = options.ReloadAfter,
                AwsOptions = options.AwsOptions,
                JsonProcessor = options.JsonProcessor,
                JsonProcessorSecrets = options.JsonProcessorSecrets,
                SecretProcessor = options.SecretProcessor,
                OnLoadException = options.OnLoadException
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

        value = value.Trim();
        var notObject = !value.StartsWith('{') || !value.EndsWith('}');
        var notArray = !value.StartsWith('[') || !value.EndsWith(']');

        if (notObject && notArray)
        {
            return false;
        }

        try
        {
            JsonDocument.Parse(value);
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    /// <summary>
    /// Flattens a <see cref="JsonElement"/> into key-value pairs, storing the results in the provided dictionary.
    /// Complex objects and arrays are traversed recursively, using <c>parentKey</c> as the prefix
    /// for each generated key.
    /// </summary>
    /// <param name="jsonElement">The JSON element to flatten.</param>
    /// <param name="output">The dictionary where flattened key-value pairs will be stored.</param>
    /// <param name="parentKey">The prefix key under which all values from this element will be stored.</param>
    private static void FlattenJsonElement(
        this in JsonElement jsonElement,
        Dictionary<string, string?> output,
        string parentKey)
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
    private static void ProcessObject(
        in JsonElement element,
        Dictionary<string, string?> output,
        string parentKey)
    {
        const string keyDelimiter = ":";
        foreach (var property in element.EnumerateObject())
        {
            var
                key = string.IsNullOrEmpty(parentKey)
                    ? property.Name
                    : parentKey + keyDelimiter + property.Name;
            FlattenJsonElement(property.Value, output, key);
        }
    }

    /// <summary>
    /// Processes a <see cref="JsonElement"/> of type <c>Array</c> and flattens its items into the dictionary.
    /// </summary>
    /// <param name="element">The array JSON element to process.</param>
    /// <param name="output">The target dictionary for flattened key-value pairs.</param>
    /// <param name="parentKey">The key prefix representing the current nesting level.</param>
    private static void ProcessArray(
        in JsonElement element,
        Dictionary<string, string?> output,
        string parentKey)
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
    private static void ProcessPrimitive(
        in JsonElement element,
        Dictionary<string, string?> output,
        string key)
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
    internal static void FlattenJsonValue(
        this string jsonValue,
        Dictionary<string, string?> configurationData,
        string prefix)
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
