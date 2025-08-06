Feature: App Configuration Basic Operations
As a developer using FlexKit with Azure App Configuration
I want to load configuration from App Configuration into FlexKit
So that I can access centralized configuration data dynamically

    @AppConfiguration @BasicOperations @FlexKit
    Scenario: Load basic configuration from App Configuration
        Given I have established an app config controller environment
        And I have app config controller configuration with App Configuration from "infrastructure-module-azure-config.json"
        When I configure app config controller by building the configuration
        Then the app config controller should support FlexKit dynamic access patterns
        And the app config controller configuration should contain "myapp:api:timeout" with value "30"
        And the app config controller should demonstrate FlexKit type conversion capabilities

    @AppConfiguration @LabelFiltering @FlexKit
    Scenario: Load environment-specific configuration using labels
        Given I have established an app config controller environment
        And I have app config controller configuration with labeled App Configuration from "infrastructure-module-azure-config.json"
        When I configure app config controller with environment label "production"
        And I configure app config controller by building the configuration
        Then the app config controller should support labeled configuration access
        And the app config controller configuration should contain "myapp:database:connectionString" with production value
        And the app config controller configuration should contain "myapp:logging:level" with value "Warning"

    @AppConfiguration @KeyFiltering @FlexKit
    Scenario: Load filtered configuration using key patterns
        Given I have established an app config controller environment
        And I have app config controller configuration with filtered App Configuration from "infrastructure-module-azure-config.json"
        When I configure app config controller with key filter "myapp:*"
        And I configure app config controller by building the configuration
        Then the app config controller should support filtered configuration access
        And the app config controller configuration should only contain keys starting with "myapp:"
        And the app config controller should not contain keys from other applications

    @AppConfiguration @FeatureFlags @FlexKit
    Scenario: Load and evaluate feature flags from App Configuration
        Given I have established an app config controller environment
        And I have app config controller configuration with feature flags from "infrastructure-module-azure-config.json"
        When I configure app config controller by building the configuration
        Then the app config controller should support feature flag evaluation
        And the app config controller configuration should contain "FeatureFlags:NewUI" with value "True"
        And the app config controller configuration should contain "FeatureFlags:BetaFeatures" with value "False"