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
    public class SuccessErrorFormatterTests
    {
        [Fact]
        public void Format_WhenDisableFormattingTrue_ReturnsTemplateAndParameters()
        {
            // Arrange
            var translator = Substitute.For<IMessageTranslator>();
            translator.TranslateTemplate(Arg.Any<string>()).Returns("✅ Method {MethodName} completed");
            translator.TranslateParameters(Arg.Any<IReadOnlyDictionary<string, object?>>(), Arg.Any<string>())
                .Returns(callInfo => callInfo.Arg<IReadOnlyDictionary<string, object?>>());
            
            var formatter = new SuccessErrorFormatter(translator);
            
            var config = new LoggingConfig();
            var logEntry = LogEntry.CreateStart("TestMethod", "TestClass");
            var context = FormattingContext.Create(logEntry, config)
                .WithoutFormatting();

            // Act
            var result = formatter.Format(context);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Template.Should().Be("✅ Method {MethodName} completed");
            result.Parameters.Should().ContainKey("MethodName");
            result.Parameters["MethodName"].Should().Be("TestMethod");
        }

        [Fact]
        public void Format_WhenDisableFormattingFalseAndEmptyParameters_ReturnsFormattedMessage()
        {
            // Arrange
            var translator = Substitute.For<IMessageTranslator>();
            translator.TranslateTemplate(Arg.Any<string>()).Returns("✅ Success message");
            translator.TranslateParameters(Arg.Any<IReadOnlyDictionary<string, object?>>(), Arg.Any<string>())
                .Returns(new Dictionary<string, object?>()); // Empty parameters
            
            var formatter = new SuccessErrorFormatter(translator);
            
            var config = new LoggingConfig();
            var logEntry = LogEntry.CreateStart("TestMethod", "TestClass");
            var context = FormattingContext.Create(logEntry, config);

            // Act
            var result = formatter.Format(context);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Message.Should().Be("✅ Success message");
        }

        [Fact]
        public void Format_WhenDisableFormattingFalseAndHasParametersAndPlaceholders_ReplacesPlaceholders()
        {
            // Arrange
            var translator = Substitute.For<IMessageTranslator>();
            translator.TranslateTemplate(Arg.Any<string>()).Returns("✅ Method {MethodName} completed in {Duration}ms");
            translator.TranslateParameters(Arg.Any<IReadOnlyDictionary<string, object?>>(), Arg.Any<string>())
                .Returns(new Dictionary<string, object?>
                {
                    ["MethodName"] = "ProcessPayment",
                    ["Duration"] = "450",
                    ["Success"] = "True"
                });
            
            var formatter = new SuccessErrorFormatter(translator);
            
            var config = new LoggingConfig();
            var logEntry = LogEntry.CreateStart("ProcessPayment", "PaymentService");
            var context = FormattingContext.Create(logEntry, config);

            // Act
            var result = formatter.Format(context);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Message.Should().Be("✅ Method ProcessPayment completed in 450ms");
        }

        [Fact]
        public void Format_WhenExceptionThrown_ReturnsFormattedMessageFailure()
        {
            // Arrange
            var translator = Substitute.For<IMessageTranslator>();
            translator.TranslateTemplate(Arg.Any<string>()).Throws(new ArgumentException("Template translation failed"));
            
            var formatter = new SuccessErrorFormatter(translator);
            
            var config = new LoggingConfig();
            var logEntry = LogEntry.CreateStart("TestMethod", "TestClass");
            var context = FormattingContext.Create(logEntry, config);

            // Act
            var result = formatter.Format(context);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Be("SuccessError formatting failed: Template translation failed");
        }
    }
}