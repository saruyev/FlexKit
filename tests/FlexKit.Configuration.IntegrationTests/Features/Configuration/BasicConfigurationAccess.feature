Feature: Basic Configuration Access
    As a developer using FlexKit Configuration
    I want to access configuration values through different patterns
    So that I can choose the most appropriate access method for my scenario

    Background:
        Given I have a configuration source with the following data:
            | Key                           | Value                                      |
            | Application:Name              | FlexKit Test Application                   |
            | Application:Version           | 1.0.0                                      |
            | Application:Environment       | Test                                       |
            | Database:ConnectionString     | Server=localhost;Database=TestDb;          |
            | Database:CommandTimeout       | 30                                         |
            | Database:MaxRetryCount        | 3                                          |
            | Database:EnableLogging        | true                                       |
            | External:PaymentApi:BaseUrl   | https://api.payment.test.com               |
            | External:PaymentApi:ApiKey    | test-payment-key-12345                     |
            | External:PaymentApi:Timeout   | 5000                                       |
            | Features:EnableCaching        | true                                       |
            | Features:EnableMetrics        | false                                      |
            | Features:MaxUploadSize        | 10485760                                   |
            | Servers:0:Name                | Primary                                    |
            | Servers:0:Host                | primary.test.com                           |
            | Servers:0:Port                | 443                                        |
            | Servers:1:Name                | Secondary                                  |
            | Servers:1:Host                | secondary.test.com                         |
            | Servers:1:Port                | 80                                         |

    @ConfigurationAccess @BasicAccess
    Scenario: Access configuration using string indexer
        When I access the configuration using the string indexer with key "Application:Name"
        Then the returned value should be "FlexKit Test Application"
        When I access the configuration using the string indexer with key "Database:CommandTimeout"
        Then the returned value should be "30"
        When I access the configuration using the string indexer with key "Features:EnableCaching"
        Then the returned value should be "true"

    @ConfigurationAccess @BasicAccess
    Scenario: Access configuration using dynamic access
        When I access the configuration dynamically as "config.Application.Name"
        Then the dynamic result should be "FlexKit Test Application"
        When I access the configuration dynamically as "config.Database.ConnectionString"
        Then the dynamic result should be "Server=localhost;Database=TestDb;"
        When I access the configuration dynamically as "config.External.PaymentApi.BaseUrl"
        Then the dynamic result should be "https://api.payment.test.com"

    @ConfigurationAccess @BasicAccess
    Scenario: Access configuration using numeric indexer for arrays
        When I access the configuration section "Servers" using numeric indexer 0
        Then the server configuration should have Name "Primary"
        And the server configuration should have Host "primary.test.com"
        And the server configuration should have Port "443"
        When I access the configuration section "Servers" using numeric indexer 1
        Then the server configuration should have Name "Secondary"
        And the server configuration should have Host "secondary.test.com"
        And the server configuration should have Port "80"

    @ConfigurationAccess @BasicAccess
    Scenario: Access nested configuration sections
        When I get the current config for section "Database"
        Then the section should contain key "ConnectionString" with value "Server=localhost;Database=TestDb;"
        And the section should contain key "CommandTimeout" with value "30"
        And the section should contain key "EnableLogging" with value "true"
        When I get the current config for section "External"
        And I get the current config for section "PaymentApi" from the previous result
        Then the section should contain key "BaseUrl" with value "https://api.payment.test.com"
        And the section should contain key "ApiKey" with value "test-payment-key-12345"

    @ConfigurationAccess @BasicAccess
    Scenario: Handle non-existent configuration keys gracefully
        When I access the configuration using the string indexer with key "NonExistent:Key"
        Then the returned value should be null
        When I access the configuration dynamically as "config.NonExistent.Property"
        Then the dynamic result should be null
        When I access the configuration section "NonExistentSection" using numeric indexer 0
        Then the result should be null

    @ConfigurationAccess @BasicAccess
    Scenario: Access configuration with case insensitive keys
        When I access the configuration using the string indexer with key "application:name"
        Then the returned value should be "FlexKit Test Application"
        When I access the configuration using the string indexer with key "DATABASE:CONNECTIONSTRING"
        Then the returned value should be "Server=localhost;Database=TestDb;"
        When I get the current config for section "database"
        Then the section should contain key "ConnectionString" with value "Server=localhost;Database=TestDb;"

    @ConfigurationAccess @BasicAccess
    Scenario: Verify FlexConfiguration wraps IConfiguration correctly
        When I get the FlexConfiguration from IConfiguration
        Then the FlexConfiguration should not be null
        And the underlying IConfiguration should be accessible
        When I access the FlexConfiguration using string indexer with key "Application:Name"
        Then the returned value should be "FlexKit Test Application"
        When I access the FlexConfiguration dynamically as "config.Database.CommandTimeout"
        Then the dynamic result should be "30"

    @ConfigurationAccess @BasicAccess @ErrorHandling
    Scenario: Handle edge cases with special characters and empty values
        Given I have additional configuration data:
            | Key                     | Value                           |
            | Special:EmptyValue      |                                 |
            | Special:SpacesValue     | value with spaces               |
            | Special:SpecialChars    | value!@#$%^&*()                 |
            | Special:UnicodeValue    | Ñandú and 漢字                   |
        When I access the configuration using the string indexer with key "Special:EmptyValue"
        Then the returned value should be empty
        When I access the configuration using the string indexer with key "Special:SpacesValue"
        Then the returned value should be "value with spaces"
        When I access the configuration using the string indexer with key "Special:SpecialChars"
        Then the returned value should be "value!@#$%^&*()"
        When I access the configuration using the string indexer with key "Special:UnicodeValue"
        Then the returned value should be "Ñandú and 漢字"

    @ConfigurationAccess @BasicAccess @Performance
    Scenario: Verify multiple access patterns return consistent results
        When I access "Application:Name" using string indexer
        And I access "Application.Name" using dynamic access
        And I get section "Application" and access "Name" key
        Then all three access methods should return the same value "FlexKit Test Application"
        When I access "Database:CommandTimeout" using string indexer
        And I access "Database.CommandTimeout" using dynamic access
        And I get section "Database" and access "CommandTimeout" key
        Then all three access methods should return the same value "30"

    @ConfigurationAccess @BasicAccess @Chaining
    Scenario: Access deeply nested configuration with method chaining
        When I access the configuration dynamically as "config.External.PaymentApi.ApiKey"
        Then the dynamic result should be "test-payment-key-12345"
        When I get the current config for section "External"
        And I get the current config for section "PaymentApi" from the previous result
        And I access the section using string indexer with key "Timeout"
        Then the returned value should be "5000"

    @ConfigurationAccess @BasicAccess @Arrays
    Scenario: Access array configuration elements
        When I access the configuration section "Servers" using numeric indexer 0
        Then the result should not be null
        And the server configuration should have Name "Primary"
        When I access the configuration section "Servers" using numeric indexer 1
        Then the result should not be null
        And the server configuration should have Name "Secondary"
        When I access the configuration section "Servers" using numeric indexer 99
        Then the result should be null

    @ConfigurationAccess @BasicAccess @BooleanValues
    Scenario: Access and verify boolean configuration values
        When I access the configuration using the string indexer with key "Features:EnableCaching"
        Then the returned value should be "true"
        When I access the configuration using the string indexer with key "Features:EnableMetrics"
        Then the returned value should be "false"
        When I access the configuration using the string indexer with key "Database:EnableLogging"
        Then the returned value should be "true"

    @ConfigurationAccess @BasicAccess @NumericValues
    Scenario: Access and verify numeric configuration values
        When I access the configuration using the string indexer with key "Database:CommandTimeout"
        Then the returned value should be "30"
        When I access the configuration using the string indexer with key "Database:MaxRetryCount"
        Then the returned value should be "3"
        When I access the configuration using the string indexer with key "External:PaymentApi:Timeout"
        Then the returned value should be "5000"
        When I access the configuration using the string indexer with key "Features:MaxUploadSize"
        Then the returned value should be "10485760"
        