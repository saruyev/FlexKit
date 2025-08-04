Feature: Secrets Manager Basic Loading
As a developer using AWS Secrets Manager
I want to load configuration secrets from Secrets Manager
So that I can use centralized secure configuration in my applications

    @SecretsManager @BasicLoading
    Scenario: Load simple string secrets from Secrets Manager
        Given I have established a secrets module environment
        And I have secrets module configuration from "infrastructure-module-aws-config.json"
        When I configure secrets module by building the configuration
        Then the secrets module configuration should contain "infrastructure-module-database-credentials" with value "{"host":"db.example.com","port":5432,"username":"dbuser","password":"dbpass123"}"
        And the secrets module configuration should contain "infrastructure-module-api-keys" with value "{"external_api_key":"ext-api-123","payment_gateway_key":"pay-gw-456"}"

    @SecretsManager @BasicLoading @JsonProcessing
    Scenario: Load secrets with JSON processing enabled
        Given I have established a secrets module environment
        And I have secrets module configuration with JSON processing from "infrastructure-module-aws-config.json"
        When I configure secrets module by building the configuration
        Then the secrets module configuration should contain "infrastructure-module-database-credentials:host" with value "db.example.com"
        And the secrets module configuration should contain "infrastructure-module-database-credentials:port" with value "5432"
        And the secrets module configuration should contain "infrastructure-module-api-keys:external_api_key" with value "ext-api-123"

    @SecretsManager @BasicLoading @BinarySecrets
    Scenario: Load binary secrets from Secrets Manager
        Given I have established a secrets module environment
        And I have secrets module configuration with binary secrets from "infrastructure-module-aws-config.json"
        When I configure secrets module by building the configuration
        Then the secrets module configuration should contain "infrastructure-module-certificates" with base64 encoded value

    @SecretsManager @BasicLoading @Optional
    Scenario: Load optional secrets with missing secret
        Given I have established a secrets module environment
        And I have secrets module configuration with missing secret as optional from "infrastructure-module-minimal-config.json"
        When I configure secrets module by building the configuration
        Then the secrets module configuration should be built successfully

    @SecretsManager @BasicLoading @ErrorHandling
    Scenario: Handle invalid configuration
        Given I have established a secrets module environment
        And I have secrets module configuration with missing secret as required from "infrastructure-module-invalid-config.json"
        When I configure secrets module by building the configuration
        Then the secrets module configuration should be built successfully

    @SecretsManager @BasicLoading @FlexConfig
    Scenario: Access secrets through FlexConfig dynamic interface
        Given I have established a secrets module environment
        And I have secrets module configuration from "infrastructure-module-aws-config.json"
        When I configure secrets module by building the configuration
        And I verify secrets module dynamic access capabilities
        Then the secrets module FlexConfig should provide dynamic access to "infrastructure-module-database-credentials"