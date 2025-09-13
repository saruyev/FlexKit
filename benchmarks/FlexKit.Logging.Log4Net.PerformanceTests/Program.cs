using BenchmarkDotNet.Running;
using FlexKit.Logging.Log4Net.PerformanceTests.Benchmarks;

BenchmarkRunner.Run<Log4NetAsyncMethodBenchmarks>();
BenchmarkRunner.Run<Log4NetBackgroundQueueBenchmarks>();
BenchmarkRunner.Run<Log4NetComplexParameterBenchmarks>();
BenchmarkRunner.Run<Log4NetConfigurationOverheadBenchmarks>();
BenchmarkRunner.Run<Log4NetExclusionPatternBenchmarks>();
BenchmarkRunner.Run<Log4NetFormatterBenchmarks>();
BenchmarkRunner.Run<Log4NetInterceptionDecisionCacheBenchmarks>();
BenchmarkRunner.Run<Log4NetInterceptionOverheadBenchmarks>();
BenchmarkRunner.Run<Log4NetProviderComparisonBenchmarks>();
BenchmarkRunner.Run<Log4NetMemoryAllocationBenchmarks>();
BenchmarkRunner.Run<Log4NetMethodFrequencyBenchmarks>();