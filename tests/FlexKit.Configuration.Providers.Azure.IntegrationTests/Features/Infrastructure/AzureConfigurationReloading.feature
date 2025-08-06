Feature: Azure Configuration Reloading
As a developer building dynamic applications
I want to test automatic reloading of Azure configuration
So that I can respond to configuration changes without restarting

    @Reloading @KeyVault @DynamicUpdates
    Scenario: Test automatic Key Vault secret reloading
        Given I have established a reloading controller environment
        And I have reloading controller configuration with auto-reload Key Vault from "infrastructure-module-azure-config.json"
        When I configure reloading controller with automatic reloading enabled
        And I configure reloading controller by building the configuration
        And I update secrets in the Key Vault
        And I wait for automatic reload to trigger
        Then the reloading controller should detect Key Vault changes
        And the reloading controller configuration should contain updated secret values
        And the reloading controller should demonstrate change notification capabilities

    @Reloading @AppConfiguration @DynamicUpdates
    Scenario: Test automatic App Configuration reloading
        Given I have established a reloading controller environment
        And I have reloading controller configuration with auto-reload App Configuration from "infrastructure-module-azure-config.json"
        When I configure reloading controller with automatic reloading enabled
        And I configure reloading controller by building the configuration
        And I update configuration in App Configuration
        And I wait for automatic reload to trigger
        Then the reloading controller should detect App Configuration changes
        And the reloading controller configuration should contain updated configuration values
        And the reloading controller should demonstrate real-time configuration updates

    @Reloading @CombinedSources @ChangeManagement
    Scenario: Test reloading with combined Azure sources
        Given I have established a reloading controller environment
        And I have reloading controller configuration with auto-reload Key Vault from "infrastructure-module-azure-config.json"
        And I have reloading controller configuration with auto-reload App Configuration from "infrastructure-module-azure-config.json"
        When I configure reloading controller with automatic reloading enabled
        And I configure reloading controller by building the configuration
        And I update both Key Vault and App Configuration
        And I wait for automatic reload to trigger
        Then the reloading controller should detect changes in both sources
        And the reloading controller should handle combined source reloading
        And the reloading controller should maintain proper precedence during reloading

    @Reloading @ErrorRecovery @Resilience
    Scenario: Test configuration reloading with error recovery
        Given I have established a reloading controller environment
        And I have reloading controller configuration with error-prone auto-reload from "infrastructure-module-azure-config.json"
        When I configure reloading controller with automatic reloading enabled
        And I configure reloading controller by building the configuration
        And I simulate reload errors in Azure services
        And I wait for error recovery mechanisms to activate
        Then the reloading controller should handle reload errors gracefully
        And the reloading controller should attempt error recovery
        And the reloading controller should maintain last known good configuration