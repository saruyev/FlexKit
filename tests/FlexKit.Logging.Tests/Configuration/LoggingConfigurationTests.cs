using FlexKit.Logging.Configuration;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Xunit;
// ReSharper disable MethodTooLong

namespace FlexKit.Logging.Tests.Configuration;

public class LoggingConfigurationTests
{
    [Fact]
    public void LoggingConfig_DefaultValues_AreSetCorrectly()
    {
        // Arrange & Act
        var config = new LoggingConfig();

        // Assert
        config.AutoIntercept.Should().BeTrue();
        config.QueueCapacity.Should().Be(10000);
        config.MaxBatchSize.Should().Be(1);
        config.BatchTimeout.Should().Be(TimeSpan.FromSeconds(1));
        config.DefaultFormatter.Should().Be(FormatterType.StandardStructured);
        config.EnableFallbackFormatting.Should().BeTrue();
        config.FallbackTemplate.Should().Be("Method {TypeName}.{MethodName} - Status: {Success}");
        config.DefaultTarget.Should().BeNull();
        config.ActivitySourceName.Should().Be("FlexKit.Logging");
        config.RequiresSerialization.Should().BeTrue();
        config.SuppressedLogLevel.Should().Be(LogLevel.None);
        config.Services.Should().BeEmpty();
        config.Targets.Should().BeEmpty();
        config.Templates.Should().BeEmpty();
        config.SuppressedCategories.Should().BeEquivalentTo(["Microsoft", "System", "FlexKit"]);
    }

    [Fact]
    public void LoggingConfig_SettersWork_AllProperties()
    {
        // Arrange
        var config = new LoggingConfig();
        var templates = new Dictionary<string, TemplateConfig> { ["test"] = new() };
        var services = new Dictionary<string, InterceptionConfig> { ["test"] = new() };
        var targets = new Dictionary<string, LoggingTarget> { ["test"] = new() };
        var suppressedCategories = new List<string> { "Custom" };

        // Act
        config.AutoIntercept = false;
        config.QueueCapacity = 2000;
        config.MaxBatchSize = 10;
        config.BatchTimeout = TimeSpan.FromMinutes(1);
        config.DefaultFormatter = FormatterType.Json;
        config.EnableFallbackFormatting = false;
        config.FallbackTemplate = "Custom template";
        config.DefaultTarget = "console";
        config.ActivitySourceName = "CustomSource";
        config.RequiresSerialization = false;
        config.SuppressedLogLevel = LogLevel.Warning;
        config.Templates = templates;
        config.Services = services;
        config.Targets = targets;
        config.SuppressedCategories = suppressedCategories;

        // Assert
        config.AutoIntercept.Should().BeFalse();
        config.QueueCapacity.Should().Be(2000);
        config.MaxBatchSize.Should().Be(10);
        config.BatchTimeout.Should().Be(TimeSpan.FromMinutes(1));
        config.DefaultFormatter.Should().Be(FormatterType.Json);
        config.EnableFallbackFormatting.Should().BeFalse();
        config.FallbackTemplate.Should().Be("Custom template");
        config.DefaultTarget.Should().Be("console");
        config.ActivitySourceName.Should().Be("CustomSource");
        config.RequiresSerialization.Should().BeFalse();
        config.SuppressedLogLevel.Should().Be(LogLevel.Warning);
        config.Templates.Should().BeSameAs(templates);
        config.Services.Should().BeSameAs(services);
        config.Targets.Should().BeSameAs(targets);
        config.SuppressedCategories.Should().BeSameAs(suppressedCategories);
    }

    [Fact]
    public void LoggingTarget_DefaultValues_AreSetCorrectly()
    {
        // Arrange & Act
        var target = new LoggingTarget();

        // Assert
        target.Type.Should().BeEmpty();
        target.Enabled.Should().BeTrue();
        target.Formatter.Should().BeNull();
        target.Properties.Should().BeEmpty();
    }

    [Fact]
    public void LoggingTarget_SettersWork_AllProperties()
    {
        // Arrange
        var target = new LoggingTarget();
        var properties = new Dictionary<string, IConfigurationSection?> { ["key"] = null };

        // Act
        target.Type = "Console";
        target.Enabled = false;
        target.Formatter = FormatterType.Json;
        target.Properties = properties;

        // Assert
        target.Type.Should().Be("Console");
        target.Enabled.Should().BeFalse();
        target.Formatter.Should().Be(FormatterType.Json);
        target.Properties.Should().BeSameAs(properties);
    }

    [Fact]
    public void InterceptionConfig_DefaultValues_AreSetCorrectly()
    {
        // Arrange & Act
        var config = new InterceptionConfig();

        // Assert
        config.LogInput.Should().BeFalse();
        config.LogOutput.Should().BeFalse();
        config.Level.Should().Be(LogLevel.Information);
        config.ExceptionLevel.Should().Be(LogLevel.Error);
        config.Target.Should().BeNull();
        config.ExcludeMethodPatterns.Should().BeEmpty();
        config.MaskParameterPatterns.Should().BeEmpty();
        config.MaskReplacement.Should().Be("***MASKED***");
    }

    [Fact]
    public void InterceptionConfig_SettersWork_AllProperties()
    {
        // Arrange
        var config = new InterceptionConfig();
        var excludePatterns = new List<string> { "Get*", "Set*" };
        var maskPatterns = new List<string> { "*password*", "*secret*" };

        // Act
        config.LogInput = true;
        config.LogOutput = true;
        config.Level = LogLevel.Debug;
        config.ExceptionLevel = LogLevel.Critical;
        config.Target = "file";
        config.ExcludeMethodPatterns = excludePatterns;
        config.MaskParameterPatterns = maskPatterns;
        config.MaskReplacement = "[REDACTED]";

        // Assert
        config.LogInput.Should().BeTrue();
        config.LogOutput.Should().BeTrue();
        config.Level.Should().Be(LogLevel.Debug);
        config.ExceptionLevel.Should().Be(LogLevel.Critical);
        config.Target.Should().Be("file");
        config.ExcludeMethodPatterns.Should().BeSameAs(excludePatterns);
        config.MaskParameterPatterns.Should().BeSameAs(maskPatterns);
        config.MaskReplacement.Should().Be("[REDACTED]");
    }

    [Fact]
    public void InterceptionConfig_GetDecision_ReturnsCorrectDecision()
    {
        // Arrange & Act & Assert
        var config1 = new InterceptionConfig { LogInput = true, LogOutput = false };
        var decision1 = config1.GetDecision();
        decision1.Behavior.Should().Be(InterceptionBehavior.LogInput);
        decision1.Level.Should().Be(LogLevel.Information);
        decision1.ExceptionLevel.Should().Be(LogLevel.Error);

        var config2 = new InterceptionConfig { LogInput = false, LogOutput = true };
        var decision2 = config2.GetDecision();
        decision2.Behavior.Should().Be(InterceptionBehavior.LogOutput);
        decision2.Level.Should().Be(LogLevel.Information);
        decision2.ExceptionLevel.Should().Be(LogLevel.Error);

        var config3 = new InterceptionConfig { LogInput = true, LogOutput = true };
        var decision3 = config3.GetDecision();
        decision3.Behavior.Should().Be(InterceptionBehavior.LogBoth);
        decision3.Level.Should().Be(LogLevel.Information);
        decision3.ExceptionLevel.Should().Be(LogLevel.Error);

        var config4 = new InterceptionConfig { LogInput = false, LogOutput = false };
        var decision4 = config4.GetDecision();
        decision4.Behavior.Should().Be(InterceptionBehavior.LogInput);
        decision4.Level.Should().Be(LogLevel.Information);
        decision4.ExceptionLevel.Should().Be(LogLevel.Error);
    }

    [Fact]
    public void TemplateConfig_DefaultValues_AreSetCorrectly()
    {
        // Arrange & Act
        var config = new TemplateConfig();

        // Assert
        config.SuccessTemplate.Should().BeEmpty();
        config.ErrorTemplate.Should().BeEmpty();
        config.GeneralTemplate.Should().BeEmpty();
        config.Enabled.Should().BeTrue();
    }

    [Fact]
    public void TemplateConfig_SettersWork_AllProperties()
    {
        // Arrange
        var config = new TemplateConfig
        {
            // Act
            SuccessTemplate = "Success: {Method}",
            ErrorTemplate = "Error: {Method} - {Exception}",
            GeneralTemplate = "General: {Method}",
            Enabled = false
        };

        // Assert
        config.SuccessTemplate.Should().Be("Success: {Method}");
        config.ErrorTemplate.Should().Be("Error: {Method} - {Exception}");
        config.GeneralTemplate.Should().Be("General: {Method}");
        config.Enabled.Should().BeFalse();
    }

    [Fact]
    public void TemplateConfig_GetTemplateForOutcome_Success_ReturnsCorrectTemplate()
    {
        // Arrange
        var config = new TemplateConfig
        {
            SuccessTemplate = "Success template",
            ErrorTemplate = "Error template",
            GeneralTemplate = "General template"
        };

        // Act & Assert
        config.GetTemplateForOutcome(true).Should().Be("Success template");
    }

    [Fact]
    public void TemplateConfig_GetTemplateForOutcome_Error_ReturnsCorrectTemplate()
    {
        // Arrange
        var config = new TemplateConfig
        {
            SuccessTemplate = "Success template",
            ErrorTemplate = "Error template",
            GeneralTemplate = "General template"
        };

        // Act & Assert
        config.GetTemplateForOutcome(false).Should().Be("Error template");
    }

    [Fact]
    public void TemplateConfig_GetTemplateForOutcome_FallsBackToGeneral_WhenSpecificTemplateEmpty()
    {
        // Arrange
        var config = new TemplateConfig
        {
            SuccessTemplate = "",
            ErrorTemplate = "",
            GeneralTemplate = "General template"
        };

        // Act & Assert
        config.GetTemplateForOutcome(true).Should().Be("General template");
        config.GetTemplateForOutcome(false).Should().Be("General template");
    }

    [Fact]
    public void TemplateConfig_IsValid_ReturnsTrueWhenTemplatesExist()
    {
        // Arrange & Act & Assert
        var config1 = new TemplateConfig { SuccessTemplate = "template" };
        config1.IsValid().Should().BeTrue();

        var config2 = new TemplateConfig { ErrorTemplate = "template" };
        config2.IsValid().Should().BeTrue();

        var config3 = new TemplateConfig { GeneralTemplate = "template" };
        config3.IsValid().Should().BeTrue();
    }

    [Fact]
    public void TemplateConfig_IsValid_ReturnsFalseWhenAllTemplatesEmpty()
    {
        // Arrange
        var config = new TemplateConfig
        {
            SuccessTemplate = "",
            ErrorTemplate = "",
            GeneralTemplate = ""
        };

        // Act & Assert
        config.IsValid().Should().BeFalse();
    }

    [Fact]
    public void FormatterSettings_DefaultValues_AreSetCorrectly()
    {
        // Arrange & Act
        var settings = new FormatterSettings();

        // Assert
        settings.Json.Should().NotBeNull();
        settings.Hybrid.Should().NotBeNull();
        settings.CustomTemplate.Should().NotBeNull();
    }
}