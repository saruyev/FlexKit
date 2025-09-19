using System.Reflection;
using FlexKit.Logging.Configuration;
using FlexKit.Logging.Interception;
using FlexKit.Logging.Interception.Attributes;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Xunit;

namespace FlexKit.Logging.Tests.Interception.Attributes;

public class AttributeResolverTests
{
    [Fact]
    public void IsLoggingDisabled_WithMethodLevelNoLogAttribute_ReturnsTrue()
    {
        // Arrange
        var method = GetMethodWithAttributes<TestClass>(nameof(TestClass.MethodWithNoLog));

        // Act
        var result = AttributeResolver.IsLoggingDisabled(method);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsLoggingDisabled_WithNoDisablingAttributes_ReturnsFalse()
    {
        // Arrange
        var method = GetMethodWithAttributes<TestClass>(nameof(TestClass.PlainMethod));

        // Act
        var result = AttributeResolver.IsLoggingDisabled(method);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void ResolveInterceptionDecision_WithLogBothAttribute_ReturnsCorrectDecision()
    {
        // Arrange
        var method = GetMethodWithAttributes<TestClass>(nameof(TestClass.MethodWithLogBoth));

        // Act
        var result = AttributeResolver.ResolveInterceptionDecision(method);

        // Assert
        result.Should().NotBeNull();
        result!.Value.Behavior.Should().Be(InterceptionBehavior.LogBoth);
        result.Value.Level.Should().Be(LogLevel.Information);
    }

    [Fact]
    public void ResolveInterceptionDecision_WithBothLogInputAndLogOutput_ReturnsLogBothDecision()
    {
        // Arrange
        var method = GetMethodWithAttributes<TestClass>(nameof(TestClass.MethodWithBothInputAndOutput));

        // Act
        var result = AttributeResolver.ResolveInterceptionDecision(method);

        // Assert
        result.Should().NotBeNull();
        result!.Value.Behavior.Should().Be(InterceptionBehavior.LogBoth);
    }

    [Fact]
    public void ResolveInterceptionDecision_WithLogInputAttribute_ReturnsLogInputDecision()
    {
        // Arrange
        var method = GetMethodWithAttributes<TestClass>(nameof(TestClass.MethodWithLogInput));

        // Act
        var result = AttributeResolver.ResolveInterceptionDecision(method);

        // Assert
        result.Should().NotBeNull();
        result!.Value.Behavior.Should().Be(InterceptionBehavior.LogInput);
        result.Value.Level.Should().Be(LogLevel.Debug);
    }

    [Fact]
    public void ResolveInterceptionDecision_WithLogOutputAttribute_ReturnsLogOutputDecision()
    {
        // Arrange
        var method = GetMethodWithAttributes<TestClass>(nameof(TestClass.MethodWithLogOutput));

        // Act
        var result = AttributeResolver.ResolveInterceptionDecision(method);

        // Assert
        result.Should().NotBeNull();
        result!.Value.Behavior.Should().Be(InterceptionBehavior.LogOutput);
        result.Value.Level.Should().Be(LogLevel.Warning);
    }

    private static MethodInfo GetMethodWithAttributes<T>(string methodName)
    {
        return typeof(T).GetMethod(methodName) 
               ?? throw new InvalidOperationException($"Method {methodName} not found");
    }

    private class TestClass
    {
        public void PlainMethod() { }

        [NoLog]
        public void MethodWithNoLog() { }

        [LogBoth]
        public void MethodWithLogBoth() { }

        [LogInput(LogLevel.Debug)]
        [LogOutput(LogLevel.Warning)]
        public void MethodWithBothInputAndOutput() { }

        [LogInput(LogLevel.Debug)]
        public void MethodWithLogInput() { }

        [LogOutput(LogLevel.Warning)]
        public void MethodWithLogOutput() { }
    }
}