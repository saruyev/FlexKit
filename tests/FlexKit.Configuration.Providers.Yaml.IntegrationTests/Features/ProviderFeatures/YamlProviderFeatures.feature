Feature: YAML Provider Features
  As a developer using FlexKit Configuration with YAML provider
  I want to utilize advanced YAML provider features including arrays, data types, and YAML-specific capabilities
  So that I can leverage the full power of YAML configuration format with FlexKit

Background:
  Given I have established a provider features environment

@YamlProviderFeatures @ArrayNavigation
Scenario: Navigate complex arrays and nested objects
  Given I have a provider features configuration with complex arrays and objects
  When I configure provider features by building the configuration
  And I verify provider features array access capabilities
  Then the provider features should be configured successfully
  And the provider features should support complex array navigation
  And the provider features should provide FlexConfig integration

@YamlProviderFeatures @DataTypes
Scenario: Handle mixed data types in YAML configuration
  Given I have a provider features configuration with mixed data types from "mixed-data-types.yaml"
  When I configure provider features by building the configuration
  And I verify provider features data type handling
  Then the provider features should be configured successfully
  And the provider features should handle all YAML data types correctly

@YamlProviderFeatures @NumericFormats
Scenario: Process various numeric formats and quoted values
  Given I have a provider features configuration from file "numeric-formats.yaml"
  When I configure provider features by building the configuration
  And I verify provider features numeric format processing
  Then the provider features should be configured successfully
  And the provider features should process numeric formats accurately

@YamlProviderFeatures @BooleanNullVariations
Scenario: Handle boolean and null value variations
  Given I have a provider features configuration from file "boolean-null.yaml"
  When I configure provider features by building the configuration
  And I verify provider features boolean and null processing
  Then the provider features should be configured successfully
  And the provider features should handle boolean variations correctly
  And the provider features should handle null variations correctly

@YamlProviderFeatures @MultiLineStrings
Scenario: Support literal and folded multi-line strings
  Given I have a provider features configuration from file "literal-strings.yaml"
  When I configure provider features by building the configuration
  And I verify provider features multi-line string handling
  Then the provider features should be configured successfully
  And the provider features should support multi-line string formats

@YamlProviderFeatures @MultiLineStrings
Scenario: Support folded multi-line strings
  Given I have a provider features configuration from file "folded-strings.yaml"
  When I configure provider features by building the configuration
  And I verify provider features multi-line string handling
  Then the provider features should be configured successfully
  And the provider features should support multi-line string formats

@YamlProviderFeatures @YamlAnchorsAliases
Scenario: Resolve YAML anchors and aliases for configuration reuse
  Given I have a provider features configuration with anchors and aliases from "anchors-aliases.yaml"
  When I configure provider features by building the configuration
  And I verify provider features YAML anchor resolution
  Then the provider features should be configured successfully
  And the provider features should resolve YAML anchors and aliases

@YamlProviderFeatures @SpecialCharacters
Scenario: Handle special characters and Unicode content
  Given I have a provider features configuration from file "special-characters.yaml"
  When I configure provider features by building the configuration
  And I verify provider features special character support
  Then the provider features should be configured successfully
  And the provider features should handle special characters and Unicode

@YamlProviderFeatures @DeepNesting
Scenario: Navigate extremely deep nesting structures
  Given I have a provider features configuration from file "deep-nesting.yaml"
  When I configure provider features by building the configuration
  And I verify provider features deep nesting navigation
  Then the provider features should be configured successfully
  And the provider features should navigate deep nesting structures

@YamlProviderFeatures @ErrorHandling
Scenario: Handle invalid YAML syntax gracefully
  When I attempt provider features configuration with invalid YAML from "invalid-syntax.yaml"
  Then the provider features should fail with YAML parsing error

@YamlProviderFeatures @ErrorHandling  
Scenario: Handle invalid YAML indentation gracefully
  When I attempt provider features configuration with invalid YAML from "invalid-indentation.yaml"
  Then the provider features should fail with YAML parsing error

@YamlProviderFeatures @EmptyFiles
Scenario: Handle empty YAML files gracefully
  Given I have a provider features configuration from file "empty-file.yaml"
  When I configure provider features by building the configuration
  Then the provider features should be configured successfully
  And the provider features should support empty file handling

@YamlProviderFeatures @DuplicateKeys
Scenario: Handle duplicate keys by taking the last value
  Given I have a provider features configuration from file "duplicate-keys.yaml"
  When I configure provider features by building the configuration
  Then the provider features should be configured successfully
  And the provider features should handle duplicate keys by taking last value

@YamlProviderFeatures @ComprehensiveFeatureDemo
Scenario: Demonstrate comprehensive YAML provider feature integration
  Given I have a provider features configuration with mixed data types from "mixed-data-types.yaml"
  And I have a provider features configuration with anchors and aliases from "anchors-aliases.yaml"
  And I have a provider features configuration from file "numeric-formats.yaml"
  When I configure provider features by building the configuration
  And I verify provider features data type handling
  And I verify provider features YAML anchor resolution
  And I verify provider features numeric format processing
  Then the provider features should be configured successfully
  And the provider features should handle all YAML data types correctly
  And the provider features should resolve YAML anchors and aliases
  And the provider features should process numeric formats accurately
  And the provider features should provide FlexConfig integration