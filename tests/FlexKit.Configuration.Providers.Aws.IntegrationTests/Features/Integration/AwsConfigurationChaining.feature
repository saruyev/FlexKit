Feature: AWS Configuration Chaining
As a developer using AWS providers together
I want to chain Parameter Store and Secrets Manager configurations
So that I can build comprehensive configuration systems with proper precedence and overrides

    @AwsChaining @ParameterStore @SecretsManager
    Scenario: Chain Parameter Store and Secrets Manager configurations
        Given I have established a chaining controller environment
        And I have chaining controller configuration with Parameter Store from "infrastructure-module-aws-config.json"
        And I have chaining controller configuration with Secrets Manager from "infrastructure-module-aws-config.json"
        When I configure chaining controller by building the configuration
        Then the chaining controller configuration should contain "infrastructure-module:database:host" with value "localhost"
        And the chaining controller configuration should contain "infrastructure-module-database-credentials" with JSON value containing "db.example.com"
        And the chaining controller should demonstrate configuration source chaining

    @AwsChaining @JsonProcessing @Mixed
    Scenario: Chain AWS configurations with JSON processing enabled
        Given I have established a chaining controller environment
        And I have chaining controller configuration with Parameter Store and JSON processing from "infrastructure-module-aws-config.json"
        And I have chaining controller configuration with Secrets Manager and JSON processing from "infrastructure-module-aws-config.json"
        When I configure chaining controller by building the configuration
        Then the chaining controller configuration should contain "infrastructure-module:database:credentials:username" with value "testuser"
        And the chaining controller configuration should contain "infrastructure-module-database-credentials:host" with value "db.example.com"
        And the chaining controller should handle JSON processing across multiple sources

    @AwsChaining @Precedence @Override
    Scenario: Demonstrate configuration precedence with chained AWS sources
        Given I have established a chaining controller environment
        And I have chaining controller configuration with Parameter Store precedence from "infrastructure-module-aws-config.json"
        And I have chaining controller configuration with Secrets Manager precedence from "infrastructure-module-aws-config.json"
        When I configure chaining controller with source precedence testing
        Then the chaining controller should demonstrate proper configuration precedence
        And the chaining controller configuration should show later sources overriding earlier ones
        And the chaining controller should handle precedence with JSON and non-JSON values

    @AwsChaining @Optional @ErrorHandling
    Scenario: Chain AWS configurations with optional sources and error handling
        Given I have established a chaining controller environment
        And I have chaining controller configuration with required Parameter Store from "infrastructure-module-aws-config.json"
        And I have chaining controller configuration with optional Secrets Manager from "infrastructure-module-aws-config.json"
        When I configure chaining controller with mixed optional requirements
        Then the chaining controller configuration should be built successfully
        And the chaining controller should handle optional sources gracefully
        And the chaining controller configuration should contain required Parameter Store values

    @AwsChaining @FlexKit @DynamicAccess
    Scenario: Demonstrate FlexKit dynamic access across chained AWS configurations
        Given I have established a chaining controller environment
        And I have chaining controller configuration with full AWS integration from "infrastructure-module-aws-config.json"
        When I configure chaining controller by building the configuration
        And I verify chaining controller dynamic access capabilities
        Then the chaining controller should support dynamic access across all sources
        And the chaining controller should demonstrate FlexKit integration with chained sources
        And the chaining controller configuration should support complex property navigation

    @AwsChaining @Performance @Optimization
    Scenario: Verify performance characteristics of chained AWS configurations
        Given I have established a chaining controller environment
        And I have chaining controller configuration with optimized Parameter Store from "infrastructure-module-aws-config.json"
        And I have chaining controller configuration with optimized Secrets Manager from "infrastructure-module-aws-config.json"
        When I configure chaining controller with performance monitoring
        Then the chaining controller should build configuration efficiently
        And the chaining controller should handle multiple AWS sources without significant overhead
        And the chaining controller should demonstrate acceptable configuration loading times
