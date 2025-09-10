using BenchmarkDotNet.Attributes;
using FlexKit.Configuration.Core;
using JetBrains.Annotations;
using Microsoft.Extensions.Hosting;
// ReSharper disable NullableWarningSuppressionIsUsed

namespace FlexKit.Logging.PerformanceTests.Benchmarks;

[MemoryDiagnoser]
public class FlexKitBenchmarkBase
{
    [UsedImplicitly]
    protected IHost FlexKitHost = null!;
    protected IServiceProvider FlexKitServices = null!;

    public virtual void Setup()
    {
        FlexKitHost = Host.CreateDefaultBuilder()
            .AddFlexConfig()  // Provider auto-detected based on project references
            .Build();

        FlexKitServices = FlexKitHost.Services;
    }

    [GlobalCleanup]
    public virtual void Cleanup()
    {
        FlexKitHost.Dispose();
    }
}