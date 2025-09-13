using System.Diagnostics;
using System.Reflection;
using Autofac;
using Autofac.Extras.DynamicProxy;
using FlexKit.Logging.Interception;

namespace FlexKit.Logging.Detection;

/// <summary>
/// Extension methods for registering types with logging interception in Autofac.
/// Handles the different interception strategies based on type characteristics.
/// </summary>
internal static class TypeRegistrationExtensions
{
    /// <summary>
    /// Registers a collection of types with appropriate logging interception.
    /// Automatically determines the best interception strategy for each type.
    /// </summary>
    /// <param name="builder">The Autofac container builder.</param>
    /// <param name="types">Types to register with logging interception.</param>
    public static void RegisterTypesWithLogging(
        this ContainerBuilder builder,
        IEnumerable<Type> types)
    {
        foreach (var type in types)
        {
            builder.RegisterTypeWithLogging(type);
        }
    }

    /// <summary>
    /// Registers a single type with appropriate logging interception.
    /// Uses interface interception when possible, falls back to class interception,
    /// or registers without interception if neither is viable.
    /// </summary>
    /// <param name="builder">The Autofac container builder.</param>
    /// <param name="type">Type to register with logging interception.</param>
    private static void RegisterTypeWithLogging(
        this ContainerBuilder builder,
        Type type)
    {
        var userInterfaces = GetUserInterfaces(type);

        if (userInterfaces.Count > 0)
        {
            builder.RegisterWithInterfaceInterception(type, userInterfaces);
        }
        else if (CanUseClassInterception(type))
        {
            builder.RegisterWithClassInterception(type);
        }
        else
        {
            builder.RegisterWithoutInterception(type);
        }
    }

    /// <summary>
    /// Registers a type with interface-based interception.
    /// The type will be intercepted when accessed through any of its user-defined interfaces.
    /// </summary>
    /// <param name="builder">The Autofac container builder.</param>
    /// <param name="type">The implementation type.</param>
    /// <param name="interfaces">User-defined interfaces to register as.</param>
    private static void RegisterWithInterfaceInterception(
        this ContainerBuilder builder,
        Type type,
        IList<Type> interfaces) =>
        builder.RegisterType(type)
            .As(interfaces.ToArray())
            .EnableInterfaceInterceptors()
            .InterceptedBy(typeof(MethodLoggingInterceptor))
            .InstancePerLifetimeScope();

    /// <summary>
    /// Registers a type with class-based interception.
    /// Used when the type has no user-defined interfaces but has virtual methods.
    /// </summary>
    /// <param name="builder">The Autofac container builder.</param>
    /// <param name="type">The type to register with class interception.</param>
    private static void RegisterWithClassInterception(
        this ContainerBuilder builder,
        Type type) =>
        builder.RegisterType(type)
            .AsSelf()
            .EnableClassInterceptors()
            .InterceptedBy(typeof(MethodLoggingInterceptor))
            .InstancePerLifetimeScope();

    /// <summary>
    /// Registers a type without interception when it cannot be intercepted.
    /// Logs a warning to help developers understand why logging won't work for this type.
    /// </summary>
    /// <param name="builder">The Autofac container builder.</param>
    /// <param name="type">The type to register without interception.</param>
    private static void RegisterWithoutInterception(
        this ContainerBuilder builder,
        Type type)
    {
        Debug.WriteLine(
            $"Warning: Cannot intercept {type.FullName} - class is sealed or has no virtual methods. " +
            "Consider adding an interface or making methods virtual for logging to work.");

        builder.RegisterType(type)
            .AsSelf()
            .InstancePerLifetimeScope();
    }

    /// <summary>
    /// Retrieves a list of user-defined interfaces implemented by the specified type,
    /// excluding interfaces from .NET or Microsoft system libraries.
    /// </summary>
    /// <param name="type">The type for which user-defined interfaces are to be retrieved.</param>
    /// <returns>A list of user-defined interface types implemented by the specified type.</returns>
    private static List<Type> GetUserInterfaces(Type type) =>
        [.. type.GetInterfaces().Where(i => !IsSystemInterface(i))];

    /// <summary>
    /// Determines whether the given interface type belongs to the .NET or Microsoft system libraries.
    /// </summary>
    /// <param name="interfaceType">The interface type to evaluate.</param>
    /// <returns>True if the interface is part of the system libraries; otherwise, false.</returns>
    private static bool IsSystemInterface(Type interfaceType)
    {
        var name = interfaceType.FullName ?? "";

        return name.StartsWith("System.", StringComparison.InvariantCulture) ||
               name.StartsWith("Microsoft.", StringComparison.InvariantCulture) ||
               interfaceType.Assembly == typeof(object).Assembly;
    }

    /// <summary>
    /// Determines whether a given type can be intercepted using class-based interception.
    /// A type is eligible if it is non-sealed and has at least one public virtual method
    /// that is not final and is not inherited from the <see cref="object"/> class.
    /// </summary>
    /// <param name="type">The type to evaluate for class-based interception support.</param>
    /// <returns>True if the type can be intercepted using class-based interception; otherwise, false.</returns>
    private static bool CanUseClassInterception(Type type)
    {
        // Class must not be sealed
        if (type.IsSealed)
        {
            return false;
        }

        // Must have at least one public virtual method (excluding Object methods)
        return type.GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .Any(m => m.DeclaringType != typeof(object) && m is { IsVirtual: true, IsFinal: false });
    }
}
