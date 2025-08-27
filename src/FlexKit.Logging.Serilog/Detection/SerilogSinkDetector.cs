using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.DependencyModel;
using Serilog;
using Serilog.Configuration;

namespace FlexKit.Logging.Serilog.Detection;

/// <summary>
/// Auto-detects available Serilog sinks and enrichers by scanning loaded assemblies for
/// LoggerSinkConfiguration and LoggerEnrichmentConfiguration extension methods.
/// </summary>
public static class SerilogComponentDetector
{
    /// <summary>
    /// Information about a detected Serilog component (sink or enricher) including its configuration
    /// method and parameters.
    /// </summary>
    public class ComponentInfo
    {
        /// <summary>
        /// Gets the name of the detected Serilog component, such as a sink or enricher.
        /// The name corresponds to the component's configuration method name.
        /// </summary>
        public string Name { get; init; } = string.Empty;

        /// <summary>
        /// Gets the MethodInfo representing the detected Serilog component's configuration method.
        /// This method contains the implementation details for integrating the component
        /// (e.g., sink or enricher) with Serilog.
        /// </summary>
        public required MethodInfo Method { get; init; }

        /// <summary>
        /// Gets the parameters of the detected Serilog component's configuration method.
        /// Each parameter provides metadata about expected inputs for the method.
        /// </summary>
        public ParameterInfo[] Parameters { get; init; } = [];

        /// <summary>
        /// Gets the name of the assembly containing the detected Serilog component,
        /// such as a sink or enricher.
        /// This property indicates the source assembly from which the component originates.
        /// </summary>
        public string AssemblyName { get; init; } = string.Empty;
    }

    /// <summary>
    /// Type of Serilog component.
    /// </summary>
    public enum ComponentType
    {
        /// <summary>
        /// Represents a Serilog sink component type. Sinks are responsible for defining
        /// destinations for log messages, such as files, databases, or external services.
        /// </summary>
        Sink = 0,

        /// <summary>
        /// Represents a Serilog enricher component type. Enrichers are used to augment log events
        /// with additional contextual properties or metadata, such as user information, machine names,
        /// or request identifiers.
        /// </summary>
        Enricher = 1
    }

    /// <summary>
    /// Detects all available Serilog sinks by scanning loaded assemblies for
    /// extension methods on LoggerSinkConfiguration.
    /// </summary>
    /// <returns>Dictionary mapping sink names to their configuration information.</returns>
    public static Dictionary<string, ComponentInfo> DetectAvailableSinks() =>
        DetectComponents(typeof(LoggerSinkConfiguration), ComponentType.Sink);

    /// <summary>
    /// Detects all available Serilog enrichers by scanning loaded assemblies for
    /// extension methods on LoggerEnrichmentConfiguration.
    /// </summary>
    /// <returns>Dictionary mapping enricher names to their configuration information.</returns>
    public static Dictionary<string, ComponentInfo> DetectAvailableEnrichers() =>
        DetectComponents(typeof(LoggerEnrichmentConfiguration), ComponentType.Enricher);

    /// <summary>
    /// Detects all available Serilog components (sinks or enrichers) by scanning loaded assemblies and
    /// identifying extension methods associated with a specified configuration type.
    /// </summary>
    /// <param name="configurationType">
    /// The configuration type to scan for (e.g., LoggerSinkConfiguration or LoggerEnrichmentConfiguration).
    /// </param>
    /// <param name="componentType">The type of component to detect (e.g., sink or enricher).</param>
    /// <returns>A dictionary mapping component names to their corresponding ComponentInfo objects.</returns>
    private static Dictionary<string, ComponentInfo> DetectComponents(
        Type configurationType,
        ComponentType componentType)
    {
        var components = new Dictionary<string, ComponentInfo>(StringComparer.OrdinalIgnoreCase);

        // Find all assemblies that might contain Serilog components
        foreach (var assembly in GetSerilogAssemblies())
        {
            try
            {
                DetectComponentsInAssembly(assembly, configurationType, components);
            }
            catch (Exception ex)
            {
                // Log warning but continue - one bad assembly shouldn't break everything
                Console.WriteLine($"Warning: Failed to scan assembly {assembly.FullName} for Serilog {componentType.ToString().ToLowerInvariant()}s: {ex.Message}");
            }
        }

        return components;
    }

    /// <summary>
    /// Gets all assemblies that might contain Serilog components.
    /// </summary>
    private static Assembly[] GetSerilogAssemblies() =>
        DependencyContext.Default?.RuntimeLibraries
            .Where(lib => lib.Name.StartsWith("Serilog", StringComparison.OrdinalIgnoreCase))
            .SelectMany(lib => lib.GetDefaultAssemblyNames(DependencyContext.Default))
            .Select(Assembly.Load)
            .ToArray() ?? [];

    /// <summary>
    /// Scans the given assembly to detect available Serilog components (such as sinks or enrichers),
    /// by identifying extension methods on the specified configuration type.
    /// </summary>
    /// <param name="assembly">The assembly to search for Serilog components.</param>
    /// <param name="configurationType">The type of Logger configuration (sink or enricher) to detect.</param>
    /// <param name="components">A dictionary to store the detected components along with their metadata.</param>
    private static void DetectComponentsInAssembly(
        Assembly assembly,
        Type configurationType,
        Dictionary<string, ComponentInfo> components)
    {
        // Find static classes that might contain extension methods
        var extensionClasses = assembly.GetTypes()
            .Where(t => t.IsSealed && t is { IsAbstract: true, IsPublic: true }) // Static classes
            .ToArray();

        foreach (var extensionClass in extensionClasses)
        {
            DetectComponentsInType(extensionClass, configurationType, assembly, components);
        }
    }

    private static void DetectComponentsInType(
        Type extensionClass,
        Type configurationType,
        Assembly assembly,
        Dictionary<string, ComponentInfo> components)
    {
        var methods = extensionClass.GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Where(m => IsValidComponentExtensionMethod(m, configurationType))
            .ToArray();

        foreach (var method in methods)
        {
            var componentName = method.Name;

            // Skip if we already have this component (the first one wins)
            if (components.ContainsKey(componentName))
            {
                continue;
            }

            var parameters = method.GetParameters().Skip(1).ToArray(); // Skip configuration parameter

            components[componentName] = new ComponentInfo
            {
                Name = componentName,
                Method = method,
                Parameters = parameters,
                AssemblyName = assembly.GetName().Name ?? "Unknown"
            };
        }
    }

    /// <summary>
    /// Determines whether the specified method is a valid extension method for
    /// configuring a Serilog component, such as a sink or enricher,
    /// based on the given configuration type.
    /// </summary>
    /// <param name="method">
    /// The method to evaluate.
    /// </param>
    /// <param name="configurationType">
    /// The expected configuration type for the first parameter of the method.
    /// </param>
    /// <returns>
    /// True if the method meets the criteria for a valid component extension
    /// method; otherwise, false.
    /// </returns>
    private static bool IsValidComponentExtensionMethod(
        MethodInfo method,
        Type configurationType)
    {
        // Must be an extension method
        if (!method.IsDefined(typeof(ExtensionAttribute)))
        {
            return false;
        }

        var parameters = method.GetParameters();

        // Must have at least one parameter
        if (parameters.Length == 0)
        {
            return false;
        }

        // The first parameter must be the expected configuration type
        if (parameters[0].ParameterType != configurationType)
        {
            return false;
        }

        // Must return LoggerConfiguration (for method chaining)
        return method.ReturnType == typeof(LoggerConfiguration);
    }

    /// <summary>
    /// Gets a list of all detected sink names for diagnostic purposes.
    /// </summary>
    public static IEnumerable<string> GetAvailableSinkNames()
    {
        return DetectAvailableSinks().Keys;
    }

    /// <summary>
    /// Gets a list of all detected enricher names for diagnostic purposes.
    /// </summary>
    public static IEnumerable<string> GetAvailableEnricherNames()
    {
        return DetectAvailableEnrichers().Keys;
    }

    /// <summary>
    /// Checks if a specific sink type is available.
    /// </summary>
    public static bool IsSinkAvailable(string sinkType)
    {
        var sinks = DetectAvailableSinks();
        return sinks.ContainsKey(sinkType);
    }

    /// <summary>
    /// Checks if a specific enricher is available.
    /// </summary>
    public static bool IsEnricherAvailable(string enricherName)
    {
        var enrichers = DetectAvailableEnrichers();
        return enrichers.ContainsKey(enricherName);
    }
}
