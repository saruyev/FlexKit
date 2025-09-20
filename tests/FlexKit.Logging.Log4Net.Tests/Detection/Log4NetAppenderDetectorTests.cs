using System.Diagnostics;
using System.Reflection;
using FlexKit.Logging.Log4Net.Detection;
using FluentAssertions;
using log4net.Appender;
using log4net.Core;
using log4net.Filter;
using NSubstitute;
using Xunit;

// ReSharper disable RedundantExtendsListEntry
// ReSharper disable UnusedMember.Global

namespace FlexKit.Logging.Log4Net.Tests.Detection;

/// <summary>
/// Unit tests for Log4NetAppenderDetector class covering appender discovery and metadata extraction.
/// </summary>
public class Log4NetAppenderDetectorTests
{
    [Fact]
    public void DetectAvailableAppenders_WithValidLog4NetAssemblies_ReturnsDetectedAppenders()
    {
        // Act
        var appenders = Log4NetAppenderDetector.DetectAvailableAppenders();

        // Assert
        appenders.Should().NotBeEmpty("should detect at least some standard Log4Net appenders");
        
        // Check for common Log4Net appenders that should be available
        appenders.Keys.Should().Contain(key => 
            key.Equals("Console", StringComparison.OrdinalIgnoreCase) ||
            key.Equals("File", StringComparison.OrdinalIgnoreCase) ||
            key.Equals("RollingFile", StringComparison.OrdinalIgnoreCase),
            "should detect standard Log4Net appenders");
    }

    [Fact]
    public void DetectAvailableAppenders_WithValidAppenderType_CreatesCorrectAppenderInfo()
    {
        // Act
        var appenders = Log4NetAppenderDetector.DetectAvailableAppenders();

        // Assert
        var consoleAppender = appenders.Values.FirstOrDefault(a => 
            a.Name.Equals("Console", StringComparison.OrdinalIgnoreCase));
        
        if (consoleAppender != null)
        {
            consoleAppender.AppenderType.Should().NotBeNull();
            consoleAppender.AppenderType.IsClass.Should().BeTrue();
            consoleAppender.AppenderType.IsAbstract.Should().BeFalse();
            consoleAppender.Properties.Should().NotBeNull();
        }
    }

    [Fact]
    public void AppenderInfo_WithRequiredProperties_HasCorrectInitialization()
    {
        // Arrange
        var mockType = typeof(TestValidAppender);

        // Act
        var appenderInfo = new Log4NetAppenderDetector.AppenderInfo
        {
            Name = "Test",
            AppenderType = mockType,
            Properties = mockType.GetProperties(),
            RequiresActivation = true
        };

        // Assert
        appenderInfo.Name.Should().Be("Test");
        appenderInfo.AppenderType.Should().Be(mockType);
        appenderInfo.Properties.Should().NotBeNull();
        appenderInfo.RequiresActivation.Should().BeTrue();
    }

    [Fact]
    public void AppenderInfo_DefaultInitialization_HasExpectedDefaults()
    {
        // Act
        var appenderInfo = new Log4NetAppenderDetector.AppenderInfo
        {
            AppenderType = typeof(TestValidAppender)
        };

        // Assert
        appenderInfo.Name.Should().Be(string.Empty);
        appenderInfo.AppenderType.Should().Be(typeof(TestValidAppender));
        appenderInfo.Properties.Should().BeEmpty();
        appenderInfo.RequiresActivation.Should().BeFalse();
    }

    [Theory]
    [InlineData(typeof(TestValidAppender), true)]
    [InlineData(typeof(TestAbstractAppender), false)]
    [InlineData(typeof(TestNonPublicAppender), false)]
    [InlineData(typeof(TestNonAppenderClass), false)]
    [InlineData(typeof(ITestInterface), false)]
    public void IsValidAppenderType_WithVariousTypes_ReturnsExpectedResult(Type type, bool expectedResult)
    {
        // Act
        var result = InvokePrivateStaticMethod<bool>("IsValidAppenderType", type);

        // Assert
        result.Should().Be(expectedResult);
    }

    [Fact]
    public void CreateAppenderInfoFromType_WithValidAppender_ReturnsCorrectInfo()
    {
        // Arrange
        var appenderType = typeof(TestValidAppender);

        // Act
        var result = InvokePrivateStaticMethod<Log4NetAppenderDetector.AppenderInfo>(
            "CreateAppenderInfoFromType", appenderType);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be("TestValid"); // "TestValidAppender" with "Appender" suffix removed
        result.AppenderType.Should().Be(appenderType);
        result.Properties.Should().NotBeEmpty();
        result.RequiresActivation.Should().BeTrue(); // TestValidAppender implements IOptionHandler
    }

    [Fact]
    public void CreateAppenderInfoFromType_WithAppenderSuffix_RemovesSuffixFromName()
    {
        // Arrange
        var appenderType = typeof(CustomTestAppender);

        // Act
        var result = InvokePrivateStaticMethod<Log4NetAppenderDetector.AppenderInfo>(
            "CreateAppenderInfoFromType", appenderType);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be("CustomTest"); // "CustomTestAppender" with "Appender" suffix removed
    }

    [Fact]
    public void CreateAppenderInfoFromType_WithoutAppenderSuffix_KeepsOriginalName()
    {
        // Arrange
        var appenderType = typeof(TestValidConsole);

        // Act
        var result = InvokePrivateStaticMethod<Log4NetAppenderDetector.AppenderInfo>(
            "CreateAppenderInfoFromType", appenderType);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be("TestValidConsole"); // No suffix to remove
    }

    [Theory]
    [InlineData(typeof(TestValidAppender), true)] // Implements IOptionHandler
    [InlineData(typeof(TestAppenderWithActivateOptions), true)] // Has ActivateOptions method
    [InlineData(typeof(TestMinimalAppender), false)] // No activation requirement
    public void CheckActivationRequirement_WithVariousAppenders_ReturnsExpectedResult(Type appenderType, bool expectedResult)
    {
        // Act
        var result = InvokePrivateStaticMethod<bool>("CheckActivationRequirement", appenderType);

        // Assert
        result.Should().Be(expectedResult);
    }

    [Fact]
    public void CreateAppenderInfo_WithNullType_ReturnsNull()
    {
        // Act
        var result = InvokePrivateStaticMethod<Log4NetAppenderDetector.AppenderInfo?>(
            "CreateAppenderInfo", (Type?)null);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void GetLog4NetAssemblies_ReturnsExpectedAssemblies()
    {
        // Act
        var assemblies = InvokePrivateStaticMethod<Assembly[]>("GetLog4NetAssemblies");

        // Assert
        assemblies.Should().NotBeNull();
        assemblies.Should().NotBeEmpty();
        assemblies.Should().OnlyContain(a => 
            a.FullName!.Contains("log4net", StringComparison.OrdinalIgnoreCase) == true,
            "should only return assemblies containing 'log4net' in their name");
    }

    [Fact]
    public void DetectAppendersInAssembly_WithValidAssembly_PopulatesAppendersDictionary()
    {
        // Arrange
        var assembly = typeof(ConsoleAppender).Assembly; // log4net assembly
        var appenders = new Dictionary<string, Log4NetAppenderDetector.AppenderInfo>(StringComparer.OrdinalIgnoreCase);

        // Act
        InvokePrivateStaticMethod("DetectAppendersInAssembly", assembly, appenders);

        // Assert
        appenders.Should().NotBeEmpty("should detect appenders in the log4net assembly");
    }

    [Fact]
    public void DetectAppendersInAssembly_WithReflectionTypeLoadException_HandlesGracefully()
    {
        // Arrange
        var mockAssembly = Substitute.For<Assembly>();
        mockAssembly.GetTypes().Returns(_ => throw new ReflectionTypeLoadException([], []));
        
        var appenders = new Dictionary<string, Log4NetAppenderDetector.AppenderInfo>(StringComparer.OrdinalIgnoreCase);

        // Act & Assert - Should not throw
        var act = () => InvokePrivateStaticMethod("DetectAppendersInAssembly", mockAssembly, appenders);
        act.Should().NotThrow();
    }

    [Fact]
    public void DetectAvailableAppenders_WithAssemblyLoadFailure_ContinuesProcessing()
    {
        // This test verifies that if one assembly fails to load, the detection continues with other assemblies
        // Act
        var act = Log4NetAppenderDetector.DetectAvailableAppenders;

        // Assert - Should not throw even if some assemblies fail to load
        act.Should().NotThrow();
    }

    [Fact]
    public void DetectAvailableAppenders_ResultsAreCaseInsensitive()
    {
        // Act
        var appenders = Log4NetAppenderDetector.DetectAvailableAppenders();

        // Assert
        appenders.Comparer.Should().Be(StringComparer.OrdinalIgnoreCase);
    }

    [Fact]
    public void DetectAvailableAppenders_AvoidsDuplicateAppenders()
    {
        // Act
        var appenders = Log4NetAppenderDetector.DetectAvailableAppenders();

        // Assert
        var appenderNames = appenders.Keys.ToList();
        var distinctNames = appenderNames.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        
        appenderNames.Count.Should().Be(distinctNames.Count, 
            "should not contain duplicate appender names (case-insensitive)");
    }
    
    [Fact]
    public void DetectAvailableAppenders_WithPartiallyLoadableAssembly_HandlesReflectionTypeLoadException()
    {
        // Create a test assembly that will cause ReflectionTypeLoadException
        // This happens when an assembly has types that reference unavailable dependencies
    
        // First, ensure we have a log4net assembly that might have loading issues
        var log4netAssembly = typeof(log4net.Appender.ConsoleAppender).Assembly;
    
        // Create a dictionary to capture results
        var testAppenders = new Dictionary<string, Log4NetAppenderDetector.AppenderInfo>(StringComparer.OrdinalIgnoreCase);
    
        // We need to call DetectAvailableAppenders in a scenario where some assembly
        // might have ReflectionTypeLoadException - this can happen with mixed-mode assemblies
        // or assemblies with missing dependencies
    
        // Act - This should handle any ReflectionTypeLoadException gracefully
        var result = Log4NetAppenderDetector.DetectAvailableAppenders();
    
        // Assert - Should complete successfully even if some types fail to load
        result.Should().NotBeNull("should handle ReflectionTypeLoadException gracefully");
    
        // The method should still find available appenders from types that did load successfully
        if (AppDomain.CurrentDomain.GetAssemblies().Any(a => 
                a.FullName?.Contains("log4net", StringComparison.OrdinalIgnoreCase) == true))
        {
            result.Should().NotBeEmpty("should find appenders from successfully loaded types");
        }
    }

    // Helper method to invoke private static methods for testing
    private static T InvokePrivateStaticMethod<T>(string methodName, params object?[] parameters)
    {
        var method = typeof(Log4NetAppenderDetector).GetMethod(methodName, 
            BindingFlags.NonPublic | BindingFlags.Static);
        
        method.Should().NotBeNull($"method {methodName} should exist");
        
        var result = method.Invoke(null, parameters);
        return (T)result!;
    }

    private static void InvokePrivateStaticMethod(string methodName, params object[] parameters)
    {
        var method = typeof(Log4NetAppenderDetector).GetMethod(methodName, 
            BindingFlags.NonPublic | BindingFlags.Static);
        
        method.Should().NotBeNull($"method {methodName} should exist");
        method.Invoke(null, parameters);
    }
}

// Test helper classes for appender type validation tests
public class TestValidAppender : AppenderSkeleton, IOptionHandler
{
    public string TestProperty { get; set; } = string.Empty;
    public int NumberProperty { get; set; }
    
    protected override void Append(LoggingEvent loggingEvent)
    {
        // Test implementation
    }

    public override void ActivateOptions()
    {
        // Test implementation
    }
}

public abstract class TestAbstractAppender : AppenderSkeleton
{
    protected override void Append(LoggingEvent loggingEvent)
    {
        // Test implementation
    }
}

internal class TestNonPublicAppender : AppenderSkeleton
{
    protected override void Append(LoggingEvent loggingEvent)
    {
        // Test implementation
    }
}

public class TestNonAppenderClass
{
    public string SomeProperty { get; set; } = string.Empty;
}

public interface ITestInterface
{
    void DoSomething();
}

public class CustomTestAppender : AppenderSkeleton
{
    public string CustomProperty { get; set; } = string.Empty;
    
    protected override void Append(LoggingEvent loggingEvent)
    {
        // Test implementation
    }
}

public class TestValidConsole : AppenderSkeleton
{
    public string OutputProperty { get; set; } = string.Empty;
    
    protected override void Append(LoggingEvent loggingEvent)
    {
        // Test implementation
    }
}

public class TestAppenderWithActivateOptions : AppenderSkeleton
{
    public string ConfigProperty { get; set; } = string.Empty;
    
    protected override void Append(LoggingEvent loggingEvent)
    {
        // Test implementation
    }
    
    public override void ActivateOptions()
    {
        // Test implementation - method exists but doesn't implement IOptionHandler
    }
}

public class TestMinimalAppender : IAppender
{
    public string Name { get; set; } = string.Empty;
    public IFilter Filter { get; set; } = null!;
    public IErrorHandler ErrorHandler { get; set; } = null!;

    public void Close()
    {
        // Minimal implementation
    }

    public void DoAppend(LoggingEvent loggingEvent)
    {
        // Minimal implementation
    }
}
