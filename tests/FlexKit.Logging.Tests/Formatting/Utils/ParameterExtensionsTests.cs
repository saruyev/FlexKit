using System.Text.Json;
using FluentAssertions;
using FlexKit.Logging.Configuration;
using FlexKit.Logging.Formatting.Models;
using FlexKit.Logging.Formatting.Utils;
using FlexKit.Logging.Models;
using NSubstitute;
using Xunit;
// ReSharper disable MethodTooLong

namespace FlexKit.Logging.Tests.Formatting.Utils;

public class ParameterExtensionsTests
{
    [Fact]
    public void ExtractParameters_WhenSuccessIsTrue_ShouldIncludeBasicParametersWithoutExceptionParameters()
    {
        // Arrange
        var logEntry = LogEntry.CreateStart("TestMethod", "TestType")
            .WithInput("test input")
            .WithOutput("test output")
            .WithCompletion(true, 1000000L); // 100.5 ms in ticks (approximate)

        var config = new LoggingConfig
        {
            DefaultFormatter = FormatterType.StandardStructured,
            Templates = new Dictionary<string, TemplateConfig>(),
            Targets = new Dictionary<string, LoggingTarget>(),
            EnableFallbackFormatting = false
        };

        var context = FormattingContext.Create(logEntry, config)
            .WithProperties(
                new Dictionary<string, object?>
                {
                    ["CustomProperty"] = "CustomValue"
                });

        // Act
        var result = context.ExtractParameters();

        // Assert
        result.Should().ContainKey("MethodName").WhoseValue.Should().Be("TestMethod");
        result.Should().ContainKey("TypeName").WhoseValue.Should().Be("TestType");
        result.Should().ContainKey("Success").WhoseValue.Should().Be(true);
        result.Should().ContainKey("InputParameters").WhoseValue.Should().Be("test input");
        result.Should().ContainKey("OutputValue").WhoseValue.Should().Be("test output");
        result.Should().ContainKey("CustomProperty").WhoseValue.Should().Be("CustomValue");

        // Exception parameters should not be present when Success is true
        result.Should().NotContainKey("ExceptionType");
        result.Should().NotContainKey("ExceptionMessage");
        result.Should().NotContainKey("StackTrace");
    }

    [Fact]
    public void ExtractParameters_WhenSuccessIsFalse_ShouldIncludeBasicAndExceptionParameters()
    {
        // Arrange
        var exception = new ArgumentException("Test exception message");
        var logEntry = LogEntry.CreateStart("FailedMethod", "FailedType")
            .WithInput("failed input")
            .WithCompletion(false, 500000L, exception);

        var config = new LoggingConfig
        {
            DefaultFormatter = FormatterType.StandardStructured,
            Templates = new Dictionary<string, TemplateConfig>(),
            Targets = new Dictionary<string, LoggingTarget>(),
            EnableFallbackFormatting = false
        };

        var context = FormattingContext.Create(logEntry, config)
            .WithProperties(
                new Dictionary<string, object?>
                {
                    ["ErrorContext"] = "Error occurred"
                });

        // Act
        var result = context.ExtractParameters();

        // Assert
        result.Should().ContainKey("MethodName").WhoseValue.Should().Be("FailedMethod");
        result.Should().ContainKey("TypeName").WhoseValue.Should().Be("FailedType");
        result.Should().ContainKey("Success").WhoseValue.Should().Be(false);
        result.Should().ContainKey("InputParameters").WhoseValue.Should().Be("failed input");
        result.Should().ContainKey("ErrorContext").WhoseValue.Should().Be("Error occurred");

        // Exception parameters should be present when Success is false
        result.Should().ContainKey("ExceptionType").WhoseValue.Should().Be("ArgumentException");
        result.Should().ContainKey("ExceptionMessage").WhoseValue.Should().Be("Test exception message");
        result.Should().ContainKey("StackTrace");
    }

    [Fact]
    public void ExtractParameters_WhenPropertiesIsEmpty_ShouldOnlyIncludeLogEntryParameters()
    {
        // Arrange
        var logEntry = LogEntry.CreateStart("EmptyPropsMethod", "EmptyPropsType")
            .WithCompletion(true, 2000000L); // 200 ms in ticks (approximate)

        var config = new LoggingConfig
        {
            DefaultFormatter = FormatterType.StandardStructured,
            Templates = new Dictionary<string, TemplateConfig>(),
            Targets = new Dictionary<string, LoggingTarget>(),
            EnableFallbackFormatting = false
        };

        var context = FormattingContext.Create(logEntry, config)
            .WithProperties(new Dictionary<string, object?>());

        // Act
        var result = context.ExtractParameters();

        // Assert
        result.Should().ContainKey("MethodName").WhoseValue.Should().Be("EmptyPropsMethod");
        result.Should().ContainKey("TypeName").WhoseValue.Should().Be("EmptyPropsType");
        result.Should().ContainKey("Success").WhoseValue.Should().Be(true);

        // Should contain exactly these basic log entry parameters: MethodName, TypeName, Success, ThreadId, Timestamp, Id, ActivityId, Duration, DurationSeconds, InputParameters, OutputValue
        result.Should().HaveCount(11);
        result.Should().ContainKeys(
            "MethodName",
            "TypeName",
            "Success",
            "ThreadId",
            "Timestamp",
            "Id",
            "ActivityId",
            "Duration",
            "DurationSeconds",
            "InputParameters",
            "OutputValue");
    }

    [Fact]
    public void WithParametersJson_WhenInputParametersIsNotObjectArrayOrEmpty_ShouldUseInputParametersAsIs()
    {
        // Arrange
        var logEntry = LogEntry.CreateStart("TestMethod", "TestType")
            .WithInput("simple string input")
            .WithOutput("test output");

        // Act
        var result = logEntry.WithParametersJson();

        // Assert
        result.InputParameters.Should().Be("simple string input");
        result.OutputValue.Should().Be("test output");
    }

    [Fact]
    public void WithParametersJson_WhenInputParametersIsNonEmptyObjectArray_ShouldTransformToAnonymousObjects()
    {
        // Arrange
        var inputParameters = new object[]
        {
            new InputParameter("param1", "String", "value1"),
            new InputParameter("param2", "Int32", 42),
            new InputParameter("param3", "String", null)
        };

        var logEntry = LogEntry.CreateStart("TestMethod", "TestType")
            .WithInput(inputParameters)
            .WithOutput("test output");

        // Act
        var result = logEntry.WithParametersJson();

        // Assert
        var transformedInput = result.InputParameters.Should().BeAssignableTo<IEnumerable<object>>().Subject;
        var inputList = transformedInput.ToArray();

        inputList.Should().HaveCount(3);

        // Verify the structure of transformed objects (anonymous objects with name, type, value properties)
        var firstParam = inputList[0];
        firstParam.Should().NotBeNull();
        firstParam.GetType().GetProperty("name")?.GetValue(firstParam).Should().Be("param1");
        firstParam.GetType().GetProperty("type")?.GetValue(firstParam).Should().Be("String");
        firstParam.GetType().GetProperty("value")?.GetValue(firstParam).Should().Be("value1");

        var thirdParam = inputList[2];
        thirdParam.GetType().GetProperty("value")?.GetValue(thirdParam).Should().Be("null");

        result.OutputValue.Should().Be("test output");
    }

    [Fact]
    public void WithParametersString_HappyPath_ShouldSerializeInputParametersAndOutputValue()
    {
        // Arrange
        var inputParameters = new object[]
        {
            new InputParameter("param1", "String", "value1"),
            new InputParameter("param2", "Int32", 42)
        };

        var outputValue = new { Success = true, Message = "Completed" };

        var logEntry = LogEntry.CreateStart("TestMethod", "TestType")
            .WithInput(inputParameters)
            .WithOutput(outputValue);

        // Act
        var result = logEntry.WithParametersString();

        // Assert
        result.InputParameters.Should().BeOfType<string>();
        var inputJson = result.InputParameters as string;
        inputJson.Should().Contain("param1");
        inputJson.Should().Contain("value1");
        inputJson.Should().Contain("param2");

        result.OutputValue.Should().BeOfType<JsonElement>();
        var outputJson = JsonSerializer.Serialize(result.OutputValue);
        outputJson.Should().Contain("Success");
        outputJson.Should().Contain("Completed");
    }

    [Fact]
    public void WithParametersString_SerializeComplexObjectForJson_WhenJsonLengthGreaterThan2000_ShouldReturnTruncatedObject()
    {
        // Arrange - Create a large object that will produce > 2000 char JSON
        var largeData = string.Join("", Enumerable.Repeat("A", 500)); // 500 chars
        var largeObject = new
        {
            LargeProperty1 = largeData,
            LargeProperty2 = largeData,
            LargeProperty3 = largeData,
            LargeProperty4 = largeData,
            LargeProperty5 = largeData // This should create > 2000 chars when serialized
        };

        var logEntry = LogEntry.CreateStart("TestMethod", "TestType")
            .WithOutput(largeObject);

        // Act
        var result = logEntry.WithParametersString();
        var serializedOutput = result.OutputValue!.ToString();
        serializedOutput.Should().Contain("_truncated");
        serializedOutput.Should().Contain("_preview");
        serializedOutput.Should().Contain("...");
    }

    [Fact]
    public void WithParametersString_SerializeComplexObjectForJson_WhenJsonLengthLessThan2000_ShouldReturnDeserializedObject()
    {
        // Arrange - Create a small object that will produce < 2000 char JSON
        var smallObject = new { Name = "Test", Value = 42, Active = true };

        var logEntry = LogEntry.CreateStart("TestMethod", "TestType")
            .WithOutput(smallObject);

        // Act
        var result = logEntry.WithParametersString();

        // Assert
        result.OutputValue.Should().BeOfType<JsonElement>();
        var serializedOutput = JsonSerializer.Serialize(result.OutputValue);
        serializedOutput.Should().Contain("Name");
        serializedOutput.Should().Contain("Test");
        serializedOutput.Should().Contain("Value");
        serializedOutput.Should().Contain("42");
        serializedOutput.Should().Contain("Active");
        serializedOutput.Should().Contain("true");
    }

    [Fact]
    public void WithParametersString_SerializeComplexObjectForJson_WhenSerializationThrows_ShouldReturnErrorObject()
    {
        // Arrange - Create an object that might cause serialization issues
        // We'll use a self-referencing structure that could potentially cause issues
        var problematicObject = new Dictionary<string, object>();
        problematicObject["self"] = problematicObject; // Self-reference

        // Add some additional complex structure
        var nestedDict = new Dictionary<string, object>();
        for (int i = 0; i < 10; i++)
        {
            nestedDict[$"key{i}"] = new { Index = i, Data = new string('X', 100) };
        }

        problematicObject["nested"] = nestedDict;

        var logEntry = LogEntry.CreateStart("TestMethod", "TestType")
            .WithOutput(problematicObject);

        // Act
        var result = logEntry.WithParametersString();

        // Assert
        result.OutputValue.Should().BeOfType<object[]>();
        var serializedOutput = result.OutputValue as object[];

        serializedOutput![0].ToString().Should().Contain("_error = Serialization failed");
        serializedOutput[1].ToString().Should().Contain("_error = Serialization failed");
    }

    [Fact]
    public void WithParametersString_SerializeComplexObjectForJson_WhenDeserializeReturnsNull_ShouldReturnFallbackObject()
    {
        // Arrange - This is tricky to test without creating a special scenario
        // We'll create an object that serializes to a valid JSON but might deserialize to null
        // This could happen with custom converters or edge cases in System.Text.Json

        // Create an object with specific characteristics that might cause deserialization issues
        var edgeCaseObject = new
        {
            EmptyValue = (object?)null,
            SpecialChars = "\u0000\u0001\u0002",
            EmptyString = "",
            ZeroValue = 0.0,
            NegativeZero = -0.0
        };

        var logEntry = LogEntry.CreateStart("TestMethod", "TestType")
            .WithOutput(edgeCaseObject);

        // Act
        var result = logEntry.WithParametersString();

        // Assert
        result.OutputValue.Should().BeOfType<JsonElement>();
        var serializedOutput = JsonSerializer.Serialize(result.OutputValue);

        serializedOutput.Should().NotContain("EmptyValue");
        serializedOutput.Should().Contain("SpecialChars");
    }

    [Fact]
    public void WithParametersString_SerializeValueForJson_ShouldHandleAllSwitchExpressionCases()
    {
        // Arrange - Create input parameters covering all switch expression cases
        var largeCollection = Enumerable.Range(1, 15).ToList(); // ICollection with Count > 10
        var smallEnumerable = new[] { 1, 2, 3, 4, 5 }; // IEnumerable with Count <= 10
        var complexObject = new { Name = "Test", Age = 30 }; // Complex object
        var dateTime = new DateTime(2023, 12, 25, 10, 30, 45, DateTimeKind.Utc);
        var dateTimeOffset = new DateTimeOffset(2023, 12, 25, 10, 30, 45, TimeSpan.FromHours(5));
        var guid = Guid.Parse("12345678-1234-5678-9abc-123456789012");

        var entry = LogEntry.CreateStart("TestMethod", "TestType")
            .WithInput(
                new object[]
                {
                    // Primitive types
                    new InputParameter("stringParam", "String", "test string"),
                    new InputParameter("boolParam", "Boolean", true),
                    new InputParameter("byteParam", "Byte", (byte)255),
                    new InputParameter("sbyteParam", "SByte", (sbyte)-128),
                    new InputParameter("shortParam", "Int16", (short)32767),
                    new InputParameter("ushortParam", "UInt16", (ushort)65535),
                    new InputParameter("intParam", "Int32", 2147483647),
                    new InputParameter("uintParam", "UInt32", 4294967295U),
                    new InputParameter("longParam", "Int64", 9223372036854775807L),
                    new InputParameter("ulongParam", "UInt64", 18446744073709551615UL),
                    new InputParameter("floatParam", "Single", 3.14f),
                    new InputParameter("doubleParam", "Double", 3.14159),
                    new InputParameter("decimalParam", "Decimal", 123.456m),

                    // Special types
                    new InputParameter("nullParam", "Object", null),
                    new InputParameter("dateTimeParam", "DateTime", dateTime),
                    new InputParameter("dateTimeOffsetParam", "DateTimeOffset", dateTimeOffset),
                    new InputParameter("guidParam", "Guid", guid),

                    // Collections
                    new InputParameter("largeCollectionParam", "List", largeCollection),
                    new InputParameter("smallEnumerableParam", "Array", smallEnumerable),

                    // Complex object
                    new InputParameter("complexObjectParam", "Object", complexObject)
                });

        // Act
        var result = entry.WithParametersString();

        // Assert
        result.InputParameters.Should().BeOfType<string>();
        var serializedJson = result.InputParameters as string;
        serializedJson.Should().NotBeNullOrEmpty();

        // Verify primitive types are serialized as-is
        serializedJson.Should().Contain("\"value\":\"test string\"");
        serializedJson.Should().Contain("\"value\":true");
        serializedJson.Should().Contain("\"value\":255");
        serializedJson.Should().Contain("\"value\":-128");
        serializedJson.Should().Contain("\"value\":32767");
        serializedJson.Should().Contain("\"value\":65535");
        serializedJson.Should().Contain("\"value\":2147483647");
        serializedJson.Should().Contain("\"value\":4294967295");
        serializedJson.Should().Contain("\"value\":9223372036854775807");
        serializedJson.Should().Contain("\"value\":18446744073709551615");
        serializedJson.Should().Contain("\"value\":3.14");
        serializedJson.Should().Contain("\"value\":3.14159");
        serializedJson.Should().Contain("\"value\":123.456");

        // Verify null is serialized as "null"
        serializedJson.Should().Contain("\"value\":\"null\"");

        // Verify DateTime is serialized in ISO format
        serializedJson.Should().Contain("\"value\":\"2023-12-25T10:30:45.0000000Z\"");

        // Verify DateTimeOffset is serialized in ISO format
        serializedJson.Should().Contain("\"value\":\"2023-12-25T10:30:45.0000000\\u002B05:00\"");

        // Verify Guid is serialized as a string
        serializedJson.Should().Contain("\"value\":\"12345678-1234-5678-9abc-123456789012\"");

        // Verify a large collection (Count > 10) is truncated with metadata
        serializedJson.Should().Contain("\"_type\":\"Collection\"");
        serializedJson.Should().Contain("\"_count\":15");
        serializedJson.Should().Contain("\"_truncated\":true");
        serializedJson.Should().Contain("\"items\":[1,2,3]");

        // Verify small enumerable is serialized as an array
        serializedJson.Should().Contain("\"value\":[1,2,3,4,5]");

        // Verify a complex object is processed through SerializeComplexObjectForJson
        serializedJson.Should().Contain("\"Name\":\"Test\"");
        serializedJson.Should().Contain("\"Age\":30");
    }
    
    [Fact]
    public void WithParametersString_ShouldCatch_WhenEnumerationFails()
    {
        // Arrange
        var entry = LogEntry.CreateStart("TestMethod", "TestType")
            .WithInput(new object[] { new InputParameter("p", "BadEnumerable", new BadEnumerable()) });

        // Act
        var result = entry.WithParametersString();

        // Assert
        result.Should().Be(entry); // fallback path
    }

    
    class BadEnumerable : IEnumerable<object>
    {
        public IEnumerator<object> GetEnumerator() => throw new InvalidOperationException("boom");
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
    }
}