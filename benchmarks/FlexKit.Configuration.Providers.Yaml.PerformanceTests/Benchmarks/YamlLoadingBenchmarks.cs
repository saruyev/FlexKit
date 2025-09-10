using BenchmarkDotNet.Attributes;
using FlexKit.Configuration.Core;
using FlexKit.Configuration.Providers.Yaml.Extensions;
using FlexKit.Configuration.Providers.Yaml.Sources;
using Microsoft.Extensions.Configuration;
// ReSharper disable NullableWarningSuppressionIsUsed

namespace FlexKit.Configuration.Providers.Yaml.PerformanceTests.Benchmarks;

/// <summary>
/// Benchmarks for YAML file loading and parsing performance.
/// Tests the overhead of YAML file processing compared to JSON and other configuration sources.
/// </summary>
[MemoryDiagnoser]
[SimpleJob]
public class YamlLoadingBenchmarks
{
    private string _equivalentJsonContent = null!;

    [GlobalSetup]
    public void Setup()
    {
        // Validate that YAML test files exist
        ValidateYamlFile("simple-config.yaml");
        ValidateYamlFile("complex-config.yaml");
        ValidateYamlFile("large-config.yaml");

        // Load equivalent JSON for comparison
        _equivalentJsonContent = LoadJsonFile("equivalent-config.json");
    }

    private static void ValidateYamlFile(string fileName)
    {
        var yamlPath = Path.Combine("TestData", fileName);

        if (!File.Exists(yamlPath))
        {
            throw new FileNotFoundException($"YAML test file not found at: {yamlPath}. " +
                $"Please ensure {fileName} exists in the TestData folder.");
        }
    }

    private string LoadJsonFile(string fileName)
    {
        var jsonPath = Path.Combine("TestData", fileName);

        if (!File.Exists(jsonPath))
        {
            throw new FileNotFoundException($"JSON test file not found at: {jsonPath}. " +
                $"Please ensure {fileName} exists in the TestData folder.");
        }

        return File.ReadAllText(jsonPath);
    }

    // === Simple Configuration Loading Benchmarks ===

    [Benchmark(Baseline = true)]
    public IConfiguration LoadSimpleJsonConfiguration()
    {
        var builder = new ConfigurationBuilder();
        builder.AddJsonStream(new MemoryStream(System.Text.Encoding.UTF8.GetBytes(_equivalentJsonContent)));
        return builder.Build();
    }

    [Benchmark]
    public IConfiguration LoadSimpleYamlConfiguration()
    {
        var builder = new ConfigurationBuilder();
        builder.Add(new YamlConfigurationSource { Path = Path.Combine("TestData", "simple-config.yaml") });
        return builder.Build();
    }

    [Benchmark]
    public FlexConfiguration LoadSimpleYamlFlexConfiguration()
    {
        var builder = new ConfigurationBuilder();
        builder.Add(new YamlConfigurationSource { Path = Path.Combine("TestData", "simple-config.yaml") });
        var standardConfig = builder.Build();
        return new FlexConfiguration(standardConfig);
    }

    // === Complex Configuration Loading Benchmarks ===

    [Benchmark]
    public IConfiguration LoadComplexYamlConfiguration()
    {
        var builder = new ConfigurationBuilder();
        builder.Add(new YamlConfigurationSource { Path = Path.Combine("TestData", "complex-config.yaml") });
        return builder.Build();
    }

    [Benchmark]
    public FlexConfiguration LoadComplexYamlFlexConfiguration()
    {
        var builder = new ConfigurationBuilder();
        builder.Add(new YamlConfigurationSource { Path = Path.Combine("TestData", "complex-config.yaml") });
        var standardConfig = builder.Build();
        return new FlexConfiguration(standardConfig);
    }

    // === Large Configuration Loading Benchmarks ===

    [Benchmark]
    public IConfiguration LoadLargeYamlConfiguration()
    {
        var builder = new ConfigurationBuilder();
        builder.Add(new YamlConfigurationSource { Path = Path.Combine("TestData", "large-config.yaml") });
        return builder.Build();
    }

    [Benchmark]
    public FlexConfiguration LoadLargeYamlFlexConfiguration()
    {
        var builder = new ConfigurationBuilder();
        builder.Add(new YamlConfigurationSource { Path = Path.Combine("TestData", "large-config.yaml") });
        var standardConfig = builder.Build();
        return new FlexConfiguration(standardConfig);
    }

    // === File-based Loading Benchmarks ===

    [Benchmark]
    public IConfiguration LoadYamlFromFile()
    {
        var builder = new ConfigurationBuilder();
        builder.Add(new YamlConfigurationSource { Path = Path.Combine("TestData", "complex-config.yaml") });
        return builder.Build();
    }

    [Benchmark]
    public FlexConfiguration LoadYamlFromFileToFlexConfig()
    {
        var builder = new ConfigurationBuilder();
        builder.Add(new YamlConfigurationSource { Path = Path.Combine("TestData", "complex-config.yaml") });
        var standardConfig = builder.Build();
        return new FlexConfiguration(standardConfig);
    }

    // === FlexConfigurationBuilder Integration Benchmarks ===

    [Benchmark]
    public IFlexConfig LoadYamlWithFlexConfigurationBuilder()
    {
        return new FlexConfigurationBuilder()
            .AddYamlFile(Path.Combine("TestData", "complex-config.yaml"))
            .Build();
    }

    [Benchmark]
    public IFlexConfig LoadMixedSourcesWithFlexConfigurationBuilder()
    {
        return new FlexConfigurationBuilder()
            .AddYamlFile(Path.Combine("TestData", "simple-config.yaml"))
            .AddJsonFile(Path.Combine("TestData", "equivalent-config.json"))
            .Build();
    }
}