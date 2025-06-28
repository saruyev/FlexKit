Feature: Complex Configuration Scenarios
    As a developer using FlexKit Configuration
    I want to test complex real-world configuration scenarios
    So that I can ensure the system handles intricate setups and edge cases properly

    Background:
        Given I have organized a comprehensive configuration testing environment

    @ComplexConfigurationScenarios @HierarchicalOverrides
    Scenario: Multi-layered configuration with hierarchical overrides
        When I organize base configuration from JSON file "TestData/ConfigurationFiles/appsettings.json"
        And I organize environment-specific overrides from JSON file "TestData/ConfigurationFiles/appsettings.Development.json"
        And I organize environment variables with specific precedence:
            | Variable                          | Value                           |
            | DATABASE__COMMANDTIMEOUT         | 120                             |
            | FEATURES__ENABLECACHING          | false                           |
            | EXTERNAL__PAYMENTAPI__TIMEOUT    | 15000                           |
        And I organize .env file overrides from "TestData/ConfigurationFiles/test.env"
        And I organize in-memory final overrides:
            | Key                               | Value                           |
            | Application:Version               | 2.0.0-complex                  |
            | Security:Encryption:KeySize       | 512                             |
            | Performance:CacheSize             | 1000000                         |
        And I execute complex configuration assembly
        Then the complex configuration should load successfully
        And hierarchical precedence should be maintained across all layers
        And the complex configuration should contain "Application:Version" with value "2.0.0-complex"
        And the complex configuration should contain "Database:CommandTimeout" with value "120"
        And the complex configuration should contain "Features:EnableCaching" with value "false"
        And the complex configuration should contain "External:PaymentApi:Timeout" with value "15000"

    @ComplexConfigurationScenarios @TypedConfigurationBinding
    Scenario: Complex typed configuration binding and validation
        When I organize comprehensive test configuration structure:
            | Key                                    | Value                                  |
            | Database:ConnectionString              | Server=complex.db.com;Database=TestDB  |
            | Database:CommandTimeout                | 60                                     |
            | Database:MaxRetryCount                 | 5                                      |
            | External:PaymentApi:BaseUrl            | https://complex.payment.api.com        |
            | External:PaymentApi:ApiKey             | complex-api-key-12345                  |
            | External:PaymentApi:Timeout            | 30000                                  |
            | External:NotificationApi:BaseUrl       | https://complex.notifications.com      |
            | External:NotificationApi:ApiKey        | complex-notification-key-67890         |
            | Features:EnableAdvancedLogging         | true                                   |
            | Features:MaxConcurrentConnections      | 250                                    |
            | Features:CacheExpirationMinutes        | 45                                     |
            | Security:Jwt:Issuer                    | complex.test.issuer                    |
            | Security:Jwt:ExpirationHours           | 24                                     |
            | Security:Encryption:Algorithm          | AES256                                 |
        And I execute complex configuration assembly
        And I organize typed configuration binding for complex objects
        Then the complex configuration should load successfully
        And database configuration should be properly typed and accessible
        And external API configurations should be properly structured
        And feature flag configurations should be properly converted
        And security configurations should maintain proper data types
        And all configuration sections should support dynamic access

    @ComplexConfigurationScenarios @ConditionalConfiguration
    Scenario: Environment-conditional configuration loading
        When I organize conditional configuration based on environment "Development"
        And I organize base configuration from JSON file "TestData/ConfigurationFiles/appsettings.json"
        And I organize conditional overrides when environment is "Development":
            | Key                                    | Value                                  |
            | Logging:LogLevel:Default               | Debug                                  |
            | Features:EnableDetailedErrors          | true                                   |
            | Database:EnableQueryLogging            | true                                   |
            | External:PaymentApi:BaseUrl            | https://dev.payment.api.com            |
            | Performance:CacheSize                  | 10000                                  |
        And I organize conditional overrides when environment is "Production":
            | Key                                    | Value                                  |
            | Logging:LogLevel:Default               | Information                            |
            | Features:EnableDetailedErrors          | false                                  |
            | Database:EnableQueryLogging            | false                                  |
            | External:PaymentApi:BaseUrl            | https://prod.payment.api.com           |
            | Performance:CacheSize                  | 1000000                                |
        And I execute complex configuration assembly
        Then the complex configuration should load successfully
        And environment-specific settings should be active
        And the complex configuration should contain "Logging:LogLevel:Default" with value "Debug"
        And the complex configuration should contain "Features:EnableDetailedErrors" with value "true"
        And the complex configuration should contain "External:PaymentApi:BaseUrl" with value "https://dev.payment.api.com"

    @ComplexConfigurationScenarios @DynamicConfigurationReconfiguration
    Scenario: Dynamic configuration reconfiguration and hot reloading simulation
        When I organize initial configuration from JSON file "TestData/ConfigurationFiles/appsettings.json"
        And I execute complex configuration assembly
        Then the complex configuration should load successfully
        When I organize dynamic configuration updates:
            | Key                                    | NewValue                               |
            | Features:EnableAdvancedSearch          | true                                   |
            | Database:PoolSize                      | 50                                     |
            | External:PaymentApi:MaxRetries         | 5                                      |
            | Cache:ExpirationPolicy                 | Sliding                                |
        And I execute configuration hot reload simulation
        Then the complex configuration should incorporate dynamic updates
        And the complex configuration should contain "Features:EnableAdvancedSearch" with value "true"
        And the complex configuration should contain "Database:PoolSize" with value "50"
        And existing configuration values should remain unchanged
        And FlexConfig should reflect the updated configuration

    @ComplexConfigurationScenarios @ConfigurationValidationAndErrors
    Scenario: Complex configuration validation and error handling
        When I organize configuration with intentional validation scenarios:
            | Key                                    | Value                                  | ValidationRule                         |
            | Database:CommandTimeout                | 30                                     | Between 10 and 300                    |
            | Features:MaxConcurrentConnections      | 100                                    | Greater than 0                        |
            | External:PaymentApi:Timeout            | 5000                                   | Between 1000 and 60000                |
            | Security:Jwt:ExpirationHours           | 2                                      | Between 1 and 48                      |
            | Performance:CacheSize                  | 100000                                 | Positive number                        |
        And I organize configuration with validation edge cases:
            | Key                                    | Value                                  | ExpectedOutcome                        |
            | Database:InvalidTimeout                | -1                                     | Should handle gracefully               |
            | Features:InvalidConnectionCount        | not-a-number                           | Should provide clear error             |
            | External:EmptyApiKey                   |                                        | Should use fallback                    |
            | Security:InvalidEncryption             | UNKNOWN_ALGORITHM                      | Should default appropriately           |
        And I execute complex configuration assembly with validation
        Then the complex configuration should load successfully
        And valid configuration values should be properly stored
        And invalid configuration values should be handled gracefully
        And appropriate validation errors should be captured
        And fallback values should be applied where configured

    @ComplexConfigurationScenarios @PerformanceAndMemoryOptimization
    Scenario: Configuration performance with large datasets
        When I organize performance test configuration with large dataset:
            | ConfigurationType                      | Count                                  |
            | DatabaseConnections                    | 50                                     |
            | ExternalApiEndpoints                   | 25                                     |
            | FeatureFlags                          | 100                                    |
            | SecurityPolicies                      | 30                                     |
            | CacheConfigurations                   | 40                                     |
        And I organize batch configuration data from multiple sources
        And I execute complex configuration assembly with performance monitoring
        Then the complex configuration should load successfully
        And configuration loading should complete within acceptable time limits
        And memory usage should remain within reasonable bounds
        And FlexConfig performance should meet established benchmarks
        And all configuration sections should be accessible efficiently

    @ComplexConfigurationScenarios @CrossPlatformConfiguration
    Scenario: Cross-platform configuration path resolution
        When I organize configuration with platform-specific paths:
            | Key                                    | WindowsValue                           | LinuxValue                             |
            | Logging:FilePath                       | C:\logs\app.log                        | /var/log/app.log                       |
            | Storage:TempDirectory                  | C:\temp\app                            | /tmp/app                               |
            | Database:BackupPath                    | D:\backups\database                    | /backup/database                       |
            | Security:CertificatePath               | C:\certs\app.pfx                       | /etc/ssl/certs/app.crt                 |
        And I organize environment-specific path configurations
        And I execute complex configuration assembly with cross-platform support
        Then the complex configuration should load successfully
        And platform-appropriate paths should be resolved correctly
        And configuration should work consistently across different operating systems
        And file path separators should be normalized properly

    @ComplexConfigurationScenarios @ConfigurationEncryptionAndSecurity
    Scenario: Configuration with encrypted values and security considerations
        When I organize configuration with security-sensitive data:
            | Key                                    | Value                                  | SecurityLevel                          |
            | Database:ConnectionString              | Server=secure.db.com;Password=secret   | Encrypted                              |
            | External:PaymentApi:ApiKey             | secure-payment-key-12345               | Encrypted                              |
            | Security:Jwt:SecretKey                 | super-secret-jwt-key-67890             | Encrypted                              |
            | External:NotificationApi:ApiKey        | notification-secret-key-abcde          | Encrypted                              |
            | Features:AdminPassword                 | admin-secure-password-xyz              | Encrypted                              |
        And I organize security configuration metadata:
            | SecuritySetting                        | Value                                  |
            | EncryptionEnabled                      | true                                   |
            | KeyRotationInterval                    | 30                                     |
            | AuditLoggingEnabled                    | true                                   |
            | SecureStorageProvider                  | InMemory                               |
        And I execute complex configuration assembly with security features
        Then the complex configuration should load successfully
        And sensitive configuration values should be properly protected
        And security metadata should be accessible through FlexConfig
        And configuration access should maintain security boundaries
        And audit trails should be generated for sensitive configuration access
        