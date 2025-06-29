# FlexKit.Configuration Performance Benchmarks

Comprehensive performance testing suite for FlexKit.Configuration using BenchmarkDotNet to measure real-world performance characteristics of dynamic configuration access patterns.

## Benchmark Categories

### 1. DynamicAccessBenchmarks
Tests the performance overhead of different configuration access patterns:

- **StandardConfigurationAccess**: Baseline using `IConfiguration["key"]`
- **FlexConfigurationIndexerAccess**: FlexConfig using `flexConfig["key"]`
- **FlexConfigurationDynamicAccess**: Dynamic access using `config.Database.ConnectionString`
- **FlexConfigurationChainedDynamicAccess**: More realistic dynamic access pattern

**Key Questions Answered:**
- How much overhead does FlexConfiguration add over standard IConfiguration?
- What's the cost of dynamic property access vs indexer access?
- Is the convenience of dynamic access worth the performance cost?

### 2. TypeConversionBenchmarks
Compares FlexKit's type conversion methods against standard parsing:

- **StandardConfigurationIntParsing**: Using `int.Parse(config["key"])`
- **FlexConfigurationToTypeInt**: Using `config["key"].ToType<int>()`
- Similar tests for bool and double types
- **DynamicAccessWithTypeConversion**: Combined dynamic + conversion overhead

**Key Questions Answered:**
- How efficient are FlexKit's `ToType<T>()` extension methods?
- What's the overhead of culture-invariant parsing?
- Should you use FlexKit conversions or standard .NET parsing?

### 3. ConfigurationBuildingBenchmarks
Measures initialization and creation costs:

- **BuildStandardConfiguration**: Creating standard IConfiguration
- **BuildFlexConfiguration**: Wrapping with `new FlexConfiguration(config)`
- **BuildFlexConfigurationWithExtension**: Using `config.GetFlexConfiguration()`

**Key Questions Answered:**
- What's the startup cost of wrapping configurations?
- Are there differences between creation methods?
- How does FlexConfig creation scale with configuration size?

### 4. SectionNavigationBenchmarks
Tests deep configuration hierarchy navigation:

- **StandardConfigurationDeepAccess**: Using colon notation `"Level1:Level2:Level3:Property"`
- **FlexConfigurationDynamicDeepAccess**: Using `config.Level1.Level2.Level3.Property`
- **SectionNavigation**: Comparing section retrieval methods
- **MultipleSectionAccess**: Realistic multi-value access patterns

**Key Questions Answered:**
- How does dynamic navigation compare to direct key access?
- What's the cost of section caching vs repeated lookups?
- When should you cache configuration sections?

### 5. MemoryAllocationBenchmarks
Analyzes memory allocation patterns and GC pressure:

- **StandardConfigurationAllocations**: Baseline memory usage
- **FlexConfigurationIndexerAllocations**: FlexConfig indexer memory impact
- **FlexConfigurationDynamicAllocations**: Dynamic access allocation overhead
- **RepeatedDynamicAccess**: Memory cost of repeated dynamic calls
- **CachedSectionAccess**: Memory benefits of section caching

**Key Questions Answered:**
- How much extra memory does FlexConfig allocate?
- What allocation patterns emerge from dynamic access?
- Should you cache sections for memory efficiency?

## Running the Benchmarks

### Prerequisites
- .NET 8.0 SDK
- FlexKit.Configuration project built
- Release configuration for accurate results

### Command Line Execution
```bash
# Run all benchmarks
dotnet run -c Release

# Run specific benchmark category
dotnet run -c Release --filter "*DynamicAccess*"
dotnet run -c Release --filter "*TypeConversion*"
dotnet run -c Release --filter "*MemoryAllocation*"

# Generate detailed reports
dotnet run -c Release --exporters html,csv,json
```

### Benchmark Configuration
- **Job**: SimpleJob (single measurement)
- **Memory Diagnoser**: Enabled for allocation tracking
- **Baseline**: Standard IConfiguration methods marked as baseline
- **Warmup**: BenchmarkDotNet default (auto)
- **Iterations**: BenchmarkDotNet default (auto)

## Expected Results & Analysis

### Performance Expectations

**Dynamic Access Overhead:**
- Dynamic property access: 2-5x slower than indexer access
- Additional allocations from DLR and reflection
- TryGetMember calls create new FlexConfiguration instances

**Type Conversion Performance:**
- FlexKit ToType methods: 10-30% slower than direct parsing
- Culture-invariant parsing adds small overhead
- Null/empty value handling adds safety cost

**Memory Allocation Patterns:**
- Dynamic access creates temporary objects
- Section navigation allocates FlexConfiguration wrappers
- Repeated dynamic calls don't benefit from caching

### When to Use Each Pattern

**Use Dynamic Access When:**
- Developer productivity > performance
- Configuration access is infrequent (startup, occasional reads)
- Code readability is important
- Prototyping or development scenarios

**Use Indexer Access When:**
- Performance is critical
- Configuration is accessed frequently
- Memory allocation must be minimized
- Production hot paths

**Use Standard IConfiguration When:**
- Maximum performance required
- Working with existing codebases
- Minimal dependencies preferred
- Framework integration needed

## Interpreting Results

### Key Metrics to Watch

**Mean Execution Time:**
- Measures average method execution time
- Lower is better
- Compare ratios against baseline

**Allocated Memory:**
- Shows bytes allocated per operation
- Includes GC overhead impact
- Higher allocation = more GC pressure

**Gen 0/1/2 Collections:**
- Tracks garbage collection frequency
- Higher collection count indicates memory pressure
- Gen 2 collections are most expensive

### Sample Result Analysis

```
Method                              Mean      Allocated
StandardConfigurationAccess        50.0 ns   0 B
FlexConfigurationIndexerAccess     55.0 ns   0 B      (10% overhead)
FlexConfigurationDynamicAccess     180.0 ns  120 B    (260% overhead, allocations)
```

**Interpretation:**
- Indexer access has minimal overhead (acceptable)
- Dynamic access significantly slower with allocations
- 120B allocation suggests object creation overhead

## Custom Benchmark Development

### Adding New Benchmarks

```csharp
[MemoryDiagnoser]
[SimpleJob]
public class CustomBenchmarks
{
    private FlexConfiguration _config = null!;
    
    [GlobalSetup]
    public void Setup()
    {
        // Initialize test data
        var data = new Dictionary<string, string?> { /* test data */ };
        var builder = new ConfigurationBuilder();
        builder.AddInMemoryCollection(data);
        _config = new FlexConfiguration(builder.Build());
    }
    
    [Benchmark(Baseline = true)]
    public object BaselineMethod()
    {
        // Your baseline implementation
        return _config.Configuration["key"];
    }
    
    [Benchmark]
    public object TestMethod()
    {
        // Your test implementation
        dynamic config = _config;
        return config.Section.Key;
    }
}
```

### Configuration Scenarios

Test different configuration patterns:
- Large configuration files (100+ keys)
- Deep hierarchies (5+ levels)
- Array/collection configurations
- Missing key scenarios
- Multi-threaded access patterns

## Troubleshooting

### Common Issues

**Benchmark Not Running:**
- Ensure Release configuration: `dotnet run -c Release`
- Check project references are correct
- Verify .NET 8.0 SDK installation

**Unexpected Results:**
- JIT warmup affects first runs
- Background processes impact timing
- Debug vs Release builds show different patterns

**Memory Measurements:**
- Memory diagnoser only works in Release mode
- Some allocations may be optimized away
- GC collection can skew individual measurements

### Best Practices

**Environment Setup:**
- Close unnecessary applications
- Use dedicated benchmark machine
- Disable Windows Defender scanning
- Run multiple times for consistency

**Result Validation:**
- Compare across multiple runs
- Look for consistent patterns, not absolute numbers
- Focus on relative performance differences
- Validate results make logical sense

## Contributing

When adding new benchmarks:
1. Use realistic configuration data
2. Include memory allocation tracking
3. Provide baseline comparisons
4. Document expected patterns
5. Test edge cases and error conditions

Focus on real-world scenarios that developers encounter when choosing between configuration access patterns.