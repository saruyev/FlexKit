using BenchmarkDotNet.Attributes;
using FlexKit.Configuration.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Autofac;
using JetBrains.Annotations;
// ReSharper disable ClassTooBig
// ReSharper disable NullableWarningSuppressionIsUsed

namespace FlexKit.Configuration.PerformanceTests.Benchmarks;

/// <summary>
/// Benchmarks for RegisterConfig strongly typed configuration binding vs. IOptions pattern.
/// Tests creation overhead, resolution performance, and memory allocation patterns.
/// </summary>
[MemoryDiagnoser]
[SimpleJob]
public class RegisterConfigBenchmarks
{
    private IContainer _flexKitContainer = null!;
    private IServiceProvider _optionsContainer = null!;
    private Dictionary<string, string?> _configData = null!;

    // Test configuration classes
    public class DatabaseConfig
    {
        public string ConnectionString { get; [UsedImplicitly] set; } = string.Empty;
        public int CommandTimeout { get; [UsedImplicitly] set; } = 30;
        public int MaxRetryCount { get; [UsedImplicitly] set; } = 3;
        public bool EnableLogging { get; [UsedImplicitly] set; }
    }

    public class ApiConfig
    {
        public string BaseUrl { get; set; } = string.Empty;
        public string ApiKey { get; set; } = string.Empty;
        public int Timeout { get; set; } = 5000;
        public bool EnableCompression { get; set; } = true;
    }

    public class AppConfig
    {
        public string Name { get; [UsedImplicitly] set; } = string.Empty;
        public string Version { get; [UsedImplicitly] set; } = string.Empty;
        public string Environment { get; set; } = string.Empty;
        public DatabaseConfig Database { get; [UsedImplicitly] set; } = new();
        [UsedImplicitly] public Dictionary<string, string> Settings { get; [UsedImplicitly] set; } = new();
    }

    [GlobalSetup]
    public void Setup()
    {
        _configData = new Dictionary<string, string?>
        {
            // Database configuration
            ["Database:ConnectionString"] = "Server=localhost;Database=TestDb;User=test;Password=test123",
            ["Database:CommandTimeout"] = "60",
            ["Database:MaxRetryCount"] = "5",
            ["Database:EnableLogging"] = "true",
            
            // API configuration
            ["Api:BaseUrl"] = "https://api.example.com",
            ["Api:ApiKey"] = "abc123def456ghi789",
            ["Api:Timeout"] = "8000",
            ["Api:EnableCompression"] = "false",
            
            // App configuration (root level)
            ["Name"] = "FlexKit Performance Test App",
            ["Version"] = "1.0.0",
            ["Environment"] = "Benchmark",
            ["Settings:Theme"] = "Dark",
            ["Settings:Language"] = "en-US",
            ["Settings:MaxConnections"] = "100"
        };

        SetupFlexKitContainer();
        SetupOptionsContainer();
    }

    private void SetupFlexKitContainer()
    {
        var containerBuilder = new ContainerBuilder();
        
        containerBuilder.AddFlexConfig(config => config
            .AddSource(new Microsoft.Extensions.Configuration.Memory.MemoryConfigurationSource 
            { 
                InitialData = _configData 
            }))
            .RegisterConfig<DatabaseConfig>("Database")
            .RegisterConfig<ApiConfig>("Api")
            .RegisterConfig<AppConfig>(); // Root binding

        _flexKitContainer = containerBuilder.Build();
    }

    private void SetupOptionsContainer()
    {
        var services = new ServiceCollection();
        
        var configBuilder = new ConfigurationBuilder();
        configBuilder.AddInMemoryCollection(_configData);
        var configuration = configBuilder.Build();
        
        services.AddSingleton<IConfiguration>(configuration);
        
        // Use the configuration binding extension methods
        services.AddOptions<DatabaseConfig>()
            .Bind(configuration.GetSection("Database"));
        services.AddOptions<ApiConfig>()
            .Bind(configuration.GetSection("Api"));
        services.AddOptions<AppConfig>()
            .Bind(configuration);
        
        _optionsContainer = services.BuildServiceProvider();
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _flexKitContainer.Dispose();
        (_optionsContainer as IDisposable)?.Dispose();
    }

    // === Configuration Resolution Benchmarks ===

    [Benchmark(Baseline = true)]
    public DatabaseConfig StandardIOptionsResolution()
    {
        var options = _optionsContainer.GetRequiredService<IOptions<DatabaseConfig>>();
        return options.Value;
    }

    [Benchmark]
    public DatabaseConfig FlexKitRegisterConfigResolution()
    {
        return _flexKitContainer.Resolve<DatabaseConfig>();
    }

    [Benchmark]
    public ApiConfig StandardIOptionsApiResolution()
    {
        var options = _optionsContainer.GetRequiredService<IOptions<ApiConfig>>();
        return options.Value;
    }

    [Benchmark]
    public ApiConfig FlexKitRegisterConfigApiResolution()
    {
        return _flexKitContainer.Resolve<ApiConfig>();
    }

    [Benchmark]
    public AppConfig StandardIOptionsRootResolution()
    {
        var options = _optionsContainer.GetRequiredService<IOptions<AppConfig>>();
        return options.Value;
    }

    [Benchmark]
    public AppConfig FlexKitRegisterConfigRootResolution()
    {
        return _flexKitContainer.Resolve<AppConfig>();
    }

    // === Multiple Configuration Access Patterns ===

    [Benchmark]
    public object StandardIOptionsMultipleAccess()
    {
        var dbOptions = _optionsContainer.GetRequiredService<IOptions<DatabaseConfig>>();
        var apiOptions = _optionsContainer.GetRequiredService<IOptions<ApiConfig>>();
        var appOptions = _optionsContainer.GetRequiredService<IOptions<AppConfig>>();
        
        return new
        {
            DatabaseConnection = dbOptions.Value.ConnectionString,
            DatabaseTimeout = dbOptions.Value.CommandTimeout,
            ApiBaseUrl = apiOptions.Value.BaseUrl,
            ApiTimeout = apiOptions.Value.Timeout,
            AppName = appOptions.Value.Name,
            AppVersion = appOptions.Value.Version
        };
    }

    [Benchmark]
    public object FlexKitRegisterConfigMultipleAccess()
    {
        var dbConfig = _flexKitContainer.Resolve<DatabaseConfig>();
        var apiConfig = _flexKitContainer.Resolve<ApiConfig>();
        var appConfig = _flexKitContainer.Resolve<AppConfig>();
        
        return new
        {
            DatabaseConnection = dbConfig.ConnectionString,
            DatabaseTimeout = dbConfig.CommandTimeout,
            ApiBaseUrl = apiConfig.BaseUrl,
            ApiTimeout = apiConfig.Timeout,
            AppName = appConfig.Name,
            AppVersion = appConfig.Version
        };
    }

    // === Property Access Benchmarks ===

    [Benchmark]
    public string StandardIOptionsPropertyAccess()
    {
        var dbOptions = _optionsContainer.GetRequiredService<IOptions<DatabaseConfig>>();
        return dbOptions.Value.ConnectionString;
    }

    [Benchmark]
    public string FlexKitRegisterConfigPropertyAccess()
    {
        var dbConfig = _flexKitContainer.Resolve<DatabaseConfig>();
        return dbConfig.ConnectionString;
    }

    [Benchmark]
    public object StandardIOptionsNestedPropertyAccess()
    {
        var appOptions = _optionsContainer.GetRequiredService<IOptions<AppConfig>>();
        var app = appOptions.Value;
        
        return new
        {
            DatabaseConnection = app.Database.ConnectionString,
            DatabaseTimeout = app.Database.CommandTimeout,
            DatabaseLogging = app.Database.EnableLogging,
            AppName = app.Name,
            SettingsCount = app.Settings.Count
        };
    }

    [Benchmark]
    public object FlexKitRegisterConfigNestedPropertyAccess()
    {
        var appConfig = _flexKitContainer.Resolve<AppConfig>();
        
        return new
        {
            DatabaseConnection = appConfig.Database.ConnectionString,
            DatabaseTimeout = appConfig.Database.CommandTimeout,
            DatabaseLogging = appConfig.Database.EnableLogging,
            AppName = appConfig.Name,
            SettingsCount = appConfig.Settings.Count
        };
    }

    // === Container Creation Benchmarks ===

    [Benchmark]
    public IServiceProvider CreateStandardIOptionsContainer()
    {
        var services = new ServiceCollection();
        
        var configBuilder = new ConfigurationBuilder();
        configBuilder.AddInMemoryCollection(_configData);
        var configuration = configBuilder.Build();
        
        services.AddSingleton<IConfiguration>(configuration);
        
        // Use the configuration binding extension methods
        services.AddOptions<DatabaseConfig>()
            .Bind(configuration.GetSection("Database"));
        services.AddOptions<ApiConfig>()
            .Bind(configuration.GetSection("Api"));
        services.AddOptions<AppConfig>()
            .Bind(configuration);
        
        return services.BuildServiceProvider();
    }

    [Benchmark]
    public IContainer CreateFlexKitRegisterConfigContainer()
    {
        var containerBuilder = new ContainerBuilder();
        
        containerBuilder.AddFlexConfig(config => config
            .AddSource(new Microsoft.Extensions.Configuration.Memory.MemoryConfigurationSource 
            { 
                InitialData = _configData 
            }))
            .RegisterConfig<DatabaseConfig>("Database")
            .RegisterConfig<ApiConfig>("Api")
            .RegisterConfig<AppConfig>();

        return containerBuilder.Build();
    }

    // === Repeated Access Performance (Simulating Production Usage) ===

    [Benchmark]
    public object StandardIOptionsRepeatedAccess()
    {
        var results = new List<string>();
        
        // Simulate accessing configuration multiple times (typical in services)
        for (int i = 0; i < 10; i++)
        {
            var dbOptions = _optionsContainer.GetRequiredService<IOptions<DatabaseConfig>>();
            results.Add(dbOptions.Value.ConnectionString);
        }
        
        return results;
    }

    [Benchmark]
    public object FlexKitRegisterConfigRepeatedAccess()
    {
        var results = new List<string>();
        
        // Simulate accessing configuration multiple times (typical in services)
        for (int i = 0; i < 10; i++)
        {
            var dbConfig = _flexKitContainer.Resolve<DatabaseConfig>();
            results.Add(dbConfig.ConnectionString);
        }
        
        return results;
    }

    // === Type Conversion During Binding Benchmarks ===

    [Benchmark]
    public object StandardIOptionsWithTypeConversion()
    {
        var dbOptions = _optionsContainer.GetRequiredService<IOptions<DatabaseConfig>>();
        var apiOptions = _optionsContainer.GetRequiredService<IOptions<ApiConfig>>();
        
        var db = dbOptions.Value;
        var api = apiOptions.Value;
        
        return new
        {
            db.CommandTimeout,  // int conversion
            db.MaxRetryCount,   // int conversion  
            db.EnableLogging,   // bool conversion
            ApiTimeout = api.Timeout,           // int conversion
            api.EnableCompression // bool conversion
        };
    }

    [Benchmark]
    public object FlexKitRegisterConfigWithTypeConversion()
    {
        var dbConfig = _flexKitContainer.Resolve<DatabaseConfig>();
        var apiConfig = _flexKitContainer.Resolve<ApiConfig>();
        
        return new
        {
            dbConfig.CommandTimeout,  // int conversion
            dbConfig.MaxRetryCount,   // int conversion  
            dbConfig.EnableLogging,   // bool conversion
            ApiTimeout = apiConfig.Timeout,           // int conversion
            apiConfig.EnableCompression // bool conversion
        };
    }
}