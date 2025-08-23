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
    public static void RegisterTypesWithLogging(this ContainerBuilder builder, IEnumerable<Type> types)
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
    public static void RegisterTypeWithLogging(this ContainerBuilder builder, Type type)
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
    public static void RegisterWithInterfaceInterception(this ContainerBuilder builder, Type type, IList<Type> interfaces) =>
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
    public static void RegisterWithClassInterception(this ContainerBuilder builder, Type type) =>
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
    public static void RegisterWithoutInterception(this ContainerBuilder builder, Type type)
    {
        Debug.WriteLine(
            $"Warning: Cannot intercept {type.FullName} - class is sealed or has no virtual methods. " +
            "Consider adding an interface or making methods virtual for logging to work.");

        builder.RegisterType(type)
            .AsSelf()
            .InstancePerLifetimeScope();
    }

    private static List<Type> GetUserInterfaces(Type type) =>
        [.. type.GetInterfaces().Where(i => !IsSystemInterface(i))];

    private static bool IsSystemInterface(Type interfaceType)
    {
        var name = interfaceType.FullName ?? "";

        return name.StartsWith("System.", StringComparison.InvariantCulture) ||
               name.StartsWith("Microsoft.", StringComparison.InvariantCulture) ||
               interfaceType.Assembly == typeof(object).Assembly;
    }

    private static bool CanUseClassInterception(Type type)
    {
        // Class must not be sealed
        if (type.IsSealed)
        {
            return false;
        }

        // Must have at least one public virtual method (excluding Object methods)
        return type.GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .Where(m => m.DeclaringType != typeof(object))
            .Any(m => m is { IsVirtual: true, IsFinal: false });
    }
}
