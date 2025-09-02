using FlexKit.Logging.Configuration;
using FlexKit.Logging.Core;
using FlexKit.Logging.Interception.Attributes;
using FlexKit.Logging.Models;
using JetBrains.Annotations;
// ReSharper disable TooManyArguments

// ReSharper disable NullableWarningSuppressionIsUsed

namespace FlexKit.Logging.PerformanceTests.Services;

public interface INativeService
{
    string ProcessData(string input);
}

[UsedImplicitly]
public class NativeService : INativeService
{
    public string ProcessData(string input) => $"Native: {input}";
}

public interface IManualService
{
    string ProcessData(string input);
}

[UsedImplicitly]
public class ManualService(IFlexKitLogger logger) : IManualService
{
    public string ProcessData(string input)
    {
        var entry = LogEntry.CreateStart(nameof(ProcessData), GetType().FullName!)
            .WithInput(input);
        logger.Log(entry);
        
        var result = $"Manual: {input}";
        logger.Log(entry.WithCompletion(true).WithOutput(result));
        return result;
    }
}

// Attribute-based services
public interface INoLogService { string ProcessData(string input); }
[UsedImplicitly]
[NoLog]
public class NoLogService : INoLogService
{
    public string ProcessData(string input) => $"NoLog: {input}";
}

public interface ILogInputService { string ProcessData(string input); }
[UsedImplicitly]
[LogInput]
public class LogInputService : ILogInputService
{
    public string ProcessData(string input) => $"LogInput: {input}";
}

public interface ILogBothService 
{ 
    string ProcessData(string input);
    string ProcessComplexData(ComplexTestData data);
}
[LogBoth]
public class LogBothService : ILogBothService
{
    public string ProcessData(string input) => $"LogBoth: {input}";
    public string ProcessComplexData(ComplexTestData data) => $"Complex: {data.Name}";
}

public interface IAutoService { string ProcessData(string input); }
[UsedImplicitly]
public class AutoService : IAutoService
{
    public string ProcessData(string input) => $"Auto: {input}";
}

// Test data classes
[UsedImplicitly]
public record ComplexTestData(string Name, int Value)
{
    [UsedImplicitly]
    public Dictionary<string, object> Properties { get; init; } = new();
    
    [UsedImplicitly]
    public string[] Items { get; init; } = [];
}

public interface IAsyncLogBothService
{
    Task<string> ProcessDataAsync(string input);
    
    [UsedImplicitly]
    Task ProcessVoidAsync(string input);
}

[UsedImplicitly]
[LogBoth]
public class AsyncLogBothService : IAsyncLogBothService
{
    public async Task<string> ProcessDataAsync(string input)
    {
        await Task.Delay(1); // Simulate async work
        return $"Async LogBoth: {input}";
    }

    public async Task ProcessVoidAsync(string input)
    {
        await Task.Delay(1);
        // Void async method
    }
}

public interface IAsyncManualService
{
    Task<string> ProcessDataAsync(string input);
}

[UsedImplicitly]
public class AsyncManualService(IFlexKitLogger logger) : IAsyncManualService
{
    public async Task<string> ProcessDataAsync(string input)
    {
        var entry = LogEntry.CreateStart(nameof(ProcessDataAsync), GetType().FullName!)
            .WithInput(input);
        logger.Log(entry);
        
        await Task.Delay(1);
        var result = $"Manual Async: {input}";
        
        logger.Log(entry.WithCompletion(true).WithOutput(result));
        return result;
    }
}

// For ConfigurationOverheadBenchmarks - these would match patterns in appsettings.json
public interface IExactMatchService { string ProcessData(string input); }

[UsedImplicitly]
public class ExactMatchService : IExactMatchService
{
    public string ProcessData(string input) => $"ExactMatch: {input}";
}

public interface IWildcardMatchService { string ProcessData(string input); }

[UsedImplicitly]
public class WildcardMatchService : IWildcardMatchService
{
    public string ProcessData(string input) => $"WildcardMatch: {input}";
}

public interface IAttributeOverrideService { string ProcessData(string input); }

[UsedImplicitly]
[LogInput] // Overrides configuration
public class AttributeOverrideService : IAttributeOverrideService
{
    public string ProcessData(string input) => $"AttributeOverride: {input}";
}

public interface INoConfigService { string ProcessData(string input); }

[UsedImplicitly]
public class NoConfigService : INoConfigService
{
    public string ProcessData(string input) => $"NoConfig: {input}";
}

// Additional service interfaces for exclusion patterns (reuse implementations from v5)
public interface IExactExclusionService 
{ 
    string ProcessData(string input);
    string ToString();
}

public interface IPrefixExclusionService 
{ 
    string GetUserName(int id);
}

public interface ISuffixExclusionService 
{ 
    string ProcessInternal(string data);
}

public interface IMixedExclusionService 
{ 
    string ProcessMainData(string data);
}

public interface INoExclusionService 
{ 
    string ProcessData(string input);
}

[UsedImplicitly]
// For ComplexParameterBenchmarks
public record SimpleData(string Name, int Value);

public record VeryComplexData
{
    public Guid Id { get; init; }
    
    [UsedImplicitly]
    public Dictionary<string, object> Data { get; init; } = new();
    
    [UsedImplicitly]
    public List<ComplexTestData> NestedObjects { get; init; } = new();
    
    [UsedImplicitly]
    public int[] LargeArray { get; init; } = [];
}

// Extended ILogBothService for complex parameter testing
public interface IExtendedLogBothService : ILogBothService
{
    string ProcessSimpleData(SimpleData data);
    string ProcessVeryComplexData(VeryComplexData data);
    string ProcessMultipleParameters(string str, int num, SimpleData data, int[] array);
}

[LogBoth]
public class ExtendedLogBothService : LogBothService, IExtendedLogBothService
{
    public string ProcessSimpleData(SimpleData data) => $"Simple: {data.Name}";
    public string ProcessVeryComplexData(VeryComplexData data) => $"VeryComplex: {data.Id}";
    public string ProcessMultipleParameters(string str, int num, SimpleData data, int[] array) => 
        $"Multiple: {str}, {num}, {data.Name}, [{string.Join(",", array)}]";
}

// Services configured with specific formatters via appsettings.json
public interface IStandardFormatterService
{
    string ProcessComplexData(ComplexTestData data);
}

[LogBoth]
public class StandardFormatterService : IStandardFormatterService
{
    public string ProcessComplexData(ComplexTestData data) => $"Standard: {data.Name}";
}

public interface IJsonFormatterService
{
    string ProcessComplexData(ComplexTestData data);
}

[LogBoth]
public class JsonFormatterService : IJsonFormatterService
{
    public string ProcessComplexData(ComplexTestData data) => $"Json: {data.Name}";
}

public interface IHybridFormatterService
{
    string ProcessComplexData(ComplexTestData data);
}

[LogBoth]
public class HybridFormatterService : IHybridFormatterService
{
    public string ProcessComplexData(ComplexTestData data) => $"Hybrid: {data.Name}";
}


// Service that uses manual logging with LogEntry formatter specification
public interface IFormatterManualService
{
    string ProcessWithFormatter(string formatterName, ComplexTestData data);
}

public class FormatterManualService : IFormatterManualService
{
    private readonly IFlexKitLogger _logger;

    public FormatterManualService(IFlexKitLogger logger) => _logger = logger;

    public string ProcessWithFormatter(string formatterName, ComplexTestData data)
    {
        var formatterType = Enum.Parse<FormatterType>(formatterName);
        
        var startEntry = LogEntry.CreateStart(nameof(ProcessWithFormatter), GetType().FullName!)
            .WithInput(data)
            .WithFormatter(formatterType)
            .WithTarget("Console");

        _logger.Log(startEntry);

        var result = $"Manual {formatterName}: {data.Name}";

        var endEntry = startEntry
            .WithCompletion(true)
            .WithOutput(result);

        _logger.Log(endEntry);
        return result;
    }
}

// Service that uses attribute-level formatter specification
public interface IFormatterAttributeService
{
    string ProcessWithFormatter(string formatterName, ComplexTestData data);
}

[UsedImplicitly]
public class FormatterAttributeService : IFormatterAttributeService
{
    [UsedImplicitly]
    [LogBoth(formatter: "Json")]
    public string ProcessWithJson(ComplexTestData data) => $"Json: {data.Name}";

    [UsedImplicitly]
    [LogBoth(formatter: "Hybrid")]
    public string ProcessWithHybrid(ComplexTestData data) => $"Hybrid: {data.Name}";

    [UsedImplicitly]
    [LogBoth(formatter: "CustomTemplate")]
    public string ProcessWithCustomTemplate(ComplexTestData data) => $"CustomTemplate: {data.Name}";

    [UsedImplicitly]
    [LogBoth(formatter: "StandardStructured")]
    public string ProcessWithStandardStructured(ComplexTestData data) => $"StandardStructured: {data.Name}";

    [UsedImplicitly]
    [LogBoth(formatter: "SuccessError")]
    public string ProcessWithSuccessError(ComplexTestData data) => $"SuccessError: {data.Name}";

    public string ProcessWithFormatter(string formatterName, ComplexTestData data)
    {
        return formatterName switch
        {
            "Json" => ProcessWithJson(data),
            "Hybrid" => ProcessWithHybrid(data),
            "CustomTemplate" => ProcessWithCustomTemplate(data),
            "StandardStructured" => ProcessWithStandardStructured(data),
            "SuccessError" => ProcessWithSuccessError(data),
            _ => throw new ArgumentException($"Unknown formatter: {formatterName}")
        };
    }
}

// Add these to FlexKit.Logging.PerformanceTests.Core

[UsedImplicitly]
public class ExactExclusionService : IExactExclusionService
{
    public string ProcessData(string input) => $"Processed: {input}";
    public override string ToString() => "ExactExclusionService";
}

[UsedImplicitly]
public class PrefixExclusionService : IPrefixExclusionService
{
    public string GetUserName(int id) => $"User_{id}";
}

[UsedImplicitly]
public class SuffixExclusionService : ISuffixExclusionService
{
    public string ProcessInternal(string data) => $"Internal: {data}";
}

[UsedImplicitly]
public class MixedExclusionService : IMixedExclusionService
{
    public string ProcessMainData(string data) => $"Main: {data}";
}

[UsedImplicitly]
public class NoExclusionService : INoExclusionService
{
    public string ProcessData(string input) => $"NoExclusion: {input}";
}