using System.Reflection;
using FlexKit.Logging.Configuration;
using FlexKit.Logging.Detection;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable ReplaceAutoPropertyWithComputedProperty
// ReSharper disable FlagArgument
// ReSharper disable TooManyArguments
// ReSharper disable PropertyCanBeMadeInitOnly.Global
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable MethodTooLong
// ReSharper disable ClassTooBig

namespace FlexKit.Logging.Tests.Detection;

/// <summary>
/// Tests for MelExtensions.GetFormatterOptionsType method covering all switch expression cases.
/// </summary>
public class MelExtensionsTests
{
    [Theory]
    [InlineData("Simple", "AddSimpleConsole", MelNames.SimpleConsoleOptionsType)]
    [InlineData("Systemd", "AddSystemdConsole", MelNames.ConsoleOptionsType)]
    [InlineData("Json", "AddJsonConsole", MelNames.JsonConsoleOptionsType)]
    public void GetFormatterOptionsType_WithValidFormatterTypes_ReturnsCorrectTypeAndMethod(
        string formatterValue,
        string expectedMethodName,
        string expectedTypeName)
    {
        // Arrange
        var target = CreateLoggingTarget(formatterValue);

        // Act
        var (optionsType, methodName) = target.GetFormatterOptionsType();

        // Assert
        methodName.Should().Be(expectedMethodName);
        optionsType.Should().NotBeNull();
        optionsType.FullName.Should().Contain(GetTypeNameWithoutAssembly(expectedTypeName));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("Unknown")]
    [InlineData("invalid")]
    [InlineData("SIMPLE")] // Case-sensitive
    [InlineData("simple")] // Case-sensitive
    public void GetFormatterOptionsType_WithInvalidOrMissingFormatter_ReturnsDefaultSimpleConsole(string? formatterValue)
    {
        // Arrange
        var target = CreateLoggingTarget(formatterValue);

        // Act
        var (optionsType, methodName) = target.GetFormatterOptionsType();

        // Assert
        methodName.Should().Be("AddSimpleConsole");
        optionsType.Should().NotBeNull();
        optionsType.FullName.Should().Contain("SimpleConsoleFormatterOptions");
    }

    [Theory]
    [InlineData(new string[0], false)] // No properties
    [InlineData(new[] { "LogName" }, true)] // Only LogName
    [InlineData(new[] { "SourceName" }, true)] // Only SourceName  
    [InlineData(new[] { "MachineName" }, true)] // Only MachineName
    [InlineData(new[] { "LogName", "SourceName" }, true)] // LogName + SourceName
    [InlineData(new[] { "LogName", "MachineName" }, true)] // LogName + MachineName
    [InlineData(new[] { "SourceName", "MachineName" }, true)] // SourceName + MachineName
    [InlineData(new[] { "LogName", "SourceName", "MachineName" }, true)] // All three
    [InlineData(new[] { "OtherProperty" }, false)] // Unrelated property
    [InlineData(new[] { "LogName", "OtherProperty" }, true)] // LogName + unrelated
    [InlineData(new[] { "logname" }, false)] // Case-sensitive - wrong case
    public void HasEventLogConfiguration_WithVariousProperties_ReturnsExpectedResult(string[] propertyNames, bool expected)
    {
        // Arrange
        var properties = new Dictionary<string, IConfigurationSection>();
        foreach (var propertyName in propertyNames)
        {
            var configSection = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?> { [""] = "test-value" })
                .Build()
                .GetSection("");

            properties[propertyName] = configSection;
        }

        var target = new LoggingTarget
        {
            Type = "EventLog",
            Enabled = true,
            Properties = properties!
        };

        // Act
        var result = target.HasEventLogConfiguration();

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("Configure", true, 2, true, typeof(Action<>), true, true)] // Valid Configure method
    [InlineData("Configure", true, 2, true, typeof(Func<>), false, true)] // Wrong generic type (Func instead of Action)
    [InlineData("Configure", true, 2, false, null, false, true)] // The second parameter is not generic
    [InlineData("Configure", true, 1, false, null, false, false)] // Wrong parameter count (1)
    [InlineData("Configure", true, 3, true, typeof(Action<>), false, true)] // Wrong parameter count (3)
    [InlineData("Configure", true, 0, false, null, false, false)] // No parameters
    [InlineData("Configure", false, 2, true, typeof(Action<>), false, true)] // Not generic method definition
    [InlineData("ConfigureOther", true, 2, true, typeof(Action<>), false, true)] // Wrong method name
    [InlineData("configure", true, 2, true, typeof(Action<>), false, true)] // Case-sensitive method name
    [InlineData("CONFIGURE", true, 2, true, typeof(Action<>), false, true)] // Case-sensitive method name
    [InlineData("Configure", true, 2, true, typeof(Action<>), false, false)] // The first parameter is not IServiceCollection
    public void IsConfigure_WithVariousMethodSignatures_ReturnsExpectedResult(
        string methodName,
        bool isGenericMethodDefinition,
        int parameterCount,
        bool secondParamIsGeneric,
        Type? secondParamGenericDefinition,
        bool expected,
        bool firstParamIsServiceCollection)
    {
        // Arrange
        var method = CreateMockMethodInfo(
            methodName,
            isGenericMethodDefinition,
            parameterCount,
            secondParamIsGeneric,
            secondParamGenericDefinition,
            firstParamIsServiceCollection);

        // Act
        var result = MelExtensions.IsConfigure(method);

        // Assert
        result.Should().Be(expected);
    }

    private static MethodInfo CreateMockMethodInfo(
        string methodName,
        bool isGenericMethodDefinition,
        int parameterCount,
        bool secondParamIsGeneric,
        Type? secondParamGenericDefinition,
        bool firstParamIsServiceCollection)
    {
        var method = Substitute.For<MethodInfo>();
        method.Name.Returns(methodName);
        method.IsGenericMethodDefinition.Returns(isGenericMethodDefinition);

        var parameters = new ParameterInfo[parameterCount];
        for (int i = 0; i < parameterCount; i++)
        {
            var param = Substitute.For<ParameterInfo>();

            if (i == 0 && firstParamIsServiceCollection) // The first parameter should be IServiceCollection
            {
                param.ParameterType.Returns(typeof(Microsoft.Extensions.DependencyInjection.IServiceCollection));
            }
            else if (i == 1 && secondParamIsGeneric) // Second parameter (index 1)
            {
                var paramType = Substitute.For<Type>();
                paramType.IsGenericType.Returns(true);
                paramType.GetGenericTypeDefinition().Returns(secondParamGenericDefinition);
                param.ParameterType.Returns(paramType);
            }
            else
            {
                var paramType = Substitute.For<Type>();
                paramType.IsGenericType.Returns(false);
                param.ParameterType.Returns(paramType);
            }

            parameters[i] = param;
        }

        method.GetParameters().Returns(parameters);
        return method;
    }

    [Fact]
    public void CreateTelemetryConfigurationAction_NoConnectionStringProperty_DoesNothing()
    {
        // Arrange
        var target = new LoggingTarget { Properties = new Dictionary<string, IConfigurationSection?>() };
        var telemetryConfigType = typeof(TestTelemetryConfig);

        // Act
        var action = target.CreateTelemetryConfigurationAction(telemetryConfigType);
        var config = new TestTelemetryConfig { ConnectionString = "original" };
        ((Action<TestTelemetryConfig>)action)(config);

        // Assert
        config.ConnectionString.Should().Be("original");
    }

    [Fact]
    public void CreateTelemetryConfigurationAction_HasConnectionStringButTypeDoesNot_DoesNothing()
    {
        // Arrange
        var target = CreateTargetWithConnectionString("test-value");
        var telemetryConfigType = typeof(TestTelemetryConfigWithoutConnectionString);

        // Act
        var action = target.CreateTelemetryConfigurationAction(telemetryConfigType);
        var config = new TestTelemetryConfigWithoutConnectionString();
        ((Action<TestTelemetryConfigWithoutConnectionString>)action)(config);

        // Assert - Just verify no exception thrown
        Assert.True(true);
    }

    [Fact]
    public void CreateTelemetryConfigurationAction_BothHaveConnectionString_SetsProperty()
    {
        // Arrange
        var target = CreateTargetWithConnectionString("test-connection");
        var telemetryConfigType = typeof(TestTelemetryConfig);

        // Act
        var action = target.CreateTelemetryConfigurationAction(telemetryConfigType);
        var config = new TestTelemetryConfig();
        ((Action<TestTelemetryConfig>)action)(config);

        // Assert
        config.ConnectionString.Should().Be("test-connection");
    }

    [Fact]
    public void CreateConfigurationAction_MapsTargetPropertiesToOptions()
    {
        // Arrange
        var target = new LoggingTarget
        {
            Properties = new Dictionary<string, IConfigurationSection?>
            {
                ["StringProperty"] = CreateMockConfigurationSection("test-value"),
                ["IntProperty"] = CreateMockConfigurationSection("42"),
                ["BoolProperty"] = CreateMockConfigurationSection("true"),
                ["NonMatchingProperty"] = CreateMockConfigurationSection("ignored")
            }
        };

        // Act
        var action = target.CreateConfigurationAction(typeof(TestOptions));
        var options = new TestOptions();
        ((Action<TestOptions>)action)(options);

        // Assert
        options.StringProperty.Should().Be("test-value");
        options.IntProperty.Should().Be(42);
        options.BoolProperty.Should().Be(true);
        options.ReadOnlyProperty.Should().Be("default"); // Should not be set
    }

    [Fact]
public void ConfigureOptions_WithEmptyProperties_DoesNothing()
{
    // Arrange
    var target = new LoggingTarget { Properties = new Dictionary<string, IConfigurationSection?>() };
    var options = new TestOptions { StringProperty = "original" };

    // Act
    var action = target.CreateConfigurationAction(typeof(TestOptions));
    ((Action<TestOptions>)action)(options);

    // Assert
    options.StringProperty.Should().Be("original");
}

[Fact]
public void ConfigureOptions_WhenTryGetValueReturnsFalse_SkipsProperty()
{
    // Arrange
    var target = new LoggingTarget
    {
        Properties = new Dictionary<string, IConfigurationSection?>
        {
            ["OtherProperty"] = CreateMockConfigurationSection("value")
            // Missing StringProperty
        }
    };
    var options = new TestOptions { StringProperty = "original" };

    // Act
    var action = target.CreateConfigurationAction(typeof(TestOptions));
    ((Action<TestOptions>)action)(options);

    // Assert
    options.StringProperty.Should().Be("original");
}

[Fact]
public void ConfigureOptions_WithNullValue_ReturnsEarly()
{
    // Arrange
    var target = new LoggingTarget
    {
        Properties = new Dictionary<string, IConfigurationSection?>
        {
            ["StringProperty"] = null!
        }
    };
    var options = new TestOptions { StringProperty = "original" };

    // Act
    var action = target.CreateConfigurationAction(typeof(TestOptions));
    ((Action<TestOptions>)action)(options);

    // Assert
    options.StringProperty.Should().Be("original");
}

[Fact]
public void ConfigureOptions_WhenTryToSetValueThrows_SkipsProperty()
{
    // Arrange
    var target = new LoggingTarget
    {
        Properties = new Dictionary<string, IConfigurationSection?>
        {
            ["IntProperty"] = CreateMockConfigurationSection("not-a-number")
        }
    };
    var options = new TestOptions { IntProperty = 99 };

    // Act
    var action = target.CreateConfigurationAction(typeof(TestOptions));
    ((Action<TestOptions>)action)(options);

    // Assert
    options.IntProperty.Should().Be(99); // Should remain unchanged
}

[Fact]
public void TryToSetValue_WithEnumProperty_ParsesEnumValue()
{
    // Arrange
    var target = new LoggingTarget
    {
        Properties = new Dictionary<string, IConfigurationSection?>
        {
            ["Status"] = CreateMockConfigurationSection("Active")
        }
    };
    var options = new TestOptionsWithEnum { Status = TestEnum.Inactive };

    // Act
    var action = target.CreateConfigurationAction(typeof(TestOptionsWithEnum));
    ((Action<TestOptionsWithEnum>)action)(options);

    // Assert
    options.Status.Should().Be(TestEnum.Active);
}

[Fact]
public void TryToSetValue_WithRegularProperty_ConvertsType()
{
    // Arrange
    var target = new LoggingTarget
    {
        Properties = new Dictionary<string, IConfigurationSection?>
        {
            ["IntProperty"] = CreateMockConfigurationSection("123")
        }
    };
    var options = new TestOptions { IntProperty = 0 };

    // Act
    var action = target.CreateConfigurationAction(typeof(TestOptions));
    ((Action<TestOptions>)action)(options);

    // Assert
    options.IntProperty.Should().Be(123);
}

[Fact]
public void TryToSetValue_WithNullValue_UsesConfigurationGet()
{
    // Arrange - Create a configuration section with null Value but structured data for Get()
    var configData = new Dictionary<string, string?> 
    { 
        ["ComplexProperty:Name"] = "TestName",
        ["ComplexProperty:Value"] = "42"
    };
    var configuration = new ConfigurationBuilder()
        .AddInMemoryCollection(configData)
        .Build();
    var configSection = configuration.GetSection("ComplexProperty");
    
    // Verify: Value is null (no direct value), but Get<ComplexType>() works
    configSection.Value.Should().BeNull();
    
    var target = new LoggingTarget
    {
        Properties = new Dictionary<string, IConfigurationSection?> { ["ComplexProperty"] = configSection }
    };
    var options = new TestOptionsWithComplex { ComplexProperty = null };

    // Act
    var action = target.CreateConfigurationAction(typeof(TestOptionsWithComplex));
    ((Action<TestOptionsWithComplex>)action)(options);

    // Assert
    options.ComplexProperty.Should().NotBeNull();
    options.ComplexProperty!.Name.Should().Be("TestName");
    options.ComplexProperty.Value.Should().Be(42);
}

[Theory]
[InlineData(null, LogLevel.Information)] // No LogLevel property
[InlineData("", LogLevel.Information)] // Empty LogLevel value  
[InlineData("InvalidLevel", LogLevel.Information)] // Invalid enum value
[InlineData("Debug", LogLevel.Debug)] // Valid enum value
[InlineData("Information", LogLevel.Information)] // Valid enum value
[InlineData("Warning", LogLevel.Warning)] // Valid enum value
[InlineData("Error", LogLevel.Error)] // Valid enum value
[InlineData("Critical", LogLevel.Critical)] // Valid enum value
[InlineData("None", LogLevel.None)] // Valid enum value
[InlineData("debug", LogLevel.Debug)] // Case-insensitive parsing
public void GetLogLevel_WithVariousLogLevelValues_ReturnsExpectedLevel(string? logLevelValue, LogLevel expected)
{
    // Arrange
    var target = new LoggingTarget 
    { 
        Properties = new Dictionary<string, IConfigurationSection?>() 
    };
    
    if (logLevelValue != null)
    {
        target.Properties["LogLevel"] = CreateMockConfigurationSection(logLevelValue);
    }

    // Act
    var result = target.GetLogLevel();

    // Assert
    result.Should().Be(expected);
}

public class TestOptionsWithComplex 
{ 
    public ComplexType? ComplexProperty { get; set; } 
}

public class ComplexType
{
    public string? Name { get; set; }
    public int Value { get; set; }
}

    public class TestOptions
    {
        public string? StringProperty { get; set; }
        public int IntProperty { get; set; }
        public bool BoolProperty { get; set; }
        public string ReadOnlyProperty { get; } = "default";
    }

    private static LoggingTarget CreateTargetWithConnectionString(string value)
    {
        var configSection = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { [""] = value })
            .Build().GetSection("");

        return new LoggingTarget
        {
            Properties = new Dictionary<string, IConfigurationSection?> { ["ConnectionString"] = configSection }
        };
    }

    public class TestTelemetryConfig
    {
        public string? ConnectionString { get; set; }
    }

    public class TestTelemetryConfigWithoutConnectionString
    {
        public string? OtherProperty { get; set; }
    }
    
    public class TestOptionsWithEnum 
    { 
        public TestEnum Status { get; set; } 
    }

    public enum TestEnum 
    { 
        Inactive, 
        Active 
    }

    /// <summary>
    /// Creates a LoggingTarget with the specified FormatterType value.
    /// </summary>
    private static LoggingTarget CreateLoggingTarget(string? formatterValue)
    {
        var properties = new Dictionary<string, IConfigurationSection>();

        if (formatterValue != null)
        {
            var configSection = CreateMockConfigurationSection(formatterValue);
            properties["FormatterType"] = configSection;
        }

        return new LoggingTarget
        {
            Type = "Console",
            Enabled = true,
            Properties = properties!
        };
    }

    /// <summary>
    /// Creates a mock IConfigurationSection with the specified value.
    /// </summary>
    private static IConfigurationSection CreateMockConfigurationSection(string? value)
    {
        var configData = new Dictionary<string, string?>();
        if (value != null)
        {
            configData[""] = value; // Root value
        }

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configData)
            .Build();

        return configuration.GetSection("");
    }

    /// <summary>
    /// Extracts the type name without assembly information for comparison.
    /// </summary>
    private static string GetTypeNameWithoutAssembly(string fullTypeName)
    {
        return fullTypeName.Split(',')[0].Trim();
    }
}