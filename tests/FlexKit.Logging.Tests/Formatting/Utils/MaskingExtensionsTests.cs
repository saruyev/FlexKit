using System.Reflection;
using FlexKit.Logging.Configuration;
using FlexKit.Logging.Formatting.Utils;
using FlexKit.Logging.Interception;
using FlexKit.Logging.Interception.Attributes;
using FluentAssertions;
using NSubstitute;
using Xunit;
// ReSharper disable UnusedAutoPropertyAccessor.Local
// ReSharper disable PropertyCanBeMadeInitOnly.Local
// ReSharper disable UnusedParameter.Local
// ReSharper disable UnusedMember.Local
// ReSharper disable TooManyDeclarations
// ReSharper disable ClassTooBig

namespace FlexKit.Logging.Tests.Formatting.Utils;

public class MaskingExtensionsTests
{
    [Fact]
    public void ApplyParameterMasking_ValueOrParameterInfoIsNull_ReturnsOriginalValue()
    {
        // Arrange
        var config = new LoggingConfig();
        var context = new MethodLoggingInterceptor.InputContext(
            [],
            [],
            typeof(object),
            config);

        // Get actual parameter info from a real method
        var method = typeof(MaskingExtensionsTests).GetMethod(
            nameof(TestMethodWithNormalParameter),
            BindingFlags.NonPublic | BindingFlags.Instance)!;

        var parameterInfo = method.GetParameters()[0];

        // Act & Assert - null value
        object? result1 = ((object?)null).ApplyParameterMasking(parameterInfo, context);
        result1.Should().BeNull();

        // Act & Assert - null parameterInfo
        var value = "test";
        object? result2 = value.ApplyParameterMasking(null, context);
        result2.Should().Be(value);

        // Act & Assert - both null
        object? result3 = ((object?)null).ApplyParameterMasking(null, context);
        result3.Should().BeNull();
    }

    [Fact]
    public void ApplyParameterMasking_HasParameterMaskAttributeIsTrue_ReturnsDefaultMaskReplacement()
    {
        // Arrange
        var config = new LoggingConfig
        {
            Services = new Dictionary<string, InterceptionConfig>
            {
                ["TestService"] = new() { MaskReplacement = "***CUSTOM***" }
            }
        };

        var context = new MethodLoggingInterceptor.InputContext(
            [],
            [],
            typeof(object),
            config);

        var maskedTypeValue = new MaskedTypeValue();
        var parameterInfo = Substitute.For<ParameterInfo>();

        // Act
        var result = maskedTypeValue.ApplyParameterMasking(parameterInfo, context);

        // Assert
        result.Should().Be("***CUSTOM***");
    }

    [Fact]
    public void ApplyParameterMasking_ParameterInfoHasMaskAttribute_ReturnsMaskAttributeReplacement()
    {
        // Arrange
        var config = new LoggingConfig();
        var context = new MethodLoggingInterceptor.InputContext(
            [],
            [],
            typeof(object),
            config);

        var value = "sensitive-data";

        // Get actual parameter info from a real method that has the MaskAttribute
        var method = typeof(MaskingExtensionsTests).GetMethod(
            nameof(TestMethodWithMaskedParameter),
            BindingFlags.NonPublic | BindingFlags.Instance)!;

        var parameterInfo = method.GetParameters()[0];

        // Act
        var result = value.ApplyParameterMasking(parameterInfo, context);

        // Assert
        result.Should().Be("[REDACTED]");
    }

    [Fact]
    public void ApplyParameterMasking_ConfigServicesNotEmptyButNoMatchingPatterns_ReturnsOriginalValue()
    {
        // Arrange
        var config = new LoggingConfig
        {
            Services = new Dictionary<string, InterceptionConfig>
            {
                ["SomeOtherType"] = new()
                {
                    MaskParameterPatterns = ["differentPattern"],
                    MaskReplacement = "***PATTERN_MASKED***"
                }
            }
        };

        var context = new MethodLoggingInterceptor.InputContext(
            [],
            [],
            typeof(string),
            config);

        var value = "secret123";
        var method = typeof(MaskingExtensionsTests).GetMethod(
            nameof(TestMethodWithPasswordParameter),
            BindingFlags.NonPublic | BindingFlags.Instance)!;

        var parameterInfo = method.GetParameters()[0];

        // Act
        var result = value.ApplyParameterMasking(parameterInfo, context);

        // Assert
        result.Should().BeSameAs(value);
    }

    [Fact]
    public void ApplyParameterMasking_ConfigServicesHasMatchingWildcardPattern_ReturnsMaskReplacement()
    {
        // Arrange
        var config = new LoggingConfig
        {
            Services = new Dictionary<string, InterceptionConfig>
            {
                ["System.*"] = new()
                {
                    MaskParameterPatterns = ["password"],
                    MaskReplacement = "***WILDCARD_MASKED***"
                }
            }
        };

        var context = new MethodLoggingInterceptor.InputContext(
            [],
            [],
            typeof(string),
            config);

        var value = "secret123";
        var method = typeof(MaskingExtensionsTests).GetMethod(
            nameof(TestMethodWithPasswordParameter),
            BindingFlags.NonPublic | BindingFlags.Instance)!;

        var parameterInfo = method.GetParameters()[0];

        // Act
        var result = value.ApplyParameterMasking(parameterInfo, context);

        // Assert
        result.Should().Be("***WILDCARD_MASKED***");
    }

    [Fact]
    public void ApplyParameterMasking_ConfigServicesTryGetValueIsTrue_ReturnsMaskReplacement()
    {
        // Arrange
        var config = new LoggingConfig
        {
            Services = new Dictionary<string, InterceptionConfig>
            {
                ["System.String"] = new()
                {
                    MaskParameterPatterns = ["password"],
                    MaskReplacement = "***EXACT_MATCH_MASKED***"
                }
            }
        };

        var context = new MethodLoggingInterceptor.InputContext(
            [],
            [],
            typeof(string),
            config);

        var value = "secret123";
        var method = typeof(MaskingExtensionsTests).GetMethod(
            nameof(TestMethodWithPasswordParameter),
            BindingFlags.NonPublic | BindingFlags.Instance)!;

        var parameterInfo = method.GetParameters()[0];

        // Act
        var result = value.ApplyParameterMasking(parameterInfo, context);

        // Assert
        result.Should().Be("***EXACT_MATCH_MASKED***");
    }

    [Fact]
    public void ApplyParameterMasking_ParameterMaskReplacementIsNotNull_ReturnsParameterMaskReplacement()
    {
        // Arrange
        var config = new LoggingConfig
        {
            Services = new Dictionary<string, InterceptionConfig>
            {
                ["System.Object"] = new()
                {
                    MaskParameterPatterns = ["password"],
                    MaskReplacement = "***PATTERN_MASKED***"
                }
            }
        };

        var context = new MethodLoggingInterceptor.InputContext(
            [],
            [],
            typeof(object),
            config);

        var value = "secret123";

        // Get actual parameter info from a real method
        var method = typeof(MaskingExtensionsTests).GetMethod(
            nameof(TestMethodWithPasswordParameter),
            BindingFlags.NonPublic | BindingFlags.Instance)!;

        var parameterInfo = method.GetParameters()[0];

        // Act
        var result = value.ApplyParameterMasking(parameterInfo, context);

        // Assert
        result.Should().Be("***PATTERN_MASKED***");
    }

    [Fact]
    public void ApplyParameterMasking_HasMaskedPropertiesIsTrue_ReturnsCreateMaskedCopy()
    {
        // Arrange
        var config = new LoggingConfig();
        var context = new MethodLoggingInterceptor.InputContext(
            [],
            [],
            typeof(object),
            config);

        var value = new TestClassWithMaskedProperties { Username = "john", Password = "secret" };

        // Get actual parameter info from a real method
        var method = typeof(MaskingExtensionsTests).GetMethod(
            nameof(TestMethodWithNormalParameter),
            BindingFlags.NonPublic | BindingFlags.Instance)!;

        var parameterInfo = method.GetParameters()[0];

        // Act
        var result = value.ApplyParameterMasking(parameterInfo, context);

        // Assert
        result.Should().NotBeSameAs(value);
        result.Should().BeOfType<TestClassWithMaskedProperties>();
        var maskedResult = (TestClassWithMaskedProperties)result;
        maskedResult.Username.Should().Be("john");
        maskedResult.Password.Should().Be("***MASKED***");
    }

    [Fact]
    public void ApplyParameterMasking_HasMaskedPropertiesIsFalse_ReturnsOriginalValue()
    {
        // Arrange
        var config = new LoggingConfig();
        var context = new MethodLoggingInterceptor.InputContext(
            [],
            [],
            typeof(object),
            config);

        var value = new TestClassWithoutMaskedProperties { Username = "john", Description = "test" };

        // Get actual parameter info from a real method
        var method = typeof(MaskingExtensionsTests).GetMethod(
            nameof(TestMethodWithNormalParameter),
            BindingFlags.NonPublic | BindingFlags.Instance)!;

        var parameterInfo = method.GetParameters()[0];

        // Act
        var result = value.ApplyParameterMasking(parameterInfo, context);

        // Assert
        result.Should().BeSameAs(value);
    }

    [Fact]
    public void ApplyOutputMasking_ValueIsNull_ReturnsNull()
    {
        // Arrange
        var config = new LoggingConfig();
        object? value = null;
        var declaringType = typeof(string);

        // Act
        var result = value.ApplyOutputMasking(declaringType, config);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void ApplyOutputMasking_DeclaringTypeIsNull_ReturnsOriginalValue()
    {
        // Arrange
        var config = new LoggingConfig();
        var value = "test-value";
        Type? declaringType = null;

        // Act
        var result = value.ApplyOutputMasking(declaringType, config);

        // Assert
        result.Should().BeSameAs(value);
    }

    [Fact]
    public void ApplyOutputMasking_OutputMaskReplacementIsNotNull_ReturnsOutputMaskReplacement()
    {
        // Arrange
        var config = new LoggingConfig
        {
            Services = new Dictionary<string, InterceptionConfig>
            {
                ["System.String"] = new()
                {
                    MaskOutputPatterns = ["*connection*"],
                    MaskReplacement = "***OUTPUT_MASKED***"
                }
            }
        };

        var value = "Server=localhost;Password=secret123;";
        var declaringType = typeof(string);

        // Act
        var result = value.ApplyOutputMasking(declaringType, config);

        // Assert
        result.Should().Be("***OUTPUT_MASKED***");
    }

    [Fact]
    public void ApplyOutputMasking_HasMaskedPropertiesIsTrue_ReturnsCreateMaskedCopy()
    {
        // Arrange
        var config = new LoggingConfig();
        var value = new TestClassWithMaskedProperties { Username = "john", Password = "secret" };
        var declaringType = typeof(string);

        // Act
        var result = value.ApplyOutputMasking(declaringType, config);

        // Assert
        result.Should().NotBeSameAs(value);
        result.Should().BeOfType<TestClassWithMaskedProperties>();
        var maskedResult = (TestClassWithMaskedProperties)result;
        maskedResult.Username.Should().Be("john");
        maskedResult.Password.Should().Be("***MASKED***");
    }

    [Fact]
    public void ApplyOutputMasking_HasMaskedPropertiesIsFalse_ReturnsOriginalValue()
    {
        // Arrange
        var config = new LoggingConfig();
        var value = new TestClassWithoutMaskedProperties { Username = "john", Description = "test" };
        var declaringType = typeof(string);

        // Act
        var result = value.ApplyOutputMasking(declaringType, config);

        // Assert
        result.Should().BeSameAs(value);
    }

    [Fact]
    public void ApplyOutputMasking_MatchesOutputPattern_OutputStringIsNullOrEmpty_ReturnsFalse()
    {
        // Arrange
        var config = new LoggingConfig
        {
            Services = new Dictionary<string, InterceptionConfig>
            {
                ["System.String"] = new()
                {
                    MaskOutputPatterns = ["*connection*"],
                    MaskReplacement = "***OUTPUT_MASKED***"
                }
            }
        };

        var value = ""; // Empty string will cause string.IsNullOrEmpty(outputString) to be true
        var declaringType = typeof(string);

        // Act
        var result = value.ApplyOutputMasking(declaringType, config);

        // Assert
        result.Should().BeSameAs(value); // Should return the original value, not masked
    }

    [Fact]
    public void ApplyParameterMasking_CreateMaskedCopy_CatchException_ReturnsOriginal()
    {
        // Arrange
        var config = new LoggingConfig();
        var context = new MethodLoggingInterceptor.InputContext(
            [],
            [],
            typeof(object),
            config);

        // Use an object that will throw an exception during cloning (no parameterless constructor)
        var value = new TestClassThatThrowsOnCloning("test");
        var method = typeof(MaskingExtensionsTests).GetMethod(
            nameof(TestMethodWithNormalParameter),
            BindingFlags.NonPublic | BindingFlags.Instance)!;

        var parameterInfo = method.GetParameters()[0];

        // Act
        var result = value.ApplyParameterMasking(parameterInfo, context);

        // Assert
        result.Should().BeSameAs(value); // Should return original when cloning fails
    }

    [Fact]
    public void ApplyParameterMasking_CreateClone_PropertyCannotReadOrWrite_SkipsProperty()
    {
        // Arrange
        var config = new LoggingConfig();
        var context = new MethodLoggingInterceptor.InputContext(
            [],
            [],
            typeof(object),
            config);

        var value = new TestClassWithReadOnlyProperty { WritableProperty = "test" };
        var method = typeof(MaskingExtensionsTests).GetMethod(
            nameof(TestMethodWithNormalParameter),
            BindingFlags.NonPublic | BindingFlags.Instance)!;

        var parameterInfo = method.GetParameters()[0];

        // Act
        var result = value.ApplyParameterMasking(parameterInfo, context);

        // Assert
        result.Should().NotBeSameAs(value);
        var clonedResult = (TestClassWithReadOnlyProperty)result;
        clonedResult.WritableProperty.Should().Be("test");
    }

    [Fact]
    public void ApplyParameterMasking_SetMaskText_ConfigMaskTextNotNull_UsesConfigMaskText()
    {
        // Arrange
        var config = new LoggingConfig
        {
            Services = new Dictionary<string, InterceptionConfig>
            {
                ["FlexKit.Logging.Tests.Formatting.Utils.MaskingExtensionsTests+TestClassWithMaskedPropertiesAndConfig"] = new()
                {
                    MaskPropertyPatterns = ["Password"],
                    MaskReplacement = "***CONFIG_MASKED***"
                }
            }
        };

        var context = new MethodLoggingInterceptor.InputContext(
            [],
            [],
            typeof(object),
            config);

        var value = new TestClassWithMaskedPropertiesAndConfig { Username = "john", Password = "secret" };
        var method = typeof(MaskingExtensionsTests).GetMethod(
            nameof(TestMethodWithNormalParameter),
            BindingFlags.NonPublic | BindingFlags.Instance)!;

        var parameterInfo = method.GetParameters()[0];

        // Act
        var result = value.ApplyParameterMasking(parameterInfo, context);

        // Assert
        result.Should().NotBeSameAs(value);
        var maskedResult = (TestClassWithMaskedPropertiesAndConfig)result;
        maskedResult.Password.Should().Be("***CONFIG_MASKED***"); // Should use config mask text
    }

    [Fact]
    public void ApplyParameterMasking_GetPropertyMaskReplacement_MatchesPatternCalled()
    {
        // Arrange
        var config = new LoggingConfig
        {
            Services = new Dictionary<string, InterceptionConfig>
            {
                ["FlexKit.Logging.Tests.Formatting.Utils.MaskingExtensionsTests+TestClassWithMaskedPropertiesForPattern"] = new()
                {
                    MaskPropertyPatterns = ["*word*"], // Will match "Password"
                    MaskReplacement = "***PATTERN_MATCHED***"
                }
            }
        };

        var context = new MethodLoggingInterceptor.InputContext(
            [],
            [],
            typeof(object),
            config);

        var value = new TestClassWithMaskedPropertiesForPattern { Username = "john", Password = "secret" };
        var method = typeof(MaskingExtensionsTests).GetMethod(
            nameof(TestMethodWithNormalParameter),
            BindingFlags.NonPublic | BindingFlags.Instance)!;

        var parameterInfo = method.GetParameters()[0];

        // Act
        var result = value.ApplyParameterMasking(parameterInfo, context);

        // Assert
        result.Should().NotBeSameAs(value);
        var maskedResult = (TestClassWithMaskedPropertiesForPattern)result;
        maskedResult.Password.Should().Be("***PATTERN_MATCHED***");
    }

    [Fact]
    public void ApplyParameterMasking_MatchesPattern_NameOrPatternIsNullOrEmpty_ReturnsFalse()
    {
        // Arrange
        var config = new LoggingConfig
        {
            Services = new Dictionary<string, InterceptionConfig>
            {
                ["System.String"] = new()
                {
                    MaskParameterPatterns = [""], // Empty pattern
                    MaskReplacement = "***MASKED***"
                }
            }
        };

        var context = new MethodLoggingInterceptor.InputContext(
            [],
            [],
            typeof(string),
            config);

        var value = "secret123";
        var method = typeof(MaskingExtensionsTests).GetMethod(
            nameof(TestMethodWithPasswordParameter),
            BindingFlags.NonPublic | BindingFlags.Instance)!;

        var parameterInfo = method.GetParameters()[0];

        // Act
        var result = value.ApplyParameterMasking(parameterInfo, context);

        // Assert
        result.Should().BeSameAs(value); // Should not match an empty pattern
    }

    [Fact]
    public void ApplyParameterMasking_MatchesPattern_PatternStartsAndEndsWithWildcard_ContainsMatch()
    {
        // Arrange
        var config = new LoggingConfig
        {
            Services = new Dictionary<string, InterceptionConfig>
            {
                ["System.String"] = new()
                {
                    MaskParameterPatterns = ["*word*"], // Contains pattern
                    MaskReplacement = "***CONTAINS_MATCH***"
                }
            }
        };

        var context = new MethodLoggingInterceptor.InputContext(
            [],
            [],
            typeof(string),
            config);

        var value = "secret123";
        var method = typeof(MaskingExtensionsTests).GetMethod(
            nameof(TestMethodWithPasswordParameter),
            BindingFlags.NonPublic | BindingFlags.Instance)!;

        var parameterInfo = method.GetParameters()[0];

        // Act
        var result = value.ApplyParameterMasking(parameterInfo, context);

        // Assert
        result.Should().Be("***CONTAINS_MATCH***"); // "password" contains "word"
    }

    [Fact]
    public void ApplyParameterMasking_MatchesPattern_PatternStartsWithWildcard_EndsWithMatch()
    {
        // Arrange
        var config = new LoggingConfig
        {
            Services = new Dictionary<string, InterceptionConfig>
            {
                ["System.String"] = new()
                {
                    MaskParameterPatterns = ["*word"], // Ends with a pattern
                    MaskReplacement = "***ENDS_WITH_MATCH***"
                }
            }
        };

        var context = new MethodLoggingInterceptor.InputContext(
            [],
            [],
            typeof(string),
            config);

        var value = "secret123";
        var method = typeof(MaskingExtensionsTests).GetMethod(
            nameof(TestMethodWithPasswordParameter),
            BindingFlags.NonPublic | BindingFlags.Instance)!;

        var parameterInfo = method.GetParameters()[0];

        // Act
        var result = value.ApplyParameterMasking(parameterInfo, context);

        // Assert
        result.Should().Be("***ENDS_WITH_MATCH***"); // "password" ends with "word"
    }

    [Fact]
    public void ApplyParameterMasking_MatchesPattern_PatternEndsWithWildcard_StartsWithMatch()
    {
        // Arrange
        var config = new LoggingConfig
        {
            Services = new Dictionary<string, InterceptionConfig>
            {
                ["System.String"] = new()
                {
                    MaskParameterPatterns = ["pass*"], // Starts with a pattern
                    MaskReplacement = "***STARTS_WITH_MATCH***"
                }
            }
        };

        var context = new MethodLoggingInterceptor.InputContext(
            [],
            [],
            typeof(string),
            config);

        var value = "secret123";
        var method = typeof(MaskingExtensionsTests).GetMethod(
            nameof(TestMethodWithPasswordParameter),
            BindingFlags.NonPublic | BindingFlags.Instance)!;

        var parameterInfo = method.GetParameters()[0];

        // Act
        var result = value.ApplyParameterMasking(parameterInfo, context);

        // Assert
        result.Should().Be("***STARTS_WITH_MATCH***"); // "password" starts with "pass"
    }

    [Fact]
    public void ApplyOutputMasking_MatchesPattern_CallsMatchesPatternMethod()
    {
        // Arrange
        var config = new LoggingConfig
        {
            Services = new Dictionary<string, InterceptionConfig>
            {
                ["System.String"] = new()
                {
                    MaskOutputPatterns = ["*test*"], // This will call MatchesPattern since it's not "*connection*"
                    MaskReplacement = "***OUTPUT_MASKED***"
                }
            }
        };

        var value = "This contains test data"; // Will match the pattern *test*
        var declaringType = typeof(string);

        // Act
        var result = value.ApplyOutputMasking(declaringType, config);

        // Assert
        result.Should().Be("***OUTPUT_MASKED***"); // Should be masked because MatchesPattern returns true
    }

    [Fact]
    public void ApplyParameterMasking_MatchesPattern_PatternWithoutWildcardNotExactMatch_ReturnsFalse()
    {
        // Arrange
        var config = new LoggingConfig
        {
            Services = new Dictionary<string, InterceptionConfig>
            {
                ["System.String"] = new()
                {
                    MaskParameterPatterns = ["exactmatch"], // No wildcards and won't match "password"
                    MaskReplacement = "***MASKED***"
                }
            }
        };

        var context = new MethodLoggingInterceptor.InputContext(
            [],
            [],
            typeof(string),
            config);

        var value = "secret123";
        var method = typeof(MaskingExtensionsTests).GetMethod(
            nameof(TestMethodWithPasswordParameter),
            BindingFlags.NonPublic | BindingFlags.Instance)!;

        var parameterInfo = method.GetParameters()[0];

        // Act
        var result = value.ApplyParameterMasking(parameterInfo, context);

        // Assert
        result.Should().BeSameAs(value); // Should return the original because !pattern.Contains('*') and no exact match
    }

    [Fact]
    public void ApplyParameterMasking_FindMatchingConfigurationForType_TypeFullNameIsNull_ReturnsNull()
    {
        // Create a mock type or use reflection to get a type with null FullName
        // Generic type parameters in open generic types can have null FullName
        var openGeneric = typeof(Dictionary<,>);
        var typeParams = openGeneric.GetGenericArguments();
        var typeWithNullFullName = typeParams[0]; // Generic type parameters have null FullName

        var config = new LoggingConfig
        {
            Services = new Dictionary<string, InterceptionConfig>
            {
                ["SomeType"] = new()
                {
                    MaskParameterPatterns = ["password"],
                    MaskReplacement = "***PATTERN_MASKED***"
                }
            }
        };

        var context = new MethodLoggingInterceptor.InputContext(
            [],
            [],
            typeWithNullFullName,
            config);

        var value = "secret123";
        var method = typeof(MaskingExtensionsTests).GetMethod(
            nameof(TestMethodWithPasswordParameter),
            BindingFlags.NonPublic | BindingFlags.Instance)!;

        var parameterInfo = method.GetParameters()[0];

        // Act
        var result = value.ApplyParameterMasking(parameterInfo, context);

        // Assert
        result.Should().BeSameAs(value);
    }
    
    [Fact]
    public void ApplyParameterMasking_CreateMaskedCopy_MaskedPropertiesLengthZero_ReturnsOriginal()
    {
        var config = new LoggingConfig();
        var context = new MethodLoggingInterceptor.InputContext([], [], typeof(object), config);

        // We need to manually manipulate the cache to create this scenario
        // Clear the cache first
        var maskedPropsCache = typeof(MaskingExtensions)
            .GetField("_maskedPropertiesCache", BindingFlags.NonPublic | BindingFlags.Static)?
            .GetValue(null) as System.Collections.IDictionary;
        maskedPropsCache?.Clear();

        var typeHasMaskedPropsCache = typeof(MaskingExtensions)
            .GetField("_typeHasMaskedPropsCache", BindingFlags.NonPublic | BindingFlags.Static)?
            .GetValue(null) as System.Collections.IDictionary;
        typeHasMaskedPropsCache?.Clear();

        // Add inconsistent cache entries
        var testType = typeof(TestClassWithMaskedProperties);
        typeHasMaskedPropsCache?.Add(testType, true); // HasMaskedProperties returns true
        maskedPropsCache?.Add(testType, Array.Empty<PropertyInfo>()); // GetMaskedProperties returns empty array

        var value = new TestClassWithMaskedProperties { Username = "john", Password = "secret" };
        var method = typeof(MaskingExtensionsTests).GetMethod(nameof(TestMethodWithNormalParameter), BindingFlags.NonPublic | BindingFlags.Instance)!;
        var parameterInfo = method.GetParameters()[0];

        var result = value.ApplyParameterMasking(parameterInfo, context);

        result.Should().BeSameAs(value); // Should return original when MaskedProperties.Length == 0
    }
    
    [Fact]
    public void CreateClone_WhenCloneIsNull_ReturnsOriginal()
    {
        // Arrange
        var config = new LoggingConfig();
        var nullableType = typeof(int?);
        var original = new object(); // Any object as original
        var maskedProperties = Array.Empty<PropertyInfo>(); // Empty array
        var defaultMaskText = "***MASKED***";

        // Create MaskingContext using reflection
        var maskingContextType = typeof(MaskingExtensions).GetNestedTypes(BindingFlags.NonPublic)
            .First(t => t.Name == "MaskingContext");
    
        var context = Activator.CreateInstance(maskingContextType, 
            original, config, nullableType, maskedProperties, defaultMaskText);

        // Get the private CreateClone method
        var createCloneMethod = typeof(MaskingExtensions)
            .GetMethod("CreateClone", BindingFlags.NonPublic | BindingFlags.Static);

        // Act
        var result = createCloneMethod!.Invoke(null, [context]);

        // Assert
        result.Should().BeSameAs(original); // Should return original when clone is null
    }

    // Class that throws an exception during cloning (no parameterless constructor)
    private class TestClassThatThrowsOnCloning(string value)
    {
        [Mask] public string Value { get; set; } = value;
    }

// Abstract class wrapper that cannot be instantiated

// Class with read-only property
    private class TestClassWithReadOnlyProperty
    {
        public string ReadOnlyProperty { get; } = "readonly";
        public string WritableProperty { get; set; } = string.Empty;

        [Mask] public string MaskedProperty { get; set; } = "masked";
    }

// Class with masked properties for config testing
    private class TestClassWithMaskedPropertiesAndConfig
    {
        public string Username { get; set; } = string.Empty;

        [Mask] public string Password { get; set; } = string.Empty;
    }

// Class with masked properties for pattern testing
    private class TestClassWithMaskedPropertiesForPattern
    {
        public string Username { get; set; } = string.Empty;

        [Mask] public string Password { get; set; } = string.Empty;
    }

// Helper method for testing parameter with MaskAttribute
    private void TestMethodWithMaskedParameter([Mask(Replacement = "[REDACTED]")] string maskedParam)
    {
        // This method is only used for getting parameter info in tests
    }

    // Helper method for testing parameter name pattern matching
    private void TestMethodWithPasswordParameter(string password)
    {
        // This method is only used for getting parameter info in tests
    }

    // Helper method for testing normal parameter (no attributes)
    private void TestMethodWithNormalParameter(object normalParam)
    {
        // This method is only used for getting parameter info in tests
    }

    [Mask]
    private class MaskedTypeValue
    {
        public string Value { get; set; } = "test";
    }

    private class TestClassWithMaskedProperties
    {
        public string Username { get; set; } = string.Empty;

        [Mask] public string Password { get; set; } = string.Empty;
    }

    private class TestClassWithoutMaskedProperties
    {
        public string Username { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }
}
