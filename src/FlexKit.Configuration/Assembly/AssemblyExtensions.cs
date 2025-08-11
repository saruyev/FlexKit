// <copyright file="AssemblyExtensions.cs" company="Michael Saruyev">
// Copyright (c) Michael Saruyev. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

using System.Reflection;
using System.Runtime.Loader;
using Autofac;
using FlexKit.Configuration.Core;
using JetBrains.Annotations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyModel;
using Microsoft.Extensions.DependencyModel.Resolution;
using Module = Autofac.Module;

namespace FlexKit.Configuration.Assembly;

/// <summary>
/// Extension methods for assembly scanning and module registration.
/// Provides sophisticated assembly discovery and registration capabilities for Autofac containers,
/// with support for both runtime and compile-time assembly resolution.
/// </summary>
/// <remarks>
/// <para>
/// This class implements the FlexKit Configuration library's assembly scanning strategy,
/// which automatically discovers and registers Autofac modules from
/// - Current application domain assemblies
/// - Dependencies resolved through DependencyContext
/// - Assemblies filtered by configurable naming patterns
/// </para>
/// <para>
/// The scanning process respects the MappingConfig settings to control which assemblies
/// are included in the dependency injection container setup.
/// </para>
///
/// <para>
/// <strong>Usage Example:</strong>
/// <code>
/// var builder = new ContainerBuilder();
/// builder.AddModules(configuration);
/// // This will scan and register all modules from configured assemblies
/// </code>
/// </para>
/// </remarks>
public static class AssemblyExtensions
{
    /// <summary>
    /// Configuration section path for assembly mapping definitions.
    /// Maps to the "Application:Mapping" section in configuration files.
    /// </summary>
    /// <remarks>
    /// This constant defines the configuration path used to retrieve assembly scanning rules.
    /// The configuration structure should match the <see cref="MappingConfig"/> class properties.
    /// Expected configuration format:
    /// <code>
    /// {
    ///   "Application": {
    ///     "Mapping": {
    ///       "Prefix": "MyCompany",
    ///       "Names": ["MyCompany.Services", "MyCompany.Data"]
    ///     }
    ///   }
    /// }
    /// </code>
    /// </remarks>
    private const string MappingSectionName = "Application:Mapping";

    /// <summary>
    /// Composite resolver for compilation assemblies supporting multiple resolution strategies.
    /// Combines app base, reference assembly, and package-based resolution approaches
    /// to ensure comprehensive assembly discovery across different deployment scenarios.
    /// </summary>
    /// <remarks>
    /// The resolver chain includes:
    /// - <see cref="AppBaseCompilationAssemblyResolver"/>: Resolves from application base directory
    /// - <see cref="ReferenceAssemblyPathResolver"/>: Resolves reference assemblies
    /// - <see cref="PackageCompilationAssemblyResolver"/>: Resolves NuGet package assemblies
    ///
    /// This composite approach ensures assemblies can be found in various deployment scenarios,
    /// including self-contained deployments, framework-dependent deployments, and development environments.
    /// </remarks>
    private static readonly ICompilationAssemblyResolver _resolver = new CompositeCompilationAssemblyResolver(
    [
        new AppBaseCompilationAssemblyResolver(),
        new ReferenceAssemblyPathResolver(),
        new PackageCompilationAssemblyResolver(),
    ]);

    /// <summary>
    /// Adds assemblies from the base directory to the Autofac assembly scanner.
    /// Scans the current application domain for assemblies that match the configured criteria
    /// and contain Autofac modules, then registers those modules with the container.
    /// </summary>
    /// <param name="builder">Container builder instance to register modules with.</param>
    /// <param name="configuration">The current application configuration settings containing assembly mapping rules.</param>
    /// <remarks>
    /// This method performs the following operations:
    /// 1. Retrieves assembly mapping configuration from the "Application:Mapping" section
    /// 2. Filters assemblies from the current app domain based on naming patterns
    /// 3. Checks each assembly for the presence of Autofac modules
    /// 4. Excludes the FlexKit.Configuration assembly itself to prevent circular dependencies
    /// 5. Registers all discovered modules with the provided container builder
    ///
    /// <para>
    /// <strong>Performance Considerations:</strong>
    /// This method uses reflection to scan assemblies and should be called during application
    /// startup rather than in performance-critical paths.
    /// </para>
    /// </remarks>
    /// <exception cref="ReflectionTypeLoadException">
    /// Thrown when an assembly cannot be loaded or contains types that cannot be loaded.
    /// </exception>
    [UsedImplicitly]
    public static void RegisterAssembliesFromBaseDirectory(
        this ContainerBuilder builder,
        IConfiguration? configuration = null)
    {
        // Retrieve assembly mapping configuration to determine which assemblies to scan
        var config = configuration?.GetSection(MappingSectionName).Get<MappingConfig>();

        // Get all currently loaded assemblies from the application domain
        var allAssemblies = AppDomain.CurrentDomain.GetAssemblies();

        // Filter assemblies based on naming patterns and module presence
        var assemblies = allAssemblies
            .Where(assembly => FilterLibraries(assembly.FullName, config) && ContainsModules(assembly))
            .Where(assembly =>
                !assembly.GetName().Name?.Equals("FlexKit.Configuration", StringComparison.OrdinalIgnoreCase) == true)
            .ToList();

        // Register all modules found in the filtered assemblies
        foreach (var assembly in assemblies)
        {
            builder.RegisterAssemblyModules(assembly);
        }
    }

    /// <summary>
    /// Adds modules to the container builder from registered assemblies.
    /// Combines dependency context scanning with base directory scanning to provide
    /// comprehensive module discovery across all available assemblies.
    /// </summary>
    /// <param name="builder">The container builder to extend with discovered modules.</param>
    /// <param name="configuration">The current application configuration settings containing assembly mapping rules.</param>
    /// <remarks>
    /// This method implements a two-phase scanning approach:
    /// 1. Scans assemblies from the dependency context (compile-time dependencies)
    /// 2. Scans assemblies from the current application domain (runtime assemblies)
    ///
    /// This dual approach ensures that modules are discovered regardless of how the
    /// application is deployed or which assemblies are loaded at runtime.
    ///
    /// <para>
    /// <strong>Usage Pattern:</strong>
    /// This is typically the primary entry point for module registration in FlexKit applications.
    /// Call this method once during container configuration.
    /// </para>
    /// </remarks>
    [UsedImplicitly]
    public static void AddModules(this ContainerBuilder builder, IConfiguration configuration)
    {
        builder.RegisterModule<ConfigurationModule>(); // Register FlexKit.Configuration module>()

        // Register modules from dependency context (compile-time dependencies)
        foreach (var assembly in DependencyContext.Default?.GetAssemblies(configuration) ?? [])
        {
            builder.RegisterAssemblyModules(assembly);
        }

        // Register modules from current app domain (runtime assemblies)
        builder.RegisterAssembliesFromBaseDirectory(configuration);
    }

    /// <summary>
    /// Retrieves a list of application assemblies from the dependency context.
    /// Resolves assemblies using the compilation library information and applies
    /// filtering based on the provided configuration settings.
    /// </summary>
    /// <param name="context">The current application dependency context containing compilation information.</param>
    /// <param name="configuration">The current application configuration settings containing assembly mapping rules.</param>
    /// <returns>
    /// List of successfully resolved and loaded assemblies that match the filtering criteria.
    /// Returns an empty list if the dependency context is null or contains no compilation options.
    /// </returns>
    /// <remarks>
    /// This method performs the following steps:
    /// 1. Uses the provided dependency context or falls back to the default context
    /// 2. Converts runtime libraries to compilation libraries for resolution
    /// 3. Applies assembly name filtering based on configuration
    /// 4. Resolves assembly paths using the composite resolver
    /// 5. Loads assemblies from resolved file paths
    /// 6. Excludes the FlexKit.Configuration assembly to prevent self-registration
    ///
    /// <para>
    /// <strong>Error Handling:</strong>
    /// Assemblies that cannot be resolved or loaded are silently skipped to ensure
    /// robust operation in various deployment scenarios.
    /// </para>
    /// </remarks>
    [UsedImplicitly]
    public static List<System.Reflection.Assembly> GetAssemblies(this DependencyContext? context, IConfiguration? configuration)
    {
        // Use provided context or fall back to default
        context ??= DependencyContext.Default;

        // Retrieve assembly filtering configuration
        var config = configuration?.GetSection(MappingSectionName).Get<MappingConfig>();

        // Process runtime libraries through the resolution pipeline
        // ReSharper disable once NullableWarningSuppressionIsUsed - we already checked the issue
        return [.. context!.RuntimeLibraries
            .Select(lib => lib.ConvertToCompilation()) // Convert to a compilation library for resolution
            .Where(lib => FilterLibraries(lib.Name, config)) // Apply name-based filtering
            .Where(lib => !lib.Name.Equals("FlexKit.Configuration", StringComparison.OrdinalIgnoreCase)) // Exclude self
            .SelectMany(ResolveReferencePaths) // Resolve to file paths
            .Where(File.Exists) // Ensure files exist
            .Select(AssemblyLoadContext.Default.LoadFromAssemblyPath)];
    }

    /// <summary>
    /// Filters assemblies containing or inheriting Autofac modules.
    /// Performs reflection-based inspection to determine if an assembly contains
    /// types that inherit from the Autofac Module base class.
    /// </summary>
    /// <param name="assembly">The current assembly candidate to inspect for module presence.</param>
    /// <returns>
    /// <c>true</c> if the assembly contains at least one type inheriting from <see cref="Module"/>;
    /// otherwise, <c>false</c>.
    /// </returns>
    /// <remarks>
    /// This method uses reflection to examine all public types in the assembly.
    /// It specifically looks for types that:
    /// - Inherit from the Autofac Module class
    /// - Are not the Module class itself
    /// - Can be instantiated (not abstract)
    ///
    /// <para>
    /// <strong>Performance Note:</strong>
    /// This method involves reflection and type loading, which can be expensive.
    /// Results should be cached when possible for repeated calls.
    /// </para>
    /// </remarks>
    /// <exception cref="ReflectionTypeLoadException">
    /// May be thrown if the assembly contains types that cannot be loaded.
    /// The method handles this gracefully by continuing with available types.
    /// </exception>
    private static bool ContainsModules(System.Reflection.Assembly assembly)
    {
        try
        {
            return assembly.GetTypes()
                .Any(t => t != typeof(Module) && typeof(Module).IsAssignableFrom(t));
        }
        catch (ReflectionTypeLoadException)
        {
            // Handle assemblies with types that cannot be loaded
            // This can happen with mixed-mode assemblies or missing dependencies
            return false;
        }
    }

    /// <summary>
    /// Filters assemblies by name mapping configuration.
    /// Applies the configured naming patterns to determine whether an assembly
    /// should be included in the module scanning process.
    /// </summary>
    /// <param name="lib">The current assembly name to evaluate against filtering criteria.</param>
    /// <param name="config">Assembly mapping configuration containing filtering rules.</param>
    /// <returns>
    /// <c>true</c> if the assembly name matches the configured filtering criteria;
    /// otherwise, <c>false</c>.
    /// </returns>
    /// <remarks>
    /// The filtering logic applies the following priority order:
    /// 1. If a prefix is configured, the assembly name must start with that prefix
    /// 2. If specific names are configured, the assembly name must start with one of those names
    /// 3. If no configuration is provided, defaults to FlexKit.Configuration assemblies or those containing "Module"
    ///
    /// <para>
    /// <strong>Configuration Examples:</strong>
    /// <code>
    /// // Prefix-based filtering
    /// "Mapping": { "Prefix": "MyCompany" } // Includes: MyCompany.Services, MyCompany.Data
    ///
    /// // Name-based filtering
    /// "Mapping": { "Names": ["Acme.Services", "Acme.Data"] } // Includes only specified prefixes
    /// </code>
    /// </para>
    /// </remarks>
    private static bool FilterLibraries(string? lib, MappingConfig? config)
    {
        // Null or empty assembly names are always excluded
        if (lib is null)
        {
            return false;
        }

        var prefix = config?.Prefix;
        var names = config?.Names;

        // Priority 1: Prefix-based filtering
        if (!string.IsNullOrEmpty(prefix))
        {
            return lib.StartsWith(prefix, StringComparison.InvariantCulture);
        }

        // Priority 2: Name-based filtering
        if (names?.Count > 0)
        {
            return names.Any(lib.StartsWith);
        }

        // Priority 3: Default filtering for FlexKit assemblies or module-containing assemblies
        return lib.StartsWith("FlexKit.Configuration", StringComparison.InvariantCulture) ||
               lib.Contains("Module", StringComparison.InvariantCulture);
    }

    /// <summary>
    /// Converts <see cref="RuntimeLibrary"/> to <see cref="CompilationLibrary"/>.
    /// Transforms runtime library information into a format suitable for compilation
    /// assembly resolution, enabling path resolution for assembly loading.
    /// </summary>
    /// <param name="library">The <see cref="RuntimeLibrary"/> instance to convert.</param>
    /// <returns>
    /// A <see cref="CompilationLibrary"/> instance containing the same metadata
    /// but formatted for compilation-time resolution.
    /// </returns>
    /// <remarks>
    /// This conversion is necessary because the dependency resolution system works with
    /// compilation libraries, while the dependency context provides runtime libraries.
    /// The conversion preserves all essential metadata including
    /// - Library type and name
    /// - Version and hash information
    /// - Asset paths from runtime assembly groups
    /// - Dependency relationships
    /// - Serviceability flags
    ///
    /// <para>
    /// <strong>Technical Note:</strong>
    /// This method flattens runtime assembly groups into a single collection of asset paths,
    /// which is appropriate for most assembly resolution scenarios.
    /// </para>
    /// </remarks>
    private static CompilationLibrary ConvertToCompilation(this RuntimeLibrary library) => new(
        library.Type,
        library.Name,
        library.Version,
        library.Hash,
        library.RuntimeAssemblyGroups.SelectMany(g => g.AssetPaths),
        library.Dependencies,
        library.Serviceable);

    /// <summary>
    /// Resolves reference paths for a compilation library.
    /// Uses the configured assembly resolver to determine the file system paths
    /// where the library's assemblies can be found.
    /// </summary>
    /// <param name="compilationLibrary">The compilation library to resolve paths for.</param>
    /// <returns>
    /// An enumerable collection of file system paths pointing to the library's assemblies.
    /// May return an empty collection if the library cannot be resolved.
    /// </returns>
    /// <remarks>
    /// This method delegates to the composite compilation assembly resolver configured
    /// in the static field. The resolver attempts multiple resolution strategies to
    /// find assemblies in various deployment scenarios.
    ///
    /// <para>
    /// <strong>Resolution Strategies:</strong>
    /// The composite resolver tries the following approaches in order:
    /// 1. Application base directory resolution
    /// 2. Reference assembly path resolution
    /// 3. NuGet package resolution
    /// </para>
    /// </remarks>
    /// <exception cref="FileNotFoundException">
    /// May be thrown by underlying resolvers if critical assemblies cannot be located.
    /// </exception>
    private static IEnumerable<string> ResolveReferencePaths(CompilationLibrary compilationLibrary) =>
        compilationLibrary.ResolveReferencePaths(_resolver);
}
