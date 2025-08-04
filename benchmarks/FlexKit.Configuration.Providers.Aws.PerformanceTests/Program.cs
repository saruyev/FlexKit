using BenchmarkDotNet.Running;
using FlexKit.Configuration.Providers.Aws.PerformanceTests.Benchmarks;

BenchmarkRunner.Run<AwsParameterStoreBenchmarks>();
BenchmarkRunner.Run<AwsSecretsManagerBenchmarks>();
BenchmarkRunner.Run<AwsConfigurationLoadingBenchmarks>();
