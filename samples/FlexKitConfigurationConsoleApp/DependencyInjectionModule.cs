using Autofac;
using FlexKit.Configuration.Core;
using FlexKitConfigurationConsoleApp.Services;

namespace FlexKitConfigurationConsoleApp;

/// <summary>
/// Autofac module for registering application services and dependencies.
/// This demonstrates assembly scanning through FlexKit.Configuration.
/// </summary>
public class DependencyInjectionModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        // Register services with various lifetime scopes
        builder.RegisterType<DatabaseService>()
            .As<IDatabaseService>()
            .SingleInstance();

        builder.RegisterType<ApiService>()
            .As<IApiService>()
            .SingleInstance();

        builder.RegisterType<ServerManagementService>()
            .As<IServerManagementService>()
            .PropertiesAutowired() // Enable property injection
            .SingleInstance();
    }
}