using System.IO.Abstractions.TestingHelpers;
using Autofac;
using FlexKit.IntegrationTests.Hooks;
using Reqnroll;

namespace FlexKit.IntegrationTests.Utils;

/// <summary>
/// Extension methods for ScenarioContext to simplify common operations in integration tests.
/// </summary>
public static class ScenarioContextExtensions
{
    /// <summary>
    /// Registers a disposable resource to be automatically cleaned up after the scenario.
    /// Use this for containers, file streams, HTTP clients, etc.
    /// </summary>
    /// <param name="scenarioContext">The current scenario context</param>
    /// <param name="resource">The disposable resource to register for cleanup</param>
    public static void RegisterForCleanup(this ScenarioContext scenarioContext, IDisposable resource)
    {
        TestHooks.RegisterDisposableResource(scenarioContext, resource);
    }

    // Resource cleanup registration methods

    /// <summary>
    /// Registers an Autofac container for automatic disposal after the scenario.
    /// </summary>
    /// <param name="scenarioContext">The current scenario context</param>
    /// <param name="container">The Autofac container to register</param>
    /// <returns>The same container for method chaining</returns>
    public static void RegisterAutofacContainer(this ScenarioContext scenarioContext, IContainer container)
    {
        ScenarioCleanupHooks.RegisterAutofacContainer(scenarioContext, container);
    }

    /// <summary>
    /// Registers a service provider for automatic disposal after the scenario.
    /// </summary>
    /// <param name="scenarioContext">The current scenario context</param>
    /// <param name="serviceProvider">The service provider to register</param>
    /// <returns>The same service provider for method chaining</returns>
    public static void RegisterServiceProvider(this ScenarioContext scenarioContext, IServiceProvider serviceProvider)
    {
        ScenarioCleanupHooks.RegisterServiceProvider(scenarioContext, serviceProvider);
    }

    /// <summary>
    /// Registers a mock file system for cleanup after the scenario.
    /// </summary>
    /// <param name="scenarioContext">The current scenario context</param>
    /// <param name="mockFileSystem">The mock file system to register</param>
    /// <returns>The same mock file system for method chaining</returns>
    public static void RegisterMockFileSystem(this ScenarioContext scenarioContext, MockFileSystem mockFileSystem)
    {
        ScenarioCleanupHooks.RegisterMockFileSystem(scenarioContext, mockFileSystem);
    }

    /// <summary>
    /// Sets an environment variable and registers it for restoration after the scenario.
    /// </summary>
    /// <param name="scenarioContext">The current scenario context</param>
    /// <param name="variableName">The name of the environment variable</param>
    /// <param name="value">The value to set</param>
    public static void SetEnvironmentVariable(this ScenarioContext scenarioContext, string variableName, string? value)
    {
        ScenarioCleanupHooks.RegisterEnvironmentVariableChange(scenarioContext, variableName);
        Environment.SetEnvironmentVariable(variableName, value);
    }
}