Feature: Azure FlexKit Integration
As a developer integrating Azure services with FlexKit Configuration
I want to combine Key Vault and App Configuration with FlexKit dynamic capabilities
So that I can leverage Azure configuration services with FlexKit's enhanced features

    @Azure @FlexKit @Integration
    Scenario: Integrate Key Vault with FlexKit dynamic access
        Given I have established an integration controller environment
        And I have integration controller configuration with Key Vault from "infrastructure-module-azure-config.json"
        When I configure integration controller by building the configuration
        Then the integration controller should support FlexKit dynamic access patterns
        And the integration controller configuration should contain "myapp:database:host" with value "localhost"
        And the integration controller should demonstrate FlexKit type conversion capabilities

    @Azure @FlexKit @AppConfiguration @Integration
    Scenario: Integrate App Configuration with FlexKit enhanced features
        Given I have established an integration controller environment
        And I have integration controller configuration with App Configuration from "infrastructure-module-azure-config.json"
        When I configure integration controller by building the configuration
        Then the integration controller should support FlexKit dynamic access to configuration
        And the integration controller configuration should contain "myapp:api:timeout" with value "30"
        And the integration controller should demonstrate advanced FlexKit features

    @Azure @FlexKit @CombinedSources @Integration
    Scenario: Integrate combined Key Vault and App Configuration sources
        Given I have established an integration controller environment
        And I have integration controller configuration with Key Vault from "infrastructure-module-azure-config.json"
        And I have integration controller configuration with App Configuration from "infrastructure-module-azure-config.json"
        When I configure integration controller by building the configuration
        Then the integration controller should support FlexKit dynamic access patterns
        And the integration controller configuration should demonstrate integrated Azure configuration
        And the integration controller should demonstrate FlexKit precedence handling

    @Azure @FlexKit @JSONProcessing @Integration
    Scenario: Integrate JSON processing across Azure sources with FlexKit
        Given I have established an integration controller environment
        And I have integration controller configuration with JSON-enabled Key Vault from "infrastructure-module-azure-config.json"
        And I have integration controller configuration with JSON-enabled App Configuration from "infrastructure-module-azure-config.json"
        When I configure integration controller by building the configuration
        Then the integration controller should support comprehensive JSON processing
        And the integration controller configuration should demonstrate complex data structure handling
        And the integration controller should demonstrate FlexKit JSON integration capabilities

    @Azure @FlexKit @ErrorTolerance @Integration
    Scenario: Test FlexKit integration with Azure error tolerance
        Given I have established an integration controller environment
        And I have integration controller configuration with optional Azure sources from "infrastructure-module-azure-config.json"
        When I configure integration controller with error tolerance by building the configuration
        And I verify integration controller advanced FlexKit capabilities
        Then the integration controller should support FlexKit dynamic access patterns
        And the integration controller should demonstrate error-tolerant Azure integration