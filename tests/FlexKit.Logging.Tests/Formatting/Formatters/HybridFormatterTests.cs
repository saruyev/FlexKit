using FlexKit.Logging.Configuration;
using FlexKit.Logging.Formatting.Formatters;
using FlexKit.Logging.Formatting.Models;
using FlexKit.Logging.Formatting.Translation;
using FlexKit.Logging.Models;
using FluentAssertions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;
// ReSharper disable TooManyDeclarations

namespace FlexKit.Logging.Tests.Formatting.Formatters;

public class HybridFormatterTests
{
    [Fact]
    public void Format_WhenMessageResultIsSuccessFalse_ReturnsFailureMessage()
    {
        // Arrange
        var translator = Substitute.For<IMessageTranslator>();
        translator.TranslateTemplate(Arg.Any<string>()).Throws(new InvalidOperationException("Template translation failed"));
    
        var formatter = new HybridFormatter(translator);
    
        var config = new LoggingConfig
        {
            Formatters =
            {
                Hybrid = new HybridFormatterSettings(),
            },
        };

        var logEntry = LogEntry.CreateStart("TestMethod", "TestClass");
        var context = FormattingContext.Create(logEntry, config);

        // Act
        var result = formatter.Format(context);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Be("Custom template formatting failed: Template translation failed");
    }

    [Fact]
    public void Format_WhenDisableFormattingFalse_ReturnsSerializedMessageWithMetadata()
    {
        // Arrange
        var translator = Substitute.For<IMessageTranslator>();
        translator.TranslateTemplate(Arg.Any<string>()).Returns(callInfo => callInfo.Arg<string>());
        translator.TranslateParameters(Arg.Any<IReadOnlyDictionary<string, object?>>(), Arg.Any<string>())
            .Returns(callInfo => callInfo.Arg<IReadOnlyDictionary<string, object?>>());
            
        var formatter = new HybridFormatter(translator);
            
        var hybridSettings = new HybridFormatterSettings
        {
            IncludeMetadata = true,
            MetadataSeparator = " | "
        };
            
        var config = new LoggingConfig
        {
            Formatters =
            {
                Hybrid = hybridSettings,
            },
        };

        var logEntry = LogEntry.CreateStart("TestMethod", "TestClass")
            .WithInput(new { UserId = 123 });
        var context = FormattingContext.Create(logEntry, config);

        // Act
        var result = formatter.Format(context);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Message.Should().Contain(" | ");
        result.Message.Should().Contain("TestMethod");
        result.Message.Should().Contain("{"); // JSON metadata part
    }

    [Fact]
    public void Format_WhenDisableFormattingTrue_ReturnsRawMessageWithTemplateAndParameters()
    {
        // Arrange
        var translator = Substitute.For<IMessageTranslator>();
        translator.TranslateTemplate(Arg.Any<string>()).Returns(callInfo => callInfo.Arg<string>());
        translator.TranslateParameters(Arg.Any<IReadOnlyDictionary<string, object?>>(), Arg.Any<string>())
            .Returns(callInfo => callInfo.Arg<IReadOnlyDictionary<string, object?>>());
            
        var formatter = new HybridFormatter(translator);
            
        var hybridSettings = new HybridFormatterSettings
        {
            IncludeMetadata = true,
            MetadataSeparator = " | "
        };
            
        var config = new LoggingConfig
        {
            Formatters =
            {
                Hybrid = hybridSettings,
            },
        };

        var logEntry = LogEntry.CreateStart("TestMethod", "TestClass")
            .WithInput(new { UserId = 123 });
        var context = FormattingContext.Create(logEntry, config)
            .WithoutFormatting();

        // Act
        var result = formatter.Format(context);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Template.Should().NotBeEmpty();
        result.Parameters.Should().ContainKey("MethodName");
        result.Parameters["MethodName"].Should().Be("TestMethod");
        result.Parameters.Should().ContainKey("InputParameters");
        result.Message.Should().BeNull();
        result.Template.Should().Contain("|  {Metadata}");
    }
    
    [Fact]
    public void Format_WhenJsonSerializationFails_ReturnsFormattedMessageFailure()
    {
        // Arrange
        var translator = Substitute.For<IMessageTranslator>();
        translator.TranslateTemplate(Arg.Any<string>()).Returns("Valid template");
        translator.TranslateParameters(Arg.Any<IReadOnlyDictionary<string, object?>>(), Arg.Any<string>())
            .Returns(_ => 
            {
                var circularRef = new Dictionary<string, object?>();
                circularRef["Self"] = circularRef; // Create a circular reference that will cause JsonSerializer to fail
                return circularRef;
            });
    
        var formatter = new HybridFormatter(translator);
    
        var hybridSettings = new HybridFormatterSettings
        {
            IncludeMetadata = true
        };
    
        var config = new LoggingConfig
        {
            Formatters =
            {
                Hybrid = hybridSettings,
            },
        };

        var logEntry = LogEntry.CreateStart("TestMethod", "TestClass");
        var context = FormattingContext.Create(logEntry, config);

        // Act
        var result = formatter.Format(context);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().StartWith("Hybrid formatting failed:");
    }
    
    [Fact]
    public void Format_WhenMessageTemplateIsEmpty_UsesStandardStructuredFormatter()
    {
        // Arrange
        var translator = Substitute.For<IMessageTranslator>();
        translator.TranslateTemplate(Arg.Any<string>()).Returns("Standard template");
        translator.TranslateParameters(Arg.Any<IReadOnlyDictionary<string, object?>>(), Arg.Any<string>())
            .Returns(new Dictionary<string, object?> { ["MethodName"] = "TestMethod" });
    
        var formatter = new HybridFormatter(translator);
    
        var hybridSettings = new HybridFormatterSettings
        {
            MessageTemplate = null!, // This will trigger the standard formatter path
            IncludeMetadata = false
        };
    
        var config = new LoggingConfig
        {
            Formatters =
            {
                Hybrid = hybridSettings,
            },
        };

        var logEntry = LogEntry.CreateStart("TestMethod", "TestClass");
        var context = FormattingContext.Create(logEntry, config);

        // Act
        var result = formatter.Format(context);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Message.Should().Be("Standard template"); // This comes from the StandardStructuredFormatter path
    }
}