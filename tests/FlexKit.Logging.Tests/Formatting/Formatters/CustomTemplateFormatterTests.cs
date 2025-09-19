using FlexKit.Logging.Configuration;
using FlexKit.Logging.Formatting.Formatters;
using FlexKit.Logging.Formatting.Models;
using FlexKit.Logging.Formatting.Translation;
using FlexKit.Logging.Models;
using FluentAssertions;
using NSubstitute;
using Xunit;
// ReSharper disable TooManyDeclarations

namespace FlexKit.Logging.Tests.Formatting.Formatters;

public class CustomTemplateFormatterTests
{
    [Fact]
    public void Format_WhenTemplateFoundSuccessfully_ReturnsFormattedMessage()
    {
        // Arrange
        var translator = Substitute.For<IMessageTranslator>();
        translator.TranslateTemplate(Arg.Any<string>()).Returns("Translated template");
        translator.TranslateParameters(Arg.Any<IReadOnlyDictionary<string, object?>>(), Arg.Any<string>())
            .Returns(new Dictionary<string, object?>());
            
        var formatter = new CustomTemplateFormatter(translator);
            
        var customSettings = new CustomTemplateFormatterSettings
        {
            DefaultTemplate = "Processing {MethodName}",
            StrictValidation = false
        };
            
        var config = new LoggingConfig{Formatters = new FormatterSettings { CustomTemplate = customSettings }};
            
        var logEntry = LogEntry.CreateStart(
            nameof(Format_WhenTemplateFoundSuccessfully_ReturnsFormattedMessage),
            nameof(CustomTemplateFormatterTests));
        var context = FormattingContext.Create(logEntry, config);

        // Act
        var result = formatter.Format(context);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }
        
    [Fact]
    public void Format_OnException_ReturnsFailureMessage()
    {
        // Arrange
        var translator = Substitute.For<IMessageTranslator>();
        var formatter = new CustomTemplateFormatter(translator);
            
        var customSettings = new CustomTemplateFormatterSettings
        {
            DefaultTemplate = null! // No fallback template
        };
            
        var config = new LoggingConfig
        {
            Formatters = new FormatterSettings { CustomTemplate = customSettings },
            Templates = new Dictionary<string, TemplateConfig>()
        };
            
        var logEntry = new LogEntry();
        var context = FormattingContext.Create(logEntry, config);

        // Act
        var result = formatter.Format(context);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().StartWith("Custom template formatting failed:");
    }

    [Fact]
    public void Format_WhenNoTemplateFound_ReturnsFailureMessage()
    {
        // Arrange
        var translator = Substitute.For<IMessageTranslator>();
        var formatter = new CustomTemplateFormatter(translator);
            
        var customSettings = new CustomTemplateFormatterSettings
        {
            DefaultTemplate = null! // No fallback template
        };
            
        var config = new LoggingConfig
        {
            Formatters = new FormatterSettings { CustomTemplate = customSettings },
            Templates = new Dictionary<string, TemplateConfig>()
        };
            
        var logEntry = LogEntry.CreateStart(
            nameof(Format_WhenNoTemplateFound_ReturnsFailureMessage),
            nameof(CustomTemplateFormatterTests));
        var context = FormattingContext.Create(logEntry, config);

        // Act
        var result = formatter.Format(context);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Be("No custom template found for context");
    }

    [Fact]
    public void Format_WhenFallbackTemplateValidationFails_ReturnsValidationFailureMessage()
    {
        // Arrange
        var translator = Substitute.For<IMessageTranslator>();
        var formatter = new CustomTemplateFormatter(translator);
            
        var customSettings = new CustomTemplateFormatterSettings
        {
            DefaultTemplate = "Invalid template {unclosed",
            StrictValidation = true
        };
            
        var config = new LoggingConfig{Formatters = new FormatterSettings { CustomTemplate = customSettings }};
            
        var logEntry = LogEntry.CreateStart(
            nameof(Format_WhenFallbackTemplateValidationFails_ReturnsValidationFailureMessage),
            nameof(CustomTemplateFormatterTests));
        var context = FormattingContext.Create(logEntry, config);

        // Act
        var result = formatter.Format(context);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Be("No custom template found for context");
    }
        
    [Fact]
    public void Format_WhenDefaultValidationFails_ReturnsValidationFailureMessage()
    {
        // Arrange
        var translator = Substitute.For<IMessageTranslator>();
        var formatter = new CustomTemplateFormatter(translator);

        var customSettings = new CustomTemplateFormatterSettings(){DefaultTemplate = null!};
            
        var config = new LoggingConfig{Formatters = new FormatterSettings { CustomTemplate = customSettings }, Templates = new Dictionary<string, TemplateConfig>{
            ["Default"] = new TemplateConfig{Enabled = true, GeneralTemplate = "Invalid template {unclosed"}
        }};
            
        var logEntry = LogEntry.CreateStart(
            nameof(Format_WhenDefaultValidationFails_ReturnsValidationFailureMessage),
            nameof(CustomTemplateFormatterTests));
        var context = FormattingContext.Create(logEntry, config);

        // Act
        var result = formatter.Format(context);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Be("No custom template found for context");
    }
        
    [Fact]
    public void Format_WhenDefaultValidationFailsOnParameters_ReturnsValidationFailureMessage()
    {
        // Arrange
        var translator = Substitute.For<IMessageTranslator>();
        var formatter = new CustomTemplateFormatter(translator);

        var customSettings = new CustomTemplateFormatterSettings(){DefaultTemplate = null!};
            
        var config = new LoggingConfig{Formatters = new FormatterSettings { CustomTemplate = customSettings }, Templates = new Dictionary<string, TemplateConfig>{
            ["Default"] = new TemplateConfig{Enabled = true, GeneralTemplate = "Invalid template {bad parameter}"}
        }};
            
        var logEntry = LogEntry.CreateStart(
            nameof(Format_WhenDefaultValidationFailsOnParameters_ReturnsValidationFailureMessage),
            nameof(CustomTemplateFormatterTests));
        var context = FormattingContext.Create(logEntry, config);

        // Act
        var result = formatter.Format(context);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Be("Template validation failed for: Invalid template {bad parameter}");
    }
    
    [Fact]
public void Format_WithExtractParameters_ContainsRawInputParameters()
{
    // Arrange
    var translator = Substitute.For<IMessageTranslator>();
    translator.TranslateTemplate(Arg.Any<string>()).Returns("Template");
    translator.TranslateParameters(Arg.Any<IReadOnlyDictionary<string, object?>>(), Arg.Any<string>())
        .Returns(callInfo => callInfo.Arg<IReadOnlyDictionary<string, object?>>());
    
    var formatter = new CustomTemplateFormatter(translator);
    
    var customSettings = new CustomTemplateFormatterSettings
    {
        DefaultTemplate = "Processing {MethodName}",
        StrictValidation = false
    };
    
    var config = new LoggingConfig
    {
        Formatters =
        {
            CustomTemplate = customSettings,
        },
    };

    var inputData = new { UserId = 123, Name = "John" };
    var logEntry = LogEntry.CreateStart("TestMethod", "TestClass")
        .WithInput(inputData);
    
    var context = FormattingContext.Create(logEntry, config)
        .WithoutFormatting();

    // Act
    var result = formatter.Format(context);

    // Assert
    result.IsSuccess.Should().BeTrue();
    result.Parameters["InputParameters"].Should().Be(inputData);
}

[Fact] 
public void Format_WithStringifyParameters_ContainsStringifiedInputParameters()
{
    // Arrange
    var translator = Substitute.For<IMessageTranslator>();
    translator.TranslateTemplate(Arg.Any<string>()).Returns("Template");
    translator.TranslateParameters(Arg.Any<IReadOnlyDictionary<string, object?>>(), Arg.Any<string>())
        .Returns(callInfo => callInfo.Arg<IReadOnlyDictionary<string, object?>>());
    
    var formatter = new CustomTemplateFormatter(translator);
    
    var customSettings = new CustomTemplateFormatterSettings
    {
        DefaultTemplate = "Processing {MethodName}",
        StrictValidation = false
    };
    
    var config = new LoggingConfig
    {
        Formatters =
        {
            CustomTemplate = customSettings,
        },
    };

    var inputParams = new[] 
    {
        new InputParameter("userId", "Int32", 123),
        new InputParameter("name", "String", "John")
    };
    var logEntry = LogEntry.CreateStart("TestMethod", "TestClass")
        .WithInput(inputParams);
    
    var context = FormattingContext.Create(logEntry, config).WithFormatterType(FormatterType.CustomTemplate);

    // Act
    var result = formatter.Format(context);

    // Assert
    result.IsSuccess.Should().BeTrue();
    result.Parameters["InputParameters"].Should().BeOfType<string>();
    result.Parameters["InputParameters"]!.ToString().Should().Contain("userId");
    result.Parameters["InputParameters"]!.ToString().Should().Contain("name");
}

[Fact]
public void Format_WithJsonifyParameters_ContainsJsonProcessedInputParameters()
{
    // Arrange  
    var translator = Substitute.For<IMessageTranslator>();
    translator.TranslateTemplate(Arg.Any<string>()).Returns("Template");
    translator.TranslateParameters(Arg.Any<IReadOnlyDictionary<string, object?>>(), Arg.Any<string>())
        .Returns(callInfo => callInfo.Arg<IReadOnlyDictionary<string, object?>>());
    
    var formatter = new CustomTemplateFormatter(translator);
    
    var customSettings = new CustomTemplateFormatterSettings
    {
        DefaultTemplate = "Processing {MethodName}",
        StrictValidation = false
    };
    
    var config = new LoggingConfig
    {
        Formatters =
        {
            CustomTemplate = customSettings,
        },
    };

    var inputParams = new[]
    {
        new InputParameter("userId", "Int32", 123),
        new InputParameter("name", "String", "John")
    };
    var logEntry = LogEntry.CreateStart("TestMethod", "TestClass")
        .WithInput(inputParams);
    
    var context = FormattingContext.Create(logEntry, config)
        .WithFormatterType(FormatterType.Json); // This triggers Jsonify path

    // Act
    var result = formatter.Format(context);

    // Assert
    result.IsSuccess.Should().BeTrue();
    var inputParameters = result.Parameters["InputParameters"];
    inputParameters.Should().NotBeNull();
    inputParameters.Should().BeAssignableTo<IEnumerable<object>>();
}
}