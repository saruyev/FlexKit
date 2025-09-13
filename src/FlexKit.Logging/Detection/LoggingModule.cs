using Autofac;
using FlexKit.Logging.Configuration;
using FlexKit.Logging.Interception;
using JetBrains.Annotations;
using Module = Autofac.Module;

namespace FlexKit.Logging.Detection;

/// <summary>
/// Autofac module that automatically discovers and registers classes for logging interception using three approaches:
/// 1. Attribute-based (the highest priority)
/// 2. Configuration-based patterns (medium priority)
/// 3. Auto-interception for all public classes (the lowest priority)
/// Provides transparent logging infrastructure that requires no manual service registration by the user.
/// </summary>
[UsedImplicitly]
internal sealed class LoggingModule : Module
{
    /// <summary>
    /// Loads and configures the logging infrastructure components and discovers classes that need logging.
    /// Uses a three-tier discovery approach with proper precedence handling.
    /// </summary>
    /// <param name="builder">The Autofac container builder to register components with.</param>
    protected override void Load(ContainerBuilder builder)
    {
        // Register all infrastructure first
        builder.RegisterLoggingInfrastructure();

        // Discover and register types for interception
        var candidateTypes = AssemblyScanner.DiscoverCandidateTypes().ToList();

        // Register decision cache
        builder.Register(c =>
        {
            var config = c.Resolve<LoggingConfig>();
            var cache = new InterceptionDecisionCache(config);
            candidateTypes.ForEach(cache.CacheTypeDecisions);
            return cache;
        }).AsSelf().SingleInstance();

        // Register types with logging
        builder.RegisterTypesWithLogging(candidateTypes);
    }
}
