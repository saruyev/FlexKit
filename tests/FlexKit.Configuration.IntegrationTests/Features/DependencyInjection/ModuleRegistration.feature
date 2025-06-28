Feature: Module Registration
    As a developer using FlexKit Configuration with Autofac
    I want to register configuration modules automatically
    So that I can organize configuration setup in modular components

    Background:
        Given I have established a module registration test environment

    @ModuleRegistration @BasicModuleSetup
    Scenario: Register a basic configuration module
        When I compose a test configuration module with settings:
            | Key                     | Value                    |
            | Module:Name             | TestModule               |
            | Module:Version          | 1.0.0                    |
            | Module:IsEnabled        | true                     |
        And I deploy the test module to the container builder
        And I finalize the container with deployed modules
        Then the container should reflect deployed test module registrations
        And the deployed test module should expose FlexConfig correctly
        And I should access configuration through the deployed test module

    @ModuleRegistration @ModuleWithDependencies
    Scenario: Register a module with service dependencies
        When I compose a test configuration module with settings:
            | Key                     | Value                    |
            | Module:ServiceA         | ServiceA-Config          |
            | Module:ServiceB         | ServiceB-Config          |
            | Module:DatabaseUrl      | test://localhost:5432    |
        And I deploy a test service depending on module configuration
        And I deploy the test module to the container builder
        And I finalize the container with deployed modules
        Then the container should encompass both test module and service deployments
        And the service should obtain configuration from the deployed test module
        And the deployed test module configuration should be available to dependent services

    @ModuleRegistration @MultipleModules
    Scenario: Register multiple configuration modules
        When I compose configuration module "ModuleA" with settings:
            | Key                     | Value                    |
            | ModuleA:Setting1        | valueA1                  |
            | ModuleA:Setting2        | valueA2                  |
        And I compose configuration module "ModuleB" with settings:
            | Key                     | Value                    |
            | ModuleB:Setting1        | valueB1                  |
            | ModuleB:Setting2        | valueB2                  |
        And I deploy both test modules to the container builder
        And I finalize the container with deployed modules
        Then the container should encompass deployments from both test modules
        And configuration from test module "ModuleA" should be reachable
        And configuration from test module "ModuleB" should be reachable
        And deployed test modules should work independently without interference