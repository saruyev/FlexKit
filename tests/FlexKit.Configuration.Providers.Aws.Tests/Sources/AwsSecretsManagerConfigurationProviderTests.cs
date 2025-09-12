using System.Reflection;
using Amazon.SecretsManager;
using Amazon.SecretsManager.Model;
using FlexKit.Configuration.Providers.Aws.Sources;
using FluentAssertions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;
// ReSharper disable TooManyDeclarations
// ReSharper disable ClassTooBig
// ReSharper disable ComplexConditionExpression
// ReSharper disable NullableWarningSuppressionIsUsed
#pragma warning disable CS0618 // Type or member is obsolete

namespace FlexKit.Configuration.Providers.Aws.Tests.Sources;

public class AwsSecretsManagerConfigurationProviderTests : IDisposable
{
    private readonly IAmazonSecretsManager _mockSecretsClient;
    private readonly AwsSecretsManagerConfigurationSource _source;
    private readonly AwsSecretsManagerConfigurationProvider _provider;

    public AwsSecretsManagerConfigurationProviderTests()
    {
        _mockSecretsClient = Substitute.For<IAmazonSecretsManager>();
        _source = new AwsSecretsManagerConfigurationSource
        {
            SecretNames = ["test-secret"],
            Optional = true,
            AwsOptions = new Amazon.Extensions.NETCore.Setup.AWSOptions
            {
                Credentials = new Amazon.Runtime.AnonymousAWSCredentials(),
                Region = Amazon.RegionEndpoint.USEast1
            }
        };

        _provider = new AwsSecretsManagerConfigurationProvider(_source);

        // Use reflection to replace the private _secretsClient field
        var secretsClientField = typeof(AwsSecretsManagerConfigurationProvider)
            .GetField("_secretsClient", BindingFlags.NonPublic | BindingFlags.Instance);
        secretsClientField!.SetValue(_provider, _mockSecretsClient);
    }

    public void Dispose()
    {
        _provider.Dispose();
        _mockSecretsClient.Dispose();
    }

    [Fact]
    public void Constructor_WithNullSource_ThrowsArgumentNullException()
    {
        // Act & Assert
        var action = () => new AwsSecretsManagerConfigurationProvider(null!);
        action.Should().Throw<ArgumentNullException>().WithParameterName("source");
    }

    [Fact]
    public void Load_WithSingleSecret_LoadsCorrectly()
    {
        // Arrange
        var response = new GetSecretValueResponse
        {
            Name = "test-secret",
            SecretString = "secret-value"
        };

        _mockSecretsClient.GetSecretValueAsync(Arg.Any<GetSecretValueRequest>())
            .Returns(response);

        // Act
        _provider.Load();

        // Assert
        _provider.TryGet("test:secret", out var value).Should().BeTrue();
        value.Should().Be("secret-value");
    }

    [Fact]
    public void Load_WithOptionalSecretError_DoesNotThrow()
    {
        // Arrange
        _source.Optional = true;
        _mockSecretsClient.GetSecretValueAsync(Arg.Any<GetSecretValueRequest>())
            .ThrowsAsync(new Exception("AWS error"));

        // Act & Assert
        var action = () => _provider.Load();
        action.Should().NotThrow();
    }

    [Fact]
    public void Load_WithRequiredSecretError_ThrowsInvalidOperationException()
    {
        // Arrange
        _source.Optional = false;
        _mockSecretsClient.GetSecretValueAsync(Arg.Any<GetSecretValueRequest>())
            .ThrowsAsync(new Exception("AWS error"));

        // Act & Assert
        var action = () => _provider.Load();
        action.Should().Throw<InvalidOperationException>()
            .WithMessage("*Failed to load configuration from AWS Secrets Manager*");
    }

    [Fact]
    public void Load_WithErrorAndOnLoadExceptionCallback_InvokesCallback()
    {
        // Arrange
        _source.Optional = true;
        var callbackInvoked = false;
        SecretsManagerConfigurationProviderException? capturedException = null;

        _source.OnLoadException = ex =>
        {
            callbackInvoked = true;
            capturedException = ex;
        };

        var innerException = new Exception("AWS error");
        _mockSecretsClient.GetSecretValueAsync(Arg.Any<GetSecretValueRequest>())
            .ThrowsAsync(innerException);

        // Act
        _provider.Load();

        // Assert
        callbackInvoked.Should().BeTrue();
        capturedException.Should().NotBeNull();
        capturedException!.InnerException.Should().BeSameAs(innerException);
        capturedException.Source.Should().Be("SecretsManager[test-secret]");
    }

    [Fact]
    public void Load_WithMultipleSecrets_LoadsAll()
    {
        // Arrange
        _source.SecretNames = ["secret1", "secret2"];

        _mockSecretsClient.GetSecretValueAsync(Arg.Is<GetSecretValueRequest>(r => r.SecretId == "secret1"))
            .Returns(new GetSecretValueResponse { Name = "secret1", SecretString = "value1" });

        _mockSecretsClient.GetSecretValueAsync(Arg.Is<GetSecretValueRequest>(r => r.SecretId == "secret2"))
            .Returns(new GetSecretValueResponse { Name = "secret2", SecretString = "value2" });

        // Act
        _provider.Load();

        // Assert
        _provider.TryGet("secret1", out var value1).Should().BeTrue();
        value1.Should().Be("value1");

        _provider.TryGet("secret2", out var value2).Should().BeTrue();
        value2.Should().Be("value2");
    }

    [Fact]
    public void Load_WithEmptySecretNames_DoesNotCallAws()
    {
        // Arrange
        _source.SecretNames = [];

        // Act
        _provider.Load();

        // Assert
        _mockSecretsClient.DidNotReceive().GetSecretValueAsync(Arg.Any<GetSecretValueRequest>());
    }

    [Fact]
    public void Load_WithNullSecretNames_DoesNotCallAws()
    {
        // Arrange
        _source.SecretNames = null;

        // Act
        _provider.Load();

        // Assert
        _mockSecretsClient.DidNotReceive().GetSecretValueAsync(Arg.Any<GetSecretValueRequest>());
    }

    [Fact]
    public void Dispose_DisposesSecretsClient()
    {
        // Act
        _provider.Dispose();

        // Assert
        _mockSecretsClient.Received(1).Dispose();
    }

    [Fact]
    public void Load_WithBinarySecret_ConvertsToBase64()
    {
        // Arrange
        var binaryData = new MemoryStream([1, 2, 3, 4, 5]);
        var response = new GetSecretValueResponse
        {
            Name = "test-secret",
            SecretBinary = binaryData
        };

        _mockSecretsClient.GetSecretValueAsync(Arg.Any<GetSecretValueRequest>())
            .Returns(response);

        // Act
        _provider.Load();

        // Assert
        _provider.TryGet("test:secret", out var value).Should().BeTrue();
        value.Should().Be(Convert.ToBase64String([1, 2, 3, 4, 5]));
    }

    [Fact]
    public void Load_WithJsonSecretAndJsonProcessorEnabled_FlattensJson()
    {
        // Arrange
        _source.JsonProcessor = true;
        var jsonSecret = """{"database": {"host": "localhost", "port": 5432}, "apiKey": "secret-key"}""";
        var response = new GetSecretValueResponse
        {
            Name = "test-secret",
            SecretString = jsonSecret
        };

        _mockSecretsClient.GetSecretValueAsync(Arg.Any<GetSecretValueRequest>())
            .Returns(response);

        // Act
        _provider.Load();

        // Assert
        _provider.TryGet("test:secret:database:host", out var host).Should().BeTrue();
        host.Should().Be("localhost");

        _provider.TryGet("test:secret:database:port", out var port).Should().BeTrue();
        port.Should().Be("5432");

        _provider.TryGet("test:secret:apiKey", out var apiKey).Should().BeTrue();
        apiKey.Should().Be("secret-key");
    }

    [Fact]
    public void Load_WithJsonProcessorDisabled_StoresAsSimpleString()
    {
        // Arrange
        _source.JsonProcessor = false;
        var jsonSecret = """{"key": "value"}""";
        var response = new GetSecretValueResponse
        {
            Name = "test-secret",
            SecretString = jsonSecret
        };

        _mockSecretsClient.GetSecretValueAsync(Arg.Any<GetSecretValueRequest>())
            .Returns(response);

        // Act
        _provider.Load();

        // Assert
        _provider.TryGet("test:secret", out var value).Should().BeTrue();
        value.Should().Be("""{"key": "value"}""");
    }

    [Fact]
    public void Load_WithSelectiveJsonProcessorSecrets_ProcessesOnlySpecified()
    {
        // Arrange
        _source.JsonProcessor = true;
        _source.JsonProcessorSecrets = ["test-secret"];
        _source.SecretNames = ["test-secret", "plain-secret"];

        var jsonResponse = new GetSecretValueResponse
        {
            Name = "test-secret",
            SecretString = """{"key": "value"}"""
        };

        var plainResponse = new GetSecretValueResponse
        {
            Name = "plain-secret",
            SecretString = """{"this": "stays-as-string"}"""
        };

        _mockSecretsClient.GetSecretValueAsync(Arg.Is<GetSecretValueRequest>(r => r.SecretId == "test-secret"))
            .Returns(jsonResponse);

        _mockSecretsClient.GetSecretValueAsync(Arg.Is<GetSecretValueRequest>(r => r.SecretId == "plain-secret"))
            .Returns(plainResponse);

        // Act
        _provider.Load();

        // Assert
        _provider.TryGet("test:secret:key", out var jsonValue).Should().BeTrue();
        jsonValue.Should().Be("value");

        _provider.TryGet("plain:secret", out var plainValue).Should().BeTrue();
        plainValue.Should().Be("""{"this": "stays-as-string"}""");
    }

    [Fact]
    public void Load_WithPatternSecrets_LoadsMatchingSecrets()
    {
        // Arrange
        _source.SecretNames = ["myapp-*"];

        var listResponse = new ListSecretsResponse
        {
            SecretList = [
                new SecretListEntry { Name = "myapp-database" },
                new SecretListEntry { Name = "myapp-cache" },
                new SecretListEntry { Name = "other-secret" }
            ],
            NextToken = null
        };

        _mockSecretsClient.ListSecretsAsync(Arg.Any<ListSecretsRequest>())
            .Returns(listResponse);

        _mockSecretsClient.GetSecretValueAsync(Arg.Is<GetSecretValueRequest>(r => r.SecretId == "myapp-database"))
            .Returns(new GetSecretValueResponse { Name = "myapp-database", SecretString = "db-value" });

        _mockSecretsClient.GetSecretValueAsync(Arg.Is<GetSecretValueRequest>(r => r.SecretId == "myapp-cache"))
            .Returns(new GetSecretValueResponse { Name = "myapp-cache", SecretString = "cache-value" });

        // Act
        _provider.Load();

        // Assert
        _provider.TryGet("myapp:database", out var dbValue).Should().BeTrue();
        dbValue.Should().Be("db-value");

        _provider.TryGet("myapp:cache", out var cacheValue).Should().BeTrue();
        cacheValue.Should().Be("cache-value");
    }

    [Fact]
    public void Load_WithPaginatedListSecrets_LoadsAllPages()
    {
        // Arrange
        _source.SecretNames = ["prefix-*"];

        var firstPage = new ListSecretsResponse
        {
            SecretList = [new SecretListEntry { Name = "prefix-secret1" }],
            NextToken = "next-token"
        };

        var secondPage = new ListSecretsResponse
        {
            SecretList = [new SecretListEntry { Name = "prefix-secret2" }],
            NextToken = null
        };

        _mockSecretsClient.ListSecretsAsync(Arg.Is<ListSecretsRequest>(r => r.NextToken == null))
            .Returns(firstPage);

        _mockSecretsClient.ListSecretsAsync(Arg.Is<ListSecretsRequest>(r => r.NextToken == "next-token"))
            .Returns(secondPage);

        _mockSecretsClient.GetSecretValueAsync(Arg.Is<GetSecretValueRequest>(r => r.SecretId == "prefix-secret1"))
            .Returns(new GetSecretValueResponse { Name = "prefix-secret1", SecretString = "value1" });

        _mockSecretsClient.GetSecretValueAsync(Arg.Is<GetSecretValueRequest>(r => r.SecretId == "prefix-secret2"))
            .Returns(new GetSecretValueResponse { Name = "prefix-secret2", SecretString = "value2" });

        // Act
        _provider.Load();

        // Assert
        _provider.TryGet("prefix:secret1", out var value1).Should().BeTrue();
        value1.Should().Be("value1");

        _provider.TryGet("prefix:secret2", out var value2).Should().BeTrue();
        value2.Should().Be("value2");
    }

    [Fact]
    public void Load_WithVersionStage_RequestsCorrectVersion()
    {
        // Arrange
        _source.VersionStage = "AWSPENDING";
        var response = new GetSecretValueResponse
        {
            Name = "test-secret",
            SecretString = "pending-value"
        };

        _mockSecretsClient.GetSecretValueAsync(Arg.Any<GetSecretValueRequest>())
            .Returns(response);

        // Act
        _provider.Load();

        // Assert
        _mockSecretsClient.Received(1).GetSecretValueAsync(
            Arg.Is<GetSecretValueRequest>(r => r.VersionStage == "AWSPENDING"));
    }

    [Fact]
    public void Load_WithNullVersionStage_UsesAwsCurrent()
    {
        // Arrange
        _source.VersionStage = null;
        var response = new GetSecretValueResponse
        {
            Name = "test-secret",
            SecretString = "current-value"
        };

        _mockSecretsClient.GetSecretValueAsync(Arg.Any<GetSecretValueRequest>())
            .Returns(response);

        // Act
        _provider.Load();

        // Assert
        _mockSecretsClient.Received(1).GetSecretValueAsync(
            Arg.Is<GetSecretValueRequest>(r => r.VersionStage == "AWSCURRENT"));
    }

    [Fact]
    public void Load_WithCustomSecretProcessor_AppliesTransformation()
    {
        // Arrange
        var mockProcessor = Substitute.For<ISecretProcessor>();
        mockProcessor.ProcessSecretName("test:secret", "test-secret")
            .Returns("custom:transformed:key");

        _source.SecretProcessor = mockProcessor;

        var response = new GetSecretValueResponse
        {
            Name = "test-secret",
            SecretString = "value"
        };

        _mockSecretsClient.GetSecretValueAsync(Arg.Any<GetSecretValueRequest>())
            .Returns(response);

        // Act
        _provider.Load();

        // Assert
        _provider.TryGet("custom:transformed:key", out var value).Should().BeTrue();
        value.Should().Be("value");
        mockProcessor.Received(1).ProcessSecretName("test:secret", "test-secret");
    }

    [Theory]
    [InlineData("app-database", "app:database")]
    [InlineData("my-app-cache", "my:app:cache")]
    [InlineData("simple", "simple")]
    [InlineData("complex-multi-part-name", "complex:multi:part:name")]
    public void TransformSecretName_ConvertsHyphensToColons(string secretName, string expectedKey)
    {
        // Arrange
        var response = new GetSecretValueResponse
        {
            Name = secretName,
            SecretString = "value"
        };

        _mockSecretsClient.GetSecretValueAsync(Arg.Any<GetSecretValueRequest>())
            .Returns(response);

        // Act
        _provider.Load();

        // Assert
        _provider.TryGet(expectedKey, out var value).Should().BeTrue();
        value.Should().Be("value");
    }

    [Fact]
    public void Constructor_WithReloadAfterSet_CreatesTimer()
    {
        // Arrange
        var sourceWithReload = new AwsSecretsManagerConfigurationSource
        {
            SecretNames = ["test-secret"],
            ReloadAfter = TimeSpan.FromMinutes(5),
            AwsOptions = new Amazon.Extensions.NETCore.Setup.AWSOptions
            {
                Credentials = new Amazon.Runtime.AnonymousAWSCredentials(),
                Region = Amazon.RegionEndpoint.USEast1
            }
        };

        // Act
        var providerWithReload = new AwsSecretsManagerConfigurationProvider(sourceWithReload);

        // Assert
        var timerField = typeof(AwsSecretsManagerConfigurationProvider)
            .GetField("_reloadTimer", BindingFlags.NonPublic | BindingFlags.Instance);
        var timer = timerField?.GetValue(providerWithReload);
        timer.Should().NotBeNull();

        providerWithReload.Dispose();
    }

    [Fact]
    public void Constructor_WithNullReloadAfter_DoesNotCreateTimer()
    {
        // Arrange
        var sourceWithoutReload = new AwsSecretsManagerConfigurationSource
        {
            SecretNames = ["test-secret"],
            ReloadAfter = null,
            AwsOptions = new Amazon.Extensions.NETCore.Setup.AWSOptions
            {
                Credentials = new Amazon.Runtime.AnonymousAWSCredentials(),
                Region = Amazon.RegionEndpoint.USEast1
            }
        };

        // Act
        var providerWithoutReload = new AwsSecretsManagerConfigurationProvider(sourceWithoutReload);

        // Assert
        var timerField = typeof(AwsSecretsManagerConfigurationProvider)
            .GetField("_reloadTimer", BindingFlags.NonPublic | BindingFlags.Instance);
        var timer = timerField?.GetValue(providerWithoutReload);
        timer.Should().BeNull();

        providerWithoutReload.Dispose();
    }
    
    private static readonly Lock AwsTestLock = new();

    [Fact]
    public void Constructor_WithAwsClientCreationFailure_ThrowsInvalidOperationException()
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

                // 3. Force the SDK to re-evaluate credentials
                Amazon.Runtime.FallbackCredentialsFactory.Reset();

                // Clear the cached credentials using reflection
                var fallbackType = typeof(Amazon.Runtime.FallbackCredentialsFactory);
                var cachedCredsField =
                    fallbackType.GetField("cachedCredentials", BindingFlags.Static | BindingFlags.NonPublic);
                cachedCredsField?.SetValue(null, null);

                // Clear any ClientFactory cache
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

                // 4. Arrange
                var source = new AwsSecretsManagerConfigurationSource
                {
                    SecretNames = ["test-secret"],
                    AwsOptions =
                        null // This will trigger the fallback to new AWSOptions() which will fail credential resolution
                };

                // Act & Assert
                var action = () => new AwsSecretsManagerConfigurationProvider(source);
                action.Should().Throw<InvalidOperationException>()
                    .WithMessage(
                        "Failed to create AWS Secrets Manager client. Ensure AWS credentials are properly configured.")
                    .WithInnerException<Amazon.Runtime.AmazonClientException>();
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

                // Reset the factory to restore normal operation
                Amazon.Runtime.FallbackCredentialsFactory.Reset();
            }
        }
    }

    [Fact]
    public async Task Constructor_WithReloadAfter_TimerCallbackExecutesLoadAsync()
    {
        // Arrange
        var sourceWithReload = new AwsSecretsManagerConfigurationSource
        {
            SecretNames = ["test-secret"],
            ReloadAfter = TimeSpan.FromMilliseconds(100), // Very short interval for testing
            AwsOptions = new Amazon.Extensions.NETCore.Setup.AWSOptions
            {
                Credentials = new Amazon.Runtime.AnonymousAWSCredentials(),
                Region = Amazon.RegionEndpoint.USEast1
            }
        };

        var mockSecretsClient = Substitute.For<IAmazonSecretsManager>();
        var response = new GetSecretValueResponse
        {
            Name = "test-secret",
            SecretString = "reloaded-value"
        };
        mockSecretsClient.GetSecretValueAsync(Arg.Any<GetSecretValueRequest>())
            .Returns(response);

        // Act
        var provider = new AwsSecretsManagerConfigurationProvider(sourceWithReload);

        // Replace the client with mock using reflection
        var secretsClientField = typeof(AwsSecretsManagerConfigurationProvider)
            .GetField("_secretsClient", BindingFlags.NonPublic | BindingFlags.Instance);
        secretsClientField!.SetValue(provider, mockSecretsClient);

        // Wait for a timer callback to execute
        await Task.Delay(200);

        // Assert
        await mockSecretsClient.Received().GetSecretValueAsync(Arg.Any<GetSecretValueRequest>());

        provider.Dispose();
    }

    [Fact]
    public void Load_WithResourceNotFoundAndRequiredSecret_ThrowsSpecificInvalidOperationException()
    {
        // Arrange
        _source.Optional = false;
        _source.SecretNames = ["required-secret"];

        var resourceNotFoundException = new ResourceNotFoundException("Secret not found");

        _mockSecretsClient.GetSecretValueAsync(Arg.Any<GetSecretValueRequest>())
            .ThrowsAsync(resourceNotFoundException);

        // Act & Assert
        var action = () => _provider.Load();
        action.Should().Throw<InvalidOperationException>()
            .WithMessage("*Failed to load configuration from AWS Secrets Manager*")
            .WithInnerException<InvalidOperationException>()
            .Which.Message.Should().Be("Required secret 'required-secret' not found in AWS Secrets Manager.");
    }

    [Fact]
    public void Load_WithResourceNotFoundAndNotRequiredSecret_NotThrowsInvalidOperationException()
    {
        // Arrange
        _source.Optional = true;
        _source.SecretNames = ["not-required-secret"];

        var resourceNotFoundException = new ResourceNotFoundException("Secret not found");

        _mockSecretsClient.GetSecretValueAsync(Arg.Any<GetSecretValueRequest>())
            .ThrowsAsync(resourceNotFoundException);

        // Act & Assert
        var action = () => _provider.Load();
        action.Should().NotThrow<InvalidOperationException>();
    }
}