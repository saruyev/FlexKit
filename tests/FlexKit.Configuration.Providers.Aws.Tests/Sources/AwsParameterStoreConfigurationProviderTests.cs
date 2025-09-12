using System.Reflection;
using Amazon.Runtime;
using Amazon.SimpleSystemsManagement;
using Amazon.SimpleSystemsManagement.Model;
using FlexKit.Configuration.Providers.Aws.Sources;
using FluentAssertions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;
// ReSharper disable AccessToDisposedClosure
// ReSharper disable DisposeOnUsingVariable
// ReSharper disable ClassTooBig
// ReSharper disable ComplexConditionExpression
// ReSharper disable NullableWarningSuppressionIsUsed
// ReSharper disable MethodTooLong
#pragma warning disable CS0618 // Type or member is obsolete

namespace FlexKit.Configuration.Providers.Aws.Tests.Sources;

/// <summary>
/// Unit tests for AwsParameterStoreConfigurationProvider covering all functionality.
/// 
/// NOTE: This requires InternalsVisibleTo to test internal methods like ShouldProcessAsJson.
/// Add to FlexKit.Configuration.Providers.Aws.csproj:
/// &lt;ItemGroup&gt;
///   &lt;InternalsVisibleTo Include="FlexKit.Configuration.Providers.Aws.Tests" /&gt;
/// &lt;/ItemGroup&gt;
/// </summary>
public class AwsParameterStoreConfigurationProviderTests : IDisposable
{
    private readonly IAmazonSimpleSystemsManagement _mockSsmClient;
    private readonly AwsParameterStoreConfigurationSource _source;
    private readonly AwsParameterStoreConfigurationProvider _provider;

    public AwsParameterStoreConfigurationProviderTests()
    {
        _mockSsmClient = Substitute.For<IAmazonSimpleSystemsManagement>();
        _source = new AwsParameterStoreConfigurationSource
        {
            Path = "/test/",
            Optional = true,
            AwsOptions = new Amazon.Extensions.NETCore.Setup.AWSOptions
            {
                Credentials = new AnonymousAWSCredentials(),
                Region = Amazon.RegionEndpoint.USEast1
            }
        };

        // Use reflection to create a provider with a mocked SSM client
        _provider = new AwsParameterStoreConfigurationProvider(_source);
        var ssmClientField = typeof(AwsParameterStoreConfigurationProvider)
            .GetField("_ssmClient", BindingFlags.NonPublic | BindingFlags.Instance);

        ssmClientField!.SetValue(_provider, _mockSsmClient);
    }

    public void Dispose()
    {
        _provider.Dispose();
        _mockSsmClient.Dispose();
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithNullSource_ThrowsArgumentNullException()
    {
        // Act & Assert
        var action = () => new AwsParameterStoreConfigurationProvider(null!);
        action.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Constructor_WithValidSource_CreatesProvider()
    {
        // Arrange & Act
        using var provider = new AwsParameterStoreConfigurationProvider(_source);

        // Assert
        provider.Should().NotBeNull();
    }
    
    private static readonly Lock AwsTestLock = new();

    [Fact]
    public void Constructor_WithInvalidAwsCredentials_ThrowsInvalidOperationException()
    {
        // Ensure only one AWS-related test runs at a time
        lock (AwsTestLock)
        {
            // Store original state
            var originalEnv = new Dictionary<string, string>
            {
                ["AWS_ACCESS_KEY_ID"] = Environment.GetEnvironmentVariable("AWS_ACCESS_KEY_ID")!,
                ["AWS_SECRET_ACCESS_KEY"] = Environment.GetEnvironmentVariable("AWS_SECRET_ACCESS_KEY")!,
                ["AWS_SESSION_TOKEN"] = Environment.GetEnvironmentVariable("AWS_SESSION_TOKEN")!,
                ["AWS_PROFILE"] = Environment.GetEnvironmentVariable("AWS_PROFILE")!,
                ["AWS_SHARED_CREDENTIALS_FILE"] = Environment.GetEnvironmentVariable("AWS_SHARED_CREDENTIALS_FILE")!,
                ["AWS_CONFIG_FILE"] = Environment.GetEnvironmentVariable("AWS_CONFIG_FILE")!
            };

            var homeDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            var credFile = Path.Combine(homeDir, ".aws", "credentials");
            var configFile = Path.Combine(homeDir, ".aws", "config");
            var credBackup = credFile + $".backup-{Guid.NewGuid()}";
            var configBackup = configFile + $".backup-{Guid.NewGuid()}";
            bool credMoved = false, configMoved = false;

            try
            {
                // 1. Clear environment
                foreach (var key in originalEnv.Keys)
                {
                    Environment.SetEnvironmentVariable(key, null);
                }

                Environment.SetEnvironmentVariable("AWS_SHARED_CREDENTIALS_FILE", "/nonexistent/credentials");
                Environment.SetEnvironmentVariable("AWS_CONFIG_FILE", "/nonexistent/config");

                // 2. Move credential files
                if (File.Exists(credFile))
                {
                    File.Move(credFile, credBackup);
                    credMoved = true;
                }

                if (File.Exists(configFile))
                {
                    File.Move(configFile, configBackup);
                    configMoved = true;
                }
                
                FallbackCredentialsFactory.Reset();

                // Also clear the cached credentials using reflection
                var fallbackType = typeof(FallbackCredentialsFactory);
                var cachedCredsField =
                    fallbackType.GetField("cachedCredentials", BindingFlags.Static | BindingFlags.NonPublic);
                cachedCredsField?.SetValue(null, null);

                // Clear any ClientFactory cache if it exists
                var clientFactoryType = typeof(Amazon.Extensions.NETCore.Setup.AWSOptions).Assembly
                    .GetType("Amazon.Extensions.NETCore.Setup.ClientFactory");
                if (clientFactoryType != null)
                {
                    var fields = clientFactoryType.GetFields(BindingFlags.Static | BindingFlags.NonPublic);
                    foreach (var field in fields)
                    {
                        if (field.Name.Contains("cache", StringComparison.OrdinalIgnoreCase))
                        {
                            try
                            {
                                field.SetValue(null, null);
                            }
                            catch
                            {
                                // ignored
                            }
                        }
                    }
                }

                // 4. Create the source
                var invalidSource = new AwsParameterStoreConfigurationSource
                {
                    Path = "/test/",
                    AwsOptions = new Amazon.Extensions.NETCore.Setup.AWSOptions
                    {
                        Region = Amazon.RegionEndpoint.USEast1,
                        Profile = "nonexistent-profile"
                    }
                };

                // Act & Assert
                var action = () => new AwsParameterStoreConfigurationProvider(invalidSource);
                action.Should().Throw<InvalidOperationException>()
                    .WithMessage("Failed to create AWS Systems Manager client*");
            }
            finally
            {
                // Restore everything
                foreach (var kvp in originalEnv)
                {
                    Environment.SetEnvironmentVariable(kvp.Key, kvp.Value);
                }

                if (credMoved && File.Exists(credBackup))
                {
                    File.Move(credBackup, credFile);
                }

                if (configMoved && File.Exists(configBackup))
                {
                    File.Move(configBackup, configFile);
                }

                FallbackCredentialsFactory.Reset();
            }
        }
    }

    #endregion

    #region Load Method Tests

    [Fact]
    public void Load_WithValidParameters_LoadsConfigurationSuccessfully()
    {
        // Arrange
        var parameters = new List<Parameter>
        {
            new() { Name = "/test/app/database/host", Value = "localhost", Type = "String" },
            new() { Name = "/test/app/database/port", Value = "5432", Type = "String" },
            new() { Name = "/test/app/features", Value = "cache,logging", Type = "StringList" }
        };

        _mockSsmClient.GetParametersByPathAsync(Arg.Any<GetParametersByPathRequest>())
            .Returns(
                new GetParametersByPathResponse
                {
                    Parameters = parameters,
                    NextToken = null
                });

        // Act
        _provider.Load();

        // Assert
        _provider.TryGet("app:database:host", out var host).Should().BeTrue();
        host.Should().Be("localhost");

        _provider.TryGet("app:database:port", out var port).Should().BeTrue();
        port.Should().Be("5432");

        _provider.TryGet("app:features:0", out var feature1).Should().BeTrue();
        feature1.Should().Be("cache");

        _provider.TryGet("app:features:1", out var feature2).Should().BeTrue();
        feature2.Should().Be("logging");
    }

    [Fact]
    public void Load_WithJsonParameter_FlattensJsonWhenEnabled()
    {
        // Arrange
        _source.JsonProcessor = true;
        var jsonParameter = new Parameter
        {
            Name = "/test/config",
            Value = """{"database": {"host": "localhost", "port": 5432}, "enabled": true}""",
            Type = "String"
        };

        _mockSsmClient.GetParametersByPathAsync(Arg.Any<GetParametersByPathRequest>())
            .Returns(
                new GetParametersByPathResponse
                {
                    Parameters = [jsonParameter],
                    NextToken = null
                });

        // Act
        _provider.Load();

        // Assert
        _provider.TryGet("config:database:host", out var host).Should().BeTrue();
        host.Should().Be("localhost");

        _provider.TryGet("config:database:port", out var port).Should().BeTrue();
        port.Should().Be("5432");

        _provider.TryGet("config:enabled", out var enabled).Should().BeTrue();
        enabled.Should().Be("true");
    }

    [Fact]
    public void Load_WithSecureStringParameter_ProcessesCorrectly()
    {
        // Arrange
        var secureParameter = new Parameter
        {
            Name = "/test/password",
            Value = "secret-password",
            Type = "SecureString"
        };

        _mockSsmClient.GetParametersByPathAsync(Arg.Any<GetParametersByPathRequest>())
            .Returns(
                new GetParametersByPathResponse
                {
                    Parameters = [secureParameter],
                    NextToken = null
                });

        // Act
        _provider.Load();

        // Assert
        _provider.TryGet("password", out var password).Should().BeTrue();
        password.Should().Be("secret-password");
    }

    [Fact]
    public void Load_WithUnknownParameterType_TreatsAsString()
    {
        // Arrange
        var unknownParameter = new Parameter
        {
            Name = "/test/unknown",
            Value = "some-value",
            Type = "UnknownType"
        };

        _mockSsmClient.GetParametersByPathAsync(Arg.Any<GetParametersByPathRequest>())
            .Returns(
                new GetParametersByPathResponse
                {
                    Parameters = [unknownParameter],
                    NextToken = null
                });

        // Act
        _provider.Load();

        // Assert
        _provider.TryGet("unknown", out var value).Should().BeTrue();
        value.Should().Be("some-value");
    }

    [Fact]
    public void Load_WithPaginatedResults_LoadsAllParameters()
    {
        // Arrange
        var firstPage = new GetParametersByPathResponse
        {
            Parameters = [new() { Name = "/test/param1", Value = "value1", Type = "String" }],
            NextToken = "next-token"
        };

        var secondPage = new GetParametersByPathResponse
        {
            Parameters = [new() { Name = "/test/param2", Value = "value2", Type = "String" }],
            NextToken = null
        };

        _mockSsmClient.GetParametersByPathAsync(Arg.Is<GetParametersByPathRequest>(r => r.NextToken == null))
            .Returns(firstPage);

        _mockSsmClient.GetParametersByPathAsync(Arg.Is<GetParametersByPathRequest>(r => r.NextToken == "next-token"))
            .Returns(secondPage);

        // Act
        _provider.Load();

        // Assert
        _provider.TryGet("param1", out var value1).Should().BeTrue();
        value1.Should().Be("value1");

        _provider.TryGet("param2", out var value2).Should().BeTrue();
        value2.Should().Be("value2");
    }

    [Fact]
    public void Load_WhenOptionalAndFails_InvokesOnLoadException()
    {
        // Arrange
        var exceptionInvoked = false;
        ConfigurationProviderException? capturedException = null;

        _source.OnLoadException = ex =>
        {
            exceptionInvoked = true;
            capturedException = ex;
        };

        _mockSsmClient.GetParametersByPathAsync(Arg.Any<GetParametersByPathRequest>())
            .Throws(new AmazonSimpleSystemsManagementException("Access denied"));

        // Act
        _provider.Load();

        // Assert
        exceptionInvoked.Should().BeTrue();
        capturedException.Should().NotBeNull();
        capturedException!.Source.Should().Be("/test/");
        capturedException.InnerException.Should().BeOfType<AmazonSimpleSystemsManagementException>();
    }

    [Fact]
    public void Load_WhenRequiredAndFails_ThrowsInvalidOperationException()
    {
        // Arrange
        _source.Optional = false;
        _mockSsmClient.GetParametersByPathAsync(Arg.Any<GetParametersByPathRequest>())
            .Throws(new AmazonSimpleSystemsManagementException("Access denied"));

        // Act & Assert
        var action = () => _provider.Load();
        action.Should().Throw<InvalidOperationException>()
            .WithMessage("Failed to load configuration from AWS Parameter Store path '/test/'*")
            .WithInnerException<AmazonSimpleSystemsManagementException>();
    }

    [Fact]
    public void Load_WithEmptyStringListParameter_SkipsProcessing()
    {
        // Arrange
        var emptyListParameter = new Parameter
        {
            Name = "/test/empty-list",
            Value = "",
            Type = "StringList"
        };

        _mockSsmClient.GetParametersByPathAsync(Arg.Any<GetParametersByPathRequest>())
            .Returns(
                new GetParametersByPathResponse
                {
                    Parameters = [emptyListParameter],
                    NextToken = null
                });

        // Act
        _provider.Load();

        // Assert
        _provider.TryGet("empty-list:0", out _).Should().BeFalse();
    }

    #endregion

    #region TransformParameterNameToConfigKey Tests

    [Fact]
    public void TransformParameterNameToConfigKey_WithPathPrefix_RemovesPrefix()
    {
        // Arrange
        var method = typeof(AwsParameterStoreConfigurationProvider)
            .GetMethod(
                "TransformParameterNameToConfigKey",
                BindingFlags.NonPublic | BindingFlags.Instance);

        // Act
        var result = (string)method!.Invoke(_provider, ["/test/app/database/host"])!;

        // Assert
        result.Should().Be("app:database:host");
    }

    [Fact]
    public void TransformParameterNameToConfigKey_WithoutPathPrefix_TransformsSlashes()
    {
        // Arrange
        _source.Path = "/different/";
        var method = typeof(AwsParameterStoreConfigurationProvider)
            .GetMethod(
                "TransformParameterNameToConfigKey",
                BindingFlags.NonPublic | BindingFlags.Instance);

        // Act
        var result = (string)method!.Invoke(_provider, ["/test/app/database/host"])!;

        // Assert
        result.Should().Be("test:app:database:host");
    }

    [Fact]
    public void TransformParameterNameToConfigKey_WithParameterProcessor_AppliesCustomProcessing()
    {
        // Arrange
        var mockProcessor = Substitute.For<IParameterProcessor>();
        mockProcessor.ProcessParameterName("app:database:host", "/test/app/database/host")
            .Returns("custom:app:database:host");

        _source.ParameterProcessor = mockProcessor;

        var method = typeof(AwsParameterStoreConfigurationProvider)
            .GetMethod(
                "TransformParameterNameToConfigKey",
                BindingFlags.NonPublic | BindingFlags.Instance);

        // Act
        var result = (string)method!.Invoke(_provider, ["/test/app/database/host"])!;

        // Assert
        result.Should().Be("custom:app:database:host");
        mockProcessor.Received(1).ProcessParameterName("app:database:host", "/test/app/database/host");
    }

    #endregion

    #region ShouldProcessAsJson Tests

    [Fact]
    public void ShouldProcessAsJson_WithNoJsonProcessorPaths_ReturnsTrue()
    {
        // Arrange
        _source.JsonProcessor = true;
        _source.JsonProcessorPaths = null;

        var method = typeof(AwsParameterStoreConfigurationProvider)
            .GetMethod("ShouldProcessAsJson", BindingFlags.NonPublic | BindingFlags.Instance);

        // Act
        var result = (bool)method!.Invoke(_provider, ["app:config"])!;

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void ShouldProcessAsJson_WithEmptyJsonProcessorPaths_ReturnsTrue()
    {
        // Arrange
        _source.JsonProcessor = true;
        _source.JsonProcessorPaths = [];

        var method = typeof(AwsParameterStoreConfigurationProvider)
            .GetMethod("ShouldProcessAsJson", BindingFlags.NonPublic | BindingFlags.Instance);

        // Act
        var result = (bool)method!.Invoke(_provider, ["app:config"])!;

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void ShouldProcessAsJson_WithMatchingPath_ReturnsTrue()
    {
        // Arrange
        _source.JsonProcessor = true;
        // The JsonProcessorPaths should match the transformed config key format
        _source.JsonProcessorPaths = ["app:config"];

        var method = typeof(AwsParameterStoreConfigurationProvider)
            .GetMethod("ShouldProcessAsJson", BindingFlags.NonPublic | BindingFlags.Instance);

        // Act
        var result = (bool)method!.Invoke(_provider, ["app:config:database"])!;

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void ShouldProcessAsJson_WithNonMatchingPath_ReturnsFalse()
    {
        // Arrange
        _source.JsonProcessor = true;
        _source.JsonProcessorPaths = ["app:config"];

        var method = typeof(AwsParameterStoreConfigurationProvider)
            .GetMethod("ShouldProcessAsJson", BindingFlags.NonPublic | BindingFlags.Instance);

        // Act
        var result = (bool)method!.Invoke(_provider, ["app:other:setting"])!;

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task Constructor_WithReloadAfter_SetsUpTimerCallback()
    {
        // Arrange
        var sourceWithReload = new AwsParameterStoreConfigurationSource
        {
            Path = "/test/",
            Optional = true,
            ReloadAfter = TimeSpan.FromMilliseconds(100), // Short interval for testing
            AwsOptions = new Amazon.Extensions.NETCore.Setup.AWSOptions
            {
                Credentials = new AnonymousAWSCredentials(),
                Region = Amazon.RegionEndpoint.USEast1
            }
        };

        _mockSsmClient.GetParametersByPathAsync(Arg.Any<GetParametersByPathRequest>())
            .Returns(
                new GetParametersByPathResponse
                {
                    Parameters = [new() { Name = "/test/timer-test", Value = "timer-value", Type = "String" }],
                    NextToken = null
                });

        // Act
        using var provider = new AwsParameterStoreConfigurationProvider(sourceWithReload);

        // Replace the SSM client with our mock
        var ssmClientField = typeof(AwsParameterStoreConfigurationProvider)
            .GetField("_ssmClient", BindingFlags.NonPublic | BindingFlags.Instance);

        ssmClientField!.SetValue(provider, _mockSsmClient);

        // Wait for the timer to fire (this will trigger the callback: _ => LoadAsync().ConfigureAwait(false))
        await Task.Delay(200);

        // Assert - Verify that the timer callback was executed by checking if parameters were loaded
        provider.TryGet("timer-test", out var value).Should().BeTrue();
        value.Should().Be("timer-value");
    }

    #endregion

    #region ProcessParameterValue Tests

    [Fact]
    public void ProcessParameterValue_WithStringType_CallsProcessStringParameter()
    {
        // Arrange
        var parameter = new Parameter { Name = "/test/key", Value = "value", Type = "String" };
        var configurationData = new Dictionary<string, string?>();

        var method = typeof(AwsParameterStoreConfigurationProvider)
            .GetMethod("ProcessParameterValue", BindingFlags.NonPublic | BindingFlags.Instance);

        // Act
        method!.Invoke(_provider, [parameter, configurationData, "key"]);

        // Assert
        configurationData["key"].Should().Be("value");
    }

    [Fact]
    public void ProcessParameterValue_WithStringListType_CallsProcessStringListParameter()
    {
        // Arrange
        var parameter = new Parameter { Name = "/test/list", Value = "a,b,c", Type = "StringList" };
        var configurationData = new Dictionary<string, string?>();

        var method = typeof(AwsParameterStoreConfigurationProvider)
            .GetMethod("ProcessParameterValue", BindingFlags.NonPublic | BindingFlags.Instance);

        // Act
        method!.Invoke(_provider, [parameter, configurationData, "list"]);

        // Assert
        configurationData["list:0"].Should().Be("a");
        configurationData["list:1"].Should().Be("b");
        configurationData["list:2"].Should().Be("c");
    }

    [Fact]
    public void ProcessParameterValue_WithSecureStringType_CallsProcessSecureStringParameter()
    {
        // Arrange
        var parameter = new Parameter { Name = "/test/secret", Value = "secret-value", Type = "SecureString" };
        var configurationData = new Dictionary<string, string?>();

        var method = typeof(AwsParameterStoreConfigurationProvider)
            .GetMethod("ProcessParameterValue", BindingFlags.NonPublic | BindingFlags.Instance);

        // Act
        method!.Invoke(_provider, [parameter, configurationData, "secret"]);

        // Assert
        configurationData["secret"].Should().Be("secret-value");
    }

    [Fact]
    public void ProcessParameterValue_WithUnknownType_TreatsAsString()
    {
        // Arrange
        var parameter = new Parameter { Name = "/test/unknown", Value = "unknown-value", Type = "UnknownType" };
        var configurationData = new Dictionary<string, string?>();

        var method = typeof(AwsParameterStoreConfigurationProvider)
            .GetMethod("ProcessParameterValue", BindingFlags.NonPublic | BindingFlags.Instance);

        // Act
        method!.Invoke(_provider, [parameter, configurationData, "unknown"]);

        // Assert
        configurationData["unknown"].Should().Be("unknown-value");
    }

    #endregion

    #region ProcessStringListParameter Tests

    [Fact]
    public void ProcessStringListParameter_WithValidList_CreatesIndexedEntries()
    {
        // Arrange
        var parameter = new Parameter { Value = "first, second , third" };
        var configurationData = new Dictionary<string, string?>();

        var method = typeof(AwsParameterStoreConfigurationProvider)
            .GetMethod(
                "ProcessStringListParameter",
                BindingFlags.NonPublic | BindingFlags.Static);

        // Act
        method!.Invoke(null, [parameter, configurationData, "list"]);

        // Assert
        configurationData.Should().HaveCount(3);
        configurationData["list:0"].Should().Be("first");
        configurationData["list:1"].Should().Be("second");
        configurationData["list:2"].Should().Be("third");
    }

    [Fact]
    public void ProcessStringListParameter_WithEmptyValue_DoesNothing()
    {
        // Arrange
        var parameter = new Parameter { Value = "" };
        var configurationData = new Dictionary<string, string?>();

        var method = typeof(AwsParameterStoreConfigurationProvider)
            .GetMethod(
                "ProcessStringListParameter",
                BindingFlags.NonPublic | BindingFlags.Static);

        // Act
        method!.Invoke(null, [parameter, configurationData, "list"]);

        // Assert
        configurationData.Should().BeEmpty();
    }

    [Fact]
    public void ProcessStringListParameter_WithNullValue_DoesNothing()
    {
        // Arrange
        var parameter = new Parameter { Value = null };
        var configurationData = new Dictionary<string, string?>();

        var method = typeof(AwsParameterStoreConfigurationProvider)
            .GetMethod(
                "ProcessStringListParameter",
                BindingFlags.NonPublic | BindingFlags.Static);

        // Act
        method!.Invoke(null, [parameter, configurationData, "list"]);

        // Assert
        configurationData.Should().BeEmpty();
    }

    #endregion

    #region Dispose Tests

    [Fact]
    public void Dispose_MultipleCalls_DoesNotThrow()
    {
        // Arrange
        using var provider = new AwsParameterStoreConfigurationProvider(_source);

        // Act & Assert
        var action = () =>
        {
            provider.Dispose();
            provider.Dispose();
        };

        action.Should().NotThrow();
    }

    #endregion

    #region ConfigurationProviderException Tests

    [Fact]
    public void ConfigurationProviderException_WithSourceAndInnerException_SetsPropertiesCorrectly()
    {
        // Arrange
        var innerException = new Exception("Inner error");

        // Act
        var exception = new ConfigurationProviderException(_source, innerException);

        // Assert
        exception.Source.Should().Be("/test/");
        exception.InnerException.Should().BeSameAs(innerException);
        exception.Message.Should().Contain("Failed to load configuration from AWS Parameter Store source: /test/");
    }

    #endregion

    #region Integration Tests with Timer

    [Fact]
    public void Constructor_WithReloadAfter_CreatesTimer()
    {
        // Arrange
        var sourceWithReload = new AwsParameterStoreConfigurationSource
        {
            Path = "/test/",
            Optional = true,
            ReloadAfter = TimeSpan.FromMinutes(5),
            AwsOptions = new Amazon.Extensions.NETCore.Setup.AWSOptions
            {
                Credentials = new AnonymousAWSCredentials(),
                Region = Amazon.RegionEndpoint.USEast1
            }
        };

        // Act
        using var provider = new AwsParameterStoreConfigurationProvider(sourceWithReload);

        // Assert
        // Timer creation is verified by successful construction without exception
        provider.Should().NotBeNull();
    }

    #endregion

    #region API Request Verification Tests

    [Fact]
    public void LoadParametersAsync_SetsCorrectRequestParameters()
    {
        // Arrange
        _mockSsmClient.GetParametersByPathAsync(Arg.Any<GetParametersByPathRequest>())
            .Returns(
                new GetParametersByPathResponse
                {
                    Parameters = [],
                    NextToken = null
                });

        // Act
        _provider.Load();

        // Assert
        _mockSsmClient.Received(1).GetParametersByPathAsync(
            Arg.Is<GetParametersByPathRequest>(req =>
                req.Path == "/test/" &&
                req.Recursive == true &&
                req.WithDecryption == true &&
                req.MaxResults == 10));
    }

    #endregion
}