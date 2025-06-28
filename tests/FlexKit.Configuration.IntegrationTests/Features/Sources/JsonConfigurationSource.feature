Feature: JSON Configuration Source
    As a developer using FlexKit Configuration
    I want to load configuration from JSON files
    So that I can use structured configuration data in my applications

    Background:
        Given I have prepared a JSON configuration source environment

    @JsonConfigurationSource @BasicJsonFile
    Scenario: Load configuration from basic JSON file
        When I register JSON file "TestData/ConfigurationFiles/appsettings.json" as configuration source
        And I create the JSON-based configuration
        Then the JSON configuration should be loaded successfully
        And the JSON configuration should contain application settings
        And the JSON configuration should have the expected JSON structure

    @JsonConfigurationSource @EnvironmentSpecificFiles
    Scenario: Load configuration from environment-specific JSON files
        When I register JSON file "TestData/ConfigurationFiles/appsettings.json" as base configuration
        And I register JSON file "TestData/ConfigurationFiles/appsettings.Development.json" as environment configuration
        And I create the JSON-based configuration
        Then the JSON configuration should be loaded successfully
        And the environment-specific values should override base values
        And both JSON files should contribute to the final configuration

    @JsonConfigurationSource @OptionalJsonFile
    Scenario: Handle optional JSON files gracefully
        When I register JSON file "TestData/ConfigurationFiles/appsettings.json" as required configuration
        And I register non-existent JSON file "TestData/ConfigurationFiles/appsettings.Missing.json" as optional configuration
        And I create the JSON-based configuration
        Then the JSON configuration should be loaded successfully
        And the required JSON file values should be present
        And missing optional files should not cause errors

    @JsonConfigurationSource @InvalidJsonFile
    Scenario: Handle invalid JSON files with proper error handling
        When I register JSON file with invalid JSON content as configuration source
        And I attempt to create the JSON-based configuration
        Then the JSON configuration loading should fail with format error
        And the error should indicate JSON parsing failure

    @JsonConfigurationSource @DynamicJsonContent
    Scenario: Create configuration from dynamic JSON content
        When I provide JSON content with nested configuration:
            """
            {
                "Application": {
                    "Name": "Dynamic JSON Test",
                    "Version": "1.0.0",
                    "Environment": "Test"
                },
                "Database": {
                    "ConnectionString": "Server=dynamic.test.com;Database=JsonTest;",
                    "CommandTimeout": 45,
                    "EnableLogging": true
                },
                "Features": {
                    "EnableCaching": true,
                    "EnableMetrics": false,
                    "MaxUsers": 1000
                },
                "External": {
                    "PaymentApi": {
                        "BaseUrl": "https://payment.dynamic.com",
                        "ApiKey": "dynamic-key-12345",
                        "Timeout": 8000
                    }
                },
                "ArrayExample": [
                    "item1",
                    "item2",
                    "item3"
                ]
            }
            """
        And I register the dynamic JSON content as configuration source
        And I create the JSON-based configuration
        Then the JSON configuration should be loaded successfully
        And the JSON configuration should contain "Application:Name" with value "Dynamic JSON Test"
        And the JSON configuration should contain "Database:CommandTimeout" with value "45"
        And the JSON configuration should contain "Features:EnableCaching" with value "True"
        And the JSON configuration should contain "External:PaymentApi:BaseUrl" with value "https://payment.dynamic.com"
        And the JSON configuration should contain "ArrayExample:0" with value "item1"
        And the JSON configuration should contain "ArrayExample:2" with value "item3"

    @JsonConfigurationSource @JsonArrayHandling
    Scenario: Handle JSON arrays correctly in configuration
        When I provide JSON content with array configurations:
            """
            {
                "Servers": [
                    {
                        "Name": "Server1",
                        "Host": "server1.test.com",
                        "Port": 8080
                    },
                    {
                        "Name": "Server2", 
                        "Host": "server2.test.com",
                        "Port": 8081
                    }
                ],
                "AllowedHosts": ["localhost", "127.0.0.1", "*.test.com"],
                "Ports": [8080, 8081, 8082]
            }
            """
        And I register the dynamic JSON content as configuration source
        And I create the JSON-based configuration
        Then the JSON configuration should be loaded successfully
        And the JSON configuration should contain "Servers:0:Name" with value "Server1"
        And the JSON configuration should contain "Servers:1:Host" with value "server2.test.com"
        And the JSON configuration should contain "AllowedHosts:0" with value "localhost"
        And the JSON configuration should contain "AllowedHosts:2" with value "*.test.com"
        And the JSON configuration should contain "Ports:1" with value "8081"

    @JsonConfigurationSource @JsonHierarchy
    Scenario: Load deeply nested JSON configuration hierarchies
        When I provide JSON content with deep nesting:
            """
            {
                "Level1": {
                    "Level2": {
                        "Level3": {
                            "Level4": {
                                "DeepValue": "Found at level 4",
                                "DeepNumber": 42
                            }
                        },
                        "SiblingValue": "At level 2"
                    }
                },
                "Root": "At root level"
            }
            """
        And I register the dynamic JSON content as configuration source
        And I create the JSON-based configuration
        Then the JSON configuration should be loaded successfully
        And the JSON configuration should contain "Level1:Level2:Level3:Level4:DeepValue" with value "Found at level 4"
        And the JSON configuration should contain "Level1:Level2:Level3:Level4:DeepNumber" with value "42"
        And the JSON configuration should contain "Level1:Level2:SiblingValue" with value "At level 2"
        And the JSON configuration should contain "Root" with value "At root level"

    @JsonConfigurationSource @JsonTypesHandling
    Scenario: Handle different JSON data types correctly
        When I provide JSON content with various data types:
            """
            {
                "StringValue": "test string",
                "IntegerValue": 123,
                "DoubleValue": 45.67,
                "BooleanTrue": true,
                "BooleanFalse": false,
                "NullValue": null,
                "EmptyString": "",
                "ZeroNumber": 0
            }
            """
        And I register the dynamic JSON content as configuration source
        And I create the JSON-based configuration
        Then the JSON configuration should be loaded successfully
        And the JSON configuration should contain "StringValue" with value "test string"
        And the JSON configuration should contain "IntegerValue" with value "123"
        And the JSON configuration should contain "DoubleValue" with value "45.67"
        And the JSON configuration should contain "BooleanTrue" with value "True"
        And the JSON configuration should contain "BooleanFalse" with value "False"
        And the JSON configuration should contain "EmptyString" with value ""
        And the JSON configuration should contain "ZeroNumber" with value "0"

    @JsonConfigurationSource @ConfigurationPrecedence
    Scenario: Test JSON configuration precedence and overrides
        When I register JSON file with base configuration:
            """
            {
                "Application": {
                    "Name": "Base App",
                    "Environment": "Development"
                },
                "Database": {
                    "ConnectionString": "Server=base.db.com",
                    "Timeout": 30
                }
            }
            """
        And I register JSON file with override configuration:
            """
            {
                "Application": {
                    "Environment": "Test"
                },
                "Database": {
                    "Timeout": 60
                },
                "NewSection": {
                    "NewValue": "Added value"
                }
            }
            """
        And I create the JSON-based configuration
        Then the JSON configuration should be loaded successfully
        And the JSON configuration should contain "Application:Name" with value "Base App"
        And the JSON configuration should contain "Application:Environment" with value "Test"
        And the JSON configuration should contain "Database:ConnectionString" with value "Server=base.db.com"
        And the JSON configuration should contain "Database:Timeout" with value "60"
        And the JSON configuration should contain "NewSection:NewValue" with value "Added value"

    @JsonConfigurationSource @JsonFlexConfigIntegration
    Scenario: Verify JSON configuration works with FlexConfig
        When I register JSON file "TestData/ConfigurationFiles/appsettings.json" as configuration source
        And I create the FlexConfig from JSON configuration
        Then the FlexConfig should be created successfully
        And the FlexConfig should provide access to JSON configuration values
        And the FlexConfig should support dynamic access to JSON data
        And the FlexConfig should maintain compatibility with standard configuration access

    @JsonConfigurationSource @JsonReloadOnChange
    Scenario: Test JSON configuration reload functionality
        When I register JSON file with reload-on-change enabled
        And I create the JSON-based configuration
        Then the JSON configuration should be loaded successfully
        And the configuration should be set up for automatic reloading
        And changes to the JSON file should trigger configuration updates

    @JsonConfigurationSource @JsonErrorRecovery
    Scenario: Handle JSON configuration errors gracefully
        When I register multiple JSON files with mixed validity:
            | File                                              | Valid | Optional |
            | TestData/ConfigurationFiles/appsettings.json     | true  | false    |
            | TestData/ConfigurationFiles/invalid.json         | false | true     |
            | TestData/ConfigurationFiles/missing.json         | false | true     |
        And I create the JSON-based configuration
        Then the JSON configuration should be loaded successfully
        And valid JSON files should contribute to configuration
        And invalid optional files should be skipped gracefully
        And the configuration should contain data from valid sources only