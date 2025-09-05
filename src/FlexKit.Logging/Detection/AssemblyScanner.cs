using System.Diagnostics;
using System.Reflection;

namespace FlexKit.Logging.Detection;

/// <summary>
/// Static utility for discovering types that could potentially be intercepted for logging.
/// Scans user assemblies and identifies public classes with interceptable methods.
/// </summary>
internal static class AssemblyScanner
{
    /// <summary>
    /// Discovers all types that could potentially be intercepted for logging.
    /// Returns all public, non-abstract classes in user assemblies that have interceptable methods.
    /// </summary>
    /// <returns>Collection of candidate types for logging interception.</returns>
    public static IEnumerable<Type> DiscoverCandidateTypes() =>
        GetScannableAssemblies().SelectMany(ScanAssembly);

    /// <summary>
    /// Identifies and retrieves a list of assemblies that can be scanned for logging interception.
    /// Excludes dynamic assemblies, system assemblies, and other non-relevant assemblies
    /// based on specific criteria.
    /// </summary>
    /// <returns>A list of assemblies eligible for scanning.</returns>
    private static List<Assembly> GetScannableAssemblies() =>
        [.. AppDomain.CurrentDomain.GetAssemblies().Where(ShouldScanAssembly)];

    /// <summary>
    /// Determines whether an assembly should be included in the scanning process
    /// based on its characteristics, such as its name, whether it is dynamic,
    /// or if it references certain namespaces.
    /// </summary>
    /// <param name="assembly">The assembly to evaluate for inclusion in the scanning process.</param>
    /// <returns>True if the assembly should be included in the scan; otherwise, false.</returns>
    private static bool ShouldScanAssembly(Assembly assembly)
    {
        if (assembly.IsDynamic)
        {
            return false;
        }

        var name = assembly.FullName ?? "";

        // Skip FlexKit framework assemblies
        if (
            name.StartsWith("FlexKit.Logging.", StringComparison.InvariantCulture) &&
            !name.Contains("Tests", StringComparison.InvariantCulture))
        {
            return false;
        }

        // Skip system assemblies
        string[] systemPrefixes = [
            "System.", "Microsoft.", "mscorlib", "netstandard", "Windows.",
            "Autofac", "Castle.", "Newtonsoft.",
        ];

        return !Array.Exists(
                   systemPrefixes, p =>
                       name.StartsWith(p, StringComparison.InvariantCulture)) &&
               ReferencesFlexKitLogging(assembly);
    }

    /// <summary>
    /// Determines if the given assembly references any FlexKit logging-related assemblies.
    /// Checks the referenced assemblies of the specified assembly to identify links to "FlexKit." namespaces.
    /// </summary>
    /// <param name="assembly">The assembly to inspect for FlexKit logging references.</param>
    /// <returns>True if the assembly references FlexKit logging-related assemblies; otherwise, false.</returns>
    private static bool ReferencesFlexKitLogging(Assembly assembly)
    {
        try
        {
            return assembly.GetReferencedAssemblies()
                .Any(refAsm => refAsm.Name?.StartsWith("FlexKit.", StringComparison.Ordinal) == true);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Scans the specified assembly to discover all types that could potentially be intercepted for logging.
    /// Identifies public, non-abstract classes with methods eligible for logging interception.
    /// </summary>
    /// <param name="assembly">The assembly to scan for interceptable types.</param>
    /// <returns>A collection of types within the assembly that meet the criteria for logging interception.</returns>
    private static IEnumerable<Type> ScanAssembly(Assembly assembly)
    {
        try
        {
            return assembly.GetTypes()
                .Where(IsInterceptableType);
        }
        catch (ReflectionTypeLoadException ex)
        {
            // Handle assemblies with missing dependencies
            return ex.Types
                .Where(t => t != null && IsInterceptableType(t))
                .Cast<Type>();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Warning: Failed to scan assembly {assembly.FullName}: {ex.Message}");
            return Enumerable.Empty<Type>();
        }
    }

    /// <summary>
    /// Determines whether a given type is interceptable for logging.
    /// A type is considered interceptable if it is a public, non-abstract class and has methods
    /// that meet criteria for logging interception.
    /// </summary>
    /// <param name="type">The type to evaluate for interceptability.</param>
    /// <returns>True if the type is interceptable; otherwise, false.</returns>
    private static bool IsInterceptableType(Type type) =>
        type is { IsClass: true, IsPublic: true, IsAbstract: false } &&
        HasInterceptableMethods(type);

    /// <summary>
    /// Determines if a type contains methods that can be intercepted for logging.
    /// A method is considered interceptable if it is public, instance-based, and meets specific criteria.
    /// </summary>
    /// <param name="type">The type to evaluate for interceptable methods.</param>
    /// <returns>True if the type contains interceptable methods; otherwise, false.</returns>
    private static bool HasInterceptableMethods(Type type) =>
        type.GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .Where(m => m.DeclaringType == type) // Only methods declared on this type
            .Any(ShouldInterceptMethod);

    /// <summary>
    /// Determines whether the specified method should be intercepted for logging.
    /// A method is considered interceptable if it is public, instance-based, non-static,
    /// non-constructor, non-special name, and not part of the base object type.
    /// </summary>
    /// <param name="method">The method to evaluate for logging interception.</param>
    /// <returns>True if the method meets the criteria for interception; otherwise, false.</returns>
    private static bool ShouldInterceptMethod(MethodInfo method) =>
        method is { IsPublic: true, IsStatic: false } and
        { IsConstructor: false, IsSpecialName: false } && // Excludes property getters/setters
        method.DeclaringType != typeof(object); // Exclude Object methods
}
