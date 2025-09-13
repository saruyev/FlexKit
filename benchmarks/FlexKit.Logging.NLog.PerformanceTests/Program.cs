using BenchmarkDotNet.Running;
using FlexKit.Logging.NLog.PerformanceTests.Benchmarks;

BenchmarkRunner.Run<NLogAsyncMethodBenchmarks>();
BenchmarkRunner.Run<NLogComplexParameterBenchmarks>();
BenchmarkRunner.Run<NLogConfigurationOverheadBenchmarks>();
BenchmarkRunner.Run<NLogExclusionPatternBenchmarks>();
BenchmarkRunner.Run<NLogFormatterBenchmarks>();
BenchmarkRunner.Run<NLogInterceptionDecisionCacheBenchmarks>();
BenchmarkRunner.Run<NLogInterceptionOverheadBenchmarks>();
BenchmarkRunner.Run<NLogProviderComparisonBenchmarks>();
BenchmarkRunner.Run<NLogMemoryAllocationBenchmarks>();
BenchmarkRunner.Run<NLogMethodFrequencyBenchmarks>();