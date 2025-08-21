using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq.Expressions;
using System.Reflection;
using FlexKit.Logging.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace FlexKit.Logging.Core;

/// <summary>
/// A factory class responsible for managing and configuring logging providers
/// for the Microsoft.Extensions.Logging framework. Enables dynamic management
/// of log provider configurations based on a logging configuration model.
/// </summary>
public class MelProviderFactory
{
    private readonly Dictionary<string, Action<LoggingTarget>> _providerConfigurers = new();
    private readonly LoggingConfig _config;
    private readonly ILoggingBuilder _builder;
    private readonly string[] _types;

    public MelProviderFactory(ILoggingBuilder builder, LoggingConfig config)
    {
        _builder = builder;
        _config = config;
        _types = config.Targets.Values.Select(t => t.Type).Distinct().ToArray();
        _providerConfigurers["Debug"] = TryAddDebug;
        _providerConfigurers["Console"] = TryAddConsole;
    }

    public void ConfigureProviders()
    {
        _builder.ClearProviders();

        // 1) Add the providers you discovered
        foreach (var target in _config.Targets.Values.Where(t => t.Enabled).ToArray())
        {
            if (_providerConfigurers.TryGetValue(target.Type, out var configurer))
            {
                configurer(target);
            }
        }
    }

    private void TryAddDebug(LoggingTarget target)
    {
        var type = Type.GetType(
            "Microsoft.Extensions.Logging.DebugLoggerFactoryExtensions, Microsoft.Extensions.Logging.Debug");
        var method = type?.GetMethod("AddDebug", new[] { typeof(ILoggingBuilder) });
        method?.Invoke(null, [_builder]);

        // Add filters for this provider
        AddFiltersForProvider(
            Type.GetType("Microsoft.Extensions.Logging.Debug.DebugLoggerProvider, Microsoft.Extensions.Logging.Debug"),
            target);
    }

    private void TryAddConsole(LoggingTarget target)
    {
        var type = Type.GetType("Microsoft.Extensions.Logging.ConsoleLoggerExtensions, Microsoft.Extensions.Logging.Console");

        target.Properties.TryGetValue("FormatterType", out var formatter);

        var methodName = formatter?.Value switch
        {
            "Simple" => "AddSimpleConsole",
            "Systemd" => "AddSystemdConsole",
            "Json" => "AddJsonConsole",
            _ => "AddSimpleConsole"
        };

        // Check if we have formatter options to configure
        var configMethodType = GetFormatterOptionsType(methodName);
        if (configMethodType != null)
        {
            var actionType = typeof(Action<>).MakeGenericType(configMethodType);
            var method = type?.GetMethod(methodName, [typeof(ILoggingBuilder), actionType]);

            if (method != null)
            {
                var configAction = CreateConfigurationAction(target, configMethodType);
                method.Invoke(null, [_builder, configAction]);
            }
            else
            {
                var fallbackMethod = type?.GetMethod(methodName, [typeof(ILoggingBuilder)]);
                fallbackMethod?.Invoke(null, [_builder]);
            }
        }

        // Add filters for this provider
        AddFiltersForProvider(
            Type.GetType("Microsoft.Extensions.Logging.Console.ConsoleLoggerProvider, Microsoft.Extensions.Logging.Console"),
            target);
    }

    private static Type? GetFormatterOptionsType(string methodName)
    {
        return methodName switch
        {
            "AddSimpleConsole" => Type.GetType("Microsoft.Extensions.Logging.Console.SimpleConsoleFormatterOptions, Microsoft.Extensions.Logging.Console"),
            "AddSystemdConsole" => Type.GetType("Microsoft.Extensions.Logging.Console.ConsoleFormatterOptions, Microsoft.Extensions.Logging.Console"),
            "AddJsonConsole" => Type.GetType("Microsoft.Extensions.Logging.Console.JsonConsoleFormatterOptions, Microsoft.Extensions.Logging.Console"),
            _ => null
        };
    }

    [SuppressMessage("Major Code Smell", "S3011:Reflection should not be used to increase accessibility of classes, methods, or fields")]
    private static Delegate CreateConfigurationAction(LoggingTarget target, Type optionsType)
    {
        var actionType = typeof(Action<>).MakeGenericType(optionsType);

        // Create a closure that captures the target
        var configureMethod = typeof(MelProviderFactory).GetMethod(nameof(ConfigureOptions), BindingFlags.NonPublic | BindingFlags.Static)
            ?.MakeGenericMethod(optionsType);

        if (configureMethod == null)
        {
            throw new InvalidOperationException("ConfigureOptions method not found");
        }

        // Create the action using a lambda that captures target
        var parameter = Expression.Parameter(optionsType, "options");
        var targetConstant = Expression.Constant(target);
        var methodCall = Expression.Call(configureMethod, targetConstant, parameter);
        var lambda = Expression.Lambda(actionType, methodCall, parameter);

        return lambda.Compile();
    }

    private static void ConfigureOptions<T>(LoggingTarget target, T options) where T : class
    {
        var properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanWrite);

        foreach (var prop in properties)
        {
            if (target.Properties.TryGetValue(prop.Name, out var value))
            {
                try
                {
                    if (value == null)
                    {
                        return;
                    }

                    try
                    {
                        object? convertedValue;

                        if (value.Value == null)
                        {
                            convertedValue = Convert.ChangeType(value.Get(prop.PropertyType), prop.PropertyType, CultureInfo.InvariantCulture);
                        }
                        else if (prop.PropertyType.IsEnum)
                        {
                            // Handle enum conversion from string
                            convertedValue = Enum.Parse(prop.PropertyType, value.Value, true);
                        }
                        else
                        {
                            convertedValue = Convert.ChangeType(value.Value, prop.PropertyType, CultureInfo.InvariantCulture);
                        }

                        prop.SetValue(options, convertedValue);
                    }
                    catch
                    {
                        // Skip invalid conversions
                    }
                }
                catch
                {
                    // Skip invalid conversions
                }
            }
        }
    }

    private void AddFiltersForProvider(Type? providerType, LoggingTarget target)
    {
        if (providerType == null)
        {
            return;
        }

        var filterType = Type.GetType("Microsoft.Extensions.Logging.FilterLoggingBuilderExtensions, Microsoft.Extensions.Logging");

        // Find the generic AddFilter<T> method: AddFilter<T>(ILoggingBuilder, string?, LogLevel)
        var genericMethod = filterType?.GetMethods()
            .FirstOrDefault(m =>
                m.Name == "AddFilter" &&
                m.IsGenericMethodDefinition &&
                m.GetParameters().Length == 3 &&
                m.GetParameters()[1].ParameterType == typeof(string) &&
                m.GetParameters()[2].ParameterType == typeof(LogLevel));

        if (genericMethod == null)
        {
            return;
        }

        // Make it specific to our provider type
        var specificMethod = genericMethod.MakeGenericMethod(providerType);

        // Block other categories from this provider
        foreach (var otherCategory in _types.Where(t => t != target.Type))
        {
            specificMethod.Invoke(null, [_builder, otherCategory, LogLevel.None]);
        }

        // Allow this target's category through
        specificMethod.Invoke(null, [_builder, target.Type, GetLogLevel(target)]);
    }

    private static LogLevel GetLogLevel(LoggingTarget target)
    {
        if (!target.Properties.TryGetValue("LogLevel", out var level))
        {
            return LogLevel.Information;
        }

        return level?.Value is string levelString && Enum.TryParse<LogLevel>(levelString, out var parsedLevel)
            ? parsedLevel
            : LogLevel.Information;

    }
}
