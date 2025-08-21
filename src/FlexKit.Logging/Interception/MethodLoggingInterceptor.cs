using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text.Json;
using Castle.DynamicProxy;
using FlexKit.Logging.Configuration;
using FlexKit.Logging.Core;
using FlexKit.Logging.Interception.Attributes;
using FlexKit.Logging.Models;
using Microsoft.Extensions.Logging;

namespace FlexKit.Logging.Interception;

/// <summary>
/// Castle DynamicProxy interceptor that provides automatic method logging based on cached interception decisions.
/// Uses the InterceptionDecisionCache to determine what logging behavior to apply for each intercepted method.
/// </summary>
/// <remarks>
/// Initializes a new instance of the MethodLoggingInterceptor.
/// </remarks>
/// <param name="cache">The cache containing pre-computed interception decisions.</param>
/// <param name="logQueue">The background log queue for processing log entries.</param>
/// <param name="logger">Logger for the interceptor itself.</param>
public sealed class MethodLoggingInterceptor(
    InterceptionDecisionCache cache,
    IBackgroundLog logQueue,
    ILogger<MethodLoggingInterceptor> logger) : IInterceptor
{
    private readonly InterceptionDecisionCache _cache = cache ?? throw new ArgumentNullException(nameof(cache));
    private readonly IBackgroundLog _logQueue = logQueue ?? throw new ArgumentNullException(nameof(logQueue));
    private readonly ILogger<MethodLoggingInterceptor> _logger =
        logger ?? throw new ArgumentNullException(nameof(logger));
    private static readonly Action<ILogger, Exception?> _logSerializationFailure =
        LoggerMessage.Define(
            LogLevel.Warning,
            new EventId(1),
            "Failed to serialize input parameters for logging");
    private static readonly Action<ILogger, Exception?> _logQueueFullWarning =
        LoggerMessage.Define(
            LogLevel.Warning,
            new EventId(1),
            "Failed to enqueue the logging task.");

    /// <summary>
    /// Intercepts method calls and applies logging based on cached decisions.
    /// This is the main entry point for all intercepted method calls.
    /// </summary>
    /// <param name="invocation">The method invocation context from Castle DynamicProxy.</param>
    public void Intercept(IInvocation invocation)
    {
        var decision = _cache.GetInterceptionDecision(invocation.Method);

        if (decision == null)
        {
            invocation.Proceed();
            return;
        }

        LogMethodExecution(invocation, decision.Value);
    }

    /// <summary>
    /// Handles the complete method execution logging lifecycle, including timing and exception handling.
    /// Manages the start entry creation, method execution, completion logging, and error handling.
    /// </summary>
    /// <param name="invocation">The method invocation context.</param>
    /// <param name="decision">The interception decision that determines what to log and at what level.</param>
    [SuppressMessage("ReSharper", "FlagArgument")]
    private void LogMethodExecution(
        IInvocation invocation,
        InterceptionDecision decision)
    {
        var startEntry = CreateStartEntry(invocation, decision);
        var stopwatch = Stopwatch.StartNew();
        try
        {
            invocation.Proceed();
            stopwatch.Stop();
            var durationTicks = (long)(stopwatch.ElapsedTicks * TimeSpan.TicksPerSecond / (double)Stopwatch.Frequency);
            var entry = startEntry.WithCompletion(true, durationTicks);

            if (decision.Behavior is InterceptionBehavior.LogOutput or InterceptionBehavior.LogBoth)
            {
                var output = SerializeOutputValue(invocation.ReturnValue);
                entry = entry.WithOutput(output);
            }

            TryEnqueueEntry(entry);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            var entry = startEntry.WithCompletion(false, stopwatch.ElapsedTicks, ex);
            TryEnqueueEntry(entry);
            throw;
        }
    }

    /// <summary>
    /// Creates the initial log entry when a method starts execution.
    /// Includes input parameters if the behavior requires input logging.
    /// </summary>
    /// <param name="invocation">The method invocation context.</param>
    /// <param name="decision">The interception decision that determines what to log and at what level.</param>
    /// <returns>A log entry representing the method's start with optional input parameters.</returns>
    private LogEntry CreateStartEntry(
        IInvocation invocation,
        InterceptionDecision decision)
    {
        var entry = LogEntry.CreateStart(
            invocation.Method.Name,
            invocation.Method.DeclaringType?.FullName ?? "Unknown",
            decision.Level).WithErrorLevel(decision.ExceptionLevel).WithTarget(decision.Target);

        // Add input parameters if required
        return decision.Behavior is not InterceptionBehavior.LogInput and not InterceptionBehavior.LogBoth ?
            entry : entry.WithInput(SerializeInputParameters(invocation.Arguments, invocation.Method));
    }

    /// <summary>
    /// Attempts to enqueue a log entry to the background queue with error handling for queue full scenarios.
    /// </summary>
    /// <param name="entry">The log entry to enqueue.</param>
    private void TryEnqueueEntry(in LogEntry entry)
    {
        if (_logQueue.TryEnqueue(entry))
        {
            return;
        }

        _logQueueFullWarning(_logger, null);
    }

    /// <summary>
    /// Serializes method input parameters for logging with actual parameter names.
    /// Creates a structured format that works well for both JSON and text formatters.
    /// </summary>
    /// <param name="arguments">The method arguments to serialize.</param>
    /// <param name="method">The method info to get parameter names from.</param>
    /// <returns>A JSON string containing structured parameter information.</returns>
    private string SerializeInputParameters(
        object[]? arguments,
        MethodInfo method)
    {
        try
        {
            if (arguments == null || arguments.Length == 0)
            {
                return JsonSerializer.Serialize(Array.Empty<object>());
            }

            var parameters = method.GetParameters();
            var structuredArgs = CreateParameterStructures(arguments, parameters);
            return JsonSerializer.Serialize(structuredArgs);
        }
        catch (Exception ex)
        {
            _logSerializationFailure(_logger, ex);
            return JsonSerializer.Serialize(new[] { new { error = "Serialization failed", message = ex.Message } });
        }
    }

    /// <summary>
    /// Creates structured parameter objects from method arguments and parameter metadata.
    /// Each parameter includes name, type, and serialized value information.
    /// </summary>
    /// <param name="arguments">The method arguments to structure.</param>
    /// <param name="parameters">The parameter metadata from the method.</param>
    /// <returns>An array of structured parameter objects.</returns>
    private static object[] CreateParameterStructures(
        object[] arguments,
        ParameterInfo[] parameters) =>
        [.. arguments.Select((arg, index) => CreateSingleParameterStructure(arg, index, parameters))];

    /// <summary>
    /// Creates a structured representation of a single parameter with name, type, and value.
    /// Handles cases where parameter metadata might be missing or incomplete.
    /// </summary>
    /// <param name="argument">The argument value to structure.</param>
    /// <param name="index">The parameter index in the argument list.</param>
    /// <param name="parameters">The parameter metadata array.</param>
    /// <returns>An anonymous object containing a parameter name, type, and serialized value.</returns>
    private static object CreateSingleParameterStructure(
        object? argument,
        int index,
        ParameterInfo[] parameters)
    {
        var parameterName = GetParameterName(index, parameters);
        var parameterType = GetParameterType(argument, index, parameters);
        var serializedValue = SerializeValueForJson(argument);

        return new
        {
            name = parameterName,
            type = parameterType,
            value = serializedValue
        };
    }

    /// <summary>
    /// Gets the parameter name from metadata or generates a fallback name for the given index.
    /// </summary>
    /// <param name="index">The parameter index.</param>
    /// <param name="parameters">The parameter metadata array.</param>
    /// <returns>The parameter name or a generated fallback name like "arg0".</returns>
    private static string? GetParameterName(
        int index,
        ParameterInfo[] parameters) =>
        index < parameters.Length && !string.IsNullOrEmpty(parameters[index].Name)
            ? parameters[index].Name
            : $"arg{index}";

    /// <summary>
    /// Gets the parameter type name from metadata or infers it from the argument value.
    /// </summary>
    /// <param name="argument">The argument value to infer type from if metadata is unavailable.</param>
    /// <param name="index">The parameter index.</param>
    /// <param name="parameters">The parameter metadata array.</param>
    /// <returns>The parameter type name or "null" if the argument is null and no metadata is available.</returns>
    private static string GetParameterType(
        object? argument,
        int index,
        ParameterInfo[] parameters) =>
        index < parameters.Length ? parameters[index].ParameterType.Name : argument?.GetType().Name ?? "null";

    /// <summary>
    /// Serializes method output value for logging with type information.
    /// Creates a structured format containing both the return type and serialized value.
    /// </summary>
    /// <param name="returnValue">The method's return value to serialize.</param>
    /// <returns>A JSON string containing structured output information.</returns>
    private string SerializeOutputValue(object? returnValue)
    {
        try
        {
            var result = new
            {
                type = returnValue?.GetType().Name ?? "null",
                value = SerializeValueForJson(returnValue)
            };

            return JsonSerializer.Serialize(result);
        }
        catch (Exception ex)
        {
            _logSerializationFailure(_logger, ex);
            return JsonSerializer.Serialize(new { error = "Serialization failed", message = ex.Message });
        }
    }

    /// <summary>
    /// Serializes a value specifically for JSON output, returning objects that can be properly JSON serialized.
    /// Handles various data types including primitives, collections, and complex objects with appropriate formatting.
    /// </summary>
    /// <param name="value">The value to serialize for JSON output.</param>
    /// <returns>A JSON-serializable representation of the value.</returns>
    private static object? SerializeValueForJson(object? value)
    {
        return value switch
        {
            null => null,
            string or bool or byte or sbyte or short or ushort or int or uint or long or ulong or float or double
                or decimal => value,
            DateTime dt => dt.ToString("O"),
            DateTimeOffset dto => dto.ToString("O"),
            Guid guid => guid.ToString(),

            // For collections, create a proper array structure
            System.Collections.ICollection { Count: > 10 } collection =>
                new
                {
                    _type = "Collection",
                    _count = collection.Count,
                    _truncated = true,
                    items = collection.Cast<object>().Take(3).Select(SerializeValueForJson).ToArray()
                },

            System.Collections.IEnumerable enumerable =>
                enumerable.Cast<object>().Take(10).Select(SerializeValueForJson).ToArray(),

            // For complex objects, try to create a JSON-serializable representation
            var complexObj => SerializeComplexObjectForJson(complexObj)
        };
    }

    /// <summary>
    /// Serializes complex objects for JSON output with truncation and error handling.
    /// Attempts deep serialization with cycle detection and size limits to prevent performance issues.
    /// </summary>
    /// <param name="obj">The complex object to serialize.</param>
    /// <returns>A JSON-serializable representation of the complex object with metadata.</returns>
    private static object SerializeComplexObjectForJson(object obj)
    {
        try
        {
            // Try to serialize to get actual object structure
            var options = new JsonSerializerOptions
            {
                WriteIndented = false,
                ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles,
                MaxDepth = 3,
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
            };

            // Serialize and deserialize to get a clean object structure
            var json = JsonSerializer.Serialize(obj, options);

            if (json.Length > 2000)
            {
                return new
                {
                    _type = obj.GetType().Name,
                    _truncated = true,
                    _preview = json[..100] + "..."
                };
            }

            return JsonSerializer.Deserialize<object>(json, options) ?? new { _type = obj.GetType().Name, _value = obj.ToString() };
        }
        catch
        {
            return new
            {
                _type = obj.GetType().Name,
                _error = "Serialization failed",
                _toString = obj.ToString() ?? "null"
            };
        }
    }
}
