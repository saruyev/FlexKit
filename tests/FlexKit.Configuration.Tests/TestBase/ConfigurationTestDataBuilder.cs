using System;
using System.Collections.Generic;
using Bogus;

namespace FlexKit.Configuration.Tests.TestBase;

/// <summary>
/// Bogus-based builder for creating realistic configuration test data.
/// </summary>
public static class ConfigurationTestDataBuilder
{
    /// <summary>
    /// Creates fake database configuration settings.
    /// </summary>
    public static Faker<DatabaseConfig> DatabaseConfig { get; } = 
        new Faker<DatabaseConfig>()
            .RuleFor(d => d.ConnectionString, f => 
                $"Server={f.Internet.DomainName()};Database={f.Database.Engine()};User Id={f.Internet.UserName()};Password={f.Internet.Password()};")
            .RuleFor(d => d.CommandTimeout, f => f.Random.Int(30, 300))
            .RuleFor(d => d.MaxRetryCount, f => f.Random.Int(1, 5))
            .RuleFor(d => d.EnableLogging, f => f.Random.Bool())
            .RuleFor(d => d.PoolSize, f => f.Random.Int(5, 50));

    /// <summary>
    /// Creates fake API configuration settings.
    /// </summary>
    public static Faker<ApiConfig> ApiConfig { get; } = 
        new Faker<ApiConfig>()
            .RuleFor(a => a.BaseUrl, f => f.Internet.Url())
            .RuleFor(a => a.ApiKey, f => f.Random.AlphaNumeric(32))
            .RuleFor(a => a.Timeout, f => f.Random.Int(5000, 60000))
            .RuleFor(a => a.MaxRetries, f => f.Random.Int(1, 3))
            .RuleFor(a => a.EnableCompression, f => f.Random.Bool())
            .RuleFor(a => a.UserAgent, f => f.Internet.UserAgent());

    /// <summary>
    /// Creates fake user configuration data.
    /// </summary>
    public static Faker<UserConfig> UserConfig { get; } = 
        new Faker<UserConfig>()
            .RuleFor(u => u.Id, f => f.Random.Guid())
            .RuleFor(u => u.Email, f => f.Internet.Email())
            .RuleFor(u => u.FullName, f => f.Name.FullName())
            .RuleFor(u => u.Role, f => f.PickRandom("Admin", "User", "Guest", "Manager"))
            .RuleFor(u => u.IsActive, f => f.Random.Bool(0.8f)) // 80% chance of being active
            .RuleFor(u => u.CreatedAt, f => f.Date.Past(2))
            .RuleFor(u => u.LastLoginAt, f => f.Date.Recent(30));

    /// <summary>
    /// Creates configuration dictionary with realistic key-value pairs.
    /// </summary>
    public static Dictionary<string, string> CreateConfigurationDictionary()
    {
        var faker = new Faker();
        
        return new Dictionary<string, string>
        {
            // Database settings
            ["ConnectionStrings:DefaultConnection"] = DatabaseConfig.Generate().ConnectionString,
            ["Database:CommandTimeout"] = faker.Random.Int(30, 300).ToString(),
            ["Database:MaxRetryCount"] = faker.Random.Int(1, 5).ToString(),
            
            // API settings
            ["External:PaymentApi:BaseUrl"] = faker.Internet.Url(),
            ["External:PaymentApi:ApiKey"] = faker.Random.AlphaNumeric(32),
            ["External:PaymentApi:Timeout"] = faker.Random.Int(5000, 30000).ToString(),
            
            // Application settings
            ["Application:Name"] = faker.Company.CompanyName(),
            ["Application:Version"] = faker.System.Version().ToString(),
            ["Application:Environment"] = faker.PickRandom("Development", "Staging", "Production"),
            
            // Feature flags
            ["Features:EnableAdvancedSearch"] = faker.Random.Bool().ToString(),
            ["Features:EnableMetrics"] = faker.Random.Bool().ToString(),
            ["Features:MaxUploadSize"] = faker.Random.Int(1024, 10240).ToString(),
            
            // Logging
            ["Logging:LogLevel:Default"] = faker.PickRandom("Debug", "Information", "Warning", "Error"),
            ["Logging:LogLevel:Microsoft"] = faker.PickRandom("Information", "Warning", "Error"),
            
            // Cache settings
            ["Cache:DefaultTtl"] = faker.Random.Int(300, 3600).ToString(),
            ["Cache:MaxSize"] = faker.Random.Int(100, 1000).ToString()
        };
    }
}

/// <summary>
/// Test data classes for configuration testing.
/// </summary>
public class DatabaseConfig
{
    public string ConnectionString { get; set; } = string.Empty;
    public int CommandTimeout { get; set; }
    public int MaxRetryCount { get; set; }
    public bool EnableLogging { get; set; }
    public int PoolSize { get; set; }
}

public class ApiConfig
{
    public string BaseUrl { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public int Timeout { get; set; }
    public int MaxRetries { get; set; }
    public bool EnableCompression { get; set; }
    public string UserAgent { get; set; } = string.Empty;
}

public class UserConfig
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }
}
