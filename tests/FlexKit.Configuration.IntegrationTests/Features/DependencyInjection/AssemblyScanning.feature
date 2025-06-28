Feature: Assembly Scanning
    As a developer using FlexKit Configuration
    I want to automatically discover and register modules from assemblies
    So that I can avoid manual module registration and achieve modular architecture

    Background:
        Given I have initialized an assembly scanning environment

    @AssemblyScanning @BasicScanning
    Scenario: Discover modules from current domain assemblies
        When I scan assemblies in current application domain
        And I build the container with scanned modules
        Then the container should be built successfully
        And scanned modules should be registered in the container
        And the container should contain services from discovered modules

    @AssemblyScanning @ConfigurationFiltering
    Scenario: Scan assemblies with configuration-based filtering
        When I configure assembly scanning with prefix "FlexKit"
        And I scan assemblies using configuration filters
        And I build the container with filtered modules
        Then the container should be built successfully
        And only assemblies matching the prefix should be scanned
        And modules from filtered assemblies should be registered

    @AssemblyScanning @NameBasedFiltering  
    Scenario: Scan assemblies with name-based filtering
        When I configure assembly scanning with specific names:
            | AssemblyName                    |
            | FlexKit.Configuration           |
            | FlexKit.Configuration.Tests     |
        And I scan assemblies using name-based filters
        And I build the container with name-filtered modules
        Then the container should be built successfully
        And only specified assemblies should be scanned
        And modules from named assemblies should be registered

    @AssemblyScanning @NoConfigurationScanning
    Scenario: Scan assemblies without configuration (default behavior)
        When I scan assemblies without specific configuration
        And I build the container with default scanning
        Then the container should be built successfully
        And default assembly filtering should apply
        And FlexKit assemblies should be included by default

    @AssemblyScanning @ModuleDiscovery
    Scenario: Discover different types of modules in assemblies
        When I create custom scanning module "TestScanningModule"
        And I register the custom scanning module in test assembly
        And I scan assemblies for module discovery
        And I build the container with discovered modules
        Then the container should be built successfully
        And the custom scanning module should be discovered
        And services from the custom module should be registered

    @AssemblyScanning @FilteringWithJSON
    Scenario: Configure assembly scanning through JSON configuration
        When I configure assembly scanning through JSON config:
            """
            {
                "Application": {
                    "Mapping": {
                        "Prefix": "TestAssembly"
                    }
                }
            }
            """
        And I scan assemblies using JSON configuration
        And I build the container with JSON-configured scanning
        Then the container should be built successfully
        And assembly filtering should respect JSON configuration
        And only assemblies with "TestAssembly" prefix should be scanned

    @AssemblyScanning @FlexConfigIntegration
    Scenario: Assembly scanning with FlexConfig integration
        When I configure FlexConfig with assembly scanning
        And I scan assemblies for FlexConfig-related modules
        And I build the container with FlexConfig integration
        Then the container should be built successfully
        And FlexConfig should be available from scanning results
        And dynamic configuration should work with scanned modules

    @AssemblyScanning @ErrorHandling
    Scenario: Handle assembly scanning errors gracefully
        When I scan assemblies with some invalid assemblies present
        And I attempt to build container with problematic assemblies
        Then assembly scanning should handle errors gracefully
        And valid assemblies should still be processed
        And the container should be built with available modules
        And scanning errors should not prevent container creation

    @AssemblyScanning @PerformanceValidation
    Scenario: Validate assembly scanning performance
        When I scan multiple assemblies in bulk
        And I measure assembly discovery performance
        And I build the container with performance monitoring
        Then assembly scanning should complete within reasonable time
        And module discovery should be efficient
        And container building should not be significantly impacted

    @AssemblyScanning @DependencyContextScanning
    Scenario: Scan assemblies from dependency context
        When I scan assemblies from dependency context
        And I build the container with dependency context modules
        Then the container should be built successfully
        And compile-time dependencies should be scanned
        And runtime assemblies should also be included
        And all discovered modules should be registered properly