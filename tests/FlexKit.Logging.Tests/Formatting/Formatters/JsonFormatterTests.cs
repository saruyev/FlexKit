using FlexKit.Logging.Configuration;
using FlexKit.Logging.Formatting.Models;
using FlexKit.Logging.Formatting.Translation;
using FlexKit.Logging.Models;
using FluentAssertions;
using NSubstitute;
using FlexKit.Logging.Formatting.Formatters;
using NSubstitute.ExceptionExtensions;
using Xunit;

namespace FlexKit.Logging.Tests.Formatting.Formatters
{
    public class JsonFormatterTests
    {
        [Fact]
        public void Format_WhenDisableFormattingTrueAndPrettyPrintFalse_CallsPrepareObject()
        {
            // Arrange
            var translator = Substitute.For<IMessageTranslator>();
            translator.TranslateTemplate("{Metadata}").Returns("{Metadata}");
            
            var formatter = new JsonFormatter(translator);
            
            var config = new LoggingConfig();
            config.Formatters.Json.PrettyPrint = false;
            
            var logEntry = LogEntry.CreateStart("TestMethod", "TestClass")
                .WithInput(new { UserId = 123 });
            var context = FormattingContext.Create(logEntry, config)
                .WithoutFormatting();

            // Act
            var result = formatter.Format(context);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Template.Should().Be("{Metadata}");
            result.Parameters.Should().ContainKey("Metadata");
            result.Parameters["Metadata"].Should().BeOfType<LogEntry>();
        }

        [Fact]
        public void Format_WhenDisableFormattingFalseAndPrettyPrintFalse_ReturnsCompactJson()
        {
            // Arrange
            var translator = Substitute.For<IMessageTranslator>();
            var formatter = new JsonFormatter(translator);
            
            var config = new LoggingConfig();
            config.Formatters.Json.PrettyPrint = false;
            
            var logEntry = LogEntry.CreateStart("TestMethod", "TestClass");
            var context = FormattingContext.Create(logEntry, config);

            // Act
            var result = formatter.Format(context);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Message.Should().NotContain("\n"); // Compact JSON has no newlines
            result.Message.Should().Contain("\"method_name\":\"TestMethod\""); // Snake case naming
        }

        [Fact]
        public void Format_WhenDisableFormattingTrueAndPrettyPrintTrue_ReturnsTemplateWithPrettyJsonMetadata()
        {
            // Arrange
            var translator = Substitute.For<IMessageTranslator>();
            translator.TranslateTemplate("{Metadata}", Arg.Any<LoggingConfig>()).Returns("{Metadata}");
            
            var formatter = new JsonFormatter(translator);
            
            var config = new LoggingConfig();
            config.Formatters.Json.PrettyPrint = true;
            
            var logEntry = LogEntry.CreateStart("TestMethod", "TestClass");
            var context = FormattingContext.Create(logEntry, config)
                .WithoutFormatting();

            // Act
            var result = formatter.Format(context);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Template.Should().Be("{Metadata}");
            result.Parameters.Should().ContainKey("Metadata");
            var metadataJson = result.Parameters["Metadata"] as string;
            metadataJson.Should().Contain("\n"); // Pretty print has newlines
        }

        [Fact]
        public void Format_WhenDisableFormattingFalseAndPrettyPrintTrue_ReturnsPrettyJson()
        {
            // Arrange
            var translator = Substitute.For<IMessageTranslator>();
            var formatter = new JsonFormatter(translator);
            
            var config = new LoggingConfig();
            config.Formatters.Json.PrettyPrint = true;
            
            var logEntry = LogEntry.CreateStart("TestMethod", "TestClass");
            var context = FormattingContext.Create(logEntry, config);

            // Act
            var result = formatter.Format(context);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Message.Should().Contain("\n"); // Pretty print has newlines
            result.Message.Should().Contain("  "); // Pretty print has indentation
            result.Message.Should().Contain("\"method_name\": \"TestMethod\""); // Snake case naming
        }

        [Fact]
        public void Format_WhenExceptionThrown_ReturnsFormattedMessageFailure()
        {
            // Arrange
            var translator = Substitute.For<IMessageTranslator>();
            translator.TranslateTemplate(Arg.Any<string>(), Arg.Any<LoggingConfig>())
                .Throws(new InvalidOperationException("Translation failed"));
            
            var formatter = new JsonFormatter(translator);
            
            var config = new LoggingConfig();
            config.Formatters.Json.PrettyPrint = false;
            
            var logEntry = LogEntry.CreateStart("TestMethod", "TestClass");
            var context = FormattingContext.Create(logEntry, config)
                .WithoutFormatting();

            // Act
            var result = formatter.Format(context);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Be("JSON formatting failed: Translation failed");
        }
    }
}