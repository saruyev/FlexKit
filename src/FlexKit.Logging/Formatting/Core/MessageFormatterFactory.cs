using FlexKit.Logging.Configuration;
using FlexKit.Logging.Formatting.Models;

namespace FlexKit.Logging.Formatting.Core;

/// <summary>
/// Default implementation of a message formatter factory that manages formatter instances
/// and provides selection logic based on configuration and context requirements.
/// </summary>
internal sealed class MessageFormatterFactory : IMessageFormatterFactory
{
    /// <summary>
    /// Represents a read-only dictionary that maps formatter types to their corresponding
    /// <see cref="IMessageFormatter"/> instances. This dictionary is used to manage
    /// and provide message formatters based on configuration and context requirements.
    /// </summary>
    private readonly IReadOnlyDictionary<FormatterType, IMessageFormatter> _formatters;

    /// <summary>
    /// Represents the default fallback formatter to be used when no suitable
    /// formatter is found or explicitly specified. This instance ensures that
    /// message formatting remains operational in scenarios where a formatter
    /// type is unavailable or unrecognized.
    /// </summary>
    private readonly IMessageFormatter _fallbackFormatter;

    /// <summary>
    /// Initializes a new instance of the MessageFormatterFactory.
    /// </summary>
    /// <param name="formatters">Collection of available formatters.</param>
    /// <exception cref="ArgumentNullException">Thrown when formatters' collection is null.</exception>
    /// <exception cref="ArgumentException">
    /// Thrown when formatters' collection is empty or contains duplicate types.
    /// </exception>
    public MessageFormatterFactory(IEnumerable<IMessageFormatter> formatters)
    {
        ArgumentNullException.ThrowIfNull(formatters);

        var formatterArray = formatters.ToArray();
        if (formatterArray.Length == 0)
        {
            throw new ArgumentException("At least one formatter must be provided.", nameof(formatters));
        }

        try
        {
            _formatters = formatterArray.ToDictionary(f => f.FormatterType, f => f);
        }
        catch (ArgumentException ex)
        {
            throw new ArgumentException("Duplicate formatter types found in collection.", nameof(formatters), ex);
        }

        // Use StandardStructured as a fallback - it should always be available
        _fallbackFormatter = _formatters.GetValueOrDefault(FormatterType.StandardStructured)
            ?? _formatters.Values.First();
    }

    /// <inheritdoc />
    public (IMessageFormatter formatter, bool isFallback) GetFormatter(FormattingContext context)
    {
        // Try the primary formatter first
        if (_formatters.TryGetValue(context.FormatterType, out var primaryFormatter))
        {
            return (primaryFormatter, false);
        }

        // If primary fails and fallback is enabled, try other formatters
        return context.EnableFallback
            ? (_fallbackFormatter, true)
            : throw new InvalidOperationException(
            $"No suitable formatter found for type {context.FormatterType}. " +
            $"EnableFallback: {context.EnableFallback}");
    }
}
