Feature: Secrets Manager Error Handling
As a developer using AWS Secrets Manager
I want proper error handling for various failure scenarios
So that my application can gracefully handle configuration loading issues

    @SecretsManager @ErrorHandling
    Scenario: Handle invalid JSON secret with error recovery
        Given I have established a secrets error handler environment
        And I have secrets error handler configuration with invalid JSON from "infrastructure-module-error-scenarios.json"
        When I process secrets error handler configuration with error tolerance
        Then the secrets error handler should capture JSON parsing errors
        And the secrets error handler should continue loading valid secrets
        And the secrets error handler configuration should contain "infrastructure-module:database:host" with value "localhost"

    @SecretsManager @ErrorHandling @AccessDenied
    Scenario: Handle access denied errors for restricted secrets
        Given I have established a secrets error handler environment
        And I have secrets error handler configuration with access restrictions from "infrastructure-module-error-scenarios.json"
        When I process secrets error handler configuration with simulated access denied error
        Then the secrets error handler should capture access denied exceptions
        And the secrets error handler should log authorization failure details
        And the secrets error handler should provide fallback configuration values

    @SecretsManager @ErrorHandling @MissingRequired
    Scenario: Handle missing required secrets
        Given I have established a secrets error handler environment
        And I have secrets error handler configuration with missing required secret from "infrastructure-module-error-scenarios.json"
        When I process secrets error handler configuration as required source
        Then the secrets error handler should throw configuration loading exception
        And the secrets error handler should indicate missing secret name
        And the secrets error handler exception should contain secret name details

    @SecretsManager @ErrorHandling @Optional
    Scenario: Handle missing optional secrets gracefully
        Given I have established a secrets error handler environment
        And I have secrets error handler configuration with missing optional secret from "infrastructure-module-error-scenarios.json"
        When I process secrets error handler configuration as optional source
        Then the secrets error handler should complete loading without errors
        And the secrets error handler should skip missing secret names
        And the secrets error handler should log missing secret warnings

    @SecretsManager @ErrorHandling @LargeSecrets
    Scenario: Handle secret value size limits
        Given I have established a secrets error handler environment
        And I have secrets error handler configuration with oversized secret from "infrastructure-module-error-scenarios.json"
        When I process secrets error handler configuration with size validation
        Then the secrets error handler should detect secret size violations
        And the secrets error handler should truncate or reject large secrets
        And the secrets error handler should continue processing remaining secrets

    @SecretsManager @ErrorHandling @NetworkFailure
    Scenario: Handle network connectivity failures
        Given I have established a secrets error handler environment
        And I have secrets error handler configuration from "infrastructure-module-aws-config.json"
        When I process secrets error handler configuration with network failure simulation
        Then the secrets error handler should capture network timeout exceptions
        And the secrets error handler should attempt retry operations
        And the secrets error handler should provide cached or default values

    @SecretsManager @ErrorHandling @FlexConfig
    Scenario: Verify error handling through FlexConfig interface
        Given I have established a secrets error handler environment
        And I have secrets error handler configuration with mixed errors from "infrastructure-module-error-scenarios.json"
        When I process secrets error handler configuration through FlexConfig
        And I verify secrets error handler error recovery capabilities
        Then the secrets error handler FlexConfig should handle missing key access gracefully
        And the secrets error handler FlexConfig should provide default values for failed secrets