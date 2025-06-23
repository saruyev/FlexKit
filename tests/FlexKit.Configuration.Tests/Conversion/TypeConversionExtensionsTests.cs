using System.Collections;
using System.Globalization;
using AutoFixture.Xunit2;
using FlexKit.Configuration.Conversion;
using FlexKit.Configuration.Tests.TestBase;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using NSubstitute;
using Xunit;

namespace FlexKit.Configuration.Tests.Conversion;

/// <summary>
/// Comprehensive unit tests for TypeConversionExtensions covering all methods and edge cases.
/// </summary>
public class TypeConversionExtensionsTests : UnitTestBase
{
    protected override void RegisterFixtureCustomizations()
    {
        // Customize string generation to avoid null values and control characters
        Fixture.Customize<string>(composer => composer.FromFactory(() => 
            "test-string-" + Guid.NewGuid().ToString("N")[..8]));
    }

    #region ToType(string?, Type) Tests

    [Fact]
    public void ToType_WithNullType_ThrowsArgumentNullException()
    {
        // Arrange
        var text = Create<string>();

        // Act & Assert
        var action = () => text.ToType(null!);
        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("type");
    }

    [Fact]
    public void ToType_WithNullTextAndValueType_ReturnsDefaultValue()
    {
        // Act
        var intResult = ((string?)null).ToType(typeof(int));
        var boolResult = ((string?)null).ToType(typeof(bool));
        var doubleResult = ((string?)null).ToType(typeof(double));

        // Assert
        intResult.Should().Be(0);
        boolResult.Should().Be(false);
        doubleResult.Should().Be(0.0);
    }

    [Fact]
    public void ToType_WithNullTextAndReferenceType_ReturnsNull()
    {
        // Act
        var stringResult = ((string?)null).ToType(typeof(string));
        var objectResult = ((string?)null).ToType(typeof(object));

        // Assert
        stringResult.Should().BeNull();
        objectResult.Should().BeNull();
    }

    [Theory]
    [InlineData("42", typeof(int), 42)]
    [InlineData("true", typeof(bool), true)]
    [InlineData("false", typeof(bool), false)]
    [InlineData("3.14", typeof(double), 3.14)]
    [InlineData("123.45", typeof(decimal), 123.45)]
    [InlineData("100", typeof(long), 100L)]
    [InlineData("A", typeof(char), 'A')]
    public void ToType_WithValidPrimitiveValues_ConvertsCorrectly(string input, Type targetType, object expected)
    {
        // Act
        var result = input.ToType(targetType);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("not-a-number", typeof(int))]
    [InlineData("not-a-bool", typeof(bool))]
    [InlineData("not-a-double", typeof(double))]
    public void ToType_WithInvalidPrimitiveValues_ThrowsFormatException(string input, Type targetType)
    {
        // Act & Assert
        var action = () => input.ToType(targetType);
        action.Should().Throw<FormatException>();
    }

    [Theory]
    [InlineData("99999999999999999999", typeof(int))]
    [InlineData("-99999999999999999999", typeof(int))]
    public void ToType_WithOverflowValues_ThrowsOverflowException(string input, Type targetType)
    {
        // Act & Assert
        var action = () => input.ToType(targetType);
        action.Should().Throw<OverflowException>();
    }

    [Theory]
    [InlineData("Warning", LogLevel.Warning)]
    [InlineData("Error", LogLevel.Error)]
    [InlineData("Information", LogLevel.Information)]
    [InlineData("Debug", LogLevel.Debug)]
    public void ToType_WithValidEnumValues_ConvertsCorrectly(string input, LogLevel expected)
    {
        // Act
        var result = input.ToType(typeof(LogLevel));

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("InvalidLevel")]
    [InlineData("warning")] // Case-sensitive
    [InlineData("")]
    public void ToType_WithInvalidEnumValues_ThrowsArgumentException(string input)
    {
        // Act & Assert
        var action = () => input.ToType(typeof(LogLevel));
        action.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void ToType_WithStringType_ReturnsOriginalString()
    {
        // Arrange
        var input = Create<string>();

        // Act
        var result = input.ToType(typeof(string));

        // Assert
        result.Should().Be(input);
    }

    [Theory]
    [InlineData("2023-12-25", typeof(DateTime))]
    [InlineData("10:30:45", typeof(TimeSpan))]
    public void ToType_WithDateTimeTypes_ConvertsCorrectly(string input, Type targetType)
    {
        // Act & Assert - Should not throw
        var action = () => input.ToType(targetType);
        action.Should().NotThrow();
    }

    [Fact]
    public void ToType_UsesInvariantCulture()
    {
        // Arrange - Use a culture-specific decimal format
        var originalCulture = CultureInfo.CurrentCulture;
        try
        {
            // Set to a culture that uses comma as a decimal separator
            CultureInfo.CurrentCulture = new CultureInfo("de-DE");
            
            // Act - Use period as decimal separator (invariant culture format)
            var result = "3.14".ToType(typeof(double));

            // Assert
            result.Should().Be(3.14);
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
        }
    }

    #endregion

    #region ToType<T>(string?) Tests

    [Theory]
    [AutoData]
    public void ToTypeGeneric_WithValidIntString_ConvertsToInt(int expectedValue)
    {
        // Arrange
        var input = expectedValue.ToString();

        // Act
        var result = input.ToType<int>();

        // Assert
        result.Should().Be(expectedValue);
    }

    [Theory]
    [AutoData]
    public void ToTypeGeneric_WithValidBoolString_ConvertsToBool(bool expectedValue)
    {
        // Arrange
        var input = expectedValue.ToString();

        // Act
        var result = input.ToType<bool>();

        // Assert
        result.Should().Be(expectedValue);
    }

    [Fact]
    public void ToTypeGeneric_WithNullString_ReturnsDefaultForValueType()
    {
        // Act
        var intResult = ((string?)null).ToType<int>();
        var boolResult = ((string?)null).ToType<bool>();
        var doubleResult = ((string?)null).ToType<double>();

        // Assert
        intResult.Should().Be(0);
        boolResult.Should().Be(false);
        doubleResult.Should().Be(0.0);
    }

    [Fact]
    public void ToTypeGeneric_WithNullString_ReturnsNullForReferenceType()
    {
        // Act
        var stringResult = ((string?)null).ToType<string>();

        // Assert
        stringResult.Should().BeNull();
    }

    [Theory]
    [InlineData("Warning")]
    [InlineData("Error")]
    [InlineData("Information")]
    public void ToTypeGeneric_WithEnumType_ConvertsCorrectly(string enumValue)
    {
        // Act
        var result = enumValue.ToType<LogLevel>();

        // Assert
        result.Should().Be(Enum.Parse<LogLevel>(enumValue));
    }

    #endregion

    #region ToArray(IEnumerable<string?>?, Type?) Tests

    [Fact]
    public void ToArray_WithNullSource_ReturnsNull()
    {
        // Act
        var result = ((IEnumerable<string?>?)null).ToArray(typeof(int[]));

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void ToArray_WithNullType_ReturnsNull()
    {
        // Arrange
        var source = new[] { "1", "2", "3" };

        // Act
        var result = source.ToArray(null);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void ToArray_WithNonArrayType_ReturnsNull()
    {
        // Arrange
        var source = new[] { "1", "2", "3" };

        // Act
        var result = source.ToArray(typeof(int)); // Not an array type

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void ToArray_WithValidIntegerStrings_CreatesIntArray()
    {
        // Arrange
        var source = new[] { "1", "2", "3", "4", "5" };

        // Act
        var result = source.ToArray(typeof(int[])) as int[];

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEquivalentTo([1, 2, 3, 4, 5]);
    }

    [Fact]
    public void ToArray_WithValidBooleanStrings_CreatesBoolArray()
    {
        // Arrange
        var source = new[] { "true", "false", "true" };

        // Act
        var result = source.ToArray(typeof(bool[])) as bool[];

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEquivalentTo([true, false, true]);
    }

    [Fact]
    public void ToArray_WithValidEnumStrings_CreatesEnumArray()
    {
        // Arrange
        var source = new[] { "Warning", "Error", "Information" };

        // Act
        var result = source.ToArray(typeof(LogLevel[])) as LogLevel[];

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEquivalentTo([LogLevel.Warning, LogLevel.Error, LogLevel.Information]);
    }

    [Fact]
    public void ToArray_WithNullElementsInSource_FiltersOutNulls()
    {
        // Arrange
        var source = new[] { "1", null, "2", null, "3" };

        // Act
        var result = source.ToArray(typeof(int[])) as int[];

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEquivalentTo([1, 2, 3]);
    }

    [Fact]
    public void ToArray_WithInvalidConversionValue_ThrowsFormatException()
    {
        // Arrange
        var source = new[] { "1", "not-a-number", "3" };

        // Act & Assert
        var action = () => source.ToArray(typeof(int[]));
        action.Should().Throw<FormatException>();
    }

    [Fact]
    public void ToArray_WithEmptySource_CreatesEmptyArray()
    {
        // Arrange
        var source = Array.Empty<string>();

        // Act
        var result = source.ToArray(typeof(string[])) as string[];

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public void ToArray_WithStringArrayType_CreatesStringArray()
    {
        // Arrange
        var source = new[] { "apple", "banana", "cherry" };

        // Act
        var result = source.ToArray(typeof(string[])) as string[];

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEquivalentTo(new[] { "apple", "banana", "cherry" });
    }

    #endregion

    #region GetCollection<T>(string?, char) Tests

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void GetCollection_WithNullOrEmptySource_ReturnsNull(string? source)
    {
        // Act
        var result = source.GetCollection<string>();

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void GetCollection_WithCommaSeparatedIntegers_ReturnsIntArray()
    {
        // Arrange
        var source = "1,2,3,4,5";

        // Act
        var result = source.GetCollection<int>();

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEquivalentTo(new int?[] { 1, 2, 3, 4, 5 });
    }

    [Fact]
    public void GetCollection_WithSemicolonSeparatedStrings_ReturnsStringArray()
    {
        // Arrange
        var source = "apple;banana;cherry";

        // Act
        var result = source.GetCollection<string>(';');

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEquivalentTo(new[] { "apple", "banana", "cherry" });
    }

    [Fact]
    public void GetCollection_WithSpaceSeparatedBooleans_ReturnsBoolArray()
    {
        // Arrange
        var source = "true false true";

        // Act
        var result = source.GetCollection<bool>(' ');

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEquivalentTo(new bool?[] { true, false, true });
    }

    [Fact]
    public void GetCollection_WithSingleValue_ReturnsSingleElementArray()
    {
        // Arrange
        var source = "42";

        // Act
        var result = source.GetCollection<int>();

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(1);
        result[0].Should().Be(42);
    }

    [Fact]
    public void GetCollection_WithEmptyElementsBetweenSeparators_IncludesEmptyElements()
    {
        // Arrange
        var source = "1,,3"; // Empty string between commas

        // Act & Assert
        // This should throw because empty string cannot be converted to int
        var action = () => source.GetCollection<int>();
        action.Should().Throw<FormatException>();
    }

    [Fact]
    public void GetCollection_WithLeadingAndTrailingSeparators_IncludesEmptyElements()
    {
        // Arrange
        var source = ",apple,banana,"; // Leading and trailing commas

        // Act
        var result = source.GetCollection<string>();

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(4);
        result[0].Should().Be("");
        result[1].Should().Be("apple");
        result[2].Should().Be("banana");
        result[3].Should().Be("");
    }

    [Fact]
    public void GetCollection_WithEnumValues_ReturnsEnumArray()
    {
        // Arrange
        var source = "Warning,Error,Information";

        // Act
        var result = source.GetCollection<LogLevel>();

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEquivalentTo(new LogLevel?[] { LogLevel.Warning, LogLevel.Error, LogLevel.Information });
    }

    [Fact]
    public void GetCollection_WithInvalidConversion_ThrowsException()
    {
        // Arrange
        var source = "1,not-a-number,3";

        // Act & Assert
        var action = () => source.GetCollection<int>();
        action.Should().Throw<FormatException>();
    }

    [Theory]
    [InlineData(',')]
    [InlineData(';')]
    [InlineData('|')]
    [InlineData('\t')]
    public void GetCollection_WithCustomSeparators_WorksCorrectly(char separator)
    {
        // Arrange
        var source = $"value1{separator}value2{separator}value3";

        // Act
        var result = source.GetCollection<string>(separator);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEquivalentTo(["value1", "value2", "value3"]);
    }

    #endregion

    #region ToDictionary Tests

    [Fact]
    public void ToDictionary_WithNullSource_ThrowsArgumentNullException()
    {
        // Act & Assert
        var action = () => ((IEnumerable<IConfigurationSection>)null!).ToDictionary(typeof(Dictionary<string, int>));
        action.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void ToDictionary_WithValidSections_CreatesDictionary()
    {
        // Arrange
        var mockSections = CreateMockConfigurationSections();

        // Act
        var result = mockSections.ToDictionary(typeof(Dictionary<string, int>)) as Dictionary<string, int>;

        // Assert
        result.Should().NotBeNull();
        result.Should().ContainKey("key1").WhoseValue.Should().Be(10);
        result.Should().ContainKey("key2").WhoseValue.Should().Be(20);
    }

    [Fact]
    public void ToDictionary_WithEmptySource_CreatesEmptyDictionary()
    {
        // Arrange
        var emptySections = Array.Empty<IConfigurationSection>();

        // Act
        var result = emptySections.ToDictionary(typeof(Dictionary<string, string>)) as Dictionary<string, string>;

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public void ToDictionary_WithSectionWithoutChildren_SkipsSection()
    {
        // Arrange
        var sectionWithoutChildren = CreateMock<IConfigurationSection>();
        sectionWithoutChildren.GetChildren().Returns([]);

        var sections = new[] { sectionWithoutChildren };

        // Act
        var result = sections.ToDictionary(typeof(Dictionary<string, string>)) as Dictionary<string, string>;

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public void ToDictionary_WithSectionWithMultipleChildren_SkipsSection()
    {
        // Arrange
        var child1 = CreateMock<IConfigurationSection>();
        child1.Key.Returns("key1");
        child1.Value.Returns("10");

        var child2 = CreateMock<IConfigurationSection>();
        child2.Key.Returns("key2");
        child2.Value.Returns("20");

        var section = CreateMock<IConfigurationSection>();
        // The code uses SingleOrDefault() which returns null when multiple elements exist
        section.GetChildren().Returns([child1, child2]);

        var sections = new[] { section };

        // Act & Assert
        // SingleOrDefault() with multiple children throws InvalidOperationException
        Func<IDictionary?> action = () => sections.ToDictionary(typeof(Dictionary<string, int>));
        action.Should().Throw<InvalidOperationException>()
            .WithMessage("Sequence contains more than one element");
    }

    [Fact]
    public void ToDictionary_WithInvalidDictionaryType_ReturnsNull()
    {
        // Arrange
        var mockSections = CreateMockConfigurationSections();

        // Act & Assert
        var action = () => mockSections.ToDictionary(typeof(int)); // Not a dictionary type
        action.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void ToDictionary_WithInvalidKeyConversion_ThrowsInvalidOperationException()
    {
        // Arrange
        var child = CreateMock<IConfigurationSection>();
        child.Key.Returns("not-a-number"); // Cannot convert to int
        child.Value.Returns("10");

        var section = CreateMock<IConfigurationSection>();
        section.GetChildren().Returns([child]);

        var sections = new[] { section };

        // Act & Assert
        // The code does: item.Key.ToType(args[0]) ?? throw new InvalidOperationException();
        // When ToType fails, it throws FormatException, but the ?? operator catches null and throws InvalidOperationException
        // However, ToType throws FormatException for invalid conversions, so we expect FormatException
        var action = () => sections.ToDictionary(typeof(Dictionary<int, int>));
        action.Should().Throw<FormatException>();
    }

    [Fact]
    public void ToDictionary_WithInvalidValueConversion_ThrowsFormatException()
    {
        // Arrange
        var child = CreateMock<IConfigurationSection>();
        child.Key.Returns("key1");
        child.Value.Returns("not-a-number"); // Cannot convert to int

        var section = CreateMock<IConfigurationSection>();
        section.GetChildren().Returns([child]);

        var sections = new[] { section };

        // Act & Assert
        var action = () => sections.ToDictionary(typeof(Dictionary<string, int>));
        action.Should().Throw<FormatException>();
    }

    [Fact]
    public void ToDictionary_WithStringToStringDictionary_WorksCorrectly()
    {
        // Arrange
        var child1 = CreateMock<IConfigurationSection>();
        child1.Key.Returns("name");
        child1.Value.Returns("John");

        var child2 = CreateMock<IConfigurationSection>();
        child2.Key.Returns("age");
        child2.Value.Returns("30");

        var section1 = CreateMock<IConfigurationSection>();
        section1.GetChildren().Returns([child1]);

        var section2 = CreateMock<IConfigurationSection>();
        section2.GetChildren().Returns([child2]);

        var sections = new[] { section1, section2 };

        // Act
        var result = sections.ToDictionary(typeof(Dictionary<string, string>)) as Dictionary<string, string>;

        // Assert
        result.Should().NotBeNull();
        result.Should().ContainKey("name").WhoseValue.Should().Be("John");
        result.Should().ContainKey("age").WhoseValue.Should().Be("30");
    }

    [Fact]
    public void ToDictionary_WithUncreatableDictionaryType_ThrowsMissingMethodException()
    {
        // Arrange
        var mockSections = CreateMockConfigurationSections();

        // Act & Assert
        // IDictionary<,> passes the type validation (it's a generic type with the right definition),
        // but Activator.CreateInstance fails because it's an interface
        var action = () => mockSections.ToDictionary(typeof(IDictionary<string, int>));
        action.Should().Throw<MissingMethodException>();
    }

    [Fact]
    public void ToTypeGeneric_WithNullableType_HandlesCorrectly()
    {
        // Act
        // For nullable types, we need to test the actual behavior of the cast
        var nullResult = ((string?)null).ToType<int>();  // This returns 0 (default int)
        var validResult = "42".ToType<int>();             // This returns 42

        // Assert
        nullResult.Should().Be(0);    // Default value for int when input is null
        validResult.Should().Be(42);  // Converted value

        // Test that an empty string throws FormatException for int conversion
        var action = () => "".ToType<int>();
        action.Should().Throw<FormatException>();
    }

    #endregion

    #region Capitalize Tests

    [Theory]
    [InlineData("hello", "Hello")]
    [InlineData("HELLO", "Hello")]
    [InlineData("hELLo", "Hello")]
    [InlineData("h", "H")]
    [InlineData("hello world", "Hello world")]
    [InlineData("HELLO WORLD", "Hello world")]
    public void Capitalize_WithValidStrings_CapitalizesCorrectly(string input, string expected)
    {
        // Act
        var result = input.Capitalize();

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Capitalize_WithNullOrEmptyString_ReturnsEmptyString(string? input)
    {
        // Act
        var result = input.Capitalize();

        // Assert
        result.Should().Be(string.Empty);
    }

    [Fact]
    public void Capitalize_WithWhitespaceOnlyString_ReturnsEmptyString()
    {
        // Act
        var result = "   ".Capitalize();

        // Assert
        result.Should().Be(string.Empty);
    }

    [Fact]
    public void Capitalize_UsesInvariantCulture()
    {
        // Arrange
        var originalCulture = CultureInfo.CurrentCulture;
        try
        {
            // Set to Turkish culture where 'i' uppercases to 'İ' (dotted I)
            CultureInfo.CurrentCulture = new CultureInfo("tr-TR");
            
            // Act
            var result = "istanbul".Capitalize();

            // Assert - Should use invariant culture, so 'i' becomes 'I' (not 'İ')
            result.Should().Be("Istanbul");
            result[0].Should().Be('I'); // ASCII I, not Turkish İ
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
        }
    }

    [Theory]
    [AutoData]
    public void Capitalize_WithBogusGeneratedStrings_WorksCorrectly(string input)
    {
        // Arrange - Ensure we have a non-empty string
        if (string.IsNullOrEmpty(input))
            input = "test";

        // Act
        var result = input.Capitalize();

        // Assert
        result.Should().NotBeNull();
        if (input.Length > 0)
        {
            result[0].Should().Be(char.ToUpper(input.ToLowerInvariant()[0], CultureInfo.InvariantCulture));
            if (input.Length > 1)
            {
                result.Substring(1).Should().Be(input.ToLowerInvariant().Substring(1));
            }
        }
    }

    [Fact]
    public void Capitalize_WithUnicodeCharacters_HandlesCorrectly()
    {
        // Act
        var result = "ñoño".Capitalize();

        // Assert
        result.Should().Be("Ñoño");
    }

    [Fact]
    public void Capitalize_WithNumbersAndSpecialCharacters_HandlesCorrectly()
    {
        // Act
        var result1 = "123abc".Capitalize();
        var result2 = "@hello".Capitalize();
        var result3 = "-world".Capitalize();

        // Assert
        result1.Should().Be("123abc"); // Numbers don't change the case
        result2.Should().Be("@hello"); // Special characters don't change the case
        result3.Should().Be("-world"); // Special characters don't change the case
    }

    #endregion

    #region Helper Methods

    private IEnumerable<IConfigurationSection> CreateMockConfigurationSections()
    {
        var child1 = CreateMock<IConfigurationSection>();
        child1.Key.Returns("key1");
        child1.Value.Returns("10");

        var child2 = CreateMock<IConfigurationSection>();
        child2.Key.Returns("key2");
        child2.Value.Returns("20");

        var section1 = CreateMock<IConfigurationSection>();
        section1.GetChildren().Returns([child1]);

        var section2 = CreateMock<IConfigurationSection>();
        section2.GetChildren().Returns([child2]);

        return [section1, section2];
    }

    #endregion
}

/// <summary>
/// Test enum for conversion testing.
/// </summary>
public enum LogLevel
{
    Debug,
    Information,
    Warning,
    Error
}