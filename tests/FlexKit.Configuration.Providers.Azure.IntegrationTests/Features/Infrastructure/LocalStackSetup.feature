Feature: LocalStack Setup for Azure Services
As a developer testing Azure integration
I want to set up LocalStack to simulate Azure Key Vault and App Configuration
So that I can test FlexKit Azure configuration integration without real Azure dependencies

    @LocalStackModule @Setup @Infrastructure
    Scenario: Set up local stack module with Azure services
        Given I have prepared a local stack module environment
        When I configure local stack module with configuration from "TestData/infrastructure-module-azure-config.json"
        And I initialize the local stack module setup
        And I populate local stack module with all test data
        Then the local stack module should be configured successfully
        And the local stack module should have all Key Vault data populated
        And the local stack module should have all App Configuration data populated
        And the local stack module should be ready for integration testing

    @LocalStackModule @Setup @ConfigurationValidation
    Scenario: Validate local stack module configuration structure
        Given I have prepared a local stack module environment
        When I configure local stack module with configuration from "TestData/infrastructure-module-azure-config.json"
        And I validate local stack module configuration structure
        Then the local stack module configuration should be valid
        And the local stack module should contain LocalStack settings
        And the local stack module should contain Azure test credentials
        And the local stack module should contain test key vault definition
        And the local stack module should contain test app configuration definition

    @LocalStackModule @Teardown @Infrastructure
    Scenario: Tear down local stack module after testing
        Given I have a running local stack module environment
        When I request local stack module teardown
        Then the local stack module should stop gracefully
        And the local stack module resources should be cleaned up
        And the local stack module container should be removed