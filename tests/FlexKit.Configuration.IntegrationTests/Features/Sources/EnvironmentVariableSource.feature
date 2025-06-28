# Environment Variable Configuration Source Integration Tests

Feature: Environment Variable Configuration Source
    As a developer using FlexKit Configuration
    I want to load configuration from environment variables
    So that I can configure my application at runtime without changing files

    Background:
        Given I have prepared an environment variable configuration source environment

    @EnvironmentVariableSource @BasicEnvironmentLoading
    Scenario: Load configuration from environment variables
        When I setup environment variables:
            | Name           | Value                    |
            | APP_NAME       | Environment Test App     |
            | APP_VERSION    | 1.2.3                   |
            | DEBUG_MODE     | true                    |
            | PORT_NUMBER    | 8080                    |
        And I register environment variables as configuration source
        And I build the environment configuration
        Then the environment configuration should be loaded successfully
        And the environment configuration should contain "APP_NAME" with value "Environment Test App"
        And the environment configuration should contain "APP_VERSION" with value "1.2.3"
        And the environment configuration should contain "DEBUG_MODE" with value "true"
        And the environment configuration should contain "PORT_NUMBER" with value "8080"

    @EnvironmentVariableSource @HierarchicalKeys
    Scenario: Handle hierarchical configuration keys with double underscores
        When I setup environment variables:
            | Name                          | Value                           |
            | DATABASE__CONNECTIONSTRING    | Server=localhost;Database=Test  |
            | DATABASE__TIMEOUT             | 30                              |
            | LOGGING__LOGLEVEL__DEFAULT    | Debug                           |
            | LOGGING__LOGLEVEL__MICROSOFT  | Warning                         |
            | API__BASEURL                  | https://api.test.com            |
            | API__TIMEOUT                  | 5000                            |
        And I register environment variables as configuration source
        And I build the environment configuration
        Then the environment configuration should be loaded successfully
        And the environment configuration should contain "DATABASE:CONNECTIONSTRING" with value "Server=localhost;Database=Test"
        And the environment configuration should contain "DATABASE:TIMEOUT" with value "30"
        And the environment configuration should contain "LOGGING:LOGLEVEL:DEFAULT" with value "Debug"
        And the environment configuration should contain "LOGGING:LOGLEVEL:MICROSOFT" with value "Warning"
        And the environment configuration should contain "API:BASEURL" with value "https://api.test.com"
        And the environment configuration should contain "API:TIMEOUT" with value "5000"

    @EnvironmentVariableSource @PrefixFiltering
    Scenario: Filter environment variables by prefix
        When I setup environment variables:
            | Name                 | Value                  |
            | MYAPP_DATABASE_HOST  | myapp.database.com     |
            | MYAPP_DATABASE_PORT  | 5432                   |
            | MYAPP_API_KEY        | secret-api-key         |
            | OTHERAPP_SETTING     | should-be-ignored      |
            | GLOBAL_SETTING       | also-ignored           |
        And I register environment variables with prefix "MYAPP_" as configuration source
        And I build the environment configuration
        Then the environment configuration should be loaded successfully
        And the environment configuration should contain "DATABASE_HOST" with value "myapp.database.com"
        And the environment configuration should contain "DATABASE_PORT" with value "5432"
        And the environment configuration should contain "API_KEY" with value "secret-api-key"
        And the environment configuration should not contain "OTHERAPP_SETTING"
        And the environment configuration should not contain "GLOBAL_SETTING"

    @EnvironmentVariableSource @DotEnvFileIntegration
    Scenario: Load configuration from .env file using environment variables
        When I register .env file "TestData/ConfigurationFiles/test.env" as configuration source
        And I build the environment configuration
        Then the environment configuration should be loaded successfully
        And the environment configuration should contain "APP_NAME" with value "FlexKit Configuration Integration Tests"
        And the environment configuration should contain "DATABASE_HOST" with value "localhost"
        And the environment configuration should contain "LOGGING__LOGLEVEL__DEFAULT" with value "Debug"
        And the environment configuration should contain "SECURITY__JWTSETTINGS__ISSUER" with value "flexkit-env.test.com"

    @EnvironmentVariableSource @EnvironmentOverrides
    Scenario: Environment variables override configuration precedence
        When I setup base configuration data:
            | Key                    | Value              |
            | Application:Name       | Base App Name      |
            | Database:Host          | base.database.com  |
            | Api:Timeout            | 3000               |
        And I setup environment variables:
            | Name                | Value                    |
            | APPLICATION__NAME   | Overridden App Name      |
            | API__TIMEOUT        | 10000                    |
        And I register base configuration as source
        And I register environment variables as configuration source
        And I build the environment configuration
        Then the environment configuration should be loaded successfully
        And the environment configuration should contain "Application:Name" with value "Overridden App Name"
        And the environment configuration should contain "Database:Host" with value "base.database.com"
        And the environment configuration should contain "Api:Timeout" with value "10000"

    @EnvironmentVariableSource @EmptyAndSpecialValues
    Scenario: Handle empty and special environment variable values
        When I setup environment variables:
            | Name           | Value  |
            | EMPTY_VALUE    |        |
            | NULL_VALUE     | null   |
            | ZERO_VALUE     | 0      |
            | FALSE_VALUE    | false  |
            | TRUE_VALUE     | true   |
            | SPACE_VALUE    |        |
        And I register environment variables as configuration source
        And I build the environment configuration
        Then the environment configuration should be loaded successfully
        And the environment configuration should contain "EMPTY_VALUE" with value ""
        And the environment configuration should contain "NULL_VALUE" with value "null"
        And the environment configuration should contain "ZERO_VALUE" with value "0"
        And the environment configuration should contain "FALSE_VALUE" with value "false"
        And the environment configuration should contain "TRUE_VALUE" with value "true"

    @EnvironmentVariableSource @MultipleEnvironmentSources
    Scenario: Combine multiple environment configuration sources
        When I setup environment variables:
            | Name                | Value                |
            | GLOBAL_APP_NAME     | Global App           |
            | GLOBAL_VERSION      | 1.0.0                |
        And I setup additional environment variables:
            | Name                    | Value                    |
            | FEATURE_ENABLE_CACHE    | true                     |
            | FEATURE_ENABLE_METRICS  | false                    |
        And I register environment variables as configuration source
        And I register additional environment variables as configuration source
        And I build the environment configuration
        Then the environment configuration should be loaded successfully
        And the environment configuration should contain "GLOBAL_APP_NAME" with value "Global App"
        And the environment configuration should contain "GLOBAL_VERSION" with value "1.0.0"
        And the environment configuration should contain "FEATURE_ENABLE_CACHE" with value "true"
        And the environment configuration should contain "FEATURE_ENABLE_METRICS" with value "false"

    @EnvironmentVariableSource @ComplexEnvironmentConfiguration
    Scenario: Load complex multi-section configuration from environment variables
        When I setup environment variables:
            | Name                               | Value                                      |
            | APPLICATION__NAME                  | Complex Environment App                    |
            | APPLICATION__VERSION               | 2.1.0                                      |
            | DATABASE__CONNECTIONSTRING         | Server=env.db.com;Database=EnvApp;         |
            | DATABASE__COMMANDTIMEOUT           | 45                                         |
            | DATABASE__MAXRETRYCOUNT            | 5                                          |
            | LOGGING__LOGLEVEL__DEFAULT         | Information                                |
            | LOGGING__LOGLEVEL__MICROSOFT       | Warning                                    |
            | LOGGING__LOGLEVEL__SYSTEM          | Error                                      |
            | EXTERNAL__API__BASEURL             | https://env.external.api.com               |
            | EXTERNAL__API__APIKEY              | env-api-key-12345                          |
            | EXTERNAL__API__TIMEOUT             | 8000                                       |
            | FEATURES__ENABLECACHING            | true                                       |
            | FEATURES__ENABLEMETRICS            | false                                      |
            | FEATURES__ENABLEADVANCEDAUTH       | true                                       |
        And I register environment variables as configuration source
        And I build the environment configuration
        Then the environment configuration should be loaded successfully
        And the environment configuration should contain "APPLICATION:NAME" with value "Complex Environment App"
        And the environment configuration should contain "APPLICATION:VERSION" with value "2.1.0"
        And the environment configuration should contain "DATABASE:CONNECTIONSTRING" with value "Server=env.db.com;Database=EnvApp;"
        And the environment configuration should contain "DATABASE:COMMANDTIMEOUT" with value "45"
        And the environment configuration should contain "DATABASE:MAXRETRYCOUNT" with value "5"
        And the environment configuration should contain "LOGGING:LOGLEVEL:DEFAULT" with value "Information"
        And the environment configuration should contain "LOGGING:LOGLEVEL:MICROSOFT" with value "Warning"
        And the environment configuration should contain "LOGGING:LOGLEVEL:SYSTEM" with value "Error"
        And the environment configuration should contain "EXTERNAL:API:BASEURL" with value "https://env.external.api.com"
        And the environment configuration should contain "EXTERNAL:API:APIKEY" with value "env-api-key-12345"
        And the environment configuration should contain "EXTERNAL:API:TIMEOUT" with value "8000"
        And the environment configuration should contain "FEATURES:ENABLECACHING" with value "true"
        And the environment configuration should contain "FEATURES:ENABLEMETRICS" with value "false"
        And the environment configuration should contain "FEATURES:ENABLEADVANCEDAUTH" with value "true"

    @EnvironmentVariableSource @FlexConfigIntegration
    Scenario: Access environment configuration through FlexConfig
        When I setup environment variables:
            | Name                    | Value                 |
            | APP__NAME               | FlexConfig Env App    |
            | APP__DEBUG              | true                  |
            | DATABASE__HOST          | flex.database.com     |
            | API__ENDPOINTS__PRIMARY | https://api1.com      |
            | API__ENDPOINTS__BACKUP  | https://api2.com      |
        And I register environment variables as configuration source
        And I build the environment configuration
        And I create FlexConfig from environment configuration
        Then the FlexConfig should be loaded successfully
        And FlexConfig should contain "APP:NAME" with value "FlexConfig Env App"
        And FlexConfig should contain "APP:DEBUG" with value "true"
        And FlexConfig should contain "DATABASE:HOST" with value "flex.database.com"
        And FlexConfig should contain "API:ENDPOINTS:PRIMARY" with value "https://api1.com"
        And FlexConfig should contain "API:ENDPOINTS:BACKUP" with value "https://api2.com"