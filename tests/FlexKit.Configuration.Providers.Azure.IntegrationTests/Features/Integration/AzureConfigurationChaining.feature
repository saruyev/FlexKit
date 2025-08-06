Feature: Azure Configuration Chaining
As a developer using multiple Azure configuration sources
I want to chain Key Vault and App Configuration with proper precedence
So that I can build complex configuration hierarchies with FlexKit

    @Azure @Chaining @Precedence
    Scenario: Chain Key Vault and App Configuration with precedence
        Given I have established a chaining controller environment
        And I have chaining controller configuration with Key Vault from "infrastructure-module-azure-config.json"
        And I have chaining controller configuration with App Configuration from "infrastructure-module-azure-config.json"
        When I configure chaining controller by building the configuration
        Then the chaining controller should demonstrate proper source precedence
        And the chaining controller configuration should prioritize App Configuration over Key Vault
        And the chaining controller should support FlexKit dynamic access patterns

    @Azure @Chaining @JSONProcessing
    Scenario: Chain Azure sources with JSON processing enabled
        Given I have established a chaining controller environment
        And I have chaining controller configuration with JSON-enabled Key Vault from "infrastructure-module-azure-config.json"
        And I have chaining controller configuration with JSON-enabled App Configuration from "infrastructure-module-azure-config.json"
        When I configure chaining controller with JSON processing
        And I configure chaining controller by building the configuration
        Then the chaining controller should support cross-source JSON processing
        And the chaining controller configuration should demonstrate complex JSON chaining
        And the chaining controller should maintain proper precedence with JSON flattening

    @Azure @Chaining @Performance
    Scenario: Monitor performance of chained Azure configuration loading
        Given I have established a chaining controller environment
        And I have chaining controller configuration with performance monitoring
        And I have chaining controller configuration with Key Vault from "infrastructure-module-azure-config.json"
        And I have chaining controller configuration with App Configuration from "infrastructure-module-azure-config.json"
        When I configure chaining controller with performance tracking
        And I configure chaining controller by building the configuration
        Then the chaining controller should complete configuration loading within acceptable time
        And the chaining controller should demonstrate efficient source chaining
        And the chaining controller should report meaningful performance metrics