Feature: Key Vault Basic Operations
As a developer using FlexKit with Azure Key Vault
I want to load secrets from Key Vault into FlexKit configuration
So that I can access secure configuration data dynamically

    @KeyVault @BasicOperations @FlexKit
    Scenario: Load simple secrets from Key Vault
        Given I have established a key vault controller environment
        And I have key vault controller configuration with Key Vault from "infrastructure-module-azure-config.json"
        When I configure key vault controller by building the configuration
        Then the key vault controller should support FlexKit dynamic access patterns
        And the key vault controller configuration should contain "myapp:database:host" with value "localhost"
        And the key vault controller should demonstrate FlexKit type conversion capabilities

    @KeyVault @HierarchicalSecrets @FlexKit
    Scenario: Load hierarchical secrets from Key Vault
        Given I have established a key vault controller environment
        And I have key vault controller configuration with hierarchical Key Vault from "infrastructure-module-azure-config.json"
        When I configure key vault controller by building the configuration
        Then the key vault controller should support hierarchical secret access
        And the key vault controller configuration should contain "myapp:features:cache:enabled" with value "true"
        And the key vault controller configuration should contain "myapp:features:cache:ttl" with value "300"

    @KeyVault @JSONProcessing @FlexKit
    Scenario: Load JSON secrets from Key Vault with processing
        Given I have established a key vault controller environment
        And I have key vault controller configuration with JSON-enabled Key Vault from "infrastructure-module-azure-config.json"
        When I configure key vault controller by building the configuration
        Then the key vault controller should support JSON secret flattening
        And the key vault controller configuration should contain "database-config:host" with value "db.example.com"
        And the key vault controller configuration should contain "database-config:port" with value "5432"
        And the key vault controller configuration should contain "database-config:ssl" with value "True"

    @KeyVault @OptionalSecrets @ErrorHandling
    Scenario: Handle optional Key Vault configuration gracefully
        Given I have established a key vault controller environment
        And I have key vault controller configuration with optional Key Vault from "infrastructure-module-azure-config.json"
        When I configure key vault controller with error tolerance by building the configuration
        Then the key vault controller should build successfully with error tolerance
        And the key vault controller should demonstrate FlexKit capabilities despite missing secrets