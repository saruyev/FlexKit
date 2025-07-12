Feature: Parameter Store Basic Loading
As a developer using AWS Parameter Store
I want to load configuration parameters from Parameter Store
So that I can use centralized configuration in my applications

    @ParameterStore @BasicLoading
    Scenario: Load simple string parameters from Parameter Store
        Given I have established a parameters basic module environment
        And I have parameters basic module configuration from "infrastructure-module-aws-config.json"
        When I configure parameters basic module by building the configuration
        Then the parameters basic module configuration should contain "infrastructure-module:database:host" with value "localhost"
        And the parameters basic module configuration should contain "infrastructure-module:database:port" with value "5432"

    @ParameterStore @BasicLoading @JsonProcessing
    Scenario: Load parameters with JSON processing enabled
        Given I have established a parameters basic module environment
        And I have parameters basic module configuration with JSON processing from "infrastructure-module-complex-parameters.json"
        When I configure parameters basic module by building the configuration
        Then the parameters basic module configuration should contain "infrastructure-module:app:config:database:host" with value "complex-db.example.com"
        And the parameters basic module configuration should contain "infrastructure-module:app:config:database:port" with value "5432"

    @ParameterStore @BasicLoading @StringList
    Scenario: Load StringList parameters from Parameter Store
        Given I have established a parameters basic module environment
        And I have parameters basic module configuration with StringList from "infrastructure-module-aws-config.json"
        When I configure parameters basic module by building the configuration
        Then the parameters basic module configuration should contain "infrastructure-module:api:allowed-origins:0" with value "http://localhost:3000"
        And the parameters basic module configuration should contain "infrastructure-module:api:allowed-origins:1" with value "https://test.example.com"

    @ParameterStore @BasicLoading @Optional
    Scenario: Load optional parameters with missing path
        Given I have established a parameters basic module environment
        And I have parameters basic module configuration with missing path as optional from "infrastructure-module-minimal-config.json"
        When I configure parameters basic module by building the configuration
        Then the parameters basic module configuration should be built successfully

    @ParameterStore @BasicLoading @ErrorHandling
    Scenario: Handle invalid configuration
        Given I have established a parameters basic module environment
        And I have parameters basic module configuration with missing path as required from "infrastructure-module-invalid-config.json"
        When I configure parameters basic module by building the configuration
        Then the parameters basic module configuration should be built successfully

    @ParameterStore @BasicLoading @FlexConfig
    Scenario: Access parameters through FlexConfig dynamic interface
        Given I have established a parameters basic module environment
        And I have parameters basic module configuration from "infrastructure-module-aws-config.json"
        When I configure parameters basic module by building the configuration
        And I verify parameters basic module dynamic access capabilities
        Then the parameters basic module FlexConfig should provide dynamic access to "infrastructure-module.database.host"