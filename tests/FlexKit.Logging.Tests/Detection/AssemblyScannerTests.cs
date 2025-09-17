using System.Diagnostics;
using System.Reflection;
using Autofac;
using FlexKit.Logging.Configuration;
using FlexKit.Logging.Core;
using FlexKit.Logging.Detection;
using FluentAssertions;
using log4net.Repository;
using Microsoft.Extensions.Configuration;
using NSubstitute;
using Xunit;
using Xunit.Abstractions;

// ReSharper disable ComplexConditionExpression
// ReSharper disable PossibleMultipleEnumeration
// ReSharper disable TooManyDeclarations
// ReSharper disable UnusedMember.Global
// ReSharper disable UnusedMember.Local
// ReSharper disable UnusedType.Global
// ReSharper disable MemberCanBePrivate.Global

namespace FlexKit.Logging.Tests.Detection;

public class LastCollectionOrderer : ITestCollectionOrderer
{
    public IEnumerable<ITestCollection> OrderTestCollections(IEnumerable<ITestCollection> testCollections)
    {
        var ordered = testCollections
            .OrderBy(c => c.DisplayName.Contains("RunLast") ? 1 : 0);

        return ordered;
    }
}

[Collection("RunLast")]
public class AssemblyScannerTests
{
    [Fact]
    public void DiscoverCandidateTypes_ShouldReturnInterceptableTypes()
    {
        // Act
        var candidateTypes = AssemblyScanner.DiscoverCandidateTypes().ToList();

        // Assert
        candidateTypes.Should().NotBeEmpty();
        candidateTypes.Should().OnlyContain(t => t.IsClass && t.IsPublic && !t.IsAbstract);

        // Verify all returned types have interceptable methods
        foreach (var type in candidateTypes)
        {
            var hasInterceptableMethods = type.GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .Where(m => m.DeclaringType == type)
                .Any(m => m is { IsPublic: true, IsStatic: false, IsConstructor: false, IsSpecialName: false } &&
                          m.DeclaringType != typeof(object));

            hasInterceptableMethods.Should().BeTrue($"type {type.Name} should have interceptable methods");
        }
    }

    [Fact]
    public void DiscoverCandidateTypes_ShouldExcludeSystemAssemblies()
    {
        // Act
        var candidateTypes = AssemblyScanner.DiscoverCandidateTypes().ToList();
        var assemblies = candidateTypes.Select(t => t.Assembly).Distinct();

        // Assert
        assemblies.Should().NotContain(a => a.FullName != null && a.FullName.StartsWith("System."));
        assemblies.Should().NotContain(a => a.FullName != null && a.FullName.StartsWith("Microsoft."));
        assemblies.Should().NotContain(a => a.FullName != null && a.FullName.StartsWith("mscorlib"));
        assemblies.Should().NotContain(a => a.FullName != null && a.FullName.StartsWith("Autofac"));
        assemblies.Should().NotContain(a => a.FullName != null && a.FullName.StartsWith("Castle."));
        assemblies.Should().NotContain(a => a.FullName != null && a.FullName.StartsWith("Newtonsoft."));
    }

    [Fact]
    public void DiscoverCandidateTypes_ShouldIncludeTestAssembly()
    {
        // Act
        var candidateTypes = AssemblyScanner.DiscoverCandidateTypes().ToList();
        var assemblies = candidateTypes.Select(t => t.Assembly).Distinct();

        // Assert
        var testAssembly = typeof(AssemblyScannerTests).Assembly;
        assemblies.Should().Contain(testAssembly, "test assembly should be included in scanning");
    }

    [Fact]
    public void DiscoverCandidateTypes_ShouldBeConsistent()
    {
        // Act
        var firstCall = AssemblyScanner.DiscoverCandidateTypes().ToList();
        var secondCall = AssemblyScanner.DiscoverCandidateTypes().ToList();

        // Assert
        firstCall.Should().BeEquivalentTo(secondCall, "multiple calls should return consistent results");
    }

    [Fact]
    public void DiscoverCandidateTypes_ShouldCompleteReasonably()
    {
        // Act
        var stopwatch = Stopwatch.StartNew();
        var candidateTypes = AssemblyScanner.DiscoverCandidateTypes().ToList();
        stopwatch.Stop();

        // Assert
        candidateTypes.Should().NotBeEmpty();
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(5000, "discovery should complete within reasonable time");
    }
    
    [Fact]
    public void ShouldScanAssembly_WithFlexKitNonTestAssembly_ShouldExclude()
    {
        // Force FlexKit.Logging.Log4Net to be loaded by referencing a type from it
        _ = typeof(FlexKit.Logging.Log4Net.Core.Log4NetLogger);
    
        // Act
        var candidateTypes = AssemblyScanner.DiscoverCandidateTypes().ToList();
        var scannedAssemblies = candidateTypes.Select(t => t.Assembly).Distinct();
    
        // Assert - FlexKit.Logging.Log4Net should be excluded (it doesn't contain "Tests")
        var log4NetAssembly = typeof(FlexKit.Logging.Log4Net.Core.Log4NetLogger).Assembly;
        var loadedAssemblies = AppDomain.CurrentDomain.GetAssemblies();
        loadedAssemblies.Should().Contain(log4NetAssembly, 
            "Log4Net assembly should be loaded in AppDomain");
        
        scannedAssemblies.Should().NotContain(log4NetAssembly,
            "FlexKit.Logging assemblies without 'Tests' should be excluded");
        
    }
    
    [Fact]
    public void RegisterLoggingInfrastructure_WithLog4NetAssemblyLoaded_SkipsBackgroundLoggingAndMelRegistration()
    {
        // Arrange
        var builder = new ContainerBuilder();
        builder.Register(_ => new ConfigurationBuilder().Build()).As<IConfiguration>();
    
        // Act - Load log4net assembly into appdomain
        _ = new FlexKit.Logging.Log4Net.Core.Log4NetLogger("test", Substitute.For<ILoggerRepository>(), new LoggingConfig());
    
        // Register infrastructure after loading Log4Net assembly
        builder.RegisterLoggingInfrastructure();
        var container = builder.Build();
    
        // Assert - Verify that background logging types and mel tools are not registered
        container.IsRegistered<BackgroundLoggingService>().Should().BeFalse("BackgroundLoggingService should not be registered when provider assemblies are present");
        container.IsRegistered<IBackgroundLog>().Should().BeFalse("IBackgroundLog should not be registered when provider assemblies are present");
        container.IsRegistered<FormattedLogWriter>().Should().BeFalse("FormattedLogWriter should not be registered when provider assemblies are present");
    
        // Core components should still be registered
        container.IsRegistered<LoggingConfig>().Should().BeTrue("LoggingConfig should always be registered");
    
        container.Dispose();
    }

    // Test for edge case 1: ReferencesFlexKitLogging method's catch block
    [Fact]
    public void ReferencesFlexKitLogging_ThrowsException_ReturnsFalse()
    {
        // Arrange
        var mockAssembly = Substitute.For<Assembly>();
        // Configure GetReferencedAssemblies to throw an exception
        mockAssembly.GetReferencedAssemblies().Returns(_ => throw new Exception("Simulated assembly loading error"));

        // Act
        // Use reflection to access the private static method
        var referencesFlexKitLogging = GetPrivateStaticMethod<ReferencesFlexKitLoggingDelegate>("ReferencesFlexKitLogging");
        var result = referencesFlexKitLogging(mockAssembly);

        // Assert
        result.Should().BeFalse();
    }

    //Test for edge case 2: ScanAssembly method's ReflectionTypeLoadException catch block
    [Fact]
    public void ScanAssembly_ThrowsReflectionTypeLoadException_ReturnsOnlyValidTypes()
    {
        // Arrange
        var mockAssembly = Substitute.For<Assembly>();
    
        // Create fake types for the exception
        var validType1 = typeof(TestClassWithPublicMethod); // An interceptable type
        Type nullType = null!;
        var validType2 = typeof(AnotherTestClass); // Another interceptable type
    
        // Simulate ReflectionTypeLoadException
        var ex = new ReflectionTypeLoadException(
            [validType1, nullType, validType2], // Loaded types (some valid, some not, some null)
            [new Exception("Error loading type 1"), null, new Exception("Error loading type 3"), null] // Corresponding exceptions
        );
    
        // Configure GetTypes to throw the ReflectionTypeLoadException
        mockAssembly.When(x => x.GetTypes()).Throw(ex);
    
        // Act
        var scanAssembly = GetPrivateStaticMethod<ScanAssemblyDelegate>("ScanAssembly");
        var result = scanAssembly(mockAssembly);
    
        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2); // Only validType1 and validType2 should be returned
        result.Should().Contain(validType1);
        result.Should().Contain(validType2);
        result.Should().NotContain(nullType);
    }

    // Test for edge case 3: ScanAssembly method's generic Exception catch block
    [Fact]
    public void ScanAssembly_ThrowsGenericException_ReturnsEmptyCollectionAndWritesToDebug()
    {
        // Arrange
        var mockAssembly = Substitute.For<Assembly>();
        var errorMessage = "Simulated generic assembly scanning error";
        mockAssembly.When(x => x.GetTypes()).Throw(new Exception(errorMessage));

        // Redirect Debug.WriteLine output to capture it
        var debugOutput = new StringWriter();
        Trace.Listeners.Add(new TextWriterTraceListener(debugOutput));

        // Act
        // Use reflection to access the private static method
        var scanAssembly = GetPrivateStaticMethod<ScanAssemblyDelegate>("ScanAssembly");
        var result = scanAssembly(mockAssembly);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();

        // Check debug output
        debugOutput.ToString().Should().Contain($"Warning: Failed to scan assembly {mockAssembly.FullName}: {errorMessage}");

        // Clean up debug listener
        Trace.Listeners.Clear();
    }
    
    public delegate bool ReferencesFlexKitLoggingDelegate(Assembly assembly);
    public delegate IEnumerable<Type> ScanAssemblyDelegate(Assembly assembly);
    
    private static T GetPrivateStaticMethod<T>(string methodName)
    {
        var method = typeof(AssemblyScanner).GetMethod(methodName, BindingFlags.Static | BindingFlags.NonPublic);
        return (T)(object)method!.CreateDelegate(typeof(T));
    }
}

// Helper classes for testing IsInterceptableType and HasInterceptableMethods logic
public class TestClassWithPublicMethod
{
    public void PublicMethod() { }
    private void PrivateMethod() { }
    public static void StaticMethod() { }
}

public abstract class AbstractTestClass
{
    public abstract void AbstractMethod();
}

internal class InternalClass
{
    public void SomeMethod() { }
}

public class AnotherTestClass
{
    public int GetValue() => 1;
    internal void InternalMethod() { }
}

public class ClassWithoutInterceptableMethods
{
    private void PrivateMethod() { }
    internal void InternalMethod() { }
    public static void StaticPublicMethod() { }
}