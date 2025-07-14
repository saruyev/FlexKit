Feature: Secrets Manager JSON Processing
As a developer using AWS Secrets Manager with JSON configuration data
I want to automatically process JSON secrets into hierarchical configuration keys
So that I can access complex configuration structures using dot notation and dynamic syntax

    @SecretsManager @JsonProcessing @ComplexData
    Scenario: Process JSON secret with nested objects
        Given I have established a secrets json processor environment
        And I have secrets json processor configuration with JSON processing from "infrastructure-module-complex-parameters.json"
        When I configure secrets json processor by building the configuration
        Then the secrets json processor configuration should contain "infrastructure-module:app:config:database:host" with value "complex-db.example.com"
        And the secrets json processor configuration should contain "infrastructure-module:app:config:database:port" with value "5432"
        And the secrets json processor configuration should contain "infrastructure-module:app:config:database:ssl" with value "true"
        And the secrets json processor configuration should contain "infrastructure-module:app:config:cache:type" with value "redis"

    @SecretsManager @JsonProcessing @NestedArrays
    Scenario: Process JSON secret with nested arrays and complex structures
        Given I have established a secrets json processor environment
        And I have secrets json processor configuration with JSON processing from "infrastructure-module-complex-parameters.json"
        When I configure secrets json processor by building the configuration
        Then the secrets json processor configuration should contain "infrastructure-module:app:config:database:pool:min" with value "5"
        And the secrets json processor configuration should contain "infrastructure-module:app:config:database:pool:max" with value "20"
        And the secrets json processor configuration should contain "infrastructure-module:app:config:database:features:read_replicas:0" with value "replica1.example.com"
        And the secrets json processor configuration should contain "infrastructure-module:app:config:database:features:read_replicas:1" with value "replica2.example.com"

    @SecretsManager @JsonProcessing @CacheConfiguration
    Scenario: Process JSON secret with cache cluster configuration
        Given I have established a secrets json processor environment
        And I have secrets json processor configuration with JSON processing from "infrastructure-module-complex-parameters.json"
        When I configure secrets json processor by building the configuration
        Then the secrets json processor configuration should contain "infrastructure-module:app:config:cache:cluster:nodes:0:host" with value "cache1.example.com"
        And the secrets json processor configuration should contain "infrastructure-module:app:config:cache:cluster:nodes:0:port" with value "6379"
        And the secrets json processor configuration should contain "infrastructure-module:app:config:cache:cluster:nodes:1:host" with value "cache2.example.com"
        And the secrets json processor configuration should contain "infrastructure-module:app:config:cache:cluster:sharding" with value "true"

    @SecretsManager @JsonProcessing @MicroservicesConfig
    Scenario: Process JSON secret with microservices configuration arrays
        Given I have established a secrets json processor environment
        And I have secrets json processor configuration with JSON processing from "infrastructure-module-complex-parameters.json"
        When I configure secrets json processor by building the configuration
        Then the secrets json processor configuration should contain "infrastructure-module:services:config:microservices:0:name" with value "user-service"
        And the secrets json processor configuration should contain "infrastructure-module:services:config:microservices:0:replicas" with value "3"
        And the secrets json processor configuration should contain "infrastructure-module:services:config:microservices:0:resources:cpu" with value "500m"
        And the secrets json processor configuration should contain "infrastructure-module:services:config:microservices:1:name" with value "order-service"
        And the secrets json processor configuration should contain "infrastructure-module:services:config:microservices:1:endpoints:0:path" with value "/api/orders"

    @SecretsManager @JsonProcessing @MonitoringConfig
    Scenario: Process JSON secret with monitoring and logging configuration
        Given I have established a secrets json processor environment
        And I have secrets json processor configuration with JSON processing from "infrastructure-module-complex-parameters.json"
        When I configure secrets json processor by building the configuration
        Then the secrets json processor configuration should contain "infrastructure-module:app:config:monitoring:metrics:enabled" with value "true"
        And the secrets json processor configuration should contain "infrastructure-module:app:config:monitoring:metrics:providers:0" with value "prometheus"
        And the secrets json processor configuration should contain "infrastructure-module:app:config:monitoring:metrics:providers:1" with value "datadog"
        And the secrets json processor configuration should contain "infrastructure-module:app:config:monitoring:metrics:custom_tags:environment" with value "test"
        And the secrets json processor configuration should contain "infrastructure-module:app:config:monitoring:logging:outputs:2" with value "elk"

    @SecretsManager @JsonProcessing @DynamicAccess
    Scenario: Access JSON-processed secrets through FlexConfig dynamic syntax
        Given I have established a secrets json processor environment
        And I have secrets json processor configuration with JSON processing from "infrastructure-module-complex-parameters.json"
        When I configure secrets json processor by building the configuration
        And I create secrets json processor FlexConfig from the configuration
        Then the secrets json processor FlexConfig should support dynamic property access for "infrastructure-module.app.config.database.host"
        And the secrets json processor FlexConfig should support indexer access for "infrastructure-module:app:config:database:port"
        And the secrets json processor FlexConfig should handle missing secrets gracefully with default values
        And the secrets json processor FlexConfig should provide typed access to JSON-processed secret values

    @SecretsManager @JsonProcessing @ErrorHandling
    Scenario: Handle mixed JSON and non-JSON secrets gracefully
        Given I have established a secrets json processor environment
        And I have secrets json processor configuration with mixed secret types from "infrastructure-module-aws-config.json"
        When I configure secrets json processor by building the configuration
        Then the secrets json processor should process JSON secrets into hierarchical keys
        And the secrets json processor should keep non-JSON secrets as plain string values
        And the secrets json processor configuration should contain valid configuration data

    @SecretsManager @JsonProcessing @OptionalProcessing
    Scenario: Handle optional JSON processing with fallback behavior
        Given I have established a secrets json processor environment
        And I have secrets json processor configuration with optional JSON processing from "infrastructure-module-aws-config.json"
        When I configure secrets json processor by building the configuration
        Then the secrets json processor should complete loading without errors
        And the secrets json processor should apply JSON processing where enabled
        And the secrets json processor should skip JSON processing for excluded secrets
        And the secrets json processor configuration should provide consistent access patterns

    @SecretsManager @JsonProcessing @ComplexHierarchy
    Scenario: Process deeply nested JSON secrets with complex hierarchy
        Given I have established a secrets json processor environment
        And I have secrets json processor configuration with deep nesting from "infrastructure-module-complex-parameters.json"
        When I configure secrets json processor by building the configuration
        Then the secrets json processor configuration should handle deep nesting correctly
        And the secrets json processor configuration should contain "infrastructure-module:app:config:database:pool:max" with value "20"
        And the secrets json processor configuration should contain "infrastructure-module:app:config:database:pool:timeout" with value "30000"
        And the secrets json processor configuration should flatten complex structures into accessible keys