Feature: Secrets Manager Versions
As a developer using AWS Secrets Manager with secret versioning
I want to retrieve specific versions of secrets using version stages
So that I can manage secret rotation, rollback scenarios, and testing different secret versions

    @SecretsManager @Versions @CurrentVersion
    Scenario: Load secrets from current version stage
        Given I have established a secrets versions module environment
        And I have secrets versions module configuration with current version from "infrastructure-module-aws-config.json"
        When I configure secrets versions module by building the configuration
        Then the secrets versions module configuration should contain "infrastructure-module-database-credentials" with JSON value containing "db.example.com"
        And the secrets versions module configuration should be built successfully

    @SecretsManager @Versions @PendingVersion
    Scenario: Load secrets from pending version stage during rotation
        Given I have established a secrets versions module environment
        And I have secrets versions module configuration with pending version from "infrastructure-module-aws-config.json"
        When I configure secrets versions module by building the configuration
        Then the secrets versions module configuration should contain "infrastructure-module-database-credentials" with pending version value
        And the secrets versions module configuration should be built successfully

    @SecretsManager @Versions @PreviousVersion
    Scenario: Load secrets from previous version stage for rollback
        Given I have established a secrets versions module environment
        And I have secrets versions module configuration with previous version from "infrastructure-module-aws-config.json"
        When I configure secrets versions module by building the configuration
        Then the secrets versions module configuration should contain "infrastructure-module-database-credentials" with previous version value
        And the secrets versions module configuration should be built successfully

    @SecretsManager @Versions @CustomVersion
    Scenario: Load secrets from custom version stage
        Given I have established a secrets versions module environment
        And I have secrets versions module configuration with custom version stage "STAGING" from "infrastructure-module-aws-config.json"
        When I configure secrets versions module by building the configuration
        Then the secrets versions module configuration should contain "infrastructure-module-database-credentials" with custom version value
        And the secrets versions module configuration should be built successfully

    @SecretsManager @Versions @JsonProcessing
    Scenario: Load and process JSON secrets from specific version stage
        Given I have established a secrets versions module environment
        And I have secrets versions module configuration with current version and JSON processing from "infrastructure-module-aws-config.json"
        When I configure secrets versions module by building the configuration
        Then the secrets versions module configuration should contain "infrastructure-module-database-credentials:host" with value "db.example.com"
        And the secrets versions module configuration should contain "infrastructure-module-database-credentials:port" with value "5432"
        And the secrets versions module configuration should contain "infrastructure-module-database-credentials:username" with value "dbuser"
        And the secrets versions module should handle JSON processing correctly for versioned secrets

    @SecretsManager @Versions @VersionComparison
    Scenario: Compare configuration values across different version stages
        Given I have established a secrets versions module environment
        And I have secrets versions module configuration with current version from "infrastructure-module-aws-config.json"
        When I configure secrets versions module by building the configuration
        And I verify secrets versions module dynamic access capabilities
        Then the secrets versions module FlexConfig should provide dynamic access to "infrastructure-module-database-credentials"
        And the secrets versions module should support version-aware configuration access

    @SecretsManager @Versions @ErrorHandling
    Scenario: Handle missing version stage gracefully
        Given I have established a secrets versions module environment
        And I have secrets versions module configuration with missing version stage as optional from "infrastructure-module-aws-config.json"
        When I configure secrets versions module by building the configuration
        Then the secrets versions module configuration should be built successfully
        And the secrets versions module should handle missing version stages gracefully

    @SecretsManager @Versions @RequiredVersion
    Scenario: Handle missing required version stage
        Given I have established a secrets versions module environment
        And I have secrets versions module configuration with missing version stage as required from "infrastructure-module-aws-config.json"
        When I configure secrets versions module by building the configuration
        Then the secrets versions module configuration should fail to build
        And the secrets versions module should have configuration exception for missing version

    @SecretsManager @Versions @MultipleVersions
    Scenario: Load multiple secrets with different version stages
        Given I have established a secrets versions module environment
        And I have secrets versions module configuration with mixed version stages from "infrastructure-module-aws-config.json"
        When I configure secrets versions module by building the configuration
        Then the secrets versions module configuration should contain secrets from current version
        And the secrets versions module configuration should contain secrets from pending version
        And the secrets versions module configuration should be built successfully
