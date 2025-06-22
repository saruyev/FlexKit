#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using Autofac;
using AutoFixture;
using AutoFixture.AutoNSubstitute;
using NSubstitute;

namespace FlexKit.Configuration.Tests.TestBase;

/// <summary>
/// Base class for unit tests providing Autofac container, AutoFixture, and common test utilities.
/// Manages test lifecycle, dependency injection, and data generation consistently across test classes.
/// </summary>
public abstract class UnitTestBase : IDisposable
{
    private bool _disposed;

    /// <summary>
    /// Autofac container for dependency injection in tests.
    /// </summary>
    protected IContainer Container { get; private set; } = null!;

    /// <summary>
    /// AutoFixture instance for generating test data with auto-mocking support.
    /// </summary>
    protected IFixture Fixture { get; private set; } = null!;

    /// <summary>
    /// Initializes the test base with container and fixture setup.
    /// </summary>
    protected UnitTestBase()
    {
        SetupFixture();
        SetupContainer();
    }

    /// <summary>
    /// Configures AutoFixture with auto-mocking and test-specific customizations.
    /// </summary>
    private void SetupFixture()
    {
        Fixture = new Fixture();
        
        // Enable auto-mocking with NSubstitute
        Fixture.Customize(new AutoNSubstituteCustomization
        {
            ConfigureMembers = true,
            GenerateDelegates = true
        });

        // Customize common behaviors
        Fixture.Behaviors.OfType<ThrowingRecursionBehavior>()
            .ToList()
            .ForEach(b => Fixture.Behaviors.Remove(b));
        Fixture.Behaviors.Add(new OmitOnRecursionBehavior());

        // Register custom customizations
        RegisterFixtureCustomizations();
    }

    /// <summary>
    /// Override to register custom AutoFixture customizations.
    /// </summary>
    protected virtual void RegisterFixtureCustomizations()
    {
        // Base implementation - override in derived classes
    }

    /// <summary>
    /// Sets up Autofac container with test-specific registrations.
    /// </summary>
    private void SetupContainer()
    {
        var builder = new ContainerBuilder();
        
        // Register AutoFixture as a service
        builder.RegisterInstance(Fixture).As<IFixture>();
        
        // Allow derived classes to configure the container
        ConfigureContainer(builder);
        
        Container = builder.Build();
    }

    /// <summary>
    /// Override to configure the Autofac container for specific test scenarios.
    /// </summary>
    /// <param name="builder">Container builder to configure.</param>
    protected virtual void ConfigureContainer(ContainerBuilder builder)
    {
        // Base implementation - override in derived classes
    }

    /// <summary>
    /// Creates and returns a mock of type T using NSubstitute.
    /// </summary>
    protected T CreateMock<T>() where T : class
    {
        return Substitute.For<T>();
    }

    /// <summary>
    /// Creates a test instance of type T using AutoFixture.
    /// </summary>
    protected T Create<T>()
    {
        return Fixture.Create<T>();
    }

    /// <summary>
    /// Creates a collection of test instances using AutoFixture.
    /// </summary>
    protected IEnumerable<T> CreateMany<T>(int count = 3)
    {
        return Fixture.CreateMany<T>(count);
    }

    /// <summary>
    /// Resolves a service from the Autofac container.
    /// </summary>
    protected T Resolve<T>() where T : notnull
    {
        return Container.Resolve<T>();
    }

    /// <summary>
    /// Resolves a service from the Autofac container with optional registration.
    /// </summary>
    protected T? ResolveOptional<T>() where T : class
    {
        return Container.ResolveOptional<T>();
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed && disposing)
        {
            Container?.Dispose();
            _disposed = true;
        }
    }
}
