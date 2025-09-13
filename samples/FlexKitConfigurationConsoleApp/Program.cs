using FlexKit.Configuration.Core;
using FlexKit.Configuration.Conversion;
using FlexKit.Configuration.Providers.Aws.Extensions;
using FlexKit.Configuration.Providers.Azure.Extensions;
using FlexKit.Configuration.Providers.Yaml.Extensions;
using FlexKitConfigurationConsoleApp.Configuration;
using FlexKitConfigurationConsoleApp.Services;
using Microsoft.Extensions.Configuration.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace FlexKitConfigurationConsoleApp;

internal class Program
{
    private static async Task Main(string[] args)
    {
        Console.WriteLine("=== FlexKit Configuration Console App Demo ===\n");

        // Set the environment to Development for this demo
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Development");
        Environment.SetEnvironmentVariable("API__APIKEY", "environment-variable-api-key-98765");

        try
        {
            // Create host builder and configure FlexKit
            var builder = Host.CreateDefaultBuilder(args);

            // Demonstrate the host-based AddFlexConfig extension method with comprehensive configuration sources
            builder.AddFlexConfig(config =>
            {
                Console.WriteLine("Configuring FlexKit with comprehensive configuration sources (including Azure)...");
                
                // Add YAML configuration files in order of precedence (lowest to highest)
                config.AddYamlFile("appsettings.yaml", optional: false)          // Base YAML configuration (required)
                      .AddYamlFile("features.yaml", optional: true)              // Feature-specific YAML
                      .AddYamlFile("database.yaml", optional: true)              // Database-specific YAML
                      .AddYamlFile($"services.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")}.yaml", optional: true); // Environment-specific services
                
                // Add AWS Parameter Store configuration (demonstrates all AWS features)
                config.AddAwsParameterStore("/flexkit-test/development/", optional: true)  // Basic Parameter Store
                      .AddAwsParameterStore(options =>                                      // Advanced Parameter Store with JSON processing
                      {
                          options.Path = "/flexkit-test/shared/";
                          options.Optional = true;
                          options.JsonProcessor = false;  // Don't process shared parameters as JSON
                      });
                
                // Add Azure Key Vault configuration (demonstrates Key Vault features)
                config.AddAzureKeyVault("https://flexkittestkv1757629870.vault.azure.net/", optional: true)  // Basic Key Vault
                      .AddAzureKeyVault(options =>                                                             // Advanced Key Vault with JSON processing
                      {
                          options.VaultUri = "https://flexkittestkv1757629870.vault.azure.net/";
                          options.Optional = true;
                          options.JsonProcessor = true;
                          options.JsonProcessorSecrets = new[] { "flexkit--features--config", "flexkit--api--config" };
                          options.ReloadAfter = TimeSpan.FromMinutes(5);
                      });
                
                // Add Azure App Configuration with JSON processing
                config.AddAzureAppConfiguration(options =>
                      {
                          options.ConnectionString = "https://flexkit-test-config-1757629946.azconfig.io";
                          options.Optional = true;
                          options.KeyFilter = "flexkit:*";
                          // options.Label = "development";  // Removed to load all keys regardless of label
                          options.JsonProcessor = true;
                          options.ReloadAfter = TimeSpan.FromMinutes(3);
                      });
                
                // Add .env file support (higher precedence than YAML and AWS)
                config.AddDotEnvFile(".env", optional: true);
                
                // Add in-memory configuration source using AddSource method (highest precedence)
                var memoryData = new Dictionary<string, string?>
                {
                    ["InMemory:Setting1"] = "Value from in-memory source",
                    ["InMemory:Setting2"] = "Another in-memory value",
                    ["Database:MaxRetries"] = "8", // Override JSON, YAML, and AWS values
                    ["Features:InMemoryFeature"] = "true",
                    ["YamlDemo:Enabled"] = "true",
                    ["AwsDemo:Enabled"] = "true",
                    ["AwsDemo:ParameterStore:TestCompleted"] = "true"
                };

                var memorySource = new MemoryConfigurationSource
                {
                    InitialData = memoryData
                };
                
                config.AddSource(memorySource);
                
                Console.WriteLine("FlexKit configuration sources added successfully (including AWS Parameter Store and Azure services).\n");
            });

            // Configure services with strongly typed configuration classes using Microsoft DI
            // This demonstrates the new services.ConfigureFlexKit<T>() extension methods
            builder.ConfigureServices((context, services) =>
            {
                // Register strongly typed configuration classes using the new ConfigureFlexKit method
                services.ConfigureFlexKit<DatabaseConfig>("Database");
                services.ConfigureFlexKit<ApiConfig>("Api");
                services.ConfigureFlexKit<AppConfig>(); // Root level binding
                services.ConfigureFlexKit<AzureConfig>("Azure");
                services.ConfigureFlexKit<AzureFeaturesConfig>("flexkit:features");
                
                // Register application services
                services.AddScoped<IDatabaseService, DatabaseService>();
                services.AddScoped<IApiService, ApiService>();
                services.AddScoped<IServerManagementService, ServerManagementService>();
                services.AddScoped<AzureTestService>();
            });

            var host = builder.Build();

            // Get services and configuration
            var flexConfig = host.Services.GetRequiredService<IFlexConfig>();
            var databaseService = host.Services.GetRequiredService<IDatabaseService>();
            var apiService = host.Services.GetRequiredService<IApiService>();
            var serverService = host.Services.GetRequiredService<IServerManagementService>();
            var azureTestService = host.Services.GetRequiredService<AzureTestService>();

            // === Demonstrate Configuration Sources ===
            Console.WriteLine("=== Configuration Sources Demonstration ===");
            await DemonstrateConfigurationSources(flexConfig);

            // === Demonstrate YAML Configuration Features ===
            Console.WriteLine("=== YAML Configuration Features Demonstration ===");
            await DemonstrateYamlConfigurationFeatures(flexConfig);
            
            // === Demonstrate AWS Parameter Store Configuration Features ===
            Console.WriteLine("=== AWS Parameter Store Configuration Features Demonstration ===");
            await DemonstrateAwsParameterStoreFeatures(flexConfig);
            
            // === Demonstrate Strongly Typed Configuration ===
            Console.WriteLine("=== Strongly Typed Configuration Access ===");
            databaseService.DisplayConfiguration();
            await databaseService.TestConnectionAsync();
            Console.WriteLine();
            
            // === Demonstrate Dynamic Access ===
            Console.WriteLine("=== Dynamic Configuration Access ===");
            apiService.DisplayConfiguration();
            await apiService.CallApiAsync();
            Console.WriteLine();
            
            // === Demonstrate Property Injection ===
            Console.WriteLine("=== Property Injection by Convention ===");
            serverService.DisplayConfiguration();
            var activeServers = await serverService.GetActiveServersAsync();
            Console.WriteLine($"Active servers found: {activeServers.Count}");
            foreach (var server in activeServers)
            {
                Console.WriteLine($"  - {server}");
            }
            Console.WriteLine();
            
            // === Demonstrate Type Conversion ===
            Console.WriteLine("=== Type Conversion Demonstration ===");
            await DemonstrateTypeConversion(flexConfig);
            
            // === Test Azure Configuration Providers ===
            azureTestService.TestAzureKeyVaultIntegration();
            azureTestService.TestAzureAppConfigurationIntegration();
            azureTestService.VerifyAzureIntegration();
            
            Console.WriteLine("=== Demo completed successfully! ===");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error occurred: {ex.Message}");
            Console.WriteLine(ex.StackTrace);
        }
    }

    private static async Task DemonstrateConfigurationSources(IFlexConfig flexConfig)
    {
        Console.WriteLine("Configuration values from different sources:");
        Console.WriteLine("(Higher precedence sources override lower precedence)");
        Console.WriteLine();
        
        Console.WriteLine("1. JSON Configuration (appsettings.json):");
        Console.WriteLine($"   Application Name: {flexConfig["Application:Name"]}");
        Console.WriteLine($"   Database Timeout: {flexConfig["Database:CommandTimeout"]}");
        
        Console.WriteLine("\n2. Environment-specific JSON (appsettings.Development.json):");
        Console.WriteLine($"   Database ConnectionString: {flexConfig["Database:ConnectionString"]}");
        Console.WriteLine($"   Logging Level: {flexConfig["Logging:LogLevel:Default"]}");
        
        Console.WriteLine("\n3. YAML Configuration (appsettings.yaml, features.yaml, database.yaml):");
        Console.WriteLine($"   Application Name from YAML: {flexConfig["Application:Name"]}");
        Console.WriteLine($"   YAML Database Max Retries: {flexConfig["Database:MaxRetries"]}");
        Console.WriteLine($"   Feature Flag from YAML: {flexConfig["FeatureFlags:EnableNewDashboard"]}");
        
        Console.WriteLine("\n4. AWS Parameter Store Configuration:");
        Console.WriteLine($"   Application Name from AWS: {flexConfig["application:name"]}");
        Console.WriteLine($"   AWS Database Command Timeout: {flexConfig["database:command-timeout"]}");
        Console.WriteLine($"   AWS Feature Enable New Dashboard: {flexConfig["features:enable-new-dashboard"]}");
        Console.WriteLine($"   Shared Cache Provider: {flexConfig["cache:provider"]}");
        
        Console.WriteLine("\n5. .env File Configuration:");
        Console.WriteLine($"   API Key from .env: {flexConfig["API_APIKEY"] ?? "Not found"}");
        Console.WriteLine($"   Environment Name: {flexConfig["ENVIRONMENT_NAME"] ?? "Not found"}");
        
        Console.WriteLine("\n6. Environment Variables:");
        Console.WriteLine($"   API Key from env var: {flexConfig["API:APIKEY"] ?? "Not found"}");
        
        Console.WriteLine("\n7. In-Memory Source (Highest Precedence):");
        Console.WriteLine($"   In-Memory Setting1: {flexConfig["InMemory:Setting1"]}");
        Console.WriteLine($"   Max Retries (final override): {flexConfig["Database:MaxRetries"]}");
        Console.WriteLine($"   AWS Demo Enabled: {flexConfig["AwsDemo:Enabled"]}");
        
        Console.WriteLine();
        await Task.CompletedTask;
    }

    private static async Task DemonstrateYamlConfigurationFeatures(IFlexConfig flexConfig)
    {
        Console.WriteLine("YAML Configuration Features:");
        Console.WriteLine("(Demonstrating YAML's superior readability and features)");
        Console.WriteLine();

        // === Multi-line strings and comments ===
        Console.WriteLine("1. Multi-line Strings and Comments Support:");
        dynamic config = flexConfig;
        var description = config?.Application?.Description as string;
        if (!string.IsNullOrEmpty(description))
        {
            var lines = description.Split('\n').Take(3); // Show first 3 lines
            foreach (var line in lines)
            {
                Console.WriteLine($"   {line.Trim()}");
            }
            Console.WriteLine("   ... (truncated)");
        }
        Console.WriteLine();

        // === Complex nested structures ===
        Console.WriteLine("2. Complex Nested Structures:");
        Console.WriteLine($"   External API Base URL: {config?.ExternalApis?.PaymentGateway?.BaseUrl}");
        Console.WriteLine($"   Circuit Breaker Threshold: {config?.ExternalApis?.PaymentGateway?.CircuitBreaker?.FailureThreshold}");
        Console.WriteLine($"   Notification Email Provider: {config?.ExternalApis?.NotificationService?.Channels?.Email?.Provider}");
        Console.WriteLine();

        // === Arrays and collections ===
        Console.WriteLine("3. YAML Arrays and Collections:");
        Console.WriteLine("   Server configurations from YAML:");
        
        // Demonstrate YAML array access using indexed access
        for (int i = 0; i < 10; i++)
        {
            var server = flexConfig[i];
            if (server == null) break;

            dynamic serverConfig = server;
            var name = serverConfig?.Name as string;
            var host = serverConfig?.Host as string;
            var port = serverConfig?.Port;
            var environment = serverConfig?.Environment as string;
            
            if (!string.IsNullOrEmpty(name))
            {
                Console.WriteLine($"     Server {i}: {name} ({host}:{port}) [{environment}]");
                
                // Demonstrate complex nested access in arrays
                var tags = serverConfig?.Tags;
                if (tags != null)
                {
                    var tagList = new List<string>();
                    // Convert tags to string list if it's an array
                    if (tags is System.Collections.IEnumerable tagEnumerable && tags is not string)
                    {
                        foreach (var tag in tagEnumerable)
                        {
                            if (tag != null)
                                tagList.Add(tag.ToString()!);
                        }
                        Console.WriteLine($"       Tags: [{string.Join(", ", tagList)}]");
                    }
                }
            }
        }
        Console.WriteLine();

        // === Feature flags and configuration ===
        Console.WriteLine("4. Feature Flags and Complex Configuration:");
        Console.WriteLine($"   Enable New Dashboard: {config?.FeatureFlags?.EnableNewDashboard}");
        Console.WriteLine($"   Enable Analytics: {config?.FeatureFlags?.EnableAnalytics}");
        Console.WriteLine($"   Dashboard Refresh Interval: {config?.Features?.Dashboard?.RefreshIntervalSeconds}s");
        Console.WriteLine($"   Max Widgets: {config?.Features?.Dashboard?.MaxWidgets}");
        Console.WriteLine();

        // === Database configuration hierarchy ===
        Console.WriteLine("5. Database Configuration Hierarchy:");
        Console.WriteLine($"   Primary DB Host: {config?.Database?.Providers?.Primary?.Host}");
        Console.WriteLine($"   Primary DB SSL Mode: {config?.Database?.Providers?.Primary?.Ssl?.Mode}");
        Console.WriteLine($"   Analytics DB Name: {config?.Database?.Providers?.Analytics?.Database}");
        Console.WriteLine($"   Connection Pool Size: {config?.Database?.ConnectionPoolSize}");
        Console.WriteLine();

        // === Monitoring and performance settings ===
        Console.WriteLine("6. Monitoring and Performance Settings:");
        Console.WriteLine($"   Metrics Collection Interval: {config?.Monitoring?.Metrics?.CollectionInterval}s");
        Console.WriteLine($"   Health Check Interval: {config?.Monitoring?.HealthChecks?.CheckInterval}s");
        Console.WriteLine($"   Memory Buffer Pool Max Size: {config?.Optimization?.Memory?.BufferPool?.MaxSizeMB}MB");
        Console.WriteLine();

        // === Security configuration ===
        Console.WriteLine("7. Security Configuration:");
        var corsOrigins = config?.Security?.Cors?.AllowedOrigins;
        if (corsOrigins is System.Collections.IEnumerable corsCollection && corsOrigins is not string)
        {
            var origins = new List<string>();
            foreach (var origin in corsCollection)
            {
                if (origin != null)
                    origins.Add(origin.ToString()!);
            }
            if (origins.Count > 0)
                Console.WriteLine($"   CORS Allowed Origins: {string.Join(", ", origins)}");
        }
        Console.WriteLine($"   JWT Expiration Minutes: {config?.Security?.Jwt?.ExpirationMinutes}");
        Console.WriteLine($"   Rate Limit Requests Per Minute: {config?.Security?.RateLimit?.RequestsPerMinute}");
        Console.WriteLine();

        // === Type conversion with YAML values ===
        Console.WriteLine("8. Type Conversion with YAML Values:");
        var metricsInterval = flexConfig["Monitoring:Metrics:CollectionInterval"].ToType<int>();
        var healthCheckInterval = flexConfig["Monitoring:HealthChecks:CheckInterval"].ToType<int>();
        var enableAnalytics = flexConfig["FeatureFlags:EnableAnalytics"].ToType<bool>();
        
        // Safe nullable conversion
        var bufferPoolSizeStr = flexConfig["Optimization:Memory:BufferPool:MaxSizeMB"];
        int? bufferPoolSize = null;
        if (!string.IsNullOrEmpty(bufferPoolSizeStr))
        {
            bufferPoolSize = bufferPoolSizeStr.ToType<int>();
        }
        
        Console.WriteLine($"   Metrics Interval (int): {metricsInterval}");
        Console.WriteLine($"   Health Check Interval (int): {healthCheckInterval}");
        Console.WriteLine($"   Analytics Enabled (bool): {enableAnalytics}");
        Console.WriteLine($"   Buffer Pool Size (int?): {bufferPoolSize}");
        Console.WriteLine();

        // === Environment-specific overrides ===
        Console.WriteLine("9. Environment-specific YAML Overrides:");
        Console.WriteLine($"   Service Email Provider: {config?.ServiceConfiguration?.EmailService?.Provider}");
        Console.WriteLine($"   Service Email SMTP Host: {config?.ServiceConfiguration?.EmailService?.SmtpHost}");
        Console.WriteLine($"   Service Email SMTP Port: {config?.ServiceConfiguration?.EmailService?.SmtpPort}");
        Console.WriteLine($"   Development Hot Reload: {config?.Development?.HotReload?.Enabled}");
        Console.WriteLine();

        await Task.CompletedTask;
    }

    private static async Task DemonstrateTypeConversion(IFlexConfig flexConfig)
    {
        Console.WriteLine("Type conversion examples:");
        
        // Basic type conversions
        var port = flexConfig["Api:Timeout"].ToType<int>();
        var enableLogging = flexConfig["Api:EnableLogging"].ToType<bool>();
        var maxUsers = flexConfig["Features:MaxConcurrentUsers"].ToType<int>();
        
        Console.WriteLine($"API Timeout (string -> int): {port}");
        Console.WriteLine($"Enable Logging (string -> bool): {enableLogging}");
        Console.WriteLine($"Max Users (string -> int): {maxUsers}");
        
        // Collection conversion
        var allowedHosts = flexConfig["Features:AllowedHosts"].GetCollection<string>();
        Console.WriteLine($"Allowed Hosts (string -> string[]): [{string.Join(", ", allowedHosts ?? new string[0])}]");
        
        // Nullable conversion
        var optionalSetting = flexConfig["NonExistent:Setting"].ToType<int?>();
        Console.WriteLine($"Optional Setting (null -> int?): {optionalSetting}");
        
        // Dynamic access with conversion
        dynamic config = flexConfig;
        var dynamicTimeout = ((string?)config.Api.Timeout)?.ToType<TimeSpan>();
        Console.WriteLine($"Timeout as TimeSpan: {dynamicTimeout}");
        
        Console.WriteLine();
        await Task.CompletedTask;
    }

    private static async Task DemonstrateAwsParameterStoreFeatures(IFlexConfig flexConfig)
    {
        Console.WriteLine("AWS Parameter Store Configuration Features:");
        Console.WriteLine("(Demonstrating all AWS Parameter Store integration capabilities)");
        Console.WriteLine();


        // === Basic Parameter Store access ===
        Console.WriteLine("1. Basic Parameter Store Access:");
        Console.WriteLine($"   Application Name: {flexConfig["application:name"]}");
        Console.WriteLine($"   Application Version: {flexConfig["application:version"]}");
        Console.WriteLine($"   Environment: {flexConfig["application:environment"]}");
        Console.WriteLine($"   Debug Enabled: {flexConfig["application:debug-enabled"]}");
        Console.WriteLine();

        // === SecureString parameters (encrypted) ===
        Console.WriteLine("2. SecureString Parameters (Encrypted in Parameter Store):");
        Console.WriteLine($"   Database Connection String: {flexConfig["database:connection-string"]}");
        Console.WriteLine($"   API Key: {flexConfig["api:api-key"]}");
        Console.WriteLine($"   JWT Secret: {flexConfig["security:jwt-secret"]}");
        Console.WriteLine($"   Encryption Key: {flexConfig["security:encryption-key"]}");
        Console.WriteLine();

        // === JSON parameter processing ===  
        Console.WriteLine("3. JSON Parameter Processing:");
        // Note: The providers parameter contains complex JSON structure
        var databaseProvidersJson = flexConfig["database:providers"];
        Console.WriteLine($"   Database Providers JSON: {databaseProvidersJson}");
        
        var externalServicesJson = flexConfig["api:external-services"];
        Console.WriteLine($"   External Services JSON: {externalServicesJson}");
        
        var advancedFeaturesJson = flexConfig["features:advanced-features"];
        Console.WriteLine($"   Advanced Features JSON: {advancedFeaturesJson}");
        Console.WriteLine();

        // === Dynamic access to AWS parameters ===
        Console.WriteLine("4. Dynamic Access to AWS Parameters:");
        dynamic config = flexConfig;
        
        // Access flattened parameter structure
        var appName = config?.application?.name as string;
        var dbTimeout = config?.database?.command_timeout as string;
        var cacheProvider = config?.cache?.provider as string;
        
        Console.WriteLine($"   App Name (dynamic): {appName}");
        Console.WriteLine($"   DB Timeout (dynamic): {dbTimeout}");
        Console.WriteLine($"   Cache Provider (dynamic): {cacheProvider}");
        Console.WriteLine();

        // === Type conversion with AWS parameters ===
        Console.WriteLine("5. Type Conversion with AWS Parameters:");
        var maxUsers = flexConfig["application:max-concurrent-users"].ToType<int>();
        var commandTimeout = flexConfig["database:command-timeout"].ToType<int>();
        var connectionPoolSize = flexConfig["database:connection-pool-size"].ToType<int>();
        var enableNewDashboard = flexConfig["features:enable-new-dashboard"].ToType<bool>();
        var enableAnalytics = flexConfig["features:enable-analytics"].ToType<bool>();
        var cacheExpiryMinutes = flexConfig["features:cache-expiry-minutes"].ToType<int>();
        
        Console.WriteLine($"   Max Concurrent Users (int): {maxUsers}");
        Console.WriteLine($"   Command Timeout (int): {commandTimeout}");
        Console.WriteLine($"   Connection Pool Size (int): {connectionPoolSize}");
        Console.WriteLine($"   Enable New Dashboard (bool): {enableNewDashboard}");
        Console.WriteLine($"   Enable Analytics (bool): {enableAnalytics}");
        Console.WriteLine($"   Cache Expiry Minutes (int): {cacheExpiryMinutes}");
        Console.WriteLine();

        // === Array structure from individual parameters ===
        Console.WriteLine("6. Array Structure from Individual Parameters:");
        Console.WriteLine("   Server configurations from AWS Parameter Store:");
        
        for (int i = 0; i < 3; i++) // Check for up to 3 servers
        {
            var serverName = flexConfig[$"servers:{i}:name"];
            var serverHost = flexConfig[$"servers:{i}:host"];
            var serverPort = flexConfig[$"servers:{i}:port"];
            var serverActive = flexConfig[$"servers:{i}:is-active"];
            
            if (!string.IsNullOrEmpty(serverName))
            {
                Console.WriteLine($"     Server {i}: {serverName} ({serverHost}:{serverPort}) - Active: {serverActive}");
            }
        }
        Console.WriteLine();

        // === Monitoring and operational parameters ===
        Console.WriteLine("7. Monitoring and Operational Parameters:");
        var metricsInterval = flexConfig["monitoring:metrics-interval"].ToType<int>();
        var healthCheckInterval = flexConfig["monitoring:health-check-interval"].ToType<int>();
        var detailedLogging = flexConfig["monitoring:enable-detailed-logging"].ToType<bool>();
        
        Console.WriteLine($"   Metrics Interval: {metricsInterval} seconds");
        Console.WriteLine($"   Health Check Interval: {healthCheckInterval} seconds");
        Console.WriteLine($"   Detailed Logging Enabled: {detailedLogging}");
        Console.WriteLine();

        // === Shared configuration across environments ===
        Console.WriteLine("8. Shared Configuration Across Environments:");
        Console.WriteLine($"   Cache Provider: {flexConfig["cache:provider"]}");
        Console.WriteLine($"   Cache Connection String: {flexConfig["cache:connection-string"]}");
        var cacheTtl = flexConfig["cache:default-ttl-minutes"].ToType<int>();
        Console.WriteLine($"   Default Cache TTL: {cacheTtl} minutes");
        Console.WriteLine();

        // === API configuration from Parameter Store ===
        Console.WriteLine("9. API Configuration from Parameter Store:");
        Console.WriteLine($"   API Base URL: {flexConfig["api:base-url"]}");
        var apiTimeout = flexConfig["api:timeout"].ToType<int>();
        var apiRetryCount = flexConfig["api:retry-count"].ToType<int>();
        Console.WriteLine($"   API Timeout: {apiTimeout}ms");
        Console.WriteLine($"   API Retry Count: {apiRetryCount}");
        Console.WriteLine();

        // === Parameter Store path hierarchies ===
        Console.WriteLine("10. Parameter Store Path Hierarchies:");
        Console.WriteLine("   Demonstrating hierarchical parameter organization:");
        Console.WriteLine("   /flexkit-test/development/application/* - Application settings");
        Console.WriteLine("   /flexkit-test/development/database/* - Database configuration");
        Console.WriteLine("   /flexkit-test/development/api/* - API settings");
        Console.WriteLine("   /flexkit-test/development/features/* - Feature flags");
        Console.WriteLine("   /flexkit-test/development/monitoring/* - Monitoring settings");
        Console.WriteLine("   /flexkit-test/shared/* - Cross-environment shared settings");
        Console.WriteLine();

        // === Parameter Store best practices demonstration ===
        Console.WriteLine("11. Parameter Store Best Practices Demonstrated:");
        Console.WriteLine("   ✅ Hierarchical organization with environment separation");
        Console.WriteLine("   ✅ SecureString parameters for sensitive data (encrypted with AWS KMS)");
        Console.WriteLine("   ✅ JSON parameters for complex configuration structures");
        Console.WriteLine("   ✅ Shared parameters for cross-environment configuration");
        Console.WriteLine("   ✅ Individual parameters for simple array structures");
        Console.WriteLine("   ✅ Clear parameter descriptions for operational documentation");
        Console.WriteLine("   ✅ Optional configuration loading to prevent application startup failures");
        Console.WriteLine();

        await Task.CompletedTask;
    }
}