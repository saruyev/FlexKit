Feature: YAML Configuration
  As a developer using FlexKit Configuration with YAML provider
  I want to configure my application using YAML configuration sources
  So that I can manage application settings in a human-readable format

  Background:
    Given I have established a configuration module environment

  Scenario: Build basic YAML configuration
    Given I have configured a configuration module with base settings
    When I configure the module by building the configuration
    Then the configuration module should be built successfully
    And the configuration module should contain key "Application:Name" with value "TestApp"
    And the configuration module should contain key "Application:Version" with value "1.0.0"
    And the configuration module should contain key "Server:Host" with value "localhost"
    And the configuration module should contain key "Server:Port" with value "8080"

  Scenario: Load YAML configuration with complex content
    When I configure the module with additional YAML content:
      """
      Application:
        Name: "FlexKit YAML Test App"
        Version: "1.2.3"
      Database:
        ConnectionString: "Server=localhost;Database=YamlTestDb;"
        Timeout: 30
        Pool:
          Min: 5
          Max: 20
      """
    And I configure the module by building the configuration
    Then the configuration module should be built successfully
    And the configuration module should contain key "Application:Name" with value "FlexKit YAML Test App"
    And the configuration module should contain key "Database:ConnectionString" with value "Server=localhost;Database=YamlTestDb;"
    And the configuration module should contain key "Database:Pool:Min" with value "5"

  Scenario: Combine multiple YAML configuration sources with precedence
    Given I have configured a configuration module with multiple YAML sources
    When I configure the module by building the configuration
    Then the configuration module should be built successfully
    And the configuration module should have "2" configuration sources
    And the configuration module should have precedence where later sources override earlier ones

  Scenario: Add additional YAML content to existing configuration
    Given I have configured a configuration module with base settings
    When I configure the module with additional YAML content:
      """
      Additional:
        Setting1: "AdditionalValue1"
        Setting2: "AdditionalValue2"
      Features:
        NewFeature: true
        BetaFeature: false
      """
    And I configure the module by building the configuration
    Then the configuration module should be built successfully
    And the configuration module should contain key "Additional:Setting1" with value "AdditionalValue1"
    And the configuration module should contain key "Features:NewFeature" with value "True"
    And the configuration module should contain key "Application:Name" with value "TestApp"

  Scenario: Configure YAML with hierarchical data structure
    When I configure the module with hierarchical data:
      | Key                           | Value                        |
      | Database:Primary:Host         | primary.db.com               |
      | Database:Primary:Port         | 5432                         |
      | Database:Secondary:Host       | secondary.db.com             |
      | Database:Secondary:Port       | 5433                         |
      | Api:External:Payment:BaseUrl  | https://payment.api.com      |
      | Api:External:Payment:Key      | payment-key-123              |
      | Api:External:Shipping:BaseUrl | https://shipping.api.com     |
      | Api:External:Shipping:Key     | shipping-key-456             |
    And I configure the module by building the configuration
    Then the configuration module should be built successfully
    And the configuration module should contain key "Database:Primary:Host" with value "primary.db.com"
    And the configuration module should contain key "Api:External:Payment:BaseUrl" with value "https://payment.api.com"
    And the configuration module should contain key "Api:External:Shipping:Key" with value "shipping-key-456"

  Scenario: Handle invalid YAML content gracefully
    When I configure the module with invalid YAML content:
      """
      invalid_yaml:
        - missing_closing_bracket: [
        unmatched: "quotes
      """
    Then the configuration module should throw an exception

  Scenario: Clear and rebuild configuration with new sources
    Given I have configured a configuration module with base settings
    When I configure the module using clear and rebuild
    And I configure the module by building the configuration
    Then the configuration module should be built successfully
    And the configuration module after clear should only contain new data

  Scenario: Support FlexConfig dynamic access patterns
    Given I have configured a configuration module with base settings
    When I configure the module by building the configuration
    Then the configuration module should be built successfully
    And the configuration module should support FlexConfig dynamic access
    And the configuration module should provide access to the underlying IConfiguration

  Scenario: Handle empty YAML files
    When I configure the module with additional YAML content:
      """
      # This is just a comment
      # No actual configuration data
      """
    And I configure the module by building the configuration
    Then the configuration module should be built successfully
    And the configuration module should handle empty YAML files gracefully

  Scenario Outline: Environment-specific configuration overrides
    Given I have configured a configuration module with base settings
    When I configure the module with environment-specific overrides for "<Environment>"
    And I configure the module by building the configuration
    Then the configuration module should be built successfully
    And the configuration module should reflect environment-specific values for "<Environment>"

    Examples:
      | Environment |
      | Development |
      | Staging     |
      | Production  |

  Scenario: Complex YAML structure with arrays and nested objects
    When I configure the module with additional YAML content:
      """
      Servers:
        - Name: "WebServer1"
          Host: "web1.example.com"
          Port: 80
          Features:
            - "SSL"
            - "LoadBalancing"
        - Name: "WebServer2"
          Host: "web2.example.com"
          Port: 443
          Features:
            - "SSL"
            - "Caching"
      ConnectionStrings:
        Default: "Server=localhost;Database=App;"
        ReadOnly: "Server=readonly.db.com;Database=App;"
      AppSettings:
        Theme: "Dark"
        Language: "en-US"
        MaxUsers: 1000
        EnableFeatures:
          Authentication: true
          Logging: true
          Metrics: false
      """
    And I configure the module by building the configuration
    Then the configuration module should be built successfully
    And the configuration module should contain key "Servers:0:Name" with value "WebServer1"
    And the configuration module should contain key "Servers:0:Features:0" with value "SSL"
    And the configuration module should contain key "Servers:1:Host" with value "web2.example.com"
    And the configuration module should contain key "ConnectionStrings:Default" with value "Server=localhost;Database=App;"
    And the configuration module should contain key "AppSettings:MaxUsers" with value "1000"
    And the configuration module should contain key "AppSettings:EnableFeatures:Authentication" with value "True"