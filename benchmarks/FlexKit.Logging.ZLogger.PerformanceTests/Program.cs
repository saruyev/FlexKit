using BenchmarkDotNet.Running;
using FlexKit.Logging.ZLogger.PerformanceTests.Benchmarks;

BenchmarkRunner.Run<ZLoggerAsyncMethodBenchmarks>();
BenchmarkRunner.Run<ZLoggerComplexParameterBenchmarks>();
BenchmarkRunner.Run<ZLoggerConfigurationOverheadBenchmarks>();
BenchmarkRunner.Run<ZLoggerExclusionPatternBenchmarks>();
BenchmarkRunner.Run<ZLoggerFormatterBenchmarks>();
BenchmarkRunner.Run<ZLoggerInterceptionDecisionCacheBenchmarks>();
BenchmarkRunner.Run<ZLoggerInterceptionOverheadBenchmarks>();
BenchmarkRunner.Run<ZLoggerProviderComparisonBenchmarks>();
BenchmarkRunner.Run<ZLoggerMemoryAllocationBenchmarks>();
BenchmarkRunner.Run<ZLoggerMethodFrequencyBenchmarks>();