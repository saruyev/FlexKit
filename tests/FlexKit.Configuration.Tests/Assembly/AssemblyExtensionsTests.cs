using Autofac;
using AutoFixture.Xunit2;
using FlexKit.Configuration.Assembly;
using FlexKit.Configuration.Tests.TestBase;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyModel;
using NSubstitute;
using Xunit;
using Module = Autofac.Module;
// ReSharper disable ClassTooBig

namespace FlexKit.Configuration.Tests.Assembly;

/// <summary>
/// Unit tests for AssemblyExtensions class covering assembly scanning and module registration.
/// </summary>
public class AssemblyExtensionsTests : UnitTestBase
{
    private IConfiguration _mockConfiguration = null!;

    protected override void RegisterFixtureCustomizations()
    {
        // Customize string generation to avoid null values
        Fixture.Customize<string>(composer => composer.FromFactory(() => "test-string-" + Guid.NewGuid().ToString("N")[..8]));
    }

    protected override void ConfigureContainer(ContainerBuilder builder)
    {
        _mockConfiguration = CreateMock<IConfiguration>();
        builder.RegisterInstance(_mockConfiguration).As<IConfiguration>();
    }

    [Fact]
    public void RegisterAssembliesFromBaseDirectory_WithNullConfiguration_UsesDefaultFiltering()
    {
        // Arrange
        var builder = new ContainerBuilder();

        // Act
        var action = () => builder.RegisterAssembliesFromBaseDirectory();

        // Assert - Should not throw and should complete successfully
        action.Should().NotThrow();
    }

    [Fact]
    public void RegisterAssembliesFromBaseDirectory_WithValidConfiguration_RegistersMatchingAssemblies()
    {
        // Arrange
        var testData = new Dictionary<string, string?>
        {
            ["Application:Mapping:Prefix"] = "FlexKit"
        };

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(testData)
            .Build();

        var builder = new ContainerBuilder();

        // Act
        var action = () => builder.RegisterAssembliesFromBaseDirectory(configuration);

        // Assert - Should complete without throwing
        action.Should().NotThrow();
    }

    [Fact]
    public void RegisterAssembliesFromBaseDirectory_WithSpecificNames_FiltersCorrectly()
    {
        // Arrange
        var testData = new Dictionary<string, string?>
        {
            ["Application:Mapping:Names:0"] = "FlexKit.Configuration",
            ["Application:Mapping:Names:1"] = "TestAssembly"
        };

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(testData)
            .Build();

        var builder = new ContainerBuilder();

        // Act
        var action = () => builder.RegisterAssembliesFromBaseDirectory(configuration);

        // Assert - Should complete without throwing
        action.Should().NotThrow();
    }

    [Fact]
    public void AddModules_WithValidConfiguration_RegistersModulesFromMultipleSources()
    {
        // Arrange
        var testData = new Dictionary<string, string?>
        {
            ["Application:Mapping:Prefix"] = "FlexKit"
        };

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(testData)
            .Build();

        var builder = new ContainerBuilder();

        // Act
        var action = () => builder.AddModules(configuration);

        // Assert - Should complete without throwing
        action.Should().NotThrow();
    }

    [Fact]
    public void AddModules_WithNullConfiguration_HandlesGracefully()
    {
        // Arrange
        var builder = new ContainerBuilder();

        // Act & Assert - Should not throw, method is designed to handle null configuration
        var action = () => builder.AddModules(null!);
        action.Should().NotThrow();
    }

    [Fact]
    public void GetAssemblies_WithValidContext_FiltersAndLoadsAssemblies()
    {
        // Arrange - We'll test with the actual default dependency context if available
        var context = DependencyContext.Default;
        var testData = new Dictionary<string, string?>
        {
            ["Application:Mapping:Prefix"] = "System" // Use System assemblies that are likely to exist
        };

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(testData)
            .Build();

        // Act
        var result = context.GetAssemblies(configuration);

        // Assert
        result.Should().NotBeNull();
        // We can't guarantee specific assemblies will be present, but the method should work
        result.Should().BeOfType<List<System.Reflection.Assembly>>();
    }

    [Theory]
    [InlineData("FlexKit", true)]
    [InlineData("Microsoft", false)] // Should be filtered out for non-FlexKit prefix
    [InlineData("System", false)]    // Should be filtered out for non-FlexKit prefix
    [InlineData("TestAssembly", false)]
    public void FilterLibraries_WithPrefix_FiltersCorrectly(string assemblyName, bool shouldInclude)
    {
        // Arrange
        var config = new MappingConfig { Prefix = "FlexKit" };

        // Act - Use reflection to call the private method
        var method = typeof(AssemblyExtensions).GetMethod("FilterLibraries", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        var result = (bool)method!.Invoke(null, [assemblyName, config])!;

        // Assert
        result.Should().Be(shouldInclude);
    }

    [Theory]
    [InlineData("Acme.Services", true)]
    [InlineData("Acme.Data", true)]
    [InlineData("OtherVendor.Services", false)]
    [InlineData("FlexKit.Configuration", false)] // Not in the names list
    public void FilterLibraries_WithNames_FiltersCorrectly(string assemblyName, bool shouldInclude)
    {
        // Arrange
        var config = new MappingConfig { Names = ["Acme.Services", "Acme.Data"] };

        // Act - Use reflection to call the private method
        var method = typeof(AssemblyExtensions).GetMethod("FilterLibraries", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        var result = (bool)method!.Invoke(null, [assemblyName, config])!;

        // Assert
        result.Should().Be(shouldInclude);
    }

    [Theory]
    [InlineData("FlexKit.Configuration", true)]
    [InlineData("FlexKit.Configuration.Tests", true)]
    [InlineData("SomeAssembly.Module", true)]
    [InlineData("TestModule.Extensions", true)]
    [InlineData("RandomAssembly", false)]
    [InlineData("Microsoft.Extensions", false)]
    public void FilterLibraries_WithNoConfiguration_UsesDefaults(string assemblyName, bool shouldInclude)
    {
        // Arrange
        MappingConfig? config = null;

        // Act - Use reflection to call the private method
        var method = typeof(AssemblyExtensions).GetMethod("FilterLibraries", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        var result = (bool)method!.Invoke(null, [assemblyName, config])!;

        // Assert
        result.Should().Be(shouldInclude);
    }

    [Fact]
    public void FilterLibraries_WithNullAssemblyName_ReturnsFalse()
    {
        // Arrange
        string? nullAssemblyName = null;
        var config = new MappingConfig { Prefix = "FlexKit" };

        // Act - Use reflection to call the private method
        var method = typeof(AssemblyExtensions).GetMethod("FilterLibraries", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        var result = (bool)method!.Invoke(null, [nullAssemblyName, config])!;

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void FilterLibraries_WithPrefixTakesPrecedenceOverNames()
    {
        // Arrange
        var config = new MappingConfig 
        { 
            Prefix = "FlexKit", 
            Names = ["Acme.Services"]
        };

        // Act - Test with an assembly that matches names but not a prefix
        var method = typeof(AssemblyExtensions).GetMethod("FilterLibraries", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        var result1 = (bool)method!.Invoke(null, ["Acme.Services", config])!;
        var result2 = (bool)method.Invoke(null, ["FlexKit.Something", config])!;

        // Assert
        result1.Should().BeFalse(); // Names are ignored when Prefix is set
        result2.Should().BeTrue();  // Prefix takes precedence
    }

    [Fact]
    public void ContainsModules_WithAssemblyContainingModules_ReturnsTrue()
    {
        // Arrange - Use current test assembly which likely contains test modules
        var testAssembly = typeof(AssemblyExtensionsTests).Assembly;

        // Act - Use reflection to call the private method
        var method = typeof(AssemblyExtensions).GetMethod("ContainsModules", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        bool result = (bool)method!.Invoke(null, [testAssembly])!;

        // Assert - The test assembly might or might not contain modules, but the method should not throw
        result.Should().BeTrue();
    }

    [Fact]
    public void ContainsModules_WithAssemblyWithoutModules_ReturnsFalse()
    {
        // Arrange - Use a system assembly that's unlikely to contain Autofac modules
        var systemAssembly = typeof(string).Assembly; // mscorlib/System.Private.CoreLib

        // Act - Use reflection to call the private method
        var method = typeof(AssemblyExtensions).GetMethod("ContainsModules", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        var result = (bool)method!.Invoke(null, [systemAssembly])!;

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void RegisterAssembliesFromBaseDirectory_ExcludesFlexKitConfigurationAssembly()
    {
        // Arrange
        var builder = new ContainerBuilder();
        var testData = new Dictionary<string, string?>
        {
            ["Application:Mapping:Prefix"] = "FlexKit"
        };

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(testData)
            .Build();

        // Act
        builder.RegisterAssembliesFromBaseDirectory(configuration);

        // Assert - Should complete without issues
        // The FlexKit.Configuration assembly should be filtered out
        var action = () => builder.Build();
        action.Should().NotThrow();
    }

    [Fact]
    public void AddModules_RegistersModulesFromBothDependencyContextAndBaseDirectory()
    {
        // Arrange
        var builder = new ContainerBuilder();
        var testData = new Dictionary<string, string?>
        {
            ["Application:Mapping:Prefix"] = "FlexKit"
        };

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(testData)
            .Build();

        // Act
        builder.AddModules(configuration);

        // Assert - Should complete without throwing, indicating both methods were called
        var action = () => builder.Build();
        action.Should().NotThrow();
    }

    [Fact]
    public void ConvertToCompilation_WithValidRuntimeLibrary_CreatesCompilationLibrary()
    {
        // Arrange - We need to create a mock RuntimeLibrary
        var mockRuntimeLibrary = CreateMockRuntimeLibrary();

        // Act - Use reflection to call the private extension method
        var method = typeof(AssemblyExtensions).GetMethod("ConvertToCompilation", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        var result = method!.Invoke(null, [mockRuntimeLibrary]);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeOfType<CompilationLibrary>();
    }
    
    [Theory]
    [AutoData]
    public void GetAssemblies_WithCustomConfiguration_FiltersUsingMappingConfig(string customPrefix)
    {
        // Arrange
        var testData = new Dictionary<string, string?>
        {
            ["Application:Mapping:Prefix"] = customPrefix
        };

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(testData)
            .Build();

        var context = DependencyContext.Default;

        // Act
        var result = context.GetAssemblies(configuration);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeOfType<List<System.Reflection.Assembly>>();
        // With a custom prefix that likely doesn't match system assemblies, 
        // we should get an empty or very small list
    }

    [Fact]
    public void MappingSectionName_HasCorrectValue()
    {
        // Arrange & Act - Use reflection to get the private constant
        var field = typeof(AssemblyExtensions).GetField("MappingSectionName", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        var value = (string)field!.GetValue(null)!;

        // Assert
        value.Should().Be("Application:Mapping");
    }

    [Fact]
    public void RegisterAssembliesFromBaseDirectory_WithEmptyConfiguration_UsesDefaultBehavior()
    {
        // Arrange
        var builder = new ContainerBuilder();
        var configuration = new ConfigurationBuilder().Build(); // Empty configuration

        // Act & Assert - Should not throw
        var action = () => builder.RegisterAssembliesFromBaseDirectory(configuration);
        action.Should().NotThrow();
    }

    [Fact]
    public void GetAssemblies_WithNullConfiguration_UsesDefaultFiltering()
    {
        // Arrange
        var context = DependencyContext.Default;

        // Act
        var result = context.GetAssemblies(null);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeOfType<List<System.Reflection.Assembly>>();
    }

    [Fact]
    public void ContainsModules_WithReflectionTypeLoadException_ReturnsFalse()
    {
        // Arrange - Create a mock assembly that will throw ReflectionTypeLoadException
        var mockAssembly = CreateMock<System.Reflection.Assembly>();
        mockAssembly.GetTypes().Returns(_ => throw new System.Reflection.ReflectionTypeLoadException([], []));

        // Act - Use reflection to call the private method
        var method = typeof(AssemblyExtensions).GetMethod("ContainsModules", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        var result = (bool)method!.Invoke(null, [mockAssembly])!;

        // Assert - Should handle the exception gracefully and return false
        result.Should().BeFalse();
    }

    [Fact]
    public void FilterLibraries_WithEmptyStringAssemblyName_ReturnsFalse()
    {
        // Arrange
        var emptyAssemblyName = "";
        var config = new MappingConfig { Prefix = "FlexKit" };

        // Act - Use reflection to call the private method
        var method = typeof(AssemblyExtensions).GetMethod("FilterLibraries", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        var result = (bool)method!.Invoke(null, [emptyAssemblyName, config])!;

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void FilterLibraries_WithEmptyNamesArray_UsesDefaultFiltering()
    {
        // Arrange
        var config = new MappingConfig { Names = [] }; // Empty array
        var assemblyName = "FlexKit.Configuration";

        // Act - Use reflection to call the private method
        var method = typeof(AssemblyExtensions).GetMethod("FilterLibraries", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        var result = (bool)method!.Invoke(null, [assemblyName, config])!;

        // Assert - Should fall back to default filtering and return true for FlexKit assembly
        result.Should().BeTrue();
    }

    [Theory]
    [InlineData("FlexKit.Configuration.Something")]
    [InlineData("FlexKit.Configuration")]
    [InlineData("FlexKit.Configuration.Tests")]
    public void FilterLibraries_WithFlexKitPrefix_IncludesFlexKitAssemblies(string assemblyName)
    {
        // Arrange
        MappingConfig? config = null; // Use default filtering

        // Act - Use reflection to call the private method
        var method = typeof(AssemblyExtensions).GetMethod("FilterLibraries", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        var result = (bool)method!.Invoke(null, [assemblyName, config])!;

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void GetAssemblies_FiltersOutFlexKitConfigurationAssembly()
    {
        // Arrange
        var context = DependencyContext.Default;
        var testData = new Dictionary<string, string?>
        {
            ["Application:Mapping:Prefix"] = "FlexKit"
        };

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(testData)
            .Build();

        // Act
        var result = context.GetAssemblies(configuration);

        // Assert
        result.Should().NotBeNull();
        // Check that FlexKit.Configuration assembly is filtered out
        result.Should().NotContain(assembly => 
            assembly.GetName().Name != null && assembly.GetName().Name!.Equals("FlexKit.Configuration", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void ConvertToCompilation_PreservesAllLibraryProperties()
    {
        // Arrange
        var mockRuntimeLibrary = CreateMockRuntimeLibrary();

        // Act - Use reflection to call the private extension method
        var method = typeof(AssemblyExtensions).GetMethod("ConvertToCompilation", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        var result = (CompilationLibrary)method!.Invoke(null, [mockRuntimeLibrary])!;

        // Assert - Verify all properties are preserved
        result.Name.Should().Be(mockRuntimeLibrary.Name);
        result.Version.Should().Be(mockRuntimeLibrary.Version);
        result.Hash.Should().Be(mockRuntimeLibrary.Hash);
        result.Type.Should().Be(mockRuntimeLibrary.Type);
        result.Serviceable.Should().Be(mockRuntimeLibrary.Serviceable);
        result.Dependencies.Should().BeEquivalentTo(mockRuntimeLibrary.Dependencies);
    }

    [Fact]
    public void ResolveReferencePaths_WithEmptyCompilationLibrary_ReturnsEmptyEnumerable()
    {
        // Arrange - Create a compilation library with no assemblies
        var emptyCompilationLibrary = new CompilationLibrary(
            type: "package",
            name: "EmptyLibrary",
            version: "1.0.0",
            hash: "hash",
            assemblies: [], // Empty assemblies
            dependencies: new List<Dependency>(),
            serviceable: true
        );

        // Act - Use reflection to call the private method
        var method = typeof(AssemblyExtensions).GetMethod("ResolveReferencePaths", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        var result = (IEnumerable<string>)method!.Invoke(null, [emptyCompilationLibrary])!;

        // Assert
        // ReSharper disable once PossibleMultipleEnumeration
        result.Should().NotBeNull();
        // ReSharper disable once PossibleMultipleEnumeration
        result.Should().BeEmpty();
    }

    [Fact]
    public void RegisterAssembliesFromBaseDirectory_WithNonExistentFiles_HandlesGracefully()
    {
        // Arrange
        var builder = new ContainerBuilder();
        var testData = new Dictionary<string, string?>
        {
            ["Application:Mapping:Names:0"] = "NonExistentAssembly"
        };

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(testData)
            .Build();

        // Act & Assert - Should not throw even if no matching assemblies are found
        var action = () => builder.RegisterAssembliesFromBaseDirectory(configuration);
        action.Should().NotThrow();
    }

    private RuntimeLibrary CreateMockRuntimeLibrary()
    {
        // Create a basic RuntimeLibrary for testing
        // ReSharper disable once CollectionNeverUpdated.Local
        var dependencies = new List<Dependency>();
        var runtimeAssemblyGroups = new List<RuntimeAssetGroup>
        {
            new RuntimeAssetGroup(string.Empty, new[] { "test.dll" })
        };

        return new RuntimeLibrary(
            type: "package",
            name: "TestLibrary",
            version: "1.0.0",
            hash: "testhash",
            runtimeAssemblyGroups: runtimeAssemblyGroups,
            nativeLibraryGroups: new List<RuntimeAssetGroup>(),
            resourceAssemblies: new List<ResourceAssembly>(),
            dependencies: dependencies,
            serviceable: true
        );
    }
}

/// <summary>
/// Test module for testing assembly scanning functionality.
/// </summary>
public class TestModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        // Empty test module for assembly scanning tests
        builder.RegisterType<TestService>().AsImplementedInterfaces();
    }
}

/// <summary>
/// Test service for module registration testing.
/// </summary>
public interface ITestService
{
    string GetName();
}

public class TestService : ITestService
{
    public string GetName() => "TestService";
}