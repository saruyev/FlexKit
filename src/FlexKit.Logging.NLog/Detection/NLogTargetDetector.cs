using System.Diagnostics;
using System.Reflection;
using Microsoft.Extensions.DependencyModel;

namespace FlexKit.Logging.NLog.Detection;

/// <summary>
/// Auto-detects available NLog targets and layout renderers by scanning loaded assemblies for
/// extension methods and target types that can be used with NLog configuration.
/// </summary>
public static class NLogTargetDetector
{
    /// <summary>
    /// Information about a detected NLog target, including its configuration details.
    /// </summary>
    public class TargetInfo
    {
        /// <summary>
        /// Gets the name of the detected NLog target.
        /// The name corresponds to the target's type name or alias.
        /// </summary>
        public string Name { get; init; } = string.Empty;

        /// <summary>
        /// Gets the Type representing the detected NLog target class.
        /// This type inherits from NLog.Targets.Target.
        /// </summary>
        public required Type TargetType { get; init; }

        /// <summary>
        /// Gets the properties available for configuring this target.
        /// Each property can be set through FlexKit configuration.
        /// </summary>
        public PropertyInfo[] Properties { get; init; } = [];

        /// <summary>
        /// Gets a value indicating whether this target supports async operations.
        /// Async targets can be wrapped with AsyncWrapper for better performance.
        /// </summary>
        public bool SupportsAsync { get; init; }
    }

    /// <summary>
    /// Detects all available NLog targets by scanning loaded assemblies for
    /// classes that inherit from NLog.Targets.Target.
    /// </summary>
    /// <returns>Dictionary mapping target names to their configuration information.</returns>
    public static Dictionary<string, TargetInfo> DetectAvailableTargets()
    {
        var targets = new Dictionary<string, TargetInfo>(StringComparer.OrdinalIgnoreCase);

        // Find all assemblies that might contain NLog targets
        foreach (var assembly in GetNLogAssemblies())
        {
            try
            {
                DetectTargetsInAssembly(assembly, targets);
            }
            catch (Exception ex)
            {
                // Log warning but continue - one bad assembly shouldn't break everything
                Debug.WriteLine($"Warning: Failed to scan assembly {assembly.FullName} for NLog targets: {ex.Message}");
            }
        }

        return targets;
    }

    /// <summary>
    /// Gets all assemblies that might contain NLog targets.
    /// Includes NLog core assembly and any NLog extension assemblies.
    /// </summary>
    /// <returns>Array of assemblies to scan for NLog targets.</returns>
    private static Assembly[] GetNLogAssemblies()
    {
        var assemblies = new List<Assembly>();

        // Always include currently loaded assemblies that reference NLog
        assemblies.AddRange(AppDomain.CurrentDomain.GetAssemblies()
            .Where(ReferencesNLog));

        // Also check runtime libraries if available
        var runtimeAssemblies = DependencyContext.Default?.RuntimeLibraries
            .Where(lib => lib.Name.StartsWith("NLog", StringComparison.OrdinalIgnoreCase))
            .SelectMany(lib => lib.GetDefaultAssemblyNames(DependencyContext.Default))
            .Select(name =>
            {
                try
                {
                    return Assembly.Load(name);
                }
                catch
                {
                    return null;
                }
            })
            .Where(assembly => assembly != null)
            .Cast<Assembly>() ?? [];

        assemblies.AddRange(runtimeAssemblies);

        return assemblies.Distinct().ToArray();
    }

    /// <summary>
    /// Determines if an assembly references NLog by checking its referenced assemblies.
    /// </summary>
    /// <param name="assembly">The assembly to check.</param>
    /// <returns>True if the assembly references NLog; otherwise, false.</returns>
    private static bool ReferencesNLog(Assembly assembly)
    {
        try
        {
            return assembly.GetReferencedAssemblies()
                .Any(name => name.Name?.StartsWith("NLog", StringComparison.OrdinalIgnoreCase) == true);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Scans the given assembly to detect available NLog targets by looking for
    /// classes that inherit from NLog.Targets.Target.
    /// </summary>
    /// <param name="assembly">The assembly to search for NLog targets.</param>
    /// <param name="targets">A dictionary to store the detected targets along with their metadata.</param>
    private static void DetectTargetsInAssembly(
        Assembly assembly,
        Dictionary<string, TargetInfo> targets)
    {
        try
        {
            var types = assembly.GetTypes()
                .Where(IsValidTargetType)
                .ToArray();

            foreach (var type in types)
            {
                var targetInfo = CreateTargetInfo(type);
                if (targetInfo != null)
                {
                    targets.TryAdd(targetInfo.Name, targetInfo);
                }
            }
        }
        catch (ReflectionTypeLoadException ex)
        {
            // Handle partial loading - some types might not be available
            var availableTypes = ex.Types.Where(t => t != null).Cast<Type>();
            foreach (var type in availableTypes.Where(IsValidTargetType))
            {
                var targetInfo = CreateTargetInfo(type);
                if (targetInfo != null)
                {
                    targets.TryAdd(targetInfo.Name, targetInfo);
                }
            }
        }
    }

    /// <summary>
    /// Determines whether a type is a valid NLog target that can be instantiated and configured.
    /// </summary>
    /// <param name="type">The type to evaluate.</param>
    /// <returns>True if the type is a valid NLog target; otherwise, false.</returns>
    private static bool IsValidTargetType(Type type)
    {
        // Must be a concrete class (not abstract, not interface)
        if (!type.IsClass || type.IsAbstract)
        {
            return false;
        }

        // Must be public
        if (!type.IsPublic)
        {
            return false;
        }

        // Must inherit from NLog Target (check base classes)
        var baseType = type.BaseType;
        while (baseType != null)
        {
            if (baseType is { Name: "Target", Namespace: "NLog.Targets" })
            {
                return true;
            }
            baseType = baseType.BaseType;
        }

        return false;
    }

    /// <summary>
    /// Creates a TargetInfo object for a detected NLog target type.
    /// </summary>
    /// <param name="targetType">The target type to analyze.</param>
    /// <returns>TargetInfo object with target metadata, or null if the target cannot be analyzed.</returns>
    private static TargetInfo? CreateTargetInfo(Type targetType)
    {
        try
        {
            // Determine a target name (use type name without "Target" suffix if present)
            var targetName = targetType.Name;
            if (targetName.EndsWith("Target", StringComparison.InvariantCulture))
            {
                targetName = targetName[..^6]; // Remove "Target" suffix
            }

            // Get configurable properties
            var properties = targetType
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p is { CanWrite: true, SetMethod.IsPublic: true })
                .ToArray();

            // Check if the target supports async operations
            var supportsAsync = CheckAsyncSupport(targetType);

            return new TargetInfo
            {
                Name = targetName,
                TargetType = targetType,
                Properties = properties,
                SupportsAsync = supportsAsync
            };
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Determines if a target type supports asynchronous operations.
    /// </summary>
    /// <param name="targetType">The target type to check.</param>
    /// <returns>True if the target supports async operations; otherwise, false.</returns>
    private static bool CheckAsyncSupport(Type targetType)
    {
        // Check for async-related methods or properties
        var methods = targetType.GetMethods(BindingFlags.Public | BindingFlags.Instance);

        return methods.Any(m =>
            m.Name.Contains("Async", StringComparison.OrdinalIgnoreCase) ||
            m.ReturnType == typeof(Task) ||
            (m.ReturnType.IsGenericType && m.ReturnType.GetGenericTypeDefinition() == typeof(Task<>)));
    }
}
