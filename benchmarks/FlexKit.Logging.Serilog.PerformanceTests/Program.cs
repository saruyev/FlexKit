using BenchmarkDotNet.Running;
using FlexKit.Logging.Serilog.PerformanceTests.Benchmarks;

BenchmarkRunner.Run<SerilogAsyncMethodBenchmarks>();
BenchmarkRunner.Run<SerilogComplexParameterBenchmarks>();
BenchmarkRunner.Run<SerilogConfigurationOverheadBenchmarks>();
BenchmarkRunner.Run<SerilogExclusionPatternBenchmarks>();
BenchmarkRunner.Run<SerilogFormatterBenchmarks>();
BenchmarkRunner.Run<SerilogInterceptionDecisionCacheBenchmarks>();
BenchmarkRunner.Run<SerilogInterceptionOverheadBenchmarks>();
BenchmarkRunner.Run<SerilogProviderComparisonBenchmarks>();
BenchmarkRunner.Run<SerilogMemoryAllocationBenchmarks>();
BenchmarkRunner.Run<SerilogMethodFrequencyBenchmarks>();