using BenchmarkDotNet.Attributes;
using FlexKit.Logging.PerformanceTests.Benchmarks;

// ReSharper disable ClassNeverInstantiated.Global

namespace FlexKit.Logging.ZLogger.PerformanceTests.Benchmarks;

public class ZLoggerAsyncMethodBenchmarks : AsyncMethodBenchmarks;

[MaxIterationCount(16)]
[InvocationCount(5_000)]
public class ZLoggerComplexParameterBenchmarks : ComplexParameterBenchmarks;

[MaxIterationCount(16)]
[InvocationCount(10_000)]
public class ZLoggerConfigurationOverheadBenchmarks : ConfigurationOverheadBenchmarks;

[MaxIterationCount(16)]
[InvocationCount(10_000)]
public class ZLoggerExclusionPatternBenchmarks : ExclusionPatternBenchmarks;

[MaxIterationCount(16)]
[InvocationCount(5_000)]
public class ZLoggerFormatterBenchmarks : FormatterBenchmarks;
public class ZLoggerInterceptionDecisionCacheBenchmarks : InterceptionDecisionCacheBenchmarks;

[MaxIterationCount(16)]
[InvocationCount(5_000)]
public class ZLoggerInterceptionOverheadBenchmarks : InterceptionOverheadBenchmarks;

[MaxIterationCount(16)]
[InvocationCount(1_000)]
public class ZLoggerMemoryAllocationBenchmarks : MemoryAllocationBenchmarks;

[MaxIterationCount(16)]
[InvocationCount(1)]
public class ZLoggerMethodFrequencyBenchmarks : MethodFrequencyBenchmarks;