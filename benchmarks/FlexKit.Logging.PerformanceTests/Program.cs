using BenchmarkDotNet.Running;
using FlexKit.Logging.PerformanceTests.Benchmarks;

BenchmarkRunner.Run<AsyncMethodBenchmarks>();
BenchmarkRunner.Run<BackgroundQueueBenchmarks>();
BenchmarkRunner.Run<ComplexParameterBenchmarks>();
BenchmarkRunner.Run<ConfigurationOverheadBenchmarks>();
BenchmarkRunner.Run<ExclusionPatternBenchmarks>();
BenchmarkRunner.Run<FormatterBenchmarks>();
BenchmarkRunner.Run<InterceptionDecisionCacheBenchmarks>();
BenchmarkRunner.Run<InterceptionOverheadBenchmarks>();
BenchmarkRunner.Run<MelProviderComparisonBenchmarks>();
BenchmarkRunner.Run<MemoryAllocationBenchmarks>();
BenchmarkRunner.Run<MethodFrequencyBenchmarks>();