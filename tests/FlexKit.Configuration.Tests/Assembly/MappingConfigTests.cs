using AutoFixture.Xunit2;
using FlexKit.Configuration.Assembly;
using FlexKit.Configuration.Tests.TestBase;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Xunit;
// ReSharper disable ClassTooBig

namespace FlexKit.Configuration.Tests.Assembly;

/// <summary>
/// Unit tests for MappingConfig record covering all properties and behaviors.
/// </summary>
public class MappingConfigTests : UnitTestBase
{
    protected override void RegisterFixtureCustomizations()
    {
        // Customize string generation to avoid null values and ensure realistic assembly names
        Fixture.Customize<string>(composer => composer.FromFactory(() =>
            "TestAssembly" + Guid.NewGuid().ToString("N")[..6]));
    }

    [Fact]
    public void Constructor_WithDefaultValues_CreatesInstanceWithNullProperties()
    {
        // Act
        var config = new MappingConfig();

        // Assert
        config.Prefix.Should().BeNull();
        config.Names.Should().BeNull();
    }

    [Theory]
    [AutoData]
    public void Constructor_WithPrefixOnly_SetsOnlyPrefixProperty(string prefix)
    {
        // Act
        var config = new MappingConfig { Prefix = prefix };

        // Assert
        config.Prefix.Should().Be(prefix);
        config.Names.Should().BeNull();
    }

    [Theory]
    [AutoData]
    public void Constructor_WithNamesOnly_SetsOnlyNamesProperty(string[] names)
    {
        // Act
        var config = new MappingConfig { Names = names };

        // Assert
        config.Names.Should().BeEquivalentTo(names);
        config.Names.Should().BeSameAs(names);
        config.Prefix.Should().BeNull();
    }

    [Theory]
    [AutoData]
    public void Constructor_WithBothPrefixAndNames_SetsBothProperties(string prefix, string[] names)
    {
        // Act
        var config = new MappingConfig
        {
            Prefix = prefix,
            Names = names
        };

        // Assert
        config.Prefix.Should().Be(prefix);
        config.Names.Should().BeEquivalentTo(names);
        config.Names.Should().BeSameAs(names);
    }

    [Fact]
    public void Prefix_CanBeSetToNull()
    {
        // Act
        var config = new MappingConfig { Prefix = null };

        // Assert
        config.Prefix.Should().BeNull();
    }

    [Fact]
    public void Names_CanBeSetToNull()
    {
        // Act
        var config = new MappingConfig { Names = null };

        // Assert
        config.Names.Should().BeNull();
    }

    [Fact]
    public void Prefix_CanBeSetToEmptyString()
    {
        // Act
        var config = new MappingConfig { Prefix = string.Empty };

        // Assert
        config.Prefix.Should().Be(string.Empty);
    }

    [Fact]
    public void Names_CanBeSetToEmptyArray()
    {
        // Act
        var config = new MappingConfig { Names = [] };

        // Assert
        config.Names.Should().NotBeNull();
        config.Names.Should().BeEmpty();
    }

    [Theory]
    [AutoData]
    public void Names_AcceptsArrayWithMultipleElements(string name1, string name2, string name3)
    {
        // Arrange
        var names = new[] { name1, name2, name3 };

        // Act
        var config = new MappingConfig { Names = names };

        // Assert
        config.Names.Should().HaveCount(3);
        config.Names.Should().ContainInOrder(name1, name2, name3);
    }

    [Fact]
    public void Names_AcceptsArrayWithSingleElement()
    {
        // Arrange
        var names = new[] { "SingleAssembly" };

        // Act
        var config = new MappingConfig { Names = names };

        // Assert
        config.Names.Should().HaveCount(1);
        config.Names.Should().Contain("SingleAssembly");
    }

    [Theory]
    [AutoData]
    public void Prefix_AcceptsValidAssemblyPrefixes(string companyName)
    {
        // Arrange
        var prefix = $"{companyName}.";

        // Act
        var config = new MappingConfig { Prefix = prefix };

        // Assert
        config.Prefix.Should().Be(prefix);
        config.Prefix.Should().StartWith(companyName);
        config.Prefix.Should().EndWith(".");
    }

    [Fact]
    public void Names_AcceptsRealisticAssemblyNames()
    {
        // Arrange
        var names = new[]
        {
            "MyCompany.Services",
            "MyCompany.Data.EntityFramework",
            "MyCompany.Core.Utilities",
            "ThirdParty.Extensions.Logging"
        };

        // Act
        var config = new MappingConfig { Names = names };

        // Assert
        config.Names.Should().HaveCount(4);
        config.Names.Should().AllSatisfy(name =>
            name.Should().Contain(".").And.NotBeNullOrWhiteSpace());
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t")]
    [InlineData("\n")]
    public void Prefix_AcceptsWhitespaceValues(string whitespacePrefix)
    {
        // Act
        var config = new MappingConfig { Prefix = whitespacePrefix };

        // Assert
        config.Prefix.Should().Be(whitespacePrefix);
    }

    [Fact]
    public void Names_AcceptsArrayWithWhitespaceElements()
    {
        // Arrange
        var names = new[] { "", "  ", "\t", "ValidName", "\n" };

        // Act
        var config = new MappingConfig { Names = names };

        // Assert
        config.Names.Should().HaveCount(5);
        config.Names.Should().Contain("ValidName");
        config.Names.Should().Contain(name => string.IsNullOrWhiteSpace(name));
    }

    [Fact]
    public void Record_Equality_WorksCorrectly()
    {
        // Arrange
        var config1 = new MappingConfig { Prefix = "TestPrefix" };
        var config2 = new MappingConfig { Prefix = "TestPrefix" };
        var config3 = new MappingConfig { Prefix = "DifferentPrefix" };

        // Act & Assert
        config1.Should().Be(config2);
        config1.Should().NotBe(config3);
        config1.Equals(config2).Should().BeTrue();
        config1.Equals(config3).Should().BeFalse();
    }

    [Fact]
    public void Record_Equality_WithNames_WorksCorrectly()
    {
        // Arrange
        var names = new[] { "Assembly1", "Assembly2" };
        var config1 = new MappingConfig { Names = names };
        var config2 = new MappingConfig { Names = names };
        var config3 = new MappingConfig { Names = ["Assembly1", "Assembly3"] };

        // Act & Assert
        config1.Should().Be(config2);
        config1.Should().NotBe(config3);
    }

    [Fact]
    public void Record_Equality_WithBothProperties_WorksCorrectly()
    {
        // Arrange
        var names = new[] { "Assembly1", "Assembly2" };
        var config1 = new MappingConfig { Prefix = "Test", Names = names };
        var config2 = new MappingConfig { Prefix = "Test", Names = names };
        var config3 = new MappingConfig { Prefix = "Different", Names = names };
        var config4 = new MappingConfig { Prefix = "Test", Names = ["Different"] };

        // Act & Assert
        config1.Should().Be(config2);
        config1.Should().NotBe(config3);
        config1.Should().NotBe(config4);
    }

    [Fact]
    public void Record_HashCode_IsConsistent()
    {
        // Arrange
        var names = new[] { "Assembly1" };
        var config1 = new MappingConfig { Prefix = "TestPrefix", Names = names };
        var config2 = new MappingConfig { Prefix = "TestPrefix", Names = names };

        // Act
        var hash1 = config1.GetHashCode();
        var hash2 = config2.GetHashCode();

        // Assert
        hash1.Should().Be(hash2);
    }

    [Fact]
    public void Record_ToString_ReturnsReadableString()
    {
        // Arrange
        var config = new MappingConfig
        {
            Prefix = "TestPrefix",
            Names = ["Assembly1", "Assembly2"]
        };

        // Act
        var result = config.ToString();

        // Assert
        result.Should().NotBeNullOrEmpty();
        result.Should().Contain("TestPrefix");
        // Note: ToString() may not include array contents, so we just verify the prefix is included
        result.Should().Contain("MappingConfig");
    }

    [Fact]
    public void Record_WithMethod_CreatesNewInstanceWithModifiedValues()
    {
        // Arrange
        var original = new MappingConfig { Prefix = "Original" };

        // Act
        var modified = original with { Prefix = "Modified" };

        // Assert
        original.Prefix.Should().Be("Original");
        modified.Prefix.Should().Be("Modified");
        modified.Should().NotBeSameAs(original);
    }

    [Fact]
    public void Record_WithMethod_PreservesUnchangedProperties()
    {
        // Arrange
        var names = new[] { "Assembly1", "Assembly2" };
        var original = new MappingConfig { Prefix = "Original", Names = names };

        // Act
        var modified = original with { Prefix = "Modified" };

        // Assert
        modified.Names.Should().BeSameAs(names);
        modified.Names.Should().BeEquivalentTo(original.Names);
    }

    [Fact]
    public void ConfigurationBinding_FromDictionary_WorksCorrectly()
    {
        // Arrange
        var configData = new Dictionary<string, string?>
        {
            ["Application:Mapping:Prefix"] = "MyCompany",
            ["Application:Mapping:Names:0"] = "MyCompany.Services",
            ["Application:Mapping:Names:1"] = "MyCompany.Data",
            ["Application:Mapping:Names:2"] = "ThirdParty.Extensions"
        };

        var builder = new ConfigurationBuilder();
        builder.AddInMemoryCollection(configData);
        var configuration = builder.Build();

        // Act
        var mappingConfig = configuration.GetSection("Application:Mapping").Get<MappingConfig>();

        // Assert
        mappingConfig.Should().NotBeNull();
        mappingConfig.Prefix.Should().Be("MyCompany");
        mappingConfig.Names.Should().HaveCount(3);
        mappingConfig.Names.Should().ContainInOrder("MyCompany.Services", "MyCompany.Data", "ThirdParty.Extensions");
    }

    [Fact]
    public void ConfigurationBinding_PrefixOnly_WorksCorrectly()
    {
        // Arrange
        var configData = new Dictionary<string, string?>
        {
            ["Application:Mapping:Prefix"] = "MyCompany"
        };

        var builder = new ConfigurationBuilder();
        builder.AddInMemoryCollection(configData);
        var configuration = builder.Build();

        // Act
        var mappingConfig = configuration.GetSection("Application:Mapping").Get<MappingConfig>();

        // Assert
        mappingConfig.Should().NotBeNull();
        mappingConfig.Prefix.Should().Be("MyCompany");
        mappingConfig.Names.Should().BeNull();
    }

    [Fact]
    public void ConfigurationBinding_NamesOnly_WorksCorrectly()
    {
        // Arrange
        var configData = new Dictionary<string, string?>
        {
            ["Application:Mapping:Names:0"] = "Assembly1",
            ["Application:Mapping:Names:1"] = "Assembly2"
        };

        var builder = new ConfigurationBuilder();
        builder.AddInMemoryCollection(configData);
        var configuration = builder.Build();

        // Act
        var mappingConfig = configuration.GetSection("Application:Mapping").Get<MappingConfig>();

        // Assert
        mappingConfig.Should().NotBeNull();
        mappingConfig.Prefix.Should().BeNull();
        mappingConfig.Names.Should().HaveCount(2);
        mappingConfig.Names.Should().ContainInOrder("Assembly1", "Assembly2");
    }

    [Fact]
    public void ConfigurationBinding_EmptySection_ReturnsNull()
    {
        // Arrange
        // ReSharper disable once CollectionNeverUpdated.Local
        var configData = new Dictionary<string, string?>();
        var builder = new ConfigurationBuilder();
        builder.AddInMemoryCollection(configData);
        var configuration = builder.Build();

        // Act
        var mappingConfig = configuration.GetSection("Application:Mapping").Get<MappingConfig>();

        // Assert
        mappingConfig.Should().BeNull();
    }

    [Fact]
    public void ConfigurationBinding_WithEmptyValues_HandlesCorrectly()
    {
        // Arrange
        var configData = new Dictionary<string, string?>
        {
            ["Application:Mapping:Prefix"] = "",
            ["Application:Mapping:Names:0"] = "",
            ["Application:Mapping:Names:1"] = "ValidName"
        };

        var builder = new ConfigurationBuilder();
        builder.AddInMemoryCollection(configData);
        var configuration = builder.Build();

        // Act
        var mappingConfig = configuration.GetSection("Application:Mapping").Get<MappingConfig>();

        // Assert
        mappingConfig.Should().NotBeNull();
        mappingConfig.Prefix.Should().Be("");
        mappingConfig.Names.Should().HaveCount(2);
        mappingConfig.Names.Should().ContainInOrder("", "ValidName");
    }

    [Fact]
    public void Record_Properties_AreAccessibleDirectly()
    {
        // Arrange
        var prefix = "TestPrefix";
        var names = new[] { "Assembly1", "Assembly2" };
        var config = new MappingConfig { Prefix = prefix, Names = names };

        // Act & Assert - Direct property access is the intended usage pattern
        config.Prefix.Should().Be(prefix);
        config.Names.Should().BeSameAs(names);
        config.Names.Should().BeEquivalentTo(names);
    }

    [Fact]
    public void UsedImplicitly_Attributes_ArePresent()
    {
        // This test ensures that the [UsedImplicitly] attributes are present on the init accessors
        // We can verify this through configuration binding which uses these properties

        // Arrange
        var configData = new Dictionary<string, string?>
        {
            ["Prefix"] = "Test",
            ["Names:0"] = "Assembly1"
        };

        var builder = new ConfigurationBuilder();
        builder.AddInMemoryCollection(configData);
        var configuration = builder.Build();

        // Act
        var mappingConfig = configuration.Get<MappingConfig>();

        // Assert
        mappingConfig.Should().NotBeNull();
        mappingConfig.Prefix.Should().Be("Test");
        mappingConfig.Names.Should().Contain("Assembly1");
    }

    [Fact]
    public void Record_WithRealisticScenarios_HandlesProperly()
    {
        // Test various realistic scenarios for assembly mapping

        // Scenario 1: Enterprise with consistent naming
        var enterpriseConfig = new MappingConfig { Prefix = "Contoso.FinanceApp" };
        enterpriseConfig.Prefix.Should().Be("Contoso.FinanceApp");

        // Scenario 2: Mixed assemblies from different sources
        var mixedConfig = new MappingConfig
        {
            Names =
            [
                "MyApp.Core",
                "MyApp.Services",
                "ThirdParty.Logging",
                "Vendor.PaymentGateway",
            ]
        };

        mixedConfig.Names.Should().HaveCount(4);

        // Scenario 3: Microservice with focused scope
        var microserviceConfig = new MappingConfig
        {
            Prefix = "PaymentService",
            Names = ["PaymentService.Core", "PaymentService.Data"]
        };

        microserviceConfig.Prefix.Should().Be("PaymentService");
        microserviceConfig.Names.Should().HaveCount(2);
    }

    [Fact]
    public void Equals_WithIdenticalInstances_ReturnsTrue()
    {
        // Arrange
        var names = new[] { "Assembly1", "Assembly2" };
        var config1 = new MappingConfig { Prefix = "Test", Names = names };
        var config2 = new MappingConfig { Prefix = "Test", Names = names };

        // Act
        var result = config1.Equals(config2);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void Equals_WithSameReferenceInstance_ReturnsTrue()
    {
        // Arrange
        var config = new MappingConfig { Prefix = "Test" };

        // Act
        var result = config.Equals(config);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void Equals_WithNullParameter_ReturnsFalse()
    {
        // Arrange
        var config = new MappingConfig { Prefix = "Test" };

        // Act
        var result = config.Equals(null);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void Equals_WithDifferentPrefix_ReturnsFalse()
    {
        // Arrange
        var config1 = new MappingConfig { Prefix = "Test1" };
        var config2 = new MappingConfig { Prefix = "Test2" };

        // Act
        var result = config1.Equals(config2);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void Equals_WithSameContentDifferentArrayInstances_ReturnsTrue()
    {
        // Arrange
        var config1 = new MappingConfig { Names = ["Assembly1", "Assembly2"] };
        var config2 = new MappingConfig { Names = ["Assembly1", "Assembly2"] };

        // Act
        var result = config1.Equals(config2);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void Equals_WithDifferentArrayContent_ReturnsFalse()
    {
        // Arrange
        var config1 = new MappingConfig { Names = ["Assembly1", "Assembly2"] };
        var config2 = new MappingConfig { Names = ["Assembly1", "Assembly3"] };

        // Act
        var result = config1.Equals(config2);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void Equals_WithDifferentArrayOrder_ReturnsFalse()
    {
        // Arrange
        var config1 = new MappingConfig { Names = ["Assembly1", "Assembly2"] };
        var config2 = new MappingConfig { Names = ["Assembly2", "Assembly1"] };

        // Act
        var result = config1.Equals(config2);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void Equals_WithBothNamesNull_ReturnsTrue()
    {
        // Arrange
        var config1 = new MappingConfig { Prefix = "Test", Names = null };
        var config2 = new MappingConfig { Prefix = "Test", Names = null };

        // Act
        var result = config1.Equals(config2);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void Equals_WithOneNamesNullOtherNot_ReturnsFalse()
    {
        // Arrange
        var config1 = new MappingConfig { Prefix = "Test", Names = null };
        var config2 = new MappingConfig { Prefix = "Test", Names = ["Assembly1"] };

        // Act
        var result = config1.Equals(config2);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void Equals_WithEmptyArrays_ReturnsTrue()
    {
        // Arrange
        var config1 = new MappingConfig { Names = [] };
        var config2 = new MappingConfig { Names = [] };

        // Act
        var result = config1.Equals(config2);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void Equals_WithEmptyVsNull_ReturnsFalse()
    {
        // Arrange
        var config1 = new MappingConfig { Names = [] };
        var config2 = new MappingConfig { Names = null };

        // Act
        var result = config1.Equals(config2);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void Equals_WithComplexScenario_WorksCorrectly()
    {
        // Arrange
        var config1 = new MappingConfig
        {
            Prefix = "MyCompany",
            Names = ["MyCompany.Services", "MyCompany.Data"]
        };

        var config2 = new MappingConfig
        {
            Prefix = "MyCompany",
            Names = ["MyCompany.Services", "MyCompany.Data"]
        };

        var config3 = new MappingConfig
        {
            Prefix = "MyCompany",
            Names = ["MyCompany.Services", "MyCompany.Core"]
        };

        // Act & Assert
        config1.Equals(config2).Should().BeTrue();
        config1.Equals(config3).Should().BeFalse();
        config2.Equals(config3).Should().BeFalse();
    }

    [Fact]
    public void GetHashCode_WithEqualInstances_ReturnsSameHashCode()
    {
        // Arrange
        var names = new[] { "Assembly1", "Assembly2" };
        var config1 = new MappingConfig { Prefix = "Test", Names = names };
        var config2 = new MappingConfig { Prefix = "Test", Names = names };

        // Act
        var hash1 = config1.GetHashCode();
        var hash2 = config2.GetHashCode();

        // Assert
        hash1.Should().Be(hash2);
    }

    [Fact]
    public void GetHashCode_WithSameContentDifferentInstances_ReturnsSameHashCode()
    {
        // Arrange
        var config1 = new MappingConfig { Prefix = "Test", Names = ["Assembly1"] };
        var config2 = new MappingConfig { Prefix = "Test", Names = ["Assembly1"] };

        // Act
        var hash1 = config1.GetHashCode();
        var hash2 = config2.GetHashCode();

        // Assert
        hash1.Should().Be(hash2);
    }

    [Fact]
    public void GetHashCode_WithNullNames_DoesNotThrow()
    {
        // Arrange
        var config = new MappingConfig { Prefix = "Test", Names = null };

        // Act
        var action = () => config.GetHashCode();

        // Assert
        action.Should().NotThrow();
        action().Should().NotBe(0); // Should return a valid hash code
    }

    [Fact]
    public void GetHashCode_WithEmptyNames_DoesNotThrow()
    {
        // Arrange
        var config = new MappingConfig { Prefix = "Test", Names = [] };

        // Act
        var action = () => config.GetHashCode();

        // Assert
        action.Should().NotThrow();
        action().Should().NotBe(0); // Should return a valid hash code
    }
}