using BenchmarkDotNet.Running;
using FlexKit.Configuration.Providers.Azure.PerformanceTests.Benchmarks;

BenchmarkRunner.Run<AzureKeyVaultBenchmarks>();
BenchmarkRunner.Run<AzureAppConfigurationBenchmarks>();
BenchmarkRunner.Run<AzureConfigurationLoadingBenchmarks>();