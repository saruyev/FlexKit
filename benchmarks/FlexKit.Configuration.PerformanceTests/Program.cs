using BenchmarkDotNet.Running;
using FlexKit.Configuration.PerformanceTests.Benchmarks;

BenchmarkRunner.Run<DynamicAccessBenchmarks>();
BenchmarkRunner.Run<TypeConversionBenchmarks>();
BenchmarkRunner.Run<ConfigurationBuildingBenchmarks>();
BenchmarkRunner.Run<SectionNavigationBenchmarks>();
BenchmarkRunner.Run<MemoryAllocationBenchmarks>();
BenchmarkRunner.Run<RegisterConfigBenchmarks>();
BenchmarkRunner.Run<LargeConfigurationBenchmarks>();