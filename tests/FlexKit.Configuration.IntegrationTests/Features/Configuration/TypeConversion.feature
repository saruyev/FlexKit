Feature: Type Conversion
    As a developer using FlexKit Configuration
    I want to convert configuration values to strongly typed objects
    So that I can work with typed data instead of just strings

    Background:
        Given I have established a configuration for type conversion testing

    @TypeConversion @PrimitiveTypes
    Scenario: Convert configuration values to primitive types
        When I load type conversion test data:
            | Key                    | Value      | Type    |
            | Server:Port            | 8080       | int     |
            | Server:IsSecure        | true       | bool    |
            | Server:Timeout         | 30.5       | double  |
            | Server:MaxConnections  | 1000       | long    |
            | Features:IsEnabled     | false      | bool    |
            | Database:Retries       | 3          | int     |
        And I retrieve and convert "Server:Port" to integer type
        Then the converted integer result should be 8080
        When I retrieve and convert "Server:IsSecure" to boolean type
        Then the converted boolean result should be true
        When I retrieve and convert "Server:Timeout" to double type
        Then the converted double result should be 30.5
        When I retrieve and convert "Features:IsEnabled" to boolean type
        Then the converted boolean result should be false

    @TypeConversion @NullableTypes
    Scenario: Convert configuration values to nullable types
        When I load type conversion test data:
            | Key                | Value | Type  |
            | Optional:Port      | 9090  | int?  |
            | Optional:IsEnabled |       | bool? |
            | Optional:Timeout   | 45.7  | double? |
            | Optional:Missing   |       | int?  |
        And I retrieve and convert "Optional:Port" to nullable integer type
        Then the converted nullable integer should be 9090
        When I retrieve and convert "Optional:IsEnabled" to nullable boolean type
        Then the converted nullable boolean should be null
        When I retrieve and convert "Optional:Timeout" to nullable double type
        Then the converted nullable double should be 45.7

    @TypeConversion @StringCollections
    Scenario: Convert delimited strings to collections
        When I load type conversion test data:
            | Key                  | Value                           |
            | AllowedHosts         | localhost,127.0.0.1,test.com   |
            | SupportedFormats     | json;xml;yaml;csv               |
            | LogLevels            | Debug Information Warning Error |
            | EmptyList            |                                 |
        And I retrieve and convert "AllowedHosts" to string collection using comma separator
        Then the string collection should contain 3 items
        And the string collection should contain "localhost"
        And the string collection should contain "127.0.0.1"
        And the string collection should contain "test.com"
        When I retrieve and convert "SupportedFormats" to string collection using semicolon separator
        Then the string collection should contain 4 items
        And the string collection should contain "json"
        And the string collection should contain "xml"
        When I retrieve and convert "LogLevels" to string collection using space separator
        Then the string collection should contain 4 items

    @TypeConversion @NumericCollections
    Scenario: Convert delimited strings to numeric collections
        When I load type conversion test data:
            | Key              | Value            |
            | Ports            | 8080,8081,8082   |
            | ServerWeights    | 1.5;2.3;0.8;4.1  |
            | RetryDelays      | 100 250 500 1000 |
        And I retrieve and convert "Ports" to integer collection using comma separator
        Then the integer collection should contain 3 items
        And the integer collection should contain value 8080
        And the integer collection should contain value 8081
        And the integer collection should contain value 8082
        When I retrieve and convert "ServerWeights" to double collection using semicolon separator
        Then the double collection should contain 4 items
        And the double collection should contain value 1.5
        And the double collection should contain value 4.1

    @TypeConversion @JsonFileConversion
    Scenario: Convert values from JSON configuration file
        When I load configuration from JSON file "TestData/ConfigurationFiles/appsettings.json"
        And I retrieve and convert JSON value "Database:CommandTimeout" to integer type
        Then the converted integer result should match the JSON file value
        When I retrieve and convert JSON value "Features:EnableCaching" to boolean type
        Then the converted boolean result should match the JSON file value
        When I retrieve and convert JSON value "External:Api:Timeout" to integer type
        Then the converted integer result should match the JSON file value

    @TypeConversion @ErrorHandling
    Scenario: Handle invalid type conversions gracefully
        When I load type conversion test data:
            | Key                | Value     |
            | Invalid:NotNumber  | abc123    |
            | Invalid:NotBoolean | maybe     |
            | Invalid:NotDouble  | infinity! |
        And I attempt to convert "Invalid:NotNumber" to integer type
        Then the conversion should fail with format exception
        When I attempt to convert "Invalid:NotBoolean" to boolean type
        Then the conversion should fail with format exception
        When I attempt to convert "Invalid:NotDouble" to double type
        Then the conversion should fail with format exception

    @TypeConversion @DefaultValues
    Scenario: Handle null and empty values with default conversions
        When I load type conversion test data:
            | Key              | Value |
            | Empty:String     |       |
            | Missing:Integer  |       |
            | Missing:Boolean  |       |
            | Missing:Double   |       |
        And I retrieve and convert "Empty:String" to string type
        Then the converted string result should be empty
        When I retrieve and convert "Missing:Integer" to integer type
        Then the converted integer result should be 0
        When I retrieve and convert "Missing:Boolean" to boolean type
        Then the converted boolean result should be false
        When I retrieve and convert "Missing:Double" to double type
        Then the converted double result should be 0.0

    @TypeConversion @BooleanVariations
    Scenario: Convert various boolean representations
        When I load type conversion test data:
            | Key                    | Value |
            | Boolean:True1          | true  |
            | Boolean:True2          | True  |
            | Boolean:True3          | TRUE  |
            | Boolean:False1         | false |
            | Boolean:False2         | False |
            | Boolean:False3         | FALSE |
            | Boolean:Numeric1       | 1     |
            | Boolean:Numeric0       | 0     |
        And I retrieve and convert "Boolean:True1" to boolean type
        Then the converted boolean result should be true
        When I retrieve and convert "Boolean:True2" to boolean type
        Then the converted boolean result should be true
        When I retrieve and convert "Boolean:False1" to boolean type
        Then the converted boolean result should be false
        When I retrieve and convert "Boolean:False3" to boolean type
        Then the converted boolean result should be false

    @TypeConversion @CollectionEdgeCases
    Scenario: Handle edge cases in collection conversion
        When I load type conversion test data:
            | Key                    | Value                    |
            | EdgeCase:EmptyItems    | ,,item1,,item2,          |
            | EdgeCase:SingleItem    | onlyitem                 |
            | EdgeCase:Whitespace    | item1 , item2 , item3    |
            | EdgeCase:Null          |                          |
        And I retrieve and convert "EdgeCase:EmptyItems" to string collection using comma separator
        Then the string collection should contain empty string items
        When I retrieve and convert "EdgeCase:SingleItem" to string collection using comma separator
        Then the string collection should contain 1 items
        And the string collection should contain "onlyitem"
        When I retrieve and convert "EdgeCase:Null" to string collection using comma separator
        Then the string collection should be null

    @TypeConversion @LongValues
    Scenario: Convert large numeric values
        When I load type conversion test data:
            | Key                 | Value            |
            | Large:Integer       | 2147483647       |
            | Large:Long          | 9223372036854775807 |
            | Large:Double        | 1.7976931348623157E+308 |
            | Small:Integer       | -2147483648      |
            | Small:Double        | 4.94065645841247E-324 |
        And I retrieve and convert "Large:Integer" to integer type
        Then the converted integer result should be 2147483647
        When I retrieve and convert "Large:Long" to long type
        Then the converted long result should be 9223372036854775807
        When I retrieve and convert "Small:Integer" to integer type
        Then the converted integer result should be -2147483648