# FlexKit.Configuration.Providers.Yaml Performance Tests

Performance benchmarks for the YAML configuration provider using BenchmarkDotNet to measure real-world YAML file loading and parsing performance.

## Benchmark Categories

### YamlLoadingBenchmarks
Tests the performance overhead of YAML file loading and parsing:

- **LoadSimpleJsonConfiguration** (Baseline): JSON file loading baseline using `AddJsonStream`
- **LoadSimpleYamlConfiguration**: Simple YAML file loading using `YamlConfigurationSource`
- **LoadSimpleYamlFlexConfiguration**: Simple YAML file loading wrapped with `FlexConfiguration`
- **LoadComplexYamlConfiguration**: Complex hierarchical YAML file loading
- **LoadComplexYamlFlexConfiguration**: Complex YAML file with FlexConfiguration wrapper
- **LoadLargeYamlConfiguration**: Large enterprise-scale YAML file loading
- **LoadLargeYamlFlexConfiguration**: Large YAML file with FlexConfiguration wrapper
- **LoadYamlFromFile**: File-based YAML loading (vs stream-based)
- **LoadYamlFromFileToFlexConfig**: File-based YAML loading with FlexConfiguration
- **LoadYamlWithFlexConfigurationBuilder**: YAML loading using FlexConfigurationBuilder
- **LoadMixedSourcesWithFlexConfigurationBuilder**: Mixed YAML and JSON sources

**Key Questions Answered:**
- How does YAML parsing performance compare to JSON?
- What's the overhead of FlexConfiguration wrapping for YAML sources?
- How does YAML performance scale with configuration complexity?
- What's the cost of file-based vs stream-based loading?

### YamlParsingBenchmarks
Tests the performance impact of specific YAML syntax features and parsing approaches:

- **ParseSimpleYaml** (Baseline): Basic key-value pairs with common data types
- **ParseYamlWithAnchors**: YAML anchors and aliases for configuration reuse
- **ParseYamlWithMultilineStrings**: Literal (`|`) and folded (`>`) multi-line strings
- **ParseYamlWithArrays**: Complex arrays and nested object structures
- **ParseYamlWithDeepNesting**: Deep hierarchical structures (6+ levels)
- **ParseYamlWithMixedTypes**: Various data types (strings, numbers, booleans, nulls, dates)
- **ParseYamlWithUnicode**: Unicode characters and special character handling
- **ParseYamlFromFile**: File-based vs memory-based parsing comparison
- **ParseComplexYamlAllFeatures**: Combined YAML features in single document

**Key Questions Answered:**
- Which YAML features have the highest parsing overhead?
- How do anchors and aliases impact parsing performance?
- What's the cost of multi-line string processing?
- How does Unicode content affect parsing speed?
- Which YAML syntax patterns should be avoided for performance?

## Test Data Files

The benchmarks use the following test files in the `TestData/` directory:

### YamlLoadingBenchmarks Files

**simple-config.yaml**
Basic YAML configuration with simple key-value pairs, basic nesting, and common data types. Used for baseline YAML parsing performance.

**complex-config.yaml**
Advanced YAML configuration featuring:
- Deep hierarchical structures (4-5 levels)
- Arrays and complex objects
- Multiple API configurations
- Server cluster definitions
- Feature toggles with nested conditions
- Multi-environment settings
- Complex logging configuration
- Security and CORS settings

**large-config.yaml**
Enterprise-scale YAML configuration with 200+ keys across 6 levels of nesting, including:
- Multi-region database clusters
- Comprehensive cache layers
- Microservices API gateway configuration
- External service integrations
- Security and compliance settings
- Monitoring and observability
- Performance optimization settings

**equivalent-config.json**
JSON equivalent of simple-config.yaml for direct performance comparison between YAML and JSON parsing.

### YamlParsingBenchmarks Files

**anchors-aliases.yaml**
YAML configuration demonstrating anchor and alias usage:
- Default configuration anchors
- Environment-specific configurations using `<<:` merge syntax
- Shared service configurations
- Nested anchor inheritance
- Override patterns with anchors

**literal-folded-strings.yaml**
Multi-line string examples showcasing:
- Literal style (`|`) preserving line breaks for documentation and scripts
- Folded style (`>`) for long descriptions and terms
- Complex formatting preservation
- Script and configuration templates
- Unicode and special character handling

**arrays-config.yaml** (Optional)
Complex array and list structures for testing array parsing performance.

**nested-config.yaml** (Optional)
Deep nesting examples for testing navigation performance.

## Running the Benchmarks

### Prerequisites
- .NET 9.0 SDK
- FlexKit.Configuration projects built
- Release configuration for accurate results

### Command Line Execution
```bash
# Run all YAML loading benchmarks
dotnet run -c Release

# Run from solution root using script
.\benchmarks.ps1 -ProjectPath "FlexKit.Configuration.Providers.Yaml.PerformanceTests"

# Run specific benchmark methods
dotnet run -c Release --filter "*Simple*"
dotnet run -c Release --filter "*Complex*"
dotnet run -c Release --filter "*Large*"

# Generate detailed reports
dotnet run -c Release --exporters html,csv,json
```

### Benchmark Configuration
- **Job**: SimpleJob (single measurement)
- **Memory Diagnoser**: Enabled for allocation tracking
- **Baseline**: JSON configuration loading marked as baseline
- **Warmup**: BenchmarkDotNet default (auto)
- **Iterations**: BenchmarkDotNet default (auto)

## Expected Results & Analysis

### YamlParsingBenchmarks Results

| Method                        | Mean       | Error    | StdDev   | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------------ |-----------:|---------:|---------:|------:|--------:|-------:|----------:|------------:|
| ParseSimpleYaml               |   847.0 us | 15.23 us | 11.89 us |  1.00 |    0.02 |      - |  58.21 KB |        1.00 |
| ParseYamlWithAnchors          |   886.4 us | 16.98 us | 15.88 us |  1.05 |    0.02 | 1.9531 |  82.47 KB |        1.42 |
| ParseYamlWithMultilineStrings |   921.7 us | 13.88 us | 11.59 us |  1.09 |    0.02 |      - |  68.73 KB |        1.18 |
| ParseYamlWithArrays           | 1,055.5 us | 20.90 us | 24.88 us |  1.25 |    0.03 | 3.9063 | 146.23 KB |        2.51 |
| ParseYamlWithDeepNesting      |   972.7 us | 19.26 us | 27.00 us |  1.15 |    0.04 | 1.9531 | 106.44 KB |        1.83 |
| ParseYamlWithMixedTypes       |   986.4 us | 19.17 us | 26.88 us |  1.16 |    0.03 |      - | 109.85 KB |        1.89 |
| ParseYamlWithUnicode          |   933.0 us | 13.96 us | 11.66 us |  1.10 |    0.02 | 1.9531 |  78.59 KB |        1.35 |
| ParseYamlFromFile             |   838.1 us |  8.08 us |  7.56 us |  0.99 |    0.02 | 0.9766 |  57.66 KB |        0.99 |
| ParseComplexYamlAllFeatures   | 1,167.4 us | 23.17 us | 30.93 us |  1.38 |    0.04 | 3.9063 | 210.05 KB |        3.61 |

### YAML Parsing Feature Analysis

**Performance Impact by YAML Feature:**

**Anchors and Aliases (+5% overhead):**
- Minimal performance impact (886 μs vs 847 μs baseline)
- Moderate memory increase (+42% allocations, 82KB vs 58KB)
- **Recommendation**: Anchors are performance-friendly for configuration reuse

**Multi-line Strings (+9% overhead):**
- Low performance impact (922 μs, +18% memory)
- Literal (`|`) and folded (`>`) styles have similar overhead
- **Recommendation**: Safe to use for documentation and scripts

**Arrays and Objects (+25% overhead):**
- Most significant performance impact (1,056 μs, +151% memory)
- Complex nested arrays are expensive to parse
- Memory allocations increase dramatically (146KB vs 58KB)
- **Recommendation**: Consider flattening complex array structures

**Deep Nesting (+15% overhead):**
- Moderate performance impact (973 μs, +83% memory)
- 6+ levels of nesting add parsing complexity
- **Recommendation**: Limit nesting depth where possible

**Mixed Data Types (+16% overhead):**
- Consistent performance impact (986 μs, +89% memory)
- Type inference and conversion overhead
- **Recommendation**: Use consistent data types when possible

**Unicode Content (+10% overhead):**
- Low performance impact (933 μs, +35% memory)
- International characters handled efficiently
- **Recommendation**: Unicode content is well-optimized

**Combined Features (+38% overhead):**
- Highest performance impact (1,167 μs, +261% memory)
- Multiple features compound the overhead
- **Recommendation**: Use YAML features judiciously in large configs

### Key Insights for YAML Optimization

**Most Expensive Features (ranked by overhead):**
1. **Arrays and Objects** (+25%, +151% memory) - Avoid deeply nested arrays
2. **Mixed Data Types** (+16%, +89% memory) - Use consistent types
3. **Deep Nesting** (+15%, +83% memory) - Limit nesting levels
4. **Unicode Content** (+10%, +35% memory) - Well-optimized
5. **Multi-line Strings** (+9%, +18% memory) - Minimal impact
6. **Anchors and Aliases** (+5%, +42% memory) - Very efficient

**Memory Allocation Patterns:**
- **Simple YAML**: 58KB baseline allocation
- **Complex arrays**: 2.5x more memory than baseline
- **Combined features**: 3.6x more memory than baseline
- Arrays and nested objects are primary memory consumers

**File vs Memory Parsing:**
- File-based parsing is actually slightly faster (838 μs vs 847 μs)
- Nearly identical memory usage (57.66KB vs 58.21KB)
- File I/O overhead is minimal for YAML parsing

**Best Practices for Performance:**
1. **Prefer flat structures** over deeply nested hierarchies
2. **Use anchors for reuse** - they're very efficient
3. **Limit complex arrays** - biggest performance impact
4. **Unicode is fine** - well-optimized by YamlDotNet
5. **Multi-line strings are cheap** - use freely for documentation
6. **File-based loading** has no significant overhead

**When to Optimize:**
- **< 1ms total**: All YAML features perform acceptably
- **Arrays with 10+ nested objects**: Consider restructuring
- **6+ levels of nesting**: Consider flattening
- **Combined complex features**: Monitor memory usage in production

| Method                                       | Mean        | Error      | StdDev     | Median      | Ratio  | RatioSD | Gen0    | Gen1   | Gen2   | Allocated | Alloc Ratio |
|--------------------------------------------- |------------:|-----------:|-----------:|------------:|-------:|--------:|--------:|-------:|-------:|----------:|------------:|
| LoadSimpleJsonConfiguration                  |    10.57 us |   0.364 us |   1.073 us |    10.72 us |   1.01 |    0.15 |  0.5951 | 0.0153 | 0.0153 |         - |          NA |
| LoadSimpleYamlConfiguration                  |   266.54 us |   7.924 us |  22.990 us |   274.90 us |  25.49 |    3.46 |  1.9531 |      - |      - |   97543 B |          NA |
| LoadSimpleYamlFlexConfiguration              |   276.55 us |   9.334 us |  27.375 us |   287.62 us |  26.45 |    3.81 |  1.9531 |      - |      - |   97567 B |          NA |
| LoadComplexYamlConfiguration                 |   823.46 us |  34.609 us |  99.855 us |   806.71 us |  78.76 |   12.61 |  7.8125 |      - |      - |  411814 B |          NA |
| LoadComplexYamlFlexConfiguration             |   896.85 us |  40.292 us | 118.801 us |   887.14 us |  85.78 |   14.48 |  7.8125 |      - |      - |  411838 B |          NA |
| LoadLargeYamlConfiguration                   | 2,475.20 us | 112.159 us | 330.702 us | 2,355.15 us | 236.73 |   40.17 | 50.7813 | 7.8125 |      - | 1292191 B |          NA |
| LoadLargeYamlFlexConfiguration               | 2,488.24 us | 112.927 us | 332.967 us | 2,378.98 us | 237.98 |   40.42 | 31.2500 | 3.9063 |      - | 1292138 B |          NA |
| LoadYamlFromFile                             |   788.17 us |  29.260 us |  85.814 us |   802.40 us |  75.38 |   11.38 |  7.8125 |      - |      - |  411814 B |          NA |
| LoadYamlFromFileToFlexConfig                 |   790.85 us |  28.353 us |  83.599 us |   786.45 us |  75.64 |   11.25 |  7.8125 |      - |      - |  411838 B |          NA |
| LoadYamlWithFlexConfigurationBuilder         |   781.25 us |  17.005 us |  50.139 us |   780.94 us |  74.72 |    9.17 | 15.6250 |      - |      - |  411981 B |          NA |
| LoadMixedSourcesWithFlexConfigurationBuilder |   579.13 us |  11.008 us |  12.235 us |   582.87 us |  55.39 |    5.90 |  1.9531 |      - |      - |  124553 B |          NA |

### Performance Analysis

**YAML vs JSON Loading Performance:**
- **Simple YAML is ~25x slower than JSON** (266 μs vs 10.6 μs)
- **Complex YAML is ~78x slower than JSON** for complex configurations
- **Large YAML is ~237x slower than JSON** for enterprise-scale configs
- Performance gap increases dramatically with configuration complexity

**FlexConfiguration Overhead:**
- **Minimal FlexConfiguration overhead**: Only ~1-4% additional time (276 μs vs 266 μs for simple)
- **Consistent overhead pattern**: FlexConfiguration wrapping adds ~10-24 bytes of allocations
- **Scales well**: Overhead remains proportionally small across all configuration sizes

**Memory Allocation Patterns:**
- **JSON baseline**: Very low allocations for simple configurations
- **Simple YAML**: ~97KB allocations (vs minimal for JSON)
- **Complex YAML**: ~412KB allocations for hierarchical structures
- **Large YAML**: ~1.3MB allocations for enterprise configurations
- **Linear scaling**: Memory usage scales predictably with YAML complexity

**Loading Method Comparison:**
- **File vs Stream loading**: Negligible difference (788 μs vs 823 μs for complex)
- **FlexConfigurationBuilder**: Slight overhead (~781 μs) but maintains clean API
- **Mixed sources**: Reasonable performance (579 μs) for YAML+JSON combination

### Key Insights

**When to Use YAML:**
- ✅ **Development and configuration management**: Readability benefits outweigh performance cost
- ✅ **Startup/initialization**: One-time loading cost is acceptable
- ✅ **Complex hierarchical configs**: YAML's structure benefits justify overhead
- ✅ **Small to medium configs**: Performance impact manageable (< 1ms)

**When to Avoid YAML:**
- ❌ **Frequent configuration reloading**: 25-237x slower than JSON
- ❌ **Memory-constrained environments**: Significant allocation overhead
- ❌ **Hot paths or frequent access**: Use cached values instead
- ❌ **Large configuration files**: Consider JSON for 200+ keys

**Optimization Recommendations:**
1. **Cache loaded configurations**: Avoid repeated YAML parsing
2. **Use JSON for large configs**: Switch to JSON for enterprise-scale configurations
3. **Pre-load during startup**: Load YAML once during application initialization
4. **Monitor memory usage**: Watch for GC pressure with large YAML files
5. **Consider mixed approaches**: Use YAML for readability, JSON for performance-critical configs

### Configuration Scale Impact

**Simple Configurations (< 50 keys):**
- YAML overhead: ~25x slower but still sub-millisecond
- Memory impact: ~97KB allocations (manageable)
- **Recommendation**: YAML acceptable for development convenience

**Complex Configurations (100+ keys, 4+ levels deep):**
- YAML overhead: ~78x slower (~800 μs)
- Memory impact: ~412KB allocations
- **Recommendation**: Evaluate if readability benefits justify performance cost

**Enterprise Configurations (200+ keys, 6+ levels deep):**
- YAML overhead: ~237x slower (~2.5ms)
- Memory impact: ~1.3MB allocations with Gen1/Gen2 collections
- **Recommendation**: Strongly consider JSON for performance-critical applications

### Benchmark Environment

**Hardware & Runtime:**
- **.NET Runtime**: .NET 9.0
- **Benchmark Tool**: BenchmarkDotNet v0.15.2
- **Job Configuration**: SimpleJob with memory diagnostics
- **Measurements**: Mean execution time with standard deviation
- **Memory Tracking**: Gen0/Gen1/Gen2 collections and total allocations

**Test Configuration Sizes:**
- **Simple**: ~20 configuration keys, 2-3 levels deep
- **Complex**: ~100 configuration keys, 4-5 levels deep
- **Large**: ~200+ configuration keys, 6+ levels deep

*Results may vary on different hardware configurations and runtime versions. Focus on relative performance ratios rather than absolute timings.*

## Troubleshooting

### Common Issues

**Files Not Found:**
- Ensure YAML files exist in `TestData/` directory
- Check that `.csproj` includes `**/*.yaml` copy rules
- Verify working directory is correct

**Unexpected Results:**
- Use Release configuration: `dotnet run -c Release`
- Close unnecessary applications
- Run multiple times for consistency

**Memory Measurements:**
- Memory diagnoser only works in Release mode
- GC collection can affect individual measurements
- Focus on allocation patterns, not absolute numbers

## Contributing

When adding new YAML loading benchmarks:
1. Use realistic YAML configuration data
2. Include memory allocation tracking
3. Compare against JSON baseline when relevant
4. Document expected performance characteristics
5. Test with various YAML complexity levels

Focus on real-world scenarios that developers encounter when choosing between YAML and JSON for configuration files.