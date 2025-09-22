using System.Diagnostics;
using System.Reflection;
using System.Reflection.Emit;
using FlexKit.Logging.Configuration;
using FlexKit.Logging.NLog.Detection;
using FluentAssertions;
using HarmonyLib;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Primitives;
using NLog;
using NLog.Filters;
using NLog.Layouts;
using NLog.Targets;
using NLog.Targets.Wrappers;
using Xunit;
// ReSharper disable InconsistentNaming
// ReSharper disable MethodTooLong
// ReSharper disable TooManyArguments
// ReSharper disable TooManyDeclarations
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable FlagArgument

namespace FlexKit.Logging.NLog.Tests.Detection;

[Collection("NLogConfigurationBuilderTests")]
public class NLogConfigurationBuilderTests
{
    [Fact]
    public void BuildConfiguration_WhenTargetsConfiguredNotZero_ReturnsConfigurationWithTargets()
    {
        // Arrange
        var mockTargetInfo = new NLogTargetDetector.TargetInfo
        {
            Name = "Console",
            TargetType = typeof(ConsoleTarget),
            Properties = [],
            SupportsAsync = false
        };

        var target = new LoggingTarget
        {
            Type = "Console",
            Enabled = true,
            Properties = new Dictionary<string, IConfigurationSection?>()
        };

        var config = new LoggingConfig
        {
            Targets = new Dictionary<string, LoggingTarget> { ["Console"] = target }
        };

        var builder = new NLogConfigurationBuilder();
        var availableTargetsField = typeof(NLogConfigurationBuilder)
            .GetField("_availableTargets", BindingFlags.NonPublic | BindingFlags.Instance);

        var mockTargets = new Dictionary<string, NLogTargetDetector.TargetInfo>(StringComparer.OrdinalIgnoreCase)
        {
            ["Console"] = mockTargetInfo
        };

        availableTargetsField?.SetValue(builder, mockTargets);

        // Act
        var result = builder.BuildConfiguration(config);

        // Assert
        result.Should().NotBeNull();
        result.AllTargets.Should().HaveCount(1);
        result.AllTargets.First().Should().BeOfType<ConsoleTarget>();
        result.LoggingRules.Should().NotBeEmpty();
    }

    [Fact]
    public void BuildConfiguration_WhenAvailableTargetsTryGetValueIsFalse_ReturnsFallbackConsoleConfiguration()
    {
        // Arrange
        var target = new LoggingTarget
        {
            Type = "NonExistentTarget",
            Enabled = true,
            Properties = new Dictionary<string, IConfigurationSection?>()
        };

        var config = new LoggingConfig
        {
            Targets = new Dictionary<string, LoggingTarget> { ["NonExistentTarget"] = target }
        };

        var builder = new NLogConfigurationBuilder();

        // Act
        var result = builder.BuildConfiguration(config);

        // Assert
        result.Should().NotBeNull();
        result.AllTargets.Should().HaveCount(1);
        result.AllTargets.First().Should().BeOfType<ConsoleTarget>();
        result.AllTargets.First().Name.Should().Be("console");
        result.LoggingRules.Should().HaveCount(1);
    }

    [Fact]
    public void BuildConfiguration_WhenCreateTargetInstanceReturnsNull_ReturnsFallbackConsoleConfiguration()
    {
        // Arrange - Use an abstract target type that CreateTargetInstance will fail to create
        var mockTargetInfo = new NLogTargetDetector.TargetInfo
        {
            Name = "AbstractTarget",
            TargetType = typeof(TestAbstractTarget), // Abstract type - CreateInstance will return null
            Properties = [],
            SupportsAsync = false
        };

        var target = new LoggingTarget
        {
            Type = "AbstractTarget",
            Enabled = true,
            Properties = new Dictionary<string, IConfigurationSection?>()
        };

        var config = new LoggingConfig
        {
            Targets = new Dictionary<string, LoggingTarget> { ["AbstractTarget"] = target }
        };

        var builder = new NLogConfigurationBuilder();
        var availableTargetsField = typeof(NLogConfigurationBuilder)
            .GetField("_availableTargets", BindingFlags.NonPublic | BindingFlags.Instance);

        var mockTargets = new Dictionary<string, NLogTargetDetector.TargetInfo>(StringComparer.OrdinalIgnoreCase)
        {
            ["AbstractTarget"] = mockTargetInfo
        };

        availableTargetsField?.SetValue(builder, mockTargets);

        // Act
        var result = builder.BuildConfiguration(config);

        // Assert
        result.Should().NotBeNull();
        // Since CreateTargetInstance returns null, no targets are configured, fallback console is used
        result.AllTargets.Should().HaveCount(1);
        result.AllTargets.First().Should().BeOfType<ConsoleTarget>();
        result.AllTargets.First().Name.Should().Be("console");
    }

    [Fact]
    public void BuildConfiguration_WhenShouldWrapWithAsyncReturnsFalse_TargetIsAsyncTargetWrapper_DoesNotWrap()
    {
        // Arrange - Create a target that's already an AsyncTargetWrapper

        var mockTargetInfo = new NLogTargetDetector.TargetInfo
        {
            Name = "AsyncWrapper",
            TargetType = typeof(TestAsyncWrapperTarget), // This will create an AsyncTargetWrapper
            Properties = [],
            SupportsAsync = true
        };

        var target = new LoggingTarget
        {
            Type = "AsyncWrapper",
            Enabled = true,
            Properties = new Dictionary<string, IConfigurationSection?>()
        };

        var config = new LoggingConfig
        {
            Targets = new Dictionary<string, LoggingTarget> { ["AsyncWrapper"] = target }
        };

        var builder = new NLogConfigurationBuilder();
        var availableTargetsField = typeof(NLogConfigurationBuilder)
            .GetField("_availableTargets", BindingFlags.NonPublic | BindingFlags.Instance);

        var mockTargets = new Dictionary<string, NLogTargetDetector.TargetInfo>(StringComparer.OrdinalIgnoreCase)
        {
            ["AsyncWrapper"] = mockTargetInfo
        };

        availableTargetsField?.SetValue(builder, mockTargets);

        // Act
        var result = builder.BuildConfiguration(config);

        // Assert
        result.Should().NotBeNull();
        result.AllTargets.Should().HaveCount(1);
        var configuredTarget = result.AllTargets.First();
        configuredTarget.Should().BeOfType<TestAsyncWrapperTarget>();
        configuredTarget.Name.Should().Be("AsyncWrapper");
        // Verify it wasn't double-wrapped
        configuredTarget.Should().NotBeOfType<AsyncTargetWrapper>();
    }

    [Fact]
    public void BuildConfiguration_WhenExceptionThrownInTryConfigureTarget_CatchesCreateTargetExceptionAndContinuesWithFallback()
    {
        // Arrange - Create a target type that will cause an exception during Activator.CreateInstance
        var mockTargetInfo = new NLogTargetDetector.TargetInfo
        {
            Name = "ThrowingTarget",
            TargetType = typeof(TestTargetWithThrowingConstructor), 
            Properties = [],
            SupportsAsync = false
        };

        var target = new LoggingTarget
        {
            Type = "ThrowingTarget",
            Enabled = true,
            Properties = new Dictionary<string, IConfigurationSection?>()
        };

        var config = new LoggingConfig
        {
            Targets = new Dictionary<string, LoggingTarget> { ["ThrowingTarget"] = target }
        };

        var builder = new NLogConfigurationBuilder();
        var availableTargetsField = typeof(NLogConfigurationBuilder)
            .GetField("_availableTargets", BindingFlags.NonPublic | BindingFlags.Instance);

        var mockTargets = new Dictionary<string, NLogTargetDetector.TargetInfo>(StringComparer.OrdinalIgnoreCase)
        {
            ["ThrowingTarget"] = mockTargetInfo
        };

        availableTargetsField?.SetValue(builder, mockTargets);

        // Act
        var result = builder.BuildConfiguration(config);

        // Assert
        result.Should().NotBeNull();
        // Exception in TryConfigureTarget should be caught, resulting in fallback console
        result.AllTargets.Should().HaveCount(1);
        result.AllTargets.First().Should().BeOfType<ConsoleTarget>();
        result.AllTargets.First().Name.Should().Be("console");
    }

    [Fact]
    public void BuildConfiguration_WhenTargetSupportsAsyncAndShouldWrapWithAsync_WrapsWithAsyncTargetWrapper()
    {
        // Arrange
        var mockTargetInfo = new NLogTargetDetector.TargetInfo
        {
            Name = "TestAsync",
            TargetType = typeof(ConsoleTarget), // A target that supports async but isn't self-async
            SupportsAsync = true,
            Properties = []
        };

        var target = new LoggingTarget
        {
            Type = "TestAsync",
            Enabled = true,
            Properties = new Dictionary<string, IConfigurationSection?>()
        };

        var config = new LoggingConfig
        {
            Targets = new Dictionary<string, LoggingTarget> { ["TestAsync"] = target }
        };

        var builder = new NLogConfigurationBuilder();
        var availableTargetsField = typeof(NLogConfigurationBuilder)
            .GetField("_availableTargets", BindingFlags.NonPublic | BindingFlags.Instance);

        var mockTargets = new Dictionary<string, NLogTargetDetector.TargetInfo>(StringComparer.OrdinalIgnoreCase)
        {
            ["TestAsync"] = mockTargetInfo
        };

        availableTargetsField?.SetValue(builder, mockTargets);

        // Act
        var result = builder.BuildConfiguration(config);

        // Assert
        result.Should().NotBeNull();
        result.AllTargets.Should().HaveCount(1);
        result.AllTargets.First().Should().BeOfType<AsyncTargetWrapper>();

        var asyncWrapper = (AsyncTargetWrapper)result.AllTargets.First();
        asyncWrapper.WrappedTarget.Should().BeOfType<ConsoleTarget>();
        asyncWrapper.Name.Should().Be("TestAsync_Async");
    }

    [Fact]
    public void BuildConfiguration_WhenTargetIsNotAsyncTargetWrapperAndSelfAsyncTargetsContainsTargetType_DoesNotWrap()
    {
        // Arrange - Use a NetworkTarget which is in selfAsyncTargets array
        var mockTargetInfo = new NLogTargetDetector.TargetInfo
        {
            Name = "TestNetwork",
            TargetType = typeof(TestNetworkTarget), // This target type contains "NetworkTarget" so should not be wrapped
            SupportsAsync = true,
            Properties = []
        };

        var target = new LoggingTarget
        {
            Type = "TestNetwork",
            Enabled = true,
            Properties = new Dictionary<string, IConfigurationSection?>()
        };

        var config = new LoggingConfig
        {
            Targets = new Dictionary<string, LoggingTarget> { ["TestNetwork"] = target }
        };

        var builder = new NLogConfigurationBuilder();
        var availableTargetsField = typeof(NLogConfigurationBuilder)
            .GetField("_availableTargets", BindingFlags.NonPublic | BindingFlags.Instance);

        var mockTargets = new Dictionary<string, NLogTargetDetector.TargetInfo>(StringComparer.OrdinalIgnoreCase)
        {
            ["TestNetwork"] = mockTargetInfo
        };

        availableTargetsField?.SetValue(builder, mockTargets);

        // Act
        var result = builder.BuildConfiguration(config);

        // Assert
        result.Should().NotBeNull();
        result.AllTargets.Should().HaveCount(1);
        var configuredTarget = result.AllTargets.First();
        configuredTarget.Should().BeOfType<TestNetworkTarget>();
        configuredTarget.Should()
            .NotBeOfType<AsyncTargetWrapper>("NetworkTarget should not be wrapped as it handles async internally");

        configuredTarget.Name.Should().Be("TestNetwork");
    }

    [Fact]
    public void BuildConfiguration_WhenTargetThrowsAfterCreation_ShouldCatchAndReturnConfig()
    {
        var harmony = new Harmony("test.patch");
        var debugOutput = new StringWriter();
        var debugListener = new TextWriterTraceListener(debugOutput);
        Trace.Listeners.Add(debugListener);
        harmony.Patch(
            original: typeof(NLogConfigurationBuilder)
                .GetMethod("CreateTargetInstance", BindingFlags.NonPublic | BindingFlags.Static),
            prefix: new HarmonyMethod(typeof(NLogConfigurationBuilderTests), nameof(ReturnBadTarget))
        );

        var builder = new NLogConfigurationBuilder();

        // Inject fake TargetInfo via reflection
        var field = typeof(NLogConfigurationBuilder)
            .GetField("_availableTargets", BindingFlags.NonPublic | BindingFlags.Instance)!;
        var availableTargets = (Dictionary<string, NLogTargetDetector.TargetInfo>)field.GetValue(builder)!;

        var targetInfoType = availableTargets.Values.First().GetType();
        var targetInfo = (NLogTargetDetector.TargetInfo)Activator.CreateInstance(targetInfoType)!;
        targetInfoType.GetProperty("Name")!.SetValue(targetInfo, "Evil");
        targetInfoType.GetProperty("TargetType")!.SetValue(targetInfo, typeof(Target));
        targetInfoType.GetProperty("Properties")!.SetValue(targetInfo, Array.Empty<PropertyInfo>());
        targetInfoType.GetProperty("SupportsAsync")!.SetValue(targetInfo, false);
        availableTargets["Evil"] = targetInfo;

        var config = new LoggingConfig
        {
            Targets =
            {
                ["Evil"] = new LoggingTarget
                {
                    Type = "Evil",
                    Enabled = true,
                    Properties = new Dictionary<string, IConfigurationSection?>()
                },
            },
        };

        // Act
        var nlogConfig = builder.BuildConfiguration(config);
        Trace.Listeners.Remove(debugListener);
        harmony.UnpatchAll("test.patch");
        // Assert
        debugOutput.ToString().Should().Contain("Warning: Failed to configure NLog target Evil: target.Name cannot be empty (Parameter 'target')");
        nlogConfig.AllTargets.Should().HaveCount(1);
        nlogConfig.AllTargets.First().Should().BeOfType<ConsoleTarget>();
    }
    
    [Fact]
    public void BuildConfiguration_WhenActivatorCreatesNullableType_TargetIsNullBranchIsTaken()
    {
        // Arrange
        var builder = new NLogConfigurationBuilder();

        // Access private _availableTargets and create a fake TargetInfo entry
        var field = typeof(NLogConfigurationBuilder)
            .GetField("_availableTargets", BindingFlags.NonPublic | BindingFlags.Instance)
            ?? throw new InvalidOperationException("_availableTargets field not found.");

        var availableTargets = (Dictionary<string, NLogTargetDetector.TargetInfo>)field.GetValue(builder)!;

        // Use existing TargetInfo runtime type as template for creating a new instance
        var template = availableTargets.Values.First();
        var targetInfoType = template.GetType();

        // Create a new TargetInfo instance via Activator and set required init-only properties by reflection
        var newTargetInfo = (NLogTargetDetector.TargetInfo)Activator.CreateInstance(targetInfoType)!;
        targetInfoType.GetProperty("Name")!.SetValue(newTargetInfo, "Nullish");
        // Set TargetType to a Nullable<int> - Activator.CreateInstance(typeof(int?)) returns null (boxed)
        targetInfoType.GetProperty("TargetType")!.SetValue(newTargetInfo, typeof(int?)); 
        targetInfoType.GetProperty("Properties")!.SetValue(newTargetInfo, Array.Empty<PropertyInfo>());
        targetInfoType.GetProperty("SupportsAsync")!.SetValue(newTargetInfo, false);

        // Inject into the detected targets dictionary
        availableTargets["Nullish"] = newTargetInfo;

        // Prepare FlexKit LoggingConfig with an enabled target that matches our injected entry
        var flexConfig = new LoggingConfig
        {
            Targets =
            {
                ["Nullish"] = new LoggingTarget
                {
                    Type = "Nullish",
                    Enabled = true,
                    Properties = new Dictionary<string, IConfigurationSection?>()
                },
            },
        };

        // Act
        var nlogConfig = builder.BuildConfiguration(flexConfig);

        // Assert
        // Our "Nullish" target should not be present (CreateTargetInstance returned null -> target skipped)
        nlogConfig.AllTargets.Should().NotContain(t => t.Name == "Nullish");

        // Fallback console target must be present (only fallback)
        nlogConfig.AllTargets.Should().ContainSingle()
            .Which.Should().BeOfType<ConsoleTarget>()
            .Which.Name.Should().Be("console");
    }
    
    [Theory]
    [MemberData(nameof(ConvertConfigurationValueCases))]
    public void BuildConfiguration_ShouldConvertConfigurationValues(
        string propName, Type propType, object? inputValue, object? expected)
    {
        // Arrange: create a dynamic target type with the required property
        var targetType = DynamicTargetFactory.CreateTargetTypeWithProperty(propName, propType);

        var targetInfo = new NLogTargetDetector.TargetInfo
        {
            Name = "TestTarget",
            TargetType = targetType,
            Properties = [targetType.GetProperty(propName)!],
            SupportsAsync = false
        };

        var builder = new NLogConfigurationBuilder();

        // Inject fake target info
        var field = typeof(NLogConfigurationBuilder)
            .GetField("_availableTargets", BindingFlags.NonPublic | BindingFlags.Instance)!;
        var availableTargets = (Dictionary<string, NLogTargetDetector.TargetInfo>)field.GetValue(builder)!;
        availableTargets["TestTarget"] = targetInfo;

        var flexConfig = new LoggingConfig
        {
            Targets =
            {
                ["TestTarget"] = new LoggingTarget
                {
                    Type = "TestTarget",
                    Enabled = true,
                    Properties = new Dictionary<string, IConfigurationSection?>()
                },
            },
        };

        if (propName == "Complex")
        {
            var section = new ConfigurationBuilder()
                .AddInMemoryCollection((inputValue as Dictionary<string, string>)!)
                .Build()
                .GetSection("Complex");

            flexConfig.Targets["TestTarget"].Properties[propName] = section;
        }
        else if (inputValue != null)
        {
            var section = new InMemoryConfigurationSection(inputValue.ToString()!);
            flexConfig.Targets["TestTarget"].Properties[propName] = section;
        }
        else
        {
            flexConfig.Targets["TestTarget"].Properties[propName] = null;
        }

        // Act
        var nlogConfig = builder.BuildConfiguration(flexConfig);

        // Assert
        var target = nlogConfig.AllTargets.First(t => t.Name == "TestTarget");
        var actualValue = targetType.GetProperty(propName)!.GetValue(target);
        actualValue.Should().BeEquivalentTo(expected);
    }
    
    [Fact]
    public void ConfigureTargetProperties_ShouldSkipProperty_WhenSetterThrows()
    {
        // Arrange
        var flexKitTarget = new LoggingTarget
        {
            Type = "Throwing",
            Enabled = true,
            Properties = new Dictionary<string, IConfigurationSection?>
            {
                {
                    "FailingProperty",
                    new ConfigurationBuilder()
                        .AddInMemoryCollection(new Dictionary<string,string?> { { "FailingProperty", "value" } })
                        .Build()
                        .GetSection("FailingProperty")
                }
            }
        };

        var targetInfo = new NLogTargetDetector.TargetInfo
        {
            TargetType = typeof(ThrowingTarget),
            Properties = [typeof(ThrowingTarget).GetProperty(nameof(ThrowingTarget.FailingProperty))!]
        };

        var builder = new NLogConfigurationBuilder();
        var availableTargets = typeof(NLogConfigurationBuilder)
            .GetField("_availableTargets", BindingFlags.NonPublic | BindingFlags.Instance)!
            .GetValue(builder) as Dictionary<string, NLogTargetDetector.TargetInfo>;
        availableTargets!["Throwing"] = targetInfo;

        var config = new LoggingConfig
        {
            Targets = { ["Throwing"] = flexKitTarget }
        };

        // Act / Assert: should not throw
        Action act = () => builder.BuildConfiguration(config);
        act.Should().NotThrow();
    }
    
    [Theory]
    [InlineData("TRACE", "Trace")]
    [InlineData("trace", "Trace")]
    [InlineData("DEBUG", "Debug")]
    [InlineData("debug", "Debug")]
    [InlineData("INFORMATION", "Info")]
    [InlineData("information", "Info")]
    [InlineData("WARNING", "Warn")]
    [InlineData("warning", "Warn")]
    [InlineData("ERROR", "Error")]
    [InlineData("error", "Error")]
    [InlineData("CRITICAL", "Fatal")]
    [InlineData("critical", "Fatal")]
    [InlineData(null, "Info")]      // null value → default
    [InlineData("", "Info")]        // empty string → default
    [InlineData("unknown", "Off")]  // any other string → default switch `_`
    public void BuildConfiguration_ShouldApplyExpectedLogLevel(string? input, string expected)
    {
        // Arrange
        var flexKitTarget = new LoggingTarget
        {
            Type = "Memory",
            Enabled = true,
            Properties = new Dictionary<string, IConfigurationSection?>
            {
                {
                    "LogLevel",
                    new ConfigurationBuilder()
                        .AddInMemoryCollection(new Dictionary<string,string?>
                        {
                            { "LogLevel", input }
                        })
                        .Build()
                        .GetSection("LogLevel")
                }
            }
        };

        var config = new LoggingConfig
        {
            Targets = { ["TestTarget"] = flexKitTarget }
        };

        var builder = new NLogConfigurationBuilder();
        var expectedLevel = LogLevel.FromString(expected);

        // Act
        var nlogConfig = builder.BuildConfiguration(config);

        // Assert: find the rule created for this target
        var rule = nlogConfig.LoggingRules
            // ReSharper disable once RedundantEnumerableCastCall
            .First(r => r.Targets.OfType<Target>().Any(t => t.Name == "Memory_Async"));

        if (expectedLevel == LogLevel.Off)
            rule.Levels.Should().BeEmpty();
        else
            rule.Levels.Should().Contain(expectedLevel);
    }
    
    [Theory]
    [InlineData(false, true, "Log")]     // no Target property -> Log
    [InlineData(true, false, "Ignore")]  // Target property present -> Ignore
    [InlineData(false, true, "Neutral")]     // no Layout property -> Log
    public void BuildConfiguration_WhenNotExistsFilter_BehavesAsExpected(
        bool includeTargetProperty,
        bool expectLog,
        string caseName)
    {
        // Arrange
        var dict = new Dictionary<string, string?> { ["LogLevel"] = "Info" };
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(dict).Build();

        var loggingTarget = new LoggingTarget
        {
            Type = "Memory",
            Enabled = true,
            Properties = { ["LogLevel"] = configuration.GetSection("LogLevel") }
        };

        var loggingConfig = new LoggingConfig
        {
            Targets = { ["Memory"] = loggingTarget }
        };

        var builder = new NLogConfigurationBuilder();
        var nlogConfig = builder.BuildConfiguration(loggingConfig);

        // Ensure rules include Info level
        foreach (var rule in nlogConfig.LoggingRules)
        {
            rule.EnableLoggingForLevel(LogLevel.Info);
        }
        
        if (caseName == "Neutral")
        {
            var asyncTarget = nlogConfig.AllTargets
                .OfType<AsyncTargetWrapper>()
                .First(t => t.Name == "Memory_Async");
            var melRule = nlogConfig.LoggingRules
                .First(r => r.Targets.Contains(asyncTarget));

            var filters = melRule.Filters;
            filters.Clear();

// create a fresh filter instance with Layout = null via reflection
            var filterType = typeof(NLogConfigurationBuilder)
                .GetNestedType("WhenNotExistsFilter", BindingFlags.NonPublic);
            var newFilter = Activator.CreateInstance(filterType!)!;
            filters.Add((Filter)newFilter);
        }

        var factory = new LogFactory { Configuration = nlogConfig };
        var logger = factory.GetLogger("test");

        var memoryTarget = nlogConfig.AllTargets
            .OfType<AsyncTargetWrapper>()
            .First(t => t.Name == "Memory_Async")
            .WrappedTarget as MemoryTarget;

        memoryTarget.Should().NotBeNull();

        // Act
        var logEvent = new LogEventInfo(LogLevel.Info, logger.Name, "message: " + caseName);
        if (includeTargetProperty)
        {
            logEvent.Properties["Target"] = "OtherTarget";
        }
        logger.Log(logEvent);

        // Flush async wrapper
        factory.Flush();

        // Assert
        if (expectLog)
        {
            memoryTarget.Logs.Should()
                .ContainSingle(m => m.Contains("message"), "case: " + caseName);
        }
        else
        {
            memoryTarget.Logs.Should().BeEmpty("case: " + caseName);
        }
    }

   
    public static IEnumerable<object[]> ConvertConfigurationValueCases()
    {
        yield return ["StringProp", typeof(string), "hello", "hello"];
        yield return ["IntProp", typeof(int), "42", 42];
        yield return ["BoolProp", typeof(bool), "true", true];
        yield return ["EnumProp", typeof(DayOfWeek), "Friday", DayOfWeek.Friday];
        yield return ["TimeSpanProp", typeof(TimeSpan), "00:00:10", TimeSpan.FromSeconds(10)];
        yield return ["NullableIntProp", typeof(int?), null!, null!];
        yield return ["FallbackProp", typeof(object), "xyz", "xyz"];
        yield return ["LayoutProp", typeof(Layout), "${message}", Layout.FromString("${message}")];
        yield return ["BadTimeSpanProp", typeof(TimeSpan), "boom", TimeSpan.Zero];
        yield return ["Complex", typeof(ComplexType), new Dictionary<string, string?>
        {
            { "Complex:Name", "Alice" },
            { "Complex:Age", "42" }
        }, new ComplexType { Name = "Alice", Age = 42 }];
    }

    // Harmony prefix to replace CreateTargetInstance
    public static bool ReturnBadTarget(ref Target __result)
    {
        // Return a fake Target that will throw later when used
        __result = new EvilTarget();
        return false; // skip original
    }
}

public class ThrowingTarget : Target
{
    public string FailingProperty
    {
        get => throw new NotImplementedException();
        set => throw new InvalidOperationException("Setter fails!");
    }
}

public class ComplexType
{
    public string Name { get; set; } = "";
    public int Age { get; set; }
}

internal static class DynamicTargetFactory
{
    private static readonly AssemblyBuilder AsmBuilder =
        AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("DynamicTargets"), AssemblyBuilderAccess.Run);
    private static readonly ModuleBuilder ModuleBuilder = AsmBuilder.DefineDynamicModule("Main");

    public static Type CreateTargetTypeWithProperty(string propName, Type propType)
    {
        var typeBuilder = ModuleBuilder.DefineType(
            "Dynamic" + propName + "Target",
            TypeAttributes.Public | TypeAttributes.Class,
            typeof(Target));

        var field = typeBuilder.DefineField("_" + propName, propType, FieldAttributes.Private);

        var prop = typeBuilder.DefineProperty(propName, PropertyAttributes.HasDefault, propType, null);

        // Getter
        var getter = typeBuilder.DefineMethod("get_" + propName,
            MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig,
            propType, Type.EmptyTypes);
        var il = getter.GetILGenerator();
        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Ldfld, field);
        il.Emit(OpCodes.Ret);

        // Setter
        var setter = typeBuilder.DefineMethod("set_" + propName,
            MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig,
            null,
            [propType]);
        il = setter.GetILGenerator();
        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Ldarg_1);
        il.Emit(OpCodes.Stfld, field);
        il.Emit(OpCodes.Ret);

        prop.SetGetMethod(getter);
        prop.SetSetMethod(setter);

        return typeBuilder.CreateType();
    }
}

/// <summary>
/// Simple IConfigurationSection wrapper for a single value.
/// </summary>
internal class InMemoryConfigurationSection(string value) : IConfigurationSection
{
    public string? this[string key] { get => null; set { } }
    public string Key => "";
    public string Path => "";
    public string? Value { get; set; } = value;
    public IEnumerable<IConfigurationSection> GetChildren() => [];
    public IChangeToken GetReloadToken() => NullChangeToken.Singleton;
    public IConfigurationSection GetSection(string key) => this;
}

public class EvilTarget : Target
{
    protected override void Write(LogEventInfo logEvent)
    {
        throw new InvalidOperationException("boom");
    }
}

public abstract class TestAbstractTarget : Target
{
    protected override void Write(LogEventInfo logEvent)
    {
        // Abstract implementation
    }
}

public class TestAsyncWrapperTarget : AsyncTargetWrapper
{
    public TestAsyncWrapperTarget() : base(new ConsoleTarget())
    {
        Name = "AsyncWrapper";
    }
}

public class TestNetworkTarget : Target
{
    protected override void Write(LogEventInfo logEvent)
    {
        // Test implementation
    }
}

public class TestTargetWithThrowingConstructor : Target
{
    public TestTargetWithThrowingConstructor()
    {
        throw new InvalidOperationException("Constructor exception for testing");
    }

    protected override void Write(LogEventInfo logEvent)
    {
        // Test implementation
    }
}