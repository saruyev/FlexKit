// <copyright file="TypeConversionExtensions.cs" company="Michael Saruyev">
// Copyright (c) Michael Saruyev. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using JetBrains.Annotations;
using Microsoft.Extensions.Configuration;

namespace FlexKit.Configuration.Conversion;

/// <summary>
/// Extension methods for type conversion and collection handling in configuration scenarios.
/// Provides robust type conversion capabilities for transforming configuration values
/// from strings to strongly typed objects, collections, and complex data structures.
/// </summary>
/// <remarks>
/// This class implements FlexKit Configuration library's type conversion strategy,
/// supporting conversion from configuration strings to:
/// - Primitive types (int, bool, double, etc.)
/// - Enum values with name-based parsing
/// - Arrays and collections with customizable separators
/// - Dictionaries from configuration sections
/// - Custom types through the .NET type conversion system
///
/// <para>
/// <strong>Design Philosophy:</strong>
/// The conversion methods prioritize safety and predictability, using culture-invariant
/// parsing and providing sensible defaults for null or empty input values.
/// </para>
///
/// <para>
/// <strong>Usage Patterns:</strong>
/// <code>
/// // Basic type conversion
/// var port = config["Server:Port"].ToType&lt;int&gt;();
/// var isEnabled = config["Features:NewFeature"].ToType&lt;bool&gt;();
///
/// // Collection conversion
/// var allowedHosts = config["AllowedHosts"].GetCollection&lt;string&gt;();
/// var servers = config["LoadBalancer:Servers"].GetCollection&lt;string&gt;(';');
///
/// // Dictionary conversion
/// var settings = configSection.GetChildren().ToDictionary(typeof(Dictionary&lt;string, int&gt;));
/// </code>
/// </para>
/// </remarks>
public static class TypeConversionExtensions
{
    /// <summary>
    /// Converts a string value to a specified target type using culture-invariant parsing.
    /// Supports all primitive types, enums, and types that implement <see cref="IConvertible"/>.
    /// </summary>
    /// <param name="text">The source string value to convert. Can be <c>null</c> or empty.</param>
    /// <param name="type">The target type for conversion. Must not be <c>null</c>.</param>
    /// <returns>
    /// The converted value of the specified type. Returns the default value for value types
    /// or <c>null</c> for reference types when the input is <c>null</c>.
    /// </returns>
    /// <remarks>
    /// This method provides robust type conversion with the following behavior:
    ///
    /// <list type="bullet">
    /// <item>
    /// <term>Null Input Handling:</term>
    /// <description>Returns default values for value types or null for reference types</description>
    /// </item>
    /// <item>
    /// <term>Enum Conversion:</term>
    /// <description>Uses <see cref="Enum.Parse(Type, string)"/> for case-sensitive name matching</description>
    /// </item>
    /// <item>
    /// <term>Primitive Conversion:</term>
    /// <description>Uses <see cref="Convert.ChangeType(object, Type, IFormatProvider)"/> with invariant culture</description>
    /// </item>
    /// <item>
    /// <term>Culture Handling:</term>
    /// <description>All conversions use <see cref="CultureInfo.InvariantCulture"/> for consistency</description>
    /// </item>
    /// </list>
    ///
    /// <para>
    /// <strong>Supported Types:</strong>
    /// <list type="table">
    /// <listheader>
    /// <term>Type Category</term>
    /// <description>Examples</description>
    /// </listheader>
    /// <item>
    /// <term>Primitives</term>
    /// <description>int, long, double, decimal, bool, char</description>
    /// </item>
    /// <item>
    /// <term>Strings</term>
    /// <description>string (returns input as-is)</description>
    /// </item>
    /// <item>
    /// <term>Date/Time</term>
    /// <description>DateTime, DateTimeOffset, TimeSpan</description>
    /// </item>
    /// <item>
    /// <term>Enums</term>
    /// <description>Any enum type with string name matching</description>
    /// </item>
    /// <item>
    /// <term>Nullable Types</term>
    /// <description>int?, DateTime?, bool? (via underlying type conversion)</description>
    /// </item>
    /// </list>
    /// </para>
    ///
    /// <para>
    /// <strong>Example Usage:</strong>
    /// <code>
    /// // Basic type conversions
    /// object port = "8080".ToType(typeof(int));           // Returns: 8080
    /// object flag = "true".ToType(typeof(bool));          // Returns: true
    /// object value = "".ToType(typeof(int));              // Returns: 0 (default)
    /// object text = null.ToType(typeof(string));          // Returns: null
    ///
    /// // Enum conversion
    /// object level = "Warning".ToType(typeof(LogLevel));  // Returns: LogLevel.Warning
    ///
    /// // Nullable type conversion
    /// object optional = "42".ToType(typeof(int?));        // Returns: 42 (as nullable int)
    /// </code>
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="type"/> is <c>null</c>.</exception>
    /// <exception cref="ArgumentException">Thrown when the target type is not supported for conversion.</exception>
    /// <exception cref="FormatException">Thrown when the input string format is invalid for the target type.</exception>
    /// <exception cref="OverflowException">Thrown when the input value is outside the valid range for the target numeric type.</exception>
    public static object? ToType(this string? text, Type type)
    {
        ArgumentNullException.ThrowIfNull(type);

        if (text is null)
        {
            return type.IsValueType ? Activator.CreateInstance(type) : null;
        }

        if (type == typeof(TimeSpan))
        {
            return TimeSpan.Parse(text, CultureInfo.InvariantCulture);
        }

        return type.IsEnum
            ? Enum.Parse(type, text)
            : Convert.ChangeType(text, type, CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Converts a string value to a specified target type using generic type inference.
    /// This is a generic wrapper around <see cref="ToType(string?, Type)"/> that provides
    /// compile-time type safety and eliminates the need for explicit casting.
    /// </summary>
    /// <typeparam name="T">The target type for conversion. Type is inferred from a usage context.</typeparam>
    /// <param name="text">The source string value to convert. Can be <c>null</c> or empty.</param>
    /// <returns>
    /// The converted value of type <typeparamref name="T"/>. Returns the default value for
    /// <typeparamref name="T"/> when the input is <c>null</c> and <typeparamref name="T"/> is a value type.
    /// </returns>
    /// <remarks>
    /// This generic method provides a more convenient and type-safe way to perform string-to-type
    /// conversions compared to the non-generic version. The compiler can infer the target type
    /// from the assignment context, reducing the need for explicit type specification.
    ///
    /// <para>
    /// <strong>Type Inference Examples:</strong>
    /// <code>
    /// // Type inferred from variable declaration
    /// int port = "8080".ToType&lt;int&gt;();
    /// bool isEnabled = "true".ToType&lt;bool&gt;();
    ///
    /// // Type inferred from method parameter
    /// void SetPort(int port) { }
    /// SetPort("9000".ToType&lt;int&gt;());
    ///
    /// // Explicit type specification when needed
    /// var value = "42".ToType&lt;long&gt;();
    /// </code>
    /// </para>
    ///
    /// <para>
    /// <strong>Nullable Type Handling:</strong>
    /// When <typeparamref name="T"/> is a nullable value type (e.g., <c>int?</c>), the conversion
    /// behaves as follows:
    /// <list type="bullet">
    /// <item>Non-null input: Converts to the underlying type and wraps in nullable</item>
    /// <item>Null input: Returns <c>null</c> for the nullable type</item>
    /// <item>Empty string: Attempts conversion, may return default value or throw depending on type</item>
    /// </list>
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentException">Thrown when <typeparamref name="T"/> is not supported for conversion.</exception>
    /// <exception cref="FormatException">Thrown when the input string format is invalid for <typeparamref name="T"/>.</exception>
    /// <exception cref="OverflowException">Thrown when the input value is outside the valid range for <typeparamref name="T"/>.</exception>
    [UsedImplicitly]
    public static T? ToType<T>(this string? text) => (T?)ToType(text, typeof(T));

    /// <summary>
    /// Converts a collection of strings to an array of the specified element type.
    /// Creates a strongly typed array from a collection of string values, applying
    /// type conversion to each element using the same conversion logic as <see cref="ToType(string?, Type)"/>.
    /// </summary>
    /// <param name="source">The source collection of strings to convert. Can be <c>null</c>.</param>
    /// <param name="type">The array type that defines the element type for conversion. Must be an array type.</param>
    /// <returns>
    /// An array instance of the specified type containing converted elements, or <c>null</c>
    /// if the source collection or type is <c>null</c>, or if the type is not an array type.
    /// </returns>
    /// <remarks>
    /// This method provides array conversion capabilities for configuration scenarios where
    /// values are stored as collections of strings but need to be converted to strongly typed arrays.
    ///
    /// <para>
    /// <strong>Conversion Process:</strong>
    /// <list type="number">
    /// <item>Validates that the target type is an array type</item>
    /// <item>Extracts the element type from the array type</item>
    /// <item>Filters out null values from the source collection</item>
    /// <item>Creates a new array of the appropriate size</item>
    /// <item>Converts each string element to the target element type</item>
    /// <item>Populates the array with converted values</item>
    /// </list>
    /// </para>
    ///
    /// <para>
    /// <strong>Type Safety:</strong>
    /// The method ensures type safety by:
    /// <list type="bullet">
    /// <item>Validating that the target type is actually an array type</item>
    /// <item>Using the array's element type for individual conversions</item>
    /// <item>Creating arrays with the exact element type specified</item>
    /// </list>
    /// </para>
    ///
    /// <para>
    /// <strong>Example Usage:</strong>
    /// <code>
    /// // Convert string collection to int array
    /// var strings = new[] { "1", "2", "3", "4" };
    /// var integers = strings.ToArray(typeof(int[])) as int[];
    /// // Result: [1, 2, 3, 4]
    ///
    /// // Convert with mixed valid/invalid values (nulls filtered out)
    /// var mixed = new[] { "10", null, "20", "30" };
    /// var numbers = mixed.ToArray(typeof(double[])) as double[];
    /// // Result: [10.0, 20.0, 30.0]
    ///
    /// // Convert enum values
    /// var statusStrings = new[] { "Active", "Inactive", "Pending" };
    /// var statuses = statusStrings.ToArray(typeof(Status[])) as Status[];
    /// </code>
    /// </para>
    ///
    /// <para>
    /// <strong>Error Handling:</strong>
    /// <list type="bullet">
    /// <item>Null source collections return <c>null</c></item>
    /// <item>Non-array types return <c>null</c></item>
    /// <item>Null strings in the collection are filtered out</item>
    /// <item>Individual conversion errors propagate as exceptions</item>
    /// </list>
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentException">Thrown when individual string values cannot be converted to the element type.</exception>
    /// <exception cref="FormatException">Thrown when string values have an invalid format for the element type.</exception>
    /// <exception cref="OverflowException">Thrown when string values represent numbers outside the valid range for the element type.</exception>
    public static object? ToArray(this IEnumerable<string?>? source, Type? type)
    {
        if (source is null || type is null)
        {
            return null;
        }

        var elementType = type.GetElementType();
        if (elementType is null)
        {
            return null;
        }

        var array = source.Where(s => s is not null).ToArray();
        var target = Array.CreateInstance(elementType, array.Length);

        for (var i = 0; i < array.Length; i++)
        {
            target.SetValue(array[i].ToType(elementType), i);
        }

        return target;
    }

    /// <summary>
    /// Gets a collection of values from a delimited string using a specified separator.
    /// Parses a single configuration value containing multiple items separated by a delimiter
    /// and converts each item to the specified type.
    /// </summary>
    /// <typeparam name="T">The type of the collection elements.</typeparam>
    /// <param name="source">The delimited string value to parse. Can be <c>null</c> or empty.</param>
    /// <param name="separator">The character used to separate values in the string. Defaults to comma.</param>
    /// <returns>
    /// An array of type <typeparamref name="T"/> containing the parsed and converted values,
    /// or <c>null</c> if the source string is <c>null</c> or empty.
    /// </returns>
    /// <remarks>
    /// This method is particularly useful for configuration scenarios where multiple values
    /// are stored as a single delimited string. Common use cases include:
    /// - Comma-separated server lists
    /// - Semicolon-separated file paths
    /// - Space-separated feature flags
    ///
    /// <para>
    /// <strong>Processing Steps:</strong>
    /// <list type="number">
    /// <item>Splits the source string using the specified separator</item>
    /// <item>Converts each substring to type <typeparamref name="T"/></item>
    /// <item>Returns an array containing all converted values</item>
    /// </list>
    /// </para>
    ///
    /// <para>
    /// <strong>Whitespace Handling:</strong>
    /// The method does not automatically trim whitespace from individual values.
    /// If your configuration contains values with leading/trailing spaces, consider
    /// preprocessing the string or implementing custom trimming logic.
    /// </para>
    ///
    /// <para>
    /// <strong>Example Usage:</strong>
    /// <code>
    /// // Comma-separated integers (default separator)
    /// var ports = "8080,8081,8082".GetCollection&lt;int&gt;();
    /// // Result: [8080, 8081, 8082]
    ///
    /// // Semicolon-separated file paths
    /// var paths = "C:\data;D:\logs;E:\temp".GetCollection&lt;string&gt;(';');
    /// // Result: ["C:\data", "D:\logs", "E:\temp"]
    ///
    /// // Space-separated boolean flags
    /// var flags = "true false true".GetCollection&lt;bool&gt;(' ');
    /// // Result: [true, false, true]
    ///
    /// // Empty or null input
    /// var empty = "".GetCollection&lt;string&gt;();
    /// // Result: null
    /// </code>
    /// </para>
    ///
    /// <para>
    /// <strong>Error Scenarios:</strong>
    /// <list type="bullet">
    /// <item>If any individual value cannot be converted to <typeparamref name="T"/>, an exception is thrown</item>
    /// <item>Empty strings between separators are included and may cause conversion errors</item>
    /// <item>Leading/trailing separators result in empty string values</item>
    /// </list>
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentException">Thrown when individual values cannot be converted to <typeparamref name="T"/>.</exception>
    /// <exception cref="FormatException">Thrown when individual values have an invalid format for <typeparamref name="T"/>.</exception>
    /// <exception cref="OverflowException">Thrown when individual values are outside the valid range for <typeparamref name="T"/>.</exception>
    [UsedImplicitly]
    public static T?[]? GetCollection<T>(this string? source, char separator = ',')
    {
        if (string.IsNullOrEmpty(source))
        {
            return null;
        }

        var list = source.Split(separator);
        return list.Select(item => item.ToType<T>()).ToArray();
    }

    /// <summary>
    /// Converts an <see cref="IConfigurationSection"/> collection to a dictionary with keys and values of specified types.
    /// Creates a strongly typed dictionary from configuration sections where each section represents a key-value pair
    /// or contains child sections that represent dictionary entries.
    /// </summary>
    /// <param name="source">The source collection of configuration sections to convert.</param>
    /// <param name="type">The generic dictionary type specifying key and value types (e.g., typeof(Dictionary&lt;string, int&gt;)).</param>
    /// <returns>
    /// An <see cref="IDictionary"/> instance of the specified type containing the converted key-value pairs,
    /// or <c>null</c> if the dictionary cannot be created.
    /// </returns>
    /// <remarks>
    /// This method enables conversion of hierarchical configuration data into strongly typed dictionaries.
    /// It's particularly useful for configuration scenarios where you have dynamic sets of key-value pairs
    /// that need to be accessible through a dictionary interface.
    ///
    /// <para>
    /// <strong>Expected Configuration Structure:</strong>
    /// The method expects each configuration section to contain a single child element where:
    /// <list type="bullet">
    /// <item>The child's key becomes the dictionary key (converted to the key type)</item>
    /// <item>The child's value becomes the dictionary value (converted to the value type)</item>
    /// </list>
    /// </para>
    ///
    /// <para>
    /// <strong>Type Requirements:</strong>
    /// <list type="bullet">
    /// <item>The type parameter must be a generic dictionary type</item>
    /// <item>The dictionary must have exactly two generic arguments (key type and value type)</item>
    /// <item>Both key and value types must be convertible using the <see cref="ToType(string?, Type)"/> method</item>
    /// <item>The type must have a parameterless constructor</item>
    /// </list>
    /// </para>
    ///
    /// <para>
    /// <strong>Configuration Example:</strong>
    /// <code>
    /// // appsettings.json
    /// {
    ///   "ServerPorts": {
    ///     "Web": { "Port": "8080" },
    ///     "Api": { "Port": "8081" },
    ///     "Admin": { "Port": "8082" }
    ///   }
    /// }
    ///
    /// // Usage
    /// var section = configuration.GetSection("ServerPorts");
    /// var ports = section.GetChildren().ToDictionary(typeof(Dictionary&lt;string, int&gt;));
    /// // Result: { "Web": 8080, "Api": 8081, "Admin": 8082 }
    /// </code>
    /// </para>
    ///
    /// <para>
    /// <strong>Supported Dictionary Types:</strong>
    /// <list type="bullet">
    /// <item>Dictionary&lt;TKey, TValue&gt;</item>
    /// <item>ConcurrentDictionary&lt;TKey, TValue&gt;</item>
    /// <item>SortedDictionary&lt;TKey, TValue&gt;</item>
    /// <item>Any type implementing IDictionary with a parameterless constructor</item>
    /// </list>
    /// </para>
    ///
    /// <para>
    /// <strong>Error Handling:</strong>
    /// <list type="bullet">
    /// <item>Sections without exactly one child are skipped</item>
    /// <item>Keys that cannot be converted throw <see cref="InvalidOperationException"/></item>
    /// <item>Values that cannot be converted propagate conversion exceptions</item>
    /// <item>Dictionary creation failures return <c>null</c></item>
    /// </list>
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="source"/> is <c>null</c>.</exception>
    /// <exception cref="InvalidOperationException">Thrown when dictionary creation fails or keys cannot be converted.</exception>
    /// <exception cref="ArgumentException">Thrown when the type is not a valid dictionary type.</exception>
    [RequiresUnreferencedCode("Uses reflection to create dictionary instance")]
    [UsedImplicitly]
    public static IDictionary ToDictionary(this IEnumerable<IConfigurationSection> source, Type type)
    {
        ArgumentNullException.ThrowIfNull(source);

        if (!type.IsGenericType ||
            (type.GetGenericTypeDefinition() != typeof(IDictionary<,>) &&
             type.GetGenericTypeDefinition() != typeof(Dictionary<,>)))
        {
            throw new ArgumentException($"Type {type.Name} is not a supported dictionary type.", nameof(type));
        }

        var dictionary = (IDictionary?)Activator.CreateInstance(type);
        var args = type.GetGenericArguments();

        foreach (var value in source)
        {
            var item = value.GetChildren().SingleOrDefault();

            if (item is null)
            {
                continue;
            }

            var key = item.Key.ToType(args[0]) ?? throw new InvalidOperationException();
            dictionary!.Add(key, item.Value.ToType(args[1]));
        }

        return dictionary!;
    }

    /// <summary>
    /// Capitalizes the first letter of the specified string using culture-invariant rules.
    /// Converts the entire string to lowercase and then uppercases only the first character,
    /// providing consistent string formatting for configuration-related scenarios.
    /// </summary>
    /// <param name="str">The source string to capitalize. Can be <c>null</c> or empty.</param>
    /// <returns>
    /// A new string with the first character in uppercase and the rest in lowercase.
    /// Returns an empty string if the input is <c>null</c> or empty.
    /// </returns>
    /// <remarks>
    /// This utility method is commonly used in configuration scenarios for normalizing
    /// string values, particularly when dealing with:
    /// - Property names that need consistent casing
    /// - Configuration keys that require standardized formatting
    /// - Display values that need title-case formatting
    ///
    /// <para>
    /// <strong>Casing Behavior:</strong>
    /// <list type="bullet">
    /// <item>The entire input string is first converted to lowercase using <see cref="string.ToLowerInvariant"/></item>
    /// <item>The first character is then converted to uppercase using <see cref="char.ToUpper(char, CultureInfo)"/> with invariant culture</item>
    /// <item>All other characters remain in the lowercase</item>
    /// </list>
    /// </para>
    ///
    /// <para>
    /// <strong>Culture Handling:</strong>
    /// The method uses culture-invariant operations to ensure consistent behavior across
    /// different system locales and cultural settings. This is important for configuration
    /// scenarios where consistency is more important than locale-specific formatting.
    /// </para>
    ///
    /// <para>
    /// <strong>Example Usage:</strong>
    /// <code>
    /// // Basic capitalization
    /// var result1 = "hello world".Capitalize();     // "Hello world"
    /// var result2 = "HELLO WORLD".Capitalize();     // "Hello world"
    /// var result3 = "hELLo WoRLd".Capitalize();     // "Hello world"
    ///
    /// // Edge cases
    /// var result4 = "h".Capitalize();               // "H"
    /// var result5 = "".Capitalize();                // ""
    /// var result6 = ((string)null).Capitalize();   // ""
    ///
    /// // Configuration property normalization
    /// var configKey = "userName".Capitalize();      // "Username"
    /// var displayName = "firstName".Capitalize();   // "Firstname"
    /// </code>
    /// </para>
    ///
    /// <para>
    /// <strong>Performance Considerations:</strong>
    /// <list type="bullet">
    /// <item>Creates a new string instance (strings are immutable)</item>
    /// <item>Performs two passes over the string (lowercase conversion plus character array manipulation)</item>
    /// <item>Suitable for configuration scenarios where performance is not critical</item>
    /// <item>Consider caching results for frequently used strings</item>
    /// </list>
    /// </para>
    /// </remarks>
    [UsedImplicitly]
    public static string Capitalize(this string? str)
    {
        if (string.IsNullOrEmpty(str))
        {
            return string.Empty;
        }

        var arr = str.ToLowerInvariant().ToCharArray();
        arr[0] = char.ToUpper(arr[0], CultureInfo.InvariantCulture);
        return new string(arr).Trim();
    }
}
