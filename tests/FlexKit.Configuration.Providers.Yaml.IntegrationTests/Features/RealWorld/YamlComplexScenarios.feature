Feature: YAML Complex Scenarios
  As a developer working with complex YAML configurations
  I want to handle real-world scenarios including microservices, multi-environment deployments, and advanced YAML features
  So that I can manage sophisticated configuration requirements with FlexKit YAML provider

Background:
  Given I have established a complex scenario module environment

@YamlComplexScenarios @MicroservicesConfiguration
Scenario: Deploy microservices configuration with service discovery
  Given I have a complex scenario module with microservices configuration from "microservices.yaml"
  When I deploy complex scenario module configuration
  And I validate complex scenario module service discovery configuration
  Then the complex scenario module should deploy successfully
  And the complex scenario module should support microservices service discovery

@YamlComplexScenarios @MultiEnvironmentConfiguration
Scenario: Handle multi-environment deployment with YAML anchors and aliases
  Given I have a complex scenario module with multi-environment setup from "multi-environment.yaml"
  When I deploy complex scenario module configuration
  And I validate complex scenario module environment-specific deployments
  Then the complex scenario module should deploy successfully
  And the complex scenario module should support environment-specific configurations
  And the complex scenario module should handle YAML anchors and aliases

@YamlComplexScenarios @DeepNestingNavigation
Scenario: Navigate extremely deep nesting structures (20 levels)
  Given I have a complex scenario module with deep nesting configuration from "deep-nesting.yaml"
  When I deploy complex scenario module configuration
  And I validate complex scenario module deep nesting navigation
  Then the complex scenario module should deploy successfully
  And the complex scenario module should navigate deep nesting structures

@YamlComplexScenarios @SecurityComplianceConfiguration
Scenario: Load comprehensive security and compliance configuration
  Given I have a complex scenario module with security compliance configuration from "security-compliance.yaml"
  When I deploy complex scenario module configuration
  And I validate complex scenario module security and compliance features
  Then the complex scenario module should deploy successfully
  And the complex scenario module should support security and compliance standards

@YamlComplexScenarios @LayeredConfigurationSources
Scenario: Combine multiple YAML sources with proper precedence
  Given I have a complex scenario module with layered configuration sources
  When I deploy complex scenario module configuration
  Then the complex scenario module should deploy successfully
  And the complex scenario module should have loaded from multiple sources
  And the complex scenario module should provide FlexConfig dynamic access to all features

@YamlComplexScenarios @ErrorHandling
Scenario: Handle invalid YAML syntax gracefully
  When I establish complex YAML configuration from invalid file "invalid-syntax.yaml"
  Then the complex scenario module should fail with YAML parsing error

@YamlComplexScenarios @ErrorHandling
Scenario: Handle invalid YAML indentation gracefully
  When I establish complex YAML configuration from invalid file "invalid-indentation.yaml"
  Then the complex scenario module should fail with YAML parsing error

@YamlComplexScenarios @MixedDataTypes
Scenario: Process mixed data types in YAML configuration
  When I establish complex scenario module with mixed data types from "mixed-data-types.yaml"
  Then the complex scenario module should deploy successfully
  And the complex scenario module should handle mixed data types correctly

@YamlComplexScenarios @NumericFormats
Scenario: Handle various numeric formats and quoted values
  When I establish complex scenario module with numeric formats from "numeric-formats.yaml"
  Then the complex scenario module should deploy successfully
  And the complex scenario module should handle numeric formats correctly

@YamlComplexScenarios @SpecialCharacters
Scenario: Process special characters and Unicode content
  When I establish complex scenario module with special characters from "special-characters.yaml"
  Then the complex scenario module should deploy successfully
  And the complex scenario module should handle special characters and Unicode

@YamlComplexScenarios @YamlAnchorsAliases
Scenario: Resolve YAML anchors and aliases for configuration reuse
  When I establish complex scenario module with YAML anchors from "anchors-aliases.yaml"
  Then the complex scenario module should deploy successfully
  And the complex scenario module should handle YAML anchors and aliases

@YamlComplexScenarios @MultiLineStrings
Scenario: Handle literal and folded multi-line strings
  When I establish complex scenario module with literal strings from "literal-strings.yaml"
  And I establish complex scenario module with folded strings from "folded-strings.yaml"
  Then the complex scenario module should deploy successfully
  And the complex scenario module should handle multi-line strings correctly

@YamlComplexScenarios @BooleanNullVariations
Scenario: Process various boolean and null value representations
  When I establish complex scenario module with boolean and null values from "boolean-null.yaml"
  Then the complex scenario module should deploy successfully
  And the complex scenario module should handle boolean and null variations

@YamlComplexScenarios @EmptyFiles
Scenario: Handle empty YAML files gracefully
  When I establish complex scenario module with mixed data types from "empty-file.yaml"
  Then the complex scenario module should deploy successfully

@YamlComplexScenarios @DuplicateKeys
Scenario: Handle duplicate keys by taking the last value
  When I establish complex scenario module with mixed data types from "duplicate-keys.yaml"
  Then the complex scenario module should deploy successfully

@YamlComplexScenarios @RealWorldIntegration
Scenario: Real-world enterprise configuration scenario
  Given I have a complex scenario module with microservices configuration from "microservices.yaml"
  And I have a complex scenario module with multi-environment setup from "multi-environment.yaml"
  And I have a complex scenario module with security compliance configuration from "security-compliance.yaml"
  When I deploy complex scenario module configuration
  And I validate complex scenario module service discovery configuration
  And I validate complex scenario module environment-specific deployments
  And I validate complex scenario module security and compliance features
  Then the complex scenario module should deploy successfully
  And the complex scenario module should support microservices service discovery
  And the complex scenario module should support environment-specific configurations
  And the complex scenario module should support security and compliance standards
  And the complex scenario module should provide FlexConfig dynamic access to all features