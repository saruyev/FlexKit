using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text.RegularExpressions;
using FlexKit.Logging.Configuration;
using FlexKit.Logging.Formatting.Models;
using Microsoft.Extensions.Logging;
using ZLogger;

namespace FlexKit.Logging.ZLogger.Core;

/// <summary>
/// Template engine that converts FlexKit templates to native ZLogger calls.
/// Handles template parsing, parameter ordering, compilation, and caching.
/// </summary>
/// <param name="config">The logging configuration containing all templates.</param>
public partial class ZLoggerTemplateEngine(LoggingConfig config) : IZLoggerTemplateEngine
{
    [GeneratedRegex(@"\{([^}:]+)(?::([^}]+))?\}", RegexOptions.Compiled)]
    private static partial Regex ParameterRegex();

    private readonly ConcurrentDictionary<string, CompiledDelegateForTemplate> _cache = new();

    private readonly bool _prettyPrint =
        config.Formatters.Json.PrettyPrint && config.DefaultFormatter == FormatterType.Json;

    /// <summary>
    /// Pre-compiles all templates from configuration at startup for better performance.
    /// </summary>
    public void PrecompileTemplates()
    {
        // Compile all configured templates
        foreach (var (_, templateConfig) in config.Templates.Where(t => t.Value.Enabled))
        {
            CompileConfigTemplates(templateConfig);
        }

        // Compile formatter-specific templates
        CompileFormatterTemplates();

        // Compile fallback template
        if (!string.IsNullOrEmpty(config.FallbackTemplate))
        {
            CompileTemplate(config.FallbackTemplate);
        }

        // Compile hardcoded fallback templates
        CompileHardcodedTemplates();
    }

    /// <summary>
    /// Executes a template with the given parameters using cached compiled delegate.
    /// </summary>
    /// <param name="logger">The ILogger instance to log to.</param>
    /// <param name="message">The message to log.</param>
    /// <param name="level">The log level to use.</param>
    public void ExecuteTemplate(
        ILogger logger,
        in FormattedMessage message,
        LogLevel level)
    {
        if (string.IsNullOrEmpty(message.Template))
        {
            Debug.WriteLine("Empty template");
            return;
        }

        try
        {
            var compiled = _cache.GetOrAdd(message.Template, CompileTemplate);
            var orderedArgs = PrepareParameters(message.Parameters, compiled.ParameterNames, message.Template);
            compiled.Action(logger, orderedArgs, level);
        }
        catch (Exception ex)
        {
            // Final fallback - just log as string
            Debug.WriteLine($"Template execution failed for: {message.Template}. Error: {ex.Message}");
        }
    }

    /// <summary>
    /// Compiles formatter-specific templates from the logging configuration.
    /// This prepares formatter templates for use at runtime, including
    /// hybrid formatter message templates, custom formatter default templates,
    /// and predefined JSON formatter destructuring templates.
    /// </summary>
    private void CompileFormatterTemplates()
    {
        var formatters = config.Formatters;

        if (!string.IsNullOrEmpty(formatters.Hybrid.MessageTemplate))
        {
            CompileTemplate(formatters.Hybrid.MessageTemplate);
        }

        if (!string.IsNullOrEmpty(formatters.CustomTemplate.DefaultTemplate))
        {
            CompileTemplate(formatters.CustomTemplate.DefaultTemplate);
        }

        // JSON formatter destructuring template
        CompileTemplate("{Metadata}");
    }

    /// <summary>
    /// Compiles the templates specified in the provided template configuration.
    /// Processes each defined template (SuccessTemplate, ErrorTemplate, GeneralTemplate)
    /// by invoking the compilation logic for efficient runtime execution.
    /// </summary>
    /// <param name="templateConfig">The configuration object containing the templates to be compiled.</param>
    private void CompileConfigTemplates(TemplateConfig templateConfig)
    {
        if (!string.IsNullOrEmpty(templateConfig.SuccessTemplate))
        {
            CompileTemplate(templateConfig.SuccessTemplate);
        }

        if (!string.IsNullOrEmpty(templateConfig.ErrorTemplate))
        {
            CompileTemplate(templateConfig.ErrorTemplate);
        }

        if (string.IsNullOrEmpty(templateConfig.GeneralTemplate))
        {
            return;
        }

        CompileTemplate(templateConfig.GeneralTemplate);
    }

    /// <summary>
    /// Compiles the specified template into a delegate for structured logging.
    /// Converts template strings into a format optimized for ZLogger, extracting parameter names and building
    /// an actionable delegate.
    /// </summary>
    /// <param name="template">The template string to be compiled.</param>
    /// <returns>
    /// A <see cref="CompiledDelegateForTemplate"/> object containing the compiled delegate and
    /// extracted parameter names.
    /// </returns>
#pragma warning disable S3241
    private CompiledDelegateForTemplate CompileTemplate(string template)
#pragma warning restore S3241
    {
        try
        {
            return new CompiledDelegateForTemplate(
                BuildZLoggerAction(ConvertToZLoggerTemplate(template)),
                ExtractParameterNames(template));
        }
        catch
        {
            return new CompiledDelegateForTemplate(
                (_, _, _) => Debug.WriteLine($"Template compilation failed: {template}"),
                []);
        }
    }

    /// <summary>
    /// Extracts parameter names from a template using a regular expression.
    /// </summary>
    /// <param name="template">The template string containing parameters enclosed in braces.</param>
    /// <returns>An array of parameter names extracted from the template.</returns>
    private static string[] ExtractParameterNames(string template) =>
    [
        .. ParameterRegex()
            .Matches(template)
            .Select(m => m.Groups[1].Value)];

    /// <summary>
    /// Converts a FlexKit logging template into a format compatible with ZLogger.
    /// Replaces FlexKit parameter patterns with ZLogger-specific indexed format specifiers,
    /// ensuring proper parameter alignment and translation for ZLogger compatibility.
    /// </summary>
    /// <param name="template">The original FlexKit logging template to be converted.</param>
    /// <returns>The ZLogger-compatible version of the provided logging template.</returns>
    private string ConvertToZLoggerTemplate(string template)
    {
        var paramIndex = 0;
        return ParameterRegex().Replace(template, match =>
        {
            var paramName = match.Groups[1].Value;
            var formatSpec = match.Groups[2].Success ? match.Groups[2].Value : null;

            var zloggerFormatSpec = GetZLoggerFormatSpec(paramName, formatSpec);
            return $"{{{paramIndex++}{zloggerFormatSpec}}}";
        });
    }

    /// <summary>
    /// Retrieves the appropriate ZLogger format specifier based on the parameter name and
    /// an optional format specification.
    /// </summary>
    /// <param name="parameterName">The name of the parameter being formatted.</param>
    /// <param name="formatSpec">An optional format specifier explicitly provided for the parameter.</param>
    /// <returns>A string containing the formatted specifier for use in ZLogger template formatting.</returns>
    private string GetZLoggerFormatSpec(
        string parameterName,
        string? formatSpec)
    {
        if (!string.IsNullOrEmpty(formatSpec))
        {
            return $":{formatSpec}";
        }

        // Apply default format specs based on a parameter name
        return parameterName switch
        {
            "InputParameters" or "OutputValue" => ":json",
            "Metadata" when !_prettyPrint => ":json",
            "Duration" => ":N2",
            _ => ""
        };
    }

    /// <summary>
    /// Creates an action to handle ZLogger logging for a given template.
    /// The returned action processes the template and logs messages with the appropriate log level and parameters.
    /// </summary>
    /// <param name="zloggerTemplate">The ZLogger string template to be used for formatting log messages.</param>
    /// <returns>
    /// An action that accepts an ILogger, an array of arguments, and a log level,
    /// and uses the provided template to generate and log a formatted message.
    /// </returns>
    private static Action<ILogger, object?[], LogLevel> BuildZLoggerAction(string zloggerTemplate)
    {
        // Parse template: "Hello {0}, the time is {1}"
        var parsedTemplate = ParseTemplate(zloggerTemplate);

        return (logger, args, level) =>
        {
            var handler = new ZLoggerInterpolatedStringHandler(
                parsedTemplate.LiteralLength,
                parsedTemplate.FormattedCount,
                logger,
                level,
                out var enabled);

            if (!enabled)
            {
                return;
            }

            ProcessTemplateParts(parsedTemplate, handler, args);
            logger.ZLog(level, ref handler);
        };
    }

    /// <summary>
    /// Processes the parts of a parsed template and appends them to the ZLogger interpolated string handler.
    /// </summary>
    /// <param name="parsedTemplate">
    /// The parsed template containing the literal and formatted parts of the message.
    /// </param>
    /// <param name="handler">
    /// The ZLogger interpolated string handler used to construct the logged message.
    /// </param>
    /// <param name="args">The arguments to be inserted into the formatted parts of the template.</param>
    private static void ProcessTemplateParts(
        ParsedTemplate parsedTemplate,
        ZLoggerInterpolatedStringHandler handler,
        object?[] args)
    {
        var argIndex = 0;
        foreach (var part in parsedTemplate.Parts)
        {
            if (part.IsLiteral)
            {
#pragma warning disable CA1857
                handler.AppendLiteral(string.Intern(part.Text));
#pragma warning restore CA1857
            }
            else
            {
                // Pass the format string if available
                if (!string.IsNullOrEmpty(part.Format))
                {
                    handler.AppendFormatted(args[argIndex++], format: part.Format);
                }
                else
                {
                    handler.AppendFormatted(args[argIndex++]);
                }
            }
        }
    }

    /// <summary>
    /// Prepares parameters by ordering them based on their appearance in the specified template.
    /// Handles special cases, such as JSON destructuring templates, and ensures parameter alignment
    /// with the expected names.
    /// </summary>
    /// <param name="parameters">
    /// A read-only dictionary containing the named parameters and their values.
    /// </param>
    /// <param name="expectedParameterNames">
    /// An array of parameter names in the order they appear in the template.
    /// </param>
    /// <param name="template">
    /// The template string used to determine the order and special cases of parameter preparation.
    /// </param>
    /// <returns>
    /// An array of parameter values ordered to match the expected parameter names, with unmatched names set to null.
    /// </returns>
    private static object?[] PrepareParameters(
        IReadOnlyDictionary<string, object?> parameters,
        string[] expectedParameterNames,
        string template)
    {
        if (parameters.Count == 0)
        {
            return [];
        }

        // Handle special cases
        if (IsJsonDestructuringTemplate(template, parameters))
        {
            return PrepareJsonDestructuringParameters(parameters);
        }

        // Standard parameter preparation - order by template appearance
        var result = new object?[expectedParameterNames.Length];
        for (var i = 0; i < expectedParameterNames.Length; i++)
        {
            var paramName = expectedParameterNames[i];
            result[i] = parameters.TryGetValue(paramName, out var value) ? value : null;
        }

        return result;
    }

    /// <summary>
    /// Determines if the given template represents a JSON destructuring template
    /// by analyzing its structure and specific parameter values.
    /// </summary>
    /// <param name="template">The template string to evaluate.</param>
    /// <param name="parameters">The dictionary of parameters associated with the template.</param>
    /// <returns>True if the template is identified as a JSON destructuring template; otherwise, false.</returns>
    private static bool IsJsonDestructuringTemplate(
        string template,
        IReadOnlyDictionary<string, object?> parameters) =>
        template.Trim() == "{Metadata}" &&
        parameters.TryGetValue("Metadata", out var metadata) &&
        metadata is not string;

    /// <summary>
    /// Prepares parameters for JSON destructuring by extracting relevant data from the provided dictionary.
    /// Specifically, handles parameters meant for JSON serialization, such as "Metadata".
    /// </summary>
    /// <param name="parameters">
    /// A dictionary containing keys and corresponding values to be processed for JSON serialization.
    /// </param>
    /// <returns>
    /// An array of objects that represent the processed parameters for destructuring.
    /// Returns a single-element array with the value of "Metadata" if present;
    /// otherwise, an array with an empty object.
    /// </returns>
    private static object?[] PrepareJsonDestructuringParameters(IReadOnlyDictionary<string, object?> parameters)
    {
        // For JSON formatter with destructuring template "{Metadata}"
        // Let ZLogger handle JSON serialization with ":json" specifier
        if (parameters.TryGetValue("Metadata", out var metadata))
        {
            return [metadata];
        }

        return [new { }]; // Empty object instead of string
    }

    /// <summary>
    /// Compiles a predefined set of hardcoded templates to optimize logging performance.
    /// This includes standard structured templates, success and error templates,
    /// default fallback templates, and common log variations.
    /// </summary>
    private void CompileHardcodedTemplates()
    {
        // Compile hardcoded templates from TemplateExtensions and other defaults
        var hardcodedTemplates = new[]
        {
            // StandardStructured templates
            "Method {MethodName} started",
            "Method {MethodName} completed in {Duration}ms",
            "Method {MethodName} failed",
            "Method {MethodName} failed after {Duration}ms",

            // SuccessError templates
            "✅ Method {MethodName} started successfully",
            "✅ Method {MethodName} completed successfully in {Duration}ms",
            "❌ Method {MethodName} failed: {ExceptionType} - {ExceptionMessage}",
            "❌ Method {MethodName} failed: {ExceptionType} - {ExceptionMessage} (after {Duration}ms)",

            // Default fallback templates
            "Method {TypeName}.{MethodName} - Status: {Success}",
            "Method {TypeName}.{MethodName} executed",
            "Method {MethodName} completed in {Duration}ms", // Hybrid default

            // Common variations that might appear
            "{TypeName}.{MethodName}({InputParameters}) → {OutputValue}",
            "{TypeName}.{MethodName}({InputParameters}) failed: {ExceptionType} - {ExceptionMessage}",
        };

        foreach (var template in hardcodedTemplates)
        {
            CompileTemplate(template);
        }
    }

    /// <summary>
    /// Parses a template string into a structured representation suitable for use in ZLogger logging operations.
    /// </summary>
    /// <param name="template">The template string to parse, containing placeholders for parameters.</param>
    /// <returns>
    /// A structured representation of the template with separated literal parts and parameter placeholders.
    /// </returns>
    private static ParsedTemplate ParseTemplate(string template)
    {
        // very crude, just to illustrate
        var parsedTemplate = new ParsedTemplate();
        var i = 0;
        while (i < template.Length)
        {
            var brace = template.IndexOf('{', i);
            if (brace == -1)
            {
                parsedTemplate.Parts.Add(new TemplatePart { IsLiteral = true, Text = template[i..] });
                break;
            }
            if (brace > i)
            {
                parsedTemplate.Parts.Add(new TemplatePart { IsLiteral = true, Text = template[i..brace] });
            }

            var end = template.IndexOf('}', brace);
            var inside = template.Substring(brace + 1, end - brace - 1);
            var split = inside.Split(':');
            parsedTemplate.Parts.Add(new TemplatePart
            {
                IsLiteral = false,
                Format = (split.Length > 1 ? split[1] : null) ?? string.Empty
            });
            i = end + 1;
        }

        parsedTemplate.UpdateCounts();
        return parsedTemplate;
    }

    /// <summary>
    /// Represents a parsed template consisting of individual parts, which may include
    /// literal text and formatted placeholders. Used for processing and rendering template content.
    /// </summary>
    private sealed class ParsedTemplate
    {
        /// <summary>
        /// Gets the collection of individual parts that make up the parsed template.
        /// </summary>
        /// <remarks>
        /// Each part can be either a literal text segment or a formatted placeholder. Literal parts
        /// consist of a static text, while formatted parts represent dynamic placeholders that may
        /// include optional format specifiers. This collection is used during template rendering to
        /// correctly assemble the final message by combining static and dynamic content.
        /// </remarks>
        public List<TemplatePart> Parts { get; } = new();

        /// <summary>
        /// Gets the total length of all literal text segments within the parsed template.
        /// </summary>
        /// <remarks>
        /// This property represents the cumulative character count of all parts in the template
        /// that consist of a static text (literals). It is calculated during the parsing process
        /// and is used to optimize operations such as interpolated string handling during the
        /// rendering of the template.
        /// </remarks>
        public int LiteralLength { get; private set; }

        /// <summary>
        /// Gets the count of formatted placeholders present in the parsed template.
        /// </summary>
        /// <remarks>
        /// This property indicates the number of dynamic segments or placeholders
        /// in the template that require formatting during rendering. These placeholders
        /// are used to insert runtime values into the final rendered content.
        /// </remarks>
        public int FormattedCount { get; private set; }

        /// <summary>
        /// Updates the count of literal and formatted parts within the parsed template.
        /// Calculates the total length of a literal text and the number of formatted placeholders.
        /// </summary>
        public void UpdateCounts()
        {
            LiteralLength = Parts.Where(p => p.IsLiteral).Sum(p => p.Text.Length);
            FormattedCount = Parts.Count(p => !p.IsLiteral);
        }
    }

    /// <summary>
    /// Represents a single part of a parsed logging template.
    /// A template part can either be a literal text segment or a formatted placeholder.
    /// </summary>
    /// <remarks>
    /// Used internally during template parsing to structure the segments of a logging template,
    /// identifying literals and placeholders along with their formatting specifications.
    /// </remarks>
    private sealed class TemplatePart
    {
        /// <summary>
        /// Gets a value indicating whether the template part is a literal segment of text.
        /// </summary>
        /// <remarks>
        /// Literal segments are static text portions of the template that do not include dynamic
        /// placeholders or formatting. This property is used during template parsing and rendering
        /// to differentiate between the text that remains unchanged and dynamic parts that require
        /// specific processing.
        /// </remarks>
        public bool IsLiteral { get; init; }

        /// <summary>
        /// Gets the text content of the template part.
        /// </summary>
        /// <remarks>
        /// This property represents the raw text associated with the template part.
        /// If the part is a literal, it contains the static string value. For formatted
        /// placeholders, it may hold contextual information or string representations
        /// used during template processing.
        /// </remarks>
        public string Text { get; init; } = string.Empty;

        /// <summary>
        /// Gets or sets the format string associated with a placeholder in the template.
        /// </summary>
        /// <remarks>
        /// Represents the optional formatting specification for a dynamic placeholder within a logging template.
        /// If provided, this format string dictates how the placeholder's value will be formatted during rendering.
        /// When no format is specified, default formatting will be applied to the placeholder's value.
        /// </remarks>
        public string Format { get; init; } = string.Empty;
    }

    /// <summary>
    /// Represents a compiled template delegate with its parameter information.
    /// </summary>
    /// <remarks>
    /// Represents a compiled delegate for a template, containing the logic to execute
    /// the template and a list of its parameter names.
    /// </remarks>
    /// <param name="action">The delegate to execute the template.</param>
    /// <param name="parameterNames">The names of the template parameters.</param>
    private sealed class CompiledDelegateForTemplate(
        Action<ILogger, object?[], LogLevel> action,
        string[] parameterNames)
    {
        /// <summary>
        /// Gets the delegate that executes the compiled template logic using the provided parameters.
        /// </summary>
        /// <remarks>
        /// The Action property represents the main executable logic of the compiled template.
        /// It takes an ILogger instance for logging, an object array of parameters to feed into the template,
        /// and a LogLevel to denote the severity of the log entry. This delegate is created during template
        /// compilation and used to generate a log message with the expected format and values.
        /// </remarks>
        public Action<ILogger, object?[], LogLevel> Action { get; } = action;

        /// <summary>
        /// Gets the parameter names expected by the compiled template delegate.
        /// </summary>
        /// <remarks>
        /// The <c>ParameterNames</c> property represents the list of parameters that the compiled template
        /// expects to be provided when executing the template. These parameter names are derived during the
        /// compilation of the template and are used to correctly map input arguments to the template logic.
        /// </remarks>
        public string[] ParameterNames { get; } = parameterNames;
    }
}


