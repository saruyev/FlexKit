# RegisterConfig Extension Integration Tests

Feature: RegisterConfig Extension Integration
    As a developer using FlexKit Configuration
    I want to register strongly typed configuration objects with Autofac
    So that I can inject type-safe configuration into my services

    Background:
        Given I have established a registration module testing environment

    @RegisterConfig @BasicRegistration
    Scenario: Register configuration objects with root and section bindings
        When I provision registration module test data from "TestData/ConfigurationFiles/appsettings.json"
        And I provision additional registration module data:
          | Key                         | Value                   |
          | Name                        | RegisterConfig Test App |
          | Version                     | 2.1.0                   |
          | Environment                 | IntegrationTest         |
          | Database:CommandTimeout     | 60                      |
          | Database:MaxRetryCount      | 5                       |
          | Database:EnableLogging      | true                    |
          | External:PaymentApi:ApiKey  | payment-key-12345       |
          | External:PaymentApi:Timeout | 8000                    |
          | Features:EnableCaching      | true                    |
          | Features:MaxCacheSize       | 1000                    |
        And I registration module configure typed configuration mappings:
            | ConfigType              | SectionPath                |
            | AppConfig               |                            |
            | DatabaseConfig          | Database                   |
            | PaymentApiConfig        | External:PaymentApi        |
            | FeatureConfig           | Features                   |
        And I registration module register dependent services
        And I registration module finalize container construction
        Then the registration module container should build successfully
        And I should resolve all registered configuration types
        And the AppConfig should contain correct root values
        And the DatabaseConfig should contain correct database values
        And the PaymentApiConfig should contain correct payment API values
        And the FeatureConfig should contain correct feature values
        And all configuration instances should be singletons

    @RegisterConfig @ServiceInjection
    Scenario: Inject registered configurations into services
        When I provision registration module test data:
            | Key                           | Value                         |
            | Database:ConnectionString     | Server=test;Database=ServiceTest; |
            | Database:CommandTimeout       | 45                           |
            | Database:EnableLogging        | true                         |
            | Api:BaseUrl                   | https://service-test.api.com |
            | Api:ApiKey                    | service-test-key             |
            | Api:Timeout                   | 5000                         |
        And I registration module configure typed configuration mappings:
            | ConfigType     | SectionPath |
            | DatabaseConfig | Database    |
            | ApiConfig      | Api         |
        And I registration module register services that depend on configurations
        And I registration module finalize container construction
        Then the registration module container should build successfully
        And I should resolve services with injected configurations
        And the DatabaseService should have correct database configuration
        And the ApiService should have correct API configuration
        And configuration instances in services should match directly resolved ones

    @RegisterConfig @MissingSection
    Scenario: Handle missing configuration sections with default values
        When I provision registration module minimal data:
            | Key                           | Value                         |
            | ExistingSection:Value         | exists                        |
        And I registration module configure typed configuration mappings:
            | ConfigType     | SectionPath      |
            | DatabaseConfig | Database         |
            | ApiConfig      | NonExistentApi   |
        And I registration module finalize container construction
        Then the registration module container should build successfully
        And I should resolve configurations with missing sections
        And the DatabaseConfig should use default values
        And the ApiConfig should use default values

    @RegisterConfig @FluentInterface
    Scenario: Use fluent interface for chaining multiple registrations
        When I provision registration module test data:
            | Key                           | Value                         |
            | Database:ConnectionString     | Server=fluent;Database=FluentTest; |
            | Database:CommandTimeout       | 30                           |
            | Api:BaseUrl                   | https://fluent.api.com       |
            | Api:ApiKey                    | fluent-key                   |
        And I registration module use fluent interface for multiple configurations
        And I registration module finalize container construction
        Then the registration module container should build successfully
        And all fluently registered configurations should be available
        And the fluent interface should return the container builder for chaining

    @RegisterConfig @ErrorHandling
    Scenario: Handle configuration binding errors
        When I provision registration module invalid data:
            | Key                           | Value                         |
            | Database:ConnectionString     | Server=error;                |
            | Database:CommandTimeout       | not-a-number                 |
            | Database:MaxRetryCount        | also-not-a-number            |
        And I registration module configure typed configuration mappings:
            | ConfigType     | SectionPath |
            | DatabaseConfig | Database    |
        And I registration module finalize container construction
        Then the registration module container should build successfully
        When I attempt to resolve DatabaseConfig
        Then resolving DatabaseConfig should throw a dependency resolution exception
        And the exception should contain binding error information

    @RegisterConfig @BatchRegistration
    Scenario: Use batch registration for multiple configurations
        When I provision registration module test data:
            | Key                           | Value                         |
            | Database:ConnectionString     | Server=batch;Database=BatchTest; |
            | Database:CommandTimeout       | 25                           |
            | FirstApi:BaseUrl              | https://first.api.com        |
            | FirstApi:ApiKey               | first-key                    |
            | SecondApi:BaseUrl             | https://second.api.com       |
            | SecondApi:ApiKey              | second-key                   |
        And I registration module define batch configuration mappings:
            | ConfigType     | SectionPath |
            | DatabaseConfig | Database    |
            | ApiConfig      | FirstApi    |
        And I registration module register configurations using batch method
        And I registration module finalize container construction
        Then the registration module container should build successfully
        And all batch registered configurations should be available
        And each configuration type should be properly bound to its section