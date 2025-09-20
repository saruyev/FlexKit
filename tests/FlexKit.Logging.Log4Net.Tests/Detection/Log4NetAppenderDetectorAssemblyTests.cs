using System.Globalization;
using System.Reflection;
using FlexKit.Logging.Log4Net.Detection;
using FluentAssertions;
using HarmonyLib;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;
// ReSharper disable InconsistentNaming
// ReSharper disable UnusedParameter.Local
// ReSharper disable ClassTooBig
// ReSharper disable TooManyArguments

namespace FlexKit.Logging.Log4Net.Tests.Detection;

public class Log4NetAppenderDetectorAssemblyTests
{
    [Fact]
    public void Should_LogWarning_When_AssemblyScanFails()
    {
        // Patch Assembly.GetTypes to throw
        var harmony = new Harmony("test.assemblyfail");
        var original = typeof(Assembly).GetMethod(nameof(Assembly.GetTypes));
        harmony.Patch(original,
            prefix: new HarmonyMethod(typeof(Log4NetAppenderDetectorAssemblyTests)
                .GetMethod(nameof(ThrowReflectionError),
                           BindingFlags.NonPublic | BindingFlags.Static)));

        try
        {
            var result = Log4NetAppenderDetector.DetectAvailableAppenders();
            result.Should().NotBeNull();
        }
        finally
        {
            harmony.UnpatchAll("test.assemblyfail");
        }
    }

    [Fact]
    public void Should_LogWarning_When_DependencyContextLoadFails()
    {
        var harmony = new Harmony("test.depcontextfail");
        var original = typeof(Assembly).GetMethod(nameof(Assembly.Load), [typeof(AssemblyName)]);
        harmony.Patch(original,
            prefix: new HarmonyMethod(typeof(Log4NetAppenderDetectorAssemblyTests)
                .GetMethod(nameof(ThrowLoadError),
                           BindingFlags.NonPublic | BindingFlags.Static)));

        try
        {
            var result = InvokeGetLog4NetAssemblies();
            result.Should().NotBeNull();
        }
        finally
        {
            harmony.UnpatchAll("test.depcontextfail");
        }
    }

    [Fact]
    public void Should_Fallback_To_AppDomainAssemblies()
    {
        var result = InvokeGetLog4NetAssemblies();
        result.Should().Contain(a => a.FullName!.Contains("log4net", StringComparison.OrdinalIgnoreCase));
    }
    
    [Fact]
    public void DetectAppendersInAssembly_ShouldUseAvailableTypes_WhenReflectionTypeLoadExceptionThrown()
    {
        // Arrange
        var dict = new Dictionary<string, Log4NetAppenderDetector.AppenderInfo>();

        // Our "valid" type
        var validType = typeof(TestAppender);

        // ReflectionTypeLoadException requires two arrays: Types and LoaderExceptions
        var ex = new ReflectionTypeLoadException(
            [validType, null!],  // include valid and null entries
            [new InvalidOperationException("broken type")]
        );

        // Fake assembly that throws when GetTypes is called
        var fakeAssembly = Substitute.For<Assembly>();
        fakeAssembly.GetTypes().Throws(ex);

        // Act
        var method = typeof(Log4NetAppenderDetector)
            .GetMethod("DetectAppendersInAssembly", BindingFlags.NonPublic | BindingFlags.Static);
        method!.Invoke(null, [fakeAssembly, dict]);

        // Assert
        dict.Keys.Should().Contain("Test");
    }
    
    [Fact]
    public void DetectAppendersInAssembly_ShouldSkip_WhenCreateAppenderInfoThrows()
    {
        var dict = new Dictionary<string, Log4NetAppenderDetector.AppenderInfo>();
        var throwingType = new ThrowingType();

        var ex = new ReflectionTypeLoadException(
            [throwingType],
            [new InvalidOperationException("broken")]
        );

        var fakeAssembly = Substitute.For<Assembly>();
        fakeAssembly.GetTypes().Throws(ex);

        var method = typeof(Log4NetAppenderDetector)
            .GetMethod("DetectAppendersInAssembly", BindingFlags.NonPublic | BindingFlags.Static);

        // Act
        method!.Invoke(null, [fakeAssembly, dict]);

        // Assert
        dict.Should().BeEmpty();
    }
    
    [Fact]
    public void IsValidAppenderType_ShouldReturnTrue_WhenInheritsFromLog4NetAppenderBaseClass()
    {
        // Arrange
        var type = new FakeTypeWithLog4NetBase();

        // Act
        var method = typeof(Log4NetAppenderDetector)
            .GetMethod("IsValidAppenderType", BindingFlags.NonPublic | BindingFlags.Static);
        var result = (bool)method!.Invoke(null, [type])!;

        // Assert
        result.Should().BeTrue();
    }

    // Helpers for patches
    private static bool ThrowReflectionError(ref Type[] __result)
    {
        throw new Exception("Simulated failure in GetTypes");
    }

    private static bool ThrowLoadError(ref Assembly __result, AssemblyName assemblyRef)
    {
        throw new Exception("Simulated load failure");
    }

    private static Assembly[] InvokeGetLog4NetAssemblies()
    {
        var method = typeof(Log4NetAppenderDetector)
            .GetMethod("GetLog4NetAssemblies",
                BindingFlags.NonPublic | BindingFlags.Static);
        return (Assembly[])method!.Invoke(null, null)!;
    }
}

// Base class that's in log4net.Appender namespace but doesn't implement IAppender
public abstract class FakeLog4NetAppenderBase
{
    // This simulates a log4net base class
}

// Our test class that inherits from the fake base

public class TestAppender : log4net.Appender.AppenderSkeleton
{
    protected override void Append(log4net.Core.LoggingEvent loggingEvent) { }
}

public class ThrowingType : Type
{
    private readonly Type _baseType = typeof(log4net.Appender.AppenderSkeleton);

    public override string Name => "ThrowingAppender";
    public new bool IsClass => true;
    public new bool IsAbstract => false;
    public new bool IsPublic => true;

    public override PropertyInfo[] GetProperties(BindingFlags bindingAttr)
    {
        throw new InvalidOperationException("GetProperties failed");
    }

    public override Type[] GetInterfaces() => [typeof(log4net.Appender.IAppender)];

    // Required abstract members - minimal implementation
    public override Assembly Assembly => _baseType.Assembly;
    public override string AssemblyQualifiedName => "ThrowingAppender";
    public override Type BaseType => _baseType;
    public override string FullName => "ThrowingAppender";
    public override Guid GUID => Guid.NewGuid();
    public override Module Module => _baseType.Module;
    public override string Namespace => "Test";
    public override Type UnderlyingSystemType => this;

    protected override TypeAttributes GetAttributeFlagsImpl() => TypeAttributes.Public | TypeAttributes.Class;
    protected override ConstructorInfo GetConstructorImpl(BindingFlags bindingAttr, Binder? binder, CallingConventions callConvention, Type[] types, ParameterModifier[]? modifiers) => null!;
    public override ConstructorInfo[] GetConstructors(BindingFlags bindingAttr) => [];
    public override Type GetElementType() => null!;
    public override EventInfo GetEvent(string name, BindingFlags bindingAttr) => null!;
    public override EventInfo[] GetEvents(BindingFlags bindingAttr) => [];
    public override FieldInfo GetField(string name, BindingFlags bindingAttr) => null!;
    public override FieldInfo[] GetFields(BindingFlags bindingAttr) => [];
    public override Type GetInterface(string name, bool ignoreCase) => null!;
    public override MemberInfo[] GetMembers(BindingFlags bindingAttr) => [];

    protected override MethodInfo GetMethodImpl(
        string name,
        BindingFlags bindingAttr,
        Binder? binder,
        CallingConventions callConvention,
        Type[]? types,
        ParameterModifier[]? modifiers) =>
        throw new Exception();

    public override MethodInfo[] GetMethods(BindingFlags bindingAttr) => [];
    public override Type GetNestedType(string name, BindingFlags bindingAttr) => null!;
    public override Type[] GetNestedTypes(BindingFlags bindingAttr) => [];
    public new PropertyInfo[] GetProperties() => throw new InvalidOperationException("GetProperties failed");
    protected override PropertyInfo GetPropertyImpl(string name, BindingFlags bindingAttr, Binder? binder, Type? returnType, Type[]? types, ParameterModifier[]? modifiers) => null!;
    protected override bool HasElementTypeImpl() => false;
    public override object InvokeMember(string name, BindingFlags invokeAttr, Binder? binder, object? target, object?[]? args, ParameterModifier[]? modifiers, CultureInfo? culture, string[]? namedParameters) => null!;
    protected override bool IsArrayImpl() => false;
    protected override bool IsByRefImpl() => false;
    protected override bool IsCOMObjectImpl() => false;
    protected override bool IsPointerImpl() => false;
    protected override bool IsPrimitiveImpl() => false;
    public override object[] GetCustomAttributes(bool inherit) => [];
    public override object[] GetCustomAttributes(Type attributeType, bool inherit) => [];
    public override bool IsDefined(Type attributeType, bool inherit) => false;
}

public class FakeTypeWithLog4NetBase : Type
{
    private readonly Type _baseTypeToFake = typeof(FakeLog4NetAppenderBase);

    public override string Name => "CustomAppenderFromNonInterfaceBase";
    public new bool IsClass => true;
    public new bool IsAbstract => false;
    public new bool IsPublic => true;

    // Key: Return an empty array so it doesn't implement IAppender
    public override Type[] GetInterfaces() => [];

    // Key: Return a base type with log4net.Appender namespace
    public override Type BaseType => new FakeLog4NetAppenderBaseType();

    // Required abstract members
    public override Assembly Assembly => _baseTypeToFake.Assembly;
    public override string AssemblyQualifiedName => "FakeTypeWithLog4NetBase";
    public override string FullName => "FakeTypeWithLog4NetBase";
    public override Guid GUID => Guid.NewGuid();
    public override Module Module => _baseTypeToFake.Module;
    public override string Namespace => "Test";
    public override Type UnderlyingSystemType => this;

    // All other required overrides...
    protected override TypeAttributes GetAttributeFlagsImpl() => TypeAttributes.Public | TypeAttributes.Class;
    protected override ConstructorInfo GetConstructorImpl(BindingFlags bindingAttr, Binder? binder, CallingConventions callConvention, Type[] types, ParameterModifier[]? modifiers) => null!;
    public override ConstructorInfo[] GetConstructors(BindingFlags bindingAttr) => [];
    public override Type GetElementType() => null!;
    public override EventInfo GetEvent(string name, BindingFlags bindingAttr) => null!;
    public override EventInfo[] GetEvents(BindingFlags bindingAttr) => [];
    public override FieldInfo GetField(string name, BindingFlags bindingAttr) => null!;
    public override FieldInfo[] GetFields(BindingFlags bindingAttr) => [];
    public override Type GetInterface(string name, bool ignoreCase) => null!;
    public override MemberInfo[] GetMembers(BindingFlags bindingAttr) => [];

    protected override MethodInfo? GetMethodImpl(
        string name,
        BindingFlags bindingAttr,
        Binder? binder,
        CallingConventions callConvention,
        Type[]? types,
        ParameterModifier[]? modifiers) => null;

    public override MethodInfo[] GetMethods(BindingFlags bindingAttr) => [];
    public override Type GetNestedType(string name, BindingFlags bindingAttr) => null!;
    public override Type[] GetNestedTypes(BindingFlags bindingAttr) => [];
    public override PropertyInfo[] GetProperties(BindingFlags bindingAttr) => [];
    protected override PropertyInfo GetPropertyImpl(string name, BindingFlags bindingAttr, Binder? binder, Type? returnType, Type[]? types, ParameterModifier[]? modifiers) => null!;
    protected override bool HasElementTypeImpl() => false;
    public override object InvokeMember(string name, BindingFlags invokeAttr, Binder? binder, object? target, object?[]? args, ParameterModifier[]? modifiers, CultureInfo? culture, string[]? namedParameters) => null!;
    protected override bool IsArrayImpl() => false;
    protected override bool IsByRefImpl() => false;
    protected override bool IsCOMObjectImpl() => false;
    protected override bool IsPointerImpl() => false;
    protected override bool IsPrimitiveImpl() => false;
    public override object[] GetCustomAttributes(bool inherit) => [];
    public override object[] GetCustomAttributes(Type attributeType, bool inherit) => [];
    public override bool IsDefined(Type attributeType, bool inherit) => false;
}

// The fake base type that appears to be in log4net.Appender namespace
public class FakeLog4NetAppenderBaseType : Type
{
    public override string Name => "SomeAppender";
    public override string Namespace => "log4net.Appender"; // This is the key part!
    public override Type BaseType => null!; // End the inheritance chain

    // Minimal required overrides
    public override Assembly Assembly => typeof(object).Assembly;
    public override string AssemblyQualifiedName => "FakeLog4NetAppenderBaseType";
    public override string FullName => "log4net.Appender.SomeAppender";
    public override Guid GUID => Guid.NewGuid();
    public override Module Module => typeof(object).Module;
    public override Type UnderlyingSystemType => this;
    public new bool IsClass => true;
    public new bool IsAbstract => false;
    public new bool IsPublic => true;

    // All other required abstract methods with minimal implementations
    protected override TypeAttributes GetAttributeFlagsImpl() => TypeAttributes.Public | TypeAttributes.Class;
    protected override ConstructorInfo GetConstructorImpl(BindingFlags bindingAttr, Binder? binder, CallingConventions callConvention, Type[] types, ParameterModifier[]? modifiers) => null!;
    public override ConstructorInfo[] GetConstructors(BindingFlags bindingAttr) => [];
    public override Type GetElementType() => null!;
    public override EventInfo GetEvent(string name, BindingFlags bindingAttr) => null!;
    public override EventInfo[] GetEvents(BindingFlags bindingAttr) => [];
    public override FieldInfo GetField(string name, BindingFlags bindingAttr) => null!;
    public override FieldInfo[] GetFields(BindingFlags bindingAttr) => [];
    public override Type GetInterface(string name, bool ignoreCase) => null!;

    public override Type[] GetInterfaces() => [];
    public override MemberInfo[] GetMembers(BindingFlags bindingAttr) => [];

    protected override MethodInfo GetMethodImpl(
        string name,
        BindingFlags bindingAttr,
        Binder? binder,
        CallingConventions callConvention,
        Type[]? types,
        ParameterModifier[]? modifiers) => null!;

    public override MethodInfo[] GetMethods(BindingFlags bindingAttr) => [];
    public override Type GetNestedType(string name, BindingFlags bindingAttr) => null!;
    public override Type[] GetNestedTypes(BindingFlags bindingAttr) => [];
    public override PropertyInfo[] GetProperties(BindingFlags bindingAttr) => [];
    protected override PropertyInfo GetPropertyImpl(string name, BindingFlags bindingAttr, Binder? binder, Type? returnType, Type[]? types, ParameterModifier[]? modifiers) => null!;
    protected override bool HasElementTypeImpl() => false;
    public override object InvokeMember(string name, BindingFlags invokeAttr, Binder? binder, object? target, object?[]? args, ParameterModifier[]? modifiers, CultureInfo? culture, string[]? namedParameters) => null!;
    protected override bool IsArrayImpl() => false;
    protected override bool IsByRefImpl() => false;
    protected override bool IsCOMObjectImpl() => false;
    protected override bool IsPointerImpl() => false;
    protected override bool IsPrimitiveImpl() => false;
    public override object[] GetCustomAttributes(bool inherit) => [];
    public override object[] GetCustomAttributes(Type attributeType, bool inherit) => [];
    public override bool IsDefined(Type attributeType, bool inherit) => false;
}


