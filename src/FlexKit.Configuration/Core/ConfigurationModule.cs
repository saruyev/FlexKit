// <copyright file="ConfigurationModule.cs" company="Michael Saruyev">
// Copyright (c) Michael Saruyev. All rights reserved.
// </copyright>

using Autofac;
using Autofac.Core;
using Autofac.Core.Activators.Reflection;
using Autofac.Core.Registration;
using Autofac.Core.Resolving.Pipeline;
using JetBrains.Annotations;

namespace FlexKit.Configuration.Core;

/// <summary>
/// Registers configuration components in an Autofac container.
/// </summary>
[UsedImplicitly]
public class ConfigurationModule : Module
{
    /// <summary>
    /// Override to attach module-specific functionality to a component registration.
    /// </summary>
    /// <param name="componentRegistry">Component registry to apply configuration to.</param>
    /// <param name="registration">The registration to attach functionality to.</param>
    protected override void AttachToComponentRegistration(IComponentRegistryBuilder componentRegistry, IComponentRegistration registration)
    {
        // Skip if this is the IFlexConfig registration itself
        if (registration.Services.Any(s => s is TypedService ts && ts.ServiceType == typeof(IFlexConfig)))
        {
            return;
        }

        // Only apply to activations that use reflection (not delegates/instances)
        if (registration.Activator is not ReflectionActivator)
        {
            return;
        }

        // Use middleware to inject FlexConfiguration properties
        registration.PipelineBuilding += (_, pipeline) =>
        {
            pipeline.Use(PipelinePhase.Activation, MiddlewareInsertionMode.EndOfPhase, (context, next) =>
            {
                next(context);

                // After activation, inject FlexConfiguration properties
                var instance = context.Instance;
                if (instance is null)
                {
                    return;
                }

                var properties = instance.GetType()
                    .GetProperties()
                    .Where(p => p.Name == "FlexConfiguration" &&
                                p.PropertyType == typeof(IFlexConfig) &&
                                p.CanWrite);

                foreach (var property in properties)
                {
                    if (context.TryResolve<IFlexConfig>(out var flexConfig))
                    {
                        property.SetValue(instance, flexConfig);
                    }
                }
            });
        };
    }
}
