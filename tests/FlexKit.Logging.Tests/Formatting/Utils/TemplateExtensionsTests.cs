using FlexKit.Logging.Configuration;
using FlexKit.Logging.Formatting.Models;
using FlexKit.Logging.Formatting.Utils;
using FlexKit.Logging.Models;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Xunit;

namespace FlexKit.Logging.Tests.Formatting.Utils;

/// <summary>
/// Unit tests for TemplateExtensions.GetTemplate method covering different configuration scenarios.
/// </summary>
public class TemplateExtensionsTests
{
    [Fact]
    public void GetTemplate_WhenConfiguredTemplateIsNotNull_ReturnsConfiguredTemplate()
    {
        // Arrange
        var expectedTemplate = "Custom configured template";
        var templateConfig = new TemplateConfig
        {
            Enabled = true,
            SuccessTemplate = expectedTemplate
        };

        var configuration = new LoggingConfig
        {
            Templates = new Dictionary<string, TemplateConfig>
            {
                ["StandardStructured"] = templateConfig
            }
        };

        var logEntry = LogEntry.CreateStart("TestMethod", "TestType");

        var context = FormattingContext.Create(logEntry, configuration)
            .WithFormatterType(FormatterType.StandardStructured);

        // Act
        var result = context.GetTemplate(FormatterType.StandardStructured);

        // Assert
        result.Should().Be(expectedTemplate);
    }

    [Fact]
    public void GetTemplate_WhenConfiguredTemplateIsNullAndEntrySuccessIsTrueWithInputAndOutput_ReturnsFallbackTemplateWithParameters()
    {
        // Arrange
        var configuration = new LoggingConfig
        {
            Templates = new Dictionary<string, TemplateConfig>()
        };

        var logEntry = LogEntry.CreateStart("TestMethod", "TestType")
            .WithCompletion(true, 5000) // Success with duration
            .WithInput("test input parameters")
            .WithOutput("test output value");

        var context = FormattingContext.Create(logEntry, configuration)
            .WithFormatterType(FormatterType.StandardStructured);

        // Act
        var result = context.GetTemplate(FormatterType.StandardStructured);

        // Assert
        result.Should().Be("Method {MethodName} completed in {Duration}ms | Input: {InputParameters} | Output: {OutputValue}");
    }

    [Fact]
    public void GetTemplate_WhenConfiguredTemplateIsNullAndEntrySuccessIsFalseWithoutInputAndOutput_ReturnsFallbackTemplateWithoutParameters()
    {
        // Arrange
        var configuration = new LoggingConfig
        {
            Templates = new Dictionary<string, TemplateConfig>()
        };

        var logEntry = LogEntry.CreateStart("TestMethod", "TestType")
            .WithCompletion(false, 0, new InvalidOperationException("Test error"));

        var context = FormattingContext.Create(logEntry, configuration)
            .WithFormatterType(FormatterType.StandardStructured);

        // Act
        var result = context.GetTemplate(FormatterType.StandardStructured);

        // Assert
        result.Should().Be("Method {MethodName} failed after {Duration}ms");
    }
}
