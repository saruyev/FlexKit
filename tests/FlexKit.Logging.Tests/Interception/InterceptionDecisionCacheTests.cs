using System.Reflection;
using FlexKit.Logging.Configuration;
using FlexKit.Logging.Core;
using FlexKit.Logging.Interception;
using FlexKit.Logging.Interception.Attributes;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable ClassTooBig
// ReSharper disable UnusedMember.Local
// ReSharper disable UnusedParameter.Local
// ReSharper disable TooManyArguments

namespace FlexKit.Logging.Tests.Interception;

public class InterceptionDecisionCacheTests
{
    [Fact]
    public void GetInterceptionDecision_WithNullDeclaringType_ReturnsNull()
    {
        // Arrange
        var mockMethod = Substitute.For<MethodInfo>();
        mockMethod.DeclaringType.Returns((Type?)null);
    
        var config = new LoggingConfig();
        var cache = new InterceptionDecisionCache(config);

        // Act
        var result = cache.GetInterceptionDecision(mockMethod);

        // Assert
        result.Should().BeNull();
    }
    
    [Fact]
    public void GetInterceptionDecision_WithCachedType_ReturnsCachedDecision()
    {
        // Arrange
        var config = new LoggingConfig(); // AutoIntercept is true by default
        var cache = new InterceptionDecisionCache(config);
    
        var testType = typeof(TestClass);
        var method = testType.GetMethod(nameof(TestClass.TestMethod))!;
    
        // Cache the type first so it exists in _typeDecisions
        cache.CacheTypeDecisions(testType);

        // Act
        var result = cache.GetInterceptionDecision(method);

        // Assert
        result.Should().NotBeNull();
        result.Value.Behavior.Should().Be(InterceptionBehavior.LogInput);
        result.Value.Level.Should().Be(LogLevel.Information);
    }
    
    [Fact]
    public void GetInterceptionDecision_WithCachedTypeButMethodNotCached_ReturnsNull()
    {
        // Arrange
        var config = new LoggingConfig { AutoIntercept = false }; // Disable auto-intercept so methods cache as null
        var cache = new InterceptionDecisionCache(config);
    
        var testType = typeof(TestClass);
        var method = testType.GetMethod(nameof(TestClass.TestMethod))!;
    
        // Cache the type first - with AutoIntercept disabled, methods will be cached as null
        cache.CacheTypeDecisions(testType);

        // Act
        var result = cache.GetInterceptionDecision(method);

        // Assert
        result.Should().BeNull();
    }
    
    [Fact]
    public void GetInterceptionDecision_WithConcreteTypeNotCached_ReturnsComputedDecision()
    {
        // Arrange
        var config = new LoggingConfig { AutoIntercept = true };
        var cache = new InterceptionDecisionCache(config);
    
        var testType = typeof(TestClass);
        var method = testType.GetMethod(nameof(TestClass.TestMethod))!;

        // Act
        var result = cache.GetInterceptionDecision(method);

        // Assert
        result.Should().NotBeNull();
        result.Value.Behavior.Should().Be(InterceptionBehavior.LogInput);
        result.Value.Level.Should().Be(LogLevel.Information);
    }
    
    [Fact]
    public void GetInterceptionDecision_WithInterfaceNoImplementationCached_ReturnsComputedDecision()
    {
        // Arrange
        var config = new LoggingConfig { AutoIntercept = true };
        var cache = new InterceptionDecisionCache(config);
    
        var interfaceType = typeof(ITestInterface);
        var method = interfaceType.GetMethod(nameof(ITestInterface.TestMethod))!;
    
        // Don't cache any implementation types, so FindImplementationType returns null
        // This hits the implementationType == null part of the condition

        // Act
        var result = cache.GetInterceptionDecision(method);

        // Assert
        result.Should().NotBeNull();
        result.Value.Behavior.Should().Be(InterceptionBehavior.LogInput);
        result.Value.Level.Should().Be(LogLevel.Information);
    }
    
    [Fact]
    public void GetInterceptionDecision_WithInterfaceAndCachedImplementationMethod_ReturnsImplementationDecision()
    {
        // Arrange
        var config = new LoggingConfig { AutoIntercept = true };
        var cache = new InterceptionDecisionCache(config);
    
        var implementationType = typeof(TestImplementation);
        var interfaceType = typeof(ITestInterface);
    
        // Cache the implementation type first so it exists in _typeDecisions
        cache.CacheTypeDecisions(implementationType);
    
        var interfaceMethod = interfaceType.GetMethod(nameof(ITestInterface.TestMethod))!;

        // Act
        var result = cache.GetInterceptionDecision(interfaceMethod);

        // Assert
        result.Should().NotBeNull();
        result.Value.Behavior.Should().Be(InterceptionBehavior.LogInput);
        result.Value.Level.Should().Be(LogLevel.Information);
    }
    
    [Fact]
    public void GetInterceptionDecision_WithInterfaceNoMatchingImplementationMethod_ReturnsComputedDecision()
    {
        // Arrange
        var config = new LoggingConfig { AutoIntercept = true };
        var cache = new InterceptionDecisionCache(config);
    
        var implementationType = typeof(TestImplementationWithDifferentSignature);
        var interfaceType = typeof(ITestInterface);
    
        // Cache the implementation type first so it exists in _typeDecisions
        cache.CacheTypeDecisions(implementationType);
    
        var interfaceMethod = interfaceType.GetMethod(nameof(ITestInterface.TestMethod))!;

        // Act
        var result = cache.GetInterceptionDecision(interfaceMethod);

        // Assert
        result.Should().NotBeNull();
        result.Value.Behavior.Should().Be(InterceptionBehavior.LogInput);
        result.Value.Level.Should().Be(LogLevel.Information);
    }
    
    [Fact]
    public void GetInterceptionDecision_WithNonPublicMethod_ReturnsNull()
    {
        // Arrange
        var config = new LoggingConfig { AutoIntercept = true };
        var cache = new InterceptionDecisionCache(config);
    
        var testType = typeof(TestClassWithPrivateMethod);
        var method = testType.GetMethod("PrivateMethod", BindingFlags.NonPublic | BindingFlags.Instance)!;

        // Act
        var result = cache.GetInterceptionDecision(method);

        // Assert
        result.Should().BeNull(); // ShouldInterceptMethod returns false for private methods
    }
    
    [Fact]
    public void GetInterceptionDecision_WithNoLogAttribute_ReturnsNull()
    {
        // Arrange
        var config = new LoggingConfig { AutoIntercept = true };
        var cache = new InterceptionDecisionCache(config);
    
        var testType = typeof(TestClassWithNoLog);
        var method = testType.GetMethod(nameof(TestClassWithNoLog.MethodWithNoLog))!;

        // Act
        var result = cache.GetInterceptionDecision(method);

        // Assert
        result.Should().BeNull(); // AttributeResolver.IsLoggingDisabled returns true
    }
    
    [Fact]
    public void GetInterceptionDecision_WithLogInputAttribute_ReturnsAttributeDecision()
    {
        // Arrange
        var config = new LoggingConfig { AutoIntercept = true };
        var cache = new InterceptionDecisionCache(config);
    
        var testType = typeof(TestClassWithLogInput);
        var method = testType.GetMethod(nameof(TestClassWithLogInput.MethodWithLogInput))!;

        // Act
        var result = cache.GetInterceptionDecision(method);

        // Assert
        result.Should().NotBeNull();
        result.Value.Behavior.Should().Be(InterceptionBehavior.LogInput);
        result.Value.Level.Should().Be(LogLevel.Debug);
    }
    
    [Fact]
    public void GetInterceptionDecision_WithExactConfigurationMatch_ReturnsConfiguredDecision()
    {
        // Arrange
        var config = new LoggingConfig
        {
            Services =
            {
                ["FlexKit.Logging.Tests.Interception.InterceptionDecisionCacheTests+TestClassWithConfig"] = new InterceptionConfig
                {
                    LogInput = true,
                    LogOutput = true,
                    Level = LogLevel.Warning
                },
            },
        };

        var cache = new InterceptionDecisionCache(config);
        var testType = typeof(TestClassWithConfig);
        var method = testType.GetMethod(nameof(TestClassWithConfig.TestMethod))!;

        // Act
        var result = cache.GetInterceptionDecision(method);

        // Assert
        result.Should().NotBeNull();
        result.Value.Behavior.Should().Be(InterceptionBehavior.LogBoth);
        result.Value.Level.Should().Be(LogLevel.Warning);
    }
    
    [Fact]
    public void GetInterceptionDecision_WithNoMatchingConfigurationPattern_ReturnsAutoInterceptDecision()
    {
        // Arrange
        var config = new LoggingConfig
        {
            AutoIntercept = true,
            Services =
            {
                ["SomeOther.Namespace.Class"] = new InterceptionConfig
                {
                    LogInput = true,
                    Level = LogLevel.Warning
                },
                ["Different.Pattern.*"] = new InterceptionConfig
                {
                    LogOutput = true,
                    Level = LogLevel.Error
                },
            },
        };

        var cache = new InterceptionDecisionCache(config);
        var testType = typeof(TestClassWithNoMatchingConfig);
        var method = testType.GetMethod(nameof(TestClassWithNoMatchingConfig.TestMethod))!;

        // Act
        var result = cache.GetInterceptionDecision(method);

        // Assert
        result.Should().NotBeNull();
        result.Value.Behavior.Should().Be(InterceptionBehavior.LogInput);
        result.Value.Level.Should().Be(LogLevel.Information);
    }
    
    [Fact]
    public void GetInterceptionDecision_WithWildcardConfigurationMatch_ReturnsConfiguredDecision()
    {
        // Arrange
        var config = new LoggingConfig
        {
            Services =
            {
                ["FlexKit.Logging.Tests.Interception.*"] = new InterceptionConfig
                {
                    LogOutput = true,
                    Level = LogLevel.Debug
                },
            },
        };

        var cache = new InterceptionDecisionCache(config);
        var testType = typeof(TestClassWithWildcardMatch);
        var method = testType.GetMethod(nameof(TestClassWithWildcardMatch.TestMethod))!;

        // Act
        var result = cache.GetInterceptionDecision(method);

        // Assert
        result.Should().NotBeNull();
        result.Value.Behavior.Should().Be(InterceptionBehavior.LogOutput);
        result.Value.Level.Should().Be(LogLevel.Debug);
    }
    
    [Fact]
    public void GetInterceptionDecision_WithInterfaceMethodWithParameters_FindsMatchingImplementation()
    {
        // Arrange
        var config = new LoggingConfig { AutoIntercept = true };
        var cache = new InterceptionDecisionCache(config);
    
        var implementationType = typeof(TestImplementationWithParameters);
        var interfaceType = typeof(ITestInterfaceWithParameters);
    
        // Cache the implementation type first
        cache.CacheTypeDecisions(implementationType);
    
        var interfaceMethod = interfaceType.GetMethod(nameof(ITestInterfaceWithParameters.TestMethodWithParams))!;

        // Act
        var result = cache.GetInterceptionDecision(interfaceMethod);

        // Assert
        result.Should().NotBeNull();
        result.Value.Behavior.Should().Be(InterceptionBehavior.LogInput);
        result.Value.Level.Should().Be(LogLevel.Information);
    }
    
    [Fact]
    public void GetInterceptionDecision_WithFlexKitLoggerInjection_ReturnsNull()
    {
        // Arrange
        var config = new LoggingConfig { AutoIntercept = true };
        var cache = new InterceptionDecisionCache(config);
    
        var testType = typeof(TestClassWithFlexKitLogger);
        var method = testType.GetMethod(nameof(TestClassWithFlexKitLogger.TestMethod))!;

        // Act
        var result = cache.GetInterceptionDecision(method);

        // Assert
        result.Should().BeNull(); // ShouldInterceptMethod returns false due to FlexKitLogger injection
    }
    
    [Fact]
    public void GetInterceptionDecision_WithExactMethodNameMatch_ReturnsNull()
    {
        // Arrange
        var config = new LoggingConfig { AutoIntercept = true };
        config.Services["FlexKit.Logging.Tests.Interception.InterceptionDecisionCacheTests+TestClassWithExcludePattern"] = new InterceptionConfig
        {
            LogInput = true,
            Level = LogLevel.Information,
            ExcludeMethodPatterns = new List<string> { "ExcludedMethod" } // Exact match pattern
        };
    
        var cache = new InterceptionDecisionCache(config);
        var testType = typeof(TestClassWithExcludePattern);
        var method = testType.GetMethod(nameof(TestClassWithExcludePattern.ExcludedMethod))!;
    
        // Don't cache the type so it goes through ComputeMethodDecision

        // Act
        var result = cache.GetInterceptionDecision(method);

        // Assert
        result.Should().BeNull(); // Method excluded by exact name match
    }
    
    [Fact]
    public void GetInterceptionDecision_WithContainsMethodNamePattern_ReturnsNull()
    {
        // Arrange
        var config = new LoggingConfig { AutoIntercept = true };
        config.Services["FlexKit.Logging.Tests.Interception.InterceptionDecisionCacheTests+TestClassWithContainsPattern"] = new InterceptionConfig
        {
            LogInput = true,
            Level = LogLevel.Information,
            ExcludeMethodPatterns = new List<string> { "*Test*" } // Contains pattern
        };
    
        var cache = new InterceptionDecisionCache(config);
        var testType = typeof(TestClassWithContainsPattern);
        var method = testType.GetMethod(nameof(TestClassWithContainsPattern.MyTestMethod))!;
    
        // Don't cache the type so it goes through ComputeMethodDecision

        // Act
        var result = cache.GetInterceptionDecision(method);

        // Assert
        result.Should().BeNull(); // Method excluded by contains pattern match
    }
    
    [Fact]
    public void GetInterceptionDecision_WithSuffixMethodNamePattern_ReturnsNull()
    {
        // Arrange
        var config = new LoggingConfig { AutoIntercept = true };
        config.Services["FlexKit.Logging.Tests.Interception.InterceptionDecisionCacheTests+TestClassWithSuffixPattern"] = new InterceptionConfig
        {
            LogInput = true,
            Level = LogLevel.Information,
            ExcludeMethodPatterns = new List<string> { "*Method" } // Suffix pattern
        };
    
        var cache = new InterceptionDecisionCache(config);
        var testType = typeof(TestClassWithSuffixPattern);
        var method = testType.GetMethod(nameof(TestClassWithSuffixPattern.TestMethod))!;
    
        // Don't cache the type so it goes through ComputeMethodDecision

        // Act
        var result = cache.GetInterceptionDecision(method);

        // Assert
        result.Should().BeNull(); // Method excluded by suffix pattern match
    }
    
    [Fact]
    public void GetInterceptionDecision_WithPrefixMethodNamePattern_ReturnsNull()
    {
        // Arrange
        var config = new LoggingConfig { AutoIntercept = true };
        config.Services["FlexKit.Logging.Tests.Interception.InterceptionDecisionCacheTests+TestClassWithPrefixPattern"] = new InterceptionConfig
        {
            LogInput = true,
            Level = LogLevel.Information,
            ExcludeMethodPatterns = new List<string> { "Test*" } // Prefix pattern
        };
    
        var cache = new InterceptionDecisionCache(config);
        var testType = typeof(TestClassWithPrefixPattern);
        var method = testType.GetMethod(nameof(TestClassWithPrefixPattern.TestMethod))!;
    
        // Don't cache the type so it goes through ComputeMethodDecision

        // Act
        var result = cache.GetInterceptionDecision(method);

        // Assert
        result.Should().BeNull(); // Method excluded by prefix pattern match
    }
    
    [Fact]
    public void ShouldInterceptMethod_ReturnsTrue_WhenDeclaringTypeFullNameIsNull()
    {
        // Arrange
        var fakeType = new FakeType();
        var fakeMethod = new FakeMethodInfo(fakeType);

        var sut = new InterceptionDecisionCache(new LoggingConfig { AutoIntercept = true });
        var privateMethod = typeof(InterceptionDecisionCache)
            .GetMethod("ShouldInterceptMethod", BindingFlags.NonPublic | BindingFlags.Instance)!;

        // Act
        var result = (bool)privateMethod.Invoke(sut, new object[] { fakeMethod })!;

        // Assert
        result.Should().BeTrue("because DeclaringType.FullName was forced to null");
    }
    
    private class FakeMethodInfo : MethodInfo
    {
        private readonly Type _declaringType;

        public FakeMethodInfo(Type declaringType) => _declaringType = declaringType;

        public override Type DeclaringType => _declaringType;
        public override string Name => "FakeMethod";
        public override MethodAttributes Attributes => MethodAttributes.Public; // makes IsPublic = true
        public new bool IsPublic => true;
        public new bool IsStatic => false;
        public new bool IsConstructor => false;
        public new bool IsSpecialName => false;

        #region not used
        public override RuntimeMethodHandle MethodHandle => throw new NotImplementedException();
        public override ICustomAttributeProvider ReturnTypeCustomAttributes => throw new NotImplementedException();
        public override Type ReflectedType => throw new NotImplementedException();
        public override MethodInfo GetBaseDefinition() => throw new NotImplementedException();
        public override object[] GetCustomAttributes(bool inherit) => throw new NotImplementedException();
        public override object[] GetCustomAttributes(Type attributeType, bool inherit) => throw new NotImplementedException();
        public override bool IsDefined(Type attributeType, bool inherit) => throw new NotImplementedException();

        public override MethodImplAttributes GetMethodImplementationFlags() => throw new NotImplementedException();
        public override ParameterInfo[] GetParameters() => Array.Empty<ParameterInfo>();
        public override object Invoke(object? obj, BindingFlags invokeAttr, Binder? binder, object?[]? parameters, System.Globalization.CultureInfo? culture) 
            => throw new NotImplementedException();
        public override Type ReturnType => typeof(void);
        #endregion
    }

    private class FakeType : TypeDelegator
    {
        public FakeType() : base(typeof(object)) { }
        public override string FullName => null!; // <- key part
    }
    
    public class TestClassWithPrefixPattern
    {
        public void TestMethod() { } // Starts with "Test" - should be excluded
        public void MyMethod() { } // Doesn't start with "Test" - should be included
    }

    public class TestClassWithSuffixPattern
    {
        public void TestMethod() { } // Ends with "Method" - should be excluded
        public void TestAction() { } // Doesn't end with "Method" - should be included
    }

    public class TestClassWithContainsPattern
    {
        public void MyTestMethod() { } // Contains "Test" - should be excluded
        public void MyOtherMethod() { } // Doesn't contain "Test" - should be included
    }

    public class TestClassWithExcludePattern
    {
        public void ExcludedMethod() { }
        public void IncludedMethod() { }
    }

    public class TestClassWithFlexKitLogger
    {
        public TestClassWithFlexKitLogger(IFlexKitLogger logger)
        {
            // Constructor with IFlexKitLogger parameter
        }
    
        public void TestMethod() { }
    }

    public interface ITestInterfaceWithParameters
    {
        void TestMethodWithParams(string param1, int param2);
    }

    public class TestImplementationWithParameters : ITestInterfaceWithParameters
    {
        public void TestMethodWithParams(string param1, int param2) { }
    }

    public class TestClassWithWildcardMatch
    {
        public void TestMethod() { }
    }

    public class TestClassWithNoMatchingConfig
    {
        public void TestMethod() { }
    }

    public class TestClassWithConfig
    {
        public void TestMethod() { }
    }

    public class TestClassWithLogInput
    {
        [LogInput(LogLevel.Debug)]
        public void MethodWithLogInput() { }
    }

    public class TestClassWithNoLog
    {
        [NoLog]
        public void MethodWithNoLog() { }
    }

    public class TestClassWithPrivateMethod
    {
        private void PrivateMethod() { }
    }
    
    public class TestImplementationWithDifferentSignature : ITestInterface
    {
        void ITestInterface.TestMethod() { } // Explicit interface implementation - different method signature
        public void TestMethod(string parameter) { } // Different signature than interface
    }

    public interface ITestInterface
    {
        void TestMethod();
    }

    public class TestImplementation : ITestInterface
    {
        public void TestMethod() { }
    }

    public class TestClass
    {
        public void TestMethod() { }
    }
}