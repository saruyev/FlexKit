using FlexKit.Logging.Formatting.Models;

namespace FlexKit.Logging.Formatting.Core;

/// <summary>
/// Factory interface for creating and selecting appropriate message formatters.
/// Provides centralized formatter management and selection logic based on configuration.
/// </summary>
public interface IMessageFormatterFactory
{
    /// <summary>
    /// Gets a formatter that can handle the specified formatting context.
    /// </summary>
    /// <param name="context">The formatting context requiring a formatter.</param>
    /// <returns>A formatter instance capable of processing the context.</returns>
    /// <exception cref="InvalidOperationException">Thrown when no suitable formatter can be found.</exception>
    /// <remarks>
    /// This method performs more sophisticated formatter selection by:
    /// <list type="bullet">
    /// <item>Checking if the primary formatter can handle the context</item>
    /// <item>Falling back to alternative formatters if needed</item>
    /// <item>Considering configuration settings and context requirements</item>
    /// </list>
    /// </remarks>
    IMessageFormatter GetFormatter(FormattingContext context);
}
