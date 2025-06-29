using Autofac;
using Autofac.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Reqnroll;
using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using JetBrains.Annotations;

namespace FlexKit.IntegrationTests.Utils;

public abstract class BaseTestContainerBuilder<T> where T : BaseTestContainerBuilder<T>, new()
{
    protected readonly ContainerBuilder ContainerBuilder;
    protected readonly List<Action<ContainerBuilder>> Registrations;
    private readonly List<Action<IServiceCollection>> _serviceRegistrations;
    protected IConfiguration? Configuration;
    private ILoggerFactory? _loggerFactory;
    private MockFileSystem? _mockFileSystem;
    private ScenarioContext? _scenarioContext;

    /// <summary>
    /// Creates a new TestContainerBuilder.
    /// </summary>
    /// <param name="scenarioContext">Optional scenario context for automatic cleanup registration</param>
    protected BaseTestContainerBuilder(ScenarioContext? scenarioContext = null)
    {
        ContainerBuilder = new ContainerBuilder();
        Registrations = new List<Action<ContainerBuilder>>();
        _serviceRegistrations = new List<Action<IServiceCollection>>();
        _scenarioContext = scenarioContext;
    }

    /// <summary>
    /// Creates a TestContainerBuilder with automatic cleanup registration.
    /// </summary>
    /// <param name="scenarioContext">Scenario context for cleanup registration</param>
    /// <returns>New TestContainerBuilder instance</returns>
    public static T Create(ScenarioContext scenarioContext)
    {
        return new T{ _scenarioContext = scenarioContext };
    }

    /// <summary>
    /// Sets the configuration to use in the container.
    /// </summary>
    /// <param name="configuration">The configuration instance</param>
    /// <returns>This builder for method chaining</returns>
    public T WithConfiguration(IConfiguration configuration)
    {
        Configuration = configuration;
        return (T)this;
    }

    /// <summary>
    /// Sets up configuration using the provided configuration data.
    /// </summary>
    /// <param name="configurationData">Dictionary of configuration key-value pairs</param>
    /// <returns>This builder for method chaining</returns>
    [UsedImplicitly]
    public T WithConfiguration(Dictionary<string, string?> configurationData)
    {
        var builder = new ConfigurationBuilder();
        builder.AddInMemoryCollection(configurationData);
        Configuration = builder.Build();
        return (T)this;
    }

    /// <summary>
    /// Configures logging for the container.
    /// </summary>
    /// <param name="configureLogging">Action to configure logging</param>
    /// <returns>This builder for method chaining</returns>
    private T WithLogging(Action<ILoggingBuilder>? configureLogging = null)
    {
        _loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.AddConsole().SetMinimumLevel(LogLevel.Debug);
            configureLogging?.Invoke(builder);
        });

        Registrations.Add(builder =>
        {
            builder.RegisterInstance(_loggerFactory).As<ILoggerFactory>().SingleInstance();
            builder.RegisterGeneric(typeof(Logger<>)).As(typeof(ILogger<>)).SingleInstance();
        });

        return (T)this;
    }

    /// <summary>
    /// Configures the container to use a mock file system instead of the real one.
    /// </summary>
    /// <param name="configureMockFileSystem">Optional action to configure the mock file system</param>
    /// <returns>This builder for method chaining</returns>
    public T WithMockFileSystem(Action<MockFileSystem>? configureMockFileSystem = null)
    {
        _mockFileSystem = new MockFileSystem();
        configureMockFileSystem?.Invoke(_mockFileSystem);

        Registrations.Add(builder =>
        {
            builder.RegisterInstance(_mockFileSystem).As<IFileSystem>().SingleInstance();
        });

        return (T)this;
    }

    /// <summary>
    /// Configures the container to use the real file system (default behavior).
    /// </summary>
    /// <returns>This builder for method chaining</returns>
    private T WithRealFileSystem()
    {
        _mockFileSystem = null;

        Registrations.Add(builder =>
        {
            builder.RegisterType<FileSystem>().As<IFileSystem>().SingleInstance();
        });

        return (T)this;
    }

    /// <summary>
    /// Adds custom registrations to the Autofac container.
    /// </summary>
    /// <param name="registration">Action to configure the container builder</param>
    /// <returns>This builder for method chaining</returns>
    public T WithRegistration(Action<ContainerBuilder> registration)
    {
        Registrations.Add(registration);
        return (T)this;
    }

    /// <summary>
    /// Adds Microsoft DI service registrations.
    /// </summary>
    /// <param name="registration">Action to configure the service collection</param>
    /// <returns>This builder for method chaining</returns>
    public T WithServices(Action<IServiceCollection> registration)
    {
        _serviceRegistrations.Add(registration);
        return (T)this;
    }

    /// <summary>
    /// Registers a singleton service instance.
    /// </summary>
    /// <typeparam name="TInterface">The service interface type</typeparam>
    /// <param name="instance">The service instance</param>
    /// <returns>This builder for method chaining</returns>
    public T WithSingleton<TInterface>(TInterface instance)
        where TInterface : class
    {
        Registrations.Add(builder => builder.RegisterInstance(instance).As<TInterface>().SingleInstance());
        return (T)this;
    }

    /// <summary>
    /// Registers a singleton service implementation.
    /// </summary>
    /// <typeparam name="TInterface">The service interface type</typeparam>
    /// <typeparam name="TImplementation">The service implementation type</typeparam>
    /// <returns>This builder for method chaining</returns>
    public T WithSingleton<TInterface, TImplementation>()
        where TInterface : class
        where TImplementation : class, TInterface
    {
        Registrations.Add(builder => builder.RegisterType<TImplementation>().As<TInterface>().SingleInstance());
        return (T)this;
    }

    /// <summary>
    /// Registers a transient service implementation.
    /// </summary>
    /// <typeparam name="TInterface">The service interface type</typeparam>
    /// <typeparam name="TImplementation">The service implementation type</typeparam>
    /// <returns>This builder for method chaining</returns>
    public T WithTransient<TInterface, TImplementation>()
        where TInterface : class
        where TImplementation : class, TInterface
    {
        Registrations.Add(builder => builder.RegisterType<TImplementation>().As<TInterface>().InstancePerDependency());
        return (T)this;
    }

    /// <summary>
    /// Registers a scoped service implementation.
    /// </summary>
    /// <typeparam name="TInterface">The service interface type</typeparam>
    /// <typeparam name="TImplementation">The service implementation type</typeparam>
    /// <returns>This builder for method chaining</returns>
    public T WithScoped<TInterface, TImplementation>()
        where TInterface : class
        where TImplementation : class, TInterface
    {
        Registrations.Add(builder => builder.RegisterType<TImplementation>().As<TInterface>().InstancePerLifetimeScope());
        return (T)this;
    }

    /// <summary>
    /// Configures the container for ASP.NET Core integration testing.
    /// </summary>
    /// <param name="configureServices">Optional action to configure services</param>
    /// <returns>This builder for method chaining</returns>
    public T WithAspNetCore(Action<IServiceCollection>? configureServices = null)
    {
        _serviceRegistrations.Add(services =>
        {
            services.AddLogging();
            services.AddOptions();
            configureServices?.Invoke(services);
        });

        return (T)this;
    }

    /// <summary>
    /// Sets up common test services and configurations.
    /// </summary>
    /// <returns>This builder for method chaining</returns>
    [UsedImplicitly]
    public T WithTestDefaults()
    {
        return WithLogging()
            .WithRealFileSystem()
            .WithConfiguration(new Dictionary<string, string?>
            {
                ["Logging:LogLevel:Default"] = "Debug",
                ["Logging:LogLevel:Microsoft"] = "Warning",
                ["Logging:LogLevel:System"] = "Warning"
            });
    }

    /// <summary>
    /// Builds the Autofac container with all configured registrations.
    /// </summary>
    /// <returns>The configured Autofac container</returns>
    [UsedImplicitly]
    public virtual IContainer Build()
    {
        RegisterServices();
        return ApplyRegistrations();
    }

    protected IContainer ApplyRegistrations()
    {
        // Apply all custom registrations
        foreach (var registration in Registrations)
        {
            registration(ContainerBuilder);
        }

        // Build the container
        var container = ContainerBuilder.Build();

        // Register for cleanup if a scenario context is available
        if (_scenarioContext != null)
        {
            _scenarioContext.RegisterAutofacContainer(container);
            
            if (_mockFileSystem != null)
            {
                _scenarioContext.RegisterMockFileSystem(_mockFileSystem);
            }
        }

        return container;
    }

    protected void RegisterServices()
    {
        // Register Microsoft DI services first
        if (_serviceRegistrations.Count > 0)
        {
            var services = new ServiceCollection();
            foreach (var registration in _serviceRegistrations)
            {
                registration(services);
            }
            ContainerBuilder.Populate(services);
        }

        // Register configuration if provided
        if (Configuration != null)
        {
            ContainerBuilder.RegisterInstance(Configuration).As<IConfiguration>().SingleInstance();
        }
    }

    /// <summary>
    /// Builds the container and returns a service provider wrapper.
    /// </summary>
    /// <returns>A service provider that wraps the Autofac container</returns>
    public IServiceProvider BuildServiceProvider()
    {
        var container = Build();
        var serviceProvider = new AutofacServiceProvider(container);

        _scenarioContext?.RegisterServiceProvider(serviceProvider);
        
        return serviceProvider;
    }

    /// <summary>
    /// Builds the container and creates a lifetime scope.
    /// </summary>
    /// <returns>A new lifetime scope</returns>
    public ILifetimeScope BuildLifetimeScope()
    {
        var container = Build();
        var scope = container.BeginLifetimeScope();

        _scenarioContext?.RegisterForCleanup(scope);

        return scope;
    }
}
