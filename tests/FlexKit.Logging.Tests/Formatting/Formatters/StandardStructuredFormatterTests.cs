using FlexKit.Logging.Configuration;
using FlexKit.Logging.Formatting.Formatters;
using FlexKit.Logging.Formatting.Models;
using FlexKit.Logging.Formatting.Translation;
using FlexKit.Logging.Models;
using FluentAssertions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;

namespace FlexKit.Logging.Tests.Formatting.Formatters
{
    public class StandardStructuredFormatterTests
    {
        [Fact]
        public void Format_WhenDisableFormattingTrue_ReturnsTemplateAndParameters()
        {
            // Arrange
            var translator = Substitute.For<IMessageTranslator>();
            translator.TranslateTemplate(Arg.Any<string>()).Returns("Method {MethodName} completed");
            translator.TranslateParameters(Arg.Any<IReadOnlyDictionary<string, object?>>(), Arg.Any<string>())
                .Returns(callInfo => callInfo.Arg<IReadOnlyDictionary<string, object?>>());
            
            var formatter = new StandardStructuredFormatter(translator);
            
            var config = new LoggingConfig();
            var logEntry = LogEntry.CreateStart("TestMethod", "TestClass");
            var context = FormattingContext.Create(logEntry, config)
                .WithoutFormatting();

            // Act
            var result = formatter.Format(context);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Template.Should().Be("Method {MethodName} completed");
            result.Parameters.Should().ContainKey("MethodName");
            result.Parameters["MethodName"].Should().Be("TestMethod");
        }

        [Fact]
        public void Format_WhenDisableFormattingFalseAndEmptyParameters_ReturnsFormattedMessage()
        {
            // Arrange
            var translator = Substitute.For<IMessageTranslator>();
            translator.TranslateTemplate(Arg.Any<string>()).Returns("Simple message");
            translator.TranslateParameters(Arg.Any<IReadOnlyDictionary<string, object?>>(), Arg.Any<string>())
                .Returns(new Dictionary<string, object?>()); // Empty parameters
            
            var formatter = new StandardStructuredFormatter(translator);
            
            var config = new LoggingConfig();
            var logEntry = LogEntry.CreateStart("TestMethod", "TestClass");
            var context = FormattingContext.Create(logEntry, config);

            // Act
            var result = formatter.Format(context);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Message.Should().Be("Simple message");
        }

        [Fact]
        public void Format_WhenExceptionThrown_ReturnsFormattedMessageFailure()
        {
            // Arrange
            var translator = Substitute.For<IMessageTranslator>();
            translator.TranslateTemplate(Arg.Any<string>()).Throws(new InvalidOperationException("Template error"));
            
            var formatter = new StandardStructuredFormatter(translator);
            
            var config = new LoggingConfig();
            var logEntry = LogEntry.CreateStart("TestMethod", "TestClass");
            var context = FormattingContext.Create(logEntry, config);

            // Act
            var result = formatter.Format(context);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Be("StandardStructured formatting failed: Template error");
        }

        [Fact]
        public void Format_WhenDisableFormattingFalseAndHasExceptionMessageAndOtherParameters_AppendsExceptionInfo()
        {
            // Arrange
            var translator = Substitute.For<IMessageTranslator>();
            translator.TranslateTemplate(Arg.Any<string>()).Returns("Method {MethodName} failed");
            translator.TranslateParameters(Arg.Any<IReadOnlyDictionary<string, object?>>(), Arg.Any<string>())
                .Returns(new Dictionary<string, object?>
                {
                    ["MethodName"] = "TestMethod",
                    ["ExceptionMessage"] = "Something went wrong",
                    ["ExceptionType"] = "InvalidOperationException",
                    ["UserId"] = 123
                });
            
            var formatter = new StandardStructuredFormatter(translator);
            
            var config = new LoggingConfig();
            var logEntry = LogEntry.CreateStart("TestMethod", "TestClass");
            var context = FormattingContext.Create(logEntry, config);

            // Act
            var result = formatter.Format(context);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Message.Should().Contain("Method TestMethod failed");
            result.Message.Should().Contain("| Exception: InvalidOperationException - Something went wrong");
            result.Message.Should().NotContain("123"); // Other parameters are still replaced
        }
    }
}