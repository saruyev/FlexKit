Feature: YAML Basic Access
  As a developer using FlexKit Configuration with YAML provider
  I want to access YAML configuration data using basic patterns
  So that I can retrieve configuration values using string indexer, dynamic access, and section navigation

Background:
  Given I have initialized a yaml module configuration environment

Scenario: Access simple YAML values using string indexer
  Given I have a yaml module source with the following data:
    | Key           | Value                |
    | AppName       | My YAML Application  |
    | Version       | 2.1.0                |
    | Debug         | true                 |
  When I utilize yaml access with string indexer key "AppName"
  Then the yaml string result should be "My YAML Application"

Scenario: Access nested YAML values using colon notation
  Given I have a yaml module source with the following data:
    | Key                    | Value                    |
    | Database:Host          | yaml-db.example.com      |
    | Database:Port          | 5432                     |
    | Database:Name          | yamlapp_db               |
    | Api:BaseUrl            | https://yaml-api.com     |
    | Api:Timeout            | 8000                     |
  When I utilize yaml access with string indexer key "Database:Host"
  Then the yaml string result should be "yaml-db.example.com"
  When I utilize yaml access with string indexer key "Api:Timeout"
  Then the yaml string result should be "8000"

Scenario: Access YAML values using dynamic property syntax
  Given I have a yaml module source with content:
    """
    application:
      name: "YAML Dynamic App"
      environment: "staging"
      settings:
        enableCache: true
        logLevel: "Info"
    external:
      apiKey: "yaml-secret-key"
      timeout: 6000
    """
  When I utilize yaml access with dynamic property "application.name"
  Then the yaml dynamic result should be "YAML Dynamic App"
  When I utilize yaml access with dynamic property "external.apiKey"
  Then the yaml dynamic result should be "yaml-secret-key"

Scenario: Access YAML configuration sections
  Given I have a yaml module source with content:
    """
    database:
      connectionString: "Server=yaml-server;Database=yamldb;"
      poolSize: 25
    cache:
      provider: "redis"
      host: "yaml-cache.example.com"
      port: 6379
    """
  When I utilize yaml section access for "database"
  Then the yaml section result should not be null
  And the yaml section "connectionString" should have value "Server=yaml-server;Database=yamldb;"
  And the yaml section "poolSize" should have value "25"

Scenario: Access YAML array elements using indexed notation
  Given I have a yaml module source with content:
    """
    servers:
      - name: "yaml-web1"
        port: 8080
      - name: "yaml-web2"
        port: 8081
    features:
      - "yamlCaching"
      - "yamlLogging"
      - "yamlMetrics"
    """
  When I utilize yaml indexed access for "servers:0:name"
  Then the yaml string result should be "yaml-web1"
  When I utilize yaml indexed access for "servers:1:port"
  Then the yaml string result should be "8081"
  When I utilize yaml indexed access for "features:0"
  Then the yaml string result should be "yamlCaching"
  When I utilize yaml indexed access for "features:2"
  Then the yaml string result should be "yamlMetrics"

Scenario: Handle missing YAML configuration keys gracefully
  Given I have a yaml module source with the following data:
    | Key              | Value            |
    | ExistingKey      | yaml-value       |
    | Another:Setting  | yaml-setting     |
  When I utilize yaml access with string indexer key "NonExistentKey"
  Then the yaml string result should be null
  When I utilize yaml access with dynamic property "missing.property"
  Then the yaml dynamic result should be null

Scenario: Verify YAML configuration key existence
  Given I have a yaml module source with content:
    """
    app:
      title: "YAML Test App"
      version: "3.0.0"
    database:
      enabled: true
    """
  Then the yaml configuration should contain key "app:title"
  And the yaml configuration should contain key "database:enabled"
  And the yaml configuration should not contain key "nonexistent:key"

Scenario: Navigate complex YAML nested structures
  Given I have a yaml module source with content:
    """
    application:
      services:
        authentication:
          provider: "oauth2"
          settings:
            clientId: "yaml-client-123"
            redirectUrl: "https://yaml-app.com/callback"
        database:
          primary:
            host: "yaml-primary.db.com"
            credentials:
              username: "yamluser"
              timeout: 30
    """
  When I utilize yaml dynamic navigation to "application.services.authentication.provider"
  Then the yaml dynamic result should be "oauth2"
  When I utilize yaml dynamic navigation to "application.services.database.primary.host"
  Then the yaml dynamic result should be "yaml-primary.db.com"
  When I utilize yaml access with string indexer key "application:services:authentication:settings:clientId"
  Then the yaml string result should be "yaml-client-123"

Scenario: Access YAML configuration with special characters and quoted values
  Given I have a yaml module source with content:
    """
    special:
      quoted: "Value with spaces and 'quotes'"
      escaped: "Line 1\nLine 2\nLine 3"
      unicode: "YAML: 你好世界 🌏"
    paths:
      windows: "C:\\yaml\\config\\path"
      unix: "/yaml/config/path"
    """
  When I utilize yaml access with string indexer key "special:quoted"
  Then the yaml string result should be "Value with spaces and 'quotes'"
  When I utilize yaml access with string indexer key "special:unicode"
  Then the yaml string result should be "YAML: 你好世界 🌏"
  When I utilize yaml access with string indexer key "paths:windows"
  Then the yaml string result should be "C:\yaml\config\path"

Scenario: Combine multiple YAML access patterns in sequence
  Given I have a yaml module source with content:
    """
    app:
      name: "Multi-Access YAML App"
      version: "1.5.0"
    config:
      database:
        host: "yaml-multi.db.com"
      cache:
        enabled: true
    features:
      - "multiAuth"
      - "multiCache"
    """
  When I utilize yaml access with string indexer key "app:name"
  And I utilize yaml access with dynamic property "config.database.host"
  And I utilize yaml indexed access for "features:1"
  And I utilize yaml section access for "config"
  Then the yaml access results should include:
    | Result                                                    |
    | StringIndexer[app:name] = Multi-Access YAML App          |
    | Dynamic[config.database.host] = yaml-multi.db.com        |
    | IndexedAccess[features:1] = multiCache                   |
    | Section[config] = Section                                |