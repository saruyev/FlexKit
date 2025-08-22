using FlexKit.Logging.Configuration;
using FlexKit.Logging.Formatting.Models;

namespace FlexKit.Logging.Formatting.Core;

/// <summary>
/// Default implementation of a message formatter factory that manages formatter instances
/// and provides selection logic based on configuration and context requirements.
/// </summary>
public sealed class MessageFormatterFactory : IMessageFormatterFactory
{
    private readonly IReadOnlyDictionary<FormatterType, IMessageFormatter> _formatters;
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
    public IMessageFormatter GetFormatter(FormattingContext context)
    {
        // Try the primary formatter first
        if (_formatters.TryGetValue(context.FormatterType, out var primaryFormatter)
            && primaryFormatter.CanFormat(context))
        {
            return primaryFormatter;
        }

        // If primary fails and fallback is enabled, try other formatters
        if (context.EnableFallback)
        {
            // Try fallback formatter
            if (_fallbackFormatter.CanFormat(context))
            {
                return _fallbackFormatter;
            }

            // Last resort - try any available formatter
            var availableFormatter = _formatters.Values.FirstOrDefault(f => f.CanFormat(context));
            if (availableFormatter is not null)
            {
                return availableFormatter;
            }
        }

        throw new InvalidOperationException(
            $"No suitable formatter found for type {context.FormatterType}. " +
            $"EnableFallback: {context.EnableFallback}");
    }
}
