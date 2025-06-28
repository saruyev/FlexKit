Feature: ASP.NET Core Integration
  As a developer building ASP.NET Core applications
  I want to integrate FlexKit.Configuration with ASP.NET Core hosting
  So that I can use FlexConfig in my web applications with proper dependency injection

  Background:
    Given I have setup an ASP.NET Core application environment

  Scenario: Configure FlexConfig with WebApplicationBuilder using JSON configuration
    When I configure ASP.NET Core host with FlexConfig and the following configuration:
      | Key                    | Value                |
      | Application:Name       | TestWebApp           |
      | Application:Version    | 1.0.0                |
      | Database:Provider      | SqlServer            |
      | Database:Timeout       | 30                   |
    And I configure ASP.NET Core host to use FlexConfig with JSON file "TestData/ConfigurationFiles/appsettings.json"
    And I build the ASP.NET Core application host
    Then the ASP.NET Core host should contain FlexConfig service
    And I should be able to resolve FlexConfig from ASP.NET Core services
    And FlexConfig should contain the expected ASP.NET Core configuration values

  Scenario: Configure FlexConfig with WebApplicationBuilder using multiple sources
    When I configure ASP.NET Core host with FlexConfig using multiple sources:
      | Source         | Path                                              | Optional |
      | JsonFile       | TestData/ConfigurationFiles/appsettings.json     | false    |
      | JsonFile       | TestData/ConfigurationFiles/appsettings.Development.json | true     |
      | DotEnvFile     | TestData/ConfigurationFiles/test.env             | true     |
      | Environment    |                                                   | false    |
    And I configure ASP.NET Core host with the following environment variables:
      | Key               | Value              |
      | ASPNETCORE_ENVIRONMENT | Development   |
      | API_KEY           | secret-dev-key     |
    And I build the ASP.NET Core application host
    Then the ASP.NET Core host should contain FlexConfig service
    And FlexConfig should include values from all ASP.NET Core sources
    And the environment variables should override configuration file values

  Scenario: Register custom services with FlexConfig dependency in ASP.NET Core
    Given I have a test service that depends on FlexConfig
    When I configure ASP.NET Core host with FlexConfig and the following configuration:
      | Key                    | Value                |
      | Service:Name           | TestService          |
      | Service:Enabled        | true                 |
      | Service:MaxRetries     | 3                    |
    And I register the test service in ASP.NET Core services
    And I build the ASP.NET Core application host
    Then I should be able to resolve the test service from ASP.NET Core services
    And the test service should have FlexConfig injected
    And the test service should be able to access configuration values through FlexConfig

  Scenario: Configure Autofac container with ASP.NET Core and FlexConfig
    When I configure ASP.NET Core host to use Autofac service provider factory
    And I configure ASP.NET Core Autofac container with FlexConfig and the following configuration:
      | Key                    | Value                |
      | Autofac:ModuleName     | TestModule           |
      | Autofac:Enabled        | true                 |
      | Database:Provider      | InMemory             |
    And I register a test Autofac module in the ASP.NET Core container
    And I build the ASP.NET Core application host
    Then the ASP.NET Core host should use Autofac as service provider
    And I should be able to resolve FlexConfig from Autofac container
    And the test Autofac module should be registered and functional

  Scenario: Configure FlexConfig with ASP.NET Core configuration validation
    When I configure ASP.NET Core host with FlexConfig and the following configuration:
      | Key                    | Value                |
      | Validation:Required    | RequiredValue        |
      | Validation:MaxLength   | 50                   |
      | Validation:MinValue    | 10                   |
    And I configure ASP.NET Core host to validate configuration on startup
    And I build the ASP.NET Core application host
    Then the ASP.NET Core application should start successfully
    And FlexConfig should contain validated configuration values
    And the configuration validation should pass

  Scenario: Handle configuration errors during ASP.NET Core startup
    When I configure ASP.NET Core host with FlexConfig and invalid JSON file "TestData/ConfigurationFiles/invalid.json"
    And I try to build the ASP.NET Core application host
    Then the ASP.NET Core host building should fail gracefully
    And the error should indicate configuration loading failure
    And the error message should be descriptive for ASP.NET Core context

  Scenario: Configure FlexConfig with ASP.NET Core environment-specific settings
    When I configure ASP.NET Core host for "Development" environment
    And I configure ASP.NET Core host with FlexConfig using environment-specific files:
      | Environment | JsonFile                                              |
      | Default     | TestData/ConfigurationFiles/appsettings.json         |
      | Development | TestData/ConfigurationFiles/appsettings.Development.json |
      | Production  | TestData/ConfigurationFiles/appsettings.Production.json  |
    And I build the ASP.NET Core application host
    Then FlexConfig should load the Development-specific configuration
    And the Development values should override base configuration values
    And the ASP.NET Core environment should be correctly configured

  Scenario: Use FlexConfig dynamic access in ASP.NET Core controllers
    Given I have setup an ASP.NET Core controller that uses FlexConfig
    When I configure ASP.NET Core host with FlexConfig and the following configuration:
      | Key                    | Value                |
      | Api:BaseUrl            | https://api.test.com |
      | Api:Timeout            | 5000                 |
      | Features:EnableCache   | true                 |
    And I register the test controller in ASP.NET Core services
    And I build the ASP.NET Core application host
    Then I should be able to resolve the test controller from ASP.NET Core services
    And the test controller should access configuration values dynamically
    And the controller should demonstrate FlexConfig dynamic access patterns