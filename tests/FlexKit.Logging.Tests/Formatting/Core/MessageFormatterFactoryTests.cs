using FlexKit.Logging.Configuration;
using FlexKit.Logging.Formatting.Core;
using FlexKit.Logging.Formatting.Models;
using FlexKit.Logging.Models;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace FlexKit.Logging.Tests.Formatting.Core
{
    public class MessageFormatterFactoryTests
    {
        [Fact]
        public void Constructor_WithNullFormatters_ThrowsArgumentNullException()
        {
            // Act
            var act = () => new MessageFormatterFactory(null!);

            // Assert
            act.Should().Throw<ArgumentNullException>()
                .WithParameterName("formatters");
        }

        [Fact]
        public void Constructor_WithEmptyFormatters_ThrowsArgumentException()
        {
            // Arrange
            var formatters = Array.Empty<IMessageFormatter>();

            // Act
            var act = () => new MessageFormatterFactory(formatters);

            // Assert
            act.Should().Throw<ArgumentException>()
                .WithParameterName("formatters")
                .WithMessage("*At least one formatter must be provided*");
        }

        [Fact]
        public void Constructor_WithDuplicateFormatterTypes_ThrowsArgumentException()
        {
            // Arrange
            var formatter1 = Substitute.For<IMessageFormatter>();
            formatter1.FormatterType.Returns(FormatterType.StandardStructured);

            var formatter2 = Substitute.For<IMessageFormatter>();
            formatter2.FormatterType.Returns(FormatterType.StandardStructured);

            var formatters = new[] { formatter1, formatter2 };

            // Act
            var act = () => new MessageFormatterFactory(formatters);

            // Assert
            act.Should().Throw<ArgumentException>()
                .WithParameterName("formatters")
                .WithMessage("*Duplicate formatter types found in collection*");
        }

        [Fact]
        public void GetFormatter_WhenFormatterTypeExists_ReturnsMatchingFormatterWithFalseIsFallback()
        {
            // Arrange
            var jsonFormatter = Substitute.For<IMessageFormatter>();
            jsonFormatter.FormatterType.Returns(FormatterType.Json);

            var standardFormatter = Substitute.For<IMessageFormatter>();
            standardFormatter.FormatterType.Returns(FormatterType.StandardStructured);

            var formatters = new[] { jsonFormatter, standardFormatter };
            var factory = new MessageFormatterFactory(formatters);

            var context = default(FormattingContext)
                .WithFormatterType(FormatterType.Json);

            // Act
            var result = factory.GetFormatter(context);

            // Assert
            result.formatter.Should().Be(jsonFormatter);
            result.isFallback.Should().BeFalse();
        }

        [Fact]
        public void GetFormatter_WhenFormatterTypeNotFoundAndFallbackEnabled_ReturnsFallbackFormatterWithTrueIsFallback()
        {
            // Arrange
            var standardFormatter = Substitute.For<IMessageFormatter>();
            standardFormatter.FormatterType.Returns(FormatterType.StandardStructured);
    
            var formatters = new[] { standardFormatter };
            var factory = new MessageFormatterFactory(formatters);
    
            var config = new LoggingConfig
            {
                DefaultFormatter = FormatterType.StandardStructured,
                EnableFallbackFormatting = true,
            };

            var logEntry = new LogEntry();
            var context = FormattingContext.Create(logEntry, config)
                .WithFormatterType(FormatterType.Json);

            // Act
            var result = factory.GetFormatter(context);

            // Assert
            result.formatter.Should().Be(standardFormatter);
            result.isFallback.Should().BeTrue();
        }

        [Fact]
        public void GetFormatter_WhenFormatterTypeNotFoundAndFallbackDisabled_ThrowsInvalidOperationException()
        {
            // Arrange  
            var standardFormatter = Substitute.For<IMessageFormatter>();
            standardFormatter.FormatterType.Returns(FormatterType.StandardStructured);

            var formatters = new[] { standardFormatter };
            var factory = new MessageFormatterFactory(formatters);

            var context = default(FormattingContext)
                .WithFormatterType(FormatterType.Json);
            // EnableFallback defaults to false

            // Act
            var act = () => factory.GetFormatter(context);

            // Assert
            act.Should().Throw<InvalidOperationException>()
                .WithMessage("*No suitable formatter found for type Json*EnableFallback: False*");
        }
    }
}
