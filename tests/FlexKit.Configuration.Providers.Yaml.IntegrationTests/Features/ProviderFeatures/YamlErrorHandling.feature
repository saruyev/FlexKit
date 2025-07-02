Feature: YAML Error Handling
  As a developer using FlexKit Configuration with YAML provider
  I want robust error handling for invalid YAML files and edge cases
  So that my application can handle configuration errors gracefully and provide meaningful error messages

Background:
  Given I have established an error handling module environment

@YamlErrorHandling @InvalidSyntax
Scenario: Handle invalid YAML syntax gracefully
  When I attempt error handling with invalid YAML from file "invalid-syntax.yaml"
  Then the error handling module should fail with YAML parsing error

@YamlErrorHandling @InvalidIndentation
Scenario: Handle invalid YAML indentation gracefully
  When I attempt error handling with invalid YAML from file "invalid-indentation.yaml"
  Then the error handling module should fail with YAML parsing error

@YamlErrorHandling @MissingRequiredFile
Scenario: Handle missing required YAML files
  When I attempt error handling with missing required file "nonexistent-required.yaml"
  Then the error handling module should fail with file not found error

@YamlErrorHandling @MissingOptionalFile
Scenario: Handle missing optional YAML files gracefully
  When I attempt error handling with missing optional file "nonexistent-optional.yaml"
  Then the error handling module should handle missing optional files gracefully

@YamlErrorHandling @EmptyFile
Scenario: Handle empty YAML files gracefully
  Given I have an error handling module with valid YAML from file "empty-file.yaml"
  When I process error handling module configuration
  And I verify error handling module empty file handling
  Then the error handling module should be configured successfully
  And the error handling module should handle empty files correctly

@YamlErrorHandling @CorruptedContent
Scenario: Handle corrupted YAML content
  When I attempt error handling with corrupted YAML content:
    """
    database:
      host: localhost
      port: [unclosed array
        credentials:
          username: "unclosed string
    """
  Then the error handling module should fail with YAML parsing error

@YamlErrorHandling @MalformedStructure
Scenario: Handle malformed YAML structure
  When I attempt error handling with corrupted YAML content:
    """
    key1: value1
      key2: value2
    key3:
        key4: value4
      key5: value5
    """
  Then the error handling module should fail with YAML parsing error

@YamlErrorHandling @InvalidCharacters
Scenario: Handle YAML with potentially invalid characters
  When I attempt error handling with corrupted YAML content:
    """
    database:
      host: localhost
      credentials:
        password: "password with invalid \x00 null character"
        token: "token with \x01 control character"
    """
  Then the error handling module should either fail with YAML parsing error or succeed

@YamlErrorHandling @GracefulDegradation
Scenario: Support graceful degradation for missing configuration keys
  Given I have an error handling module with valid configuration:
    """
    database:
      host: localhost
      port: 5432
    features:
      - caching
      - logging
    """
  When I process error handling module configuration
  And I verify error handling module graceful degradation
  Then the error handling module should be configured successfully
  And the error handling module should support graceful degradation
  And the error handling module should maintain FlexConfig functionality

@YamlErrorHandling @DuplicateKeys
Scenario: Handle duplicate keys by taking last value
  Given I have an error handling module with valid YAML from file "duplicate-keys.yaml"
  When I process error handling module configuration
  Then the error handling module should be configured successfully

@YamlErrorHandling @LargeFileHandling
Scenario: Handle configuration errors in complex nested structures
  Given I have an error handling module with valid YAML from file "deep-nesting.yaml"
  When I process error handling module configuration
  And I verify error handling module graceful degradation
  Then the error handling module should be configured successfully
  And the error handling module should support graceful degradation

@YamlErrorHandling @SpecialCharacters
Scenario: Handle special characters and edge case values
  Given I have an error handling module with valid YAML from file "special-characters.yaml"
  When I process error handling module configuration
  And I verify error handling module graceful degradation
  Then the error handling module should be configured successfully
  And the error handling module should maintain FlexConfig functionality

@YamlErrorHandling @BooleanNullEdgeCases
Scenario: Handle boolean and null value edge cases
  Given I have an error handling module with valid YAML from file "boolean-null.yaml"
  When I process error handling module configuration
  And I verify error handling module graceful degradation
  Then the error handling module should be configured successfully
  And the error handling module should support graceful degradation

@YamlErrorHandling @NumericFormatsEdgeCases
Scenario: Handle numeric format edge cases and potential overflow
  Given I have an error handling module with valid YAML from file "numeric-formats.yaml"
  When I process error handling module configuration
  And I verify error handling module graceful degradation
  Then the error handling module should be configured successfully
  And the error handling module should maintain FlexConfig functionality

@YamlErrorHandling @RobustErrorRecovery
Scenario: Demonstrate robust error recovery and configuration validation
  Given I have an error handling module with valid configuration:
    """
    application:
      name: "Error Handling Test App"
      version: "1.0.0"
      debug: true
    database:
      host: localhost
      port: 5432
      retries: 3
    features:
      - authentication
      - authorization
      - logging
    """
  When I process error handling module configuration
  And I verify error handling module graceful degradation
  Then the error handling module should be configured successfully
  And the error handling module should support graceful degradation
  And the error handling module should maintain FlexConfig functionality