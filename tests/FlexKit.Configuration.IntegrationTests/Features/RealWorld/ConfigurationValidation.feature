Feature: Configuration Validation
    As a developer using FlexKit Configuration
    I want to validate configuration values and handle validation errors
    So that I can ensure my application has valid configuration before startup

    Background:
        Given I have validation setup with a configuration validation environment

    @ConfigurationValidation @RequiredValues
    Scenario: Validate required configuration values are present
        When I validation setup configuration with required values:
            | Key                           | Value                     | ValidationRule           |
            | Database:ConnectionString     | Server=localhost;Database=test | Required                |
            | External:Api:BaseUrl          | https://api.example.com   | Required                |
            | Security:Jwt:SecretKey        | super-secret-key-12345    | Required                |
        And I validation setup validation rules for required values:
            | Key                           | Rule                      |
            | Database:ConnectionString     | Must be present           |
            | External:Api:BaseUrl          | Must be present           |
            | Security:Jwt:SecretKey        | Must be present           |
        And I validation trigger configuration validation process
        Then the configuration validation should succeed
        And all required values should be validated successfully
        And FlexConfig should contain all required validated values

    @ConfigurationValidation @MissingRequiredValues
    Scenario: Handle missing required configuration values
        When I validation setup configuration with missing required values:
            | Key                           | Value                     | ValidationRule           |
            | Database:ConnectionString     |                           | Required                |
            | External:Api:BaseUrl          | https://api.example.com   | Required                |
            | Security:Jwt:SecretKey        |                           | Required                |
        And I validation setup validation rules for required values:
            | Key                           | Rule                      |
            | Database:ConnectionString     | Must be present           |
            | External:Api:BaseUrl          | Must be present           |
            | Security:Jwt:SecretKey        | Must be present           |
        And I validation trigger configuration validation process
        Then the configuration validation should fail
        And validation errors should indicate missing required values
        And the error should specify which configuration keys are missing

    @ConfigurationValidation @TypeValidation
    Scenario: Validate configuration value types and formats
        When I validation setup configuration with typed values:
            | Key                           | Value                     | ExpectedType             |
            | Database:CommandTimeout       | 30                        | Integer                  |
            | Database:MaxRetryCount        | 3                         | Integer                  |
            | External:Api:Timeout          | 5000                      | Integer                  |
            | Features:EnableCaching        | true                      | Boolean                  |
            | Security:Jwt:ExpirationMinutes| 15                        | Integer                  |
            | Cache:DefaultExpiration       | 00:05:00                  | TimeSpan                 |
        And I validation setup type validation rules:
            | Key                           | ValidationRule            |
            | Database:CommandTimeout       | Must be positive integer  |
            | Database:MaxRetryCount        | Must be positive integer  |
            | External:Api:Timeout          | Must be positive integer  |
            | Features:EnableCaching        | Must be boolean           |
            | Security:Jwt:ExpirationMinutes| Must be positive integer  |
            | Cache:DefaultExpiration       | Must be valid timespan    |
        And I validation trigger type validation process
        Then the type validation should succeed
        And all values should be convertible to their expected types
        And FlexConfig should provide properly typed access to values

    @ConfigurationValidation @InvalidTypeValues
    Scenario: Handle invalid type conversions with proper errors
        When I validation setup configuration with invalid typed values:
            | Key                           | Value                     | ExpectedType             |
            | Database:CommandTimeout       | not-a-number              | Integer                  |
            | Database:MaxRetryCount        | -5                        | Integer                  |
            | External:Api:Timeout          | forever                   | Integer                  |
            | Features:EnableCaching        | maybe                     | Boolean                  |
            | Security:Jwt:ExpirationMinutes| zero                      | Integer                  |
        And I validation setup type validation rules:
            | Key                           | ValidationRule            |
            | Database:CommandTimeout       | Must be positive integer  |
            | Database:MaxRetryCount        | Must be positive integer  |
            | External:Api:Timeout          | Must be positive integer  |
            | Features:EnableCaching        | Must be boolean           |
            | Security:Jwt:ExpirationMinutes| Must be positive integer  |
        And I validation trigger type validation process
        Then the type validation should fail
        And validation errors should indicate type conversion failures
        And the error should specify which values cannot be converted

    @ConfigurationValidation @RangeValidation
    Scenario: Validate configuration values within acceptable ranges
        When I validation setup configuration with range values:
            | Key                           | Value                     | MinValue    | MaxValue    |
            | Database:CommandTimeout       | 30                        | 1           | 300         |
            | Database:PoolSize             | 10                        | 1           | 100         |
            | External:Api:RetryCount       | 3                         | 0           | 10          |
            | Security:SessionTimeoutMinutes| 20                        | 5           | 60          |
            | Cache:MaxSizeKB               | 1024                      | 100         | 10240       |
        And I validation setup range validation rules:
            | Key                           | ValidationRule            |
            | Database:CommandTimeout       | Between 1 and 300         |
            | Database:PoolSize             | Between 1 and 100         |
            | External:Api:RetryCount       | Between 0 and 10          |
            | Security:SessionTimeoutMinutes| Between 5 and 60          |
            | Cache:MaxSizeKB               | Between 100 and 10240     |
        And I validation trigger range validation process
        Then the range validation should succeed
        And all values should be within their specified ranges
        And FlexConfig should contain validated range values

    @ConfigurationValidation @OutOfRangeValues
    Scenario: Handle configuration values outside acceptable ranges
        When I validation setup configuration with out-of-range values:
            | Key                           | Value                     | MinValue    | MaxValue    |
            | Database:CommandTimeout       | 500                       | 1           | 300         |
            | Database:PoolSize             | 150                       | 1           | 100         |
            | External:Api:RetryCount       | -1                        | 0           | 10          |
            | Security:SessionTimeoutMinutes| 0                         | 5           | 60          |
            | Cache:MaxSizeKB               | 50                        | 100         | 10240       |
        And I validation setup range validation rules:
            | Key                           | ValidationRule            |
            | Database:CommandTimeout       | Between 1 and 300         |
            | Database:PoolSize             | Between 1 and 100         |
            | External:Api:RetryCount       | Between 0 and 10          |
            | Security:SessionTimeoutMinutes| Between 5 and 60          |
            | Cache:MaxSizeKB               | Between 100 and 10240     |
        And I validation trigger range validation process
        Then the range validation should fail
        And validation errors should indicate out-of-range values
        And the error should specify which values are outside acceptable ranges

    @ConfigurationValidation @ConfigurationFiles
    Scenario: Validate configuration loaded from JSON files with validation rules
        When I validation setup configuration from JSON file "TestData/ConfigurationFiles/appsettings.json"
        And I validation setup additional validation configuration:
            | Key                           | ValidationRule            |
            | Application:Name              | Required                  |
            | Application:Version           | Required                  |
            | Database:Provider             | Must be SqlServer or InMemory |
            | Logging:LogLevel:Default      | Must be valid log level   |
        And I validation trigger file-based configuration validation
        Then the file-based validation should succeed
        And JSON configuration values should pass validation rules
        And FlexConfig should provide validated file-based configuration

    @ConfigurationValidation @InvalidConfigurationFile
    Scenario: Handle validation failures with invalid configuration files
        When I validation setup configuration from invalid JSON file "TestData/ConfigurationFiles/invalid.json"
        And I validation setup strict validation rules:
            | Key                           | ValidationRule            |
            | Database:CommandTimeout       | Must be positive integer  |
            | External:Api:BaseUrl          | Must be valid URL         |
            | Features:EnableCaching        | Must be boolean           |
        And I validation trigger file-based configuration validation
        Then the file-based validation should fail gracefully
        And validation errors should indicate configuration file issues
        And the system should handle invalid configuration appropriately

    @ConfigurationValidation @EnvironmentVariableValidation
    Scenario: Validate configuration values from environment variables
        When I validation setup environment variables for validation:
            | Name                          | Value                     |
            | VALIDATION_DATABASE_HOST      | localhost                 |
            | VALIDATION_DATABASE_PORT      | 5432                      |
            | VALIDATION_API_KEY            | test-key-12345            |
            | VALIDATION_ENABLE_FEATURES    | true                      |
            | VALIDATION_MAX_CONNECTIONS    | 50                        |
        And I validation setup environment validation rules:
            | Key                           | ValidationRule            |
            | DATABASE_HOST                 | Required                  |
            | DATABASE_PORT                 | Must be valid port number |
            | API_KEY                       | Required                  |
            | ENABLE_FEATURES               | Must be boolean           |
            | MAX_CONNECTIONS               | Must be positive integer  |
        And I validation trigger environment variable validation
        Then the environment validation should succeed
        And environment values should pass validation rules
        And FlexConfig should contain validated environment values

    @ConfigurationValidation @MultiSourceValidation
    Scenario: Validate configuration from multiple sources with comprehensive rules
        When I validation setup multi-source configuration with:
            | Source          | Path                                          |
            | JSON            | TestData/ConfigurationFiles/appsettings.json |
            | Environment     | test.env                                      |
            | InMemory        | additional-config                             |
        And I validation setup comprehensive validation rules:
            | Key                           | ValidationRule                    |
            | Application:Name              | Required                          |
            | Application:Version           | Required                          |
            | Logging:LogLevel:Default      | Must be valid log level           |
        And I validation trigger multi-source validation process
        Then the multi-source validation should succeed
        And all sources should contribute to validated configuration
        And FlexConfig should provide comprehensive validated configuration
        And configuration precedence should be maintained during validation