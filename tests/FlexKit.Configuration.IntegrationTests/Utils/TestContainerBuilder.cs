using Autofac;
using FlexKit.Configuration.Core;
using Reqnroll;
using FlexKit.IntegrationTests.Utils;
using JetBrains.Annotations;
// ReSharper disable NullableWarningSuppressionIsUsed

namespace FlexKit.Configuration.IntegrationTests.Utils;

/// <summary>
/// Builder for creating and configuring Autofac containers in integration tests.
/// Provides a fluent API for setting up dependency injection with FlexKit.Configuration
/// and common test services.
/// </summary>
public class TestContainerBuilder : BaseTestContainerBuilder<TestContainerBuilder>
{
    private bool _registerFlexConfig = true;

    /// <summary>
    /// Sets up FlexKit configuration with the provided builder action.
    /// </summary>
    /// <param name="configureFlexConfig">Action to configure FlexConfig</param>
    /// <returns>This builder for method chaining</returns>
    public TestContainerBuilder WithFlexConfig(Action<FlexConfigurationBuilder> configureFlexConfig)
    {
        Registrations.Add(builder => builder.AddFlexConfig(configureFlexConfig));
        return this;
    }

    /// <summary>
    /// Disables automatic FlexConfig registration.
    /// Use this when you want to manually configure FlexConfig or don't need it.
    /// </summary>
    /// <returns>This builder for method chaining</returns>
    [UsedImplicitly]
    public TestContainerBuilder WithoutFlexConfig()
    {
        _registerFlexConfig = false;
        return this;
    }

    /// <summary>
    /// Creates a minimal container with just the essential services for basic testing.
    /// </summary>
    /// <param name="scenarioContext">Optional scenario context for cleanup</param>
    /// <returns>A minimal test container</returns>
    public static IContainer CreateMinimal(ScenarioContext? scenarioContext = null)
    {
        return Create(scenarioContext!)
            .WithTestDefaults()
            .WithoutFlexConfig()
            .Build();
    }

    /// <summary>
    /// Creates a container with FlexConfig and common test services.
    /// </summary>
    /// <param name="configurationData">Configuration data for FlexConfig</param>
    /// <param name="scenarioContext">Optional scenario context for cleanup</param>
    /// <returns>A test container with FlexConfig</returns>
    public static IContainer CreateWithFlexConfig(
        Dictionary<string, string?> configurationData,
        ScenarioContext? scenarioContext = null)
    {
        return Create(scenarioContext!)
            .WithTestDefaults()
            .WithConfiguration(configurationData)
            .Build();
    }

    /// <summary>
    /// Builds the Autofac container with all configured registrations.
    /// </summary>
    /// <returns>The configured Autofac container</returns>
    public override IContainer Build()
    {
        RegisterServices();

        // Register FlexConfig if enabled and configuration is available
        if (_registerFlexConfig && Configuration != null)
        {
            ContainerBuilder.RegisterInstance(new FlexConfiguration(Configuration))
                .As<IFlexConfig>()
                .As<dynamic>()
                .SingleInstance();
        }

        return ApplyRegistrations();
    }
}