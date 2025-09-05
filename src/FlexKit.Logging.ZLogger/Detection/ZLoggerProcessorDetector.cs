using System.Diagnostics;
using System.Reflection;
using Microsoft.Extensions.DependencyModel;

namespace FlexKit.Logging.ZLogger.Detection;

/// <summary>
/// Auto-detects available ZLogger processors by scanning loaded assemblies for
/// built-in extension methods and classes that implement IAsyncLogProcessor.
/// </summary>
public static class ZLoggerProcessorDetector
{
    /// <summary>
    /// Information about a detected ZLogger processor, including its configuration details.
    /// </summary>
    public class ProcessorInfo
    {
        /// <summary>
        /// Gets the name of the detected ZLogger processor.
        /// The name corresponds to the processor's type name with the "Output" suffix removed where applicable.
        /// </summary>
        public string Name { get; init; } = string.Empty;

        /// <summary>
        /// Gets the Type representing the detected ZLogger processor class.
        /// This type implements IAsyncLogProcessor or is a built-in ZLogger extension.
        /// </summary>
        public required Type ProcessorType { get; init; }

        /// <summary>
        /// Gets the properties available for configuring this processor.
        /// Each property can be set through FlexKit configuration.
        /// </summary>
        public PropertyInfo[] Properties { get; init; } = [];

        /// <summary>
        /// Gets a value indicating whether this processor is a built-in ZLogger extension method.
        /// Built-in processors are added via AddZLoggerConsole, AddZLoggerFile, etc.
        /// </summary>
        public bool IsBuiltIn { get; init; }

        /// <summary>
        /// Gets the extension method info if this is a built-in processor.
        /// Used to invoke AddZLoggerConsole, AddZLoggerFile, etc.
        /// </summary>
        public MethodInfo? ExtensionMethod { get; init; }
    }

    /// <summary>
    /// Detects all available ZLogger processors by scanning loaded assemblies for
    /// built-in extension methods and IAsyncLogProcessor implementations.
    /// </summary>
    /// <returns>Dictionary mapping processor names to their configuration information.</returns>
    public static Dictionary<string, ProcessorInfo> DetectAvailableProcessors()
    {
        var processors = new Dictionary<string, ProcessorInfo>(StringComparer.OrdinalIgnoreCase);

        // Find all assemblies that might contain ZLogger processors
        foreach (var assembly in GetZLoggerAssemblies())
        {
            try
            {
                DetectProcessorsInAssembly(assembly, processors);
            }
            catch (Exception ex)
            {
                // Log warning but continue - one bad assembly shouldn't break everything
                Debug.WriteLine(
                    $"Warning: Failed to scan assembly {assembly.FullName} for ZLogger processors: {ex.Message}");
            }
        }

        return processors;
    }

    /// <summary>
    /// Gets all assemblies that might contain ZLogger processors.
    /// Includes ZLogger core assembly and any ZLogger extension assemblies.
    /// </summary>
    /// <returns>Array of assemblies to scan for ZLogger processors.</returns>
    private static Assembly[] GetZLoggerAssemblies()
    {
        var assemblies = new List<Assembly>();

        try
        {
            // Get assemblies from DependencyContext if available (preferred method)
            var dependencyAssemblies = DependencyContext.Default?.RuntimeLibraries
                .Where(lib =>
                    lib.Name.StartsWith("ZLogger", StringComparison.OrdinalIgnoreCase) ||
                    lib.Name.Contains("ZLogger", StringComparison.OrdinalIgnoreCase))
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
                .Where(a => a.FullName?.Contains("ZLogger", StringComparison.OrdinalIgnoreCase) == true));
        }

        return [.. assemblies];
    }

    /// <summary>
    /// Scans the given assembly to detect available ZLogger processors.
    /// </summary>
    /// <param name="assembly">The assembly to search for ZLogger processors.</param>
    /// <param name="processors">A dictionary to store the detected processors along with their metadata.</param>
    private static void DetectProcessorsInAssembly(
        Assembly assembly,
        Dictionary<string, ProcessorInfo> processors)
    {
        try
        {
            // Detect built-in extension methods first
            DetectBuiltInExtensionMethods(assembly, processors);

            // Then detect custom IAsyncLogProcessor implementations
            DetectAsyncLogProcessors(assembly, processors);
        }
        catch (ReflectionTypeLoadException ex)
        {
            // Handle partial loading - some types might not be available

            foreach (var type in ex.Types.Where(t => t != null && IsValidAsyncLogProcessor(t)))
            {
                var processorInfo = CreateAsyncLogProcessorInfo(type);
                if (processorInfo != null)
                {
                    processors.TryAdd(processorInfo.Name, processorInfo);
                }
            }
        }
    }

    /// <summary>
    /// Detects built-in ZLogger extension methods like AddZLoggerConsole, AddZLoggerFile.
    /// </summary>
    /// <param name="assembly">The assembly to search.</param>
    /// <param name="processors">Dictionary to store detected processors.</param>
    private static void DetectBuiltInExtensionMethods(
        Assembly assembly,
        Dictionary<string, ProcessorInfo> processors)
    {
        // Find static classes that might contain ZLogger extension methods
        var extensionClasses = assembly.GetTypes()
            .Where(t => t.IsSealed && t is { IsAbstract: true, IsPublic: true }) // Static classes
            .ToArray();

        foreach (var extensionClass in extensionClasses)
        {
            var methods = extensionClass.GetMethods(BindingFlags.Public | BindingFlags.Static)
                .Where(IsZLoggerBuiltInExtension)
                .ToArray();

            foreach (var method in methods)
            {
                var processorInfo = CreateBuiltInProcessorInfo(method);
                if (processorInfo != null)
                {
                    processors.TryAdd(processorInfo.Name, processorInfo);
                }
            }
        }
    }

    /// <summary>
    /// Detects IAsyncLogProcessor implementations.
    /// </summary>
    /// <param name="assembly">The assembly to search.</param>
    /// <param name="processors">Dictionary to store detected processors.</param>
    private static void DetectAsyncLogProcessors(
        Assembly assembly,
        Dictionary<string, ProcessorInfo> processors)
    {
        var types = assembly.GetTypes()
            .Where(IsValidAsyncLogProcessor)
            .ToArray();

        foreach (var type in types)
        {
            var processorInfo = CreateAsyncLogProcessorInfo(type);
            if (processorInfo != null)
            {
                processors.TryAdd(processorInfo.Name, processorInfo);
            }
        }
    }

    /// <summary>
    /// Determines whether the specified method is a ZLogger built-in extension method.
    /// </summary>
    /// <param name="method">The method to evaluate.</param>
    /// <returns>True if the method is a ZLogger built-in extension; otherwise, false.</returns>
    private static bool IsZLoggerBuiltInExtension(MethodInfo method)
    {
        // Must be an extension method
        if (!method.IsDefined(typeof(System.Runtime.CompilerServices.ExtensionAttribute)))
        {
            return false;
        }

        // Check if the method name matches ZLogger built-in pattern
        var methodName = method.Name;
        if (!methodName.StartsWith("AddZLogger", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var parameters = method.GetParameters();

        // Must have at least one parameter (the extended type)
        if (parameters.Length == 0)
        {
            return false;
        }

        // First parameter should be ILoggingBuilder (ZLogger extension methods extend ILoggingBuilder)
        var firstParamType = parameters[0].ParameterType;
        return firstParamType is { Name: "ILoggingBuilder", Namespace: "Microsoft.Extensions.Logging" };
    }

    /// <summary>
    /// Determines whether a type is a valid IAsyncLogProcessor implementation.
    /// </summary>
    /// <param name="type">The type to evaluate.</param>
    /// <returns>True if the type is a valid IAsyncLogProcessor; otherwise, false.</returns>
    private static bool IsValidAsyncLogProcessor(Type type)
    {
        // Must be a concrete class (not abstract, not interface)
        if (!type.IsClass || type.IsAbstract || !type.IsPublic)
        {
            return false;
        }

        // Must implement IAsyncLogProcessor interface
        var interfaces = type.GetInterfaces();
        return interfaces.Any(i => i.Name == "IAsyncLogProcessor");
    }

    /// <summary>
    /// Creates a ProcessorInfo object for a built-in ZLogger extension method.
    /// </summary>
    /// <param name="method">The extension method to analyze.</param>
    /// <returns>ProcessorInfo object with processor metadata, or null if the method cannot be analyzed.</returns>
    private static ProcessorInfo? CreateBuiltInProcessorInfo(MethodInfo method)
    {
        try
        {
            // Extract processor name from method name: AddZLoggerConsole -> Console
            var processorName = method.Name;
            if (processorName.StartsWith("AddZLogger", StringComparison.OrdinalIgnoreCase))
            {
                processorName = processorName[10..]; // Remove "AddZLogger" prefix
            }

            return new ProcessorInfo
            {
                Name = processorName,
                ProcessorType = method.DeclaringType ?? typeof(object),
                Properties = [],
                IsBuiltIn = true,
                ExtensionMethod = method
            };
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Creates a ProcessorInfo object for an IAsyncLogProcessor implementation.
    /// </summary>
    /// <param name="processorType">The processor type to analyze.</param>
    /// <returns>ProcessorInfo object with processor metadata, or null if the processor cannot be analyzed.</returns>
    private static ProcessorInfo? CreateAsyncLogProcessorInfo(Type? processorType)
    {
        try
        {
            return processorType == null ? null : CreateLogProcessorInfoFromType(processorType);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Creates a <see cref="ProcessorInfo"/> instance based on the provided processor type,
    /// extracting its name, configurable properties, and other metadata.
    /// </summary>
    /// <param name="processorType">The type of the processor for which information should be created.</param>
    /// <returns>A <see cref="ProcessorInfo"/> instance containing detailed information about the processor.</returns>
    private static ProcessorInfo CreateLogProcessorInfoFromType(Type processorType)
    {
        // Determine processor name (use type name, removing "Output" suffix if present)
        var processorName = processorType.Name;
        if (processorName.EndsWith("Output", StringComparison.InvariantCulture))
        {
            // Remove the "Output" suffix: DebugOutput -> Debug
            processorName = processorName[..^6];
        }
        if (processorName.EndsWith("Processor", StringComparison.InvariantCulture))
        {
            processorName = processorName[..^9]; // Remove the "Processor" suffix
        }

        // Get configurable properties
        var properties = processorType
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p is { CanWrite: true, SetMethod.IsPublic: true })
            .ToArray();

        return new ProcessorInfo
        {
            Name = processorName,
            ProcessorType = processorType,
            Properties = properties,
            IsBuiltIn = false
        };
    }
}
