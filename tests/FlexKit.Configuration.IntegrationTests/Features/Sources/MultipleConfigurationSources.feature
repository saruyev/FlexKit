Feature: Multiple Configuration Sources
    As a developer using FlexKit Configuration
    I want to combine multiple configuration sources
    So that I can create layered configuration with proper precedence and fallbacks

    Background:
        Given I have established a multi-source configuration environment

    @MultipleConfigurationSources @BasicCombination
    Scenario: Combine JSON and environment variable sources
        When I incorporate JSON file "TestData/ConfigurationFiles/appsettings.json" as primary layer
        And I incorporate environment variables with prefix "FLEXKIT_" as secondary layer
        And I configure environment variable "FLEXKIT_DATABASE__HOST" to "env-override.com"
        And I configure environment variable "FLEXKIT_EXTERNAL__APIKEY" to "env-api-key-123"
        And I construct the multi-layered configuration
        Then the multi-source configuration should load without errors
        And the final configuration should contain "Database:Host" with value "env-override.com"
        And the final configuration should contain "External:ApiKey" with value "env-api-key-123"
        And the final configuration should contain values from the base JSON file

    @MultipleConfigurationSources @PrecedenceOrder
    Scenario: Verify configuration source precedence
        When I incorporate JSON file "TestData/ConfigurationFiles/appsettings.json" as foundation layer
        And I incorporate .env file "TestData/ConfigurationFiles/test.env" as intermediate layer
        And I incorporate in-memory configuration as top layer with:
            | Key                    | Value                  |
            | Database:Host          | inmemory-host.com      |
            | Application:Name       | InMemory App Override  |
        And I construct the multi-layered configuration
        Then the multi-source configuration should load without errors
        And the final configuration should contain "Database:Host" with value "inmemory-host.com"
        And the final configuration should contain "Application:Name" with value "InMemory App Override"
        And higher layers should override lower layers

    @MultipleConfigurationSources @EnvironmentSpecific
    Scenario: Environment-specific configuration layering
        When I incorporate JSON file "TestData/ConfigurationFiles/appsettings.json" as base layer
        And I incorporate JSON file "TestData/ConfigurationFiles/appsettings.Development.json" as environment layer
        And I incorporate environment variables without prefix as override layer
        And I configure environment variable "DATABASE__COMMANDTIMEOUT" to "120"
        And I configure environment variable "FEATURES__ENABLECACHING" to "false"
        And I construct the multi-layered configuration
        Then the multi-source configuration should load without errors
        And the final configuration should contain "Database:CommandTimeout" with value "120"
        And the final configuration should contain "Features:EnableCaching" with value "false"
        And environment variables should have highest precedence

    @MultipleConfigurationSources @OptionalSources
    Scenario: Handle combination with optional sources
        When I incorporate JSON file "TestData/ConfigurationFiles/appsettings.json" as required layer
        And I incorporate JSON file "TestData/ConfigurationFiles/appsettings.Missing.json" as optional layer
        And I incorporate .env file "TestData/ConfigurationFiles/test.env" as required layer
        And I incorporate .env file "TestData/ConfigurationFiles/missing.env" as optional layer
        And I construct the multi-layered configuration
        Then the multi-source configuration should load without errors
        And required layers should contribute to final configuration
        And missing optional layers should be skipped gracefully
        And the final configuration should contain data from all available layers

    @MultipleConfigurationSources @ErrorHandling
    Scenario: Handle errors in multi-source configuration
        When I incorporate JSON file "TestData/ConfigurationFiles/appsettings.json" as required layer
        And I incorporate JSON file "TestData/ConfigurationFiles/invalid.json" as required layer
        And I incorporate .env file "TestData/ConfigurationFiles/test.env" as required layer
        And I attempt to construct the multi-layered configuration
        Then the multi-source configuration construction should fail
        And the error should indicate which layer caused the failure
        And valid layers should be processed before the error occurs

    @MultipleConfigurationSources @FlexConfigIntegration
    Scenario: Create FlexConfig from multiple sources
        When I incorporate JSON file "TestData/ConfigurationFiles/appsettings.json" as configuration layer
        And I incorporate .env file "TestData/ConfigurationFiles/test.env" as configuration layer
        And I incorporate in-memory configuration with test data:
            | Key                           | Value                    |
            | External:PaymentGateway:ApiKey| multi-source-key-456     |
            | Security:Jwt:Issuer           | multi-source.test.com    |
            | Features:Notifications:Enabled| true                     |
        And I construct the multi-layered configuration
        And I generate FlexConfig from multi-source configuration
        Then the multi-source FlexConfig should be created successfully
        And FlexConfig should enable dynamic access to all layer data
        And FlexConfig should maintain the correct precedence order

    @MultipleConfigurationSources @DynamicSourceAddition
    Scenario: Add sources dynamically and rebuild configuration
        When I incorporate JSON file "TestData/ConfigurationFiles/appsettings.json" as initial layer
        And I construct the current multi-layered configuration
        Then the multi-source configuration should contain base JSON data
        When I supplement with .env file "TestData/ConfigurationFiles/test.env" as additional layer
        And I supplement with environment variable "DYNAMIC_SETTING" with value "dynamically-added"
        And I reconstruct the multi-layered configuration
        Then the final configuration should include data from all layers
        And the final configuration should contain "DYNAMIC_SETTING" with value "dynamically-added"
        And the final configuration should contain data from both JSON and .env layers

    @MultipleConfigurationSources @SourceValidation
    Scenario: Validate individual source contributions
        When I incorporate JSON file "TestData/ConfigurationFiles/appsettings.json" as "json-layer"
        And I incorporate .env file "TestData/ConfigurationFiles/test.env" as "env-layer"
        And I incorporate in-memory data as "memory-layer" with:
            | Key                    | Value                |
            | Memory:OnlyKey         | memory-only-value    |
            | Database:Host          | memory-host.com      |
        And I construct the multi-layered configuration
        Then the multi-source configuration should load without errors
        And layer "json-layer" should contribute JSON-specific keys
        And layer "env-layer" should contribute environment-specific keys
        And layer "memory-layer" should contribute memory-specific keys
        And conflicting keys should follow precedence rules

    @MultipleConfigurationSources @PerformanceValidation
    Scenario: Verify performance with multiple sources
        When I incorporate JSON file "TestData/ConfigurationFiles/appsettings.json" as configuration layer
        And I incorporate JSON file "TestData/ConfigurationFiles/appsettings.Development.json" as configuration layer
        And I incorporate .env file "TestData/ConfigurationFiles/test.env" as configuration layer
        And I incorporate 50 in-memory key-value pairs as bulk layer
        And I construct the multi-layered configuration
        Then the multi-source configuration should load without errors
        And configuration construction should complete within reasonable time
        And all layer data should be accessible through standard configuration API
        And FlexConfig generation should complete within reasonable time