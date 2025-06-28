Feature: Configuration Builder
    As a developer using FlexKit Configuration
    I want to build configurations from multiple sources
    So that I can combine different configuration providers effectively

    Background:
        Given I have initialized a test configuration builder

    @ConfigurationBuilder @BasicBuilder
    Scenario: Create configuration with in-memory data
        When I append in-memory configuration data:
            | Key                     | Value                           |
            | Application:Name        | FlexKit Builder Test            |
            | Application:Version     | 1.0.0                          |
            | Database:ConnectionString | Server=localhost;Database=Test; |
            | Database:Timeout        | 45                             |
        And I construct the configuration
        Then the built configuration should contain "Application:Name" with value "FlexKit Builder Test"
        And the built configuration should contain "Database:Timeout" with value "45"

    @ConfigurationBuilder @SectionBuilder
    Scenario: Create configuration using sections
        When I append configuration section "Application" with data:
            | Key         | Value                    |
            | Name        | FlexKit Section Test     |
            | Version     | 2.0.0                    |
            | Environment | Development              |
        And I append configuration section "Features" with data:
            | Key            | Value  |
            | EnableCaching  | True   |
            | EnableMetrics  | False  |
            | MaxConcurrency | 100    |
        And I construct the configuration
        Then the built configuration should contain "Application:Name" with value "FlexKit Section Test"
        And the built configuration should contain "Features:EnableCaching" with value "True"
        And the built configuration should contain "Features:MaxConcurrency" with value "100"

    @ConfigurationBuilder @MultipleSourcesBuilder
    Scenario: Create configuration with multiple sources
        When I append in-memory configuration data:
            | Key                     | Value                         |
            | Application:Name        | Base App                      |
            | Application:Version     | 1.0.0                        |
            | Database:Provider       | SqlServer                     |
        And I append configuration section "Override" with data:
            | Key         | Value                    |
            | Name        | Overridden App Name      |
            | NewSetting  | Additional Value         |
        And I append key-value pair "Features:NewFeature" with value "enabled"
        And I construct the configuration
        Then the built configuration should contain "Application:Name" with value "Base App"
        And the built configuration should contain "Application:Version" with value "1.0.0"
        And the built configuration should contain "Override:Name" with value "Overridden App Name"
        And the built configuration should contain "Features:NewFeature" with value "enabled"

    @ConfigurationBuilder @FlexConfigBuilder
    Scenario: Create FlexConfiguration from builder
        When I append database configuration with defaults
        And I append API configuration with defaults
        And I append logging configuration with defaults
        And I construct the FlexConfiguration
        Then the built FlexConfiguration should not be null
        And the built FlexConfiguration should contain "Database:ConnectionString" with value "Data Source=:memory:"
        And the built FlexConfiguration should contain "External:Api:BaseUrl" with value "https://test.api.com"
        And the built FlexConfiguration should contain "Logging:LogLevel:Default" with value "Debug"

    @ConfigurationBuilder @AdvancedBuilder
    Scenario: Create configuration with feature flags
        When I append feature flags:
            | Feature             | Enabled |
            | EnableCaching       | true    |
            | EnableMetrics       | false   |
            | EnableAdvancedAuth  | true    |
            | EnableBetaFeatures  | false   |
        And I construct the configuration
        Then the built configuration should contain "Features:EnableCaching" with value "True"
        And the built configuration should contain "Features:EnableMetrics" with value "False"
        And the built configuration should contain "Features:EnableAdvancedAuth" with value "True"
        And the built configuration should contain "Features:EnableBetaFeatures" with value "False"

    @ConfigurationBuilder @EnvironmentBuilder
    Scenario: Create configuration with environment variables
        When I configure test environment variable "TEST_APP_NAME" to "Environment Test App"
        And I configure test environment variable "TEST_DATABASE_HOST" to "env.database.com"
        And I append environment variables with prefix "TEST_"
        And I construct the configuration
        Then the built configuration should contain "APP_NAME" with value "Environment Test App"
        And the built configuration should contain "DATABASE_HOST" with value "env.database.com"

    @ConfigurationBuilder @JsonFileBuilder
    Scenario: Create configuration with JSON file from TestData folder
        When I append existing JSON file "TestData/ConfigurationFiles/appsettings.json" as configuration source
        And I construct the configuration
        Then the built configuration should build successfully
        And the built configuration should contain values from the JSON file

    @ConfigurationBuilder @ErrorHandling
    Scenario: Handle missing optional configuration sources gracefully
        When I append in-memory configuration data:
            | Key                | Value           |
            | Application:Name   | Resilient App   |
        And I append a non-existent JSON file as optional source
        And I construct the configuration
        Then the configuration should build successfully
        And the built configuration should contain "Application:Name" with value "Resilient App"

    @ConfigurationBuilder @DefaultConfigurations
    Scenario: Create configuration with common defaults
        When I append database configuration with connection string "Server=custom.db.com" and timeout 60
        And I append API configuration with base URL "https://custom.api.com" and API key "custom-key-123"
        And I append logging configuration with default level "Information"
        And I construct the configuration
        Then the built configuration should contain "Database:ConnectionString" with value "Server=custom.db.com"
        And the built configuration should contain "Database:CommandTimeout" with value "60"
        And the built configuration should contain "External:Api:BaseUrl" with value "https://custom.api.com"
        And the built configuration should contain "External:Api:ApiKey" with value "custom-key-123"
        And the built configuration should contain "Logging:LogLevel:Default" with value "Information"

    @ConfigurationBuilder @ChainedBuilder
    Scenario: Create configuration with method chaining
        When I chain configuration builder with:
            | ConfigType    | Parameters                                                |
            | Database      | connectionString=Server=chain.db.com,timeout=120         |
            | Api           | baseUrl=https://chain.api.com,apiKey=chain-key           |
            | FeatureFlags  | EnableCaching=true,EnableMetrics=true                     |
            | Logging       | defaultLevel=Warning,microsoftLevel=Error                |
        And I construct the configuration
        Then the built configuration should contain "Database:ConnectionString" with value "Server=chain.db.com"
        And the built configuration should contain "Database:CommandTimeout" with value "120"
        And the built configuration should contain "External:Api:BaseUrl" with value "https://chain.api.com"
        And the built configuration should contain "Features:EnableCaching" with value "True"
        And the built configuration should contain "Logging:LogLevel:Default" with value "Warning"
        And the built configuration should contain "Logging:LogLevel:Microsoft" with value "Error"

    @ConfigurationBuilder @ValidationBuilder
    Scenario: Validate built configuration contains expected sections
        When I append database configuration with defaults
        And I append API configuration with defaults
        And I append feature flags:
            | Feature         | Enabled |
            | EnableCaching   | true    |
            | EnableMetrics   | false   |
        And I append complete logging configuration with defaults
        And I construct the configuration
        Then the built configuration should have section "Database"
        And the built configuration should have section "External"
        And the built configuration should have section "Features"
        And the built configuration should have section "Logging"
        But the built configuration should not have section "NonExistentSection"
        