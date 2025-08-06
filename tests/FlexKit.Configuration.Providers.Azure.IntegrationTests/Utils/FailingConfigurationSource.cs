using JetBrains.Annotations;
using Microsoft.Extensions.Configuration;

namespace FlexKit.Configuration.Providers.Azure.IntegrationTests.Utils;

/// <summary>
/// A configuration source that always fails to load, used for testing error scenarios.
/// </summary>
public class FailingConfigurationSource : IConfigurationSource
{
    /// <summary>
    /// Gets or sets the error message to throw.
    /// </summary>
    public string ErrorMessage { get; init; } = "Simulated configuration failure";

    /// <summary>
    /// Gets or sets the type of source for error messages.
    /// </summary>
    public string SourceType { [UsedImplicitly] get; set; } = "Test";

    /// <summary>
    /// Builds the configuration provider, which will fail during Load().
    /// </summary>
    /// <param name="builder">The configuration builder</param>
    /// <returns>A failing configuration provider</returns>
    public IConfigurationProvider Build(IConfigurationBuilder builder)
    {
        return new FailingConfigurationProvider(this);
    }
}

/// <summary>
/// A configuration provider that always fails to load, used for testing error scenarios.
/// </summary>
public class FailingConfigurationProvider : ConfigurationProvider
{
    private readonly FailingConfigurationSource _source;

    /// <summary>
    /// Initializes a new failing configuration provider.
    /// </summary>
    /// <param name="source">The failing configuration source</param>
    public FailingConfigurationProvider(FailingConfigurationSource source)
    {
        _source = source;
    }

    /// <summary>
    /// Loads configuration data by throwing an exception to simulate failure.
    /// </summary>
    public override void Load()
    {
        throw new InvalidOperationException(_source.ErrorMessage);
    }
}
