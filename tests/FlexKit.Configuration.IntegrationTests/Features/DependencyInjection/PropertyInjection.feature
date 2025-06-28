Feature: Property Injection
    As a developer using FlexKit Configuration with Autofac
    I want to inject configuration into service properties
    So that I can access configuration without constructor dependencies

    Background:
        Given I have arranged a property injection test environment

    @PropertyInjection @BasicPropertyInjection
    Scenario: Inject FlexConfig into service property
        When I enable property injection for container
        And I register service with FlexConfig property
        And I build container with property injection support
        And I resolve service with property injection
        Then the service should have FlexConfig injected into property
        And the injected FlexConfig should provide access to configuration values

    @PropertyInjection @MultiplePropertyInjection
    Scenario: Inject multiple configuration properties
        Given I have configured test configuration data:
            | Key                    | Value                |
            | Database:Host          | prop-injection.db    |
            | Database:Port          | 5432                 |
            | Api:BaseUrl           | https://prop-api.com |
            | Api:Timeout           | 30000                |
        When I enable property injection for container
        And I register service with multiple configuration properties
        And I build container with property injection support
        And I resolve service with multiple properties
        Then the service should have all configuration properties injected
        And each injected property should contain expected configuration data

    @PropertyInjection @PropertyInjectionWithJson
    Scenario: Property injection with JSON configuration source
        When I enable property injection for container
        And I integrate JSON file "TestData/ConfigurationFiles/appsettings.json" as configuration source
        And I register service with FlexConfig property
        And I build container with property injection support
        And I resolve service with property injection
        Then the service should have FlexConfig injected into property
        And the injected FlexConfig should provide access to JSON configuration values
        And the property injection should work with JSON-based configuration

    @PropertyInjection @PropertyInjectionLifetime
    Scenario: Verify property injection lifetime behavior
        When I enable property injection for container
        And I register service with FlexConfig property as singleton
        And I build container with property injection support
        And I resolve multiple instances of service with property injection
        Then all instances should share the same FlexConfig property instance
        And the singleton service should maintain property injection across resolutions

    @PropertyInjection @PropertyInjectionWithModule
    Scenario: Property injection with custom configuration module
        When I enable property injection for container
        And I create custom property injection module with configuration:
            | Key                      | Value                     |
            | CustomModule:Setting1    | custom-prop-value-1       |
            | CustomModule:Setting2    | custom-prop-value-2       |
            | Features:EnableLogging   | true                      |
        And I register custom module in container
        And I register service with FlexConfig property
        And I build container with property injection support
        And I resolve service with property injection
        Then the service should have FlexConfig injected into property
        And the injected FlexConfig should contain custom module configuration

    @PropertyInjection @PropertyInjectionError
    Scenario: Handle property injection configuration errors
        When I enable property injection for container
        And I register service with FlexConfig property without providing configuration
        And I attempt to build container with property injection support
        Then the container should build successfully with default behavior
        And property injection should handle missing configuration gracefully

    @PropertyInjection @PropertyInjectionValidation
    Scenario: Validate property injection with environment variables
        When I enable property injection for container
        And I assign environment variable "PROP_INJECTION__DATABASE__HOST" to "env-host.com"
        And I assign environment variable "PROP_INJECTION__FEATURES__ENABLED" to "true"
        And I assign environment variables with prefix "PROP_INJECTION" as configuration source
        And I register service with FlexConfig property
        And I build container with property injection support
        And I resolve service with property injection
        Then the service should have FlexConfig injected into property
        And the injected FlexConfig should provide access to environment variable values
        And property injection should work with environment-based configuration