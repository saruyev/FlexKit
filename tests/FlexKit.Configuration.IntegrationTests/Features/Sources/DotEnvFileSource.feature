# .env File Configuration Source Integration Tests

Feature: DotEnv File Configuration Source
    As a developer using FlexKit Configuration
    I want to load configuration from .env files
    So that I can manage environment-like configuration in development scenarios

    Background:
        Given I have established a .env file configuration source environment

    @DotEnvFileSource @BasicDotEnvLoading
    Scenario: Load configuration from .env file
        When I specify .env content with basic configuration:
            """
            APP_NAME=DotEnv Test Application
            APP_VERSION=1.5.0
            DEBUG_MODE=true
            PORT_NUMBER=9000
            DATABASE_URL=postgresql://localhost:5432/dotenvtest
            """
        And I add the dynamic .env content to configuration
        And I build the .env file configuration
        Then the .env file configuration loads without errors
        And the .env configuration includes "APP_NAME" having value "DotEnv Test Application"
        And the .env configuration includes "APP_VERSION" having value "1.5.0"
        And the .env configuration includes "DEBUG_MODE" having value "true"
        And the .env configuration includes "PORT_NUMBER" having value "9000"
        And the .env configuration includes "DATABASE_URL" having value "postgresql://localhost:5432/dotenvtest"

    @DotEnvFileSource @QuotedValues
    Scenario: Handle quoted values in .env files
        When I specify .env content with quoted values:
            """
            SINGLE_QUOTED='This is single quoted'
            DOUBLE_QUOTED="This is double quoted"
            NO_QUOTES=This is without quotes
            QUOTED_WITH_SPACES="Value with spaces and special chars !@#"
            QUOTED_EMPTY=""
            SINGLE_QUOTED_EMPTY=''
            """
        And I add the dynamic .env content to configuration
        And I build the .env file configuration
        Then the .env file configuration loads without errors
        And the .env configuration includes "SINGLE_QUOTED" having value "This is single quoted"
        And the .env configuration includes "DOUBLE_QUOTED" having value "This is double quoted"
        And the .env configuration includes "NO_QUOTES" having value "This is without quotes"
        And the .env configuration includes "QUOTED_WITH_SPACES" having value "Value with spaces and special chars !@#"
        And the .env configuration includes "QUOTED_EMPTY" having value ""
        And the .env configuration includes "SINGLE_QUOTED_EMPTY" having value ""

    @DotEnvFileSource @EscapeSequences
    Scenario: Handle escape sequences in .env files
        When I specify .env content with escape sequences:
            """
            SIMPLE_VALUE="No escapes here"
            DOUBLE_BACKSLASH="Path\\\\file"
            QUOTED_VALUE="Value with spaces"
            """
        And I add the dynamic .env content to configuration
        And I build the .env file configuration
        Then the .env file configuration loads without errors
        And the .env configuration includes "SIMPLE_VALUE" having value "No escapes here"
        And the .env configuration includes "DOUBLE_BACKSLASH" having value "Path\\file"
        And the .env configuration includes "QUOTED_VALUE" having value "Value with spaces"

    @DotEnvFileSource @CommentsAndEmptyLines
    Scenario: Handle comments and empty lines in .env files
        When I specify .env content with comments and empty lines:
            """
            # Application Configuration
            APP_NAME=Comment Test App
            APP_VERSION=2.0.0

            # Database Configuration
            DATABASE_HOST=localhost
            DATABASE_PORT=5432
            # DATABASE_PASSWORD=commented_out

            # This is a comment line
              # Indented comment
            API_KEY=valid-api-key-123
            """
        And I add the dynamic .env content to configuration
        And I build the .env file configuration
        Then the .env file configuration loads without errors
        And the .env configuration includes "APP_NAME" having value "Comment Test App"
        And the .env configuration includes "APP_VERSION" having value "2.0.0"
        And the .env configuration includes "DATABASE_HOST" having value "localhost"
        And the .env configuration includes "DATABASE_PORT" having value "5432"
        And the .env configuration includes "API_KEY" having value "valid-api-key-123"
        And the .env configuration excludes "DATABASE_PASSWORD"

    @DotEnvFileSource @ExistingTestDataFile
    Scenario: Load configuration from existing test.env file
        When I load .env file "TestData/ConfigurationFiles/test.env" into configuration
        And I build the .env file configuration
        Then the .env file configuration loads without errors
        And the .env configuration includes "APP_NAME" having value "FlexKit Configuration Integration Tests"
        And the .env configuration includes "DATABASE_HOST" having value "localhost"
        And the .env configuration includes "FEATURES__ENABLECACHING" having value "true"
        And the .env configuration includes "SECURITY__JWTSETTINGS__ISSUER" having value "flexkit-env.test.com"

    @DotEnvFileSource @OptionalFiles
    Scenario: Handle optional .env files
        When I load non-existent .env file "TestData/ConfigurationFiles/nonexistent.env" as optional
        And I load .env file "TestData/ConfigurationFiles/test.env" into configuration
        And I build the .env file configuration
        Then the .env file configuration loads without errors
        And the .env configuration includes "APP_NAME" having value "FlexKit Configuration Integration Tests"

    @DotEnvFileSource @RequiredFiles
    Scenario: Handle required .env files that don't exist
        When I load non-existent .env file "TestData/ConfigurationFiles/missing.env" as required
        And I attempt to build the .env file configuration
        Then the .env file configuration fails to load
        And the .env loading error indicates missing file

    @DotEnvFileSource @MultipleDotEnvFiles
    Scenario: Load configuration from multiple .env files with precedence
        When I specify .env content with base configuration:
            """
            APP_NAME=Base DotEnv App
            APP_VERSION=1.0.0
            DATABASE_HOST=base.localhost
            API_TIMEOUT=5000
            """
        And I specify .env content with override configuration:
            """
            APP_VERSION=2.0.0
            DATABASE_HOST=override.localhost
            NEW_SETTING=additional_value
            """
        And I add base .env content to configuration
        And I add override .env content to configuration
        And I build the .env file configuration
        Then the .env file configuration loads without errors
        And the .env configuration includes "APP_NAME" having value "Base DotEnv App"
        And the .env configuration includes "APP_VERSION" having value "2.0.0"
        And the .env configuration includes "DATABASE_HOST" having value "override.localhost"
        And the .env configuration includes "API_TIMEOUT" having value "5000"
        And the .env configuration includes "NEW_SETTING" having value "additional_value"

    @DotEnvFileSource @SpecialValues
    Scenario: Handle special and edge case values in .env files
        When I specify .env content with special values:
            """
            EMPTY_VALUE=
            NULL_VALUE=null
            ZERO_VALUE=0
            FALSE_VALUE=false
            TRUE_VALUE=true
            NUMERIC_STRING="12345"
            BOOLEAN_STRING="true"
            SPECIAL_CHARS="!@#$%^&*()_+-=[]{}|;:,.<>?"
            WHITESPACE_VALUE="  spaced value  "
            """
        And I add the dynamic .env content to configuration
        And I build the .env file configuration
        Then the .env file configuration loads without errors
        And the .env configuration includes "EMPTY_VALUE" having value ""
        And the .env configuration includes "NULL_VALUE" having value "null"
        And the .env configuration includes "ZERO_VALUE" having value "0"
        And the .env configuration includes "FALSE_VALUE" having value "false"
        And the .env configuration includes "TRUE_VALUE" having value "true"
        And the .env configuration includes "NUMERIC_STRING" having value "12345"
        And the .env configuration includes "BOOLEAN_STRING" having value "true"
        And the .env configuration includes "SPECIAL_CHARS" having value "!@#$%^&*()_+-=[]{}|;:,.<>?"
        And I add the dynamic .env content to configuration
        And I build the .env file configuration
        Then the .env file configuration loads without errors
        And the .env configuration includes "EMPTY_VALUE" having value ""
        And the .env configuration includes "NULL_VALUE" having value "null"
        And the .env configuration includes "ZERO_VALUE" having value "0"
        And the .env configuration includes "FALSE_VALUE" having value "false"
        And the .env configuration includes "TRUE_VALUE" having value "true"
        And the .env configuration includes "NUMERIC_STRING" having value "12345"
        And the .env configuration includes "BOOLEAN_STRING" having value "true"
        And the .env configuration includes "SPECIAL_CHARS" having value "!@#$%^&*()_+-=[]{}|;:,.<>?"
        And the .env configuration includes "WHITESPACE_VALUE" having value "  spaced value  "

    @DotEnvFileSource @ComplexDotEnvConfiguration
    Scenario: Load complex configuration from .env file
        When I specify .env content with complex configuration:
            """
            # Application Configuration
            APP_NAME="Complex DotEnv Application"
            APP_VERSION=3.1.0
            APP_ENVIRONMENT=Development
            APP_DEBUG=true

            # Database Configuration
            DATABASE_HOST=complex.database.com
            DATABASE_PORT=5432
            DATABASE_NAME=complex_app_db
            DATABASE_USER=complex_user
            DATABASE_PASSWORD="complex_pass_123!"
            DATABASE_SSL_MODE=require

            # External API Configuration
            PAYMENT_API_URL=https://api.payment.com/v2
            PAYMENT_API_KEY="payment-api-key-complex-12345"
            NOTIFICATION_API_URL=https://api.notifications.com/v1
            NOTIFICATION_API_KEY="notification-api-key-complex-67890"

            # Feature Flags
            ENABLE_CACHING=true
            ENABLE_METRICS=false
            ENABLE_TRACING=true
            ENABLE_ADVANCED_SEARCH=false

            # File Paths
            LOG_PATH=/var/log/complex-app.log
            CONFIG_PATH="/etc/complex-app/config.ini"
            DATA_PATH='/data/complex-app'
            """
        And I add the dynamic .env content to configuration
        And I build the .env file configuration
        Then the .env file configuration loads without errors
        And the .env configuration includes "APP_NAME" having value "Complex DotEnv Application"
        And the .env configuration includes "APP_VERSION" having value "3.1.0"
        And the .env configuration includes "DATABASE_HOST" having value "complex.database.com"
        And the .env configuration includes "DATABASE_PASSWORD" having value "complex_pass_123!"
        And the .env configuration includes "PAYMENT_API_URL" having value "https://api.payment.com/v2"
        And the .env configuration includes "ENABLE_CACHING" having value "true"
        And the .env configuration includes "ENABLE_METRICS" having value "false"
        And the .env configuration includes "LOG_PATH" having value "/var/log/complex-app.log"
        And the .env configuration includes "CONFIG_PATH" having value "/etc/complex-app/config.ini"
        And the .env configuration includes "DATA_PATH" having value "/data/complex-app"

    @DotEnvFileSource @FlexConfigIntegration
    Scenario: Access .env configuration through FlexConfig
        When I specify .env content with hierarchical-style configuration:
            """
            APP_NAME=FlexConfig DotEnv App
            APP_DEBUG=true
            DATABASE_HOST=flex.database.com
            API_PRIMARY_URL=https://primary.api.com
            API_BACKUP_URL=https://backup.api.com
            FEATURE_CACHE=true
            FEATURE_METRICS=false
            """
        And I add the dynamic .env content to configuration
        And I build the .env file configuration
        And I generate FlexConfig from .env configuration
        Then the FlexConfig loads from .env successfully
        And FlexConfig includes "APP_NAME" having value "FlexConfig DotEnv App"
        And FlexConfig includes "APP_DEBUG" having value "true"
        And FlexConfig includes "DATABASE_HOST" having value "flex.database.com"
        And FlexConfig includes "API_PRIMARY_URL" having value "https://primary.api.com"
        And FlexConfig includes "API_BACKUP_URL" having value "https://backup.api.com"
        And FlexConfig includes "FEATURE_CACHE" having value "true"
        And FlexConfig includes "FEATURE_METRICS" having value "false"

    @DotEnvFileSource @InvalidDotEnvContent
    Scenario: Handle invalid .env file content gracefully
        When I specify .env content with invalid format:
            """
            VALID_KEY=valid_value
            INVALID_LINE_NO_EQUALS
            ANOTHER_VALID_KEY=another_value
            =INVALID_EMPTY_KEY
            VALID_AGAIN=works_fine
            """
        And I add the dynamic .env content to configuration
        And I build the .env file configuration
        Then the .env file configuration loads without errors
        And the .env configuration includes "VALID_KEY" having value "valid_value"
        And the .env configuration includes "ANOTHER_VALID_KEY" having value "another_value"
        And the .env configuration includes "VALID_AGAIN" having value "works_fine"
        And the .env configuration excludes "INVALID_LINE_NO_EQUALS"