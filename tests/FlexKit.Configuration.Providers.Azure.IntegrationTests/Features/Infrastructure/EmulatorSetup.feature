Feature: Emulator Setup for Azure Services
As a developer testing Azure integration
I want to set up Azure emulators to simulate Azure Key Vault and App Configuration
So that I can test FlexKit Azure configuration integration without real Azure dependencies

    @EmulatorModule @Setup @Infrastructure
    Scenario: Set up emulator module with Azure services
        Given I have prepared an emulator module environment
        When I configure emulator module with configuration from "TestData/infrastructure-module-azure-config.json"
        And I initialize the emulator module setup
        And I populate emulator module with all test data
        Then the emulator module should be configured successfully
        And the emulator module should have all Key Vault data populated
        And the emulator module should have all App Configuration data populated
        And the emulator module should be ready for integration testing

    @EmulatorModule @Setup @ConfigurationValidation
    Scenario: Validate emulator module configuration structure
        Given I have prepared an emulator module environment
        When I configure emulator module with configuration from "TestData/infrastructure-module-azure-config.json"
        And I validate emulator module configuration structure
        Then the emulator module configuration should be valid
        And the emulator module should contain Azure test credentials
        And the emulator module should contain test key vault definition
        And the emulator module should contain test app configuration definition

    @EmulatorModule @Teardown @Infrastructure
    Scenario: Tear down emulator module after testing
        Given I have a running emulator module environment
        When I request emulator module teardown
        Then the emulator module should stop gracefully
        And the emulator module resources should be cleaned up
        And the emulator module container should be removed
