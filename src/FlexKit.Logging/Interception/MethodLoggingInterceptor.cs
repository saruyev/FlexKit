using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using Castle.DynamicProxy;
using FlexKit.Logging.Configuration;
using FlexKit.Logging.Core;
using FlexKit.Logging.Models;
using Microsoft.Extensions.Logging;

namespace FlexKit.Logging.Interception;

/// <summary>
/// Castle DynamicProxy interceptor that provides automatic method logging based on cached interception decisions.
/// Uses the InterceptionDecisionCache to determine what logging behavior to apply for each intercepted method.
/// Supports both synchronous and asynchronous methods with proper async result logging.
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
    private record struct CompletionDetails(
        LogEntry Entry,
        Stopwatch Stopwatch,
        bool Success = true,
        Exception? Exception = null);

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
    /// Supports both synchronous and asynchronous methods.
    /// </summary>
    /// <param name="invocation">The method invocation context.</param>
    /// <param name="decision">The interception decision that determines what to log and at what level.</param>
    [SuppressMessage("ReSharper", "FlagArgument")]
    private void LogMethodExecution(
        IInvocation invocation,
        in InterceptionDecision decision)
    {
        var details = new CompletionDetails(
            Entry: CreateStartEntry(invocation, decision),
            Stopwatch: Stopwatch.StartNew());

        try
        {
            invocation.Proceed();

            // Check if the return value is a Task (async method)
            if (invocation.ReturnValue is Task task)
            {
                HandleAsyncCompletion(task, decision, details);
            }
            else
            {
                // Handle synchronous method completion
                HandleSyncCompletion(invocation, decision, details);
            }
        }
        catch (Exception ex)
        {
            // Handle synchronous exceptions
            details.Stopwatch.Stop();
            var durationTicks =
                (long)(details.Stopwatch.ElapsedTicks * TimeSpan.TicksPerSecond / (double)Stopwatch.Frequency);
            var entry = details.Entry.WithCompletion(false, durationTicks, ex);
            TryEnqueueEntry(entry);
            throw;
        }
    }

    /// <summary>
    /// Handles completion logging for synchronous methods.
    /// </summary>
    /// <param name="invocation">The method invocation context.</param>
    /// <param name="decision">The interception decision determining what to log.</param>
    /// <param name="details">The completion details including timing and status.</param>
    private void HandleSyncCompletion(
        IInvocation invocation,
        in InterceptionDecision decision,
        CompletionDetails details)
    {
        details.Stopwatch.Stop();
        var entry = CreateCompletionEntry(details);

        if (ShouldLogOutput(decision))
        {
            var output = SerializeOutputValue(invocation.ReturnValue);
            entry = entry.WithOutput(output);
        }

        TryEnqueueEntry(entry);
    }

    /// <summary>
    /// Handles completion logging for asynchronous methods using ContinueWith to avoid blocking.
    /// </summary>
    /// <param name="task">The Task returned by the async method.</param>
    /// <param name="decision">The interception decision determining what to log.</param>
    /// <param name="details">The completion details including timing and status.</param>
    private void HandleAsyncCompletion(
        Task task,
        InterceptionDecision decision,
        CompletionDetails details) =>
        _ = task.ContinueWith(completedTask =>
                LogAsyncCompletion(completedTask, decision, details),
            TaskContinuationOptions.ExecuteSynchronously);

    /// <summary>
    /// Logs the completion of an async method. Called by ContinueWith.
    /// </summary>
    /// <param name="completedTask">The completed task.</param>
    /// <param name="decision">The interception decision.</param>
    /// <param name="details">The completion details including timing and status.</param>
    private void LogAsyncCompletion(
        Task completedTask,
        in InterceptionDecision decision,
        CompletionDetails details)
    {
        details.Stopwatch.Stop();

        try
        {
            details.Success = completedTask.Status == TaskStatus.RanToCompletion;
            details.Exception = completedTask.Exception?.GetBaseException();
            var entry = CreateCompletionEntry(details);

            if (details.Success && ShouldLogOutput(decision))
            {
                var actualResult = ExtractTaskResult(completedTask);
                var output = SerializeOutputValue(actualResult);
                entry = entry.WithOutput(output);
            }

            TryEnqueueEntry(entry);
        }
        catch (Exception ex)
        {
            details.Success = false;
            details.Exception = ex;
            LogAsyncCompletionFailure(details);
        }
    }

    /// <summary>
    /// Creates a completion log entry with timing and status information.
    /// </summary>
    /// <param name="details">The completion details including timing and status.</param>
    /// <returns>A log entry with completion information.</returns>
    private static LogEntry CreateCompletionEntry(CompletionDetails details)
    {
        var durationTicks =
            (long)(details.Stopwatch.ElapsedTicks * TimeSpan.TicksPerSecond / (double)Stopwatch.Frequency);
        return details.Entry.WithCompletion(details.Success, durationTicks, details.Exception);
    }

    /// <summary>
    /// Determines if output should be logged based on the decision behavior.
    /// </summary>
    /// <param name="decision">The interception decision.</param>
    /// <returns>True if output should be logged.</returns>
    private static bool ShouldLogOutput(in InterceptionDecision decision) =>
        decision.Behavior is InterceptionBehavior.LogOutput or InterceptionBehavior.LogBoth;

    /// <summary>
    /// Logs a failure that occurred while trying to log async completion.
    /// </summary>
    /// <param name="details">The completion details including timing and status.</param>
    private void LogAsyncCompletionFailure(CompletionDetails details)
    {
        _logSerializationFailure(_logger, details.Exception);
        var fallbackEntry = CreateCompletionEntry(details);
        TryEnqueueEntry(fallbackEntry);
    }

    /// <summary>
    /// Extracts the actual result value from a completed Task.
    /// Handles both Task (non-generic) and Task&lt;T&gt; (generic) types, including compiler-generated async state machines.
    /// </summary>
    /// <param name="task">The completed task to extract the result from.</param>
    /// <returns>The actual result value for Task&lt;T&gt;, or null for non-generic Task.</returns>
    private static object? ExtractTaskResult(Task task)
    {
        if (task.Status != TaskStatus.RanToCompletion)
        {
            return null;
        }

        try
        {
            var resultProperty = task.GetType().GetProperty("Result");
            if (resultProperty != null && HasValidResultType(resultProperty.PropertyType))
            {
                return resultProperty.GetValue(task);
            }

            // Check inheritance chain and interfaces for Task<T>
            return ExtractFromTaskInterface(task);
        }
        catch (Exception)
        {
            return null;
        }
    }

    /// <summary>
    /// Determines if a result type is valid (not void, object, or Task).
    /// </summary>
    /// <param name="resultType">The type to check.</param>
    /// <returns>True if the type represents a valid task result.</returns>
    private static bool HasValidResultType(Type resultType) =>
        resultType != typeof(void) &&
        resultType != typeof(object) &&
        !typeof(Task).IsAssignableFrom(resultType);

    /// <summary>
    /// Attempts to extract the result by finding Task&lt;T&gt; in the type hierarchy or interfaces.
    /// </summary>
    /// <param name="task">The task to extract the result from.</param>
    /// <returns>The result value or null if not found.</returns>
    private static object? ExtractFromTaskInterface(Task task)
    {
        var taskType = task.GetType();

        // Check base types
        var baseType = taskType.BaseType;
        while (baseType != null)
        {
            if (IsGenericTask(baseType))
            {
                var resultProperty = baseType.GetProperty("Result");
                return resultProperty?.GetValue(task);
            }
            baseType = baseType.BaseType;
        }

        // Check interfaces
        return taskType.GetInterfaces()
            .Where(IsGenericTask)
            .Select(interfaceType =>
                typeof(Task<>).MakeGenericType(interfaceType.GetGenericArguments()[0]).GetProperty("Result"))
            .Select(resultProperty => resultProperty?.GetValue(task)).FirstOrDefault();
    }

    /// <summary>
    /// Checks if a type is a generic Task&lt;T&gt;
    /// </summary>
    /// <param name="type">The type to check.</param>
    /// <returns>True if the type is Task&lt;T&gt;</returns>
    private static bool IsGenericTask(Type type) =>
        type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Task<>);

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
                ReferenceHandler = ReferenceHandler.IgnoreCycles,
                MaxDepth = 3,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
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

            return JsonSerializer.Deserialize<object>(json, options) ??
                   new { _type = obj.GetType().Name, _value = obj.ToString() };
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
