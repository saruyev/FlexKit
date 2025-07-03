using BenchmarkDotNet.Running;
using FlexKit.Configuration.Providers.Yaml.PerformanceTests.Benchmarks;

BenchmarkRunner.Run<YamlLoadingBenchmarks>();
BenchmarkRunner.Run<YamlParsingBenchmarks>();