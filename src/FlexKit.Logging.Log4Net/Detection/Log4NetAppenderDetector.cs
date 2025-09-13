using System.Diagnostics;
using System.Reflection;
using Microsoft.Extensions.DependencyModel;

namespace FlexKit.Logging.Log4Net.Detection;

/// <summary>
/// Auto-detects available Log4Net appenders by scanning loaded assemblies for
/// classes that inherit from log4net.Appender.IAppender and can be configured with FlexKit.
/// </summary>
internal static class Log4NetAppenderDetector
{
    /// <summary>
    /// Information about a detected Log4Net appender, including its configuration details.
    /// </summary>
    internal sealed class AppenderInfo
    {
        /// <summary>
        /// Gets the name of the detected Log4Net appender.
        /// The name corresponds to the appender's type name or alias.
        /// </summary>
        public string Name { get; init; } = string.Empty;

        /// <summary>
        /// Gets the Type representing the detected Log4Net appender class.
        /// This type implements log4net.Appender.IAppender.
        /// </summary>
        public required Type AppenderType { get; init; }

        /// <summary>
        /// Gets the properties available for configuring this appender.
        /// Each property can be set through FlexKit configuration.
        /// </summary>
        public PropertyInfo[] Properties { get; init; } = [];

        /// <summary>
        /// Gets a value indicating whether this appender requires activation.
        /// Some appenders need explicit activation after configuration.
        /// </summary>
        public bool RequiresActivation { get; init; }
    }

    /// <summary>
    /// Detects all available Log4Net appenders by scanning loaded assemblies for
    /// classes that implement log4net.Appender.IAppender.
    /// </summary>
    /// <returns>Dictionary mapping appender names to their configuration information.</returns>
    public static Dictionary<string, AppenderInfo> DetectAvailableAppenders()
    {
        var appenders = new Dictionary<string, AppenderInfo>(StringComparer.OrdinalIgnoreCase);

        // Find all assemblies that might contain Log4Net appenders
        foreach (var assembly in GetLog4NetAssemblies())
        {
            try
            {
                DetectAppendersInAssembly(assembly, appenders);
            }
            catch (Exception ex)
            {
                // Log warning but continue - one bad assembly shouldn't break everything
                Debug.WriteLine(
                    $"Warning: Failed to scan assembly {assembly.FullName} for Log4Net appenders: {ex.Message}");
            }
        }

        return appenders;
    }

    /// <summary>
    /// Gets all assemblies that might contain Log4Net appenders.
    /// Includes Log4Net core assembly and any Log4Net extension assemblies.
    /// </summary>
    /// <returns>Array of assemblies to scan for Log4Net appenders.</returns>
    private static Assembly[] GetLog4NetAssemblies()
    {
        var assemblies = new List<Assembly>();

        try
        {
            // Get assemblies from DependencyContext if available (preferred method)
            var dependencyAssemblies = DependencyContext.Default?.RuntimeLibraries
                .Where(lib => lib.Name.StartsWith("log4net", StringComparison.OrdinalIgnoreCase))
                .SelectMany(lib => lib.GetDefaultAssemblyNames(DependencyContext.Default))
                .Select(Assembly.Load)
                .ToArray() ?? [];

            assemblies.AddRange(dependencyAssemblies);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Warning: Failed to load assemblies from DependencyContext: {ex.Message}");
        }

        // Fallback: scan currently loaded assemblies
        if (assemblies.Count == 0)
        {
            assemblies.AddRange(AppDomain.CurrentDomain.GetAssemblies()
                .Where(a => a.FullName?.Contains("log4net", StringComparison.OrdinalIgnoreCase) == true));
        }

        return [.. assemblies];
    }

    /// <summary>
    /// Scans the given assembly to detect available Log4Net appenders.
    /// </summary>
    /// <param name="assembly">The assembly to search for Log4Net appenders.</param>
    /// <param name="appenders">A dictionary to store the detected appenders along with their metadata.</param>
    private static void DetectAppendersInAssembly(
        Assembly assembly,
        Dictionary<string, AppenderInfo> appenders)
    {
        try
        {
            var types = assembly.GetTypes()
                .Where(IsValidAppenderType)
                .ToArray();

            foreach (var type in types)
            {
                var appenderInfo = CreateAppenderInfo(type);
                if (appenderInfo != null)
                {
                    appenders.TryAdd(appenderInfo.Name, appenderInfo);
                }
            }
        }
        catch (ReflectionTypeLoadException ex)
        {
            // Handle partial loading - some types might not be available
            var availableTypes = ex.Types.Where(t => t != null).Cast<Type>();
            foreach (var type in availableTypes.Where(IsValidAppenderType))
            {
                var appenderInfo = CreateAppenderInfo(type);
                if (appenderInfo != null)
                {
                    appenders.TryAdd(appenderInfo.Name, appenderInfo);
                }
            }
        }
    }

    /// <summary>
    /// Determines whether a type is a valid Log4Net appender that can be instantiated and configured.
    /// </summary>
    /// <param name="type">The type to evaluate.</param>
    /// <returns>True if the type is a valid Log4Net appender; otherwise, false.</returns>
    private static bool IsValidAppenderType(Type type)
    {
        // Must be a concrete class (not abstract, not interface)
        if (!type.IsClass || type.IsAbstract || !type.IsPublic)
        {
            return false;
        }

        if (type.GetInterfaces().Any(i => i is { Name: "IAppender", Namespace: "log4net.Appender" }))
        {
            return true;
        }

        // Alternative: Check for common Log4Net appender base classes
        var baseType = type.BaseType;
        while (baseType != null)
        {
            if (baseType.Namespace == "log4net.Appender" &&
                (baseType.Name.Contains("Appender") || baseType.Name == "AppenderSkeleton"))
            {
                return true;
            }
            baseType = baseType.BaseType;
        }

        return false;
    }

    /// <summary>
    /// Creates an AppenderInfo object for a detected Log4Net appender type.
    /// </summary>
    /// <param name="appenderType">The appender type to analyze.</param>
    /// <returns>AppenderInfo object with appender metadata, or null if the appender cannot be analyzed.</returns>
    private static AppenderInfo? CreateAppenderInfo(Type appenderType)
    {
        try
        {
            return CreateAppenderInfoFromType(appenderType);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Creates an <see cref="AppenderInfo"/> object from the specified appender type by extracting its
    /// name, configurable properties, and activation requirements.
    /// </summary>
    /// <param name="appenderType">The type of the appender to analyze.</param>
    /// <returns>
    /// An <see cref="AppenderInfo"/> object with configuration and capability details,
    /// or null if the information cannot be determined.
    /// </returns>
    private static AppenderInfo CreateAppenderInfoFromType(Type appenderType)
    {
        // Determine an appender name (use type name without "Appender" suffix if present)
        var appenderName = appenderType.Name;
        if (appenderName.EndsWith("Appender", StringComparison.InvariantCulture))
        {
            appenderName = appenderName[..^8]; // Remove "Appender" suffix
        }

        // Get configurable properties
        var properties = appenderType
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p is { CanWrite: true, SetMethod.IsPublic: true })
            .ToArray();

        // Check appender capabilities
        var requiresActivation = CheckActivationRequirement(appenderType);

        return new AppenderInfo
        {
            Name = appenderName,
            AppenderType = appenderType,
            Properties = properties,
            RequiresActivation = requiresActivation,
        };
    }

    /// <summary>
    /// Determines if an appender type requires explicit activation after configuration.
    /// </summary>
    /// <param name="appenderType">The appender type to check.</param>
    /// <returns>True if the appender requires activation; otherwise, false.</returns>
    private static bool CheckActivationRequirement(Type appenderType)
    {
        // Check for IOptionHandler interface (indicates configuration activation needed)
        var interfaces = appenderType.GetInterfaces();
        if (interfaces.Any(i => i is { Name: "IOptionHandler", Namespace: "log4net.Core" }))
        {
            return true;
        }

        // Check for the ActivateOptions method
        var methods = appenderType.GetMethods(BindingFlags.Public | BindingFlags.Instance);
        return methods.Any(m => m.Name == "ActivateOptions");
    }
}
