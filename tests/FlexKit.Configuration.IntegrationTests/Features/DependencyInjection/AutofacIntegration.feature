Feature: Autofac Integration
    As a developer using FlexKit Configuration with Autofac
    I want to integrate FlexConfig with Autofac dependency injection
    So that I can have automatic registration and injection of configuration

    Background:
        Given I have initialized an Autofac integration test environment

    @AutofacIntegration @BasicRegistration
    Scenario: Register FlexConfig in Autofac container
        When I configure FlexConfig with basic configuration data:
            | Key                      | Value                    |
            | Application:Name         | FlexKit Test App         |
            | Application:Version      | 1.0.0                    |
            | Database:ConnectionString| Server=localhost         |
            | Features:EnableCaching   | true                     |
        And I register FlexConfig in the Autofac container
        And I build the Autofac container
        Then the container should contain FlexConfig registration
        And I should be able to resolve IFlexConfig from the container
        And I should be able to resolve dynamic configuration from the container
        And the resolved configuration should contain expected values

    @AutofacIntegration @ConfigurationSourceIntegration
    Scenario: Integrate multiple configuration sources with Autofac
        When I setup FlexConfig with JSON configuration from "TestData/ConfigurationFiles/appsettings.json"
        And I setup FlexConfig with environment configuration from "TestData/ConfigurationFiles/test.env"
        And I register the multi-source FlexConfig in the Autofac container
        And I build the Autofac container
        Then the container should successfully resolve FlexConfig
        And the resolved FlexConfig should contain data from JSON sources
        And the resolved FlexConfig should contain data from environment sources
        And environment values should override JSON values where applicable

    @AutofacIntegration @ServiceRegistration
    Scenario: Register services that depend on FlexConfig
        When I configure FlexConfig with service configuration:
            | Key                    | Value                      |
            | ServiceA:Endpoint      | https://api.example.com    |
            | ServiceA:Timeout       | 5000                       |
            | ServiceB:ApiKey        | test-key-123               |
            | Database:Host          | localhost                  |
            | Database:Port          | 5432                       |
        And I register FlexConfig in the Autofac container
        And I register a test service that depends on IFlexConfig
        And I register a test service that depends on dynamic configuration
        And I build the Autofac container
        Then the container should resolve the test service with IFlexConfig dependency
        And the container should resolve the test service with dynamic dependency
        And the injected configuration should be accessible to the services

    @AutofacIntegration @PropertyInjection
    Scenario: Property injection of FlexConfig
        When I configure FlexConfig with application settings:
            | Key                  | Value                 |
            | App:Title            | Test Application      |
            | App:MaxUsers         | 100                   |
            | Logging:Level        | Information           |
        And I register FlexConfig in the Autofac container with property injection
        And I register a service with FlexConfig property
        And I build the Autofac container
        Then the container should resolve the service
        And the service should have FlexConfig injected via property
        And the injected FlexConfig should contain the expected data

    @AutofacIntegration @ModuleRegistration
    Scenario: Register FlexConfig through Autofac module
        When I configure the module with configuration data:
            | Key                     | Value                     |
            | Module:Setting1         | value1                    |
            | Module:Setting2         | value2                    |
            | External:ApiUrl         | https://test.api.com      |
        And I create an Autofac module that registers FlexConfig
        And I register the module in the Autofac container
        And I build the Autofac container
        Then the container should contain registrations from the module
        And the module should have registered FlexConfig correctly
        And I should be able to resolve configuration through the module

    @AutofacIntegration @LifetimeManagement
    Scenario: Verify FlexConfig lifetime management
        When I configure FlexConfig with test data:
            | Key           | Value        |
            | Test:Value    | singleton    |
        And I register FlexConfig as singleton in the Autofac container
        And I build the Autofac container
        Then resolving FlexConfig multiple times should return the same instance
        And the singleton instance should maintain state across resolutions
        And disposing the container should dispose the FlexConfig instance

    @AutofacIntegration @ScopedResolution
    Scenario: Test scoped resolution patterns
        When I configure FlexConfig with scoped test data:
            | Key                 | Value              |
            | Scope:TestValue     | scoped-data        |
            | Scope:Identifier    | scope-123          |
        And I register FlexConfig in the Autofac container
        And I build the Autofac container
        And I create a lifetime scope
        Then I should be able to resolve FlexConfig from the lifetime scope
        And the scoped FlexConfig should contain the expected data
        And disposing the scope should not affect the parent container

    @AutofacIntegration @DynamicAccess
    Scenario: Verify dynamic access through Autofac
        When I configure FlexConfig with nested configuration:
            | Key                          | Value                    |
            | Api:External:BaseUrl         | https://external.api.com |
            | Api:External:Timeout         | 10000                    |
            | Api:Internal:BaseUrl         | https://internal.api.com |
            | Database:Primary:Host        | primary.db.com           |
            | Database:Secondary:Host      | secondary.db.com         |
        And I register FlexConfig in the Autofac container
        And I build the Autofac container
        And I resolve dynamic configuration from the container
        Then I should be able to access nested properties dynamically
        And dynamic access should work for "Api.External.BaseUrl"
        And dynamic access should work for "Database.Primary.Host"
        And dynamic access should return correct values

    @AutofacIntegration @ErrorHandling
    Scenario: Handle configuration errors in Autofac integration
        When I attempt to register FlexConfig with invalid configuration
        And I try to build the Autofac container
        Then the container build should handle configuration errors gracefully
        And appropriate error information should be available
        And the container should remain in a valid state for other registrations

    @AutofacIntegration @MultipleRegistrations
    Scenario: Handle multiple FlexConfig registrations
        When I configure FlexConfig with test data:
            | Key              | Value              |
            | Multi:Test       | multiple-reg       |
        And I register FlexConfig as IFlexConfig in the Autofac container
        And I register FlexConfig as dynamic in the Autofac container
        And I build the Autofac container
        Then I should be able to resolve IFlexConfig from the container
        And I should be able to resolve dynamic configuration from the container
        And both registrations should refer to the same configuration data
        And the registrations should maintain consistency