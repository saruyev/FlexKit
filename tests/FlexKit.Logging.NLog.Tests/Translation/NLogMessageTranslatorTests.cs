using FlexKit.Logging.Configuration;
using FlexKit.Logging.NLog.Translation;
using FluentAssertions;
using Xunit;

namespace FlexKit.Logging.NLog.Tests.Translation;

/// <summary>
/// Unit tests for NLogMessageTranslator class.
/// </summary>
public class NLogMessageTranslatorTests
{
    private readonly NLogMessageTranslator _translator = new();

    [Fact]
    public void TranslateTemplate_WhenMessageTemplateIsNull_ReturnsEmptyString()
    {
        // Arrange
        string? messageTemplate = null;

        // Act
        var result = _translator.TranslateTemplate(messageTemplate);

        // Assert
        result.Should().Be(string.Empty);
    }

    [Fact]
    public void TranslateTemplate_WhenConfigIsJsonFormatterWithPrettyPrintTrue_ReturnsOriginalTemplate()
    {
        // Arrange
        var messageTemplate = "Method {MethodName} completed in {Duration}ms";
        var config = new LoggingConfig
        {
            DefaultFormatter = FormatterType.Json,
            Formatters = new FormatterSettings
            {
                Json = new JsonFormatterSettings
                {
                    PrettyPrint = true
                }
            }
        };

        // Act
        var result = _translator.TranslateTemplate(messageTemplate, config);

        // Assert
        result.Should().Be("Method {MethodName} completed in {Duration}ms");
    }

    [Fact]
    public void TranslateTemplate_WhenConfigIsJsonFormatterWithPrettyPrintFalse_ConvertsToNLogSyntax()
    {
        // Arrange
        var messageTemplate = "Method {MethodName} completed in {Duration}ms";
        var config = new LoggingConfig
        {
            DefaultFormatter = FormatterType.Json,
            Formatters = new FormatterSettings
            {
                Json = new JsonFormatterSettings
                {
                    PrettyPrint = false
                }
            }
        };

        // Act
        var result = _translator.TranslateTemplate(messageTemplate, config);

        // Assert
        result.Should().Be("Method {@MethodName} completed in {@Duration}ms");
    }

    [Fact]
    public void TranslateParameters_WhenParametersIsNullOrTemplateIsEmpty_ReturnsOriginalParametersOrEmptyDictionary()
    {
        // Arrange - Test null parameters
        IReadOnlyDictionary<string, object?>? nullParameters = null;
        var template = "Method {MethodName} completed in {Duration}ms";

        // Act
        var result1 = _translator.TranslateParameters(nullParameters, template);

        // Assert
        result1.Should().NotBeNull();
        result1.Should().BeEmpty();

        // Arrange - Test empty template
        var parameters = new Dictionary<string, object?>
        {
            ["MethodName"] = "TestMethod",
            ["Duration"] = 150
        };

        var emptyTemplate = "";

        // Act
        var result2 = _translator.TranslateParameters(parameters, emptyTemplate);

        // Assert
        result2.Should().BeSameAs(parameters);

        // Arrange - Test null template
        string? nullTemplate = null;

        // Act
        var result3 = _translator.TranslateParameters(parameters, nullTemplate!);

        // Assert
        result3.Should().BeSameAs(parameters);
    }

    [Fact]
    public void TranslateParameters_WhenParametersAndTemplateExist_OrdersParametersAccordingToTemplate()
    {
        // Arrange
        var parameters = new Dictionary<string, object?>
        {
            ["ZParameter"] = "ShouldBeLast", // Not in template, goes to Metadata
            ["Duration"] = 150, // Third in template
            ["MethodName"] = "TestMethod", // First in template  
            ["Success"] = true, // Second in template
            ["ExtraParam"] = "NotInTemplate" // Not in template, goes to Metadata
        };

        var template = "Method {MethodName} success: {Success} took {Duration}ms";

        // Act
        var result = _translator.TranslateParameters(parameters, template);

        // Assert - Verify ordering matches template order
        result.Should().NotBeNull();
        result.Should().HaveCount(4); // 3 template params + 1 Metadata entry

        var resultDict = result.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        var keys = resultDict.Keys.ToArray();

        // Verify parameters appear in template order
        keys[0].Should().Be("MethodName"); // First parameter in template
        keys[1].Should().Be("Success"); // Second parameter in template  
        keys[2].Should().Be("Duration"); // Third parameter in template
        keys[3].Should().Be("Metadata"); // Unmatched parameters go to Metadata

        // Verify values are correct
        result["MethodName"].Should().Be("TestMethod");
        result["Success"].Should().Be(true);
        result["Duration"].Should().Be(150);

        // Verify Metadata contains unmatched parameters
        var metadata = result["Metadata"] as Dictionary<string, object?>;
        metadata.Should().NotBeNull();
        metadata.Should().HaveCount(2);
        metadata["ZParameter"].Should().Be("ShouldBeLast");
        metadata["ExtraParam"].Should().Be("NotInTemplate");
    }
}