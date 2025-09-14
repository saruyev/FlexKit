using System.Diagnostics;
using System.Reflection;
using System.Reflection.Emit;
using FlexKit.Logging.Detection;
using FluentAssertions;
using Xunit;
// ReSharper disable ComplexConditionExpression
// ReSharper disable PossibleMultipleEnumeration

namespace FlexKit.Logging.Tests.Detection;

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
                .Any(m => m.IsPublic && !m.IsStatic && !m.IsConstructor && !m.IsSpecialName &&
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
}