using System.Reflection;
using FlexKit.Logging.Configuration;
using FlexKit.Logging.Log4Net.Detection;
using FluentAssertions;
using HarmonyLib;
using log4net.Appender;
using log4net.Core;
using log4net.Layout;
using Microsoft.Extensions.Configuration;
using NSubstitute;
using Xunit;
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable TooManyDeclarations
// ReSharper disable RedundantExtendsListEntry
// ReSharper disable InconsistentNaming
// ReSharper disable ClassTooBig

namespace FlexKit.Logging.Log4Net.Tests.Detection;

/// <summary>
/// Unit tests for Log4NetConfigurationBuilder class covering appender configuration scenarios.
/// </summary>
public class Log4NetConfigurationBuilderTests
{
    [Fact]
    public void BuildConfiguration_WhenTargetEnabledTrueAndAppenderTypeNotFound_ReturnsRepositoryWithFallbackAppender()
    {
        // Arrange
        var target = new LoggingTarget
        {
            Type = "NonExistentAppender",
            Enabled = true,
            Properties = new Dictionary<string, IConfigurationSection?>()
        };

        var config = new LoggingConfig
        {
            Targets = new Dictionary<string, LoggingTarget> { ["NonExistentAppender"] = target }
        };

        var builder = new Log4NetConfigurationBuilder();

        // Act
        var repository = builder.BuildConfiguration(config);

        // Assert
        repository.Should().NotBeNull();
        repository.Configured.Should().BeTrue();
        repository.Threshold.Should().NotBeNull();

        // Since _availableAppenders.TryGetValue returns false; TryConfigureAppender should return false
        // This results in 0 configured appenders, triggering a fallback console appender
        var hierarchy = (log4net.Repository.Hierarchy.Hierarchy)repository;
        hierarchy.Root.Appenders.Count.Should().BeGreaterThan(0, "fallback console appender should be configured");
    }

    [Fact]
    public void BuildConfiguration_WhenTargetEnabledTrueAndActivatorCreateInstanceReturnsNull_ReturnsNoFallbackAppender()
    {
        // Arrange - Create a custom appender detector that returns an abstract type
        var mockAppenderInfo = new Log4NetAppenderDetector.AppenderInfo
        {
            Name = "TestAbstract",
            AppenderType = typeof(TestAbstractAppender), // Abstract type - CreateInstance will return null
            Properties = [],
            RequiresActivation = false
        };

        var target = new LoggingTarget
        {
            Type = "TestAbstract",
            Enabled = true,
            Properties = new Dictionary<string, IConfigurationSection?>()
        };

        var config = new LoggingConfig
        {
            Targets = new Dictionary<string, LoggingTarget> { ["TestAbstract"] = target }
        };

        // Create a builder and inject mock appender info using reflection
        var builder = new Log4NetConfigurationBuilder();
        var availableAppendersField = typeof(Log4NetConfigurationBuilder)
            .GetField("_availableAppenders", BindingFlags.NonPublic | BindingFlags.Instance);

        var mockAppenders = new Dictionary<string, Log4NetAppenderDetector.AppenderInfo>(StringComparer.OrdinalIgnoreCase)
        {
            ["TestAbstract"] = mockAppenderInfo
        };

        availableAppendersField?.SetValue(builder, mockAppenders);

        // Act
        var repository = builder.BuildConfiguration(config);

        // Assert
        repository.Should().NotBeNull();
        repository.Configured.Should().BeTrue();

        // Since CreateAppenderInstance returns null for abstract type, TryConfigureAppender should return false
        // This results in 0 configured appenders, triggering fallback console appender
        var hierarchy = (log4net.Repository.Hierarchy.Hierarchy)repository;
        hierarchy.Root.Appenders.Count.Should().Be(0, "fallback console appender shouldn't be available");
    }

    [Fact]
    public void
        BuildConfiguration_WhenTargetEnabledTrueAndAppenderRequiresActivationAndIsIOptionHandler_ConfiguresAppenderSuccessfully()
    {
        // Arrange - Use TestValidAppender which implements IOptionHandler
        var mockAppenderInfo = new Log4NetAppenderDetector.AppenderInfo
        {
            Name = "TestValid",
            AppenderType = typeof(TestValidAppender),
            Properties = typeof(TestValidAppender).GetProperties()
                .Where(p => p is { CanWrite: true, SetMethod.IsPublic: true })
                .ToArray(),
            RequiresActivation = true
        };

        var target = new LoggingTarget
        {
            Type = "TestValid",
            Enabled = true,
            Properties = new Dictionary<string, IConfigurationSection?>()
        };

        var config = new LoggingConfig
        {
            Targets = new Dictionary<string, LoggingTarget> { ["TestValid"] = target }
        };

        // Create a builder and inject mock appender info
        var builder = new Log4NetConfigurationBuilder();
        var availableAppendersField = typeof(Log4NetConfigurationBuilder)
            .GetField("_availableAppenders", BindingFlags.NonPublic | BindingFlags.Instance);

        var mockAppenders = new Dictionary<string, Log4NetAppenderDetector.AppenderInfo>(StringComparer.OrdinalIgnoreCase)
        {
            ["TestValid"] = mockAppenderInfo
        };

        availableAppendersField?.SetValue(builder, mockAppenders);

        // Act
        var repository = builder.BuildConfiguration(config);

        // Assert
        repository.Should().NotBeNull();
        repository.Configured.Should().BeTrue();

        // Verify that the appender was configured successfully
        var hierarchy = (log4net.Repository.Hierarchy.Hierarchy)repository;
        hierarchy.Root.Appenders.Count.Should().BeGreaterThan(0);

        // Verify a specific logger was created for the target
        var targetLogger = hierarchy.GetLogger("TestValid");
        targetLogger.Should().NotBeNull();

        // Since appenderInfo.RequiresActivation is true and TestValidAppender implements IOptionHandler,
        // the ActivateOptions path should be taken
    }

    [Fact]
    public void
        BuildConfiguration_WhenTargetEnabledTrueAndAppenderRequiresActivationButNotIOptionHandler_ConfiguresAppenderWithoutActivation()
    {
        // Arrange - Use TestMinimalAppender which doesn't implement IOptionHandler
        var mockAppenderInfo = new Log4NetAppenderDetector.AppenderInfo
        {
            Name = "TestMinimal",
            AppenderType = typeof(TestMinimalAppender),
            Properties = typeof(TestMinimalAppender).GetProperties()
                .Where(p => p is { CanWrite: true, SetMethod.IsPublic: true })
                .ToArray(),
            RequiresActivation = true // Set to true but appender doesn't implement IOptionHandler
        };

        var target = new LoggingTarget
        {
            Type = "TestMinimal",
            Enabled = true,
            Properties = new Dictionary<string, IConfigurationSection?>()
        };

        var config = new LoggingConfig
        {
            Targets = new Dictionary<string, LoggingTarget> { ["TestMinimal"] = target }
        };

        // Create a builder and inject mock appender info
        var builder = new Log4NetConfigurationBuilder();
        var availableAppendersField = typeof(Log4NetConfigurationBuilder)
            .GetField("_availableAppenders", BindingFlags.NonPublic | BindingFlags.Instance);

        var mockAppenders = new Dictionary<string, Log4NetAppenderDetector.AppenderInfo>(StringComparer.OrdinalIgnoreCase)
        {
            ["TestMinimal"] = mockAppenderInfo
        };

        availableAppendersField?.SetValue(builder, mockAppenders);

        // Act
        var repository = builder.BuildConfiguration(config);

        // Assert
        repository.Should().NotBeNull();
        repository.Configured.Should().BeTrue();

        // Verify that the appender was configured successfully
        var hierarchy = (log4net.Repository.Hierarchy.Hierarchy)repository;
        hierarchy.Root.Appenders.Count.Should().BeGreaterThan(0);

        // Verify a specific logger was created for the target
        var targetLogger = hierarchy.GetLogger("TestMinimal");
        targetLogger.Should().NotBeNull();

        // Since TestMinimalAppender doesn't implement IOptionHandler,
        // the condition (appenderInfo.RequiresActivation && appender is IOptionHandler optionHandler) 
        // evaluates to false, so ActivateOptions should not be called
    }

    [Fact]
    public void
        BuildConfiguration_WhenTargetEnabledTrueAndActivatorCreateInstanceReturnsNull_ReturnsRepositoryWithFallbackAppender()
    {
        // Arrange - Create a custom appender detector that returns an abstract type
        var mockAbstractAppenderInfo = new Log4NetAppenderDetector.AppenderInfo
        {
            Name = "TestAbstract",
            AppenderType = typeof(TestAbstractAppender), // Abstract type - CreateInstance will return null
            Properties = [],
            RequiresActivation = false
        };

        // Also need a console appender for fallback logic
        var mockConsoleAppenderInfo = new Log4NetAppenderDetector.AppenderInfo
        {
            Name = "Console",
            AppenderType = typeof(ConsoleAppender), // Real console appender for fallback
            Properties = typeof(ConsoleAppender).GetProperties()
                .Where(p => p is { CanWrite: true, SetMethod.IsPublic: true })
                .ToArray(),
            RequiresActivation = false
        };

        var target = new LoggingTarget
        {
            Type = "TestAbstract",
            Enabled = true,
            Properties = new Dictionary<string, IConfigurationSection?>()
        };

        var config = new LoggingConfig
        {
            Targets = new Dictionary<string, LoggingTarget> { ["TestAbstract"] = target }
        };

        // Create a builder and inject mock appender info using reflection
        var builder = new Log4NetConfigurationBuilder();
        var availableAppendersField = typeof(Log4NetConfigurationBuilder)
            .GetField("_availableAppenders", BindingFlags.NonPublic | BindingFlags.Instance);

        var mockAppenders = new Dictionary<string, Log4NetAppenderDetector.AppenderInfo>(StringComparer.OrdinalIgnoreCase)
        {
            ["TestAbstract"] = mockAbstractAppenderInfo,
            ["Console"] = mockConsoleAppenderInfo // Add console appender for fallback
        };

        availableAppendersField?.SetValue(builder, mockAppenders);

        // Act
        var repository = builder.BuildConfiguration(config);

        // Assert
        repository.Should().NotBeNull();
        repository.Configured.Should().BeTrue();

        // Since CreateAppenderInstance returns null for abstract type, TryConfigureAppender should return false
        // This results in 0 configured appenders, triggering fallback console appender
        var hierarchy = (log4net.Repository.Hierarchy.Hierarchy)repository;
        hierarchy.Root.Appenders.Count.Should().BeGreaterThan(0, "fallback console appender should be configured");
    }

    [Fact]
    public void BuildConfiguration_WhenExceptionThrownInActivateOptions_CatchesExceptionAndContinuesWithFallback()
    {
        // Arrange - Create an appender that throws in ActivateOptions
        var mockAppenderInfo = new Log4NetAppenderDetector.AppenderInfo
        {
            Name = "ThrowingActivateAppender",
            AppenderType = typeof(ThrowingActivateAppender),
            Properties = [],
            RequiresActivation = true // This will trigger the ActivateOptions call
        };

        var mockConsoleAppenderInfo = new Log4NetAppenderDetector.AppenderInfo
        {
            Name = "Console",
            AppenderType = typeof(ConsoleAppender),
            Properties = [],
            RequiresActivation = false
        };

        var target = new LoggingTarget
        {
            Type = "ThrowingActivateAppender",
            Enabled = true,
            Properties = new Dictionary<string, IConfigurationSection?>()
        };

        var config = new LoggingConfig
        {
            Targets = new Dictionary<string, LoggingTarget> { ["ThrowingActivateAppender"] = target }
        };

        var builder = new Log4NetConfigurationBuilder();
        var availableAppendersField = typeof(Log4NetConfigurationBuilder)
            .GetField("_availableAppenders", BindingFlags.NonPublic | BindingFlags.Instance);

        var mockAppenders = new Dictionary<string, Log4NetAppenderDetector.AppenderInfo>(StringComparer.OrdinalIgnoreCase)
        {
            ["ThrowingActivateAppender"] = mockAppenderInfo,
            ["Console"] = mockConsoleAppenderInfo
        };

        availableAppendersField?.SetValue(builder, mockAppenders);

        // Act
        var repository = builder.BuildConfiguration(config);

        // Assert
        repository.Should().NotBeNull();
        repository.Configured.Should().BeTrue();

        // Exception in ActivateOptions should be caught, resulting in 0 configured appenders and fallback
        var hierarchy = (log4net.Repository.Hierarchy.Hierarchy)repository;
        hierarchy.Root.Appenders.Count.Should().BeGreaterThan(
            0,
            "fallback console appender should be configured after ActivateOptions exception");
    }

    [Fact]
    public void BuildConfiguration_WhenCreateConsoleAppenderActivatorCreateInstanceReturnsNull_HandlesFallbackGracefully()
    {
        // Arrange - Create a config with no targets to trigger fallback console appender
        var config = new LoggingConfig
        {
            Targets = new Dictionary<string, LoggingTarget>() // Empty targets will trigger fallback
        };

        var builder = new Log4NetConfigurationBuilder();

        // Patch Activator.CreateInstance to return null for ConsoleAppender type
        var harmony = new Harmony("test.consoleappender.null");
        var original = typeof(Activator).GetMethod("CreateInstance", [typeof(Type)]);
        var prefix = typeof(Log4NetConfigurationBuilderTests)
            .GetMethod(nameof(ReturnNullForConsoleAppender), BindingFlags.NonPublic | BindingFlags.Static);

        harmony.Patch(original, prefix: new HarmonyMethod(prefix));

        try
        {
            // Act
            var repository = builder.BuildConfiguration(config);

            // Assert
            repository.Should().NotBeNull();
            repository.Configured.Should().BeTrue();

            // When CreateConsoleAppender's Activator.CreateInstance returns null,
            // it should return early and not add any appenders
            var hierarchy = (log4net.Repository.Hierarchy.Hierarchy)repository;
            hierarchy.Root.Appenders.Count.Should().Be(0, "no appenders should be configured when console appender creation fails");
        }
        finally
        {
            harmony.UnpatchAll("test.consoleappender.null");
        }
    }

    [Fact]
    public void BuildConfiguration_WhenCreateConsoleAppenderThrowsException_CatchesExceptionGracefully()
    {
        // Arrange - Create a config with no targets to trigger fallback console appender
        var config = new LoggingConfig
        {
            Targets = new Dictionary<string, LoggingTarget>() // Empty targets will trigger fallback
        };

        var builder = new Log4NetConfigurationBuilder();

        // Patch Activator.CreateInstance to throw an exception for ConsoleAppender type
        var harmony = new Harmony("test.consoleappender.exception");
        var original = typeof(Activator).GetMethod("CreateInstance", [typeof(Type)]);
        var prefix = typeof(Log4NetConfigurationBuilderTests)
            .GetMethod(nameof(ThrowExceptionForConsoleAppender), BindingFlags.NonPublic | BindingFlags.Static);

        harmony.Patch(original, prefix: new HarmonyMethod(prefix));

        try
        {
            // Act
            var repository = builder.BuildConfiguration(config);

            // Assert
            repository.Should().NotBeNull();
            repository.Configured.Should().BeTrue();

            // When CreateConsoleAppender throws an exception, it should be caught
            // and the method should continue gracefully without crashing
            var hierarchy = (log4net.Repository.Hierarchy.Hierarchy)repository;
            hierarchy.Root.Appenders.Count.Should().Be(
                0,
                "no appenders should be configured when console appender creation throws");
        }
        finally
        {
            harmony.UnpatchAll("test.consoleappender.exception");
        }
    }

    [Theory]
    [InlineData("TRACE", "Level.Trace")]
    [InlineData("DEBUG", "Level.Debug")]
    [InlineData("INFORMATION", "Level.Info")]
    [InlineData("WARNING", "Level.Warn")]
    [InlineData("ERROR", "Level.Error")]
    [InlineData("CRITICAL", "Level.Fatal")]
    [InlineData("NONE", "Level.Off")]
    [InlineData("UNKNOWN", "Level.Info")] // Default case
    [InlineData(null, "Level.Info")] // Null case
    [InlineData("", "Level.Info")] // Empty string case
    [InlineData("trace", "Level.Trace")] // Lowercase
    [InlineData("Debug", "Level.Debug")] // Mixed case
    public void BuildConfiguration_GetLogLevelFromTarget_ReturnsCorrectLogLevel(string? logLevelValue, string expectedLevelName)
    {
        // Arrange
        var mockConfigSection = Substitute.For<IConfigurationSection>();
        mockConfigSection.Value.Returns(logLevelValue);

        var target = new LoggingTarget
        {
            Type = "TestValid",
            Enabled = true,
            Properties = new Dictionary<string, IConfigurationSection?>()
        };

        if (logLevelValue != null)
        {
            target.Properties["LogLevel"] = mockConfigSection;
        }

        var mockAppenderInfo = new Log4NetAppenderDetector.AppenderInfo
        {
            Name = "TestValid",
            AppenderType = typeof(TestValidAppender),
            Properties = [],
            RequiresActivation = false
        };

        var config = new LoggingConfig
        {
            Targets = new Dictionary<string, LoggingTarget> { ["TestValid"] = target }
        };

        var builder = new Log4NetConfigurationBuilder();
        var availableAppendersField = typeof(Log4NetConfigurationBuilder)
            .GetField("_availableAppenders", BindingFlags.NonPublic | BindingFlags.Instance);

        var mockAppenders = new Dictionary<string, Log4NetAppenderDetector.AppenderInfo>(StringComparer.OrdinalIgnoreCase)
        {
            ["TestValid"] = mockAppenderInfo
        };

        availableAppendersField?.SetValue(builder, mockAppenders);

        // Act
        var repository = builder.BuildConfiguration(config);

        // Assert
        repository.Should().NotBeNull();
        var hierarchy = (log4net.Repository.Hierarchy.Hierarchy)repository;
        var targetLogger = hierarchy.GetLogger("TestValid") as log4net.Repository.Hierarchy.Logger;

        targetLogger.Should().NotBeNull();

        // Convert the expected level name to an actual Level object for comparison
        var expectedLevel = expectedLevelName switch
        {
            "Level.Trace" => Level.Trace,
            "Level.Debug" => Level.Debug,
            "Level.Info" => Level.Info,
            "Level.Warn" => Level.Warn,
            "Level.Error" => Level.Error,
            "Level.Fatal" => Level.Fatal,
            "Level.Off" => Level.Off,
            _ => Level.Info
        };

        targetLogger.Level.Should().Be(expectedLevel);
    }

    [Fact]
    public void BuildConfiguration_WhenAppenderAlreadyHasLayout_DoesNotOverrideExistingLayout()
    {
        // Arrange - Create appender that initializes with a layout
        var mockAppenderInfo = new Log4NetAppenderDetector.AppenderInfo
        {
            Name = "TestAppenderWithPresetLayout",
            AppenderType = typeof(TestAppenderWithPresetLayout),
            Properties = [],
            RequiresActivation = false
        };

        var target = new LoggingTarget
        {
            Type = "TestAppenderWithPresetLayout",
            Enabled = true,
            Properties = new Dictionary<string, IConfigurationSection?>()
        };

        var config = new LoggingConfig
        {
            Targets = new Dictionary<string, LoggingTarget> { ["TestAppenderWithPresetLayout"] = target }
        };

        var builder = new Log4NetConfigurationBuilder();
        var availableAppendersField = typeof(Log4NetConfigurationBuilder)
            .GetField("_availableAppenders", BindingFlags.NonPublic | BindingFlags.Instance);

        var mockAppenders = new Dictionary<string, Log4NetAppenderDetector.AppenderInfo>(StringComparer.OrdinalIgnoreCase)
        {
            ["TestAppenderWithPresetLayout"] = mockAppenderInfo
        };

        availableAppendersField?.SetValue(builder, mockAppenders);

        // Act
        var repository = builder.BuildConfiguration(config);

        // Assert
        repository.Should().NotBeNull();
        var hierarchy = (log4net.Repository.Hierarchy.Hierarchy)repository;
        var appender = hierarchy.Root.Appenders[0] as TestAppenderWithPresetLayout;

        appender.Should().NotBeNull();
        appender.Layout.Should().NotBeNull();
        ((PatternLayout)appender.Layout).ConversionPattern.Should().Be("PRESET", "existing layout should not be overridden");
    }

    [Fact]
    public void BuildConfiguration_WhenPropertyNameExistsInDictionary_FindPropertySectionReturnsExactMatch()
    {
        // Arrange
        var mockConfigSection = Substitute.For<IConfigurationSection>();
        mockConfigSection.Value.Returns("test-value");

        var mockAppenderInfo = new Log4NetAppenderDetector.AppenderInfo
        {
            Name = "TestValid",
            AppenderType = typeof(TestValidAppender),
            Properties = typeof(TestValidAppender).GetProperties()
                .Where(p => p is { CanWrite: true, SetMethod.IsPublic: true })
                .ToArray(),
            RequiresActivation = false
        };

        var target = new LoggingTarget
        {
            Type = "TestValid",
            Enabled = true,
            Properties = new Dictionary<string, IConfigurationSection?>
            {
                ["TestProperty"] = mockConfigSection // Exact match for TestValidAppender.TestProperty
            }
        };

        var config = new LoggingConfig
        {
            Targets = new Dictionary<string, LoggingTarget> { ["TestValid"] = target }
        };

        var builder = new Log4NetConfigurationBuilder();
        var availableAppendersField = typeof(Log4NetConfigurationBuilder)
            .GetField("_availableAppenders", BindingFlags.NonPublic | BindingFlags.Instance);

        var mockAppenders = new Dictionary<string, Log4NetAppenderDetector.AppenderInfo>(StringComparer.OrdinalIgnoreCase)
        {
            ["TestValid"] = mockAppenderInfo
        };

        availableAppendersField?.SetValue(builder, mockAppenders);

        // Act
        var repository = builder.BuildConfiguration(config);

        // Assert
        repository.Should().NotBeNull();

        // Verify the exact match was found and used (property should be set)
        var hierarchy = (log4net.Repository.Hierarchy.Hierarchy)repository;
        var appender = hierarchy.Root.Appenders[0] as TestValidAppender;
        appender.Should().NotBeNull();
        appender.TestProperty.Should().Be("test-value", "exact match should be found and property set");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void FindPropertySection_WhenPropertyNameIsNullOrEmpty_ReturnsNull(string? propertyName)
    {
        // Arrange
        var properties = new Dictionary<string, IConfigurationSection?>
        {
            ["TestProperty"] = Substitute.For<IConfigurationSection>()
        };

        // Act - Call the private method directly using reflection
        var result = InvokePrivateStaticMethod<IConfigurationSection?>("FindPropertySection", properties, propertyName);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void FindPropertySection_WhenPropertyNameExistsInDictionary_ReturnsExactMatch()
    {
        // Arrange
        var mockConfigSection = Substitute.For<IConfigurationSection>();
        var properties = new Dictionary<string, IConfigurationSection?>
        {
            ["TestProperty"] = mockConfigSection,
            ["OtherProperty"] = Substitute.For<IConfigurationSection>()
        };

        // Act - Call the private method directly using reflection
        var result = InvokePrivateStaticMethod<IConfigurationSection?>("FindPropertySection", properties, "TestProperty");

        // Assert
        result.Should().BeSameAs(mockConfigSection);
    }

    [Fact]
    public void BuildConfiguration_WhenPropertySetterThrows_CatchesExceptionAndContinues()
    {
        // Arrange
        var mockConfigSection = Substitute.For<IConfigurationSection>();
        mockConfigSection.Value.Returns("not-a-number"); // This will cause Convert.ChangeType to throw

        var mockAppenderInfo = new Log4NetAppenderDetector.AppenderInfo
        {
            Name = "TestValid",
            AppenderType = typeof(TestValidAppender),
            Properties = typeof(TestValidAppender).GetProperties()
                .Where(p => p is { CanWrite: true, SetMethod.IsPublic: true })
                .ToArray(),
            RequiresActivation = false
        };

        var target = new LoggingTarget
        {
            Type = "TestValid",
            Enabled = true,
            Properties = new Dictionary<string, IConfigurationSection?>
            {
                ["NumberProperty"] = mockConfigSection // TestValidAppender.NumberProperty is an int, this will throw
            }
        };

        var config = new LoggingConfig
        {
            Targets = new Dictionary<string, LoggingTarget> { ["TestValid"] = target }
        };

        var builder = new Log4NetConfigurationBuilder();
        var availableAppendersField = typeof(Log4NetConfigurationBuilder)
            .GetField("_availableAppenders", BindingFlags.NonPublic | BindingFlags.Instance);

        var mockAppenders = new Dictionary<string, Log4NetAppenderDetector.AppenderInfo>(StringComparer.OrdinalIgnoreCase)
        {
            ["TestValid"] = mockAppenderInfo
        };

        availableAppendersField?.SetValue(builder, mockAppenders);

        // Act
        var repository = builder.BuildConfiguration(config);

        // Assert
        repository.Should().NotBeNull();
        repository.Configured.Should().BeTrue();

        // The appender should still be configured despite the property setting failure
        var hierarchy = (log4net.Repository.Hierarchy.Hierarchy)repository;
        hierarchy.Root.Appenders.Count.Should().BeGreaterThan(0);

        var appender = hierarchy.Root.Appenders[0] as TestValidAppender;
        appender.Should().NotBeNull();
        appender.NumberProperty.Should().Be(0, "property should remain at default value when setting fails");
    }
    
    [Fact]
    public void BuildConfiguration_WhenActivatorCreateInstanceReturnsNull_HandlesGracefully()
    {
        // Arrange - Use Nullable<int> which Activator.CreateInstance can return as null
        var mockAppenderInfo = new Log4NetAppenderDetector.AppenderInfo
        {
            Name = "NullableInt",
            AppenderType = typeof(int?), // Nullable type - CreateInstance can return null
            Properties = [],
            RequiresActivation = false
        };

        var target = new LoggingTarget
        {
            Type = "NullableInt",
            Enabled = true,
            Properties = new Dictionary<string, IConfigurationSection?>()
        };

        var config = new LoggingConfig
        {
            Targets = new Dictionary<string, LoggingTarget> { ["NullableInt"] = target }
        };

        var builder = new Log4NetConfigurationBuilder();
        var availableAppendersField = typeof(Log4NetConfigurationBuilder)
            .GetField("_availableAppenders", BindingFlags.NonPublic | BindingFlags.Instance);
    
        var mockAppenders = new Dictionary<string, Log4NetAppenderDetector.AppenderInfo>(StringComparer.OrdinalIgnoreCase)
        {
            ["NullableInt"] = mockAppenderInfo
        };
        availableAppendersField?.SetValue(builder, mockAppenders);

        // Act
        var repository = builder.BuildConfiguration(config);

        // Assert
        repository.Should().NotBeNull();
        repository.Configured.Should().BeTrue();
    
        // When Activator.CreateInstance returns null, the cast to IAppender? is also null
        // This hits the appender == null condition
        var hierarchy = (log4net.Repository.Hierarchy.Hierarchy)repository;
        hierarchy.Root.Appenders.Count.Should().Be(0, "fallback console appender should not be configured");
    }

    private static T InvokePrivateStaticMethod<T>(string methodName, params object?[] parameters)
    {
        var method = typeof(Log4NetConfigurationBuilder).GetMethod(
            methodName,
            BindingFlags.NonPublic | BindingFlags.Static);

        method.Should().NotBeNull($"method {methodName} should exist");

        var result = method.Invoke(null, parameters);
        return (T)result!;
    }

    public sealed class TestAppenderWithPresetLayout : AppenderSkeleton
    {
        public TestAppenderWithPresetLayout()
        {
            Layout = new PatternLayout("PRESET"); // Pre-set layout
        }

        protected override void Append(LoggingEvent loggingEvent)
        {
            // Test implementation
        }
    }

    private static bool ThrowExceptionForConsoleAppender(Type type)
    {
        // Check if this is being called for a console appender type
        if (type.Name.Contains("Console", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Test exception in console appender creation");
        }

        return true; // call original for all other types
    }

    private static bool ReturnNullForConsoleAppender(ref object __result, Type type)
    {
        // Check if this is being called for a console appender type
        if (type.Name.Contains("Console", StringComparison.OrdinalIgnoreCase))
        {
            __result = null!;
            return false; // skip original method
        }

        return true; // call original for all other types
    }

    // Simple test appender that throws in ActivateOptions
    public class ThrowingActivateAppender : AppenderSkeleton, IOptionHandler
    {
        protected override void Append(LoggingEvent loggingEvent)
        {
            // Simple implementation
        }

        public override void ActivateOptions()
        {
            throw new InvalidOperationException("Test exception in ActivateOptions");
        }
    }
}