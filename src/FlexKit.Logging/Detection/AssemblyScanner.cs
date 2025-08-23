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

    private static List<Assembly> GetScannableAssemblies() =>
        [.. AppDomain.CurrentDomain.GetAssemblies().Where(ShouldScanAssembly)];

    private static bool ShouldScanAssembly(Assembly assembly)
    {
        if (assembly.IsDynamic)
        {
            return false;
        }

        var name = assembly.FullName ?? "";

        // Skip FlexKit framework assemblies
        if (name.StartsWith("FlexKit.Logging.", StringComparison.InvariantCulture))
        {
            return false;
        }

        // Skip system assemblies
        string[] systemPrefixes = [
            "System.", "Microsoft.", "mscorlib", "netstandard", "Windows.",
            "Autofac", "Castle.", "Newtonsoft."
        ];

        return !Array.Exists(
                   systemPrefixes, p =>
                       name.StartsWith(p, StringComparison.InvariantCulture)) &&
               ReferencesFlexKitLogging(assembly);
    }

    private static bool ReferencesFlexKitLogging(Assembly assembly)
    {
        try
        {
            return assembly.GetReferencedAssemblies()
                .Any(refAsm => refAsm.Name?.Equals("FlexKit.Logging", StringComparison.Ordinal) == true);
        }
        catch
        {
            return false;
        }
    }

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

    private static bool IsInterceptableType(Type type) =>
        type is { IsClass: true, IsPublic: true, IsAbstract: false } &&
        HasInterceptableMethods(type);

    private static bool HasInterceptableMethods(Type type) =>
        type.GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .Where(m => m.DeclaringType == type) // Only methods declared on this type
            .Any(ShouldInterceptMethod);

    private static bool ShouldInterceptMethod(MethodInfo method) =>
        method is { IsPublic: true, IsStatic: false } and
        { IsConstructor: false, IsSpecialName: false } && // Excludes property getters/setters
        method.DeclaringType != typeof(object); // Exclude Object methods
}
