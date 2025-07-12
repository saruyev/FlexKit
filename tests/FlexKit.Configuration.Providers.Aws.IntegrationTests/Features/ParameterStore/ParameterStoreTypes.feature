Feature: Parameter Store Types
As a developer using AWS Parameter Store with different parameter types
I want to load String, StringList, and SecureString parameters correctly
So that I can handle various data types in my configuration

    @ParameterStore @Types @String
    Scenario: Load String parameter types from Parameter Store
        Given I have established a parameters types module environment
        And I have parameters types module configuration with String parameters from "infrastructure-module-aws-config.json"
        When I configure parameters types module by building the configuration
        Then the parameters types module configuration should contain "infrastructure-module:database:host" with value "localhost"
        And the parameters types module configuration should contain "infrastructure-module:database:port" with value "5432"
        And the parameters types module should handle String parameters correctly

    @ParameterStore @Types @StringList
    Scenario: Load StringList parameter types from Parameter Store
        Given I have established a parameters types module environment
        And I have parameters types module configuration with StringList parameters from "infrastructure-module-aws-config.json"
        When I configure parameters types module by building the configuration
        Then the parameters types module configuration should contain "infrastructure-module:api:allowed-origins:0" with value "http://localhost:3000"
        And the parameters types module configuration should contain "infrastructure-module:api:allowed-origins:1" with value "https://test.example.com"
        And the parameters types module configuration should contain "infrastructure-module:api:allowed-origins:2" with value "https://api.example.com"
        And the parameters types module should handle StringList parameters correctly

    @ParameterStore @Types @SecureString
    Scenario: Load SecureString parameter types from Parameter Store
        Given I have established a parameters types module environment
        And I have parameters types module configuration with SecureString parameters from "infrastructure-module-aws-config.json"
        When I configure parameters types module by building the configuration
        Then the parameters types module configuration should contain "infrastructure-module:database:credentials" with value '{"username":"testuser","password":"testpass"}'
        And the parameters types module should handle SecureString parameters correctly

    @ParameterStore @Types @SecureString @JsonProcessing
    Scenario: Load SecureString parameter with JSON processing
        Given I have established a parameters types module environment
        And I have parameters types module configuration with SecureString JSON processing from "infrastructure-module-aws-config.json"
        When I configure parameters types module by building the configuration
        Then the parameters types module configuration should contain "infrastructure-module:database:credentials:username" with value "testuser"
        And the parameters types module configuration should contain "infrastructure-module:database:credentials:password" with value "testpass"
        And the parameters types module should handle SecureString JSON processing correctly

    @ParameterStore @Types @Mixed
    Scenario: Load mixed parameter types in single configuration
        Given I have established a parameters types module environment
        And I have parameters types module configuration with mixed parameter types from "infrastructure-module-aws-config.json"
        When I configure parameters types module by building the configuration
        Then the parameters types module configuration should contain "infrastructure-module:database:host" with value "localhost"
        And the parameters types module configuration should contain "infrastructure-module:api:allowed-origins:0" with value "http://localhost:3000"
        And the parameters types module configuration should contain "infrastructure-module:database:credentials" with value '{"username":"testuser","password":"testpass"}'
        And the parameters types module should handle mixed parameter types correctly

    @ParameterStore @Types @JsonProcessing @Mixed
    Scenario: Load mixed parameter types with selective JSON processing
        Given I have established a parameters types module environment
        And I have parameters types module configuration with mixed types and JSON processing from "infrastructure-module-aws-config.json"
        When I configure parameters types module by building the configuration
        Then the parameters types module configuration should contain "infrastructure-module:database:host" with value "localhost"
        And the parameters types module configuration should contain "infrastructure-module:api:allowed-origins:0" with value "http://localhost:3000"
        And the parameters types module configuration should contain "infrastructure-module:database:credentials:username" with value "testuser"
        And the parameters types module configuration should contain "infrastructure-module:database:credentials:password" with value "testpass"
        And the parameters types module should handle mixed types with JSON processing correctly

    @ParameterStore @Types @FlexConfig
    Scenario: Access different parameter types through FlexConfig dynamic interface
        Given I have established a parameters types module environment
        And I have parameters types module configuration with mixed parameter types from "infrastructure-module-aws-config.json"
        When I configure parameters types module by building the configuration
        And I verify parameters types module dynamic access capabilities
        Then the parameters types module FlexConfig should provide dynamic access to "infrastructure-module.database.host"
        And the parameters types module FlexConfig should provide dynamic access to "infrastructure-module.api.allowed-origins.0"
        And the parameters types module should provide dynamic access to different types

    @ParameterStore @Types @Optional
    Scenario: Handle optional parameter types with missing parameters
        Given I have established a parameters types module environment
        And I have parameters types module configuration with optional parameter types from "infrastructure-module-aws-config.json"
        When I configure parameters types module by building the configuration
        Then the parameters types module configuration should be built successfully
        And the parameters types module should handle optional parameters gracefully