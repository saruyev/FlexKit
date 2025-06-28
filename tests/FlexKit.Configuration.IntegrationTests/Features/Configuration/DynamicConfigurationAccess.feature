Feature: Dynamic Configuration Access
    As a developer using FlexKit Configuration
    I want to access configuration values using advanced dynamic patterns
    So that I can navigate complex configuration hierarchies naturally

    Background:
        Given I setup a complex configuration structure:
            | Key                                    | Value                                      |
            | Application:Name                       | FlexKit Dynamic Test App                   |
            | Application:Version                    | 2.1.0                                      |
            | Application:Features:Count             | 5                                          |
            | Database:Primary:ConnectionString      | Server=primary.db.com;Database=AppPrimary;|
            | Database:Primary:CommandTimeout        | 45                                         |
            | Database:Primary:EnableLogging         | true                                       |
            | Database:Secondary:ConnectionString    | Server=secondary.db.com;Database=AppSecond;|
            | Database:Secondary:CommandTimeout      | 30                                         |
            | Database:Secondary:ReadOnly            | true                                       |
            | External:PaymentGateway:BaseUrl        | https://payments.example.com/api/v2        |
            | External:PaymentGateway:ApiKey         | pk_test_dynamic_12345                      |
            | External:PaymentGateway:Timeout        | 8000                                       |
            | External:PaymentGateway:RetryCount     | 3                                          |
            | External:EmailService:SmtpHost         | smtp.example.com                           |
            | External:EmailService:SmtpPort         | 587                                        |
            | External:EmailService:UseSsl           | true                                       |
            | Features:Payment:Enabled               | true                                       |
            | Features:Payment:MaxAmount             | 10000.50                                   |
            | Features:Notifications:Enabled         | false                                      |
            | Features:Notifications:BatchSize       | 100                                        |
            | Security:Jwt:Issuer                    | dynamic-test.example.com                   |
            | Security:Jwt:Audience                  | dynamic-api                                |
            | Security:Jwt:ExpirationMinutes         | 60                                         |

    @DynamicAccess @PropertyNavigation
    Scenario: Navigate to properties using dynamic syntax
        When I navigate dynamically to "Application.Name"
        Then the property value should be "FlexKit Dynamic Test App"
        When I navigate dynamically to "Application.Version"
        Then the property value should be "2.1.0"
        When I navigate dynamically to "Application.Features.Count"
        Then the property value should be "5"

    @DynamicAccess @DeepNavigation
    Scenario: Navigate to deeply nested properties
        When I navigate dynamically to "Database.Primary.ConnectionString"
        Then the property value should be "Server=primary.db.com;Database=AppPrimary;"
        When I navigate dynamically to "External.PaymentGateway.BaseUrl"
        Then the property value should be "https://payments.example.com/api/v2"
        When I navigate dynamically to "Features.Payment.MaxAmount"
        Then the property value should be "10000.50"

    @DynamicAccess @SectionAccess
    Scenario: Access configuration sections dynamically
        When I get dynamic section "Database.Primary"
        Then the section should be valid
        And the section should contain "ConnectionString" with value "Server=primary.db.com;Database=AppPrimary;"
        And the section should contain "CommandTimeout" with value "45"
        And the section should contain "EnableLogging" with value "true"

    @DynamicAccess @ChainedNavigation
    Scenario: Chain dynamic navigation through multiple levels
        When I start navigation at root
        And I move to section "External"
        And I move to section "PaymentGateway"
        And I get final property "BaseUrl"
        Then the navigation result should be "https://payments.example.com/api/v2"

    @DynamicAccess @NullSafety
    Scenario: Handle missing properties gracefully
        When I navigate dynamically to "NonExistent.Property"
        Then the property value should be null
        When I navigate dynamically to "Database.Missing.Value"
        Then the property value should be null
        When I navigate dynamically to "Completely.Missing.Deep.Nested.Property"
        Then the property value should be null

    @DynamicAccess @TypeConversions
    Scenario: Convert dynamic property values to specific types
        When I navigate to property "Database.Primary.CommandTimeout" as integer
        Then the integer value should be 45
        When I navigate to property "Database.Primary.EnableLogging" as boolean
        Then the boolean value should be true
        When I navigate to property "Features.Payment.MaxAmount" as decimal
        Then the decimal value should be 10000.50

    @DynamicAccess @PropertyComparison
    Scenario: Compare dynamic navigation with traditional indexer access
        When I navigate dynamically to "External.PaymentGateway.ApiKey"
        And I access traditionally "External:PaymentGateway:ApiKey"
        Then both approaches should yield the same result

    @DynamicAccess @CaseSensitivity
    Scenario: Verify case insensitive dynamic navigation
        When I navigate dynamically to "database.primary.connectionstring"
        Then the property value should be "Server=primary.db.com;Database=AppPrimary;"
        When I navigate dynamically to "DATABASE.PRIMARY.COMMANDTIMEOUT"
        Then the property value should be "45"

    @DynamicAccess @StringOperations
    Scenario: Perform string operations on dynamic values
        When I navigate to property "Security.Jwt.ExpirationMinutes" and convert to string
        Then the text result should be "60"
        When I navigate to property "External.EmailService.UseSsl" and convert to string
        Then the text result should be "true"

    @DynamicAccess @ErrorHandling
    Scenario: Validate error handling for dynamic access
        When I navigate dynamically to "Database.Primary.InvalidProperty"
        Then the property value should be null
        And no errors should occur
        When I navigate dynamically to "Missing.Chain.Of.Properties"
        Then the property value should be null
        And no errors should occur

    @DynamicAccess @PerformanceValidation
    Scenario: Verify dynamic access performance
        When I perform multiple dynamic navigations to "Database.Primary.ConnectionString"
        Then all navigation attempts should return consistent results
        When I perform 50 dynamic property accesses
        Then all access operations should complete successfully

    @DynamicAccess @BooleanHandling
    Scenario: Handle boolean configurations through dynamic access
        When I navigate dynamically to "Database.Primary.EnableLogging"
        Then the property value should be "true"
        When I navigate dynamically to "Database.Secondary.ReadOnly"
        Then the property value should be "true"
        When I navigate dynamically to "Features.Notifications.Enabled"
        Then the property value should be "false"

    @DynamicAccess @NumericHandling
    Scenario: Handle numeric configurations through dynamic access
        When I navigate dynamically to "External.PaymentGateway.Timeout"
        Then the property value should be "8000"
        When I navigate dynamically to "Features.Notifications.BatchSize"
        Then the property value should be "100"
        When I navigate dynamically to "Security.Jwt.ExpirationMinutes"
        Then the property value should be "60"

    @DynamicAccess @SectionValidation
    Scenario: Validate dynamic section access functionality
        When I get dynamic section "External.PaymentGateway"
        Then the section should be valid
        And the section should contain "BaseUrl" with value "https://payments.example.com/api/v2"
        And the section should contain "ApiKey" with value "pk_test_dynamic_12345"
        And the section should contain "Timeout" with value "8000"
        And the section should contain "RetryCount" with value "3"

    @DynamicAccess @ToStringBehavior
    Scenario: Verify ToString behavior on dynamic configuration results
        When I navigate dynamically to "Database.Primary.ConnectionString"
        And I convert the result to string representation
        Then the string output should be "Server=primary.db.com;Database=AppPrimary;"
        When I navigate dynamically to "Features.Payment.Enabled"
        And I convert the result to string representation
        Then the string output should be "true"