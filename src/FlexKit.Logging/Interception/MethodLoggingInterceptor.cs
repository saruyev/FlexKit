using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Castle.DynamicProxy;
using FlexKit.Logging.Configuration;
using FlexKit.Logging.Core;
using FlexKit.Logging.Formatting.Utils;
using FlexKit.Logging.Models;
using Microsoft.Extensions.Logging;
// ReSharper disable NullableWarningSuppressionIsUsed

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
internal sealed class MethodLoggingInterceptor(
    InterceptionDecisionCache cache,
    IBackgroundLog logQueue,
    ILogger<MethodLoggingInterceptor> logger) : IInterceptor
{
    /// <summary>
    /// Represents a private readonly instance of <see cref="InterceptionDecisionCache"/> used within
    /// the <see cref="MethodLoggingInterceptor"/> to cache decisions related to method interception.
    /// This cache is used for determining interception behavior, configuration, and logging details.
    /// </summary>
    private readonly InterceptionDecisionCache _cache = cache ?? throw new ArgumentNullException(nameof(cache));

    /// <summary>
    /// Represents a private readonly instance of <see cref="IBackgroundLog"/> used for handling background
    /// logging operations. This queue is responsible for enqueueing log entries and managing asynchronous
    /// logging workflows within <see cref="MethodLoggingInterceptor"/>.
    /// Ensures that log messages are processed efficiently without blocking the main execution flow.
    /// </summary>
    private readonly IBackgroundLog _logQueue = logQueue ?? throw new ArgumentNullException(nameof(logQueue));

    /// <summary>
    /// Represents an instance of <see cref="ILogger{TCategoryName}"/> specifically for
    /// the <see cref="MethodLoggingInterceptor"/> class. This logger is used for handling
    /// logging operations, including asynchronous logging failures and warnings when the
    /// logging queue is full.
    /// </summary>
    private readonly ILogger<MethodLoggingInterceptor> _logger =
        logger ?? throw new ArgumentNullException(nameof(logger));

    /// <summary>
    /// Defines a static, cached logging action for serialization failure messages during logging interception.
    /// Used to log warnings when input parameter serialization fails, improving log consistency and performance.
    /// </summary>
    private static readonly Action<ILogger, Exception?> _logSerializationFailure =
        LoggerMessage.Define(
            LogLevel.Warning,
            new EventId(1),
            "Failed to serialize input parameters for logging");

    /// <summary>
    /// Represents a static logger action used to log a warning when the background log queue fails
    /// to enqueue a logging task. Primarily used within the <see cref="MethodLoggingInterceptor"/> to notify about
    /// potential logging issues, such as the background log queue being full or unavailable.
    /// </summary>
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
            Stopwatch: Stopwatch.StartNew(),
            DeclaringType: FindDeclaringType(invocation.Method.DeclaringType!));

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
            entry = entry.WithOutput(
                invocation.ReturnValue.ApplyOutputMasking(
                    details.DeclaringType,
                    _cache.Config));
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
                entry = entry.WithOutput(
                    ExtractTaskResult(completedTask)
                    .ApplyOutputMasking(details.DeclaringType, _cache.Config));
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
            var resultProperty = task.GetType().GetProperty(
                "Result",
                BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.Instance);
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

        return null;
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
        in InterceptionDecision decision)
    {
        var entry = LogEntry.CreateStart(
                invocation.Method.Name,
                invocation.Method.DeclaringType?.FullName ?? "Unknown",
                decision.Level)
            .WithErrorLevel(decision.ExceptionLevel)
            .WithTarget(decision.Target)
            .WithFormatter(decision.Formatter);

        // Add input parameters if required
        return decision.Behavior is not InterceptionBehavior.LogInput and not InterceptionBehavior.LogBoth
            ? entry
            : entry.WithInput(
                CreateParameterStructures(
                    new InputContext(
                        invocation.Arguments,
                        invocation.Method.GetParameters(),
                        FindDeclaringType(invocation.Method.DeclaringType!),
                        _cache.Config)));
    }

    /// <summary>
    /// Identifies the declaring type for the provided type definition, resolving interfaces
    /// to their implementing types where applicable, and returning the original type if no
    /// implementation mapping exists or if the type is not an interface.
    /// </summary>
    /// <param name="declaringType">The type to evaluate, which could be an interface or a class.</param>
    /// <returns>
    /// The resolved type if the input is an interface with a mapped implementation.
    /// Returns the input type itself if it is not an interface or no mapping is found.
    /// </returns>
    private Type FindDeclaringType(Type declaringType) =>
        declaringType.IsInterface
            ? _cache.FindImplementationType(declaringType) ?? declaringType
        : declaringType;

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
    /// Creates structured parameter objects from method arguments and parameter metadata.
    /// Each parameter includes a name, type, and serialized value information.
    /// </summary>
    /// <param name="context">The input context containing method arguments and metadata.</param>
    /// <returns>An array of structured parameter objects.</returns>
    private static object[] CreateParameterStructures(InputContext context) =>
        [.. context.Arguments.Select((arg, index) => CreateSingleParameterStructure(arg, index, context))];

    /// <summary>
    /// Creates a structured representation of a single parameter with name, type, and value.
    /// Handles cases where parameter metadata might be missing or incomplete.
    /// </summary>
    /// <param name="argument">The argument value to structure.</param>
    /// <param name="index">The parameter index in the argument list.</param>
    /// <param name="context">The input context containing method arguments and metadata.</param>
    /// <returns>An anonymous object containing a parameter name, type, and serialized value.</returns>
    private static InputParameter CreateSingleParameterStructure(
        object? argument,
        int index,
        InputContext context)
    {
        var paramName = GetParameterName(index, context.Parameters);
        var paramType = GetParameterType(argument, index, context.Parameters);

        // Apply masking to the argument value
        var maskedValue = argument.ApplyParameterMasking(
            index < context.Parameters.Length ? context.Parameters[index] : null,
            context);
        return new InputParameter(paramName, paramType, maskedValue);
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
    /// Represents the context for method input parameters, encapsulating method arguments, parameter metadata,
    /// and the declaring type. Useful for scenarios like logging or method interception where detailed information about
    /// inputs is required.
    /// </summary>
    /// <remarks>
    /// Provides a structured encapsulation of method input data, including arguments, parameter details, and the type
    /// that declares the method. Typically used in logging or interception frameworks to facilitate operations like parameter
    /// serialization or validation.
    /// </remarks>
    /// <param name="Arguments">The array of arguments passed to the method.</param>
    /// <param name="Parameters">The metadata describing the method parameters.</param>
    /// <param name="DeclaringType">The type that declares the method being invoked.</param>
    /// <param name="Config">The logging configuration.</param>
    internal sealed record InputContext(
        object[] Arguments,
        ParameterInfo[] Parameters,
        Type DeclaringType,
        LoggingConfig Config);

    /// <summary>
    /// Represents the details of a method execution used for logging completion information.
    /// Captures metadata including the log entry, stopwatch timing, success status, and any exceptions that occurred.
    /// </summary>
    /// <param name="Entry">The log entry representing the method execution details.</param>
    /// <param name="Stopwatch">The stopwatch instance tracking the duration of the method execution.</param>
    /// <param name="DeclaringType">The type that declares the method being invoked.</param>
    /// <param name="Success">Indicates whether the method execution completed successfully. Defaults to true.</param>
    /// <param name="Exception">The exception, if any, that was thrown during method execution. Defaults to null.</param>
    private record struct CompletionDetails(
        LogEntry Entry,
        Stopwatch Stopwatch,
        Type DeclaringType,
        bool Success = true,
        Exception? Exception = null);
}
