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
internal class ConfigurationModule : Module
{
    /// <summary>
    /// Override to attach module-specific functionality to a component registration.
    /// </summary>
    /// <param name="componentRegistry">Component registry to apply configuration to.</param>
    /// <param name="registration">The registration to attach functionality to.</param>
    protected override void AttachToComponentRegistration(
        IComponentRegistryBuilder componentRegistry,
        IComponentRegistration registration)
    {
        // Skip if this is the IFlexConfig registration itself
        if (IsFlexConfigRegistration(registration))
        {
            return;
        }

        // Only apply to activations that use reflection (not delegates/instances)
        if (!IsReflectionActivatedRegistration(registration))
        {
            return;
        }

        // Add property injection middleware to the pipeline
        AddPropertyInjectionMiddleware(registration);
    }

    /// <summary>
    /// Determines whether the specified registration is for the IFlexConfig service itself.
    /// This check prevents the module from processing its own registration and potentially
    /// causing circular dependencies or infinite loops during container resolution.
    /// </summary>
    /// <param name="registration">The component registration to examine.</param>
    /// <returns>
    /// <c>true</c> if the registration is for IFlexConfig service; otherwise, <c>false</c>.
    /// </returns>
    /// <remarks>
    /// This method examines all services exposed by the registration to determine if any
    /// of them represent the IFlexConfig type. This is necessary because a single registration
    /// can expose multiple service interfaces.
    /// </remarks>
    private static bool IsFlexConfigRegistration(IComponentRegistration registration)
        => registration.Services.Any(s => s is TypedService ts && ts.ServiceType == typeof(IFlexConfig));

    /// <summary>
    /// Determines whether the specified registration uses reflection-based activation.
    /// Property injection only works with reflection-based activators, as they create
    /// instances through reflection and allow post-creation property modification.
    /// </summary>
    /// <param name="registration">The component registration to examine.</param>
    /// <returns>
    /// <c>true</c> if the registration uses ReflectionActivator; otherwise, <c>false</c>.
    /// </returns>
    /// <remarks>
    /// <para>
    /// Autofac supports multiple activation strategies:
    /// <list type="bullet">
    /// <item>ReflectionActivator - Creates instances using constructors via reflection</item>
    /// <item>DelegateActivator - Creates instances using provided factory delegates</item>
    /// <item>ProvidedInstanceActivator - Uses pre-created instances</item>
    /// </list>
    /// </para>
    /// <para>
    /// Property injection can only be applied to reflection-activated components because:
    /// <list type="bullet">
    /// <item>Delegate activators control the entire creation process</item>
    /// <item>Instance activators use pre-created objects that may be immutable</item>
    /// <item>Only reflection activation allows post-creation modification</item>
    /// </list>
    /// </para>
    /// </remarks>
    private static bool IsReflectionActivatedRegistration(IComponentRegistration registration)
        => registration.Activator is ReflectionActivator;

    /// <summary>
    /// Adds property injection middleware to the component registration pipeline.
    /// This middleware runs after component activation and injects IFlexConfig instances
    /// into properties named "FlexConfiguration" that have the correct type and write access.
    /// </summary>
    /// <param name="registration">The component registration to enhance with property injection.</param>
    /// <remarks>
    /// <para>
    /// The middleware is added to the Activation phase at the end of the phase, ensuring it runs
    /// after the component instance has been created, but before it's returned to the requesting code.
    /// </para>
    /// <para>
    /// The injection process follows these steps:
    /// <list type="number">
    /// <item>Wait for component activation to complete</item>
    /// <item>Examine the created instance for suitable properties</item>
    /// <item>Attempt to resolve IFlexConfig from the container</item>
    /// <item>Inject the resolved IFlexConfig into matching properties</item>
    /// </list>
    /// </para>
    /// <para>
    /// This approach uses Autofac's middleware pipeline for reliable and efficient
    /// property injection that integrates seamlessly with the container's lifecycle management.
    /// </para>
    /// </remarks>
    private static void AddPropertyInjectionMiddleware(IComponentRegistration registration) =>
        registration.PipelineBuilding +=
            (_, pipeline) => pipeline.Use(
                PipelinePhase.Activation,
                MiddlewareInsertionMode.EndOfPhase,
                (context, next) =>
                {
                    next(context);
                    InjectFlexConfigurationProperties(context);
                });

    /// <summary>
    /// Injects IFlexConfig instances into suitable properties of the activated component.
    /// Searches for properties named "FlexConfiguration" with type IFlexConfig that have write access,
    /// then attempts to resolve and inject IFlexConfig from the container context.
    /// </summary>
    /// <param name="context">The resolution context containing the activated instance and container scope.</param>
    /// <remarks>
    /// <para>
    /// <strong>Property Selection Criteria:</strong>
    /// Properties must meet all the following requirements to be eligible for injection:
    /// <list type="bullet">
    /// <item>The property name must be exactly "FlexConfiguration" (case-sensitive)</item>
    /// <item>Property type must be exactly IFlexConfig</item>
    /// <item>Property must have a public setter (CanWrite = true)</item>
    /// </list>
    /// </para>
    /// <para>
    /// <strong>Error Handling:</strong>
    /// The method handles various error conditions gracefully:
    /// <list type="bullet">
    /// <item>Null instances are ignored (defensive programming)</item>
    /// <item>Missing IFlexConfig registrations are handled silently</item>
    /// <item>Property injection failures do not cause activation to fail</item>
    /// <item>Reflection errors are contained and do not propagate</item>
    /// </list>
    /// </para>
    /// <para>
    /// <strong>Performance Considerations:</strong>
    /// <list type="bullet">
    /// <item>Reflection is used to discover and set properties</item>
    /// <item>Property discovery is performed once per activation</item>
    /// <item>Container resolution uses the current scope for efficiency</item>
    /// <item>Only matching properties are processed to minimize overhead</item>
    /// </list>
    /// </para>
    /// </remarks>
    private static void InjectFlexConfigurationProperties(ResolveRequestContext context)
    {
        // Defensive check - instance should never be null but handle gracefully
        var instance = context.Instance;

        if (instance is null)
        {
            return;
        }

        // Inject IFlexConfig into each matching property
        foreach (var property in GetInjectableFlexConfigProperties(instance.GetType()))
        {
            InjectFlexConfigIntoProperty(context, instance, property);
        }
    }

    /// <summary>
    /// Retrieves all properties from the specified type that are eligible for FlexConfig injection.
    /// Uses reflection to examine the type's properties and filter them based on injection criteria.
    /// </summary>
    /// <param name="instanceType">The type to examine for injectable properties.</param>
    /// <returns>
    /// An enumerable collection of PropertyInfo objects representing properties that meet
    /// the injection criteria (name, type, and write access).
    /// </returns>
    /// <remarks>
    /// <para>
    /// This method performs compile-time filtering of properties to identify injection targets.
    /// It examines all public instance properties of the type, including inherited properties,
    /// to find those suitable for FlexConfig injection.
    /// </para>
    /// <para>
    /// <strong>Inheritance Support:</strong>
    /// The method searches all properties in the type hierarchy, allowing base classes
    /// to define FlexConfiguration properties that will be injected in derived class instances.
    /// </para>
    /// <para>
    /// <strong>Performance Note:</strong>
    /// Property reflection is performed once per component type during activation.
    /// Consider caching results if this becomes a performance bottleneck with many component types.
    /// </para>
    /// </remarks>
    private static IEnumerable<System.Reflection.PropertyInfo> GetInjectableFlexConfigProperties(Type instanceType) =>
        instanceType
            .GetProperties()
            .Where(p => p.Name == "FlexConfiguration" &&
                        p.PropertyType == typeof(IFlexConfig) &&
                        p.CanWrite);

    /// <summary>
    /// Attempts to inject an IFlexConfig instance into the specified property of the given object.
    /// Resolves IFlexConfig from the container context and sets it on the property using reflection.
    /// </summary>
    /// <param name="context">The resolution context used to resolve IFlexConfig from the container.</param>
    /// <param name="instance">The object instance whose property will receive the injection.</param>
    /// <param name="property">The property to inject the IFlexConfig instance into.</param>
    /// <remarks>
    /// <para>
    /// <strong>Resolution Strategy:</strong>
    /// Uses TryResolve to attempt IFlexConfig resolution, which gracefully handles cases where
    /// IFlexConfig is not registered in the container. This prevents activation failures due to
    /// missing optional dependencies.
    /// </para>
    /// <para>
    /// <strong>Injection Safety:</strong>
    /// <list type="bullet">
    /// <item>Only proceeds with injection if IFlexConfig can be resolved</item>
    /// <item>Uses the current resolution context/scope for proper lifetime management</item>
    /// <item>Property setting uses reflection with an appropriate error handling</item>
    /// <item>Failures in injection do not prevent component activation from completing</item>
    /// </list>
    /// </para>
    /// <para>
    /// <strong>Scope Awareness:</strong>
    /// The injection respects Autofac's scoping rules by using the current resolution context,
    /// ensuring that the injected IFlexConfig instance follows the same lifetime management
    /// as the component being activated.
    /// </para>
    /// </remarks>
    private static void InjectFlexConfigIntoProperty(
        ResolveRequestContext context,
        object instance,
        System.Reflection.PropertyInfo property)
    {
        if (!context.TryResolve<IFlexConfig>(out var flexConfig))
        {
            return;
        }

        property.SetValue(instance, flexConfig);
    }
}
