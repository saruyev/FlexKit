Feature: AWS FlexKit Integration
As a developer integrating AWS services with FlexKit Configuration
I want to combine Parameter Store and Secrets Manager with FlexKit dynamic capabilities
So that I can leverage AWS configuration services with FlexKit's enhanced features

    @AWS @FlexKit @Integration
    Scenario: Integrate Parameter Store with FlexKit dynamic access
        Given I have established an integration controller environment
        And I have integration controller configuration with Parameter Store from "infrastructure-module-aws-config.json"
        When I configure integration controller by building the configuration
        Then the integration controller should support FlexKit dynamic access patterns
        And the integration controller configuration should contain "infrastructure-module:database:host" with value "localhost"
        And the integration controller should demonstrate FlexKit type conversion capabilities

    @AWS @FlexKit @SecretsManager @Integration
    Scenario: Integrate Secrets Manager with FlexKit enhanced features
        Given I have established an integration controller environment
        And I have integration controller configuration with Secrets Manager from "infrastructure-module-aws-config.json"
        When I configure integration controller by building the configuration
        Then the integration controller should support FlexKit dynamic access to secrets
        And the integration controller configuration should contain "infrastructure-module-database-credentials" with value '{"host":"db.example.com","port":5432,"username":"dbuser","password":"dbpass123"}'
        And the integration controller should handle JSON secret processing with FlexKit

    @AWS @FlexKit @CombinedSources @Integration
    Scenario: Combine Parameter Store and Secrets Manager with FlexKit
        Given I have established an integration controller environment
        And I have integration controller configuration with Parameter Store from "infrastructure-module-aws-config.json"
        And I have integration controller configuration with Secrets Manager from "infrastructure-module-aws-config.json"
        When I configure integration controller by building the configuration
        Then the integration controller should support FlexKit access across AWS sources
        And the integration controller should handle parameter precedence correctly
        And the integration controller configuration should demonstrate integrated AWS configuration

    @AWS @FlexKit @JsonProcessing @Integration
    Scenario: AWS configuration with FlexKit JSON processing integration
        Given I have established an integration controller environment
        And I have integration controller configuration with JSON-enabled Parameter Store from "infrastructure-module-aws-config.json"
        And I have integration controller configuration with JSON-enabled Secrets Manager from "infrastructure-module-aws-config.json"
        When I configure integration controller by building the configuration
        Then the integration controller should process JSON configuration hierarchically
        And the integration controller configuration should contain "infrastructure-module:database:credentials:username" with value "testuser"
        And the integration controller should support FlexKit navigation of JSON-processed AWS data

    @AWS @FlexKit @ErrorHandling @Integration
    Scenario: AWS FlexKit integration with graceful error handling
        Given I have established an integration controller environment
        And I have integration controller configuration with optional AWS sources from "infrastructure-module-minimal-config.json"
        When I configure integration controller with error tolerance
        Then the integration controller should build successfully with partial AWS data
        And the integration controller should maintain FlexKit capabilities with limited configuration
        And the integration controller should handle missing AWS resources gracefully

    @AWS @FlexKit @DynamicConfiguration @Integration
    Scenario: Demonstrate comprehensive FlexKit dynamic capabilities with AWS
        Given I have established an integration controller environment
        And I have integration controller configuration with comprehensive AWS setup from "infrastructure-module-aws-config.json"
        When I configure integration controller by building the configuration
        And I verify integration controller advanced FlexKit capabilities
        Then the integration controller should support dynamic property access
        And the integration controller should enable complex configuration navigation
        And the integration controller should demonstrate FlexKit type safety with AWS data