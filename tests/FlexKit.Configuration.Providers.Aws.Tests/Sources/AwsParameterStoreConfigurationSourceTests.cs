// <copyright file="AwsParameterStoreConfigurationSourceTests.cs" company="FlexKit">
// Copyright (c) FlexKit. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

using Amazon.Extensions.NETCore.Setup;
using Amazon.Runtime;
using FlexKit.Configuration.Providers.Aws.Sources;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using NSubstitute;
using Xunit;
// ReSharper disable ClassTooBig
// ReSharper disable NullableWarningSuppressionIsUsed

namespace FlexKit.Configuration.Providers.Aws.Tests.Sources;

/// <summary>
/// Unit tests for <see cref="AwsParameterStoreConfigurationSource"/>.
/// Tests cover property initialization, validation, and provider creation functionality.
/// </summary>
public class AwsParameterStoreConfigurationSourceTests
{
    #region Property Tests

    [Fact]
    public void Path_CanBeSetAndRetrieved()
    {
        // Arrange
        var source = new AwsParameterStoreConfigurationSource();
        const string path = "/test/app/";

        // Act
        source.Path = path;

        // Assert
        source.Path.Should().Be(path);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Optional_CanBeSetAndRetrieved(bool optional)
    {
        // Arrange
        var source = new AwsParameterStoreConfigurationSource
        {
            // Act
            Optional = optional,
        };

        // Assert
        source.Optional.Should().Be(optional);
    }

    [Fact]
    public void AwsOptions_CanBeSetAndRetrieved()
    {
        // Arrange
        var source = new AwsParameterStoreConfigurationSource();
        var awsOptions = new AWSOptions();

        // Act
        source.AwsOptions = awsOptions;

        // Assert
        source.AwsOptions.Should().BeSameAs(awsOptions);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void JsonProcessor_CanBeSetAndRetrieved(bool jsonProcessor)
    {
        // Arrange
        var source = new AwsParameterStoreConfigurationSource
        {
            // Act
            JsonProcessor = jsonProcessor,
        };

        // Assert
        source.JsonProcessor.Should().Be(jsonProcessor);
    }

    [Fact]
    public void JsonProcessorPaths_CanBeSetAndRetrieved()
    {
        // Arrange
        var source = new AwsParameterStoreConfigurationSource();
        var paths = new[] { "/test/database/", "/test/cache/" };

        // Act
        source.JsonProcessorPaths = paths;

        // Assert
        source.JsonProcessorPaths.Should().BeEquivalentTo(paths);
    }

    [Fact]
    public void JsonProcessorPaths_CanBeSetToNull()
    {
        // Arrange
        var source = new AwsParameterStoreConfigurationSource
        {
            // Act
            JsonProcessorPaths = null,
        };

        // Assert
        source.JsonProcessorPaths.Should().BeNull();
    }

    [Fact]
    public void ReloadAfter_CanBeSetAndRetrieved()
    {
        // Arrange
        var source = new AwsParameterStoreConfigurationSource();
        var reloadAfter = TimeSpan.FromMinutes(5);

        // Act
        source.ReloadAfter = reloadAfter;

        // Assert
        source.ReloadAfter.Should().Be(reloadAfter);
    }

    [Fact]
    public void ParameterProcessor_CanBeSetAndRetrieved()
    {
        // Arrange
        var source = new AwsParameterStoreConfigurationSource();
        var processor = Substitute.For<IParameterProcessor>();

        // Act
        source.ParameterProcessor = processor;

        // Assert
        source.ParameterProcessor.Should().BeSameAs(processor);
    }

    [Fact]
    public void ParameterProcessor_CanBeSetToNull()
    {
        // Arrange
        var source = new AwsParameterStoreConfigurationSource
        {
            // Act
            ParameterProcessor = null,
        };

        // Assert
        source.ParameterProcessor.Should().BeNull();
    }

    [Fact]
    public void OnLoadException_CanBeSetAndRetrieved()
    {
        // Arrange
        var source = new AwsParameterStoreConfigurationSource();
        var exceptionHandler = new Action<ConfigurationProviderException>(_ => { });

        // Act
        source.OnLoadException = exceptionHandler;

        // Assert
        source.OnLoadException.Should().BeSameAs(exceptionHandler);
    }

    [Fact]
    public void OnLoadException_CanBeSetToNull()
    {
        // Arrange
        var source = new AwsParameterStoreConfigurationSource
        {
            // Act
            OnLoadException = null,
        };

        // Assert
        source.OnLoadException.Should().BeNull();
    }

    #endregion

    #region Default Values Tests

    [Fact]
    public void NewInstance_HasExpectedDefaultValues()
    {
        // Act
        var source = new AwsParameterStoreConfigurationSource();

        // Assert
        source.Path.Should().Be(string.Empty);
        source.Optional.Should().BeTrue(); // Optional defaults to true
        source.AwsOptions.Should().BeNull();
        source.JsonProcessor.Should().BeFalse();
        source.JsonProcessorPaths.Should().BeNull();
        source.ReloadAfter.Should().BeNull();
        source.ParameterProcessor.Should().BeNull();
        source.OnLoadException.Should().BeNull();
    }

    #endregion

    [Fact]
    public void Build_WithValidBuilderAndValidAwsCredentials_ReturnsProvider()
    {
        // Arrange
        var source = new AwsParameterStoreConfigurationSource
        {
            Path = "/test/",
            AwsOptions = CreateMockedAwsOptions()
        };
        var builder = Substitute.For<IConfigurationBuilder>();

        // Act
        var provider = source.Build(builder);

        // Assert
        provider.Should().NotBeNull();
        provider.Should().BeOfType<AwsParameterStoreConfigurationProvider>();
    }

    [Fact]
    public void Build_WithNullBuilder_ThrowsArgumentNullException()
    {
        // Arrange
        var source = new AwsParameterStoreConfigurationSource
        {
            AwsOptions = CreateMockedAwsOptions()
        };

        // Act & Assert
        source.Invoking(s => s.Build(null!))
            .Should().Throw<ArgumentNullException>()
            .WithParameterName("builder");
    }

    [Fact]
    public void Build_WithInvalidAwsCredentials_ThrowsInvalidOperationException()
    {
        // Arrange
        var source = new AwsParameterStoreConfigurationSource
        {
            Path = "/test/",
            AwsOptions = new AWSOptions
            {
                Region = Amazon.RegionEndpoint.GetBySystemName("invalid-region")
            }
        };
        var builder = Substitute.For<IConfigurationBuilder>();

        // Act & Assert
        source.Invoking(s => s.Build(builder))
            .Should().Throw<InvalidOperationException>()
            .WithMessage("Failed to create AWS Systems Manager client*");
    }

    [Fact]
    public void Build_CreatesProviderWithSameSourceInstance()
    {
        // Arrange
        var source = new AwsParameterStoreConfigurationSource
        {
            Path = "/test/",
            Optional = true,
            JsonProcessor = true,
            AwsOptions = CreateMockedAwsOptions()
        };
        var builder = Substitute.For<IConfigurationBuilder>();

        // Act
        var provider = source.Build(builder) as AwsParameterStoreConfigurationProvider;

        // Assert
        provider.Should().NotBeNull();

        // Use reflection to access the private _source field to verify it's the same instance
        var sourceField = typeof(AwsParameterStoreConfigurationProvider)
            .GetField("_source", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        sourceField.Should().NotBeNull();
        var providerSource = sourceField.GetValue(provider);
        providerSource.Should().BeSameAs(source);
    }

    [Fact]
    public void Build_MultipleCalls_ReturnsDifferentProviderInstances()
    {
        // Arrange
        var source = new AwsParameterStoreConfigurationSource
        {
            AwsOptions = CreateMockedAwsOptions()
        };
        var builder = Substitute.For<IConfigurationBuilder>();

        // Act
        var provider1 = source.Build(builder);
        var provider2 = source.Build(builder);

        // Assert
        provider1.Should().NotBeSameAs(provider2);
        provider1.Should().BeOfType<AwsParameterStoreConfigurationProvider>();
        provider2.Should().BeOfType<AwsParameterStoreConfigurationProvider>();
    }

    #region Property Chaining Tests

    [Fact]
    public void Properties_SupportFluentConfiguration()
    {
        // Arrange & Act
        var source = new AwsParameterStoreConfigurationSource
        {
            Path = "/test/app/",
            Optional = true,
            JsonProcessor = true,
            JsonProcessorPaths = ["/test/database/"],
            ReloadAfter = TimeSpan.FromMinutes(10),
            AwsOptions = new AWSOptions(),
            ParameterProcessor = Substitute.For<IParameterProcessor>(),
            OnLoadException = _ => { }
        };

        // Assert
        source.Path.Should().Be("/test/app/");
        source.Optional.Should().BeTrue();
        source.JsonProcessor.Should().BeTrue();
        source.JsonProcessorPaths.Should().ContainSingle("/test/database/");
        source.ReloadAfter.Should().Be(TimeSpan.FromMinutes(10));
        source.AwsOptions.Should().NotBeNull();
        source.ParameterProcessor.Should().NotBeNull();
        source.OnLoadException.Should().NotBeNull();
    }

    #endregion

    #region Edge Cases Tests

    [Fact]
    public void JsonProcessorPaths_WithEmptyArray_SetsEmptyArray()
    {
        // Arrange
        var source = new AwsParameterStoreConfigurationSource();
        var emptyPaths = Array.Empty<string>();

        // Act
        source.JsonProcessorPaths = emptyPaths;

        // Assert
        source.JsonProcessorPaths.Should().BeSameAs(emptyPaths);
        source.JsonProcessorPaths.Should().BeEmpty();
    }

    [Fact]
    public void ReloadAfter_WithZeroTimeSpan_SetsZeroTimeSpan()
    {
        // Arrange
        var source = new AwsParameterStoreConfigurationSource();
        var zeroTimeSpan = TimeSpan.Zero;

        // Act
        source.ReloadAfter = zeroTimeSpan;

        // Assert
        source.ReloadAfter.Should().Be(zeroTimeSpan);
    }

    [Fact]
    public void ReloadAfter_WithNegativeTimeSpan_SetsNegativeTimeSpan()
    {
        // Arrange
        var source = new AwsParameterStoreConfigurationSource();
        var negativeTimeSpan = TimeSpan.FromSeconds(-1);

        // Act
        source.ReloadAfter = negativeTimeSpan;

        // Assert
        source.ReloadAfter.Should().Be(negativeTimeSpan);
    }

    [Fact]
    public void Path_WithEmptyString_SetsEmptyString()
    {
        // Arrange
        var source = new AwsParameterStoreConfigurationSource
        {
            // Act
            Path = string.Empty,
        };

        // Assert
        source.Path.Should().Be(string.Empty);
    }

    [Fact]
    public void Path_WithWhitespace_SetsWhitespace()
    {
        // Arrange
        var source = new AwsParameterStoreConfigurationSource();
        const string whitespace = "   ";

        // Act
        source.Path = whitespace;

        // Assert
        source.Path.Should().Be(whitespace);
    }

    #endregion

    #region Interface Implementation Tests

    [Fact]
    public void ImplementsIConfigurationSource()
    {
        // Arrange & Act
        var source = new AwsParameterStoreConfigurationSource();

        // Assert
        source.Should().BeAssignableTo<IConfigurationSource>();
    }

    [Fact]
    public void IConfigurationSource_Build_CallsPublicBuildMethod()
    {
        // Arrange
        IConfigurationSource source = new AwsParameterStoreConfigurationSource
        {
            AwsOptions = CreateMockedAwsOptions()
        };
        var builder = Substitute.For<IConfigurationBuilder>();

        // Act
        var provider = source.Build(builder);

        // Assert
        provider.Should().NotBeNull();
        provider.Should().BeOfType<AwsParameterStoreConfigurationProvider>();
    }

    #endregion

    #region Equality and State Tests

    [Fact]
    public void TwoInstances_WithSameProperties_AreNotEqual()
    {
        // Arrange
        var source1 = new AwsParameterStoreConfigurationSource
        {
            Path = "/test/",
            Optional = true,
            JsonProcessor = true
        };

        var source2 = new AwsParameterStoreConfigurationSource
        {
            Path = "/test/",
            Optional = true,
            JsonProcessor = true
        };

        // Act & Assert
        source1.Should().NotBeSameAs(source2);
        source1.Should().NotBe(source2); // Reference equality, not value equality
    }

    [Fact]
    public void PropertyModification_AfterCreation_UpdatesProperty()
    {
        // Arrange
        var source = new AwsParameterStoreConfigurationSource
        {
            Path = "/original/"
        };

        // Act
        source.Path = "/modified/";

        // Assert
        source.Path.Should().Be("/modified/");
    }

    #endregion

    #region Complex Scenarios Tests

    [Fact]
    public void Build_WithComplexConfiguration_CreatesProviderSuccessfully()
    {
        // Arrange
        var processor = Substitute.For<IParameterProcessor>();
        var exceptionHandler = new Action<ConfigurationProviderException>(_ => { });

        var source = new AwsParameterStoreConfigurationSource
        {
            Path = "/prod/myapp/",
            Optional = false,
            AwsOptions = CreateMockedAwsOptions(),
            JsonProcessor = true,
            JsonProcessorPaths = ["/prod/myapp/database/", "/prod/myapp/cache/"],
            ReloadAfter = TimeSpan.FromMinutes(15),
            ParameterProcessor = processor,
            OnLoadException = exceptionHandler
        };

        var builder = Substitute.For<IConfigurationBuilder>();

        // Act
        var provider = source.Build(builder);

        // Assert
        provider.Should().NotBeNull();
        provider.Should().BeOfType<AwsParameterStoreConfigurationProvider>();
    }

    [Fact]
    public void Build_WithMinimalConfiguration_CreatesProviderSuccessfully()
    {
        // Arrange
        var source = new AwsParameterStoreConfigurationSource
        {
            Path = "/simple/",
            AwsOptions = CreateMockedAwsOptions()
        };

        var builder = Substitute.For<IConfigurationBuilder>();

        // Act
        var provider = source.Build(builder);

        // Assert
        provider.Should().NotBeNull();
        provider.Should().BeOfType<AwsParameterStoreConfigurationProvider>();
    }

    #endregion

    private static AWSOptions CreateMockedAwsOptions()
    {
        var mockCredentials = Substitute.For<AWSCredentials>();
        mockCredentials.GetCredentials().Returns(new ImmutableCredentials("fake-key", "fake-secret", "fake-token"));

        return new AWSOptions
        {
            Credentials = mockCredentials,
            Region = Amazon.RegionEndpoint.USEast1
        };
    }
}