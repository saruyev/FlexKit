using FlexKit.Logging.Configuration;
using FlexKit.Logging.Formatting.Translation;
using FluentAssertions;
using System.Text.RegularExpressions;
using Xunit;

namespace FlexKit.Logging.Tests.Formatting.Translation;

public class DefaultMessageTranslatorTests
{
    [Fact]
    public void TranslateTemplate_WithSerilogFeatures_CleansProperlyRetainingBasicFormat()
    {
        // Arrange
        var translator = new DefaultMessageTranslator();
        var template = "Method {@MethodName:l} completed in {Duration:N2}ms with {$UserId}";

        // Act
        var result = translator.TranslateTemplate(template);

        // Assert
        result.Should().Be("Method {MethodName} completed in {Duration}ms with {UserId}");
    }

    [Fact]
    public void TranslateTemplate_WithNLogFeatures_CleansProperlyRetainingBasicFormat()
    {
        // Arrange
        var translator = new DefaultMessageTranslator();
        var template = "Method ${MethodName} completed in ${Duration}ms with ${var:UserId} ${when:condition}";
    
        // Act
        var result = translator.TranslateTemplate(template);
    
        // Assert
        result.Should().Be("Method {MethodName} completed in {Duration}ms with {UserId} ");
    }

    [Fact]
    public void TranslateParameters_WithParameters_ReturnsUntouchedParameters()
    {
        // Arrange
        var translator = new DefaultMessageTranslator();
        var parameters = new Dictionary<string, object?>
        {
            ["MethodName"] = "TestMethod",
            ["Duration"] = 450,
            ["UserId"] = 123
        };
        var template = "Method {MethodName} completed in {Duration}ms";

        // Act
        var result = translator.TranslateParameters(parameters, template);

        // Assert
        result.Should().BeEquivalentTo(parameters);
        result.Should().BeSameAs(parameters); // Should return the exact same reference
    }
        
    [Fact]
    public void TestOrderForTemplate_WhenNoMatchesFoundAndParametersEmpty_ReturnsEmptyMetadata()
    {
        // Arrange
        var translator = new TestableDefaultMessageTranslator();
        var template = "Simple message with no placeholders";

        // Act
        var result = translator.TestOrderForTemplate( new Dictionary<string, object?>(), template);

        // Assert
        result.Should().ContainSingle();
        result.Should().ContainKey("Metadata");
        result["Metadata"].Should().BeOfType<Dictionary<string, object?>>()
            .Which.Should().BeEmpty();
    }

    [Fact]
    public void TestOrderForTemplate_WhenMatchesFoundButNoCompatibleKeys_ReturnsAllParametersInMetadata()
    {
        // Arrange
        var translator = new TestableDefaultMessageTranslator();
        var parameters = new Dictionary<string, object?>
        {
            ["UserId"] = 123,
            ["CustomerName"] = "John Doe"
        };
        var template = "Method {MethodName} completed in {Duration}ms"; // No matching keys

        // Act
        var result = translator.TestOrderForTemplate(parameters, template);

        // Assert
        result.Should().ContainSingle();
        result.Should().ContainKey("Metadata");
        var metadata = result["Metadata"].Should().BeOfType<Dictionary<string, object?>>().Subject;
        metadata.Should().Contain("UserId", 123);
        metadata.Should().Contain("CustomerName", "John Doe");
    }

    [Fact]
    public void TestOrderForTemplate_WhenMatchesFoundAndCompatibleKeys_ReturnsOrderedParametersAndMetadata()
    {
        // Arrange
        var translator = new TestableDefaultMessageTranslator();
        var parameters = new Dictionary<string, object?>
        {
            ["Duration"] = 450,
            ["UserId"] = 123,
            ["MethodName"] = "ProcessPayment",
            ["ExtraData"] = "NotInTemplate"
        };
        var template = "Method {MethodName} completed in {Duration}ms";

        // Act
        var result = translator.TestOrderForTemplate(parameters, template);

        // Assert
        result.Should().HaveCount(3); // MethodName, Duration, Metadata
        result.Should().ContainKey("MethodName").WhoseValue.Should().Be("ProcessPayment");
        result.Should().ContainKey("Duration").WhoseValue.Should().Be(450);
        result.Should().ContainKey("Metadata");
            
        var metadata = result["Metadata"].Should().BeOfType<Dictionary<string, object?>>().Subject;
        metadata.Should().Contain("UserId", 123);
        metadata.Should().Contain("ExtraData", "NotInTemplate");
        metadata.Should().HaveCount(2);
    }
}

public class TestableDefaultMessageTranslator : DefaultMessageTranslator
{
    private static readonly Regex TestParameterRegex = new(@"\{([^}]+)\}", RegexOptions.Compiled);

    public Dictionary<string, object?> TestOrderForTemplate(
        IReadOnlyDictionary<string, object?> parameters,
        string currentTemplate)
    {
        return OrderForTemplate(parameters, currentTemplate, TestParameterRegex);
    }
}