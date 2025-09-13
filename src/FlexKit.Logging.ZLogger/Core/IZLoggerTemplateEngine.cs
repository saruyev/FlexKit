using FlexKit.Logging.Formatting.Models;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;

namespace FlexKit.Logging.ZLogger.Core;

/// <summary>
/// Defines the contract for a template engine that processes and executes logging templates.
/// This interface supports precompiled templates for performance optimization and executing
/// precompiled templates using logging parameters.
/// </summary>
internal interface IZLoggerTemplateEngine
{
    /// <summary>
    /// Pre-compiles all templates from configuration at startup for better performance.
    /// </summary>
    [UsedImplicitly]
    void PrecompileTemplates();

    /// <summary>
    /// Executes a template with the given parameters using cached compiled delegate.
    /// </summary>
    /// <param name="logger">The ILogger instance to log to.</param>
    /// <param name="message">The message to log.</param>
    /// <param name="level">The log level to use.</param>
    void ExecuteTemplate(
        ILogger logger,
        in FormattedMessage message,
        LogLevel level);
}
